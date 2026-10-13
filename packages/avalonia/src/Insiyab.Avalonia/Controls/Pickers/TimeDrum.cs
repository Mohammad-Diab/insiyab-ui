using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Insiyab.Avalonia.Controls;

/// <summary>Hour, minute and period wheels under one lens (the web's <c>.ins-drum</c>), as the time field and the date-and-time calendar show them.</summary>
[TemplatePart("PART_Hour", typeof(DrumColumn))]
[TemplatePart("PART_Minute", typeof(DrumColumn))]
[TemplatePart("PART_Period", typeof(DrumColumn))]
public class TimeDrum : TemplatedControl
{
    public static readonly StyledProperty<TimeSpan?> TimeProperty =
        AvaloniaProperty.Register<TimeDrum, TimeSpan?>(nameof(Time));

    public static readonly StyledProperty<TimeSpan?> MinTimeProperty =
        AvaloniaProperty.Register<TimeDrum, TimeSpan?>(nameof(MinTime));

    public static readonly StyledProperty<TimeSpan?> MaxTimeProperty =
        AvaloniaProperty.Register<TimeDrum, TimeSpan?>(nameof(MaxTime));

    public static readonly StyledProperty<int> MinuteStepProperty =
        AvaloniaProperty.Register<TimeDrum, int>(nameof(MinuteStep), 30);

    public static readonly StyledProperty<bool> Is12HourProperty =
        AvaloniaProperty.Register<TimeDrum, bool>(nameof(Is12Hour), true);

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<TimeDrum, string?>(nameof(Locale));

    private DrumColumn? _hour;
    private DrumColumn? _minute;
    private DrumColumn? _period;
    private int[] _mins = [];
    private TimeSpan? _value;
    private bool _syncing;

    /// <summary>Raised when the person turns the wheels to a new time (the web's drum pick); setting <see cref="Time"/> from code does not raise it.</summary>
    public event EventHandler<TimeSpan>? TimePicked;

    public TimeSpan? Time { get => GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    public TimeSpan? MinTime { get => GetValue(MinTimeProperty); set => SetValue(MinTimeProperty, value); }

    public TimeSpan? MaxTime { get => GetValue(MaxTimeProperty); set => SetValue(MaxTimeProperty, value); }

    /// <summary>The minute wheel's step; the time field's default is half an hour.</summary>
    public int MinuteStep { get => GetValue(MinuteStepProperty); set => SetValue(MinuteStepProperty, value); }

    public bool Is12Hour { get => GetValue(Is12HourProperty); set => SetValue(Is12HourProperty, value); }

    /// <summary>"ar" or "en"; unset, it follows the direction.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    internal DrumColumn? HourColumn => _hour;
    internal DrumColumn? MinuteColumn => _minute;
    internal DrumColumn? PeriodColumn => _period;

    private PickerLocale Words => PickerLocale.For(Locale, FlowDirection);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        foreach (var c in new[] { _hour, _minute, _period })
            if (c is not null) c.Settled -= OnColumnSettled;
        _hour = e.NameScope.Find<DrumColumn>("PART_Hour");
        _minute = e.NameScope.Find<DrumColumn>("PART_Minute");
        _period = e.NameScope.Find<DrumColumn>("PART_Period");
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MinuteStepProperty || change.Property == Is12HourProperty || change.Property == LocaleProperty || change.Property == FlowDirectionProperty)
            Build();
        else if (change.Property == TimeProperty && !_syncing)
            Place(Time ?? DefaultFor(MinuteStep), false);
        else if (change.Property == MinTimeProperty || change.Property == MaxTimeProperty)
            Marks();
    }

    private void Build()
    {
        if (_hour is null || _minute is null) return;
        var words = Words;
        var h12 = Is12Hour;
        var step = Math.Max(1, Math.Min(MinuteStep, 60));
        var hours = new List<string>();
        for (var h = 0; h < (h12 ? 12 : 24); h++) hours.Add(h12 ? PickerLocale.N(h == 0 ? 12 : h) : $"{h:00}");
        var minutes = new List<string>();
        var mins = new List<int>();
        for (var m = 0; m < 60; m += step) { minutes.Add($"{m:00}"); mins.Add(m); }
        _mins = [.. mins];
        _hour.IsLooping = true;
        _minute.IsLooping = mins.Count > 2;
        _hour.Items = hours;
        _minute.Items = minutes;
        _hour.Settled -= OnColumnSettled;
        _minute.Settled -= OnColumnSettled;
        _hour.Settled += OnColumnSettled;
        _minute.Settled += OnColumnSettled;
        if (_period is not null)
        {
            _period.Settled -= OnColumnSettled;
            _period.IsVisible = h12;
            _period.Classes.Set("ar", words.IsArabic);
            _period.Items = [words.Am, words.Pm];
            _period.Settled += OnColumnSettled;
        }
        _hour.IsOut = v => Out(HourSpan(v));
        _minute.IsOut = v => { var hour = Read().Hours * 60; var m = hour + _mins[Math.Clamp(v, 0, _mins.Length - 1)]; return Out((m, m)); };
        if (_period is not null) _period.IsOut = v => Out((v * 720, v * 720 + 719));
        Place(Time ?? DefaultFor(MinuteStep), false);
    }

    private (int A, int B) HourSpan(int v)
    {
        var pm = Is12Hour && _period?.Value == 1 ? 12 : 0;
        var h = (v + pm) * 60;
        return (h, h + 59);
    }

    private bool Out((int A, int B) span)
    {
        var lo = MinTime is { } l ? (int)l.TotalMinutes : (int?)null;
        var hi = MaxTime is { } x ? (int)x.TotalMinutes : (int?)null;
        return (lo is not null && span.B < lo) || (hi is not null && span.A > hi);
    }

    // The next step of the clock from now, the time a wheel shows before anything is chosen.
    internal static TimeSpan DefaultFor(int step)
    {
        step = Math.Max(1, step);
        var now = DateTime.Now;
        var m = (int)(Math.Ceiling((now.Hour * 60 + now.Minute) / (double)step) * step) % 1440;
        return new TimeSpan(m / 60, m % 60, 0);
    }

    /// <summary>The time the wheels show right now.</summary>
    public TimeSpan Read()
    {
        if (_hour is null || _minute is null || _mins.Length == 0) return Time ?? TimeSpan.Zero;
        var h = _hour.Value;
        if (Is12Hour) h += _period?.Value == 1 ? 12 : 0;
        return new TimeSpan(h, _mins[Math.Clamp(_minute.Value, 0, _mins.Length - 1)], 0);
    }

    // drumSet: the wheels moved to a time, the minute to the step at or before it.
    private void Place(TimeSpan t, bool smooth)
    {
        if (_hour is null || _minute is null || _mins.Length == 0) return;
        var mi = 0;
        for (var k = 0; k < _mins.Length; k++) if (_mins[k] <= t.Minutes) mi = k;
        _value = new TimeSpan(t.Hours, _mins[mi], 0);
        _syncing = true;
        if (Is12Hour)
        {
            _hour.ScrollTo(t.Hours % 12, smooth);
            _minute.ScrollTo(mi, smooth);
            _period?.ScrollTo(t.Hours >= 12 ? 1 : 0, smooth);
        }
        else
        {
            _hour.ScrollTo(t.Hours, smooth);
            _minute.ScrollTo(mi, smooth);
        }
        _syncing = false;
        Marks();
    }

    private void Marks()
    {
        _hour?.InvalidateVisual();
        _minute?.InvalidateVisual();
        _period?.InvalidateVisual();
    }

    // drumSettle: a time outside min–max snaps back to the limit; a new time is picked.
    private void OnColumnSettled(object? sender, int value)
    {
        if (_syncing) return;
        var v = Read();
        if (MinTime is { } lo && v < lo) { Place(lo, true); v = new TimeSpan(lo.Hours, lo.Minutes, 0); }
        else if (MaxTime is { } hi && v > hi) { Place(hi, true); v = new TimeSpan(hi.Hours, hi.Minutes, 0); }
        Marks();
        if (v == _value) return;
        _value = v;
        _syncing = true;
        SetCurrentValue(TimeProperty, v);
        _syncing = false;
        TimePicked?.Invoke(this, v);
    }

    /// <summary>Moves keyboard focus to the hour wheel.</summary>
    public void FocusFirst() => _hour?.Focus(global::Avalonia.Input.NavigationMethod.Tab);
}
