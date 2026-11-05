using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class SmallSwitchTests
{
    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void A_small_switch_stands_as_tall_as_a_small_button(FlowDirection dir)
    {
        var toggle = new ToggleSwitch { Classes = { "sm" }, Content = "ساعات العمل" };
        var button = new Button { Classes = { "sm" }, Content = "اليوم" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Orientation = Orientation.Horizontal, Children = { button, toggle } }, dir);
        var track = Part<Panel>(toggle, "SwitchAreaGrid");
        var knob = Part<Panel>(toggle, "PART_MovingKnobs");
        var label = Part<ContentPresenter>(toggle, "PART_ContentPresenter");

        Assert.Equal((double)Res(toggle, "InsCtlSmH"), toggle.Bounds.Height, 0.5);
        Assert.Equal(36, track.Bounds.Width, 0.5);
        Assert.Equal(20, track.Bounds.Height, 0.5);
        Assert.Equal(14, knob.Bounds.Width, 0.5);
        Assert.Equal((double)Res(toggle, "InsFsSm"), label.FontSize, 0.01);

        toggle.IsChecked = true;
        Settle();
        var trackBox = track.GetTransformedBounds()!.Value;
        var knobBox = knob.GetTransformedBounds()!.Value;
        var t = trackBox.Bounds.TransformToAABB(trackBox.Transform);
        var k = knobBox.Bounds.TransformToAABB(knobBox.Transform);
        if (dir == FlowDirection.LeftToRight) Assert.Equal(t.Right - 3, k.Right, 0.5);
        else Assert.Equal(t.Left + 3, k.Left, 0.5);
    }
}
