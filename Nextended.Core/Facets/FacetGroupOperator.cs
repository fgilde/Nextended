namespace Nextended.Core.Facets;

/// <summary>
/// Specifies the logical operator used to combine multiple selected options within a facet group.
/// </summary>
public enum FacetGroupOperator
{
    /// <summary>
    /// Combines filter options using AND logic (all conditions must be met).
    /// </summary>
    And,
    
    /// <summary>
    /// Combines filter options using OR logic (any condition must be met).
    /// </summary>
    Or
}