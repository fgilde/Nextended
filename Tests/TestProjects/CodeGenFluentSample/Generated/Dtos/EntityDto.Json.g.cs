namespace FluentSample.Contracts;

[System.Text.Json.Serialization.JsonSerializable(typeof(EntityDto))]
public partial class EntityDtoJsonContext : System.Text.Json.Serialization.JsonSerializerContext;