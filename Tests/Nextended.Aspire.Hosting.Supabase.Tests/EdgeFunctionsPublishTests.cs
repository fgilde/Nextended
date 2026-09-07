using System.Formats.Tar;
using System.IO.Compression;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Nextended.Aspire.Hosting.Supabase.Resources;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// How Edge Functions reach the container. Locally they are bind-mounted; in publish mode
/// (ACA init containers cannot share volumes) the whole functions directory travels as a
/// base64 tar.gz split across environment variables. Both paths are covered here because
/// the publish path stays invisible until something is actually deployed.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public class EdgeFunctionsPublishTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"sbfn-{Guid.NewGuid():N}");

    public EdgeFunctionsPublishTests()
    {
        // A function with a shared module next to it — the case that used to break.
        Write("hello/index.ts", "import { x } from \"../_shared/util.ts\";\nserve(() => new Response(x));\n");
        Write("_shared/util.ts", "export const x = \"shared\";\n");
        Write("_shared/nested/deep.ts", "export const deep = 1;\n");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private void Write(string relativePath, string content)
    {
        var full = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private static IDistributedApplicationBuilder Builder(bool publish) =>
        DistributedApplication.CreateBuilder(publish
            ? new DistributedApplicationOptions { Args = ["--operation", "publish", "--publisher", "manifest"] }
            : new DistributedApplicationOptions { Args = [] });

    private IResourceBuilder<SupabaseStackResource> Stack(bool publish, string? functionsPath = null) =>
        Builder(publish).AddSupabase("sb").WithEdgeFunctions(functionsPath ?? _root);

    private static async Task<Dictionary<string, string>> EnvOf(IResourceWithEnvironment resource, bool publish) =>
        // Obsolete in favour of ExecutionConfigurationBuilder, but that has no equivalent for
        // reading resolved values in a model-only test — and this overload still ships.
#pragma warning disable CS0618
        (await resource.GetEnvironmentVariableValuesAsync(
            publish ? DistributedApplicationOperation.Publish : DistributedApplicationOperation.Run))
#pragma warning restore CS0618
        .ToDictionary(e => e.Key, e => e.Value);

    private static SupabaseEdgeRuntimeResource Edge(IResourceBuilder<SupabaseStackResource> stack) =>
        stack.Resource.EdgeRuntime!.Resource;

    private static async Task<string> ArgsOf(IResource resource)
    {
        var args = Assert.Single(resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>());
        var collected = new List<object>();
        await args.Callback(new CommandLineArgsCallbackContext(collected));
        return string.Join(" ", collected.Select(a => a.ToString()));
    }

    private static byte[] ArchiveFrom(Dictionary<string, string> env)
    {
        var parts = int.Parse(env["FUNCS_TGZ_PARTS"]);
        var base64 = string.Concat(Enumerable.Range(0, parts).Select(i => env[$"FUNCS_TGZ_B64_{i}"]));
        return Convert.FromBase64String(base64);
    }

    private static List<string> EntriesOf(byte[] targz)
    {
        using var gz = new GZipStream(new MemoryStream(targz), CompressionMode.Decompress);
        using var tar = new TarReader(gz);
        var names = new List<string>();
        while (tar.GetNextEntry() is { } entry) names.Add(entry.Name.Replace('\\', '/'));
        return names;
    }

    [Fact]
    public async Task Publish_ships_the_whole_directory_including_shared_modules()
    {
        var stack = Stack(publish: true);
        var env = await EnvOf(Edge(stack), publish: true);

        var entries = EntriesOf(ArchiveFrom(env));

        // Regression: only "<function>/index.ts" used to be shipped, so every function
        // importing ../_shared/* failed to start in publish mode.
        Assert.Contains("hello/index.ts", entries);
        Assert.Contains("_shared/util.ts", entries);
        Assert.Contains("_shared/nested/deep.ts", entries);
    }

    [Fact]
    public async Task Publish_extracts_the_archive_and_starts_deno()
    {
        var stack = Stack(publish: true);
        var edge = Edge(stack);

        var script = await ArgsOf(edge);

        Assert.Contains("FUNCS_TGZ_PARTS", script);
        Assert.Contains("base64 -d | tar -xzf -", script);
        Assert.Contains("exec deno run", script);
        // The placeholder must be substituted, never shipped literally.
        Assert.DoesNotContain("__FUNCTIONS_DIR__", script);
    }

    [Fact]
    public async Task Publish_chunks_stay_below_the_env_var_limit()
    {
        // Bicep caps a literal env var at 128KB, the kernel at MAX_ARG_STRLEN (128KB).
        var stack = Stack(publish: true);
        var env = await EnvOf(Edge(stack), publish: true);

        var chunks = env.Where(e => e.Key.StartsWith("FUNCS_TGZ_B64_")).ToList();
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.True(c.Value.Length <= 64000, $"{c.Key} is {c.Value.Length} chars"));
        Assert.Equal(chunks.Count.ToString(), env["FUNCS_TGZ_PARTS"]);
    }

    [Fact]
    public async Task Publish_skips_oversized_files_instead_of_blowing_the_environment_budget()
    {
        // The router spawns a child process per function and passes the environment on, so
        // exceeding ARG_MAX (~2MB for env+args) breaks every function, not just startup.
        Write("hello/huge.bin", new string('A', 600 * 1024));
        var stack = Stack(publish: true);
        var env = await EnvOf(Edge(stack), publish: true);

        var entries = EntriesOf(ArchiveFrom(env));
        Assert.DoesNotContain("hello/huge.bin", entries);
        Assert.Contains("hello/index.ts", entries);
    }

    [Fact]
    public async Task Run_mode_keeps_bind_mounts_and_no_archive()
    {
        var stack = Stack(publish: false);
        var edge = Edge(stack);

        var mounts = edge.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Contains(mounts, m => m.Target == "/home/deno/main");
        Assert.Contains(mounts, m => m.Target == "/home/deno/functions");

        // Locally deno is started directly; nothing is unpacked from the environment.
        // (Env values are not resolved here on purpose: in run mode that waits for real
        // endpoint host/port values, which only exist once the application runs.)
        var script = await ArgsOf(edge);
        Assert.DoesNotContain("FUNCS_TGZ", script);
        Assert.Contains("main.ts", script);
    }

    [Fact]
    public async Task Without_a_functions_directory_nothing_is_shipped()
    {
        var missing = Path.Combine(_root, "does-not-exist");
        var stack = Stack(publish: true, functionsPath: missing);
        var env = await EnvOf(Edge(stack), publish: true);

        Assert.DoesNotContain("FUNCS_TGZ_PARTS", env.Keys);
        Assert.Contains("MAIN_TS_BASE64", env.Keys);
    }
}
