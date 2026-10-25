using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Nextended.UI.WPF.Extensions;

namespace Nextended.UI.Tests;

public class WindowAndElementExtensionsTests
{
    [Fact]
    public void MoveTo_subtracts_padding_on_both_sides() => Sta.Run(() =>
    {
        var window = new Window();

        window.MoveTo(new RectangleF(100, 50, 400, 300), new Thickness(10, 20, 30, 40));

        Assert.Equal((110d, 70d, 360d, 240d), (window.Left, window.Top, window.Width, window.Height));
    });

    [Fact]
    public void MoveTo_never_shrinks_below_ten() => Sta.Run(() =>
    {
        var window = new Window();

        window.MoveTo(new RectangleF(0, 0, 20, 20), new Thickness(15));

        Assert.Equal((10d, 10d), (window.Width, window.Height));
    });

    [Fact]
    public void Center_keeps_size_inside_bounds() => Sta.Run(() =>
    {
        var window = new Window { Width = 200, Height = 100 };

        window.Center(new Rectangle(1000, 0, 800, 600));

        Assert.Equal((1300d, 250d, 200d, 100d), (window.Left, window.Top, window.Width, window.Height));
    });

    [Fact]
    public void FindAncestors_walks_up_nearest_first() => Sta.Run(() =>
    {
        var button = new System.Windows.Controls.Button();
        var inner = new StackPanel { Tag = "inner", Children = { button } };
        var outer = new StackPanel { Tag = "outer", Children = { new Border { Child = inner } } };

        Assert.Equal([inner, outer], button.FindAncestors<StackPanel>());
        Assert.Equal([outer], button.FindAncestors<StackPanel>(p => (string)p.Tag == "outer"));
    });

    [Fact]
    public void Add_configures_hidden_elements_once_they_become_visible() => Sta.Run(() =>
    {
        var panel = new StackPanel();
        var configured = false;

        var button = panel.Add<System.Windows.Controls.Button>(_ => configured = true);

        Assert.Same(button, panel.Children[0]);
        Assert.False(configured); // not visible yet: configuration waits
        Assert.Empty(panel.RemoveAll().Children);
    });
}
