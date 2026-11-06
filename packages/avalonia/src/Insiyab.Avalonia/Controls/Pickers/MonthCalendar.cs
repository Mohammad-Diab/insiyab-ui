using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
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

/// <summary>How a <see cref="MonthCalendar"/> lays out its header.</summary>
public enum MonthHeaderLayout
{
    /// <summary>Today and the work-hours switch at the start, the date between its arrows in the middle, the view switch at the end; when they do not fit on one line, the date takes its own line under the rest.</summary>
    Centered,
    /// <summary>The date at the start and every control together at the end, as the web's month header.</summary>
    TitleStart,
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

    /// <summary>Anything the page keeps with the event, such as its record, for its details card to show.</summary>
    public object? Tag { get; set; }
}

/// <summary>The month calendar plugin (the web's <c>.ins-month</c>): a month as a table of weeks starting on the locale's first day, today marked, each day showing three events and a "+n" for the rest, dots instead of names when narrow, and a header that moves a month back, on, or to today. The desktop adds a week and a day view by the hour, the work hours only unless asked, with a switch between the three.</summary>
[TemplatePart("PART_Host", typeof(Decorator))]
[PseudoClasses(":narrow", ":month", ":week", ":day")]
public class MonthCalendar : TemplatedControl, IWords
{
    void IWords.Reword() => Draw();

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

    public static readonly StyledProperty<bool> ShowViewSwitchProperty =
        AvaloniaProperty.Register<MonthCalendar, bool>(nameof(ShowViewSwitch), true);

    public static readonly StyledProperty<int> DayStartHourProperty =
        AvaloniaProperty.Register<MonthCalendar, int>(nameof(DayStartHour), 6);

    public static readonly StyledProperty<int> DayEndHourProperty =
        AvaloniaProperty.Register<MonthCalendar, int>(nameof(DayEndHour), 18);

    public static readonly StyledProperty<bool> WorkHoursOnlyProperty =
        AvaloniaProperty.Register<MonthCalendar, bool>(nameof(WorkHoursOnly), true);

    public static readonly StyledProperty<bool> ShowHoursSwitchProperty =
        AvaloniaProperty.Register<MonthCalendar, bool>(nameof(ShowHoursSwitch), true);

    public static readonly StyledProperty<double> HourHeightProperty =
        AvaloniaProperty.Register<MonthCalendar, double>(nameof(HourHeight), 48);

    public static readonly StyledProperty<MonthHeaderLayout> HeaderLayoutProperty =
        AvaloniaProperty.Register<MonthCalendar, MonthHeaderLayout>(nameof(HeaderLayout));

    public static readonly StyledProperty<IDataTemplate?> EventDetailsProperty =
        AvaloniaProperty.Register<MonthCalendar, IDataTemplate?>(nameof(EventDetails));

    // How far a turn's new days slide in from the side they come from.
    internal const double TurnSlide = 24;

    private const int Shown = 3;
    private const double Shortest = 22;
    private Decorator? _host;
    private bool _narrow;
    private bool _hold;
    private bool _syncSwitch;
    private readonly Dictionary<DateTime, Border> _cells = [];
    private readonly Dictionary<MonthEvent, Border> _chips = [];
    private StackPanel? _root;
    private MonthHead? _head;
    private MonthHeaderLayout? _headLayout;
    private PickerLocale? _headWords;
    private TextBlock? _title;
    private Button? _prev;
    private Button? _next;
    private Button? _today;
    private Segmented? _switch;
    private PopCard? _card;
    private MonthEvent? _cardEvent;
    private Control? _cardAnchor;
    private DayTimeline? _nowPanel;
    private Control? _now;
    private DateTime _drawnToday;
    private DispatcherTimer? _clock;
    private bool _stale;
    private ToggleSwitch? _hours;
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

    /// <summary>Raised when an event that can be pressed is pressed: one with an <see cref="MonthEvent.Href"/>, or any event while <see cref="EventDetails"/> is set.</summary>
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
    public bool ShowViewSwitch { get => GetValue(ShowViewSwitchProperty); set => SetValue(ShowViewSwitchProperty, value); }

    /// <summary>The hour the work hours start at, 6 unless set; kept within 0 to 23.</summary>
    public int DayStartHour { get => GetValue(DayStartHourProperty); set => SetValue(DayStartHourProperty, value); }

    /// <summary>The hour the work hours end at, 18 unless set; 24 is midnight, and it stays after the start.</summary>
    public int DayEndHour { get => GetValue(DayEndHourProperty); set => SetValue(DayEndHourProperty, value); }

    /// <summary>The week and day views show only the work hours (the default), and count the events outside them at the edges; false shows the whole day, scrolled to the work start.</summary>
    public bool WorkHoursOnly { get => GetValue(WorkHoursOnlyProperty); set => SetValue(WorkHoursOnlyProperty, value); }

    /// <summary>The work-hours toggle in the header of the week and day views.</summary>
    public bool ShowHoursSwitch { get => GetValue(ShowHoursSwitchProperty); set => SetValue(ShowHoursSwitchProperty, value); }

    /// <summary>How tall an hour is in the week and day views.</summary>
    public double HourHeight { get => GetValue(HourHeightProperty); set => SetValue(HourHeightProperty, value); }

    /// <summary>The header's order: <see cref="MonthHeaderLayout.Centered"/> (the default) or <see cref="MonthHeaderLayout.TitleStart"/>.</summary>
    public MonthHeaderLayout HeaderLayout { get => GetValue(HeaderLayoutProperty); set => SetValue(HeaderLayoutProperty, value); }

    /// <summary>What an event's details card shows, built with the <see cref="MonthEvent"/> as its data; while set, every event can be pressed and opens its card beside its chip, growing out of it.</summary>
    public IDataTemplate? EventDetails { get => GetValue(EventDetailsProperty); set => SetValue(EventDetailsProperty, value); }

    internal IReadOnlyDictionary<DateTime, Border> Cells => _cells;

    internal IReadOnlyDictionary<MonthEvent, Border> Chips => _chips;

    internal Control? NowLine => _now;

    internal IReadOnlyDictionary<(DateTime Day, bool After), Border> Bands => _bands;

    internal ScrollViewer? HoursScroller => _scroll;

    internal static Func<DateTime> Now { get; set; } = () => DateTime.Now;

    internal MonthHead? HeadRow => _head;

    internal PopCard? DetailsCard => _card;

    internal Control? Body => _root is { Children.Count: > 1 } root ? root.Children[1] : null;

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
            || change.Property == FlowDirectionProperty || change.Property == ViewProperty || change.Property == ShowViewSwitchProperty
            || change.Property == DayStartHourProperty || change.Property == DayEndHourProperty || change.Property == HourHeightProperty
            || change.Property == WorkHoursOnlyProperty || change.Property == ShowHoursSwitchProperty || change.Property == HeaderLayoutProperty
            || change.Property == EventDetailsProperty)
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
        var was = Span(Words).First;
        if (View == MonthCalendarView.Month)
        {
            var from = First(Month);
            var next = by == 0 ? First(now) : from.AddMonths(by);
            Hold(() =>
            {
                SetCurrentValue(DateProperty, by == 0 ? now : next);
                SetCurrentValue(MonthProperty, next);
            });
            Draw();
            if (next != from) Arrive(next.CompareTo(from));
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
        var turned = Span(Words).First.CompareTo(was);
        if (turned != 0) Arrive(turned);
        DateChanged?.Invoke(this, day);
    }

    // The new days slide in from the side they come from (back from the start, on from the end, mirrored in RTL); a switch of view rises in; still motion shows them at once.
    private void Arrive(int toward)
    {
        if (Body is not { } body) return;
        if (toward == 0) Entrance.Play(body, EntranceEffect.Rise);
        else Entrance.Slide(body, toward > 0 ? TurnSlide : -TurnSlide);
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
        Arrive(0);
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
        // The chips are drawn again, so a details card hanging from one closes.
        _card?.HideNow();
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
    private MonthHead Head(PickerLocale words, MonthCalendarView view)
    {
        if (_head is null || _headWords != words)
        {
            if (_head?.Parent is Panel p) p.Children.Remove(_head);
            _headWords = words;
            _headLayout = null;
            _head = new MonthHead
            {
                Classes = { "month-head" },
                Children =
                {
                    new MonthStack { Orientation = Orientation.Horizontal, Spacing = 4, Classes = { "month-nav" } },
                    new MonthStack { Orientation = Orientation.Horizontal, Classes = { "month-middle" } },
                    new MonthStack { Orientation = Orientation.Horizontal, Spacing = 4, Classes = { "month-end" } },
                },
            };
            foreach (var group in _head.Children) KeyboardNavigation.SetTabNavigation(group, KeyboardNavigationMode.Local);
            _title = new MonthText { Classes = { "month-title" } };
            _switch = new Segmented { Classes = { "month-views" } };
            _syncSwitch = true;
            _switch.ItemsSource = new[] { words.Words["month.month"], words.Words["month.week"], words.Words["month.day"] };
            _syncSwitch = false;
            AutomationProperties.SetName(_switch, words.Words["month.view"]);
            _switch.SelectionChanged += (_, _) =>
            {
                if (_syncSwitch || _switch.SelectedIndex < 0) return;
                var to = (MonthCalendarView)_switch.SelectedIndex;
                if (to != View) Dispatcher.UIThread.Post(() => SwitchTo(to));
            };
            _hours = new ToggleSwitch { Content = words.Words["month.workHours"], Classes = { "sm", "month-hours" }, Tag = "hours" };
            _hours.Click += (_, _) => SetHours(_hours.IsChecked == true, null);
            _prev = Arrow(false, () => Go(-1));
            _today = new Button { Content = words.Today, Classes = { "secondary", "sm" }, Tag = "today" };
            _today.Click += (_, _) => Go(0);
            _next = Arrow(true, () => Go(1));
        }
        if (_headLayout != HeaderLayout) Lay(HeaderLayout);

        var greg = PickerCalendar.Gregorian;
        var (first, _) = Span(words);
        _title!.Text = view switch
        {
            MonthCalendarView.Week => words.FormatRange(greg, first, first.AddDays(6)),
            MonthCalendarView.Day => words.FullDay(greg, first),
            _ => words.MonthYear(greg, First(Month)),
        };
        var say = words.Words;
        var (back, on) = view switch
        {
            MonthCalendarView.Week => (say["month.prevWeek"], say["month.nextWeek"]),
            MonthCalendarView.Day => (say["month.prevDay"], say["month.nextDay"]),
            _ => (words.PrevMonth, words.NextMonth),
        };
        AutomationProperties.SetName(_prev!, back);
        AutomationProperties.SetName(_next!, on);
        _switch!.IsVisible = ShowViewSwitch;
        var (ws, we) = WorkHours();
        _hours!.IsVisible = view != MonthCalendarView.Month && ShowHoursSwitch && Trims;
        _hours.IsChecked = WorkHoursOnly;
        var from = words.FormatTime(TimeSpan.FromHours(ws));
        var to = words.FormatTime(TimeSpan.FromHours(we));
        ToolTip.SetTip(_hours, say.Format("month.workHoursTip", from, to));
        AutomationProperties.SetName(_hours, say["month.workHours"]);
        _syncSwitch = true;
        _switch.SelectedIndex = (int)view;
        _syncSwitch = false;
        return _head;
    }

    // Puts the header's parts in their groups: centred, Today and the hours at the start, the date between its arrows in the middle, the views at the end; or the date alone at the start and the rest at the end.
    private void Lay(MonthHeaderLayout layout)
    {
        _headLayout = layout;
        var (start, middle, end) = ((Panel)_head!.Children[0], (Panel)_head.Children[1], (Panel)_head.Children[2]);
        start.Children.Clear();
        middle.Children.Clear();
        end.Children.Clear();
        if (layout == MonthHeaderLayout.TitleStart)
        {
            _hours!.Margin = new Thickness(0, 0, 8, 0);
            _switch!.Margin = new Thickness(0, 0, 8, 0);
            start.Children.Add(_title!);
            end.Children.AddRange([_hours, _switch, _prev!, _today!, _next!]);
            middle.IsVisible = false;
            return;
        }
        _hours!.Margin = new Thickness(10, 0, 0, 0);
        _switch!.Margin = default;
        start.Children.AddRange([_today!, _hours]);
        middle.Children.AddRange([_prev!, _title!, _next!]);
        end.Children.Add(_switch);
        middle.IsVisible = true;
    }

    private void SetHours(bool workOnly, double? scrollTo)
    {
        Hold(() => SetCurrentValue(WorkHoursOnlyProperty, workOnly));
        _scrollTo = scrollTo;
        Draw();
    }

    private static string Count(PickerLocale words, int n) => words.Words.Count("month.events", n, k =>
    {
        var r = k % 100;
        return k == 1 ? "موعد واحد" : k == 2 ? "موعدان" : PickerLocale.N(k) + (r is >= 3 and <= 10 ? " مواعيد" : r >= 11 ? " موعدًا" : " موعد");
    });

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
            var th = new MonthText { Text = words.ShortDay(wd), Classes = { "month-wd" } };
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
        var num = new MonthBorder { Child = new MonthText { Text = PickerLocale.N(day.Day) }, Classes = { "month-num" } };
        AutomationProperties.SetName(num, DayName(words, day));
        stack.Children.Add(num);
        if (list.Count > 0)
        {
            var events = new MonthStack { Spacing = 2, Classes = { "month-events" } };
            foreach (var ev in list.Take(list.Count > Shown ? Shown - 1 : Shown))
            {
                var chip = Chip(words, ev);
                events.Children.Add(chip);
                if (chip is Border shown) _chips.TryAdd(ev, shown);
            }
            if (list.Count > Shown)
            {
                var more = new MonthText { Text = "+" + PickerLocale.N(list.Count - Shown + 1), Classes = { "month-more" } };
                ToolTip.SetTip(more, string.Join("، ", list.Skip(Shown - 1).Select(e => e.Title)));
                events.Children.Add(more);
            }
            stack.Children.Add(events);
            var dots = new MonthStack { Orientation = Orientation.Horizontal, Spacing = 3, Classes = { "month-dots" } };
            foreach (var ev in list.Take(4)) dots.Children.Add(new Ellipse { Classes = { "month-dot", ToneClass(ev.Tone) } });
            stack.Children.Add(dots);
        }
        var cell = new MonthBorder { Child = stack, Classes = { "month-day" }, Tag = day };
        if (other) cell.Classes.Add("other");
        if (isToday) cell.Classes.Add("today");
        cell.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton != MouseButton.Left || e.Handled || Dismissing) return;
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

        var corner = new MonthBorder { Classes = { "week-corner" } };
        top.Children.Add(corner);
        var allLabel = new MonthText { Text = words.Words["month.allDay"], Classes = { "week-allday-label" } };
        Grid.SetRow(allLabel, 1);
        top.Children.Add(allLabel);

        var gutter = new MonthStack { Classes = { "week-hours" } };
        var lines = new MonthStack { IsHitTestVisible = false, Classes = { "week-lines" } };
        for (var h = startH; h < endH; h++)
        {
            gutter.Children.Add(new MonthBorder { Height = hourH, Classes = { "week-hour-cell" }, Child = new MonthText { Text = words.FormatTime(new TimeSpan(h, 0, 0)), Classes = { "week-hour" } } });
            lines.Children.Add(new MonthBorder { Height = hourH, Classes = { "week-line" } });
        }
        Grid.SetRow(gutter, 1);
        body.Children.Add(gutter);
        Grid.SetRow(lines, 1);
        Grid.SetColumn(lines, 1);
        Grid.SetColumnSpan(lines, days);
        body.Children.Add(lines);

        var from = startH * 60.0;
        var to = endH * 60.0;
        var pieces = Pieces(first, days);
        var hiddenBefore = new List<Piece>[days];
        var hiddenAfter = new List<Piece>[days];

        for (var d = 0; d < days; d++)
        {
            var day = first.AddDays(d);
            var list = byDay.TryGetValue(day, out var l) ? l : [];
            var isToday = day == today;

            var name = new MonthText { Text = words.ShortDay(day.DayOfWeek), Classes = { "week-wd" } };
            var num = new MonthBorder { Child = new MonthText { Text = PickerLocale.N(day.Day) }, Classes = { "month-num" } };
            var head = new MonthBorder { Child = new MonthStack { Children = { name, num } }, Classes = { "week-head" }, Tag = day };
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

            var allDay = new MonthStack { Spacing = 2, Classes = { "week-allday" } };
            foreach (var ev in list.Where(e => e.Time is null))
            {
                var chip = Chip(words, ev);
                allDay.Children.Add(chip);
                if (chip is Border b) _chips[ev] = b;
            }
            var allCell = new MonthBorder { Child = allDay, Classes = { "week-allday-cell" } };
            Grid.SetRow(allCell, 1);
            Grid.SetColumn(allCell, d + 1);
            top.Children.Add(allCell);

            var lane = new DayTimeline { HourHeight = hourH, From = startH * 60, To = endH * 60 };
            var timed = pieces.TryGetValue(day, out var dp) ? dp : [];
            var before = timed.Where(e => e.End <= from).ToList();
            var after = timed.Where(e => e.Start >= to).ToList();
            var inside = timed.Where(e => e.End > from && e.Start < to).ToList();
            hiddenBefore[d] = before;
            hiddenAfter[d] = after;
            var spans = inside.Select(e => Visible(e.Start, e.End, lane, hourH)).ToList();
            var lanes = Lanes(spans);
            for (var i = 0; i < inside.Count; i++)
            {
                var chip = TimedChip(words, inside[i].Event);
                if (inside[i].Start < from || inside[i].FromBefore) chip.Classes.Add("cut-start");
                if (inside[i].End > to || inside[i].IntoNext) chip.Classes.Add("cut-end");
                lane.Add(chip, spans[i].Start, spans[i].End, lanes[i].Lane, lanes[i].Lanes);
                _chips.TryAdd(inside[i].Event, chip);
            }
            if (isToday && LineAt(lane) is { } at)
            {
                _now = new Panel { Classes = { "week-now" }, IsHitTestVisible = false, ZIndex = 2, Children = { new MonthBorder { Classes = { "week-now-line" } }, new Ellipse { Classes = { "week-now-dot" } } } };
                _nowPanel = lane;
                lane.SetLine(_now, at);
            }
            var column = new MonthBorder { Child = lane, Classes = { "week-day" } };
            if (isToday) column.Classes.Add("today");
            column.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton != MouseButton.Left || e.Handled || Dismissing) return;
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
            var scroll = new ScrollViewer { Content = body, Height = Math.Clamp(we - ws, 4, 24) * hourH, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Visible, AllowAutoHide = true, Classes = { "week-scroll" } };
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
        return new MonthBorder { Child = outer, Classes = { "week-frame" } };
    }

    // The row above (row 0) or below (row 2) the hours: a label in the gutter and, in each day with hidden events, a pill counting them.
    private void AddBands(PickerLocale words, Grid body, DateTime first, bool after, int hour, List<Piece>[] lists)
    {
        var row = after ? 2 : 0;
        var kind = after ? "after" : "before";
        var at = words.FormatTime(TimeSpan.FromHours(hour));
        var label = words.Words.Format(after ? "month.after" : "month.before", at);
        var gutter = new MonthBorder { Classes = { "week-band-gutter", kind }, Child = new MonthText { Text = label, Classes = { "week-band-label" } } };
        Grid.SetRow(gutter, row);
        body.Children.Add(gutter);
        for (var d = 0; d < lists.Length; d++)
        {
            var cell = new MonthBorder { Classes = { "week-band-cell", kind } };
            if (lists[d].Count > 0) cell.Child = Band(words, first.AddDays(d), lists[d], after, hour);
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, d + 1);
            body.Children.Add(cell);
        }
    }

    private Border Band(PickerLocale words, DateTime day, List<Piece> hidden, bool after, int hour)
    {
        var ar = words.IsArabic;
        var at = words.FormatTime(TimeSpan.FromHours(hour));
        var band = new MonthBorder { Child = new MonthText { Text = "+" + PickerLocale.N(hidden.Count), Classes = { "week-band-count" } }, Classes = { "week-band", after ? "after" : "before" }, Focusable = true, Tag = day };
        AutomationProperties.SetName(band, words.Words.Format(after ? "month.eventsAfter" : "month.eventsBefore", Count(words, hidden.Count), at));
        ToolTip.SetTip(band, string.Join(ar ? "، " : ", ", hidden.Select(p => $"{words.FormatTime(p.Event.Time!.Value)} {p.Event.Title}")));
        var first = hidden.Min(p => p.Start);
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

    // A timed event as drawn on one day: its minutes there, and whether it goes on from the day before or into the next.
    private readonly record struct Piece(MonthEvent Event, double Start, double End, bool FromBefore, bool IntoNext);

    // Each shown day's timed pieces: an event past midnight goes on into the following days.
    private Dictionary<DateTime, List<Piece>> Pieces(DateTime first, int days)
    {
        var map = new Dictionary<DateTime, List<Piece>>();
        var last = first.AddDays(days);
        foreach (var ev in Events ?? [])
        {
            if (ev.Time is not { } t) continue;
            var start = t.TotalMinutes;
            var end = start + Length(ev).TotalMinutes;
            for (var k = 0; k < 32; k++)
            {
                var from = Math.Max(0, start - k * 1440);
                var to = Math.Min(1440, end - k * 1440);
                if (to <= from) break;
                var day = ev.Date.Date.AddDays(k);
                if (day >= first && day < last)
                {
                    if (!map.TryGetValue(day, out var list)) map[day] = list = [];
                    list.Add(new Piece(ev, from, to, k > 0, end - k * 1440 > 1440));
                }
            }
        }
        foreach (var list in map.Values) list.Sort((a, b) => a.Start.CompareTo(b.Start));
        return map;
    }

    /// <summary>The chip drawn for an event in the current view, to place a pop card on; null when the event is not shown.</summary>
    public Control? ChipFor(MonthEvent ev) => _chips.TryGetValue(ev, out var chip) ? chip : null;

    private static TimeSpan Length(MonthEvent ev) => ev.Duration is { } d && d > TimeSpan.Zero ? d : TimeSpan.FromHours(1);

    // An event's minutes as drawn: inside the shown hours, and never shorter than a chip that can hold its title.
    private static (double Start, double End) Visible(double start, double end, DayTimeline lane, double hourH)
    {
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
            var time = new MonthText { Text = words.FormatTime(t), Classes = { "month-time" } };
            DockPanel.SetDock(time, Dock.Left);
            row.Children.Add(time);
        }
        row.Children.Add(new MonthText { Text = ev.Title, Classes = { "month-event-title" } });
        var chip = new MonthBorder { Child = row, Classes = { "month-event", ToneClass(ev.Tone) } };
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
                new MonthText { Text = ev.Title, Classes = { "month-event-title", "week-event-title" } },
                new MonthText { Text = $"{words.FormatTime(start)} – {words.FormatTime(end)}", Classes = { "week-event-time" } },
            },
        };
        var chip = new MonthBorder { Child = stack, Classes = { "month-event", "timed", ToneClass(ev.Tone) } };
        Interact(chip, ev);
        return chip;
    }

    private void Interact(Border chip, MonthEvent ev)
    {
        ToolTip.SetTip(chip, ev.Title);
        if (ev.Href is not null || EventDetails is not null)
        {
            chip.Classes.Add("link");
            chip.Focusable = true;
            AutomationProperties.SetName(chip, ev.Title);
            chip.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton != MouseButton.Left) return;
                e.Handled = true;
                Invoke(ev, chip);
            };
            chip.KeyDown += (_, e) =>
            {
                if (e.Key is not (Key.Enter or Key.Space)) return;
                e.Handled = true;
                Invoke(ev, chip);
            };
        }
        else
        {
            chip.PointerReleased += (_, e) => e.Handled = true;
        }
    }

    // A press on an event: its card opens beside it, or closes when it was already open on it (a second press on a trigger closes its pop).
    private void Invoke(MonthEvent ev, Control chip)
    {
        EventInvoked?.Invoke(this, ev);
        if (EventDetails is null) return;
        if (_card is { IsOpen: true } card && _cardEvent == ev && _cardAnchor == chip)
        {
            if (!card.Grow.IsShrinking) card.Hide();
            return;
        }
        Open(ev, chip);
    }

    /// <summary>Opens the event's details card (<see cref="EventDetails"/>) beside its chip, growing out of it; when the event is not drawn, beside its day, or else the calendar. Nothing opens while <see cref="EventDetails"/> is unset.</summary>
    public void ShowDetails(MonthEvent ev)
    {
        if (EventDetails is null) return;
        Control anchor = ChipFor(ev) ?? (_cells.TryGetValue(ev.Date.Date, out var cell) ? cell : this);
        Open(ev, anchor);
    }

    /// <summary>Closes the details card, shrinking it back into its event.</summary>
    public void HideDetails() => _card?.Hide();

    private void Open(MonthEvent ev, Control anchor)
    {
        if (EventDetails?.Build(ev) is not { } view) return;
        var card = _card ??= MakeCard();
        card.HideNow();
        view.DataContext = ev;
        _cardEvent = ev;
        _cardAnchor = anchor;
        card.Content = view;
        card.ShowAt(anchor.IsAttachedToVisualTree() ? anchor : this);
    }

    private PopCard MakeCard()
    {
        // A press outside closes the card and still reaches what it lands on, so another event opens its own card at once.
        var card = new PopCard { OpensBeside = true, GrowsFromTarget = true, OverlayDismissEventPassThrough = true };
        card.FlyoutPresenterClasses.Add("month-details");
        // Focus goes into the card, to its first control that takes it, and Esc there closes it.
        card.Opened += (_, _) =>
        {
            if (card.Popup.Child is not Control body) return;
            body.RemoveHandler(KeyDownEvent, OnCardKey);
            body.AddHandler(KeyDownEvent, OnCardKey, RoutingStrategies.Tunnel);
            Dispatcher.UIThread.Post(() =>
            {
                if (!card.IsOpen || card.Content is not Control view) return;
                var first = view.GetSelfAndVisualDescendants().OfType<InputElement>().FirstOrDefault(c => c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled);
                first?.Focus(NavigationMethod.Tab);
            }, DispatcherPriority.Loaded);
        };
        // Closed, focus left in the card or lost goes back to the event it hung from.
        card.Closed += (_, _) =>
        {
            var anchor = _cardAnchor;
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
            var lost = focused is null || !focused.IsAttachedToVisualTree() || focused is TopLevel || (card.Popup.Child is Visual body && (focused == body || body.IsVisualAncestorOf(focused)));
            if (lost && anchor is InputElement { Focusable: true, IsEffectivelyVisible: true } back && anchor.IsAttachedToVisualTree()) back.Focus(NavigationMethod.Tab);
        };
        return card;
    }

    // A press that only closed the details card is not also a press on an empty day.
    private bool Dismissing => _card is { IsOpen: true };

    private void OnCardKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _card is not { IsOpen: true } card) return;
        e.Handled = true;
        card.Hide();
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
