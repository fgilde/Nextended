using System;
using System.Collections.Generic;
using System.Linq;
using Nextended.Core.Extensions;

namespace Nextended.Core.Types;

public abstract class Hierarchical<T>
    where T : Hierarchical<T>
{
    private HashSet<T> _children;

    public T Parent { get; set; }

    public IEnumerable<T> Path => GetPath().Reverse().Where(t => t != null);

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
    {
        return string.Join(separator, Path.Select(toStringFn));
    }

    private void UpdateParents(IEnumerable<T> items)
    {
        foreach (T hierarchical in items)
            hierarchical.Parent = (T)this;
    }

    public virtual bool HasChildren => Children?.Any() == true;

    public virtual bool ContainsChild(T entry)
    {
        return Children.EmptyIfNull().Recursive(n => n.Children.EmptyIfNull()).Contains(entry);
    }
    private IEnumerable<T> GetPath()
    {
        var node = this;
        while (node != null)
        {
            yield return (T)node;
            node = node.Parent;
        }
    }
}

public static class HierarchicalExtensions
{
    public static IEnumerable<T> Find<T>(this IEnumerable<T> entries, Func<T, bool> predicate)
        where T : Hierarchical<T>
    {
        return entries.Recursive(h => h.Children.EmptyIfNull()).Where(predicate);
    }
}