using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Provides extension methods for configuring the Supabase Auth (GoTrue).
/// </summary>
public static class AuthBuilderExtensions
{
    #region Direct Stack Methods (Aspire-Standard Pattern)

    /// <summary>
    /// Sets the site URL for authentication redirects.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="url">The site URL for redirects.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithAuthSiteUrl(
        this IResourceBuilder<SupabaseStackResource> builder,
        string url)
    {
        var stack = builder.Resource;
        if (stack.Auth is null)
            throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

        stack.Auth.Resource.SiteUrl = url;
        stack.Auth.WithEnvironment("GOTRUE_SITE_URL", url);
        return builder;
    }

    /// <summary>
    /// Sets the site URL for authentication redirects from a frontend resource endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the frontend resource.</typeparam>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="frontendResource">The frontend resource to use for the site URL.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithAuthSiteUrl<T>(
        this IResourceBuilder<SupabaseStackResource> builder,
        IResourceBuilder<T> frontendResource) where T : IResourceWithEndpoints
    {
        var stack = builder.Resource;
        if (stack.Auth is null)
            throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

        stack.Auth.WithFrontendSiteUrl(frontendResource);
        return builder;
    }

    /// <summary>
    /// Enables or disables auto-confirmation of email addresses.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="enabled">Whether auto-confirm is enabled.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithAuthAutoConfirm(
        this IResourceBuilder<SupabaseStackResource> builder,
        bool enabled = true)
    {
        var stack = builder.Resource;
        if (stack.Auth is null)
            throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

        stack.Auth.Resource.AutoConfirm = enabled;
        stack.Auth.WithEnvironment("GOTRUE_MAILER_AUTOCONFIRM", enabled ? "true" : "false");
        return builder;
    }

    /// <summary>
    /// Enables or disables user signup.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="disabled">Whether signup is disabled.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithAuthDisableSignup(
        this IResourceBuilder<SupabaseStackResource> builder,
        bool disabled = true)
    {
        var stack = builder.Resource;
        if (stack.Auth is null)
            throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

        stack.Auth.Resource.DisableSignup = disabled;
        stack.Auth.WithEnvironment("GOTRUE_DISABLE_SIGNUP", disabled ? "true" : "false");
        return builder;
    }

    /// <summary>
    /// Enables or disables anonymous users.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="enabled">Whether anonymous users are enabled.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithAuthAnonymousUsers(
        this IResourceBuilder<SupabaseStackResource> builder,
        bool enabled = true)
    {
        var stack = builder.Resource;
        if (stack.Auth is null)
            throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

        stack.Auth.Resource.AnonymousUsersEnabled = enabled;
        stack.Auth.WithEnvironment("GOTRUE_ANONYMOUS_USERS_ENABLED", enabled ? "true" : "false");
        return builder;
    }

    /// <summary>
    /// Sets the JWT expiration time in seconds.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="seconds">The expiration time in seconds.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> WithAuthJwtExpiration(
        this IResourceBuilder<SupabaseStackResource> builder,
        int seconds)
    {
        var stack = builder.Resource;
        if (stack.Auth is null)
            throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

        stack.Auth.Resource.JwtExpiration = seconds;
        stack.Auth.WithEnvironment("GOTRUE_JWT_EXP", seconds.ToString());
        return builder;
    }

    #endregion

    #region Legacy ConfigureAuth (Obsolete)

    /// <summary>
    /// Configures the GoTrue authentication settings.
    /// </summary>
    /// <param name="builder">The Supabase stack resource builder.</param>
    /// <param name="configure">Configuration action for the auth resource builder.</param>
    /// <returns>The Supabase stack resource builder for chaining.</returns>
    public static IResourceBuilder<SupabaseStackResource> ConfigureAuth(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<IResourceBuilder<SupabaseAuthResource>> configure)
    {
        var stack = builder.Resource;
        if (stack.Auth is null)
            throw new InvalidOperationException("Auth not configured. Ensure AddSupabase() has been called.");

        configure(stack.Auth);
        return builder;
    }

    #endregion

    #region Sub-Resource Methods (for use with ConfigureAuth)

    /// <summary>
    /// Sets the site URL for authentication redirects.
    /// </summary>
    public static IResourceBuilder<SupabaseAuthResource> WithSiteUrl(
        this IResourceBuilder<SupabaseAuthResource> builder,
        string url)
    {
        builder.Resource.SiteUrl = url;
        builder.WithEnvironment("GOTRUE_SITE_URL", url);
        return builder;
    }
    
    /// <summary>
    /// Sets the site URL for authentication redirects from a frontend resource endpoint.
    /// This ensures the URL is dynamically resolved and works in both local and deployed environments.
    /// </summary>
    public static IResourceBuilder<SupabaseAuthResource> WithSiteUrl<T>(
        this IResourceBuilder<SupabaseAuthResource> builder,
        IResourceBuilder<T> frontendResource) where T : IResourceWithEndpoints
    {
        return builder.WithFrontendSiteUrl(frontendResource);
    }

    /// <summary>
    /// The frontend's address as site URL — the one a browser has: deployed the public endpoint,
    /// locally the address on the host. (Locally the reference resolved to the container-network
    /// name, e.g. aspire.dev.internal:60349, and every link and sign-in redirect built from the site
    /// URL — mails, the OAuth consent page — pointed at an address no browser reaches.)
    /// </summary>
    private static IResourceBuilder<SupabaseAuthResource> WithFrontendSiteUrl<T>(
        this IResourceBuilder<SupabaseAuthResource> auth, IResourceBuilder<T> frontendResource) where T : IResourceWithEndpoints
    {
        var frontendEndpoint = frontendResource.GetEndpoint("http");
        if (auth.ApplicationBuilder.ExecutionContext.IsPublishMode)
            return auth.WithEnvironment("GOTRUE_SITE_URL", frontendEndpoint);
        return auth.WithEnvironment(context => context.EnvironmentVariables["GOTRUE_SITE_URL"] = frontendEndpoint.Url);
    }

    /// <summary>
    /// Enables or disables auto-confirmation of email addresses.
    /// </summary>
    public static IResourceBuilder<SupabaseAuthResource> WithAutoConfirm(
        this IResourceBuilder<SupabaseAuthResource> builder,
        bool enabled = true)
    {
        builder.Resource.AutoConfirm = enabled;
        builder.WithEnvironment("GOTRUE_MAILER_AUTOCONFIRM", enabled ? "true" : "false");
        return builder;
    }

    /// <summary>
    /// Enables or disables user signup.
    /// </summary>
    public static IResourceBuilder<SupabaseAuthResource> WithDisableSignup(
        this IResourceBuilder<SupabaseAuthResource> builder,
        bool disabled = true)
    {
        builder.Resource.DisableSignup = disabled;
        builder.WithEnvironment("GOTRUE_DISABLE_SIGNUP", disabled ? "true" : "false");
        return builder;
    }

    /// <summary>
    /// Enables or disables anonymous users.
    /// </summary>
    public static IResourceBuilder<SupabaseAuthResource> WithAnonymousUsers(
        this IResourceBuilder<SupabaseAuthResource> builder,
        bool enabled = true)
    {
        builder.Resource.AnonymousUsersEnabled = enabled;
        builder.WithEnvironment("GOTRUE_ANONYMOUS_USERS_ENABLED", enabled ? "true" : "false");
        return builder;
    }

    /// <summary>
    /// Sets the JWT expiration time in seconds.
    /// </summary>
    public static IResourceBuilder<SupabaseAuthResource> WithJwtExpiration(
        this IResourceBuilder<SupabaseAuthResource> builder,
        int seconds)
    {
        builder.Resource.JwtExpiration = seconds;
        builder.WithEnvironment("GOTRUE_JWT_EXP", seconds.ToString());
        return builder;
    }

    #endregion
}
