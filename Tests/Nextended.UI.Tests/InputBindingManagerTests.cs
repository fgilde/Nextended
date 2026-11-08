using System.Windows.Forms;
using Nextended.UI.Input;
using Nextended.UI.Input.Gamepad;

namespace Nextended.UI.Tests;

public class InputBindingManagerTests
{
    private readonly FakeHook _hook = new();
    private readonly InputBindingManager _manager;

    public InputBindingManagerTests() => _manager = new InputBindingManager(enableGamepad: false, () => _hook);

    [Fact]
    public void Registered_single_raises_pressed_and_released()
    {
        var events = new List<string>();
        _manager.OnBindingPressed += id => events.Add("down:" + id);
        _manager.OnBindingReleased += id => events.Add("up:" + id);
        _manager.RegisterBinding("single", Keys.F7);

        _hook.Press(Keys.F7);
        _hook.Release(Keys.F7);

        Assert.Equal(["down:single", "up:single"], events);
    }

    [Fact]
    public void Chord_fires_only_once_all_parts_are_held()
    {
        var pressed = 0;
        _manager.OnBindingPressed += id => { if (id == "chord") pressed++; };
        _manager.RegisterBinding("chord", StoredInputBinding.Combo([Keys.LControlKey, Keys.F8]));

        _hook.Press(Keys.LControlKey);
        Assert.Equal(0, pressed);
        _hook.Press(Keys.F8);
        Assert.Equal(1, pressed);

        _hook.Release(Keys.F8);
        _hook.Release(Keys.LControlKey);
    }

    [Fact]
    public void Recording_commits_everything_held_on_first_release()
    {
        (string Id, StoredInputBinding Binding)? recorded = null;
        _manager.OnBindingSet += (id, b) => recorded = (id, b);
        _manager.StartListeningForBinding("rec");

        _hook.Press(Keys.LShiftKey);
        _hook.Press(MouseButtons.XButton1);
        _hook.Release(MouseButtons.XButton1);
        _hook.Release(Keys.LShiftKey);

        Assert.Equal("rec", recorded!.Value.Id);
        Assert.Equal(StoredInputBinding.Combo([Keys.LShiftKey, MouseButtons.XButton1]), recorded.Value.Binding);
        // Press order is kept so a modifier held first is replayed first.
        Assert.Equal(Keys.LShiftKey, recorded.Value.Binding.Components![0].KeyboardKey);
        Assert.Same(recorded.Value.Binding, _manager.Bindings["rec"]);
    }

    private sealed class FakePad : IGamepadReader
    {
        public bool IsConnected => true;
        public event EventHandler<GamepadEventArgs>? ButtonEvent;
        public bool IsPressed(string button) => false;
        // Like the readers: pressed by a fixed global threshold, value always reported.
        public void Trigger(string name, float value)
            => ButtonEvent?.Invoke(this, new GamepadEventArgs { Button = name, Value = value, IsPressed = value >= 0.7f });
        public void Dispose() { }
    }

    [Fact]
    public void A_trigger_range_decides_held_instead_of_the_global_threshold()
    {
        var pad = new FakePad();
        _manager.AttachGamepadReader(pad);
        var events = new List<string>();
        _manager.OnBindingPressed += id => events.Add("down:" + id);
        _manager.OnBindingReleased += id => events.Add("up:" + id);
        var light = new StoredInputBinding(GamepadSlider.LeftTrigger) { MinValue = 0.1, MaxValue = 0.5 };
        _manager.RegisterBinding("light", light);
        _manager.RegisterBinding("plain", GamepadSlider.LeftTrigger);

        pad.Trigger("LT", 0.3f); // below the global 70 %, inside the range
        Assert.Equal(["down:light"], events);
        Assert.True(light.IsHolding());

        pad.Trigger("LT", 0.9f); // past the range, past the global threshold
        Assert.Equal(["down:light", "up:light", "down:plain"], events);
        Assert.False(light.IsHolding());

        pad.Trigger("LT", 0f);
        Assert.Equal(["down:light", "up:light", "down:plain", "up:plain"], events);
    }

    [Fact]
    public void A_range_starting_at_zero_does_not_count_a_released_trigger()
        => Assert.False(new StoredInputBinding(GamepadSlider.RightTrigger) { MaxValue = 0.4 }.InRange(0));
}
