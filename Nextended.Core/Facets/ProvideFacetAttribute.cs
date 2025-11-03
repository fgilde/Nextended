using System;

namespace Nextended.Core.Facets;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ProvideFacetAttribute(
    string label,
    FacetType type = FacetType.CheckboxList,
    bool multi = true,
    FacetGroupOperator groupOperator = FacetGroupOperator.Or,
    int order = 0,
    int topDistinct = 50)
    : Attribute
{
    public string Label { get; } = label;
    public FacetType Type { get; } = type;
    public bool MultiSelect { get; } = multi;
    public FacetGroupOperator GroupOperator { get; } = groupOperator;
    public int Order { get; } = order;
    public int TopDistinct { get; } = topDistinct;

    /// <summary>
    /// Optional value path for navigation properties (e.g., "TransportMode/Id" or "TransportModeId").
    /// If not set, the builder uses the property itself (for scalar fields).
    /// </summary>
    public string? ValuePath { get; set; }

    /// <summary>
    /// Optional label path to show a human-friendly value (e.g., "TransportMode/Name").
    /// If not set, the builder will use the value or value.ToString().
    /// </summary>
    public string? LabelPath { get; set; }

    /// <summary>
    /// The CLR type of the value path, required when ValuePath is set (e.g., typeof(Guid)).
    /// </summary>
    public Type? ValueType { get; set; }
}
