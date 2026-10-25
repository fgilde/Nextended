using System.Windows.Forms;
using Nextended.UI.Input;

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
}
