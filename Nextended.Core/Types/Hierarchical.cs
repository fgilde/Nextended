using System;
using System.Collections.Generic;
using System.Linq;
using Nextended.Core.Extensions;

namespace Nextended.Core.Types;

public abstract class Hierarchical<T> : IHierarchical<T>
    where T : Hierarchical<T>
{
    private HashSet<T> _children;

    public T Parent { get; set; }

    public IEnumerable<T> Path => (this as T).Path();

    public bool IsExpanded { get; set; }

    public virtual HashSet<T> Children
    {
        get => _children;
        set
        {
            _children = value;
            UpdateParents(Children);
        }
    }

    public string GetPathString(Func<T, string> toStringFn, string separator = "/") 
        => HierarchicalExtensions.GetPathString(this as T, toStringFn, separator);

    private void UpdateParents(IEnumerable<T> items)
    {
        foreach (T hierarchical in items)
            hierarchical.Parent = (T)this;
    }

    public virtual bool HasChildren => Children?.Any() == true;

    public virtual bool ContainsChild(T entry)
    {
        return ((T) this).Contains<T>(entry);
    }
}

public interface IHierarchical<T> where T : IHierarchical<T>
{
    public HashSet<T> Children { get; }
    public T Parent { get; }
}

public static class HierarchicalExtensions
{
    public static IEnumerable<T> Find<T>(this IEnumerable<T> entries, Func<T, bool> predicate)
        where T : IHierarchical<T>
    {
        return entries.Recursive(h => h.Children.EmptyIfNull()).Where(predicate);
    }

    public static IEnumerable<T> Siblings<T>(this T node)
        where T : IHierarchical<T>
    {
        return node?.Parent?.Children.EmptyIfNull().Where(n => !ReferenceEquals(n, node));
    }

    public static string GetPathString<T>(this T node, Func<T, string> toStringFn, string separator = "/")
        where T : IHierarchical<T> =>
        string.Join(separator, node.Path().Select(toStringFn));

    public static IEnumerable<T> Path<T>(this T item) where T : IHierarchical<T>
        => GetPath(item).Reverse().Where(t => t != null);

    public static bool Contains<T>(this T node, T entry)
        where T : IHierarchical<T> =>
        node?.Children?.EmptyIfNull().Recursive(n => n?.Children?.EmptyIfNull()).Contains(entry) == true;

    private static IEnumerable<T> GetPath<T>(T item)
        where T : IHierarchical<T>
    {
        var node = item;
        while (node != null)
        {
            yield return node;
            node = node.Parent;
        }
    }
}