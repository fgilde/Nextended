using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.SigNoz;

/// <summary>
/// SigNoz as Aspire resources: <see cref="AddSigNoz"/> adds ClickHouse, the SigNoz collector and SigNoz, with an admin,
/// dashboards and alert rules for .NET services provisioned on every start. Point the services at it with
/// <see cref="WithSigNozTelemetry{T}"/>.
/// </summary>
public static class SigNozBuilderExtensions
{
    private const string MigrateAndRun =
        "/signoz-otel-collector migrate ready && /signoz-otel-collector migrate bootstrap && " +
        "/signoz-otel-collector migrate sync up && /signoz-otel-collector migrate async up && exec /signoz-otel-collector";

    /// <summary>
    /// Adds SigNoz (<paramref name="name"/>), its ClickHouse (<c>{name}-clickhouse</c>) and collector (<c>{name}-collector</c>).
    /// Its admin is <c>admin@signoz.local</c> with a generated password (parameter <c>{name}-password</c>) until
    /// <see cref="WithAdminUser(IResourceBuilder{SigNozResource}, string, IResourceBuilder{ParameterResource}, string?)"/> names one.
    /// Generated files land under <paramref name="configRootPath"/><c>/.generated/signoz/{name}</c> (default: <c>{AppHostDirectory}/observability</c>).
    /// </summary>
    public static IResourceBuilder<SigNozResource> AddSigNoz(this IDistributedApplicationBuilder builder, string name = "signoz", string? configRootPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var stack = new SigNozStack
        {
            Name = name,
            ConfigRootPath = Path.GetFullPath(configRootPath ?? Path.Combine(builder.AppHostDirectory, "observability")),
            IsPublishMode = builder.ExecutionContext.IsPublishMode,
        };
        var publishing = stack.IsPublishMode;
        var scheme = publishing ? "tcp" : "http";

        var clickHouse = builder.AddResource(new SigNozClickHouseResource($"{name}-clickhouse"))
            .WithImage(SigNozDefaults.ClickHouseImage, SigNozDefaults.ClickHouseTag)
            .WithEndpoint(targetPort: publishing ? SigNozStack.PublishedClickHousePort : SigNozClickHouseResource.NativeTargetPort, port: publishing ? SigNozStack.PublishedClickHousePort : null,
                name: SigNozClickHouseResource.NativeEndpointName, scheme: "tcp")
            .WithEntrypoint("/bin/bash")
            .WithArgs($"{SigNozStack.ClickHouseConfigDir}/start.sh")
            .WithEnvironment("CLICKHOUSE_CONFIG", $"{SigNozStack.ClickHouseConfigDir}/config.yaml")
            .WithEnvironment("CLICKHOUSE_SKIP_USER_SETUP", "1");
        if (!publishing)
        {
            clickHouse.WithHttpEndpoint(targetPort: SigNozClickHouseResource.HttpTargetPort, name: SigNozClickHouseResource.HttpEndpointName)
                .WithUrls(context => context.Urls.Clear())
                .WithHttpHealthCheck("/ping", endpointName: SigNozClickHouseResource.HttpEndpointName);
        }

        var native = clickHouse.GetEndpoint(SigNozClickHouseResource.NativeEndpointName);
        var clickHouseAddress = ReferenceExpression.Create($"{native.Property(EndpointProperty.Host)}:{native.Property(EndpointProperty.Port)}");

        var signoz = builder.AddResource(new SigNozResource(name) { Stack = stack })
            .WithImage(SigNozDefaults.SigNozImage, SigNozDefaults.SigNozTag)
            .WithHttpEndpoint(targetPort: SigNozResource.DefaultTargetPort, name: SigNozResource.HttpEndpointName)
            .WithExternalHttpEndpoints()
            .WithEntrypoint("/bin/sh")
            .WithArgs($"{SigNozStack.ProvisioningDir}/start.sh")
            .WithEnvironment("SIGNOZ_TELEMETRYSTORE_PROVIDER", "clickhouse")
            .WithEnvironment("SIGNOZ_TELEMETRYSTORE_CLICKHOUSE_DSN", ReferenceExpression.Create($"tcp://{clickHouseAddress}"))
            .WithEnvironment("SIGNOZ_USER_ROOT_ENABLED", "true")
            .WithEnvironment("SIGNOZ_USER_ROOT_ORG_NAME", name)
            .WithEnvironment("SIGNOZ_ANALYTICS_ENABLED", "false")
            .WithEnvironment("SIGNOZ_STATSREPORTER_ENABLED", "false")
            .WithEnvironment("SIGNOZ_VERSION_BANNER_ENABLED", "false")
            .WithHttpHealthCheck("/api/v1/health", endpointName: SigNozResource.HttpEndpointName)
            .WaitFor(clickHouse);
        if (stack.OpAmp)
        {
            signoz.WithEndpoint(targetPort: SigNozResource.OpAmpTargetPort, name: SigNozResource.OpAmpEndpointName, scheme: "tcp")
                .WithUrlForEndpoint(SigNozResource.OpAmpEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);
        }

        stack.SigNoz = signoz;
        WithExternalUrl(signoz, signoz.GetEndpoint(SigNozResource.HttpEndpointName));

        var collectorArgs = $"{MigrateAndRun} --config={SigNozStack.CollectorConfigDir}/config.yaml"
            + (stack.OpAmp ? $" --manager-config={SigNozStack.CollectorConfigDir}/opamp.yaml --copy-path=/var/tmp/collector-config.yaml" : "");
        var collector = builder.AddResource(new SigNozCollectorResource($"{name}-collector"))
            .WithImage(SigNozDefaults.CollectorImage, SigNozDefaults.CollectorTag)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", collectorArgs)
            .WithEnvironment("SIGNOZ_OTEL_COLLECTOR_CLICKHOUSE_DSN", ReferenceExpression.Create($"tcp://{clickHouseAddress}"))
            .WithEnvironment("CLICKHOUSE_ENDPOINT", clickHouseAddress)
            .WithEnvironment("SIGNOZ_OTEL_COLLECTOR_TIMEOUT", "10m")
            .WithEnvironment("LOW_CARDINAL_EXCEPTION_GROUPING", "false")
            .WaitFor(clickHouse);
        // a deployment receives OTLP over HTTP only, on a port of its own (Grafana's collector may hold 4318 in the same environment)
        if (!publishing)
        {
            collector.WithEndpoint(targetPort: SigNozCollectorResource.OtlpGrpcTargetPort, name: SigNozCollectorResource.OtlpGrpcEndpointName, scheme: "http");
        }

        collector.WithEndpoint(targetPort: publishing ? SigNozStack.PublishedOtlpHttpPort : SigNozCollectorResource.OtlpHttpTargetPort, port: publishing ? SigNozStack.PublishedOtlpHttpPort : null,
            name: SigNozCollectorResource.OtlpHttpEndpointName, scheme: scheme);

        stack.ClickHouse = stack.MountConfig(clickHouse, Path.Combine(stack.GeneratedDir, "clickhouse"), SigNozStack.ClickHouseConfigDir);
        stack.Collector = stack.MountConfig(collector, Path.Combine(stack.GeneratedDir, "collector"), SigNozStack.CollectorConfigDir);
        stack.MountConfig(signoz, Path.Combine(stack.GeneratedDir, "signoz"), SigNozStack.ProvisioningDir);
        stack.Subscribe(builder);

        return signoz.WithAdminUser(SigNozDefaults.AdminEmail, builder.AddParameter($"{name}-password",
            new GenerateParameterDefault { MinLength = 20, MinLower = 1, MinUpper = 1, MinNumeric = 1, MinSpecial = 1 }, secret: true, persist: true));
    }

    // ---- Sign-in ------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The admin (SigNoz's root user, created on every start and not changeable in the UI) and its organisation (default: the resource name).
    /// SigNoz wants a valid email address and a password of at least 12 characters with upper and lower case letters, a digit and a symbol.
    /// </summary>
    public static IResourceBuilder<SigNozResource> WithAdminUser(this IResourceBuilder<SigNozResource> signoz, string email, string password, string? organization = null)
    {
        if (!IsValidAdminPassword(password))
        {
            throw new ArgumentException("SigNoz wants at least 12 characters with an upper and a lower case letter, a digit and a symbol", nameof(password));
        }

        return signoz.WithAdmin(email, organization).WithEnvironment("SIGNOZ_USER_ROOT_PASSWORD", password);
    }

    /// <inheritdoc cref="WithAdminUser(IResourceBuilder{SigNozResource}, string, string, string?)"/>
    public static IResourceBuilder<SigNozResource> WithAdminUser(this IResourceBuilder<SigNozResource> signoz, string email, IResourceBuilder<ParameterResource> password, string? organization = null) =>
        signoz.WithAdmin(email, organization).WithEnvironment("SIGNOZ_USER_ROOT_PASSWORD", password);

    /// <inheritdoc cref="WithAdminUser(IResourceBuilder{SigNozResource}, string, string, string?)"/>
    public static IResourceBuilder<SigNozResource> WithAdminUser(this IResourceBuilder<SigNozResource> signoz, ReferenceExpression email, ReferenceExpression password, string? organization = null)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);
        if (organization is not null)
        {
            signoz.WithEnvironment("SIGNOZ_USER_ROOT_ORG_NAME", organization);
        }

        return signoz.WithEnvironment("SIGNOZ_USER_ROOT_EMAIL", email).WithEnvironment("SIGNOZ_USER_ROOT_PASSWORD", password);
    }

    private static IResourceBuilder<SigNozResource> WithAdmin(this IResourceBuilder<SigNozResource> signoz, string email, string? organization)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (!email.Contains('@', StringComparison.Ordinal) || !email[(email.IndexOf('@', StringComparison.Ordinal) + 1)..].Contains('.', StringComparison.Ordinal))
        {
            throw new ArgumentException($"SigNoz wants an email address with a domain like admin@example.com — {email}", nameof(email));
        }

        if (organization is not null)
        {
            signoz.WithEnvironment("SIGNOZ_USER_ROOT_ORG_NAME", organization);
        }

        return signoz.WithEnvironment("SIGNOZ_USER_ROOT_EMAIL", email);
    }

    /// <summary>SigNoz's password rule: 12 characters, upper and lower case, a digit and a symbol of its list, nothing else.</summary>
    public static bool IsValidAdminPassword(string password)
    {
        const string symbols = "~!@#$%^&*()_+`-={}|[]\\:\"<>?,./";
        return password is { Length: >= 12 }
            && password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit) && password.Any(symbols.Contains)
            && password.All(c => char.IsLetter(c) || char.IsDigit(c) || symbols.Contains(c));
    }

    /// <summary>
    /// No sign-in: every request acts as the admin (SigNoz's impersonation). Convenient while developing, never for shared or deployed setups.
    /// API keys and the login form are off then.
    /// </summary>
    public static IResourceBuilder<SigNozResource> WithAnonymousAdmin(this IResourceBuilder<SigNozResource> signoz) =>
        signoz.WithEnvironment("SIGNOZ_IDENTN_IMPERSONATION_ENABLED", "true")
            .WithEnvironment("SIGNOZ_IDENTN_TOKENIZER_ENABLED", "false")
            .WithEnvironment("SIGNOZ_IDENTN_APIKEY_ENABLED", "false");

    /// <summary>
    /// Sign-in at an OpenID Connect provider (Keycloak, Entra ID, Authentik, Auth0, an app's own auth server …) in front of SigNoz.
    /// SigNoz's own single sign-on (OIDC, SAML) is part of its licensed edition, so an oauth2-proxy (<c>{name}-signin</c>) takes the
    /// browser's requests, lets only who the provider vouches for (and <see cref="SigNozOidcOptions"/> allows) through and forwards them to
    /// SigNoz, which acts as its admin for each of them (<see cref="WithAnonymousAdmin"/>). SigNoz itself is reachable through the proxy only.
    /// Register <c>{proxy-url}/oauth2/callback</c> as redirect URI.
    /// </summary>
    public static IResourceBuilder<SigNozResource> WithOidcLogin(this IResourceBuilder<SigNozResource> signoz, SigNozOidcOptions oidc)
    {
        ArgumentNullException.ThrowIfNull(oidc);
        ArgumentException.ThrowIfNullOrWhiteSpace(oidc.ClientId);
        foreach (var (name, url) in new[] { ("IssuerUrl", oidc.IssuerUrl), ("LoginUrl", oidc.LoginUrl), ("RedeemUrl", oidc.RedeemUrl), ("JwksUrl", oidc.JwksUrl) })
        {
            if (url is not null && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            {
                throw new ArgumentException($"{name} is an absolute http(s) URL of the provider — {url}", nameof(oidc));
            }
        }

        if ((oidc.LoginUrl is null) != (oidc.RedeemUrl is null) || (oidc.LoginUrl is null) != (oidc.JwksUrl is null))
        {
            throw new ArgumentException("LoginUrl, RedeemUrl and JwksUrl go together: all three skip the provider's discovery, none uses it", nameof(oidc));
        }

        var stack = signoz.Resource.Stack;
        var builder = signoz.ApplicationBuilder;
        var publishing = builder.ExecutionContext.IsPublishMode;
        var proxy = stack.SignIn ??= builder.AddContainer($"{signoz.Resource.Name}-signin", SigNozDefaults.SignInImage, SigNozDefaults.SignInTag)
            .WithImageRegistry(SigNozDefaults.SignInRegistry)
            .WithHttpEndpoint(targetPort: 4180, name: "http")
            .WithExternalHttpEndpoints()
            .WithUrlForEndpoint("http", u => u.DisplayText = "SigNoz")
            .WaitFor(signoz);

        // only the proxy is the way in: no link in the dashboard, and a deployment publishes no port of SigNoz itself
        signoz.WithUrls(context => context.Urls.Clear())
            .WithEndpoint(SigNozResource.HttpEndpointName, e => e.IsExternal = false)
            .WithAnonymousAdmin();
        var browser = proxy.GetEndpoint("http");
        WithExternalUrl(signoz, browser);

        proxy.WithEnvironment("OAUTH2_PROXY_PROVIDER", oidc.Provider)
            .WithEnvironment("OAUTH2_PROXY_PROVIDER_DISPLAY_NAME", oidc.Name)
            .WithEnvironment("OAUTH2_PROXY_OIDC_ISSUER_URL", oidc.IssuerUrl)
            .WithEnvironment("OAUTH2_PROXY_CLIENT_ID", oidc.ClientId)
            .WithEnvironment("OAUTH2_PROXY_CLIENT_SECRET", oidc.ClientSecret)
            .WithEnvironment("OAUTH2_PROXY_CODE_CHALLENGE_METHOD", "S256")
            .WithEnvironment("OAUTH2_PROXY_SCOPE", oidc.Scopes)
            .WithEnvironment("OAUTH2_PROXY_EMAIL_DOMAINS", string.Join(',', oidc.AllowedEmailDomains))
            .WithEnvironment("OAUTH2_PROXY_SKIP_PROVIDER_BUTTON", "true")
            .WithEnvironment("OAUTH2_PROXY_UPSTREAMS", signoz.GetEndpoint(SigNozResource.HttpEndpointName))
            // the upstream gets its own host name: Azure Container Apps routes by host and answered the proxy's public one with 404
            .WithEnvironment("OAUTH2_PROXY_PASS_HOST_HEADER", "false")
            .WithEnvironment("OAUTH2_PROXY_HTTP_ADDRESS", "0.0.0.0:4180")
            .WithEnvironment("OAUTH2_PROXY_COOKIE_SECRET", builder.AddParameter($"{signoz.Resource.Name}-signin-cookie-secret",
                new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true))
            // browsers send cookies to every port of localhost
            .WithEnvironment("OAUTH2_PROXY_COOKIE_NAME", $"_{signoz.Resource.Name}_signin")
            .WithEnvironment("OAUTH2_PROXY_COOKIE_SECURE", publishing ? "true" : "false")
            .WithEnvironment("OAUTH2_PROXY_REVERSE_PROXY", publishing ? "true" : "false");
        if (oidc.LoginUrl is not null)
        {
            proxy.WithEnvironment("OAUTH2_PROXY_SKIP_OIDC_DISCOVERY", "true")
                .WithEnvironment("OAUTH2_PROXY_LOGIN_URL", oidc.LoginUrl)
                .WithEnvironment("OAUTH2_PROXY_REDEEM_URL", oidc.RedeemUrl!)
                .WithEnvironment("OAUTH2_PROXY_OIDC_JWKS_URL", oidc.JwksUrl!);
        }

        if (oidc.AudienceClaims is { Count: > 0 } audience)
        {
            proxy.WithEnvironment("OAUTH2_PROXY_OIDC_AUDIENCE_CLAIMS", string.Join(',', audience));
        }

        if (oidc.AllowedRoles.Count > 0)
        {
            proxy.WithEnvironment("OAUTH2_PROXY_ALLOWED_ROLES", string.Join(',', oidc.AllowedRoles));
        }

        if (oidc.AllowedGroups.Count > 0)
        {
            proxy.WithEnvironment("OAUTH2_PROXY_ALLOWED_GROUPS", string.Join(',', oidc.AllowedGroups))
                .WithEnvironment("OAUTH2_PROXY_OIDC_GROUPS_CLAIM", oidc.GroupsClaim);
        }

        // deployed, the reference is the public URL; locally it would resolve to the container network, the allocated endpoint is the browser's address
        if (publishing)
        {
            proxy.WithEnvironment("OAUTH2_PROXY_REDIRECT_URL", ReferenceExpression.Create($"{browser}/oauth2/callback"));
        }
        else
        {
            proxy.WithEnvironment(context => context.EnvironmentVariables["OAUTH2_PROXY_REDIRECT_URL"] = $"{browser.Url}/oauth2/callback");
        }

        return signoz;
    }

    // links in alerts and the UI's own redirects: the address the browser knows SigNoz by
    private static void WithExternalUrl(IResourceBuilder<SigNozResource> signoz, EndpointReference browser)
    {
        if (signoz.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            signoz.WithEnvironment("SIGNOZ_GLOBAL_EXTERNAL__URL", browser)
                .WithEnvironment("SIGNOZ_ALERTMANAGER_SIGNOZ_EXTERNAL__URL", browser);
        }
        else
        {
            signoz.WithEnvironment(context =>
            {
                context.EnvironmentVariables["SIGNOZ_GLOBAL_EXTERNAL__URL"] = browser.Url;
                context.EnvironmentVariables["SIGNOZ_ALERTMANAGER_SIGNOZ_EXTERNAL__URL"] = browser.Url;
            });
        }
    }

    // ---- Content ------------------------------------------------------------------------------------------------------

    /// <summary>
    /// More dashboards: SigNoz dashboard JSON (schema v6, as the UI's JSON button shows it), one file per dashboard. Each is created on start
    /// when no dashboard of its <c>name</c> exists; changes in the UI survive restarts. A file named like a built-in one replaces it.
    /// </summary>
    public static IResourceBuilder<SigNozResource> WithDashboards(this IResourceBuilder<SigNozResource> signoz, string hostPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        signoz.Resource.Stack.DashboardFolders.Add(Path.GetFullPath(hostPath));
        return signoz;
    }

    /// <summary>More alert rules: the JSON of SigNoz's rules API (<c>POST /api/v2/rules</c>), one file per rule; created on start when no rule of its <c>alert</c> name exists.</summary>
    public static IResourceBuilder<SigNozResource> WithAlertRules(this IResourceBuilder<SigNozResource> signoz, string hostPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        signoz.Resource.Stack.AlertFolders.Add(Path.GetFullPath(hostPath));
        return signoz;
    }

    /// <summary>Leaves out the dashboards and alert rules of this package (a deleted one would come back on the next start otherwise).</summary>
    public static IResourceBuilder<SigNozResource> WithoutBuiltInContent(this IResourceBuilder<SigNozResource> signoz)
    {
        signoz.Resource.Stack.BuiltInContent = false;
        return signoz;
    }

    // ---- Components and persistence -----------------------------------------------------------------------------------

    /// <summary>Configures ClickHouse, e.g. <c>c =&gt; c.WithDataVolume()</c>.</summary>
    public static IResourceBuilder<SigNozResource> WithClickHouse(this IResourceBuilder<SigNozResource> signoz, Action<IResourceBuilder<SigNozClickHouseResource>> configure)
    {
        configure(signoz.Resource.Stack.ClickHouse);
        return signoz;
    }

    /// <summary>Configures the collector.</summary>
    public static IResourceBuilder<SigNozResource> WithCollector(this IResourceBuilder<SigNozResource> signoz, Action<IResourceBuilder<SigNozCollectorResource>> configure)
    {
        configure(signoz.Resource.Stack.Collector);
        return signoz;
    }

    /// <summary>Keeps SigNoz's own state (users, dashboards, alert rules, settings; SQLite) in a named volume.</summary>
    public static IResourceBuilder<SigNozResource> WithDataVolume(this IResourceBuilder<SigNozResource> signoz, string? name = null) =>
        signoz.WithVolume(name ?? $"{signoz.Resource.Name}-data", "/var/lib/signoz");

    /// <summary>Keeps the telemetry (and the Keeper's state) in a named volume. Needs a POSIX file system: on Azure Files over SMB every insert fails.</summary>
    public static IResourceBuilder<SigNozClickHouseResource> WithDataVolume(this IResourceBuilder<SigNozClickHouseResource> clickHouse, string? name = null) =>
        clickHouse.WithVolume(name ?? $"{clickHouse.Resource.Name}-data", "/var/lib/clickhouse");

    // ---- Telemetry ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// Points a service's OTLP exporter at SigNoz's collector: <paramref name="endpointVariable"/> gets the address, <paramref name="protocolVariable"/>
    /// the protocol — gRPC while running, <c>http/protobuf</c> in a deployment (the collector takes OTLP over HTTP only there, an exporter appends
    /// <c>/v1/traces</c> etc.). The defaults replace the Aspire dashboard as the target; pass your own names to send to both.
    /// </summary>
    public static IResourceBuilder<T> WithSigNozTelemetry<T>(this IResourceBuilder<T> resource, IResourceBuilder<SigNozResource> signoz,
        string endpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT", string protocolVariable = "OTEL_EXPORTER_OTLP_PROTOCOL")
        where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(signoz);
        var collector = signoz.Resource.Collector;
        if (resource.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            var http = collector.OtlpHttpEndpoint;
            return resource.WithEnvironment(endpointVariable, ReferenceExpression.Create($"http://{http.Property(EndpointProperty.Host)}:{http.Property(EndpointProperty.Port)}"))
                .WithEnvironment(protocolVariable, "http/protobuf");
        }

        return resource.WithEnvironment(endpointVariable, collector.OtlpGrpcEndpoint).WithEnvironment(protocolVariable, "grpc");
    }
}
