using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Nextended.Core.Facets;

public interface IFacetBuilder
{
    Task<List<FacetGroup>> BuildAsync<T>(IQueryable<T> alreadyFilteredQuery, CancellationToken ct = default);

    Task<List<FacetGroup>> BuildAsync<T>(
        IQueryable<T> baseQuery,
        IReadOnlyList<AppliedFacet> applied,
        CancellationToken ct = default);
}
