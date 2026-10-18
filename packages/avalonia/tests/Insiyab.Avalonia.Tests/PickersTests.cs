using Avalonia;
using Avalonia.Automation;
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

[Collection("Pickers")]
public class PickersTests : IDisposable
{
    private static readonly Func<DateTime> FixedClock = () => new DateTime(2026, 9, 15);

    public PickersTests() => DateCalendar.Today = FixedClock;

    public void Dispose()
    {
        if (ReferenceEquals(DateCalendar.Today, FixedClock)) DateCalendar.Today = () => DateTime.Today;
    }

    private static readonly DateTime Sep30 = new(2026, 9, 30);

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    private static StackPanel Spread(Control child)
    {
        var page = new StackPanel { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.Add(child);
        return page;
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Theme(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static void Press(Window w, Key key, RawInputModifiers mods = RawInputModifiers.None, int settle = 150)
    {
        w.KeyPress(key, mods, PhysicalKey.None, null);
        w.KeyRelease(key, mods, PhysicalKey.None, null);
        Settle(settle);
    }

    private static void Typed(Window w, string text)
    {
        w.KeyTextInput(text);
        Settle(100);
    }

    private static object? Focused(Window w) => w.FocusManager?.GetFocusedElement();

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Physical(Control c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static void ClickWith(Control control, Window window, RawInputModifiers mods)
    {
        var p = Center(control, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left, mods);
        window.MouseUp(p, MouseButton.Left, mods);
        Settle();
    }

    private static IEnumerable<string> Messages(Control c) =>
        (DataValidationErrors.GetErrors(c) ?? []).Select(e => e is Exception x ? x.Message : e?.ToString() ?? string.Empty);

    private static Button Day(DateField field, DateTime day) => field.Calendar.DayButtons[day];

    private static Button Foot(DateField field, string cls) =>
        field.Calendar.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains(cls));

    private static DateField Opened(DateField field, Window w)
    {
        Click(field.CalendarButton, w);
        Assert.True(field.Flyout.IsOpen);
        return field;
    }

    // ---------------------------------------------------------------- pure logic

    [Fact]
    public void Hijri_systems_initialise_and_convert()
    {
        Assert.Equal(new DayParts(1448, 3, 13), PickerCalendar.Hijri.Parts(new DateTime(2026, 9, 24)));
        Assert.Equal(new DayParts(1448, 3, 11), PickerCalendar.HijriCivil.Parts(new DateTime(2026, 9, 24)));
        Assert.Equal(new DayParts(1413, 6, 29), PickerCalendar.Hijri.Parts(new DateTime(1993, 1, 22)));
        Assert.Equal(new DateTime(2026, 9, 24), PickerCalendar.Hijri.FromParts(1448, 3, 13));
        Assert.Equal(28, PickerCalendar.Gregorian.MonthLength(2026, 1));
    }

    [Fact]
    public void Typed_dates_and_times_are_read()
    {
        var greg = PickerCalendar.Gregorian;
        var hijri = PickerCalendar.Hijri;
        var ar = PickerLocale.Arabic;
        Assert.Equal(Sep30, PickerParse.ParseDate("30/9/2026", greg, ar));
        Assert.Equal(Sep30, PickerParse.ParseDate("9/30/2026", greg, PickerLocale.English));
        Assert.Equal(Sep30, PickerParse.ParseDate("2026-09-30", greg, ar));
        Assert.Equal(Sep30, PickerParse.ParseDate("٣٠/٩/٢٠٢٦", greg, ar));
        var h = new DateTime(2026, 9, 24);
        Assert.Equal(h, PickerParse.ParseDate("13/4/1448", hijri, ar));
        Assert.Equal(h, PickerParse.ParseDate("13/4/48", hijri, ar));
        Assert.Equal(h, PickerParse.ParseDate("24/9/2026", hijri, ar));
        Assert.Null(PickerParse.ParseDate("31/2/2026", greg, ar));

        var half = new TimeSpan(14, 30, 0);
        Assert.Equal(half, PickerParse.ParseTime("14:30"));
        Assert.Equal(half, PickerParse.ParseTime("1430"));
        Assert.Equal(half, PickerParse.ParseTime("2:30 م"));
        Assert.Equal(new TimeSpan(14, 0, 0), PickerParse.ParseTime("2 pm"));
        Assert.Null(PickerParse.ParseTime("25:00"));
    }

    [Fact]
    public void Locale_formats_match_intl()
    {
        var greg = PickerCalendar.Gregorian;
        var ar = PickerLocale.Arabic;
        var en = PickerLocale.English;
        Assert.Equal("30 سبتمبر 2026", ar.FormatDay(greg, Sep30));
        Assert.Equal("September 30, 2026", en.FormatDay(greg, Sep30));
        Assert.Equal("16–23 سبتمبر 2026", ar.FormatRange(greg, new DateTime(2026, 9, 16), new DateTime(2026, 9, 23)));
        Assert.Equal("September 16 – 23, 2026", en.FormatRange(greg, new DateTime(2026, 9, 16), new DateTime(2026, 9, 23)));
        Assert.Equal("9:30 ص", ar.FormatTime(new TimeSpan(9, 30, 0)));
        Assert.Equal("9:30 AM", en.FormatTime(new TimeSpan(9, 30, 0)));
        Assert.Equal("13 ربيع الآخر 1448 هـ", ar.FormatDay(PickerCalendar.Hijri, new DateTime(2026, 9, 24)));
    }


    // ---------------------------------------------------------------- date field

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void DateField_writes_the_page_language(string dir)
    {
        var field = new DateField { SelectedDate = Sep30 };
        Show(Page(field), Dir(dir));
        Assert.Equal(dir == "rtl" ? "30 سبتمبر 2026" : "September 30, 2026", field.Text);
        Assert.Equal("2026-09-30", field.Value);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void DateField_has_the_field_look_and_a_button_at_its_end(string dir)
    {
        var field = new DateField { SelectedDate = Sep30 };
        var w = Show(Page(field), Dir(dir));
        Assert.Equal(46, field.Bounds.Height);
        AssertBrush(field, "InsCtlBg", () => Part<Border>(field, "PART_BorderElement").Background);
        Assert.Equal(36, field.CalendarButton.Bounds.Width);
        Assert.Equal(36, field.CalendarButton.Bounds.Height);
        var button = Physical(field.CalendarButton, w);
        var box = Physical(field, w);
        if (dir == "rtl") Assert.True(button.Center.X < box.Center.X);
        else Assert.True(button.Center.X > box.Center.X);
    }

    [AvaloniaFact]
    public void Typing_commits_on_Enter()
    {
        var field = new DateField { SelectedDate = Sep30 };
        var changes = new List<string>();
        field.DateChanged += (_, e) => changes.Add(e.Value);
        var w = Show(Page(field), FlowDirection.RightToLeft);

        field.Focus();
        field.SelectAll();
        Typed(w, "1/10/2026");
        Press(w, Key.Enter);
        Assert.Equal(new DateTime(2026, 10, 1), field.SelectedDate);
        Assert.Equal(["2026-10-01"], changes);
        Assert.Equal("1 أكتوبر 2026", field.Text);

        field.SelectAll();
        Typed(w, "abc");
        Press(w, Key.Enter);
        Assert.True(DataValidationErrors.GetHasErrors(field));
        Assert.Contains("اكتب تاريخًا صحيحًا.", Messages(field));
    }

    [AvaloniaFact]
    public void Out_of_bounds_dates_are_refused_with_a_message()
    {
        var field = new DateField { SelectedDate = Sep30, MinDate = new DateTime(2026, 9, 1) };
        var w = Show(Page(field), FlowDirection.RightToLeft);

        field.Focus();
        field.SelectAll();
        Typed(w, "15/8/2026");
        Press(w, Key.Enter);
        Assert.Contains("اختر 1 سبتمبر 2026 أو ما بعده.", Messages(field));
        Assert.Equal(Sep30, field.SelectedDate);
    }

    [AvaloniaFact]
    public void Up_and_Down_nudge_the_day()
    {
        var field = new DateField { SelectedDate = Sep30 };
        var w = Show(Page(field), FlowDirection.RightToLeft);
        field.Focus();

        Press(w, Key.Up);
        Assert.Equal(new DateTime(2026, 10, 1), field.SelectedDate);
        Press(w, Key.Down);
        Press(w, Key.Down);
        Assert.Equal(new DateTime(2026, 9, 29), field.SelectedDate);
        Assert.False(field.Flyout.IsOpen);

        field.MaxDate = new DateTime(2026, 9, 29);
        Press(w, Key.Up);
        Assert.Equal(new DateTime(2026, 9, 29), field.SelectedDate);

        field.SetDate(null);
        Press(w, Key.Up);
        Assert.Equal(new DateTime(2026, 9, 15), field.SelectedDate);
        Assert.False(field.Flyout.IsOpen);

        var range = new DateField { Mode = DatePickMode.Range, SelectedDate = Sep30 };
        var w2 = Show(Page(range), FlowDirection.RightToLeft);
        range.Focus();
        Press(w2, Key.Up);
        Assert.Equal(Sep30, range.SelectedDate);
        Assert.False(range.Flyout.IsOpen);
    }

    [AvaloniaFact]
    public void Alt_Down_opens_on_the_chosen_day_and_Esc_returns()
    {
        var field = new DateField { SelectedDate = Sep30 };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        field.Focus();

        Press(w, Key.Down, RawInputModifiers.Alt, 300);
        Assert.True(field.Flyout.IsOpen);
        var focused = Assert.IsType<Button>(Focused(w));
        Assert.Equal(Sep30, focused.Tag);

        Press(w, Key.Escape, RawInputModifiers.None, 300);
        Assert.False(field.Flyout.IsOpen);
        Assert.True(field.IsFocused);

        Press(w, Key.F4, RawInputModifiers.None, 300);
        Assert.True(field.Flyout.IsOpen);
    }

    [AvaloniaFact]
    public void The_button_opens_and_a_second_click_closes()
    {
        var field = new DateField { SelectedDate = Sep30 };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);

        Click(field.CalendarButton, w);
        Assert.True(field.Flyout.IsOpen);
        Click(field.CalendarButton, w);
        Assert.False(field.Flyout.IsOpen);

        Click(field, w);
        Assert.False(field.Flyout.IsOpen);
    }

    [AvaloniaFact]
    public void Clicking_a_day_picks_it()
    {
        var field = new DateField { SelectedDate = Sep30 };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        Opened(field, w);

        Click(Day(field, new DateTime(2026, 9, 20)), w);
        Assert.False(field.Flyout.IsOpen);
        Assert.Equal(new DateTime(2026, 9, 20), field.SelectedDate);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Calendar_keys(string dir)
    {
        var start = new DateTime(2026, 9, 16);
        var field = new DateField { SelectedDate = start };
        var w = Show(Page(field), Dir(dir), width: 800, height: 700);
        field.Focus();
        Press(w, Key.Down, RawInputModifiers.Alt, 300);
        var cal = field.Calendar;
        Assert.Equal(start, cal.FocusDate);

        void From(Key key, RawInputModifiers mods, DateTime expected)
        {
            cal.MoveTo(start);
            Settle(150);
            Press(w, key, mods, 250);
            Assert.Equal(expected, cal.FocusDate);
        }

        From(Key.Right, RawInputModifiers.None, dir == "rtl" ? new DateTime(2026, 9, 15) : new DateTime(2026, 9, 17));
        From(Key.Left, RawInputModifiers.None, dir == "rtl" ? new DateTime(2026, 9, 17) : new DateTime(2026, 9, 15));
        From(Key.Down, RawInputModifiers.None, new DateTime(2026, 9, 23));
        From(Key.Up, RawInputModifiers.None, new DateTime(2026, 9, 9));
        From(Key.Home, RawInputModifiers.None, dir == "rtl" ? new DateTime(2026, 9, 12) : new DateTime(2026, 9, 13));
        From(Key.End, RawInputModifiers.None, dir == "rtl" ? new DateTime(2026, 9, 18) : new DateTime(2026, 9, 19));
        From(Key.PageDown, RawInputModifiers.None, new DateTime(2026, 10, 16));
        Assert.Equal(new DateTime(2026, 10, 1), cal.DisplayMonth);
        From(Key.PageUp, RawInputModifiers.None, new DateTime(2026, 8, 16));
        From(Key.PageDown, RawInputModifiers.Control, new DateTime(2027, 9, 16));
        From(Key.PageUp, RawInputModifiers.Shift, new DateTime(2025, 9, 16));
        From(Key.Home, RawInputModifiers.Control, new DateTime(2026, 9, 1));
        From(Key.End, RawInputModifiers.Control, new DateTime(2026, 9, 30));
    }

    [AvaloniaFact]
    public void Enter_on_a_focused_day_picks_it()
    {
        var field = new DateField { SelectedDate = new DateTime(2026, 9, 16) };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        field.Focus();

        Press(w, Key.Down, RawInputModifiers.Alt, 300);
        Press(w, Key.Right, RawInputModifiers.None, 250);
        Press(w, Key.Enter, RawInputModifiers.None, 300);
        Assert.Equal(new DateTime(2026, 9, 15), field.SelectedDate);
        Assert.False(field.Flyout.IsOpen);
    }

    [AvaloniaFact]
    public void Wheel_over_the_grid_turns_the_month()
    {
        var field = new DateField { SelectedDate = Sep30 };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        field.Open(false);
        Settle();
        var cal = field.Calendar;
        Assert.Equal(new DateTime(2026, 9, 1), cal.DisplayMonth);

        w.MouseWheel(Center(cal.DayButtons[new DateTime(2026, 9, 10)], w), new Vector(0, -1));
        Settle(250);
        Assert.Equal(new DateTime(2026, 10, 1), cal.DisplayMonth);

        w.MouseWheel(Center(cal.DayButtons[new DateTime(2026, 10, 10)], w), new Vector(0, 1));
        Settle(250);
        Assert.Equal(new DateTime(2026, 9, 1), cal.DisplayMonth);

        var grid = cal.GetVisualDescendants().OfType<Grid>().First(g => g.Classes.Contains("cal-grid") && g.IsEffectivelyVisible);
        var gap = grid.TranslatePoint(new Point(1, 1), w)!.Value;
        Assert.Same(grid, w.InputHitTest(gap));
        w.MouseWheel(gap, new Vector(0, -1));
        Settle(250);
        Assert.Equal(new DateTime(2026, 10, 1), cal.DisplayMonth);
    }

    [AvaloniaFact]
    public void Range_picks_in_two_clicks_and_Shift_moves_an_end()
    {
        var field = new DateField { Mode = DatePickMode.Range };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        Opened(field, w);

        Click(Day(field, new DateTime(2026, 9, 16)), w);
        Assert.True(field.Flyout.IsOpen);
        w.MouseMove(Center(Day(field, new DateTime(2026, 9, 17)), w));
        Settle(250);
        Assert.Contains("band", field.Calendar.DayCells[new DateTime(2026, 9, 17)].Classes);

        Click(Day(field, new DateTime(2026, 9, 23)), w);
        Assert.False(field.Flyout.IsOpen);
        Assert.Equal(new DateTime(2026, 9, 16), field.SelectedDate);
        Assert.Equal(new DateTime(2026, 9, 23), field.EndDate);
        Assert.Equal("16–23 سبتمبر 2026", field.Text);
        Assert.Equal("2026-09-23", field.EndValue);

        Opened(field, w);
        ClickWith(Day(field, new DateTime(2026, 9, 25)), w, RawInputModifiers.Shift);
        Assert.Equal(new DateTime(2026, 9, 16), field.SelectedDate);
        Assert.Equal(new DateTime(2026, 9, 25), field.EndDate);

        Opened(field, w);
        ClickWith(Day(field, new DateTime(2026, 9, 10)), w, RawInputModifiers.Shift);
        Assert.Equal(new DateTime(2026, 9, 10), field.SelectedDate);
        Assert.Equal(new DateTime(2026, 9, 25), field.EndDate);
    }

    [AvaloniaFact]
    public void Range_shows_two_months_on_a_wide_window()
    {
        var wide = new DateField { Mode = DatePickMode.Range, SelectedDate = Sep30 };
        Show(Page(wide), FlowDirection.RightToLeft, width: 800, height: 700);
        wide.Open(false);
        Settle();
        Assert.Contains(":dual", wide.Calendar.Classes);
        Assert.Equal(608, wide.Calendar.Bounds.Width);

        var narrow = new DateField { Mode = DatePickMode.Range, SelectedDate = Sep30 };
        Show(Page(narrow), FlowDirection.RightToLeft, width: 640, height: 700);
        narrow.Open(false);
        Settle();
        Assert.DoesNotContain(":dual", narrow.Calendar.Classes);
        Assert.Equal(304, narrow.Calendar.Bounds.Width);
    }

    [AvaloniaFact]
    public void DateTime_keeps_open_and_writes_both()
    {
        var field = new DateField { Mode = DatePickMode.DateTime, MinuteStep = 15, SelectedTime = new TimeSpan(14, 0, 0) };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        Opened(field, w);

        Click(Day(field, new DateTime(2026, 9, 26)), w);
        Assert.True(field.Flyout.IsOpen);

        field.Calendar.Drum!.MinuteColumn!.Focus();
        Press(w, Key.Down, RawInputModifiers.None, 450);
        Press(w, Key.Down, RawInputModifiers.None, 450);
        Assert.Equal(new TimeSpan(14, 30, 0), field.SelectedTime);

        Click(Foot(field, "cal-done"), w);
        Assert.False(field.Flyout.IsOpen);
        Assert.Equal("26 سبتمبر 2026 في 2:30 م", field.Text);
        Assert.Equal("2026-09-26T14:30", field.Value);
    }

    [AvaloniaFact]
    public void DateTime_stacks_on_a_narrow_window()
    {
        var field = new DateField { Mode = DatePickMode.DateTime, SelectedDate = Sep30, SelectedTime = new TimeSpan(14, 0, 0) };
        Show(Page(field), FlowDirection.RightToLeft, width: 500, height: 700);
        field.Open(false);
        Settle();
        Assert.Contains(":stacked", field.Calendar.Classes);
        Assert.Equal(304, field.Calendar.Bounds.Width);
    }

    [AvaloniaFact]
    public void Hijri_field_shows_Hijri_and_switches()
    {
        var field = new DateField { CalendarKind = CalendarKind.Hijri, SelectedDate = new DateTime(2026, 9, 24) };
        var switched = new List<bool>();
        field.CalendarSwitched += (_, g) => switched.Add(g);
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        Assert.Equal("13 ربيع الآخر 1448 هـ", field.Text);
        Assert.Equal("2026-09-24", field.Value);

        Opened(field, w);
        var gregory = field.Calendar.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-switch-option") && b.Tag as string == "gregory");
        Click(gregory, w);
        Assert.Equal("24 سبتمبر 2026", field.Text);
        Assert.Equal([true], switched);
    }

    [AvaloniaTheory]
    [InlineData(DatePickMode.Single)]
    [InlineData(DatePickMode.Range)]
    [InlineData(DatePickMode.DateTime)]
    public void Calendar_head_uses_the_small_select_between_the_arrows(DatePickMode mode)
    {
        var cal = new DateCalendar { Mode = mode, CalendarKind = CalendarKind.Hijri, Time = new TimeSpan(14, 30, 0) };
        var w = Show(Spread(cal), FlowDirection.RightToLeft, width: 900, height: 700);
        cal.Show(Sep30);
        Settle();

        var boxes = cal.GetVisualDescendants().OfType<ComboBox>().ToList();
        Assert.Equal(2, boxes.Count);
        foreach (var b in boxes)
        {
            Assert.Contains("sm", b.Classes);
            Assert.Equal(32, b.Bounds.Height);
        }
        var buttons = cal.GetVisualDescendants().OfType<Button>().ToList();
        var prev = Physical(buttons.First(b => b.Tag as string == "prev"), w);
        var next = Physical(buttons.First(b => b.Tag as string == "next"), w);
        foreach (var b in boxes.Select(b => Physical(b, w)))
        {
            Assert.True(b.Left >= next.Right - 0.5, $"{b} runs over the next arrow {next}");
            Assert.True(b.Right <= prev.Left + 0.5, $"{b} runs over the previous arrow {prev}");
        }
    }

    // ---------------------------------------------------------------- time field and drum

    [AvaloniaFact]
    public void TimeField_types_and_nudges()
    {
        var field = new TimeField { SelectedTime = new TimeSpan(9, 30, 0), MinuteStep = 30 };
        var w = Show(Page(field), FlowDirection.RightToLeft);
        Assert.Equal("9:30 ص", field.Text);

        field.Focus();
        field.SelectAll();
        Typed(w, "2:30 م");
        Press(w, Key.Enter);
        Assert.Equal(new TimeSpan(14, 30, 0), field.SelectedTime);
        Assert.Equal("14:30", field.Value);

        Press(w, Key.Up);
        Assert.Equal("3:00 م", field.Text);

        field.MinTime = new TimeSpan(8, 0, 0);
        field.SelectAll();
        Typed(w, "7:00");
        Press(w, Key.Enter);
        Assert.Contains("اكتب وقتًا صحيحًا.", Messages(field));
    }

    [AvaloniaFact]
    public void TimeField_button_and_Alt_Down_open_the_wheels()
    {
        var field = new TimeField { SelectedTime = new TimeSpan(9, 30, 0) };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);

        Click(field.ClockButton, w);
        Assert.True(field.Flyout.IsOpen);
        Press(w, Key.Escape, RawInputModifiers.None, 300);
        Assert.False(field.Flyout.IsOpen);
        Assert.True(field.IsFocused);

        Press(w, Key.Down, RawInputModifiers.Alt, 300);
        Assert.True(field.Flyout.IsOpen);
        Assert.Same(field.Drum.HourColumn, Focused(w));
    }

    [AvaloniaFact]
    public void Drum_column_steps_with_keys_and_wheel()
    {
        var drum = new TimeDrum { Time = new TimeSpan(9, 0, 0), MinuteStep = 30, HorizontalAlignment = HorizontalAlignment.Left };
        var picked = new List<TimeSpan>();
        drum.TimePicked += (_, t) => picked.Add(t);
        var w = Show(Page(drum), FlowDirection.RightToLeft);

        drum.HourColumn!.Focus();
        Press(w, Key.Down, RawInputModifiers.None, 450);
        Assert.Equal([new TimeSpan(10, 0, 0)], picked);

        w.MouseWheel(Center(drum.MinuteColumn!, w), new Vector(0, -1));
        Settle(450);
        Assert.Equal(new TimeSpan(10, 30, 0), picked.Last());
    }

    // ---------------------------------------------------------------- pop-up

    [AvaloniaFact]
    public void Pop_up_reuses_the_menu_presenter()
    {
        var field = new DateField { SelectedDate = Sep30 };
        var w = Show(Page(field), FlowDirection.RightToLeft, width: 800, height: 700);
        Opened(field, w);

        var presenter = w.GetVisualDescendants().OfType<FlyoutPresenter>().Single();
        Assert.Contains("pop", presenter.Classes);
        Assert.Contains("picker", presenter.Classes);
        Assert.Equal(new Thickness(40, 16, 40, 64), Part<Border>(presenter, "PART_Room").Padding);
        var box = Part<Border>(presenter, "LayoutRoot");
        Assert.Equal(Res(presenter, "InsRLg"), box.CornerRadius);
        Assert.Equal(Res(presenter, "InsShLg"), box.BoxShadow);
        Assert.True(field.Flyout.Popup.ShouldUseOverlayLayer);
        Assert.True(field.Flyout.Popup.IsUsingOverlayLayer);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Pop_up_hangs_from_the_leading_edge(string dir)
    {
        // The pop keeps 40px of room for its shadow, so a trigger nearer the window edge than that is slid inward; this page keeps clear of it.
        var field = new DateField { SelectedDate = Sep30 };
        var page = Page(field);
        page.Margin = new Thickness(60, 20);
        var w = Show(page, Dir(dir), width: 800, height: 700);
        Opened(field, w);

        var presenter = w.GetVisualDescendants().OfType<FlyoutPresenter>().Single();
        var card = Physical(Part<Border>(presenter, "LayoutRoot"), w);
        var box = Physical(field, w);
        if (dir == "rtl") Assert.InRange(card.Right - box.Right, -2, 2);
        else Assert.InRange(card.Left - box.Left, -2, 2);
        Assert.InRange(card.Top - box.Bottom, 4, 8);
    }

    [AvaloniaFact]
    public void DateTimeGroup_draws_one_box()
    {
        var group = new DateTimeGroup { SelectedDate = new DateTime(2026, 9, 26), SelectedTime = new TimeSpan(14, 30, 0) };
        Show(Page(group), FlowDirection.RightToLeft);

        Assert.Equal("2026-09-26T14:30", group.Value);
        AssertBrush(group, "InsCtlBg", () => Part<Border>(group, "PART_BorderElement").Background);
        Assert.Equal(new Thickness(0), Part<Border>(group.DatePart!, "PART_BorderElement").BorderThickness);
        Assert.Equal(new Thickness(0), Part<Border>(group.TimePart!, "PART_BorderElement").BorderThickness);
    }

    // ---------------------------------------------------------------- month calendar

    private static List<MonthEvent> MonthEvents() =>
    [
        new() { Date = new DateTime(2026, 9, 3), Title = "تسليم الطلب 1042", Tone = MonthEventTone.Warn, Href = "#month" },
        new() { Date = new DateTime(2026, 9, 8), Time = new TimeSpan(10, 0, 0), Title = "اجتماع الفريق" },
        new() { Date = new DateTime(2026, 9, 8), Time = new TimeSpan(14, 30, 0), Title = "مراجعة العقد", Tone = MonthEventTone.Info },
        new() { Date = new DateTime(2026, 9, 15), Title = "إطلاق الحملة", Tone = MonthEventTone.Ok },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(9, 0, 0), Title = "زيارة المورد" },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(11, 0, 0), Title = "مكالمة العميل" },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(13, 0, 0), Title = "تدريب" },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(16, 0, 0), Title = "موعد نهائي", Tone = MonthEventTone.Bad },
        new() { Date = new DateTime(2026, 9, 26), Title = "جرد المخزون", Tone = MonthEventTone.Info },
    ];

    private static ToggleButton HoursToggle(MonthCalendar month) =>
        month.GetVisualDescendants().OfType<ToggleButton>().Single(b => b.Tag as string == "hours");

    private static List<Border> HourCells(MonthCalendar month) =>
        month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-hour-cell")).ToList();

    private static readonly DateTime OutsideDay = new(2026, 9, 21);

    // 4:00 and 5:00 to 6:00 wholly before 6:00, 5:30 to 7:30 across the start, 17:00 to 20:00 across the end, 19:00 wholly after 18:00.
    private static MonthEvent[] Outside() =>
    [
        new() { Date = OutsideDay, Time = new TimeSpan(4, 0, 0), Title = "رحلة الصباح" },
        new() { Date = OutsideDay, Time = new TimeSpan(5, 0, 0), Title = "تجهيز القاعة" },
        new() { Date = OutsideDay, Time = new TimeSpan(5, 30, 0), Duration = TimeSpan.FromHours(2), Title = "استقبال الوفد" },
        new() { Date = OutsideDay, Time = new TimeSpan(17, 0, 0), Duration = TimeSpan.FromHours(3), Title = "ورشة عمل" },
        new() { Date = OutsideDay, Time = new TimeSpan(19, 0, 0), Title = "عشاء العمل" },
    ];

    [AvaloniaFact]
    public void MonthCalendar_shows_events_and_moves()
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Events = MonthEvents() };
        var w = Show(Spread(month), FlowDirection.RightToLeft, width: 800, height: 900);
        var moved = new List<DateTime>();
        month.MonthChanged += (_, m) => moved.Add(m);

        var busy = month.Cells[new DateTime(2026, 9, 21)];
        Assert.Equal(2, busy.GetVisualDescendants().OfType<Border>().Count(b => b.Classes.Contains("month-event")));
        Assert.Equal("+2", busy.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("month-more")).Text);
        Assert.Contains("today", month.Cells[new DateTime(2026, 9, 15)].Classes);
        Assert.Equal("السبت", month.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("month-wd")).Text);

        month.Go(1);
        Settle(200);
        Assert.Equal([new DateTime(2026, 10, 1)], moved);

        var next = month.GetVisualDescendants().OfType<Button>().First(b => b.Tag as string == "next");
        next.Focus();
        Press(w, Key.PageUp, RawInputModifiers.None, 250);
        Assert.Equal(new DateTime(2026, 9, 1), month.Month);
        Press(w, Key.PageDown, RawInputModifiers.None, 250);
        Assert.Equal(new DateTime(2026, 10, 1), month.Month);
    }

    [AvaloniaFact]
    public void MonthCalendar_follows_its_locale_and_its_own_width()
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Events = MonthEvents(), Locale = "en", FlowDirection = FlowDirection.LeftToRight, Width = 400 };
        Show(Spread(month), FlowDirection.LeftToRight, width: 800, height: 900);
        Assert.Equal("Sun", month.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("month-wd")).Text);
        Assert.Contains(":narrow", month.Classes);
        Assert.True(month.GetVisualDescendants().OfType<StackPanel>().First(p => p.Classes.Contains("month-dots")).IsVisible);
        Assert.False(month.GetVisualDescendants().OfType<StackPanel>().First(p => p.Classes.Contains("month-events")).IsVisible);
    }

    private static Rect Box(Visual v, Visual root)
    {
        var a = v.TranslatePoint(new Point(0, 0), root)!.Value;
        var b = v.TranslatePoint(new Point(v.Bounds.Width, v.Bounds.Height), root)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaFact]
    public void MonthCalendar_events_start_as_an_empty_list_that_redraws()
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1) };
        Show(Spread(month), FlowDirection.RightToLeft, width: 800, height: 900);
        Assert.NotNull(month.Events);
        Assert.Empty(month.Events!);
        month.Events!.Add(new MonthEvent { Date = new DateTime(2026, 9, 10), Title = "اجتماع" });
        Settle(200);
        Assert.Single(month.Cells[new DateTime(2026, 9, 10)].GetVisualDescendants().OfType<Border>(), b => b.Classes.Contains("month-event"));
        Assert.NotSame(month.Events, new MonthCalendar().Events);
    }

    [AvaloniaFact]
    public void MonthCalendar_takes_its_events_as_children_in_xaml()
    {
        var month = global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Parse<MonthCalendar>(
            """
            <ins:MonthCalendar xmlns="https://github.com/avaloniaui" xmlns:ins="using:Insiyab.Avalonia.Controls" Month="2026-09-01">
              <ins:MonthEvent Date="2026-09-10" Title="اجتماع" Tone="Info" />
              <ins:MonthEvent Date="2026-09-12" Title="تسليم" />
            </ins:MonthCalendar>
            """, typeof(MonthCalendar).Assembly);
        Assert.Equal(2, month.Events!.Count);
        Assert.Equal(MonthEventTone.Info, month.Events[0].Tone);
        Show(Spread(month), FlowDirection.RightToLeft, width: 800, height: 900);
        Assert.Single(month.Cells[new DateTime(2026, 9, 12)].GetVisualDescendants().OfType<Border>(), b => b.Classes.Contains("month-event"));
    }

    [AvaloniaFact]
    public void MonthCalendar_lanes_put_overlapping_events_side_by_side()
    {
        var lanes = MonthCalendar.Lanes([(0, 60), (30, 90), (60, 120), (200, 260), (210, 230), (215, 225)]);
        Assert.Equal([(0, 2), (1, 2), (0, 2), (0, 3), (1, 3), (2, 3)], lanes);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void MonthCalendar_week_view_places_events_by_time_and_length(string dir)
    {
        var visit = new MonthEvent { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(9, 0, 0), Duration = TimeSpan.FromMinutes(90), Title = "زيارة المورد" };
        var call = new MonthEvent { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(10, 0, 0), Title = "مكالمة العميل" };
        var training = new MonthEvent { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(13, 0, 0), Duration = TimeSpan.FromHours(2), Title = "تدريب" };
        var holiday = new MonthEvent { Date = new DateTime(2026, 9, 21), Title = "إجازة" };
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 21), Width = 760, Events = [visit, call, training, holiday] };
        if (dir == "ltr") month.Locale = "en";
        var w = Show(Spread(month), Dir(dir), width: 820, height: 900);
        Settle(300);

        Assert.Contains(":week", month.Classes);
        var title = month.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("month-title")).Text;
        Assert.Equal(dir == "rtl" ? "19–25 سبتمبر 2026" : "September 20 – 26, 2026", title);
        var heads = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-head")).ToList();
        Assert.Equal(7, heads.Count);
        Assert.Equal(dir == "rtl" ? new DateTime(2026, 9, 19) : new DateTime(2026, 9, 20), heads[0].Tag);
        Assert.DoesNotContain(heads, h => h.Classes.Contains("today"));

        const double hour = 48;
        Assert.Equal(1.5 * hour - 2, month.Chips[visit].Bounds.Height, 1);
        Assert.Equal(hour - 2, month.Chips[call].Bounds.Height, 1);
        Assert.Equal(2 * hour - 2, month.Chips[training].Bounds.Height, 1);
        Assert.Equal(month.Chips[visit].Bounds.Width, month.Chips[call].Bounds.Width, 1);
        // Layout rounding can give each half-width lane half a pixel, so the pair may be a pixel wider than the whole.
        Assert.InRange(2 * month.Chips[visit].Bounds.Width + 2 - month.Chips[training].Bounds.Width, -1, 1);
        var a = Box(month.Chips[visit], w);
        var b = Box(month.Chips[call], w);
        var c = Box(month.Chips[training], w);
        Assert.Equal(hour, b.Top - a.Top, 1);
        Assert.Equal(4 * hour, c.Top - a.Top, 1);
        if (dir == "rtl") Assert.True(a.Left > b.Left);
        else Assert.True(a.Left < b.Left);
        Assert.Contains(month.Chips[holiday], month.GetVisualDescendants().OfType<StackPanel>().Single(p => p.Classes.Contains("week-allday") && p.Children.Count > 0).Children);
        Assert.Equal(dir == "rtl" ? "1:00 م – 3:00 م" : "1:00 PM – 3:00 PM", month.Chips[training].GetVisualDescendants().OfType<TextBlock>().Last().Text);
    }

    [AvaloniaFact]
    public void MonthCalendar_week_and_day_views_step_and_switch()
    {
        var month = new MonthCalendar { Date = new DateTime(2026, 9, 21), Width = 760, Events = MonthEvents() };
        Assert.Equal(new DateTime(2026, 9, 21), month.Date);
        Show(Spread(month), FlowDirection.RightToLeft, width: 820, height: 900);
        var dates = new List<DateTime>();
        var views = new List<MonthCalendarView>();
        month.DateChanged += (_, d) => dates.Add(d);
        month.ViewChanged += (_, v) => views.Add(v);

        var seg = month.GetVisualDescendants().OfType<Segmented>().Single(s => s.Classes.Contains("month-views"));
        Assert.True(seg.IsVisible);
        Assert.Equal(0, seg.SelectedIndex);
        seg.SelectedIndex = 1;
        Settle(200);
        Assert.Equal(MonthCalendarView.Week, month.View);
        Assert.Equal([MonthCalendarView.Week], views);

        month.Go(1);
        Assert.Equal(new DateTime(2026, 9, 28), month.Date);
        month.Go(1);
        Assert.Equal(new DateTime(2026, 10, 5), month.Date);
        Assert.Equal(new DateTime(2026, 10, 1), month.Month);
        month.Go(0);
        Assert.Equal(new DateTime(2026, 9, 15), month.Date);
        Assert.Equal([new DateTime(2026, 9, 28), new DateTime(2026, 10, 5), new DateTime(2026, 9, 15)], dates);

        month.View = MonthCalendarView.Day;
        Settle(200);
        Assert.Equal(2, seg.SelectedIndex);
        Assert.Single(month.GetVisualDescendants().OfType<Border>(), b => b.Classes.Contains("week-day"));
        month.Go(-1);
        Assert.Equal(new DateTime(2026, 9, 14), month.Date);
        Assert.Equal("الاثنين، 14 سبتمبر 2026", month.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("month-title")).Text);

        month.ShowViewSwitch = false;
        Settle(100);
        Assert.False(seg.IsVisible);
    }

    [AvaloniaFact]
    public void MonthCalendar_month_and_date_stay_together()
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 3, 1) };
        Assert.Equal(new DateTime(2026, 3, 1), month.Date);
        month.Date = new DateTime(2026, 5, 9);
        Assert.Equal(new DateTime(2026, 5, 1), month.Month);
        month.Month = new DateTime(2026, 5, 20);
        Assert.Equal(new DateTime(2026, 5, 9), month.Date);
    }

    [AvaloniaFact]
    public void MonthCalendar_now_line_crosses_today_at_the_current_time()
    {
        var before = MonthCalendar.Now;
        MonthCalendar.Now = () => new DateTime(2026, 9, 15, 10, 30, 0);
        try
        {
            var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 15), Width = 760 };
            Show(Spread(month), FlowDirection.RightToLeft, width: 820, height: 900);
            Settle(200);
            var line = Assert.IsAssignableFrom<Control>(month.NowLine);
            var column = month.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("week-day") && b.Classes.Contains("today"));
            Assert.Same(column.Child, line.GetVisualParent());
            Assert.Equal(4.5 * 48 - 5, line.Bounds.Y, 1);
            Assert.Equal(column.Bounds.Width - column.BorderThickness.Left, line.Bounds.Width, 1);

            month.Date = new DateTime(2026, 9, 25);
            Settle(100);
            Assert.Null(month.NowLine);
        }
        finally
        {
            MonthCalendar.Now = before;
        }
    }

    [AvaloniaFact]
    public void MonthCalendar_work_hours_are_on_from_6_to_18_by_default()
    {
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 21), Width = 760 };
        Assert.True(month.WorkHoursOnly);
        Assert.Equal(6, month.DayStartHour);
        Assert.Equal(18, month.DayEndHour);
        Assert.True(month.ShowHoursSwitch);
        Show(Spread(month), FlowDirection.RightToLeft, width: 820, height: 900);
        Settle(200);

        var cells = HourCells(month);
        Assert.Equal(12, cells.Count);
        Assert.Equal("6:00 ص", ((TextBlock)cells[0].Child!).Text);
        Assert.Null(month.HoursScroller);
        Assert.Empty(month.Bands);

        var toggle = HoursToggle(month);
        Assert.True(toggle.IsVisible);
        Assert.True(toggle.IsChecked);
        Assert.Equal("ساعات الدوام", toggle.Content);
        Assert.Equal("اعرض ساعات الدوام وحدها، من 6:00 ص إلى 6:00 م", ToolTip.GetTip(toggle));

        month.View = MonthCalendarView.Month;
        Settle(100);
        Assert.False(toggle.IsVisible);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void MonthCalendar_all_hours_scroll_from_the_work_start_with_the_days_in_line(string dir)
    {
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 21), Width = 760, WorkHoursOnly = false };
        if (dir == "ltr") month.Locale = "en";
        var w = Show(Spread(month), Dir(dir), width: 820, height: 900);
        Settle(300);

        Assert.Equal(24, HourCells(month).Count);
        var scroll = Assert.IsType<ScrollViewer>(month.HoursScroller);
        Assert.Equal(12 * 48, scroll.Bounds.Height, 1);
        Assert.Equal(6 * 48, scroll.Offset.Y, 1);

        var heads = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-head")).ToList();
        var cols = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-day")).ToList();
        Assert.Equal(7, heads.Count);
        Assert.Equal(7, cols.Count);
        for (var i = 0; i < 7; i++)
        {
            Assert.Equal(Box(cols[i], w).Left, Box(heads[i], w).Left, 1);
            Assert.Equal(Box(cols[i], w).Right, Box(heads[i], w).Right, 1);
        }
        Assert.Empty(month.Bands);
        Assert.False(HoursToggle(month).IsChecked);
    }

    [AvaloniaFact]
    public void MonthCalendar_hours_toggle_switches_and_keeps_the_scroll()
    {
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 21), Width = 760 };
        var w = Show(Spread(month), FlowDirection.RightToLeft, width: 820, height: 900);
        Settle(200);

        Click(HoursToggle(month), w);
        Assert.False(month.WorkHoursOnly);
        Assert.False(HoursToggle(month).IsChecked);
        Assert.Equal(6 * 48, month.HoursScroller!.Offset.Y, 1);

        var first = month.HoursScroller!;
        first.Offset = new Vector(0, 96);
        Settle(100);
        month.Events!.Add(new MonthEvent { Date = new DateTime(2026, 9, 22), Time = new TimeSpan(3, 0, 0), Title = "مناوبة" });
        Settle(200);
        Assert.NotSame(first, month.HoursScroller);
        Assert.Equal(96, month.HoursScroller!.Offset.Y, 1);

        month.Go(1);
        Settle(200);
        Assert.Equal(6 * 48, month.HoursScroller!.Offset.Y, 1);

        HoursToggle(month).Focus();
        Press(w, Key.Space, RawInputModifiers.None, 250);
        Assert.True(month.WorkHoursOnly);
        Assert.Null(month.HoursScroller);
        Assert.True(HoursToggle(month).IsFocused);
    }

    [AvaloniaFact]
    public void MonthCalendar_events_outside_the_work_hours_are_counted_at_the_edges()
    {
        var ev = Outside();
        var month = new MonthCalendar { View = MonthCalendarView.Day, Date = OutsideDay, Width = 600, Events = ev };
        var w = Show(Spread(month), FlowDirection.RightToLeft, width: 700, height: 900);
        Settle(200);

        Assert.Equal(12, HourCells(month).Count);
        Assert.DoesNotContain(ev[0], month.Chips.Keys);
        Assert.DoesNotContain(ev[1], month.Chips.Keys);
        Assert.DoesNotContain(ev[4], month.Chips.Keys);
        Assert.Equal(1, month.Chips[ev[2]].Bounds.Y, 1);
        Assert.Contains("cut-start", month.Chips[ev[2]].Classes);
        Assert.DoesNotContain("cut-end", month.Chips[ev[2]].Classes);
        var lane = month.Chips[ev[3]].GetVisualParent<Control>()!;
        Assert.Equal(lane.Bounds.Height - 1, month.Chips[ev[3]].Bounds.Bottom, 1);
        Assert.Contains("cut-end", month.Chips[ev[3]].Classes);

        var before = month.Bands[(OutsideDay, false)];
        var after = month.Bands[(OutsideDay, true)];
        Assert.Equal("+2", before.GetVisualDescendants().OfType<TextBlock>().Single().Text);
        Assert.Equal("+1", after.GetVisualDescendants().OfType<TextBlock>().Single().Text);
        Assert.Equal("موعدان قبل 6:00 ص", AutomationProperties.GetName(before));
        Assert.Equal("موعد واحد بعد 6:00 م", AutomationProperties.GetName(after));
        Assert.Equal("4:00 ص رحلة الصباح، 5:00 ص تجهيز القاعة", ToolTip.GetTip(before));
        var labels = month.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("week-band-label")).Select(t => t.Text).ToList();
        Assert.Equal(["قبل 6:00 ص", "بعد 6:00 م"], labels);
        Assert.True(Box(before, w).Bottom <= Box(lane, w).Top + 1);
        Assert.True(Box(after, w).Top >= Box(lane, w).Bottom - 1);
    }

    [AvaloniaFact]
    public void MonthCalendar_pressing_a_band_shows_all_hours_at_its_events()
    {
        var ev = Outside();
        var month = new MonthCalendar { View = MonthCalendarView.Day, Date = OutsideDay, Width = 600, Events = ev };
        var w = Show(Spread(month), FlowDirection.RightToLeft, width: 700, height: 900);
        Settle(200);
        var invoked = new List<DateTime>();
        month.DayInvoked += (_, d) => invoked.Add(d);

        Click(month.Bands[(OutsideDay, false)], w);
        Assert.False(month.WorkHoursOnly);
        Assert.Empty(invoked);
        Assert.Equal(4 * 48 - 24, month.HoursScroller!.Offset.Y, 1);
        Assert.Contains(ev[0], month.Chips.Keys);

        Click(HoursToggle(month), w);
        Assert.True(month.WorkHoursOnly);
        var band = month.Bands[(OutsideDay, true)];
        band.Focus();
        Press(w, Key.Enter, RawInputModifiers.None, 250);
        Assert.False(month.WorkHoursOnly);
        Assert.Equal(12 * 48, month.HoursScroller!.Offset.Y, 1);
        Assert.True(HoursToggle(month).IsFocused);
    }

    [AvaloniaFact]
    public void MonthCalendar_work_hours_stay_in_range()
    {
        var month = new MonthCalendar { View = MonthCalendarView.Day, Date = OutsideDay, Width = 600, DayStartHour = 20, DayEndHour = 18 };
        Show(Spread(month), FlowDirection.RightToLeft, width: 700, height: 900);
        Settle(200);
        var cells = HourCells(month);
        Assert.Single(cells);
        Assert.Equal("8:00 م", ((TextBlock)cells[0].Child!).Text);

        month.DayStartHour = -3;
        month.DayEndHour = 30;
        Settle(100);
        Assert.Equal(24, HourCells(month).Count);
        Assert.False(HoursToggle(month).IsVisible);

        month.WorkHoursOnly = false;
        Settle(100);
        Assert.Null(month.HoursScroller);
        Assert.Equal(24, HourCells(month).Count);
    }

    [AvaloniaFact]
    public void MonthCalendar_page_keys_turn_the_week_from_inside_the_hours()
    {
        var call = new MonthEvent { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(9, 0, 0), Title = "مكالمة العميل", Href = "#call" };
        var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 21), Width = 760, WorkHoursOnly = false, Events = [call] };
        var w = Show(Spread(month), FlowDirection.RightToLeft, width: 820, height: 900);
        Settle(300);

        month.Chips[call].Focus();
        Press(w, Key.PageDown, RawInputModifiers.None, 250);
        Assert.Equal(new DateTime(2026, 9, 28), month.Date);
        Assert.True(month.GetVisualDescendants().OfType<Button>().Single(b => b.Tag as string == "next").IsFocused);
        Press(w, Key.PageUp, RawInputModifiers.None, 250);
        Assert.Equal(new DateTime(2026, 9, 21), month.Date);
    }

    [AvaloniaFact]
    public void MonthCalendar_hours_switch_can_be_hidden()
    {
        var ev = Outside();
        var month = new MonthCalendar { View = MonthCalendarView.Day, Date = OutsideDay, Width = 600, ShowHoursSwitch = false, Events = ev };
        var w = Show(Spread(month), FlowDirection.RightToLeft, width: 700, height: 900);
        Settle(200);
        Assert.False(HoursToggle(month).IsVisible);
        Assert.Contains((OutsideDay, true), month.Bands.Keys);

        month.Bands[(OutsideDay, true)].Focus();
        Press(w, Key.Enter, RawInputModifiers.None, 250);
        Assert.False(month.WorkHoursOnly);
        Assert.True(month.GetVisualDescendants().OfType<Button>().Single(b => b.Tag as string == "today").IsFocused);
    }

    [AvaloniaFact]
    public void MonthCalendar_now_line_follows_the_shown_hours()
    {
        var before = MonthCalendar.Now;
        MonthCalendar.Now = () => new DateTime(2026, 9, 15, 3, 0, 0);
        try
        {
            var month = new MonthCalendar { View = MonthCalendarView.Week, Date = new DateTime(2026, 9, 15), Width = 760 };
            Show(Spread(month), FlowDirection.RightToLeft, width: 820, height: 900);
            Settle(200);
            Assert.Null(month.NowLine);

            month.WorkHoursOnly = false;
            Settle(200);
            var line = Assert.IsAssignableFrom<Control>(month.NowLine);
            Assert.Equal(3 * 48 - 5, line.Bounds.Y, 1);
        }
        finally
        {
            MonthCalendar.Now = before;
        }
    }
}
