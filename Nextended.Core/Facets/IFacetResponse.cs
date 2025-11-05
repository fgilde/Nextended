using System.Collections.Generic;
using Nextended.Core.OData;

namespace Nextended.Core.Facets;

/// <summary>
/// Represents the response from a faceted search operation, containing filter groups, applied filters, and query metadata.
/// </summary>
public interface IFacetResponse
{
    /// <summary>
    /// All applicable filter groups (for UI rendering).
    /// </summary>
    public List<FacetGroup> Filters { get; set; }

    ///// <summary>
    ///// Actually applied filters as chips (e.g. for removal in the UI).
    ///// </summary>
    //public List<AppliedFacet> Applied { get; set; }

    /// <summary>
    /// Complete OData filter that combines all applied filters.
    /// </summary>
    public string? ODataFilter { get; set; }

    /// <summary>
    /// Additional information about the current query (e.g. $orderby, $top, $skip).
    /// </summary>
    public ODataQueryModel Query { get; set; }
}