using System.Collections.Generic;

namespace Nextended.ResponseFilters;

/// <summary>
/// Per-pipeline-run ledger of <see cref="StructuralEdit"/>s, keyed by the object instance the edit
/// applies to (reference identity). Structural rules record into it while the pipeline walks the
/// graph; the serialization layer replays it against the produced JSON tree.
/// </summary>
/// <remarks>
/// Not thread-safe — rules are applied sequentially per object, matching the rest of the pipeline.
/// </remarks>
public sealed class StructuralEditBook
{
    private readonly Dictionary<object, List<StructuralEdit>> _byOwner =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>True when at least one edit has been recorded — lets the host skip the JSON transform entirely.</summary>
    public bool HasAny => _byOwner.Count > 0;

    /// <summary>Record an edit against <paramref name="owner"/>. No-op when <paramref name="owner"/> is null.</summary>
    public void Record(object? owner, StructuralEdit edit)
    {
        if (owner is null) return;
        if (!_byOwner.TryGetValue(owner, out var list))
        {
            list = new List<StructuralEdit>();
            _byOwner[owner] = list;
        }
        list.Add(edit);
    }

    /// <summary>The edits recorded for <paramref name="owner"/>, or <c>null</c> when there are none.</summary>
    public IReadOnlyList<StructuralEdit>? ForOwner(object owner)
        => _byOwner.TryGetValue(owner, out var list) ? list : null;
}
