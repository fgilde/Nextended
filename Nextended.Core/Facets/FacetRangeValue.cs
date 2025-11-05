namespace Nextended.Core.Facets;

/// <summary>
/// Represents the selected value range for a range-based facet filter.
/// Contains the from/to bounds and the corresponding OData filter expression.
/// </summary>
public class FacetRangeValue
{
    /// <summary>
    /// Gets or sets the lower bound of the range.
    /// </summary>
    public string? From { get; set; }
    
    /// <summary>
    /// Gets or sets the upper bound of the range.
    /// </summary>
    public string? To { get; set; }

    /// <summary>
    /// OData-Fragmment for this range, e.g. "(Eta ge 2025-01-01 and Eta lt 2025-02-01)".
    /// </summary>
    public string? OData { get; set; }
}