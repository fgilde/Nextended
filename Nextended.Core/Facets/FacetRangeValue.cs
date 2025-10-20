namespace Nextended.Core.Facets;

public class FacetRangeValue
{
    
    public string? From { get; set; } 
    public string? To { get; set; }

    /// <summary>
    /// OData-Fragmment for this range, e.g. "(Eta ge 2025-01-01 and Eta lt 2025-02-01)".
    /// </summary>
    public string? OData { get; set; }
}