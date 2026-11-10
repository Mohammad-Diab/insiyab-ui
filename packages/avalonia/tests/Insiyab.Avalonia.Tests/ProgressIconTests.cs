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

public class ProgressIconTests
{
    private static ContentPresenter Presenter(ProgressIcon icon) => Part<ContentPresenter>(icon, "PART_ContentPresenter");

    private static double Scale(Visual v) => v.RenderTransform?.Value.M11 ?? 1;

    [AvaloniaFact]
    public void An_icon_shows_no_ring_until_it_is_given_a_value()
    {
        var icon = new ProgressIcon();
        Assert.True(double.IsNaN(icon.Value));
        Assert.False(icon.IsIndeterminate);
        Show(new StackPanel { Margin = new Thickness(40), Children = { icon } });
        Assert.DoesNotContain(":busy", icon.Classes);
        Assert.Equal(0, Part<ProgressRing>(icon, "PART_Ring").Opacity);
        Assert.False(Part<LoadingSpinner>(icon, "PART_Spinner").IsVisible);
        Assert.Equal(1, Scale(Presenter(icon)), 2);
    }

    [AvaloniaFact]
    public void A_value_shrinks_the_icon_inside_a_ring_and_the_size_stays()
    {
        var icon = new ProgressIcon { Content = new Icon { Size = 22 } };
        var button = new Button { Classes = { "bare", "icon" }, Width = 44, Height = 44, Content = icon };
        Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        var before = (icon.Bounds, button.Bounds);
        icon.Value = 24;
        Settle();
        var ring = Part<ProgressRing>(icon, "PART_Ring");
        Assert.Contains(":progress", icon.Classes);
        Assert.Contains(":busy", icon.Classes);
        Assert.Equal(24, ring.Value);
        Assert.Equal(2, ring.Thickness);
        Assert.Equal(1, ring.Opacity, 2);
        Assert.Equal(0.6, Scale(Presenter(icon)), 2);
        // the ring sits 4px outside the icon's box and takes no room
        Assert.Equal(30, ring.Bounds.Width, 0.5);
        Assert.Equal(before, (icon.Bounds, button.Bounds));
        AssertBrush(ring, "InsPrimaryMid", ring.Fill);
        Assert.Empty(ring.GetVisualChildren());
    }

    [AvaloniaFact]
    public void Back_to_nan_the_ring_fades_with_its_last_fill_and_the_icon_grows_back()
    {
        var icon = new ProgressIcon { Content = new Icon { Size = 22 }, Value = 80 };
        Show(new StackPanel { Margin = new Thickness(40), Children = { icon } });
        icon.Value = double.NaN;
        Settle();
        var ring = Part<ProgressRing>(icon, "PART_Ring");
        Assert.DoesNotContain(":progress", icon.Classes);
        Assert.Equal(80, ring.Value);
        Assert.Equal(0, ring.Opacity, 2);
        Assert.Equal(1, Scale(Presenter(icon)), 2);
        icon.Value = 140;
        Assert.Equal(100, ring.Value);
    }

    [AvaloniaFact]
    public void Waiting_turns_an_arc_in_the_rings_place()
    {
        var icon = new ProgressIcon { Content = new Icon { Size = 22 }, Value = 30, IsIndeterminate = true };
        Show(new StackPanel { Margin = new Thickness(40), Children = { icon } });
        var spinner = Part<LoadingSpinner>(icon, "PART_Spinner");
        Assert.Contains(":waiting", icon.Classes);
        Assert.Contains(":busy", icon.Classes);
        Assert.DoesNotContain(":progress", icon.Classes);
        Assert.True(spinner.IsVisible);
        Assert.Equal(30, spinner.Bounds.Width, 0.5);
        Assert.Equal(2, spinner.Thickness);
        Assert.Equal(0, Part<ProgressRing>(icon, "PART_Ring").Opacity, 2);
        Assert.Equal(0.6, Scale(Presenter(icon)), 2);
    }

    [AvaloniaFact]
    public void Inside_a_button_the_arc_keeps_the_brand_colour()
    {
        var icon = new ProgressIcon { Content = new Icon { Size = 22 }, IsIndeterminate = true };
        Show(new StackPanel { Margin = new Thickness(40), Children = { new Button { Content = icon } } });
        var spinner = Part<LoadingSpinner>(icon, "PART_Spinner");
        AssertBrush(spinner, "InsPrimaryMid", spinner.Arc);
    }

    [AvaloniaFact]
    public void A_still_window_changes_at_once()
    {
        var icon = new ProgressIcon { Content = new Icon { Size = 22 } };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { icon } });
        Motion.SetStill(window, true);
        Settle(50);
        icon.Value = 50;
        Settle(30);
        Assert.Equal(0.6, Scale(Presenter(icon)), 2);
        Assert.Equal(1, Part<ProgressRing>(icon, "PART_Ring").Opacity, 2);
    }
}
