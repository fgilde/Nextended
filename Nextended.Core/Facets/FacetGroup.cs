using System;
using System.Collections.Generic;

namespace Nextended.Core.Facets;

/// <summary>
/// Represents a group of facet options for filtering and searching.
/// A facet group defines the structure, behavior, and options for a specific filter category.
/// </summary>
public class FacetGroup
{
    /// <summary>
    /// Gets or sets the unique key identifying this facet group.
    /// </summary>
    public string Key { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the display label for this facet group.
    /// </summary>
    public string Label { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the field name in the data source that this facet operates on.
    /// </summary>
    public string Field { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the path to the value property when working with complex objects.
    /// </summary>
    public string? ValuePath { get; set; }
    
    /// <summary>
    /// Gets or sets the path to the label property when working with complex objects.
    /// </summary>
    public string? LabelPath { get; set; }
    
    /// <summary>
    /// Gets or sets the CLR type name for the value (used for type conversion).
    /// </summary>
    public string? ValueClrType { get; set; }
    
    /// <summary>
    /// Gets or sets the type of facet UI control (checkbox list, range, etc.).
    /// </summary>
    public FacetType Type { get; set; } = FacetType.CheckboxList;
    
    /// <summary>
    /// Gets or sets the logical operator for combining multiple selections (OR/AND).
    /// </summary>
    public FacetGroupOperator GroupOperator { get; set; } = FacetGroupOperator.Or;
    
    /// <summary>
    /// Gets or sets the list of available options for this facet group.
    /// </summary>
    public List<FacetOption> Options { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the range definition if this is a range-based facet.
    /// </summary>
    public FacetRangeDefinition? Range { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether multiple options can be selected simultaneously.
    /// </summary>
    public bool MultiSelect { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether this facet group is currently enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the list of dependencies that control when this facet is available.
    /// </summary>
    public List<FacetDependency> DependsOn { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the display order for this facet group relative to others.
    /// </summary>
    public int Order { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets the time taken to build this facet group's options.
    /// </summary>
    public TimeSpan BuildDuration { get; set; }
}