using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// The supabase/postgres image creates the pgsodium/Vault root key inside the container
/// (/etc/postgresql-custom/pgsodium_root.key) when it is missing. Every new container — a revision
/// update or node move in Azure Container Apps, every fresh local container — got a new key, and
/// Vault secrets written before failed to decrypt ("invalid ciphertext"). The key now lives with
/// the data: beside the data directory on the deployed share, in the config folder of a local run.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public class VaultKeyTests
{
    private const string ImageKeyPath = "/etc/postgresql-custom/pgsodium_root.key";

    private static async Task<string> DeployedWrapper()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
        });
        var supabase = builder.AddSupabase("sb");
#pragma warning disable CS0618 // no model-only equivalent yet
        var env = await ((IResourceWithEnvironment)supabase.Resource.Database!.Resource)
            .GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618
        return Encoding.UTF8.GetString(Convert.FromBase64String(env["WRAPPER_SCRIPT_BASE64"]));
    }

    /// <summary>The local database's mount of the key and the key file behind it.</summary>
    private static (ContainerMountAnnotation Mount, string KeyFile) LocalStack()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [] });
        var db = builder.AddSupabase("sb").Resource.Database!.Resource;
        var mount = db.Annotations.OfType<ContainerMountAnnotation>().Single(m => m.Target == ImageKeyPath);
        return (mount, mount.Source!);
    }

    [Fact]
    public async Task Deployed_the_key_lives_on_the_share_and_the_image_reads_it_through_a_link()
    {
        var wrapper = await DeployedWrapper();

        Assert.Contains("KEY_FILE=\"$ROOT/pgsodium_root.key\"", wrapper);
        Assert.Contains($"ln -sf \"$KEY_FILE\" {ImageKeyPath}", wrapper);
        Assert.Contains("chmod 0600 \"$KEY_FILE\"", wrapper);
        // Created once and never replaced: noclobber makes the first writer win.
        Assert.Contains("( set -C; umask 077; head -c 32 /dev/urandom | od -A n -t x1 | tr -d ' \\n' > \"$KEY_FILE\" )", wrapper);
        // In place before PostgreSQL (and pgsodium) starts.
        Assert.True(wrapper.IndexOf("ln -sf \"$KEY_FILE\"", StringComparison.Ordinal)
                    < wrapper.IndexOf("exec python3 /tmp/pg-lock.py", StringComparison.Ordinal));
    }

    [Fact]
    public void Locally_the_key_is_a_file_beside_the_config_mounted_read_only_into_the_image()
    {
        var (mount, keyFile) = LocalStack();

        Assert.True(mount.IsReadOnly);
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Matches("^[0-9a-f]{64}$", File.ReadAllText(keyFile));
    }

    [Fact]
    public void A_key_created_before_is_kept()
    {
        var (_, keyFile) = LocalStack();
        var first = File.ReadAllText(keyFile);

        var (_, again) = LocalStack();

        Assert.Equal(keyFile, again);
        Assert.Equal(first, File.ReadAllText(again));
    }

    [Fact]
    public void An_empty_or_broken_key_file_is_replaced_by_a_valid_one()
    {
        var (_, keyFile) = LocalStack();
        File.WriteAllText(keyFile, "");

        var (_, again) = LocalStack();

        Assert.Matches("^[0-9a-f]{64}$", File.ReadAllText(again));
    }
}
