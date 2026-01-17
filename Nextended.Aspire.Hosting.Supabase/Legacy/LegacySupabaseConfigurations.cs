using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Legacy;

/// <summary>
/// Legacy configuration classes for backward compatibility.
/// These classes delegate to the new typed resource builders.
/// </summary>

#region Configuration Classes (Legacy - delegate to new Resource properties)

/// <summary>
/// Configuration for the PostgreSQL database container.
/// </summary>
[Obsolete("Use ConfigureDatabase with typed resource builder instead: .ConfigureDatabase(db => db.WithPassword(...).WithPort(...))")]
public class DatabaseConfiguration
{
    internal string Password { get; set; } = "postgres-insecure-dev-password";
    internal int Port { get; set; } = 54322;
    internal string Image { get; set; } = "supabase/postgres";
    internal string ImageTag { get; set; } = "15.1.1.78";

    public DatabaseConfiguration WithPassword(string password) { Password = password; return this; }
    public DatabaseConfiguration WithPort(int port) { Port = port; return this; }
    public DatabaseConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

/// <summary>
/// Configuration for the GoTrue authentication container.
/// </summary>
[Obsolete("Use ConfigureAuth with typed resource builder instead: .ConfigureAuth(auth => auth.WithAutoConfirm(...))")]
public class AuthConfiguration
{
    internal bool AutoConfirm { get; set; } = true;
    internal bool DisableSignup { get; set; } = false;
    internal bool AnonymousUsersEnabled { get; set; } = true;
    internal int JwtExpSeconds { get; set; } = 3600;
    internal string SiteUrl { get; set; } = "http://localhost:3000";
    internal string Image { get; set; } = "supabase/gotrue";
    internal string ImageTag { get; set; } = "v2.185.0";

    public AuthConfiguration WithAutoConfirm(bool enabled) { AutoConfirm = enabled; return this; }
    public AuthConfiguration WithDisableSignup(bool disabled) { DisableSignup = disabled; return this; }
    public AuthConfiguration WithAnonymousUsers(bool enabled) { AnonymousUsersEnabled = enabled; return this; }
    public AuthConfiguration WithJwtExpiration(int seconds) { JwtExpSeconds = seconds; return this; }
    public AuthConfiguration WithSiteUrl(string url) { SiteUrl = url; return this; }
    public AuthConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

/// <summary>
/// Configuration for the PostgREST container.
/// </summary>
[Obsolete("Use ConfigureRest with typed resource builder instead")]
public class RestConfiguration
{
    internal string[] Schemas { get; set; } = ["public", "storage", "graphql_public"];
    internal string AnonRole { get; set; } = "anon";
    internal string Image { get; set; } = "postgrest/postgrest";
    internal string ImageTag { get; set; } = "v12.2.0";

    public RestConfiguration WithSchemas(params string[] schemas) { Schemas = schemas; return this; }
    public RestConfiguration WithAnonRole(string role) { AnonRole = role; return this; }
    public RestConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

/// <summary>
/// Configuration for the Storage API container.
/// </summary>
[Obsolete("Use ConfigureStorage with typed resource builder instead")]
public class StorageConfiguration
{
    internal long FileSizeLimit { get; set; } = 52428800;
    internal string Backend { get; set; } = "file";
    internal bool EnableImageTransformation { get; set; } = true;
    internal string Image { get; set; } = "supabase/storage-api";
    internal string ImageTag { get; set; } = "v1.11.13";

    public StorageConfiguration WithFileSizeLimit(long bytes) { FileSizeLimit = bytes; return this; }
    public StorageConfiguration WithBackend(string backend) { Backend = backend; return this; }
    public StorageConfiguration WithImageTransformation(bool enabled) { EnableImageTransformation = enabled; return this; }
    public StorageConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

/// <summary>
/// Configuration for the Kong API Gateway container.
/// </summary>
[Obsolete("Use ConfigureKong with typed resource builder instead")]
public class KongConfiguration
{
    internal int Port { get; set; } = 8000;
    internal string[] Plugins { get; set; } = ["request-transformer", "cors", "key-auth", "acl", "basic-auth"];
    internal string Image { get; set; } = "kong";
    internal string ImageTag { get; set; } = "2.8.1";

    public KongConfiguration WithPort(int port) { Port = port; return this; }
    public KongConfiguration WithPlugins(params string[] plugins) { Plugins = plugins; return this; }
    public KongConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

/// <summary>
/// Configuration for the Postgres-Meta container.
/// </summary>
[Obsolete("Use ConfigureMeta with typed resource builder instead")]
public class MetaConfiguration
{
    internal int Port { get; set; } = 8080;
    internal string Image { get; set; } = "supabase/postgres-meta";
    internal string ImageTag { get; set; } = "v0.84.2";

    public MetaConfiguration WithPort(int port) { Port = port; return this; }
    public MetaConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

/// <summary>
/// Configuration for the Studio Dashboard container.
/// </summary>
[Obsolete("Use ConfigureStudio with typed resource builder instead")]
public class StudioConfiguration
{
    internal int Port { get; set; } = 54323;
    internal string OrganizationName { get; set; } = "Default Organization";
    internal string ProjectName { get; set; } = "Default Project";
    internal string Image { get; set; } = "supabase/studio";
    internal string ImageTag { get; set; } = "latest";

    public StudioConfiguration WithPort(int port) { Port = port; return this; }
    public StudioConfiguration WithOrganizationName(string name) { OrganizationName = name; return this; }
    public StudioConfiguration WithProjectName(string name) { ProjectName = name; return this; }
    public StudioConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

/// <summary>
/// Configuration for the Edge Runtime container.
/// </summary>
[Obsolete("Use ConfigureEdgeRuntime with typed resource builder instead")]
public class EdgeRuntimeConfiguration
{
    internal int Port { get; set; } = 9000;
    internal string Image { get; set; } = "supabase/edge-runtime";
    internal string ImageTag { get; set; } = "v1.67.4";

    public EdgeRuntimeConfiguration WithPort(int port) { Port = port; return this; }
    public EdgeRuntimeConfiguration WithImage(string image, string tag) { Image = image; ImageTag = tag; return this; }
}

#endregion

#region Legacy Extension Methods

/// <summary>
/// Legacy extension methods for backward compatibility.
/// </summary>
[Obsolete("Use the new typed Configure* methods instead")]
public static class LegacySupabaseExtensions
{
    /// <summary>
    /// Configures the PostgreSQL password for the Supabase database.
    /// </summary>
    [Obsolete("Use ConfigureDatabase(db => db.WithPassword(...)) instead")]
    public static IResourceBuilder<SupabaseStackResource> WithPostgresPassword(
        this IResourceBuilder<SupabaseStackResource> builder,
        string password)
    {
        return builder.ConfigureDatabase(db => db.WithPassword(password));
    }

    /// <summary>
    /// Configures the Kong API Gateway HTTP port.
    /// </summary>
    [Obsolete("Use ConfigureKong(kong => kong.WithPort(...)) instead")]
    public static IResourceBuilder<SupabaseStackResource> WithKongPort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        return builder.ConfigureKong(kong => kong.WithPort(port));
    }

    /// <summary>
    /// Configures the PostgreSQL external port.
    /// </summary>
    [Obsolete("Use ConfigureDatabase(db => db.WithPort(...)) instead")]
    public static IResourceBuilder<SupabaseStackResource> WithPostgresPort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        return builder.ConfigureDatabase(db => db.WithPort(port));
    }

    /// <summary>
    /// Configures the Studio Dashboard port.
    /// </summary>
    [Obsolete("Use ConfigureStudio(studio => studio.WithStudioPort(...)) instead")]
    public static IResourceBuilder<SupabaseStackResource> WithStudioPort(
        this IResourceBuilder<SupabaseStackResource> builder,
        int port)
    {
        return builder.ConfigureStudio(studio => studio.WithStudioPort(port));
    }
}

#endregion
