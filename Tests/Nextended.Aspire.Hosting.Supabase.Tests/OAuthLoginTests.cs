using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Aspire.Hosting.Grafana;
using Nextended.Aspire.Hosting.Observability;
using Nextended.Aspire.Hosting.Supabase.Builders;

using Nextended.Aspire.Hosting.Supabase.Resources;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// Sign-in beyond passwords: Supabase Auth as OAuth 2.1 provider for Grafana, Studio and other
/// clients (consent page, clients with stable ids, access token hook), external providers for the
/// app (Keycloak …), and the login proxy in front of Studio — the latter run for real with node.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public sealed class OAuthLoginTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"oauth-{Guid.NewGuid():N}");

    public OAuthLoginTests()
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

    private (IDistributedApplicationBuilder Builder, IResourceBuilder<SupabaseStackResource> Supabase) Stack(bool publish)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = publish ? ["--operation", "publish", "--publisher", "manifest"] : [],
        });
        builder.Configuration["DcpPublisher:CliPath"] = "unused";
        builder.Configuration["DcpPublisher:DashboardPath"] = "unused";
        if (publish) builder.AddMinioS3OnNfs("nfs");
        return (builder, builder.AddSupabase("sb"));
    }

    private void WithGrafana(IDistributedApplicationBuilder builder, IResourceBuilder<SupabaseStackResource> supabase,
        Action<ObservabilityStackOptions> configure) =>
        builder.AddObservabilityStack(supabase, options =>
        {
            options.ConfigRootPath = _root;
            options.DashboardsPath = Path.Combine(_root, "grafana", "dashboards");
            configure(options);
        });

    private static async Task<Dictionary<string, string>> Env(IDistributedApplicationBuilder builder, string name, bool publish = true)
    {
        var resource = (IResourceWithEnvironment)builder.Resources.Single(r => r.Name == name);
#pragma warning disable CS0618 // no model-only equivalent yet
        return await resource.GetEnvironmentVariableValuesAsync(publish ? DistributedApplicationOperation.Publish : DistributedApplicationOperation.Run)
            .AsTask().WaitAsync(TimeSpan.FromSeconds(10));
#pragma warning restore CS0618
    }

    private static void Allocate(IDistributedApplicationBuilder builder, string resource, int port)
    {
        var http = builder.Resources.Single(r => r.Name == resource).Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "http");
        http.AllocatedEndpoint = new AllocatedEndpoint(http, "localhost", port);
    }

    private static string PostInitSql(Dictionary<string, string> env)
    {
        var parts = int.Parse(env.GetValueOrDefault("POST_INIT_SQL_GZ_PARTS", "1"));
        var blob = string.Concat(Enumerable.Range(0, parts).Select(i => env[$"POST_INIT_SQL_GZ_BASE64_{i}"]));
        using var gz = new System.IO.Compression.GZipStream(new MemoryStream(Convert.FromBase64String(blob)), System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(gz);
        return reader.ReadToEnd();
    }

    // ---- Supabase Auth settings --------------------------------------------------------------------

    [Fact]
    public async Task The_oauth_server_and_the_access_token_hook_are_auth_settings()
    {
        var (builder, supabase) = Stack(publish: true);
        supabase.WithOAuthServer().WithCustomAccessTokenHook();

        var auth = await Env(builder, "sb-auth");
        Assert.Equal("true", auth["GOTRUE_OAUTH_SERVER_ENABLED"]);
        Assert.Equal(OAuthBuilderExtensions.DefaultConsentPath, auth["GOTRUE_OAUTH_SERVER_AUTHORIZATION_PATH"]);
        Assert.Equal("true", auth["GOTRUE_HOOK_CUSTOM_ACCESS_TOKEN_ENABLED"]);
        Assert.Equal("pg-functions://postgres/public/custom_access_token_hook", auth["GOTRUE_HOOK_CUSTOM_ACCESS_TOKEN_URI"]);
        Assert.False(auth.ContainsKey("GOTRUE_OAUTH_SERVER_ALLOW_DYNAMIC_REGISTRATION"));

        Assert.Throws<ArgumentException>(() => supabase.WithCustomAccessTokenHook("hook"));
        Assert.Throws<ArgumentException>(() => supabase.WithCustomAccessTokenHook("public.drop table"));
        Assert.Throws<ArgumentException>(() => supabase.WithOAuthServer("oauth/consent"));
    }

    [Fact]
    public async Task Keycloak_signs_the_app_in_through_its_realm_and_comes_back_to_the_public_api()
    {
        var (builder, supabase) = Stack(publish: true);
        supabase.WithPublicUrl("https://api.example.com/")
            .WithKeycloak("https://sso.example.com/realms/acme/", "adam", ReferenceExpression.Create($"kc-secret"));

        var auth = await Env(builder, "sb-auth");
        Assert.Equal("true", auth["GOTRUE_EXTERNAL_KEYCLOAK_ENABLED"]);
        Assert.Equal("adam", auth["GOTRUE_EXTERNAL_KEYCLOAK_CLIENT_ID"]);
        Assert.Equal("kc-secret", auth["GOTRUE_EXTERNAL_KEYCLOAK_SECRET"]);
        Assert.Equal("https://sso.example.com/realms/acme", auth["GOTRUE_EXTERNAL_KEYCLOAK_URL"]);
        Assert.Equal("https://api.example.com/auth/v1/callback", auth["GOTRUE_EXTERNAL_KEYCLOAK_REDIRECT_URI"]);
        Assert.Equal("https://api.example.com", auth["API_EXTERNAL_URL"]);
        Assert.Equal("https://api.example.com", (await Env(builder, "sb"))["SUPABASE_PUBLIC_URL"]);

        Assert.Throws<ArgumentException>(() => supabase.WithKeycloak("https://sso.example.com/acme", "adam", ReferenceExpression.Create($"s")));
        Assert.Throws<ArgumentException>(() => supabase.WithExternalProvider("Key-Cloak", "adam", ReferenceExpression.Create($"s")));
    }

    [Fact]
    public async Task Any_other_provider_comes_back_to_kong_when_there_is_no_public_url()
    {
        var (builder, supabase) = Stack(publish: true);
        supabase.WithExternalProvider("linkedin_oidc", "li", ReferenceExpression.Create($"s"));

        var auth = await Env(builder, "sb-auth");
        Assert.Equal("true", auth["GOTRUE_EXTERNAL_LINKEDIN_OIDC_ENABLED"]);
        Assert.False(auth.ContainsKey("GOTRUE_EXTERNAL_LINKEDIN_OIDC_URL"));
        Assert.Matches(@"^\{sb-kong\.bindings\.http\.url\}/auth/v1/callback$", auth["GOTRUE_EXTERNAL_LINKEDIN_OIDC_REDIRECT_URI"]);
    }

    [Fact]
    public async Task Locally_the_site_url_is_the_frontend_address_the_browser_has()
    {
        // Regression: it resolved to the container-network name (aspire.dev.internal:…), so the
        // OAuth consent redirect and every mail link pointed at an address no browser reaches.
        var (builder, supabase) = Stack(publish: false);
        var frontend = builder.AddContainer("web", "nginx").WithHttpEndpoint(targetPort: 80, name: "http");
        supabase.WithAuthSiteUrl(frontend);
        Allocate(builder, "web", 8080);

        // Only this callback: the rest of Auth's environment holds container-network addresses.
        var env = new Dictionary<string, object>();
        var auth = builder.Resources.Single(r => r.Name == "sb-auth");
        await auth.Annotations.OfType<EnvironmentCallbackAnnotation>().Last()
            .Callback(new EnvironmentCallbackContext(builder.ExecutionContext, env));
        Assert.Equal("http://localhost:8080", env["GOTRUE_SITE_URL"]);
    }

    // ---- OAuth clients -----------------------------------------------------------------------------

    [Fact]
    public void A_client_keeps_its_id_per_stack_and_name()
    {
        Assert.Equal(SupabaseOAuthClient.IdFor("sb", "Grafana"), SupabaseOAuthClient.IdFor("sb", "Grafana"));
        Assert.NotEqual(SupabaseOAuthClient.IdFor("sb", "Grafana"), SupabaseOAuthClient.IdFor("other", "Grafana"));
        Assert.NotEqual(SupabaseOAuthClient.IdFor("sb", "Grafana"), SupabaseOAuthClient.IdFor("sb", "Studio"));
        Assert.Equal('5', SupabaseOAuthClient.IdFor("sb", "Grafana").ToString()[14]);
    }

    [Fact]
    public async Task Deployed_the_init_sql_upserts_the_clients_with_their_redirects_as_variables()
    {
        var (builder, supabase) = Stack(publish: true);
        var tool = supabase.AddOAuthClient("Tool", ReferenceExpression.Create($"https://tool.example.com/cb"));
        Assert.Same(tool, supabase.AddOAuthClient("Tool", ReferenceExpression.Create($"https://tool.example.org/cb")));
        supabase.AddOAuthClient("O'Brien's tool", ReferenceExpression.Create($"https://other.example.com/cb"));

        Assert.Equal("true", (await Env(builder, "sb-auth"))["GOTRUE_OAUTH_SERVER_ENABLED"]);
        var init = await Env(builder, "sb-init");
        Assert.Contains("oauth_client_redirects_0", init["POST_INIT_VAR_NAMES"].Split(' '));
        Assert.Contains("oauth_client_redirects_1", init["POST_INIT_VAR_NAMES"].Split(' '));
        Assert.Equal("https://tool.example.com/cb,https://tool.example.org/cb", init["POST_INIT_VAR_oauth_client_redirects_0"]);

        var sql = PostInitSql(init);
        Assert.Contains("SELECT to_regclass('auth.oauth_clients') IS NOT NULL AS nextended_oauth_clients \\gset", sql);
        Assert.Contains($"VALUES ('{tool.ClientId}', '', 'manual', 'public', 'none', :'oauth_client_redirects_0', 'authorization_code,refresh_token', 'Tool')", sql);
        Assert.Contains("'O''Brien''s tool'", sql);
        Assert.Contains("ON CONFLICT (id) DO UPDATE SET redirect_uris = EXCLUDED.redirect_uris", sql);
        Assert.Contains("\\endif", sql);
    }

    [Fact]
    public async Task Locally_the_init_script_registers_the_clients_and_drops_the_file_without_them()
    {
        var (builder, supabase) = Stack(publish: false);
        var tool = supabase.AddOAuthClient("Tool", ReferenceExpression.Create($"http://localhost:5000/cb"));
        var scripts = Path.Combine(supabase.Resource.InfraRootDirForTests()!, "scripts");

        await BeforeDatabaseStarts(builder);
        var sql = File.ReadAllText(Path.Combine(scripts, "oauth_clients.sql"));
        Assert.Contains($"VALUES ('{tool.ClientId}', '', 'manual', 'public', 'none', :'oauth_client_redirects_0'", sql);
        Assert.DoesNotContain("\r", sql);
        var script = File.ReadAllText(Path.Combine(scripts, "post_init.sh"));
        Assert.Contains("if [ -f /scripts/oauth_clients.sql ]; then", script);
        Assert.Contains("set -- \"$@\" -v \"$n=$(printenv \"POST_INIT_VAR_$n\")\"", script);
        Assert.Contains("\"$@\" -f /scripts/oauth_clients.sql", script);

        // The folder outlives the run: a stack without clients must not register the old ones again.
        var (next, _) = Stack(publish: false);
        await BeforeDatabaseStarts(next);
        Assert.False(File.Exists(Path.Combine(scripts, "oauth_clients.sql")));
    }

    private static async Task BeforeDatabaseStarts(IDistributedApplicationBuilder builder)
    {
        using var app = builder.Build();
        var db = builder.Resources.Single(r => r.Name == "sb-db");
        await app.Services.GetRequiredService<IDistributedApplicationEventing>()
            .PublishAsync(new BeforeResourceStartedEvent(db, app.Services));
    }

    [Fact]
    public void Kong_lets_the_oauth_endpoints_through_without_an_apikey()
    {
        var (_, supabase) = Stack(publish: false);
        var yaml = File.ReadAllText(Path.Combine(supabase.Resource.InfraRootDirForTests()!, "config", "kong.yml")).Replace("\r\n", "\n");
        var start = yaml.IndexOf("  - name: auth-v1-open-oauth\n", StringComparison.Ordinal);
        Assert.True(start >= 0, yaml);
        var block = yaml[start..yaml.IndexOf("\n  - name: ", start + 10, StringComparison.Ordinal)];
        Assert.Contains("url: http://sb-auth:9999/oauth/", block);
        Assert.Contains("- /auth/v1/oauth/", block);
        Assert.DoesNotContain("key-auth", block);
    }

    // ---- Studio --------------------------------------------------------------------------------------

    [Fact]
    public async Task Studio_signs_in_with_the_stack_accounts_instead_of_a_password()
    {
        var (builder, supabase) = Stack(publish: true);
        supabase.WithPublicUrl("https://api.example.com")
            .WithLogin("admin", "secret")
            .WithSupabaseLogin("permissions", "studio.manage", publicUrl: "https://studio.example.com/");

        var studio = await Env(builder, "sb");
        Assert.Equal("oauth", studio["STUDIO_AUTH_MODE"]);
        Assert.Equal(SupabaseOAuthClient.IdFor("sb", "Supabase Studio").ToString(), studio["STUDIO_OAUTH_CLIENT_ID"]);
        Assert.Equal("https://api.example.com/auth/v1/oauth/authorize", studio["STUDIO_OAUTH_AUTH_URL"]);
        Assert.Equal("{sb-auth.bindings.http.url}/oauth/token", studio["STUDIO_OAUTH_TOKEN_URL"]);
        Assert.Equal("https://studio.example.com/__oauth/callback", studio["STUDIO_OAUTH_REDIRECT_URI"]);
        Assert.Equal("email", studio["STUDIO_OAUTH_SCOPES"]);
        Assert.Equal("permissions", studio["STUDIO_OAUTH_CLAIM"]);
        Assert.Equal("studio.manage", studio["STUDIO_OAUTH_VALUE"]);
        Assert.False(studio.ContainsKey("DASHBOARD_PASSWORD"));
        Assert.False(studio.ContainsKey("STUDIO_OAUTH_CLIENT_SECRET"));
        Assert.Equal(StudioBuilderExtensions.AuthProxyScriptSource(), Encoding.UTF8.GetString(Convert.FromBase64String(studio["AUTH_PROXY_SCRIPT_BASE64"])));

        var init = await Env(builder, "sb-init");
        Assert.Equal("{sb.bindings.http.url}/__oauth/callback,https://studio.example.com/__oauth/callback",
            init["POST_INIT_VAR_oauth_client_redirects_0"]);
    }

    [Fact]
    public async Task Locally_the_studio_client_comes_back_to_the_host()
    {
        var (builder, supabase) = Stack(publish: false);
        supabase.WithSupabaseLogin("permissions", "studio.manage");
        Allocate(builder, "sb", 54323);
        Allocate(builder, "sb-kong", 54321);

        // Studio's own environment also holds container-network addresses, which a model-only test
        // cannot resolve; the client registration is what has to point at the host.
        var init = await Env(builder, "sb-init", publish: false);
        Assert.Equal("oauth_client_redirects_0", init["POST_INIT_VAR_NAMES"]);
        Assert.Equal("http://localhost:54323/__oauth/callback", init["POST_INIT_VAR_oauth_client_redirects_0"]);
    }

    [Fact]
    public async Task Studio_can_sign_in_through_keycloak_too()
    {
        var (builder, supabase) = Stack(publish: true);
        supabase.WithOAuthLogin(StudioOAuthOptions.Keycloak("https://sso.example.com/realms/acme", "studio",
            ReferenceExpression.Create($"kc"), backchannelRealmUrl: "http://keycloak:8080/realms/acme"));

        var studio = await Env(builder, "sb");
        Assert.Equal("https://sso.example.com/realms/acme/protocol/openid-connect/auth", studio["STUDIO_OAUTH_AUTH_URL"]);
        Assert.Equal("http://keycloak:8080/realms/acme/protocol/openid-connect/token", studio["STUDIO_OAUTH_TOKEN_URL"]);
        Assert.Equal("kc", studio["STUDIO_OAUTH_CLIENT_SECRET"]);
        Assert.Equal("roles", studio["STUDIO_OAUTH_CLAIM"]);
        Assert.Equal("studio-admin", studio["STUDIO_OAUTH_VALUE"]);
        Assert.Equal("openid email profile", studio["STUDIO_OAUTH_SCOPES"]);
        Assert.False(studio.ContainsKey("STUDIO_OAUTH_REDIRECT_URI"));
        Assert.Throws<ArgumentException>(() => StudioOAuthOptions.Keycloak("https://sso.example.com/acme", "s", ReferenceExpression.Create($"kc")));
    }

    // ---- Grafana -------------------------------------------------------------------------------------

    [Fact]
    public async Task Grafana_signs_in_with_the_stack_accounts_as_a_public_client()
    {
        var (builder, supabase) = Stack(publish: true);
        supabase.WithPublicUrl("https://api.example.com");
        WithGrafana(builder, supabase, options => options.ConfigureGrafana = grafana => grafana.WithSupabaseLogin(supabase,
            GrafanaOAuthOptions.Roles("permissions", grafanaAdmins: ["grafana.manage"]), publicUrl: "https://grafana.example.com", name: "ADAM"));

        var grafana = await Env(builder, "monitoring-grafana");
        Assert.Equal("true", grafana["GF_AUTH_GENERIC_OAUTH_ENABLED"]);
        Assert.Equal("ADAM", grafana["GF_AUTH_GENERIC_OAUTH_NAME"]);
        Assert.Equal(SupabaseOAuthClient.IdFor("sb", "Grafana").ToString(), grafana["GF_AUTH_GENERIC_OAUTH_CLIENT_ID"]);
        Assert.False(grafana.ContainsKey("GF_AUTH_GENERIC_OAUTH_CLIENT_SECRET"));
        Assert.Equal("InParams", grafana["GF_AUTH_GENERIC_OAUTH_AUTH_STYLE"]);
        Assert.Equal("email profile", grafana["GF_AUTH_GENERIC_OAUTH_SCOPES"]);
        Assert.Equal("https://api.example.com/auth/v1/oauth/authorize", grafana["GF_AUTH_GENERIC_OAUTH_AUTH_URL"]);
        Assert.Equal("{sb-auth.bindings.http.url}/oauth/token", grafana["GF_AUTH_GENERIC_OAUTH_TOKEN_URL"]);
        Assert.Equal("{sb-auth.bindings.http.url}/oauth/userinfo", grafana["GF_AUTH_GENERIC_OAUTH_API_URL"]);
        Assert.Equal("access_token", grafana["GF_AUTH_GENERIC_OAUTH_ID_TOKEN_ATTRIBUTE_NAME"]);
        Assert.Equal(GrafanaOAuthOptions.Roles("permissions", grafanaAdmins: ["grafana.manage"]), grafana["GF_AUTH_GENERIC_OAUTH_ROLE_ATTRIBUTE_PATH"]);
        Assert.Equal("true", grafana["GF_AUTH_GENERIC_OAUTH_ROLE_ATTRIBUTE_STRICT"]);
        Assert.Equal("true", grafana["GF_AUTH_DISABLE_LOGIN_FORM"]);
        Assert.Equal("true", grafana["GF_SECURITY_DISABLE_INITIAL_ADMIN_CREATION"]);
        Assert.Equal("https://grafana.example.com", grafana["GF_SERVER_ROOT_URL"]);

        var init = await Env(builder, "sb-init");
        Assert.Equal("{monitoring-grafana.bindings.http.url}/login/generic_oauth,https://grafana.example.com/login/generic_oauth",
            init["POST_INIT_VAR_oauth_client_redirects_0"]);
    }

    [Fact]
    public async Task Locally_the_grafana_client_comes_back_to_the_host()
    {
        var (builder, supabase) = Stack(publish: false);
        WithGrafana(builder, supabase, options => options.ConfigureGrafana = grafana =>
            grafana.WithSupabaseLogin(supabase, GrafanaOAuthOptions.Roles("permissions", admins: ["grafana.manage"])));
        Allocate(builder, "monitoring-grafana", 43210);
        Allocate(builder, "sb-kong", 54321);

        var init = await Env(builder, "sb-init", publish: false);
        Assert.Equal("http://localhost:43210/login/generic_oauth", init["POST_INIT_VAR_oauth_client_redirects_0"]);
    }

    // ---- the login proxy, run with node ------------------------------------------------------------

    [Fact]
    public async Task The_studio_proxy_signs_in_with_pkce_and_admits_only_the_permission()
    {
        if (!NodeAvailable()) return; // the proxy is JavaScript; without node there is nothing to run

        var dir = Path.Combine(_root, "proxy");
        Directory.CreateDirectory(dir);
        var proxyPath = Path.Combine(dir, "proxy.js");
        File.WriteAllText(proxyPath, StudioBuilderExtensions.AuthProxyScriptSource());
        var tokenPort = FreePort();
        // Stands in for Studio (on PORT) and the provider's token endpoint (on TOKEN_PORT): the code
        // decides whether the token carries the permission.
        File.WriteAllText(Path.Combine(dir, "fake.js"), """
            const http = require('http');
            http.createServer((q, s) => s.end('studio:' + q.url)).listen(process.env.PORT);
            const jwt = (claims) => ['{"alg":"HS256"}', JSON.stringify(claims), 'sig'].map(p => Buffer.from(p).toString('base64url')).join('.');
            http.createServer((q, s) => {
              let body = ''; q.on('data', c => body += c); q.on('end', () => {
                const f = new URLSearchParams(body);
                if (f.get('grant_type') !== 'authorization_code' || f.get('client_id') !== 'c1' || !f.get('code_verifier')) { s.writeHead(400); return s.end('{"error":"invalid_request"}'); }
                const permissions = f.get('code') === 'ok' ? ['grafana.manage', 'studio.manage'] : [];
                s.setHeader('Content-Type', 'application/json');
                s.end(JSON.stringify({ access_token: jwt({ sub: 'u1', email: 'a@b.c', permissions }), token_type: 'bearer' }));
              });
            }).listen(process.env.TOKEN_PORT);
            """);

        var port = FreePort();
        using var proxy = StartNode(proxyPath, dir, new Dictionary<string, string>
        {
            ["PORT"] = port.ToString(),
            ["TOKEN_PORT"] = tokenPort.ToString(),
            ["STUDIO_SERVER_JS"] = "fake.js",
            ["STUDIO_APP_DIR"] = dir,
            ["STUDIO_PROXY_DELAY_MS"] = "100",
            ["STUDIO_AUTH_MODE"] = "oauth",
            ["STUDIO_OAUTH_AUTH_URL"] = "http://provider.test/oauth/authorize",
            ["STUDIO_OAUTH_TOKEN_URL"] = $"http://127.0.0.1:{tokenPort}/token",
            ["STUDIO_OAUTH_CLIENT_ID"] = "c1",
            ["STUDIO_OAUTH_SCOPES"] = "email",
            ["STUDIO_OAUTH_CLAIM"] = "permissions",
            ["STUDIO_OAUTH_VALUE"] = "studio.manage",
            ["AUTH_JWT_SECRET"] = "test-secret",
        });
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
        };
        await WaitUntilListening(proxy);

        // A page load goes to the provider with PKCE; Studio's API calls get a 401.
        var start = await Get(http, "/project/default?x=1", html: true);
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        var authorize = start.Headers.Location!;
        Assert.Equal("provider.test", authorize.Host);
        var query = System.Web.HttpUtility.ParseQueryString(authorize.Query);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("c1", query["client_id"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(43, query["code_challenge"]!.Length);
        Assert.Equal($"http://127.0.0.1:{port}/__oauth/callback", query["redirect_uri"]);
        Assert.Equal("email", query["scope"]);
        var pending = Cookie(start, "studio_oauth");
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(http, "/api/platform/projects", html: false)).StatusCode);

        // Wrong state, then no permission, then the right one.
        Assert.Equal(HttpStatusCode.BadRequest, (await Get(http, "/__oauth/callback?code=ok&state=forged", html: true, pending)).StatusCode);
        var refused = await Get(http, $"/__oauth/callback?code=no&state={query["state"]}", html: true, pending);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Contains("No access", await refused.Content.ReadAsStringAsync());

        var done = await Get(http, $"/__oauth/callback?code=ok&state={query["state"]}", html: true, pending);
        Assert.Equal(HttpStatusCode.Redirect, done.StatusCode);
        Assert.Equal("/project/default?x=1", done.Headers.Location!.OriginalString);
        var session = Cookie(done, "studio_session");
        var page = await Get(http, "/api/platform/projects", html: false, session);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("studio:/api/platform/projects", await page.Content.ReadAsStringAsync());

        // A forged session cookie does not count.
        var forged = session[..^4] + "AAAA";
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get(http, "/api/platform/projects", html: false, forged)).StatusCode);
    }

    [Fact]
    public async Task The_studio_proxy_still_does_basic_auth_by_default()
    {
        if (!NodeAvailable()) return;

        var dir = Path.Combine(_root, "basic");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "proxy.js"), StudioBuilderExtensions.AuthProxyScriptSource());
        File.WriteAllText(Path.Combine(dir, "fake.js"), "require('http').createServer((q, s) => s.end('studio')).listen(process.env.PORT);");
        var port = FreePort();
        using var proxy = StartNode(Path.Combine(dir, "proxy.js"), dir, new Dictionary<string, string>
        {
            ["PORT"] = port.ToString(),
            ["STUDIO_SERVER_JS"] = "fake.js",
            ["STUDIO_APP_DIR"] = dir,
            ["STUDIO_PROXY_DELAY_MS"] = "100",
            ["DASHBOARD_USERNAME"] = "admin",
            ["DASHBOARD_PASSWORD"] = "pw",
        });
        using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        await WaitUntilListening(proxy);

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/")).StatusCode);
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:pw")));
        var ok = await http.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("studio", await ok.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> Get(HttpClient http, string path, bool html, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd(html ? "text/html" : "application/json");
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        return http.SendAsync(request);
    }

    /// <summary>The <c>name=value</c> part of the response's cookie <paramref name="name"/>.</summary>
    private static string Cookie(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]).Single(c => c.StartsWith(name + "=", StringComparison.Ordinal));

    private static bool NodeAvailable()
    {
        try
        {
            using var node = Process.Start(new ProcessStartInfo("node", "--version") { RedirectStandardOutput = true, UseShellExecute = false });
            return node is not null && node.WaitForExit(10_000) && node.ExitCode == 0;
        }
        catch { return false; }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class NodeProcess(Process process) : IDisposable
    {
        public readonly StringBuilder Output = new();
        public Process Process => process;

        public void Dispose()
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            process.Dispose();
        }
    }

    private static NodeProcess StartNode(string script, string dir, Dictionary<string, string> env)
    {
        var info = new ProcessStartInfo("node", $"\"{script}\"")
        {
            WorkingDirectory = dir, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var (key, value) in env) info.Environment[key] = value;
        var node = new NodeProcess(Process.Start(info)!);
        node.Process.OutputDataReceived += (_, e) => { lock (node.Output) node.Output.AppendLine(e.Data); };
        node.Process.ErrorDataReceived += (_, e) => { lock (node.Output) node.Output.AppendLine(e.Data); };
        node.Process.BeginOutputReadLine();
        node.Process.BeginErrorReadLine();
        return node;
    }

    /// <summary>The proxy says when it listens; connecting to a closed port takes seconds on Windows.</summary>
    private static async Task WaitUntilListening(NodeProcess node)
    {
        for (var i = 0; i < 150; i++)
        {
            lock (node.Output)
                if (node.Output.ToString().Contains("[Auth Proxy]", StringComparison.Ordinal)) return;
            if (node.Process.HasExited) break;
            await Task.Delay(100);
        }
        lock (node.Output) throw new TimeoutException($"the proxy did not start: {node.Output}");
    }
}
