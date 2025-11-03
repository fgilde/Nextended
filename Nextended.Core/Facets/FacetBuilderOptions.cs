namespace Nextended.Core.Facets;

public sealed class FacetBuilderOptions
{
    /// <summary>
    /// If true, facets will be built. If false, only applied filters will be extracted.
    /// </summary>
    public bool BuildFacets { get; set; }

    /// <summary>
    /// If true, the list of available values for properties with <see cref="ProvideFacetAttribute.Type"/> <see cref="FacetType.CheckboxList"/>, <see cref="FacetType.TokenList"/> or <see cref="FacetType.Radio"/> will be calculated
    /// </summary>
    public bool ComputeDynamicLists { get; set; } = true;

    /// <summary>
    /// If true, compute counts per group on a query with all other groups' filters applied (disjunctive faceting).
    /// </summary>
    public bool DisjunctiveFacets { get; set; } = false;

    /// <summary>
    /// If true, group operator (AND/OR) will be considered when computing disjunctive facets. Only relevant if DisjunctiveFacets is false.
    /// </summary>
    public bool DisjunctiveByGroupOperator { get; set; } = true;


    /// <summary>
    /// Default top limit for discrete facets if not set on attribute.
    /// </summary>
    public int? DefaultTopDistinct { get; set; } = 50;

    /// <summary>
    /// Specifies whether to build literals should be false for Edm provided models.
    /// </summary>
    public bool BuildLiterals { get; set; } = false;

    public bool IsDisjunctiveBuild => DisjunctiveFacets || DisjunctiveByGroupOperator;
}