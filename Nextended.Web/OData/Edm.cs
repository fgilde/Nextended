using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Nextended.Web;

public static class Edm
{
    private static IEdmModel? _cachedModel;
    private static IDictionary<Type, IEnumerable<ProvideAsEdmAttribute>>? _providedTypes;

    private static readonly Dictionary<string, Type> _entitySetClrMap = new(StringComparer.Ordinal);
    public static IReadOnlyDictionary<string, Type> EntitySetClrMap => _entitySetClrMap;

    public static IEdmModel GetEdmModel()
    {
        if (_cachedModel != null) return _cachedModel;

        var modelBuilder = new ODataConventionModelBuilder();
        _providedTypes ??= CollectProvidedTypes();

        var entities = BuildEntityTypes(modelBuilder, _providedTypes.Keys);
        WireInheritance(entities);
        EnsureEntitySetsForProvided(modelBuilder, entities, _providedTypes);
        EnsureEntitySetsForNavTargets(modelBuilder, entities);

        _cachedModel = modelBuilder.GetEdmModel()!;
        return _cachedModel;
    }

    private static Dictionary<Type, IEnumerable<ProvideAsEdmAttribute>> CollectProvidedTypes()
    {
        var result = new Dictionary<Type, IEnumerable<ProvideAsEdmAttribute>>();
        var allTypes = GetAllTypes();

        var withAttr = allTypes
            .Select(t => new { Type = t, Attrs = t.GetCustomAttributes<ProvideAsEdmAttribute>(inherit: false).ToArray() })
            .Where(x => x.Attrs.Length > 0)
            .ToArray();

        foreach (var x in withAttr)
            result[x.Type] = x.Attrs;

        foreach (var x in withAttr.Where(x => x.Attrs.Any(a => a.ProvideInherits)))
        {
            foreach (var d in allTypes)
            {
                if (d == x.Type || !d.IsClass) continue;
                if (!x.Type.IsAssignableFrom(d)) continue;
                if (!result.ContainsKey(d)) result[d] = Array.Empty<ProvideAsEdmAttribute>();
            }
        }

        return result;
    }

    private static Dictionary<Type, EntityTypeConfiguration> BuildEntityTypes(
        ODataConventionModelBuilder builder,
        IEnumerable<Type> roots)
    {
        var entities = new Dictionary<Type, EntityTypeConfiguration>();

        void EnsureEntityType(Type type)
        {
            if (!IsEntityCandidate(type)) return;

            for (var cur = type; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                if (entities.ContainsKey(cur)) continue;

                var etc = builder.AddEntityType(cur);
                if (cur.IsAbstract) etc.Abstract();

                var declaredId = cur.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                var inheritedId = cur.BaseType?.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);

                if (declaredId != null && inheritedId != null)
                {
                    etc.RemoveProperty(declaredId);
                }
                else if (declaredId != null && inheritedId == null)
                {
                    etc.HasKey(declaredId);
                }

                entities[cur] = etc;
            }
        }

        foreach (var t in roots) EnsureEntityType(t);

        var queue = new Queue<Type>(entities.Keys);
        var seen = new HashSet<Type>(entities.Keys);

        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            foreach (var navTarget in FindNavigationTargets(t))
            {
                if (!seen.Add(navTarget)) continue;
                EnsureEntityType(navTarget);
                queue.Enqueue(navTarget);
            }
        }

        return entities;
    }

    private static void WireInheritance(IDictionary<Type, EntityTypeConfiguration> entities)
    {
        foreach (var kv in entities)
        {
            var type = kv.Key;
            var etc = kv.Value;
            var baseType = type.BaseType;

            if (baseType == null || baseType == typeof(object)) continue;
            if (!entities.TryGetValue(baseType, out var baseEtc)) continue;

            etc.DerivesFrom(baseEtc);
        }
    }

    private static void EnsureEntitySetsForProvided(
        ODataConventionModelBuilder builder,
        IDictionary<Type, EntityTypeConfiguration> entities,
        IDictionary<Type, IEnumerable<ProvideAsEdmAttribute>> provided)
    {
        foreach (var (type, attrs) in provided)
        {
            var etc = entities[type];
            var setName = attrs.FirstOrDefault()?.Name ?? type.Name;
            var es = builder.AddEntitySet(setName, etc);
            _entitySetClrMap[es.Name] = etc.ClrType;
        }
    }

    private static void EnsureEntitySetsForNavTargets(
        ODataConventionModelBuilder builder,
        IDictionary<Type, EntityTypeConfiguration> entities)
    {
        bool HasSet(EntityTypeConfiguration tEtc) =>
            builder.NavigationSources.OfType<EntitySetConfiguration>().Any(es => es.EntityType == tEtc);

        foreach (var etc in entities.Values)
        {
            foreach (var nav in etc.NavigationProperties)
            {
                var targetClr = nav.RelatedClrType;
                if (targetClr == null) continue;
                if (!entities.TryGetValue(targetClr, out var targetEtc)) continue;
                if (HasSet(targetEtc)) continue;

                var es = builder.AddEntitySet(targetClr.Name, targetEtc);
                _entitySetClrMap[es.Name] = targetEtc.ClrType;
            }
        }
    }

    private static bool IsEntityCandidate(Type t)
    {
        if (!t.IsClass) return false;
        if (t.IsAbstract && t == typeof(object)) return false;
        if (t.IsPrimitive || t.IsEnum) return false;

        if (t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(Guid))
            return false;

        if (t.IsArray) return false;

        var hasId = t.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public) != null;
        var hasAttr = t.GetCustomAttributes<ProvideAsEdmAttribute>(inherit: false).Any();

        return hasId || hasAttr;
    }

    private static IEnumerable<Type> FindNavigationTargets(Type type)
    {
        foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var (_, elem) = UnwrapElementType(p.PropertyType);
            var candidate = elem ?? p.PropertyType;
            if (IsEntityCandidate(candidate)) yield return candidate;
        }
    }

    private static (bool isCollection, Type? elementType) UnwrapElementType(Type type)
    {
        if (type == typeof(string)) return (false, null);

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var ienumT =
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    ? type
                    : type.GetInterfaces().FirstOrDefault(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (ienumT != null) return (true, ienumT.GetGenericArguments()[0]);
        }

        return (false, null);
    }

    private static Type[] GetAllTypes()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .ToArray();
    }
}
