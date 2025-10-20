using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nextended.Core.Facets;
using Nextended.Web.OData;

namespace Nextended.Web.Controller;

public abstract class GenericODataController<T> : ODataController 
{
    protected virtual string IdPropertyName => "Id";

    [EnableQuery]
    protected abstract IQueryable<T> Queryable();

    [EnableQuery]
    public virtual async Task<IQueryable<T>> Get()
    {
        var res = Queryable();
        if (Request.Headers["Prefer"].Contains("odata.include-annotations=\"*\""))
        {
            var facetBuilder = Request.HttpContext.RequestServices.GetService<IFacetBuilder>();
            if (facetBuilder != null)
            {
                var facets = await facetBuilder.BuildAsync(res);
                HttpContext.Items[FacetResourceSetSerializer.NAME] = facets;
            }
        }
        return res;
    }

    [EnableQuery]
    [HttpGet("{key}")]
    public virtual SingleResult<T> Get([FromODataUri] Guid key)
    {
        var param = Expression.Parameter(typeof(T), "e");
        var idProp = Expression.Property(param, IdPropertyName);
        var body = Expression.Equal(idProp, Expression.Constant(key));
        var lambda = Expression.Lambda<Func<T, bool>>(body, param);

        var query = Queryable().Where(lambda);
        return SingleResult.Create(query);
    }
}