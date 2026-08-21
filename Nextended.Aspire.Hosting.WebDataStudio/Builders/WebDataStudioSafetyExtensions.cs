using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// The studio masks columns whose names say they hold a secret — <c>password</c>, <c>api_key</c>,
/// <c>iban</c> and the like — before the values leave the server. These calls correct that guess for
/// a schema the word list reads wrong, from the place the rest of the configuration lives.
/// </summary>
public static class WebDataStudioSafetyExtensions
{
    /// <summary>
    /// Masks these columns as well, whatever the studio's word list thinks of their names. Matched
    /// by column name, case-insensitively, on every connection of this studio.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithMaskedColumns(
        this IResourceBuilder<WebDataStudioResource> builder, params string[] columns)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithColumnList("WDS_MASK_EXTRA", columns);
    }

    /// <summary>
    /// Leaves these columns alone, whatever the word list thinks. <c>token_type</c> and
    /// <c>secret_santa</c> are the kind of name this is for.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithUnmaskedColumns(
        this IResourceBuilder<WebDataStudioResource> builder, params string[] columns)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithColumnList("WDS_MASK_NEVER", columns);
    }

    /// <summary>
    /// Turns the name heuristic off, leaving only the columns named by
    /// <see cref="WithMaskedColumns"/>. For a schema where the guessing costs more than it saves.
    /// </summary>
    public static IResourceBuilder<WebDataStudioResource> WithoutColumnMasking(
        this IResourceBuilder<WebDataStudioResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithEnvironment("WDS_MASK_DEFAULT", "false");
    }

    /// Several calls add up rather than replacing each other, which is what chaining looks like it
    /// should do.
    private static IResourceBuilder<WebDataStudioResource> WithColumnList(
        this IResourceBuilder<WebDataStudioResource> builder, string variable, string[] columns)
    {
        var named = (columns ?? [])
            .Where(column => !string.IsNullOrWhiteSpace(column))
            .Select(column => column.Trim());

        var list = variable == "WDS_MASK_EXTRA"
            ? builder.Resource.MaskedColumnList
            : builder.Resource.UnmaskedColumnList;

        foreach (var column in named) list.Add(column);

        return builder.WithEnvironment(context =>
            context.EnvironmentVariables[variable] = string.Join(",", list));
    }
}
