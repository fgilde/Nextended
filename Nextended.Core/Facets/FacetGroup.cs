using System;
using System.Collections.Generic;

namespace Nextended.Core.Facets;

public class FacetGroup
{
    public string Key { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Field { get; set; } = default!;     
    public string? ValuePath { get; set; }            
    public string? LabelPath { get; set; }            
    public string? ValueClrType { get; set; }         
    public FacetType Type { get; set; } = FacetType.CheckboxList;
    public FacetGroupOperator GroupOperator { get; set; } = FacetGroupOperator.Or;
    public List<FacetOption> Options { get; set; } = new();
    public FacetRangeDefinition? Range { get; set; }
    public bool MultiSelect { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public List<FacetDependency> DependsOn { get; set; } = new();
    public int Order { get; set; } = 0;
    public TimeSpan BuildDuration { get; set; }
}