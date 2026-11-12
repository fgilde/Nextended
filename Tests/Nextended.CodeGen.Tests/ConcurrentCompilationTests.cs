using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nextended.CodeGen;
using Shouldly;
using static Nextended.CodeGen.Tests.TestCompilation;

namespace Nextended.CodeGen.Tests;

[TestClass]
public class ConcurrentCompilationTests
{
    // one process compiles several projects at once (the compiler server, Visual Studio, Rider): every project must get its own DTOs in its own
    // namespace, never another project's (the sub-generators used to be shared and kept the config of whichever compilation ran last)
    [TestMethod]
    public void Projects_compiled_at_the_same_time_keep_their_own_configuration()
    {
        var a = Project("A");
        var b = Project("B");

        Parallel.For(0, 40, i =>
        {
            var (compilation, ns) = i % 2 == 0 ? a : b;
            var result = CSharpGeneratorDriver.Create(new MainGenerator()).RunGenerators(compilation).GetRunResult();
            var dto = result.GeneratedTrees.Single(t => Path.GetFileName(t.FilePath) == "OrderLineDto.g.cs").ToString();
            dto.ShouldContain($"namespace {ns}");
        });
    }

    private static (Compilation Compilation, string Namespace) Project(string name)
    {
        var entities = $$"""
            namespace Shop{{name}}.Domain
            {
                public class OrderLine
                {
                    public int Id { get; set; }
                    public string? Product { get; set; }
                    public decimal Price{{name}} { get; set; }
                }
            }
            """;
        var profile = $$"""
            using Nextended.Core.CodeGen;
            using Shop{{name}}.Domain;

            public sealed class Config{{name}} : CodeGenConfig
            {
                public Config{{name}}()
                {
                    Defaults(d => d.Namespace("Shop{{name}}.Api"));
                    Dto<OrderLine>();
                }
            }
            """;
        return (CompileWithInTreeAttributes(entities, profile), $"Shop{name}.Api");
    }
}
