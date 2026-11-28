using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ProgressIconTests
{
    private static ContentPresenter Presenter(ProgressIcon icon) => Part<ContentPresenter>(icon, "PART_ContentPresenter");

    private static double Scale(Visual v) => v.RenderTransform?.Value.M11 ?? 1;

    // What a reading gives frame by frame for that long (real time, as the headless clock is).
    private static List<double> Sample(Window window, int milliseconds, Func<double> read)
    {
        var samples = new List<double>();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            samples.Add(read());
            Thread.Sleep(6);
        }
        return samples;
    }

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
        Settle(500);
        Assert.Equal(100, ring.Value, 1);
    }

    // While work runs the ring never stands still, as the Play Store's install ring: it turns at a steady pace whatever the value, a short arc before anything is done, and its arc grows to each new value along an ease, never in a jump.
    [AvaloniaFact]
    public void A_running_ring_keeps_turning_and_eases_to_each_value_from_a_short_arc()
    {
        Motion.SystemOverride = false;
        try
        {
            var icon = new ProgressIcon { Content = new Icon { Size = 22 } };
            var w = Show(new StackPanel { Margin = new Thickness(40), Children = { icon } });
            var ring = Part<ProgressRing>(icon, "PART_Ring");
            icon.Value = 0;
            Settle(400);
            Assert.Equal(TurningFill.MinShare * 100, ring.Value, 1);
            var turns = Sample(w, 300, () => ring.Turn);
            Assert.True(turns.Distinct().Count() > 3, "the ring turns before anything is done");
            icon.Value = 60;
            var fills = Sample(w, 700, () => ring.Value);
            Assert.Contains(fills, v => v > TurningFill.MinShare * 100 + 2 && v < 58);
            Assert.True(fills.Zip(fills.Skip(1)).All(p => p.Second >= p.First - 1e-9), "the arc only grows to a higher value");
            Assert.Equal(60, fills[^1], 1);
            turns = Sample(w, 300, () => ring.Turn);
            Assert.True(turns.Distinct().Count() > 3, "the ring turns on between values");
            // back to nothing: it fades out as it was, then its frames stop
            icon.Value = double.NaN;
            Settle(600);
            var still = ring.Turn;
            Settle(150);
            Assert.Equal(still, ring.Turn);
            Assert.Equal(60, ring.Value, 1);
            // a new run starts from the short arc again
            icon.Value = 30;
            Assert.True(Sample(w, 60, () => ring.Value).Min() < 30);
        }
        finally
        {
            Motion.SystemOverride = null;
        }
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
        var ring = Part<ProgressRing>(icon, "PART_Ring");
        Assert.Equal(0.6, Scale(Presenter(icon)), 2);
        Assert.Equal(1, ring.Opacity, 2);
        // the fill at once, from twelve, and no turning; a short arc at 0
        Assert.Equal(50, ring.Value, 2);
        Assert.Equal(0, ring.Turn);
        Settle(200);
        Assert.Equal(0, ring.Turn);
        icon.Value = 0;
        Settle(30);
        Assert.Equal(TurningFill.MinShare * 100, ring.Value, 2);
    }
}
