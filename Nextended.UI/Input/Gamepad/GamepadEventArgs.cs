using System;
using System.Linq;
using Nextended.Core.Helper;

namespace Nextended.UI.Input.Gamepad;

/// <summary>
///     One normalized gamepad input event. <see cref="Button"/> is the transport-neutral string
///     vocabulary ("A", "LT", "RSX", …) shared by all readers.
/// </summary>
public class GamepadEventArgs : EventArgs
{
    public GamepadEventArgs()
    { }

    public GamepadEventArgs(GamepadAxis axis, float? value = null)
    {
        Button = axis.ToDescriptionString();
        IsStickEvent = true;
        Value = value;
    }

    public GamepadEventArgs(GamepadSlider slider, float? value = null)
    {
        Button = slider.ToDescriptionString();
        Value = value;
        if (value != null)
            IsPressed = value > 0;
    }

    public GamepadEventArgs(GamepadButton button, bool? pressed = null)
    {
        Button = button.ToDescriptionString();
        IsPressed = pressed;
        if (pressed == true)
            Value = 1f;
        else if (pressed == false)
            Value = 0f;
    }

    public bool IsStickEvent { get; set; }
    public string Button { get; set; } = string.Empty;
    public bool? IsPressed { get; set; }
    public float? Value { get; set; }
    public string Code => Button;

    public GamepadButton? GamepadButton => MatchEnum<GamepadButton>();
    public GamepadSlider? GamepadSlider => MatchEnum<GamepadSlider>();
    public GamepadAxis? GamepadAxis => MatchEnum<GamepadAxis>();

    // Resolve the enum value whose [Description] equals Button. FirstOrDefault yields default(T)
    // (the zero-valued member) when nothing matches, so re-check the description to distinguish a
    // genuine match at the zero index from "no match".
    private T? MatchEnum<T>() where T : struct, Enum =>
        Enum.GetValues<T>().FirstOrDefault(b => b.ToDescriptionString() == Button) is var match && match.ToDescriptionString() == Button
            ? match
            : null;

    public override string ToString() => Button;
}
