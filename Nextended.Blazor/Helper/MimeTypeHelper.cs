using System.Text.RegularExpressions;

namespace Nextended.Blazor.Helper;

public static class MimeTypeHelper
{

    public static bool IsZip(string contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType) && Matches(contentType, "application/zip*", "application/x-zip*");
    }

    /**
     * Returns true if the given mimeType matches any of given mimeTypes
     */
    public static bool Matches(string mimeType, params string[] mimeTypes)
    {
        if (mimeTypes == null || mimeTypes.Length == 0)
            return false;
        return mimeTypes.Any(type => mimeType != null && (type.Equals(mimeType, StringComparison.InvariantCultureIgnoreCase) || Regex.IsMatch(mimeType, type, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)));
    }
}
