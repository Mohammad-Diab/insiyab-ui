using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The month calendar at a phone's width: two events a day, narrow columns of hours, a header that wraps, a sideways swipe, and details in a sheet.
public class PhoneCalendarTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static Rect BoxOf(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static readonly DateTime Day = new(2026, 9, 21);

    private static List<MonthEvent> Events() =>
    [
        new() { Date = Day, Time = new TimeSpan(9, 0, 0), Title = "Supplier visit at the warehouse", Tag = "Bring the delivery notes" },
        new() { Date = Day, Time = new TimeSpan(11, 0, 0), Title = "Client call" },
        new() { Date = Day, Time = new TimeSpan(13, 0, 0), Title = "Training" },
        new() { Date = Day, Time = new TimeSpan(16, 0, 0), Title = "Deadline", Tone = MonthEventTone.Bad },
        new() { Date = Day.AddDays(1), Title = "Stock count", Tone = MonthEventTone.Info },
        new() { Date = Day.AddDays(2), Time = new TimeSpan(10, 0, 0), Title = "Team meeting" },
    ];

    private static IDataTemplate Details() => new FuncDataTemplate<MonthEvent>((e, _) => new TextBlock { Text = e?.Title + " · " + e?.Tag }, supportsRecycling: false);

    private static MonthCalendar Calendar(MonthCalendarView view = MonthCalendarView.Month, string dir = "ltr") => new()
    {
        View = view,
        Date = Day,
        Events = Events(),
        Locale = dir == "rtl" ? "ar" : "en",
        EventDetails = Details(),
    };

    private static void Swipe(TopLevel w, Point from, Vector by, int steps = 8, int pause = 12)
    {
        Settle(550);
        var t = w.TouchBegin(from);
        Settle(40);
        for (var k = 1; k <= steps; k++)
        {
            w.TouchMove(t, from + by * k / steps);
            Settle(pause);
        }
        w.TouchEnd(t, from + by);
        Settle(80);
    }

    private static void Tap(TopLevel w, Point p)
    {
        Settle(550);
        var t = w.TouchBegin(p);
        Settle(40);
        w.TouchEnd(t, p);
        Settle(80);
    }

    private static bool Back(TopLevel top)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        top.RaiseEvent(e);
        return e.Handled;
    }

    [AvaloniaFact]
    public void A_single_view_at_a_phone_width_is_compact_and_wide_it_is_not()
    {
        var month = Calendar(dir: "rtl");
        var root = OverlaysTests.SingleView(new Border { Width = 360, Child = month }, 360, 800);
        try
        {
            OverlaysTests.Pump(root, 300);
            Assert.True(month.IsCompact);
            Assert.Contains(":compact", month.Classes);
            Assert.Single(month.Cells[Day].GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("month-more-link")));
            ((Border)month.Parent!).Width = 700;
            OverlaysTests.Pump(root, 300);
            Assert.False(month.IsCompact);
            Assert.Empty(month.Cells[Day].GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("month-more-link")));
        }
        finally { root.StopRendering(); }
    }

    // The layout at 360 is checked in a window asked to be compact: a headless single view lays its content out at its host window's size.
    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_compact_month_shows_two_events_a_line_each_and_a_count_that_opens_the_day(string dir)
    {
        var month = Calendar(dir: dir);
        month.NarrowLayout = MonthNarrowLayout.Compact;
        var w = Show(month, Dir(dir), width: 360, height: 800);
        Settle(200);
        Assert.True(month.IsCompact);
        var cell = month.Cells[Day];
        var chips = cell.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("month-event")).ToList();
        Assert.Equal(2, chips.Count);
        foreach (var chip in chips)
        {
            Assert.True(chip.Bounds.Height <= 17, $"a chip is {chip.Bounds.Height} tall");
            Assert.False(chip.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Classes.Contains("month-time")));
            var box = BoxOf(chip, w);
            var day = BoxOf(cell, w);
            Assert.True(box.Left >= day.Left - 0.5 && box.Right <= day.Right + 0.5, $"{box} in {day}");
        }
        var cut = chips[0].GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("month-event-title"));
        Assert.Equal(TextTrimming.CharacterEllipsis, cut.TextTrimming);
        Assert.True(cut.TextLayout.TextLines[0].HasCollapsed, "a long title is cut with an ellipsis");
        Assert.All(month.Cells.Values.Take(7), c => Assert.Equal(month.Cells[Day].Bounds.Width, c.Bounds.Width, 1));
        var more = cell.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("month-more-link"));
        Assert.Equal("+2", more.GetVisualDescendants().OfType<TextBlock>().Single().Text);
        Assert.True(more.Focusable);
        Assert.Contains(dir == "rtl" ? "٤ مواعيد" : "4 events", AutomationProperties.GetName(more));
        Assert.True(BoxOf(more, w).Bottom <= BoxOf(cell, w).Bottom + 0.5, "the count fits in the day");
        Assert.Contains(month.GetVisualDescendants().OfType<TextBlock>(), t => t.Classes.Contains("month-wd") && t.Text == (dir == "rtl" ? "ن" : "M"));

        var days = new List<DateTime>();
        month.DayInvoked += (_, d) => days.Add(d);
        more.Focus(NavigationMethod.Tab);
        w.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        w.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Settle(100);
        Tap(w, BoxOf(more, w).Center);
        Assert.Equal([Day, Day], days);
        Capture(w, $"month-phone-month-{dir}");
    }

    [AvaloniaFact]
    public void A_desktop_window_as_narrow_keeps_its_dots()
    {
        var month = Calendar();
        Show(month, width: 360, height: 800);
        Settle(200);
        Assert.False(month.IsCompact);
        Assert.Contains(":narrow", month.Classes);
        var cell = month.Cells[Day];
        Assert.True(cell.GetVisualDescendants().OfType<StackPanel>().Single(s => s.Classes.Contains("month-dots")).IsEffectivelyVisible);
        Assert.False(cell.GetVisualDescendants().OfType<StackPanel>().Single(s => s.Classes.Contains("month-events")).IsEffectivelyVisible);
        Assert.Empty(cell.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("month-more-link")));

        month.NarrowLayout = MonthNarrowLayout.Compact;
        Settle(200);
        Assert.True(month.IsCompact);
        Assert.Single(month.Cells[Day].GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("month-more-link")));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_phone_week_keeps_seven_readable_columns_beside_narrow_hours_with_the_whole_day_row_above(string dir)
    {
        var month = Calendar(MonthCalendarView.Week, dir);
        month.NarrowLayout = MonthNarrowLayout.Compact;
        month.Events!.Add(new MonthEvent { Date = Day, Title = "Holiday", Tone = MonthEventTone.Ok });
        var w = Show(month, Dir(dir), width: 360, height: 900);
        Settle(200);
        Assert.True(month.IsCompact);
        var hours = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-hour-cell")).ToList();
        Assert.True(hours[0].Bounds.Width <= 44, $"the hours column is {hours[0].Bounds.Width} wide");
        Assert.Equal(dir == "rtl" ? "6 ص" : "6 AM", hours[0].GetVisualDescendants().OfType<TextBlock>().Single().Text);
        var columns = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-day")).ToList();
        Assert.Equal(7, columns.Count);
        Assert.All(columns, c => Assert.True(c.Bounds.Width >= 42, $"a day is {c.Bounds.Width} wide"));
        var heads = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-head")).ToList();
        Assert.All(heads, h => Assert.Single(h.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("week-wd")).Text!));
        var timed = month.ChipFor(month.Events.First())!;
        Assert.False(timed.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("week-event-time")).IsVisible);
        Assert.Equal(10, timed.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("week-event-title")).FontSize);
        var allDay = month.ChipFor(month.Events[^1])!;
        Assert.True(BoxOf(allDay, w).Bottom <= BoxOf(columns[0], w).Top + 0.5, "the whole-day events sit above the hours");
        Assert.True(BoxOf(month, w).Width <= 360.5);
        Capture(w, $"month-phone-week-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl", MonthHeaderLayout.Centered)]
    [InlineData("ltr", MonthHeaderLayout.Centered)]
    [InlineData("rtl", MonthHeaderLayout.TitleStart)]
    [InlineData("ltr", MonthHeaderLayout.TitleStart)]
    public void The_head_wraps_cleanly_at_a_phone_width(string dir, MonthHeaderLayout layout)
    {
        var month = Calendar(MonthCalendarView.Week, dir);
        month.HeaderLayout = layout;
        var w = Show(new Panel { Margin = new Thickness(16), Children = { month } }, Dir(dir), width: 360, height: 900);
        Settle(200);
        var head = month.HeadRow!;
        Assert.True(head.IsWrapped);
        var row = BoxOf(head, w);
        var parts = head.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible && (c is Button or ToggleSwitch or Segmented || c.Classes.Contains("month-title"))).ToList();
        Assert.True(parts.Count >= 6);
        foreach (var part in parts)
        {
            var box = BoxOf(part, w);
            Assert.True(box.Left >= row.Left - 0.5 && box.Right <= row.Right + 0.5, $"{part.GetType().Name} {box} leaves the head {row}");
        }
        for (var i = 0; i < parts.Count; i++)
            for (var j = i + 1; j < parts.Count; j++)
                if (!parts[i].IsVisualAncestorOf(parts[j]) && !parts[j].IsVisualAncestorOf(parts[i]))
                    Assert.False(BoxOf(parts[i], w).Intersects(BoxOf(parts[j], w)), $"{parts[i].GetType().Name} and {parts[j].GetType().Name} overlap");
        // centred: Today keeps the date beside it on the first line (Google Calendar's grouping) while the two fit together
        if (layout == MonthHeaderLayout.Centered && !head.IsStacked)
            Assert.Equal(BoxOf(Assert.Single(parts, p => p.Tag as string == "today"), w).Center.Y, BoxOf(Assert.Single(parts, p => p.Classes.Contains("month-title")), w).Center.Y, 1);
        Assert.True(BoxOf(month.Body!, w).Top >= row.Bottom);
        Capture(w, $"month-phone-head-{layout}-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_sideways_swipe_turns_with_the_slide_and_never_opens_an_event(string dir)
    {
        var month = Calendar(dir: dir);
        month.NarrowLayout = MonthNarrowLayout.Compact;
        var opened = 0;
        var days = 0;
        month.EventInvoked += (_, _) => opened++;
        month.DayInvoked += (_, _) => days++;
        var w = Show(new ScrollViewer { Content = new StackPanel { Children = { month, new Border { Height = 900 } } } }, Dir(dir), width: 400, height: 700);
        Settle(200);
        var chip = month.ChipFor(month.Events!.First())!;
        // toward the start brings the next month in from the end, so a right-to-left page swipes right
        var toStart = dir == "rtl" ? 1 : -1;
        var real = Entrance.Now;
        try
        {
            Entrance.Now = () => 1000;
            Swipe(w, BoxOf(chip, w).Center, new Vector(120 * toStart, 6));
            Assert.Equal(new DateTime(2026, 10, 1), month.Month);
            Assert.True(month.Body!.RenderTransform is TranslateTransform { X: MonthCalendar.TurnSlide }, "the new days slide in from the end");
        }
        finally { Entrance.Now = real; }
        Settle(600);
        Assert.Equal(0, opened);
        Assert.Equal(0, days);

        Swipe(w, BoxOf(month.Body!, w).Center, new Vector(-120 * toStart, 0));
        Assert.Equal(new DateTime(2026, 9, 1), month.Month);

        // a short, slow swipe leaves the month as it was and the days ease back
        Swipe(w, BoxOf(month.Body!, w).Center, new Vector(30 * toStart, 0), 6, 120);
        Assert.Equal(new DateTime(2026, 9, 1), month.Month);
        Settle(500);
        Assert.Null(month.Body!.RenderTransform);
        Assert.Equal(1, month.Body.Opacity);

        // the week turns the same way
        month.View = MonthCalendarView.Week;
        Settle(200);
        var was = month.Date;
        Swipe(w, BoxOf(month.Body!, w).Center, new Vector(120 * toStart, 0));
        Assert.Equal(was.AddDays(7), month.Date);
        Assert.Equal(0, opened);
    }

    [AvaloniaFact]
    public void A_mouse_drag_and_a_vertical_finger_never_turn()
    {
        var month = Calendar();
        var scroller = new ScrollViewer { Content = new StackPanel { Children = { month, new Border { Height = 900 } } }, IsScrollInertiaEnabled = false };
        var w = Show(scroller, width: 900, height: 600);
        Settle(200);
        var at = BoxOf(month.Body!, w).Center;
        w.MouseDown(at, MouseButton.Left);
        w.MouseMove(at + new Vector(-200, 0));
        w.MouseUp(at + new Vector(-200, 0), MouseButton.Left);
        Settle(200);
        Assert.Equal(new DateTime(2026, 9, 1), month.Month);

        Swipe(w, at, new Vector(-20, -160));
        Assert.Equal(new DateTime(2026, 9, 1), month.Month);
        Assert.True(scroller.Offset.Y > 60);
    }

    [AvaloniaFact]
    public void Details_open_in_a_sheet_on_a_phone_and_beside_the_event_on_a_desktop()
    {
        var wide = Calendar();
        var w = Show(wide, width: 900, height: 800);
        Settle(200);
        wide.ShowDetails(wide.Events!.First());
        Settle();
        Assert.True(wide.DetailsCard!.IsOpen);
        Assert.Null(wide.DetailsSheet);
        wide.HideDetails();
        Settle();

        wide.DetailsPresentation = MonthDetailsPresentation.Sheet;
        wide.ShowDetails(wide.Events!.First());
        Settle();
        Assert.True(wide.DetailsSheet!.IsOpen);
        Assert.Contains(":sheet", wide.DetailsSheet.Classes);
        wide.HideDetails();
        Settle();
        Assert.False(wide.DetailsSheet.IsOpen);

        var month = Calendar(dir: "rtl");
        var root = OverlaysTests.SingleView(month, 360, 800);
        try
        {
            OverlaysTests.Pump(root, 300);
            Assert.True(month.IsCompact);
            var ev = month.Events!.First();
            var invoked = 0;
            month.EventInvoked += (_, _) => invoked++;
            Tap(root, BoxOf(month.ChipFor(ev)!, root).Center);
            OverlaysTests.Pump(root);
            Assert.Equal(1, invoked);
            var sheet = month.DetailsSheet!;
            Assert.True(sheet.IsOpen);
            Assert.True(sheet.IsSheet);
            Assert.False(month.DetailsCard?.IsOpen == true);
            Assert.Same(ev, ((Control)sheet.Content!).DataContext);
            Assert.Equal("Supplier visit at the warehouse · Bring the delivery notes", ((TextBlock)sheet.Content!).Text);
            Assert.Equal(FlowDirection.RightToLeft, sheet.FlowDirection);
            Assert.Equal(800, BoxOf(sheet, root).Bottom, 1);
            Assert.True(Back(root));
            OverlaysTests.Pump(root);
            Assert.False(sheet.IsOpen);
        }
        finally { root.StopRendering(); }
    }
    // A page that moves the calendar from its own bar drops the calendar's header; Go and View still drive it.
    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Without_its_header_the_calendar_starts_with_its_days_and_still_turns(string dir)
    {
        var month = Calendar(MonthCalendarView.Month, dir);
        month.NarrowLayout = MonthNarrowLayout.Compact;
        month.ShowHeader = false;
        var w = Show(month, Dir(dir), width: 360, height: 900);
        Settle(200);
        Assert.False(month.HeadRow!.IsVisible);
        var bare = BoxOf(month.Body!, w).Top - BoxOf(month, w).Top;
        Assert.True(bare < 8, $"the days start {bare} under the calendar's top");
        var turned = new List<DateTime>();
        month.MonthChanged += (_, first) => turned.Add(first);
        month.Go(1);
        Settle(400);
        Assert.Equal([new DateTime(2026, 10, 1)], turned);
        month.ShowHeader = true;
        Settle(200);
        Assert.True(month.HeadRow!.IsVisible);
        Assert.True(BoxOf(month.Body!, w).Top >= BoxOf(month.HeadRow, w).Bottom, "with its header the days start under it");
    }

    // A compact week's narrow column breaks a long title between whole words, cutting a too-long word with «…»; so does the desktop's week (its own wrapping broke «Informatio / n» letter by letter in a narrow column), its lines as wide as its column.
    [AvaloniaFact]
    public void A_compact_week_breaks_a_long_title_between_its_words_only()
    {
        var month = Calendar(MonthCalendarView.Week);
        month.NarrowLayout = MonthNarrowLayout.Compact;
        const string title = "Information System Security";
        month.Events = [new MonthEvent { Date = Day, Time = new TimeSpan(9, 0, 0), Duration = TimeSpan.FromHours(2), Title = title }];
        Show(month, FlowDirection.LeftToRight, width: 360, height: 900);
        Settle(200);
        var text = month.ChipFor(month.Events[0])!.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("week-event-title"));
        Assert.Equal(TextTrimming.CharacterEllipsis, text.TextTrimming);
        var lines = text.Text!.Split('\n');
        Assert.True(lines.Length >= 2, "the title takes more than one line");
        Assert.Equal(title, string.Join(' ', lines)); // whole words on each line, in their order
        Assert.All(text.TextLayout.TextLines, line => Assert.True(line.Width <= text.Bounds.Width + 0.5, $"a line is {line.Width} wide in {text.Bounds.Width}"));

        month.NarrowLayout = MonthNarrowLayout.Dots; // the desktop's week: whole words too, never a word broken inside itself
        Settle(200);
        var wide = month.ChipFor(month.Events[0])!.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("week-event-title"));
        Assert.Equal(TextTrimming.CharacterEllipsis, wide.TextTrimming);
        Assert.Equal(title, string.Join(' ', wide.Text!.Split('\n')));
        Assert.All(wide.TextLayout.TextLines, line => Assert.True(line.Width <= wide.Bounds.Width + 0.5, $"a line is {line.Width} wide in {wide.Bounds.Width}"));
    }
}
