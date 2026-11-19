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
        Assert.Equal(["today"], start.Children.Select(c => c.Tag as string));
        Assert.Equal(["hours"], ((Panel)head.Children[3]).Children.Select(c => c.Tag as string));
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
    public void A_narrow_head_puts_the_hours_and_the_views_first_and_today_with_the_date_under_them(string dir)
    {
        var month = Week(dir, 470);
        var w = Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: 520, height: 700);
        var head = month.HeadRow!;
        var (start, middle, end, aside) = ((Panel)head.Children[0], (Panel)head.Children[1], (Panel)head.Children[2], (Panel)head.Children[3]);
        Assert.True(head.IsWrapped);
        Assert.False(head.IsStacked);
        var row = Box(head, w);
        // line 1, as a phone's head and Google Calendar group it: what is shown first, the hours and the views at the end
        Assert.Equal(Box(aside, w).Center.Y, Box(end, w).Center.Y, 1);
        Assert.Equal(row.Top, Box(end, w).Top, 1);
        Assert.Equal(dir == "rtl" ? row.Left : row.Right, dir == "rtl" ? Box(end, w).Left : Box(end, w).Right, 1);
        Assert.True(dir == "rtl" ? Box(aside, w).Left >= Box(end, w).Right + 11 : Box(aside, w).Right <= Box(end, w).Left - 11);
        // line 2: Today, then the date between its arrows (in the row's middle while it clears Today)
        Assert.Equal(Box(start, w).Center.Y, Box(middle, w).Center.Y, 1);
        Assert.True(Box(start, w).Top >= Box(end, w).Bottom + 11);
        Assert.Equal(dir == "rtl" ? row.Right : row.Left, dir == "rtl" ? Box(start, w).Right : Box(start, w).Left, 1);
        Assert.True(dir == "rtl" ? Box(middle, w).Right <= Box(start, w).Left - 11 : Box(middle, w).Left >= Box(start, w).Right + 11);
        Assert.Equal(1, KeyboardNavigation.GetTabIndex(aside));
        Assert.Equal(2, KeyboardNavigation.GetTabIndex(end));
        Assert.Equal(3, KeyboardNavigation.GetTabIndex(start));
        Assert.Equal(4, KeyboardNavigation.GetTabIndex(middle));
        Capture(w, $"month-head-narrow-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void An_apps_lead_action_comes_first_and_opens_the_first_line_when_the_head_wraps(string dir)
    {
        var month = Week(dir, 960);
        var w = Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: 1000, height: 700);
        var head = month.HeadRow!;
        var lead = (Panel)head.Children[4];
        Assert.Contains("month-lead", lead.Classes);
        Assert.Empty(lead.Children);
        var add = new Button { Content = "Add event", Classes = { "primary", "sm" } };
        lead.Children.Add(add);
        Settle(100);
        var row = Box(head, w);
        var today = Box(Tagged(month, "today"), w);
        Assert.False(head.IsWrapped);
        Assert.Equal(dir == "rtl" ? row.Right : row.Left, dir == "rtl" ? Box(add, w).Right : Box(add, w).Left, 1);
        Assert.Equal(12, dir == "rtl" ? Box(add, w).Left - today.Right : today.Left - Box(add, w).Right, 1);
        Assert.Equal(0, KeyboardNavigation.GetTabIndex(lead));

        month.Width = 560;
        Settle(100);
        Assert.True(head.IsWrapped);
        var end = Box(head.Children[2], w);
        Assert.Equal(Box(add, w).Center.Y, end.Center.Y, 1);
        Assert.True(Box(Tagged(month, "today"), w).Top >= end.Bottom + 11);
        Capture(w, $"month-head-lead-{dir}");
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
        Assert.Equal(["today"], start.Children.Select(c => c.Tag as string));
        Assert.Equal(["hours"], ((Panel)head.Children[3]).Children.Select(c => c.Tag as string));
        Assert.True(middle.IsVisible);
    }

    [AvaloniaTheory]
    [InlineData("rtl", 760)]
    [InlineData("ltr", 760)]
    [InlineData("rtl", 1200)]
    public void Paging_keeps_the_dates_width_and_the_heads_form_a_long_week_wraps_inside_it(string dir, double width)
    {
        // a week inside one month, then one across two months («31 تشرين الأول – 6 تشرين الثاني 2026»), then one inside a month again
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 10, 21), Width = width, Locale = dir == "rtl" ? "ar" : "en" };
        Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: width + 40, height: 700);
        var head = month.HeadRow!;
        var title = head.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("month-title"));
        var (wrapped, titleWidth, prev) = (head.IsWrapped, title.Bounds.Width, Box(Tagged(month, "prev"), month).X);
        foreach (var day in new[] { new DateTime(2026, 11, 2), new DateTime(2026, 11, 11), new DateTime(2026, 11, 30) })
        {
            month.Date = day;
            Settle(50);
            Assert.Equal(wrapped, head.IsWrapped);                                // the head's form follows the row's width only
            Assert.Equal(titleWidth, title.Bounds.Width, 0.5);                    // the date keeps one width
            Assert.Equal(prev, Box(Tagged(month, "prev"), month).X, 0.5);         // so the arrows never move
        }
        month.Date = new DateTime(2026, 9, 30);                                   // a week from September into October, whatever its first day
        Settle(50);
        Assert.Equal(titleWidth, title.Bounds.Width, 0.5);                        // a week across two months keeps the width too (wrapping inside it when longer)
        Assert.Equal(wrapped, head.IsWrapped);
    }

    [AvaloniaTheory]
    [InlineData("rtl", 840)]
    [InlineData("rtl", 1280)]
    [InlineData("ltr", 840)]
    public void Capture_paging_over_a_week_across_two_months(string dir, double width)
    {
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 23), Width = width - 40, Locale = dir == "rtl" ? "ar" : "en", ShowHoursSwitch = true };
        var w = Show(new Panel { Margin = new Thickness(20), Children = { month } }, Dir(dir), width: width, height: 260);
        var k = 0;
        foreach (var day in new[] { new DateTime(2026, 9, 23), new DateTime(2026, 9, 30), new DateTime(2026, 10, 7) })
        {
            month.Date = day;
            Capture(w, $"month-paging-{dir}-{width}-{k++}");
        }
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
