using System.Windows.Forms;
using Nextended.UI.Input;
using Nextended.UI.Input.Gamepad;

namespace Nextended.UI.Tests;

public class StoredInputBindingTests
{
    [Fact]
    public void Typed_accessors_respect_the_device()
    {
        StoredInputBinding arrowRight = Keys.Right;
        StoredInputBinding mouseRight = MouseButtons.Right;

        Assert.Equal(Keys.Right, arrowRight.KeyboardKey);
        Assert.Null(arrowRight.MouseButton);
        // Same Key text "Right", but a mouse binding must never read as the arrow key.
        Assert.Null(mouseRight.KeyboardKey);
        Assert.Equal(MouseButtons.Right, mouseRight.MouseButton);
        Assert.True(mouseRight.Is(MouseButtons.Right));
        Assert.False(mouseRight.Is(Keys.Right));
    }

    [Fact]
    public void Gamepad_binding_round_trips_to_an_event()
    {
        StoredInputBinding trigger = GamepadSlider.RightTrigger;

        Assert.True(trigger.Is(GamepadSlider.RightTrigger));
        Assert.False(trigger.Is(GamepadButton.A));
        Assert.Equal(GamepadSlider.RightTrigger, trigger.ToGamepadEventArgs()!.GamepadSlider);
        Assert.Null(((StoredInputBinding)Keys.A).ToGamepadEventArgs());
    }

    [Fact]
    public void Combo_has_no_single_device_value()
    {
        var combo = StoredInputBinding.Combo([Keys.ControlKey, Keys.X]);

        Assert.True(combo.IsCombo);
        Assert.Null(combo.KeyboardKey);
        Assert.Null(combo.ToGamepadEventArgs());
        Assert.Equal(StoredInputBinding.Combo([Keys.X, Keys.ControlKey]), combo);
    }
}
