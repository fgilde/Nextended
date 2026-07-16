using System;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.XInput;

namespace Nextended.UI.Input.Gamepad;

/// <summary>
///     XInput polling reader (10 ms loop). Edge-detects buttons/triggers/sticks against the
///     previous state and raises <see cref="ButtonEvent"/> with the normalized string vocabulary.
/// </summary>
public class XInputGamepadReader : IXInputGamepadReader
{
    private UserIndex _userIndex;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _pollingTask;
    private readonly TaskScheduler _scheduler;

    public Controller Controller { get; private set; }
    public State State { get; private set; }

    /// <summary>Trigger value (0..1) above which LT counts as pressed.</summary>
    public float LeftTriggerThreshold { get; set; } = 0.1f;

    /// <summary>Trigger value (0..1) above which RT counts as pressed.</summary>
    public float RightTriggerThreshold { get; set; } = 0.1f;

    public event EventHandler<GamepadEventArgs>? ButtonEvent;

    public XInputGamepadReader(UserIndex userIndex = UserIndex.One)
    {
        _userIndex = userIndex;
        // Marshal ButtonEvent onto the captured context (UI thread when built there). Fall back to
        // the default scheduler if there is no sync context (e.g. a test host).
        try { _scheduler = TaskScheduler.FromCurrentSynchronizationContext(); }
        catch { _scheduler = TaskScheduler.Default; }
        Controller = ConnectController();
        StartPolling();
    }

    // The physical pad isn't always on XInput slot One — another device can take a lower slot and
    // leave slot One empty. Prefer the last-known slot (stay on the same pad across brief
    // drop-outs), otherwise scan all four. Never returns null so Controller is always assigned.
    private Controller ConnectController()
    {
        var preferred = new Controller(_userIndex);
        if (preferred.IsConnected) return preferred;
        for (var i = UserIndex.One; i <= UserIndex.Four; i++)
        {
            var candidate = new Controller(i);
            if (candidate.IsConnected) { _userIndex = i; return candidate; }
        }
        return preferred; // none connected yet — keep retrying the preferred slot
    }

    public UserIndex CurrentSlot => _userIndex;

    /// <summary>
    ///     Switch the physical source to a specific XInput slot at runtime. The poll loop simply
    ///     continues against the new <see cref="Controller"/> — no teardown/restart. State is reset
    ///     so we don't diff the new pad against the old pad's last state.
    /// </summary>
    public void UseSlot(UserIndex slot)
    {
        _userIndex = slot;
        Controller = new Controller(slot);
        State = default;
    }

    public bool IsConnected => Controller.IsConnected;

    public bool IsPressed(string button)
    {
        if (!IsConnected) return false;
        var g = State.Gamepad;
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

    private void StartPolling()
    {
        var token = _cancellationTokenSource.Token;
        _pollingTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (IsConnected)
                {
                    try { Poll(); }
                    catch { /* transient GetState failure (brief disconnect) — keep the loop alive */ }
                }
                else
                {
                    await Task.Delay(1000, token);
                    Controller = ConnectController(); // re-scan all slots — the pad may be on a different one
                }
                await Task.Delay(10, token);
            }
        }, token);
    }

    private void Poll()
    {
        var newState = Controller.GetState();

        CheckButtonState(newState, GamepadButtonFlags.A, "A");
        CheckButtonState(newState, GamepadButtonFlags.B, "B");
        CheckButtonState(newState, GamepadButtonFlags.X, "X");
        CheckButtonState(newState, GamepadButtonFlags.Y, "Y");
        CheckButtonState(newState, GamepadButtonFlags.LeftShoulder, "LB");
        CheckButtonState(newState, GamepadButtonFlags.RightShoulder, "RB");
        CheckButtonState(newState, GamepadButtonFlags.RightThumb, "RS");
        CheckButtonState(newState, GamepadButtonFlags.LeftThumb, "LS");
        CheckButtonState(newState, GamepadButtonFlags.DPadLeft, "LEFT");
        CheckButtonState(newState, GamepadButtonFlags.DPadRight, "RIGHT");
        CheckButtonState(newState, GamepadButtonFlags.DPadDown, "DOWN");
        CheckButtonState(newState, GamepadButtonFlags.DPadUp, "UP");
        CheckButtonState(newState, GamepadButtonFlags.Start, "START");
        CheckButtonState(newState, GamepadButtonFlags.Back, "BACK");

        CheckTriggerState(newState.Gamepad.RightTrigger, State.Gamepad.RightTrigger, "RT");
        CheckTriggerState(newState.Gamepad.LeftTrigger, State.Gamepad.LeftTrigger, "LT");

        CheckStickState(newState.Gamepad.RightThumbX, State.Gamepad.RightThumbX, "RSX");
        CheckStickState(newState.Gamepad.RightThumbY, State.Gamepad.RightThumbY, "RSY");
        CheckStickState(newState.Gamepad.LeftThumbX, State.Gamepad.LeftThumbX, "LSX");
        CheckStickState(newState.Gamepad.LeftThumbY, State.Gamepad.LeftThumbY, "LSY");

        State = newState;
    }

    private void CheckButtonState(State newState, GamepadButtonFlags flag, string buttonName)
    {
        bool now = newState.Gamepad.Buttons.HasFlag(flag);
        bool was = State.Gamepad.Buttons.HasFlag(flag);
        if (now && !was)
            InvokeEvent(new GamepadEventArgs { Button = buttonName, IsPressed = true });
        else if (!now && was)
            InvokeEvent(new GamepadEventArgs { Button = buttonName, IsPressed = false });
    }

    private void CheckTriggerState(byte newValue, byte oldValue, string triggerName)
    {
        if (newValue == oldValue) return;
        var value = newValue / 255.0f;
        var minValue = triggerName == "LT" ? LeftTriggerThreshold : RightTriggerThreshold;
        InvokeEvent(new GamepadEventArgs { Button = triggerName, IsPressed = value >= minValue, Value = value });
    }

    private void CheckStickState(short newValue, short oldValue, string stickName)
    {
        if (newValue != oldValue)
            InvokeEvent(new GamepadEventArgs { Button = stickName, Value = newValue, IsStickEvent = true });
    }

    private void InvokeEvent(GamepadEventArgs args)
        => Task.Factory.StartNew(() => ButtonEvent?.Invoke(this, args), CancellationToken.None, TaskCreationOptions.None, _scheduler);

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        try { _pollingTask?.Wait(500); } catch { /* ignore */ }
        _cancellationTokenSource.Dispose();
    }
}
