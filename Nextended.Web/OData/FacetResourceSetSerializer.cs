using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System;
using System.Collections;

namespace Nextended.Web.OData;

public sealed class FacetResourceSetSerializer : ODataResourceSetSerializer
{
    public const string NAME = "cn.facets";
    public FacetResourceSetSerializer(ODataSerializerProvider provider) : base(provider) { }

    public override ODataResourceSet CreateResourceSet(
        IEnumerable resourceSet, IEdmCollectionTypeReference collectionType, ODataSerializerContext writeContext)
    {
        var set = base.CreateResourceSet(resourceSet, collectionType, writeContext);

        if (writeContext.Request?.HttpContext.Items.TryGetValue(NAME, out var facetsObj) == true && facetsObj is not null)
        {
            var raw = System.Text.Json.JsonSerializer.Serialize(facetsObj);
            set.InstanceAnnotations.Add(
                new ODataInstanceAnnotation(
                    NAME,
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
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return _setSerializer;

        return base.GetODataPayloadSerializer(type, request);
    }
}