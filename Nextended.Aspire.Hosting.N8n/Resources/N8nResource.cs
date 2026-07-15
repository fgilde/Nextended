using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.N8n.Resources;

/// <summary>
/// Represents an n8n workflow-automation container resource.
/// The resource exposes the n8n editor/REST endpoint as its connection string and
/// acts as the visual parent for all related containers (database, redis, workers)
/// in the Aspire dashboard.
/// </summary>
public sealed class N8nResource : ContainerResource, IResourceWithConnectionString
{
    /// <summary>The name of the primary HTTP endpoint exposed by n8n.</summary>
    public const string HttpEndpointName = "http";

    /// <summary>
    /// Creates a new instance of the <see cref="N8nResource"/>.
    /// </summary>
    /// <param name="name">The name of the n8n resource (shown in the Aspire dashboard).</param>
    public N8nResource(string name) : base(name)
    {
    }

    private EndpointReference? _httpReference;

    /// <summary>
    /// Gets the reference to the primary HTTP endpoint of the n8n editor/REST API.
    /// </summary>
    public EndpointReference HttpEndpoint => _httpReference ??= new(this, HttpEndpointName);

    /// <summary>
    /// Gets the connection string expression for n8n, which resolves to the editor/REST API URL.
    /// Works in both local development and Azure Container Apps deployments via Aspire's endpoint references.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{HttpEndpoint.Property(EndpointProperty.Url)}");

    // --- Security ---

    /// <summary>
    /// Gets the n8n encryption key used to encrypt stored credentials.
    /// MUST stay stable across restarts, otherwise existing credentials can no longer be decrypted.
    /// Used when no <see cref="EncryptionKeyParameter"/> is configured.
    /// </summary>
    public string EncryptionKey { get; internal set; } = "n8n-insecure-dev-encryption-key-change-me";

    /// <summary>
    /// Gets the Aspire parameter supplying the encryption key (takes precedence over <see cref="EncryptionKey"/>).
    /// When set, the secret flows through Aspire's parameter system (user secrets locally, Key Vault on deploy).
    /// </summary>
    internal ParameterResource? EncryptionKeyParameter { get; set; }

    /// <summary>Gets the basic-auth user (null = basic auth disabled).</summary>
    public string? BasicAuthUser { get; internal set; }

    /// <summary>Gets the basic-auth password.</summary>
    public string? BasicAuthPassword { get; internal set; }

    // --- URLs / Locale ---

    /// <summary>Gets the timezone used by n8n (e.g. "Europe/Berlin"). Default: UTC.</summary>
    public string Timezone { get; internal set; } = "UTC";

    /// <summary>Gets the public webhook base URL (used to build webhook URLs behind a proxy).</summary>
    public string? WebhookUrl { get; internal set; }

    /// <summary>Gets the public editor base URL.</summary>
    public string? EditorBaseUrl { get; internal set; }

    // --- Container configuration ---

    /// <summary>Gets the host port the n8n editor is exposed on for local development.</summary>
    public int HostPort { get; internal set; } = 5678;

    /// <summary>Gets the n8n container image (without tag).</summary>
    public string Image { get; internal set; } = "n8nio/n8n";

    /// <summary>Gets the n8n container image tag.</summary>
    public string ImageTag { get; internal set; } = "1.110.1";

    /// <summary>Gets the path of the bind mount used to persist the n8n data directory for local development.</summary>
    internal string? DataBindMountPath { get; set; }

    // --- Related resources ---

    /// <summary>Reference to the distributed application builder (used to add related containers).</summary>
    internal IDistributedApplicationBuilder? AppBuilder { get; set; }

    /// <summary>Reference to the resource builder for this resource (used for chaining).</summary>
    internal IResourceBuilder<N8nResource>? SelfBuilder { get; set; }

    /// <summary>True when n8n uses the bundled SQLite backend instead of PostgreSQL.</summary>
    public bool UsesSqlite { get; internal set; }

    /// <summary>
    /// True when the PostgreSQL backend was created automatically by <c>AddN8n</c>
    /// (and may therefore be removed again if the caller supplies an external database).
    /// </summary>
    internal bool OwnsDatabase { get; set; }

    /// <summary>Gets the PostgreSQL server resource created for n8n (null when using SQLite or an external database server).</summary>
    internal IResourceBuilder<PostgresServerResource>? PostgresServer { get; set; }

    /// <summary>Gets the PostgreSQL database resource n8n connects to.</summary>
    internal IResourceBuilder<PostgresDatabaseResource>? Database { get; set; }

    /// <summary>Gets the Redis container resource used for queue mode (null when queue mode is disabled).</summary>
    internal IResourceBuilder<ContainerResource>? Redis { get; set; }

    /// <summary>Gets the password protecting the queue-mode Redis instance (used when no <see cref="RedisPasswordParameter"/> is set).</summary>
    internal string? RedisPassword { get; set; }

    /// <summary>Gets the Aspire parameter supplying the Redis password (takes precedence over <see cref="RedisPassword"/>).</summary>
    internal ParameterResource? RedisPasswordParameter { get; set; }

    /// <summary>Gets the effective Redis password value (parameter or string) for use as an environment/argument value.</summary>
    internal object? RedisPasswordValue =>
        (object?)RedisPasswordParameter ?? (string.IsNullOrEmpty(RedisPassword) ? null : RedisPassword);

    /// <summary>Gets the effective encryption key value (parameter or string) for use as an environment value.</summary>
    internal object EncryptionKeyValue => (object?)EncryptionKeyParameter ?? EncryptionKey;

    /// <summary>Gets the n8n worker container resources (queue mode).</summary>
    internal List<IResourceBuilder<N8nWorkerResource>> Workers { get; } = [];

    /// <summary>True when queue mode (Redis + workers) is enabled.</summary>
    public bool QueueModeEnabled { get; internal set; }

    // --- Import (local development) ---

    /// <summary>Host path to the (managed) staging directory of workflow JSON files imported on startup.</summary>
    internal string? ImportWorkflowsPath { get; set; }

    /// <summary>Host path to a directory of credential JSON files imported on startup.</summary>
    internal string? ImportCredentialsPath { get; set; }

    /// <summary>The one-shot init container that imports workflows/credentials before n8n starts.</summary>
    internal IResourceBuilder<ContainerResource>? ImportContainer { get; set; }

    /// <summary>Number of workflows seeded so far (used to generate unique staging file names).</summary>
    internal int SeededWorkflowCount { get; set; }

    /// <summary>True once the workflow staging directory has been cleared for this run.</summary>
    internal bool WorkflowStagingCleared { get; set; }

    /// <summary>True once the workflow staging directory has been bind-mounted into the import container.</summary>
    internal bool WorkflowsMountAdded { get; set; }

    /// <summary>True once the credentials directory has been bind-mounted into the import container.</summary>
    internal bool CredentialsMountAdded { get; set; }

    /// <summary>True once <c>WithOwner</c> registered its seeding callback (guards double subscription).</summary>
    internal bool OwnerConfigured { get; set; }
}
