using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// A clone is a container that runs once.
///
/// Not code in the app host: the app host does not run when a stack is published, and a clone that
/// only happens on `dotnet run` is no use to anybody building a new system out of an old one. A
/// container resource is in the model, so it is in the manifest, so it runs wherever the stack does.
///
/// The tools are the engine's own — `pg_dump`, `mysqldump`, `mongodump` — and for most engines they
/// are already inside the database's image, which is why the image a clone runs in defaults to one
/// that matches the target.
internal static class CloneContainer
{
    /// Both ends of a clone travel as environment variables, prefixed: both have all five, and a
    /// script that mixed them up would be very hard to read.
    internal const string SourcePrefix = "CLONE_SOURCE_";
    internal const string TargetPrefix = "CLONE_TARGET_";

    /// A script written in a C# file, ready for a Linux container.
    ///
    /// A raw string keeps this file's line endings, and on a machine that checks out CRLF the
    /// container's shell reads `do\r` and answers "syntax error near unexpected token". That is not
    /// hypothetical: it is exactly how a demo's setup containers failed silently for a day.
    internal static string Sh(string script) => script.ReplaceLineEndings("\n");

    internal static IResourceBuilder<ContainerResource> Add(
        IDistributedApplicationBuilder builder,
        string name,
        string image,
        string? tag,
        DbEndpoint source,
        DbEndpoint target,
        string script,
        DbCloneOptions options,
        int defaultPort)
    {
        var clone = tag is null
            ? builder.AddContainer(name, image)
            : builder.AddContainer(name, image, tag);

        // The prologue turns a whole connection string into the five parts, for the ends that only
        // know it when the stack runs. Where the parts are known already it does nothing.
        clone.WithEntrypoint("/bin/sh")
            .WithArgs("-c", Sh(ClonePrologue.Script(defaultPort) + script));

        Describe(clone, SourcePrefix, source);
        Describe(clone, TargetPrefix, target);

        // What the script asks about before it writes anything.
        clone.WithEnvironment("CLONE_ONLY_WHEN_EMPTY",
            options.OnlyWhenEmpty && !options.Overwrite ? "1" : "0");
        clone.WithEnvironment("CLONE_OVERWRITE", options.Overwrite ? "1" : "0");
        clone.WithEnvironment("CLONE_SCHEMA_ONLY", options.SchemaOnly ? "1" : "0");
        clone.WithEnvironment("CLONE_DATA_ONLY", options.DataOnly ? "1" : "0");
        clone.WithEnvironment("CLONE_TIMEOUT", options.TimeoutSeconds.ToString());

        return clone;
    }

    private static void Describe(IResourceBuilder<ContainerResource> clone, string prefix, DbEndpoint end)
    {
        if (end.WholeString is { } whole)
        {
            clone.WithEnvironment($"{prefix}URL", whole);
            return;
        }

        clone.WithEnvironment($"{prefix}HOST", end.Host!);
        clone.WithEnvironment($"{prefix}PORT", end.Port!);
        clone.WithEnvironment($"{prefix}USER", end.User!);
        clone.WithEnvironment($"{prefix}PASSWORD", end.Password!);
        clone.WithEnvironment($"{prefix}DB", end.Database!);
    }

    /// The name the clone resource gets: the target's, plus what it does, so a stack with three of
    /// them reads as three lines rather than as a riddle.
    internal static string NameFor(string targetName, DbCloneOptions options) =>
        options.Name is { Length: > 0 } given ? given : $"{targetName}-clone";
}
