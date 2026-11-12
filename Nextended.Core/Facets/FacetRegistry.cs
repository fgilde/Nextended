using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nextended.Core.Facets;

/// <summary>
/// Facets registered in code next to <see cref="ProvideFacetAttribute"/>: for types that cannot or should not carry the attribute
/// (a copied or referenced model). Nextended.CodeGen writes these registrations from the fluent <c>Facet(...)</c> rules (a module
/// initializer); <see cref="Register"/> can also be called directly. <see cref="FacetBuilder"/> reads facets through <see cref="GetFacets"/>.
/// </summary>
public static class FacetRegistry
{
    private static readonly ConcurrentDictionary<(Type Type, string Property), List<Action<ProvideFacetAttribute>>> Registrations = new();
    private static int _version;

    /// <summary>Changes with every registration (for caches of <see cref="GetFacets"/>).</summary>
    public static int Version => _version;

    /// <summary>
    /// Registers a facet for the property <paramref name="propertyName"/> of <paramref name="type"/> (a generic type definition covers all its
    /// constructions; a base type reaches the derived types). <paramref name="configure"/> sets its values; on a property with
    /// <see cref="ProvideFacetAttribute"/> it changes the attribute's values. Several registrations apply in order.
    /// </summary>
    public static void Register(Type type, string propertyName, Action<ProvideFacetAttribute>? configure = null)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("A property name is required.", nameof(propertyName));
        var list = Registrations.GetOrAdd((Key(type), propertyName), _ => []);
        lock (list)
            list.Add(configure ?? (_ => { }));
        System.Threading.Interlocked.Increment(ref _version);
    }

    /// <summary>
    /// The facets of the public instance properties of <paramref name="type"/>: <see cref="ProvideFacetAttribute"/>s and registrations,
    /// ordered by <see cref="ProvideFacetAttribute.Order"/>. Runs the module initializers of the type's assemblies first.
    /// </summary>
    public static (PropertyInfo Property, ProvideFacetAttribute Facet)[] GetFacets(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        EnsureRegistered(type);
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(p => Facets(type, p).Select(facet => (Property: p, Facet: facet)))
            .OrderBy(x => x.Facet.Order)
            .ToArray();
    }

    /// <summary>The facet of one property of <paramref name="type"/> (attribute and registrations), null when it has none.</summary>
    public static ProvideFacetAttribute? GetFacet(Type type, PropertyInfo property)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (property == null) throw new ArgumentNullException(nameof(property));
        EnsureRegistered(type);
        return Facets(type, property).LastOrDefault();
    }

    private static IEnumerable<ProvideFacetAttribute> Facets(Type type, PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes<ProvideFacetAttribute>(true).ToList();
        if (Registrations.IsEmpty)
            return attributes;
        // from the most general type to the type itself: the nearest registration wins
        var chain = new List<Type>();
        for (var t = type; t != null; t = t.BaseType)
            chain.Insert(0, t);
        foreach (var t in chain)
        {
            if (!Registrations.TryGetValue((Key(t), property.Name), out var actions))
                continue;
            if (attributes.Count == 0)
                attributes.Add(new ProvideFacetAttribute());
            Action<ProvideFacetAttribute>[] snapshot;
            lock (actions)
                snapshot = actions.ToArray();
            foreach (var attribute in attributes)
                foreach (var action in snapshot)
                    action(attribute);
        }
        return attributes;
    }

    private static Type Key(Type type) => type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;

    // the generated registrations run in a module initializer, which runs only once code of the assembly runs: run it now
    private static void EnsureRegistered(Type type)
    {
        for (var t = type; t != null; t = t.BaseType)
            RuntimeHelpers.RunModuleConstructor(t.Module.ModuleHandle);
    }
}
