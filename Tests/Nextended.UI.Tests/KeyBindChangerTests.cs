using System.Windows;
using System.Windows.Forms;
using Nextended.UI.Input;
using Nextended.UI.WPF.Controls;

namespace Nextended.UI.Tests;

public class KeyBindChangerTests
{
    private static (KeyBindChanger Changer, InputBindingManager Manager, FakeHook Hook) Create()
    {
        var hook = new FakeHook();
        var manager = new InputBindingManager(enableGamepad: false, () => hook);
        var changer = new KeyBindChanger();
        changer.BindingManager = manager;
        changer.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        return (changer, manager, hook);
    }

    private static void Record(FakeHook hook, KeyBindChanger changer, Keys key)
    {
        changer.RecordNewBinding();
        hook.Press(key);
        hook.Release(key);
    }

    [Fact]
    public void Recorded_binding_is_a_registered_user_change() => Sta.Run(() =>
    {
        var (changer, manager, hook) = Create();
        KeyBindChangedEventArgs? changed = null;
        changer.KeyBindChanged += (_, e) => changed = e;

        Record(hook, changer, Keys.F9);

        Assert.Equal(Keys.F9, changer.KeyBind!.KeyboardKey);
        Assert.True(changed!.IsUserChange);
        Assert.Same(changer.KeyBind, manager.Bindings[changer.BindingId]);
    });

    [Fact]
    public void Programmatic_set_is_not_a_user_change() => Sta.Run(() =>
    {
        var (changer, _, _) = Create();
        KeyBindChangedEventArgs? changed = null;
        changer.KeyBindChanged += (_, e) => changed = e;

        changer.KeyBind = Keys.F10;

        Assert.False(changed!.IsUserChange);
    });

    [Fact]
    public void Cancelled_recording_keeps_the_old_binding() => Sta.Run(() =>
    {
        var (changer, manager, hook) = Create();
        changer.KeyBind = Keys.F11;
        changer.KeyBindChanging += (_, e) => e.Cancel = e.NewValue.Is(Keys.F12);

        Record(hook, changer, Keys.F12);

        Assert.Equal(Keys.F11, changer.KeyBind!.KeyboardKey);
        Assert.Equal(Keys.F11, manager.Bindings[changer.BindingId].KeyboardKey);
    });

    [Fact]
    public void Delete_clears_and_unregisters() => Sta.Run(() =>
    {
        var (changer, manager, _) = Create();
        changer.KeyBind = Keys.F6;
        var deleted = false;
        changer.KeyDeleted += (_, _) => deleted = true;

        changer.DeleteBinding();

        Assert.True(deleted);
        Assert.False(changer.HasKeySet);
        Assert.False(manager.Bindings.ContainsKey(changer.BindingId));
    });

    [Fact]
    public void Global_press_raises_binding_pressed_until_unloaded() => Sta.Run(() =>
    {
        var (changer, _, hook) = Create();
        changer.KeyBind = Keys.F5;
        var pressed = 0;
        changer.BindingPressed += (_, _) => pressed++;

        hook.Press(Keys.F5);
        hook.Release(Keys.F5);
        changer.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        hook.Press(Keys.F5);
        hook.Release(Keys.F5);

        Assert.Equal(1, pressed);
    });

    [Fact]
    public void Tooltip_is_localizable() => Sta.Run(() =>
    {
        var changer = new KeyBindChanger { DeviceNameProvider = b => b.DeviceName == "Keyboard" ? "Tastatur" : b.DeviceName };
        changer.KeyBind = Keys.F2;
        Assert.Equal("Tastatur: F2", changer.ToolTip);

        changer.BoundToolTipFormat = "Derzeit {1} ({0})";
        Assert.Equal("Derzeit F2 (Tastatur)", changer.ToolTip);
    });

    [Fact]
    public void Renaming_moves_the_registration() => Sta.Run(() =>
    {
        var (changer, manager, _) = Create();
        changer.KeyConfigName = "first";
        changer.KeyBind = Keys.F3;

        changer.KeyConfigName = "second";

        Assert.False(manager.Bindings.ContainsKey("first"));
        Assert.True(manager.Bindings.ContainsKey("second"));
    });

    [Fact]
    public void Title_collapses_while_empty() => Sta.Run(() =>
    {
        var changer = new KeyBindChanger();
        Assert.Equal(Visibility.Collapsed, changer.TitleLabel.Visibility);

        changer.Text = "Zoom";
        Assert.Equal(Visibility.Visible, changer.TitleLabel.Visibility);
        Assert.Equal("Zoom", changer.TitleLabel.Content);
    });
}
