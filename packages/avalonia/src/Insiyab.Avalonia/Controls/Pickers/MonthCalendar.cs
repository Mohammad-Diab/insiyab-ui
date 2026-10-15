using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The tone of an event in the month calendar (the web's <c>data-tone</c>).</summary>
public enum MonthEventTone
{
    Brand,
    Ok,
    Warn,
    Bad,
    Info,
}

/// <summary>What a <see cref="MonthCalendar"/> shows: the month's weeks, one week by the hour, or one day by the hour.</summary>
public enum MonthCalendarView
{
    Month,
    Week,
    Day,
}

/// <summary>One event in a <see cref="MonthCalendar"/>: its day, its words, and optionally its time, length, tone and link.</summary>
public sealed class MonthEvent
{
    public DateTime Date { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Written before the title, and the order of the day's events; without it the event takes the whole day, in the all-day row of the week and day views.</summary>
    public TimeSpan? Time { get; set; }

    /// <summary>How long a timed event lasts, which sets its height in the week and day views; an hour unless set.</summary>
    public TimeSpan? Duration { get; set; }

    public MonthEventTone Tone { get; set; }

    /// <summary>A link: the event can be pressed, and fills with its tone under the pointer.</summary>
    public string? Href { get; set; }
}

/// <summary>The month calendar plugin (the web's <c>.ins-month</c>): a month as a table of weeks starting on the locale's first day, today marked, each day showing three events and a "+n" for the rest, dots instead of names when narrow, and a header that moves a month back, on, or to today. The desktop adds a week and a day view by the hour, the work hours only unless asked, with a switch between the three.</summary>
[TemplatePart("PART_Host", typeof(Decorator))]
[PseudoClasses(":narrow", ":month", ":week", ":day")]
public class MonthCalendar : TemplatedControl
{
    public static readonly StyledProperty<DateTime> MonthProperty =
        AvaloniaProperty.Register<MonthCalendar, DateTime>(nameof(Month), new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));

    public static readonly StyledProperty<DateTime> DateProperty =
        AvaloniaProperty.Register<MonthCalendar, DateTime>(nameof(Date), DateTime.Today);

    public static readonly StyledProperty<IList<MonthEvent>?> EventsProperty =
        AvaloniaProperty.Register<MonthCalendar, IList<MonthEvent>?>(nameof(Events));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<MonthCalendar, string?>(nameof(Locale));

    public static readonly StyledProperty<MonthCalendarView> ViewProperty =
        AvaloniaProperty.Register<MonthCalendar, MonthCalendarView>(nameof(View));

    public static readonly StyledProperty<bool> HasViewSwitchProperty =
        AvaloniaProperty.Register<MonthCalendar, bool>(nameof(HasViewSwitch), true);

    public static readonly StyledProperty<int> DayStartHourProperty =
        AvaloniaProperty.Register<MonthCalendar, int>(nameof(DayStartHour), 6);

    public static readonly StyledProperty<int> DayEndHourProperty =
        AvaloniaProperty.Register<MonthCalendar, int>(nameof(DayEndHour), 18);

    public static readonly StyledProperty<bool> WorkHoursOnlyProperty =
        AvaloniaProperty.Register<MonthCalendar, bool>(nameof(WorkHoursOnly), true);

    public static readonly StyledProperty<bool> HasHoursSwitchProperty =
        AvaloniaProperty.Register<MonthCalendar, bool>(nameof(HasHoursSwitch), true);

    public static readonly StyledProperty<double> HourHeightProperty =
        AvaloniaProperty.Register<MonthCalendar, double>(nameof(HourHeight), 48);

    private const int Shown = 3;
    private const double Shortest = 22;
    private Decorator? _host;
    private bool _narrow;
    private bool _hold;
    private bool _syncSwitch;
    private readonly Dictionary<DateTime, Border> _cells = [];
    private readonly Dictionary<MonthEvent, Border> _chips = [];
    private StackPanel? _root;
    private Grid? _head;
    private PickerLocale? _headWords;
    private TextBlock? _title;
    private Button? _prev;
    private Button? _next;
    private Segmented? _switch;
    private DayTimeline? _nowPanel;
    private Control? _now;
    private DateTime _drawnToday;
    private DispatcherTimer? _clock;
    private bool _stale;
    private ToggleButton? _hours;
    private ScrollViewer? _scroll;
    private (DateTime First, MonthCalendarView View)? _scrollKey;
    private double _scrollAt;
    private double? _scrollTo;
    private bool _scrollReady;
    private readonly Dictionary<(DateTime Day, bool After), Border> _bands = [];

    public MonthCalendar()
    {
        Events = new AvaloniaList<MonthEvent>();
        AddHandler(KeyDownEvent, OnTurnKey, RoutingStrategies.Tunnel);
    }

    /// <summary>Raised when the header moves to another month (the web's <c>ins:month</c>), with its first day.</summary>
    public event EventHandler<DateTime>? MonthChanged;

    /// <summary>Raised when the header moves the week or day view, with the new <see cref="Date"/>.</summary>
    public event EventHandler<DateTime>? DateChanged;

    /// <summary>Raised when the user switches the view, or opens a day from the week view's heading.</summary>
    public event EventHandler<MonthCalendarView>? ViewChanged;

    /// <summary>Raised when a day is pressed away from its events (<c>ins:month-day</c>); in the week and day views, with the time of the half hour pressed.</summary>
    public event EventHandler<DateTime>? DayInvoked;

    /// <summary>Raised when a linked event is pressed.</summary>
    public event EventHandler<MonthEvent>? EventInvoked;

    /// <summary>The month shown; any day in it will do.</summary>
    public DateTime Month { get => GetValue(MonthProperty); set => SetValue(MonthProperty, value); }

    /// <summary>The day the week and day views show, today unless set; kept inside <see cref="Month"/>.</summary>
    public DateTime Date { get => GetValue(DateProperty); set => SetValue(DateProperty, value); }

    /// <summary>The events, empty to begin with, so <see cref="MonthEvent"/>s can be written inside the calendar in XAML.</summary>
    [Content]
    public IList<MonthEvent>? Events { get => GetValue(EventsProperty); set => SetValue(EventsProperty, value); }

    /// <summary>"ar" or "en"; unset, it follows the direction. It decides the names and the week's first day.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    /// <summary>The month's weeks, one week by the hour, or one day by the hour.</summary>
    public MonthCalendarView View { get => GetValue(ViewProperty); set => SetValue(ViewProperty, value); }

    /// <summary>The month, week and day switch in the header.</summary>
    public bool HasViewSwitch { get => GetValue(HasViewSwitchProperty); set => SetValue(HasViewSwitchProperty, value); }

    /// <summary>The hour the work hours start at, 6 unless set; kept within 0 to 23.</summary>
    public int DayStartHour { get => GetValue(DayStartHourProperty); set => SetValue(DayStartHourProperty, value); }

    /// <summary>The hour the work hours end at, 18 unless set; 24 is midnight, and it stays after the start.</summary>
    public int DayEndHour { get => GetValue(DayEndHourProperty); set => SetValue(DayEndHourProperty, value); }

    /// <summary>The week and day views show only the work hours (the default), and count the events outside them at the edges; false shows the whole day, scrolled to the work start.</summary>
    public bool WorkHoursOnly { get => GetValue(WorkHoursOnlyProperty); set => SetValue(WorkHoursOnlyProperty, value); }

    /// <summary>The work-hours toggle in the header of the week and day views.</summary>
    public bool HasHoursSwitch { get => GetValue(HasHoursSwitchProperty); set => SetValue(HasHoursSwitchProperty, value); }

    /// <summary>How tall an hour is in the week and day views.</summary>
    public double HourHeight { get => GetValue(HourHeightProperty); set => SetValue(HourHeightProperty, value); }

    internal IReadOnlyDictionary<DateTime, Border> Cells => _cells;

    internal IReadOnlyDictionary<MonthEvent, Border> Chips => _chips;

    internal Control? NowLine => _now;

    internal IReadOnlyDictionary<(DateTime Day, bool After), Border> Bands => _bands;

    internal ScrollViewer? HoursScroller => _scroll;

    internal static Func<DateTime> Now { get; set; } = () => DateTime.Now;

    private PickerLocale Words => PickerLocale.For(Locale, FlowDirection);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host = e.NameScope.Find<Decorator>("PART_Host");
        Draw();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _clock ??= new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, (_, _) => Tick());
        _clock.Start();
        if (_stale) Draw();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _clock?.Stop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EventsProperty)
        {
            if (change.OldValue is INotifyCollectionChanged o) o.CollectionChanged -= OnEventsChanged;
            if (change.NewValue is INotifyCollectionChanged n) n.CollectionChanged += OnEventsChanged;
        }
        if (_hold) return;
        if (change.Property == MonthProperty && !SameMonth(Date, Month))
            Hold(() => SetCurrentValue(DateProperty, First(Month)));
        else if (change.Property == DateProperty && !SameMonth(Date, Month))
            Hold(() => SetCurrentValue(MonthProperty, First(Date)));
        if (change.Property == MonthProperty || change.Property == DateProperty || change.Property == EventsProperty || change.Property == LocaleProperty
            || change.Property == FlowDirectionProperty || change.Property == ViewProperty || change.Property == HasViewSwitchProperty
            || change.Property == DayStartHourProperty || change.Property == DayEndHourProperty || change.Property == HourHeightProperty
            || change.Property == WorkHoursOnlyProperty || change.Property == HasHoursSwitchProperty)
            Draw();
        else if (change.Property == BoundsProperty)
        {
            var narrow = Bounds.Width > 0 && Bounds.Width < 544;
            if (narrow != _narrow)
            {
                _narrow = narrow;
                PseudoClasses.Set(":narrow", narrow);
            }
        }
    }

    private void Hold(Action set)
    {
        _hold = true;
        try
        {
            set();
        }
        finally
        {
            _hold = false;
        }
    }

    private static bool SameMonth(DateTime a, DateTime b) => a.Year == b.Year && a.Month == b.Month;

    private static DateTime First(DateTime d) => new(d.Year, d.Month, 1);

    private void OnEventsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Draw();

    // PageUp and PageDown anywhere inside it, the hours' scroll box included, turn the month (or the week, or the day); the header's button that held focus gets it back, and focus on a chip or a day that the turn threw away goes to the matching arrow.
    private void OnTurnKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.PageUp or Key.PageDown)) return;
        e.Handled = true;
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        var held = (focused as Button)?.Tag as string;
        Go(e.Key == Key.PageUp ? -1 : 1);
        if (held is not null) _host?.GetLogicalDescendantsOfButtons().FirstOrDefault(b => b.Tag as string == held)?.Focus(NavigationMethod.Directional);
        else if (focused is not null && !focused.IsAttachedToVisualTree()) (e.Key == Key.PageUp ? _prev : _next)?.Focus(NavigationMethod.Directional);
    }

    /// <summary>Moves back (-1), on (1), or to today (0) by the view's step: a month, a week or a day. A month says so with <see cref="MonthChanged"/>, a week or a day with <see cref="DateChanged"/>.</summary>
    public void Go(int by)
    {
        var now = DateCalendar.Today();
        if (View == MonthCalendarView.Month)
        {
            var next = by == 0 ? First(now) : First(Month).AddMonths(by);
            Hold(() =>
            {
                SetCurrentValue(DateProperty, by == 0 ? now : next);
                SetCurrentValue(MonthProperty, next);
            });
            Draw();
            MonthChanged?.Invoke(this, next);
            return;
        }
        var day = by == 0 ? now : Date.Date.AddDays(by * (View == MonthCalendarView.Week ? 7 : 1));
        Hold(() =>
        {
            SetCurrentValue(DateProperty, day);
            SetCurrentValue(MonthProperty, First(day));
        });
        Draw();
        DateChanged?.Invoke(this, day);
    }

    private void SwitchTo(MonthCalendarView view, DateTime? day = null)
    {
        Hold(() =>
        {
            if (day is { } d)
            {
                SetCurrentValue(DateProperty, d);
                SetCurrentValue(MonthProperty, First(d));
            }
            SetCurrentValue(ViewProperty, view);
        });
        Draw();
        ViewChanged?.Invoke(this, view);
    }

    // The first day the view shows and how many: a week from the locale's first day, or the one day.
    private (DateTime First, int Days) Span(PickerLocale words)
    {
        var d = Date.Date;
        if (View == MonthCalendarView.Day) return (d, 1);
        var start = (int)words.WeekStart;
        return (d.AddDays(-(((int)d.DayOfWeek - start + 7) % 7)), 7);
    }

    private void Draw()
    {
        if (_host is null) return;
        // Out of the tree (a page taken down) it draws when it comes back, as its resources are out of reach meanwhile.
        _stale = !this.IsAttachedToVisualTree();
        if (_stale) return;
        _cells.Clear();
        _bands.Clear();
        _scroll = null;
        _chips.Clear();
        _now = null;
        _nowPanel = null;
        _drawnToday = DateCalendar.Today();
        var words = Words;
        var view = View;
        PseudoClasses.Set(":month", view == MonthCalendarView.Month);
        PseudoClasses.Set(":week", view == MonthCalendarView.Week);
        PseudoClasses.Set(":day", view == MonthCalendarView.Day);
        var byDay = (Events ?? []).GroupBy(e => e.Date.Date).ToDictionary(g => g.Key, g => g.OrderBy(e => e.Time?.Ticks ?? -1).ToList());

        if (_root is null || _host.Child != _root)
        {
            if (_head?.Parent is Panel old) old.Children.Remove(_head);
            _root = new StackPanel();
            _host.Child = _root;
        }
        var head = Head(words, view);
        if (_root.Children.Count == 0 || _root.Children[0] != head)
        {
            _root.Children.Clear();
            _root.Children.Add(head);
        }
        Control body = view == MonthCalendarView.Month ? MonthGrid(words, byDay) : TimeGrid(words, view, byDay);
        if (_root.Children.Count > 1) _root.Children[1] = body;
        else _root.Children.Add(body);
    }

    // The header lives across draws, so the button that holds focus keeps it and the view switch keeps its place; only the body is built again.
    private Grid Head(PickerLocale words, MonthCalendarView view)
    {
        if (_head is null || _headWords != words)
        {
            if (_head?.Parent is Panel p) p.Children.Remove(_head);
            _headWords = words;
            _head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Classes = { "month-head" } };
            _title = new TextBlock { Classes = { "month-title" } };
            _head.Children.Add(_title);
            var nav = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Classes = { "month-nav" } };
            _switch = new Segmented { Classes = { "month-views" } };
            _syncSwitch = true;
            _switch.ItemsSource = words.IsArabic ? new[] { "شهر", "أسبوع", "يوم" } : new[] { "Month", "Week", "Day" };
            _syncSwitch = false;
            AutomationProperties.SetName(_switch, words.IsArabic ? "طريقة العرض" : "View");
            _switch.SelectionChanged += (_, _) =>
            {
                if (_syncSwitch || _switch.SelectedIndex < 0) return;
                var to = (MonthCalendarView)_switch.SelectedIndex;
                if (to != View) Dispatcher.UIThread.Post(() => SwitchTo(to));
            };
            _hours = new ToggleButton { Content = words.IsArabic ? "ساعات الدوام" : "Work hours", Classes = { "secondary", "sm", "month-hours" }, Tag = "hours" };
            _hours.Click += (_, _) => SetHours(_hours.IsChecked == true, null);
            nav.Children.Add(_hours);
            nav.Children.Add(_switch);
            _prev = Arrow(false, () => Go(-1));
            nav.Children.Add(_prev);
            var today = new Button { Content = words.Today, Classes = { "secondary", "sm" }, Tag = "today" };
            today.Click += (_, _) => Go(0);
            nav.Children.Add(today);
            _next = Arrow(true, () => Go(1));
            nav.Children.Add(_next);
            Grid.SetColumn(nav, 1);
            _head.Children.Add(nav);
        }

        var greg = PickerCalendar.Gregorian;
        var (first, _) = Span(words);
        _title!.Text = view switch
        {
            MonthCalendarView.Week => words.FormatRange(greg, first, first.AddDays(6)),
            MonthCalendarView.Day => words.FullDay(greg, first),
            _ => words.MonthYear(greg, First(Month)),
        };
        var ar = words.IsArabic;
        var (back, on) = view switch
        {
            MonthCalendarView.Week => (ar ? "الأسبوع السابق" : "Previous week", ar ? "الأسبوع التالي" : "Next week"),
            MonthCalendarView.Day => (ar ? "اليوم السابق" : "Previous day", ar ? "اليوم التالي" : "Next day"),
            _ => (words.PrevMonth, words.NextMonth),
        };
        AutomationProperties.SetName(_prev!, back);
        AutomationProperties.SetName(_next!, on);
        _switch!.IsVisible = HasViewSwitch;
        var (ws, we) = WorkHours();
        _hours!.IsVisible = view != MonthCalendarView.Month && HasHoursSwitch && Trims;
        _hours.IsChecked = WorkHoursOnly;
        var from = words.FormatTime(TimeSpan.FromHours(ws));
        var to = words.FormatTime(TimeSpan.FromHours(we));
        ToolTip.SetTip(_hours, ar ? $"اعرض ساعات الدوام وحدها، من {from} إلى {to}" : $"Show only the work hours, {from} to {to}");
        AutomationProperties.SetName(_hours, words.IsArabic ? "ساعات الدوام" : "Work hours");
        _syncSwitch = true;
        _switch.SelectedIndex = (int)view;
        _syncSwitch = false;
        return _head;
    }

    private void SetHours(bool workOnly, double? scrollTo)
    {
        Hold(() => SetCurrentValue(WorkHoursOnlyProperty, workOnly));
        _scrollTo = scrollTo;
        Draw();
    }

    private static string Count(bool ar, int n)
    {
        if (!ar) return n == 1 ? "1 event" : $"{PickerLocale.N(n)} events";
        var r = n % 100;
        return n == 1 ? "موعد واحد" : n == 2 ? "موعدان" : PickerLocale.N(n) + (r is >= 3 and <= 10 ? " مواعيد" : r >= 11 ? " موعدًا" : " موعد");
    }

    private Button Arrow(bool next, Action go)
    {
        var path = new global::Avalonia.Controls.Shapes.Path { Data = this.FindResource(next ? "InsMonthArrowIcon" : "InsMonthArrowBackIcon") as Geometry, Classes = { "month-arrow" } };
        var b = new Button { Content = new Viewbox { Width = 16, Height = 16, Child = new Canvas { Width = 24, Height = 24, Children = { path } } }, Classes = { "bare", "icon", "sm", "month-go", next ? "next" : "prev" }, Tag = next ? "next" : "prev" };
        b.Click += (_, _) => go();
        return b;
    }

    private Grid MonthGrid(PickerLocale words, Dictionary<DateTime, List<MonthEvent>> byDay)
    {
        var month = First(Month);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"), ColumnSpacing = 3, RowSpacing = 3, Margin = new Thickness(3), Classes = { "month-grid" } };
        var start = (int)words.WeekStart;
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var d = 0; d < 7; d++)
        {
            var wd = (DayOfWeek)((start + d) % 7);
            var th = new TextBlock { Text = words.ShortDay(wd), Classes = { "month-wd" } };
            AutomationProperties.SetName(th, words.LongDay(wd));
            Grid.SetColumn(th, d);
            grid.Children.Add(th);
        }
        var lead = ((int)month.DayOfWeek - start + 7) % 7;
        var cur = month.AddDays(-lead);
        var now = DateCalendar.Today();
        for (var week = 0; week < 6; week++)
        {
            if (week == 5 && cur.Month != month.Month) break;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var k = 0; k < 7; k++)
            {
                var day = cur;
                cur = cur.AddDays(1);
                var list = byDay.TryGetValue(day, out var l) ? l : [];
                var cell = BuildDay(words, day, list, day.Month != month.Month, day == now);
                Grid.SetRow(cell, week + 1);
                Grid.SetColumn(cell, k);
                grid.Children.Add(cell);
                _cells[day] = cell;
            }
        }
        return grid;
    }

    private Border BuildDay(PickerLocale words, DateTime day, List<MonthEvent> list, bool other, bool isToday)
    {
        var stack = new StackPanel();
        var num = new Border { Child = new TextBlock { Text = PickerLocale.N(day.Day) }, Classes = { "month-num" } };
        AutomationProperties.SetName(num, DayName(words, day));
        stack.Children.Add(num);
        if (list.Count > 0)
        {
            var events = new StackPanel { Spacing = 2, Classes = { "month-events" } };
            foreach (var ev in list.Take(list.Count > Shown ? Shown - 1 : Shown)) events.Children.Add(Chip(words, ev));
            if (list.Count > Shown)
            {
                var more = new TextBlock { Text = "+" + PickerLocale.N(list.Count - Shown + 1), Classes = { "month-more" } };
                ToolTip.SetTip(more, string.Join("، ", list.Skip(Shown - 1).Select(e => e.Title)));
                events.Children.Add(more);
            }
            stack.Children.Add(events);
            var dots = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, Classes = { "month-dots" } };
            foreach (var ev in list.Take(4)) dots.Children.Add(new Ellipse { Classes = { "month-dot", ToneClass(ev.Tone) } });
            stack.Children.Add(dots);
        }
        var cell = new Border { Child = stack, Classes = { "month-day" }, Tag = day };
        if (other) cell.Classes.Add("other");
        if (isToday) cell.Classes.Add("today");
        cell.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton != MouseButton.Left || e.Handled) return;
            DayInvoked?.Invoke(this, day);
        };
        return cell;
    }

    private static string DayName(PickerLocale words, DateTime day) =>
        words.IsArabic ? $"{PickerLocale.N(day.Day)} {words.MonthName(PickerCalendar.Gregorian, day.Month - 1)}" : $"{words.MonthName(PickerCalendar.Gregorian, day.Month - 1)} {PickerLocale.N(day.Day)}";

    // The week and day views: day headings, an all-day row, then the hours (the work hours, or the whole day in a scroll box) with each day's events placed by their time and length, side by side where they overlap; events wholly outside the hours are counted in a band above or below.
    private Border TimeGrid(PickerLocale words, MonthCalendarView view, Dictionary<DateTime, List<MonthEvent>> byDay)
    {
        var (first, days) = Span(words);
        var (ws, we) = WorkHours();
        var (startH, endH) = ShownHours();
        var scrolls = (startH, endH) != (ws, we);
        var hourH = Math.Max(16, HourHeight);
        var today = DateCalendar.Today();
        var top = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), Classes = { "week-top" } };
        var body = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), Classes = { "week-body" } };
        foreach (var g in new[] { top, body })
        {
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "WeekHours" });
            for (var d = 0; d < days; d++) g.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }
        if (scrolls)
        {
            top.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var gap = new Border { Classes = { "week-bar-gap" } };
            Grid.SetColumn(gap, days + 1);
            Grid.SetRowSpan(gap, 2);
            top.Children.Add(gap);
        }

        var corner = new Border { Classes = { "week-corner" } };
        top.Children.Add(corner);
        var allLabel = new TextBlock { Text = words.IsArabic ? "طوال اليوم" : "All day", Classes = { "week-allday-label" } };
        Grid.SetRow(allLabel, 1);
        top.Children.Add(allLabel);

        var gutter = new StackPanel { Classes = { "week-hours" } };
        var lines = new StackPanel { IsHitTestVisible = false, Classes = { "week-lines" } };
        for (var h = startH; h < endH; h++)
        {
            gutter.Children.Add(new Border { Height = hourH, Classes = { "week-hour-cell" }, Child = new TextBlock { Text = words.FormatTime(new TimeSpan(h, 0, 0)), Classes = { "week-hour" } } });
            lines.Children.Add(new Border { Height = hourH, Classes = { "week-line" } });
        }
        Grid.SetRow(gutter, 1);
        body.Children.Add(gutter);
        Grid.SetRow(lines, 1);
        Grid.SetColumn(lines, 1);
        Grid.SetColumnSpan(lines, days);
        body.Children.Add(lines);

        static double StartOf(MonthEvent e) => e.Time!.Value.TotalMinutes;
        static double EndOf(MonthEvent e) => StartOf(e) + Length(e).TotalMinutes;
        var from = startH * 60.0;
        var to = endH * 60.0;
        var hiddenBefore = new List<MonthEvent>[days];
        var hiddenAfter = new List<MonthEvent>[days];

        for (var d = 0; d < days; d++)
        {
            var day = first.AddDays(d);
            var list = byDay.TryGetValue(day, out var l) ? l : [];
            var isToday = day == today;

            var name = new TextBlock { Text = words.ShortDay(day.DayOfWeek), Classes = { "week-wd" } };
            var num = new Border { Child = new TextBlock { Text = PickerLocale.N(day.Day) }, Classes = { "month-num" } };
            var head = new Border { Child = new StackPanel { Children = { name, num } }, Classes = { "week-head" }, Tag = day };
            AutomationProperties.SetName(head, words.FullDay(PickerCalendar.Gregorian, day));
            if (isToday) head.Classes.Add("today");
            if (view == MonthCalendarView.Week)
            {
                head.Classes.Add("link");
                head.PointerReleased += (_, e) =>
                {
                    if (e.InitialPressMouseButton != MouseButton.Left) return;
                    e.Handled = true;
                    SwitchTo(MonthCalendarView.Day, day);
                };
            }
            Grid.SetColumn(head, d + 1);
            top.Children.Add(head);

            var allDay = new StackPanel { Spacing = 2, Classes = { "week-allday" } };
            foreach (var ev in list.Where(e => e.Time is null))
            {
                var chip = Chip(words, ev);
                allDay.Children.Add(chip);
                if (chip is Border b) _chips[ev] = b;
            }
            var allCell = new Border { Child = allDay, Classes = { "week-allday-cell" } };
            Grid.SetRow(allCell, 1);
            Grid.SetColumn(allCell, d + 1);
            top.Children.Add(allCell);

            var lane = new DayTimeline { HourHeight = hourH, From = startH * 60, To = endH * 60 };
            var timed = list.Where(e => e.Time is not null).ToList();
            var before = timed.Where(e => EndOf(e) <= from).ToList();
            var after = timed.Where(e => StartOf(e) >= to).ToList();
            var inside = timed.Where(e => !before.Contains(e) && !after.Contains(e)).ToList();
            hiddenBefore[d] = before;
            hiddenAfter[d] = after;
            var spans = inside.Select(e => Visible(e, lane, hourH)).ToList();
            var lanes = Lanes(spans);
            for (var i = 0; i < inside.Count; i++)
            {
                var chip = TimedChip(words, inside[i]);
                if (StartOf(inside[i]) < from) chip.Classes.Add("cut-start");
                if (EndOf(inside[i]) > to) chip.Classes.Add("cut-end");
                lane.Add(chip, spans[i].Start, spans[i].End, lanes[i].Lane, lanes[i].Lanes);
                _chips[inside[i]] = chip;
            }
            if (isToday && LineAt(lane) is { } at)
            {
                _now = new Panel { Classes = { "week-now" }, IsHitTestVisible = false, ZIndex = 2, Children = { new Border { Classes = { "week-now-line" } }, new Ellipse { Classes = { "week-now-dot" } } } };
                _nowPanel = lane;
                lane.SetLine(_now, at);
            }
            var column = new Border { Child = lane, Classes = { "week-day" } };
            if (isToday) column.Classes.Add("today");
            column.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton != MouseButton.Left || e.Handled) return;
                var minutes = lane.From + Math.Floor(e.GetPosition(lane).Y / hourH * 2) * 30;
                DayInvoked?.Invoke(this, day.AddMinutes(Math.Clamp(minutes, lane.From, lane.To - 30)));
            };
            Grid.SetRow(column, 1);
            Grid.SetColumn(column, d + 1);
            body.Children.Add(column);
        }
        if (hiddenBefore.Any(l => l.Count > 0)) AddBands(words, body, first, false, startH, hiddenBefore);
        if (hiddenAfter.Any(l => l.Count > 0)) AddBands(words, body, first, true, endH, hiddenAfter);

        Control hours = body;
        if (scrolls)
        {
            var key = (first, view);
            var y = _scrollTo is { } want ? Math.Max(0, want / 60 * hourH - hourH / 2) : _scrollKey == key ? _scrollAt : ws * hourH;
            _scrollTo = null;
            _scrollKey = key;
            _scrollAt = y;
            _scrollReady = false;
            var scroll = new ScrollViewer { Content = body, Height = Math.Clamp(we - ws, 4, 24) * hourH, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Visible, Classes = { "week-scroll" } };
            scroll.ScrollChanged += (_, _) => { if (_scrollReady && scroll == _scroll) _scrollAt = scroll.Offset.Y; };
            void Place(object? s, EventArgs e)
            {
                if (scroll.Extent.Height <= 0) return;
                scroll.LayoutUpdated -= Place;
                scroll.Offset = new Vector(0, y);
                _scrollAt = scroll.Offset.Y;
                _scrollReady = true;
            }
            scroll.LayoutUpdated += Place;
            _scroll = scroll;
            hours = scroll;
        }
        else
        {
            _scrollKey = null;
            _scrollTo = null;
        }
        Grid.SetRow(hours, 1);
        var outer = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };
        Grid.SetIsSharedSizeScope(outer, true);
        outer.Children.Add(top);
        outer.Children.Add(hours);
        return new Border { Child = outer, Classes = { "week-frame" } };
    }

    // The row above (row 0) or below (row 2) the hours: a label in the gutter and, in each day with hidden events, a pill counting them.
    private void AddBands(PickerLocale words, Grid body, DateTime first, bool after, int hour, List<MonthEvent>[] lists)
    {
        var ar = words.IsArabic;
        var row = after ? 2 : 0;
        var kind = after ? "after" : "before";
        var at = words.FormatTime(TimeSpan.FromHours(hour));
        var label = ar ? $"{(after ? "بعد" : "قبل")} {at}" : $"{(after ? "After" : "Before")} {at}";
        var gutter = new Border { Classes = { "week-band-gutter", kind }, Child = new TextBlock { Text = label, Classes = { "week-band-label" } } };
        Grid.SetRow(gutter, row);
        body.Children.Add(gutter);
        for (var d = 0; d < lists.Length; d++)
        {
            var cell = new Border { Classes = { "week-band-cell", kind } };
            if (lists[d].Count > 0) cell.Child = Band(words, first.AddDays(d), lists[d], after, hour);
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, d + 1);
            body.Children.Add(cell);
        }
    }

    private Border Band(PickerLocale words, DateTime day, List<MonthEvent> hidden, bool after, int hour)
    {
        var ar = words.IsArabic;
        var at = words.FormatTime(TimeSpan.FromHours(hour));
        var band = new Border { Child = new TextBlock { Text = "+" + PickerLocale.N(hidden.Count), Classes = { "week-band-count" } }, Classes = { "week-band", after ? "after" : "before" }, Focusable = true, Tag = day };
        AutomationProperties.SetName(band, ar ? $"{Count(true, hidden.Count)} {(after ? "بعد" : "قبل")} {at}" : $"{Count(false, hidden.Count)} {(after ? "after" : "before")} {at}");
        ToolTip.SetTip(band, string.Join(ar ? "، " : ", ", hidden.Select(e => $"{words.FormatTime(e.Time!.Value)} {e.Title}")));
        var first = hidden.Min(e => e.Time!.Value.TotalMinutes);
        void Open(bool keyboard)
        {
            SetHours(false, first);
            if (!keyboard) return;
            if (_hours is { IsVisible: true }) _hours.Focus(NavigationMethod.Tab);
            else _host?.GetLogicalDescendantsOfButtons().FirstOrDefault(b => b.Tag as string == "today")?.Focus(NavigationMethod.Tab);
        }
        band.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton != MouseButton.Left) return;
            e.Handled = true;
            Open(false);
        };
        band.KeyDown += (_, e) =>
        {
            if (e.Key is not (Key.Enter or Key.Space)) return;
            e.Handled = true;
            Open(true);
        };
        _bands[(day, after)] = band;
        return band;
    }

    private static TimeSpan Length(MonthEvent ev) => ev.Duration is { } d && d > TimeSpan.Zero ? d : TimeSpan.FromHours(1);

    // An event's minutes as drawn: inside the shown hours, and never shorter than a chip that can hold its title.
    private static (double Start, double End) Visible(MonthEvent ev, DayTimeline lane, double hourH)
    {
        var start = ev.Time!.Value.TotalMinutes;
        var end = Math.Min(24 * 60, start + Length(ev).TotalMinutes);
        var shortest = Shortest / hourH * 60;
        start = Math.Clamp(start, lane.From, lane.To - shortest);
        end = Math.Max(Math.Min(end, lane.To), start + shortest);
        return (start, end);
    }

    /// <summary>Lays overlapping events side by side: each event's lane and the number of lanes in its group of overlaps.</summary>
    internal static (int Lane, int Lanes)[] Lanes(IReadOnlyList<(double Start, double End)> spans)
    {
        var result = new (int Lane, int Lanes)[spans.Count];
        var order = Enumerable.Range(0, spans.Count).OrderBy(i => spans[i].Start).ThenByDescending(i => spans[i].End).ToList();
        var ends = new List<double>();
        var group = new List<int>();
        var groupEnd = double.MinValue;
        void Close()
        {
            foreach (var g in group) result[g].Lanes = ends.Count;
            group.Clear();
            ends.Clear();
            groupEnd = double.MinValue;
        }
        foreach (var i in order)
        {
            var (start, end) = spans[i];
            if (group.Count > 0 && start >= groupEnd) Close();
            var lane = ends.FindIndex(e => e <= start);
            if (lane < 0)
            {
                lane = ends.Count;
                ends.Add(end);
            }
            else
            {
                ends[lane] = end;
            }
            result[i].Lane = lane;
            group.Add(i);
            groupEnd = Math.Max(groupEnd, end);
        }
        Close();
        return result;
    }

    private static double? LineAt(DayTimeline lane)
    {
        var at = Now().TimeOfDay.TotalMinutes;
        return at >= lane.From && at <= lane.To ? at : null;
    }

    // Every half minute the now line moves on; a new day, or the line entering or leaving the shown hours, draws the view again.
    private void Tick()
    {
        if (View == MonthCalendarView.Month || _host is null) return;
        var (first, days) = Span(Words);
        var today = DateCalendar.Today();
        var shown = today >= first && today < first.AddDays(days);
        if (today != _drawnToday || (shown && _nowPanel is not null && LineAt(_nowPanel) is null) || (shown && _now is null && WantsLine()))
        {
            Draw();
            return;
        }
        if (_now is not null && _nowPanel is not null && LineAt(_nowPanel) is { } at) _nowPanel.SetLine(_now, at);
    }

    private bool WantsLine()
    {
        var at = Now().TimeOfDay.TotalMinutes;
        var (s, e) = ShownHours();
        return at >= s * 60 && at <= e * 60;
    }

    // The work hours as set, kept in range: the start within 0 to 23, the end after it up to 24.
    private (int Start, int End) WorkHours()
    {
        var start = Math.Clamp(DayStartHour, 0, 23);
        return (start, Math.Clamp(DayEndHour, start + 1, 24));
    }

    private bool Trims => WorkHours() is var (s, e) && (s > 0 || e < 24);

    private (int Start, int End) ShownHours() => WorkHoursOnly && Trims ? WorkHours() : (0, 24);

    private static string ToneClass(MonthEventTone tone) => tone switch
    {
        MonthEventTone.Ok => "ok",
        MonthEventTone.Warn => "warn",
        MonthEventTone.Bad => "bad",
        MonthEventTone.Info => "info",
        _ => "brand",
    };

    private Control Chip(PickerLocale words, MonthEvent ev)
    {
        var row = new DockPanel();
        if (ev.Time is { } t)
        {
            var time = new TextBlock { Text = words.FormatTime(t), Classes = { "month-time" } };
            DockPanel.SetDock(time, Dock.Left);
            row.Children.Add(time);
        }
        row.Children.Add(new TextBlock { Text = ev.Title, Classes = { "month-event-title" } });
        var chip = new Border { Child = row, Classes = { "month-event", ToneClass(ev.Tone) } };
        Interact(chip, ev);
        return chip;
    }

    private Border TimedChip(PickerLocale words, MonthEvent ev)
    {
        var start = ev.Time!.Value;
        var end = start + Length(ev);
        var stack = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = ev.Title, Classes = { "month-event-title", "week-event-title" } },
                new TextBlock { Text = $"{words.FormatTime(start)} – {words.FormatTime(end)}", Classes = { "week-event-time" } },
            },
        };
        var chip = new Border { Child = stack, Classes = { "month-event", "timed", ToneClass(ev.Tone) } };
        Interact(chip, ev);
        return chip;
    }

    private void Interact(Border chip, MonthEvent ev)
    {
        ToolTip.SetTip(chip, ev.Title);
        if (ev.Href is not null)
        {
            chip.Classes.Add("link");
            chip.Focusable = true;
            AutomationProperties.SetName(chip, ev.Title);
            chip.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton != MouseButton.Left) return;
                e.Handled = true;
                EventInvoked?.Invoke(this, ev);
            };
            chip.KeyDown += (_, e) =>
            {
                if (e.Key is not (Key.Enter or Key.Space)) return;
                e.Handled = true;
                EventInvoked?.Invoke(this, ev);
            };
        }
        else
        {
            chip.PointerReleased += (_, e) => e.Handled = true;
        }
    }
}

/// <summary>One day's hours in the week and day views: each event at its time, as tall as it lasts, in its lane; and the now line across.</summary>
internal sealed class DayTimeline : Panel
{
    private readonly Dictionary<Control, (double Start, double End, int Lane, int Lanes)> _slots = [];
    private Control? _line;
    private double _lineAt;

    public double HourHeight { get; init; } = 48;

    public double From { get; init; }

    public double To { get; init; } = 24 * 60;

    public void Add(Control chip, double start, double end, int lane, int lanes)
    {
        _slots[chip] = (start, end, lane, Math.Max(1, lanes));
        Children.Add(chip);
    }

    public void SetLine(Control line, double at)
    {
        if (_line != line)
        {
            if (_line is not null) Children.Remove(_line);
            _line = line;
            Children.Add(line);
        }
        _lineAt = at;
        InvalidateArrange();
    }

    private double Y(double minutes) => (Math.Clamp(minutes, From, To) - From) / 60 * HourHeight;

    protected override Size MeasureOverride(Size availableSize)
    {
        var finite = double.IsFinite(availableSize.Width);
        var width = finite ? availableSize.Width : double.PositiveInfinity;
        var wanted = 0.0;
        foreach (var child in Children)
        {
            if (child == _line)
            {
                child.Measure(new Size(width, 10));
            }
            else if (_slots.TryGetValue(child, out var s))
            {
                child.Measure(new Size(finite ? Math.Max(0, width / s.Lanes - 2) : width, Math.Max(0, Y(s.End) - Y(s.Start) - 2)));
                wanted = Math.Max(wanted, (child.DesiredSize.Width + 2) * s.Lanes);
            }
        }
        return new Size(finite ? 0 : wanted, (To - From) / 60 * HourHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            if (child == _line)
            {
                child.Arrange(new Rect(0, Y(_lineAt) - 5, finalSize.Width, 10));
                continue;
            }
            if (!_slots.TryGetValue(child, out var s)) continue;
            var top = Y(s.Start);
            var height = Math.Max(0, Y(s.End) - top);
            var width = finalSize.Width / s.Lanes;
            child.Arrange(new Rect(s.Lane * width + 1, top + 1, Math.Max(0, width - 2), Math.Max(0, height - 2)));
        }
        return finalSize;
    }
}
