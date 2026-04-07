using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Studio Dashboard.
/// Note: The SupabaseStackResource IS the Studio container, so these methods configure the stack directly.
/// </summary>
public static class StudioBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    // Note: WithStudioPort, WithOrganizationName, WithProjectName are already direct methods
    // on the stack, so they follow the Aspire standard pattern.

    #endregion

    #region Legacy ConfigureStudio (Obsolete)

    /// <summary>
    /// Configures the Studio Dashboard settings.
    /// Since the SupabaseStackResource IS the Studio container, this configures the stack itself.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the Studio (stack) resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureStudio(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseStackResource>> configure)
    {
        configure(builder);
        return builder;
    }

    #endregion

    #region Studio Configuration Methods

    /// <summary>
    /// Sets the external Studio port.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithStudioPort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        builder.Resource.StudioPort = port;
        return builder;
    }

    /// <summary>
    /// Sets the organization name displayed in Studio.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithOrganizationName(
        this IResourceBuilder<SupabaseStackResource> builder,
        string name)
    {
        builder.WithEnvironment("DEFAULT_ORGANIZATION_NAME", name);
        return builder;
    }

    /// <summary>
    /// Sets the project name displayed in Studio.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithProjectName(
        this IResourceBuilder<SupabaseStackResource> builder,
        string name)
    {
        builder.WithEnvironment("DEFAULT_PROJECT_NAME", name);
        return builder;
    }


    public static IResourceBuilder<SupabaseStackResource> WithOpenAIIntegration(
        this IResourceBuilder<SupabaseStackResource> builder,
        string openAIApiKey)
    {
        builder.WithEnvironment("OPENAI_API_KEY ", openAIApiKey);
        return builder;
    }


    public static IResourceBuilder<SupabaseStackResource> WithOpenAIIntegration(
        this IResourceBuilder<SupabaseStackResource> builder,
        IResourceBuilder<ParameterResource> openAIApiKey)
    {
        builder.WithEnvironment("OPENAI_API_KEY", openAIApiKey);
        return builder;
    }

    /// <summary>
    /// Enables login protection for Supabase Studio Dashboard with username and password strings.
    /// When enabled, users must authenticate via HTTP Basic Auth before accessing Studio.
    /// Works both locally and in deployed environments.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithLogin(
        this IResourceBuilder<SupabaseStackResource> builder,
        string username,
        string password)
    {
        builder.Resource.DashboardUsername = username;
        builder.Resource.DashboardPassword = password;
        builder.WithEnvironment("DASHBOARD_USERNAME", username);
        builder.WithEnvironment("DASHBOARD_PASSWORD", password);
        return ApplyAuthProxy(builder);
    }

    /// <summary>
    /// Enables login protection for Supabase Studio Dashboard with Aspire parameters.
    /// Useful for keeping credentials in user secrets or configuration.
    /// Works both locally and in deployed environments.
    /// </summary>
    public static IResourceBuilder<SupabaseStackResource> WithLogin(
        this IResourceBuilder<SupabaseStackResource> builder,
        IResourceBuilder<ParameterResource> username,
        IResourceBuilder<ParameterResource> password)
    {
        builder.Resource.DashboardUsername = "param";
        builder.Resource.DashboardPassword = "param";
        builder.WithEnvironment("DASHBOARD_USERNAME", username);
        builder.WithEnvironment("DASHBOARD_PASSWORD", password);
        return ApplyAuthProxy(builder);
    }

    /// <summary>
    /// Injects a lightweight Node.js Basic Auth reverse proxy in front of Studio.
    /// The proxy reads DASHBOARD_USERNAME/DASHBOARD_PASSWORD from env vars at runtime,
    /// starts Studio on PORT+1, and proxies authenticated requests to it.
    /// </summary>
    private static IResourceBuilder<SupabaseStackResource> ApplyAuthProxy(
        IResourceBuilder<SupabaseStackResource> builder)
    {
        const string proxyScript = """
            const http = require('http');
            const {spawn} = require('child_process');
            const user = process.env.DASHBOARD_USERNAME || 'admin';
            const pass = process.env.DASHBOARD_PASSWORD || 'admin';
            const PORT = parseInt(process.env.PORT || '3000');
            const STUDIO_PORT = PORT + 1;

            // Start Studio on STUDIO_PORT
            process.env.PORT = String(STUDIO_PORT);
            process.env.HOSTNAME = '0.0.0.0';
            const studio = spawn('node', ['apps/studio/server.js'], {
                cwd: '/app', stdio: 'inherit', env: {...process.env}
            });

            // Wait for Studio to start, then launch auth proxy
            setTimeout(() => {
                const proxy = http.createServer((req, res) => {
                    const auth = req.headers.authorization;
                    if (!auth || !auth.startsWith('Basic ')) {
                        res.writeHead(401, {'WWW-Authenticate': 'Basic realm="Supabase Studio"'});
                        return res.end('Unauthorized');
                    }
                    const [u, p] = Buffer.from(auth.slice(6), 'base64').toString().split(':');
                    if (u !== user || p !== pass) {
                        res.writeHead(401, {'WWW-Authenticate': 'Basic realm="Supabase Studio"'});
                        return res.end('Invalid credentials');
                    }
                    const opts = {hostname: '127.0.0.1', port: STUDIO_PORT, path: req.url, method: req.method, headers: {...req.headers}};
                    delete opts.headers.authorization;
                    const pReq = http.request(opts, pRes => {
                        res.writeHead(pRes.statusCode, pRes.headers);
                        pRes.pipe(res);
                    });
                    pReq.on('error', () => { res.writeHead(502); res.end('Studio not ready'); });
                    req.pipe(pReq);
                });
                proxy.listen(PORT, '0.0.0.0', () =>
                    console.log(`[Auth Proxy] Listening on ${PORT}, forwarding to Studio on ${STUDIO_PORT}`)
                );
            }, 3000);

            studio.on('exit', code => process.exit(code || 0));
            """;

        var proxyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(proxyScript));
        builder.WithEnvironment("AUTH_PROXY_SCRIPT_BASE64", proxyBase64);
        builder.WithEntrypoint("/bin/sh");
        builder.WithArgs("-c",
            "echo \"$AUTH_PROXY_SCRIPT_BASE64\" | base64 -d > /tmp/auth-proxy.js && " +
            "exec node /tmp/auth-proxy.js");

        return builder;
    }

    #endregion
}
