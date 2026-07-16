using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Gma.System.MouseKeyHook;
using Nextended.UI.Input.Gamepad;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace Nextended.UI.Input;

/// <summary>
///     Global input listener + binding matcher. Installs a low-level keyboard/mouse hook (via
///     MouseKeyHook) and optionally polls a gamepad; matches registered
///     <see cref="StoredInputBinding"/>s (singles AND chords like Ctrl+X or LT+A) against the live
///     held-input set and raises <see cref="OnBindingPressed"/>/<see cref="OnBindingReleased"/>.
///     Also records new bindings (commit-on-release) for the key-changer controls.
/// </summary>
public class InputBindingManager : IDisposable
{
    /// <summary>Last created instance (the typical app has exactly one).</summary>
    public static InputBindingManager? Instance { get; private set; }

    public InputBindingManager(bool enableGamepad = true)
    {
        EnableGamepad = enableGamepad;
        Instance = this;
    }

    private IKeyboardMouseEvents? _mEvents;
    private bool _gamepadListen;
    private IGamepadReader? _gamepadReader;
    private readonly Dictionary<string, StoredInputBinding> bindings = new();
    private static readonly Dictionary<string, (bool IsHolding, DateTime StartTime)> isHolding = new();
    private string? settingBindingId;
    private static readonly List<(StoredInputBinding Binding, bool IsHolding, DateTime StartTime)> holdingBindings = new();

    // Live device-qualified set of currently-held raw inputs — the source of truth for matching
    // both singles AND chords. ConcurrentDictionary so the kb/mouse-hook thread and the gamepad
    // callback can mutate it without corrupting a plain HashSet.
    private static readonly ConcurrentDictionary<string, byte> _heldKeys = new();
    // When each currently-held chord first became fully held (for combo MinTime).
    private static readonly ConcurrentDictionary<StoredInputBinding, DateTime> _comboHeldSince = new();
    // Recording accumulator — press order preserved so a modifier held first is listed first.
    private readonly List<StoredInputBinding> _recordPressed = new();
    private bool _recordStarted;

    /// <summary>When true (default), an XInput gamepad reader is created lazily on first use.</summary>
    public bool EnableGamepad { get; set; }

    /// <summary>The gamepad reader currently feeding events (null until listening starts).</summary>
    public IGamepadReader? GamepadReader => _gamepadReader;

    public event Action<string, StoredInputBinding>? OnBindingSet;
    public event Action<StoredInputBinding>? OnKeyPressed;
    public event Action<StoredInputBinding>? OnKeyReleased;
    public event Action<string>? OnBindingPressed;
    public event Action<string>? OnBindingReleased;
    public event Action<MouseEventArgs>? OnMouseMove;

    /// <summary>All currently registered bindings by id.</summary>
    public IReadOnlyDictionary<string, StoredInputBinding> Bindings => bindings;

    /// <summary>
    ///     Attach a custom gamepad reader (e.g. a <see cref="DirectInputGamepadReader"/> for a raw
    ///     DualSense) instead of the default XInput reader. Disposes/replaces a previous reader.
    /// </summary>
    public void AttachGamepadReader(IGamepadReader reader)
    {
        if (_gamepadReader != null)
        {
            _gamepadReader.ButtonEvent -= GamepadReader_ButtonEvent;
            _gamepadReader.Dispose();
        }
        _gamepadReader = reader;
        _gamepadReader.ButtonEvent += GamepadReader_ButtonEvent;
        _gamepadListen = true;
    }

    public static bool IsHoldingBinding(string bindingId)
        => isHolding.TryGetValue(bindingId, out var holding) && holding.IsHolding;

    public static TimeSpan GetHoldingTime(string bindingId)
    {
        if (isHolding.TryGetValue(bindingId, out var holding) && holding.IsHolding)
            return DateTime.Now - holding.StartTime;
        return TimeSpan.Zero;
    }

    public static bool IsHoldingBinding(StoredInputBinding binding) => IsHoldingBindingFor(binding, null);

    public static bool IsHoldingBindingFor(StoredInputBinding binding, TimeSpan? duration)
    {
        if (binding is not { IsValid: true }) return false;

        if (binding.IsCombo)
        {
            // A chord counts as held only while ALL its components are in the live held set.
            if (!IsHeldNow(binding)) { _comboHeldSince.TryRemove(binding, out _); return false; }
            if (duration == null || duration <= TimeSpan.Zero) return true;
            // Lazily stamp when the chord first became fully held — works whether or not the chord
            // is registered in `bindings`.
            var since = _comboHeldSince.GetOrAdd(binding, _ => DateTime.Now);
            return (DateTime.Now - since) >= duration;
        }

        var holdingEntry = holdingBindings.FirstOrDefault(h => h.Binding.Equals(binding));
        if (holdingEntry.Binding is { IsValid: true } && holdingEntry.IsHolding)
        {
            if (duration == null || duration <= TimeSpan.Zero)
                return true;
            return (DateTime.Now - holdingEntry.StartTime) >= duration;
        }

        return false;
    }

    public static bool IsHoldingBindingFor(string bindingId, TimeSpan? duration)
    {
        if (duration == null || duration <= TimeSpan.Zero)
            return IsHoldingBinding(bindingId);
        return GetHoldingTime(bindingId) >= duration;
    }

    // Device-qualified key for one raw input ("Keyboard:X", "Mouse:Left", "Gamepad:RT") — the
    // qualifier stops keyboard "A" colliding with gamepad "A".
    private static string Qual(StoredInputBinding single) => $"{single.DeviceName}:{single.Key}";

    // True when the binding is fully held right now: a single → its key in the held set; a combo
    // → every component key in the held set.
    private static bool IsHeldNow(StoredInputBinding b) =>
        b.IsCombo
            ? b.Components!.All(c => _heldKeys.ContainsKey(Qual(c)))
            : _heldKeys.ContainsKey(Qual(b));

    private void UpdateHoldingState(StoredInputBinding binding, bool isPressed)
    {
        if (isPressed)
            OnKeyPressed?.Invoke(binding);
        else
            OnKeyReleased?.Invoke(binding);

        var index = holdingBindings.FindIndex(h => h.Binding.Equals(binding));
        if (index != -1)
            holdingBindings[index] = isPressed ? (binding, true, DateTime.Now) : (binding, false, DateTime.MinValue);
        else if (isPressed)
            holdingBindings.Add((binding, true, DateTime.Now));
    }

    /// <summary>Register (or replace) a binding under an id and start listening.</summary>
    public void RegisterBinding(string bindingId, StoredInputBinding bindingValue)
    {
        if (bindingValue is not { IsValid: true })
            return;
        bindings[bindingId] = bindingValue;
        isHolding[bindingId] = (false, DateTime.MinValue);
        OnBindingSet?.Invoke(bindingId, bindingValue);
        EnsureHookEvents();
    }

    /// <summary>Remove a registered binding by id.</summary>
    public void RemoveBinding(string bindingId)
    {
        bindings.Remove(bindingId);
        isHolding.Remove(bindingId);
    }

    /// <summary>
    ///     Arm recording for the given id: the next inputs pressed together become the new binding
    ///     (committed on first release, 1 input → single, ≥2 → chord), raised via
    ///     <see cref="OnBindingSet"/>.
    /// </summary>
    public void StartListeningForBinding(string bindingId)
    {
        settingBindingId = bindingId;
        _recordPressed.Clear();
        _recordStarted = false;
        EnsureHookEvents();
    }

    /// <summary>Ensure the global hook (and gamepad reader) are installed and feeding events.</summary>
    public void EnsureHookEvents()
    {
        if (_mEvents == null)
        {
            _mEvents = Hook.GlobalEvents();
            _mEvents.KeyDown += GlobalHookKeyDown!;
            _mEvents.MouseDown += GlobalHookMouseDown!;
            _mEvents.KeyUp += GlobalHookKeyUp!;
            _mEvents.MouseUp += GlobalHookMouseUp!;
            _mEvents.MouseMove += MEventsOnMouseMove;
        }

        if (!_gamepadListen && EnableGamepad)
        {
            _gamepadListen = true;
            _gamepadReader = new XInputGamepadReader();
            _gamepadReader.ButtonEvent += GamepadReader_ButtonEvent;
        }
    }

    private void MEventsOnMouseMove(object? sender, MouseEventArgs e) => OnMouseMove?.Invoke(e);

    private void GamepadReader_ButtonEvent(object? sender, GamepadEventArgs e)
    {
        if (e.IsStickEvent) return; // sticks are never bindings
        OnRawInput(new StoredInputBinding(e), e.IsPressed == true);
    }

    private void InvokeBindingReleased(KeyValuePair<string, StoredInputBinding> binding)
        => OnBindingReleased?.Invoke(binding.Key);

    private async void InvokeBindingPressed(KeyValuePair<string, StoredInputBinding> binding)
    {
        if (binding.Value == null || await binding.Value.WaitHoldingFor())
            OnBindingPressed?.Invoke(binding.Key);
    }

    // All four global hooks (and the gamepad callback) funnel into one pipeline so single- and
    // chord-matching live in exactly one place.
    private void GlobalHookKeyDown(object sender, KeyEventArgs e) => OnRawInput(new StoredInputBinding(e), true);
    private void GlobalHookKeyUp(object sender, KeyEventArgs e) => OnRawInput(new StoredInputBinding(e), false);
    private void GlobalHookMouseDown(object sender, MouseEventArgs e) => OnRawInput(new StoredInputBinding(e), true);
    private void GlobalHookMouseUp(object sender, MouseEventArgs e) => OnRawInput(new StoredInputBinding(e), false);

    // ===== Unified raw-input pipeline (single + combo) =====
    private void OnRawInput(StoredInputBinding raw, bool pressed)
    {
        if (raw is not { IsValid: true }) return;
        if (settingBindingId != null) { HandleRecording(raw, pressed); return; }

        // Per-input hold tracking + OnKeyPressed/Released (raw is always a single here).
        UpdateHoldingState(raw, pressed);

        // The live device-qualified held set drives matching for singles AND chords.
        var qual = Qual(raw);
        if (pressed) _heldKeys[qual] = 0; else _heldKeys.TryRemove(qual, out _);

        // Edge-detect every registered binding against the new held set: rising → Pressed, falling
        // → Released. A single is just a 1-component chord, so this path covers both uniformly.
        foreach (var kv in bindings.ToArray())
        {
            bool nowHeld = IsHeldNow(kv.Value);
            bool wasHeld = isHolding.TryGetValue(kv.Key, out var st) && st.IsHolding;
            if (nowHeld && !wasHeld)
            {
                isHolding[kv.Key] = (true, DateTime.Now);
                InvokeBindingPressed(kv);
            }
            else if (!nowHeld && wasHeld)
            {
                isHolding[kv.Key] = (false, DateTime.MinValue);
                InvokeBindingReleased(kv);
            }
        }
    }

    // Commit-on-release recording: accumulate everything pressed during the record session, then on
    // the FIRST genuine release snapshot it all into a single (1 input) or a chord (>=2). Press
    // order is preserved so a modifier held first is listed first.
    private void HandleRecording(StoredInputBinding raw, bool pressed)
    {
        if (pressed)
        {
            if (!_recordPressed.Any(p => Qual(p) == Qual(raw))) _recordPressed.Add(raw);
            _recordStarted = true;
            return;
        }
        // Ignore: (a) releases before anything was pressed — e.g. the very click that started
        // recording; (b) releases of an input never pressed this session — e.g. a gamepad trigger's
        // spurious sub-threshold IsPressed=false events.
        if (!_recordStarted || !_recordPressed.Any(p => Qual(p) == Qual(raw))) return;

        var id = settingBindingId!;
        var combo = StoredInputBinding.Combo(_recordPressed.ToList());
        settingBindingId = null;
        _recordStarted = false;
        _recordPressed.Clear();
        bindings[id] = combo;
        OnBindingSet?.Invoke(id, combo);
    }

    public void StopListening()
    {
        if (_gamepadReader != null)
        {
            _gamepadReader.ButtonEvent -= GamepadReader_ButtonEvent;
            _gamepadReader.Dispose();
            _gamepadReader = null;
            _gamepadListen = false;
        }
        if (_mEvents != null)
        {
            _mEvents.KeyDown -= GlobalHookKeyDown!;
            _mEvents.MouseDown -= GlobalHookMouseDown!;
            _mEvents.KeyUp -= GlobalHookKeyUp!;
            _mEvents.MouseUp -= GlobalHookMouseUp!;
            _mEvents.MouseMove -= MEventsOnMouseMove;
            _mEvents.Dispose();
            _mEvents = null;
        }
    }

    public void Dispose()
    {
        StopListening();
        bindings.Clear();
        if (Instance == this)
            Instance = null;
    }
}
