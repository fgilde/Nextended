using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.SigNoz.Tests;

/// <summary>The stack AddSigNoz builds, its sign-in variants, the files it writes and the content it ships.</summary>
public class SigNozTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "signoz-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private IResourceBuilder<SigNozResource> Add(IDistributedApplicationBuilder? builder = null) =>
        (builder ?? DistributedApplication.CreateBuilder()).AddSigNoz("signoz", _root);

    private static IDistributedApplicationBuilder PublishBuilder() =>
        DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest", "--output-path", "."],
        });

    /// <summary>Environment of a resource: plain values as they are, references as their manifest expression.</summary>
    private static async Task<Dictionary<string, string>> EnvOf(IResource resource, bool publishing = false)
    {
        var context = new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(
            publishing ? DistributedApplicationOperation.Publish : DistributedApplicationOperation.Run));
        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            try { await annotation.Callback(context); }
            catch (InvalidOperationException) { } // an endpoint URL that is allocated only when running
        }

        return context.EnvironmentVariables.ToDictionary(e => e.Key, e => e.Value switch
        {
            string text => text,
            IManifestExpressionProvider expression => expression.ValueExpression,
            _ => e.Value?.ToString() ?? "",
        });
    }

    private static IEnumerable<EndpointAnnotation> Endpoints(IResource resource) => resource.Annotations.OfType<EndpointAnnotation>();

    [Fact]
    public void AddSigNoz_adds_ClickHouse_the_collector_and_SigNoz()
    {
        var signoz = Add();
        var resources = signoz.ApplicationBuilder.Resources;

        Assert.Single(resources.OfType<SigNozResource>());
        Assert.Equal("signoz-clickhouse", Assert.Single(resources.OfType<SigNozClickHouseResource>()).Name);
        Assert.Equal("signoz-collector", Assert.Single(resources.OfType<SigNozCollectorResource>()).Name);
        Assert.Same(signoz.Resource.ClickHouse, resources.OfType<SigNozClickHouseResource>().Single());
        Assert.Null(signoz.Resource.SignIn);

        Assert.Equal("signoz/signoz", signoz.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
        Assert.Contains(Endpoints(signoz.Resource), e => e.Name == "http" && e.TargetPort == 8080 && e.IsExternal);
        Assert.Contains(Endpoints(signoz.Resource.Collector), e => e.Name == "otlp-grpc" && e.TargetPort == 4317);
        Assert.Contains(Endpoints(signoz.Resource.Collector), e => e.Name == "otlp-http" && e.TargetPort == 4318);
    }

    [Fact]
    public async Task SigNoz_and_the_collector_reach_ClickHouse_by_its_native_endpoint()
    {
        var signoz = Add();

        var env = await EnvOf(signoz.Resource);
        Assert.Contains("signoz-clickhouse.bindings.native", env["SIGNOZ_TELEMETRYSTORE_CLICKHOUSE_DSN"]);
        Assert.StartsWith("tcp://", env["SIGNOZ_TELEMETRYSTORE_CLICKHOUSE_DSN"]);
        Assert.Equal("true", env["SIGNOZ_USER_ROOT_ENABLED"]);
        Assert.Equal("admin@signoz.local", env["SIGNOZ_USER_ROOT_EMAIL"]);
        Assert.Equal("false", env["SIGNOZ_STATSREPORTER_ENABLED"]);

        var collector = await EnvOf(signoz.Resource.Collector);
        Assert.Contains("signoz-clickhouse.bindings.native", collector["CLICKHOUSE_ENDPOINT"]);
        var args = signoz.Resource.Collector.Annotations.OfType<CommandLineArgsCallbackAnnotation>().ToList();
        Assert.NotEmpty(args);
    }

    [Fact]
    public void The_default_admin_password_is_a_generated_secret_parameter()
    {
        var signoz = Add();
        var password = signoz.ApplicationBuilder.Resources.OfType<ParameterResource>().Single(p => p.Name == "signoz-password");
        Assert.True(password.Secret);
    }

    [Theory]
    [InlineData("Sample-Passw0rd!", true)]
    [InlineData("123Pa$$word!", true)]
    [InlineData("short-Pa1!", false)]
    [InlineData("nouppercase-pa55!", false)]
    [InlineData("NoSymbolPassw0rd", false)]
    [InlineData("Unknown-Symbol'Passw0rd", false)]
    public void Passwords_follow_the_rule_of_SigNoz(string password, bool valid) =>
        Assert.Equal(valid, SigNozBuilderExtensions.IsValidAdminPassword(password));

    [Fact]
    public async Task WithAdminUser_sets_the_root_user_and_refuses_what_SigNoz_would_refuse()
    {
        var signoz = Add().WithAdminUser("ops@example.com", "Sample-Passw0rd!", "Shop");

        var env = await EnvOf(signoz.Resource);
        Assert.Equal("ops@example.com", env["SIGNOZ_USER_ROOT_EMAIL"]);
        Assert.Equal("Sample-Passw0rd!", env["SIGNOZ_USER_ROOT_PASSWORD"]);
        Assert.Equal("Shop", env["SIGNOZ_USER_ROOT_ORG_NAME"]);

        Assert.Throws<ArgumentException>(() => Add().WithAdminUser("ops@example.com", "password"));
        Assert.Throws<ArgumentException>(() => Add().WithAdminUser("admin@localhost", "Sample-Passw0rd!"));
    }

    [Fact]
    public async Task WithAnonymousAdmin_turns_on_impersonation_and_off_tokens_and_keys()
    {
        var env = await EnvOf(Add().WithAnonymousAdmin().Resource);

        Assert.Equal("true", env["SIGNOZ_IDENTN_IMPERSONATION_ENABLED"]);
        Assert.Equal("false", env["SIGNOZ_IDENTN_TOKENIZER_ENABLED"]);
        Assert.Equal("false", env["SIGNOZ_IDENTN_APIKEY_ENABLED"]);
    }

    [Fact]
    public async Task WithOidcLogin_puts_a_proxy_in_front_and_hides_SigNoz()
    {
        var signoz = Add().WithOidcLogin(SigNozOidcOptions.Keycloak("http://localhost:8181/realms/demo", "signoz",
            ReferenceExpression.Create($"secret"), backchannelRealmUrl: "http://keycloak:8080/realms/demo"));

        var proxy = Assert.IsAssignableFrom<ContainerResource>(signoz.Resource.SignIn);
        Assert.Equal("signoz-signin", proxy.Name);
        Assert.Contains(Endpoints(proxy), e => e.Name == "http" && e.TargetPort == 4180 && e.IsExternal);
        Assert.DoesNotContain(Endpoints(signoz.Resource), e => e.IsExternal);

        var env = await EnvOf(proxy);
        Assert.Equal("keycloak-oidc", env["OAUTH2_PROXY_PROVIDER"]);
        Assert.Equal("http://localhost:8181/realms/demo", env["OAUTH2_PROXY_OIDC_ISSUER_URL"]);
        Assert.Equal("http://localhost:8181/realms/demo/protocol/openid-connect/auth", env["OAUTH2_PROXY_LOGIN_URL"]);
        Assert.Equal("http://keycloak:8080/realms/demo/protocol/openid-connect/token", env["OAUTH2_PROXY_REDEEM_URL"]);
        Assert.Equal("http://keycloak:8080/realms/demo/protocol/openid-connect/certs", env["OAUTH2_PROXY_OIDC_JWKS_URL"]);
        Assert.Equal("true", env["OAUTH2_PROXY_SKIP_OIDC_DISCOVERY"]);
        Assert.Equal("signoz:admin", env["OAUTH2_PROXY_ALLOWED_ROLES"]);
        Assert.Contains("signoz.bindings.http", env["OAUTH2_PROXY_UPSTREAMS"]);
        Assert.Equal("false", env["OAUTH2_PROXY_PASS_HOST_HEADER"]);

        var signozEnv = await EnvOf(signoz.Resource);
        Assert.Equal("true", signozEnv["SIGNOZ_IDENTN_IMPERSONATION_ENABLED"]);
    }

    [Fact]
    public async Task WithOidcLogin_with_the_issuer_alone_uses_discovery()
    {
        var signoz = Add().WithOidcLogin(new SigNozOidcOptions
        {
            IssuerUrl = "https://login.example.com", ClientId = "signoz", ClientSecret = ReferenceExpression.Create($"s"),
            AllowedGroups = ["ops"],
        });

        var env = await EnvOf(signoz.Resource.SignIn!);
        Assert.Equal("oidc", env["OAUTH2_PROXY_PROVIDER"]);
        Assert.False(env.ContainsKey("OAUTH2_PROXY_SKIP_OIDC_DISCOVERY"));
        Assert.Equal("ops", env["OAUTH2_PROXY_ALLOWED_GROUPS"]);
        Assert.Equal("groups", env["OAUTH2_PROXY_OIDC_GROUPS_CLAIM"]);
    }

    [Fact]
    public void WithOidcLogin_refuses_half_a_set_of_endpoints_and_bad_realms()
    {
        Assert.Throws<ArgumentException>(() => Add().WithOidcLogin(new SigNozOidcOptions
        {
            IssuerUrl = "https://login.example.com", ClientId = "signoz", ClientSecret = ReferenceExpression.Create($"s"),
            LoginUrl = "https://login.example.com/authorize",
        }));
        Assert.Throws<ArgumentException>(() => SigNozOidcOptions.Keycloak("https://sso.example.com", "signoz", ReferenceExpression.Create($"s")));
    }

    [Fact]
    public async Task WithSigNozTelemetry_points_the_exporter_at_the_collector()
    {
        var signoz = Add();
        var service = signoz.ApplicationBuilder.AddContainer("svc", "nginx").WithSigNozTelemetry(signoz, "SIGNOZ_OTLP_ENDPOINT", "SIGNOZ_OTLP_PROTOCOL");

        var env = await EnvOf(service.Resource);
        Assert.Contains("signoz-collector.bindings.otlp-grpc", env["SIGNOZ_OTLP_ENDPOINT"]);
        Assert.Equal("grpc", env["SIGNOZ_OTLP_PROTOCOL"]);
    }

    [Fact]
    public void Published_ports_are_none_that_Container_Apps_reserves()
    {
        // the ports Azure Container Apps refuses as a target port (its error lists them; 19000 was the first choice and was refused)
        int[] reserved = [53, 1601, 1602, 8578, 12356, 19000, 23040, 23042, 23044, 23045, 23143, 23148, 53827, 53828];

        Assert.DoesNotContain(SigNozStack.PublishedClickHousePort, reserved);
        Assert.DoesNotContain(SigNozStack.PublishedOtlpHttpPort, reserved);
        Assert.DoesNotContain(SigNozStack.PublishedOpAmpPort, reserved);
    }

    [Fact]
    public async Task Publishing_uses_own_TCP_ports_OTLP_over_HTTP_and_no_OpAmp()
    {
        var builder = PublishBuilder();
        var signoz = Add(builder);
        var service = builder.AddContainer("svc", "nginx").WithSigNozTelemetry(signoz);

        Assert.Contains(Endpoints(signoz.Resource.ClickHouse), e => e.Name == "native" && e.Port == 29000 && e.TargetPort == 29000 && e.UriScheme == "tcp");
        Assert.DoesNotContain(Endpoints(signoz.Resource.ClickHouse), e => e.Name == "http");
        Assert.DoesNotContain(Endpoints(signoz.Resource.Collector), e => e.Name == "otlp-grpc");
        Assert.Contains(Endpoints(signoz.Resource.Collector), e => e.Name == "otlp-http" && e.Port == 14318 && e.TargetPort == 14318 && e.UriScheme == "tcp");
        Assert.DoesNotContain(Endpoints(signoz.Resource), e => e.Name == "opamp");

        var env = await EnvOf(service.Resource, publishing: true);
        Assert.Equal("http/protobuf", env["OTEL_EXPORTER_OTLP_PROTOCOL"]);
        Assert.StartsWith("http://", env["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    }

    [Fact]
    public void Publishing_bakes_the_configuration_into_images()
    {
        var signoz = Add(PublishBuilder());

        signoz.Resource.Stack.WriteConfigs();

        foreach (var resource in new IResource[] { signoz.Resource, signoz.Resource.ClickHouse, signoz.Resource.Collector })
        {
            Assert.DoesNotContain(resource.Annotations.OfType<ContainerMountAnnotation>(), m => m.Type == ContainerMountType.BindMount);
            var dockerfile = File.ReadAllText(Path.Combine(_root, ".generated", "signoz", "signoz", "publish", resource.Name, "Dockerfile"));
            Assert.StartsWith("FROM ", dockerfile);
            Assert.Contains("COPY files/0/ /etc/", dockerfile);
        }

        Assert.DoesNotContain("--manager-config", string.Join(' ', ArgsOf(signoz.Resource.Collector)));
        Assert.Contains(":14320/", File.ReadAllText(Path.Combine(_root, ".generated", "signoz", "signoz", "collector", "opamp.yaml")));
        // Container Apps exposes a TCP ingress on its target port: the containers listen on the published ports themselves
        var clickHouse = File.ReadAllText(Path.Combine(_root, ".generated", "signoz", "signoz", "clickhouse", "config.yaml"));
        Assert.Contains("tcp_port: 29000", clickHouse);
        Assert.DoesNotContain("port: 9000", clickHouse);
        Assert.Contains("0.0.0.0:14318", File.ReadAllText(Path.Combine(_root, ".generated", "signoz", "signoz", "collector", "config.yaml")));
    }

    private static List<string> ArgsOf(IResource resource)
    {
        var context = new CommandLineArgsCallbackContext([]);
        foreach (var annotation in resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
            annotation.Callback(context).GetAwaiter().GetResult();
        return context.Args.Select(a => a.ToString()!).ToList();
    }

    [Fact]
    public void Running_mounts_the_generated_folders_and_wires_the_dashboard_tree()
    {
        var signoz = Add();
        var stack = signoz.Resource.Stack;

        Assert.Contains(signoz.Resource.Annotations.OfType<ContainerMountAnnotation>(), m => m.Target == "/etc/signoz-provisioning" && m.IsReadOnly);
        Assert.Contains(signoz.Resource.ClickHouse.Annotations.OfType<ContainerMountAnnotation>(), m => m.Target == "/etc/clickhouse-server/signoz");
        Assert.Contains("--manager-config", string.Join(' ', ArgsOf(signoz.Resource.Collector)));
        Assert.Contains(Endpoints(signoz.Resource), e => e.Name == "opamp");
        Assert.NotNull(stack);
    }

    [Fact]
    public void WriteConfigs_writes_LF_scripts_the_built_in_content_and_the_app_hosts_files()
    {
        var mine = Directory.CreateDirectory(Path.Combine(_root, "mine")).FullName;
        File.WriteAllText(Path.Combine(mine, "orders.json"), "{\"name\":\"orders\"}");
        File.WriteAllText(Path.Combine(mine, "aspire-http-services.json"), "{\"name\":\"aspire-http-services\",\"mine\":true}");
        var signoz = Add().WithDashboards(mine);

        signoz.Resource.Stack.WriteConfigs();

        var dir = Path.Combine(_root, ".generated", "signoz", "signoz");
        foreach (var script in new[] { "signoz/start.sh", "signoz/provision.sh", "clickhouse/start.sh" })
            Assert.DoesNotContain("\r", File.ReadAllText(Path.Combine(dir, script)));
        Assert.Equal("server_endpoint: ws://signoz:4320/v1/opamp\n", File.ReadAllText(Path.Combine(dir, "collector", "opamp.yaml")));
        Assert.Contains("tcp_port: 9000", File.ReadAllText(Path.Combine(dir, "clickhouse", "config.yaml")));
        Assert.Contains("0.0.0.0:4318", File.ReadAllText(Path.Combine(dir, "collector", "config.yaml")));
        Assert.True(File.Exists(Path.Combine(dir, "signoz", "dashboards", "orders.json")));
        Assert.Contains("\"mine\":true", File.ReadAllText(Path.Combine(dir, "signoz", "dashboards", "aspire-http-services.json")));
        Assert.True(File.Exists(Path.Combine(dir, "signoz", "dashboards", "aspire-dotnet-runtime.json")));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(dir, "signoz", "alerts")));
    }

    [Fact]
    public void WithoutBuiltInContent_leaves_only_the_app_hosts_files()
    {
        var signoz = Add().WithoutBuiltInContent();

        signoz.Resource.Stack.WriteConfigs();

        var dir = Path.Combine(_root, ".generated", "signoz", "signoz", "signoz");
        Assert.Empty(Directory.GetFiles(Path.Combine(dir, "dashboards")));
        Assert.Empty(Directory.GetFiles(Path.Combine(dir, "alerts")));
    }

    public static TheoryData<string> Dashboards() =>
        [.. SigNozStack.EmbeddedContent().Where(c => c.Path.StartsWith("dashboards/", StringComparison.Ordinal)).Select(c => c.Path)];

    [Theory]
    [MemberData(nameof(Dashboards))]
    public void Every_built_in_dashboard_is_schema_v6_and_places_every_panel(string path)
    {
        using var json = JsonDocument.Parse(SigNozStack.Content(path));
        var root = json.RootElement;
        Assert.Equal("v6", root.GetProperty("schemaVersion").GetString());
        Assert.Equal(Path.GetFileNameWithoutExtension(path), root.GetProperty("name").GetString());

        var panels = root.GetProperty("spec").GetProperty("panels").EnumerateObject().Select(p => p.Name).ToHashSet();
        var placed = root.GetProperty("spec").GetProperty("layouts").EnumerateArray()
            .SelectMany(l => l.GetProperty("spec").GetProperty("items").EnumerateArray())
            .Select(i => i.GetProperty("content").GetProperty("$ref").GetString()!["#/spec/panels/".Length..]).ToList();
        Assert.Equal(panels.Order(), placed.Order());
        Assert.All(root.GetProperty("spec").GetProperty("layouts").EnumerateArray()
            .SelectMany(l => l.GetProperty("spec").GetProperty("items").EnumerateArray()), i => Assert.True(i.GetProperty("x").GetInt32() + i.GetProperty("width").GetInt32() <= 12));
    }

    public static TheoryData<string> AlertRules() =>
        [.. SigNozStack.EmbeddedContent().Where(c => c.Path.StartsWith("alerts/", StringComparison.Ordinal)).Select(c => c.Path)];

    [Theory]
    [MemberData(nameof(AlertRules))]
    public void Every_built_in_alert_rule_is_routed_by_policy_and_named_where_provisioning_finds_it(string path)
    {
        var content = SigNozStack.Content(path);
        using var json = JsonDocument.Parse(content);
        Assert.True(json.RootElement.GetProperty("notificationSettings").GetProperty("usePolicy").GetBoolean());
        // provision.sh reads the name with sed: a line of its own starting with "alert"
        var name = json.RootElement.GetProperty("alert").GetString();
        Assert.Contains(content.Split('\n'), l => l.TrimStart().StartsWith($"\"alert\": \"{name}\"", StringComparison.Ordinal));
    }
}
