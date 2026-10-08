using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Grafana;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// Supabase-aware entry point for the observability stack. The stack itself
/// (Grafana, Prometheus, Loki, Promtail, cAdvisor, Tempo, OTel-Collector,
/// postgres_exporter) lives in the <c>Nextended.Aspire.Hosting.Grafana</c>
/// package — see <see cref="ObservabilityStackExtensions"/> for the generic
/// options-based overload and
/// <see cref="Nextended.Aspire.Hosting.Grafana.GrafanaBuilderExtensions"/> for
/// the fluent piecemeal API.
///
/// Deployed (publish mode) the stack keeps its state in what the Supabase stack already runs:
/// Grafana in a <c>grafana</c> database of the Supabase Postgres, Loki and Tempo in MinIO buckets
/// when MinIO (<see cref="MinioOnNfsStorageExtensions"/>) is in use, and Grafana's SQL datasource reads through a login without write rights.
/// </summary>
public static class ObservabilityStack
{
    /// <summary>Login of Grafana's SQL datasource in a deployed stack: reads everything (RLS included), writes nothing.</summary>
    public const string ReaderRole = "grafana_reader";

    /// <summary>Owner of Grafana's own database in a deployed stack.</summary>
    public const string GrafanaRole = "grafana";

    public static IResourceBuilder<SupabaseStackResource> WithObservability(
        this IResourceBuilder<SupabaseStackResource> supabase,
        Action<ObservabilityStackOptions>? configure = null)
    {
        supabase.ApplicationBuilder.AddObservabilityStack(supabase, configure);
        return supabase;
    }

    /// <summary>
    /// Convenience overload — derives Postgres connection details from the
    /// supplied Supabase stack and lets the caller tweak everything else.
    /// </summary>
    public static IDistributedApplicationBuilder AddObservabilityStack(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SupabaseStackResource> supabase,
        Action<ObservabilityStackOptions>? configure = null)
    {
        var stack = supabase.Resource;
        if (stack.Database is null && stack.ExternalDatabasePassword is null)
            throw new InvalidOperationException("Supabase database password not configured");

        var defaultConfigRoot = Path.GetFullPath(
            Path.Combine(builder.AppHostDirectory, "..", "observability"));

        // Use the actual Database container's resource name — keeps things
        // in lock-step if the Supabase library ever changes the suffix.
        var dbHost = stack.Database?.Resource.Name ?? $"{stack.Name}-db";

        var options = new ObservabilityStackOptions
        {
            ConfigRootPath = defaultConfigRoot,
            PostgresExporter = new PostgresExporterOptions
            {
                Host = dbHost,
                // A parameter stays a reference, deployed a secret.
                PasswordExpression = stack.DatabasePassword,
            },
        };

        var deployed = builder.ExecutionContext.IsPublishMode;
        if (deployed) ApplyDeployedDefaults(builder, stack, options, dbHost);

        configure?.Invoke(options);
        builder.AddObservabilityStack(options);

        if (deployed) PinDeployedComponents(builder);
        return builder;
    }

    /// <summary>psql variable with the password of <see cref="GrafanaRole"/> in the deployed post-init SQL.</summary>
    public const string GrafanaPasswordVariable = "grafana_db_password";

    /// <summary>psql variable with the password of <see cref="ReaderRole"/> in the deployed post-init SQL.</summary>
    public const string ReaderPasswordVariable = "grafana_reader_password";

    /// <summary>Defaults for a deployed stack, set before <c>configure</c> so the caller can still override them.</summary>
    private static void ApplyDeployedDefaults(
        IDistributedApplicationBuilder builder, SupabaseStackResource stack, ObservabilityStackOptions options, string dbHost)
    {
        // Container filesystem only, and ACA gives a replica little of it.
        options.PrometheusRetention = "3d";

        // Grafana's state and its datasource login live in the Supabase Postgres, each login with a
        // password of its own: generated parameters (azd creates them once per environment), which
        // the post-init SQL receives at runtime and Grafana as secrets.
        var grafanaPassword = GeneratedPassword(builder, $"{stack.Name}-grafana-db-password");
        var readerPassword = GeneratedPassword(builder, $"{stack.Name}-grafana-reader-password");
        stack.PostInitSqlVariables[GrafanaPasswordVariable] = grafanaPassword;
        stack.PostInitSqlVariables[ReaderPasswordVariable] = readerPassword;
        stack.AdditionalPostInitSql.Add(GrafanaDatabaseSql);

        options.GrafanaDatabase = new GrafanaDatabaseOptions
        {
            HostAndPort = ReferenceExpression.Create($"{dbHost}:5432"),
            Name = GrafanaRole,
            User = GrafanaRole,
            Password = ReferenceExpression.Create($"{grafanaPassword}"),
        };
        options.PostgresExporter!.GrafanaUsername = ReaderRole;
        options.PostgresExporter.GrafanaPasswordExpression = ReferenceExpression.Create($"{readerPassword}");

        if (SupabaseBuilderExtensions.StorageS3Backend is not { HostAndPort: { } s3HostAndPort } s3) return;

        // Behind Azure Container Apps' ingress MinIO speaks HTTPS on 443; a container
        // environment (compose) talks to it directly over HTTP.
        var insecure = SupabaseBuilderExtensions.PublishTarget != SupabasePublishTarget.AzureContainerApps;
        S3StorageOptions Bucket(string name)
        {
            if (!s3.AdditionalBuckets.Contains(name)) s3.AdditionalBuckets.Add(name);
            return new S3StorageOptions
            {
                Endpoint = s3HostAndPort,
                Bucket = name,
                AccessKey = ReferenceExpression.Create($"{s3.AccessKey}"),
                SecretKey = s3.SecretKeyExpression,
                Region = s3.Region,
                Insecure = insecure,
            };
        }

        options.LokiStorage = Bucket("loki");
        options.TempoStorage = Bucket("tempo");
    }

    /// <summary>Letters and digits only, so it needs no quoting in SQL, YAML or env.</summary>
    private static ParameterResource GeneratedPassword(IDistributedApplicationBuilder builder, string name) =>
        builder.AddParameter(name, new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true).Resource;

    /// <summary>
    /// Idempotent (the init container re-runs it on every start, and it sets the passwords each
    /// time). <c>BYPASSRLS</c> because nearly every app table has row level security without a
    /// policy for this login — the datasource would see empty tables; <c>pg_read_all_data</c>
    /// grants reading, nothing grants writing.
    /// </summary>
    internal const string GrafanaDatabaseSql = $$"""
        -- Observability: Grafana's own database and a read-only login for its SQL datasource.
        SELECT 'CREATE ROLE {{GrafanaRole}} LOGIN'
        WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{{GrafanaRole}}')\gexec
        ALTER ROLE {{GrafanaRole}} WITH LOGIN PASSWORD :'{{GrafanaPasswordVariable}}';
        SELECT 'CREATE ROLE {{ReaderRole}} LOGIN BYPASSRLS'
        WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '{{ReaderRole}}')\gexec
        ALTER ROLE {{ReaderRole}} WITH LOGIN BYPASSRLS PASSWORD :'{{ReaderPasswordVariable}}';
        GRANT pg_read_all_data TO {{ReaderRole}};
        SELECT 'CREATE DATABASE {{GrafanaRole}} OWNER {{GrafanaRole}}'
        WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '{{GrafanaRole}}')\gexec
        """;

    /// <summary>
    /// Azure Container Apps settings for the deployed components: the internal ones are TCP
    /// (Docker-network addresses <c>name:port</c> stay valid, see the stack's
    /// <c>InternalScheme</c>) and need the port exposed as is — as the Supabase database and
    /// realtime do. Every component runs as exactly one replica: Loki and Tempo hold their ring
    /// in memory, Prometheus its TSDB on local disk.
    /// </summary>
    private static void PinDeployedComponents(IDistributedApplicationBuilder builder)
    {
        foreach (var resource in builder.Resources.OfType<ContainerResource>().ToList())
        {
            var isInternal = resource is PrometheusResource or LokiResource or TempoResource
                or OtelCollectorResource or PostgresExporterResource;
            if (!isInternal && resource is not GrafanaResource) continue;

            var mainPort = resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault()?.TargetPort;
            builder.CreateResourceBuilder(resource).PublishAsAcaWhenTargeted((_, app) =>
            {
                if (isInternal && mainPort is { } port) app.Configuration.Ingress.ExposedPort = port;
                app.Template.Scale.MinReplicas = 1;
                app.Template.Scale.MaxReplicas = 1;
            });
        }
    }
}
