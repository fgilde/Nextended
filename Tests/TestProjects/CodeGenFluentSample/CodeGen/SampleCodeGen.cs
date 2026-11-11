using FluentSample.Domain;
using Nextended.Core.CodeGen;
using Nextended.Core.Enums;

namespace FluentSample.CodeGen;

/// <summary>
/// The project-wide configuration: what codegen.json would hold (Defaults), the types to generate and rules for groups of types.
/// Read by Nextended.CodeGen at compile time, never executed.
/// </summary>
public sealed class SampleCodeGen : CodeGenConfig
{
    public SampleCodeGen()
    {
        Defaults(d => d
            .Namespace("FluentSample.Contracts")
            .OutputPath("./Generated/Dtos/")
            .MappingOutputPath("./Generated/Mapping/")
            .CreateFileHeaders(false)
            .InitializeCollections()
            .DefaultMappingSettings(m => m.Strategy(DtoMappingStrategy.Extensions))
            // one more file for every DTO: a JSON source-generation registration
            .Emit("{Dto}.Json.g.cs", """
                namespace {DtoNamespace};

                [System.Text.Json.Serialization.JsonSerializable(typeof({Dto}))]
                public partial class {Dto}JsonContext : System.Text.Json.Serialization.JsonSerializerContext;
                """));

        // TenantId never leaves the server: one rule on the base reaches every DTO and view of every entity
        Dto<Entity>(e => e.Ignore(x => x.TenantId));

        Dto<Category>(c => c.Property(x => x.Description).PreClassString("[System.ComponentModel.DataAnnotations.MaxLength(500)]"));

        // the enum is sent by name, without the member that only old data has
        Dto<ProductState>(s => s
            .Ignore(nameof(ProductState.Legacy))
            .PreClassString("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]"));
    }
}
