using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
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

// The birth date plugin: its typing rules (no UI), then the field, its pop-up of columns and its inline layout.
[Collection("Pickers")]
public class PickersBirthTests : IDisposable
{
    private static readonly DateTime Today = new(2026, 10, 11);
    private static readonly Func<DateTime> FixedClock = () => Today;
    private static readonly DateTime Born = new(1993, 1, 22);

    public PickersBirthTests() => DateCalendar.Today = FixedClock;

    public void Dispose()
    {
        if (ReferenceEquals(DateCalendar.Today, FixedClock)) DateCalendar.Today = () => DateTime.Today;
    }

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
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

    private static string TextOf(TextBlock t) => t.Text ?? string.Join(string.Empty, t.Inlines!.OfType<Run>().Select(r => r.Text));

    private static StackPanel Seg(BirthDateField field, char k) =>
        field.GetVisualDescendants().OfType<StackPanel>().First(s => s.Classes.Contains("birth-seg") && s.Classes.Contains(k.ToString()));

    private static Border Box(BirthDateField field, char k) =>
        Seg(field, k).GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("birth-box"));

    // Types a string into the engine the way the field does: each digit goes where the last answer said focus went.
    private static void Feed(BirthEngine engine, ref char at, string keys)
    {
        foreach (var c in keys)
        {
            var next = c == '/' ? engine.Separate(at) : engine.Press(at, c);
            if (next is { } n) at = n;
        }
    }

    // ---------------------------------------------------------------- the engine

    [Fact]
    public void Birth_engine_follows_the_typing_rules()
    {
        BirthEngine New(int minAge = 0) => new(minAge, 120, 31, () => Today);
        var e = New();
        Assert.Equal('m', e.Press('d', '4'));
        Assert.Equal("04", e.Str('d'));
        Assert.True(e.IsAuto('d', 0));
        Assert.False(e.IsAuto('d', 1));

        e.Clear();
        var at = 'd';
        Feed(e, ref at, "22193");
        Assert.Equal(("22", "01", "1993"), (e.Str('d'), e.Str('m'), e.Str('y')));
        Assert.Equal(Born, e.Date);

        e.Clear();
        at = 'd';
        Feed(e, ref at, "2/3/93");
        Assert.Equal(("02", "03", "1993"), (e.Str('d'), e.Str('m'), e.Str('y')));

        e.Clear();
        at = 'd';
        Feed(e, ref at, "31");
        Assert.Equal('m', at);
        Assert.Null(e.Press('m', '0'));
        Assert.False(e.Flashed);
        Assert.Null(e.Press('m', '2'));
        Assert.True(e.Flashed);
        Assert.Equal('m', e.FlashedPart);
        Assert.Equal("0", e.Str('m'));

        e.Clear();
        e.Press('d', '3');
        e.Press('d', '1');
        Assert.Equal('d', e.Backspace('m'));
        Assert.Equal("3", e.Str('d'));

        foreach (var text in new[] { "22 يناير 1993", "29 رجب 1413", "1993-01-22", "22 كانون الثاني 93" })
            Assert.Equal(Born, BirthEngine.Understand(text, Today));
        Assert.Null(BirthEngine.Understand("hello", Today));

        var young = New(18);
        young.FillAll(new DateTime(2015, 1, 1));
        Assert.Null(young.Date);
        Assert.Equal("يجب أن يكون العمر 18 سنة على الأقل", young.Message(false, PickerLocale.Arabic));
        Assert.Equal("You must be at least 18 years old", young.Message(false, PickerLocale.English));
    }

    [Fact]
    public void Birth_engine_limits_and_snapshots()
    {
        var e = new BirthEngine(0, 70, 22, () => Today);
        Assert.Equal(1956, e.FirstYear);
        Assert.Equal(2026, e.LastYear);
        Assert.Equal(Today, e.Limit);
        Assert.Equal(2004, e.Val('y'));
        Assert.Equal(31, e.DaysInMonth);
        e.Choose('m', 2);
        e.Choose('y', 2024);
        Assert.Equal(29, e.DaysInMonth);
        var saved = e.Save();
        e.Choose('d', 29);
        e.Choose('y', 2023);
        Assert.Equal(28, e.Val('d'));
        Assert.True(e.DayItemsChanged);
        e.Restore(saved);
        Assert.False(e.IsSet('d'));
        Assert.Equal(2024, e.Val('y'));
        Assert.Equal(PickerLocale.English.BirthIncomplete, e.Message(false, PickerLocale.English));
        e.Clear();
        Assert.Equal(string.Empty, e.Message(false, PickerLocale.English));
        Assert.Equal(PickerLocale.English.BirthRequired, e.Message(true, PickerLocale.English));
    }

    // ---------------------------------------------------------------- the field

    [AvaloniaFact]
    public void Birth_field_types_and_reads_the_date()
    {
        var field = new BirthDateField();
        var changes = new List<BirthChangedEventArgs>();
        field.DateChanged += (_, e) => changes.Add(e);
        var w = Show(Page(field), FlowDirection.RightToLeft);

        field.DayBox.Focus();
        Settle(100);
        Typed(w, "2");
        Typed(w, "2");
        Assert.True(field.MonthBox.IsFocused);
        Typed(w, "01");
        Assert.True(field.YearBox.IsFocused);
        Typed(w, "1993");
        Assert.Equal("1993-01-22", field.Value);
        Assert.Equal(Born, field.SelectedDate);
        Assert.Single(changes);
        var line = TextOf(field.Line!);
        Assert.Contains("الجمعة 22 يناير 1993", line);
        Assert.Contains("العمر 33 سنة", line);
        Assert.Contains("29 رجب 1413 هـ", line);

        Press(w, Key.Escape);
        Assert.Equal(string.Empty, field.Value);
        Assert.False(field.Engine.Any);
        Assert.True(field.DayBox.IsFocused);
        Assert.Equal(2, changes.Count);
    }

    [AvaloniaFact]
    public void Birth_field_says_what_is_wrong()
    {
        var field = new BirthDateField { MinAge = 18, IsRequired = true };
        var w = Show(Page(field), FlowDirection.RightToLeft);
        Assert.Equal(PickerLocale.Arabic.BirthRequired, field.ValidationMessage);
        Assert.True(DataValidationErrors.GetHasErrors(field));

        field.DayBox.Focus();
        Typed(w, "1");
        Assert.Equal(PickerLocale.Arabic.BirthIncomplete, field.ValidationMessage);
        field.SetDate(new DateTime(2015, 1, 1));
        Settle(100);
        Assert.Equal(string.Empty, field.Value);
        Assert.Equal("يجب أن يكون العمر 18 سنة على الأقل", field.ValidationMessage);
        Assert.Equal("يجب أن يكون العمر 18 سنة على الأقل", TextOf(field.Line!));
    }

    [AvaloniaFact]
    public void Birth_field_paste_and_arrows()
    {
        var field = new BirthDateField();
        var w = Show(Page(field));
        field.DayBox.Focus();
        field.Paste("22 يناير 1993");
        Settle(100);
        Assert.Equal("1993-01-22", field.Value);
        Assert.True(field.YearBox.IsFocused);

        Press(w, Key.Up);
        Assert.Equal("1994-01-22", field.Value);
        field.DayBox.Focus();
        Press(w, Key.Down);
        Assert.Equal("1994-01-21", field.Value);
        field.MonthBox.Focus();
        Press(w, Key.Up);
        Assert.Equal("1994-02-21", field.Value);
    }

    [AvaloniaFact]
    public void Birth_field_takes_a_date_from_code_quietly()
    {
        var field = new BirthDateField();
        var changes = 0;
        field.DateChanged += (_, _) => changes++;
        Show(Page(field));

        field.SetDate(Born);
        Assert.Equal("1993-01-22", field.Value);
        Assert.Equal(Born, field.SelectedDate);
        Assert.Equal("22", field.DayBox.Text);
        field.SelectedDate = new DateTime(2000, 5, 3);
        Assert.Equal("2000-05-03", field.Value);
        field.SetDate(null);
        Assert.Equal(string.Empty, field.Value);
        Assert.Null(field.SelectedDate);
        Assert.Equal(0, changes);
    }

    [AvaloniaFact]
    public void Birth_pop_up_drums_and_cancel()
    {
        var field = new BirthDateField();
        var w = Show(Page(field), FlowDirection.RightToLeft, height: 700);

        field.DayBox.Focus();
        Settle(100);
        Press(w, Key.Down, RawInputModifiers.Alt, 400);
        Assert.True(field.Flyout!.IsOpen);
        Assert.All(new[] { field.DayColumn, field.MonthColumn, field.YearColumn }, c => Assert.True(c.IsUnset));

        field.DayColumn.Focus();
        Press(w, Key.Down, settle: 600);
        Press(w, Key.Enter);
        Assert.True(field.Engine.IsSet('d'));
        Assert.True(field.MonthColumn.IsFocused);

        Press(w, Key.Escape, settle: 300);
        Assert.False(field.Engine.IsSet('d'));
        Assert.False(field.Flyout.IsOpen);
        Assert.True(field.DayBox.IsFocused);
    }

    [AvaloniaFact]
    public void Birth_pop_up_button_toggles_and_done_keeps_what_was_chosen()
    {
        var field = new BirthDateField { Locale = "en" };
        var w = Show(Page(field), height: 700);
        Click(field.PickButton!, w);
        Assert.True(field.Flyout!.IsOpen);
        Assert.True(field.IsDropDownOpen);
        Click(field.PickButton!, w);
        Assert.False(field.Flyout.IsOpen);
        Assert.False(field.IsDropDownOpen);

        Click(field.PickButton!, w);
        field.MonthColumn.Focus();
        Press(w, Key.Down, settle: 600);
        Assert.True(field.Engine.IsSet('m'));
        var done = w.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == "Done");
        Click(done, w);
        Assert.False(field.Flyout.IsOpen);
        Assert.True(field.Engine.IsSet('m'));
        Assert.Equal("07", field.Engine.Str('m'));
    }

    [AvaloniaFact]
    public void Birth_pop_up_column_turns_with_the_wheel_off_its_text()
    {
        var field = new BirthDateField { Locale = "en" };
        var w = Show(Page(field), height: 700);
        Click(field.PickButton!, w);
        Assert.True(field.Flyout!.IsOpen);

        var col = field.MonthColumn;
        var before = col.Value;
        var empty = col.TranslatePoint(new Point(2, col.Bounds.Height / 2 + col.RowHeight / 2), w)!.Value;
        Assert.Same(col, w.InputHitTest(empty));
        w.MouseWheel(empty, new Vector(0, -1));
        Settle(450);
        Assert.Equal(before + 1, col.Value);
        Assert.True(field.Engine.IsSet('m'));
    }

    [AvaloniaFact]
    public void Birth_inline_shows_its_drums()
    {
        var field = new BirthDateField { Layout = BirthLayout.Inline };
        Show(Page(field), height: 500);

        Assert.Null(field.Flyout);
        foreach (var column in new[] { field.DayColumn, field.MonthColumn, field.YearColumn })
        {
            Assert.True(column.IsVisible);
            Assert.True(column.Bounds.Height > 0);
            Assert.Equal(3, column.VisibleRows);
        }
        Assert.Equal(52, Box(field, 'd').Bounds.Height);
        Assert.Equal(field.Engine.LastYear - field.Engine.FirstYear + 1, field.YearColumn.Items.Count);
        Assert.Equal(121, field.YearColumn.Items.Count);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Birth_theme_states(string theme)
    {
        var field = new BirthDateField { Layout = BirthLayout.Inline };
        var w = Show(Page(field), theme: Theme(theme), height: 500);

        field.DayBox.Focus();
        Settle(300);
        AssertBrush(field, "InsGlassStrong", () => Box(field, 'd').Background);

        Typed(w, "3102");
        var seg = Seg(field, 'm');
        Assert.Contains("no", seg.Classes);
        Assert.Equal("0", field.Engine.Str('m'));
        Settle(400);
        Assert.DoesNotContain("no", seg.Classes);
    }
}
