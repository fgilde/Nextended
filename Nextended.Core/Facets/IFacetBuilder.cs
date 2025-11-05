using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Nextended.Core.Facets;

/// <summary>
/// Provides methods for building facet groups from queryable data sources.
/// Analyzes data to generate filter options and counts for faceted search functionality.
/// </summary>
public interface IFacetBuilder
{
    /// <summary>
    /// Builds facet groups from an already filtered query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="alreadyFilteredQuery">The query with filters already applied.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of facet groups with computed counts and options.</returns>
    Task<List<FacetGroup>> BuildAsync<T>(IQueryable<T> alreadyFilteredQuery, CancellationToken ct = default);

    /// <summary>
    /// Builds facet groups from a base query and applied facets.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="baseQuery">The base unfiltered query.</param>
    /// <param name="applied">The list of already applied facets.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of facet groups with computed counts and options.</returns>
    Task<List<FacetGroup>> BuildAsync<T>(
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        CancellationToken ct = default);

    /// <summary>
    /// Configures the facet builder with custom options.
    /// </summary>
    /// <param name="options">The facet builder options.</param>
    /// <returns>The facet builder instance for method chaining.</returns>
    IFacetBuilder WithOptions(FacetBuilderOptions options);
    
    /// <summary>
    /// Sets a localization function for translating facet labels and values.
    /// </summary>
    /// <param name="localizerFn">The localization function.</param>
    /// <returns>The facet builder instance for method chaining.</returns>
    IFacetBuilder WithLocalizationFunc(Func<string, Type?, string> localizerFn);

}
