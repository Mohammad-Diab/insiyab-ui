using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Motion nobody can see does not run: a loop rests and an entrance waits until the control is seen.
public class OffscreenMotionTests
{
    private const string SeenClass = ":ins-seen";

    // The subject sits 2000px down a 200px scroller, so it starts out of view.
    private static (Window Window, ScrollViewer Scroll) Rig(Control subject)
    {
        var scroll = new ScrollViewer { Height = 200, Content = new StackPanel { Children = { new Border { Height = 2000 }, subject } } };
        var window = Show(scroll);
        Settle(200);
        return (window, scroll);
    }

    private static void ScrollIn(ScrollViewer scroll, int milliseconds = 150)
    {
        scroll.ScrollToEnd();
        Settle(milliseconds);
    }

    private static void ScrollOut(ScrollViewer scroll, int milliseconds = 150)
    {
        scroll.ScrollToHome();
        Settle(milliseconds);
    }

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

    // Entrances run on a clock this holds and steps, so a slow run sees the same frames as a fast one.
    private sealed class HeldEntrance : IDisposable
    {
        private readonly Func<double> _real = Entrance.Now;
        private double _at = 1000;

        public HeldEntrance() => Entrance.Now = () => _at;

        public void To(double milliseconds, int settle = 60)
        {
            _at = 1000 + milliseconds;
            Settle(settle);
        }

        public void Dispose() => Entrance.Now = _real;
    }

    [AvaloniaFact]
    public void A_spinner_rests_out_of_view_and_turns_again_in_view()
    {
        var spinner = new LoadingSpinner { HorizontalAlignment = HorizontalAlignment.Center };
        var (_, scroll) = Rig(spinner);

        Settle(200);
        Assert.DoesNotContain(SeenClass, spinner.Classes);
        Assert.Equal(0, spinner.Angle);

        ScrollIn(scroll);
        Assert.Contains(SeenClass, spinner.Classes);
        Assert.True(spinner.Angle > 0, $"angle {spinner.Angle}");

        ScrollOut(scroll, 200);
        Assert.DoesNotContain(SeenClass, spinner.Classes);
        Assert.Equal(0, spinner.Angle);
    }

    [AvaloniaFact]
    public void A_skeleton_and_an_indeterminate_bar_rest_out_of_view()
    {
        var skeleton = new Skeleton { Classes = { "block" } };
        var bar = new ProgressBar { IsIndeterminate = true, Width = 300 };
        var (_, scroll) = Rig(new StackPanel { Spacing = 8, Children = { skeleton, bar } });
        var sweep = Part<Shimmer>(bar, "IndeterminateRoot");

        Settle(200);
        Assert.DoesNotContain(SeenClass, skeleton.Classes);
        Assert.DoesNotContain(SeenClass, sweep.Classes);
        Assert.Equal(0, skeleton.Phase);
        Assert.Equal(0, sweep.Phase);

        ScrollIn(scroll);
        Assert.Contains(SeenClass, skeleton.Classes);
        Assert.Contains(SeenClass, sweep.Classes);
        Assert.True(skeleton.Phase > 0, $"skeleton {skeleton.Phase}");
        Assert.True(sweep.Phase > 0, $"sweep {sweep.Phase}");
    }

    [AvaloniaFact]
    public void A_minimised_window_stops_its_loops_and_a_restored_one_starts_them()
    {
        var spinner = new LoadingSpinner();
        var skeleton = new Skeleton { Classes = { "block" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { spinner, skeleton } });
        Settle(150);
        Assert.Contains(SeenClass, spinner.Classes);
        Assert.True(spinner.Angle > 0, $"angle {spinner.Angle}");
        Assert.True(skeleton.Phase > 0, $"phase {skeleton.Phase}");

        window.WindowState = WindowState.Minimized;
        Settle(200);
        Assert.DoesNotContain(SeenClass, spinner.Classes);
        Assert.DoesNotContain(SeenClass, skeleton.Classes);
        Assert.Equal(0, spinner.Angle);
        Assert.Equal(0, skeleton.Phase);

        window.WindowState = WindowState.Normal;
        Settle(150);
        Assert.Contains(SeenClass, spinner.Classes);
        Assert.Contains(SeenClass, skeleton.Classes);
        Assert.True(spinner.Angle > 0, $"angle {spinner.Angle}");
        Assert.True(skeleton.Phase > 0, $"phase {skeleton.Phase}");
    }

    [AvaloniaFact]
    public void Status_and_save_spinners_turn_only_in_view()
    {
        var item = new StatusItem { Content = "جارٍ العمل", IsBusy = true };
        var save = new SaveStatus { State = SaveState.Saving };
        var (_, scroll) = Rig(new StackPanel { Spacing = 8, Children = { item, save } });
        var itemSpinner = Part<LoadingSpinner>(item, "PART_Spinner");
        var saveSpinner = Part<LoadingSpinner>(save, "PART_Spinner");

        Settle(200);
        Assert.Equal(0, itemSpinner.Angle);
        Assert.Equal(0, saveSpinner.Angle);

        ScrollIn(scroll);
        Assert.True(itemSpinner.Angle > 0, $"status {itemSpinner.Angle}");
        Assert.True(saveSpinner.Angle > 0, $"save {saveSpinner.Angle}");
    }

    [AvaloniaFact]
    public void Seen_follows_hidden_parents_collapsed_sections_and_minimised_windows()
    {
        var probe = new Border { Width = 60, Height = 20 };
        var parent = new StackPanel { Children = { probe } };
        var section = new Expander { Header = "قسم", Content = new Border { Height = 40 } };
        var far = new Border { Width = 60, Height = 20 };
        var scroll = new ScrollViewer { Height = 120, Content = new StackPanel { Children = { new Border { Height = 1000 }, far } } };
        var canvas = new Canvas { Height = 30 };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { parent, section, scroll, canvas } });
        var content = Part<Border>(section, "ExpanderContent");

        var states = new List<bool>();
        using var watch = Watch(probe, states);
        Assert.True(Now(probe));
        Assert.Equal([true], states);

        parent.IsVisible = false;
        Assert.False(Now(probe));
        Assert.Equal([true, false], states);
        parent.IsVisible = true;
        Settle(100);
        Assert.True(Now(probe));
        Assert.Equal([true, false, true], states);

        Assert.False(Now(content));
        section.IsExpanded = true;
        Settle(500);
        Assert.True(Now(content));

        var farStates = new List<bool>();
        using var farWatch = Watch(far, farStates);
        Assert.False(Now(far));
        scroll.ScrollToEnd();
        Settle(150);
        Assert.True(Now(far));
        scroll.ScrollToHome();
        Settle(150);
        Assert.False(Now(far));
        Assert.Equal([false, true, false], farStates);

        window.WindowState = WindowState.Minimized;
        Assert.False(Now(probe));
        Assert.Equal([true, false, true, false], states);
        window.WindowState = WindowState.Normal;
        Settle(100);
        Assert.True(Now(probe));
        Assert.Equal([true, false, true, false, true], states);

        var late = new Border { Width = 10, Height = 10 };
        canvas.Children.Add(late);
        var lateStates = new List<bool>();
        using var lateWatch = Watch(late, lateStates);
        Settle(100);
        Assert.Equal([true], lateStates);
    }

    private static IDisposable Watch(Control c, List<bool> into) => Seen.Watch(c, into.Add);

    private static bool Now(Visual v) => Seen.Now(v);

    [AvaloniaFact]
    public void An_entrance_out_of_view_waits_and_plays_on_scroll_in()
    {
        using var clock = new HeldEntrance();
        var box = new Border { Width = 60, Height = 30, HorizontalAlignment = HorizontalAlignment.Center };
        Entrance.SetEffect(box, EntranceEffect.Rise);
        var (_, scroll) = Rig(box);

        Settle(100);
        Assert.Equal(1, box.Opacity);
        Assert.Null(box.RenderTransform);

        ScrollIn(scroll, 100);
        Assert.Equal(0, box.Opacity, 3);
        Assert.Equal(6, box.RenderTransform!.Value.M32, 3);

        clock.To(280);
        Assert.Equal(1, box.Opacity);
        Assert.Null(box.RenderTransform);
    }

    [AvaloniaFact]
    public void A_visible_entrance_keeps_its_timing()
    {
        using var clock = new HeldEntrance();
        var box = new Border { Width = 60, Height = 30 };
        Entrance.SetEffect(box, EntranceEffect.Rise);
        Show(new StackPanel { Margin = new Thickness(20), Children = { box } });

        Assert.Equal(0, box.Opacity, 3);
        Assert.Equal(6, box.RenderTransform!.Value.M32, 3);

        clock.To(140);
        var half = new SplineEasing(0.22, 1, 0.36, 1).Ease(0.5);
        Assert.Equal(half, box.Opacity, 3);
        Assert.Equal(6 * (1 - half), box.RenderTransform!.Value.M32, 3);

        clock.To(280);
        Assert.Equal(1, box.Opacity);
        Assert.Null(box.RenderTransform);
    }

    [AvaloniaFact]
    public void A_marker_lands_at_once_out_of_view()
    {
        var seg = new Segmented { AutoScrollToSelectedItem = false, Items = { new SegmentedItem { Content = "اليوم" }, new SegmentedItem { Content = "الأسبوع" }, new SegmentedItem { Content = "الشهر" } } };
        var (_, _) = Rig(seg);
        Assert.False(Seen.Now(seg));
        var real = MarkerFlight.Now;
        var at = real();
        MarkerFlight.Now = () => at;
        try
        {
            seg.SelectedIndex = 2;
            Settle(100);
            Assert.False(Part<Border>(seg, "PART_Flight").IsVisible);
            Assert.DoesNotContain("moving", seg.Classes);
            Assert.Equal(2, seg.SelectedIndex);
        }
        finally { MarkerFlight.Now = real; }
    }

    [AvaloniaFact]
    public void A_minimised_window_gets_the_theme_without_a_sweep()
    {
        try
        {
            var origin = new Button { Content = "الوضع" };
            var window = Show(new Panel { Children = { origin } }, width: 800, height: 500);
            window.WindowState = WindowState.Minimized;
            ThemeSweep.Run(origin, () => Application.Current!.RequestedThemeVariant = ThemeVariant.Dark);
            Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
            Assert.Null(window.GetVisualDescendants().OfType<Image>().FirstOrDefault(i => i.Name == "PART_ThemeSweep"));
        }
        finally { Application.Current!.RequestedThemeVariant = ThemeVariant.Light; }
    }

    [AvaloniaFact]
    public void A_chart_draws_in_when_it_scrolls_into_view()
    {
        var chart = new Chart { Type = ChartType.Bar, Labels = "أ,ب,ج", Width = 400 };
        chart.Series.Add(new ChartSeries("س", 10, 20, 15));
        var (_, scroll) = Rig(chart);
        var plot = chart.GetVisualDescendants().OfType<ChartPlot>().Single();

        Settle(100);
        Assert.False(plot.IsDrawing);

        ScrollIn(scroll, 100);
        Assert.True(plot.IsDrawing);
    }

    [AvaloniaFact]
    public void The_bell_does_not_ring_unseen()
    {
        var notify = new Notify();
        notify.Items.Add(new NotifyItem { Title = "اكتمل التصدير", Description = "وصف", End = "الآن" });
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } });
        var bell = Assert.IsAssignableFrom<Control>(Part<Button>(notify, "PART_Bell").Content);

        window.WindowState = WindowState.Minimized;
        Settle(100);
        notify.Items.Insert(0, new NotifyItem { Title = "إشعار جديد", IsUnread = true });
        Settle(250);
        Assert.Equal(1, notify.UnreadCount);
        Assert.Null(bell.RenderTransform);

        window.WindowState = WindowState.Normal;
        Settle(100);
        notify.Items.Insert(0, new NotifyItem { Title = "إشعار آخر", IsUnread = true });
        Tick(150);
        Assert.NotNull(bell.RenderTransform);
    }

    [AvaloniaFact]
    public void A_progress_value_set_out_of_view_jumps()
    {
        var bar = new ProgressBar { Value = 20, Width = 400, HorizontalAlignment = HorizontalAlignment.Center };
        var (_, scroll) = Rig(bar);
        var indicator = Part<Border>(bar, "PART_Indicator");

        bar.Value = 70;
        Tick(60);
        Assert.Equal(280, indicator.Bounds.Width, 0);

        ScrollIn(scroll);
        bar.Value = 20;
        Tick(60);
        Assert.InRange(indicator.Bounds.Width, 81, 279);
        Settle();
        Assert.Equal(80, indicator.Bounds.Width, 0);
    }
}
