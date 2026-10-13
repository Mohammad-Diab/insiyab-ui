using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Insiyab.Avalonia.Controls;

/// <summary>A date and a time in two fields sharing one box (the web's <c>.ins-input-group[data-ins-datetime]</c>): the date with its calendar, a hairline, then the time with its wheels; <see cref="Value"/> holds both as <c>2026-09-26T14:30</c>.</summary>
[TemplatePart("PART_Date", typeof(DateField))]
[TemplatePart("PART_Time", typeof(TimeField))]
public class DateTimeGroup : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<DateTimeGroup, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<DateTimeGroup, TimeSpan?>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        DateCalendar.MinDateProperty.AddOwner<DateTimeGroup>();

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        DateCalendar.MaxDateProperty.AddOwner<DateTimeGroup>();

    public static readonly StyledProperty<int> MinuteStepProperty =
        AvaloniaProperty.Register<DateTimeGroup, int>(nameof(MinuteStep), 30);

    public static readonly StyledProperty<string?> LocaleProperty =
        DateCalendar.LocaleProperty.AddOwner<DateTimeGroup>();

    public static readonly DirectProperty<DateTimeGroup, string> ValueProperty =
        AvaloniaProperty.RegisterDirect<DateTimeGroup, string>(nameof(Value), o => o.Value);

    private DateField? _date;
    private TimeField? _time;
    private string _value = string.Empty;

    public DateTime? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }

    public TimeSpan? SelectedTime { get => GetValue(SelectedTimeProperty); set => SetValue(SelectedTimeProperty, value); }

    public DateTime? MinDate { get => GetValue(MinDateProperty); set => SetValue(MinDateProperty, value); }

    public DateTime? MaxDate { get => GetValue(MaxDateProperty); set => SetValue(MaxDateProperty, value); }

    /// <summary>The time wheels' minute step; half an hour by default, as the time field's.</summary>
    public int MinuteStep { get => GetValue(MinuteStepProperty); set => SetValue(MinuteStepProperty, value); }

    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    /// <summary>The date and the time together, as the group sends them; empty until both are set.</summary>
    public string Value { get => _value; private set => SetAndRaise(ValueProperty, ref _value, value); }

    internal DateField? DatePart => _date;

    internal TimeField? TimePart => _time;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_date is not null) _date.DateChanged -= OnPartChanged;
        if (_time is not null) _time.TimeChanged -= OnTimeChanged;
        _date = e.NameScope.Find<DateField>("PART_Date");
        _time = e.NameScope.Find<TimeField>("PART_Time");
        if (_date is not null)
        {
            _date.SelectedDate = SelectedDate;
            _date.MinDate = MinDate;
            _date.MaxDate = MaxDate;
            _date.Locale = Locale;
            _date.DateChanged += OnPartChanged;
        }
        if (_time is not null)
        {
            _time.SelectedTime = SelectedTime;
            _time.MinuteStep = MinuteStep;
            _time.Locale = Locale;
            _time.TimeChanged += OnTimeChanged;
        }
        Sync();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedDateProperty && _date is not null) _date.SelectedDate = SelectedDate;
        else if (change.Property == SelectedTimeProperty && _time is not null) _time.SelectedTime = SelectedTime;
        else if (change.Property == MinDateProperty && _date is not null) _date.MinDate = MinDate;
        else if (change.Property == MaxDateProperty && _date is not null) _date.MaxDate = MaxDate;
        else if (change.Property == MinuteStepProperty && _time is not null) _time.MinuteStep = MinuteStep;
        else if (change.Property == LocaleProperty)
        {
            if (_date is not null) _date.Locale = Locale;
            if (_time is not null) _time.Locale = Locale;
        }
        if (change.Property == SelectedDateProperty || change.Property == SelectedTimeProperty) Sync();
    }

    private void OnPartChanged(object? sender, DateChangedEventArgs e)
    {
        SetCurrentValue(SelectedDateProperty, e.Date);
        Sync();
    }

    private void OnTimeChanged(object? sender, TimeSpan? t)
    {
        SetCurrentValue(SelectedTimeProperty, t);
        Sync();
    }

    // datetimeSync: both or nothing.
    private void Sync() =>
        Value = SelectedDate is { } d && SelectedTime is { } t ? $"{PickerParse.IsoOf(d)}T{PickerParse.HhMm(t)}" : string.Empty;
}
