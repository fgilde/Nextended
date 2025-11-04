using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Nextended.Web.OData;

public static class ODataExtensions
{
    public static ODataQueryOptions<T> ODataQueryOptions<T>(this HttpRequest request, IEdmModel edmModel, IEdmEntitySet? set = null) where T : class => ODataQueryOptions<T>(request, edmModel, set != null ? new ODataPath(new EntitySetSegment(set)) : new ODataPath());

    public static ODataQueryOptions<T> ODataQueryOptions<T>(this HttpRequest request, IEdmModel edmModel, ODataPath path) where T : class => ODataQueryOptions<T>(request, new ODataQueryContext(edmModel, typeof(T), path));

    public static ODataQueryOptions<T> ODataQueryOptions<T>(this HttpRequest request, ODataQueryContext context) where T : class => new(context, request);

    public static IQueryable<T> ApplyOData<T>(this IQueryable<T> query, ODataQueryOptions options, ODataQuerySettings? settings = null) where T : class => query.ApplyODataExpandIncludes(options).ApplySafeODataOptions(options, settings);

    private static IQueryable<T> ApplySafeODataOptions<T>(this IQueryable<T>? query, ODataQueryOptions options, ODataQuerySettings? settings = null) where T : class
    {
        settings ??= DefaultODataQuerySettings;

        return query
              .ApplyODataFilter(options.Filter, settings)
              .ApplyODataOrderBy(options.OrderBy, settings)
              .ApplyODataSkip(options.Skip, settings)
              .ApplyODataTop(options.Top, settings);
    }

    private static ODataQuerySettings DefaultODataQuerySettings => new() { HandleNullPropagation = HandleNullPropagationOption.False };

    public static IQueryable<T>? ApplyODataFilter<T>(this IQueryable<T>? query, FilterQueryOption? option, ODataQuerySettings? settings = null) where T : class
        => option != null ? (IQueryable<T>)option.ApplyTo(query, settings ?? DefaultODataQuerySettings) : query;

    public static IQueryable<T>? ApplyODataOrderBy<T>(this IQueryable<T>? query, OrderByQueryOption? option, ODataQuerySettings? settings = null) where T : class
        => option != null ? (IQueryable<T>)option.ApplyTo(query, settings ?? DefaultODataQuerySettings) : query;

    public static IQueryable<T>? ApplyODataSkip<T>(this IQueryable<T>? query, SkipQueryOption? option, ODataQuerySettings? settings = null) where T : class
        => option != null ? (IQueryable<T>)option.ApplyTo(query, settings ?? DefaultODataQuerySettings) : query;

    public static IQueryable<T>? ApplyODataTop<T>(this IQueryable<T>? query, TopQueryOption? option, ODataQuerySettings? settings = null) where T : class
        => option != null ? (IQueryable<T>)option.ApplyTo(query, settings ?? DefaultODataQuerySettings) : query;

    public static IQueryable<T> ApplyODataTop<T>(this IQueryable<T> query, ODataQueryOptions options, ODataQuerySettings? settings = null) where T : class => query.ApplyODataTop(options.Top, settings);
    public static IQueryable<T> ApplyODataSkip<T>(this IQueryable<T> query, ODataQueryOptions options, ODataQuerySettings? settings = null) where T : class => query.ApplyODataSkip(options.Skip, settings);
    public static IQueryable<T> ApplyODataOrderBy<T>(this IQueryable<T> query, ODataQueryOptions options, ODataQuerySettings? settings = null) where T : class => query.ApplyODataOrderBy(options.OrderBy, settings);
    public static IQueryable<T> ApplyODataFilter<T>(this IQueryable<T> query, ODataQueryOptions options, ODataQuerySettings? settings = null) where T : class => query.ApplyODataFilter(options.Filter, settings);
    public static IQueryable<T> ApplyODataExpandIncludes<T>(this IQueryable<T> query, ODataQueryOptions options) where T : class => query.ApplyODataExpandIncludes(options.SelectExpand);


    public static IQueryable<T> ApplyODataExpandIncludes<T>(this IQueryable<T> query, SelectExpandQueryOption? option) where T : class
    {
        var clause = option?.SelectExpandClause;
        if (clause == null) return query;

        var includePaths = new List<string>();
        CollectIncludes(clause, typeof(T), "", includePaths);

        var withPrefixes = includePaths
            .SelectMany(p =>
            {
                var segs = p.Split('.');
                return Enumerable.Range(1, segs.Length).Select(i => string.Join('.', segs.Take(i)));
            })
            .Distinct();

        foreach (var p in withPrefixes)
            query = query.Include(p);

        return query;

        static void CollectIncludes(SelectExpandClause clause, Type currentClr, string prefix, List<string> paths)
        {
            foreach (var item in clause.SelectedItems.OfType<ExpandedNavigationSelectItem>())
            {
                var (localPath, endClr) = BuildClrPathFromExpand(item.PathToNavigationProperty, currentClr);
                if (string.IsNullOrEmpty(localPath)) continue;

                var full = string.IsNullOrEmpty(prefix) ? localPath : $"{prefix}.{localPath}";
                paths.Add(full);

                // nested $expand
                if (item.SelectAndExpand != null)
                    CollectIncludes(item.SelectAndExpand, endClr, full, paths);
            }
        }

        static (string path, Type endClr) BuildClrPathFromExpand(ODataExpandPath expandPath, Type startClr)
        {
            var parts = new List<string>();
            var clr = startClr;

            foreach (var seg in expandPath)
            {
                if (seg is NavigationPropertySegment navSeg)
                {
                    var edmName = navSeg.NavigationProperty.Name;
                    var prop = FindClrProperty(clr, edmName);
                    if (prop == null) return ("", clr);

                    parts.Add(prop.Name);
                    clr = UnwrapElementType(prop.PropertyType);
                }
            }

            return (string.Join(".", parts), clr);
        }

        static PropertyInfo? FindClrProperty(Type clrType, string edmName)
        {
            var pi = clrType.GetProperty(edmName, BindingFlags.Instance | BindingFlags.Public);
            if (pi != null) return pi;
            return clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                          .FirstOrDefault(p => string.Equals(p.Name, edmName, StringComparison.OrdinalIgnoreCase));
        }

        static Type UnwrapElementType(Type type)
        {
            if (type == typeof(string)) return type;
            var nt = Nullable.GetUnderlyingType(type) ?? type;

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(nt) && nt != typeof(string))
            {
                var enumerationType = nt.IsGenericType && nt.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    ? nt
                    : nt.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumerationType != null) return enumerationType.GetGenericArguments()[0];
            }
            return nt;
        }
    }
}