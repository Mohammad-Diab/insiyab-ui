using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ButtonMotionTests
{
    // The B1 switch that turns motion off; if B1 named its setter differently, this is the one line to change.
    private static void Still(Window w, bool on) => Motion.SetStill(w, on);

    // Pumps frames for a while and records a reading each frame, so a motion can be checked mid-way.
    private static List<(long Ms, double Value)> Sample(Window window, int milliseconds, Func<double> read)
    {
        var samples = new List<(long, double)>();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            samples.Add((clock.ElapsedMilliseconds, read()));
            Thread.Sleep(6);
        }
        return samples;
    }

    private static double ScaleOf(Control part) => part.RenderTransform?.Value.M11 ?? 1;

    private static ControlTheme Theme(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, ThemeVariant.Light, out var theme));
        return (ControlTheme)theme!;
    }

    // The clock stands still, so the click counts as a tap however slow the run, and the depth is read from the dip's keyframes rather than from frames that load can skip.
    private static double QuickClickDip(Button button, Window window)
    {
        var part = Part<Panel>(button, "PART_Press");
        var p = Center(button, window);
        window.MouseMove(p);
        Settle(300);
        var real = PressDip.Now;
        var at = real();
        PressDip.Now = () => at;
        var dips = new List<(Control Part, Animation Run)>();
        PressDip.Started = (c, a) => dips.Add((c, a));
        try
        {
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
        }
        finally
        {
            PressDip.Now = real;
            PressDip.Started = null;
        }
        var samples = Sample(window, 400, () => ScaleOf(part));
        Assert.Equal(1, samples[^1].Value, 3);
        if (dips.Count == 0) return 1;
        var (dipped, run) = Assert.Single(dips);
        Assert.Same(part, dipped);
        var scales = run.Children.Select(k => ((TransformOperations)k.Setters.OfType<Setter>().Single(s => s.Property == Visual.RenderTransformProperty).Value!).Value.M11).ToList();
        Assert.Equal(1, scales[0], 3);
        Assert.Equal(1, scales[^1], 3);
        return scales.Min();
    }

    [AvaloniaFact]
    public void A_quick_click_dips_the_button_once()
    {
        var button = new Button { Classes = { "primary" }, Content = "حفظ" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var lowest = QuickClickDip(button, window);
        Assert.InRange(lowest, 0.965, 0.978);
    }

    [AvaloniaFact]
    public void The_dip_reaches_its_depth_early_and_is_over_by_240ms()
    {
        var button = new Button { Classes = { "secondary" }, Content = "حفظ" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var part = Part<Panel>(button, "PART_Press");
        var p = Center(button, window);
        window.MouseMove(p);
        Settle(300);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        var samples = Sample(window, 400, () => ScaleOf(part));
        var bottom = samples.First(s => s.Value == samples.Min(m => m.Value));
        Assert.InRange(bottom.Ms, 10, 110);
        Assert.All(samples.Where(s => s.Ms > 300), s => Assert.Equal(1, s.Value, 3));
    }

    [AvaloniaFact]
    public void A_held_press_does_not_dip_on_release()
    {
        var button = new Button { Classes = { "primary" }, Content = "حفظ" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var part = Part<Panel>(button, "PART_Press");
        var p = Center(button, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        Settle(260);
        window.MouseUp(p, MouseButton.Left);
        var samples = Sample(window, 300, () => ScaleOf(part));
        Assert.All(samples, s => Assert.Equal(1, s.Value, 3));
    }

    [AvaloniaFact]
    public void Bare_and_close_buttons_never_dip()
    {
        var bare = new Button { Classes = { "bare" }, Content = "إلغاء" };
        var close = new Button { Classes = { "close" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { bare, close } });
        Assert.Equal(1, QuickClickDip(bare, window), 3);
        Assert.Equal(1, QuickClickDip(close, window), 3);
    }

    [AvaloniaFact]
    public void The_dip_sits_on_top_of_the_lift()
    {
        var button = new Button { Classes = { "primary", "lift" }, Content = "ابدأ" };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        var root = Part<Panel>(button, "PART_Root");
        var lowest = QuickClickDip(button, window);
        Assert.InRange(lowest, 0.965, 0.978);
        Assert.Equal(-2, root.RenderTransform!.Value.M32, 1);
    }

    [AvaloniaFact]
    public void Space_pressed_quickly_dips_too()
    {
        var button = new Button { Classes = { "primary" }, Content = "حفظ" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var part = Part<Panel>(button, "PART_Press");
        button.Focus();
        Settle(100);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        var samples = Sample(window, 400, () => ScaleOf(part));
        Assert.InRange(samples.Min(s => s.Value), 0.965, 0.978);
    }

    [AvaloniaFact]
    public void Pills_and_islands_dip_to_their_own_depth()
    {
        var pill = new Button { Theme = Theme("InsPillButton"), Content = "مفتوحة" };
        var island = new Button { Theme = Theme("InsIslandButton"), Content = "الإشعارات" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 16, Children = { pill, island } });
        Assert.InRange(QuickClickDip(pill, window), 0.955, 0.968);
        Assert.InRange(QuickClickDip(island, window), 0.965, 0.978);
    }

    [AvaloniaFact]
    public void A_still_window_gets_no_dip()
    {
        var button = new Button { Classes = { "primary" }, Content = "حفظ" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { button } });
        Still(window, true);
        Assert.Equal(1, QuickClickDip(button, window), 3);
    }

    [AvaloniaFact]
    public void Segmented_face_stretches_slowly_first_then_follows()
    {
        var seg = new Segmented { Items = { new SegmentedItem { Content = "اليوم" }, new SegmentedItem { Content = "الأسبوع" }, new SegmentedItem { Content = "الشهر" } } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { seg } });
        var first = (SegmentedItem)seg.Items[0]!;
        var third = (SegmentedItem)seg.Items[2]!;
        var flight = Part<Border>(seg, "PART_Flight");
        var start = first.Bounds.Width;
        var span = third.Bounds.Right - first.Bounds.Left;
        seg.SelectedIndex = 2;
        var samples = Sample(window, 700, () => flight.IsVisible ? flight.Width : double.NaN);
        var early = samples.Where(s => s.Ms <= 80 && !double.IsNaN(s.Value)).ToList();
        Assert.NotEmpty(early);
        Assert.All(early, s => Assert.True(s.Value - start < 0.3 * (span - start), $"at {s.Ms}ms the face is {s.Value} wide already"));
        var widest = samples.Where(s => !double.IsNaN(s.Value)).MaxBy(s => s.Value);
        Assert.InRange(widest.Ms, 100, 320);
        Assert.InRange(widest.Value, start + 0.3 * (span - start), span + 0.5);
        Assert.False(flight.IsVisible);
    }

    [AvaloniaFact]
    public void Segmented_does_not_fly_in_a_still_window()
    {
        var seg = new Segmented { Items = { new SegmentedItem { Content = "اليوم" }, new SegmentedItem { Content = "الأسبوع" } } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { seg } });
        Still(window, true);
        seg.SelectedIndex = 1;
        Assert.False(Part<Border>(seg, "PART_Flight").IsVisible);
        Assert.DoesNotContain("moving", seg.Classes);
    }

    private static (Grid Grid, ScrollViewer Scroller, Toc Toc, List<TocLink> Links) MakeToc()
    {
        var column = new StackPanel();
        var toc = new Toc { Title = "المحتويات" };
        var links = new List<TocLink>();
        foreach (var name in new[] { "القسم الأول", "القسم الثاني", "القسم الثالث", "القسم الرابع" })
        {
            var section = new Border { MinHeight = 208, Child = new TextBlock { Text = name } };
            column.Children.Add(section);
            var link = new TocLink { Content = name, Section = section };
            links.Add(link);
            toc.Items.Add(link);
        }
        var scroller = new ScrollViewer { Height = 256, Content = column };
        var grid = new Grid { Margin = new Thickness(24), ColumnDefinitions = new ColumnDefinitions("*,20,176"), VerticalAlignment = VerticalAlignment.Top };
        grid.Children.Add(scroller);
        Grid.SetColumn(toc, 2);
        grid.Children.Add(toc);
        return (grid, scroller, toc, links);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Toc_marker_stretches_over_both_links_then_lands(string dir)
    {
        var x = MakeToc();
        var window = Show(x.Grid, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, height: 520);
        var marker = Part<Border>(x.Toc, "PART_Marker");
        Settle(400);
        var rest = marker.Height;
        var target = x.Links[2];
        var p = Center(target, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        var samples = Sample(window, 800, () => marker.Height);
        var span = x.Links[2].Bounds.Bottom - 7 - (x.Links[0].Bounds.Top + 7);
        Assert.All(samples.Where(s => s.Ms <= 80), s => Assert.True(s.Value - rest < 0.3 * (span - rest), $"at {s.Ms}ms the marker is {s.Value} tall already"));
        var tallest = samples.MaxBy(s => s.Value);
        Assert.InRange(tallest.Ms, 100, 320);
        Assert.InRange(tallest.Value, rest + 0.3 * (span - rest), span + 0.5);
        Assert.Equal(rest, samples[^1].Value, 1);
        var y = target.TranslatePoint(default, marker.Parent as Visual ?? x.Toc)!.Value.Y + 7;
        Assert.Equal(y, marker.RenderTransform!.Value.M32, 1);
    }
}
