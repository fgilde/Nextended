using System.Text.Json;
using System.Text.Json.Serialization;
using Nextended.CodeGen.Config;
using Shouldly;
using static Nextended.CodeGen.Tests.TestCompilation;

namespace Nextended.CodeGen.Tests;

/// <summary>
/// The CodeGenSample project checks its generated DTOs and mappings in. Generating them again from its entities and its
/// CodeGen.config.json must give the same files: proof that the default output did not change.
/// </summary>
[TestClass]
public class SampleGoldenTests
{
    private static string SampleDir([System.Runtime.CompilerServices.CallerFilePath] string file = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "..", "TestProjects", "CodeGenSample"));

    [TestMethod]
    public void Sample_output_is_unchanged()
    {
        var dir = SampleDir();
        var sources = Directory.GetFiles(Path.Combine(dir, "Entities"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Append(ImplicitUsings) // the sample has ImplicitUsings enabled
            .ToArray();
        var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "CodeGen.config.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var config = json.RootElement.GetProperty("DtoGeneration").Deserialize<DtoGenerationConfig>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter() }
        })!;

        var files = Generate(Compile(sources), config);

        // the generated files land in the test output too, to diff them against the sample when this fails
        var actualDir = Path.Combine(AppContext.BaseDirectory, "golden-actual");
        var changed = new List<string>();
        foreach (var file in files.Where(f => f.FileName != "ComGuids.g.cs")) // random ids
        {
            var path = Path.Combine(dir, file.OutputPath!, file.FileName);
            var actual = Path.Combine(actualDir, file.OutputPath!, file.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(actual)!);
            File.WriteAllText(actual, file.Content);
            if (!File.Exists(path) || Normalize(file.Content) != Normalize(File.ReadAllText(path)))
                changed.Add(Path.Combine(file.OutputPath!, file.FileName));
        }
        changed.ShouldBeEmpty($"generated into {actualDir}");
    }

    private static string FluentSampleDir([System.Runtime.CompilerServices.CallerFilePath] string file = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "..", "TestProjects", "CodeGenFluentSample"));

    /// <summary>
    /// The CodeGenFluentSample has no codegen.json and no attributes: its CodeGen/*.cs classes configure everything. Its checked-in
    /// Generated folder is what generating from them gives.
    /// </summary>
    [TestMethod]
    public void Fluent_sample_output_is_unchanged()
    {
        var dir = FluentSampleDir();
        var sources = new[] { "Entities", "CodeGen" }
            .SelectMany(d => Directory.GetFiles(Path.Combine(dir, d), "*.cs", SearchOption.AllDirectories))
            .Select(File.ReadAllText)
            .Append(ImplicitUsings)
            .ToArray();

        var (files, diagnostics) = GenerateWithDiagnostics(CompileWithInTreeAttributes(sources), new DtoGenerationConfig());

        diagnostics.ShouldBeEmpty();
        var actualDir = Path.Combine(AppContext.BaseDirectory, "golden-fluent-actual");
        var changed = new List<string>();
        foreach (var file in files)
        {
            file.OutputPath.ShouldNotBeNull(file.FileName);
            var path = Path.Combine(dir, file.OutputPath!, file.FileName);
            var actual = Path.Combine(actualDir, file.OutputPath!, file.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(actual)!);
            File.WriteAllText(actual, file.Content);
            if (!File.Exists(path) || Normalize(file.Content) != Normalize(File.ReadAllText(path)))
                changed.Add(Path.Combine(file.OutputPath!, file.FileName));
        }
        changed.ShouldBeEmpty($"generated into {actualDir}");
        var expected = files.Select(f => Path.GetFullPath(Path.Combine(dir, f.OutputPath!, f.FileName))).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Directory.GetFiles(Path.Combine(dir, "Generated"), "*", SearchOption.AllDirectories)
            .Where(f => !expected.Contains(Path.GetFullPath(f))).ShouldBeEmpty("stale files in Generated");
    }

    private const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd();
}
