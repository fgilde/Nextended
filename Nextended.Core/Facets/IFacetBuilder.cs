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
    Task<List<FacetGroup>> BuildAsync<T>(IQueryable<T> alreadyFilteredQuery, CancellationToken ct = default) where T : class;

    Task<List<FacetGroup>> BuildAsync<T>(IQueryable<T> baseQuery, IReadOnlyList<AppliedFacet> applied, CancellationToken ct = default) where T : class;// where T: IGuidEntity;

    Task<List<FacetGroup>> BuildAsync<T>(
        Func<IServiceProvider, Task<IQueryable<T>>> baseQueryFactory,
        IReadOnlyList<AppliedFacet> applied,
        CancellationToken ct = default) where T : class;


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
