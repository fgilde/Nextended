using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Shouldly;
using static Nextended.CodeGen.Tests.TestCompilation;

namespace Nextended.CodeGen.Tests;

/// <summary>
/// Attribute&lt;TAttribute&gt;(args) of the fluent configuration: the attribute written with its full name, the arguments as C# literals,
/// read from the configuration, never executed. Also nameof and constant interpolated strings in string settings.
/// </summary>
[TestClass]
public class TypedAttributeTests
{
    private const string Entities = """
        using System.Collections.Generic;

        namespace Shop.Domain
        {
            public class Order
            {
                public string Number { get; set; } = "";
                public string? InternalNote { get; set; }
                public List<OrderLine> Lines { get; set; } = [];
            }

            public class OrderLine
            {
                public string Sku { get; set; } = "";
                public int Quantity { get; set; }
                public string? Note { get; set; }
            }
        }

        namespace Lib.Marks
        {
            public enum Tone { Plain, Loud = 4 }

            [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
            public sealed class MarkAttribute : System.Attribute
            {
                public MarkAttribute(params object?[] values) => Values = values;
                public object?[] Values { get; }
                public string? Label { get; set; }
                public Tone Tone { get; set; }
                public System.Type? Kind { get; set; }
                public int[]? Numbers;
            }

            public static class Holder
            {
                public sealed class InnerAttribute(string value) : System.Attribute { public string Value { get; } = value; }
                public static readonly string NotConstant = "x";
                public static string Computed() => "x";
            }

            public sealed class TypedAttribute<T> : System.Attribute { }

            public abstract class BaseMarkAttribute : System.Attribute { }
            public sealed class FlagAttribute : BaseMarkAttribute { }

            public static class Keys { public const string Port = "port"; }
        }
        """;

    private static DtoGenerationConfig Config() => new()
    {
        Namespace = "Shop.Contracts",
        CreateFileHeaders = false,
        CreateRegions = false,
        CreateComments = false
    };

    private static string Profile(string body) => $$"""
        using Nextended.Core.CodeGen;
        using Shop.Domain;
        using Lib.Marks;
        public sealed class P : DtoConfig<OrderLine> { public P() { {{body}} } }
        """;

    private static (List<GeneratedFile> Files, List<Diagnostic> Diagnostics, Compilation Compilation) Run(params string[] sources)
    {
        var compilation = CompileWithInTreeAttributes(sources);
        var (files, diagnostics) = GenerateWithDiagnostics(compilation, Config());
        return (files, diagnostics, compilation);
    }

    private static string File(IEnumerable<GeneratedFile> files, string name)
        => files.SingleOrDefault(f => f.FileName == name)?.Content ?? throw new ShouldAssertException($"{name} not generated: {string.Join(", ", files.Select(f => f.FileName))}");

    private static void NoDiagnostics(IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty(string.Join(Environment.NewLine, diagnostics));

    [TestMethod]
    public void Constructor_arguments_become_csharp_literals_of_their_type()
    {
        var (files, diagnostics, compilation) = Run(Entities, Profile("""
            Property(x => x.Sku).Attribute<MarkAttribute>("q\"\n", 'c', true, 1, -2L, 3U, 4UL, 1.5F, 2.5, (byte)7, (short)-3, null,
                typeof(string), typeof(System.Collections.Generic.Dictionary<,>), new[] { 1, 2 }, new string[0], Tone.Loud, (Tone)5, Keys.Port);
            """));

        NoDiagnostics(diagnostics);
        var dto = File(files, "OrderLineDto.g.cs");
        dto.ShouldContain("""[global::Lib.Marks.Mark("q\"\n", 'c', true, 1, -2L, 3U, 4UL, 1.5F, 2.5D, ((byte)7), ((short)-3), null, typeof(string), """
                          + """typeof(global::System.Collections.Generic.Dictionary<,>), new int[] { 1, 2 }, new string[0], global::Lib.Marks.Tone.Loud, """
                          + """((global::Lib.Marks.Tone)5), "port")]""");

        // the generated attribute compiles and carries the values of the configuration
        var assembly = Load(CompileWithGenerated(compilation, files));
        var mark = assembly.GetType("Shop.Contracts.OrderLineDto")!.GetProperty("Sku")!.GetCustomAttributes(false).Single(a => a.GetType().Name == "MarkAttribute");
        var values = (object?[])Get(mark, "Values")!;
        values.Take(12).ShouldBe(["q\"\n", 'c', true, 1, -2L, 3U, 4UL, 1.5F, 2.5, (byte)7, (short)-3, null]);
        values[12].ShouldBe(typeof(string));
        values[14].ShouldBe(new[] { 1, 2 });
        values[16]!.ToString().ShouldBe("Loud");
        values[18].ShouldBe("port");
    }

    [TestMethod]
    public void Generic_nested_base_and_framework_attribute_types_get_their_full_names()
    {
        var (files, diagnostics, compilation) = Run(Entities, Profile("""
            Attribute<System.SerializableAttribute>();
            Property(x => x.Sku).Attribute<Holder.InnerAttribute>(Keys.Port);
            Property(x => x.Quantity).Attribute<TypedAttribute<System.Collections.Generic.List<int>>>();
            Property(x => x.Note).Attribute<FlagAttribute>();
            """));

        NoDiagnostics(diagnostics);
        var dto = File(files, "OrderLineDto.g.cs");
        dto.ShouldContain("[global::System.Serializable]");
        dto.ShouldContain("""[global::Lib.Marks.Holder.Inner("port")]""");
        dto.ShouldContain("[global::Lib.Marks.Typed<global::System.Collections.Generic.List<int>>]");
        dto.ShouldContain("[global::Lib.Marks.Flag]");
        CompileWithGenerated(compilation, files);
    }

    [TestMethod]
    public void Named_arguments_follow_the_constructor_arguments()
    {
        var (files, diagnostics, compilation) = Run(Entities, Profile("""
            Property(x => x.Sku).Attribute<MarkAttribute>(a => { a.Label = nameof(OrderLine.Sku); a.Tone = Tone.Loud; a.Kind = typeof(System.Collections.Generic.List<>); a.Numbers = new[] { 3 }; }, 1);
            Property(x => x.Note).Attribute<MarkAttribute>(a => a.Label = $"{nameof(OrderLine)}.{nameof(OrderLine.Note)}");
            """));

        NoDiagnostics(diagnostics);
        var dto = File(files, "OrderLineDto.g.cs");
        dto.ShouldContain("""[global::Lib.Marks.Mark(1, Label = "Sku", Tone = global::Lib.Marks.Tone.Loud, Kind = typeof(global::System.Collections.Generic.List<>), Numbers = new int[] { 3 })]""");
        dto.ShouldContain("""[global::Lib.Marks.Mark(Label = "OrderLine.Note")]""");
        CompileWithGenerated(compilation, files);
    }

    [TestMethod]
    public void Typed_attributes_are_offered_wherever_the_string_attribute_is()
    {
        const string profile = """
            using Nextended.Core.CodeGen;
            using Shop.Domain;
            using Lib.Marks;
            public sealed class P : CodeGenConfig
            {
                public P()
                {
                    ViewDefaults(v => v.Attribute<MarkAttribute>("view default"));
                    Dto<Order>(o => o
                        .Attribute<MarkAttribute>("type")
                        .Property(x => x.Number, p => p.Attribute<MarkAttribute>("member"))
                        .View("OrderRow", v => v.Attribute<MarkAttribute>("view").Property(x => x.Number).Attribute<MarkAttribute>("view member"))
                        .View("OrderRef"));
                    Dto(typeof(OrderLine));
                    ForNamespace("Shop.Domain", n => n.Attribute<MarkAttribute>("namespace"));
                }
            }
            """;
        var (files, diagnostics, compilation) = Run(Entities, profile);

        NoDiagnostics(diagnostics);
        var order = File(files, "OrderDto.g.cs");
        order.ShouldContain("""[global::Lib.Marks.Mark("type")]""");
        order.ShouldContain("""[global::Lib.Marks.Mark("member")]""");
        var row = File(files, "OrderRowDto.g.cs");
        row.ShouldContain("""[global::Lib.Marks.Mark("view")]""");
        row.ShouldContain("""[global::Lib.Marks.Mark("view member")]""");
        File(files, "OrderRefDto.g.cs").ShouldContain("""[global::Lib.Marks.Mark("view default")]""");
        File(files, "OrderLineDto.g.cs").ShouldContain("""[global::Lib.Marks.Mark("namespace")]""");
        CompileWithGenerated(compilation, files);

        foreach (var api in new[] { "Nextended.Core.CodeGen.IDtoSettings`1", "Nextended.Core.CodeGen.IPropertyBuilder" })
            compilation.GetTypeByMetadataName(api)!.GetMembers("Attribute").OfType<IMethodSymbol>().Count(m => m.IsGenericMethod).ShouldBe(2, api);
    }

    [TestMethod]
    public void Arguments_that_are_not_constant_are_reported_and_the_statement_is_skipped()
    {
        var (files, diagnostics, _) = Run(Entities, Profile("""
            Property(x => x.Sku).Attribute<MarkAttribute>(Holder.NotConstant);
            Property(x => x.Quantity).Attribute<MarkAttribute>(1, Holder.Computed());
            Property(x => x.Note).Attribute<MarkAttribute>(a => a.Label = Holder.NotConstant);
            Attribute<MarkAttribute>(a => System.Console.WriteLine());
            """));

        var unreadable = diagnostics.Where(d => d.Id == "NCG020").Select(d => d.GetMessage()).ToList();
        unreadable.Count.ShouldBe(4, string.Join(Environment.NewLine, unreadable));
        unreadable[0].ShouldContain("'Holder.NotConstant' is not a constant");
        unreadable[1].ShouldContain("'Holder.Computed()' is not a constant");
        unreadable[3].ShouldContain("must set a member of the attribute");
        File(files, "OrderLineDto.g.cs").ShouldNotContain("Mark");
    }

    [TestMethod]
    public void Nameof_and_constant_interpolated_strings_work_in_string_settings()
    {
        const string profile = """
            using Nextended.Core.CodeGen;
            using Shop.Domain;
            public sealed class P : CodeGenConfig
            {
                public P()
                {
                    Defaults(d => d.PropertiesToIgnore(nameof(Order.InternalNote), "Lines"));
                    Dto<Order>();
                    Dto<OrderLine>(l => l
                        .Attribute($"[System.ComponentModel.Description(\"{nameof(OrderLine)}\")]")
                        .Ignore(nameof(OrderLine.Quantity))
                        .Property(nameof(OrderLine.Note)).Attribute($"[System.ComponentModel.Description(nameof({nameof(OrderLine)}.{nameof(OrderLine.Note)}))]"));
                }
            }
            """;
        var (files, diagnostics, compilation) = Run(Entities, profile);

        NoDiagnostics(diagnostics);
        var order = File(files, "OrderDto.g.cs");
        order.ShouldNotContain("InternalNote");
        order.ShouldNotContain("Lines");
        var line = File(files, "OrderLineDto.g.cs");
        line.ShouldContain("""[System.ComponentModel.Description("OrderLine")]""");
        line.ShouldContain("[System.ComponentModel.Description(nameof(OrderLine.Note))]");
        line.ShouldNotContain("Quantity");
    }
}
