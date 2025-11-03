using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Nextended.Web.OData;

public class ODataOptionsConfiguration(IEdmModel edmModel) : IConfigureOptions<ODataOptions>
{
    private const string RoutePrefix = "odata";

    public void Configure(ODataOptions options)
    {
        ConfigureDefaultAutoOData(options, edmModel, RoutePrefix);
    }

    internal static void ConfigureDefaultAutoOData(ODataOptions options, IEdmModel edmModel, string routePrefix)
    {
        options.EnableQueryFeatures()
            .AddRouteComponents(routePrefix, edmModel, services =>
            {
                services.AddSingleton<IODataSerializerProvider, FacetSerializerProvider>();
                services.AddSingleton<ODataUriResolver>(sp => new ODataUriResolver { EnableCaseInsensitive = true });
            });
    }
}