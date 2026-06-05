using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;
using static Nextended.Aspire.Hosting.N8n.Helpers.N8nLogger;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides the main extension method for adding n8n to an Aspire application.
/// </summary>
public static class N8nBuilderExtensions
{
    #region Constants

    internal static class Defaults
    {
        public const string Image = "n8nio/n8n";
        public const string ImageTag = "1.110.1";
        public const int ContainerPort = 5678;
        public const int HostPort = 5678;
        public const string DatabaseName = "n8n";
        public const string DataMountTarget = "/home/node/.n8n";
        public const string EncryptionKey = "n8n-insecure-dev-encryption-key-change-me";

        // Queue mode Redis. We run a plain (non-TLS) Redis container on purpose:
        // Aspire's AddRedis enables TLS with a self-signed cert on the primary port, which the
        // n8n/ioredis client cannot use out of the box (it speaks plaintext -> connection reset).
        public const string RedisImage = "redis";
        public const string RedisImageTag = "7.4-alpine";
        public const int RedisPort = 6379;
        public const string RedisPassword = "n8n-redis-insecure-dev-password";
    }

    #endregion

    #region Main Entry Point

    /// <summary>
    /// Adds an n8n workflow-automation instance to the application.
    /// By default a dedicated PostgreSQL container is created as the n8n backend.
    /// Use <c>WithDatabase(...)</c> to attach an existing Aspire PostgreSQL resource or
    /// <c>WithSqlite()</c> to use the bundled SQLite database instead.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the n8n resource (shown in the Aspire dashboard).</param>
    /// <param name="httpPort">Optional fixed host port for the n8n editor (local development only). Defaults to 5678.</param>
    /// <returns>A resource builder for further configuration.</returns>
    public static IResourceBuilder<N8nResource> AddN8n(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? httpPort = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var isPublishMode = builder.ExecutionContext.IsPublishMode;

        var resource = new N8nResource(name)
        {
            AppBuilder = builder,
            EncryptionKey = Defaults.EncryptionKey,
            HostPort = httpPort ?? Defaults.HostPort
        };

        // --- Default PostgreSQL backend ---------------------------------------------------
        // Created eagerly so the common case "just works". If the caller later supplies an
        // external database via WithDatabase(...) or switches to SQLite via WithSqlite(),
        // these auto-created resources are removed again (see N8nDatabaseExtensions).
        var postgres = builder.AddPostgres($"{name}-pg");
        var database = postgres.AddDatabase($"{name}-db", Defaults.DatabaseName);
        resource.PostgresServer = postgres;
        resource.Database = database;
        resource.OwnsDatabase = true;

        // --- n8n main container ------------------------------------------------------------
        var n8nBuilder = builder.AddResource(resource)
            .WithImage(Defaults.Image, Defaults.ImageTag)
            .WithHttpEndpoint(
                port: isPublishMode ? null : resource.HostPort,
                targetPort: Defaults.ContainerPort,
                name: N8nResource.HttpEndpointName)
            .WithEnvironment(ctx => ApplyEnvironment(ctx, resource, isPublishMode))
            .WithContainerRuntimeArgs("--restart=on-failure:10")
            .WaitFor(database);

        resource.SelfBuilder = n8nBuilder;

        // --- Persistence -------------------------------------------------------------------
        if (isPublishMode)
        {
            var volPrefix = name.Length > 12 ? name[..12] : name;
            n8nBuilder.WithVolume($"{volPrefix}-data", Defaults.DataMountTarget);
        }
        else
        {
            var dataDir = Path.Combine(builder.AppHostDirectory, "..", "infra", "n8n", name, "data");
            Directory.CreateDirectory(dataDir);
            resource.DataBindMountPath = dataDir;
            n8nBuilder.WithBindMount(dataDir, Defaults.DataMountTarget);
        }

        // --- Visual grouping ---------------------------------------------------------------
        postgres.WithParentRelationship(resource);

        // --- Azure Container Apps ----------------------------------------------------------
        if (isPublishMode)
        {
            n8nBuilder.WithExternalHttpEndpoints();
        }

        LogInformation($"n8n '{name}' added (image {Defaults.Image}:{Defaults.ImageTag}, PostgreSQL backend).");
        return n8nBuilder;
    }

    #endregion

    #region Environment

    /// <summary>
    /// Applies the full n8n environment based on the current resource state.
    /// Runs as a deferred callback so configuration applied after <c>AddN8n</c>
    /// (database, queue mode, URLs, auth, ...) is always reflected.
    /// </summary>
    internal static void ApplyEnvironment(EnvironmentCallbackContext ctx, N8nResource resource, bool isPublishMode)
    {
        var env = ctx.EnvironmentVariables;

        // Core
        env["N8N_PORT"] = Defaults.ContainerPort.ToString();
        env["N8N_PROTOCOL"] = isPublishMode ? "https" : "http";
        env["N8N_HOST"] = isPublishMode
            ? resource.HttpEndpoint.Property(EndpointProperty.Host)
            : "localhost";
        env["N8N_ENCRYPTION_KEY"] = resource.EncryptionKeyValue;
        env["GENERIC_TIMEZONE"] = resource.Timezone;
        env["TZ"] = resource.Timezone;

        // Sensible defaults for a self-hosted dev/CI instance
        env["N8N_DIAGNOSTICS_ENABLED"] = "false";
        env["N8N_HIRING_BANNER_ENABLED"] = "false";
        env["N8N_PERSONALIZATION_ENABLED"] = "false";
        env["N8N_VERSION_NOTIFICATIONS_ENABLED"] = "false";
        env["N8N_RUNNERS_ENABLED"] = "true";
        env["N8N_SECURE_COOKIE"] = isPublishMode ? "true" : "false";

        // Database
        if (!resource.UsesSqlite && resource.Database is { } db)
        {
            var server = db.Resource.Parent;
            var ep = server.PrimaryEndpoint;

            env["DB_TYPE"] = "postgresdb";
            env["DB_POSTGRESDB_HOST"] = ep.Property(EndpointProperty.Host);
            env["DB_POSTGRESDB_PORT"] = ep.Property(EndpointProperty.Port);
            env["DB_POSTGRESDB_DATABASE"] = db.Resource.DatabaseName;
            env["DB_POSTGRESDB_USER"] = server.UserNameParameter is { } user ? user : "postgres";
            env["DB_POSTGRESDB_PASSWORD"] = server.PasswordParameter;
            env["DB_POSTGRESDB_SCHEMA"] = "public";
        }
        else
        {
            env["DB_TYPE"] = "sqlite";
            env["DB_SQLITE_POOL_SIZE"] = "5";
        }

        // Basic auth
        if (!string.IsNullOrEmpty(resource.BasicAuthUser))
        {
            env["N8N_BASIC_AUTH_ACTIVE"] = "true";
            env["N8N_BASIC_AUTH_USER"] = resource.BasicAuthUser;
            env["N8N_BASIC_AUTH_PASSWORD"] = resource.BasicAuthPassword ?? string.Empty;
        }

        // Public URLs
        if (!string.IsNullOrEmpty(resource.WebhookUrl))
            env["WEBHOOK_URL"] = resource.WebhookUrl;
        else if (isPublishMode)
            env["WEBHOOK_URL"] = resource.HttpEndpoint.Property(EndpointProperty.Url);

        if (!string.IsNullOrEmpty(resource.EditorBaseUrl))
            env["N8N_EDITOR_BASE_URL"] = resource.EditorBaseUrl;
        else if (isPublishMode)
            env["N8N_EDITOR_BASE_URL"] = resource.HttpEndpoint.Property(EndpointProperty.Url);

        if (isPublishMode)
            env["N8N_PROXY_HOPS"] = "1";

        // Queue mode (Redis)
        if (resource.QueueModeEnabled && resource.Redis is { } redis)
        {
            var redisEp = redis.GetEndpoint("tcp");
            env["EXECUTIONS_MODE"] = "queue";
            env["QUEUE_BULL_REDIS_HOST"] = redisEp.Property(EndpointProperty.Host);
            env["QUEUE_BULL_REDIS_PORT"] = redisEp.Property(EndpointProperty.Port);
            if (resource.RedisPasswordValue is { } redisPwd)
                env["QUEUE_BULL_REDIS_PASSWORD"] = redisPwd;
            env["QUEUE_HEALTH_CHECK_ACTIVE"] = "true";
        }
    }

    #endregion
}
