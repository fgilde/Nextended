using System.Collections.Generic;

namespace Nextended.Core.Facets;

/// <summary>
/// Defines the configuration for a range-based facet filter, including data type, selected range, and preset range options.
/// </summary>
public class FacetRangeDefinition
{
    /// <summary>
    /// Range type (Number, Decimal, Date, DateTime).
    /// </summary>
    public FacetRangeDataType DataType { get; set; }

    /// <summary>
    /// Current selected range.
    /// </summary>
    public FacetRangeValue? Selected { get; set; }

    /// <summary>
    /// Predefined buckets (e.g. "Last 7 days").
    /// </summary>
    public List<FacetRangeBucket> Presets { get; set; } = new();
}