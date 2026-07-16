using System.ComponentModel;

namespace Nextended.UI.Input.Gamepad;

public enum GamepadButton
{
    [Description("A")]
    A,

    [Description("B")]
    B,

    [Description("X")]
    X,

    [Description("Y")]
    Y,

    [Description("LB")]
    LeftShoulder,

    [Description("RB")]
    RightShoulder,

    [Description("BACK")]
    Back,

    [Description("START")]
    Start,

    [Description("LS")]
    LeftThumb,

    [Description("RS")]
    RightThumb,

    [Description("UP")]
    Up,

    [Description("DOWN")]
    Down,

    [Description("LEFT")]
    Left,

    [Description("RIGHT")]
    Right,
}

public enum GamepadSlider
{
    [Description("LT")]
    LeftTrigger,

    [Description("RT")]
    RightTrigger,
}

public enum GamepadAxis
{
    [Description("LSX")]
    LeftThumbX,

    [Description("LSY")]
    LeftThumbY,

    [Description("RSX")]
    RightThumbX,

    [Description("RSY")]
    RightThumbY,
}
