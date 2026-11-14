using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.Supabase.Resources;

namespace Nextended.Aspire.Hosting.Supabase.Builders;

/// <summary>
/// Scheduled database backups of the stack: a small service container (the stack's own
/// supabase/postgres image, so pg_dump matches the server) that backs up the database and the
/// stored files, keeps them in the stack's storage and optional external targets, applies a
/// retention and restores a backup on request. Settings and the list of backups live in the
/// schema <c>nx_backup</c>; an app shows them through the functions there.
/// </summary>
public static class DatabaseBackupBuilderExtensions
{
    /// <summary>
    /// Adds the backup service (<c>{stack}-backup</c>). One call per stack.
    /// </summary>
    /// <example>
    /// <code>
    /// supabase.WithDatabaseBackups(o =>
    /// {
    ///     o.RunAt = new TimeOnly(2, 0);
    ///     o.TimeZone = "Europe/Berlin";
    ///     o.AddAzureBlobTarget("azure", builder.AddParameter("backup-sas-url", secret: true), prefix: "prod");
    /// });
    /// </code>
    /// </example>
    public static IResourceBuilder<SupabaseStackResource> WithDatabaseBackups(
        this IResourceBuilder<SupabaseStackResource> builder,
        Action<SupabaseBackupOptions>? configure = null)
    {
        var stack = builder.Resource;
        if (stack.Storage is null || stack.DatabaseEndpoint is null || stack.AppBuilder is null)
            throw new InvalidOperationException("Storage/database not configured. Ensure AddSupabase() has been called.");
        if (stack.Backup is not null)
            throw new InvalidOperationException("WithDatabaseBackups was already called for this stack.");

        var options = new SupabaseBackupOptions();
        configure?.Invoke(options);
        options.Validate();

        var db = stack.DatabaseEndpoint;
        var name = $"{stack.Name}-backup";
        var container = stack.AppBuilder.AddContainer(name, SupabaseBuilderExtensions.Images.Postgres, SupabaseBuilderExtensions.Images.PostgresTag)
            .WithContainerName(name)
            .WithEnvironment("DB_HOST", db.Property(EndpointProperty.Host))
            .WithEnvironment("DB_PORT", db.Property(EndpointProperty.Port))
            .WithStackValue("DB_PASSWORD", () => stack.DatabasePassword)
            .WithEnvironment("STORAGE_URL", stack.Storage.GetEndpoint("http"))
            .WithStackValue("SERVICE_KEY", () => stack.ServiceRoleKeyValue.EnvironmentValue)
            .WithEnvironment("BACKUP_STACK_NAME", stack.Name)
            .WithEnvironment("BACKUP_BUCKET", options.Bucket)
            .WithEnvironment("BACKUP_PART_MB", options.PartSizeMegabytes.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithEnvironment("BACKUP_POLL_SECONDS", options.PollSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithEnvironment("BACKUP_EXCLUDE_SCHEMAS", string.Join(',', options.ExcludeSchemas))
            .WithEnvironment("BACKUP_KEEP_TABLES", string.Join(',', options.KeepTables))
            .WithEnvironment("BACKUP_DEFAULT_ENABLED", options.Enabled ? "true" : "false")
            .WithEnvironment("BACKUP_DEFAULT_RUN_AT", options.RunAt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture))
            .WithEnvironment("BACKUP_DEFAULT_TIME_ZONE", options.TimeZone)
            .WithEnvironment("BACKUP_DEFAULT_KEEP_DAILY", options.KeepDaily.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithEnvironment("BACKUP_DEFAULT_KEEP_WEEKLY", options.KeepWeekly.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithEnvironment("BACKUP_DEFAULT_INCLUDE_FILES", options.IncludeFiles ? "true" : "false")
            .WithEnvironment("BACKUP_TARGET_COUNT", options.Targets.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithEntrypoint("/bin/bash")
            // The image's own health check asks for a postgres server this container does not run.
            .WithContainerRuntimeArgs("--no-healthcheck")
            .WaitFor(stack.DatabaseWaitTarget!)
            .WaitFor(stack.Storage);

        if (stack.AppBuilder.ExecutionContext.IsPublishMode || stack.InfraRootDir is null)
        {
            // Deployed the script travels in the environment (no file to mount there).
            container
                .WithEnvironment("BACKUP_SCRIPT_BASE64", Convert.ToBase64String(Encoding.UTF8.GetBytes(BackupScriptSource())))
                .WithArgs("-c", "printf '%s' \"$BACKUP_SCRIPT_BASE64\" | base64 -d > /tmp/database-backup.py && exec python3 -u /tmp/database-backup.py");
        }
        else
        {
            // Locally as a mounted file: the environment goes onto the docker command line, which
            // the script would push past Windows' length limit ("The filename or extension is too long").
            var dir = Path.Combine(stack.InfraRootDir, "backup");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "database-backup.py"), BackupScriptSource());
            container
                .WithBindMount(dir, "/backup", isReadOnly: true)
                .WithArgs("-c", "exec python3 -u /backup/database-backup.py");
        }

        for (var i = 0; i < options.Targets.Count; i++)
            foreach (var (field, value) in options.Targets[i].Fields)
                container.WithEnvironment($"BACKUP_TARGET_{i}_{field}", value);

        // Exactly one replica: the schedule and the restore must never run twice at once.
        container.PublishAsAcaWhenTargeted((_, app) =>
        {
            app.Template.Scale.MinReplicas = 1;
            app.Template.Scale.MaxReplicas = 1;
        });

        stack.Backup = container;
        return builder;
    }

    /// <summary>The service script (Python 3, standard library only).</summary>
    public static string BackupScriptSource() => BackupScript.Value;

    private static readonly Lazy<string> BackupScript = new(() =>
    {
        using var stream = typeof(DatabaseBackupBuilderExtensions).Assembly.GetManifestResourceStream("database-backup.py")
            ?? throw new InvalidOperationException("database-backup.py is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
}

/// <summary>Options of <see cref="DatabaseBackupBuilderExtensions.WithDatabaseBackups"/>.</summary>
/// <remarks>
/// The schedule values are the defaults of the very first start: from then on they live in
/// <c>nx_backup.settings</c> and an app may change them at runtime. Targets and their secrets
/// stay in the deployment.
/// </remarks>
public sealed class SupabaseBackupOptions
{
    /// <summary>Scheduled backups on. Default true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Local time of the daily backup. Default 02:00.</summary>
    public TimeOnly RunAt { get; set; } = new(2, 0);

    /// <summary>IANA time zone of <see cref="RunAt"/> and of the days the retention counts. Default UTC.</summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>Scheduled backups kept: the newest of this many days. Default 14, at least 1.</summary>
    public int KeepDaily { get; set; } = 14;

    /// <summary>On top: the newest backup of each of this many weeks. Default 8.</summary>
    public int KeepWeekly { get; set; } = 8;

    /// <summary>Back up the stored files too, not only the database. Default true.</summary>
    public bool IncludeFiles { get; set; } = true;

    /// <summary>Storage bucket of the backups in the stack itself (private). Default <c>db-backups</c>.</summary>
    public string Bucket { get; set; } = "db-backups";

    /// <summary>A backup is written in parts of this size (stays below the storage's upload limit). Default 25.</summary>
    public int PartSizeMegabytes { get; set; } = 25;

    /// <summary>How often the service looks for work. Default 30 seconds.</summary>
    public int PollSeconds { get; set; } = 30;

    /// <summary>Schemas to leave out on top of Supabase's own (extensions, realtime, vault, cron …).</summary>
    public List<string> ExcludeSchemas { get; } = [];

    /// <summary>
    /// Tables (<c>schema.table</c>) whose rows a restore leaves as they are, on top of the migration
    /// bookkeeping (<c>public._aspire_applied_migrations</c>, <c>auth.schema_migrations</c>, <c>storage.migrations</c>).
    /// </summary>
    public List<string> KeepTables { get; } = [];

    internal List<BackupTarget> Targets { get; } = [];

    /// <summary>
    /// Also writes every backup to an Azure Blob container, addressed by a container SAS URL
    /// (permissions read, add, create, write, delete, list). A value that is no URL at runtime
    /// (empty, <c>none</c>) switches the target off, so one parameter can serve every environment.
    /// </summary>
    /// <param name="name">Name shown with the backups.</param>
    /// <param name="containerSasUrl">https://&lt;account&gt;.blob.core.windows.net/&lt;container&gt;?&lt;sas&gt;</param>
    /// <param name="prefix">Folder in the container — several deployments can share one.</param>
    public SupabaseBackupOptions AddAzureBlobTarget(string name, IResourceBuilder<ParameterResource> containerSasUrl, string? prefix = null) =>
        AddAzureBlobTarget(name, ReferenceExpression.Create($"{containerSasUrl.Resource}"), prefix);

    /// <inheritdoc cref="AddAzureBlobTarget(string, IResourceBuilder{ParameterResource}, string?)"/>
    public SupabaseBackupOptions AddAzureBlobTarget(string name, ReferenceExpression containerSasUrl, string? prefix = null)
    {
        Targets.Add(new BackupTarget(name, "azure-blob", prefix)
        {
            Fields = { ["URL"] = containerSasUrl },
        });
        return this;
    }

    /// <summary>
    /// Also writes every backup to an S3-compatible bucket (AWS, MinIO, …; path-style). An endpoint
    /// that is no URL (empty, <c>none</c>) or an empty secret at runtime switches the target off.
    /// </summary>
    public SupabaseBackupOptions AddS3Target(string name, ReferenceExpression endpoint, string bucket, string accessKey,
        ReferenceExpression secretKey, string region = "us-east-1", string? prefix = null)
    {
        Targets.Add(new BackupTarget(name, "s3", prefix)
        {
            Fields =
            {
                ["ENDPOINT"] = endpoint,
                ["BUCKET"] = ReferenceExpression.Create($"{bucket}"),
                ["REGION"] = ReferenceExpression.Create($"{region}"),
                ["ACCESS_KEY"] = ReferenceExpression.Create($"{accessKey}"),
                ["SECRET_KEY"] = secretKey,
            },
        });
        return this;
    }

    /// <inheritdoc cref="AddS3Target(string, ReferenceExpression, string, string, ReferenceExpression, string, string?)"/>
    public SupabaseBackupOptions AddS3Target(string name, string endpoint, string bucket, string accessKey,
        IResourceBuilder<ParameterResource> secretKey, string region = "us-east-1", string? prefix = null) =>
        AddS3Target(name, ReferenceExpression.Create($"{endpoint}"), bucket, accessKey,
            ReferenceExpression.Create($"{secretKey.Resource}"), region, prefix);

    internal void Validate()
    {
        if (KeepDaily < 1) throw new ArgumentOutOfRangeException(nameof(KeepDaily), "At least one daily backup is kept.");
        if (KeepWeekly < 0) throw new ArgumentOutOfRangeException(nameof(KeepWeekly));
        if (PartSizeMegabytes < 1) throw new ArgumentOutOfRangeException(nameof(PartSizeMegabytes));
        if (string.IsNullOrWhiteSpace(Bucket)) throw new ArgumentException("A bucket name is required.", nameof(Bucket));
        foreach (var target in Targets)
        {
            if (string.IsNullOrWhiteSpace(target.Name) || target.Name == "app")
                throw new ArgumentException($"Target name '{target.Name}' is empty or reserved ('app' is the stack's own storage).");
        }
        if (Targets.Select(t => t.Name).Distinct(StringComparer.Ordinal).Count() != Targets.Count)
            throw new ArgumentException("Target names must be unique.");
    }

    internal sealed class BackupTarget(string name, string kind, string? prefix)
    {
        public string Name { get; } = name;

        public Dictionary<string, ReferenceExpression> Fields { get; } = new()
        {
            ["NAME"] = ReferenceExpression.Create($"{name}"),
            ["KIND"] = ReferenceExpression.Create($"{kind}"),
            ["PREFIX"] = ReferenceExpression.Create($"{prefix ?? ""}"),
        };
    }
}
