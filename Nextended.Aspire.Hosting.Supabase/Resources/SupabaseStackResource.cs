using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Config;

namespace Nextended.Aspire.Hosting.Supabase.Resources;

/// <summary>
/// Represents a registered development user.
/// </summary>
public record RegisteredUser(string Email, string Password, string DisplayName);

/// <summary>
/// Represents a complete Supabase stack resource containing all sub-services.
/// This resource IS the Studio Dashboard container and serves as the visual parent
/// for all other Supabase containers in the Aspire dashboard.
/// </summary>
public sealed class SupabaseStackResource : ContainerResource, IResourceWithConnectionString, ISupabaseReferenceInfo
{
    /// <summary>
    /// Creates a new instance of the SupabaseStackResource.
    /// </summary>
    /// <param name="name">The name of the Supabase stack (will be the Studio container name).</param>
    public SupabaseStackResource(string name) : base(name)
    {
    }

    // --- Secrets auf Stack-Ebene ---

    /// <summary>
    /// Gets or sets the JWT secret used for token signing.
    /// </summary>
    public string JwtSecret { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the Anon Key for client-side authentication.
    /// </summary>
    public string AnonKey { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the Service Role Key for server-side authentication.
    /// </summary>
    public string ServiceRoleKey { get; internal set; } = string.Empty;

    // --- Typisierte Container Referenzen ---

    /// <summary>
    /// Gets the PostgreSQL database container resource.
    /// </summary>
    public IResourceBuilder<SupabaseDatabaseResource>? Database { get; internal set; }

    /// <summary>
    /// Gets the GoTrue authentication container resource.
    /// </summary>
    public IResourceBuilder<SupabaseAuthResource>? Auth { get; internal set; }

    /// <summary>
    /// Gets the PostgREST container resource.
    /// </summary>
    public IResourceBuilder<SupabaseRestResource>? Rest { get; internal set; }

    /// <summary>
    /// Gets the Storage API container resource.
    /// </summary>
    public IResourceBuilder<SupabaseStorageResource>? Storage { get; internal set; }

    /// <summary>
    /// Gets the Kong API Gateway container resource.
    /// </summary>
    public IResourceBuilder<SupabaseKongResource>? Kong { get; internal set; }

    /// <summary>
    /// Gets the Postgres-Meta container resource.
    /// </summary>
    public IResourceBuilder<SupabaseMetaResource>? Meta { get; internal set; }

    /// <summary>
    /// Gets the Realtime container resource.
    /// </summary>
    public IResourceBuilder<SupabaseRealtimeResource>? Realtime { get; internal set; }

    /// <summary>
    /// Gets the Edge Runtime container resource for Edge Functions.
    /// </summary>
    public IResourceBuilder<SupabaseEdgeRuntimeResource>? EdgeRuntime { get; internal set; }

    /// <summary>
    /// Gets the Init container resource (used in publish mode for post-init SQL).
    /// </summary>
    internal IResourceBuilder<ContainerResource>? InitContainer { get; set; }

    // --- Connection String ---

    /// <summary>
    /// Gets the connection string expression for the Supabase API (Kong endpoint URL).
    /// Uses Aspire's endpoint reference for proper service discovery in both local and deployed environments.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            if (Kong == null)
                throw new InvalidOperationException("Kong not configured. Ensure AddSupabase() has been called.");

            // Use the endpoint reference in a ReferenceExpression  
            var kongEndpoint = Kong.GetEndpoint("http");
            return ReferenceExpression.Create($"{kongEndpoint}");
        }
    }

    // --- Internal Configuration ---

    /// <summary>
    /// Reference to the distributed application builder for adding containers.
    /// </summary>
    internal IDistributedApplicationBuilder? AppBuilder { get; set; }

    /// <summary>
    /// Reference to the resource builder for this stack (used for chaining).
    /// </summary>
    internal IResourceBuilder<SupabaseStackResource>? StackBuilder { get; set; }

    /// <summary>
    /// Root directory for infrastructure files.
    /// </summary>
    internal string? InfraRootDir { get; set; }

    /// <summary>
    /// Path to the database initialization SQL scripts directory.
    /// </summary>
    internal string? InitSqlPath { get; set; }

    /// <summary>
    /// Path to the Edge Functions directory.
    /// </summary>
    internal string? EdgeFunctionsPath { get; set; }

    /// <summary>
    /// List of users to register on startup.
    /// </summary>
    internal List<RegisteredUser> RegisteredUsers { get; } = [];

    // --- Sync Configuration ---

    internal string? SyncFromProjectRef { get; set; }
    internal string? SyncServiceKey { get; set; }
    internal bool SyncSchema { get; set; } = true;
    internal bool SyncData { get; set; } = false;

    // --- Azure Publish Mode Configuration ---

    /// <summary>
    /// Base64-encoded init SQL for Azure deployment.
    /// </summary>
    internal string? InitSqlBase64 { get; set; }

    /// <summary>
    /// Base64-encoded Kong config YAML for Azure deployment.
    /// </summary>
    internal string? KongConfigBase64 { get; set; }

    /// <summary>
    /// Base64-encoded post-init SQL for Azure deployment.
    /// </summary>
    internal string? PostInitSqlBase64 { get; set; }

    /// <summary>
    /// Path to the scripts directory.
    /// </summary>
    internal string? ScriptsDir { get; set; }

    // --- Computed Properties ---

    public string ProjectRefId => SyncFromProjectRef ?? "";
    public string ServiceKey => AnonKey;

    /// <summary>
    /// Gets the Supabase API URL (Kong endpoint).
    /// For programmatic access, prefer using ConnectionStringExpression for dynamic resolution.
    /// </summary>
    public string GetApiUrl()
    {
        if (Kong == null)
            throw new InvalidOperationException("Kong not configured");
        
        // For local development display purposes - actual connection should use ConnectionStringExpression
        return Kong.Resource.ExternalPort > 0 
            ? $"http://localhost:{Kong.Resource.ExternalPort}" 
            : "http://<dynamically-assigned>";
    }

    /// <summary>
    /// Gets the Studio Dashboard URL (this resource IS the Studio).
    /// For programmatic access, prefer using the resource endpoint.
    /// </summary>
    public string GetStudioUrl()
    {
        if (StackBuilder == null)
            throw new InvalidOperationException("Stack not configured");
        
        // For local development display purposes
        return StudioPort > 0 
            ? $"http://localhost:{StudioPort}" 
            : "http://<dynamically-assigned>";
    }

    /// <summary>
    /// Gets the PostgreSQL connection string for external tools.
    /// </summary>
    public string GetPostgresConnectionString() =>
        Database != null
            ? $"Host=localhost;Port={Database.Resource.ExternalPort};Database=postgres;Username=postgres;Password={Database.Resource.Password}"
            : throw new InvalidOperationException("Database not configured");

    /// <summary>
    /// Gets or sets the external Studio port.
    /// </summary>
    internal int StudioPort { get; set; } = 54323;
}
