using System;
using System.Drawing;
using System.Windows;
using Nextended.UI.Native;
using Screen = System.Windows.Forms.Screen;

namespace Nextended.UI.WPF.Extensions;

/// <summary>Window conveniences on top of <see cref="NativeMethods"/>: capture exclusion, click-through, z-order and placement.</summary>
public static class WindowInteropExtensions
{
    /// <inheritdoc cref="NativeMethods.HideForCapture"/>
    public static T HideForCapture<T>(this T window) where T : Window
    {
        NativeMethods.HideForCapture(window.GetHandle());
        return window;
    }

    /// <inheritdoc cref="NativeMethods.SetCaptureExclusion"/>
    public static T SetCaptureExclusion<T>(this T window, bool exclude) where T : Window
    {
        NativeMethods.SetCaptureExclusion(window.GetHandle(), exclude);
        return window;
    }

    /// <inheritdoc cref="NativeMethods.MakeClickThrough"/>
    public static T MakeClickThrough<T>(this T window) where T : Window
    {
        NativeMethods.MakeClickThrough(window.GetHandle());
        return window;
    }

    /// <inheritdoc cref="NativeMethods.SetTopMost"/>
    public static T SetTopMost<T>(this T window, bool isTopMost = true) where T : Window
    {
        NativeMethods.SetTopMost(window.GetHandle(), isTopMost);
        return window;
    }

    /// <summary>Centers the window on the given screen (primary screen when null). Thread-safe.</summary>
    public static void Center(this Window window, Screen? screen = null)
        => window.Center((screen ?? Screen.PrimaryScreen!).Bounds);

    /// <summary>Centers the window, keeping its size, inside <paramref name="bounds"/>. Thread-safe.</summary>
    public static void Center(this Window window, Rectangle bounds)
    {
        if (!window.CheckAccess())
        {
            window.Dispatcher.Invoke(() => window.Center(bounds));
            return;
        }
        var centerX = bounds.Left + bounds.Width / 2.0;
        var centerY = bounds.Top + bounds.Height / 2.0;
        window.MoveTo(new RectangleF((float)(centerX - window.Width / 2), (float)(centerY - window.Height / 2), (float)window.Width, (float)window.Height));
    }

    /// <summary>
    ///     Restores the window and sets its bounds to <paramref name="bounds"/>, shrunk by
    ///     <paramref name="padding"/> (never below 10x10). Thread-safe.
    /// </summary>
    public static void MoveTo(this Window window, RectangleF bounds, Thickness? padding = null)
    {
        if (!window.CheckAccess())
        {
            window.Dispatcher.Invoke(() => window.MoveTo(bounds, padding));
            return;
        }

        var p = padding ?? new Thickness(0);
        window.WindowState = WindowState.Normal;
        window.Left = bounds.Left + p.Left;
        window.Top = bounds.Top + p.Top;
        window.Width = Math.Max(bounds.Width - (p.Left + p.Right), 10);
        window.Height = Math.Max(bounds.Height - (p.Top + p.Bottom), 10);
    }

    /// <summary>Fills the given screen with the window. Thread-safe.</summary>
    public static void MoveToScreen(this Window window, Screen screen) => window.MoveTo(screen.Bounds);
}
