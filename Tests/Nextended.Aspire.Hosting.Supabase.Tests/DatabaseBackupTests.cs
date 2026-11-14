using System.Diagnostics;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using Nextended.Aspire.Hosting.Supabase.Builders;
using Nextended.Aspire.Hosting.Supabase.Resources;
using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// WithDatabaseBackups: the backup service container (the stack's postgres image running the
/// embedded Python script), its configuration, its targets and — with Python at hand — the
/// script's own self-test (retention, schedule, parts, S3 signing).
/// </summary>
[Collection(SupabaseModelCollection.Name)]
public sealed class DatabaseBackupTests : IDisposable
{
    public void Dispose()
    {
        SupabaseBuilderExtensions.StorageS3Backend = null;
        SupabaseBuilderExtensions.PublishTarget = SupabasePublishTarget.AzureContainerApps;
    }

    private static (IDistributedApplicationBuilder Builder, IResourceBuilder<SupabaseStackResource> Supabase) Stack(bool publish = true)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = publish ? ["--operation", "publish", "--publisher", "manifest"] : [],
        });
        builder.Configuration["DcpPublisher:CliPath"] = "unused";
        builder.Configuration["DcpPublisher:DashboardPath"] = "unused";
        return (builder, builder.AddSupabase("sb"));
    }

    private static IResource Resource(IDistributedApplicationBuilder builder, string name) =>
        builder.Resources.Single(r => r.Name == name);

    private static async Task<Dictionary<string, string>> Env(IDistributedApplicationBuilder builder, string name)
    {
        var resource = (IResourceWithEnvironment)Resource(builder, name);
#pragma warning disable CS0618 // no model-only equivalent yet
        return await resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish)
            .AsTask().WaitAsync(TimeSpan.FromSeconds(10));
#pragma warning restore CS0618
    }

    private static async Task<List<string>> Args(IDistributedApplicationBuilder builder, string name)
    {
        var args = new List<object>();
        var context = new CommandLineArgsCallbackContext(args) { ExecutionContext = builder.ExecutionContext };
        foreach (var callback in Resource(builder, name).Annotations.OfType<CommandLineArgsCallbackAnnotation>())
            await callback.Callback(context);
        return args.Select(a => a.ToString()!).ToList();
    }

    [Fact]
    public async Task Runs_the_embedded_script_in_the_stacks_own_postgres_image()
    {
        var (builder, supabase) = Stack();
        supabase.WithDatabaseBackups();

        var backup = Resource(builder, "sb-backup");
        Assert.Same(backup, supabase.Resource.Backup!.Resource);
        // pg_dump has to match the server: same image, same tag as the database
        var image = backup.Annotations.OfType<ContainerImageAnnotation>().Single();
        var db = Resource(builder, "sb-db").Annotations.OfType<ContainerImageAnnotation>().Single();
        Assert.Equal((db.Image, db.Tag), (image.Image, image.Tag));

        var env = await Env(builder, "sb-backup");
        Assert.Equal(DatabaseBackupBuilderExtensions.BackupScriptSource(),
            Encoding.UTF8.GetString(Convert.FromBase64String(env["BACKUP_SCRIPT_BASE64"])));
        Assert.Contains("exec python3 -u /tmp/database-backup.py", string.Join(' ', await Args(builder, "sb-backup")));
        Assert.Contains("base64 -d", string.Join(' ', await Args(builder, "sb-backup")));
        // Superuser connection and the storage API with the service key
        Assert.Contains("{sb-db.bindings.tcp.host}", env["DB_HOST"]);
        Assert.Contains("{sb-storage.bindings.http.url}", env["STORAGE_URL"]);
        Assert.Equal(supabase.Resource.ServiceRoleKey, env["SERVICE_KEY"]);
        Assert.Equal("sb", env["BACKUP_STACK_NAME"]);
    }

    [Fact]
    public async Task Locally_the_script_is_a_mounted_file_not_an_environment_variable()
    {
        // DCP puts the environment on the docker command line; the script there broke Windows'
        // length limit ("fork/exec docker.exe: The filename or extension is too long").
        var (builder, supabase) = Stack(publish: false);
        supabase.WithDatabaseBackups();

        var backup = Resource(builder, "sb-backup");
        var mount = backup.Annotations.OfType<ContainerMountAnnotation>().Single(m => m.Target == "/backup");
        Assert.True(mount.IsReadOnly);
        Assert.Equal(DatabaseBackupBuilderExtensions.BackupScriptSource(), File.ReadAllText(Path.Combine(mount.Source!, "database-backup.py")));
        var args = string.Join(' ', await Args(builder, "sb-backup"));
        Assert.Contains("exec python3 -u /backup/database-backup.py", args);
        Assert.DoesNotContain("BACKUP_SCRIPT_BASE64", args);
    }

    [Fact]
    public async Task The_schedule_defaults_and_limits_come_from_the_options()
    {
        var (builder, supabase) = Stack();
        supabase.WithDatabaseBackups(o =>
        {
            o.RunAt = new TimeOnly(3, 30);
            o.TimeZone = "Europe/Berlin";
            o.KeepDaily = 7;
            o.KeepWeekly = 4;
            o.IncludeFiles = false;
            o.Enabled = false;
            o.Bucket = "backups-db";
            o.PartSizeMegabytes = 10;
            o.ExcludeSchemas.Add("scratch");
            o.KeepTables.Add("public.audit_logs");
        });

        var env = await Env(builder, "sb-backup");
        Assert.Equal("03:30", env["BACKUP_DEFAULT_RUN_AT"]);
        Assert.Equal("Europe/Berlin", env["BACKUP_DEFAULT_TIME_ZONE"]);
        Assert.Equal("7", env["BACKUP_DEFAULT_KEEP_DAILY"]);
        Assert.Equal("4", env["BACKUP_DEFAULT_KEEP_WEEKLY"]);
        Assert.Equal("false", env["BACKUP_DEFAULT_INCLUDE_FILES"]);
        Assert.Equal("false", env["BACKUP_DEFAULT_ENABLED"]);
        Assert.Equal("backups-db", env["BACKUP_BUCKET"]);
        Assert.Equal("10", env["BACKUP_PART_MB"]);
        Assert.Equal("scratch", env["BACKUP_EXCLUDE_SCHEMAS"]);
        Assert.Equal("public.audit_logs", env["BACKUP_KEEP_TABLES"]);
        Assert.Equal("0", env["BACKUP_TARGET_COUNT"]);
    }

    [Fact]
    public async Task External_targets_keep_their_secrets_as_parameters()
    {
        var (builder, supabase) = Stack();
        var sas = builder.AddParameter("backup-sas", secret: true);
        var s3Secret = builder.AddParameter("backup-s3-secret", secret: true);
        supabase.WithDatabaseBackups(o => o
            .AddAzureBlobTarget("azure", sas, prefix: "prod")
            .AddS3Target("s3", "https://s3.example.com", "dumps", "AKID", s3Secret, region: "eu-central-1"));

        var env = await Env(builder, "sb-backup");
        Assert.Equal("2", env["BACKUP_TARGET_COUNT"]);
        Assert.Equal(("azure", "azure-blob", "prod", "{backup-sas.value}"),
            (env["BACKUP_TARGET_0_NAME"], env["BACKUP_TARGET_0_KIND"], env["BACKUP_TARGET_0_PREFIX"], env["BACKUP_TARGET_0_URL"]));
        Assert.Equal(("s3", "s3", "https://s3.example.com", "dumps", "eu-central-1", "AKID", "{backup-s3-secret.value}"),
            (env["BACKUP_TARGET_1_NAME"], env["BACKUP_TARGET_1_KIND"], env["BACKUP_TARGET_1_ENDPOINT"], env["BACKUP_TARGET_1_BUCKET"],
             env["BACKUP_TARGET_1_REGION"], env["BACKUP_TARGET_1_ACCESS_KEY"], env["BACKUP_TARGET_1_SECRET_KEY"]));
    }

    [Fact]
    public void Deployed_it_runs_as_exactly_one_replica()
    {
        var (builder, supabase) = Stack();
        supabase.WithDatabaseBackups();

        var app = new ContainerApp("probe");
        var customizations = Resource(builder, "sb-backup").Annotations.OfType<AzureContainerAppCustomizationAnnotation>().ToArray();
        Assert.NotEmpty(customizations);
        foreach (var customization in customizations) customization.Configure(null!, app);
        Assert.Equal((1, 1), (app.Template.Scale.MinReplicas.Value, app.Template.Scale.MaxReplicas.Value));
    }

    [Fact]
    public void Wrong_options_fail_while_building()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Stack().Supabase.WithDatabaseBackups(o => o.KeepDaily = 0));
        Assert.Throws<ArgumentException>(() => Stack().Supabase.WithDatabaseBackups(o => o.AddAzureBlobTarget("app", ReferenceExpression.Create($"x"))));
        Assert.Throws<ArgumentException>(() => Stack().Supabase.WithDatabaseBackups(o => o
            .AddAzureBlobTarget("a", ReferenceExpression.Create($"x")).AddAzureBlobTarget("a", ReferenceExpression.Create($"y"))));
        var (_, supabase) = Stack();
        supabase.WithDatabaseBackups();
        Assert.Throws<InvalidOperationException>(() => supabase.WithDatabaseBackups());
    }

    [Fact]
    public void The_scripts_self_test_passes()
    {
        var python = new[] { "python3", "python" }.FirstOrDefault(Runs);
        if (python is null) return; // the logic is Python; without an interpreter there is nothing to run
        var file = Path.Combine(Path.GetTempPath(), $"database-backup-{Guid.NewGuid():N}.py");
        File.WriteAllText(file, DatabaseBackupBuilderExtensions.BackupScriptSource());
        try
        {
            using var process = Process.Start(new ProcessStartInfo(python, $"-I \"{file}\" --self-test")
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            })!;
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            Assert.True(process.WaitForExit(60_000));
            Assert.True(process.ExitCode == 0, output);
            Assert.Contains("self-test ok", output);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static bool Runs(string executable)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(executable, "--version")
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            });
            return process is not null && process.WaitForExit(10_000) && process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
