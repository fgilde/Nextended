namespace Nextended.Core.Facets;

public class FacetRangeBucket
{
    public string Key { get; set; } = default!;   // z.B. "last7d"
    public string Label { get; set; } = default!; // z.B. "Last 7 days"
    public FacetRangeValue Value { get; set; } = new();
    public long? Count { get; set; }
}