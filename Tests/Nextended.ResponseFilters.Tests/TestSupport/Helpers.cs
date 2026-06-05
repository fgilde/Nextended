using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nextended.ResponseFilters.Pipeline;

namespace Nextended.ResponseFilters.Tests.TestSupport;

/// <summary>Tiny helpers for spinning up a <see cref="IResponseFilterContext"/> and pipeline in tests.</summary>
internal static class Helpers
{
    public static IResponseFilterContext MakeContext(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        var provider = services.BuildServiceProvider();
        return new ResponseFilterContext(provider, CancellationToken.None);
    }

    /// <summary>
    /// Build a fully-wired pipeline plus context, register the given filter instances as the registry's
    /// implementations for their target types, and return both. Optionally tweak <see cref="ResponseFilterOptions"/>.
    /// </summary>
    public static (IResponseFilterPipeline Pipeline, IResponseFilterContext Context) BuildPipeline(
        params IResponseFilter[] filters)
        => BuildPipeline(null, filters);

    public static (IResponseFilterPipeline Pipeline, IResponseFilterContext Context) BuildPipeline(
        Action<ResponseFilterOptions>? configureOptions,
        params IResponseFilter[] filters)
    {
        var services = new ServiceCollection();

        var typeMap = new ResponseFilterTypeMap();
        foreach (var f in filters)
        {
            typeMap.Add(f.TargetType, f.GetType());
            services.AddSingleton(f.GetType(), f);
        }

        var reachability = new TypeReachabilityCache();
        reachability.SetTargetTypes(typeMap.TargetTypes);

        services.AddSingleton(typeMap);
        services.AddSingleton(reachability);
        services.AddSingleton<IResponseFilterRegistry, ResponseFilterRegistry>();
        services.AddSingleton<IResponseFilterPipeline, ResponseFilterPipeline>();
        services.AddOptions<ResponseFilterOptions>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<IResponseFilterPipeline>(),
            new ResponseFilterContext(provider));
    }
}
