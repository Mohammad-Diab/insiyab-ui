using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Insiyab.Avalonia.Controls;

/// <summary>A day over its month in a small tile, for a dated row or card. <c>Date</c> fills both in the page's language (Arabic right to left, English left to right, or <c>Locale</c>); <c>Day</c> and <c>Month</c> set them as text. Tone classes <c>ok warn bad info brand</c> as a pill's, <c>filled</c> for a solid tile, sizes <c>sm lg</c>.</summary>
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

    public static readonly DirectProperty<DateBadge, string?> DayTextProperty =
        AvaloniaProperty.RegisterDirect<DateBadge, string?>(nameof(DayText), o => o.DayText);

    public static readonly DirectProperty<DateBadge, string?> MonthTextProperty =
        AvaloniaProperty.RegisterDirect<DateBadge, string?>(nameof(MonthText), o => o.MonthText);

    private string? _dayText;
    private string? _monthText;

    public DateTime? Date { get => GetValue(DateProperty); set => SetValue(DateProperty, value); }

    /// <summary>The day as written; it wins over <see cref="Date"/>.</summary>
    public string? Day { get => GetValue(DayProperty); set => SetValue(DayProperty, value); }

    /// <summary>The month as written; it wins over <see cref="Date"/>.</summary>
    public string? Month { get => GetValue(MonthProperty); set => SetValue(MonthProperty, value); }

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
        }
    }
}
