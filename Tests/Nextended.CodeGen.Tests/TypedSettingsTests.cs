using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Contracts;
using Shouldly;
using static Nextended.CodeGen.Tests.TestCompilation;

namespace Nextended.CodeGen.Tests;

/// <summary>
/// Typed forms of the fluent settings that name a type (BaseType, Interface, PropertyTypeOverride): written with the full name from the
/// symbol, the string forms unchanged.
/// </summary>
[TestClass]
public class TypedSettingsTests
{
    private const string Entities = """
        using System;
        namespace Shop.Domain
        {
            public class Order { public string Number { get; set; } = ""; }
            public class Line { public int Quantity { get; set; } }
            public class Page<T> { public T? Item { get; set; } }
        }
        namespace Lib.Contracts
        {
            public interface IKeyed<TKey> { }
            public interface IAudited { }
            public interface ITracked { }
            public abstract class PageBase<T> { }
            public interface IPaged<T> { }
            public static class Outer { public interface INested { } }
        }
        """;

    private static DtoGenerationConfig Config() => new()
    {
        Namespace = "Shop.Contracts",
        CreateFileHeaders = false,
        CreateRegions = false,
        CreateComments = false,
        GeneratedCodeAttribute = false
    };

    private static (List<GeneratedFile> Files, List<Diagnostic> Diagnostics, Compilation Compilation) Run(string body)
    {
        var compilation = CompileWithInTreeAttributes(Entities, $$"""
            using System;
            using Nextended.Core.CodeGen;
            using Shop.Domain;
            using Lib.Contracts;
            public sealed class P : CodeGenConfig { public P() { {{body}} } }
            """);
        var (files, diagnostics) = GenerateWithDiagnostics(compilation, Config());
        return (files, diagnostics, compilation);
    }

    private static string File(IEnumerable<GeneratedFile> files, string name)
        => files.SingleOrDefault(f => f.FileName == name)?.Content ?? throw new ShouldAssertException($"{name} not generated: {string.Join(", ", files.Select(f => f.FileName))}");

    private static void NoDiagnostics(IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty(string.Join(Environment.NewLine, diagnostics));

    [TestMethod]
    public void BaseType_from_a_type_argument_or_typeof_is_the_full_name()
    {
        var (generic, d1, compilation) = Run("Dto<Order>(o => o.BaseType<IKeyed<Guid>>()); Dto<Line>(l => l.BaseType(typeof(Outer.INested)));");
        var (byString, d2, _) = Run("""Dto<Order>(o => o.BaseType("Lib.Contracts.IKeyed<System.Guid>")); Dto<Line>(l => l.BaseType("Lib.Contracts.Outer.INested"));""");

        NoDiagnostics(d1);
        NoDiagnostics(d2);
        var order = File(generic, "OrderDto.g.cs");
        order.ShouldContain("public partial class OrderDto : global::Lib.Contracts.IKeyed<global::System.Guid>, IOrderDto");
        File(generic, "LineDto.g.cs").ShouldContain("public partial class LineDto : global::Lib.Contracts.Outer.INested, ILineDto");
        // the string form is unchanged; the typed one differs only in the spelling of the base type
        File(byString, "OrderDto.g.cs").ShouldBe(order.Replace("global::Lib.Contracts.IKeyed<global::System.Guid>", "Lib.Contracts.IKeyed<System.Guid>"));
        CompileWithGenerated(compilation, generic);
    }

    [TestMethod]
    public void Open_generic_base_types_keep_their_type_parameter_names()
    {
        var (files, diagnostics, compilation) = Run("Dto(typeof(Page<>), p => p.BaseType(typeof(PageBase<>)).Interface(typeof(IPaged<>)));");

        NoDiagnostics(diagnostics);
        var page = File(files, "PageDto.g.cs");
        page.ShouldContain("global::Lib.Contracts.PageBase<T>");
        page.ShouldContain("global::Lib.Contracts.IPaged<T>");
        CompileWithGenerated(compilation, files);
    }

    [TestMethod]
    public void Typed_interfaces_add_up_and_strings_replace()
    {
        var (files, diagnostics, compilation) = Run("""
            Defaults(d => d.Interface<IAudited>());
            Dto<Order>(o => o.Interface<IAudited>().Interface(typeof(ITracked)).Interface<IAudited>());
            Dto<Line>(l => l.Interface<ITracked>().Interfaces("Lib.Contracts.IAudited"));
            """);

        NoDiagnostics(diagnostics);
        var order = File(files, "OrderDto.g.cs");
        order.ShouldContain("global::Lib.Contracts.IAudited, global::Lib.Contracts.ITracked");
        order.Split("IAudited").Length.ShouldBe(2, order);                    // once (the DTO's list replaces the default)
        var line = File(files, "LineDto.g.cs");
        line.ShouldContain("Lib.Contracts.IAudited");
        line.ShouldNotContain("ITracked");                                    // Interfaces(...) replaced it
        CompileWithGenerated(compilation, files);
    }

    [TestMethod]
    public void Defaults_take_a_typed_base_type()
    {
        var (files, diagnostics, compilation) = Run("Defaults(d => d.BaseType<ITracked>()); Dto<Order>(); Dto<Line>(l => l.BaseType((string?)null));");

        NoDiagnostics(diagnostics);
        File(files, "OrderDto.g.cs").ShouldContain("public partial class OrderDto : global::Lib.Contracts.ITracked, IOrderDto");
        File(files, "LineDto.g.cs").ShouldContain("public partial class LineDto : global::Lib.Contracts.ITracked, ILineDto"); // null = the default, as before (needs the cast now)
        CompileWithGenerated(compilation, files);
    }

    [TestMethod]
    public void Excel_PropertyTypeOverride_from_a_type()
    {
        string Source([System.Runtime.CompilerServices.CallerFilePath] string file = "")
            => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "..", "TestProjects", "CodeGenSample", "Sources", "scac_codes.xlsx")).Replace(Path.DirectorySeparatorChar, '/');
        string Row(string overrides)
        {
            var profile = $$"""
                using Nextended.Core.CodeGen;
                public sealed class P : CodeGenConfig
                {
                    public P() { ExcelGeneration(e => e.SourceFile("{{Source()}}").Namespace("Shop.Scac").RootClassName("Scac").GenerateStaticTable(false){{overrides}}); }
                }
                """;
            var result = CSharpGeneratorDriver.Create(new MainGenerator()).RunGenerators(CompileWithInTreeAttributes(profile)).GetRunResult();
            result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty();
            return result.GeneratedTrees.Single(t => t.FilePath.EndsWith("scac_codes.row.g.cs")).ToString();
        }

        Row(""".PropertyTypeOverride<System.Uri>("CarrierName")""").ShouldContain("public global::System.Uri CarrierName { get; set; }");
        Row(""".PropertyTypeOverride("CarrierName", typeof(System.Uri))""").ShouldContain("public global::System.Uri CarrierName { get; set; }");
        Row(""".PropertyTypeOverride("CarrierName", "System.Uri")""").ShouldContain("public System.Uri CarrierName { get; set; }");
    }
}
