using System.Text;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Turns an Aspire resource name into the suffix of a <c>WDS_CONN_&lt;NAME&gt;</c> variable, which
/// is also the label the studio shows for that connection.
/// </summary>
internal static class WebDataStudioNaming
{
    /// <summary>
    /// WebDataStudio reads these endings as settings for another connection, so a connection may
    /// not be called anything that ends in one of them.
    /// </summary>
    internal static readonly string[] ReservedSuffixes = ["_ENGINE", "_READONLY", "_COLOR", "_GROUP"];

    /// <summary>
    /// Uppercases and replaces everything an environment variable name cannot carry. A resource
    /// called <c>shop-db</c> becomes <c>SHOP_DB</c>.
    /// </summary>
    internal static string ToVariableSuffix(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var builder = new StringBuilder(resourceName.Length);
        foreach (var character in resourceName)
            builder.Append(char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : '_');

        var name = builder.ToString().Trim('_');

        if (name.Length == 0)
            throw new ArgumentException(
                $"'{resourceName}' has no letters or digits to build a connection name from; " +
                "pass connectionName explicitly.", nameof(resourceName));

        // A digit first would still be a legal env var on Linux, but not everywhere; prefix it.
        if (char.IsDigit(name[0])) name = "C" + name;

        foreach (var suffix in ReservedSuffixes)
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                throw new ArgumentException(
                    $"a connection may not be called '{name}': WebDataStudio reads '{suffix}' as a " +
                    $"setting for a connection called '{name[..^suffix.Length]}'. Choose another " +
                    "name with the connectionName argument.",
                    nameof(resourceName));

        return name;
    }
}
