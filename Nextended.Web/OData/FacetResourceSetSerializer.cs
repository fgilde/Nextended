using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System;
using System.Collections;
using System.Text.Json;

namespace Nextended.Web.OData;

public sealed class FacetResourceSetSerializer(ODataSerializerProvider provider) : ODataResourceSetSerializer(provider)
{
    public const string FacetsAnnotationName = "cn.facets";

    public override ODataResourceSet CreateResourceSet(
        IEnumerable resourceSet, IEdmCollectionTypeReference collectionType, ODataSerializerContext writeContext)
    {
        var set = base.CreateResourceSet(resourceSet, collectionType, writeContext);

        if (writeContext.Request?.HttpContext.Items.TryGetValue(FacetsAnnotationName, out var facetsObj) == true && facetsObj is not null)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
            };

            var raw = JsonSerializer.Serialize(facetsObj, options);
            set.InstanceAnnotations.Add(
                new ODataInstanceAnnotation(
                    FacetsAnnotationName,
                    new ODataUntypedValue { RawValue = raw }
                )
            );
        }

        return set;
    }

}

public sealed class FacetSerializerProvider : ODataSerializerProvider
{
    private readonly FacetResourceSetSerializer _setSerializer;

    public FacetSerializerProvider(IServiceProvider sp) : base(sp)
        => _setSerializer = new FacetResourceSetSerializer(this);

    public override IODataSerializer GetODataPayloadSerializer(Type type, HttpRequest request)
    {
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return _setSerializer;

        return base.GetODataPayloadSerializer(type, request);
    }
}