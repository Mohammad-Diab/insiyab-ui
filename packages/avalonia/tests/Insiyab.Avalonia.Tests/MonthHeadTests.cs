using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MonthHeadTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Button Tagged(MonthCalendar month, string tag) => month.GetVisualDescendants().OfType<Button>().Single(b => b.Tag as string == tag);

    private static MonthCalendar Week(string dir, double width) =>
        new() { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 21), Width = width, Locale = dir == "rtl" ? "ar" : "en" };

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Today_and_the_hours_lead_the_date_sits_in_the_middle_and_the_views_close_the_row(string dir)
    {
        var month = Week(dir, 960);
        var w = Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: 1000, height: 700);
        var head = month.HeadRow!;
        var (start, middle, end) = ((Panel)head.Children[0], (Panel)head.Children[1], (Panel)head.Children[2]);
        Assert.Equal(["today", "hours"], start.Children.Select(c => c.Tag as string));
        Assert.Equal("prev", middle.Children[0].Tag);
        Assert.Contains("month-title", middle.Children[1].Classes);
        Assert.Equal("next", middle.Children[2].Tag);
        Assert.IsType<Segmented>(Assert.Single(end.Children));
        Assert.False(head.IsWrapped);

        var row = Box(head, w);
        var s = Box(start, w);
        var m = Box(middle, w);
        var e = Box(end, w);
        if (dir == "rtl")
        {
            Assert.Equal(row.Right, s.Right, 1);
            Assert.Equal(row.Left, e.Left, 1);
        }
        else
        {
            Assert.Equal(row.Left, s.Left, 1);
            Assert.Equal(row.Right, e.Right, 1);
        }
        Assert.InRange(m.Center.X - row.Center.X, -1, 1);
        Assert.Equal(s.Center.Y, m.Center.Y, 2);
        var today = Box(Tagged(month, "today"), w);
        var hours = Box(Tagged(month, "hours"), w);
        Assert.Equal(14, dir == "rtl" ? today.Left - hours.Right : hours.Left - today.Right, 1);
        Capture(w, $"month-head-week-{dir}");

        month.View = MonthCalendarView.Month;
        Settle(200);
        Assert.False(Tagged(month, "hours").IsVisible);
        Assert.InRange(Box(middle, w).Center.X - Box(head, w).Center.X, -1, 1);
        Capture(w, $"month-head-month-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_narrow_head_gives_the_date_its_own_line_and_the_tab_order_follows(string dir)
    {
        var month = Week(dir, 470);
        var w = Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: 520, height: 700);
        var head = month.HeadRow!;
        var (start, middle, end) = ((Panel)head.Children[0], (Panel)head.Children[1], (Panel)head.Children[2]);
        Assert.True(head.IsWrapped);
        Assert.Equal(Box(start, w).Center.Y, Box(end, w).Center.Y, 1);
        Assert.True(Box(middle, w).Top >= Box(start, w).Bottom + 11);
        Assert.InRange(Box(middle, w).Center.X - Box(head, w).Center.X, -1, 1);
        Assert.Equal(0, KeyboardNavigation.GetTabIndex(start));
        Assert.Equal(1, KeyboardNavigation.GetTabIndex(end));
        Assert.Equal(2, KeyboardNavigation.GetTabIndex(middle));
        Capture(w, $"month-head-narrow-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void TitleStart_puts_the_date_first_and_every_control_at_the_end(string dir)
    {
        var month = Week(dir, 960);
        month.HeaderLayout = MonthHeaderLayout.TitleStart;
        var w = Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: 1000, height: 700);
        var head = month.HeadRow!;
        var (start, middle, end) = ((Panel)head.Children[0], (Panel)head.Children[1], (Panel)head.Children[2]);
        Assert.Contains("month-title", Assert.Single(start.Children).Classes);
        Assert.False(middle.IsVisible);
        Assert.Equal(["hours", null, "prev", "today", "next"], end.Children.Select(c => c.Tag as string));
        var row = Box(head, w);
        Assert.Equal(dir == "rtl" ? row.Left : row.Right, dir == "rtl" ? Box(end, w).Left : Box(end, w).Right, 1);
        Capture(w, $"month-head-title-start-{dir}");

        month.HeaderLayout = MonthHeaderLayout.Centered;
        Settle(200);
        Assert.Equal(["today", "hours"], start.Children.Select(c => c.Tag as string));
        Assert.True(middle.IsVisible);
    }

    [AvaloniaFact]
    public void The_work_hours_are_a_small_switch_with_their_words()
    {
        var month = Week("rtl", 960);
        Show(month, FlowDirection.RightToLeft, width: 1000, height: 700);
        var hours = Assert.IsType<ToggleSwitch>(Tagged(month, "hours"));
        Assert.Contains("sm", hours.Classes);
        Assert.Equal("ساعات الدوام", hours.Content);
        Assert.True(hours.IsChecked);
    }
}
