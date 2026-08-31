using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// The clone itself, as a resource — for everything that must not start before the copy is there.
/// </summary>
public static class CloneExtensions
{
    /// <summary>
    /// The container that fills a cloned database, so other resources can wait for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A cloned database reports itself running as soon as its server is up, because that is all a
    /// database resource in Aspire is: it has no state of its own and cannot be made to wait for
    /// anything. So a stack that must not touch the copy before it is complete waits for the *clone*
    /// instead — an application, a migration, a studio, anything that supports waiting:
    /// </para>
    /// <example>
    /// <code>
    /// var copy = sql.AddDatabase("orders-copy").WithCloneFrom(staging);
    ///
    /// builder.AddProject&lt;Projects.Api&gt;("api")
    ///        .WithReference(copy)
    ///        .WaitForCompletion(builder.CloneOf("orders-copy"));   // starts when the copy is there
    /// </code>
    /// </example>
    /// <para>
    /// The clone exits 0 when it has copied, and also when it found the target already full and left
    /// it alone — both mean "the database is ready to be used", which is what waiting is about.
    /// </para>
    /// </remarks>
    /// <param name="builder">The application builder.</param>
    /// <param name="databaseName">The name of the cloned database — the resource
    /// <c>WithCloneFrom</c> was called on.</param>
    /// <param name="cloneName">The clone's own resource name, where
    /// <see cref="DbCloneOptions.Name"/> gave it one.</param>
    /// <exception cref="ArgumentException">No such clone in this stack. The message lists the ones
    /// there are, because the usual cause is a name typed twice differently.</exception>
    public static IResourceBuilder<ContainerResource> CloneOf(
        this IDistributedApplicationBuilder builder,
        string databaseName,
        string? cloneName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var name = cloneName ?? $"{databaseName}-clone";

        var clone = builder.Resources.OfType<ContainerResource>()
            .FirstOrDefault(resource =>
                string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase));

        if (clone is null)
        {
            var known = builder.Resources.OfType<ContainerResource>()
                .Select(resource => resource.Name)
                .Where(resourceName => resourceName.EndsWith("-clone", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            throw new ArgumentException(
                $"there is no clone called '{name}' in this stack"
                + (known.Length > 0
                    ? $"; the clones here are {string.Join(", ", known)}"
                    : "; no database in this stack is cloned at all"),
                nameof(databaseName));
        }

        return builder.CreateResourceBuilder(clone);
    }
}
