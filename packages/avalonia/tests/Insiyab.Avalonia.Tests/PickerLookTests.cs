using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The pickers' parts: every state pictured and dumped for the before/after comparison, runtime class changes, overrides, and which elements take a theme or a type of their own.
[Collection("Pickers")]
public class PickerLookTests : IDisposable
{
    private static readonly Func<DateTime> FixedToday = () => new DateTime(2026, 9, 15);
    private static readonly DateTime Sep15 = new(2026, 9, 15);
    private readonly Func<DateTime> _now = MonthCalendar.Now;

    public PickerLookTests()
    {
        DateCalendar.Today = FixedToday;
        MonthCalendar.Now = () => new DateTime(2026, 9, 15, 10, 30, 0);
    }

    public void Dispose()
    {
        if (ReferenceEquals(DateCalendar.Today, FixedToday)) DateCalendar.Today = () => DateTime.Today;
        MonthCalendar.Now = _now;
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 420, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    private static T First<T>(Visual root, string cls) where T : Control => root.GetVisualDescendants().OfType<T>().First(c => c.Classes.Contains(cls));

    private static StackPanel Seg(BirthDateField field, char k) =>
        field.GetVisualDescendants().OfType<StackPanel>().First(s => s.Classes.Contains("birth-seg") && s.Classes.Contains(k.ToString()));

    private static Border Box(BirthDateField field, char k) => First<Border>(Seg(field, k), "birth-box");

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    // Every kind of event: a linked all-day one, timed ones, four on one day ("+2"), and on the 15th (today) events before, across and after the work hours.
    private static List<MonthEvent> Events() =>
    [
        new() { Date = new DateTime(2026, 9, 3), Title = "تسليم الطلب 1042", Tone = MonthEventTone.Warn, Href = "#month" },
        new() { Date = new DateTime(2026, 9, 8), Time = new TimeSpan(10, 0, 0), Title = "اجتماع الفريق" },
        new() { Date = new DateTime(2026, 9, 8), Time = new TimeSpan(14, 30, 0), Title = "مراجعة العقد", Tone = MonthEventTone.Info },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(9, 0, 0), Title = "زيارة المورد" },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(11, 0, 0), Title = "مكالمة العميل" },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(13, 0, 0), Title = "تدريب" },
        new() { Date = new DateTime(2026, 9, 21), Time = new TimeSpan(16, 0, 0), Title = "موعد نهائي", Tone = MonthEventTone.Bad },
        new() { Date = Sep15, Title = "إطلاق الحملة", Tone = MonthEventTone.Ok },
        new() { Date = Sep15, Time = new TimeSpan(4, 0, 0), Title = "رحلة الصباح" },
        new() { Date = Sep15, Time = new TimeSpan(5, 30, 0), Duration = TimeSpan.FromHours(2), Title = "استقبال الوفد", Href = "#visit" },
        new() { Date = Sep15, Time = new TimeSpan(9, 0, 0), Duration = TimeSpan.FromMinutes(90), Title = "مراجعة الخطة", Tone = MonthEventTone.Info },
        new() { Date = Sep15, Time = new TimeSpan(17, 0, 0), Duration = TimeSpan.FromHours(3), Title = "ورشة عمل", Tone = MonthEventTone.Warn },
        new() { Date = Sep15, Time = new TimeSpan(19, 0, 0), Title = "عشاء العمل" },
    ];

    // The stacks the pickers build are their own type, so the dumps leave the type out and read the same before and after.
    private static readonly Regex BuiltStack = new(@"^(Birth|Month)Stack \[", RegexOptions.Multiline);

    private static void Dump(Visual root, string name) =>
        File.WriteAllText(System.IO.Path.Combine(ShotsDir(), name + ".txt"), BuiltStack.Replace(LookCheck.Dump(root), "StackPanel ["));

    private static void Keep(Window w, Visual root, string name)
    {
        Capture(w, name);
        Dump(root, name);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Birth_keeps_its_look(string theme, string dir)
    {
        var inline = new BirthDateField { Layout = BirthLayout.Inline };
        var field = new BirthDateField();
        var page = Page(inline, field);
        var w = Show(page, Dir(dir), Variant(theme), 520, 760);
        Keep(w, page, $"picker-birth-empty-{theme}-{dir}");
        inline.SelectedDate = new DateTime(1993, 1, 22);
        field.SelectedDate = new DateTime(1993, 1, 22);
        Settle(300);
        Keep(w, page, $"picker-birth-filled-{theme}-{dir}");
        Assert.Equal(52, Box(inline, 'd').Height);
        Assert.Equal(32, Box(field, 'd').Height);
        Assert.Equal(6, First<StackPanel>(inline, "birth-root").Spacing);
        Assert.Equal(0, First<StackPanel>(field, "birth-root").Spacing);
        Assert.Equal(4, Seg(inline, 'd').Spacing);

        w.MouseMove(Center(Box(inline, 'm'), w));
        Settle(300);
        Keep(w, page, $"picker-birth-hover-inline-{theme}-{dir}");
        AssertBrush(inline, "InsCtlBgHover", () => Box(inline, 'm').Background);
        w.MouseMove(Center(First<Border>(field, "birth-control"), w));
        Settle(300);
        Keep(w, page, $"picker-birth-hover-field-{theme}-{dir}");
        AssertBrush(field, "InsPickerHoverBorder", () => First<Border>(field, "birth-control").BorderBrush);

        Seg(inline, 'y').Classes.Add("no");
        Seg(field, 'd').Classes.Add("no");
        Settle(300);
        Keep(w, page, $"picker-birth-error-{theme}-{dir}");
        AssertBrush(inline, "InsToneBadBg", () => Box(inline, 'y').Background);
        Seg(inline, 'y').Classes.Remove("no");
        Seg(field, 'd').Classes.Remove("no");

        inline.MonthBox.Focus();
        Settle(300);
        Keep(w, page, $"picker-birth-focus-inline-{theme}-{dir}");
        AssertBrush(inline, "InsGlassStrong", () => Box(inline, 'm').Background);
        field.DayBox.Focus();
        Settle(300);
        Keep(w, page, $"picker-birth-focus-field-{theme}-{dir}");
        AssertBrush(field, "InsToneBrandBg", () => Box(field, 'd').Background);

        Click(field.PickButton!, w);
        Assert.True(field.Flyout!.IsOpen);
        var pop = (Control)field.Flyout.Content!;
        Capture(w, $"picker-birth-pop-{theme}-{dir}");
        Dump(pop, $"picker-birth-pop-{theme}-{dir}");
        Assert.Equal(304, pop.Width);
        Assert.Equal(new Thickness(0, 1, 0, 0), First<Border>(pop, "birth-foot").BorderThickness);
        Assert.Equal((double)Res(pop, "InsFootGap"), First<StackPanel>(pop, "birth-foot-row").Spacing);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Month_keeps_its_look(string theme, string dir)
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Date = Sep15, Events = Events(), Width = 760 };
        var page = new StackPanel { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, Children = { month } };
        var w = Show(page, Dir(dir), Variant(theme), 820, 900);
        Keep(w, month, $"picker-month-{theme}-{dir}");
        var nav = First<StackPanel>(month, "month-nav");
        Assert.Equal(VerticalAlignment.Center, nav.VerticalAlignment);
        var events = First<StackPanel>(month.Cells[new DateTime(2026, 9, 8)], "month-events");
        Assert.Equal(new Thickness(0, 4, 0, 0), events.Margin);
        Assert.True(events.IsVisible);
        var dots = First<StackPanel>(month.Cells[new DateTime(2026, 9, 8)], "month-dots");
        Assert.False(dots.IsVisible);
        Assert.Equal(HorizontalAlignment.Center, dots.HorizontalAlignment);

        w.MouseMove(Center(month.Cells[new DateTime(2026, 9, 10)], w));
        Settle(300);
        Keep(w, month, $"picker-month-hover-{theme}-{dir}");

        month.Width = 400;
        Settle(300);
        Keep(w, month, $"picker-month-narrow-{theme}-{dir}");
        Assert.True(dots.IsVisible);
        Assert.False(events.IsVisible);

        month.Width = 760;
        month.View = MonthCalendarView.Week;
        Settle(300);
        Keep(w, month, $"picker-week-{theme}-{dir}");
        var head = month.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("week-head"));
        var column = Assert.IsAssignableFrom<StackPanel>(head.Child);
        Assert.Equal(HorizontalAlignment.Center, column.HorizontalAlignment);
        Assert.Equal(2, column.Spacing);
        w.MouseMove(Center(month.Bands[(Sep15, false)], w));
        Settle(300);
        Keep(w, month, $"picker-week-hover-{theme}-{dir}");

        month.View = MonthCalendarView.Day;
        month.WorkHoursOnly = false;
        Settle(300);
        Keep(w, month, $"picker-day-{theme}-{dir}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Drum_and_picker_glass_keep_their_look(string theme, string dir)
    {
        var drum = new TimeDrum { Time = new TimeSpan(9, 30, 0), MinuteStep = 30, HorizontalAlignment = HorizontalAlignment.Left };
        var time = new TimeField { SelectedTime = new TimeSpan(9, 30, 0) };
        var date = new DateField { SelectedDate = Sep15 };
        var page = Page(drum, time, date);
        var w = Show(page, Dir(dir), Variant(theme), 800, 900);
        Keep(w, page, $"picker-drum-{theme}-{dir}");
        var lens = First<Border>(drum, "drum-lens");
        Assert.Equal(36, lens.Height);
        Assert.False(lens.IsHitTestVisible);
        AssertBrush(drum, "InsToneBrandBg", () => lens.Background);
        var sep = First<TextBlock>(drum, "drum-sep");
        Assert.Equal(FontWeight.ExtraBold, sep.FontWeight);
        AssertBrush(drum, "InsToneBrand", () => sep.Foreground);

        Click(time.ClockButton, w);
        Assert.True(time.Flyout.IsOpen);
        var timePop = (Control)time.Flyout.Content!;
        Capture(w, $"picker-time-pop-{theme}-{dir}");
        Dump(timePop, $"picker-time-pop-{theme}-{dir}");
        var glass = First<Border>(timePop, "picker-glass");
        var fill = Assert.IsType<LinearGradientBrush>(glass.Background);
        Assert.Equal((Color)Res(glass, "InsGlassStrongColor"), fill.GradientStops[0].Color);
        Assert.Equal(FlowDirection.LeftToRight, glass.FlowDirection);
        Assert.False(glass.IsHitTestVisible);
        Click(time.ClockButton, w);
        Assert.False(time.Flyout.IsOpen);

        Click(date.CalendarButton, w);
        Assert.True(date.Flyout.IsOpen);
        Capture(w, $"picker-date-pop-{theme}-{dir}");
        Dump(date.Calendar, $"picker-date-pop-{theme}-{dir}");
        Assert.NotNull(First<Border>(date.Calendar, "picker-glass").Background);
    }

    [AvaloniaFact]
    public void Picker_classes_added_at_runtime_take_their_look()
    {
        var lens = new Border { Width = 40 };
        var sep = new TextBlock { Text = ":" };
        var glass = new Border { Width = 40, Height = 40 };
        Show(new StackPanel { Children = { lens, sep, glass } }, FlowDirection.RightToLeft);
        Assert.True(double.IsNaN(lens.Height));
        Assert.True(lens.IsHitTestVisible);
        Assert.Equal(FontWeight.Normal, sep.FontWeight);
        Assert.Null(glass.Background);
        Assert.Equal(FlowDirection.RightToLeft, glass.FlowDirection);

        lens.Classes.Add("drum-lens");
        sep.Classes.Add("drum-sep");
        glass.Classes.Add("picker-glass");
        Settle(300);
        Assert.Equal(36, lens.Height);
        Assert.Equal(new Thickness(1), lens.BorderThickness);
        Assert.False(lens.IsHitTestVisible);
        AssertBrush(lens, "InsToneBrandBg", () => lens.Background);
        Assert.Equal(FontWeight.ExtraBold, sep.FontWeight);
        AssertBrush(sep, "InsToneBrand", () => sep.Foreground);
        Assert.IsType<LinearGradientBrush>(glass.Background);
        Assert.Equal(FlowDirection.LeftToRight, glass.FlowDirection);
        Assert.False(glass.IsHitTestVisible);

        lens.Classes.Remove("drum-lens");
        sep.Classes.Remove("drum-sep");
        glass.Classes.Remove("picker-glass");
        Settle(300);
        Assert.True(double.IsNaN(lens.Height));
        Assert.True(lens.IsHitTestVisible);
        Assert.Equal(new Thickness(0), lens.BorderThickness);
        Assert.Equal(FontWeight.Normal, sep.FontWeight);
        Assert.Null(glass.Background);
        Assert.Equal(FlowDirection.RightToLeft, glass.FlowDirection);
        Assert.True(glass.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_picker_looks()
    {
        var lens = new Border { Classes = { "drum-lens" } };
        var glass = new Border { Classes = { "picker-glass" } };
        var sep = new TextBlock { Classes = { "drum-sep" }, Text = ":" };
        var local = new Border { Classes = { "drum-lens" }, Height = 20 };
        var page = new StackPanel { Children = { lens, glass, sep, local } };
        page.Styles.Add(App<Border>("drum-lens", Border.CornerRadiusProperty, new CornerRadius(3)));
        var appStyle = App<Border>("picker-glass", Border.CornerRadiusProperty, new CornerRadius(5));
        var typeOnly = new Style(x => x.OfType<Border>()) { Setters = { new Setter(Border.BorderThicknessProperty, new Thickness(7)) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(typeOnly);
        try
        {
            Show(page, FlowDirection.RightToLeft, height: 400);
            Assert.Equal(new CornerRadius(3), lens.CornerRadius);
            Assert.Equal(new CornerRadius(5), glass.CornerRadius);
            Assert.Equal(20, local.Height);
            Assert.Equal(new Thickness(1), lens.BorderThickness);
            Assert.Equal(36, lens.Height);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(typeOnly);
        }
        Settle();
        Assert.Equal(Res(glass, "InsRLg"), glass.CornerRadius);
    }

    private static void AssertStacks(Visual root, string typeName, params string[] prefixes)
    {
        var stacks = root.GetSelfAndVisualDescendants().OfType<StackPanel>().Where(s => s.Classes.Any(c => prefixes.Any(p => c.StartsWith(p, StringComparison.Ordinal)))).ToList();
        Assert.NotEmpty(stacks);
        Assert.All(stacks, s => Assert.Equal(typeName, s.GetType().Name));
    }

    [AvaloniaFact]
    public void Only_the_picker_parts_take_a_theme()
    {
        var plain = new Border { Child = new TextBlock { Text = "أ" } };
        var plainStack = new StackPanel();
        var lens = new Border { Classes = { "drum-lens" } };
        var glass = new Border { Classes = { "picker-glass" } };
        var sep = new TextBlock { Classes = { "drum-sep" }, Text = ":" };
        var drum = new TimeDrum { Time = new TimeSpan(9, 0, 0) };
        Show(new StackPanel { Children = { plain, plainStack, lens, glass, sep, drum } }, FlowDirection.RightToLeft, height: 600);
        Assert.Null(plain.Theme);
        Assert.Null(((TextBlock)plain.Child!).Theme);
        Assert.Null(plainStack.Theme);
        Assert.NotNull(lens.Theme);
        Assert.NotNull(glass.Theme);
        Assert.NotNull(sep.Theme);
        Assert.NotNull(Part<Border>(drum, "PART_Lens").Theme);
        Assert.NotNull(First<TextBlock>(drum, "drum-sep").Theme);

        var inline = new BirthDateField { Layout = BirthLayout.Inline };
        var field = new BirthDateField();
        var w = Show(Page(inline, field), FlowDirection.RightToLeft, null, 520, 760);
        Click(field.PickButton!, w);
        var pop = (Control)field.Flyout!.Content!;
        var shared = inline.GetVisualDescendants().Concat(pop.GetVisualDescendants()).OfType<Border>().Where(b => b.Classes.Contains("drum-lens") || b.Classes.Contains("picker-glass")).ToList();
        Assert.Contains(shared, b => b.Classes.Contains("drum-lens"));
        Assert.Contains(shared, b => b.Classes.Contains("picker-glass"));
        Assert.All(shared, b => Assert.NotNull(b.Theme));
        AssertStacks(inline, "BirthStack", "birth-");
        AssertStacks(field, "BirthStack", "birth-");
        AssertStacks(pop, "BirthStack", "birth-");

        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Date = Sep15, Events = Events(), Width = 760 };
        var mw = Show(new StackPanel { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, Children = { month } }, FlowDirection.RightToLeft, null, 820, 900);
        AssertStacks(month, "MonthStack", "month-", "week-");
        month.View = MonthCalendarView.Week;
        Settle(300);
        AssertStacks(month, "MonthStack", "month-", "week-");
        var heads = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-head")).ToList();
        Assert.NotEmpty(heads);
        Assert.All(heads, h => Assert.Equal("MonthStack", h.Child!.GetType().Name));
        Assert.NotNull(mw);
    }

    [AvaloniaFact]
    public void No_global_rule_names_a_picker_class()
    {
        var found = new List<string>();
        found.AddRange(LookCheck.SubjectRules(typeof(Border), "drum-lens", "ins-still", "IsInactive"));
        found.AddRange(LookCheck.SubjectRules(typeof(Border), "picker-glass", "ins-still", "IsInactive"));
        found.AddRange(LookCheck.SubjectRules(typeof(TextBlock), "drum-sep", "ins-still", "IsInactive"));
        var stack = new Regex(@"(?:^|[\s>])StackPanel(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\.(?:birth|month|week)-|week-head\s*>\s*StackPanel");
        found.AddRange(LookCheck.GlobalSelectors().SelectMany(s => s.Split(", ")).Select(a => a.Trim()).Where(a => stack.IsMatch(a)));
        found = found.Distinct().ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
