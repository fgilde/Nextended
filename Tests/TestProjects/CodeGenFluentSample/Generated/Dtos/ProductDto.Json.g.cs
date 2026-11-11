namespace FluentSample.Contracts;

[System.Text.Json.Serialization.JsonSerializable(typeof(ProductDto))]
public partial class ProductDtoJsonContext : System.Text.Json.Serialization.JsonSerializerContext;