using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Where a toolbar command goes when the row runs out of room.</summary>
public enum ToolbarOverflow
{
    /// <summary>In the row while it fits; moves into the «⋯» menu, from the end, when it does not.</summary>
    Auto,
    /// <summary>Always in the row, however narrow the toolbar gets.</summary>
    Never,
    /// <summary>Only in the «⋯» menu.</summary>
    Always,
}

/// <summary>A desktop command bar: a row of commands that moves what does not fit, from the end, into a «⋯» menu. One Tab stop; the arrows, Home and End move between its commands.</summary>
[TemplatePart("PART_Panel", typeof(ToolbarPanel))]
[TemplatePart("PART_More", typeof(ToolbarButton))]
public class Toolbar : TemplatedControl
{
    /// <summary>On a command in a toolbar: whether it may move into the «⋯» menu. Only buttons, split buttons and separators move; anything else, such as a search field, stays in the row.</summary>
    public static readonly AttachedProperty<ToolbarOverflow> OverflowProperty =
        AvaloniaProperty.RegisterAttached<Control, ToolbarOverflow>("Overflow", typeof(Toolbar));

    /// <summary>The «⋯» button's name and tip, «المزيد» unless set.</summary>
    public static readonly StyledProperty<string> MoreTextProperty =
        AvaloniaProperty.Register<Toolbar, string>(nameof(MoreText), "المزيد");

    internal const string Overflowed = "toolbar-overflowed";

    private ToolbarPanel? _panel;
    private ToolbarButton? _more;
    private readonly PopMenu _menu;
    private readonly AvaloniaList<Control> _entries = new() { ResetBehavior = ResetBehavior.Remove };

    public Toolbar()
    {
        // The presenter is made on the first open, before Opening, and keeps the list it found: a list of our own lets it see the entries filled then.
        _menu = new PopMenu { IsEnd = true, ItemsSource = _entries };
        Items.CollectionChanged += (_, e) =>
        {
            Hosted.Adopt(LogicalChildren, e);
            Fill();
        };
        _menu.Opening += (_, _) => FillMenu();
        AddHandler(GotFocusEvent, OnFocusIn);
    }

    /// <summary>The commands, in order from the start.</summary>
    [Content]
    public global::Avalonia.Controls.Controls Items { get; } = new();

    public string MoreText { get => GetValue(MoreTextProperty); set => SetValue(MoreTextProperty, value); }

    public static ToolbarOverflow GetOverflow(Control control) => control.GetValue(OverflowProperty);
    public static void SetOverflow(Control control, ToolbarOverflow value) => control.SetValue(OverflowProperty, value);

    /// <summary>The commands that are in the «⋯» menu now.</summary>
    public IEnumerable<Control> OverflowItems => Items.Where(c => c.Classes.Contains(Overflowed));

    internal ToolbarButton? MoreButton => _more;

    internal static bool CanMove(Control c) => c is Button or SplitButton or Separator;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_panel is not null)
            foreach (var c in Items) _panel.Children.Remove(c);
        if (_more is not null) _more.Flyout = null;
        _panel = e.NameScope.Find<ToolbarPanel>("PART_Panel");
        _more = e.NameScope.Find<ToolbarButton>("PART_More");
        if (_more is not null) _more.Flyout = _menu;
        if (_panel is not null) _panel.More = _more;
        Fill();
    }

    private void Fill()
    {
        if (_panel is null) return;
        foreach (var c in _panel.Children.Where(c => c != _more).ToList())
        {
            _panel.Children.Remove(c);
            c.Classes.Remove(Overflowed);
        }
        var at = 0;
        foreach (var c in Items) _panel.Children.Insert(at++, c);
    }

    private void FillMenu()
    {
        _entries.Clear();
        _entries.AddRange(MenuEntries());
    }

    /// <summary>The «⋯» menu's entries for what has moved: labels shown, a toggle as a check item, a dropdown or split button as a submenu.</summary>
    internal List<Control> MenuEntries()
    {
        var list = new List<Control>();
        foreach (var c in OverflowItems)
        {
            if (c is Separator)
            {
                if (list.Count > 0 && list[^1] is not Separator) list.Add(new Separator());
                continue;
            }
            if (Entry(c) is { } entry) list.Add(entry);
        }
        while (list.Count > 0 && list[^1] is Separator) list.RemoveAt(list.Count - 1);
        return list;
    }

    private MenuItem? Entry(Control c)
    {
        var (label, icon, shortcut) = Describe(c);
        var item = Item(label, icon, shortcut, c.IsEffectivelyEnabled);
        switch (c)
        {
            case SplitButton split:
                var main = Item(label, icon, shortcut, true);
                main.Click += (_, _) => Invoke(split);
                item.Items.Add(main);
                if (split.Flyout is MenuFlyout splitMenu && splitMenu.Items.Count > 0)
                {
                    item.Items.Add(new Separator());
                    Clone(splitMenu.Items, item.Items);
                }
                break;
            case Button { Flyout: MenuFlyout menu }:
                Clone(menu.Items, item.Items);
                break;
            case Button { Flyout: { } flyout }:
                item.Click += (_, _) => Dispatcher.UIThread.Post(() => { if (_more is not null) flyout.ShowAt(_more); });
                break;
            case ToggleButton toggle:
                item.ToggleType = MenuItemToggleType.CheckBox;
                item.IsChecked = toggle.IsChecked == true;
                item.Click += (_, _) => Invoke(toggle);
                break;
            case Button button:
                item.Click += (_, _) => Invoke(button);
                break;
            default:
                return null;
        }
        return item;
    }

    private static MenuItem Item(string label, Geometry? icon, string? shortcut, bool enabled)
    {
        var item = new MenuItem { Header = label, IsEnabled = enabled };
        if (icon is not null) item.Icon = new Icon { Data = icon };
        if (Gesture(shortcut) is { } gesture) item.InputGesture = gesture;
        return item;
    }

    private static KeyGesture? Gesture(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try { return KeyGesture.Parse(text); }
        catch (ArgumentException) { return null; }
        catch (FormatException) { return null; }
    }

    // A menu's items are its own children, so the «⋯» submenu gets copies that pass the click back to the original.
    private static void Clone(IEnumerable<object?> source, ItemCollection into)
    {
        foreach (var o in source)
        {
            if (o is Separator) { into.Add(new Separator()); continue; }
            if (o is not MenuItem m) continue;
            var copy = new MenuItem
            {
                Header = m.Header as string ?? AutomationProperties.GetName(m),
                InputGesture = m.InputGesture,
                IsEnabled = m.IsEffectivelyEnabled,
                ToggleType = m.ToggleType,
                IsChecked = m.IsChecked,
            };
            copy.Classes.AddRange(m.Classes.Where(x => !x.StartsWith(':')));
            if (m.Icon is Icon icon) copy.Icon = new Icon { Data = icon.Data };
            if (m.Items.Count > 0) Clone(m.Items, copy.Items);
            else copy.Click += (_, _) => Click(m);
            into.Add(copy);
        }
    }

    private static void Click(MenuItem m)
    {
        if (m.ToggleType == MenuItemToggleType.CheckBox) m.SetCurrentValue(MenuItem.IsCheckedProperty, !m.IsChecked);
        else if (m.ToggleType == MenuItemToggleType.Radio) m.SetCurrentValue(MenuItem.IsCheckedProperty, true);
        var args = new RoutedEventArgs(MenuItem.ClickEvent);
        m.RaiseEvent(args);
        if (!args.Handled && m.Command is { } command && command.CanExecute(m.CommandParameter)) command.Execute(m.CommandParameter);
    }

    /// <summary>Clicks a command as the pointer would: its Click, its Command, and a toggle's change.</summary>
    internal static void Invoke(Control c)
    {
        if (!c.IsEffectivelyEnabled) return;
        if (c is ToolbarSplitButton own) { own.ClickPrimary(); return; }
        var peer = ControlAutomationPeer.CreatePeerForElement(c);
        if (c is ToggleButton && peer.GetProvider<IToggleProvider>() is { } toggle) toggle.Toggle();
        else peer.GetProvider<IInvokeProvider>()?.Invoke();
    }

    /// <summary>A command's name, icon and shortcut text, as its tip and its «⋯» entry show them.</summary>
    internal static (string Label, Geometry? Icon, string? Shortcut) Describe(Control c)
    {
        if (c is IToolbarCommand cmd) return (cmd.Label ?? NameOf(c), cmd.Icon, cmd.ShortcutText);
        var hot = c switch { Button b => b.HotKey, SplitButton s => s.HotKey, _ => null };
        return (NameOf(c), null, hot?.ToString());
    }

    private static string NameOf(Control c) =>
        AutomationProperties.GetName(c) is { Length: > 0 } name ? name
        : (c as ContentControl)?.Content as string ?? ToolTip.GetTip(c) as string ?? "";

    /// <summary>The commands the arrows move between: shown in the row, enabled and focusable, then «⋯» while it shows.</summary>
    internal List<Control> Stops() =>
        _panel is null ? [] : _panel.Children.Where(c => c.IsVisible && c.IsEffectivelyEnabled && c.Focusable && c is not Separator).ToList();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || e.Source is TextBox) return;
        if (e.Source is not Visual source || !this.IsVisualAncestorOf(source)) return;
        if (e.Key is not (Key.Left or Key.Right or Key.Home or Key.End)) return;
        var stops = Stops();
        if (stops.Count == 0) return;
        var at = stops.FindIndex(s => s == source || s.IsVisualAncestorOf(source));
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var to = e.Key switch
        {
            Key.Home => 0,
            Key.End => stops.Count - 1,
            Key.Left => at < 0 ? 0 : at + (rtl ? 1 : -1),
            _ => at < 0 ? 0 : at + (rtl ? -1 : 1),
        };
        to = (to % stops.Count + stops.Count) % stops.Count;
        stops[to].Focus(NavigationMethod.Directional);
        Remember(stops[to]);
        e.Handled = true;
    }

    // Tab comes back to the command that last had the focus.
    private void OnFocusIn(object? sender, FocusChangedEventArgs e)
    {
        if (_panel is null || e.Source is not Visual source) return;
        foreach (var c in _panel.Children)
            if (c == source || c.IsVisualAncestorOf(source)) { Remember(c); return; }
    }

    private void Remember(Control c)
    {
        if (_panel is not null) KeyboardNavigation.SetTabOnceActiveElement(_panel, c);
    }
}

/// <summary>The toolbar's row: the commands from the start, «⋯» at the end, and what does not fit moved into «⋯» from the end. Part of the Toolbar template.</summary>
public class ToolbarPanel : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ToolbarPanel, double>(nameof(Spacing), 2);

    private readonly Dictionary<Control, double> _widths = [];
    private double _moreWidth = 30;

    static ToolbarPanel()
    {
        AffectsMeasure<ToolbarPanel>(SpacingProperty);
        AffectsParentMeasure<ToolbarPanel>(Toolbar.OverflowProperty);
    }

    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    internal Control? More { get; set; }

    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Hosted.Show(this, VisualChildren, LogicalChildren);
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var free = new Size(double.PositiveInfinity, availableSize.Height);
        var gap = Spacing;
        var row = new List<(Control C, double W, bool Movable)>();
        var moved = new HashSet<Control>();
        foreach (var c in Children)
        {
            if (c == More) continue;
            var mode = Toolbar.GetOverflow(c);
            var movable = Toolbar.CanMove(c);
            if (movable && mode == ToolbarOverflow.Always) { moved.Add(c); continue; }
            // Hidden by the app, not by the toolbar: neither in the row nor in the menu.
            if (!c.IsVisible && !c.Classes.Contains(Toolbar.Overflowed)) continue;
            if (c.IsVisible)
            {
                c.Measure(free);
                _widths[c] = c.DesiredSize.Width;
            }
            row.Add((c, _widths.GetValueOrDefault(c), movable && mode == ToolbarOverflow.Auto));
        }

        double Sum() => row.Sum(x => x.W) + gap * Math.Max(0, row.Count - 1);
        if (Sum() > availableSize.Width || moved.Any(c => c is not Separator))
        {
            var room = availableSize.Width - _moreWidth - gap;
            for (var i = row.Count - 1; i >= 0 && Sum() > room; i--)
                if (row[i].Movable) { moved.Add(row[i].C); row.RemoveAt(i); }
        }
        // A separator never starts or ends the row, nor follows another.
        for (var i = row.Count - 1; i >= 0; i--)
            if (row[i].C is Separator && (i == 0 || i == row.Count - 1 || row[i - 1].C is Separator)) { moved.Add(row[i].C); row.RemoveAt(i); }

        foreach (var c in Children)
            if (c != More) c.Classes.Set(Toolbar.Overflowed, moved.Contains(c));

        var width = 0.0;
        var height = 0.0;
        var count = 0;
        foreach (var (c, _, _) in row)
        {
            if (!c.IsVisible) continue;
            c.Measure(free);
            _widths[c] = c.DesiredSize.Width;
            width += c.DesiredSize.Width;
            height = Math.Max(height, c.DesiredSize.Height);
            count++;
        }
        if (More is not null)
        {
            var shown = moved.Any(c => c is not Separator);
            More.SetCurrentValue(IsVisibleProperty, shown);
            if (shown)
            {
                More.Measure(free);
                _moreWidth = More.DesiredSize.Width;
                width += _moreWidth;
                height = Math.Max(height, More.DesiredSize.Height);
                count++;
            }
        }
        foreach (var gone in _widths.Keys.Where(k => !Children.Contains(k)).ToList()) _widths.Remove(gone);
        return new Size(width + gap * Math.Max(0, count - 1), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        foreach (var c in Children)
        {
            if (c == More || !c.IsVisible) continue;
            c.Arrange(new Rect(x, 0, c.DesiredSize.Width, finalSize.Height));
            x += c.DesiredSize.Width + Spacing;
        }
        if (More is { IsVisible: true } more)
        {
            var w = more.DesiredSize.Width;
            more.Arrange(new Rect(Math.Max(x, finalSize.Width - w), 0, w, finalSize.Height));
        }
        return finalSize;
    }
}
