namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// What a clone copies, and what it is allowed to overwrite.
/// </summary>
public sealed record DbCloneOptions
{
    /// <summary>
    /// Leave a target that already has something in it alone.
    /// </summary>
    /// <remarks>
    /// On by default, because the alternative is a stack restart that throws away the morning's
    /// work. Turn it off — with <see cref="Overwrite"/> — when replacing the target is the point,
    /// which it is when a new system is being built out of an old one.
    /// </remarks>
    public bool OnlyWhenEmpty { get; init; } = true;

    /// <summary>
    /// Replace whatever is in the target.
    /// </summary>
    /// <remarks>
    /// Implies that <see cref="OnlyWhenEmpty"/> no longer applies: the clone drops what it finds
    /// before restoring. Say it deliberately; nothing here does it for you.
    /// </remarks>
    public bool Overwrite { get; init; }

    /// <summary>The shape without the rows.</summary>
    public bool SchemaOnly { get; init; }

    /// <summary>The rows into a schema that is already there.</summary>
    public bool DataOnly { get; init; }

    /// <summary>
    /// The image the dump and restore run in.
    /// </summary>
    /// <remarks>
    /// Null means the one that fits the target — for most engines that is the database's own image,
    /// because the tools live in it. Set it for an air-gapped stack with a mirror of its own, or to
    /// pin a version.
    /// </remarks>
    public string? Image { get; init; }

    /// <summary>
    /// How long the clone may take before it is a failure. A first clone of a large database is
    /// minutes, not seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 3600;

    /// <summary>
    /// The name of the resource the clone appears as, so the dashboard says which one it is when a
    /// stack has several.
    /// </summary>
    public string? Name { get; init; }

    /// Whether to wait for the source as well.
    ///
    /// True where the source is a resource in this stack — nothing can be dumped out of a server
    /// that has not started. There is nothing to wait for when the source is somewhere else, and
    /// that is what the script's own retry loop is for.
    internal bool WaitForSource { get; init; }

    internal DbCloneOptions Validated()
    {
        if (SchemaOnly && DataOnly)
            throw new ArgumentException(
                "a clone is either the schema without the rows or the rows without the schema, "
                + "not both at once");

        if (TimeoutSeconds < 1)
            throw new ArgumentOutOfRangeException(nameof(TimeoutSeconds),
                "a clone needs longer than no time at all");

        return this;
    }
}
