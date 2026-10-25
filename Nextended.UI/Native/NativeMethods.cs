using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nextended.UI.Native;

/// <summary>
///     Win32 P/Invoke signatures (user32, kernel32, dwmapi, Magnification) plus small safe helpers
///     on window handles. Window-object conveniences live in
///     <see cref="Nextended.UI.WPF.Extensions.WindowInteropExtensions"/>.
/// </summary>
public static class NativeMethods
{
    #region P/Invoke signatures

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] MINPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", EntryPoint = "mouse_event")]
    public static extern void MouseEvent(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern IntPtr SetTimer(IntPtr hWnd, int nIDEvent, int uElapse, IntPtr lpTimerFunc);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool KillTimer(IntPtr hwnd, int idEvent);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, [In, Out] ref RECT rect);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr CreateWindow(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, LayeredWindowAttributeFlags dwFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string modName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagInitialize();

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagUninitialize();

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowSource(IntPtr hwnd, RECT rect);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagGetWindowSource(IntPtr hwnd, ref RECT pRect);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowTransform(IntPtr hwnd, ref Transformation pTransform);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagGetWindowTransform(IntPtr hwnd, ref Transformation pTransform);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowFilterList(IntPtr hwnd, int dwFilterMode, int count, IntPtr pHWND);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern int MagGetWindowFilterList(IntPtr hwnd, IntPtr pdwFilterMode, int count, IntPtr pHWND);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetColorEffect(IntPtr hwnd, ref ColorEffect pEffect);

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagGetColorEffect(IntPtr hwnd, ref ColorEffect pEffect);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

    [DllImport("user32.dll")]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, NativeMonitorInfo lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

    #endregion P/Invoke signatures

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);

    /// <summary>Excludes the window from screen capture (screenshots, recordings, desktop duplication).</summary>
    public static void HideForCapture(IntPtr hwnd) => SetCaptureExclusion(hwnd, true);

    /// <summary>Excludes (or re-includes) the window from screen capture.</summary>
    public static void SetCaptureExclusion(IntPtr hwnd, bool exclude)
        => SetWindowDisplayAffinity(hwnd, (uint)(exclude ? WindowAffinity.WDA_EXCLUDEFROMCAPTURE : WindowAffinity.WDA_NONE));

    /// <summary>Lets mouse input pass through the window (WS_EX_TRANSPARENT).</summary>
    public static void MakeClickThrough(IntPtr hwnd)
        => SetWindowLong(hwnd, NativeConstants.GWL_EXSTYLE, GetWindowLong(hwnd, NativeConstants.GWL_EXSTYLE) | (int)ExtendedWindowStyles.WS_EX_TRANSPARENT);

    /// <summary>Current cursor position in screen coordinates, or (0,0) when it cannot be read.</summary>
    public static System.Drawing.Point GetCursorPosition()
        => GetCursorPos(out var p) ? new System.Drawing.Point(p.X, p.Y) : System.Drawing.Point.Empty;

    /// <summary>The window's bounds in screen coordinates.</summary>
    /// <exception cref="Win32Exception">The handle is not a valid window.</exception>
    public static RECT GetWindowRectangle(IntPtr hWnd)
    {
        RECT rect = new();
        if (!GetWindowRect(hWnd, ref rect))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return rect;
    }

    public static bool IsWindowTopMost(IntPtr hWnd)
        => (GetWindowLong(hWnd, NativeConstants.GWL_EXSTYLE) & NativeConstants.WS_EX_TOPMOST) == NativeConstants.WS_EX_TOPMOST;

    /// <summary>Places the window in (or removes it from) the topmost z-order band without moving, sizing or activating it.</summary>
    public static void SetTopMost(IntPtr handle, bool isTopMost = true)
        => SetWindowPos(handle, isTopMost ? NativeConstants.HWND_TOPMOST : NativeConstants.HWND_NOTOPMOST, 0, 0, 0, 0,
            (int)(SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_SHOWWINDOW));

    public static void RemoveTopMost(IntPtr handle) => SetTopMost(handle, false);

    /// <summary>The window directly above <paramref name="hWnd"/> in z-order (use as hWndInsertAfter to keep a position).</summary>
    public static IntPtr GetWindowInsertAfter(IntPtr hWnd) => GetWindow(hWnd, NativeConstants.GW_HWNDPREV);

    /// <summary>Processes with a titled main window that <see cref="IsWindowValidForCapture"/> accepts.</summary>
    public static IEnumerable<Process> RecordableProcesses()
        => Process.GetProcesses().Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle) && IsWindowValidForCapture(p.MainWindowHandle));

    /// <summary>
    ///     True for a visible, enabled, non-cloaked top-level window other than the shell — the same
    ///     filter the Windows capture picker applies.
    /// </summary>
    public static bool IsWindowValidForCapture(IntPtr hwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero || hwnd == GetShellWindow() || !IsWindowVisible(hwnd))
                return false;

            if (GetAncestor(hwnd, GetAncestorFlags.GetRoot) != hwnd)
                return false;

            // Styles are 32 bit; go through Int64 so WS_POPUP (high bit) can't overflow on 64-bit handles.
            var style = (WindowStyles)(uint)GetWindowLongPtr(hwnd, (int)GWL.STYLE).ToInt64();
            if (style.HasFlag(WindowStyles.WS_DISABLED))
                return false;

            var hr = DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.Cloaked, out var cloaked, Marshal.SizeOf<bool>());
            return !(hr == 0 && cloaked);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
