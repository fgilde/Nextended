using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Nextended.Web.OData;

namespace Nextended.Web.Extensions;

public static class ODataExtensions
{
    public static IServiceCollection AddODataAuto(this IServiceCollection services)
    {
        IEdmModel edmModel = ProvidedAsEdm.GetEdmModel();
        services.AddSingleton(edmModel);
        return services;    
    }

    public static IMvcBuilder AddODataAuto(this IMvcBuilder mvcBuilder, IEdmModel model, string routePrefix = "odata")
    {
        mvcBuilder.AddOData(opt =>
        {
            opt?.EnableQueryFeatures()?
                .AddRouteComponents(routePrefix, model, services =>
                {
                    services.AddSingleton<IODataSerializerProvider, FacetSerializerProvider>();
                    services.AddSingleton<Microsoft.OData.UriParser.ODataUriResolver>(sp => new Microsoft.OData.UriParser.ODataUriResolver { EnableCaseInsensitive = true });
                });
        });
        return mvcBuilder;
    }
}