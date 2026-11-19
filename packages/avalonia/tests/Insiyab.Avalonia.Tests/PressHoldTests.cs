using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Effects off drop a press's movement, never its feedback: a quick tap (press and release before a frame is drawn) still draws the pressed colour in the frames after it, and lets go of it soon after. The kinds that showed no press at all hold it with the effects on too.
public class PressHoldTests
{
    private static readonly Geometry Plus = Geometry.Parse("M4 12h16M12 4v16");

    private sealed class Nothing : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
    }

    private static Button B(string classes, object? content = null)
    {
        var b = new Button { Content = content ?? "حفظ" };
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) b.Classes.Add(c);
        return b;
    }

    // each pressable kind: what goes in the window, and what the finger taps
    public static TheoryData<string> Kinds() =>
    [
        "secondary", "primary", "danger", "fab", "ghost icon", "bare", "bare row", "link", "close", "chip", "island", "pop-item", "list row", "notify row",
        "segment", "sidebar row",
    ];

    private static (Control Host, Control Target) Make(string kind)
    {
        switch (kind)
        {
            case "secondary": return One(B("secondary sm"));
            case "primary": return One(B("primary sm"));
            case "danger": return One(B("danger sm"));
            case "fab": return One(new Button { Classes = { "fab" }, Content = new Icon { Data = Plus }, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = default });
            case "ghost icon": return One(B("ghost icon sm", new Icon { Data = Plus }));
            case "bare": return One(B("bare", "إلغاء"));
            case "bare row":
                return One(new Button
                {
                    Classes = { "bare" }, Width = 300, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Content = new StackPanel { Children = { new TextBlock { Text = "وظيفة BIS601" }, new TextBlock { Text = "بعد يومين" } } },
                });
            case "link": return One(B("sm link", "عرض الكل"));
            case "close": return One(B("close"));
            case "chip": return One(B("chip", "عرض ما مضى (3)"));
            case "island": return One(B("island icon", new Icon { Data = Plus }));
            case "pop-item": return One(new Button { Classes = { "pop-item" }, Content = "نسخ الرابط", Width = 220 });
            case "list row":
            {
                var list = new ListBox { Width = 300 };
                ListRow.SetCommand(list, new Nothing());
                var a = new ListBoxItem { Content = "الرسائل غير المقروءة" };
                var b = new ListBoxItem { Content = "شؤون الموظفين" };
                list.Items.Add(a);
                list.Items.Add(b);
                return (new Border { Classes = { "card" }, Child = list }, b);
            }
            case "notify row": return One(new NotifyItem { Content = "رسالة جديدة", Width = 300, Icon = Plus });
            case "segment":
            {
                var other = new SegmentedItem { Content = "الأسبوع" };
                return (new Segmented { Items = { new SegmentedItem { Content = "الشهر" }, other }, SelectedIndex = 0 }, other);
            }
            case "sidebar row":
            {
                var list = new ListBox { Classes = { "shell-nav" }, Width = 260 };
                var a = new ListBoxItem { Content = "الرئيسية" };
                var b = new ListBoxItem { Content = "التقويم" };
                list.Items.Add(a);
                list.Items.Add(b);
                list.SelectedIndex = 0;
                return (list, b);
            }
        }
        throw new ArgumentException(kind);

        static (Control, Control) One(Control c) => (c, c);
    }

    private static Window Host(Control host, bool still, bool touch = true)
    {
        var outer = new Panel { Children = { new Border { Padding = new Thickness(24), Child = host, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } } };
        if (touch) outer.Classes.Add("touch");
        var window = Show(outer, FlowDirection.RightToLeft, width: 440, height: 200);
        Motion.SetStill(window, still);
        Settle(300);
        return window;
    }

    private static Rect Box(Control c, Window w)
    {
        var a = c.TranslatePoint(default, w)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), w)!.Value;
        var r = new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y))).Inflate(4);
        return new Rect(Math.Floor(r.X), Math.Floor(r.Y), Math.Ceiling(r.Width), Math.Ceiling(r.Height));
    }

    private static byte[] Pixels(WriteableBitmap bmp, Rect box)
    {
        using var fb = bmp.Lock();
        var w = (int)box.Width;
        var outp = new byte[w * (int)box.Height * 4];
        for (var y = 0; y < (int)box.Height; y++)
            Marshal.Copy(fb.Address + ((int)box.Y + y) * fb.RowBytes + (int)box.X * 4, outp, y * w * 4, w * 4);
        return outp;
    }

    // how many pixels of the box moved by a visible step (8 of 255 in a channel) from the rest picture
    private static int Changed(byte[] a, byte[] b)
    {
        var n = 0;
        for (var i = 0; i < a.Length; i += 4)
            if (Math.Max(Math.Abs(a[i] - b[i]), Math.Max(Math.Abs(a[i + 1] - b[i + 1]), Math.Abs(a[i + 2] - b[i + 2]))) >= 8) n++;
        return n;
    }

    // a finger's quick tap (down and up before any frame is drawn), then the frames of the next ms: each frame's changed pixels against the rest picture
    private static List<(long Ms, int Px)> Tap(Window window, Control target, int ms)
    {
        var box = Box(target, window);
        var rest = Pixels(window.CaptureRenderedFrame()!, box);
        var at = box.Center;
        var t = window.TouchBegin(at);
        window.TouchEnd(t, at);
        var frames = new List<(long, int)>();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < ms)
        {
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            frames.Add((clock.ElapsedMilliseconds, Changed(rest, Pixels(window.CaptureRenderedFrame()!, box))));
            Thread.Sleep(4);
        }
        return frames;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Kinds))]
    public void A_quick_tap_with_the_effects_off_draws_the_pressed_colour_in_the_next_frames(string kind)
    {
        var (host, target) = Make(kind);
        var window = Host(host, still: true);
        var area = (int)(target.Bounds.Width * target.Bounds.Height);
        var frames = Tap(window, target, 700); // long enough for the fade back to end even on a busy machine
        // seen from the first frames, through about 100 ms, and gone by the end (the colour fades back once the hold is over)
        var early = frames.Where(f => f.Ms is >= 20 and <= 110).ToList();
        Assert.NotEmpty(early);
        Assert.All(early, f => Assert.True(f.Px >= Math.Max(30, area / 20), $"{kind}: {f.Px} px changed at {f.Ms} ms of {area}"));
        // (a list row, a segment and a sidebar row stay chosen after their tap: their end is their selection, not the press)
        if (kind is not ("list row" or "segment" or "sidebar row"))
            Assert.True(frames[^1].Px < Math.Max(30, area / 20), $"{kind}: still {frames[^1].Px} px changed at the end");
    }

    // The calendar's parts (MonthBorder): a day and a linked event show their pressed colour while a finger or the mouse is down, only the innermost part under the press takes it (the chip, not its day), and a quick tap's colour is held a moment, effects on or off
    [AvaloniaTheory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void A_calendar_day_and_its_event_show_their_press(bool touch, bool still)
    {
        var ev = new MonthEvent { Date = new DateTime(2026, 9, 22), Title = "مراجعة", Href = "/r/1", Tone = MonthEventTone.Info };
        var month = new MonthCalendar { Date = new DateTime(2026, 9, 21), Events = [ev], Width = 760, Height = 560 };
        var outer = new Panel { Children = { month } };
        if (touch) outer.Classes.Add("touch");
        var window = Show(outer, FlowDirection.RightToLeft, width: 800, height: 600);
        Motion.SetStill(window, still);
        Settle(300);
        var chip = month.ChipFor(ev)!;
        var day = chip.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("month-day"));
        foreach (var (target, other) in new (Control, Control?)[] { (chip, day), (day, null) })
        {
            var box = Box(target, window);
            var at = target == day ? new Point(box.X + 12, box.Bottom - 12) : box.Center;
            if (!touch)
            {
                window.MouseMove(at);
                Settle(300);
            }
            var rest = Pixels(window.CaptureRenderedFrame()!, box);
            var t = touch ? window.TouchBegin(at) : null;
            if (!touch) window.MouseDown(at, MouseButton.Left);
            Settle(120);
            Assert.Contains(":pressed", target.Classes);
            if (other is not null) Assert.DoesNotContain(":pressed", other.Classes);
            var area = (int)(target.Bounds.Width * target.Bounds.Height);
            var px = Changed(rest, Pixels(window.CaptureRenderedFrame()!, box));
            Assert.True(px >= Math.Max(30, area / 20), $"{target.Classes}: {px} px changed of {area} while down");
            // a slide (a swipe, a scroll) lets go of it, and nothing is held
            var away = at + new Vector(0, 30);
            if (touch)
            {
                window.TouchMove(t!, away);
                window.TouchEnd(t!, away);
            }
            else
            {
                window.MouseMove(away);
                window.MouseUp(away, MouseButton.Left);
            }
            Dispatcher.UIThread.RunJobs();
            Assert.DoesNotContain(":pressed", target.Classes);
        }
        // a quick tap on the chip: held after the release, then let go
        var c = Center(chip, window);
        if (touch)
        {
            var q = window.TouchBegin(c);
            window.TouchEnd(q, c);
        }
        else
        {
            window.MouseDown(c, MouseButton.Left);
            window.MouseUp(c, MouseButton.Left);
        }
        Dispatcher.UIThread.RunJobs();
        Assert.True(PressHold.IsHeld(chip));
        Settle(PressHold.Hold.Milliseconds + 150);
        Assert.False(PressHold.IsHeld(chip));
        month.HideDetails();
    }

    [AvaloniaFact]
    public void The_hold_ends_after_its_time_and_the_window_moves_nothing()
    {
        var button = B("primary sm");
        var window = Host(button, still: true);
        var at = Center(button, window);
        var t = window.TouchBegin(at);
        window.TouchEnd(t, at);
        Dispatcher.UIThread.RunJobs();
        Assert.False(button.IsPressed);
        Assert.Contains(":pressed", button.Classes);
        Assert.True(PressHold.IsHeld(button));
        var root = Part<Panel>(button, "PART_Root");
        Assert.True(root.RenderTransform is null || root.RenderTransform.Value.IsIdentity);
        Settle(PressHold.Hold.Milliseconds + 150);
        Assert.DoesNotContain(":pressed", button.Classes);
        Assert.False(PressHold.IsHeld(button));
    }

    [AvaloniaFact]
    public void Effects_on_a_moving_kind_is_not_held_and_still_dips()
    {
        var button = B("primary sm");
        var window = Host(button, still: false);
        var at = Center(button, window);
        var t = window.TouchBegin(at);
        window.TouchEnd(t, at);
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(":pressed", button.Classes);
        Assert.False(PressHold.IsHeld(button));
    }

    [AvaloniaFact]
    public void Effects_on_a_kind_with_no_press_of_its_own_is_held()
    {
        var (host, target) = Make("bare row");
        var window = Host(host, still: false);
        var frames = Tap(window, target, 120);
        Assert.Contains(frames, f => f.Ms >= 30 && f.Px > 200);
    }

    [AvaloniaFact]
    public void A_press_that_slides_away_or_lets_go_outside_is_not_held()
    {
        var button = B("secondary sm");
        var window = Host(button, still: true);
        var at = Center(button, window);
        var t = window.TouchBegin(at);
        window.TouchMove(t, at + new Vector(0, 40));
        window.TouchEnd(t, at + new Vector(0, 40));
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(":pressed", button.Classes);

        var m = Center(button, window);
        window.MouseMove(m);
        window.MouseDown(m, MouseButton.Left);
        window.MouseUp(m, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(":pressed", button.Classes); // a mouse click is held the same way
        Settle(400);
        Assert.DoesNotContain(":pressed", button.Classes);
    }

    [AvaloniaFact]
    public void A_new_press_during_the_hold_keeps_its_own_look()
    {
        var button = B("secondary sm");
        var window = Host(button, still: true);
        var at = Center(button, window);
        var t = window.TouchBegin(at);
        window.TouchEnd(t, at);
        var again = window.TouchBegin(at);
        Settle(PressHold.Hold.Milliseconds + 100);
        Assert.True(button.IsPressed);
        Assert.Contains(":pressed", button.Classes); // the first hold's end did not clear the finger that is down now
        window.TouchEnd(again, at);
        Settle(400);
        Assert.DoesNotContain(":pressed", button.Classes);
    }

    [AvaloniaFact]
    public void The_press_layer_has_a_light_and_a_dark_tone()
    {
        var button = B("bare");
        var window = Show(button);
        Assert.True(Application.Current!.TryFindResource("InsPressLayer", ThemeVariant.Light, out var light));
        Assert.True(Application.Current!.TryFindResource("InsPressLayer", ThemeVariant.Dark, out var dark));
        Assert.NotEqual(((ISolidColorBrush)light!).Color, ((ISolidColorBrush)dark!).Color);
        Assert.NotNull(window);
    }
}
