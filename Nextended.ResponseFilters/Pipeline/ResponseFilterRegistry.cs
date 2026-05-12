using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Nextended.ResponseFilters.Pipeline;

/// <summary>
/// Default registry: scoped, looks up <see cref="IResponseFilter"/> implementations via DI
/// and caches the type → filter-implementations mapping for the lifetime of the host.
/// </summary>
/// <remarks>
/// Filters themselves are scoped (so they can capture per-request dependencies) but the
/// type-to-types map is built once per process from <c>IServiceCollection</c> snapshot via
/// <see cref="ResponseFilterTypeMap"/>.
/// </remarks>
public sealed class ResponseFilterRegistry : IResponseFilterRegistry
{
    private readonly IServiceProvider _services;
    private readonly ResponseFilterTypeMap _typeMap;
    private static readonly IReadOnlyList<IResponseFilter> Empty = Array.Empty<IResponseFilter>();

    public ResponseFilterRegistry(IServiceProvider services, ResponseFilterTypeMap typeMap)
    {
        _services = services;
        _typeMap = typeMap;
    }

    public IReadOnlyList<IResponseFilter> GetFilters(Type type)
    {
        if (!_typeMap.TryGet(type, out var implTypes))
        {
            return Empty;
        }

        var result = new IResponseFilter[implTypes.Length];
        for (var i = 0; i < implTypes.Length; i++)
        {
            result[i] = (IResponseFilter)_services.GetRequiredService(implTypes[i]);
        }
        return result;
    }

    public bool HasFilters(Type type) => _typeMap.TryGet(type, out _);
}

/// <summary>
/// Process-wide cache of target type → filter implementation types.
/// Populated at startup by <c>AddResponseFilters</c>; thread-safe for read.
/// </summary>
public sealed class ResponseFilterTypeMap
{
    private readonly ConcurrentDictionary<Type, Type[]> _map = new();

    public void Add(Type targetType, Type filterImplType)
    {
        _map.AddOrUpdate(
            targetType,
            _ => new[] { filterImplType },
            (_, existing) => existing.Contains(filterImplType)
                ? existing
                : existing.Append(filterImplType).ToArray());
    }

    public bool TryGet(Type targetType, out Type[] implTypes)
    {
        if (_map.TryGetValue(targetType, out var arr))
        {
            implTypes = arr;
            return true;
        }
        implTypes = Array.Empty<Type>();
        return false;
    }
}
