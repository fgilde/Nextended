using System.Collections.Concurrent;
using Nextended.Core.Extensions;

namespace Nextended.ResponseFilters.Pipeline;

/// <summary>
/// Precomputes "is any registered filter target reachable from <c>rootType</c>?" so the pipeline
/// can early-out for responses that have nothing to filter. Walks the type graph once per root,
/// caches the answer.
/// </summary>
/// <remarks>
/// Polymorphism guard: types whose subgraph contains <see cref="object"/> or interfaces that the
/// static walker can't resolve are conservatively treated as "may be affected" — the pipeline will
/// run for them rather than risk a false negative.
/// </remarks>
public sealed class TypeReachabilityCache
{
    private readonly ConcurrentDictionary<Type, bool> _cache = new();
    private HashSet<Type> _targetTypes = new();

    /// <summary>Replace the set of registered target types. Invalidates the cache.</summary>
    /// <remarks>Called by the registry at startup; safe to call again if filters are added dynamically.</remarks>
    public void SetTargetTypes(IEnumerable<Type> targetTypes)
    {
        _targetTypes = new HashSet<Type>(targetTypes);
        _cache.Clear();
    }

    /// <summary>
    /// True if <paramref name="rootType"/> itself or any reachable navigable property type matches
    /// a registered filter target. Conservative: returns true on uncertainty (e.g. <see cref="object"/>
    /// in the graph) to avoid false negatives.
    /// </summary>
    public bool MayBeAffected(Type rootType)
    {
        if (rootType == typeof(object)) return true;          // can hold anything → must walk
        if (rootType.IsScalar()) return false;                 // primitive/leaf, no children
        if (_targetTypes.Count == 0) return false;             // no filters registered at all

        return _cache.GetOrAdd(rootType, Walk);
    }

    private bool Walk(Type rootType)
    {
        var visited = new HashSet<Type>();
        return WalkInternal(rootType, visited);
    }

    private bool WalkInternal(Type type, HashSet<Type> visited)
    {
        if (!visited.Add(type)) return false;        // cycle guard
        if (_targetTypes.Contains(type)) return true; // direct hit
        if (type == typeof(object)) return true;     // open polymorphism — assume yes
        if (type.IsInterface) return true;           // can't enumerate concrete implementations statically
        if (type.IsScalar()) return false;

        // For arrays / IEnumerable<T>, the element type may carry navigable properties of its own.
        var elementType = TypeGraphInspector.UnwrapEnumerable(type);
        if (elementType != type && WalkInternal(elementType, visited)) return true;

        foreach (var nav in TypeGraphInspector.GetNavigableProperties(type))
        {
            if (WalkInternal(nav.MemberType, visited)) return true;
        }

        return false;
    }
}
