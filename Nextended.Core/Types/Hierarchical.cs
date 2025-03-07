﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Nextended.Core.Extensions;

namespace Nextended.Core.Types;

public abstract class Hierarchical<T> : IAsyncHierarchical<T>, IChildInfo
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
                LoadChildren();
                return GetLoadingIndicatorItems();
            }
            return _children;
        }
        set
        {
            _children = value;
            UpdateParents(Children);
        }
    }

    public virtual HashSet<T> GetLoadingIndicatorItems() => [new T { IsLoading = true }];

    public Task LoadChildren()
    {
        if (LoadChildrenFunc != null && _loadCancel == null)
        {
            _loadCancel = new CancellationTokenSource();
            //IsLoading = true;
            return LoadChildrenFunc(this as T, _loadCancel.Token).ContinueWith(t =>
            {
                LoadChildrenFunc = null;
                Children = t.Result;
                //IsLoading = false;
                OnChildrenLoaded?.Invoke(this, t.Result);
            });
        }
        return Task.CompletedTask;
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
    public HashSet<T> Children { get; }
    public T Parent { get; }
}

public interface IAsyncHierarchical<T>: IHierarchical<T> where T : IHierarchical<T>
{
    public bool IsLoading { get; set; }
    Task LoadChildren();
    HashSet<T> GetLoadingIndicatorItems();
    Func<T, CancellationToken, Task<HashSet<T>>> LoadChildrenFunc { get; }
    public Action<IHierarchical<T>, HashSet<T>> OnChildrenLoaded { get; set; }
}

public interface IChildInfo
{
    public bool HasChildren { get; }
}

public static class HierarchicalExtensions
{
    public static IEnumerable<T> GetLoadedChildren<T>(this T node) where T : IHierarchical<T>
    {
        return node != null && node.HasChildren() && !node.NeedsLoadChildren() ? node.Children.EmptyIfNull() : [];
    }

    public static bool NeedsLoadChildren<T>(this T node) where T : IHierarchical<T>
    {
        if (node is IAsyncHierarchical<T> asyncNode)
        {
            return node.HasChildren() && asyncNode?.LoadChildrenFunc != null;
        }
        return false;
    }

    private static bool ValidForRecursion<T>(this T node) where T : IHierarchical<T>
    {
        return true;
        if (node is IAsyncHierarchical<T> asyncNode)
        {
            return !asyncNode.IsLoading && asyncNode.LoadChildrenFunc == null;
        }
        return true;
    }

    public static IEnumerable<T> Find<T>(this IEnumerable<T> entries, Func<T, bool> predicate)
        where T : IHierarchical<T>
    {
        return entries.Recursive(h => h.GetLoadedChildren(), a => a.ValidForRecursion()).Where(predicate);
    }

    public static IEnumerable<T> Find<T>(this T entry, Func<T, bool> predicate)
        where T : IHierarchical<T>
    {
        return entry.GetLoadedChildren().Recursive(h => h.GetLoadedChildren(), a => a.ValidForRecursion()).Where(predicate);
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
        return node?.Parent?.GetLoadedChildren().EmptyIfNull().Where(n => !ReferenceEquals(n, node));
    }

    public static string GetPathString<T>(this T node, Func<T, string> toStringFn, string separator = "/")
        where T : IHierarchical<T> =>
        string.Join(separator, node.Path().Select(toStringFn));

    public static IEnumerable<T> Path<T>(this T item) where T : IHierarchical<T>
        => GetPath(item).Reverse().Where(t => t != null);

    public static bool Contains<T>(this T node, T entry)
        where T : IHierarchical<T> =>
        node?.GetLoadedChildren()?.EmptyIfNull().Recursive(n => n.GetLoadedChildren(), a => a.ValidForRecursion()).Contains(entry) == true;

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