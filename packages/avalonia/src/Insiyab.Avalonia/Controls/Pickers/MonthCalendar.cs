using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

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

/// <summary>One event in a <see cref="MonthCalendar"/>: its day, its words, and optionally its time, tone and link.</summary>
public sealed class MonthEvent
{
    public DateTime Date { get; init; }

    public string Title { get; init; } = string.Empty;

    /// <summary>Written before the title, and the order of the day's events.</summary>
    public TimeSpan? Time { get; init; }

    public MonthEventTone Tone { get; init; }

    /// <summary>A link: the event can be pressed, and fills with its tone under the pointer.</summary>
    public string? Href { get; init; }
}

/// <summary>The month calendar plugin (the web's <c>.ins-month</c>): a month as a table of weeks starting on the locale's first day, today marked, each day showing three events and a "+n" for the rest, dots instead of names when narrow, and a header that moves a month back, on, or to today.</summary>
[TemplatePart("PART_Host", typeof(Decorator))]
[PseudoClasses(":narrow")]
public class MonthCalendar : TemplatedControl
{
    public static readonly StyledProperty<DateTime> MonthProperty =
        AvaloniaProperty.Register<MonthCalendar, DateTime>(nameof(Month), new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));

    public static readonly StyledProperty<IList<MonthEvent>?> EventsProperty =
        AvaloniaProperty.Register<MonthCalendar, IList<MonthEvent>?>(nameof(Events));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<MonthCalendar, string?>(nameof(Locale));

    private const int Shown = 3;
    private Decorator? _host;
    private bool _narrow;
    private readonly Dictionary<DateTime, Border> _cells = [];

    /// <summary>Raised when the header moves to another month (the web's <c>ins:month</c>), with its first day.</summary>
    public event EventHandler<DateTime>? MonthChanged;

    /// <summary>Raised when a day is pressed away from its events (<c>ins:month-day</c>).</summary>
    public event EventHandler<DateTime>? DayInvoked;

    /// <summary>Raised when a linked event is pressed.</summary>
    public event EventHandler<MonthEvent>? EventInvoked;

    /// <summary>The month shown; any day in it will do.</summary>
    public DateTime Month { get => GetValue(MonthProperty); set => SetValue(MonthProperty, value); }

    public IList<MonthEvent>? Events { get => GetValue(EventsProperty); set => SetValue(EventsProperty, value); }

    /// <summary>"ar" or "en"; unset, it follows the direction. It decides the names and the week's first day.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    internal IReadOnlyDictionary<DateTime, Border> Cells => _cells;

    private PickerLocale Words => PickerLocale.For(Locale, FlowDirection);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host = e.NameScope.Find<Decorator>("PART_Host");
        Draw();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EventsProperty)
        {
            if (change.OldValue is INotifyCollectionChanged o) o.CollectionChanged -= OnEventsChanged;
            if (change.NewValue is INotifyCollectionChanged n) n.CollectionChanged += OnEventsChanged;
        }
        if (change.Property == MonthProperty || change.Property == EventsProperty || change.Property == LocaleProperty || change.Property == FlowDirectionProperty)
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

    private void OnEventsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Draw();

    // PageUp and PageDown anywhere inside it turn the month; the header's button that held focus gets it back.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.PageUp or Key.PageDown)) return;
        e.Handled = true;
        var held = (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Button)?.Tag as string;
        Go(e.Key == Key.PageUp ? -1 : 1);
        if (held is not null) _host?.GetLogicalDescendantsOfButtons().FirstOrDefault(b => b.Tag as string == held)?.Focus(NavigationMethod.Directional);
    }

    /// <summary>Moves a month back (-1), on (1), or to today's month (0), and says so with <see cref="MonthChanged"/>.</summary>
    public void Go(int by)
    {
        var m = new DateTime(Month.Year, Month.Month, 1);
        var now = DateCalendar.Today();
        var next = by == 0 ? new DateTime(now.Year, now.Month, 1) : m.AddMonths(by);
        SetCurrentValue(MonthProperty, next);
        MonthChanged?.Invoke(this, next);
    }

    private void Draw()
    {
        if (_host is null) return;
        _cells.Clear();
        var words = Words;
        var month = new DateTime(Month.Year, Month.Month, 1);
        var greg = PickerCalendar.Gregorian;
        var byDay = (Events ?? []).GroupBy(e => e.Date.Date).ToDictionary(g => g.Key, g => g.OrderBy(e => e.Time?.Ticks ?? -1).ToList());

        var root = new StackPanel();
        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Classes = { "month-head" } };
        var title = new TextBlock { Text = words.MonthYear(greg, month), Classes = { "month-title" } };
        head.Children.Add(title);
        var nav = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Classes = { "month-nav" } };
        nav.Children.Add(Arrow(words.PrevMonth, false, () => Go(-1)));
        var today = new Button { Content = words.Today, Classes = { "secondary", "sm" }, Tag = "today" };
        today.Click += (_, _) => Go(0);
        nav.Children.Add(today);
        nav.Children.Add(Arrow(words.NextMonth, true, () => Go(1)));
        Grid.SetColumn(nav, 1);
        head.Children.Add(nav);
        root.Children.Add(head);

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
        root.Children.Add(grid);
        _host.Child = root;
    }

    private Button Arrow(string name, bool next, Action go)
    {
        var path = new global::Avalonia.Controls.Shapes.Path { Data = (Geometry?)this.FindResource(next ? "InsMonthArrowIcon" : "InsMonthArrowBackIcon"), Classes = { "month-arrow" } };
        var b = new Button { Content = new Viewbox { Width = 16, Height = 16, Child = new Canvas { Width = 24, Height = 24, Children = { path } } }, Classes = { "bare", "icon", "sm", "month-go", next ? "next" : "prev" }, Tag = next ? "next" : "prev" };
        AutomationProperties.SetName(b, name);
        b.Click += (_, _) => go();
        return b;
    }

    private Border BuildDay(PickerLocale words, DateTime day, List<MonthEvent> list, bool other, bool isToday)
    {
        var stack = new StackPanel();
        var num = new Border { Child = new TextBlock { Text = PickerLocale.N(day.Day) }, Classes = { "month-num" } };
        AutomationProperties.SetName(num, words.IsArabic ? $"{PickerLocale.N(day.Day)} {words.MonthName(PickerCalendar.Gregorian, day.Month - 1)}" : $"{words.MonthName(PickerCalendar.Gregorian, day.Month - 1)} {PickerLocale.N(day.Day)}");
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
        return chip;
    }
}
