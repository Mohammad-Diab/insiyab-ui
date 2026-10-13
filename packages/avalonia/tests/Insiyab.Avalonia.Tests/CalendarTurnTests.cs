using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

[Collection("Pickers")]
public class CalendarTurnTests : IDisposable
{
    private static readonly Func<DateTime> FixedClock = () => new DateTime(2026, 9, 15);
    private static readonly DateTime Sep24 = new(2026, 9, 24);

    public CalendarTurnTests() => DateCalendar.Today = FixedClock;

    public void Dispose()
    {
        CalendarTurn.Held = false;
        if (ReferenceEquals(DateCalendar.Today, FixedClock)) DateCalendar.Today = () => DateTime.Today;
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static void Still(Window w, bool on) => Motion.SetStill(w, on);

    private static (DateCalendar Cal, Window Window) Hijri(string dir = "rtl")
    {
        var cal = new DateCalendar { CalendarKind = CalendarKind.Hijri, SelectedDate = Sep24 };
        var page = new StackPanel { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, Children = { cal } };
        var w = Show(page, Dir(dir), width: 800, height: 700);
        cal.Show(Sep24);
        Settle();
        Tap(Option(cal, "gregory"), w);
        Settle();
        Tap(Option(cal, "home"), w);
        Settle();
        return (cal, w);
    }

    private static Button Option(DateCalendar cal, string tag) =>
        cal.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-switch-option") && b.Tag as string == tag);

    private static void Tap(Control c, Window w)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
    }

    // The turn is held on its first frame while the start is read, then let go.
    private static void TapHeld(Control c, Window w, Action atStart)
    {
        CalendarTurn.Held = true;
        try
        {
            Tap(c, w);
            atStart();
        }
        finally { CalendarTurn.Held = false; }
    }

    private static void Pump(Window w, int ms, Action<double>? each = null)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < ms)
        {
            w.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            each?.Invoke(clock.ElapsedMilliseconds);
            Thread.Sleep(8);
        }
    }

    private static Point Mid(Control c, Visual to) => c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), to)!.Value;

    private static bool AtRest(Visual v) => v.Opacity == 1 && (v.RenderTransform is null || v.RenderTransform.Value.IsIdentity);

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Switch_glides_each_day_from_its_old_cell_and_turns_its_number(string dir)
    {
        var (cal, w) = Hijri(dir);
        var before = Mid(cal.DayButtons[Sep24], cal);
        var hijriLabel = (string)cal.DayButtons[Sep24].Content!;

        TapHeld(Option(cal, "gregory"), w, () =>
        {
            var start = Mid(cal.DayButtons[Sep24], cal);
            Assert.True(Math.Abs(start.X - before.X) < 1 && Math.Abs(start.Y - before.Y) < 1, $"started at {start}, was at {before}");
            Assert.Equal(hijriLabel, cal.DayButtons[Sep24].Content);
        });
        var day = cal.DayButtons[Sep24];

        var thinnest = 1.0;
        var labels = new HashSet<string>();
        Pump(w, 700, _ =>
        {
            thinnest = Math.Min(thinnest, day.RenderTransform?.Value.M22 ?? 1);
            labels.Add((string)day.Content!);
        });
        Assert.True(thinnest < 0.6, $"thinnest fold {thinnest}");
        Assert.Contains(hijriLabel, labels);
        var gregorianLabel = (string)day.Content!;
        Assert.NotEqual(hijriLabel, gregorianLabel);
        Assert.True(AtRest(day));
        var end = Mid(day, cal);
        Assert.False(Math.Abs(end.X - before.X) < 1 && Math.Abs(end.Y - before.Y) < 1, "Sep 24 sits in another cell in the Gregorian month");
        Assert.False(cal.RunningTurn?.IsRunning ?? false);
    }

    [AvaloniaFact]
    public void Leaving_days_fade_as_ghosts_and_new_days_fade_in()
    {
        var (cal, w) = Hijri();
        var oldDays = cal.DayButtons.Keys.ToHashSet();
        var layer = cal.TurnLayer!;
        TapHeld(Option(cal, "gregory"), w, () =>
        {
            var ghosts = layer.Children.OfType<Button>().ToList();
            Assert.NotEmpty(ghosts);
            Assert.All(ghosts, g => Assert.Equal(38, g.Bounds.Height));
            Assert.All(ghosts, g => Assert.False(g.IsHitTestVisible));
            Assert.All(ghosts, g => Assert.Equal(1, g.Opacity));
            var arriving = cal.DayButtons.Where(kv => !oldDays.Contains(kv.Key)).Select(kv => kv.Value).ToList();
            Assert.NotEmpty(arriving);
            Assert.All(arriving, a => Assert.Equal(0, a.Opacity));
        });

        Settle();
        Assert.Empty(layer.Children);
        Assert.All(cal.DayButtons.Values, b => Assert.True(AtRest(b)));
    }

    [AvaloniaFact]
    public void Switch_face_stretches_across_then_lands_on_the_new_side()
    {
        var (cal, w) = Hijri();
        Tap(Option(cal, "gregory"), w);
        var track = cal.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("cal-switch"));
        Assert.Contains("moving", track.Classes);
        var face = track.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("cal-switch-face"));
        var home = Option(cal, "home");
        var gregory = Option(cal, "gregory");
        var widest = 0.0;
        Pump(w, 300, _ => widest = Math.Max(widest, face.Bounds.Width));
        Assert.True(widest > Math.Max(home.Bounds.Width, gregory.Bounds.Width) + 10, $"widest {widest}");

        Settle();
        Assert.DoesNotContain("moving", track.Classes);
        Assert.Null(face.Parent);
        Assert.Contains("active", gregory.Classes);
    }

    [AvaloniaFact]
    public void Titles_drop_in_after_the_switch()
    {
        var (cal, w) = Hijri();
        TapHeld(Option(cal, "gregory"), w, () => Assert.Equal(0, cal.HostControls().First(c => c.Classes.Contains("cal-title")).Opacity));
        var title = cal.HostControls().First(c => c.Classes.Contains("cal-title"));
        var seen = new List<double>();
        Pump(w, 500, _ => seen.Add(title.Opacity));
        Assert.Contains(seen, o => o > 0.05 && o < 0.95);
        Settle();
        Assert.True(AtRest(title));
    }

    [AvaloniaFact]
    public void Switching_back_mid_turn_lands_cleanly()
    {
        var (cal, w) = Hijri();
        var hijri = cal.DayButtons.ToDictionary(kv => kv.Key, kv => (string)kv.Value.Content!);
        Tap(Option(cal, "gregory"), w);
        Pump(w, 120);
        Tap(Option(cal, "home"), w);
        Settle();
        Assert.Empty(cal.TurnLayer!.Children);
        Assert.Equal(hijri, cal.DayButtons.ToDictionary(kv => kv.Key, kv => (string)kv.Value.Content!));
        Assert.All(cal.DayButtons.Values, b => Assert.True(AtRest(b)));
    }

    [AvaloniaFact]
    public void Turn_is_skipped_when_motion_is_still()
    {
        var (cal, w) = Hijri();
        Still(w, true);
        try
        {
            Tap(Option(cal, "gregory"), w);
            Assert.Null(cal.RunningTurn);
            Assert.Empty(cal.TurnLayer!.Children);
            Assert.All(cal.DayButtons.Values, b => Assert.True(AtRest(b)));
            Assert.DoesNotContain(cal.GetVisualDescendants().OfType<Border>(), b => b.Classes.Contains("cal-switch-face"));
        }
        finally { Still(w, false); }
    }

    // A click on the switch leaves no ring or brand patch on the chosen face; reached by the keyboard, the face shows its own rounded ring.
    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_switch_face_is_clean_after_a_click(string dir)
    {
        var (cal, w) = Hijri(dir);
        Tap(Option(cal, "gregory"), w);
        Settle(900);
        w.MouseMove(new Point(790, 690));
        Settle(300);
        var chosen = Option(cal, "gregory");
        Assert.Contains("active", chosen.Classes);
        Assert.Null(chosen.FocusAdorner);
        Assert.DoesNotContain(":focus-visible", chosen.Classes);
        Assert.False(Part<Border>(chosen, "PART_Ring").IsVisible);
        var face = Part<Border>(chosen, "PART_Face");
        var brand = ColorOf((IBrush)Res(chosen, "InsToneBrandSoft"));
        var frame = w.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        foreach (var corner in new[] { new Point(1, 1), new Point(face.Bounds.Width - 2, 1), new Point(1, face.Bounds.Height - 2), new Point(face.Bounds.Width - 2, face.Bounds.Height - 2) })
        {
            var at = face.TranslatePoint(corner, w)!.Value;
            var c = PixelAt(buffer, (int)at.X, (int)at.Y);
            Assert.True(Math.Abs(c.R - brand.R) + Math.Abs(c.G - brand.G) + Math.Abs(c.B - brand.B) > 90, $"{dir}: brand at {corner}: {c}");
        }

        Option(cal, "home").Focus(NavigationMethod.Tab);
        PressSpace(w);
        Settle(900);
        var home = Option(cal, "home");
        Assert.Contains("active", home.Classes);
        Assert.Contains(":focus-visible", home.Classes);
        Assert.True(Part<Border>(home, "PART_Ring").IsVisible);
    }

    [AvaloniaFact]
    public void Month_arrows_do_not_turn()
    {
        var (cal, w) = Hijri();
        var next = cal.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-nav") && b.Tag as string == "next");
        Tap(next, w);
        Assert.Null(cal.RunningTurn);
        Assert.Empty(cal.TurnLayer!.Children);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Mid_turn_frame_is_saved_for_review(string dir)
    {
        var (cal, w) = Hijri(dir);
        Tap(Option(cal, "gregory"), w);
        Pump(w, 120);
        var frame = w.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame!.Save(Path.Combine(ShotsDir(), $"calendar-turn-mid-{dir}.png"), PngBitmapEncoderOptions.Default);
        Settle();
        Capture(w, $"calendar-turn-end-{dir}");
    }
}
