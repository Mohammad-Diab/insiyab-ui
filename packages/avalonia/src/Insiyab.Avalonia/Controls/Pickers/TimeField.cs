using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The time field (the web's <c>input[data-ins-time]</c>): the time written as the page's language writes it, a clock at its end, typing such as "14:30" or "2:30 م", and hour, minute and period wheels in a pop-up under it.</summary>
public class TimeField : TextBox
{
    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<TimeField, TimeSpan?>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<TimeSpan?> MinTimeProperty =
        TimeDrum.MinTimeProperty.AddOwner<TimeField>();

    public static readonly StyledProperty<TimeSpan?> MaxTimeProperty =
        TimeDrum.MaxTimeProperty.AddOwner<TimeField>();

    public static readonly StyledProperty<int> MinuteStepProperty =
        TimeDrum.MinuteStepProperty.AddOwner<TimeField>();

    public static readonly StyledProperty<string?> LocaleProperty =
        TimeDrum.LocaleProperty.AddOwner<TimeField>();

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<TimeField, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<TimeField, string> ValueProperty =
        AvaloniaProperty.RegisterDirect<TimeField, string>(nameof(Value), o => o.Value);

    private readonly PickerFlyout _flyout;
    private readonly Border _pop;
    private readonly TimeDrum _drum;
    private readonly Button _button;
    private string _value = string.Empty;
    private bool _syncing;

    public TimeField()
    {
        _drum = new TimeDrum();
        _drum.TimePicked += (_, t) => SetTime(t);
        _pop = new Border { Child = new Panel { Children = { new Border { Classes = { "picker-glass" } }, new Decorator { Child = _drum, Margin = new Thickness(8) } } } };
        _pop.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape or Key.Enter) { e.Handled = true; Close(true); }
        };
        _flyout = new PickerFlyout { Content = _pop };
        _flyout.Closed += (_, _) =>
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            if (_pop.IsKeyboardFocusWithin || FocusedElement() is null) Focus(NavigationMethod.Tab);
        };
        _button = new Button { Classes = { "time-btn" }, Focusable = false };
        _button.Click += (_, _) =>
        {
            if (_flyout.IsOpen) Close(true);
            else Open(true);
        };
        InnerRightContent = _button;
        Classes.Add("time-field");
    }

    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>Raised when the time changes (the web's <c>ins:time</c>); not when it is set from code.</summary>
    public event EventHandler<TimeSpan?>? TimeChanged;

    public TimeSpan? SelectedTime { get => GetValue(SelectedTimeProperty); set => SetValue(SelectedTimeProperty, value); }

    /// <summary>The first time that can be chosen (the web's <c>min</c>): earlier times are dimmed on the wheels and refused when typed.</summary>
    public TimeSpan? MinTime { get => GetValue(MinTimeProperty); set => SetValue(MinTimeProperty, value); }

    public TimeSpan? MaxTime { get => GetValue(MaxTimeProperty); set => SetValue(MaxTimeProperty, value); }

    /// <summary>The minute wheel's step (the web's <c>step</c>, in minutes): half an hour when unset.</summary>
    public int MinuteStep { get => GetValue(MinuteStepProperty); set => SetValue(MinuteStepProperty, value); }

    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>What the form would send: <c>HH:MM</c>, or empty.</summary>
    public string Value { get => _value; private set => SetAndRaise(ValueProperty, ref _value, value); }

    internal PickerFlyout Flyout => _flyout;

    internal Button ClockButton => _button;

    internal TimeDrum Drum => _drum;

    private PickerLocale Words => PickerLocale.For(Locale, FlowDirection);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AutomationProperties.SetName(_button, Words.ChooseTime);
        Write();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_syncing) return;
        var p = change.Property;
        if (p == SelectedTimeProperty || p == LocaleProperty || p == FlowDirectionProperty)
        {
            Write();
            if (p != SelectedTimeProperty) AutomationProperties.SetName(_button, Words.ChooseTime);
        }
        else if (p == IsDropDownOpenProperty)
        {
            if (IsDropDownOpen && !_flyout.IsOpen) Open(false);
            else if (!IsDropDownOpen && _flyout.IsOpen) Close(false);
        }
    }

    private void Write()
    {
        _syncing = true;
        Text = SelectedTime is { } t ? Words.FormatTime(t) : string.Empty;
        Value = SelectedTime is { } v ? PickerParse.HhMm(v) : string.Empty;
        _syncing = false;
    }

    /// <summary>Sets the time as the person would (the web's <c>setTime</c>); a time outside min–max is refused with a message.</summary>
    public bool SetTime(TimeSpan? t, bool quiet = false)
    {
        if (t is { } x && ((MinTime is { } lo && x < lo) || (MaxTime is { } hi && x > hi)))
        {
            DataValidationErrors.SetError(this, new DataValidationException(Words.BadTime));
            return false;
        }
        DataValidationErrors.ClearErrors(this);
        var changed = SelectedTime != t;
        _syncing = true;
        SetCurrentValue(SelectedTimeProperty, t);
        _syncing = false;
        Write();
        if (_flyout.IsOpen && t is { } now && _drum.Time != now) _drum.Time = now;
        if (!quiet && changed) TimeChanged?.Invoke(this, t);
        return true;
    }

    // commitTime: what was typed, read as the person leaves the field.
    internal void CommitTyped()
    {
        var text = (Text ?? string.Empty).Trim();
        if (SelectedTime is { } cur && text == Words.FormatTime(cur)) return;
        if (text.Length == 0) { SetTime(null); return; }
        var t = PickerParse.ParseTime(text);
        if (t is not null) SetTime(t);
        else DataValidationErrors.SetError(this, new DataValidationException(Words.BadTime));
    }

    /// <summary>Opens the wheels; with <paramref name="focusDrum"/>, focus goes to the hour.</summary>
    public void Open(bool focusDrum)
    {
        if (_flyout.IsOpen)
        {
            if (focusDrum) _drum.FocusFirst();
            return;
        }
        _drum.Locale = Locale ?? (Words.IsArabic ? "ar" : "en");
        _drum.MinuteStep = MinuteStep;
        _drum.MinTime = MinTime;
        _drum.MaxTime = MaxTime;
        _drum.Time = SelectedTime;
        _pop.FlowDirection = FlowDirection;
        _flyout.IsEnd = false;
        _flyout.ShowAt(this);
        SetCurrentValue(IsDropDownOpenProperty, true);
        if (focusDrum) Dispatcher.UIThread.Post(_drum.FocusFirst, DispatcherPriority.Input);
    }

    public void Close(bool refocus)
    {
        if (_flyout.IsOpen) _flyout.Hide();
        SetCurrentValue(IsDropDownOpenProperty, false);
        if (refocus) Focus(NavigationMethod.Tab);
    }

    private object? FocusedElement() => TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();

    // A mouse click in the text only places the caret, the clock button opens the wheels; a touch tap opens them as the web does.
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer.Type != PointerType.Touch || _button.IsPointerOver) return;
        if (_flyout.IsOpen) Close(false);
        else Open(false);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if ((e.Key == Key.Down && alt) || e.Key == Key.F4)
        {
            e.Handled = true;
            if (_flyout.IsOpen) Close(true);
            else Open(true);
            return;
        }
        if (e.Key == Key.Escape && _flyout.IsOpen)
        {
            e.Handled = true;
            Close(true);
            return;
        }
        if ((e.Key == Key.Up || e.Key == Key.Down) && !alt && e.KeyModifiers == KeyModifiers.None && !_flyout.IsOpen)
        {
            e.Handled = true;
            CommitTyped();
            var step = Math.Max(1, MinuteStep);
            var t = SelectedTime is { } cur ? Wrap(cur, e.Key == Key.Up ? step : -step) : TimeDrum.DefaultFor(step);
            if (MinTime is { } lo && t < lo) t = lo;
            if (MaxTime is { } hi && t > hi) t = hi;
            SetTime(t);
            CaretIndex = Text?.Length ?? 0;
            return;
        }
        if (e.Key == Key.Enter)
        {
            CommitTyped();
            Close(false);
        }
        base.OnKeyDown(e);
    }

    // The time a number of minutes on, round midnight.
    private static TimeSpan Wrap(TimeSpan t, int minutes)
    {
        var m = (((int)t.TotalMinutes + minutes) % 1440 + 1440) % 1440;
        return new TimeSpan(m / 60, m % 60, 0);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        CommitTyped();
        Dispatcher.UIThread.Post(() => { if (_flyout.IsOpen && !IsKeyboardFocusWithin && !_pop.IsKeyboardFocusWithin) Close(false); }, DispatcherPriority.Input);
    }
}
