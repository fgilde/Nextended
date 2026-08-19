using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.WebDataStudio;

/// <summary>
/// Attaches WebDataStudio to a database from the database's own side, the way Aspire's
/// <c>WithPgAdmin</c> and <c>WithRedisInsight</c> do it: the studio is created once and every
/// further call attaches another connection to the same one.
/// </summary>
public static class WebDataStudioAttachExtensions
{
    /// <summary>
    /// Adds this database to a WebDataStudio instance, creating that instance on the first call.
    /// <para>
    /// Sharing follows the studio's resource name. Every call that leaves
    /// <paramref name="studioName"/> alone lands in the same studio, so five databases give you
    /// one studio with five connections. Pass a different name to get a second studio, or pass a
    /// studio you built yourself with
    /// <see cref="WebDataStudioBuilderExtensions.AddWebDataStudio"/> to the other overload.
    /// </para>
    /// </summary>
    /// <param name="database">The database, cache or server to attach.</param>
    /// <param name="configure">Runs against the studio — the place for login, read-only mode and the rest. Called on every attach, so keep it idempotent.</param>
    /// <param name="studioName">Which studio to use (default <c>webdatastudio</c>). Same name, same instance.</param>
    /// <param name="connectionName">Label in the studio (default: the resource's name, upper-cased).</param>
    /// <param name="engine">Which engine it is (default: worked out from the resource type).</param>
    /// <returns>The database, so the call chains into the rest of its configuration.</returns>
    public static IResourceBuilder<T> WithWebDataStudio<T>(
        this IResourceBuilder<T> database,
        Action<IResourceBuilder<WebDataStudioResource>>? configure = null,
        string? studioName = null,
        string? connectionName = null,
        WebDataStudioEngine? engine = null)
        where T : IResourceWithConnectionString
    {
        ArgumentNullException.ThrowIfNull(database);

        var name = studioName ?? WebDataStudioResource.DefaultResourceName;
        var applicationBuilder = database.ApplicationBuilder;

        // One studio per name. Looking it up in the model rather than in a static keeps two app
        // hosts in the same process — a test suite, say — from sharing state.
        var existing = applicationBuilder.Resources.OfType<WebDataStudioResource>()
            .FirstOrDefault(resource => string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase));

        var studio = existing is null
            ? applicationBuilder.AddWebDataStudio(name)
            : applicationBuilder.CreateResourceBuilder(existing);

        studio.WithReference(database, connectionName, engine);
        configure?.Invoke(studio);

        return database;
    }

    /// <summary>
    /// Adds this database to a WebDataStudio instance you built yourself — the explicit form of
    /// sharing, for when one stack runs several studios and each database has to land in a
    /// particular one.
    /// </summary>
    /// <param name="database">The database, cache or server to attach.</param>
    /// <param name="studio">The studio to attach it to.</param>
    /// <param name="connectionName">Label in the studio (default: the resource's name, upper-cased).</param>
    /// <param name="engine">Which engine it is (default: worked out from the resource type).</param>
    /// <param name="readOnly">Opens this connection read-only.</param>
    /// <param name="group">Groups the connection in the explorer.</param>
    /// <param name="color">Tints the connection in the explorer, e.g. <c>#e03131</c> for production.</param>
    /// <returns>The database, so the call chains into the rest of its configuration.</returns>
    public static IResourceBuilder<T> WithWebDataStudio<T>(
        this IResourceBuilder<T> database,
        IResourceBuilder<WebDataStudioResource> studio,
        string? connectionName = null,
        WebDataStudioEngine? engine = null,
        bool readOnly = false,
        string? group = null,
        string? color = null)
        where T : IResourceWithConnectionString
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(studio);

        studio.WithReference(database, connectionName, engine, readOnly, group, color);
        return database;
    }
}
