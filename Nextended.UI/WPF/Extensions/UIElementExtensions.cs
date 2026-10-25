using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Nextended.UI.WPF.Extensions;

/// <summary>Fluent helpers for building WPF element trees in code.</summary>
public static class UIElementExtensions
{
    /// <summary>
    ///     Runs <paramref name="cfg"/> now when the element is visible, otherwise once it first
    ///     becomes visible (so configuration that needs a live layout works on hidden elements).
    /// </summary>
    public static T InitWith<T>(this T element, Action<T>? cfg) where T : UIElement
    {
        if (cfg == null)
            return element;

        if (element.IsVisible)
        {
            cfg(element);
            return element;
        }

        DependencyPropertyChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            element.IsVisibleChanged -= handler;
            element.Dispatcher.BeginInvoke(() => cfg(element));
        };
        element.IsVisibleChanged += handler;
        return element;
    }

    /// <summary>Forces a measure/arrange pass so DesiredSize/ActualSize are valid before the element is shown.</summary>
    public static void EnsureRenderedAndInitialized(this UIElement element)
    {
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(element.DesiredSize));
        element.UpdateLayout();
        element.InvalidateMeasure();
        element.InvalidateArrange();
    }

    public static T RemoveAll<T>(this T panel) where T : Panel
    {
        panel.Children.Clear();
        return panel;
    }

    public static ItemCollection AddRange(this ItemCollection collection, IEnumerable<UIElement> elements)
    {
        foreach (var element in elements)
            collection.Add(element);
        return collection;
    }

    /// <summary>Creates an element via <paramref name="ctor"/>, adds it and configures it (see <see cref="InitWith{T}"/>).</summary>
    public static T Add<T>(this IAddChild parent, Func<T> ctor, Action<T>? cfg = null) where T : UIElement
        => parent.Add(ctor(), cfg);

    /// <summary>Creates a new <typeparamref name="T"/>, adds it and configures it (see <see cref="InitWith{T}"/>).</summary>
    public static T Add<T>(this IAddChild parent, Action<T>? cfg = null) where T : UIElement, new()
        => parent.Add(new T(), cfg);

    /// <summary>Adds <paramref name="element"/> on the parent's UI thread and configures it (see <see cref="InitWith{T}"/>).</summary>
    public static T Add<T>(this IAddChild parent, T element, Action<T>? cfg = null) where T : UIElement
    {
        var dispatcher = (parent as DispatcherObject)?.Dispatcher ?? Application.Current.Dispatcher;
        dispatcher.Invoke(() => parent.AddChild(element));
        return element.InitWith(cfg);
    }
}
