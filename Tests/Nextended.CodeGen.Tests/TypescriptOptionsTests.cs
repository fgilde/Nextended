using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Generators.TypescriptGeneration;
using Shouldly;
using static Nextended.CodeGen.Tests.TestCompilation;

namespace Nextended.CodeGen.Tests;

/// <summary>The opt-in TypeScript settings found by the CargonerdsApp pilot: header, optional defaults, member filter, declared unions.</summary>
[TestClass]
public class TypescriptOptionsTests
{
    private static SortedDictionary<string, string> Ts(TypescriptGenerationConfig config, params string[] sources)
        => TypescriptGenerator.Generate(CompileWithInTreeAttributes(sources), config).Files;

    private static TypescriptGenerationConfig Config(Action<TypescriptGenerationConfig>? configure = null)
    {
        var config = new TypescriptGenerationConfig { IncludeNamespaces = ["Shop"], Layout = TypescriptLayout.Flat, Barrel = false };
        configure?.Invoke(config);
        return config;
    }

    private static string Lf(string s) => s.Replace("\r\n", "\n");

    // ---------------------------------------------------------------- header

    [TestMethod]
    public void Header_is_replaced_or_left_out()
    {
        const string source = "namespace Shop; public class A { public System.Guid Id { get; set; } }";

        Ts(Config(), source)["A.ts"].ShouldStartWith(TypescriptGenerator.Header + "\n");

        var own = Ts(Config(c => { c.Header = "// generated\r\n// by us\n"; c.Barrel = true; c.TypeAliases = true; }), source);
        own["A.ts"].ShouldBe(Lf("""
            // generated
            // by us

            import type { Guid } from './scalars';

            export interface A {
                id: Guid;
            }

            """));
        own["index.ts"].ShouldStartWith("// generated\n// by us\n\nexport * from './scalars';\n");
        own["scalars.ts"].ShouldStartWith("// generated\n// by us\n\nexport type Guid = string;\n");

        var none = Ts(Config(c => { c.Header = ""; c.Barrel = true; }), source);
        none["A.ts"].ShouldStartWith("export interface A {");
        none["index.ts"].ShouldBe("export * from './A';\n");
    }

    // ---------------------------------------------------------------- optional defaults

    private const string Request = """
        using System.ComponentModel;
        using System.Collections.Generic;
        using System.Text.Json.Serialization;
        namespace Shop;
        public class QueryDto
        {
            public string? Filter { get; set; }
            public int Max { get; set; } = 10;
            public string Sorting { get; set; } = "";
            public List<string> Tags { get; set; } = [];
            [DefaultValue(true)] public bool Active { get; set; }
            public string Required { get; set; } = null!;
            public string AlsoRequired { get; set; } = default!;
            public string? Wrapped { get; set; } = (null)!;
            public int Plain { get; set; }
            [JsonInclude] public int Field = 3;
            public int Forced { get; set; } = 1;
            [System.ComponentModel.DataAnnotations.Required] public string Term { get; set; } = "";
            [JsonRequired] public int Page { get; set; } = 1;
            public required string Name { get; set; } = "";
        }
        public record PageDto(int Skip, int Take = 20);
        """;

    [TestMethod]
    public void Members_with_default_values_are_optional_when_asked()
    {
        Ts(Config(), Request)["QueryDto.ts"].ShouldNotContain("?:");

        var config = Config(c => c.OptionalDefaultValues = true);
        config.Types["Shop.QueryDto"] = new TypescriptTypeConfig();
        config.Types["Shop.QueryDto"].Member("Forced").Optional = false;
        var files = Ts(config, Request);
        files["QueryDto.ts"].ShouldEndWith(Lf("""
            export interface QueryDto {
                filter: string | null;
                max?: number;
                sorting?: string;
                tags?: string[];
                active?: boolean;
                required: string;
                alsoRequired: string;
                wrapped: string | null;
                plain: number;
                field?: number;
                forced: number;
                term: string;
                page: number;
                name: string;
            }

            """));
        files["PageDto.ts"].ShouldContain("    skip: number;\n    take?: number;\n");

        // per type: on for one type only, off for one type
        var perType = Config();
        perType.Types["Shop.PageDto"] = new TypescriptTypeConfig { OptionalDefaultValues = true };
        var only = Ts(perType, Request);
        only["PageDto.ts"].ShouldContain("    take?: number;\n");
        only["QueryDto.ts"].ShouldNotContain("?:");
        var off = Config(c => c.OptionalDefaultValues = true);
        off.Types["Shop.PageDto"] = new TypescriptTypeConfig { OptionalDefaultValues = false };
        Ts(off, Request)["PageDto.ts"].ShouldContain("    take: number;\n");
    }

    // ---------------------------------------------------------------- member filter

    private const string Options = """
        using System;
        namespace Shop
        {
            [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)] public sealed class PublicAttribute : Attribute;
            public class PortalOptions
            {
                [Public] public string Title { get; set; } = "";
                public string ApiKey { get; set; } = "";
                [Public] public ThemeOptions Theme { get; set; } = new();
            }
            public class ThemeOptions { [Public] public string Color { get; set; } = ""; public string Secret { get; set; } = ""; }
            public class OrderDto { public string Number { get; set; } = ""; }
        }
        """;

    [TestMethod]
    public void Members_are_filtered_by_attribute_per_generation_or_type()
    {
        // generation: every type, any spelling of the attribute name
        foreach (var name in new[] { "Public", "PublicAttribute", "Shop.PublicAttribute", "Shop.Public" })
        {
            var files = Ts(Config(c => c.IncludeMembersWithAttributes = [name]), Options);
            files["PortalOptions.ts"].ShouldEndWith("export interface PortalOptions {\n    title: string;\n    theme: ThemeOptions;\n}\n", customMessage: name);
            files["ThemeOptions.ts"].ShouldEndWith("export interface ThemeOptions {\n    color: string;\n}\n", customMessage: name);
            files["OrderDto.ts"].ShouldEndWith("export interface OrderDto {\n}\n", customMessage: name);
        }

        // per type: only there; an empty list turns the generation's filter off; Include() brings a member back
        var perType = Config();
        perType.Types["Shop.PortalOptions"] = new TypescriptTypeConfig { IncludeMembersWithAttributes = ["Public"] };
        var typed = Ts(perType, Options);
        typed["PortalOptions.ts"].ShouldNotContain("apiKey");
        typed["ThemeOptions.ts"].ShouldContain("secret");
        var mixed = Config(c => c.IncludeMembersWithAttributes = ["Public"]);
        mixed.Types["Shop.OrderDto"] = new TypescriptTypeConfig { IncludeMembersWithAttributes = [] };
        mixed.Types["Shop.ThemeOptions"] = new TypescriptTypeConfig();
        mixed.Types["Shop.ThemeOptions"].Member("Secret").Ignore = false;
        var files2 = Ts(mixed, Options);
        files2["OrderDto.ts"].ShouldContain("    number: string;\n");
        files2["ThemeOptions.ts"].ShouldContain("    color: string;\n    secret: string;\n");
    }

    // ---------------------------------------------------------------- declared unions

    private const string Places = """
        using System;
        namespace Shop
        {
            public abstract class Place { public Guid Id { get; set; } }
            public class AddressPlace : Place { public string Street { get; set; } = ""; }
            public class CountryPlace : Place { public string Iso { get; set; } = ""; }
            public class PortPlace : Place { public string Code { get; set; } = ""; }
            public abstract class Middle : Place { }
            public class Margin { public Place? Origin { get; set; } public Vehicle Vehicle { get; set; } = new(); }
            public class Vehicle { public string Name { get; set; } = ""; }
            public class Truck : Vehicle { public int Axles { get; set; } }
        }
        """;

    [TestMethod]
    public void Derived_types_declared_in_the_configuration_become_a_union()
    {
        var config = Config();
        config.Types["Shop.Place"] = new TypescriptTypeConfig { DerivedTypes = ["*"] };
        var files = Ts(config, Places);

        files["Place.ts"].ShouldEndWith(Lf("""
            import type { AddressPlace } from './AddressPlace';
            import type { CountryPlace } from './CountryPlace';
            import type { PortPlace } from './PortPlace';

            export interface PlaceBase {
                id: string;
            }
            export type Place = AddressPlace | CountryPlace | PortPlace;

            """));
        files["AddressPlace.ts"].ShouldEndWith("export interface AddressPlace extends PlaceBase {\n    street: string;\n}\n");
        files["Margin.ts"].ShouldContain("    origin: Place | null;\n");

        // without the setting: unchanged
        Ts(Config(), Places)["Place.ts"].ShouldEndWith("export interface Place {\n    id: string;\n}\n");
    }

    [TestMethod]
    public void Declared_union_with_discriminator_values_and_a_concrete_base_from_json_and_fluent()
    {
        const string json = """
            {
              "Typescript": [
                {
                  "OutputPath": "ts",
                  "IncludeNamespaces": [ "Shop" ],
                  "Layout": "Flat",
                  "Barrel": false,
                  "Types": {
                    "Shop.Place": { "DerivedTypes": [ "PortPlace=port", "AddressPlace=address" ], "Discriminator": "kind" },
                    "Shop.Vehicle": { "DerivedTypes": [ "Truck" ] }
                  }
                }
              ]
            }
            """;
        var fromJson = TypescriptGenerator.Generate(CompileWithInTreeAttributes(Places), JsonConvert.DeserializeObject<MainConfig>(json)!.Typescript!.Single()).Files;

        const string fluent = """
            using Nextended.Core.CodeGen;
            using Nextended.Core.Enums;
            using Shop;
            public sealed class Ts : CodeGenConfig
            {
                public Ts()
                {
                    Typescript(t => t
                        .OutputPath("ts")
                        .IncludeNamespaces("Shop")
                        .Layout(TypescriptLayout.Flat)
                        .Barrel(false)
                        .Type<Place>(p => p.DerivedType<PortPlace>("port").DerivedType(typeof(AddressPlace), "address").Discriminator("kind"))
                        .Type<Vehicle>(v => v.DerivedTypes("Truck")));
                }
            }
            """;
        var compilation = CompileWithInTreeAttributes(Places, fluent);
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var fromFluent = TypescriptGenerator.Generate(compilation, TypescriptGenerator.FluentGenerations(compilation).Single()).Files;

        fromJson["Place.ts"].ShouldEndWith("export type Place = PortPlace | AddressPlace;\n");
        fromJson["PortPlace.ts"].ShouldEndWith("export interface PortPlace extends PlaceBase {\n    kind: 'port';\n    code: string;\n}\n");
        fromJson["AddressPlace.ts"].ShouldContain("    kind: 'address';\n");
        // a base that can be instantiated is a member of its union
        fromJson["Vehicle.ts"].ShouldEndWith("export interface VehicleBase {\n    name: string;\n}\nexport type Vehicle = VehicleBase | Truck;\n");
        fromJson["Margin.ts"].ShouldContain("    vehicle: Vehicle;\n");
        fromFluent.ShouldBe(fromJson);
    }

    [TestMethod]
    public void Declared_union_of_generated_dtos_names_the_entities_or_the_dtos()
    {
        const string entities = """
            using System;
            using Nextended.Core.Attributes;
            namespace Shop.Domain
            {
                [AutoGenerateDto] public abstract class Place { public Guid Id { get; set; } }
                [AutoGenerateDto] public class AddressPlace : Place { public string Street { get; set; } = ""; }
                [AutoGenerateDto] public class PortPlace : Place { public string Code { get; set; } = ""; }
                [AutoGenerateDto] public class Margin { public Place? Origin { get; set; } }
            }
            """;
        // the union declared on the entity (DtoConfig<T>.Typescript) with entity names
        const string profile = """
            using Nextended.Core.CodeGen;
            public sealed class PlaceConfig : DtoConfig<Shop.Domain.Place>
            {
                public PlaceConfig() { Typescript(t => t.DerivedType<Shop.Domain.AddressPlace>().DerivedType<Shop.Domain.PortPlace>()); }
            }
            """;
        foreach (var withProfile in new[] { true, false })
        {
            var compilation = withProfile ? CompileWithInTreeAttributes(entities, profile) : CompileWithInTreeAttributes(entities);
            var dtoGenerator = new Generators.DtoGeneration.DtoGenerator(new DtoGenerationConfig { Namespace = "Shop.Contracts", CreateFileHeaders = false });
            var dtoFiles = dtoGenerator.Execute(compilation).ToList();
            var config = new TypescriptGenerationConfig { FromGeneratedDtos = true, Barrel = false, Layout = TypescriptLayout.Flat };
            if (!withProfile)
                config.Types["Shop.Contracts.PlaceDto"] = new TypescriptTypeConfig { DerivedTypes = ["*PlaceDto"] };
            var files = TypescriptGenerator.Generate(compilation, config, dtoFiles, dtoGenerator.GeneratedDtos()).Files;

            files["PlaceDto.ts"].ShouldEndWith("export type PlaceDto = AddressPlaceDto | PortPlaceDto;\n", customMessage: withProfile.ToString());
            files["MarginDto.ts"].ShouldContain("    origin: PlaceDto | null;\n");
        }
    }
}
