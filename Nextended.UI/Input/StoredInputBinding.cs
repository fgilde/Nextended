using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nextended.Core.Helper;
using Nextended.UI.Input.Gamepad;

namespace Nextended.UI.Input;

/// <summary>Source device of a single input binding.</summary>
public enum InputDeviceType
{
    None = 0,
    Keyboard = 1,
    Mouse = 2,
    Gamepad = 3,
}

/// <summary>
///     One stored input binding: EITHER a single input (<see cref="Key"/> + <see cref="Device"/>)
///     OR a chord/combo (<see cref="Components"/> — every child must be held simultaneously).
///     Children of a combo are always plain singles (never nested).
///     Serializes as a plain POCO: Key, Device, MinTime, Components.
/// </summary>
public class StoredInputBinding
{
    public string? Key { get; set; }

    public InputDeviceType Device { get; set; }

    /// <summary>Hold-to-trigger time in seconds (0 = fire immediately).</summary>
    public double MinTime { get; set; }

    /// <summary>
    ///     Component inputs of a CHORD/COMBO binding (e.g. Ctrl+Shift+X, LT+A).
    ///     <c>null</c> = a plain single binding. When set, every child must be held
    ///     simultaneously to match.
    /// </summary>
    public List<StoredInputBinding>? Components { get; set; }

    /// <summary>True when this binding is a multi-input chord rather than a single input.</summary>
    [JsonIgnore]
    public bool IsCombo => Components is { Count: > 0 };

    [JsonIgnore]
    public bool IsValid => IsCombo
        ? Components!.All(c => c is { IsValid: true })
        : !string.IsNullOrWhiteSpace(Key)
          && !string.Equals(Key, "none", StringComparison.OrdinalIgnoreCase)
          && Device != InputDeviceType.None;

    [JsonIgnore]
    public string DeviceName => IsCombo ? "Combo" : Device.ToString();

    public static StoredInputBinding Empty => new();

    public StoredInputBinding()
    { }

    public StoredInputBinding(KeyEventArgs data)
    {
        Key = data.KeyCode.ToString();
        Device = InputDeviceType.Keyboard;
    }

    public StoredInputBinding(MouseEventArgs data)
    {
        Key = data.Button.ToString();
        Device = InputDeviceType.Mouse;
    }

    public StoredInputBinding(GamepadEventArgs data)
    {
        Key = data.Code;
        Device = InputDeviceType.Gamepad;
    }

    public StoredInputBinding(Keys key)
    {
        Key = key.ToString();
        Device = InputDeviceType.Keyboard;
    }

    public StoredInputBinding(MouseButtons button)
    {
        Key = button.ToString();
        Device = InputDeviceType.Mouse;
    }

    public StoredInputBinding(GamepadButton button)
    {
        Key = button.ToDescriptionString();
        Device = InputDeviceType.Gamepad;
    }

    public StoredInputBinding(GamepadSlider slider)
    {
        Key = slider.ToDescriptionString();
        Device = InputDeviceType.Gamepad;
    }

    public static implicit operator StoredInputBinding(Keys a) => new(a);
    public static implicit operator StoredInputBinding(MouseButtons a) => new(a);
    public static implicit operator StoredInputBinding(GamepadButton a) => new(a);
    public static implicit operator StoredInputBinding(GamepadSlider a) => new(a);

    /// <summary>
    ///     Build a chord from its parts. Invalid parts are dropped; an empty result is
    ///     <see cref="Empty"/>, and a single surviving part collapses back to that plain single —
    ///     so there is never a 1-element combo (keeps Equals/GetHashCode unambiguous). The parent
    ///     carries a joined <see cref="Key"/> for display and leaves <see cref="Device"/> None.
    /// </summary>
    public static StoredInputBinding Combo(IEnumerable<StoredInputBinding> parts)
    {
        var valid = parts.Where(p => p is { IsValid: true }).ToList();
        if (valid.Count == 0) return Empty;
        if (valid.Count == 1) return valid[0];
        return new StoredInputBinding
        {
            Components = valid,
            Key = string.Join("+", valid.Select(p => p.Key)),
        };
    }

    /// <summary>The component singles of a combo, or this single itself — always plain singles.</summary>
    public IEnumerable<StoredInputBinding> Flatten() => IsCombo ? Components! : new[] { this };

    public StoredInputBinding SetMinTime(double value)
    {
        MinTime = value;
        return this;
    }

    /// <summary>A copy of this binding with MinTime cleared (components copied too).</summary>
    public StoredInputBinding WithoutMinTime() => new()
    {
        Key = Key,
        Device = Device,
        Components = Components?.Select(c => c.WithoutMinTime()).ToList(),
    };

    public bool Equals(StoredInputBinding? other)
    {
        if (other is null) return false;
        if (!IsValid && !other.IsValid) return true;
        if (IsCombo || other.IsCombo)
        {
            // Combos compare as an order-independent SET of components; a combo never equals a single.
            if (!IsCombo || !other.IsCombo || Components!.Count != other.Components!.Count) return false;
            var remaining = new List<StoredInputBinding>(other.Components!);
            foreach (var c in Components!)
            {
                int idx = remaining.FindIndex(o => c.Equals(o));
                if (idx < 0) return false;
                remaining.RemoveAt(idx);
            }
            return true;
        }
        return string.Equals(Key, other.Key, StringComparison.Ordinal) && Device == other.Device;
    }

    public override bool Equals(object? obj) => obj is StoredInputBinding other && Equals(other);

    public override int GetHashCode()
    {
        // Combos must hash order-independently so {Ctrl,X} and {X,Ctrl} collide (Equals is a set
        // compare). XOR of component hashes is order-independent.
        if (IsCombo)
        {
            int h = 19;
            foreach (var c in Components!) h ^= c.GetHashCode();
            return h;
        }
        return HashCode.Combine(Key, Device);
    }

    public override string ToString() => Key ?? "None";

    public bool IsHoldingFor(TimeSpan? timeSpan = null)
        => InputBindingManager.IsHoldingBindingFor(this, timeSpan ?? TimeSpan.FromSeconds(MinTime));

    public bool IsHolding() => InputBindingManager.IsHoldingBindingFor(this, null);

    /// <summary>
    ///     Waits <see cref="MinTime"/> seconds and reports whether the binding was held the whole
    ///     time (releases cancel the wait). MinTime &lt;= 0 returns the current holding state.
    /// </summary>
    public async Task<bool> WaitHoldingFor()
    {
        var cancel = new CancellationTokenSource();
        Action<StoredInputBinding>? onReleased = s =>
        {
            if (s?.Equals(this) == true)
                cancel.Cancel();
        };
        try
        {
            if (MinTime <= 0)
                return IsHolding();
            if (InputBindingManager.Instance != null)
                InputBindingManager.Instance.OnKeyReleased += onReleased;

            await Task.Delay(TimeSpan.FromSeconds(MinTime), cancel.Token);
            return !cancel.IsCancellationRequested && IsHolding();
        }
        catch
        {
            return false;
        }
        finally
        {
            if (InputBindingManager.Instance != null)
                InputBindingManager.Instance.OnKeyReleased -= onReleased;
        }
    }
}
