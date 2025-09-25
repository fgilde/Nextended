using System;
using System.Runtime.CompilerServices;

namespace Nextended.Aspire;

public static partial class DistributedApplicationBuilderExtensions
{

    /// <summary>
    /// Conditionally adds an endpoint as an environment variable if the specified reference is not null.
    /// The key is derived from the provided expression.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <typeparam name="TTarget">The target type used to derive the key.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="keyExpression">An expression that indicates the key for the environment variable.</param>
    /// <param name="reference">The resource builder containing endpoints to retrieve the endpoint from.</param>
    /// <param name="endpointNames">The names of the endpoint to retrieve. First found will used. Defaults to "https and http".</param>
    /// <param name="caller">The caller expression used to derive the key, automatically provided by the compiler.</param>
    /// <returns>
    /// The modified resource builder with the environment variable added if the endpoint is found;
    /// otherwise, the original builder.
    /// </returns>
    /// <example>
    /// <code>
    /// builder.WithEndpointAsEnvironmentIf&lt;ProjectResource, ServerConfiguration&gt;(s => s.PublicSettings.Endpoints.Grafana, grafana);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithEndpointAsEnvironmentIf<T, TTarget>(this IResourceBuilder<T> builder,
        Func<TTarget, object> keyExpression, IResourceBuilder<IResourceWithEndpoints>? reference,
        string[] endpointNames = null,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string suffix = "",
        [CallerArgumentExpression(nameof(keyExpression))]
        string caller = null) where T : IResourceWithEnvironment
    {
        if (reference != null)
        {
            var key = string.Join("__", caller.Split('.').Skip(1).ToArray());
            return builder.WithEndpointAsEnvironment(
                environmentVariable: key,
                endpointNames: endpointNames,
                deploymentUriResolverFunc: deploymentUriResolverFunc,
                suffix: suffix
            );
        }

        return builder;
    }

    public static IResourceBuilder<T> WithEndpointAsEnvironmentIf<T, TTarget>(this IResourceBuilder<T> builder,
        Func<TTarget, object> keyExpression, IResourceBuilder<IResourceWithEndpoints>? reference,
        string endpointName,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string suffix = "",
        [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        return builder.WithEndpointAsEnvironmentIf(keyExpression, reference, [endpointName], deploymentUriResolverFunc, suffix, caller);
    }

/// <summary>
    /// Conditionally adds multiple endpoints as environment variables using a binded list.
    /// For each non-null reference, a unique key is derived using the caller expression concatenated with an index.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <typeparam name="TTarget">The target type used to derive the key.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="keyExpression">An expression that indicates the key for the environment variables.</param>
    /// <param name="references">An array of resource builders containing endpoints.</param>
    /// <param name="endpointName">The name of the endpoint to retrieve. Defaults to "http".</param>
    /// <param name="caller">The caller expression used to derive the key, automatically provided by the compiler.</param>
    /// <returns>
    /// The modified resource builder with environment variables added for each valid endpoint;
    /// otherwise, the original builder.
    /// </returns>
    /// <example>
    /// <code>
    /// builder.WithEndpointsAsEnvironmentIf&lt;ProjectResource, ServerConfiguration&gt;(s => s.PublicSettings.Endpoints, new [] { grafana, ollama, prometheus, openWebUi, pgAdmin, keycloak, stirling });
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithEndpointsAsEnvironmentIf<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, IList<string>> keyExpression,
        IResourceBuilder<IResourceWithEndpoints>[]? references,
        string[] endpointNames = null,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string suffix = "",
        [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        if (references != null)
        {
            var index = 0;
            foreach (var target in references.Where(r => r is not null))
            {
                var key = $"{string.Join("__", caller.Split('.').Skip(1).ToArray())}__{index}";
                builder.WithEndpointAsEnvironment(environmentVariable: key,
                    target: target,
                    endpointNames: endpointNames,
                    deploymentUriResolverFunc: deploymentUriResolverFunc,
                    suffix: suffix
                );
            }
        }

        return builder;
    }

    public static IResourceBuilder<T> WithEndpointsAsEnvironmentIf<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, IList<string>> keyExpression,
        IResourceBuilder<IResourceWithEndpoints>[]? references,
        string endpointName,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string suffix = "",
        [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        return builder.WithEndpointsAsEnvironmentIf(keyExpression: keyExpression,
            references: references,
            endpointNames: [endpointName],
            deploymentUriResolverFunc: deploymentUriResolverFunc,
            suffix: suffix,
            caller: caller);
    }

    /// <summary>
    /// Conditionally adds multiple endpoints as environment variables using a binded dictionary.
    /// For each non-null reference, a unique key is derived using the caller expression concatenated with the resource name.
    /// </summary>
    /// <typeparam name="T">The type of the resource that supports environment variables.</typeparam>
    /// <typeparam name="TTarget">The target type used to derive the key.</typeparam>
    /// <param name="builder">The resource builder to extend.</param>
    /// <param name="keyExpression">An expression that indicates the key for the environment variables.</param>
    /// <param name="references">An array of resource builders containing endpoints.</param>
    /// <param name="endpointName">The name of the endpoint to retrieve. Defaults to "http".</param>
    /// <param name="caller">The caller expression used to derive the key, automatically provided by the compiler.</param>
    /// <returns>
    /// The modified resource builder with environment variables added for each valid endpoint;
    /// otherwise, the original builder.
    /// </returns>
    /// <example>
    /// <code>
    /// builder.WithEndpointsAsEnvironmentIf&lt;ProjectResource, ServerConfiguration&gt;(s => s.PublicSettings.Endpoints, new [] { grafana, ollama, prometheus, openWebUi, pgAdmin, keycloak, stirling });
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithEndpointsAsEnvironmentIf<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, IDictionary<string, string>> keyExpression,
        IResourceBuilder<IResourceWithEndpoints>[]? references,
        string endpointName,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string suffix = "",
        [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        return builder.WithEndpointsAsEnvironmentIf(keyExpression: keyExpression,
            references: references,
            endpointNames: [endpointName],
            deploymentUriResolverFunc: deploymentUriResolverFunc,
            suffix: suffix,
            caller: caller);
    }

    public static IResourceBuilder<T> WithEndpointsAsEnvironmentIf<T, TTarget>(this IResourceBuilder<T> builder, Func<TTarget, IDictionary<string, string>> keyExpression,
        IResourceBuilder<IResourceWithEndpoints>[]? references,
        string[] endpointNames = null,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string suffix = "",
        [CallerArgumentExpression(nameof(keyExpression))] string caller = null) where T : IResourceWithEnvironment
    {
        if (references != null)
        {
            foreach (var target in references.Where(r => r is not null))
            {
                var key = $"{string.Join("__", caller.Split('.').Skip(1).ToArray())}__{target.Resource.Name}";
                builder.WithEndpointAsEnvironment(environmentVariable: key,
                    target: target,
                    endpointNames: endpointNames,
                    deploymentUriResolverFunc: deploymentUriResolverFunc,
                    suffix: suffix
                    );
            }
        }

        return builder;
    }


    public static IResourceBuilder<T> WithEndpointList<T>(
        this IResourceBuilder<T> builder,
        string cfgName,
        params IResourceBuilder<IResourceWithEndpoints>?[] targets
    ) where T : IResourceWithEnvironment
    {
        return builder.WithEndpointList(cfgName, deploymentUriResolverFunc: null, targets);
    }

    public static IResourceBuilder<T> WithEndpointList<T>(
        this IResourceBuilder<T> builder,
        string cfgName,
        string deploymentDomain,
        params IResourceBuilder<IResourceWithEndpoints>?[] targets
    ) where T : IResourceWithEnvironment
    {
        return builder.WithEndpointList(cfgName, (t) => BuildDeploymentDomain(t, deploymentDomain), targets);
    }


    public static IResourceBuilder<T> WithEndpointList<T>(
        this IResourceBuilder<T> builder,
        string cfgName,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        params IResourceBuilder<IResourceWithEndpoints>?[] targets
    ) where T : IResourceWithEnvironment
    {
        var i = 0;
        foreach (var endpoint in targets.SelectMany(o => o?.Resource.GetEndpoints() ?? []))
        {
            builder.WithEnvironment($"{cfgName}__{i++}", endpoint);
        }

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode && deploymentUriResolverFunc != null)
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                var buildDeploymentDomain = deploymentUriResolverFunc(target);
                if (string.IsNullOrEmpty(buildDeploymentDomain))
                {
                    builder.WithEnvironment($"{cfgName}__{i++}", buildDeploymentDomain);
                }
            }
        }

        return builder;
    }

    /// <summary>
    ///  Will set the environment variable to the first found endpoint of the target resource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="environmentVariable">Key name</param>
    /// <param name="target">Target resource or null for self reference</param>
    /// <param name="endpointNames">Specify endpoints names, the first found and existing endpoint will be used</param>
    /// <param name="suffix">If present this will be added to the url</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IResourceBuilder<T> WithEndpointAsEnvironment<T>(
        this IResourceBuilder<T> builder,
        string environmentVariable,
        string appDomain,
        IResourceBuilder<IResourceWithEndpoints>? target = null,
        string[]? endpointNames = null,
        string suffix = ""
    ) where T : IResourceWithEnvironment
    {
        return builder.WithEndpointAsEnvironment(environmentVariable, target, (t) => BuildDeploymentDomain(t, appDomain, suffix), endpointNames, suffix);
    }

    public static IResourceBuilder<T> WithEndpointAsEnvironment<T>(
        this IResourceBuilder<T> builder,
        string environmentVariable,
        IResourceBuilder<IResourceWithEndpoints>? target = null,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string[]? endpointNames = null,
        string suffix = ""
    ) where T : IResourceWithEnvironment
    {
        target ??= builder as IResourceBuilder<IResourceWithEndpoints>
                   ?? throw new ArgumentException("Target must be provided when builder is not IResourceWithEndpoints");
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode && deploymentUriResolverFunc != null)
        {
            var buildDeploymentDomain = deploymentUriResolverFunc(target);
            if (string.IsNullOrEmpty(buildDeploymentDomain))
            {
                builder.WithEnvironment(environmentVariable, buildDeploymentDomain);
                return builder;
            }
        }

        if (string.IsNullOrEmpty(suffix))
        {
            builder.WithEnvironment(environmentVariable, target.GetFirstExistingEndpoint(endpointNames));
        }
        else
        {
            builder.WithEnvironment(environmentVariable, () => target.GetFirstExistingEndpoint(endpointNames).Url + suffix);
        }

        return builder;
    }

    public static IResourceBuilder<T> WithEndpointAsEnvironment<T>(
        this IResourceBuilder<T> builder,
        string environmentVariable,
        IResourceBuilder<IResourceWithEndpoints>? target = null,
        Func<IResourceBuilder<IResourceWithEndpoints>, string?>? deploymentUriResolverFunc = null,
        string endpointName = "https",
        string suffix = "") where T : IResourceWithEnvironment
    {
        return builder.WithEndpointAsEnvironment(environmentVariable, target, deploymentUriResolverFunc, [endpointName], suffix);
    }

    public static EndpointReference GetFirstExistingEndpoint(this IResourceBuilder<IResourceWithEndpoints> builder,
        string[]? endpointNames = null)
    {
        endpointNames ??= ["https", "http"];

        foreach (var name in endpointNames)
        {
            var endpoint = builder.Resource.GetEndpoint(name);
            if (endpoint?.Exists == true)
                return endpoint;
        }

        throw new InvalidOperationException("HTTPS endpoint not found on resource " + builder.Resource.Name);

    }


    public static string? BuildDeploymentDomain(IResourceBuilder<IResourceWithEndpoints> targets, string envNameForDomain = "DEPLOYMENT_DOMAIN", string suffix = "")
        => BuildDeploymentDomains([targets], envNameForDomain, suffix);

    public static string? BuildDeploymentDomains(IEnumerable<IResourceBuilder<IResourceWithEndpoints>> targets, string envNameForDomain = "DEPLOYMENT_DOMAIN", string suffix = "")
    {
        if (string.IsNullOrEmpty(envNameForDomain))
            return null;
        var environmentVariable = Environment.GetEnvironmentVariable(envNameForDomain) ?? envNameForDomain;
        return BuildDomainName(targets, environmentVariable, suffix);
    }

    public static string[]? BuildDeploymentDomainList(IEnumerable<IResourceBuilder<IResourceWithEndpoints>> targets, string envNameForDomain = "DEPLOYMENT_DOMAIN", string suffix = "")
    {
        if (string.IsNullOrEmpty(envNameForDomain))
            return null;
        var environmentVariable = Environment.GetEnvironmentVariable(envNameForDomain) ?? envNameForDomain;
        return BuildDomainNames(targets, environmentVariable, suffix);
    }

    public static string[]? BuildDomainNames(IEnumerable<IResourceBuilder<IResourceWithEndpoints>> targets, string domainName, string suffix = "")
    {
        if (string.IsNullOrEmpty(domainName))
            return null;
        return targets.Select(t => $"https://{t.Resource.Name}.{EnsureDomain(domainName)}{suffix}").ToArray();
    }

    public static string? BuildDomainName(IEnumerable<IResourceBuilder<IResourceWithEndpoints>> targets, string domainName, string suffix = "")
    {
        var names = BuildDomainNames(targets, domainName, suffix);
        if (names == null)
            return null;
        return string.Join(",", names);
    }

    static string EnsureDomain(string input)
    {
        string host = input;

        if (!input.StartsWith("http://") && !input.StartsWith("https://"))
        {
            host = "http://" + input;
        }

        Uri uri = new Uri(host);
        string[] parts = uri.Host.Split('.');

        if (parts.Length >= 2)
        {
            return parts[^2] + "." + parts[^1];
        }

        return uri.Host;
    }

}