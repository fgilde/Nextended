using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Eventing;
using Azure.Provisioning.AppContainers;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Aspire.Hosting.Grafana;
using Nextended.Aspire.Hosting.Observability;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// The observability stack on Azure Container Apps. Locally everything stays as before (bind
/// mounts, HTTP endpoints). Deployed, a bind mount is impossible — azd fails the bicep step on it —
/// so configs are baked into images; the internal components are TCP, which keeps the Docker-network
/// addresses (<c>name:port</c>) of every generated config valid; Loki and Tempo store into MinIO,
/// Grafana into its own database of the Supabase Postgres and reads through a read-only login.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public sealed class ObservabilityStackTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"obs-{Guid.NewGuid():N}");

    public ObservabilityStackTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "grafana", "dashboards"));
        File.WriteAllText(Path.Combine(_root, "grafana", "dashboards", "app.json"), "{\"title\":\"app\"}");
    }

    public void Dispose()
    {
        SupabaseBuilderExtensions.StorageS3Backend = null;
        SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.AzureContainerApps;
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private IDistributedApplicationBuilder Stack(bool publish, Action<ObservabilityStackOptions>? configure = null)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = publish ? ["--operation", "publish", "--publisher", "manifest"] : [],
        });
        // Run mode's own BeforeStartEvent handlers validate the DCP paths; nothing gets started here.
        builder.Configuration["DcpPublisher:CliPath"] = "unused";
        builder.Configuration["DcpPublisher:DashboardPath"] = "unused";
        // MinIO has to exist before AddSupabase reads its S3 backend.
        if (publish) builder.AddMinioS3OnNfs("nfs");
        var supabase = builder.AddSupabase("sb");
        supabase.WithKongOpenTelemetry();
        builder.AddObservabilityStack(supabase, options =>
        {
            options.ConfigRootPath = _root;
            options.DashboardsPath = Path.Combine(_root, "grafana", "dashboards");
            options.IncludeTempo = true;
            options.IncludeOtelCollector = true;
            configure?.Invoke(options);
        });
        return builder;
    }

    /// <summary>Runs what happens right before start/publish: the stack writes its configs (and bakes them when publishing).</summary>
    private static async Task GenerateConfigs(IDistributedApplicationBuilder builder)
    {
        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await app.Services.GetRequiredService<IDistributedApplicationEventing>()
            .PublishAsync(new BeforeStartEvent(app.Services, model));
    }

    private static IResource Resource(IDistributedApplicationBuilder builder, string name) =>
        builder.Resources.Single(r => r.Name == name);

    private static EndpointAnnotation[] Endpoints(IDistributedApplicationBuilder builder, string name) =>
        Resource(builder, name).Annotations.OfType<EndpointAnnotation>().ToArray();

    private static async Task<Dictionary<string, string>> Env(IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable CS0618 // no model-only equivalent yet
        return await ((IResourceWithEnvironment)Resource(builder, name)).GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618
    }

    private string Baked(string resource, int index) =>
        File.ReadAllText(Path.Combine(_root, ".generated", "publish", resource, "files", index.ToString()));

    private static readonly string[] ConfigComponents =
        ["monitoring-grafana", "monitoring-prometheus", "monitoring-loki", "monitoring-tempo", "monitoring-otel-collector"];

    [Fact]
    public void Locally_configs_stay_bind_mounted_and_endpoints_plain_http()
    {
        var builder = Stack(publish: false);

        Assert.Contains(Resource(builder, "monitoring-prometheus").Annotations.OfType<ContainerMountAnnotation>(),
            m => m.Type == ContainerMountType.BindMount && m.Target == "/etc/prometheus/prometheus.yml");
        Assert.All(Endpoints(builder, "monitoring-prometheus"), e => Assert.Equal("http", e.UriScheme));
        Assert.Equal(["otlp-grpc", "otlp-http"], Endpoints(builder, "monitoring-otel-collector").Select(e => e.Name).Order());
    }

    [Fact]
    public void Deployed_internal_components_are_tcp_on_their_own_ports()
    {
        var builder = Stack(publish: true);

        foreach (var (name, port) in new[]
                 {
                     ("monitoring-prometheus", 9090), ("monitoring-loki", 3100), ("monitoring-tempo", 3200),
                     ("monitoring-otel-collector", 4318), ("monitoring-postgres-exporter", 9187),
                 })
        {
            var main = Endpoints(builder, name)[0];
            Assert.Equal("tcp", main.UriScheme);
            Assert.Equal(port, main.Port);
            Assert.Equal(port, main.TargetPort);
        }
        // gRPC on the collector would collide with Tempo's 4317 — TCP ports are unique per environment.
        Assert.Equal(["otlp-http"], Endpoints(builder, "monitoring-otel-collector").Select(e => e.Name));
        Assert.Equal("http", Endpoints(builder, "monitoring-grafana")[0].UriScheme);
    }

    [Fact]
    public async Task Deployed_configs_are_baked_into_images_built_from_the_final_image()
    {
        var builder = Stack(publish: true, options =>
        {
            options.PrometheusImage = "team/prometheus";
            options.PrometheusImageTag = "9.9.9";
        });
        await GenerateConfigs(builder);

        foreach (var name in ConfigComponents)
        {
            Assert.DoesNotContain(Resource(builder, name).Annotations.OfType<ContainerMountAnnotation>(),
                m => m.Type == ContainerMountType.BindMount);
            Assert.Contains(Resource(builder, name).Annotations, a => a.GetType().Name == "DockerfileBuildAnnotation");
        }

        var prometheus = File.ReadAllText(Path.Combine(_root, ".generated", "publish", "monitoring-prometheus", "Dockerfile"));
        Assert.StartsWith("FROM team/prometheus:9.9.9", prometheus);
        Assert.Contains("COPY files/0 /etc/prometheus/prometheus.yml", prometheus);

        var grafana = File.ReadAllText(Path.Combine(_root, ".generated", "publish", "monitoring-grafana", "Dockerfile"));
        Assert.Contains("COPY files/0/ /etc/grafana/provisioning/", grafana);
        Assert.Contains("COPY files/1/ /var/lib/grafana/dashboards/", grafana);
        Assert.True(File.Exists(Path.Combine(_root, ".generated", "publish", "monitoring-grafana", "files", "1", "app.json")));
    }

    [Fact]
    public async Task No_secret_is_baked_into_an_image()
    {
        // Images end up in a registry; credentials have to stay in the container app's environment.
        var builder = Stack(publish: true);
        await GenerateConfigs(builder);

        var baked = Directory.EnumerateFiles(Path.Combine(_root, ".generated", "publish"), "*", SearchOption.AllDirectories)
            .Select(File.ReadAllText).ToArray();
        Assert.DoesNotContain(baked, f => f.Contains("Minio-Nfs-2026-secure!"));
        Assert.Contains(baked, f => f.Contains("${LOKI_S3_SECRET_KEY}"));
    }

    [Fact]
    public async Task Deployed_loki_and_tempo_keep_their_data_in_minio()
    {
        var builder = Stack(publish: true);
        await GenerateConfigs(builder);

        var loki = await Env(builder, "monitoring-loki");
        Assert.True(loki.ContainsKey("LOKI_S3_ENDPOINT"));
        Assert.Contains("-config.expand-env=true", ((ContainerResource)Resource(builder, "monitoring-loki")).Annotations
            .OfType<CommandLineArgsCallbackAnnotation>().SelectMany(_ => ArgsOf(builder, "monitoring-loki")));
        Assert.Contains("object_store: s3", Baked("monitoring-loki", 0));
        Assert.Contains("backend: s3", Baked("monitoring-tempo", 0));
        Assert.Contains("bucket: tempo", Baked("monitoring-tempo", 0));

        var init = await Env(builder, "minio-init");
        Assert.Equal("loki tempo", init["EXTRA_BUCKETS"]);
    }

    private static IEnumerable<string> ArgsOf(IDistributedApplicationBuilder builder, string name)
    {
        var resource = Resource(builder, name);
        var args = new List<object>();
        var context = new CommandLineArgsCallbackContext(args) { ExecutionContext = builder.ExecutionContext };
        foreach (var callback in resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
            callback.Callback(context).GetAwaiter().GetResult();
        return args.Select(a => a.ToString() ?? "");
    }

    [Fact]
    public async Task Deployed_grafana_has_its_own_database_and_a_read_only_datasource_login()
    {
        var builder = Stack(publish: true);
        await GenerateConfigs(builder);

        var grafana = await Env(builder, "monitoring-grafana");
        Assert.Equal("postgres", grafana["GF_DATABASE_TYPE"]);
        Assert.Equal(ObservabilityStack.GrafanaRole, grafana["GF_DATABASE_USER"]);
        Assert.Matches("^[0-9a-f]{32}$", grafana["GF_DATABASE_PASSWORD"]);
        Assert.Matches("^[0-9a-f]{32}$", grafana["APP_DB_PASSWORD"]);
        Assert.NotEqual(grafana["GF_DATABASE_PASSWORD"], grafana["APP_DB_PASSWORD"]);
        Assert.Contains($"user: '{ObservabilityStack.ReaderRole}'",
            File.ReadAllText(Path.Combine(_root, ".generated", "publish", "monitoring-grafana", "files", "0", "datasources", "datasources.yml")));

        var sql = PostInitSql(await Env(builder, "sb-init"));
        Assert.Contains($"CREATE ROLE {ObservabilityStack.ReaderRole} LOGIN BYPASSRLS PASSWORD '{grafana["APP_DB_PASSWORD"]}'", sql);
        Assert.Contains($"CREATE ROLE {ObservabilityStack.GrafanaRole} LOGIN PASSWORD '{grafana["GF_DATABASE_PASSWORD"]}'", sql);
        Assert.Contains($"GRANT pg_read_all_data TO {ObservabilityStack.ReaderRole};", sql);
        Assert.Contains("\\gexec", sql);
    }

    private static string PostInitSql(Dictionary<string, string> env)
    {
        var parts = int.Parse(env.GetValueOrDefault("POST_INIT_SQL_GZ_PARTS", "1"));
        var blob = string.Concat(Enumerable.Range(0, parts).Select(i => env[$"POST_INIT_SQL_GZ_BASE64_{i}"]));
        using var gz = new System.IO.Compression.GZipStream(new MemoryStream(Convert.FromBase64String(blob)), System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(gz);
        return reader.ReadToEnd();
    }

    [Fact]
    public async Task Locally_grafana_keeps_sqlite_and_the_regular_login()
    {
        var builder = Stack(publish: false);
        var grafana = await Env(builder, "monitoring-grafana");
        Assert.False(grafana.ContainsKey("GF_DATABASE_TYPE"));
        Assert.False(grafana.ContainsKey("GF_AUTH_AZUREAD_ENABLED"));
    }

    [Fact]
    public async Task Entra_sign_in_is_the_only_way_into_a_deployed_grafana()
    {
        var builder = Stack(publish: true, options =>
        {
            options.GrafanaEntraId = new GrafanaEntraIdOptions
            {
                TenantId = "tenant-1",
                ClientId = "client-1",
                ClientSecret = ReferenceExpression.Create($"secret-1"),
            };
        });

        var grafana = await Env(builder, "monitoring-grafana");
        Assert.Equal("true", grafana["GF_AUTH_AZUREAD_ENABLED"]);
        Assert.Equal("client-1", grafana["GF_AUTH_AZUREAD_CLIENT_ID"]);
        Assert.Equal("tenant-1", grafana["GF_AUTH_AZUREAD_ALLOWED_ORGANIZATIONS"]);
        Assert.Equal("https://login.microsoftonline.com/tenant-1/oauth2/v2.0/authorize", grafana["GF_AUTH_AZUREAD_AUTH_URL"]);
        Assert.Equal("true", grafana["GF_AUTH_AZUREAD_ROLE_ATTRIBUTE_STRICT"]);
        Assert.Equal("true", grafana["GF_AUTH_DISABLE_LOGIN_FORM"]);
        Assert.Equal("false", grafana["GF_AUTH_BASIC_ENABLED"]);
        Assert.Equal("false", grafana["GF_AUTH_ANONYMOUS_ENABLED"]);
        // No admin/admin from Grafana's first start; server admins come from the app role GrafanaAdmin.
        Assert.Equal("true", grafana["GF_SECURITY_DISABLE_INITIAL_ADMIN_CREATION"]);
        Assert.Equal("true", grafana["GF_AUTH_AZUREAD_ALLOW_ASSIGN_GRAFANA_ADMIN"]);
        Assert.True(grafana.ContainsKey("GF_SERVER_ROOT_URL"));
    }

    [Fact]
    public async Task Keycloak_sign_in_finds_its_endpoints_in_the_realm_and_locks_grafana_down_the_same_way()
    {
        var builder = Stack(publish: true, options => options.GrafanaOAuth = GrafanaOAuthOptions.Keycloak(
            "https://sso.example.com/realms/acme/", "grafana", ReferenceExpression.Create($"kc-secret")));

        var grafana = await Env(builder, "monitoring-grafana");
        Assert.Equal("true", grafana["GF_AUTH_GENERIC_OAUTH_ENABLED"]);
        Assert.Equal("Keycloak", grafana["GF_AUTH_GENERIC_OAUTH_NAME"]);
        Assert.Equal("grafana", grafana["GF_AUTH_GENERIC_OAUTH_CLIENT_ID"]);
        Assert.Equal("kc-secret", grafana["GF_AUTH_GENERIC_OAUTH_CLIENT_SECRET"]);
        Assert.Equal("https://sso.example.com/realms/acme/protocol/openid-connect/auth", grafana["GF_AUTH_GENERIC_OAUTH_AUTH_URL"]);
        Assert.Equal("https://sso.example.com/realms/acme/protocol/openid-connect/token", grafana["GF_AUTH_GENERIC_OAUTH_TOKEN_URL"]);
        Assert.Equal("https://sso.example.com/realms/acme/protocol/openid-connect/userinfo", grafana["GF_AUTH_GENERIC_OAUTH_API_URL"]);
        Assert.Equal(GrafanaOAuthOptions.Roles("roles", ["grafana-admin"], ["admin"], ["editor"], ["viewer"]),
            grafana["GF_AUTH_GENERIC_OAUTH_ROLE_ATTRIBUTE_PATH"]);
        Assert.Equal("true", grafana["GF_AUTH_GENERIC_OAUTH_ROLE_ATTRIBUTE_STRICT"]);
        Assert.Equal("true", grafana["GF_AUTH_GENERIC_OAUTH_USE_PKCE"]);
        Assert.Equal("true", grafana["GF_AUTH_DISABLE_LOGIN_FORM"]);
        Assert.Equal("false", grafana["GF_AUTH_BASIC_ENABLED"]);
        Assert.Equal("false", grafana["GF_AUTH_ANONYMOUS_ENABLED"]);
        Assert.Equal("true", grafana["GF_SECURITY_DISABLE_INITIAL_ADMIN_CREATION"]);
        Assert.True(grafana.ContainsKey("GF_SERVER_ROOT_URL"));
        Assert.False(grafana.ContainsKey("GF_AUTH_AZUREAD_ENABLED"));
    }

    [Fact]
    public void A_role_path_puts_the_highest_role_first_and_survives_a_missing_claim()
    {
        Assert.Equal(
            "contains(groups[*] || `[]`, 'ops') && 'GrafanaAdmin' || contains(groups[*] || `[]`, 'it\\'s') && 'Viewer'",
            GrafanaOAuthOptions.Roles("groups", grafanaAdmins: ["ops"], viewers: ["it's"]));
        Assert.Throws<ArgumentException>(() => GrafanaOAuthOptions.Roles("groups"));
    }

    [Fact]
    public void Only_real_provider_urls_are_accepted()
    {
        Assert.Throws<ArgumentException>(() => GrafanaOAuthOptions.Keycloak(
            "https://sso.example.com/auth", "grafana", ReferenceExpression.Create($"s")));

        var builder = Stack(publish: false);
        var grafana = builder.CreateResourceBuilder(builder.Resources.OfType<GrafanaResource>().Single());
        Assert.Throws<ArgumentException>(() => grafana.WithOAuthLogin(new GrafanaOAuthOptions
        {
            Name = "x", ClientId = "c", ClientSecret = ReferenceExpression.Create($"s"),
            AuthUrl = "sso.example.com/auth", TokenUrl = "https://sso/token", ApiUrl = "https://sso/userinfo",
            RoleAttributePath = "'Viewer'",
        }));
    }

    [Fact]
    public async Task Deployed_kong_actually_sends_its_traces_to_the_collector()
    {
        // Regression: the publish template was frozen when Kong was created, before
        // WithKongOpenTelemetry — deployed Kong had the plugin enabled but no endpoint.
        var builder = Stack(publish: true);

        var kong = await Env(builder, "sb-kong");
        var template = Encoding.UTF8.GetString(Convert.FromBase64String(kong["KONG_CONFIG_TEMPLATE_BASE64"]));
        Assert.Contains("- name: opentelemetry", template);
        Assert.Contains("traces_endpoint: http://monitoring-otel-collector:4318/v1/traces", template);
    }

    [Fact]
    public async Task Locally_the_loki_config_stays_byte_for_byte_what_it_was()
    {
        // Same template as the S3 variant now; run mode must still get the old filesystem config.
        var local = Stack(publish: false);
        await GenerateConfigs(local);

        Assert.Equal(LocalLokiConfig.ReplaceLineEndings("\n"),
            File.ReadAllText(Path.Combine(_root, ".generated", "loki-config.yml")).ReplaceLineEndings("\n"));
    }

    private const string LocalLokiConfig = """
        auth_enabled: false

        server:
          http_listen_port: 3100
          grpc_listen_port: 9096
          log_level: warn

        common:
          instance_addr: 127.0.0.1
          path_prefix: /loki
          storage:
            filesystem:
              chunks_directory: /loki/chunks
              rules_directory: /loki/rules
          replication_factor: 1
          ring:
            kvstore:
              store: inmemory

        schema_config:
          configs:
            - from: 2024-01-01
              store: tsdb
              object_store: filesystem
              schema: v13
              index:
                prefix: index_
                period: 24h

        limits_config:
          retention_period: 336h
          reject_old_samples: true
          reject_old_samples_max_age: 168h
          allow_structured_metadata: true

        compactor:
          working_directory: /loki/compactor
          retention_enabled: true
          retention_delete_delay: 2h
          delete_request_store: filesystem
        """;

    [Fact]
    public async Task The_deployed_collector_drops_the_local_dashboard_mirror_and_logs_quietly()
    {
        var local = Stack(publish: false);
        await GenerateConfigs(local);
        var localConfig = File.ReadAllText(Path.Combine(_root, ".generated", "otel-collector-config.yml"));
        Assert.Contains("otlp/aspire", localConfig);
        Assert.Contains("verbosity: detailed", localConfig);

        // Deployed, stdout is billed log ingestion: one line per batch instead of every span.
        var deployed = Stack(publish: true);
        await GenerateConfigs(deployed);
        Assert.DoesNotContain("otlp/aspire", Baked("monitoring-otel-collector", 0));
        Assert.Contains("verbosity: basic", Baked("monitoring-otel-collector", 0));
    }

    [Fact]
    public void Deployed_components_run_as_one_replica_and_internal_ones_expose_their_port()
    {
        var builder = Stack(publish: true);
        foreach (var name in ConfigComponents.Append("monitoring-postgres-exporter"))
        {
            var app = new ContainerApp("probe");
            var customizations = Resource(builder, name).Annotations.OfType<AzureContainerAppCustomizationAnnotation>().ToArray();
            Assert.NotEmpty(customizations);
            foreach (var customization in customizations) customization.Configure(null!, app);

            Assert.Equal(1, app.Template.Scale.MinReplicas.Value);
            Assert.Equal(1, app.Template.Scale.MaxReplicas.Value);
            // Grafana is the one external HTTP app; an exposed port would turn it into TCP-only.
            if (name == "monitoring-grafana") Assert.True(app.Configuration.Ingress.ExposedPort.IsEmpty);
            else Assert.Equal(Endpoints(builder, name)[0].TargetPort, app.Configuration.Ingress.ExposedPort.Value);
        }
    }
}
