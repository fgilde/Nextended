using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Observability;

namespace Nextended.Aspire.Hosting.Grafana;

/// <summary>
/// Fluent API for a composable Grafana observability stack. Start with
/// <see cref="AddGrafana"/>, then attach components — every component call also
/// provisions the matching Grafana datasource, wires start ordering and groups
/// the container under Grafana in the Aspire dashboard. Call order does not
/// matter: cross-component configs are generated at application start.
/// </summary>
public static class GrafanaBuilderExtensions
{
    /// <summary>
    /// Adds a Grafana container with auto-provisioned datasources. Generated
    /// configs land under <paramref name="configRootPath"/><c>/.generated</c>
    /// (default: <c>{AppHostDirectory}/observability</c>).
    /// </summary>
    public static IResourceBuilder<GrafanaResource> AddGrafana(
        this IDistributedApplicationBuilder builder,
        string name = "grafana",
        string? configRootPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        configRootPath = Path.GetFullPath(configRootPath ?? Path.Combine(builder.AppHostDirectory, "observability"));

        var ctx = new ObservabilityStackContext
        {
            ConfigRootPath = configRootPath,
            IsPublishMode = builder.ExecutionContext.IsPublishMode,
        };
        return StackComponents.AddGrafana(builder, ctx, name);
    }

    // ---- Auth -------------------------------------------------------------------

    /// <summary>
    /// Starts Grafana with anonymous Admin access — no login form. Convenient for
    /// local dev; don't use for shared/deployed setups.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithAnonymousAdmin(this IResourceBuilder<GrafanaResource> grafana) =>
        grafana
            .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
            .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
            .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true");

    /// <summary>Configures the Grafana admin login. Counterpart of <see cref="WithAnonymousAdmin"/>.</summary>
    public static IResourceBuilder<GrafanaResource> WithAdminUser(
        this IResourceBuilder<GrafanaResource> grafana, string userName, string password) =>
        grafana
            .WithEnvironment("GF_SECURITY_ADMIN_USER", userName)
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", password);

    /// <summary>Configures the Grafana admin login with the password coming from an Aspire parameter (secret-friendly).</summary>
    public static IResourceBuilder<GrafanaResource> WithAdminUser(
        this IResourceBuilder<GrafanaResource> grafana, string userName, IResourceBuilder<ParameterResource> password) =>
        grafana
            .WithEnvironment("GF_SECURITY_ADMIN_USER", userName)
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", password);

    // ---- Dashboards / datasources -------------------------------------------------

    /// <summary>
    /// Bind-mounts a host folder of dashboard JSON files (one file = one dashboard,
    /// auto-loaded by Grafana's provisioning). <paramref name="folderName"/> is the
    /// sidebar folder the dashboards appear under.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithDashboards(
        this IResourceBuilder<GrafanaResource> grafana, string hostPath, string? folderName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        var ctx = grafana.Resource.Context;
        ctx.DashboardsMountPath = Path.GetFullPath(hostPath);
        if (folderName is not null) ctx.DashboardsFolderName = folderName;

        return ctx.MountConfig(grafana, ctx.DashboardsMountPath, "/var/lib/grafana/dashboards");
    }

    /// <summary>
    /// Adds any Grafana datasource to the generated provisioning — escape hatch for
    /// everything the typed methods don't cover (MySQL, Elasticsearch, InfluxDB, …).
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithDatasource(
        this IResourceBuilder<GrafanaResource> grafana, GrafanaDatasource datasource)
    {
        ArgumentNullException.ThrowIfNull(datasource);
        grafana.Resource.Context.Datasources.Add(datasource);
        return grafana;
    }

    /// <summary>
    /// Provisions an Aspire Postgres resource as a Grafana SQL datasource. The
    /// password flows through an env var on the Grafana container (never written
    /// to the generated YAML).
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithPostgresDatasource(
        this IResourceBuilder<GrafanaResource> grafana,
        IResourceBuilder<PostgresServerResource> postgres,
        string? name = null,
        string database = "postgres")
    {
        ArgumentNullException.ThrowIfNull(postgres);
        name ??= "Postgres";
        var envPrefix = ToEnvPrefix(name);

        grafana.WithEnvironment($"{envPrefix}_PASSWORD", ReferenceExpression.Create($"{postgres.Resource.PasswordParameter}"));
        var user = postgres.Resource.UserNameParameter is { } userParam
            ? ReferenceExpression.Create($"{userParam}")
            : ReferenceExpression.Create($"postgres");
        grafana.WithEnvironment($"{envPrefix}_USER", user);

        return grafana.WithDatasource(PostgresDatasource(
            name, $"{postgres.Resource.Name}:5432", database,
            user: $"${{{envPrefix}_USER}}", passwordRef: $"${{{envPrefix}_PASSWORD}}", sslMode: "disable"));
    }

    /// <summary>
    /// Provisions an external Postgres (host/credentials) as a Grafana SQL datasource —
    /// for databases that are not Aspire resources.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithPostgresDatasource(
        this IResourceBuilder<GrafanaResource> grafana,
        string host,
        string password,
        int port = 5432,
        string database = "postgres",
        string userName = "postgres",
        string sslMode = "disable",
        string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        name ??= "Postgres";
        var envPrefix = ToEnvPrefix(name);

        grafana.WithEnvironment($"{envPrefix}_PASSWORD", password);

        return grafana.WithDatasource(PostgresDatasource(
            name, $"{host}:{port}", database,
            user: userName, passwordRef: $"${{{envPrefix}_PASSWORD}}", sslMode: sslMode));
    }

    internal static GrafanaDatasource PostgresDatasource(
        string name, string url, string database, string user, string passwordRef, string sslMode)
    {
        var ds = new GrafanaDatasource
        {
            Name = name,
            Type = "postgres",
            Url = url,
            User = user,
        };
        ds.JsonData["database"] = database;
        ds.JsonData["sslmode"] = sslMode;
        ds.JsonData["postgresVersion"] = 1500;
        ds.JsonData["timescaledb"] = false;
        ds.SecureJsonData["password"] = passwordRef;
        return ds;
    }

    // ---- Components ---------------------------------------------------------------

    /// <summary>
    /// Adds Prometheus (metrics scraping + TSDB) and provisions it as Grafana's
    /// default datasource. Use <paramref name="configure"/> for retention, scrape
    /// targets, volumes or image overrides.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithPrometheus(
        this IResourceBuilder<GrafanaResource> grafana,
        string name = "prometheus",
        Action<IResourceBuilder<PrometheusResource>>? configure = null)
    {
        var ctx = grafana.Resource.Context;
        ctx.Prometheus ??= StackComponents.AddPrometheus(grafana.ApplicationBuilder, ctx, name);
        configure?.Invoke(ctx.Prometheus);
        return grafana;
    }

    /// <summary>Adds Loki (log aggregation) and provisions it as a Grafana datasource.</summary>
    public static IResourceBuilder<GrafanaResource> WithLoki(
        this IResourceBuilder<GrafanaResource> grafana,
        string name = "loki",
        Action<IResourceBuilder<LokiResource>>? configure = null)
    {
        var ctx = grafana.Resource.Context;
        ctx.Loki ??= StackComponents.AddLoki(grafana.ApplicationBuilder, ctx, name);
        configure?.Invoke(ctx.Loki);
        return grafana;
    }

    /// <summary>
    /// Adds Promtail, shipping all Docker container logs to this Loki instance.
    /// Local-only (needs the Docker socket) — automatically skipped in publish mode.
    /// </summary>
    public static IResourceBuilder<LokiResource> WithPromtail(
        this IResourceBuilder<LokiResource> loki, string name = "promtail")
    {
        var builder = loki.ApplicationBuilder;
        if (builder.ExecutionContext.IsPublishMode) return loki;

        var ctx = loki.Resource.Context;
        ctx.Promtail ??= StackComponents.AddPromtail(builder, ctx, name);
        return loki;
    }

    /// <summary>Adds Tempo (traces backend) and provisions it as a Grafana datasource with trace-to-logs/service-map links.</summary>
    public static IResourceBuilder<GrafanaResource> WithTempo(
        this IResourceBuilder<GrafanaResource> grafana,
        string name = "tempo",
        Action<IResourceBuilder<TempoResource>>? configure = null)
    {
        var ctx = grafana.Resource.Context;
        ctx.Tempo ??= StackComponents.AddTempo(grafana.ApplicationBuilder, ctx, name);
        configure?.Invoke(ctx.Tempo);
        return grafana;
    }

    /// <summary>
    /// Adds an OpenTelemetry Collector (OTLP receiver) that fans out to whatever
    /// backends the stack ends up with — traces to Tempo, logs to Loki, metrics to
    /// Prometheus — plus an optional mirror to the Aspire dashboard.
    /// </summary>
    /// <param name="grafana">The Grafana stack builder.</param>
    /// <param name="name">Resource name of the collector.</param>
    /// <param name="aspireDashboardOtlpEndpoint">
    /// Aspire-dashboard OTLP endpoint the collector mirrors traces to. Defaults to
    /// <c>host.docker.internal:18889</c>; set to <c>""</c> to disable the mirror.
    /// </param>
    /// <param name="configure">Optional extra configuration for the collector container.</param>
    public static IResourceBuilder<GrafanaResource> WithOtelCollector(
        this IResourceBuilder<GrafanaResource> grafana,
        string name = "otel-collector",
        string? aspireDashboardOtlpEndpoint = null,
        Action<IResourceBuilder<OtelCollectorResource>>? configure = null)
    {
        var ctx = grafana.Resource.Context;
        if (aspireDashboardOtlpEndpoint is not null) ctx.AspireDashboardOtlpEndpoint = aspireDashboardOtlpEndpoint;
        ctx.OtelCollector ??= StackComponents.AddOtelCollector(grafana.ApplicationBuilder, ctx, name);
        configure?.Invoke(ctx.OtelCollector);
        return grafana;
    }

    /// <summary>
    /// Adds cAdvisor for per-container CPU/Memory/Network metrics and registers the
    /// Prometheus scrape job. Local-only (needs the Docker socket) — automatically
    /// skipped in publish mode.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithCAdvisor(
        this IResourceBuilder<GrafanaResource> grafana, string name = "cadvisor")
    {
        var builder = grafana.ApplicationBuilder;
        if (builder.ExecutionContext.IsPublishMode) return grafana;

        var ctx = grafana.Resource.Context;
        ctx.CAdvisor ??= StackComponents.AddCAdvisor(builder, ctx, name);
        return grafana;
    }

    /// <summary>
    /// Adds a postgres_exporter for an Aspire Postgres resource and registers the
    /// Prometheus scrape job. Credentials flow through Aspire references — nothing
    /// is written to disk.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithPostgresExporter(
        this IResourceBuilder<GrafanaResource> grafana,
        IResourceBuilder<PostgresServerResource> postgres,
        string name = "postgres-exporter",
        string database = "postgres",
        string sslMode = "disable")
    {
        ArgumentNullException.ThrowIfNull(postgres);
        var ctx = grafana.Resource.Context;
        if (ctx.PostgresExporter is not null) return grafana;

        var dsn = new ReferenceExpressionBuilder();
        dsn.AppendLiteral("postgresql://");
        if (postgres.Resource.UserNameParameter is { } user) dsn.Append($"{user}");
        else dsn.AppendLiteral("postgres");
        dsn.Append($":{postgres.Resource.PasswordParameter}");
        dsn.AppendLiteral($"@{postgres.Resource.Name}:5432/{database}?sslmode={sslMode}");

        StackComponents.AddPostgresExporter(grafana.ApplicationBuilder, ctx, name, dsn.Build());
        return grafana;
    }

    /// <summary>Adds a postgres_exporter for an external Postgres (host/credentials).</summary>
    public static IResourceBuilder<GrafanaResource> WithPostgresExporter(
        this IResourceBuilder<GrafanaResource> grafana,
        string host,
        string password,
        int port = 5432,
        string database = "postgres",
        string userName = "postgres",
        string sslMode = "disable",
        string name = "postgres-exporter")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var ctx = grafana.Resource.Context;
        if (ctx.PostgresExporter is not null) return grafana;

        var dsn = $"postgresql://{userName}:{password}@{host}:{port}/{database}?sslmode={sslMode}";
        StackComponents.AddPostgresExporter(grafana.ApplicationBuilder, ctx, name, dsn);
        return grafana;
    }

    // ---- Component tuning -----------------------------------------------------------

    /// <summary>Scrapes an additional metrics endpoint, e.g. one of your own services: <c>WithScrapeTarget("api", "my-api:8080")</c>.</summary>
    public static IResourceBuilder<PrometheusResource> WithScrapeTarget(
        this IResourceBuilder<PrometheusResource> prometheus, string jobName, string target, string? metricsPath = null)
    {
        prometheus.Resource.Context.ScrapeJobs.Add(new PrometheusScrapeJob(jobName, target, metricsPath));
        return prometheus;
    }

    /// <summary>Prometheus TSDB retention (<c>--storage.tsdb.retention.time</c>), e.g. <c>"30d"</c>. Default 15d.</summary>
    public static IResourceBuilder<PrometheusResource> WithRetention(
        this IResourceBuilder<PrometheusResource> prometheus, string retention)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retention);
        prometheus.Resource.Context.PrometheusRetention = retention;
        return prometheus;
    }

    // ---- Deployed stacks: object storage, database, sign-in ---------------------------

    /// <summary>
    /// Keeps Loki's chunks and index in an S3 bucket (e.g. MinIO) — survives container restarts
    /// where no persistent disk exists. The bucket has to exist.
    /// </summary>
    public static IResourceBuilder<LokiResource> WithS3Storage(
        this IResourceBuilder<LokiResource> loki, S3StorageOptions storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        loki.Resource.Context.LokiStorage = storage;
        return loki
            .WithEnvironment(StackConfigGenerator.LokiS3EndpointEnv, storage.Endpoint)
            .WithEnvironment(StackConfigGenerator.LokiS3AccessKeyEnv, storage.AccessKey)
            .WithEnvironment(StackConfigGenerator.LokiS3SecretKeyEnv, storage.SecretKey)
            .WithArgs("-config.expand-env=true");
    }

    /// <summary>Keeps Tempo's trace blocks in an S3 bucket (e.g. MinIO). The bucket has to exist.</summary>
    public static IResourceBuilder<TempoResource> WithS3Storage(
        this IResourceBuilder<TempoResource> tempo, S3StorageOptions storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        tempo.Resource.Context.TempoStorage = storage;
        return tempo
            .WithEnvironment(StackConfigGenerator.TempoS3EndpointEnv, storage.Endpoint)
            .WithEnvironment(StackConfigGenerator.TempoS3AccessKeyEnv, storage.AccessKey)
            .WithEnvironment(StackConfigGenerator.TempoS3SecretKeyEnv, storage.SecretKey)
            .WithArgs("-config.expand-env=true");
    }

    /// <summary>Keeps Grafana's own state (users, preferences, UI-made dashboards) in Postgres instead of SQLite.</summary>
    public static IResourceBuilder<GrafanaResource> WithDatabase(
        this IResourceBuilder<GrafanaResource> grafana, GrafanaDatabaseOptions database)
    {
        ArgumentNullException.ThrowIfNull(database);
        return grafana
            .WithEnvironment("GF_DATABASE_TYPE", "postgres")
            .WithEnvironment("GF_DATABASE_HOST", database.HostAndPort)
            .WithEnvironment("GF_DATABASE_NAME", database.Name)
            .WithEnvironment("GF_DATABASE_USER", database.User)
            .WithEnvironment("GF_DATABASE_PASSWORD", database.Password)
            .WithEnvironment("GF_DATABASE_SSL_MODE", database.SslMode);
    }

    /// <summary>
    /// Microsoft Entra ID sign-in. Turns off the login form, basic auth, anonymous access and the
    /// initial local admin, so the only way in is a tenant account holding one of the app roles
    /// GrafanaAdmin, Admin, Editor or Viewer. The root URL comes from Grafana's own endpoint,
    /// which is what the OAuth redirect needs.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithEntraIdLogin(
        this IResourceBuilder<GrafanaResource> grafana, GrafanaEntraIdOptions entra)
    {
        ArgumentNullException.ThrowIfNull(entra);
        ArgumentException.ThrowIfNullOrWhiteSpace(entra.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entra.ClientId);

        var authority = $"https://login.microsoftonline.com/{entra.TenantId}/oauth2/v2.0";
        grafana
            .WithEnvironment("GF_AUTH_AZUREAD_ENABLED", "true")
            .WithEnvironment("GF_AUTH_AZUREAD_NAME", "Microsoft Entra ID")
            .WithEnvironment("GF_AUTH_AZUREAD_CLIENT_ID", entra.ClientId)
            .WithEnvironment("GF_AUTH_AZUREAD_CLIENT_SECRET", entra.ClientSecret)
            .WithEnvironment("GF_AUTH_AZUREAD_SCOPES", "openid email profile")
            .WithEnvironment("GF_AUTH_AZUREAD_AUTH_URL", $"{authority}/authorize")
            .WithEnvironment("GF_AUTH_AZUREAD_TOKEN_URL", $"{authority}/token")
            .WithEnvironment("GF_AUTH_AZUREAD_ALLOWED_ORGANIZATIONS", entra.TenantId)
            .WithEnvironment("GF_AUTH_AZUREAD_ALLOW_SIGN_UP", "true")
            .WithEnvironment("GF_AUTH_AZUREAD_USE_PKCE", "true")
            // Without one of the app roles a user is turned away instead of becoming a Viewer.
            .WithEnvironment("GF_AUTH_AZUREAD_ROLE_ATTRIBUTE_STRICT", "true")
            // The app role GrafanaAdmin makes a server admin; there is no other one.
            .WithEnvironment("GF_AUTH_AZUREAD_ALLOW_ASSIGN_GRAFANA_ADMIN", "true");

        if (entra.AllowedGroups.Count > 0)
            grafana.WithEnvironment("GF_AUTH_AZUREAD_ALLOWED_GROUPS", string.Join(' ', entra.AllowedGroups));

        return grafana.WithSignInOnly();
    }

    /// <summary>
    /// Sign-in through an OpenID Connect provider — Keycloak, Authentik, Auth0, Okta … — via
    /// Grafana's generic OAuth, locked down like <see cref="WithEntraIdLogin"/>: the only way in is an
    /// account the provider vouches for and <see cref="GrafanaOAuthOptions.RoleAttributePath"/>
    /// gives a role. Register <c>{grafana-url}/login/generic_oauth</c> as redirect URI.
    /// </summary>
    public static IResourceBuilder<GrafanaResource> WithOAuthLogin(
        this IResourceBuilder<GrafanaResource> grafana, GrafanaOAuthOptions oauth)
    {
        ArgumentNullException.ThrowIfNull(oauth);
        ArgumentException.ThrowIfNullOrWhiteSpace(oauth.ClientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(oauth.RoleAttributePath);
        foreach (var (name, url) in new[] { ("AuthUrl", oauth.AuthUrl), ("TokenUrl", oauth.TokenUrl), ("ApiUrl", oauth.ApiUrl) })
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException($"{name} is an absolute http(s) URL of the provider — {url}", nameof(oauth));

        grafana
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_ENABLED", "true")
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_NAME", oauth.Name)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_CLIENT_ID", oauth.ClientId)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_CLIENT_SECRET", oauth.ClientSecret)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_SCOPES", oauth.Scopes)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_AUTH_URL", oauth.AuthUrl)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_TOKEN_URL", oauth.TokenUrl)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_API_URL", oauth.ApiUrl)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_ALLOW_SIGN_UP", "true")
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_USE_PKCE", "true")
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_ROLE_ATTRIBUTE_PATH", oauth.RoleAttributePath)
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_ROLE_ATTRIBUTE_STRICT", "true")
            .WithEnvironment("GF_AUTH_GENERIC_OAUTH_ALLOW_ASSIGN_GRAFANA_ADMIN", "true");

        return grafana.WithSignInOnly();
    }

    /// <summary>
    /// What both sign-ins share: no login form, basic auth, anonymous access or local admin
    /// (Grafana would create admin/admin on its first start), and the root URL the browser comes
    /// back to after signing in — the OAuth redirect URI is built from it.
    /// </summary>
    private static IResourceBuilder<GrafanaResource> WithSignInOnly(this IResourceBuilder<GrafanaResource> grafana)
    {
        var endpoint = grafana.GetEndpoint(GrafanaResource.HttpEndpointName);
        // Deployed, the reference is Grafana's public URL. Locally it would resolve to the
        // container-network name (grafana.dev.internal:3000), which no browser reaches; the
        // allocated endpoint is the address on the host.
        if (grafana.ApplicationBuilder.ExecutionContext.IsPublishMode)
            grafana.WithEnvironment("GF_SERVER_ROOT_URL", endpoint);
        else
            grafana.WithEnvironment(context => context.EnvironmentVariables["GF_SERVER_ROOT_URL"] = endpoint.Url);

        return grafana
            .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
            .WithEnvironment("GF_AUTH_BASIC_ENABLED", "false")
            .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "false")
            .WithEnvironment("GF_SECURITY_DISABLE_INITIAL_ADMIN_CREATION", "true");
    }

    // ---- Persistence ------------------------------------------------------------------
    // Named volumes so dashboards/metrics/logs/traces survive container recreation.

    /// <summary>Persists Grafana state (users, UI-created dashboards, plugins) in a named volume.</summary>
    public static IResourceBuilder<GrafanaResource> WithDataVolume(
        this IResourceBuilder<GrafanaResource> grafana, string? name = null) =>
        grafana.WithVolume(name ?? $"{grafana.Resource.Name}-data", "/var/lib/grafana");

    /// <summary>Persists Prometheus metrics in a named volume.</summary>
    public static IResourceBuilder<PrometheusResource> WithDataVolume(
        this IResourceBuilder<PrometheusResource> prometheus, string? name = null) =>
        prometheus.WithVolume(name ?? $"{prometheus.Resource.Name}-data", "/prometheus");

    /// <summary>Persists Loki log chunks in a named volume.</summary>
    public static IResourceBuilder<LokiResource> WithDataVolume(
        this IResourceBuilder<LokiResource> loki, string? name = null) =>
        loki.WithVolume(name ?? $"{loki.Resource.Name}-data", "/loki");

    /// <summary>Persists Tempo trace blocks in a named volume.</summary>
    public static IResourceBuilder<TempoResource> WithDataVolume(
        this IResourceBuilder<TempoResource> tempo, string? name = null) =>
        tempo.WithVolume(name ?? $"{tempo.Resource.Name}-data", "/tmp/tempo");

    private static string ToEnvPrefix(string datasourceName) =>
        "GF_DS_" + new string([.. datasourceName.ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_')]);
}
