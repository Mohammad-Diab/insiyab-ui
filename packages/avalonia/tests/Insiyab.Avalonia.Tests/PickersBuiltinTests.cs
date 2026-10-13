using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The themes of Avalonia's own picker types: Calendar, CalendarDatePicker, DatePicker and TimePicker look like Insiyab.
[Collection("Pickers")]
public class PickersBuiltinTests
{
    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    // A pop-up keeps 40px of clear room round its card, so a field near the window edge makes it slide; this page keeps the field clear of that.
    private static StackPanel Inset(Control child)
    {
        var page = new StackPanel { Margin = new Thickness(60, 20), Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.Add(child);
        return page;
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Physical(Control c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    // Avalonia gives each day button the date as its data context; the day number is the way round it.
    private static CalendarDayButton Day(Calendar calendar, DateTime day)
    {
        var buttons = calendar.GetVisualDescendants().OfType<CalendarDayButton>().ToList();
        var hit = buttons.FirstOrDefault(b => b.DataContext is DateTime d && d.Date == day.Date && !b.Classes.Contains(":inactive"));
        return hit ?? buttons.First(b => !b.Classes.Contains(":inactive") && b.Content?.ToString() == day.Day.ToString());
    }

    [AvaloniaFact]
    public void Builtin_Calendar_has_the_card_look()
    {
        var t = DateTime.Today;
        var pick = t.Day == 1 ? t.AddDays(1) : t.AddDays(-1);
        var calendar = new Calendar { SelectedDate = pick, DisplayDate = t };
        Show(Page(calendar));

        var chosen = Day(calendar, pick);
        Assert.IsType<LinearGradientBrush>(Part<Border>(chosen, "PART_Face").Background);
        var today = Day(calendar, t);
        Assert.Contains(":today", today.Classes);
        AssertBrush(calendar, "InsToneBrandBd", () => Part<Border>(today, "PART_Face").BorderBrush);
        AssertBrush(calendar, "InsCtlBg", () => Part<Button>(calendar, "PART_PreviousButton").Background);
        Assert.Equal(38, chosen.Bounds.Height);
        Assert.Equal(Res(calendar, "InsRLg"), Part<CalendarItem>(calendar, "PART_CalendarItem").CornerRadius);
    }

    [AvaloniaTheory]
    [InlineData("ltr")]
    [InlineData("rtl")]
    public void Builtin_CalendarDatePicker_is_a_field_with_a_calendar(string dir)
    {
        var picker = new CalendarDatePicker { SelectedDate = new DateTime(2026, 9, 15), Width = 300 };
        var w = Show(Inset(picker), Dir(dir), height: 700);

        var box = Part<TextBox>(picker, "PART_TextBox");
        Assert.Equal(46, box.Bounds.Height);
        AssertBrush(picker, "InsCtlBg", () => Part<Border>(box, "PART_BorderElement").Background);

        Click(Part<Button>(picker, "PART_Button"), w);
        Assert.True(picker.IsDropDownOpen);
        var calendar = Part<Calendar>(w, "PART_Calendar");
        Assert.True(calendar.IsVisible);
        var field = Physical(box, w);
        var card = Physical(calendar, w);
        if (dir == "ltr") Assert.InRange(card.Left, field.Left - 2, field.Left + 2);
        else Assert.InRange(card.Right, field.Right - 2, field.Right + 2);

        Click(Day(calendar, new DateTime(2026, 9, 20)), w);
        Assert.Equal(new DateTime(2026, 9, 20), picker.SelectedDate);
    }

    private static ListBoxItem[] Wheel(Control presenter) => [.. presenter.GetVisualDescendants().OfType<ListBoxItem>()];

    [AvaloniaFact]
    public void Builtin_DatePicker_has_the_field_face_and_drum()
    {
        var picker = new DatePicker { SelectedDate = new DateTimeOffset(new DateTime(2026, 9, 30)) };
        var w = Show(Page(picker), height: 700);

        var button = Part<Button>(picker, "PART_FlyoutButton");
        Assert.Equal(46, button.Bounds.Height);
        AssertBrush(picker, "InsCtlBg", () => Part<Border>(button, "PART_Face").Background);

        Click(button, w);
        var presenter = Assert.IsType<DatePickerPresenter>(Part<Popup>(picker, "PART_Popup").Child);
        var items = Wheel(presenter);
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.Equal(36, i.Bounds.Height));
        var chosen = items.First(i => i.Classes.Contains(":selected"));
        AssertBrush(picker, "InsToneBrand", () => chosen.Foreground);
        AssertBrush(picker, "InsToneBrandBg", () => Part<Border>(presenter, "HighlightRect").Background);
    }

    [AvaloniaFact]
    public void Builtin_TimePicker_has_the_field_face_and_drum()
    {
        var picker = new TimePicker { SelectedTime = new TimeSpan(9, 30, 0) };
        var w = Show(Page(picker), height: 700);

        var button = Part<Button>(picker, "PART_FlyoutButton");
        Assert.Equal(46, button.Bounds.Height);
        AssertBrush(picker, "InsCtlBg", () => Part<Border>(button, "PART_Face").Background);

        Click(button, w);
        var presenter = Assert.IsType<TimePickerPresenter>(Part<Popup>(picker, "PART_Popup").Child);
        var items = Wheel(presenter);
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.Equal(36, i.Bounds.Height));
        var chosen = items.First(i => i.Classes.Contains(":selected"));
        AssertBrush(picker, "InsToneBrand", () => chosen.Foreground);
        AssertBrush(picker, "InsToneBrandBg", () => Part<Border>(presenter, "HighlightRect").Background);
    }
}
