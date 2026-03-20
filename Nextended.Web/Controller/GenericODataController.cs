using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Nextended.Core.Facets;
using Nextended.Web.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Web.Controller;

public abstract class GenericODataController<T> : ODataController where T : class
{
    protected virtual TService Get<TService>() => Request.HttpContext.RequestServices.GetRequiredService<TService>();
    protected virtual FacetBuilderOptions GetFacetBuilderOptions() => new();
    protected virtual string IdPropertyName => "Id";

    [EnableQuery]
    protected abstract IQueryable<T> Queryable();

   

    [EnableQuery]
    public virtual async Task<IQueryable<T>> Get(CancellationToken ct = default)
    {
        var res = Queryable();
        res = res.ApplyODataSearch(Request.ODataQueryOptions<T>(Get<IEdmModel>()).Search);
        await TryBuildFacets(ct);
        return res;
    }


    [EnableQuery]
    public virtual Task<SingleResult<T>> Get([FromODataUri] Guid key)
    {
        var param = Expression.Parameter(typeof(T), "e");
        var idProp = Expression.Property(param, IdPropertyName);
        var body = Expression.Equal(idProp, Expression.Constant(key));
        var lambda = Expression.Lambda<Func<T, bool>>(body, param);

        var query = Queryable().Where(lambda);
        return Task.FromResult(SingleResult.Create(query));
    }

    
    private async Task AddFacetResponseAsync(CancellationToken ct)
    {
        var facetBuilderOptions = GetFacetBuilderOptions();

        var facetBuilder = Get<IFacetBuilder>()
            .WithOptions(facetBuilderOptions);

        List<FacetGroup> facets;

        if (facetBuilderOptions.IsDisjunctiveBuild)
        {
            Func<IServiceProvider, Task<IQueryable<T>>> queryFactory = sp =>
            {
                try
                {
                    return Task.FromResult(Queryable().ApplyODataFilter(Request.ODataQueryOptions<T>(Get<IEdmModel>())));
                }
                catch (Exception exception)
                {
                    return Task.FromException<IQueryable<T>>(exception);
                }
            };
            facets = await facetBuilder.BuildAsync(queryFactory, [], ct);
        }
        else
        {
            var queryable = Queryable();
            facets = await facetBuilder.BuildAsync(queryable, ct);
        }

        HttpContext.Items[FacetResourceSetSerializer.FacetsAnnotationName] = facets;
    }

    protected async Task TryBuildFacets(CancellationToken ct = default)
    {
        if (ShouldBuildFacets())
        {
            await AddFacetResponseAsync(ct);
        }
    }
    
    protected virtual bool ShouldBuildFacets()
    {
        return Request.Headers["Prefer"].Any(h => h?.Contains("odata.include-annotations=\"*\"", StringComparison.InvariantCultureIgnoreCase) == true
                                                  || h?.Contains(FacetResourceSetSerializer.FacetsAnnotationName, StringComparison.InvariantCultureIgnoreCase) == true);
    }

}