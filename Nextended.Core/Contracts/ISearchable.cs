#if !NETSTANDARD
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nextended.Core.Contracts;

public interface ISearchable<TSelf> where TSelf : class
{
    private static string[] _ignored = ["ConcurrencyStamp"];

    private static PropertyInfo _prop = typeof(TSelf).GetProperty(nameof(ISearchable<>.SearchProperties), BindingFlags.Public | BindingFlags.Static)!;
    static abstract Expression<Func<TSelf, string?>>[] SearchProperties { get; }

    /// <summary>
    /// Returns member access expressions for all string properties.
    /// </summary>
    static Expression<Func<TSelf, string?>>[] AllOfString() => BuildExpressions(p => !_ignored.Contains(p.Name) && p.PropertyType == typeof(string));

    /// <summary>
    /// Returns member access expressions for all string? properties
    /// decorated with <typeparamref name="TAttribute"/>.
    /// </summary>
    static Expression<Func<TSelf, string?>>[] AllWithAttribute<TAttribute>() where TAttribute : Attribute => BuildExpressions(p => p.GetCustomAttribute<TAttribute>() is not null);

    /// <summary>
    /// Combines multiple expression arrays into one (useful for composing SearchProperties).
    /// </summary>
    static Expression<Func<TSelf, string?>>[] Combine(
        params Expression<Func<TSelf, string?>>[][] arrays) =>
        arrays.SelectMany(a => a).ToArray();

    private static Expression<Func<TSelf, string?>>[] BuildExpressions(
        Func<PropertyInfo, bool> predicate,
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        var param = Expression.Parameter(typeof(TSelf), "x");

        return typeof(TSelf)
              .GetProperties(flags)
              .Where(predicate)
              .Where(p => p.PropertyType == typeof(string)) // ensure it's string
              .Select(p =>
                          Expression.Lambda<Func<TSelf, string?>>(
                              Expression.Property(param, p),
                              param))
              .ToArray();
    }

    public static Expression<Func<TSelf, string?>>[] GetSearchProperties()
    {
        return typeof(ISearchable<TSelf>).IsAssignableFrom(typeof(TSelf))
                   ? _prop.GetValue(null) as Expression<Func<TSelf, string?>>[] ?? []
                   : AllOfString();
    }
}
#endif