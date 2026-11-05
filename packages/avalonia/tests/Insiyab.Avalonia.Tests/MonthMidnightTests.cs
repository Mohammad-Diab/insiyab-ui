using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MonthMidnightTests
{
    private static List<Border> Chips(MonthCalendar month, string title) =>
        month.GetVisualDescendants().OfType<Border>()
            .Where(b => b.Classes.Contains("month-event") && b.Classes.Contains("timed") && ToolTip.GetTip(b) as string == title)
            .ToList();

    [AvaloniaFact]
    public void An_event_past_midnight_goes_on_into_the_next_day()
    {
        var ev = new MonthEvent { Date = new DateTime(2026, 3, 3), Title = "مناوبة ليلية", Time = TimeSpan.FromHours(22), Duration = TimeSpan.FromHours(5) };
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 3, 4), WorkHoursOnly = false, Events = new List<MonthEvent> { ev } };
        Show(month, FlowDirection.RightToLeft, null, 1000, 800);
        var chips = Chips(month, "مناوبة ليلية");
        Assert.Equal(2, chips.Count);
        var cutEnd = Assert.Single(chips, c => c.Classes.Contains("cut-end") && !c.Classes.Contains("cut-start"));
        var cutStart = Assert.Single(chips, c => c.Classes.Contains("cut-start") && !c.Classes.Contains("cut-end"));
        Border Day(Border chip) => chip.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("week-day"));
        Assert.NotSame(Day(cutEnd), Day(cutStart));
        Assert.Same(month.ChipFor(ev), cutEnd);
    }

    [AvaloniaFact]
    public void The_day_columns_keep_their_width_whether_the_hours_scroll()
    {
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 3, 4), DayStartHour = 8, DayEndHour = 16, WorkHoursOnly = true };
        Show(month, FlowDirection.RightToLeft, null, 1000, 800);
        double Width() => month.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("week-day")).Bounds.Width;
        var before = Width();
        month.WorkHoursOnly = false;
        Settle();
        Assert.Equal(before, Width(), 0.5);
        Assert.True(month.HoursScroller!.AllowAutoHide);
    }

    [AvaloniaFact]
    public void ChipFor_finds_the_chip_drawn_for_an_event()
    {
        var shown = new MonthEvent { Date = new DateTime(2026, 3, 10), Title = "اجتماع الفريق", Time = TimeSpan.FromHours(10) };
        var elsewhere = new MonthEvent { Date = new DateTime(2026, 5, 1), Title = "مراجعة", Time = TimeSpan.FromHours(9) };
        var month = new MonthCalendar { Month = new DateTime(2026, 3, 1), Events = new List<MonthEvent> { shown, elsewhere } };
        Show(month, FlowDirection.RightToLeft, null, 1000, 800);
        var chip = Assert.IsAssignableFrom<Border>(month.ChipFor(shown));
        Assert.Contains("month-event", chip.Classes);
        Assert.Equal("اجتماع الفريق", ToolTip.GetTip(chip));
        Assert.Null(month.ChipFor(elsewhere));
    }
}
