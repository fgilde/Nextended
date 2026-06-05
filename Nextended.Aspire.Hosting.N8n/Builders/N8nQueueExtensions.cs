using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire.Hosting.N8n.Resources;
using static Nextended.Aspire.Hosting.N8n.Helpers.N8nLogger;

namespace Nextended.Aspire.Hosting.N8n.Builders;

/// <summary>
/// Provides extension methods for enabling n8n queue mode (Redis + worker containers).
/// </summary>
public static class N8nQueueExtensions
{
    /// <summary>
    /// Enables n8n queue mode. A Redis container is added as the message broker and the
    /// requested number of worker containers is created to process executions.
    /// Queue mode requires a PostgreSQL backend (the default). Call after <c>WithDatabase(...)</c>.
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="workers">The number of worker containers to create. Default: 1.</param>
    /// <param name="redisPassword">
    /// Optional Aspire parameter supplying the Redis password (recommended for deployments).
    /// When omitted, a stable development default is used.
    /// </param>
    public static IResourceBuilder<N8nResource> WithQueueMode(
        this IResourceBuilder<N8nResource> builder,
        int workers = 1,
        IResourceBuilder<ParameterResource>? redisPassword = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workers);

        var resource = builder.Resource;
        var app = resource.AppBuilder
                  ?? throw new InvalidOperationException("AppBuilder not available. Was AddN8n() called?");

        if (redisPassword is not null)
            resource.RedisPasswordParameter = redisPassword.Resource;

        if (resource.UsesSqlite)
            LogWarning("Queue mode requires a shared PostgreSQL backend; SQLite is not supported by n8n workers.");

        if (!resource.QueueModeEnabled)
        {
            // Plain (non-TLS) Redis container. Aspire's AddRedis enables TLS with a self-signed
            // certificate, which the n8n/ioredis client cannot consume out of the box.
            // The password is read lazily so WithRedisPassword(...) works in any call order.
            resource.RedisPassword ??= N8nBuilderExtensions.Defaults.RedisPassword;
            var redis = app.AddContainer(
                    $"{resource.Name}-redis",
                    N8nBuilderExtensions.Defaults.RedisImage,
                    N8nBuilderExtensions.Defaults.RedisImageTag)
                .WithEndpoint(targetPort: N8nBuilderExtensions.Defaults.RedisPort, name: "tcp", scheme: "tcp")
                .WithArgs(ctx =>
                {
                    ctx.Args.Add("--requirepass");
                    ctx.Args.Add(resource.RedisPasswordValue ?? N8nBuilderExtensions.Defaults.RedisPassword);
                })
                .WithContainerRuntimeArgs("--restart=on-failure:10");

            resource.Redis = redis;
            resource.QueueModeEnabled = true;
            redis.WithParentRelationship(resource);
            builder.WaitFor(redis);
            LogInformation($"n8n '{resource.Name}': queue mode enabled (Redis '{redis.Resource.Name}').");
        }

        EnsureWorkers(builder, workers);
        return builder;
    }

    /// <summary>
    /// Sets the Redis password (queue mode) from a plain string. Prefer the parameter overload for deployments.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithRedisPassword(
        this IResourceBuilder<N8nResource> builder, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        builder.Resource.RedisPassword = password;
        builder.Resource.RedisPasswordParameter = null;
        return builder;
    }

    /// <summary>
    /// Sets the Redis password (queue mode) from an Aspire parameter, so the secret flows through
    /// user secrets locally and Key Vault on deployment.
    /// </summary>
    public static IResourceBuilder<N8nResource> WithRedisPassword(
        this IResourceBuilder<N8nResource> builder, IResourceBuilder<ParameterResource> password)
    {
        ArgumentNullException.ThrowIfNull(password);
        builder.Resource.RedisPasswordParameter = password.Resource;
        return builder;
    }

    /// <summary>
    /// Ensures the given number of n8n worker containers exist. Enables queue mode (incl. Redis)
    /// when it has not been enabled yet.
    /// </summary>
    /// <param name="builder">The n8n resource builder.</param>
    /// <param name="count">The total number of worker containers desired.</param>
    public static IResourceBuilder<N8nResource> WithWorkers(
        this IResourceBuilder<N8nResource> builder, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (!builder.Resource.QueueModeEnabled)
            return builder.WithQueueMode(count);

        EnsureWorkers(builder, count);
        return builder;
    }

    private static void EnsureWorkers(IResourceBuilder<N8nResource> builder, int desiredCount)
    {
        var resource = builder.Resource;
        var app = resource.AppBuilder!;
        var isPublishMode = app.ExecutionContext.IsPublishMode;

        if (desiredCount < resource.Workers.Count)
        {
            LogWarning($"Reducing the worker count from {resource.Workers.Count} to {desiredCount} is not supported; keeping existing workers.");
            return;
        }

        for (var index = resource.Workers.Count + 1; index <= desiredCount; index++)
        {
            var worker = new N8nWorkerResource($"{resource.Name}-worker-{index}") { Parent = resource };

            var workerBuilder = app.AddResource(worker)
                .WithImage(resource.Image, resource.ImageTag)
                .WithArgs("worker")
                .WithEnvironment(ctx => N8nBuilderExtensions.ApplyEnvironment(ctx, resource, isPublishMode))
                .WithContainerRuntimeArgs("--restart=on-failure:10")
                .WaitFor(builder);

            if (resource.Redis is { } redis)
                workerBuilder.WaitFor(redis);
            if (resource.Database is { } database)
                workerBuilder.WaitFor(database);

            workerBuilder.WithParentRelationship(resource);
            resource.Workers.Add(workerBuilder);
        }

        LogInformation($"n8n '{resource.Name}': {resource.Workers.Count} worker(s) configured.");
    }
}
