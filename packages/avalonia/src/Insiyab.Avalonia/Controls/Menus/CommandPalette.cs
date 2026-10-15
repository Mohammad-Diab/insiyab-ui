using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>One thing the palette can reach: a page or a command (<c>Insiyab.palette.add()</c>'s item).</summary>
public class PaletteItem
{
    public string Label { get; set; } = string.Empty;

    /// <summary>The group it is listed under, and named after while searching.</summary>
    public string? Group { get; set; }

    /// <summary>Words the search reads and the list does not show (<c>data-keywords</c>).</summary>
    public string? Keywords { get; set; }

    /// <summary>A shortcut shown at the end of its row.</summary>
    public string? Hint { get; set; }

    /// <summary>A 24×24 line icon, drawn like the library's own.</summary>
    public Geometry? Icon { get; set; }

    /// <summary>What it is remembered by in the recent list; its label when unset (<c>data-ins-palette-key</c>).</summary>
    public string? Key { get; set; }

    /// <summary>The page the person is on: set in bold (<c>aria-current="page"</c>).</summary>
    public bool IsCurrent { get; set; }

    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }

    /// <summary>Run when it is chosen, after <see cref="Command"/>.</summary>
    public Action? Run { get; set; }

    internal string RecentKey => string.IsNullOrEmpty(Key) ? "label:" + Label : Key!;
    internal int Order { get; set; }
}

/// <summary>A choice in the palette, before the item runs; set <c>Cancel</c> to stop it (<c>ins:palette</c>).</summary>
public class PaletteChoosingEventArgs(RoutedEvent routedEvent, PaletteItem item) : RoutedEventArgs(routedEvent)
{
    public PaletteItem Item { get; } = item;
    public string Label => Item.Label;
    public string Key => Item.RecentKey;
    public bool Cancel { get; set; }
}

/// <summary>The command palette plugin: one search for every page and command, in a dialog high on the window. Matching is Arabic-aware and ranked by where the words fall; with nothing typed it shows the recent choices, then every group. Open it with <see cref="Open"/>, or <see cref="AttachTo"/> a window for Ctrl/⌘+K and «/».</summary>
[TemplatePart("PART_Backdrop", typeof(Control))]
[TemplatePart("PART_Dialog", typeof(Control))]
[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_List", typeof(ItemsControl))]
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[TemplatePart("PART_Empty", typeof(Control))]
[PseudoClasses(":searching")]
public class CommandPalette : TemplatedControl
{
    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<CommandPalette, string>(nameof(PlaceholderText), "ابحث عن صفحة أو أمر…");

    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<CommandPalette, string>(nameof(EmptyText), "لا نتائج");

    public static readonly StyledProperty<string> RecentTextProperty =
        AvaloniaProperty.Register<CommandPalette, string>(nameof(RecentText), "الأخيرة");

    public static readonly StyledProperty<string> MoveTextProperty =
        AvaloniaProperty.Register<CommandPalette, string>(nameof(MoveText), "للتنقل");

    public static readonly StyledProperty<string> ChooseTextProperty =
        AvaloniaProperty.Register<CommandPalette, string>(nameof(ChooseText), "للاختيار");

    public static readonly StyledProperty<string> CloseTextProperty =
        AvaloniaProperty.Register<CommandPalette, string>(nameof(CloseText), "للإغلاق");

    public static readonly StyledProperty<int> RecentLimitProperty =
        AvaloniaProperty.Register<CommandPalette, int>(nameof(RecentLimit), 5);

    public static readonly DirectProperty<CommandPalette, bool> IsOpenProperty =
        AvaloniaProperty.RegisterDirect<CommandPalette, bool>(nameof(IsOpen), o => o.IsOpen);

    public static readonly RoutedEvent<PaletteChoosingEventArgs> ChoosingEvent =
        RoutedEvent.Register<CommandPalette, PaletteChoosingEventArgs>(nameof(Choosing), RoutingStrategies.Bubble);

    private TextBox? _input;
    private ItemsControl? _list;
    private ScrollViewer? _scroll;
    private Control? _empty;
    private Control? _backdrop;
    private Control? _dialog;
    private OverlayLayer? _layer;
    private IInputElement? _returnFocus;
    private bool _open;
    private int _order;
    private readonly Dictionary<PaletteItem, Border> _rows = [];
    private readonly List<PaletteItem> _shown = [];
    private List<object> _display = [];
    private PaletteItem? _active;

    public CommandPalette()
    {
        Items.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (PaletteItem item in e.NewItems) item.Order = ++_order;
            if (IsOpen) Render();
        };
    }

    [Content]
    public AvaloniaList<PaletteItem> Items { get; } = [];

    /// <summary>The keys of the recent choices, newest first; persist and restore it to keep them across runs.</summary>
    public AvaloniaList<string> RecentKeys { get; } = [];

    /// <summary>The shortcuts that open and close it once it is attached; Ctrl+K and ⌘+K unless changed.</summary>
    public AvaloniaList<KeyGesture> Gestures { get; } = [new KeyGesture(Key.K, KeyModifiers.Control), new KeyGesture(Key.K, KeyModifiers.Meta)];

    /// <summary>Whether «/» opens it too, when the focus is not in a text field.</summary>
    public bool SlashOpens { get; set; } = true;

    public string PlaceholderText { get => GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }
    public string EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public string RecentText { get => GetValue(RecentTextProperty); set => SetValue(RecentTextProperty, value); }
    public string MoveText { get => GetValue(MoveTextProperty); set => SetValue(MoveTextProperty, value); }
    public string ChooseText { get => GetValue(ChooseTextProperty); set => SetValue(ChooseTextProperty, value); }
    public string CloseText { get => GetValue(CloseTextProperty); set => SetValue(CloseTextProperty, value); }

    /// <summary>How many recent choices to show; 0 hides them (<c>data-ins-palette-recent</c>).</summary>
    public int RecentLimit { get => GetValue(RecentLimitProperty); set => SetValue(RecentLimitProperty, value); }

    public bool IsOpen { get => _open; private set => SetAndRaise(IsOpenProperty, ref _open, value); }

    public event EventHandler<PaletteChoosingEventArgs>? Choosing { add => AddHandler(ChoosingEvent, value); remove => RemoveHandler(ChoosingEvent, value); }

    /// <summary>The items listed now, in order: recent and grouped with nothing typed, best first while searching.</summary>
    public IReadOnlyList<PaletteItem> Shown => _shown;

    /// <summary>The highlighted item, the one Enter would choose.</summary>
    public PaletteItem? Active => _active;

    /// <summary>The search box.</summary>
    public TextBox? InputBox => _input;

    /// <summary>Adds items and returns them; remove one with <c>Items.Remove</c>.</summary>
    public IReadOnlyList<PaletteItem> Add(params PaletteItem[] items)
    {
        Items.AddRange(items);
        return items;
    }

    /// <summary>Opens it over the window <paramref name="host"/> is in, fresh: the field empty and the first item ready for Enter.</summary>
    public void Open(Visual host)
    {
        if (IsOpen) return;
        var top = TopLevel.GetTopLevel(host);
        var layer = top is null ? null : OverlayLayer.GetOverlayLayer(top);
        if (layer is null) return;
        _returnFocus = top!.FocusManager?.GetFocusedElement();
        if (_layer != layer)
        {
            if (_layer is not null)
            {
                _layer.SizeChanged -= OnLayerSized;
                _layer.Children.Remove(this);
            }
            _layer = layer;
            layer.SizeChanged += OnLayerSized;
            layer.Children.Add(this);
        }
        Width = layer.Bounds.Width;
        Height = layer.Bounds.Height;
        IsVisible = true;
        IsOpen = true;
        if (_input is not null) _input.Text = string.Empty;
        Render();
        PlayEntrance();
        Dispatcher.UIThread.Post(() => _input?.Focus(), DispatcherPriority.Loaded);
    }

    // Hidden rather than taken out of the window, so the next opening is instant and nothing below has to re-resolve its resources.
    public void Close()
    {
        if (!IsOpen) return;
        IsVisible = false;
        IsOpen = false;
        _returnFocus?.Focus();
        _returnFocus = null;
    }

    private void PlayEntrance()
    {
        if (_dialog is not null) PopMotion.Play(_dialog);
    }

    public void Toggle(Visual host)
    {
        if (IsOpen) Close();
        else Open(host);
    }

    private void OnLayerSized(object? sender, SizeChangedEventArgs e)
    {
        Width = e.NewSize.Width;
        Height = e.NewSize.Height;
    }

    /// <summary>Listens on <paramref name="top"/> for <see cref="Gestures"/>, and «/» outside a text field; dispose the result to stop.</summary>
    public IDisposable AttachTo(TopLevel top)
    {
        void OnKey(object? sender, KeyEventArgs e)
        {
            if (e.Handled || !IsShortcut(e, top)) return;
            e.Handled = true;
            Toggle(top);
        }
        top.AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
        return new Detach(() => top.RemoveHandler(KeyDownEvent, OnKey));
    }

    // Matched by the key and by where it sits, so Ctrl+K still works with an Arabic layout on.
    internal bool IsShortcut(KeyEventArgs e, TopLevel top)
    {
        foreach (var g in Gestures)
        {
            if (e.KeyModifiers != g.KeyModifiers) continue;
            if (e.Key == g.Key) return true;
            if (g.Key is >= Key.A and <= Key.Z && e.PhysicalKey == PhysicalKey.A + (g.Key - Key.A)) return true;
        }
        if (!SlashOpens || e.KeyModifiers != KeyModifiers.None) return false;
        if (e.Key != Key.Oem2 && e.Key != Key.Divide && e.PhysicalKey != PhysicalKey.Slash) return false;
        return top.FocusManager?.GetFocusedElement() is not TextBox;
    }

    private sealed class Detach(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_input is not null)
        {
            _input.RemoveHandler(KeyDownEvent, OnInputKeyDown);
            _input.TextChanged -= OnInputTextChanged;
        }
        if (_backdrop is not null) _backdrop.PointerPressed -= OnBackdropPressed;
        if (_list is not null) _list.ContainerClearing -= OnContainerClearing;
        _input = e.NameScope.Find<TextBox>("PART_Input");
        _list = e.NameScope.Find<ItemsControl>("PART_List");
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _empty = e.NameScope.Find<Control>("PART_Empty");
        _backdrop = e.NameScope.Find<Control>("PART_Backdrop");
        _dialog = e.NameScope.Find<Control>("PART_Dialog");
        if (_input is not null)
        {
            _input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
            _input.TextChanged += OnInputTextChanged;
        }
        if (_backdrop is not null) _backdrop.PointerPressed += OnBackdropPressed;
        if (_list is not null)
        {
            _list.ItemTemplate = new FuncDataTemplate<object>((entry, _) => entry switch { PaletteItem item => BuildRow(item), Head head => new PaletteText { Text = head.Name, Classes = { "palette-head" } }, _ => null }, supportsRecycling: false);
            _list.ContainerClearing += OnContainerClearing;
        }
        _rows.Clear();
        Render();
    }

    private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (e.Container is ContentPresenter { Child: Border { Tag: PaletteItem item } row } && _rows.TryGetValue(item, out var kept) && kept == row) _rows.Remove(item);
    }

    private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        Close();
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e) => Render();

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
            case Key.Up:
                e.Handled = true;
                if (_shown.Count == 0) return;
                var at = _active is null ? -1 : _shown.IndexOf(_active);
                var next = e.Key == Key.Down ? (at + 1) % _shown.Count : (at <= 0 ? _shown.Count - 1 : at - 1);
                Activate(_shown[next], true);
                return;
            case Key.Enter:
                e.Handled = true;
                if (_active is not null) Choose(_active);
                return;
            case Key.Escape:
                e.Handled = true;
                Close();
                return;
        }
    }

    /// <summary>Chooses an item as Enter or a click does: remembered, the palette closed, <see cref="Choosing"/> raised, then the item run.</summary>
    public void Choose(PaletteItem item)
    {
        Remember(item);
        Close();
        var args = new PaletteChoosingEventArgs(ChoosingEvent, item);
        RaiseEvent(args);
        if (args.Cancel) return;
        if (item.Command is { } command && command.CanExecute(item.CommandParameter)) command.Execute(item.CommandParameter);
        item.Run?.Invoke();
    }

    private void Remember(PaletteItem item)
    {
        if (RecentLimit <= 0) return;
        var key = item.RecentKey;
        RecentKeys.Remove(key);
        RecentKeys.Insert(0, key);
        while (RecentKeys.Count > RecentLimit) RecentKeys.RemoveAt(RecentKeys.Count - 1);
    }

    private Border BuildRow(PaletteItem item)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto") };
        if (item.Icon is not null)
            grid.Children.Add(new Icon { Data = item.Icon, Size = 18, Margin = new Thickness(0, 0, 10, 0), Classes = { "palette-ico" } });
        var label = new PaletteText { Text = item.Label, Classes = { "palette-label" }, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        if (item.IsCurrent) label.FontWeight = FontWeight.ExtraBold;
        Grid.SetColumn(label, 1);
        grid.Children.Add(label);
        if (!string.IsNullOrEmpty(item.Hint))
        {
            var kbd = new Border { Classes = { "kbd" }, Margin = new Thickness(10, 0, 0, 0), Child = new TextBlock { Text = item.Hint } };
            Grid.SetColumn(kbd, 2);
            grid.Children.Add(kbd);
        }
        if (!string.IsNullOrEmpty(item.Group))
        {
            var where = new PaletteText { Text = item.Group, Classes = { "palette-where" }, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(where, 3);
            grid.Children.Add(where);
        }
        var row = new PaletteBorder { Classes = { "palette-item" }, Child = grid, Focusable = false, Tag = item };
        if (_active == item) row.Classes.Add("active");
        row.PointerMoved += (_, _) => { if (_active != item) Activate(item, false); };
        row.PointerPressed += (_, e) => e.Handled = true;
        row.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            Choose(item);
        };
        _rows[item] = row;
        return row;
    }

    private void Render()
    {
        if (_list is null) return;
        _shown.Clear();
        _rows.Clear();
        var display = new List<object>();
        var terms = ArabicSearch.Terms(_input?.Text);
        var ordered = Items.OrderBy(i => i.Order).ToList();
        PseudoClasses.Set(":searching", terms.Length > 0);

        if (terms.Length > 0)
        {
            var hits = ordered
                .Select((item, i) => (item, i, s: ArabicSearch.Score(item.Label, item.Keywords, item.Group, terms)))
                .Where(h => h.s > 0)
                .OrderByDescending(h => h.s).ThenBy(h => h.i);
            foreach (var (item, _, _) in hits)
            {
                _shown.Add(item);
                display.Add(item);
            }
        }
        else
        {
            var used = new HashSet<PaletteItem>();
            var recent = new List<PaletteItem>();
            if (RecentLimit > 0)
                foreach (var key in RecentKeys.Take(RecentLimit))
                {
                    var hit = ordered.FirstOrDefault(i => !used.Contains(i) && i.RecentKey == key);
                    if (hit is null) continue;
                    recent.Add(hit);
                    used.Add(hit);
                }
            var groups = new List<(string name, List<PaletteItem> items)>();
            if (recent.Count > 0) groups.Add((RecentText, recent));
            foreach (var item in ordered)
            {
                if (used.Contains(item)) continue;
                var name = item.Group ?? string.Empty;
                var g = groups.FindIndex(x => x.name == name && !ReferenceEquals(x.items, recent));
                if (g < 0) groups.Add((name, [item]));
                else groups[g].items.Add(item);
            }
            foreach (var (name, items) in groups)
            {
                if (name.Length > 0) display.Add(new Head(name));
                foreach (var item in items)
                {
                    _shown.Add(item);
                    display.Add(item);
                }
            }
        }
        _display = display;
        _list.ItemsSource = display;
        if (_empty is not null) _empty.IsVisible = _shown.Count == 0;
        _scroll?.ScrollToHome();
        Activate(_shown.Count > 0 ? _shown[0] : null, false);
    }

    private void Activate(PaletteItem? item, bool scroll)
    {
        if (_active is not null && _rows.TryGetValue(_active, out var prev)) prev.Classes.Remove("active");
        _active = item;
        if (item is null) return;
        if (_rows.TryGetValue(item, out var row)) row.Classes.Add("active");
        if (!scroll || _list is null) return;
        _list.ScrollIntoView(_display.IndexOf(item));
        if (_rows.TryGetValue(item, out row)) row.Classes.Add("active");
    }

    private sealed record Head(string Name);
}
