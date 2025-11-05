using System.Collections.Generic;

namespace Nextended.Core.Facets;

/// <summary>
/// Represents a dependency relationship between facet groups, where one group's availability depends on selections in another group.
/// </summary>
public class FacetDependency
{
    /// <summary>
    /// Key of the parent group (e.g. "transportMode").
    /// </summary>
    public string ParentKey { get; set; } = default!;

    /// <summary>
    /// Values that must be set in the parent group for this group to be active.
    /// </summary>
    public List<string> RequiredValues { get; set; } = new();
}