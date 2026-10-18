using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>Sent when a date field's value changes (the web's <c>ins:date</c>).</summary>
public sealed class DateChangedEventArgs : EventArgs
{
    internal DateChangedEventArgs(DateTime? date, DateTime? end, TimeSpan? time, string value)
    {
        Date = date;
        End = end;
        Time = time;
        Value = value;
    }

    public DateTime? Date { get; }

    /// <summary>The end of a range.</summary>
    public DateTime? End { get; }

    public TimeSpan? Time { get; }

    /// <summary>What the web field sends: <c>2026-09-30</c>, or <c>2026-09-26T14:30</c> for a date and time.</summary>
    public string Value { get; }
}

/// <summary>The date field (the web's <c>input[data-ins-date]</c> in an <c>.ins-input-group</c>): a text field showing the date in the page's language, a calendar button at its end, typing that is read on Enter or when focus leaves, and the calendar in a pop-up under it. Picks one day, a range or a day and a time, in Gregorian or Hijri, and always holds Gregorian dates.</summary>
public class DateField : TextBox
{
    public static readonly StyledProperty<DatePickMode> ModeProperty =
        DateCalendar.ModeProperty.AddOwner<DateField>();

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<DateField, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> EndDateProperty =
        AvaloniaProperty.Register<DateField, DateTime?>(nameof(EndDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<DateField, TimeSpan?>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        DateCalendar.MinDateProperty.AddOwner<DateField>();

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        DateCalendar.MaxDateProperty.AddOwner<DateField>();

    public static readonly StyledProperty<CalendarKind> CalendarKindProperty =
        DateCalendar.CalendarKindProperty.AddOwner<DateField>();

    public static readonly StyledProperty<bool> ShowGregorianProperty =
        DateCalendar.ShowGregorianProperty.AddOwner<DateField>();

    public static readonly StyledProperty<bool> IsRequiredProperty =
        DateCalendar.IsRequiredProperty.AddOwner<DateField>();

    public static readonly StyledProperty<string?> LocaleProperty =
        DateCalendar.LocaleProperty.AddOwner<DateField>();

    public static readonly StyledProperty<int> MinuteStepProperty =
        DateCalendar.MinuteStepProperty.AddOwner<DateField>();

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<DateField, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<DateField, string> ValueProperty =
        AvaloniaProperty.RegisterDirect<DateField, string>(nameof(Value), o => o.Value);

    public static readonly DirectProperty<DateField, string> EndValueProperty =
        AvaloniaProperty.RegisterDirect<DateField, string>(nameof(EndValue), o => o.EndValue);

    private readonly Button _button;
    private readonly PickerFlyout _flyout;
    private readonly DateCalendar _calendar;
    private string _value = string.Empty;
    private string _endValue = string.Empty;
    private bool _syncing;
    private TextPresenter? _presenter;

    public DateField()
    {
        _button = new Button { Classes = { "date-btn" }, Focusable = false };
        _button.Click += OnButtonClick;
        _calendar = new DateCalendar();
        _calendar.DayInvoked += (_, d) => Choose(d);
        _calendar.DayShiftInvoked += (_, d) => ShiftChoose(d);
        _calendar.TodayInvoked += (_, _) => OnToday();
        _calendar.ClearInvoked += (_, _) => { SetDate(null); Close(true); };
        _calendar.DoneInvoked += (_, _) => Close(true);
        _calendar.CloseRequested += (_, _) => Close(true);
        _calendar.TimeInvoked += (_, t) => { SetCurrentValue(SelectedTimeProperty, t); if (SelectedDate is { } d) SetDate(d); };
        _calendar.SystemInvoked += (_, g) => { SetCurrentValue(ShowGregorianProperty, g); WriteText(); Check(SelectedDate); CalendarSwitched?.Invoke(this, g); };
        _flyout = new PickerFlyout { Content = _calendar };
        _flyout.Closed += (_, _) =>
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            _button.Classes.Set("open", false);
            if (_calendar.IsKeyboardFocusWithin || FocusedElement() is null) Focus(NavigationMethod.Tab);
        };
        InnerRightContent = _button;
        MaxLength = 80;
        Classes.Add("date-field");
    }

    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>Raised when the date changes, as the web's <c>ins:date</c>; not when it is set from code.</summary>
    public event EventHandler<DateChangedEventArgs>? DateChanged;

    /// <summary>Raised when the footer switch flips a Hijri field to Gregorian or back (<c>ins:calendar</c>); true when Gregorian is on screen.</summary>
    public event EventHandler<bool>? CalendarSwitched;

    public DatePickMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    public DateTime? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }

    /// <summary>The end of a range (the web's <c>data-ins-date-to</c>).</summary>
    public DateTime? EndDate { get => GetValue(EndDateProperty); set => SetValue(EndDateProperty, value); }

    /// <summary>The time of a date-and-time field.</summary>
    public TimeSpan? SelectedTime { get => GetValue(SelectedTimeProperty); set => SetValue(SelectedTimeProperty, value); }

    /// <summary>The first day that can be chosen (the web's <c>min</c>): earlier days are dimmed in the calendar and refused when typed.</summary>
    public DateTime? MinDate { get => GetValue(MinDateProperty); set => SetValue(MinDateProperty, value); }

    public DateTime? MaxDate { get => GetValue(MaxDateProperty); set => SetValue(MaxDateProperty, value); }

    /// <summary>The calendar shown (the web's <c>data-ins-calendar</c>); the field still holds Gregorian dates.</summary>
    public CalendarKind CalendarKind { get => GetValue(CalendarKindProperty); set => SetValue(CalendarKindProperty, value); }

    public bool ShowGregorian { get => GetValue(ShowGregorianProperty); set => SetValue(ShowGregorianProperty, value); }

    /// <summary>The web's <c>required</c>: the calendar has no Clear button.</summary>
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <summary>"ar" or "en"; unset, Arabic in a right-to-left layout and English otherwise.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    /// <summary>The minute step of a date-and-time field's wheels (the web's <c>step</c>, in minutes); 5 by default.</summary>
    public int MinuteStep { get => GetValue(MinuteStepProperty); set => SetValue(MinuteStepProperty, value); }

    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>What the form would send under the field's name: ISO, Gregorian, empty when there is no date.</summary>
    public string Value { get => _value; private set => SetAndRaise(ValueProperty, ref _value, value); }

    /// <summary>A range's end as ISO (the web sends it under <c>data-ins-date-range</c>'s name).</summary>
    public string EndValue { get => _endValue; private set => SetAndRaise(EndValueProperty, ref _endValue, value); }

    internal DateCalendar Calendar => _calendar;

    internal PickerFlyout Flyout => _flyout;

    internal Button CalendarButton => _button;

    private PickerLocale Words => PickerLocale.For(Locale, FlowDirection);

    private PickerCalendar ViewCal => ShowGregorian ? PickerCalendar.Gregorian : PickerCalendar.Of(CalendarKind);

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AutomationProperties.SetName(_button, Words.Choose);
        WriteText();
        UpdateValue();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _presenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");
        TextDirection.FollowTyped(this, _presenter);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == TextProperty || p == FlowDirectionProperty || p == TextAlignmentProperty) TextDirection.FollowTyped(this, _presenter);
        if (_syncing) return;
        if (p == SelectedDateProperty || p == EndDateProperty || p == SelectedTimeProperty || p == ModeProperty || p == CalendarKindProperty ||
            p == ShowGregorianProperty || p == LocaleProperty || p == FlowDirectionProperty)
        {
            WriteText();
            UpdateValue();
            if (p == LocaleProperty || p == FlowDirectionProperty) AutomationProperties.SetName(_button, Words.Choose);
            if (p != SelectedTimeProperty) DataValidationErrors.ClearErrors(this);
        }
        else if (p == IsDropDownOpenProperty)
        {
            if (IsDropDownOpen && !_flyout.IsOpen) Open(false);
            else if (!IsDropDownOpen && _flyout.IsOpen) Close(false);
        }
    }

    // dateText: a day, a span of days, or a day and a time.
    private string Format()
    {
        if (SelectedDate is not { } d) return string.Empty;
        var words = Words;
        var cal = ViewCal;
        if (Mode == DatePickMode.Range)
        {
            if (EndDate is not { } to) return words.FormatDay(cal, d.Date) + " – ";
            return words.FormatRange(cal, d.Date, to.Date);
        }
        if (Mode == DatePickMode.DateTime && SelectedTime is { } t) return words.FormatDateTime(cal, d.Date, t);
        return words.FormatDay(cal, d.Date);
    }

    private void WriteText()
    {
        _syncing = true;
        Text = Format();
        _syncing = false;
    }

    private void UpdateValue()
    {
        var v = SelectedDate is { } d ? PickerParse.IsoOf(d) : string.Empty;
        if (Mode == DatePickMode.DateTime && SelectedDate is not null) v += "T" + PickerParse.HhMm(SelectedTime ?? TimeSpan.Zero);
        Value = v;
        EndValue = Mode == DatePickMode.Range && EndDate is { } e ? PickerParse.IsoOf(e) : string.Empty;
    }

    private TimeSpan DefaultTime() => TimeDrum.DefaultFor(MinuteStep);

    // checkDate: the message a bad or out-of-bounds date leaves under the field.
    private bool Check(DateTime? d, bool bad = false)
    {
        var words = Words;
        var msg = string.Empty;
        var to = Mode == DatePickMode.Range ? EndDate : null;
        if (bad) msg = words.BadDate;
        else if (d is { } x && MinDate is { } lo && x.Date < lo.Date) msg = words.Early(words.FormatDay(ViewCal, lo.Date));
        else if (d is { } y && MaxDate is { } hi && y.Date > hi.Date) msg = words.Late(words.FormatDay(ViewCal, hi.Date));
        else if (to is { } t && MaxDate is { } hi2 && t.Date > hi2.Date) msg = words.Late(words.FormatDay(ViewCal, hi2.Date));
        if (msg.Length > 0) DataValidationErrors.SetError(this, new DataValidationException(msg));
        else DataValidationErrors.ClearErrors(this);
        return msg.Length == 0;
    }

    /// <summary>Sets the date as the person would (the web's <c>setDate</c>): the text, the value, the check, and <see cref="DateChanged"/> when it changed. For a range, <paramref name="to"/> is the end; left out, the end stays unless it now falls before the start.</summary>
    public void SetDate(DateTime? d, bool quiet = false, DateTime? to = null, bool keepEnd = true)
    {
        var oldValue = Value;
        var oldEnd = EndValue;
        _syncing = true;
        SetCurrentValue(SelectedDateProperty, d?.Date);
        if (Mode == DatePickMode.DateTime && d is not null && SelectedTime is null) SetCurrentValue(SelectedTimeProperty, DefaultTime());
        if (Mode == DatePickMode.Range)
        {
            var end = keepEnd ? EndDate : to;
            if (d is null || (end is { } e && e.Date < d.Value.Date)) end = null;
            SetCurrentValue(EndDateProperty, end?.Date);
        }
        _syncing = false;
        WriteText();
        UpdateValue();
        Check(d);
        _calendar.SelectedDate = SelectedDate;
        _calendar.EndDate = EndDate;
        if (!quiet && (oldValue != Value || oldEnd != EndValue))
            DateChanged?.Invoke(this, new DateChangedEventArgs(SelectedDate, Mode == DatePickMode.Range ? EndDate : null, Mode == DatePickMode.DateTime ? SelectedTime : null, Value));
    }

    // commitTyped: what was typed, read when the person leaves the field or presses Enter; text that cannot be read is a bad date, never a crash.
    internal void CommitTyped()
    {
        try { ReadTyped(); }
        catch (Exception x) when (InputGuard.Caught(this, x)) { Check(null, true); }
    }

    private void ReadTyped()
    {
        var text = (Text ?? string.Empty).Trim();
        if (SelectedDate is not null && text == Format().Trim()) return;
        if (text.Length == 0) { SetDate(null); return; }
        var words = Words;
        var cal = ViewCal;
        if (Mode == DatePickMode.Range)
        {
            var parts = System.Text.RegularExpressions.Regex.Split(text, @"\s+[-–—]\s+|\s*[–—]\s*");
            var a = PickerParse.ParseDate(parts[0], cal, words);
            DateTime? b = parts.Length > 1 && parts[1].Trim().Length > 0 ? PickerParse.ParseDate(parts[1], cal, words) : null;
            var bBad = parts.Length > 1 && parts[1].Trim().Length > 0 && b is null;
            if (a is not null && !bBad && (b is null || b >= a))
            {
                if (Check(a) && (b is null || Check(b))) SetDate(a, false, b, false);
            }
            else Check(null, true);
            return;
        }
        if (Mode == DatePickMode.DateTime)
        {
            var (day, at) = PickerParse.ParseDateTime(text, cal, words);
            if (day is not null)
            {
                if (!Check(day)) return;
                if (at is not null) SetCurrentValue(SelectedTimeProperty, at);
                SetDate(day);
            }
            else Check(null, true);
            return;
        }
        var parsed = PickerParse.ParseDate(text, cal, words);
        if (parsed is null) Check(null, true);
        else if (Check(parsed)) SetDate(parsed);
    }

    private void SyncCalendar()
    {
        _calendar.Mode = Mode;
        _calendar.SelectedDate = SelectedDate;
        _calendar.EndDate = EndDate;
        _calendar.MinDate = MinDate;
        _calendar.MaxDate = MaxDate;
        _calendar.CalendarKind = CalendarKind;
        _calendar.ShowGregorian = ShowGregorian;
        _calendar.IsRequired = IsRequired;
        _calendar.Locale = Locale ?? Words.IsArabic switch { true => "ar", false => "en" };
        _calendar.MinuteStep = MinuteStep;
        _calendar.Time = SelectedTime ?? DefaultTime();
        _calendar.FlowDirection = FlowDirection;
    }

    /// <summary>Opens the calendar (the web's <c>openCal</c>); with <paramref name="focusGrid"/>, focus goes to the chosen day.</summary>
    public void Open(bool focusGrid)
    {
        SyncCalendar();
        _calendar.IsPickingEnd = Mode == DatePickMode.Range && SelectedDate is not null && EndDate is null;
        _calendar.Show(SelectedDate);
        _flyout.IsEnd = false;
        if (!_flyout.IsOpen) _flyout.ShowAt(this);
        SetCurrentValue(IsDropDownOpenProperty, true);
        _button.Classes.Set("open", true);
        if (focusGrid) Dispatcher.UIThread.Post(_calendar.FocusDay, DispatcherPriority.Input);
    }

    /// <summary>Closes the calendar; with <paramref name="refocus"/>, focus returns to the field.</summary>
    public void Close(bool refocus)
    {
        if (_flyout.IsOpen) _flyout.Hide();
        SetCurrentValue(IsDropDownOpenProperty, false);
        _button.Classes.Set("open", false);
        if (refocus) Focus(NavigationMethod.Tab);
    }

    // calChoose: a day picked in the calendar.
    private void Choose(DateTime d)
    {
        if (Mode == DatePickMode.Range)
        {
            var from = SelectedDate;
            if (_calendar.IsPickingEnd && from is not null && d >= from.Value.Date)
            {
                SetDate(from, false, d, false);
                Close(true);
                return;
            }
            SetDate(d, false, null, false);
            _calendar.IsPickingEnd = true;
            _calendar.MoveTo(d);
            return;
        }
        if (Mode == DatePickMode.DateTime)
        {
            SetDate(d);
            _calendar.MoveTo(d);
            return;
        }
        SetDate(d);
        Close(true);
    }

    // A Shift-click or Shift-Enter on a day moves the nearer end of a range without starting it over.
    private void ShiftChoose(DateTime d)
    {
        if (SelectedDate is not { } a) { Choose(d); return; }
        var b = EndDate ?? a;
        if (d < a) SetDate(d, false, b, false);
        else if (d > b || (d - a) > (b - d)) SetDate(a, false, d, false);
        else SetDate(d, false, b, false);
        Close(true);
    }

    private object? FocusedElement() => TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();

    private void OnToday()
    {
        var now = DateCalendar.Today();
        if ((MinDate is null || now >= MinDate.Value.Date) && (MaxDate is null || now <= MaxDate.Value.Date)) Choose(now);
        else _calendar.MoveTo(now);
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_flyout.IsOpen) Close(true);
        else Open(true);
    }

    // A mouse click in the text only places the caret, the button opens the calendar; a touch tap opens it as the web does.
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
        if ((e.Key == Key.Up || e.Key == Key.Down) && !alt && e.KeyModifiers == KeyModifiers.None && Mode != DatePickMode.Range && !_flyout.IsOpen)
        {
            e.Handled = true;
            CommitTyped();
            var start = SelectedDate ?? DateCalendar.Today();
            var next = SelectedDate is null ? start : start.AddDays(e.Key == Key.Up ? 1 : -1);
            if (MinDate is { } lo && next < lo.Date) next = lo.Date;
            if (MaxDate is { } hi && next > hi.Date) next = hi.Date;
            SetDate(next);
            CaretIndex = Text?.Length ?? 0;
            return;
        }
        if (e.Key == Key.Enter)
        {
            CommitTyped();
            if (_flyout.IsOpen) Close(false);
        }
        base.OnKeyDown(e);
    }

    // A date never holds a number longer than a year, so a fifth digit in a row is refused.
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (PickerParse.Overlong(this, e.Text)) { e.Handled = true; return; }
        base.OnTextInput(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        // Another window taking the keyboard is not leaving the field: the typed text waits for the window to come back.
        if (!WindowActivity.GetIsInactive(this)) CommitTyped();
        Dispatcher.UIThread.Post(() => { if (_flyout.IsOpen && !IsKeyboardFocusWithin && !_calendar.IsKeyboardFocusWithin) Close(false); }, DispatcherPriority.Input);
    }
}
