namespace Nextended.Core.Facets;

/// <summary>
/// Represents a single bucket in a range-based facet (e.g., date ranges, price ranges).
/// Contains the range definition, label, and count of items within this bucket.
/// </summary>
public class FacetRangeBucket
{
    /// <summary>
    /// Gets or sets the unique key for this range bucket (e.g., "last7d").
    /// </summary>
    public string Key { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the display label for this range bucket (e.g., "Last 7 days").
    /// </summary>
    public string Label { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the range value definition (from/to bounds).
    /// </summary>
    public FacetRangeValue Value { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the number of items that fall within this range bucket.
    /// </summary>
    public long? Count { get; set; }
}