using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Grafana;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// Options for <c>AddObservabilityStack</c> — the one-call, batteries-included way
/// to get the full stack. For piecemeal composition use the fluent
/// <c>AddGrafana().WithPrometheus()…</c> API from
/// <see cref="Nextended.Aspire.Hosting.Grafana.GrafanaBuilderExtensions"/>;
/// this options class drives exactly those building blocks.
/// </summary>
public sealed class ObservabilityStackOptions
{
    /// <summary>
    /// Working directory where the stack writes its generated YAML configs and
    /// (by default) reads dashboard JSONs from. Required. The stack creates a
    /// <c>.generated/</c> sub-folder here at runtime for the Prometheus / Loki /
    /// Promtail / Tempo / OTel-Collector / Grafana-Provisioning YAMLs.
    /// </summary>
    public required string ConfigRootPath { get; set; }

    /// <summary>
    /// Folder containing the Grafana dashboard JSON files (one file = one
    /// dashboard, auto-loaded by Grafana's provisioning). When <c>null</c>,
    /// defaults to <c>{ConfigRootPath}/grafana/dashboards</c>. Set explicitly
    /// if dashboards live outside the config tree.
    /// </summary>
    public string? DashboardsPath { get; set; }

    /// <summary>
    /// Name prefix used for every Aspire resource the stack creates. Final names look
    /// like <c>{ResourceNamePrefix}-prometheus</c>, <c>{ResourceNamePrefix}-grafana</c>,
    /// etc. Defaults to <c>"monitoring"</c>. Set to <c>""</c> to keep bare names.
    /// </summary>
    public string ResourceNamePrefix { get; set; } = "monitoring";

    /// <summary>
    /// Postgres-Exporter config. When <c>null</c>, no exporter container is added —
    /// useful if the consuming app doesn't use Postgres or already has metrics from
    /// elsewhere.
    /// </summary>
    public PostgresExporterOptions? PostgresExporter { get; set; }

    // ---- Component toggles -----------------------------------------------------
    // Default everything except heavy tracing pieces. Tempo/OTel-Collector are
    // off-by-default because they're only useful if downstream services actually
    // emit OTLP — turning them on without instrumentation just spins idle containers.

    public bool IncludePrometheus { get; set; } = true;
    public bool IncludeGrafana { get; set; } = true;
    public bool IncludeLoki { get; set; } = true;
    public bool IncludePromtail { get; set; } = true;
    public bool IncludeTempo { get; set; } = false;
    public bool IncludeOtelCollector { get; set; } = false;

    /// <summary>cAdvisor — per-container CPU / Memory / Network metrics scraped from the Docker socket.</summary>
    public bool IncludeCAdvisor { get; set; } = true;

    // ---- Image versions --------------------------------------------------------
    // Pinning images here gives consumers a single seam to upgrade without touching
    // the extension code. Defaults are shared with the fluent API.

    public string PrometheusImage { get; set; } = GrafanaStackDefaults.PrometheusImage;
    public string PrometheusImageTag { get; set; } = GrafanaStackDefaults.PrometheusImageTag;

    public string GrafanaImage { get; set; } = GrafanaStackDefaults.GrafanaImage;
    public string GrafanaImageTag { get; set; } = GrafanaStackDefaults.GrafanaImageTag;

    public string LokiImage { get; set; } = GrafanaStackDefaults.LokiImage;
    public string LokiImageTag { get; set; } = GrafanaStackDefaults.LokiImageTag;

    public string PromtailImage { get; set; } = GrafanaStackDefaults.PromtailImage;
    public string PromtailImageTag { get; set; } = GrafanaStackDefaults.PromtailImageTag;

    public string TempoImage { get; set; } = GrafanaStackDefaults.TempoImage;
    public string TempoImageTag { get; set; } = GrafanaStackDefaults.TempoImageTag;

    public string OtelCollectorImage { get; set; } = GrafanaStackDefaults.OtelCollectorImage;
    public string OtelCollectorImageTag { get; set; } = GrafanaStackDefaults.OtelCollectorImageTag;

    public string PostgresExporterImage { get; set; } = GrafanaStackDefaults.PostgresExporterImage;
    public string PostgresExporterImageTag { get; set; } = GrafanaStackDefaults.PostgresExporterImageTag;

    public string CAdvisorImage { get; set; } = GrafanaStackDefaults.CAdvisorImage;
    public string CAdvisorImageTag { get; set; } = GrafanaStackDefaults.CAdvisorImageTag;

    // ---- Grafana auth ----------------------------------------------------------

    /// <summary>
    /// When true (default), Grafana is started with anonymous Admin access — no
    /// login form. Convenient for local dev. Set false for shared/deployed setups
    /// and supply <see cref="GrafanaAdminUser"/>/<see cref="GrafanaAdminPassword"/>.
    /// </summary>
    public bool GrafanaAnonymousAdmin { get; set; } = true;

    public string GrafanaAdminUser { get; set; } = "admin";

    /// <summary>
    /// Admin password used when <see cref="GrafanaAnonymousAdmin"/> is false. Should
    /// come from a secret in production scenarios.
    /// </summary>
    public string? GrafanaAdminPassword { get; set; }

    /// <summary>Grafana retention for Prometheus (<c>--storage.tsdb.retention.time</c>). Default 15 days.</summary>
    public string PrometheusRetention { get; set; } = GrafanaStackDefaults.PrometheusRetention;

    /// <summary>
    /// Grafana folder name under which auto-provisioned dashboards appear in the
    /// sidebar. Default <c>"Application"</c>. Change to e.g. your app's name to
    /// brand the experience without touching dashboard JSON files.
    /// </summary>
    public string GrafanaDashboardsFolder { get; set; } = "Application";

    /// <summary>
    /// Aspire-dashboard OTLP endpoint that the OTel-Collector mirrors traces to.
    /// Default points at <c>host.docker.internal:18889</c> — Aspire 13's standard
    /// local-dev port, so it is dropped in publish mode unless set explicitly. Set to
    /// <c>""</c> to disable the mirror exporter.
    /// </summary>
    public string AspireDashboardOtlpEndpoint { get; set; } = GrafanaStackDefaults.AspireDashboardOtlpEndpoint;

    // ---- Persistence / auth for deployed stacks -----------------------------------

    /// <summary>Keeps Loki's chunks and index in S3 instead of the container filesystem.</summary>
    public S3StorageOptions? LokiStorage { get; set; }

    /// <summary>Keeps Tempo's trace blocks in S3 instead of the container filesystem.</summary>
    public S3StorageOptions? TempoStorage { get; set; }

    /// <summary>Keeps Grafana's own state (users, preferences, UI-made dashboards) in Postgres instead of SQLite.</summary>
    public GrafanaDatabaseOptions? GrafanaDatabase { get; set; }

    /// <summary>Microsoft Entra ID sign-in for Grafana; replaces the login form and basic auth.</summary>
    public GrafanaEntraIdOptions? GrafanaEntraId { get; set; }

    /// <summary>Sign-in through another OpenID Connect provider (Keycloak, Authentik, Auth0, Okta …); replaces the login form and basic auth.</summary>
    public GrafanaOAuthOptions? GrafanaOAuth { get; set; }
}

/// <summary>An S3-compatible bucket (e.g. MinIO) for Loki or Tempo.</summary>
public sealed class S3StorageOptions
{
    /// <summary><c>host:port</c> of the S3 API without scheme — e.g. an endpoint's <c>HostAndPort</c> property.</summary>
    public required ReferenceExpression Endpoint { get; init; }

    /// <summary>Bucket name; it has to exist (neither Loki nor Tempo creates buckets).</summary>
    public required string Bucket { get; init; }

    public required ReferenceExpression AccessKey { get; init; }
    public required ReferenceExpression SecretKey { get; init; }

    /// <summary>Plain HTTP instead of HTTPS.</summary>
    public bool Insecure { get; init; }

    public string Region { get; init; } = "us-east-1";
}

/// <summary>A Postgres database for Grafana's own state.</summary>
public sealed class GrafanaDatabaseOptions
{
    /// <summary><c>host:port</c> of the Postgres server.</summary>
    public required ReferenceExpression HostAndPort { get; init; }

    public string Name { get; init; } = "grafana";
    public string User { get; init; } = "grafana";
    public required ReferenceExpression Password { get; init; }
    public string SslMode { get; init; } = "disable";
}

/// <summary>
/// Sign-in through an OpenID Connect provider with Grafana's generic OAuth. Grafana does no
/// discovery there, so the three endpoints are named; <see cref="Keycloak"/> derives them from a
/// realm. The redirect URI to register is <c>{grafana-url}/login/generic_oauth</c>.
/// </summary>
public sealed class GrafanaOAuthOptions
{
    /// <summary>What the login button says: "Sign in with …".</summary>
    public required string Name { get; init; }
    public required string ClientId { get; init; }
    public required ReferenceExpression ClientSecret { get; init; }
    public required string AuthUrl { get; init; }
    public required string TokenUrl { get; init; }

    /// <summary>The provider's userinfo endpoint.</summary>
    public required string ApiUrl { get; init; }

    public string Scopes { get; init; } = "openid email profile";

    /// <summary>
    /// JMESPath over the ID token and userinfo claims yielding <c>GrafanaAdmin</c>, <c>Admin</c>,
    /// <c>Editor</c> or <c>Viewer</c> — <see cref="Roles"/> builds one. Whoever it yields nothing
    /// for is turned away.
    /// </summary>
    public required string RoleAttributePath { get; init; }

    /// <summary>
    /// A Keycloak realm, e.g. <c>https://sso.example.com/realms/company</c>. Roles come from the
    /// <c>roles</c> claim (client roles <c>grafana-admin</c>, <c>admin</c>, <c>editor</c>,
    /// <c>viewer</c>); Keycloak puts them there with a "User Client Role" mapper whose token claim
    /// name is <c>roles</c>, added to the ID token and userinfo.
    /// </summary>
    public static GrafanaOAuthOptions Keycloak(string realmUrl, string clientId, ReferenceExpression clientSecret,
        string? roleAttributePath = null, string name = "Keycloak")
    {
        if (!Uri.TryCreate(realmUrl, UriKind.Absolute, out var realm)
            || (realm.Scheme != Uri.UriSchemeHttps && realm.Scheme != Uri.UriSchemeHttp)
            || !realm.AbsolutePath.Contains("/realms/", StringComparison.Ordinal))
            throw new ArgumentException($"a realm URL looks like https://sso.example.com/realms/company — {realmUrl}", nameof(realmUrl));

        var protocol = $"{realmUrl.TrimEnd('/')}/protocol/openid-connect";
        return new GrafanaOAuthOptions
        {
            Name = name,
            ClientId = clientId,
            ClientSecret = clientSecret,
            AuthUrl = $"{protocol}/auth",
            TokenUrl = $"{protocol}/token",
            ApiUrl = $"{protocol}/userinfo",
            RoleAttributePath = roleAttributePath
                ?? Roles("roles", grafanaAdmins: ["grafana-admin"], admins: ["admin"], editors: ["editor"], viewers: ["viewer"]),
        };
    }

    /// <summary>
    /// A <see cref="RoleAttributePath"/> for names in a claim array, e.g.
    /// <c>Roles("groups", admins: ["ops"], viewers: ["staff"])</c>. In someone with several, the
    /// highest wins: GrafanaAdmin, Admin, Editor, Viewer. A token without the claim matches nothing
    /// instead of failing the evaluation.
    /// </summary>
    public static string Roles(string claim, string[]? grafanaAdmins = null, string[]? admins = null,
        string[]? editors = null, string[]? viewers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claim);
        var terms = new[] { ("GrafanaAdmin", grafanaAdmins), ("Admin", admins), ("Editor", editors), ("Viewer", viewers) }
            .SelectMany(level => (level.Item2 ?? []).Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => $"contains({claim}[*] || `[]`, '{n.Replace("\\", "\\\\").Replace("'", "\\'")}') && '{level.Item1}'"))
            .ToArray();
        if (terms.Length == 0) throw new ArgumentException("name at least one role", nameof(claim));
        return string.Join(" || ", terms);
    }
}

/// <summary>
/// Microsoft Entra ID (Azure AD) sign-in. Needs an app registration in the tenant with the
/// redirect URI <c>{grafana-url}/login/azuread</c>, a client secret and the app roles
/// <c>Admin</c>, <c>Editor</c> and <c>Viewer</c> (plus <c>GrafanaAdmin</c> for server admins —
/// there is no local admin) — Grafana maps those roles itself, and a user without one is turned away.
/// </summary>
public sealed class GrafanaEntraIdOptions
{
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required ReferenceExpression ClientSecret { get; init; }

    /// <summary>Object IDs of Entra groups allowed to sign in. Empty = every user with an app role.</summary>
    public IReadOnlyList<string> AllowedGroups { get; init; } = [];
}

/// <summary>
/// Connection options for the <c>postgres_exporter</c> sidecar.
/// </summary>
public sealed class PostgresExporterOptions
{
    /// <summary>Hostname or container name of the Postgres instance (Docker DNS). Required.</summary>
    public required string Host { get; set; }

    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "postgres";
    public string Username { get; set; } = "postgres";

    /// <summary>Password for the exporter's read connection. Required.</summary>
    public required string Password { get; set; }

    /// <summary>SSL mode. Default <c>"disable"</c> for local Docker setups.</summary>
    public string SslMode { get; set; } = "disable";

    /// <summary>Login of Grafana's SQL datasource; defaults to <see cref="Username"/>. Give it a read-only role on shared setups.</summary>
    public string? GrafanaUsername { get; set; }

    /// <summary>Password of <see cref="GrafanaUsername"/>; defaults to <see cref="Password"/>.</summary>
    public string? GrafanaPassword { get; set; }

    /// <summary>Builds the libpq-style DSN that postgres_exporter expects.</summary>
    internal string ToDataSourceName() =>
        $"postgresql://{Username}:{Password}@{Host}:{Port}/{Database}?sslmode={SslMode}";
}
