namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// One account of a WebDataStudio instance: who signs in, what they may do, and which connections
/// they see. The password is deliberately not part of this — it goes to the container and nowhere
/// else, so nothing can print it by accident.
/// </summary>
/// <param name="Name">The login name.</param>
/// <param name="Role">
/// <c>admin</c> (everything, including the administration panel), <c>editor</c> (read and write) or
/// <c>viewer</c> (every connection read-only).
/// </param>
/// <param name="Connections">
/// The connections this account may see, by name. Empty means all of them.
/// </param>
public sealed record StudioAccount(string Name, string Role, IReadOnlyList<string> Connections);

/// <summary>The roles a <see cref="StudioAccount"/> can have.</summary>
public static class StudioRoles
{
    /// <summary>Everything, including the administration panel.</summary>
    public const string Admin = "admin";

    /// <summary>Read and write, but no administration.</summary>
    public const string Editor = "editor";

    /// <summary>Every connection read-only.</summary>
    public const string Viewer = "viewer";

    internal static readonly string[] All = [Admin, Editor, Viewer];
}
