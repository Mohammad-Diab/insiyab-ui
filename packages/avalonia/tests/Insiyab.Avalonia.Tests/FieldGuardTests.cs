using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Typed text that a field cannot read, or typing into mixed Arabic text, must never take the app down.
[Collection("Pickers")]
public class FieldGuardTests
{
    private const string Huge = "99999999999999";

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static TextPresenter PresenterOf(TextBox box) => box.GetVisualDescendants().OfType<TextPresenter>().First();

    private static IEnumerable<string> Messages(Control c) =>
        (DataValidationErrors.GetErrors(c) ?? []).Select(e => e is Exception x ? x.Message : e?.ToString() ?? string.Empty);

    [Fact]
    public void Huge_numbers_are_read_as_invalid_not_thrown()
    {
        foreach (var cal in new[] { PickerCalendar.Gregorian, PickerCalendar.Hijri, PickerCalendar.HijriCivil })
            foreach (var locale in new[] { PickerLocale.Arabic, PickerLocale.English })
            {
                Assert.Null(PickerParse.ParseDate($"{Huge}/4/1448", cal, locale));
                Assert.Null(PickerParse.ParseDate($"12/{Huge}/1448", cal, locale));
                Assert.Null(PickerParse.ParseDate($"12/4/{Huge}", cal, locale));
                Assert.Null(PickerParse.ParseDate($"1448/4/{Huge}", cal, locale));
                Assert.Null(PickerParse.ParseDate("١٢/٤/٩٩٩٩٩٩٩٩٩٩٩٩", cal, locale));
                Assert.Null(PickerParse.ParseDate("১২/৪/১৪৪৮", cal, locale));
                Assert.Null(PickerParse.ParseDateTime($"12/4/1448 {Huge}:30", cal, locale).Day);
            }
        Assert.Null(PickerParse.ParseTime($"{Huge}:30"));
        Assert.Null(PickerParse.ParseTime($"2:{Huge}"));
        Assert.Null(BirthEngine.Understand($"{Huge} 9 2026", new DateTime(2026, 10, 11)));
        Assert.Null(BirthEngine.Understand($"22 يناير {Huge}", new DateTime(2026, 10, 11)));
        Assert.Null(ColorMath.ParseHex("#ffffffffffffff"));
        Assert.Null(ColorMath.ParseHex("+12345"));
        var brush = DataBrushes.Vertical.Convert([Colors.Red], typeof(IBrush), "x 1e999", CultureInfo.InvariantCulture);
        Assert.IsType<LinearGradientBrush>(brush);
    }

    [AvaloniaTheory]
    [InlineData(CalendarKind.Hijri, DatePickMode.Single)]
    [InlineData(CalendarKind.Gregorian, DatePickMode.Single)]
    [InlineData(CalendarKind.Hijri, DatePickMode.Range)]
    [InlineData(CalendarKind.Hijri, DatePickMode.DateTime)]
    public void Leaving_a_date_field_with_a_huge_number_marks_it_bad(CalendarKind kind, DatePickMode mode)
    {
        var field = new DateField { CalendarKind = kind, Mode = mode };
        var other = new TextBox();
        Show(Page(field, other), FlowDirection.RightToLeft);
        field.Focus();
        Settle(100);
        field.Text = $"12/4/{Huge}";
        other.Focus();
        Settle(100);
        Assert.Null(field.SelectedDate);
        Assert.Contains(PickerLocale.Arabic.BadDate, Messages(field));
    }

    [AvaloniaFact]
    public void Leaving_a_time_field_with_a_huge_number_marks_it_bad()
    {
        var field = new TimeField();
        var other = new TextBox();
        Show(Page(field, other), FlowDirection.RightToLeft);
        field.Focus();
        Settle(100);
        field.Text = $"{Huge}:30";
        other.Focus();
        Settle(100);
        Assert.Null(field.SelectedTime);
        Assert.Contains(PickerLocale.Arabic.BadTime, Messages(field));
    }

    [AvaloniaFact]
    public void Typing_stops_each_number_at_four_digits()
    {
        var date = new DateField { CalendarKind = CalendarKind.Hijri };
        var time = new TimeField();
        var w = Show(Page(date, time), FlowDirection.RightToLeft);
        date.Focus();
        Settle(100);
        foreach (var c in "12/4/14481234567") w.KeyTextInput(c.ToString());
        Assert.Equal("12/4/1448", date.Text);
        time.Focus();
        Settle(100);
        foreach (var c in "١٤٣٠٩٩٩") w.KeyTextInput(c.ToString());
        Assert.Equal("١٤٣٠", time.Text);
    }

    [AvaloniaFact]
    public void Huge_numbers_typed_into_birth_colour_and_phone_fields_never_throw()
    {
        var birth = new BirthDateField();
        var colour = new ColorField();
        var phone = new PhoneBox();
        var other = new TextBox();
        var w = Show(Page(birth, colour, phone, other), FlowDirection.RightToLeft, height: 700);
        birth.DayBox.Focus();
        Settle(100);
        w.KeyTextInput(Huge + Huge);
        birth.Paste($"{Huge}/{Huge}/{Huge}");
        foreach (var input in new[] { Part<TextBox>(colour, "PART_Input"), Part<TextBox>(phone, "PART_Input") })
        {
            input.Focus();
            Settle(100);
            w.KeyTextInput(Huge + Huge + Huge);
            other.Focus();
            Settle(100);
        }
        Assert.Null(colour.Value);
    }

    // Avalonia 12.1 throws hit-testing a one-letter run of mixed Arabic text, and a selection brush for the text split the line into such runs on every key.
    [AvaloniaTheory]
    [InlineData("date")]
    [InlineData("box")]
    public void Typing_anywhere_in_mixed_Arabic_text_never_throws(string kind)
    {
        TextBox field = kind == "date"
            ? new DateField { CalendarKind = CalendarKind.Hijri, SelectedDate = new DateTime(2026, 9, 24) }
            : new TextBox { Text = "13 ربيع الآخر 1448 هـ" };
        var w = Show(Page(field, new TextBox()), FlowDirection.RightToLeft);
        field.Focus(NavigationMethod.Tab);
        Settle(100);
        var start = field.Text!;
        var failures = new List<string>();
        foreach (var seq in new[] { "1", "١", "/", " ", "ع" })
            for (var i = 0; i <= start.Length; i++)
                for (var j = i; j <= Math.Min(start.Length, i + 2); j++)
                {
                    try
                    {
                        field.Text = start;
                        field.SelectionStart = i;
                        field.SelectionEnd = j;
                        foreach (var c in seq) w.KeyTextInput(c.ToString());
                    }
                    catch (InvalidOperationException e) { failures.Add($"'{seq}' at {i}-{j}: {e.Message}"); }
                }
        Assert.Empty(failures);
    }

    // Where the caret would stand before each letter, from the presenter's left; a text presenter undoes its parents' mirroring, so these are screen order.
    private static double[] Edges(TextBox box)
    {
        var p = PresenterOf(box);
        return [.. Enumerable.Range(0, (box.Text ?? string.Empty).Length + 1).Select(i => p.TextLayout.HitTestTextPosition(i).X)];
    }

    [AvaloniaTheory]
    [InlineData("rtl", CalendarKind.Hijri)]
    [InlineData("rtl", CalendarKind.Gregorian)]
    [InlineData("ltr", CalendarKind.Hijri)]
    [InlineData("ltr", CalendarKind.Gregorian)]
    public void Typed_dates_read_left_to_right_with_the_caret_after_the_last_digit(string dir, CalendarKind kind)
    {
        var field = new DateField { CalendarKind = kind };
        var w = Show(Page(field), Dir(dir));
        field.Focus();
        Settle(100);
        var p = PresenterOf(field);
        foreach (var c in "12/4/1448")
        {
            w.KeyTextInput(c.ToString());
            Settle(30);
            var x = Edges(field);
            for (var i = 1; i < x.Length; i++) Assert.True(x[i] > x[i - 1], $"'{field.Text}': letter {i} sits at {x[i]}, left of {x[i - 1]}");
            Assert.Equal(x[^1], x[field.CaretIndex]);
            if (dir == "rtl") Assert.Equal(p.Bounds.Width, x[^1], 1);
            else Assert.Equal(0, x[0], 1);
        }
        field.SelectedDate = new DateTime(2026, 9, 24);
        Settle(30);
        Assert.Equal(dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, p.FlowDirection);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Typed_times_read_left_to_right_with_the_caret_after_the_last_digit(string dir)
    {
        var field = new TimeField();
        var w = Show(Page(field), Dir(dir));
        field.Focus();
        Settle(100);
        foreach (var c in "14:30")
        {
            w.KeyTextInput(c.ToString());
            Settle(30);
            var x = Edges(field);
            for (var i = 1; i < x.Length; i++) Assert.True(x[i] > x[i - 1], $"'{field.Text}': letter {i} sits at {x[i]}, left of {x[i - 1]}");
            Assert.Equal(x[^1], x[field.CaretIndex]);
        }
    }
}
