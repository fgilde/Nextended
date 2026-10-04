using System.IO.Compression;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// An AppHost sets its own JWT secret and keys after the containers that use them exist —
/// AddSupabase, WithEdgeFunctions and references come first. Containers that copied a key when
/// they were created kept the public demo value: the edge functions all three, Studio and Storage
/// the demo service-role key. Deployed, that is a stack that half rejects itself, with a demo key
/// still in it.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public sealed class LateKeysTests : IDisposable
{
    private const string Secret = "a-late-secret-that-is-long-enough-for-hs256";
    private const string Anon = "late.anon.key";
    private const string Service = "late.service.key";

    private readonly string _functions = Path.Combine(Path.GetTempPath(), $"sbkeys-{Guid.NewGuid():N}");

    public LateKeysTests()
    {
        Directory.CreateDirectory(Path.Combine(_functions, "hello"));
        File.WriteAllText(Path.Combine(_functions, "hello", "index.ts"), "Deno.serve(() => new Response('ok'));\n");
    }

    public void Dispose()
    {
        SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.AzureContainerApps;
        try { Directory.Delete(_functions, recursive: true); } catch { /* best effort */ }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Keys_set_last_reach_every_container_and_no_demo_value_is_left(bool publish)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = publish ? ["--operation", "publish", "--publisher", "manifest"] : [],
        });
        var supabase = builder.AddSupabase("sb").WithEdgeFunctions(_functions);
        string[] demo = [supabase.Resource.JwtSecret, supabase.Resource.AnonKey, supabase.Resource.ServiceRoleKey];
        var web = builder.AddContainer("web", "nginx").WithSupabaseVite(supabase);
        var api = builder.AddContainer("api", "nginx").WithSupabaseServiceRoleReference(supabase);

        supabase.WithJwtSecret(Secret).WithAnonKey(Anon).WithServiceRoleKey(Service);

        var envs = new Dictionary<string, Dictionary<string, string>>();
        foreach (var resource in builder.Resources.OfType<IResourceWithEnvironment>())
        {
            var env = await Env(resource);
            envs[resource.Name] = env;
            foreach (var (name, value) in env)
                foreach (var old in demo)
                    Assert.False(Readable(name, value, env).Contains(old, StringComparison.Ordinal),
                        $"{resource.Name}: {name} still carries a demo value");
        }

        Assert.Equal(Anon, envs["sb-edge"]["SUPABASE_ANON_KEY"]);
        Assert.Equal(Service, envs["sb-edge"]["SUPABASE_SERVICE_ROLE_KEY"]);
        Assert.Equal(Secret, envs["sb-edge"]["JWT_SECRET"]);
        Assert.Equal(Service, envs["sb-storage"]["SERVICE_KEY"]);
        Assert.Equal(Service, envs["sb"]["SUPABASE_SERVICE_KEY"]);
        Assert.Equal(Anon, envs[web.Resource.Name]["VITE_SUPABASE_PUBLISHABLE_KEY"]);
        Assert.Equal(Service, envs[api.Resource.Name]["ConnectionStrings__supabase__Key"]);
    }

    private static async Task<Dictionary<string, string>> Env(IResourceWithEnvironment resource)
    {
#pragma warning disable CS0618 // no model-only equivalent yet
        return (await resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish))
#pragma warning restore CS0618
            .ToDictionary(e => e.Key, e => e.Value);
    }

    /// <summary>The value as text, with the base64 payloads (Kong template, post-init SQL) unpacked.</summary>
    private static string Readable(string name, string value, Dictionary<string, string> env)
    {
        if (name == "KONG_CONFIG_TEMPLATE_BASE64")
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        if (name == "POST_INIT_SQL_GZ_PARTS")
        {
            var blob = string.Concat(Enumerable.Range(0, int.Parse(value)).Select(i => env[$"POST_INIT_SQL_GZ_BASE64_{i}"]));
            using var gz = new GZipStream(new MemoryStream(Convert.FromBase64String(blob)), CompressionMode.Decompress);
            using var reader = new StreamReader(gz);
            return reader.ReadToEnd();
        }
        return value;
    }
}
