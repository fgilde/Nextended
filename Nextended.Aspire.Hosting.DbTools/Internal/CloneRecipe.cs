using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Nextended.Aspire.Hosting.DbTools;

/// What one engine needs to be cloned: where its tools live, what port it listens on, and the
/// script that does the work.
internal sealed record CloneRecipe(
    int DefaultPort,
    string Image,
    string Tag,
    Func<string> Script)
{
    /// The container that clears the way before the clone, where an engine needs one. Only SQL
    /// Server does: its own tool refuses a target that is not empty, and dropping it needs a client
    /// that lives in a different image than the one the clone runs in.
    public (string Image, string Tag, Func<string> Script)? Prepare { get; init; }

    /// Whether "the shape without the rows" means anything here. It does not for MongoDB or Redis:
    /// a collection is its documents and a key is its value, so a schema-only clone would be an
    /// empty database rather than a useful one.
    public bool SupportsSchemaOnly { get; init; } = true;

    /// Whether "the rows into a schema that is already there" means anything here. It does not for
    /// SQL Server: a BACPAC is schema and rows in one file, and sqlpackage has no way to restore one
    /// half of it.
    public bool SupportsDataOnly { get; init; } = true;

    /// Whether this engine has a way of cloning out of plain metadata. Only SQL Server needs one:
    /// the other four engines' dump tools ask for no permission their own client does not have.
    public bool SupportsFromMetadata { get; init; }

    /// What this engine is called when it has to refuse something.
    public string Label { get; init; } = "this engine";
}

/// Everything the five engines' extension methods have in common: validate, name, add the
/// container, wait for what has to exist first.
internal static class CloneBuilder
{
    internal static IResourceBuilder<TTarget> Attach<TTarget>(
        IResourceBuilder<TTarget> target,
        DbEndpoint targetEndpoint,
        DbEndpoint source,
        IResource? sourceResource,
        CloneRecipe recipe,
        DbCloneOptions? given)
        where TTarget : IResource
    {
        var options = (given ?? new DbCloneOptions()).Validated();

        // Refused here rather than by a container three minutes later.
        if (options.SchemaOnly && !recipe.SupportsSchemaOnly)
            throw new ArgumentException(
                $"{recipe.Label} has no schema apart from what it stores, so a schema-only clone "
                + "would be an empty database", nameof(given));

        if (options.FromMetadata && !recipe.SupportsFromMetadata)
            throw new ArgumentException(
                $"{recipe.Label} needs no metadata clone: its own tools ask for no permission that "
                + "reading the database does not already give", nameof(given));

        if (options.DataOnly && !recipe.SupportsDataOnly)
            throw new ArgumentException(
                $"a data-only clone is not something {recipe.Label} can do", nameof(given));

        var name = CloneContainer.NameFor(target.Resource.Name, options);

        // WaitFor takes an untyped builder, and IResourceBuilder<T> is not covariant, so both ends
        // are handed back through the application builder as what that signature wants.
        var untypedTarget = Untyped(target.ApplicationBuilder, target.Resource);

        // What the clone waits for is the *server*, never the database it fills: the database carries
        // a health check that answers "has the clone finished", so waiting for the database would be
        // waiting for itself. Every recipe creates its target database if the server has not.
        var waitFor = target.Resource is IResourceWithParent child
            ? Untyped(target.ApplicationBuilder, child.Parent)
            : untypedTarget;

        // Where an engine needs the way cleared first, that is a container of its own — a different
        // image, because the tool that clears and the tool that clones are not in the same one.
        IResourceBuilder<ContainerResource>? prepare = null;

        // Only where something actually has to be cleared. A schema-only clone on SQL Server is a
        // publish: it adds what is missing and drops nothing, so clearing the way for it would drop a
        // database that nothing is going to recreate for minutes — and everything holding a
        // connection to it, the studio included, spends those minutes saying "cannot open database".
        if (options.Overwrite && !options.SchemaOnly && recipe.Prepare is { } step)
        {
            prepare = CloneContainer.Add(target.ApplicationBuilder, $"{name}-prepare",
                step.Image, step.Tag, source, targetEndpoint, step.Script(), options,
                recipe.DefaultPort);

            prepare.WaitFor(waitFor);
            prepare.WithParentRelationship(target.Resource);
        }

        var clone = CloneContainer.Add(target.ApplicationBuilder, name,
            options.Image ?? recipe.Image,
            options.Image is null ? recipe.Tag : null,
            source, targetEndpoint, recipe.Script(), options, recipe.DefaultPort);

        clone.WaitFor(waitFor);

        // The clone belongs to the database it fills: in the dashboard it hangs under it rather than
        // somewhere in the list, because "why is this database still empty" is a question asked at
        // the database, and its answer is this resource's log.
        clone.WithParentRelationship(target.Resource);

        // A source in this stack has to be up before there is anything to read; one outside it is
        // what the script's own waiting is for.
        if (options.WaitForSource && sourceResource is not null)
            clone.WaitFor(Untyped(target.ApplicationBuilder, sourceResource));

        // And where something had to be cleared, that has to have finished.
        if (prepare is not null) clone.WaitForCompletion(prepare);

        // A database resource cannot be made to *wait* for anything — Aspire 13.5's database resources
        // do not implement IResourceWithWaitSupport — but it can carry a health check, and that is
        // enough: unhealthy while the clone runs, healthy when it has finished. So the dashboard stops
        // showing a database as ready while its contents are still on their way, and WaitFor(target)
        // in somebody else's stack waits for the copy rather than for the server.
        Health(target, clone.Resource.Name);

        // And while it runs, the database says how far it has got.
        CloneProgress.Follow(target.ApplicationBuilder, target.Resource, clone.Resource.Name);

        return target;
    }

    /// The target answers "am I ready" with "has the clone finished".
    private static void Health<TTarget>(IResourceBuilder<TTarget> target, string cloneResourceName)
        where TTarget : IResource
    {
        var services = target.ApplicationBuilder.Services;

        // One watcher for the whole app host, however many clones it has.
        services.TryAddSingleton<CloneHealth>(provider =>
        {
            var health = new CloneHealth(provider.GetRequiredService<ResourceNotificationService>());
            health.Watch();
            return health;
        });

        var key = $"dbtools-clone-{cloneResourceName}";

        services.AddHealthChecks().Add(new HealthCheckRegistration(
            key,
            provider => new CloneHealthCheck(provider.GetRequiredService<CloneHealth>(), cloneResourceName),
            failureStatus: null,
            tags: null));

        target.WithHealthCheck(key);
    }

    private static IResourceBuilder<IResource> Untyped(
        IDistributedApplicationBuilder builder, IResource resource) =>
        builder.CreateResourceBuilder(resource);
}
