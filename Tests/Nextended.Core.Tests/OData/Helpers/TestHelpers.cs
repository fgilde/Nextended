using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Nextended.Core.Tests.OData.Helpers;

public class TestHelpers
{
    
    public static QueryString GetODataQueryString<T>(string queryString) where T : class
    {
        var model = GetEdmModel<T>();

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        request.Method = "GET";
        return new QueryString(queryString);
        
    }
    public static ODataQueryOptions<T> GetODataQueryOptions<T>(string queryString) where T : class
    {
        var model = GetEdmModel<T>();

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        request.Method = "GET";
        request.QueryString = new QueryString(queryString);

        var context = new ODataQueryContext(model, typeof(T), new ODataPath());
        return new ODataQueryOptions<T>(context, request);
    }

    private static IEdmModel GetEdmModel<T>() where T : class
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<T>(typeof(T).Name + "s"); // pluralize name
        return builder.GetEdmModel();
    }

}