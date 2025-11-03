using System;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Nextended.Web.OData;

namespace Nextended.Web.Extensions;

public static class ODataExtensions
{

    public static IServiceCollection AddODataAuto(this IServiceCollection services) => services.AddODataAuto(ProvidedAsEdm.GetEdmModel());
    public static IServiceCollection AddODataAuto(this IServiceCollection services, IEdmModel edmModel) => services.AddODataAuto(_ => edmModel);

    public static IServiceCollection AddODataAuto(this IServiceCollection services, Func<IServiceProvider, IEdmModel> edmModelProvider)
    {
        services.AddSingleton<IEdmModel>(edmModelProvider);
        services.AddSingleton<IConfigureOptions<ODataOptions>, ODataOptionsConfiguration>();

        return services;    
    }

    public static IMvcBuilder AddODataAuto(this IMvcBuilder mvcBuilder, IEdmModel model, string routePrefix = "odata")
    {
        return mvcBuilder.AddOData(opt =>
        {
            ODataOptionsConfiguration.ConfigureDefaultAutoOData(opt, model, routePrefix);
        });
    }
}