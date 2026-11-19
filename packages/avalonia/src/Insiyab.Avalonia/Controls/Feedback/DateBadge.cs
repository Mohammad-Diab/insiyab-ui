using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Insiyab.Avalonia.Controls;

/// <summary>A day over its month in a small tile, for a dated row or card. <c>Date</c> fills both in the page's language (Arabic right to left, English left to right, or <c>Locale</c>); <c>Day</c> and <c>Month</c> set them as text. <c>Icon</c> stands in the day's place when there is no day to show (a date not set yet). With no month (a word alone, «يومان»), the day stands in the middle of the tile, which keeps its height. Tone classes <c>ok warn bad info brand</c> as a pill's, <c>filled</c> for a solid tile, sizes <c>sm lg</c>.</summary>
[PseudoClasses(":icon", ":no-month")]
public class DateBadge : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> DateProperty =
        AvaloniaProperty.Register<DateBadge, DateTime?>(nameof(Date));

    public static readonly StyledProperty<string?> DayProperty =
        AvaloniaProperty.Register<DateBadge, string?>(nameof(Day));

    public static readonly StyledProperty<string?> MonthProperty =
        AvaloniaProperty.Register<DateBadge, string?>(nameof(Month));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<DateBadge, string?>(nameof(Locale));

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<DateBadge, Geometry?>(nameof(Icon));

    public static readonly DirectProperty<DateBadge, string?> DayTextProperty =
        AvaloniaProperty.RegisterDirect<DateBadge, string?>(nameof(DayText), o => o.DayText);

    public static readonly DirectProperty<DateBadge, string?> MonthTextProperty =
        AvaloniaProperty.RegisterDirect<DateBadge, string?>(nameof(MonthText), o => o.MonthText);

    private string? _dayText;
    private string? _monthText;
    private TextBlock? _day;
    private double _lift;

    public DateTime? Date { get => GetValue(DateProperty); set => SetValue(DateProperty, value); }

    /// <summary>The day as written; it wins over <see cref="Date"/>.</summary>
    public string? Day { get => GetValue(DayProperty); set => SetValue(DayProperty, value); }

    /// <summary>The month as written; it wins over <see cref="Date"/>.</summary>
    public string? Month { get => GetValue(MonthProperty); set => SetValue(MonthProperty, value); }

    /// <summary>An icon in the day's place, shown while there is no day (no <see cref="Day"/> text and no <see cref="Date"/>).</summary>
    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>"ar" or "en" for the month's name; unset, it follows the direction.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    /// <summary>The day as shown.</summary>
    public string? DayText { get => _dayText; private set => SetAndRaise(DayTextProperty, ref _dayText, value); }

    /// <summary>The month as shown.</summary>
    public string? MonthText { get => _monthText; private set => SetAndRaise(MonthTextProperty, ref _monthText, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DateProperty || change.Property == DayProperty || change.Property == MonthProperty
            || change.Property == LocaleProperty || change.Property == FlowDirectionProperty)
        {
            DayText = Day ?? Date?.Day.ToString(CultureInfo.InvariantCulture);
            MonthText = Month ?? (Date is { } d ? PickerLocale.For(Locale, FlowDirection).ShortMonth(d.Month - 1) : null);
            PseudoClasses.Set(":no-month", string.IsNullOrEmpty(MonthText));
        }
        if (change.Property == DateProperty || change.Property == DayProperty || change.Property == IconProperty)
            PseudoClasses.Set(":icon", Icon is not null && string.IsNullOrEmpty(Day ?? Date?.Day.ToString(CultureInfo.InvariantCulture)));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _day = e.NameScope.Find<TextBlock>("PART_Day");
        _lift = 0;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        Lift();
        return size;
    }

    // An Arabic word alone («يومان», «اليوم») stands in the middle of the tile by its ink: the day's line box is the Latin face's (made for figures), which holds Arabic letters low, so the word is lifted by how far its ink's middle sits under the line's; figures and Latin words keep the line box's middle.
    private void Lift()
    {
        if (_day is null) return;
        var lift = PseudoClasses.Contains(":no-month") && IsArabic(DayText) ? InkLift(_day) : 0;
        if (Math.Abs(lift - _lift) < 0.5) return;
        _lift = lift;
        // a margin taken from above and given below moves the word without changing the tile's height (the no-month style's own move stays in RenderTransform)
        _day.Margin = lift == 0 ? default : new Thickness(0, -lift, 0, lift);
    }

    internal static double InkLift(TextBlock text)
    {
        if (text.TextLayout.TextLines is not { Count: > 0 } lines) return 0;
        var line = lines[0];
        Rect? ink = null;
        foreach (var run in line.TextRuns.OfType<ShapedTextRun>())
        {
            var glyphs = run.GlyphRun;
            var box = glyphs.InkBounds.Translate(new Vector(0, line.Baseline - glyphs.BaselineOrigin.Y));
            ink = ink is { } r ? r.Union(box) : box;
        }
        return ink is { Height: > 0 } i ? Math.Round(i.Center.Y - line.Height / 2) : 0;
    }

    private static bool IsArabic(string? text) =>
        text is not null && text.Any(c => c is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F' or >= '\uFB50' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF');
}
