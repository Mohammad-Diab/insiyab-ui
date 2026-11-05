using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ProgressGrowTests
{
    private static void Tick(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        do
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(5);
        }
        while (clock.ElapsedMilliseconds < milliseconds);
    }

    private static (ProgressBar Bar, Border Fill) Add(StackPanel page, bool grow)
    {
        var bar = new ProgressBar { Value = 60, Width = 400 };
        if (grow) bar.Classes.Add("grow");
        page.Children.Add(bar);
        Tick(60);
        return (bar, Part<Border>(bar, "PART_Indicator"));
    }

    [AvaloniaFact]
    public void A_grow_bar_glides_from_empty_when_first_shown()
    {
        var page = new StackPanel { Margin = new Thickness(20) };
        Show(page);
        var (_, fill) = Add(page, true);
        Assert.InRange(fill.Bounds.Width, 1, 239);
        Settle();
        Assert.Equal(240, fill.Bounds.Width, 0.5);
    }

    [AvaloniaFact]
    public void A_plain_bar_takes_its_first_value_at_once()
    {
        var page = new StackPanel { Margin = new Thickness(20) };
        Show(page);
        var (_, fill) = Add(page, false);
        Assert.Equal(240, fill.Bounds.Width, 0.5);
    }

    [AvaloniaFact]
    public void A_still_window_shows_the_value_at_once()
    {
        var page = new StackPanel { Margin = new Thickness(20) };
        var window = Show(page);
        Motion.SetStill(window, true);
        var (_, fill) = Add(page, true);
        Assert.Equal(240, fill.Bounds.Width, 0.5);
    }

    [AvaloniaFact]
    public void A_grow_bar_shown_later_glides_and_then_follows_its_value()
    {
        var bar = new ProgressBar { Value = 50, Width = 400, Classes = { "grow" } };
        var holder = new Border { IsVisible = false, Child = bar };
        Show(new StackPanel { Margin = new Thickness(20), Children = { holder } });
        holder.IsVisible = true;
        Tick(60);
        var fill = Part<Border>(bar, "PART_Indicator");
        Assert.InRange(fill.Bounds.Width, 1, 199);
        Settle();
        Assert.Equal(200, fill.Bounds.Width, 0.5);
        bar.Value = 80;
        Tick(60);
        Assert.InRange(fill.Bounds.Width, 201, 319);
    }

    [AvaloniaFact]
    public void A_grow_bar_below_the_fold_waits_to_be_scrolled_to()
    {
        var bar = new ProgressBar { Value = 50, Width = 400, Classes = { "grow" } };
        var scroll = new ScrollViewer { Height = 300, Content = new StackPanel { Children = { new Border { Height = 900 }, bar } } };
        Show(scroll);
        var fill = Part<Border>(bar, "PART_Indicator");
        Assert.Equal(0, fill.Bounds.Width, 0.5);

        scroll.Offset = new Vector(0, 700);
        Tick(60);
        Assert.InRange(fill.Bounds.Width, 1, 199);
        Settle();
        Assert.Equal(200, fill.Bounds.Width, 0.5);
    }
}
