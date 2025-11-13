using System;

namespace Nextended.Core.Facets;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ProvideFacetAttribute : Attribute
{
    public string? Label { get; set; }
    public FacetType Type { get; set; } = FacetType.CheckboxList;
    public bool MultiSelect { get; set; } = true;
    public FacetGroupOperator GroupOperator { get; set; } = FacetGroupOperator.Or;
    public int Order { get; set; } = 0;
    public int TopDistinct { get; set; }

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
