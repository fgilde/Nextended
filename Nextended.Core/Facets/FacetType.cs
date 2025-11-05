namespace Nextended.Core.Facets;

/// <summary>
/// Specifies the type of UI control and behavior for a facet filter group.
/// </summary>
public enum FacetType
{
    /// <summary>
    /// Represents a filter type that allows users to select multiple options from a list of checkboxes.
    /// </summary>
    CheckboxList,

    /// <summary>
    /// Represents a filter type that allows users to select a single option from a list of radio buttons.
    /// </summary>
    Radio,

    /// <summary>
    /// Represents a filter type that allows users to select a range of numeric values.
    /// </summary>
    Range,

    /// <summary>
    /// Represents a filter type that allows users to select a range of dates.
    /// </summary>
    DateRange,

    /// <summary>
    /// Represents a search filter type that allows free text input for searching.
    /// </summary>
    Search,     
    
    /// <summary>
    /// Represents a filter type that allows selection from a list of tokens.
    /// </summary>
    TokenList   
}