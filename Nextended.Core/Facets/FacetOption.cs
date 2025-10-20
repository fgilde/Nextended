using System.Collections.Generic;

namespace Nextended.Core.Facets;

public class FacetOption
{

    /// <summary>
    /// Stable value (e.g., "Booked").
    /// </summary>
    public string Value { get; set; } = default!;
    
    /// <summary>
    /// Label in the UI.
    /// </summary>
    public string Label { get; set; } = default!;
    
    /// <summary>
    /// Whether currently selected.
    /// </summary>
    public bool Selected { get; set; }
    
    /// <summary>
    /// Whether currently applicable (e.g., 0 results -> disabled).
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Number of results if this option were applied alone (Facet Count).
    /// </summary>
    public long? Count { get; set; }
    
    /// <summary>
    /// Prepared OData fragment for this single option (e.g., "Status eq 'Booked'").
    /// </summary>
    public string OData { get; set; } = default!;
    
    /// <summary>
    /// Optional: Tooltip / description.
    /// </summary>
    public string? Hint { get; set; }
    
    /// <summary>
    /// Arbitrary additional data (e.g., icons, badges).
    /// </summary>
    public Dictionary<string, string>? Meta { get; set; }

}