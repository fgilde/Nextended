using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DirectInput;
using SharpDX.XInput;
// The enclosing namespace segment "Gamepad" shadows the SharpDX.XInput.Gamepad type, so alias it.
using XGamepad = SharpDX.XInput.Gamepad;

namespace Nextended.UI.Input.Gamepad;

/// <summary>
///     Reads a NON-XInput pad — e.g. a PS5 DualSense connected raw over USB/Bluetooth, which XInput
///     never sees — through SharpDX.DirectInput, and exposes it through the same contract as the
///     XInput <see cref="XInputGamepadReader"/>: normalized string vocabulary ("A"/"RSX"/"LT"…) via
///     <see cref="ButtonEvent"/>. The axis/button/POV mapping follows the common DualShock 4 /
///     DualSense DirectInput layout.
/// </summary>
public class DirectInputGamepadReader : IGamepadReader
{
    // DualSense / DS4 DirectInput button indices (Sony order).
    private const int BtnSquare = 0, BtnCross = 1, BtnCircle = 2, BtnTriangle = 3,
        BtnL1 = 4, BtnR1 = 5, BtnShare = 8, BtnOptions = 9, BtnL3 = 10, BtnR3 = 11;

    private readonly DirectInput _directInput = new();
    private readonly Guid _instanceGuid;
    private Joystick? _joystick;
    private volatile bool _acquired;

    private readonly CancellationTokenSource _cts = new();
    private Task? _pollingTask;
    private readonly TaskScheduler _scheduler;

    private readonly object _stateLock = new();
    private XGamepad _mapped;   // latest mapped snapshot (for IsPressed)
    private XGamepad _prev;     // previous mapped gamepad (for edge-detected ButtonEvents)
    private bool _havePrev;

    public event EventHandler<GamepadEventArgs>? ButtonEvent;

    /// <summary>Trigger value (0..1) above which LT counts as pressed.</summary>
    public float LeftTriggerThreshold { get; set; } = 0.1f;

    /// <summary>Trigger value (0..1) above which RT counts as pressed.</summary>
    public float RightTriggerThreshold { get; set; } = 0.1f;

    /// <summary>The DirectInput device this reader is bound to.</summary>
    public Guid InstanceGuid => _instanceGuid;

    public DirectInputGamepadReader(Guid instanceGuid)
    {
        _instanceGuid = instanceGuid;
        try { _scheduler = TaskScheduler.FromCurrentSynchronizationContext(); }
        catch { _scheduler = TaskScheduler.Default; }
        TryOpen();
        StartPolling();
    }

    /// <summary>
    ///     Enumerate attached DirectInput game controllers as (InstanceGuid, name) pairs; empty on
    ///     failure. Use to pick a non-XInput pad (e.g. a raw DualSense).
    /// </summary>
    public static List<(Guid Guid, string Name)> EnumerateDevices()
    {
        var result = new List<(Guid, string)>();
        try
        {
            using var di = new DirectInput();
            foreach (var dev in di.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
                result.Add((dev.InstanceGuid, string.IsNullOrWhiteSpace(dev.InstanceName) ? "Controller" : dev.InstanceName));
        }
        catch { /* DirectInput unavailable — return what we have */ }
        return result;
    }

    /// <summary>First attached DirectInput controller, or null.</summary>
    public static Guid? FindFirstDevice()
    {
        var devices = EnumerateDevices();
        return devices.Count > 0 ? devices[0].Guid : null;
    }

    public bool IsConnected => _acquired && _joystick != null;

    private bool TryOpen()
    {
        try
        {
            _joystick = new Joystick(_directInput, _instanceGuid);
            // Background + non-exclusive so GetCurrentState works from the poll thread while another
            // app also reads the pad. Needs a window handle; use the process main window if it
            // exists yet (early in startup it may be zero — the re-acquire loop retries once it does).
            var hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (hwnd != IntPtr.Zero)
                _joystick.SetCooperativeLevel(hwnd, CooperativeLevel.NonExclusive | CooperativeLevel.Background);
            _joystick.Acquire();
            _acquired = true;
            return true;
        }
        catch
        {
            _acquired = false;
            return false;
        }
    }

    private void StartPolling()
    {
        var token = _cts.Token;
        _pollingTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (_joystick == null || !_acquired)
                {
                    await Task.Delay(1000, token);
                    TryOpen(); // device may have (re)appeared
                }
                else
                {
                    try { Poll(); }
                    catch { _acquired = false; /* device lost — re-acquire on the next tick */ }
                }
                await Task.Delay(10, token);
            }
        }, token);
    }

    private void Poll()
    {
        _joystick!.Poll();
        var gp = MapToGamepad(_joystick.GetCurrentState());

        if (_havePrev)
        {
            EmitButton(gp, _prev, GamepadButtonFlags.A, "A");
            EmitButton(gp, _prev, GamepadButtonFlags.B, "B");
            EmitButton(gp, _prev, GamepadButtonFlags.X, "X");
            EmitButton(gp, _prev, GamepadButtonFlags.Y, "Y");
            EmitButton(gp, _prev, GamepadButtonFlags.LeftShoulder, "LB");
            EmitButton(gp, _prev, GamepadButtonFlags.RightShoulder, "RB");
            EmitButton(gp, _prev, GamepadButtonFlags.LeftThumb, "LS");
            EmitButton(gp, _prev, GamepadButtonFlags.RightThumb, "RS");
            EmitButton(gp, _prev, GamepadButtonFlags.DPadLeft, "LEFT");
            EmitButton(gp, _prev, GamepadButtonFlags.DPadRight, "RIGHT");
            EmitButton(gp, _prev, GamepadButtonFlags.DPadDown, "DOWN");
            EmitButton(gp, _prev, GamepadButtonFlags.DPadUp, "UP");
            EmitButton(gp, _prev, GamepadButtonFlags.Start, "START");
            EmitButton(gp, _prev, GamepadButtonFlags.Back, "BACK");

            EmitTrigger(gp.RightTrigger, _prev.RightTrigger, "RT");
            EmitTrigger(gp.LeftTrigger, _prev.LeftTrigger, "LT");

            EmitStick(gp.RightThumbX, _prev.RightThumbX, "RSX");
            EmitStick(gp.RightThumbY, _prev.RightThumbY, "RSY");
            EmitStick(gp.LeftThumbX, _prev.LeftThumbX, "LSX");
            EmitStick(gp.LeftThumbY, _prev.LeftThumbY, "LSY");
        }

        _prev = gp;
        _havePrev = true;
        lock (_stateLock) _mapped = gp;
    }

    /// <summary>DualSense/DS4 DirectInput state → the XInput gamepad shape.</summary>
    private static XGamepad MapToGamepad(JoystickState js)
    {
        GamepadButtonFlags flags = 0;
        var b = js.Buttons;
        void Set(int idx, GamepadButtonFlags f) { if (b != null && idx < b.Length && b[idx]) flags |= f; }

        Set(BtnCross, GamepadButtonFlags.A);
        Set(BtnCircle, GamepadButtonFlags.B);
        Set(BtnSquare, GamepadButtonFlags.X);
        Set(BtnTriangle, GamepadButtonFlags.Y);
        Set(BtnL1, GamepadButtonFlags.LeftShoulder);
        Set(BtnR1, GamepadButtonFlags.RightShoulder);
        Set(BtnShare, GamepadButtonFlags.Back);
        Set(BtnOptions, GamepadButtonFlags.Start);
        Set(BtnL3, GamepadButtonFlags.LeftThumb);
        Set(BtnR3, GamepadButtonFlags.RightThumb);

        // D-pad is a single POV hat in centidegrees (0=up, 9000=right, 18000=down, 27000=left);
        // centre is -1 on most drivers, but some report 65535; treat anything outside 0..36000° as
        // centre. Inclusive diagonal boundaries so 45° positions set the two adjacent directions.
        int pov = js.PointOfViewControllers is { Length: > 0 } p ? p[0] : -1;
        if (pov is >= 0 and <= 36000)
        {
            if (pov >= 31500 || pov <= 4500) flags |= GamepadButtonFlags.DPadUp;
            if (pov is >= 4500 and <= 13500) flags |= GamepadButtonFlags.DPadRight;
            if (pov is >= 13500 and <= 22500) flags |= GamepadButtonFlags.DPadDown;
            if (pov is >= 22500 and <= 31500) flags |= GamepadButtonFlags.DPadLeft;
        }

        return new XGamepad
        {
            Buttons = flags,
            LeftThumbX = ToAxis(js.X),
            LeftThumbY = ToAxisInverted(js.Y),        // DirectInput Y is +down; XInput is +up
            RightThumbX = ToAxis(js.Z),
            RightThumbY = ToAxisInverted(js.RotationZ),
            LeftTrigger = ToTrigger(js.RotationX),
            RightTrigger = ToTrigger(js.RotationY),
        };
    }

    // DirectInput axes are 0..65535 centred at 32768; XInput uses short centred at 0.
    private static short ToAxis(int di) => (short)Math.Clamp(di - 32768, short.MinValue, short.MaxValue);
    private static short ToAxisInverted(int di) => (short)Math.Clamp(32768 - di, short.MinValue, short.MaxValue);
    private static byte ToTrigger(int di) => (byte)Math.Clamp(di * 255 / 65535, 0, 255);

    private void EmitButton(XGamepad now, XGamepad was, GamepadButtonFlags flag, string name)
    {
        bool n = now.Buttons.HasFlag(flag), o = was.Buttons.HasFlag(flag);
        if (n && !o) InvokeEvent(new GamepadEventArgs { Button = name, IsPressed = true });
        else if (!n && o) InvokeEvent(new GamepadEventArgs { Button = name, IsPressed = false });
    }

    private void EmitTrigger(byte newValue, byte oldValue, string name)
    {
        if (newValue == oldValue) return;
        var value = newValue / 255.0f;
        var minValue = name == "LT" ? LeftTriggerThreshold : RightTriggerThreshold;
        InvokeEvent(new GamepadEventArgs { Button = name, IsPressed = value >= minValue, Value = value });
    }

    private void EmitStick(short newValue, short oldValue, string name)
    {
        if (newValue != oldValue)
            InvokeEvent(new GamepadEventArgs { Button = name, Value = newValue, IsStickEvent = true });
    }

    private void InvokeEvent(GamepadEventArgs args)
        => Task.Factory.StartNew(() => ButtonEvent?.Invoke(this, args), CancellationToken.None, TaskCreationOptions.None, _scheduler);

    public bool IsPressed(string button)
    {
        XGamepad g;
        lock (_stateLock) g = _mapped;
        return button switch
        {
            "A" => g.Buttons.HasFlag(GamepadButtonFlags.A),
            "B" => g.Buttons.HasFlag(GamepadButtonFlags.B),
            "X" => g.Buttons.HasFlag(GamepadButtonFlags.X),
            "Y" => g.Buttons.HasFlag(GamepadButtonFlags.Y),
            "LB" => g.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder),
            "RB" => g.Buttons.HasFlag(GamepadButtonFlags.RightShoulder),
            "RT" => g.RightTrigger > 0,
            "LT" => g.LeftTrigger > 0,
            "RS" => g.Buttons.HasFlag(GamepadButtonFlags.RightThumb),
            "LS" => g.Buttons.HasFlag(GamepadButtonFlags.LeftThumb),
            "LEFT" => g.Buttons.HasFlag(GamepadButtonFlags.DPadLeft),
            "RIGHT" => g.Buttons.HasFlag(GamepadButtonFlags.DPadRight),
            "DOWN" => g.Buttons.HasFlag(GamepadButtonFlags.DPadDown),
            "UP" => g.Buttons.HasFlag(GamepadButtonFlags.DPadUp),
            "START" => g.Buttons.HasFlag(GamepadButtonFlags.Start),
            "BACK" => g.Buttons.HasFlag(GamepadButtonFlags.Back),
            _ => false,
        };
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _pollingTask?.Wait(500); } catch { /* ignore */ }
        try { _joystick?.Unacquire(); } catch { /* ignore */ }
        _joystick?.Dispose();
        _directInput.Dispose();
        _cts.Dispose();
    }
}
