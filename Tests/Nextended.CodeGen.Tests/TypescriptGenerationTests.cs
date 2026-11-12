using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Generators.TypescriptGeneration;
using Shouldly;
using static Nextended.CodeGen.Tests.TestCompilation;

namespace Nextended.CodeGen.Tests;

/// <summary>The TypeScript generation: selection, serializer rules, mapping table, layout, configuration channels, writing.</summary>
[TestClass]
public class TypescriptGenerationTests
{
    internal static SortedDictionary<string, string> Ts(TypescriptGenerationConfig config, params string[] sources)
        => TypescriptGenerator.Generate(CompileWithInTreeAttributes(sources), config).Files;

    private static string Body(string content) => content.Substring(TypescriptGenerator.Header.Length).TrimStart('\n');

    private static TypescriptGenerationConfig Config(Action<TypescriptGenerationConfig>? configure = null)
    {
        var config = new TypescriptGenerationConfig { IncludeNamespaces = ["Shop"], Barrel = false };
        configure?.Invoke(config);
        return config;
    }

    private const string Shop = """
        using System;
        using System.Collections.Generic;
        using System.Text.Json;
        using System.Text.Json.Serialization;

        namespace Shop.Contracts;

        public class OrderDto
        {
            public Guid Id { get; set; }
            public string Number { get; set; } = "";
            public string? Note { get; set; }
            public decimal Total { get; set; }
            public long Big { get; set; }
            public int? Count { get; set; }
            public double Ratio { get; set; }
            public bool Paid { get; set; }
            public char Letter { get; set; }
            public DateTime Created { get; set; }
            public DateTimeOffset? Shipped { get; set; }
            public DateOnly Day { get; set; }
            public TimeOnly At { get; set; }
            public TimeSpan Took { get; set; }
            public Uri? Link { get; set; }
            public OrderState State { get; set; }
            public List<OrderLineDto> Lines { get; set; } = [];
            public OrderLineDto[] LineArray { get; set; } = [];
            public IEnumerable<int?> Numbers { get; set; } = [];
            public HashSet<string> Set { get; set; } = [];
            public Dictionary<OrderState, int> Counts { get; set; } = [];
            public IReadOnlyDictionary<string, string?> Tags { get; set; } = new Dictionary<string, string?>();
            public IDictionary<Guid, OrderLineDto> ById { get; set; } = new Dictionary<Guid, OrderLineDto>();
            public Dictionary<long, string> ByNumber { get; set; } = [];
            public byte[] Blob { get; set; } = [];
            public object? Extra { get; set; }
            public JsonElement Json { get; set; }
            public KeyValuePair<string, int> Pair { get; set; }
            public List<KeyValuePair<string, OrderState>> Pairs { get; set; } = [];
            [JsonPropertyName("no")] public string URLValue { get; set; } = "";
            [JsonPropertyName("my-name")] public string Dashed { get; set; } = "";
            [JsonIgnore] public string Secret { get; set; } = "";
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string Kept { get; set; } = "";
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Maybe { get; set; }
            public string this[int i] => "";
            public static string Static { get; set; } = "";
            internal string Internal { get; set; } = "";
            public string WriteOnly { set { } }
        }

        public class OrderLineDto
        {
            public string Sku { get; set; } = "";
            public OrderDto? Order { get; set; }
        }

        public enum OrderState { Draft = 0, Placed = 5, Cancelled = -1 }
        """;

    [TestMethod]
    public void Mapping_table_nullability_and_serializer_names()
    {
        var files = Ts(Config(), Shop);

        files.Keys.ShouldBe(["Shop/Contracts/OrderDto.ts", "Shop/Contracts/OrderLineDto.ts", "Shop/Contracts/OrderState.ts"]);
        Body(files["Shop/Contracts/OrderDto.ts"]).ShouldBe("""
            import type { OrderLineDto } from './OrderLineDto';
            import type { OrderState } from './OrderState';

            export interface OrderDto {
                id: string;
                number: string;
                note: string | null;
                total: number;
                big: number;
                count: number | null;
                ratio: number;
                paid: boolean;
                letter: string;
                created: string;
                shipped: string | null;
                day: string;
                at: string;
                took: string;
                link: string | null;
                state: OrderState;
                lines: OrderLineDto[];
                lineArray: OrderLineDto[];
                numbers: (number | null)[];
                set: string[];
                counts: Partial<Record<keyof typeof OrderState, number>>;
                tags: Record<string, string | null>;
                byId: Record<string, OrderLineDto>;
                byNumber: Record<number, string>;
                blob: string;
                extra: unknown;
                json: unknown;
                pair: { key: string; value: number };
                pairs: { key: string; value: OrderState }[];
                no: string;
                'my-name': string;
                kept: string;
                maybe?: string | null;
            }

            """.Replace("\r\n", "\n"));
        Body(files["Shop/Contracts/OrderLineDto.ts"]).ShouldBe("""
            import type { OrderDto } from './OrderDto';

            export interface OrderLineDto {
                sku: string;
                order: OrderDto | null;
            }

            """.Replace("\r\n", "\n"));
        Body(files["Shop/Contracts/OrderState.ts"]).ShouldBe("""
            export enum OrderState {
                Draft = 0,
                Placed = 5,
                Cancelled = -1,
            }

            """.Replace("\r\n", "\n"));
        files.Values.ShouldAllBe(f => f.StartsWith(TypescriptGenerator.Header) && !f.Contains('\r'));
    }

    [TestMethod]
    public void Options_long_and_decimal_as_string_aliases_optional_nullable_and_names_as_is()
    {
        var files = Ts(Config(c =>
        {
            c.LongAsString = true;
            c.DecimalAsString = true;
            c.TypeAliases = true;
            c.OptionalNullable = true;
            c.PropertyNaming = TypescriptNaming.AsIs;
            c.Barrel = true;
        }), Shop);

        var order = files["Shop/Contracts/OrderDto.ts"];
        order.ShouldContain("import type { DateOnly, DateTime, DateTimeOffset, Guid, TimeOnly, TimeSpan } from '../../scalars';");
        order.ShouldContain("    Id: Guid;\n");
        order.ShouldContain("    Total: string;\n");
        order.ShouldContain("    Big: string;\n");
        order.ShouldContain("    Note?: string | null;\n");
        order.ShouldContain("    Count?: number | null;\n");
        order.ShouldContain("    Shipped?: DateTimeOffset | null;\n");
        order.ShouldContain("    ById: Record<Guid, OrderLineDto>;\n".Replace("Guid,", "string,"));
        order.ShouldContain("    no: string;\n"); // [JsonPropertyName] wins over the naming
        files["scalars.ts"].ShouldContain("export type Guid = string;\n");
        files["index.ts"].ShouldContain("export * from './scalars';\n");
        files["index.ts"].ShouldContain("export * from './Shop/Contracts/OrderDto';\n");
    }

    [TestMethod]
    public void Camel_case_follows_System_Text_Json()
    {
        foreach (var name in new[] { "URLValue", "ID", "Id", "IOStream", "already", "A", "ABC", "XMLHttpRequest", "Name", "X1", "PICKUPDay" })
            TypescriptNames.CamelCase(name).ShouldBe(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(name), name);
    }

    private const string Enums = """
        using System;
        using System.Collections.Generic;
        using System.Runtime.Serialization;
        using System.Text.Json.Serialization;

        namespace Shop.Contracts;

        public enum State { Draft = 1, Done = 4 }

        [Flags]
        public enum Channel { None = 0, Mail = 1, Push = 2, All = Mail | Push }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum Level { Low, [JsonStringEnumMemberName("very-high")] VeryHigh, [EnumMember(Value = "max")] Max }

        public enum Big : long { Huge = 5000000000 }

        public class Holder
        {
            public State State { get; set; }
            [JsonConverter(typeof(JsonStringEnumConverter))] public State StateName { get; set; }
            [JsonConverter(typeof(JsonStringEnumConverter))] public State? MaybeName { get; set; }
            public Channel Channels { get; set; }
            public Level Level { get; set; }
            public Level? MaybeLevel { get; set; }
            public Big Big { get; set; }
            public Dictionary<State, string> ByState { get; set; } = [];
            public Dictionary<Level, int> ByLevel { get; set; } = [];
            public List<State> States { get; set; } = [];
        }
        """;

    [TestMethod]
    public void Enums_as_numbers_with_explicit_values()
    {
        var files = Ts(Config(c => c.EnumValues = TypescriptEnumValues.Number), Enums);

        Body(files["Shop/Contracts/State.ts"]).ShouldBe("export enum State {\n    Draft = 1,\n    Done = 4,\n}\n");
        Body(files["Shop/Contracts/Channel.ts"]).ShouldBe("export enum Channel {\n    None = 0,\n    Mail = 1,\n    Push = 2,\n    All = 3,\n}\n");
        Body(files["Shop/Contracts/Level.ts"]).ShouldBe("export enum Level {\n    Low = 'Low',\n    VeryHigh = 'very-high',\n    Max = 'max',\n}\n");
        Body(files["Shop/Contracts/Big.ts"]).ShouldBe("export enum Big {\n    Huge = 5000000000,\n}\n");
        var holder = files["Shop/Contracts/Holder.ts"];
        holder.ShouldContain("    state: State;\n");
        holder.ShouldContain("    stateName: keyof typeof State;\n");
        holder.ShouldContain("    maybeName: keyof typeof State | null;\n");
        holder.ShouldContain("    channels: Channel;\n");
        holder.ShouldContain("    level: Level;\n");
        holder.ShouldContain("    maybeLevel: Level | null;\n");
        holder.ShouldContain("    byState: Partial<Record<keyof typeof State, string>>;\n");
        holder.ShouldContain("    byLevel: Partial<Record<'Low' | 'very-high' | 'max', number>>;\n");
        holder.ShouldContain("    states: State[];\n");
    }

    [TestMethod]
    public void Enums_as_strings_and_const_style()
    {
        var files = Ts(Config(c =>
        {
            c.EnumValues = TypescriptEnumValues.String;
            c.EnumStyle = TypescriptEnumStyle.Const;
        }), Enums);

        Body(files["Shop/Contracts/State.ts"]).ShouldBe("""
            export const State = {
                Draft: 'Draft',
                Done: 'Done',
            } as const;
            export type State = (typeof State)[keyof typeof State];

            """.Replace("\r\n", "\n"));
        // [Flags] keeps its numbers; a string converter writes combinations as "Mail, Push"
        Body(files["Shop/Contracts/Channel.ts"]).ShouldContain("    All: 3,\n");
        files["Shop/Contracts/Holder.ts"].ShouldContain("    channels: string;\n");
        files["Shop/Contracts/Holder.ts"].ShouldContain("    stateName: State;\n");

        // Default writes numbers, like Number
        Ts(Config(), Enums)["Shop/Contracts/State.ts"].ShouldContain("    Draft = 1,\n");
    }

    private const string Hierarchy = """
        using System;
        using System.Collections.Generic;
        using System.Text.Json.Serialization;

        namespace Shop.Base
        {
            public abstract class Entity<TKey> { public TKey Id { get; set; } = default!; public DateTime Created { get; set; } }
            public class Audited<TKey> : Entity<TKey> { public string? By { get; set; } }
            public class Hidden { public string Secret { get; set; } = ""; }
        }

        namespace Shop.Contracts
        {
            using Shop.Base;

            public interface INamed { string Name { get; } }

            public class Product : Audited<Guid>, INamed
            {
                public string Name { get; set; } = "";
                public Product? Parent { get; set; }
                public List<Product> Children { get; set; } = [];
            }

            public class Box<T> where T : Entity<Guid>, INamed
            {
                public T Item { get; set; } = default!;
                public T? Maybe { get; set; }
                public List<T> Items { get; set; } = [];
            }

            public class ProductBox : Box<Product> { }

            public class FromHidden : Hidden { public int Own { get; set; } }

            public class Overrides : Base2 { public override string Name { get; set; } = ""; public new int Count { get; set; } }
            public class Base2 { public virtual string Name { get; set; } = ""; public string? Count { get; set; } }

            [JsonPolymorphic]
            [JsonDerivedType(typeof(Animal), "animal")]
            [JsonDerivedType(typeof(Cat), 1)]
            [JsonDerivedType(typeof(Dog))]
            public class Animal { public string Name { get; set; } = ""; }
            public class Cat : Animal { public bool Purrs { get; set; } }
            public class Dog : Animal { public Animal? Friend { get; set; } }
            public class Zoo { public List<Animal> Animals { get; set; } = []; public Cat? Favourite { get; set; } }
        }
        """;

    [TestMethod]
    public void Inheritance_generics_constraints_and_circular_references()
    {
        var files = Ts(Config(c => c.ExcludeNamespaces = ["Shop.Base.Hidden"]), Hierarchy);

        Body(files["Shop/Contracts/Product.ts"]).ShouldBe("""
            import type { Audited } from '../Base/Audited';

            export interface Product extends Audited<string> {
                name: string;
                parent: Product | null;
                children: Product[];
            }

            """.Replace("\r\n", "\n"));
        Body(files["Shop/Base/Audited.ts"]).ShouldContain("export interface Audited<TKey> extends Entity<TKey> {\n    by: string | null;\n}");
        Body(files["Shop/Base/Entity.ts"]).ShouldContain("export interface Entity<TKey> {\n    id: TKey;\n    created: string;\n}");
        var box = Body(files["Shop/Contracts/Box.ts"]);
        box.ShouldContain("export interface Box<T extends Entity<string>> {"); // INamed is not generated: no constraint for it
        box.ShouldContain("    item: T;\n    maybe: T | null;\n    items: T[];\n");
        Body(files["Shop/Contracts/ProductBox.ts"]).ShouldContain("export interface ProductBox extends Box<Product> {\n}");
        // overrides stay with the base, a `new` member replaces it
        Body(files["Shop/Contracts/Overrides.ts"]).ShouldContain("export interface Overrides extends Omit<Base2, 'count'> {\n    count: number;\n}");
        files.Keys.ShouldNotContain("Shop/Contracts/INamed.ts");
    }

    [TestMethod]
    public void A_base_type_that_is_not_generated_is_flattened()
    {
        var files = Ts(Config(c => c.ExcludeTypes = ["Shop.Base.Hidden", "Audited`1"]), Hierarchy);

        Body(files["Shop/Contracts/FromHidden.ts"]).ShouldContain("export interface FromHidden {\n    secret: string;\n    own: number;\n}");
        Body(files["Shop/Contracts/Product.ts"]).ShouldContain("export interface Product extends Entity<string> {\n    by: string | null;\n    name: string;");
        files.Keys.ShouldNotContain("Shop/Base/Hidden.ts");

        // without referenced types: only the selected ones, the rest unknown, bases flattened
        var only = Ts(Config(c => { c.IncludeNamespaces = []; c.IncludeTypes = ["Shop.Contracts.Zoo", "Product"]; c.IncludeReferencedTypes = false; }), Hierarchy);
        only.Keys.ShouldBe(["Shop/Contracts/Product.ts", "Shop/Contracts/Zoo.ts"]);
        only["Shop/Contracts/Zoo.ts"].ShouldContain("    animals: unknown[];\n    favourite: unknown;\n");
        only["Shop/Contracts/Product.ts"].ShouldContain("export interface Product {\n    id: string;\n    created: string;\n    by: string | null;\n    name: string;");
    }

    [TestMethod]
    public void Polymorphic_types_become_discriminated_unions()
    {
        var files = Ts(Config(), Hierarchy);

        Body(files["Shop/Contracts/Animal.ts"]).ShouldBe("""
            import type { Cat } from './Cat';
            import type { Dog } from './Dog';

            export interface AnimalBase {
                name: string;
            }
            export type Animal = (AnimalBase & { $type: 'animal' }) | Cat | Dog;

            """.Replace("\r\n", "\n"));
        Body(files["Shop/Contracts/Cat.ts"]).ShouldContain("export interface Cat extends AnimalBase {\n    $type: 1;\n    purrs: boolean;\n}");
        Body(files["Shop/Contracts/Dog.ts"]).ShouldContain("export interface Dog extends AnimalBase {\n    friend: Animal | null;\n}");
        Body(files["Shop/Contracts/Dog.ts"]).ShouldContain("import type { Animal, AnimalBase } from './Animal';");
        Body(files["Shop/Contracts/Zoo.ts"]).ShouldContain("    animals: Animal[];\n    favourite: Cat | null;\n");
    }

    private const string Collisions = """
        namespace App.Sales { public class Address { public string? Street { get; set; } } public class Order { public Address? Ship { get; set; } public App.Crm.Address? Bill { get; set; } } }
        namespace App.Crm { public class Address { public string? Company { get; set; } } public class Page { } public class Page<T> { public T[] Items { get; set; } = []; } public class Address2 { } }
        """;

    [TestMethod]
    public void Namespace_folders_relative_imports_and_same_names()
    {
        var files = Ts(new TypescriptGenerationConfig { IncludeNamespaces = ["App"], RootNamespace = "App" }, Collisions);

        files.Keys.ShouldBe(["Crm/Address.ts", "Crm/Address2.ts", "Crm/Page.ts", "Crm/Page1.ts", "Sales/Address.ts", "Sales/Order.ts", "index.ts"]);
        Body(files["Sales/Order.ts"]).ShouldBe("""
            import type { Address as CrmAddress } from '../Crm/Address';
            import type { Address } from './Address';

            export interface Order {
                ship: Address | null;
                bill: CrmAddress | null;
            }

            """.Replace("\r\n", "\n"));
        Body(files["Crm/Page1.ts"]).ShouldStartWith("export interface Page1<T> {");
        Body(files["index.ts"]).ShouldBe("""
            export type { Address as CrmAddress } from './Crm/Address';
            export * from './Crm/Address2';
            export * from './Crm/Page';
            export * from './Crm/Page1';
            export type { Address as SalesAddress } from './Sales/Address';
            export * from './Sales/Order';

            """.Replace("\r\n", "\n"));
    }

    [TestMethod]
    public void Flat_layout_gives_same_names_their_namespace()
    {
        var files = Ts(new TypescriptGenerationConfig { IncludeNamespaces = ["App"], RootNamespace = "App", Layout = TypescriptLayout.Flat, Barrel = false }, Collisions);

        files.Keys.ShouldBe(["Address2.ts", "CrmAddress.ts", "Order.ts", "Page.ts", "Page1.ts", "SalesAddress.ts"]);
        Body(files["Order.ts"]).ShouldContain("import type { CrmAddress } from './CrmAddress';\nimport type { SalesAddress } from './SalesAddress';");
        Body(files["Order.ts"]).ShouldContain("    ship: SalesAddress | null;\n    bill: CrmAddress | null;\n");
    }

    [TestMethod]
    public void Output_is_deterministic()
    {
        var a = Ts(Config(), Hierarchy, Shop.Replace("namespace Shop.Contracts;", "namespace Shop.More;"));
        var b = Ts(Config(), Hierarchy, Shop.Replace("namespace Shop.Contracts;", "namespace Shop.More;"));
        a.ShouldBe(b);
    }

    private const string Selection = """
        using Nextended.Core.Attributes;

        namespace Lib.Contracts { public class A { } public class ADto { } public class BDto { } public interface IShape { int Size { get; } } public class Square : IShape { public int Size { get; set; } } }
        namespace Lib.Internal { public class Hidden { } [GenerateTypescript(Name = "Visible")] public class Marked { } public class Base { } public class Derived : Base { } public class Derived2 : Derived { } }
        namespace Lib.Ignored { [TypescriptIgnore] public class Skipped { } public class Other { } }
        namespace Lib.Services { public static class Helpers { } public class TheAttribute : System.Attribute { } public delegate void D(); }
        """;

    [TestMethod]
    public void Selection_by_namespace_name_base_type_attribute_and_exclusions()
    {
        string[] Names(TypescriptGenerationConfig c) => Ts(c, Selection).Keys.Where(k => k != "index.ts").Select(k => Path.GetFileNameWithoutExtension(k)).ToArray();

        Names(new TypescriptGenerationConfig { IncludeNamespaces = ["Lib.Contracts"] }).ShouldBe(["A", "ADto", "BDto", "Square", "Visible"]);
        Names(new TypescriptGenerationConfig { IncludeTypes = ["*Dto"] }).ShouldBe(["ADto", "BDto", "Visible"]);
        Names(new TypescriptGenerationConfig { IncludeTypes = ["Lib.Contracts.IShape"] }).ShouldBe(["IShape", "Visible"]);
        Names(new TypescriptGenerationConfig { IncludeBaseTypes = ["Lib.Internal.Base"] }).ShouldBe(["Base", "Derived", "Derived2", "Visible"]);
        Names(new TypescriptGenerationConfig { IncludeBaseTypes = ["Lib.Internal.Base"], ExcludeBaseTypes = ["Lib.Internal.Derived"], IncludeReferencedTypes = false }).ShouldBe(["Derived", "Visible"]);
        Names(new TypescriptGenerationConfig { IncludeNamespaces = ["Lib"], ExcludeNamespaces = ["Lib.Internal"], ExcludeTypes = ["Lib.Contracts.A*"] }).ShouldBe(["BDto", "Square", "Other"]);
    }

    [TestMethod]
    public void Types_of_referenced_assemblies_by_namespace()
    {
        // System.Text.Json's options class is a public type of a referenced assembly
        var files = Ts(new TypescriptGenerationConfig { IncludeTypes = ["System.Text.Json.JsonDocumentOptions"], Barrel = false }, "namespace X;");
        files.Keys.ShouldBe(["System/Text/Json/JsonDocumentOptions.ts"]);
        files.Values.Single().ShouldContain("    allowTrailingCommas: boolean;\n");
    }

    [TestMethod]
    public void Type_map_with_imports_and_type_arguments()
    {
        const string source = """
            namespace Volo.Abp.Application.Dtos { public class PagedResultDto<T> { public T[] Items { get; set; } = []; } public class ListResultDto<T> { } }
            namespace Shop.Contracts
            {
                using Volo.Abp.Application.Dtos;
                public class Item { }
                public class Lists
                {
                    public PagedResultDto<Item> Page { get; set; } = new();
                    public ListResultDto<Item>? List { get; set; }
                    public System.Version? Version { get; set; }
                    public Money Price { get; set; }
                }
                public struct Money { public decimal Amount { get; set; } }
            }
            """;
        var files = Ts(Config(c => c.TypeMap = new()
        {
            ["Volo.Abp.Application.Dtos.PagedResultDto<T>"] = "PagedResult from './abp'",
            ["Volo.Abp.Application.Dtos.ListResultDto"] = "Array<unknown>",
            ["Shop.Contracts.Money"] = "number"
        }), source);

        var lists = Body(files["Shop/Contracts/Lists.ts"]);
        lists.ShouldContain("import type { PagedResult } from '../../abp';");
        lists.ShouldContain("    page: PagedResult<Item>;\n    list: Array<unknown> | null;\n    version: string | null;\n    price: number;\n");
        files.Keys.ShouldNotContain("Shop/Contracts/Money.ts");
        files.Keys.ShouldNotContain("Volo/Abp/Application/Dtos/PagedResultDto.ts");
    }

    // ---------------------------------------------------------------- configuration channels

    private const string Plain = """
        using System;
        namespace Shop.Contracts
        {
            public class CustomerDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string? Secret { get; set; } public string? Mail { get; set; } public string Raw { get; set; } = ""; }
            public class Backoffice { }
        }
        """;

    private const string Attributed = """
        using System;
        using Nextended.Core.Attributes;
        namespace Shop.Contracts
        {
            [GenerateTypescript(Name = "Customer")]
            public class CustomerDto { public Guid Id { get; set; } public string Name { get; set; } = ""; [TypescriptIgnore] public string? Secret { get; set; } [TypescriptProperty(Name = "email", Optional = true)] public string? Mail { get; set; } [TypescriptProperty(Type = "unknown")] public string Raw { get; set; } = ""; }
            [TypescriptIgnore] public class Backoffice { }
        }
        """;

    private const string Fluent = """
        using Nextended.Core.CodeGen;
        using Shop.Contracts;
        public sealed class Ts : CodeGenConfig
        {
            public Ts()
            {
                Typescript(t => t
                    .OutputPath("ts")
                    .IncludeNamespaces("Shop.Contracts")
                    .LongAsString()
                    .Exclude<Backoffice>()
                    .Type<CustomerDto>(c => c
                        .Name("Customer")
                        .Ignore(x => x.Secret)
                        .Property(x => x.Raw).Type("unknown"))
                    .Type("Shop.Contracts.CustomerDto", c => c.Property("Mail").Name("email").Optional()));
            }
        }
        """;

    private const string Json = """
        {
          "Typescript": [
            {
              "OutputPath": "ts",
              "IncludeNamespaces": [ "Shop.Contracts" ],
              "LongAsString": true,
              "ExcludeTypes": [ "Shop.Contracts.Backoffice" ],
              "Types": {
                "Shop.Contracts.CustomerDto": {
                  "Name": "Customer",
                  "Members": { "Secret": { "Ignore": true }, "Mail": { "Name": "email", "Optional": true }, "Raw": { "Type": "unknown" } }
                }
              }
            }
          ]
        }
        """;

    [TestMethod]
    public void Json_attributes_and_fluent_give_the_same_typescript()
    {
        var json = JsonConvert.DeserializeObject<MainConfig>(Json)!.Typescript!.Single();
        var fromJson = TypescriptGenerator.Generate(CompileWithInTreeAttributes(Plain), json).Files;

        var fromAttributes = TypescriptGenerator.Generate(CompileWithInTreeAttributes(Attributed),
            new TypescriptGenerationConfig { OutputPath = "ts", IncludeNamespaces = ["Shop.Contracts"], LongAsString = true }).Files;

        var compilation = CompileWithInTreeAttributes(Plain, Fluent);
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var fluent = TypescriptGenerator.FluentGenerations(compilation).Single();
        fluent.OutputPath.ShouldBe("ts");
        var fromFluent = TypescriptGenerator.Generate(compilation, fluent).Files;

        Body(fromJson["Shop/Contracts/Customer.ts"]).ShouldBe("""
            export interface Customer {
                id: string;
                name: string;
                email?: string | null;
                raw: unknown;
            }

            """.Replace("\r\n", "\n"));
        fromAttributes.ShouldBe(fromJson);
        fromFluent.ShouldBe(fromJson);
    }

    [TestMethod]
    public void Json_types_lose_to_attributes_fluent_wins_over_both()
    {
        const string attributed = """
            using Nextended.Core.Attributes;
            namespace Shop.Contracts
            {
                [GenerateTypescript(Name = "FromAttribute")]
                public class A { [TypescriptProperty(Name = "fromAttribute")] public string X { get; set; } = ""; [TypescriptIgnore] public string Y { get; set; } = ""; }
            }
            """;
        var json = new TypescriptGenerationConfig { IncludeNamespaces = ["Shop"], Barrel = false };
        json.Types["Shop.Contracts.A"] = new TypescriptTypeConfig { Name = "FromJson" };
        json.Types["Shop.Contracts.A"].Member("X").Name = "fromJson";
        json.Types["Shop.Contracts.A"].Member("Y").Ignore = false;
        Ts(json, attributed).ShouldContainKey("Shop/Contracts/FromAttribute.ts");
        Ts(json, attributed)["Shop/Contracts/FromAttribute.ts"].ShouldContain("    fromAttribute: string;\n}");

        // DtoConfig<T>.Typescript is fluent: it wins over the attribute (and brings Y back)
        const string profile = """
            using Nextended.Core.CodeGen;
            public sealed class AConfig : DtoConfig<Shop.Contracts.A>
            {
                public AConfig() { Typescript(t => t.Name("FromFluent").Property(x => x.X).Name("fromFluent")); Typescript(t => t.Property("Y").Include()); }
            }
            """;
        var files = Ts(json, attributed, profile);
        files.Keys.ShouldContain("Shop/Contracts/FromFluent.ts");
        files["Shop/Contracts/FromFluent.ts"].ShouldContain("    fromFluent: string;\n    y: string;\n}");
    }

    [TestMethod]
    public void Generated_dtos_are_included_with_renames_views_and_ignores()
    {
        const string entities = """
            using System;
            using System.Collections.Generic;
            using Nextended.Core.Attributes;

            namespace Shop.Domain;

            [AutoGenerateDto]
            [AutoGenerateDto(GeneratedClassName = "OrderRow", PropertiesToIgnore = new[] { "Lines" })]
            public class Order
            {
                public Guid Id { get; set; }
                [GenerationPropertySetting(PropertyName = "No")] public string Number { get; set; } = "";
                [IgnoreOnGeneration] public string? Internal { get; set; }
                public string? Note { get; set; }
                public List<OrderLine> Lines { get; set; } = [];
                public OrderState State { get; set; }
            }

            [AutoGenerateDto]
            public class OrderLine { public string Sku { get; set; } = ""; }

            [AutoGenerateDto]
            public enum OrderState { Draft = 1, Placed = 2 }
            """;
        const string partial = """
            namespace Shop.Contracts;
            public partial class OrderDto { public string HandWritten { get; set; } = ""; }
            """;
        const string profile = """
            using Nextended.Core.CodeGen;
            public sealed class OrderConfig : DtoConfig<Shop.Domain.Order>
            {
                public OrderConfig() { Typescript(t => t.Ignore("Note")); }
            }
            """;
        var compilation = CompileWithInTreeAttributes(entities, partial, profile);
        var dtoGenerator = new Generators.DtoGeneration.DtoGenerator(new DtoGenerationConfig { Namespace = "Shop.Contracts", CreateFileHeaders = false });
        var dtoFiles = dtoGenerator.Execute(compilation).ToList();

        var config = new TypescriptGenerationConfig { FromGeneratedDtos = true, Barrel = false };
        var files = TypescriptGenerator.Generate(compilation, config, dtoFiles, dtoGenerator.GeneratedDtos()).Files;

        files.Keys.ShouldBe(["Shop/Contracts/OrderDto.ts", "Shop/Contracts/OrderLineDto.ts", "Shop/Contracts/OrderRowDto.ts", "Shop/Contracts/OrderStateDto.ts"]);
        var order = Body(files["Shop/Contracts/OrderDto.ts"]);
        order.ShouldContain("    no: string;\n");
        order.ShouldContain("    lines: OrderLineDto[];\n");
        order.ShouldContain("    state: OrderStateDto;\n");
        order.ShouldContain("    handWritten: string;\n");
        order.ShouldNotContain("internal");
        order.ShouldNotContain("note"); // DtoConfig<Order>.Typescript reaches the DTO
        Body(files["Shop/Contracts/OrderRowDto.ts"]).ShouldNotContain("lines");
        Body(files["Shop/Contracts/OrderStateDto.ts"]).ShouldContain("    Draft = 1,\n    Placed = 2,\n");

        // the entities themselves are no DTOs: nothing without the option
        TypescriptGenerator.Generate(compilation, new TypescriptGenerationConfig(), dtoFiles, dtoGenerator.GeneratedDtos()).Files.ShouldBeEmpty();
    }

    [TestMethod]
    public void Source_files_with_syntax_errors_make_the_output_unstable()
    {
        TypescriptGenerator.Generate(CompileWithInTreeAttributes("namespace Shop.Contracts; public class A { public int X { get; set; }"), Config()).Unstable.ShouldBeTrue();
        TypescriptGenerator.Generate(CompileWithInTreeAttributes("namespace Shop.Contracts; public class A { public int X { get; set; } }"), Config()).Unstable.ShouldBeFalse();
    }

    // ---------------------------------------------------------------- writing

    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nextended-ts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [TestMethod]
    public void Writer_writes_changes_only_removes_stale_files_keeps_foreign_ones_and_checks()
    {
        var root = TempDir();
        try
        {
            File.WriteAllText(Path.Combine(root, "hand.ts"), "export {};\n");
            var files = new Dictionary<string, string> { ["a/A.ts"] = "a\n", ["B.ts"] = "b\n" };

            TypescriptWriter.Write(root, files, check: false).ShouldBe(["a/A.ts", "B.ts"]);
            File.ReadAllText(Path.Combine(root, "a", "A.ts")).ShouldBe("a\n");
            var stamp = File.GetLastWriteTimeUtc(Path.Combine(root, "B.ts"));
            TypescriptWriter.Write(root, files, check: false).ShouldBeEmpty();
            TypescriptWriter.Write(root, files, check: true).ShouldBeEmpty();
            File.GetLastWriteTimeUtc(Path.Combine(root, "B.ts")).ShouldBe(stamp);
            File.WriteAllText(Path.Combine(root, "a", "A.ts"), "a\r\n"); // a checkout with core.autocrlf
            TypescriptWriter.Write(root, files, check: true).ShouldBeEmpty();

            File.WriteAllText(Path.Combine(root, "B.ts"), "edited\n");
            TypescriptWriter.Write(root, files, check: true).ShouldBe(["B.ts"]);
            File.ReadAllText(Path.Combine(root, "B.ts")).ShouldBe("edited\n"); // check mode writes nothing

            // a.ts is no longer generated: removed with its folder; hand.ts is not in the manifest and stays
            TypescriptWriter.Write(root, new Dictionary<string, string> { ["B.ts"] = "b\n" }, check: true).ShouldBe(["B.ts", "a/A.ts"]);
            TypescriptWriter.Write(root, new Dictionary<string, string> { ["B.ts"] = "b\n" }, check: false);
            Directory.Exists(Path.Combine(root, "a")).ShouldBeFalse();
            File.Exists(Path.Combine(root, "hand.ts")).ShouldBeTrue();
            File.ReadAllText(Path.Combine(root, TypescriptWriter.ManifestName)).ShouldEndWith("\nB.ts\n");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    // ---------------------------------------------------------------- the source generator

    private sealed class Text(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }

    private sealed class Options(Dictionary<string, string> global) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(global);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Values([]);
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Values([]);

        private sealed class Values(Dictionary<string, string> values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
        }
    }

    private static GeneratorDriverRunResult RunSourceGenerator(Compilation compilation, string projectDir, IEnumerable<AdditionalText>? texts = null, bool check = false)
    {
        var options = new Dictionary<string, string> { ["build_property.projectdir"] = projectDir + Path.DirectorySeparatorChar };
        if (check)
            options["build_property.NextendedCodeGenTypescriptCheck"] = "true";
        return CSharpGeneratorDriver.Create([new MainGenerator().AsSourceGenerator()], texts ?? [], (CSharpParseOptions)compilation.SyntaxTrees.First().Options, new Options(options))
            .RunGenerators(compilation).GetRunResult();
    }

    [TestMethod]
    public void Without_typescript_configuration_nothing_changes()
    {
        // the regression of the old attempt: a config without "Typescript" (or with null) must not add sources nor break the build
        var dir = TempDir();
        try
        {
            var compilation = CompileWithInTreeAttributes(Attributed, "namespace Shop.Domain { [Nextended.Core.Attributes.AutoGenerateDto] public class Order { public int X { get; set; } } }");
            var json = """{ "DtoGeneration": { "Namespace": "Shop.Api" } }""";
            var baseline = RunSourceGenerator(compilation, dir, [new Text(Path.Combine(dir, "codegen.json"), json)]);
            foreach (var variant in new[] { json, """{ "DtoGeneration": { "Namespace": "Shop.Api" }, "Typescript": null }""", """{ "DtoGeneration": { "Namespace": "Shop.Api" }, "Typescript": [] }""" })
            {
                var result = RunSourceGenerator(compilation, dir, [new Text(Path.Combine(dir, "codegen.json"), variant)]);
                result.GeneratedTrees.Select(t => (Path.GetFileName(t.FilePath), t.ToString())).ShouldBe(baseline.GeneratedTrees.Select(t => (Path.GetFileName(t.FilePath), t.ToString())));
                result.Diagnostics.Where(d => d.Id.StartsWith("NCG03")).ShouldBeEmpty();
                result.GeneratedTrees.ShouldNotContain(t => t.FilePath.Contains(".ts"));
            }
            baseline.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ShouldContain("OrderDto.g.cs");
            Directory.GetFileSystemEntries(dir).ShouldBeEmpty(); // [GenerateTypescript] alone writes nothing
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [TestMethod]
    public void The_source_generator_writes_typescript_from_json_and_fluent_and_checks_it()
    {
        var dir = TempDir();
        try
        {
            // fluent: "ts" below the project directory, never added to the compilation
            var compilation = CompileWithInTreeAttributes(Plain, Fluent);
            var result = RunSourceGenerator(compilation, dir);
            result.GeneratedTrees.ShouldBeEmpty();
            result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty();
            File.ReadAllText(Path.Combine(dir, "ts", "Shop", "Contracts", "Customer.ts")).ShouldContain("export interface Customer {");
            File.Exists(Path.Combine(dir, "ts", "index.ts")).ShouldBeTrue();

            // check mode: up to date, then stale after an edit
            RunSourceGenerator(compilation, dir, check: true).Diagnostics.Where(d => d.Id == "NCG032").ShouldBeEmpty();
            File.WriteAllText(Path.Combine(dir, "ts", "index.ts"), "edited");
            var stale = RunSourceGenerator(compilation, dir, check: true).Diagnostics.Single(d => d.Id == "NCG032");
            stale.Severity.ShouldBe(DiagnosticSeverity.Error);
            stale.GetMessage().ShouldContain("index.ts");
            File.ReadAllText(Path.Combine(dir, "ts", "index.ts")).ShouldBe("edited");

            // json: relative to the json file
            var jsonDir = Path.Combine(dir, "json");
            Directory.CreateDirectory(jsonDir);
            File.WriteAllText(Path.Combine(jsonDir, "codegen.json"), Json); // relative paths resolve against the json file on disk
            RunSourceGenerator(CompileWithInTreeAttributes(Plain), dir, [new Text(Path.Combine(jsonDir, "codegen.json"), Json)]);
            File.ReadAllText(Path.Combine(jsonDir, "ts", "Shop", "Contracts", "Customer.ts"))
                .ShouldBe(File.ReadAllText(Path.Combine(dir, "ts", "Shop", "Contracts", "Customer.ts")));

            // syntax errors while typing: nothing is written
            var broken = CompileWithInTreeAttributes(Plain.Replace("public class Backoffice { }", "public class Backoffice { public int Y"), Fluent);
            RunSourceGenerator(broken, dir).Diagnostics.ShouldContain(d => d.Id == "NCG031");
            File.ReadAllText(Path.Combine(dir, "ts", "index.ts")).ShouldBe("edited");

            // no OutputPath: reported, nothing written
            var noPath = CompileWithInTreeAttributes(Plain, Fluent.Replace(".OutputPath(\"ts\")", ""));
            RunSourceGenerator(noPath, dir).Diagnostics.ShouldContain(d => d.Id == "NCG030");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
