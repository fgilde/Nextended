using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

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

        if (options.DataOnly && !recipe.SupportsSchemaOnly)
            throw new ArgumentException(
                $"every clone of {recipe.Label} is data only; there is no schema to leave out",
                nameof(given));

        var name = CloneContainer.NameFor(target.Resource.Name, options);

        // WaitFor takes an untyped builder, and IResourceBuilder<T> is not covariant, so both ends
        // are handed back through the application builder as what that signature wants.
        var untypedTarget = Untyped(target.ApplicationBuilder, target.Resource);

        // Where an engine needs the way cleared first, that is a container of its own — a different
        // image, because the tool that clears and the tool that clones are not in the same one.
        IResourceBuilder<ContainerResource>? prepare = null;

        if (options.Overwrite && recipe.Prepare is { } step)
        {
            prepare = CloneContainer.Add(target.ApplicationBuilder, $"{name}-prepare",
                step.Image, step.Tag, source, targetEndpoint, step.Script(), options,
                recipe.DefaultPort);

            prepare.WaitFor(untypedTarget);
        }

        var clone = CloneContainer.Add(target.ApplicationBuilder, name,
            options.Image ?? recipe.Image,
            options.Image is null ? recipe.Tag : null,
            source, targetEndpoint, recipe.Script(), options, recipe.DefaultPort);

        clone.WaitFor(untypedTarget);

        // A source in this stack has to be up before there is anything to read; one outside it is
        // what the script's own waiting is for.
        if (options.WaitForSource && sourceResource is not null)
            clone.WaitFor(Untyped(target.ApplicationBuilder, sourceResource));

        // And where something had to be cleared, that has to have finished.
        if (prepare is not null) clone.WaitForCompletion(prepare);

        return target;
    }

    private static IResourceBuilder<IResource> Untyped(
        IDistributedApplicationBuilder builder, IResource resource) =>
        builder.CreateResourceBuilder(resource);
}
