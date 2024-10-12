using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Nextended.Core.Extensions;

namespace Nextended.Core.Types;

public abstract class Hierarchical<T> : IHierarchical<T>, IChildInfo
    where T : Hierarchical<T>, new()
{
    private HashSet<T> _children;

    public Func<T, CancellationToken, Task<HashSet<T>>> LoadChildrenFunc { get; set; } = null;

    private CancellationTokenSource? _loadCancel = null;

    public Action<IHierarchical<T>, HashSet<T>> OnChildrenLoaded { get; set; }

    public T Parent { get; set; }
    public bool IsLoading { get; set; }
    public IEnumerable<T> Path => (this as T).Path();

    public bool IsExpanded { get; set; }

    public virtual HashSet<T> Children
    {
        get
        {
            if (LoadChildrenFunc != null)
            {
                if (_loadCancel == null)
                {
                    _loadCancel = new CancellationTokenSource();
                    //IsLoading = true;
                    LoadChildrenFunc(this as T, _loadCancel.Token).ContinueWith(t =>
                    {
                        LoadChildrenFunc = null;
                        Children = t.Result;
                        //IsLoading = false;
                        OnChildrenLoaded?.Invoke(this, t.Result);
                    });
                }

                return [new T {IsLoading = true}];
            }
            return _children;
        }
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

    public virtual bool HasChildren => LoadChildrenFunc != null || Children?.Any() == true;

    public virtual bool ContainsChild(T entry)
    {
        return ((T)this).Contains<T>(entry);
    }
}


public interface IHierarchical<T> where T : IHierarchical<T>
{
    Func<T, CancellationToken, Task<HashSet<T>>> LoadChildrenFunc { get; }
    public Action<IHierarchical<T>, HashSet<T>> OnChildrenLoaded { get; set; }
    public HashSet<T> Children { get; }
    public bool IsLoading { get; }
    public T Parent { get; }
}

public interface IChildInfo
{
    public bool HasChildren { get; }
}

public static class HierarchicalExtensions
{
    public static bool ValidForRecursion<T>(this T node) where T : IHierarchical<T>
    {
        return !node.IsLoading && node.LoadChildrenFunc == null && node.HasChildren();
    }

    public static IEnumerable<T> Find<T>(this IEnumerable<T> entries, Func<T, bool> predicate)
        where T : IHierarchical<T>
    {
        return entries.Recursive(h => h.Children.EmptyIfNull(), a => a.ValidForRecursion()).Where(predicate);
    }

    public static IEnumerable<T> Find<T>(this T entry, Func<T, bool> predicate)
        where T : IHierarchical<T>
    {
        return entry.Children.EmptyIfNull().Recursive(h => h.Children.EmptyIfNull(), a => a.ValidForRecursion()).Where(predicate);
    }

    public static bool IsInPathOf<T>(this T node, T entry)
        where T : IHierarchical<T>
    {
        return entry.Path().Contains(node);
    }

    public static bool HasChildren<T>(this T node)
            where T : IHierarchical<T> =>
        node is IChildInfo info ? info.HasChildren : node?.Children?.Any() == true;

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
        node?.Children?.EmptyIfNull().Recursive(n => n?.Children?.EmptyIfNull(), a => a.ValidForRecursion()).Contains(entry) == true;

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