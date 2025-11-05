namespace Nextended.Core.Facets;

/// <summary>
/// Specifies the data type for range-based facet filters.
/// </summary>
public enum FacetRangeDataType
{
    /// <summary>
    /// Integer or whole number range.
    /// </summary>
    Number,
    
    /// <summary>
    /// Decimal or floating-point number range.
    /// </summary>
    Decimal,
    
    /// <summary>
    /// Date-only range (without time component).
    /// </summary>
    Date,
    
    /// <summary>
    /// Date and time range.
    /// </summary>
    DateTime
}