using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nextended.ResponseFilters.Pipeline;

namespace Nextended.ResponseFilters.Extensions;

/// <summary>DI registration for Nextended.ResponseFilters.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the response filter pipeline and discover all <see cref="ResponseFilter{T}"/> implementations
    /// in the given assemblies. Filters are registered as scoped so they can use scoped dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan. If none are provided, the calling assembly is used.</param>
    /// <param name="lifetime">Lifetime for discovered filters. Default: <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="configure">Optional callback to configure <see cref="ResponseFilterOptions"/> — exception
    /// behaviour, response-type opt-outs, reachability shortcuts. Can be combined across multiple
    /// <c>AddResponseFilters</c> calls (last write wins).</param>
    public static IServiceCollection AddResponseFilters(
        this IServiceCollection services,
        Assembly[]? assemblies = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<ResponseFilterOptions>? configure = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        assemblies ??= [Assembly.GetCallingAssembly()];

        var typeMap = services.GetOrAddTypeMap();
        var reachability = services.GetOrAddReachabilityCache();
        services.AddCoreServices();

        foreach (var asm in assemblies)
        {
            foreach (var (filterImpl, targetType) in DiscoverFilters(asm))
            {
                typeMap.Add(targetType, filterImpl);
                services.TryAddEnumerableLifetime(filterImpl, lifetime);
                services.TryAddLifetime(filterImpl, lifetime);
            }
        }

        // Synchronize the reachability cache with the final set of target types
        reachability.SetTargetTypes(typeMap.TargetTypes);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }

    /// <summary>
    /// Register a single filter manually (useful for tests or runtime-built filters).
    /// </summary>
    public static IServiceCollection AddResponseFilter<TFilter>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TFilter : class, IResponseFilter
    {
        var typeMap = services.GetOrAddTypeMap();
        var reachability = services.GetOrAddReachabilityCache();
        services.AddCoreServices();

        var targetType = ResolveTargetType(typeof(TFilter))
            ?? throw new InvalidOperationException(
                $"Cannot register {typeof(TFilter).FullName}: it does not derive from ResponseFilter<T>.");

        typeMap.Add(targetType, typeof(TFilter));
        services.TryAddLifetime(typeof(TFilter), lifetime);
        reachability.SetTargetTypes(typeMap.TargetTypes);
        return services;
    }

    private static void AddCoreServices(this IServiceCollection services)
    {
        services.AddOptions<ResponseFilterOptions>();
        services.TryAddScoped<IResponseFilterRegistry, ResponseFilterRegistry>();
        services.TryAddScoped<IResponseFilterPipeline, ResponseFilterPipeline>();
    }

    private static ResponseFilterTypeMap GetOrAddTypeMap(this IServiceCollection services)
        => services.GetOrAddSingletonInstance(() => new ResponseFilterTypeMap());

    private static TypeReachabilityCache GetOrAddReachabilityCache(this IServiceCollection services)
        => services.GetOrAddSingletonInstance(() => new TypeReachabilityCache());

    private static T GetOrAddSingletonInstance<T>(this IServiceCollection services, Func<T> factory) where T : class
    {
        var existing = services.FirstOrDefault(d =>
            d.ServiceType == typeof(T) && d.ImplementationInstance is T);
        if (existing?.ImplementationInstance is T instance)
        {
            return instance;
        }

        // Build eagerly so the registration phase can populate it.
        var created = factory();
        services.RemoveAll<T>();
        services.AddSingleton(created);
        return created;
    }

    private static void TryAddLifetime(this IServiceCollection services, Type implType, ServiceLifetime lifetime)
    {
        if (services.Any(d => d.ServiceType == implType))
        {
            return;
        }
        services.Add(new ServiceDescriptor(implType, implType, lifetime));
    }

    private static void TryAddEnumerableLifetime(this IServiceCollection services, Type implType, ServiceLifetime lifetime)
    {
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IResponseFilter), implType, lifetime));
    }

    private static IEnumerable<(Type FilterImpl, Type TargetType)> DiscoverFilters(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        foreach (var type in types)
        {
            if (type is null || type.IsAbstract || type.IsInterface) continue;
            if (!typeof(IResponseFilter).IsAssignableFrom(type)) continue;
            var target = ResolveTargetType(type);
            if (target is null) continue;
            yield return (type, target);
        }
    }

    private static Type? ResolveTargetType(Type filterType)
    {
        var cur = filterType;
        while (cur != null && cur != typeof(object))
        {
            if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typeof(ResponseFilter<>))
            {
                return cur.GetGenericArguments()[0];
            }
            cur = cur.BaseType;
        }
        return null;
    }
}
