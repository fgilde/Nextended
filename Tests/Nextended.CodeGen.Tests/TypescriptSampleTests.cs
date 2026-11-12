using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Nextended.CodeGen.Config;
using Nextended.CodeGen.Generators.TypescriptGeneration;
using Shouldly;
using static Nextended.CodeGen.Tests.TestCompilation;

namespace Nextended.CodeGen.Tests;

/// <summary>
/// The CodeGenTsSample project: its checked-in ts/ and ts-const/ folders are what its two fluent TypeScript generations give, and
/// they compile with <c>tsc --strict</c> (skipped without node and TypeScript).
/// </summary>
[TestClass]
public class TypescriptSampleTests
{
    internal static string SampleDir([System.Runtime.CompilerServices.CallerFilePath] string file = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "..", "TestProjects", "CodeGenTsSample"));

    private const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        """;

    internal static (Compilation Compilation, IReadOnlyList<TypescriptGenerationConfig> Generations) Sample()
    {
        var dir = SampleDir();
        var sources = new[] { "Contracts", "CodeGen" }
            .SelectMany(d => Directory.GetFiles(Path.Combine(dir, d), "*.cs", SearchOption.AllDirectories))
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(File.ReadAllText)
            .Append(ImplicitUsings)
            .ToArray();
        var compilation = CompileWithInTreeAttributes(sources);
        return (compilation, TypescriptGenerator.FluentGenerations(compilation));
    }

    [TestMethod]
    public void Sample_typescript_is_unchanged()
    {
        var (compilation, generations) = Sample();
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        generations.Select(g => g.OutputPath).ShouldBe(["ts", "ts-const", "ts-pricing"]);

        var changed = new List<string>();
        foreach (var generation in generations)
        {
            var result = TypescriptGenerator.Generate(compilation, generation);
            result.Unstable.ShouldBeFalse();
            var actualDir = Path.Combine(AppContext.BaseDirectory, "golden-ts-actual", generation.OutputPath!);
            if (Directory.Exists(actualDir))
                Directory.Delete(actualDir, true);
            TypescriptWriter.Write(actualDir, result.Files, check: false);
            if (Environment.GetEnvironmentVariable("NEXTENDED_UPDATE_GOLDEN") == "1")
                TypescriptWriter.Write(Path.Combine(SampleDir(), generation.OutputPath!), result.Files, check: false);
            // the checked-in folder must be exactly what a write would leave: no difference, no stale file
            changed.AddRange(TypescriptWriter.Write(Path.Combine(SampleDir(), generation.OutputPath!), result.Files, check: true)
                .Select(f => $"{generation.OutputPath}/{f}"));
            var expected = result.Files.Keys.Append(TypescriptWriter.ManifestName).Select(k => Path.GetFullPath(Path.Combine(SampleDir(), generation.OutputPath!, k))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            changed.AddRange(Directory.GetFiles(Path.Combine(SampleDir(), generation.OutputPath!), "*", SearchOption.AllDirectories).Where(f => !expected.Contains(Path.GetFullPath(f))));
        }
        changed.ShouldBeEmpty($"generated into {Path.Combine(AppContext.BaseDirectory, "golden-ts-actual")}");
    }

    [TestMethod]
    public void Sample_typescript_compiles_with_tsc_strict()
    {
        var dir = SampleDir();
        Tsc.Check(dir, Directory.GetFiles(Path.Combine(dir, "ts"), "*.ts", SearchOption.AllDirectories));
        // the const generation also fits erasableSyntaxOnly (no enums)
        Tsc.Check(dir, Directory.GetFiles(Path.Combine(dir, "ts-const"), "*.ts", SearchOption.AllDirectories), "--erasableSyntaxOnly");
        Tsc.Check(dir, Directory.GetFiles(Path.Combine(dir, "ts-pricing"), "*.ts", SearchOption.AllDirectories));
    }
}

/// <summary>Runs <c>tsc --noEmit --strict</c>; inconclusive without node and TypeScript (set NEXTENDED_TSC to a tsc.js to point at one).</summary>
internal static class Tsc
{
    private static readonly Lazy<string?> Path_ = new(Find);

    public static bool Available => Path_.Value != null;

    public static void Check(string workingDir, IEnumerable<string> files, params string[] options)
    {
        if (Path_.Value is not { } tsc)
        {
            Assert.Inconclusive("node with TypeScript not found (npm i -g typescript, or set NEXTENDED_TSC to a tsc.js)");
            return;
        }
        var args = new[] { tsc, "--noEmit", "--strict", "--target", "es2022", "--module", "esnext", "--moduleResolution", "bundler",
                "--isolatedModules", "--verbatimModuleSyntax", "--noUnusedLocals", "--skipLibCheck" }
            .Concat(options).Concat(files);
        var (code, output) = Run("node", args, workingDir);
        code.ShouldBe(0, output);
    }

    private static string? Find()
    {
        var env = Environment.GetEnvironmentVariable("NEXTENDED_TSC");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
            return env;
        try
        {
            if (Run("node", ["--version"], ".").Code != 0)
                return null;
        }
        catch
        {
            return null; // no node
        }
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var local = Path.Combine(dir.FullName, "node_modules", "typescript", "lib", "tsc.js");
            if (File.Exists(local))
                return local;
        }
        try
        {
            var (code, root) = Run(OperatingSystem.IsWindows() ? "cmd" : "npm", OperatingSystem.IsWindows() ? ["/c", "npm", "root", "-g"] : ["root", "-g"], ".");
            var global = Path.Combine(root.Trim(), "typescript", "lib", "tsc.js");
            return code == 0 && File.Exists(global) ? global : null;
        }
        catch
        {
            return null;
        }
    }

    private static (int Code, string Output) Run(string file, IEnumerable<string> args, string workingDir)
    {
        var info = new ProcessStartInfo(file) { WorkingDirectory = workingDir, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in args)
            info.ArgumentList.Add(a);
        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit(120_000);
        return (process.ExitCode, stdout.Result + stderr.Result);
    }
}
