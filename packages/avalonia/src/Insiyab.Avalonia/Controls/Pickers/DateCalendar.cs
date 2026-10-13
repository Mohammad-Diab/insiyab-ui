using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a date field picks: one day, a span between two days in one field, or a day and a time.</summary>
public enum DatePickMode
{
    Single,
    Range,
    DateTime,
}

/// <summary>The date field's calendar (the web's <c>.ins-cal</c>): a month grid whose week starts where the locale starts it, month and year lists, arrow-key navigation in the page's direction, a range band, two months side by side for a range, time wheels beside a date-and-time, and a footer with Today, Clear, Done and the Hijri/Gregorian switch.</summary>
[TemplatePart("PART_Host", typeof(Decorator))]
[TemplatePart("PART_Turn", typeof(Canvas))]
[PseudoClasses(":dual", ":datetime", ":stacked")]
public class DateCalendar : TemplatedControl
{
    public static readonly StyledProperty<DatePickMode> ModeProperty =
        AvaloniaProperty.Register<DateCalendar, DatePickMode>(nameof(Mode));

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<DateCalendar, DateTime?>(nameof(SelectedDate));

    public static readonly StyledProperty<DateTime?> EndDateProperty =
        AvaloniaProperty.Register<DateCalendar, DateTime?>(nameof(EndDate));

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<DateCalendar, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<DateCalendar, DateTime?>(nameof(MaxDate));

    public static readonly StyledProperty<CalendarKind> CalendarKindProperty =
        AvaloniaProperty.Register<DateCalendar, CalendarKind>(nameof(CalendarKind));

    public static readonly StyledProperty<bool> ShowGregorianProperty =
        AvaloniaProperty.Register<DateCalendar, bool>(nameof(ShowGregorian));

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<DateCalendar, bool>(nameof(IsRequired));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<DateCalendar, string?>(nameof(Locale));

    public static readonly StyledProperty<int> MinuteStepProperty =
        AvaloniaProperty.Register<DateCalendar, int>(nameof(MinuteStep), 5);

    public static readonly StyledProperty<TimeSpan?> TimeProperty =
        AvaloniaProperty.Register<DateCalendar, TimeSpan?>(nameof(Time));

    public static readonly StyledProperty<bool?> IsDualProperty =
        AvaloniaProperty.Register<DateCalendar, bool?>(nameof(IsDual));

    public static readonly StyledProperty<bool> IsPickingEndProperty =
        AvaloniaProperty.Register<DateCalendar, bool>(nameof(IsPickingEnd));

    private Decorator? _host;
    private Canvas? _turnLayer;
    private CalendarTurn? _turn;
    private DateTime _view = DateTime.Today;
    private DateTime _focus = DateTime.Today;
    private bool _dual;
    private bool _stacked;
    private bool _shift;
    private readonly List<Grid> _grids = [];
    private readonly Dictionary<DateTime, Button> _days = [];
    private readonly Dictionary<DateTime, Border> _cells = [];
    private TimeDrum? _drum;

    public DateCalendar() => AddHandler(KeyDownEvent, OnShiftEnter, RoutingStrategies.Tunnel);

    /// <summary>A day was clicked (or Enter or Space pressed on it).</summary>
    public event EventHandler<DateTime>? DayInvoked;

    /// <summary>A day was Shift-clicked (or Shift-Enter pressed on it) in a range: the nearer end moves.</summary>
    public event EventHandler<DateTime>? DayShiftInvoked;

    public event EventHandler? TodayInvoked;

    public event EventHandler? ClearInvoked;

    public event EventHandler? DoneInvoked;

    /// <summary>Escape, or Enter on the time wheels.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>The time wheels picked a time.</summary>
    public event EventHandler<TimeSpan>? TimeInvoked;

    /// <summary>The footer switch flipped between the field's own calendar and Gregorian; the argument is <see cref="ShowGregorian"/>.</summary>
    public event EventHandler<bool>? SystemInvoked;

    public DatePickMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    public DateTime? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }

    /// <summary>The end of a range.</summary>
    public DateTime? EndDate { get => GetValue(EndDateProperty); set => SetValue(EndDateProperty, value); }

    public DateTime? MinDate { get => GetValue(MinDateProperty); set => SetValue(MinDateProperty, value); }

    public DateTime? MaxDate { get => GetValue(MaxDateProperty); set => SetValue(MaxDateProperty, value); }

    public CalendarKind CalendarKind { get => GetValue(CalendarKindProperty); set => SetValue(CalendarKindProperty, value); }

    /// <summary>A Hijri calendar flipped to Gregorian by the footer switch.</summary>
    public bool ShowGregorian { get => GetValue(ShowGregorianProperty); set => SetValue(ShowGregorianProperty, value); }

    /// <summary>A required field has no Clear button.</summary>
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public int MinuteStep { get => GetValue(MinuteStepProperty); set => SetValue(MinuteStepProperty, value); }

    public TimeSpan? Time { get => GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    /// <summary>Two months side by side for a range; unset, they are when the window is at least 720 wide, as the web's 45rem query.</summary>
    public bool? IsDual { get => GetValue(IsDualProperty); set => SetValue(IsDualProperty, value); }

    /// <summary>A range waiting for its end: the footer says so and the band follows the pointer.</summary>
    public bool IsPickingEnd { get => GetValue(IsPickingEndProperty); set => SetValue(IsPickingEndProperty, value); }

    /// <summary>The first of the month on screen.</summary>
    public DateTime DisplayMonth => _view;

    /// <summary>The day that holds the grid's one tab stop.</summary>
    public DateTime FocusDate => _focus;

    internal PickerCalendar ViewCalendar => ShowGregorian ? PickerCalendar.Gregorian : PickerCalendar.Of(CalendarKind);

    internal PickerLocale Words => PickerLocale.For(Locale, FlowDirection);

    internal TimeDrum? Drum => _drum;

    internal IReadOnlyDictionary<DateTime, Button> DayButtons => _days;

    internal IReadOnlyDictionary<DateTime, Border> DayCells => _cells;

    internal Canvas? TurnLayer => _turnLayer;

    internal CalendarTurn? RunningTurn => _turn;

    internal IEnumerable<Control> HostControls() =>
        _host is null ? [] : global::Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(_host).OfType<Control>();

    internal static Func<DateTime> Today { get; set; } = () => DateTime.Today;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host = e.NameScope.Find<Decorator>("PART_Host");
        _turnLayer = e.NameScope.Find<Canvas>("PART_Turn");
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_host is null) return;
        if (change.Property == TimeProperty)
        {
            if (_drum is not null && Time is { } t && _drum.Time != t) _drum.Time = t;
            return;
        }
        if (change.Property == ModeProperty || change.Property == SelectedDateProperty || change.Property == EndDateProperty ||
            change.Property == MinDateProperty || change.Property == MaxDateProperty || change.Property == CalendarKindProperty ||
            change.Property == ShowGregorianProperty || change.Property == IsRequiredProperty || change.Property == LocaleProperty ||
            change.Property == IsDualProperty || change.Property == IsPickingEndProperty || change.Property == MinuteStepProperty ||
            change.Property == FlowDirectionProperty)
            Rebuild();
    }

    private (DateTime? Min, DateTime? Max) Bounds2 => (MinDate?.Date, MaxDate?.Date);

    /// <summary>Starts on a day (the chosen one, or today, kept inside min and max) and shows its month.</summary>
    public void Show(DateTime? start = null)
    {
        var d = (start ?? SelectedDate ?? Today()).Date;
        var (min, max) = Bounds2;
        if (min is { } lo && d < lo) d = lo;
        if (max is { } hi && d > hi) d = hi;
        _focus = d;
        _view = ViewCalendar.MonthStart(d);
        Rebuild();
    }

    /// <summary>Moves the tab stop (and focus) to a day, turning the month when it is not on screen.</summary>
    public void MoveTo(DateTime d, bool focus = true)
    {
        d = d.Date;
        _focus = d;
        var sys = ViewCalendar;
        if (!sys.SameMonth(d, _view) && !(_dual && sys.SameMonth(d, sys.AddMonths(_view, 1))))
        {
            _view = sys.MonthStart(d);
            Rebuild();
        }
        else
        {
            foreach (var (day, btn) in _days) KeyboardNavigation.SetIsTabStop(btn, day == d);
        }
        if (focus) FocusDay();
        Preview(d);
    }

    /// <summary>Focuses the day holding the tab stop.</summary>
    public void FocusDay()
    {
        if (_days.TryGetValue(_focus, out var btn)) btn.Focus(NavigationMethod.Directional);
    }

    /// <summary>Focuses the time wheels of a date-and-time calendar.</summary>
    public void FocusTime() => _drum?.FocusFirst();

    private bool ResolveDual()
    {
        if (Mode != DatePickMode.Range) return false;
        if (IsDual is { } set) return set;
        var top = TopLevel.GetTopLevel(this);
        return top is null || top.Bounds.Width >= 720;
    }

    // A date and a time stack when the window is narrower than 560.
    private bool ResolveStacked() => Mode == DatePickMode.DateTime && TopLevel.GetTopLevel(this) is { } top && top.Bounds.Width > 0 && top.Bounds.Width < 560;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if ((IsDual is null && Mode == DatePickMode.Range && ResolveDual() != _dual) || ResolveStacked() != _stacked) Rebuild();
    }

    /// <summary>Draws the calendar again from its properties.</summary>
    public void Rebuild()
    {
        if (_host is null) return;
        _turn?.Stop();
        _turn = null;
        var words = Words;
        var sys = ViewCalendar;
        var vp = sys.Parts(_view);
        var (min, max) = Bounds2;
        var mode = Mode;
        _dual = ResolveDual();
        PseudoClasses.Set(":dual", _dual);
        PseudoClasses.Set(":datetime", mode == DatePickMode.DateTime);
        _stacked = ResolveStacked();
        PseudoClasses.Set(":stacked", _stacked);
        _grids.Clear();
        _days.Clear();
        _cells.Clear();
        _drum = null;

        var main = new StackPanel();
        var head = BuildHead(words, sys, vp);
        main.Children.Add(head);

        var first = sys.FromParts(vp.Y, vp.M, 1);
        var months = _dual ? new[] { first, sys.AddMonths(first, 1) } : [first];
        Control body;
        if (_dual)
        {
            var pair = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 24 };
            for (var i = 0; i < months.Length; i++)
            {
                var g = BuildGrid(words, sys, months[i], min, max);
                Grid.SetColumn(g, i);
                pair.Children.Add(g);
            }
            body = pair;
        }
        else body = BuildGrid(words, sys, months[0], min, max);
        main.Children.Add(body);
        PaintBand(null);

        Border? side = null;
        if (mode == DatePickMode.DateTime)
        {
            _drum = new TimeDrum { Locale = Locale, MinuteStep = MinuteStep, Time = Time };
            _drum.TimePicked += (_, t) => TimeInvoked?.Invoke(this, t);
            side = new Border { Child = _drum, Classes = { "cal-time" } };
        }

        var foot = BuildFoot(words, sys);
        if (side is null || _stacked)
        {
            if (side is not null) main.Children.Add(side);
            main.Children.Add(foot);
            _host.Child = main;
            return;
        }

        // The drum beside the head and the month, centred on both; a row of its own under them would be a spanning cell, which the grid sizes badly.
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = 12 };
        layout.Children.Add(main);
        Grid.SetColumn(side, 1);
        layout.Children.Add(side);
        Grid.SetRow(foot, 1);
        Grid.SetColumnSpan(foot, 2);
        layout.Children.Add(foot);
        _host.Child = layout;
    }

    private Grid BuildHead(PickerLocale words, PickerCalendar sys, DayParts vp)
    {
        var head = new Grid
        {
            ColumnDefinitions = _dual ? new ColumnDefinitions("Auto,*,*,Auto") : new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = _dual ? 24 : 6,
            Classes = { "cal-head" },
        };
        var prev = new Button { Classes = { "cal-nav", "prev" }, Tag = "prev" };
        AutomationProperties.SetName(prev, words.PrevMonth);
        prev.Click += (_, _) => Turn(-1, "prev");
        var next = new Button { Classes = { "cal-nav", "next" }, Tag = "next" };
        AutomationProperties.SetName(next, words.NextMonth);
        next.Click += (_, _) => Turn(1, "next");

        var title = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center, Classes = { "cal-title" } };
        var monthNames = Enumerable.Range(0, 12).Select(i => words.MonthName(sys, i)).ToList();
        var monthBox = new ComboBox { ItemsSource = monthNames, SelectedIndex = vp.M, Classes = { "sm", "cal-select" }, Tag = "month" };
        AutomationProperties.SetName(monthBox, words.Month);
        var nowY = sys.Parts(Today()).Y;
        var (min, max) = Bounds2;
        var y0 = min is { } lo ? sys.Parts(lo).Y : Math.Min(vp.Y, nowY) - 100;
        var y1 = max is { } hi ? sys.Parts(hi).Y : Math.Max(vp.Y, nowY) + 20;
        var years = new List<int>();
        for (var yy = y1; yy >= y0; yy--) years.Add(yy);
        var yearBox = new ComboBox { ItemsSource = years.Select(yy => words.YearText(sys, yy)).ToList(), SelectedIndex = years.IndexOf(vp.Y), Classes = { "sm", "cal-select" }, Tag = "year" };
        AutomationProperties.SetName(yearBox, words.Year);
        monthBox.SelectionChanged += (_, _) => { if (monthBox.SelectedIndex >= 0 && monthBox.SelectedIndex != sys.Parts(_view).M) Jump(sys.Parts(_view).Y, monthBox.SelectedIndex, "month"); };
        yearBox.SelectionChanged += (_, _) => { if (yearBox.SelectedIndex >= 0 && years[yearBox.SelectedIndex] != sys.Parts(_view).Y) Jump(years[yearBox.SelectedIndex], sys.Parts(_view).M, "year"); };
        FitSelect(monthBox, monthNames[vp.M]);
        FitSelect(yearBox, words.YearText(sys, vp.Y));
        title.Children.Add(monthBox);
        title.Children.Add(yearBox);

        head.Children.Add(prev);
        Grid.SetColumn(title, 1);
        head.Children.Add(title);
        if (_dual)
        {
            var after = sys.AddMonths(sys.FromParts(vp.Y, vp.M, 1), 1);
            var t2 = new TextBlock { Text = words.MonthYear(sys, after), Classes = { "cal-title-next" } };
            Grid.SetColumn(t2, 2);
            head.Children.Add(t2);
        }
        Grid.SetColumn(next, _dual ? 3 : 2);
        head.Children.Add(next);
        return head;
    }

    // The select is as wide as the option it shows, as the web's is, so a long Hijri month name cannot push the year over the arrows.
    private void FitSelect(ComboBox box, string shown)
    {
        var face = new Typeface(this.TryFindResource(FlowDirection == FlowDirection.RightToLeft ? "InsFontArabic" : "InsFontBodyEn", out var f) && f is FontFamily ff ? ff : FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var size = this.TryFindResource("InsFsSm", out var s) && s is double d ? d : 13;
        var text = new FormattedText(shown, System.Globalization.CultureInfo.InvariantCulture, FlowDirection, face, size, Brushes.Black);
        box.Width = Math.Ceiling(text.WidthIncludingTrailingWhitespace) + 10 + 38 + 2;
    }

    private Grid BuildGrid(PickerLocale words, PickerCalendar sys, DateTime first, DateTime? min, DateTime? max)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto"), Classes = { "cal-grid" } };
        grid.PointerWheelChanged += (_, e) =>
        {
            if (e.Delta.Y == 0) return;
            e.Handled = true;
            Turn(e.Delta.Y < 0 ? 1 : -1, null);
        };
        var fd = (int)words.WeekStart;
        for (var c = 0; c < 7; c++)
        {
            var wd = (DayOfWeek)((fd + c) % 7);
            var th = new TextBlock { Text = words.Narrow(wd), Classes = { "cal-wd" } };
            AutomationProperties.SetName(th, words.LongDay(wd));
            Grid.SetColumn(th, c);
            grid.Children.Add(th);
        }
        var selected = SelectedDate?.Date;
        var today = Today();
        var range = Mode == DatePickMode.Range ? (From: selected, To: EndDate?.Date) : (From: (DateTime?)null, To: (DateTime?)null);
        var cell = first.AddDays(-(((int)first.DayOfWeek - fd + 7) % 7));
        for (var r = 0; r < 6; r++)
        {
            for (var k = 0; k < 7; k++)
            {
                var day = cell;
                cell = cell.AddDays(1);
                var inMonth = sys.SameMonth(day, first);
                if (_dual && !inMonth) continue;
                var btn = new Button { Content = PickerLocale.N(sys.Parts(day).D), Classes = { "cal-day" }, Tag = day };
                AutomationProperties.SetName(btn, words.FullDay(sys, day));
                KeyboardNavigation.SetIsTabStop(btn, day == _focus);
                if (!inMonth) btn.Classes.Add("outside");
                if (day == today) btn.Classes.Add("today");
                if (day == selected) btn.Classes.Add("selected");
                if ((min is { } lo && day < lo) || (max is { } hi && day > hi)) btn.Classes.Add("blocked");
                if (Mode == DatePickMode.Range && day != selected && (day == range.From || day == range.To)) btn.Classes.Add("edge");
                btn.AddHandler(PointerPressedEvent, (_, pe) => _shift = pe.KeyModifiers.HasFlag(KeyModifiers.Shift), RoutingStrategies.Tunnel);
                btn.Click += OnDayClick;
                btn.PointerEntered += (_, _) => { if (!btn.Classes.Contains("blocked")) Preview(day); };
                var holder = new Border { Child = btn, Classes = { "cal-cell" }, Tag = r * 7 + k };
                Grid.SetRow(holder, r + 1);
                Grid.SetColumn(holder, k);
                grid.Children.Add(holder);
                _days[day] = btn;
                _cells[day] = holder;
            }
        }
        _grids.Add(grid);
        return grid;
    }

    private Border BuildFoot(PickerLocale words, PickerCalendar sys)
    {
        var parts = new List<Control>();
        var stretch = new List<int>();
        var today = new Button { Content = words.Today, Classes = { "bare", "sm", "cal-today" } };
        today.Click += (_, _) => TodayInvoked?.Invoke(this, EventArgs.Empty);
        parts.Add(today);
        if (Mode == DatePickMode.Range) parts.Add(new TextBlock { Text = IsPickingEnd ? words.PickEnd : words.PickStart, Classes = { "cal-hint" } });
        Control? clear = null;
        Control? done = null;
        if (!IsRequired)
        {
            var b = new Button { Content = words.Clear, Classes = { "bare", "sm", "cal-clear" } };
            b.Click += (_, _) => ClearInvoked?.Invoke(this, EventArgs.Empty);
            clear = b;
        }
        if (Mode == DatePickMode.DateTime)
        {
            var b = new Button { Content = words.Done, Classes = { "primary", "sm", "cal-done" } };
            b.Click += (_, _) => DoneInvoked?.Invoke(this, EventArgs.Empty);
            done = b;
        }
        if (CalendarKind != CalendarKind.Gregorian)
        {
            stretch.Add(parts.Count);
            parts.Add(BuildSwitch(words));
            if (clear is not null || done is not null) stretch.Add(parts.Count);
        }
        else if (clear is not null || done is not null) stretch.Add(parts.Count);
        if (clear is not null) parts.Add(clear);
        if (done is not null) parts.Add(done);

        var foot = new Grid { ColumnSpacing = 12 };
        var col = 0;
        for (var i = 0; i < parts.Count; i++)
        {
            if (stretch.Contains(i))
            {
                foot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                col++;
            }
            foot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.SetColumn(parts[i], col++);
            foot.Children.Add(parts[i]);
        }
        if (!stretch.Any()) foot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        return new Border { Child = foot, Classes = { "cal-foot" } };
    }

    private Border BuildSwitch(PickerLocale words)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
        var flight = new Canvas { Classes = { "cal-switch-flight" }, IsHitTestVisible = false };
        var home = new Button { Content = words.HijriLabel, Classes = { "cal-switch-option" }, Tag = "home" };
        var greg = new Button { Content = words.Gregorian, Classes = { "cal-switch-option" }, Tag = "gregory" };
        (ShowGregorian ? greg : home).Classes.Add("active");
        home.Click += (_, _) => Flip(false);
        greg.Click += (_, _) => Flip(true);
        row.Children.Add(home);
        row.Children.Add(greg);
        var track = new Border { Child = new Panel { Children = { flight, row } }, Classes = { "cal-switch" } };
        AutomationProperties.SetName(track, words.CalendarSystem);
        return track;
    }

    private void Flip(bool gregorian)
    {
        if (ShowGregorian == gregorian) return;
        var before = CalendarTurn.Snap(this, _turn);
        SetCurrentValue(ShowGregorianProperty, gregorian);
        _view = ViewCalendar.MonthStart(_focus);
        Rebuild();
        _turn = CalendarTurn.Play(this, before);
        SystemInvoked?.Invoke(this, gregorian);
        var back = _host?.GetLogicalDescendantsOfButtons().FirstOrDefault(b => b.Tag as string == (gregorian ? "gregory" : "home"));
        back?.Focus(NavigationMethod.Tab);
    }

    private void Turn(int by, string? tag)
    {
        var sys = ViewCalendar;
        _focus = sys.AddMonths(_focus, by);
        _view = sys.MonthStart(_focus);
        Rebuild();
        if (tag is not null) _host?.GetLogicalDescendantsOfButtons().FirstOrDefault(b => b.Tag as string == tag)?.Focus(NavigationMethod.Pointer);
    }

    private void Jump(int y, int m, string tag)
    {
        var sys = ViewCalendar;
        _focus = sys.FromParts(y, m, Math.Min(sys.Parts(_focus).D, sys.MonthLength(y, m)));
        _view = sys.FromParts(y, m, 1);
        Rebuild();
    }

    private void OnDayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DateTime day } btn || btn.Classes.Contains("blocked")) return;
        var shift = _shift;
        _shift = false;
        if (shift && Mode == DatePickMode.Range) DayShiftInvoked?.Invoke(this, day);
        else DayInvoked?.Invoke(this, day);
    }

    /// <summary>While a range waits for its end, the band follows the pointer or the focused day.</summary>
    public void Preview(DateTime? d)
    {
        if (Mode != DatePickMode.Range || !IsPickingEnd || SelectedDate is null) return;
        PaintBand(d);
    }

    // calCorners: the band's outer corners only, a corner round where the band does not go on past either of its sides.
    private void PaintBand(DateTime? preview)
    {
        var from = SelectedDate?.Date;
        var to = EndDate?.Date;
        var rangeOn = Mode == DatePickMode.Range && from is not null && to is not null;
        var previewOn = Mode == DatePickMode.Range && IsPickingEnd && from is not null && preview is { } p && p >= from;
        var radius = this.TryFindResource("InsRSm", out var rs) && rs is CornerRadius cr ? cr.TopLeft : 10;
        foreach (var grid in _grids)
        {
            var on = new bool[6, 7];
            var cells = grid.Children.OfType<Border>().Where(b => b.Classes.Contains("cal-cell")).ToList();
            foreach (var c in cells)
            {
                var idx = (int)c.Tag!;
                var day = (DateTime)((Button)c.Child!).Tag!;
                on[idx / 7, idx % 7] = (rangeOn && day >= from && day <= to) || (previewOn && day >= from && day <= preview);
            }
            bool At(int y, int x) => y >= 0 && y < 6 && x >= 0 && x < 7 && on[y, x];
            foreach (var c in cells)
            {
                var idx = (int)c.Tag!;
                int r = idx / 7, k = idx % 7;
                var x = on[r, k];
                c.Classes.Set("band", x);
                c.CornerRadius = new CornerRadius(
                    x && !At(r, k - 1) && !At(r - 1, k) ? radius : 0,
                    x && !At(r, k + 1) && !At(r - 1, k) ? radius : 0,
                    x && !At(r, k + 1) && !At(r + 1, k) ? radius : 0,
                    x && !At(r, k - 1) && !At(r + 1, k) ? radius : 0);
            }
        }
    }

    // A button takes Enter for its own click, so Shift-Enter on a day of a range is read on the way down.
    private void OnShiftEnter(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !e.KeyModifiers.HasFlag(KeyModifiers.Shift) || Mode != DatePickMode.Range) return;
        if (e.Source is not Button { Tag: DateTime d } btn || !btn.Classes.Contains("cal-day") || btn.Classes.Contains("blocked")) return;
        e.Handled = true;
        DayShiftInvoked?.Invoke(this, d);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        if (e.Key == Key.Escape) { e.Handled = true; CloseRequested?.Invoke(this, EventArgs.Empty); return; }
        if (e.Key == Key.Enter && e.Source is Visual v && v.FindAncestorOfType<TimeDrum>(true) is not null) { e.Handled = true; CloseRequested?.Invoke(this, EventArgs.Empty); return; }
        if (e.Source is not Button { Tag: DateTime d } btn || !btn.Classes.Contains("cal-day")) return;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var fd = (int)Words.WeekStart;
        var col = ((int)d.DayOfWeek - fd + 7) % 7;
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var year = ctrl || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var sys = ViewCalendar;
        var p = sys.Parts(d);
        DateTime? next = e.Key switch
        {
            Key.Right => d.AddDays(rtl ? -1 : 1),
            Key.Left => d.AddDays(rtl ? 1 : -1),
            Key.Down => d.AddDays(7),
            Key.Up => d.AddDays(-7),
            Key.Home => ctrl ? sys.FromParts(p.Y, p.M, 1) : d.AddDays(-col),
            Key.End => ctrl ? sys.FromParts(p.Y, p.M, sys.MonthLength(p.Y, p.M)) : d.AddDays(6 - col),
            Key.PageUp => sys.AddMonths(d, year ? -12 : -1),
            Key.PageDown => sys.AddMonths(d, year ? 12 : 1),
            _ => null,
        };
        if (next is null) return;
        e.Handled = true;
        MoveTo(next.Value);
    }
}

internal static class CalendarTreeExtensions
{
    public static IEnumerable<Button> GetLogicalDescendantsOfButtons(this Decorator host) =>
        global::Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(host).OfType<Button>();

    public static T? FindAncestorOfType<T>(this Visual v, bool includeSelf) where T : Visual
    {
        for (Visual? x = includeSelf ? v : v.GetVisualParentSafe(); x is not null; x = x.GetVisualParentSafe())
            if (x is T t) return t;
        return null;
    }

    private static Visual? GetVisualParentSafe(this Visual v) => global::Avalonia.VisualTree.VisualExtensions.GetVisualParent(v);
}
