using System.Collections.Generic;

namespace Nextended.Core.Facets;

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