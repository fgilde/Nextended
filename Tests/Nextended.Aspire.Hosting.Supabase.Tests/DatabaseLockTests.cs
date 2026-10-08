using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// Regression: Azure Container Apps moved the database replica to another node and started the new
/// one before stopping the old. For two minutes both postmasters ran on the same NFS data directory
/// (postmaster.pid only guards one host); the old one's shutdown checkpoint left the WAL without a
/// valid checkpoint and the database never started again ("could not locate a valid checkpoint
/// record"). Revision updates overlap the same way. The deployed database now runs only while it
/// holds a lock on the share.
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public class DatabaseLockTests
{
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

    [Fact]
    public async Task The_deployed_postmaster_runs_only_while_it_holds_the_lock_on_its_share()
    {
        var wrapper = await DeployedWrapper();

        Assert.Contains("exec python3 /tmp/pg-lock.py \"$ROOT/.supabase-db.lock\" /usr/local/bin/docker-entrypoint.sh postgres", wrapper);
        Assert.Contains("fcntl.lockf(fd, fcntl.LOCK_EX)", wrapper);
        // execve keeps a POSIX lock only while its descriptor stays open.
        Assert.Contains("os.set_inheritable(fd, True)", wrapper);
        Assert.Contains("os.execv(argv[0], argv)", wrapper);
    }

    [Fact]
    public async Task A_waiting_instance_still_answers_so_the_platform_can_retire_the_old_one()
    {
        // Without it the new replica never becomes ready, the old one is never stopped, and an
        // update hangs with both waiting for each other.
        var wrapper = await DeployedWrapper();

        Assert.Contains("socket.create_server(('0.0.0.0', 5432))", wrapper);
        Assert.True(wrapper.IndexOf("fcntl.lockf(fd, fcntl.LOCK_EX)", StringComparison.Ordinal)
                    < wrapper.IndexOf("placeholder.close()", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_fresh_share_gets_the_cluster_beside_the_lock_and_an_existing_one_stays_in_place()
    {
        // initdb refuses a directory that already holds the lock file.
        var wrapper = await DeployedWrapper();

        Assert.Contains("if [ -f \"$ROOT/PG_VERSION\" ]; then export PGDATA=\"$ROOT\"; else export PGDATA=\"$ROOT/pgdata\"; fi", wrapper);
        Assert.Contains("-c data_directory=\"$PGDATA\"", wrapper);
    }
}
