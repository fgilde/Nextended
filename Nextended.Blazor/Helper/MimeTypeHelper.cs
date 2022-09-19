using Nextended.Core;

namespace Nextended.Blazor.Helper;

[Obsolete("Use MimeType from Nextended.Core instead")]
public static class MimeTypeHelper
{
    [Obsolete("Use MimeType.IsZip from Nextended.Core instead")]
    public static bool IsZip(string contentType) => MimeType.IsZip(contentType);

    /**
     * Returns true if the given mimeType matches any of given mimeTypes
     */
    [Obsolete("Use MimeType.Matches from Nextended.Core instead")]
    public static bool Matches(string mimeType, params string[] mimeTypes) 
        => MimeType.Matches(mimeType, mimeTypes);
}