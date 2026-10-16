using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The Borders and TextBlocks the library builds in code skip the app's Border.* and TextBlock.* styles and still look as before; the tooltip settings are defaults, not a style on every control.
[Collection("Pickers")]
public class StylePartsTests : IDisposable
{
    private static readonly Func<DateTime> FixedToday = () => new DateTime(2026, 9, 15);
    private static readonly DateTime Sep15 = new(2026, 9, 15);
    private readonly Func<DateTime> _now = MonthCalendar.Now;

    public StylePartsTests()
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

    private static ThemeVariant Theme(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static StackPanel Spread(Control child) => new() { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, Children = { child } };

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

    private static ChoiceMatrix Survey(string? cls)
    {
        var m = new ChoiceMatrix();
        if (cls is not null) m.Classes.Add(cls);
        m.Columns.Add(new MatrixColumn { Header = "ضعيف", Value = "1", Description = "وصف قصير للمستوى.", Points = "1" });
        m.Columns.Add(new MatrixColumn { Header = "جيد", Value = "3", Description = "وصف قصير للمستوى.", Points = "3" });
        m.Columns.Add(new MatrixColumn { Header = "ممتاز", Value = "5", Description = "وصف قصير للمستوى.", Points = "5" });
        m.Rows.Add(new MatrixRow { Header = "الأول", SelectedIndex = 1 });
        m.Rows.Add(new MatrixRow { Header = "الثاني" });
        return m;
    }

    private static T First<T>(Visual root, string cls) where T : StyledElement =>
        root.GetVisualDescendants().OfType<T>().First(e => e.Classes.Contains(cls));

    private static string F(double d) => double.IsNaN(d) ? "NaN" : Math.Round(d, 1).ToString(CultureInfo.InvariantCulture);

    private static string B(IBrush? brush) => brush switch
    {
        null => "-",
        ISolidColorBrush s => s.Color + "@" + F(s.Opacity),
        IGradientBrush g => g.GetType().Name + "(" + string.Join(" ", g.GradientStops.Select(x => x.Color + ":" + F(x.Offset))) + ")@" + F(g.Opacity),
        _ => brush.GetType().Name,
    };

    // Every Border and TextBlock under the root, in tree order, with what styles decide; the type is left out, so the dumps before and after the change compare line by line.
    private static string Dump(Visual root)
    {
        var sb = new StringBuilder();
        foreach (var v in root.GetSelfAndVisualDescendants())
        {
            if (v is not (Border or TextBlock) || v is FocusRing) continue;
            var c = (Control)v;
            var at = c.TranslatePoint(default, root) ?? default;
            sb.Append(v is Border ? "B [" : "T [").Append(string.Join(" ", c.Classes.Where(k => !k.StartsWith(':')).OrderBy(k => k, StringComparer.Ordinal))).Append("] ")
                .Append(F(at.X)).Append(',').Append(F(at.Y)).Append(' ').Append(F(c.Bounds.Width)).Append('x').Append(F(c.Bounds.Height))
                .Append(" vis=").Append(c.IsEffectivelyVisible ? 1 : 0).Append(" op=").Append(F(c.Opacity)).Append(" m=").Append(c.Margin);
            if (v is Border b)
                sb.Append(" bg=").Append(B(b.Background)).Append(" bd=").Append(B(b.BorderBrush)).Append(" bt=").Append(b.BorderThickness)
                    .Append(" cr=").Append(b.CornerRadius).Append(" pad=").Append(b.Padding).Append(" sh=").Append(b.BoxShadow)
                    .Append(" fg=").Append(B(b.GetValue(TextElement.ForegroundProperty))).Append(" fs=").Append(F(b.GetValue(TextElement.FontSizeProperty)));
            if (v is TextBlock t)
                sb.Append(" text=\"").Append(t.Text).Append("\" fs=").Append(F(t.FontSize)).Append(" fw=").Append(t.FontWeight).Append(" ff=").Append(t.FontFamily)
                    .Append(" fg=").Append(B(t.Foreground)).Append(" lh=").Append(F(t.LineHeight)).Append(" ls=").Append(F(t.LetterSpacing))
                    .Append(" ta=").Append(t.TextAlignment).Append(" trim=").Append(t.TextTrimming).Append(" wrap=").Append(t.TextWrapping)
                    .Append(" fd=").Append(t.FlowDirection).Append(" clip=").Append(t.ClipToBounds);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // The window's picture and the root's dump, under the shots folder, for the before and after comparison.
    private static void Keep(Window w, Visual root, string name)
    {
        Capture(w, name);
        File.WriteAllText(Path.Combine(ShotsDir(), name + ".txt"), Dump(root));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Month_parts_keep_their_look(string theme, string dir)
    {
        var ev = Events();
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Date = Sep15, Events = ev, Width = 760 };
        var w = Show(Spread(month), Dir(dir), Theme(theme), 820, 900);
        Keep(w, month, $"parts-month-{theme}-{dir}");
        var cell = month.Cells[new DateTime(2026, 9, 10)];
        AssertBrush(cell, "InsGlassInner", () => cell.Background);
        Assert.Equal(new Thickness(6), cell.Padding);
        Assert.Equal(100, cell.Height);
        Assert.Equal(Colors.Transparent, ColorOf(month.Cells.First(kv => kv.Key.Month != 9).Value.Background));
        var chip = First<Border>(month.Cells[new DateTime(2026, 9, 3)], "month-event");
        AssertBrush(chip, "InsToneWarn", () => chip.BorderBrush);
        AssertBrush(chip, "InsToneWarnBg", () => chip.Background);
        Assert.Equal(new Thickness(3, 0, 0, 0), chip.BorderThickness);
        var wd = First<TextBlock>(month, "month-wd");
        AssertBrush(wd, "InsLabel", () => wd.Foreground);
        Assert.Equal(FontWeight.Bold, wd.FontWeight);
        Assert.Equal(new Thickness(0, 4, 0, 6), wd.Margin);
        var num = (TextBlock)First<Border>(month.Cells[Sep15], "month-num").Child!;
        AssertBrush(num, "InsOnPrimary", () => num.Foreground);
        Assert.Equal(((FontFamily)Res(num, "InsFontBodyEn")).Name, num.FontFamily.Name);
        var more = First<TextBlock>(month, "month-more");
        AssertBrush(more, "InsText2", () => more.Foreground);
        Assert.Equal(FlowDirection.LeftToRight, more.FlowDirection);

        month.View = MonthCalendarView.Week;
        Settle(300);
        Keep(w, month, $"parts-week-{theme}-{dir}");
        var frame = First<Border>(month, "week-frame");
        AssertBrush(frame, "InsLineGlass", () => frame.BorderBrush);
        Assert.Equal(new Thickness(1), frame.BorderThickness);
        var today = month.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("week-day") && b.Classes.Contains("today"));
        AssertBrush(today, "InsZebra", () => today.Background);
        var band = month.Bands[(Sep15, false)];
        AssertBrush(band, "InsToneBrandBg", () => band.Background);
        var line = First<Border>(month.NowLine!, "week-now-line");
        AssertBrush(line, "InsToneBad", () => line.Background);
        Assert.Equal(2, line.Height);
        var cut = month.Chips[ev[9]];
        Assert.Equal(new CornerRadius(0, 0, 5, 5), cut.CornerRadius);
        Assert.Equal(new Thickness(6, 3), cut.Padding);
        var hour = First<TextBlock>(month, "week-hour");
        AssertBrush(hour, "InsText3", () => hour.Foreground);
        Assert.Equal(11, hour.FontSize);

        month.View = MonthCalendarView.Day;
        month.WorkHoursOnly = false;
        Settle(300);
        Keep(w, month, $"parts-day-{theme}-{dir}");
        var gap = First<Border>(month, "week-bar-gap");
        Assert.Equal((double)Res(gap, "InsScrollbarSize"), gap.Width);

        month.View = MonthCalendarView.Month;
        month.Width = 400;
        Settle(300);
        Keep(w, month, $"parts-month-narrow-{theme}-{dir}");
        var narrow = month.Cells[new DateTime(2026, 9, 10)];
        Assert.Equal(52, narrow.Height);
        Assert.Equal(new Thickness(4), narrow.Padding);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Calendar_parts_keep_their_look(string theme, string dir)
    {
        var range = new DateCalendar { Mode = DatePickMode.Range, CalendarKind = CalendarKind.Hijri, ShowGregorian = true, IsDual = false, SelectedDate = new DateTime(2026, 9, 10), EndDate = new DateTime(2026, 9, 18) };
        var dual = new DateCalendar { Mode = DatePickMode.Range, IsDual = true, SelectedDate = new DateTime(2026, 9, 10), IsPickingEnd = true };
        var time = new DateCalendar { Mode = DatePickMode.DateTime, SelectedDate = Sep15, Time = new TimeSpan(9, 30, 0), IsRequired = true };
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, HorizontalAlignment = HorizontalAlignment.Left, Children = { range, dual, time } };
        var w = Show(page, Dir(dir), Theme(theme), 760, 1400);
        foreach (var c in new[] { range, dual, time }) c.Show(new DateTime(2026, 9, 10));
        Settle(300);
        Keep(w, page, $"parts-calendar-{theme}-{dir}");
        var band = range.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("cal-cell") && b.Classes.Contains("band"));
        AssertBrush(band, "InsToneBrandBg", () => band.Background);
        var wd = First<TextBlock>(range, "cal-wd");
        AssertBrush(wd, "InsLabel", () => wd.Foreground);
        var hint = First<TextBlock>(range, "cal-hint");
        AssertBrush(hint, "InsToneBrand", () => hint.Foreground);
        var foot = First<Border>(range, "cal-foot");
        AssertBrush(foot, "InsLine", () => foot.BorderBrush);
        Assert.Equal(new Thickness(0, 8, 0, 0), foot.Margin);
        var track = First<Border>(range, "cal-switch");
        AssertBrush(track, "InsCtlBgHover", () => track.Background);
        var next = First<TextBlock>(dual, "cal-title-next");
        AssertBrush(next, "InsText1", () => next.Foreground);
        Assert.Equal(FontWeight.Bold, next.FontWeight);
        var side = First<Border>(time, "cal-time");
        Assert.Equal(new Thickness(1, 0, 0, 0), side.BorderThickness);
        Assert.Equal(new Thickness(12, 0, 0, 0), side.Padding);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Birth_parts_keep_their_look(string theme, string dir)
    {
        var inline = new BirthDateField { Layout = BirthLayout.Inline };
        var field = new BirthDateField();
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 24, Width = 420, HorizontalAlignment = HorizontalAlignment.Left, Children = { inline, field } };
        var w = Show(page, Dir(dir), Theme(theme), 520, 700);
        inline.SelectedDate = new DateTime(1993, 1, 22);
        field.SelectedDate = new DateTime(1993, 1, 22);
        Settle(300);
        Keep(w, page, $"parts-birth-{theme}-{dir}");
        Border Box(BirthDateField f, char k) =>
            First<Border>(f.GetVisualDescendants().OfType<StackPanel>().First(s => s.Classes.Contains("birth-seg") && s.Classes.Contains(k.ToString())), "birth-box");
        var box = Box(inline, 'd');
        Assert.Equal(52, box.Height);
        Assert.Equal(76, box.Width);
        AssertBrush(box, "InsCtlBg", () => box.Background);
        AssertBrush(box, "InsCtlBd", () => box.BorderBrush);
        var val = First<TextBlock>(inline, "birth-val");
        Assert.Equal(28, val.FontSize);
        Assert.Equal(FontWeight.ExtraBold, val.FontWeight);
        Assert.Equal(1, First<TextBlock>(inline, "birth-cap").Opacity, 2);
        var control = First<Border>(field, "birth-control");
        Assert.Equal((double)Res(control, "InsFieldH"), control.MinHeight);
        AssertBrush(control, "InsCtlBg", () => control.Background);
        var fieldBox = Box(field, 'd');
        Assert.Equal(32, fieldBox.Height);
        Assert.Equal(Colors.Transparent, ColorOf(fieldBox.Background));
        var sep = First<TextBlock>(field, "birth-sep");
        AssertBrush(sep, "InsText3", () => sep.Foreground);

        Click(field.PickButton!, w);
        Assert.True(field.Flyout!.IsOpen);
        var pop = (Control)field.Flyout.Content!;
        File.WriteAllText(Path.Combine(ShotsDir(), $"parts-birth-pop-{theme}-{dir}.txt"), Dump(pop));
        Capture(w, $"parts-birth-pop-{theme}-{dir}");
        Assert.Equal(304, pop.Width);
        var foot = First<Border>(pop, "birth-foot");
        AssertBrush(foot, "InsLine", () => foot.BorderBrush);
        Assert.Equal(new Thickness(0, 1, 0, 0), foot.BorderThickness);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Matrix_parts_keep_their_look(string theme, string dir)
    {
        var m = Survey(null);
        var cards = Survey("cards");
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 24, Children = { m, cards } };
        var w = Show(page, Dir(dir), Theme(theme), 760, 700);
        Keep(w, page, $"parts-matrix-{theme}-{dir}");
        var cell = m.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("matrix-cell") && b.Classes.Contains("checked"));
        AssertBrush(cell, "InsToneBrandBg", () => cell.Background);
        var th = First<Border>(m, "matrix-th");
        AssertBrush(th, "InsGlassInner", () => th.Background);
        Assert.Equal(new Thickness(12, 10, 16, 10), th.Padding);
        var header = (TextBlock)th.Child!;
        if (dir == "rtl") Assert.Equal(22.1, header.LineHeight, 1);
        else Assert.True(double.IsNaN(header.LineHeight));
        var col = First<TextBlock>(m, "matrix-col");
        AssertBrush(col, "InsLabel", () => col.Foreground);
        Assert.Equal(FontWeight.ExtraBold, col.FontWeight);
        var cardCell = First<Border>(cards, "matrix-cell");
        Assert.Equal(Colors.Transparent, ColorOf(cardCell.Background));
        Assert.Equal(new Thickness(4), cardCell.Padding);
        var pill = First<Border>(cards.GetVisualDescendants().OfType<RadioButton>().First(r => r.IsChecked == true), "matrix-points");
        AssertBrush(pill, "InsPrimaryMid", () => pill.Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Palette_parts_keep_their_look(string theme, string dir)
    {
        var palette = new CommandPalette
        {
            Items =
            {
                new PaletteItem { Label = "الأزرار", Group = "المكوّنات", Hint = "Ctrl+B", Icon = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z"), IsCurrent = true },
                new PaletteItem { Label = "الجداول", Group = "المكوّنات" },
                new PaletteItem { Label = "الألوان", Group = "الأسس" },
            },
        };
        var w = Show(new Panel(), Dir(dir), Theme(theme), 800, 640);
        palette.Open(w);
        Settle();
        Keep(w, palette, $"parts-palette-{theme}-{dir}");
        var rows = palette.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("palette-item")).ToList();
        var active = rows.Single(r => r.Classes.Contains("active"));
        AssertBrush(active, "InsPopItemHover", () => active.Background);
        AssertBrush(active, "InsPopItemHoverFg", () => active.GetValue(TextElement.ForegroundProperty));
        var rest = rows.First(r => !r.Classes.Contains("active"));
        AssertBrush(rest, "InsPopItemRest", () => rest.Background);
        Assert.Equal(44, rest.MinHeight);
        var label = palette.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("palette-label") && t.Text == "الجداول");
        Assert.Equal(FontWeight.SemiBold, label.FontWeight);
        var head = First<TextBlock>(palette, "palette-head");
        AssertBrush(head, "InsText3", () => head.Foreground);
        Assert.Equal(11, head.FontSize);
        var kbd = palette.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("kbd") && b.Parent is Grid);
        AssertBrush(kbd, "InsCtlBg", () => kbd.Background);
        Assert.Equal(new Thickness(1, 1, 1, 2), kbd.BorderThickness);
        Assert.False(First<TextBlock>(palette, "palette-where").IsVisible);

        palette.GetVisualDescendants().OfType<TextBox>().First(t => t.Name == "PART_Input").Text = "ال";
        Settle(300);
        Keep(w, palette, $"parts-palette-search-{theme}-{dir}");
        Assert.True(First<TextBlock>(palette, "palette-where").IsVisible);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Chart_parts_keep_their_look(string theme, string dir)
    {
        var line = new Chart { Type = ChartType.Line, Labels = "س,ح,ن,ث", Height = 220 };
        line.Series.Add(new ChartSeries("هذا الأسبوع", 32, 41, 38, 52));
        line.Series.Add(new ChartSeries("الأسبوع الماضي", 28, 35, 40, 44));
        var donut = new Chart { Type = ChartType.Donut, Labels = "أ,ب,ج", Values = "120,80,60", SeriesName = "العدد", Height = 220 };
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 520, HorizontalAlignment = HorizontalAlignment.Left, Children = { line, donut } };
        var w = Show(page, Dir(dir), Theme(theme), 600, 700);
        Settle(800);
        line.ShowCategoryTip(1, 120, 20);
        Settle(200);
        Keep(w, page, $"parts-chart-{theme}-{dir}");
        var legend = Part<WrapPanel>(line, "PART_Legend");
        var name = legend.GetVisualDescendants().OfType<TextBlock>().First();
        AssertBrush(name, "InsText2", () => name.Foreground);
        Assert.Equal((double)Res(name, "InsFsXs"), name.FontSize);
        Assert.Equal(new CornerRadius(3), First<Border>(legend, "chart-key").CornerRadius);
        var tipRows = Part<StackPanel>(line, "PART_TipRows");
        Assert.True(tipRows.IsVisible);
        Assert.Equal(2, tipRows.Children.Count);
    }

    [AvaloniaFact]
    public void Code_built_parts_keep_their_hover_and_focus_looks()
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Date = Sep15, Events = Events(), Width = 760 };
        var w = Show(Spread(month), FlowDirection.RightToLeft, null, 820, 900);
        var cell = month.Cells[new DateTime(2026, 9, 10)];
        w.MouseMove(Center(cell, w));
        AssertBrush(cell, "InsSurfaceHover", () => cell.Background);
        var link = First<Border>(month.Cells[new DateTime(2026, 9, 3)], "month-event");
        w.MouseMove(Center(link, w));
        AssertBrush(link, "InsToneWarn", () => link.Background);
        AssertBrush(link, "InsOnPrimary", () => link.GetValue(TextElement.ForegroundProperty));
        month.View = MonthCalendarView.Week;
        Settle(300);
        var head = month.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("week-head") && b.Classes.Contains("link"));
        w.MouseMove(Center(head, w));
        AssertBrush(head, "InsSurfaceHover", () => head.Background);
        var band = month.Bands[(Sep15, false)];
        w.MouseMove(Center(band, w));
        AssertBrush(band, "InsSurfaceHover", () => band.Background);

        var m = Survey(null);
        var mw = Show(m, FlowDirection.RightToLeft, null, 700, 400);
        var th = First<Border>(m, "matrix-th");
        mw.MouseMove(Center(th, mw));
        AssertBrush(th, "InsSurfaceHover", () => th.Background);

        var field = new BirthDateField();
        var bw = Show(new StackPanel { Margin = new Thickness(20), Width = 360, HorizontalAlignment = HorizontalAlignment.Left, Children = { field } }, FlowDirection.RightToLeft, null, 520, 300);
        var control = First<Border>(field, "birth-control");
        bw.MouseMove(Center(control, bw));
        AssertBrush(control, "InsPickerHoverBorder", () => control.BorderBrush);
        field.DayBox.Focus();
        Settle(300);
        AssertBrush(control, "InsPickerFocusBorder", () => control.BorderBrush);

        var palette = new CommandPalette { Items = { new PaletteItem { Label = "الأزرار" }, new PaletteItem { Label = "الجداول" } } };
        var pw = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(pw);
        Settle();
        var active = palette.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("palette-item") && b.Classes.Contains("active"));
        Assert.False(active.RenderTransform is null || active.RenderTransform.Value.IsIdentity);
        Motion.SetStill(pw, true);
        Settle(300);
        Assert.True(active.RenderTransform is null || active.RenderTransform.Value.IsIdentity);
    }

    // An app's own control made straight from Control: it takes the library's tip settings once it has a tip.
    private sealed class Bare : Control
    {
    }

    [AvaloniaFact]
    public void Tooltip_settings_are_defaults_that_need_no_style()
    {
        foreach (var c in new Control[] { new Border(), new TextBlock(), new Button(), new StackPanel(), new Icon(), new MonthCalendar(), new global::Avalonia.Controls.Shapes.Ellipse() })
        {
            Assert.Equal(PlacementMode.Top, c.GetValue(ToolTip.PlacementProperty));
            Assert.Equal(0, c.GetValue(ToolTip.VerticalOffsetProperty));
            Assert.Equal(400, c.GetValue(ToolTip.ShowDelayProperty));
            Assert.Equal(0, c.GetValue(ToolTip.BetweenShowDelayProperty));
            Assert.True(c.GetValue(ToolTip.ShouldUseOverlayLayerProperty));
        }
        var bare = new Bare();
        ToolTip.SetTip(bare, "تلميح");
        Assert.Equal(PlacementMode.Top, bare.GetValue(ToolTip.PlacementProperty));
        Assert.Equal(0, bare.GetValue(ToolTip.BetweenShowDelayProperty));
        Assert.True(bare.GetValue(ToolTip.ShouldUseOverlayLayerProperty));
    }

    private static IEnumerable<IStyle> Walk(IStyle style)
    {
        yield return style;
        IEnumerable<IStyle> kids = style is StyleInclude { Loaded: { } loaded } ? [loaded] : style.Children;
        foreach (var kid in kids)
            foreach (var s in Walk(kid)) yield return s;
    }

    [AvaloniaFact]
    public void No_style_sets_the_tooltip_settings_on_every_control()
    {
        var found = Walk(Application.Current!.Styles).OfType<Style>()
            .Where(s => s.Setters.OfType<Setter>().Any(x => x.Property == ToolTip.PlacementProperty || x.Property == ToolTip.ShowDelayProperty))
            .Select(s => s.Selector?.ToString() ?? "?").ToList();
        Assert.True(found.Count == 0, string.Join("; ", found));
    }

    [AvaloniaFact]
    public void An_apps_style_and_local_value_still_win_over_the_tip_defaults()
    {
        var styled = new Button { Content = "أ", Classes = { "below" } };
        var local = new Button { Content = "ب" };
        ToolTip.SetPlacement(local, PlacementMode.Right);
        var panel = new StackPanel { Children = { styled, local } };
        panel.Styles.Add(new Style(x => x.OfType<Button>().Class("below")) { Setters = { new Setter(ToolTip.PlacementProperty, PlacementMode.Bottom) } });
        Show(panel);
        Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(styled));
        Assert.Equal(PlacementMode.Right, ToolTip.GetPlacement(local));
        local.ClearValue(ToolTip.PlacementProperty);
        Assert.Equal(PlacementMode.Top, ToolTip.GetPlacement(local));
    }

    // Every Border and TextBlock carrying one of the control's own classes, and any TextBlock right inside such a Border, is the control's typed part.
    private static void AssertTyped(Visual root, Type border, Type text, params string[] prefixes)
    {
        bool Own(StyledElement e) => e.Classes.Any(c => prefixes.Any(p => c.StartsWith(p, StringComparison.Ordinal)));
        var all = root.GetSelfAndVisualDescendants().ToList();
        var borders = all.OfType<Border>().Where(Own).ToList();
        var texts = all.OfType<TextBlock>().Where(t => Own(t) || (t.Parent is Border p && Own(p))).ToList();
        Assert.NotEmpty(borders);
        Assert.NotEmpty(texts);
        Assert.All(borders, b => Assert.Equal(border, b.GetType()));
        Assert.All(texts, t => Assert.Equal(text, t.GetType()));
    }

    // Every StackPanel carrying one of the control's own classes is the control's typed stack.
    private static void AssertTypedStacks(Visual root, Type stack, params string[] prefixes)
    {
        var stacks = root.GetSelfAndVisualDescendants().OfType<StackPanel>().Where(s => s.Classes.Any(c => prefixes.Any(p => c.StartsWith(p, StringComparison.Ordinal)))).ToList();
        Assert.NotEmpty(stacks);
        Assert.All(stacks, s => Assert.Equal(stack, s.GetType()));
    }

    [AvaloniaFact]
    public void Code_built_parts_are_typed()
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Date = Sep15, Events = Events(), Width = 760 };
        Show(Spread(month), FlowDirection.RightToLeft, null, 820, 900);
        AssertTyped(month, typeof(MonthBorder), typeof(MonthText), "month-", "week-");
        AssertTypedStacks(month, typeof(MonthStack), "month-", "week-");
        month.View = MonthCalendarView.Week;
        Settle(200);
        Assert.NotEmpty(month.Bands);
        Assert.NotNull(month.NowLine);
        AssertTyped(month, typeof(MonthBorder), typeof(MonthText), "month-", "week-");
        AssertTypedStacks(month, typeof(MonthStack), "month-", "week-");
        var heads = month.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("week-head")).ToList();
        Assert.NotEmpty(heads);
        Assert.All(heads, h => Assert.IsType<MonthStack>(h.Child));
        month.View = MonthCalendarView.Day;
        month.WorkHoursOnly = false;
        Settle(200);
        AssertTyped(month, typeof(MonthBorder), typeof(MonthText), "month-", "week-");
        AssertTypedStacks(month, typeof(MonthStack), "month-", "week-");

        var range = new DateCalendar { Mode = DatePickMode.Range, CalendarKind = CalendarKind.Hijri, IsDual = true, SelectedDate = new DateTime(2026, 9, 10), EndDate = new DateTime(2026, 9, 18) };
        var time = new DateCalendar { Mode = DatePickMode.DateTime, SelectedDate = Sep15 };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 16, HorizontalAlignment = HorizontalAlignment.Left, Children = { range, time } }, FlowDirection.RightToLeft, null, 760, 1100);
        AssertTyped(range, typeof(CalendarBorder), typeof(CalendarText), "cal-");
        AssertTyped(time, typeof(CalendarBorder), typeof(CalendarText), "cal-");

        var hijri = new DateCalendar { CalendarKind = CalendarKind.Hijri, SelectedDate = new DateTime(2026, 9, 24) };
        var hw = Show(new StackPanel { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, Children = { hijri } }, FlowDirection.RightToLeft, null, 800, 700);
        hijri.Show(new DateTime(2026, 9, 24));
        Settle();
        Button Option(string tag) => hijri.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-switch-option") && b.Tag as string == tag);
        void Tap(Control c)
        {
            var p = Center(c, hw);
            hw.MouseMove(p);
            hw.MouseDown(p, MouseButton.Left);
            hw.MouseUp(p, MouseButton.Left);
        }
        Tap(Option("gregory"));
        Settle();
        Tap(Option("home"));
        Settle();
        Tap(Option("gregory"));
        Assert.IsType<CalendarBorder>(First<Border>(hijri, "cal-switch-face"));
        Settle();

        var inline = new BirthDateField { Layout = BirthLayout.Inline };
        var field = new BirthDateField();
        var bw = Show(new StackPanel { Margin = new Thickness(20), Spacing = 24, Width = 420, HorizontalAlignment = HorizontalAlignment.Left, Children = { inline, field } }, FlowDirection.RightToLeft, null, 520, 700);
        inline.SelectedDate = new DateTime(1993, 1, 22);
        field.SelectedDate = new DateTime(1993, 1, 22);
        Settle(200);
        AssertTyped(inline, typeof(BirthBorder), typeof(BirthText), "birth-");
        AssertTyped(field, typeof(BirthBorder), typeof(BirthText), "birth-");
        AssertTypedStacks(inline, typeof(BirthStack), "birth-");
        AssertTypedStacks(field, typeof(BirthStack), "birth-");
        Click(field.PickButton!, bw);
        var pop = (Control)field.Flyout!.Content!;
        AssertTyped(pop, typeof(BirthBorder), typeof(BirthText), "birth-");
        AssertTypedStacks(pop, typeof(BirthStack), "birth-");
        var shared = inline.GetVisualDescendants().Concat(pop.GetVisualDescendants()).OfType<Border>().Where(b => b.Classes.Contains("drum-lens") || b.Classes.Contains("picker-glass")).ToList();
        Assert.NotEmpty(shared);
        Assert.All(shared, b => Assert.Equal(typeof(Border), b.GetType()));

        var m = Survey("cards");
        Show(m, FlowDirection.RightToLeft, null, 760, 500);
        AssertTyped(m, typeof(MatrixBorder), typeof(MatrixText), "matrix-");
        Assert.IsType<MatrixText>(m.GetVisualDescendants().OfType<Panel>().First(p => p.Classes.Contains("matrix-head")).Children[0]);

        var palette = new CommandPalette { Items = { new PaletteItem { Label = "الأزرار", Group = "المكوّنات", Hint = "Ctrl+B" }, new PaletteItem { Label = "الجداول", Group = "المكوّنات" } } };
        var pw = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(pw);
        Settle();
        AssertTyped(palette, typeof(PaletteBorder), typeof(PaletteText), "palette-item", "palette-head", "palette-label", "palette-where");
        var kbd = palette.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("kbd") && b.Parent is Grid);
        Assert.Equal(typeof(Border), kbd.GetType());
        Assert.Equal(typeof(TextBlock), kbd.Child!.GetType());

        var line = new Chart { Type = ChartType.Line, Labels = "س,ح,ن,ث", Height = 220 };
        line.Series.Add(new ChartSeries("هذا الأسبوع", 32, 41, 38, 52));
        line.Series.Add(new ChartSeries("الأسبوع الماضي", 28, 35, 40, 44));
        Show(new StackPanel { Margin = new Thickness(20), Width = 520, Children = { line } }, FlowDirection.RightToLeft, null, 600, 500);
        line.ShowCategoryTip(1, 120, 20);
        Settle(200);
        var built = Part<WrapPanel>(line, "PART_Legend").GetVisualDescendants().Concat(Part<StackPanel>(line, "PART_TipRows").GetVisualDescendants()).ToList();
        Assert.NotEmpty(built.OfType<Border>());
        Assert.NotEmpty(built.OfType<TextBlock>());
        Assert.All(built.OfType<Border>(), b => Assert.IsType<TemplateBorder>(b));
        Assert.All(built.OfType<TextBlock>(), t => Assert.IsType<TemplateText>(t));
    }
}
