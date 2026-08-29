using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// WebDataStudio — a browser-based database studio — running as a container resource. Exposes an
/// HTTP endpoint for the studio, and carries the connections that were attached to it so several
/// databases can share one instance.
/// </summary>
public sealed class WebDataStudioResource(string name) : ContainerResource(name)
{
    /// <summary>The published WebDataStudio image.</summary>
    public const string DefaultImage = "ghcr.io/fgilde/webdatastudio";

    /// <summary>Default image tag.</summary>
    public const string DefaultTag = "latest";

    /// <summary>Port the studio listens on inside the container.</summary>
    public const int DefaultTargetPort = 8080;

    /// <summary>Name of the HTTP endpoint serving the studio.</summary>
    public const string HttpEndpointName = "http";

    /// <summary>Resource name used when nothing else is asked for, and the key for sharing one studio.</summary>
    public const string DefaultResourceName = "webdatastudio";

    /// <summary>The HTTP endpoint serving the studio.</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>Login name of the first account, when one was configured. Null means anonymous access.</summary>
    public string? Username { get; internal set; }

    /// <summary>
    /// The accounts configured with <c>WithLogin</c> and <c>WithUser</c>, in the order they were
    /// added. Passwords are not here: they go to the container and nowhere else.
    /// </summary>
    public IReadOnlyList<StudioAccount> Accounts => [.. _accounts.Select(a => a.Account)];

    private readonly List<(StudioAccount Account, object Secret)> _accounts = [];

    internal IReadOnlyList<(StudioAccount Account, object Secret)> AccountsWithSecrets => _accounts;

    /// True the first time, so the environment callback is registered exactly once.
    internal bool ClaimEnvironmentHook()
    {
        if (_environmentHooked) return false;
        _environmentHooked = true;
        return true;
    }

    private bool _environmentHooked;

    internal void AddAccount(StudioAccount account, object secret)
    {
        // Adding the same name twice means "I changed my mind about that account", not two accounts
        // that shadow each other.
        _accounts.RemoveAll(existing =>
            existing.Account.Name.Equals(account.Name, StringComparison.OrdinalIgnoreCase));

        _accounts.Add((account, secret));
        Username ??= account.Name;
    }

    /// <summary>
    /// The identity provider people sign in through, when one was configured with
    /// <c>WithSingleSignOn</c>. Null means the studio signs people in itself, or not at all.
    /// </summary>
    public string? SignInAuthority { get; internal set; }

    /// <summary>
    /// How many days the studio keeps its record of who did what. Null is the studio's own default
    /// of 90; zero means <c>WithoutAuditTrail</c> turned it off.
    /// </summary>
    public int? AuditDays { get; internal set; }

    /// <summary>
    /// The model the optional assistance uses, when it was configured with <c>WithAssistant</c>.
    /// Null means the studio has no assistance at all: no button, no calls.
    /// </summary>
    public string? AssistantModel { get; internal set; }

    /// <summary>
    /// Path the studio serves MCP on, when <c>WithMcpEndpoint</c> was called. Null means the studio
    /// is not an MCP server.
    /// </summary>
    public string? McpPath { get; internal set; }

    /// <summary>Whether the MCP endpoint may change data, through a preview and its hash.</summary>
    public bool McpAllowsWrite { get; internal set; }

    /// <summary>
    /// Whether the MCP endpoint was given a key. A studio with accounts refuses to serve MCP
    /// without one, so this is what the app host warns about.
    /// </summary>
    public bool McpHasKey { get; internal set; }

    /// <summary>Columns masked on top of the studio's own word list, from <c>WithMaskedColumns</c>.</summary>
    public IReadOnlyCollection<string> MaskedColumns => MaskedColumnList;

    /// <summary>Columns the studio leaves alone, from <c>WithUnmaskedColumns</c>.</summary>
    public IReadOnlyCollection<string> UnmaskedColumns => UnmaskedColumnList;

    /// <summary>
    /// The name the studio reports as in traces and metrics, from <c>WithOpenTelemetry</c>. Null
    /// when it reports nothing.
    /// </summary>
    public string? TelemetryServiceName { get; internal set; }

    /// <summary>Whether results can be shared as links, from <c>WithSharedResults</c>.</summary>
    public bool SharingEnabled { get; internal set; }

    /// <summary>Whether such a link opens without signing in.</summary>
    public bool SharingIsPublic { get; internal set; }

    /// <summary>The scheduled queries, from <c>WithScheduledQueries</c>.</summary>
    public IReadOnlyList<ScheduledStudioQuery> Schedule => ScheduleList;

    internal List<ScheduledStudioQuery> ScheduleList { get; } = [];

    /// <summary>Folder of <c>.sql</c> files imported as saved queries, from <c>WithSavedQueriesFromDirectory</c>.</summary>
    /// The files this app host wrote itself, per folder in the container: name to content. Kept so
    /// a second call adds to the first rather than replacing what it wrote.
    internal Dictionary<string, Dictionary<string, string>> InlineFiles { get; } =
        new(StringComparer.Ordinal);

    /// The paths each path-taking setting names, in the order they were added. The studio reads
    /// these settings as a list, so a repository folder and an inline one both count.
    internal Dictionary<string, string[]> PathSettings { get; } = new(StringComparer.Ordinal);

    public string? SavedQueriesPath { get; internal set; }

    /// <summary>Seed script or folder, from <c>WithSeedScript</c>.</summary>
    public string? SeedScriptPath { get; internal set; }

    /// <summary>
    /// Where schema snapshots are written, from <c>WithSchemaSnapshots</c>. Null means none are.
    /// </summary>
    public string? SchemaSnapshotPath { get; internal set; }

    /// <summary>
    /// Where kept results are written, from <c>WithArchives</c>. Null means the default beside the
    /// application database.
    /// </summary>
    public string? ArchivePath { get; internal set; }

    /// <summary>The tools the MCP endpoint is narrowed to, from <c>WithMcpTools</c>. Empty means all.</summary>
    public IReadOnlyCollection<string> McpTools => McpToolList;

    internal SortedSet<string> McpToolList { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal SortedSet<string> MaskedColumnList { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal SortedSet<string> UnmaskedColumnList { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// True once the app-host warning about a missing MCP key has been subscribed.
    internal bool WarnedAboutMcpKey { get; set; }

    /// <summary>
    /// The name the studio shows in its header and browser tab. Defaults to the resource name, so
    /// three studios in one stack are told apart at a glance; <c>WithTitle</c> overrides it and
    /// <c>WithTitle(null)</c> leaves the studio unnamed.
    /// </summary>
    public string? Title { get; internal set; }

    /// <summary>
    /// The theme the studio starts in, as the studio's own id (<c>ocean</c>, <c>aspire</c>, …).
    /// Null leaves the studio's default. A person who picks another theme keeps their choice.
    /// </summary>
    public string? Theme { get; internal set; }

    /// <summary>
    /// Names of the connections attached to this studio, in the order they were added. These are
    /// the labels the studio shows in its explorer, and the suffixes of its <c>WDS_CONN_*</c>
    /// variables.
    /// </summary>
    public IReadOnlyList<string> ConnectionNames => _connectionNames;

    private readonly List<string> _connectionNames = [];

    internal void TrackConnection(string connectionName) => _connectionNames.Add(connectionName);

    internal bool HasConnection(string connectionName) =>
        _connectionNames.Contains(connectionName, StringComparer.OrdinalIgnoreCase);
}
