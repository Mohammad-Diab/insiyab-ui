using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Under a finger the desktop's helpers stay quiet: no hover tip a tap left behind, no focus ring without a key, a phone's thin scrollbars that show only while scrolling; and a hidden bar is not the one the content frosts.
public class TouchQuietTests
{
    [AvaloniaFact]
    public void Under_a_finger_a_tip_opens_only_by_a_long_press_or_the_keyboard_and_a_mouse_keeps_its_hover_tip()
    {
        var button = new Button { Content = "Copy" };
        ToolTip.SetTip(button, "Copy the address");
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { button } });
        Touch.SetTouch(w, true);
        ToolTip.SetIsOpen(button, true);
        Settle(100);
        Assert.False(ToolTip.GetIsOpen(button));

        Touch.SetTouch(w, false);
        ToolTip.SetIsOpen(button, true);
        Settle(100);
        Assert.True(ToolTip.GetIsOpen(button));
        ToolTip.SetIsOpen(button, false);
    }

    [AvaloniaFact]
    public void Under_a_finger_a_focus_ring_shows_only_after_a_key()
    {
        var first = new Button { Content = "One" };
        var second = new Button { Content = "Two" };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { first, second } });
        Touch.SetTouch(w, true);
        first.Focus(NavigationMethod.Tab); // a sheet focusing its first control as it opens
        Settle(60);
        Assert.True(first.IsFocused);
        Assert.DoesNotContain(":focus-visible", first.Classes);

        w.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(60);
        Assert.True(second.IsFocused);
        Assert.Contains(":focus-visible", second.Classes);
    }

    [AvaloniaFact]
    public void A_mouse_keeps_the_focus_ring_of_a_tab_focus()
    {
        var first = new Button { Content = "One" };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { first } });
        Touch.SetTouch(w, false);
        first.Focus(NavigationMethod.Tab);
        Settle(60);
        Assert.Contains(":focus-visible", first.Classes);
    }

    [AvaloniaFact]
    public void On_a_phone_the_scrollbar_lies_over_the_content_and_shows_only_while_it_scrolls()
    {
        Os.Fake = OsKind.Android;
        try
        {
            var rows = new StackPanel();
            for (var i = 0; i < 40; i++) rows.Children.Add(new Border { Height = 44 });
            var scroller = new ScrollViewer { Content = rows };
            var w = Show(scroller, width: 360, height: 400);
            Touch.SetTouch(w, true);
            Settle(60);
            Assert.True(scroller.AllowAutoHide);
            var bar = scroller.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == global::Avalonia.Layout.Orientation.Vertical);
            var thumb = bar.GetVisualDescendants().OfType<Thumb>().First();
            Assert.False(bar.IsHitTestVisible);
            Assert.Equal(0, thumb.Opacity);

            scroller.Offset = new Vector(0, 120);
            Settle(100);
            Assert.Contains("ins-shown", bar.Classes);
            Settle((int)TouchScrollbars.Linger.TotalMilliseconds + 500);
            Assert.DoesNotContain("ins-shown", bar.Classes);
            Assert.Equal(0, thumb.Opacity, 2);

            // a mouse: the web's bar again
            Touch.SetTouch(w, false);
            Settle(60);
            Assert.False(scroller.AllowAutoHide);
            Assert.True(bar.IsHitTestVisible);
        }
        finally { Os.Fake = null; }
    }

    [AvaloniaFact]
    public void A_hidden_bar_is_skipped_and_the_shown_bar_around_it_frosts()
    {
        var inner = new Border { Classes = { "topbar" }, IsVisible = false, Height = 40 };
        var rows = new StackPanel();
        for (var i = 0; i < 40; i++) rows.Children.Add(new Border { Height = 44 });
        var scroller = new ScrollViewer { Content = rows };
        Shell.SetFrostsTopbar(scroller, true);
        var outer = new Border { Classes = { "topbar" }, Height = 40, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top };
        var page = new Panel { Children = { scroller, inner } };
        Show(new Panel { Children = { new ContentControl { Content = page }, outer } }, width: 360, height: 400);
        scroller.Offset = new Vector(0, 100);
        Settle(60);
        Assert.Contains("scrolled", outer.Classes);
        Assert.DoesNotContain("scrolled", inner.Classes);
    }
}

// The month calendar names its period for an app's own bar exactly as its header does.
public class MonthPeriodTitleTests
{
    [AvaloniaFact]
    public void The_period_title_is_the_headers_text_for_the_month_and_the_week()
    {
        var cal = new MonthCalendar { Locale = "ar", Date = new DateTime(2026, 11, 15), View = MonthCalendarView.Week };
        Show(cal, global::Avalonia.Media.FlowDirection.RightToLeft, width: 900, height: 600);
        Settle(100);
        string Head() => cal.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("month-title")).Text!;
        Assert.Equal(Head(), cal.PeriodTitle(MonthCalendarView.Week, new DateTime(2026, 11, 18)));
        Assert.Contains("14", Head());
        Assert.Contains("20", Head());
        cal.View = MonthCalendarView.Month;
        Settle(100);
        Assert.Equal(Head(), cal.PeriodTitle(MonthCalendarView.Month, new DateTime(2026, 11, 2)));
        // a week across two months names both
        Assert.Contains("–", cal.PeriodTitle(MonthCalendarView.Week, new DateTime(2026, 12, 1)));
    }

    // The desktop's narrow calendar names its day in a line of its own, as its hidden header would: without the weekday while narrow
    [AvaloniaFact]
    public void The_days_title_is_the_headers_text_wide_and_narrow()
    {
        var cal = new MonthCalendar { Locale = "ar", Date = new DateTime(2026, 11, 18), View = MonthCalendarView.Day };
        var window = Show(cal, global::Avalonia.Media.FlowDirection.RightToLeft, width: 900, height: 600);
        Settle(100);
        string Head() => cal.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("month-title")).Text!;
        var wide = cal.PeriodTitle(MonthCalendarView.Day, new DateTime(2026, 11, 18));
        Assert.Equal(Head(), wide);
        window.Width = 400;
        Settle(150);
        var narrow = cal.PeriodTitle(MonthCalendarView.Day, new DateTime(2026, 11, 18));
        Assert.Equal(Head(), narrow);
        Assert.True(narrow.Length < wide.Length, $"«{narrow}» still names the weekday («{wide}»)");
    }
}
