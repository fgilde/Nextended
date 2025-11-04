using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Nextended.Core.Facets;
using Nextended.Web.OData;
using System;
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
        if (ShouldBuildFacets())
        {
            await AddFacetResponseAsync(res, ct);
        }
        return res;
    }

    [EnableQuery]
    public virtual SingleResult<T> Get([FromODataUri] Guid key)
    {
        var param = Expression.Parameter(typeof(T), "e");
        var idProp = Expression.Property(param, IdPropertyName);
        var body = Expression.Equal(idProp, Expression.Constant(key));
        var lambda = Expression.Lambda<Func<T, bool>>(body, param);

        var query = Queryable().Where(lambda);
        return SingleResult.Create(query);
    }

    protected virtual async Task AddFacetResponseAsync(IQueryable<T> queryable, CancellationToken ct)
    {
        var facetBuilderOptions = GetFacetBuilderOptions();
        var facets = await Get<IFacetBuilder>().WithOptions(facetBuilderOptions)
            .BuildAsync(facetBuilderOptions.IsDisjunctiveBuild ? queryable.ApplyODataFilter(Request.ODataQueryOptions<T>(Get<IEdmModel>())) : queryable, ct);
        HttpContext.Items[FacetResourceSetSerializer.FacetsAnnotationName] = facets;
    }

    protected virtual bool ShouldBuildFacets()
    {
        return Request.Headers["Prefer"].Any(h => h?.Contains("odata.include-annotations=\"*\"", StringComparison.InvariantCultureIgnoreCase) == true
                                                  || h?.Contains(FacetResourceSetSerializer.FacetsAnnotationName, StringComparison.InvariantCultureIgnoreCase) == true);
    }

}