using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class EventCardTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Border Card(PopCard card) => Part<Border>((Control)card.Popup.Child!, "LayoutRoot");

    // A card about as tall as an event's details: its title, a line and an action.
    private static IDataTemplate Details() => new FuncDataTemplate<MonthEvent>((e, _) => new StackPanel
    {
        Spacing = 8,
        Height = 180,
        Children = { new TextBlock { Text = e.Title, FontWeight = FontWeight.Bold }, new TextBlock { Text = "تفاصيل الموعد" }, new Button { Content = "فتح", Tag = "open" } },
    });

    private static MonthCalendar Calendar(string dir, MonthCalendarView view, params MonthEvent[] events) => new()
    {
        View = view,
        Date = new DateTime(2026, 9, 21),
        Locale = dir == "rtl" ? "ar" : "en",
        HourHeight = 40,
        Events = events.ToList(),
        EventDetails = Details(),
    };

    private static MonthEvent At(int day, int hour, string title) =>
        new() { Date = new DateTime(2026, 9, day), Time = TimeSpan.FromHours(hour), Title = title };

    // The week's chips, each day at 9, so the first and last columns hold one whichever day the week starts on.
    private static MonthEvent[] EveryDay() => Enumerable.Range(19, 9).Select(d => At(d, 9, $"موعد {d}")).ToArray();

    private static (Window W, MonthCalendar Month) Open(string dir, MonthCalendar month, double width = 1000, double height = 760)
    {
        var w = Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: width, height: height);
        return (w, month);
    }

    private static PopCard Press(Window w, MonthCalendar month, Control chip)
    {
        Click(chip, w);
        var card = month.DetailsCard!;
        Assert.True(card.IsOpen);
        Assert.Same(chip, card.Target);
        return card;
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void With_room_at_its_end_the_card_opens_on_that_side_of_the_event(string dir)
    {
        var events = EveryDay();
        var (w, month) = Open(dir, Calendar(dir, MonthCalendarView.Week, events));
        var chips = events.Select(month.ChipFor).OfType<Control>().ToList();
        var chip = dir == "rtl" ? chips.MaxBy(c => Box(c, w).X)! : chips.MinBy(c => Box(c, w).X)!;
        var card = Press(w, month, chip);
        var c = Box(Card(card), w);
        var t = Box(chip, w);
        if (dir == "rtl") Assert.Equal(t.Left - 6, c.Right, 1);
        else Assert.Equal(t.Right + 6, c.Left, 1);
        Assert.Equal(t.Top, c.Top, 1);
        Capture(w, $"event-card-end-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void With_no_room_at_its_end_the_card_opens_at_its_start(string dir)
    {
        var events = EveryDay();
        var (w, month) = Open(dir, Calendar(dir, MonthCalendarView.Week, events));
        var chips = events.Select(month.ChipFor).OfType<Control>().ToList();
        var chip = dir == "rtl" ? chips.MinBy(c => Box(c, w).X)! : chips.MaxBy(c => Box(c, w).X)!;
        var card = Press(w, month, chip);
        var c = Box(Card(card), w);
        var t = Box(chip, w);
        if (dir == "rtl") Assert.Equal(t.Right + 6, c.Left, 1);
        else Assert.Equal(t.Left - 6, c.Right, 1);
        Capture(w, $"event-card-start-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void With_no_room_beside_it_the_card_opens_below_the_event(string dir)
    {
        var early = At(21, 7, "اجتماع الصباح");
        var (w, month) = Open(dir, Calendar(dir, MonthCalendarView.Day, early, At(21, 16, "مراجعة")));
        var chip = month.ChipFor(early)!;
        var card = Press(w, month, chip);
        var c = Box(Card(card), w);
        var t = Box(chip, w);
        Assert.Equal(t.Bottom + 6, c.Top, 1);
        if (dir == "rtl") Assert.Equal(t.Right, c.Right, 1);
        else Assert.Equal(t.Left, c.Left, 1);
        Capture(w, $"event-card-below-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void With_room_only_above_the_card_opens_over_the_event(string dir)
    {
        var late = At(21, 16, "مراجعة");
        var (w, month) = Open(dir, Calendar(dir, MonthCalendarView.Day, At(21, 7, "اجتماع الصباح"), late));
        var chip = month.ChipFor(late)!;
        var card = Press(w, month, chip);
        var c = Box(Card(card), w);
        var t = Box(chip, w);
        Assert.Equal(t.Top - 6, c.Bottom, 1);
        Assert.True(c.Top >= 8);
        Capture(w, $"event-card-above-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_card_grows_out_of_its_event_and_shrinks_back_into_it(string dir)
    {
        var events = EveryDay();
        var (w, month) = Open(dir, Calendar(dir, MonthCalendarView.Week, events));
        var chip = month.ChipFor(events[2])!;
        var card = Press(w, month, chip);
        var surface = card.Grow.Surface!;
        var from = card.Grow.Start!;
        var size = surface.Bounds.Size;
        var centre = new Point(size.Width / 2, size.Height / 2);
        Point Map(Point p) => centre + (Vector)from.Value.Transform(p - centre);
        var got = new Rect(Map(default), Map(new Point(size.Width, size.Height))).Normalize();
        var a = chip.TranslatePoint(default, surface)!.Value;
        var b = chip.TranslatePoint(new Point(chip.Bounds.Width, chip.Bounds.Height), surface)!.Value;
        var want = new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
        Assert.Equal(want.X, got.X, 1);
        Assert.Equal(want.Y, got.Y, 1);
        Assert.Equal(want.Width, got.Width, 1);
        Assert.Equal(want.Height, got.Height, 1);
        Assert.Equal(Matrix.Identity, (surface.RenderTransform as TransformOperations)?.Value ?? Matrix.Identity);
        Assert.Same(events[2], ((Control)card.Content!).DataContext);

        Assert.False(surface.FindAncestorOfType<global::Avalonia.Controls.Primitives.OverlayPopupHost>()!.ClipToBounds);

        month.HideDetails();
        Assert.True(card.IsOpen);
        Assert.True(card.Grow.IsShrinking);
        Settle(400);
        Assert.False(card.IsOpen);

    }

    [AvaloniaFact]
    public void Focus_goes_into_the_card_and_Esc_brings_it_back_to_the_event()
    {
        var events = EveryDay();
        var (w, month) = Open("rtl", Calendar("rtl", MonthCalendarView.Week, events));
        var invoked = new List<MonthEvent>();
        month.EventInvoked += (_, e) => invoked.Add(e);
        var chip = month.ChipFor(events[3])!;
        chip.Focus();
        w.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        w.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Settle();
        var card = month.DetailsCard!;
        Assert.True(card.IsOpen);
        Assert.Equal([events[3]], invoked);
        var open = ((Control)card.Content!).GetVisualDescendants().OfType<Button>().Single();
        Assert.True(open.IsFocused);

        w.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        w.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Settle();
        Assert.False(card.IsOpen);
        Assert.True(chip.IsFocused);
    }

    [AvaloniaFact]
    public void A_second_press_on_the_same_event_closes_its_card()
    {
        var events = EveryDay();
        var (w, month) = Open("ltr", Calendar("ltr", MonthCalendarView.Week, events));
        var chip = month.ChipFor(events[4])!;
        var card = Press(w, month, chip);
        Click(chip, w);
        Assert.False(card.IsOpen);

        Click(chip, w);
        Assert.True(card.IsOpen);
        var other = month.ChipFor(events[2])!;
        var days = new List<DateTime>();
        month.DayInvoked += (_, d) => days.Add(d);
        Click(other, w);
        Assert.True(card.IsOpen);
        Assert.Same(other, card.Target);

        var empty = month.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("week-day"));
        Click(empty, w);
        Assert.False(card.IsOpen);
        Assert.Empty(days);
    }

    [AvaloniaFact]
    public void Still_motion_opens_and_closes_the_card_at_once()
    {
        Motion.SystemOverride = true;
        try
        {
            var events = EveryDay();
            var (w, month) = Open("rtl", Calendar("rtl", MonthCalendarView.Week, events));
            var card = Press(w, month, month.ChipFor(events[2])!);
            Assert.Null(card.Grow.Start);
            Assert.Equal(1, card.Grow.Surface?.Opacity ?? 1);
            month.HideDetails();
            Assert.False(card.IsOpen);
        }
        finally { Motion.SystemOverride = null; }
    }

    [AvaloniaFact]
    public void Without_a_details_template_only_linked_events_can_be_pressed()
    {
        var plain = At(21, 9, "اجتماع");
        var linked = new MonthEvent { Date = new DateTime(2026, 9, 22), Time = TimeSpan.FromHours(9), Title = "مراجعة", Href = "/r/1" };
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 21), Events = [plain, linked] };
        Show(month, FlowDirection.RightToLeft, width: 1000, height: 760);
        Assert.False(month.ChipFor(plain)!.Focusable);
        Assert.True(month.ChipFor(linked)!.Focusable);
        month.ShowDetails(linked);
        Assert.Null(month.DetailsCard);
        month.EventDetails = Details();
        Settle(100);
        Assert.True(month.ChipFor(plain)!.Focusable);
        Assert.Contains("link", month.ChipFor(plain)!.Classes);
        month.ShowDetails(plain);
        Settle();
        Assert.True(month.DetailsCard!.IsOpen);
        Assert.Same(month.ChipFor(plain), month.DetailsCard.Target);
    }
}
