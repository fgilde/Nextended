using System.ComponentModel;
using System.Runtime.InteropServices;
using Nextended.UI.Native;

namespace Nextended.UI.Tests;

public class NativeMethodsTests
{
    [Fact]
    public void Input_structs_match_the_win32_layout()
    {
        // SendInput rejects calls whose cbSize differs from sizeof(INPUT).
        Assert.Equal(IntPtr.Size == 8 ? 40 : 28, Marshal.SizeOf<INPUT>());
        Assert.Equal(16, Marshal.SizeOf<RECT>());
    }

    [Fact]
    public void Null_handle_is_never_capturable() => Assert.False(NativeMethods.IsWindowValidForCapture(IntPtr.Zero));

    [Fact]
    public void Invalid_handle_has_no_rectangle() => Assert.Throws<Win32Exception>(() => NativeMethods.GetWindowRectangle(IntPtr.Zero));

    [Fact]
    public void Rect_converts_edges_to_size() => Assert.Equal(new System.Drawing.Rectangle(10, 20, 30, 40), new RECT(10, 20, 40, 60).ToRectangle());
}
