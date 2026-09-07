using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.AspireUI;

/// <summary>
/// The permission ids AspireUI knows, plus the presets the users page offers. Pass one of these (or
/// a comma-separated list of ids) wherever a <c>permissions</c> argument is asked for. An admin has
/// all of them regardless.
/// </summary>
public static class AspireUIPermissions
{
    /// <summary>Create, change and delete stacks; import code; run them locally.</summary>
    public const string Builder = "open-editor";

    /// <summary>Install, start, stop, update, undeploy, move and back up hosted apps.</summary>
    public const string Deploy = "deploy";

    /// <summary>Change a hosted app's environment, ports and domain.</summary>
    public const string Configure = "configure";

    /// <summary>Browse, view and download files in an app's volumes.</summary>
    public const string Files = "files";

    /// <summary>Delete files in an app's volumes.</summary>
    public const string FilesWrite = "files-write";

    /// <summary>Run commands inside an app's containers.</summary>
    public const string Terminal = "terminal";

    /// <summary>Add, change and remove deploy targets.</summary>
    public const string Targets = "targets";

    /// <summary>Manage store sources and which apps are hidden.</summary>
    public const string Store = "store";

    /// <summary>Global settings: proxy, notifications, backups, dashboard, import.</summary>
    public const string Settings = "settings";

    /// <summary>See and prune the docker host's images, containers and volumes.</summary>
    public const string Docker = "docker";

    /// <summary>Create users and grant permissions (never the admin flag).</summary>
    public const string Users = "users";

    /// <summary>Everything.</summary>
    public const string All = "all";

    /// <summary>Builder, install, configure, files (incl. delete) and terminal — no global settings.</summary>
    public const string Operator = "operator";

    /// <summary>Install, configure and browse files. No builder.</summary>
    public const string AppUser = "app-user";

    /// <summary>Look at the apps and their files. Change nothing.</summary>
    public const string Viewer = "viewer";

    /// <summary>Look only.</summary>
    public const string None = "none";

    /// <summary>What a new account gets when nothing is said: builder, install, configure.</summary>
    public const string Default = "default";
}

/// <summary>An account to create on first start. See <see cref="AspireUIPermissions"/> for the third argument.</summary>
/// <param name="Username">Login name.</param>
/// <param name="Password">Password (stored hashed; at least 8 characters).</param>
/// <param name="Permissions">A preset name or a comma-separated list of permission ids. Null = the default set.</param>
/// <param name="Admin">True makes an admin, and the permission list stops mattering.</param>
/// <param name="ViewModes">Which UIs this account may use: <c>full</c>, <c>simple</c>, or both.</param>
/// <param name="MustChangePassword">True asks for a new password at the first login.</param>
public sealed record AspireUIUser(string Username, string Password, string? Permissions = null,
    bool Admin = false, string[]? ViewModes = null, bool MustChangePassword = false);

/// <summary>
/// Everything the AspireUI container should find in its database on first start. Filled in by the
/// <c>With…</c> methods and handed over as one JSON environment variable when the app starts.
/// </summary>
public sealed class AspireUISeed
{
    /// <summary>Creates an empty seed. One is created for you by <c>AddAspireUI</c>.</summary>
    public AspireUISeed()
    {
    }

    internal List<UserSpec> Users { get; } = [];
    internal List<TargetSpec> Targets { get; } = [];
    internal List<TokenSpec> Tokens { get; } = [];
    internal List<AppSpec> Apps { get; } = [];
    internal List<SourceSpec> AppSources { get; } = [];
    internal List<StackSpec> Stacks { get; } = [];

    /// <summary>True when the seeded stacks and apps should also be deployed once hosting is up.</summary>
    public bool Deploy { get; internal set; }

    /// <summary>Names of the accounts this seed creates, in the order they were added.</summary>
    public IEnumerable<string> Usernames => Users.Select(u => u.Username);

    /// <summary>Names of the deploy targets this seed creates.</summary>
    public IEnumerable<string> TargetNames => Targets.Select(t => t.Name);

    /// <summary>Catalog ids of the store apps this seed installs.</summary>
    public IEnumerable<string> AppIds => Apps.Select(a => a.Id);

    internal bool IsEmpty => Users.Count == 0 && Targets.Count == 0 && Tokens.Count == 0
        && Apps.Count == 0 && AppSources.Count == 0 && Stacks.Count == 0 && !Deploy;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// The seed document as AspireUI reads it. Passwords and tokens that came from Aspire parameters
    /// are resolved here, which is why this is the last thing that happens before the container starts.
    /// </summary>
    internal async Task<string> ToJsonAsync(CancellationToken ct)
    {
        var users = new List<object>();
        foreach (var u in Users)
            users.Add(new
            {
                username = u.Username,
                password = await u.Password.ResolveAsync(ct).ConfigureAwait(false),
                admin = u.Admin,
                permissions = u.Admin ? null : Split(u.Permissions),
                viewModes = u.ViewModes,
                mustChangePassword = u.MustChangePassword,
            });

        var tokens = new List<object>();
        foreach (var t in Tokens)
            tokens.Add(new
            {
                name = t.Name,
                username = t.Username,
                token = await t.Token.ResolveAsync(ct).ConfigureAwait(false),
            });

        return JsonSerializer.Serialize(new
        {
            users,
            tokens,
            targets = Targets,
            apps = Apps,
            appSources = AppSources,
            stacks = Stacks,
            deploy = Deploy,
        }, Json);
    }

    // A preset name stays a single-element list on purpose: the server understands both.
    private static string[]? Split(string? permissions) =>
        string.IsNullOrWhiteSpace(permissions) ? null
        : permissions.Split([',', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    internal sealed record UserSpec(string Username, SeedValue Password, string? Permissions, bool Admin,
        string[]? ViewModes, bool MustChangePassword);

    internal sealed record TokenSpec(string Name, string Username, SeedValue Token);

    internal sealed record TargetSpec(string Name, string Kind)
    {
        public string? Host { get; init; }
        public int? Port { get; init; }
        public string? User { get; init; }
        public string? Key { get; init; }
        public string? Passphrase { get; init; }
        public string? HostKey { get; init; }
        public string? DockerHost { get; init; }
        public string? Ca { get; init; }
        public string? Cert { get; init; }
        public string? TlsKey { get; init; }
        public string? Kubeconfig { get; init; }
        public string? Context { get; init; }
        public string? Namespace { get; init; }
        public string? Expose { get; init; }
        public string? IngressHost { get; init; }
        public string? StorageClass { get; init; }
        public string? PublicHost { get; init; }
        public bool Default { get; init; }
        public string? Notes { get; init; }
    }

    internal sealed record AppSpec(string Id, string? Name, bool? Deploy);

    internal sealed record SourceSpec(string Name, string Url);

    internal sealed record StackSpec
    {
        public string? Name { get; init; }
        public string[]? Projects { get; init; }
        public string? Compose { get; init; }
        public string? Path { get; init; }
        public string? Git { get; init; }
        public string? Branch { get; init; }
        public string? Subdir { get; init; }
        public string? Mode { get; init; }
        public bool? Deploy { get; init; }
    }
}

/// <summary>A value that is either known now (a literal) or only when the app starts (an Aspire parameter).</summary>
internal readonly struct SeedValue(string? literal, ParameterResource? parameter)
{
    public static SeedValue From(string? value) => new(value, null);
    public static SeedValue From(ParameterResource parameter) => new(null, parameter);

    public async Task<string?> ResolveAsync(CancellationToken ct) =>
        parameter is null ? literal : await ((IValueProvider)parameter).GetValueAsync(ct).ConfigureAwait(false);
}
