using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Pull to refresh in the Insiyab look: the brand spinner on a small glass disc that comes down with the pull and leaves when the app is done.
public class PullRefreshTests
{
    private static (RefreshContainer Container, ScrollViewer Scroller) Make()
    {
        var rows = new StackPanel();
        for (var i = 0; i < 40; i++) rows.Children.Add(new Border { Height = 44, Child = new TextBlock { Text = "File " + i } });
        var scroller = new ScrollViewer { Content = rows };
        return (new RefreshContainer { Content = scroller }, scroller);
    }

    private static List<BoxShadow> Each(BoxShadows shadows)
    {
        var all = new List<BoxShadow>();
        for (var i = 0; i < shadows.Count; i++) all.Add(shadows[i]);
        return all;
    }

    private static RefreshDisc Disc(RefreshContainer c) => c.Visualizer!.GetVisualDescendants().OfType<RefreshDisc>().Single();

    // Where the disc's centre is in the window, and the list's own composition offset (the pull never moves the list: the disc slides over it).
    private static Point DiscCenter(RefreshContainer c, Window w) => Center(Disc(c), w);

    private static double ListY(ScrollViewer s) => ElementComposition.GetElementVisual((Visual)s.Content!)!.Offset.Y;

    // What a reading gives frame by frame for that long (real time, as the headless clock is).
    private static List<T> Sample<T>(Window window, int milliseconds, Func<T> read)
    {
        var samples = new List<T>();
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

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_pull_brings_the_disc_down_raises_refresh_and_the_disc_leaves_when_the_deferral_completes(string dir)
    {
        var (container, scroller) = Make();
        var requests = 0;
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) =>
        {
            requests++;
            deferral = e.GetDeferral();
        };
        var w = Show(container, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 360, height: 600);
        var disc = Disc(container);
        var arc = Assert.IsType<RefreshArc>(disc.Child);
        Assert.IsAssignableFrom<LoadingSpinner>(arc);
        Assert.Equal(0, disc.Opacity);
        Assert.Equal(180, Center(disc, w).X, 1);
        Assert.Equal(FlowDirection.LeftToRight, arc.FlowDirection);
        var listY = ListY(scroller);

        var from = new Point(180, 120);
        var t = w.TouchBegin(from);
        Settle(40);
        for (var k = 1; k <= 6; k++)
        {
            w.TouchMove(t, from + new Vector(0, 10 * k));
            Settle(16);
        }
        // 50 of finger past the slop is a pull of 25: the circle's top at −40 + 25, its lower part out from under the edge
        Assert.Equal(RefreshVisualizerState.Interacting, disc.State);
        Assert.Equal(1, disc.Opacity);
        Assert.Equal(-15, disc.Top, 1);
        Assert.Equal(-15 + 20, DiscCenter(container, w).Y, 1);
        Assert.Equal(0, arc.ArcLength, 1);
        // SwipeRefreshLayout's trigger is a pull of 64, a finger's 128 past the slop: 130 is short of it, 140 past it
        for (var k = 7; k <= 13; k++)
        {
            w.TouchMove(t, from + new Vector(0, 10 * k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Interacting, disc.State);
        Assert.True(arc.ArcLength > 0 && arc.ArrowScale > 0, "the ring and its arrow grow with the pull");
        var shortArc = arc.ArcLength;
        Assert.Equal(0.3, arc.Opacity, 2);
        w.TouchMove(t, from + new Vector(0, 150));
        Settle(400);
        Assert.Equal(RefreshVisualizerState.Pending, disc.State);
        Assert.True(arc.ArcLength > shortArc, "the ring grows with the pull");
        Assert.Equal(1, arc.Opacity, 2);
        Assert.Equal(listY, ListY(scroller), 1);

        w.TouchEnd(t, from + new Vector(0, 150));
        Settle(400);
        Assert.Equal(1, requests);
        Assert.Equal(RefreshVisualizerState.Refreshing, disc.State);
        Assert.Equal(24, disc.Top, 1);
        Assert.Equal(0, arc.ArrowScale);
        var a1 = arc.Angle;
        Settle(200);
        Assert.NotEqual(a1, arc.Angle);
        // CircularProgressDrawable's run: the arc between a dot (0.01 of the ring) and 0.8 of it, never the whole ring
        Assert.InRange(arc.ArcLength, 3.5, 0.8 * 360 + 0.1);
        Assert.Equal(180, Center(disc, w).X, 1);
        Assert.Equal(listY, ListY(scroller), 1);

        deferral!.Complete();
        Settle(400);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        Assert.Equal(0, disc.Opacity);
        Assert.Equal(1, disc.Scale);
        Assert.Equal(-40, disc.Top, 1);
        Assert.Equal(listY, ListY(scroller), 1);
    }

    // AndroidX SwipeRefreshLayout.moveSpinner, value for value: the circle follows the pull up to the trigger, then the slingshot's tension slows it and stops it 64 lower.
    [Fact]
    public void The_disc_follows_the_pull_as_android_swipe_refresh_does()
    {
        Assert.Equal(-40, RefreshDisc.Spinner(0).Top);
        Assert.Equal(-8, RefreshDisc.Spinner(32).Top);
        Assert.Equal(24, RefreshDisc.Spinner(64).Top, 6);
        Assert.Equal(88, RefreshDisc.Spinner(64 + 128).Top, 6);
        Assert.Equal(88, RefreshDisc.Spinner(1000).Top, 6);
        // the ring shows from 40% of the trigger, reaching 0.8 of a turn and its whole arrow there
        Assert.Equal(0, RefreshDisc.Spinner(25).Trim);
        Assert.Equal(0.8, RefreshDisc.Spinner(64).Trim, 6);
        Assert.Equal(1, RefreshDisc.Spinner(64).Arrow, 6);
        Assert.Equal(-0.125, RefreshDisc.Spinner(0).Rotation, 6);
        var last = RefreshDisc.Spinner(0).Top;
        var lastStep = 1.0;
        for (var i = 1; i <= 192; i++)
        {
            var now = RefreshDisc.Spinner(i).Top;
            Assert.True(now >= last, "it never steps back");
            var step = now - last;
            Assert.True(step <= 1.0 + 1e-9, "and never runs ahead of the pull");
            if (i > 64) Assert.True(step <= lastStep + 1e-9, "past the trigger it only slows");
            last = now;
            lastStep = step;
        }
    }

    [AvaloniaFact]
    public void A_short_pull_slides_back_without_a_refresh_and_never_moves_the_list()
    {
        var (container, scroller) = Make();
        var requests = 0;
        container.RefreshRequested += (_, e) => requests++;
        var w = Show(container, width: 360, height: 600);
        var disc = Disc(container);
        var listY = ListY(scroller);
        var from = new Point(180, 120);
        var t = w.TouchBegin(from);
        Settle(40);
        for (var k = 10; k <= 110; k += 10)
        {
            w.TouchMove(t, from + new Vector(0, k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Interacting, disc.State);
        Assert.Equal(-40 + 50, disc.Top, 1);
        Assert.Equal(listY, ListY(scroller), 1);
        w.TouchEnd(t, from + new Vector(0, 110));
        Settle(60);
        Assert.True(disc.Top < 10 && disc.Top > -40, $"it slides back ({disc.Top})");
        Settle(400);
        Assert.Equal(0, requests);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        Assert.Equal(-40, disc.Top, 1);
        Assert.Equal(0, disc.Opacity);
        Assert.Equal(listY, ListY(scroller), 1);
    }

    // Under a top bar the list scrolls under (Shell.FrostsTopbar): the disc comes out from the bar's foot, is cut there, and spins below it, over the list.
    [AvaloniaFact]
    public void Under_a_clear_top_bar_the_disc_comes_out_below_the_bar_and_spins_there()
    {
        var (container, scroller) = Make();
        scroller.Padding = new Thickness(0, 72, 0, 0);
        Shell.SetFrostsTopbar(scroller, true);
        var bar = new Border { Classes = { "topbar" }, Height = 72, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top, ZIndex = 1 };
        var page = new Panel { Children = { container, bar } };
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) => deferral = e.GetDeferral();
        var w = Show(page, width: 360, height: 600);
        var disc = Disc(container);
        var from = new Point(180, 200);
        var t = w.TouchBegin(from);
        Settle(40);
        for (var k = 1; k <= 6; k++)
        {
            w.TouchMove(t, from + new Vector(0, 10 * k));
            Settle(16);
        }
        Assert.Equal(72 - 15 + 20, DiscCenter(container, w).Y, 1);
        var clip = Assert.IsType<RectangleGeometry>(container.Visualizer!.Clip);
        Assert.Equal(72, container.Visualizer.TranslatePoint(clip.Rect.TopLeft, container)!.Value.Y, 1);
        for (var k = 7; k <= 16; k++)
        {
            w.TouchMove(t, from + new Vector(0, 10 * k));
            Settle(16);
        }
        w.TouchEnd(t, from + new Vector(0, 160));
        Settle(400);
        Assert.Equal(RefreshVisualizerState.Refreshing, disc.State);
        Assert.Equal(72 + 24 + 20, DiscCenter(container, w).Y, 1);
        deferral!.Complete();
        Settle(400);
        Assert.Equal(0, disc.Opacity);
    }

    [AvaloniaFact]
    public void The_disc_is_glass_with_the_brand_ring_and_simpler_under_light_effects()
    {
        var (container, _) = Make();
        var w = Show(container, width: 360, height: 600);
        var disc = Disc(container);
        Assert.Equal(new Size(40, 40), disc.Bounds.Size);
        Assert.Equal(new CornerRadius(20), disc.CornerRadius);
        AssertBrush(disc, "InsPrimaryMid", () => ((RefreshArc)disc.Child!).Arc);
        Assert.Equal(1, disc.ZIndex);
        var full = disc.BoxShadow;
        Assert.Contains(Each(full), s => s.Blur > 0);
        try
        {
            InsiyabTheme.Effects = EffectsLevel.Light;
            Settle(100);
            Assert.All(Each(disc.BoxShadow), s => Assert.True(s.Blur <= 12));
            Motion.SetStill(w, true);
            Settle(100);
            Assert.All(Each(disc.BoxShadow), s => Assert.True(s.Blur == 0 || s.IsInset));
        }
        finally { InsiyabTheme.Effects = EffectsLevel.Full; }
    }

    [AvaloniaFact]
    public void F5_refreshes_from_the_keyboard_and_a_still_window_draws_the_spinner_at_once()
    {
        var (container, scroller) = Make();
        var requests = 0;
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) =>
        {
            requests++;
            deferral = e.GetDeferral();
        };
        var field = new TextBox();
        ((StackPanel)scroller.Content!).Children.Insert(0, field);
        var w = Show(container, width: 360, height: 600);
        Motion.SetStill(w, true);
        field.Focus();
        w.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
        Settle(300);
        Assert.Equal(1, requests);
        var disc = Disc(container);
        Assert.Equal(RefreshVisualizerState.Refreshing, disc.State);
        Assert.InRange(((RefreshArc)disc.Child!).ArcLength, 3.5, 0.8 * 360 + 0.1);
        deferral!.Complete();
        Settle(200);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
    }

    // The app's progress turns the refresh's spinning ring into one that fills (RefreshProgress); in a still window: a short arc before anything is done, its share after, each at once, and still turning (a ring standing still reads as a hang), kept when the progress is cleared, and a spinning ring again on the next refresh.
    [AvaloniaFact]
    public void A_progress_fills_the_ring_of_a_refresh_under_way()
    {
        var (container, _) = Make();
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) => deferral = e.GetDeferral();
        var w = Show(container, width: 360, height: 600);
        Motion.SetStill(w, true);
        RefreshProgress.SetValue(container, 0);
        container.RequestRefresh();
        Settle(300);
        var disc = Disc(container);
        var arc = (RefreshArc)disc.Child!;
        Assert.Equal(RefreshVisualizerState.Refreshing, disc.State);
        Assert.InRange(arc.ArcLength, TurningFill.MinShare * 360 - 0.1, TurningFill.MinShare * 360 + 0.1);
        Assert.Equal(0, arc.ArrowScale);
        Assert.True(Sample(w, 300, () => arc.Angle).Distinct().Count() > 3, "the short arc turns in a still window too");
        RefreshProgress.SetValue(container, 0.4);
        Settle(30);
        Assert.InRange(arc.ArcLength, 0.4 * 360 - 0.1, 0.4 * 360 + 0.1);                 // at once, no ease
        Assert.True(Sample(w, 300, () => arc.Angle).Distinct().Count() > 3, "it turns on at its share");
        RefreshProgress.SetValue(container, double.NaN);
        Settle(100);
        Assert.InRange(arc.ArcLength, 0.4 * 360 - 0.1, 0.4 * 360 + 0.1);
        deferral!.Complete();
        Settle(300);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);

        // no progress: the turning ring, as before
        container.RequestRefresh();
        Settle(300);
        Assert.InRange(arc.ArcLength, 3.5, 0.8 * 360 + 0.1);
        deferral!.Complete();
    }

    // A refresh whose parts are never counted (everything was read a moment ago): the pull's spinning ring takes the app's 0 when its run starts, turns as a short arc whatever the count, and completes before it leaves, also when the run is over within a few frames.
    [AvaloniaFact]
    public void A_refresh_with_no_counted_parts_turns_and_completes()
    {
        Motion.SystemOverride = false;
        try
        {
            var (container, _) = Make();
            RefreshCompletionDeferral? deferral = null;
            container.RefreshRequested += (_, e) => deferral = e.GetDeferral();
            var w = Show(container, width: 360, height: 600);
            container.RequestRefresh(); // no value yet: the spinning ring
            Settle(400);
            var disc = Disc(container);
            var arc = (RefreshArc)disc.Child!;
            RefreshProgress.SetValue(container, 0); // the run started: nothing counted, nothing done
            var turns = Sample(w, 500, () => (arc.Angle, arc.ArcLength));
            Assert.True(turns.Select(s => s.Angle).Distinct().Count() > 5, "the ring turns with no part counted");
            Assert.InRange(turns[^1].ArcLength, TurningFill.MinShare * 360 - 0.5, TurningFill.MinShare * 360 + 0.5);
            // the run is over at once: the ring completes, turning, then the disc leaves
            RefreshProgress.SetValue(container, double.NaN);
            deferral!.Complete();
            var end = Sample(w, 900, () => (arc.ArcLength, disc.Scale, arc.Angle));
            var full = end.FindIndex(s => s.ArcLength > 359);
            Assert.True(full >= 0, "the ring completes");
            Assert.True(end.Take(full).Select(s => s.Angle).Distinct().Count() > 3, "it turns while it completes");
            Assert.True(end.FindIndex(s => s.Scale < 0.99) > full, "the disc shrinks only once the ring is complete");
            Settle(100);
            Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        }
        finally
        {
            Motion.SystemOverride = null;
        }
    }

    // The filling ring's own clock (TurningFill), frame by frame: a short arc at 0, an eased growth over 300 ms, a steady turn, and no jump after frames stopped.
    [Fact]
    public void A_turning_fill_eases_its_arc_and_turns_at_a_steady_pace()
    {
        var fill = new TurningFill();
        var at = TimeSpan.FromSeconds(10);
        fill.Aim(0);
        fill.Step(at);
        Assert.Equal(TurningFill.MinShare, fill.Length, 6);
        fill.Step(at += TimeSpan.FromMilliseconds(500));
        Assert.Equal(TurningFill.MinShare, fill.Length, 6);
        Assert.Equal(500 * TurningFill.TurnsPerMs, fill.Turn, 6);
        // about a turn in 1.6 s, as CircularProgressDrawable turns its ring
        Assert.InRange(1 / TurningFill.TurnsPerMs, 1500, 1800);
        fill.Aim(0.6);
        fill.Step(at);
        Assert.Equal(TurningFill.MinShare, fill.Length, 6);
        fill.Step(at += TimeSpan.FromMilliseconds(100));
        var early = fill.Length;
        fill.Step(at += TimeSpan.FromMilliseconds(100));
        Assert.InRange(early, TurningFill.MinShare + 0.01, fill.Length - 0.01);
        Assert.True(fill.IsEasing);
        fill.Step(at += TimeSpan.FromMilliseconds(100));
        Assert.Equal(0.6, fill.Length, 6);
        Assert.False(fill.IsEasing);
        // a new share mid-way eases on from where the arc is
        fill.Aim(0.9);
        fill.Step(at += TimeSpan.FromMilliseconds(16));
        Assert.Equal(0.6, fill.Length, 6);
        var turn = fill.Turn;
        fill.Pause();
        fill.Step(at += TimeSpan.FromSeconds(5));
        Assert.Equal(turn, fill.Turn, 6);
        // a still window: the share at once, from where it is told
        fill.Settle(-0.25);
        Assert.Equal(0.9, fill.Length, 6);
        Assert.Equal(-0.25, fill.Turn, 6);
    }

    // With motion the filling ring never stands still, as the Play Store's install ring: it turns all the time, a short arc before anything is done, its arc easing to each new share; when the refresh ends short of full it completes first, still turning, and only then shrinks away.
    [AvaloniaFact]
    public void A_progress_ring_keeps_turning_eases_to_each_share_and_completes_before_it_leaves()
    {
        Motion.SystemOverride = false;
        try
        {
            var (container, _) = Make();
            RefreshCompletionDeferral? deferral = null;
            container.RefreshRequested += (_, e) => deferral = e.GetDeferral();
            var w = Show(container, width: 360, height: 600);
            RefreshProgress.SetValue(container, 0);
            container.RequestRefresh();
            Settle(700);
            var disc = Disc(container);
            var arc = (RefreshArc)disc.Child!;
            Assert.Equal(RefreshVisualizerState.Refreshing, disc.State);
            Assert.InRange(arc.ArcLength, TurningFill.MinShare * 360 - 0.5, TurningFill.MinShare * 360 + 0.5);
            var turns = Sample(w, 300, () => arc.Angle);
            Assert.True(turns.Distinct().Count() > 3, "the short arc turns before anything is done");
            Assert.True(Sample(w, 100, () => arc.ArcLength).All(l => Math.Abs(l - TurningFill.MinShare * 360) < 0.5), "its length holds while it turns");

            RefreshProgress.SetValue(container, 0.5);
            var lengths = Sample(w, 700, () => arc.ArcLength);
            Assert.Contains(lengths, l => l > TurningFill.MinShare * 360 + 5 && l < 175);
            Assert.True(lengths.Zip(lengths.Skip(1)).All(p => p.Second >= p.First - 1e-6), "the arc only grows to a higher share");
            Assert.Equal(180, lengths[^1], 0.5);
            turns = Sample(w, 300, () => arc.Angle);
            Assert.True(turns.Distinct().Count() > 3, "the ring turns on between shares");

            // the app clears its progress as the refresh ends: the ring fills to the whole, then the disc shrinks and leaves
            RefreshProgress.SetValue(container, double.NaN);
            deferral!.Complete();
            var end = Sample(w, 900, () => (arc.ArcLength, disc.Scale));
            var full = end.FindIndex(s => s.ArcLength > 359);
            var shrinking = end.FindIndex(s => s.Scale < 0.99);
            Assert.True(full >= 0, "the ring completes");
            Assert.True(shrinking > full, "the disc shrinks only once the ring is complete");
            Settle(100);
            Assert.Equal(RefreshVisualizerState.Idle, disc.State);
            Assert.Equal(0, disc.Opacity);
            Assert.Equal(1, disc.Scale);
        }
        finally
        {
            Motion.SystemOverride = null;
        }
    }

    // A stroke that lands scrolled and reaches the top on the way, or moves mostly across, never pulls: a pull starts only at the top, past a small dead zone.
    [AvaloniaFact]
    public void A_scroll_that_reaches_the_top_mid_stroke_or_a_sideways_move_never_pulls()
    {
        var (container, scroller) = Make();
        var w = Show(container, FlowDirection.RightToLeft, width: 360, height: 600);
        var disc = Disc(container);

        scroller.Offset = new Vector(0, 60);
        Settle(40);
        var from = new Point(180, 120);
        var t = w.TouchBegin(from);
        Settle(40);
        for (var k = 1; k <= 16; k++)
        {
            w.TouchMove(t, from + new Vector(0, 12 * k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        w.TouchEnd(t, from + new Vector(0, 192));
        Settle(200);

        scroller.Offset = default;
        Settle(40);
        t = w.TouchBegin(from);
        Settle(40);
        for (var k = 1; k <= 10; k++)
        {
            w.TouchMove(t, from + new Vector(16 * k, 6 * k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        w.TouchEnd(t, from + new Vector(160, 60));
        Settle(200);

        // a tiny move at the top is not a pull either; a clear downward one still is
        t = w.TouchBegin(from);
        Settle(40);
        w.TouchMove(t, from + new Vector(0, 6));
        Settle(40);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        for (var k = 1; k <= 6; k++)
        {
            w.TouchMove(t, from + new Vector(0, 6 + 10 * k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Interacting, disc.State);
        w.TouchEnd(t, from + new Vector(0, 66));
        Settle(400);
    }

    // A wheel or a touchpad never pulls: scrolled up at the top it does nothing, and a stroke right after it does not pull either.
    [AvaloniaFact]
    public void A_wheel_at_the_top_never_refreshes_nor_lets_a_stroke_right_after_it_pull()
    {
        var (container, _) = Make();
        var requests = 0;
        container.RefreshRequested += (_, _) => requests++;
        var w = Show(container, width: 360, height: 600);
        var disc = Disc(container);
        var from = new Point(180, 120);
        for (var k = 0; k < 6; k++)
        {
            w.MouseWheel(from, new Vector(0, 3), RawInputModifiers.None);
            Settle(16);
        }
        Settle(200);
        Assert.Equal(0, requests);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        var t = w.TouchBegin(from);
        Settle(16);
        for (var k = 1; k <= 24; k++)
        {
            w.TouchMove(t, from + new Vector(0, 10 * k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        w.TouchEnd(t, from + new Vector(0, 240));
        Settle(400);
        Assert.Equal(0, requests);
    }

    // Feeds a stroke with its own pace: each step waits its interval (real time, as the headless clock is) and moves by its own amount; the headless input takes its time from the real clock, so the wait spins rather than running the dispatcher's jobs (which can take longer than a step).
    private static void Stroke(Window w, Point from, IEnumerable<(int Ms, Vector By)> steps, bool lift = true)
    {
        var t = w.TouchBegin(from);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var due = 0L;
        var at = from;
        foreach (var (ms, by) in steps)
        {
            due += ms;
            while (clock.ElapsedMilliseconds < due) Thread.SpinWait(50);
            at += by;
            w.TouchMove(t, at);
        }
        if (lift) w.TouchEnd(t, at);
        Settle(400);
    }

    // A real finger: it starts fast (a flick-like first frames), slows, wobbles across by fractions of a pixel, at uneven frame times.
    private static IEnumerable<(int Ms, Vector By)> Finger(double total, int seed)
    {
        var random = new Random(seed);
        var gone = 0.0;
        var k = 0;
        while (gone < total)
        {
            var ms = 6 + random.Next(0, 7);
            var pace = k < 4 ? 3.5 + random.NextDouble() : Math.Max(0.4, 2.6 - k * 0.12) * (0.8 + 0.4 * random.NextDouble());
            var by = Math.Min(total - gone, pace * ms);
            gone += by;
            k++;
            yield return (ms, new Vector((random.NextDouble() - 0.5) * 0.9, by));
        }
    }

    // A finger's pull refreshes however fast it starts: Android's pull has no speed rule (touch wins over the emulator's wheel).
    [AvaloniaFact]
    public void A_real_finger_pull_refreshes_every_time_however_fast_it_starts()
    {
        var (container, _) = Make();
        var requests = 0;
        container.RefreshRequested += (_, _) => requests++;
        var w = Show(container, width: 360, height: 700);
        for (var seed = 1; seed <= 6; seed++)
        {
            var before = requests;
            Stroke(w, new Point(180, 100), Finger(200, seed));
            Assert.Equal(before + 1, requests);
            Settle(300);
        }
        // a quick flick of the same length too, and a slow careful one
        Stroke(w, new Point(180, 100), Enumerable.Range(0, 8).Select(i => (8, new Vector(0.3 * (i % 2), 25.0))));
        Assert.Equal(7, requests);
        Stroke(w, new Point(180, 100), Enumerable.Range(0, 30).Select(i => (16, new Vector(i % 3 == 0 ? 0.4 : 0, 5.0))));
        Assert.Equal(8, requests);
    }

    [AvaloniaFact]
    public void The_android_emulators_wheel_never_refreshes()
    {
        // the emulator's wheel (seen with getevent): a touch at the cursor that runs 8% of the screen every 15 ms, dead straight, then lifts
        var (container, _) = Make();
        var requests = 0;
        container.RefreshRequested += (_, _) => requests++;
        var w = Show(container, width: 360, height: 600);
        var disc = Disc(container);
        // a first stroke readies the code paths, so the timed one keeps its pace
        Stroke(w, new Point(180, 60), Finger(60, 1));
        Stroke(w, new Point(180, 60), Enumerable.Range(0, 8).Select(_ => (15, new Vector(0, 48.0))));
        Assert.Equal(0, requests);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
    }

    [Fact]
    public void Only_the_emulators_steady_straight_stroke_is_told_from_a_finger()
    {
        static List<(long, double, double)> Steps(params (long At, double Along, double Across)[] s) => s.ToList();
        // the wheel: steady 3.2 dp/ms from the first step, nothing across; also when two of its steps come in one frame
        Assert.True(EdgePull.IsMachineStroke(Steps((0, 0, 0), (15, 48, 0), (30, 96, 0), (45, 144, 0))));
        Assert.True(EdgePull.IsMachineStroke(Steps((0, 0, 0), (15, 48, 0), (45, 144, 0), (60, 192, 0))));
        // a finger: a hair across, or a pace that changes, or slow, or too few steps to tell
        Assert.False(EdgePull.IsMachineStroke(Steps((0, 0, 0), (15, 48, 0.2), (30, 96, 0), (45, 144, 0))));
        Assert.False(EdgePull.IsMachineStroke(Steps((0, 0, 0), (15, 60, 0), (30, 96, 0), (45, 120, 0))));
        Assert.False(EdgePull.IsMachineStroke(Steps((0, 0, 0), (16, 20, 0), (32, 40, 0), (48, 60, 0))));
        Assert.False(EdgePull.IsMachineStroke(Steps((0, 0, 0), (15, 48, 0), (30, 96, 0))));
    }
}
