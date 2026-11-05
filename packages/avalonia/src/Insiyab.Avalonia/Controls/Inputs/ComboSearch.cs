using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The settings of a searchable select, a <c>ComboBox</c> with the class <c>searchable</c>: open, it shows a search field over its list, which keeps the items that hold every typed word anywhere in their text or keywords, Arabic folded.</summary>
public static class ComboSearch
{
    /// <summary>Extra words each item is also found by, such as its name in another language or a code: a binding to the item, text or a list of words.</summary>
    public static readonly AttachedProperty<BindingBase?> KeywordsProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, BindingBase?>("Keywords", typeof(ComboSearch));

    /// <summary>The heading each item is listed under: a binding to the item. Headings show when the items fall in more than one group.</summary>
    public static readonly AttachedProperty<BindingBase?> GroupProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, BindingBase?>("Group", typeof(ComboSearch));

    /// <summary>The search field's placeholder; «ابحث» or "Search" in the app's language when unset.</summary>
    public static readonly AttachedProperty<string?> PlaceholderProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, string?>("Placeholder", typeof(ComboSearch));

    /// <summary>The line shown when nothing matches; «لا نتائج» or "No results" in the app's language when unset.</summary>
    public static readonly AttachedProperty<string?> EmptyTextProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, string?>("EmptyText", typeof(ComboSearch));

    [AssignBinding]
    public static BindingBase? GetKeywords(ComboBox combo) => combo.GetValue(KeywordsProperty);

    public static void SetKeywords(ComboBox combo, BindingBase? value) => combo.SetValue(KeywordsProperty, value);

    [AssignBinding]
    public static BindingBase? GetGroup(ComboBox combo) => combo.GetValue(GroupProperty);

    public static void SetGroup(ComboBox combo, BindingBase? value) => combo.SetValue(GroupProperty, value);

    public static string? GetPlaceholder(ComboBox combo) => combo.GetValue(PlaceholderProperty);

    public static void SetPlaceholder(ComboBox combo, string? value) => combo.SetValue(PlaceholderProperty, value);

    public static string? GetEmptyText(ComboBox combo) => combo.GetValue(EmptyTextProperty);

    public static void SetEmptyText(ComboBox combo, string? value) => combo.SetValue(EmptyTextProperty, value);
}

/// <summary>Set by the ComboBox theme on <c>ComboBox.searchable</c>: gives the select its search.</summary>
internal static class ComboSearchLook
{
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("IsOn", typeof(ComboSearchLook));

    private static readonly ConditionalWeakTable<ComboBox, ComboSearcher> s_searchers = [];

    static ComboSearchLook()
    {
        IsOnProperty.Changed.AddClassHandler<ComboBox>((combo, e) =>
        {
            if (e.GetNewValue<bool>())
            {
                if (!s_searchers.TryGetValue(combo, out _)) s_searchers.Add(combo, new ComboSearcher(combo));
            }
            else if (s_searchers.TryGetValue(combo, out var searcher))
            {
                searcher.Detach();
                s_searchers.Remove(combo);
            }
        });
    }

    public static bool GetIsOn(ComboBox combo) => combo.GetValue(IsOnProperty);

    public static void SetIsOn(ComboBox combo, bool value) => combo.SetValue(IsOnProperty, value);

    internal static ComboSearcher? Of(ComboBox combo) => s_searchers.TryGetValue(combo, out var s) ? s : null;
}

// The search a searchable select carries: a field and a list of its own in the select's card, in place of the card's items. Only a pick sets the choice; the keys move a highlight.
internal sealed class ComboSearcher
{
    private const int PageRows = 8;
    private const double LeastWidth = 220;

    private readonly ComboBox _combo;
    private readonly TextBox _search = new() { Classes = { "search", "clearButton" } };
    private readonly TextBlock _empty = new() { Classes = { "combo-empty" }, IsVisible = false };
    private readonly ComboSearchList _list = new();
    private readonly DockPanel _body = new();
    private readonly List<ComboSearchEntry> _options = [];
    private List<ComboSearchEntry>? _entries;
    private bool _grouped;
    private readonly Dictionary<string, ComboSearchHeading> _headings = [];
    private readonly Dictionary<BindingBase, Probe> _probes = [];
    private Popup? _popup;
    private Decorator? _card;
    private ScrollViewer? _scroller;
    private object? _presenter;
    private string _opening = "";
    private bool _swallow;
    private bool _refilterPosted;
    private bool _filling;

    public ComboSearcher(ComboBox combo)
    {
        _combo = combo;
        DockPanel.SetDock(_search, Dock.Top);
        DockPanel.SetDock(_empty, Dock.Top);
        _search.AddHandler(InputElement.KeyDownEvent, OnSearchKey, RoutingStrategies.Tunnel);
        // A key the field leaves alone is not the select's either: its own handler would pick the item it thinks is focused.
        _search.KeyDown += (_, e) => e.Handled = true;
        _search.AddHandler(InputElement.TextInputEvent, OnSearchText, RoutingStrategies.Tunnel);
        _search.PropertyChanged += OnSearchChanged;
        _list.PointerMoved += OnListMoved;
        _list.AddHandler(InputElement.PointerReleasedEvent, OnListReleased, RoutingStrategies.Bubble);
        combo.AddHandler(InputElement.KeyDownEvent, OnComboKey, RoutingStrategies.Tunnel);
        combo.AddHandler(InputElement.TextInputEvent, OnComboText, RoutingStrategies.Tunnel);
        combo.TemplateApplied += OnTemplate;
        combo.PropertyChanged += OnComboChanged;
        combo.DropDownOpened += OnOpened;
        combo.DropDownClosed += OnClosed;
        ((INotifyCollectionChanged)combo.Items).CollectionChanged += OnItemsChanged;
        Texts();
        Hook();
    }

    internal TextBox Search => _search;

    internal IReadOnlyList<object> Shown => _list.ItemsSource as IReadOnlyList<object> ?? [];

    internal object? Hot => _list.Hot?.Item;

    public void Detach()
    {
        _combo.RemoveHandler(InputElement.KeyDownEvent, OnComboKey);
        _combo.RemoveHandler(InputElement.TextInputEvent, OnComboText);
        _combo.TemplateApplied -= OnTemplate;
        _combo.PropertyChanged -= OnComboChanged;
        _combo.DropDownOpened -= OnOpened;
        _combo.DropDownClosed -= OnClosed;
        ((INotifyCollectionChanged)_combo.Items).CollectionChanged -= OnItemsChanged;
        Unhook();
    }

    private void OnTemplate(object? sender, TemplateAppliedEventArgs e)
    {
        Unhook();
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _card = e.NameScope.Find<Decorator>("PopupBorder");
        Hook();
    }

    // The card's scroller holds the search list in place of the select's items, under the search field.
    private void Hook()
    {
        if (_card is null || _popup is null)
        {
            _popup = _combo.GetVisualDescendants().OfType<Popup>().FirstOrDefault(p => p.Name == "PART_Popup");
            _card = (_popup?.Child as Decorator)?.Child as Decorator;
        }
        if (_card is not { Child: ScrollViewer scroller } card) return;
        _scroller = scroller;
        _presenter = scroller.Content;
        if (_presenter is Control presenter) _list.Margin = presenter.Margin;
        scroller.Content = _list;
        card.Child = null;
        var gap = _list.Margin.Left;
        _search.Margin = new Thickness(gap, gap, gap, 0);
        _empty.Margin = new Thickness(gap, gap, gap, gap);
        _body.Children.Clear();
        _body.Children.Add(_search);
        _body.Children.Add(_empty);
        _body.Children.Add(scroller);
        card.Child = _body;
    }

    private void Unhook()
    {
        if (_card is not null && _scroller is not null && _card.Child == _body)
        {
            _body.Children.Clear();
            _card.Child = _scroller;
            _scroller.Content = _presenter;
            _card.ClearValue(Layoutable.WidthProperty);
        }
        _card = null;
        _scroller = null;
        _popup = null;
        _presenter = null;
    }

    private void Texts()
    {
        var words = Controls.Words.Of(_combo);
        var placeholder = ComboSearch.GetPlaceholder(_combo) ?? words["select.search"];
        _search.PlaceholderText = placeholder;
        AutomationProperties.SetName(_search, placeholder);
        _empty.Text = ComboSearch.GetEmptyText(_combo) ?? words["select.empty"];
    }

    private void OnComboChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ComboBox.IsDropDownOpenProperty && e.GetNewValue<bool>())
        {
            Size();
            _filling = true;
            _search.Text = _opening;
            _filling = false;
            _opening = "";
            Refilter();
        }
        else if (e.Property == SelectingItemsControl.SelectedItemProperty) _list.Chosen = _combo.SelectedItem;
        else if (e.Property == Visual.BoundsProperty && _combo.IsDropDownOpen) Size();
        else if (e.Property == ItemsControl.ItemTemplateProperty || e.Property == ItemsControl.DisplayMemberBindingProperty || e.Property == ComboSearch.KeywordsProperty || e.Property == ComboSearch.GroupProperty) _entries = null;
        else if (e.Property == ComboSearch.PlaceholderProperty || e.Property == ComboSearch.EmptyTextProperty || e.Property == Visual.FlowDirectionProperty) Texts();
    }

    // The width comes from the field, never from the rows in view, so it holds while the list scrolls; at least room for the search, within the window.
    private void Size()
    {
        if (_card is null || _combo.Bounds.Width <= 0) return;
        var width = Math.Max(_combo.Bounds.Width, LeastWidth);
        if (TopLevel.GetTopLevel(_combo) is { } top && _popup?.Child is Decorator { Padding: var room })
            width = Math.Min(width, Math.Max(_combo.Bounds.Width, top.ClientSize.Width - room.Left - room.Right));
        _card.Width = width;
    }

    private void OnComboKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled || InPopup(e.Source)) return;
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (!_combo.IsDropDownOpen)
        {
            // The closed field opens on its keys and never steps through the choices as a plain select does.
            if (e.Key is Key.Enter or Key.Space or Key.Down or Key.Up || e.Key == Key.F4 && !alt)
            {
                _swallow = e.Key == Key.Space;
                _combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, true);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape || e.Key == Key.F4 && !alt || alt && e.Key is Key.Up or Key.Down)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab) _combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
    }

    // A letter typed on the closed field opens the list with the search started; one typed before the field took the keyboard goes into it.
    private void OnComboText(object? sender, TextInputEventArgs e)
    {
        if (e.Handled || InPopup(e.Source) || string.IsNullOrWhiteSpace(e.Text) || e.Text.Any(char.IsControl)) return;
        if (!_combo.IsDropDownOpen)
        {
            _opening = e.Text;
            _combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, true);
        }
        else
        {
            _search.Text += e.Text;
            _search.CaretIndex = _search.Text?.Length ?? 0;
        }
        e.Handled = true;
    }

    private void OnSearchText(object? sender, TextInputEventArgs e)
    {
        if (_swallow && string.IsNullOrWhiteSpace(e.Text)) e.Handled = true;
        _swallow = false;
    }

    private void OnSearchKey(object? sender, KeyEventArgs e)
    {
        _swallow = false;
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        switch (e.Key)
        {
            case Key.Down when !alt: Move(1); break;
            case Key.Up when !alt: Move(-1); break;
            case Key.PageDown: Move(PageRows); break;
            case Key.PageUp: Move(-PageRows); break;
            case Key.Enter:
                if (_list.Hot is { } hot) Pick(hot);
                break;
            case Key.Escape:
            case Key.F4 when !alt:
            case Key.Up or Key.Down when alt:
                Close();
                break;
            case Key.Tab:
                Close();
                TopLevel.GetTopLevel(_combo)?.FocusManager?.TryMoveFocus(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? NavigationDirection.Previous : NavigationDirection.Next);
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private void OnSearchChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty && !_filling && _combo.IsDropDownOpen) Refilter();
    }

    private void OnListMoved(object? sender, PointerEventArgs e)
    {
        if (RowOf(e.Source) is { Entry: { IsEnabled: true } entry }) _list.SetHot(entry);
    }

    private void OnListReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left || RowOf(e.Source) is not { Entry: { } entry }) return;
        Pick(entry);
        e.Handled = true;
    }

    // The items, each with its text folded once per opening; group headings only when there is more than one group.
    private List<ComboSearchEntry> Entries()
    {
        if (_entries is not null) return _entries;
        var keywords = ComboSearch.GetKeywords(_combo);
        var group = ComboSearch.GetGroup(_combo);
        var entries = new List<ComboSearchEntry>(_combo.ItemCount);
        foreach (var item in _combo.Items)
        {
            if (item is null) continue;
            var text = TextOf(item);
            var words = keywords is null ? null : Words(Read(keywords, item));
            var heading = group is null ? null : Read(group, item)?.ToString();
            entries.Add(new ComboSearchEntry(item, text, heading, ArabicSearch.Normalize(words is null ? text : text + " " + words), item is not Control { IsEnabled: false }));
        }
        _grouped = entries.Select(x => x.Group).Distinct().Count() > 1;
        return _entries = entries;
    }

    private string TextOf(object item)
    {
        if (TextSearch.GetTextBinding(_combo) is { } text) return Read(text, item)?.ToString() ?? "";
        if (_combo.DisplayMemberBinding is { } display) return Read(display, item)?.ToString() ?? "";
        if (item is Interactive control && TextSearch.GetText(control) is { Length: > 0 } t) return t;
        if (item is ContentControl content) return content.Content as string ?? (content.Content as TextBlock)?.Text ?? content.Content?.ToString() ?? "";
        return item.ToString() ?? "";
    }

    private static string? Words(object? value) => value switch
    {
        null => null,
        string s => s,
        IEnumerable list => string.Join(' ', list.Cast<object?>().Select(o => o?.ToString())),
        _ => value.ToString(),
    };

    private object? Read(BindingBase binding, object item)
    {
        if (!_probes.TryGetValue(binding, out var probe))
        {
            probe = new Probe();
            probe.Bind(Probe.ValueProperty, binding);
            _probes[binding] = probe;
        }
        probe.DataContext = item;
        return probe.GetValue(Probe.ValueProperty);
    }

    private void Refilter(int? keepAt = null)
    {
        var terms = ArabicSearch.Terms(_search.Text);
        var all = Entries();
        var rows = new List<object>();
        _options.Clear();
        if (_grouped)
        {
            var groups = new List<(string? Key, List<ComboSearchEntry> Items)>();
            var at = new Dictionary<string, int>();
            var loose = -1;
            foreach (var entry in all)
            {
                if (!Holds(entry, terms)) continue;
                var index = entry.Group is { } key ? at.TryGetValue(key, out var i) ? i : -1 : loose;
                if (index < 0)
                {
                    index = groups.Count;
                    groups.Add((entry.Group, []));
                    if (entry.Group is { } k) at[k] = index;
                    else loose = index;
                }
                groups[index].Items.Add(entry);
            }
            foreach (var (key, items) in groups)
            {
                if (key is not null)
                {
                    if (!_headings.TryGetValue(key, out var heading)) _headings[key] = heading = new ComboSearchHeading(key);
                    rows.Add(heading);
                }
                rows.AddRange(items);
                _options.AddRange(items);
            }
        }
        else
        {
            foreach (var entry in all)
            {
                if (!Holds(entry, terms)) continue;
                rows.Add(entry);
                _options.Add(entry);
            }
        }
        _list.ItemTemplate = Template();
        _list.Chosen = _combo.SelectedItem;
        _list.SetHot(null);
        _list.ItemsSource = rows;
        _empty.IsVisible = _options.Count == 0;
        if (_scroller is not null) _scroller.IsVisible = _options.Count > 0;
        var hot = keepAt is { } k2 && _options.Count > 0 ? _options[Math.Clamp(k2, 0, _options.Count - 1)]
            : terms.Length == 0 && _options.FirstOrDefault(o => Equals(o.Item, _combo.SelectedItem)) is { } chosen ? chosen
            : _options.FirstOrDefault(o => o.IsEnabled);
        _list.SetHot(hot);
    }

    private static bool Holds(ComboSearchEntry entry, string[] terms)
    {
        foreach (var term in terms)
            if (!entry.Folded.Contains(term, StringComparison.Ordinal)) return false;
        return true;
    }

    // Rows draw an item as the select draws it: its ItemTemplate, or the text its DisplayMemberBinding gives.
    private IDataTemplate? Template()
    {
        if (_combo.ItemTemplate is { } template) return template;
        if (_combo.DisplayMemberBinding is not { } display) return null;
        if (_list.ItemTemplate is DisplayTemplate own && own.Binding == display) return own;
        return new DisplayTemplate(display);
    }

    private void Move(int by)
    {
        if (_options.Count == 0) return;
        var at = _list.Hot is { } hot ? _options.IndexOf(hot) : -1;
        var to = Math.Clamp(at < 0 ? (by > 0 ? 0 : _options.Count - 1) : at + by, 0, _options.Count - 1);
        var step = by > 0 ? 1 : -1;
        while (to >= 0 && to < _options.Count && !_options[to].IsEnabled) to += step;
        if (to < 0 || to >= _options.Count) return;
        _list.SetHot(_options[to]);
        ShowHot();
    }

    // The highlighted row in view; at the first item, the top, so its heading shows too.
    private void ShowHot()
    {
        if (_list.Hot is not { } hot) return;
        if (_options.IndexOf(hot) == 0) _list.ScrollIntoView(0);
        else _list.ScrollIntoView(hot);
    }

    private void Pick(ComboSearchEntry entry)
    {
        if (!entry.IsEnabled) return;
        _combo.SetCurrentValue(SelectingItemsControl.SelectedItemProperty, entry.Item);
        Close();
    }

    private void Close()
    {
        _combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
        _combo.Focus();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _search.Focus();
        _search.CaretIndex = _search.Text?.Length ?? 0;
        ShowHot();
        // The card is laid out after it opens: the choice is brought into view again then, and the field takes the keyboard if it could not yet.
        Dispatcher.UIThread.Post(() =>
        {
            if (!_combo.IsDropDownOpen) return;
            if (!_search.IsFocused) _search.Focus();
            ShowHot();
        }, DispatcherPriority.Loaded);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _list.SetHot(null);
        _list.ItemsSource = null;
        _options.Clear();
        // Closed by a press outside it: the keyboard comes back to the field unless the press gave it to something else.
        Dispatcher.UIThread.Post(() =>
        {
            if (TopLevel.GetTopLevel(_combo)?.FocusManager?.GetFocusedElement() is null) _combo.Focus();
        });
    }

    // Items that change while the list is open (names arriving late) are searched again once, the highlight kept at its place.
    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _entries = null;
        if (!_combo.IsDropDownOpen || _refilterPosted) return;
        _refilterPosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _refilterPosted = false;
            if (_combo.IsDropDownOpen) Refilter(_list.Hot is { } hot ? _options.IndexOf(hot) : null);
        });
    }

    private bool InPopup(object? source) => source is Visual v && _popup?.Child is { } child && (child == v || child.IsVisualAncestorOf(v));

    private static ComboSearchRow? RowOf(object? source) => (source as Visual)?.FindAncestorOfType<ComboSearchRow>(includeSelf: true);

    // Reads a binding's value for an item, off the tree.
    private sealed class Probe : Control
    {
        public static readonly StyledProperty<object?> ValueProperty =
            AvaloniaProperty.Register<Probe, object?>("Value");
    }

    private sealed class DisplayTemplate(BindingBase binding) : IRecyclingDataTemplate
    {
        public BindingBase Binding { get; } = binding;

        public Control? Build(object? param) => Build(param, null);

        public Control? Build(object? data, Control? existing)
        {
            if (existing is TemplateText reused) return reused;
            var text = new TemplateText();
            text.Bind(TextBlock.TextProperty, Binding);
            return text;
        }

        public bool Match(object? data) => true;
    }
}

internal sealed class ComboSearchEntry(object item, string text, string? group, string folded, bool enabled)
{
    public object Item { get; } = item;
    public string Text { get; } = text;
    public string? Group { get; } = group;
    public string Folded { get; } = folded;
    public bool IsEnabled { get; } = enabled;
}

internal sealed record ComboSearchHeading(string Title);

// The search list: headings are pop labels, items are rows drawn as the select's items; virtualized, the two kinds recycled apart.
internal sealed class ComboSearchList : ItemsControl
{
    private const string Heading = "heading";
    private const string Option = "option";

    static ComboSearchList()
    {
        ItemsPanelProperty.OverrideDefaultValue<ComboSearchList>(new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()));
    }

    protected override Type StyleKeyOverride => typeof(ItemsControl);

    public ComboSearchEntry? Hot { get; private set; }

    public object? Chosen
    {
        get => _chosen;
        set
        {
            _chosen = value;
            foreach (var row in GetRealizedContainers().OfType<ComboSearchRow>()) row.IsSelected = row.Entry is { } e && Equals(e.Item, value);
        }
    }

    private object? _chosen;

    public void SetHot(ComboSearchEntry? entry)
    {
        if (Hot is { } old && ContainerFromItem(old) is ComboSearchRow was) was.IsHot = false;
        Hot = entry;
        if (entry is not null && ContainerFromItem(entry) is ComboSearchRow row) row.IsHot = true;
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = item is ComboSearchHeading ? Heading : Option;
        return true;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        Equals(recycleKey, Heading) ? new TextBlock { Classes = { "pop-label" } } : new ComboSearchRow();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (container is TextBlock heading)
        {
            heading.Text = (item as ComboSearchHeading)?.Title;
            return;
        }
        if (container is not ComboSearchRow row || item is not ComboSearchEntry entry) return;
        row.Entry = entry;
        if (entry.Item is ContentControl source)
        {
            row.Content = source.Content is Control ? entry.Text : source.Content;
            row.ContentTemplate = source.ContentTemplate;
        }
        else
        {
            row.Content = entry.Item;
            row.ContentTemplate = ItemTemplate;
        }
        row.IsEnabled = entry.IsEnabled;
        row.IsSelected = Equals(entry.Item, _chosen);
        row.IsHot = entry == Hot;
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is not ComboSearchRow row) return;
        row.Entry = null;
        row.Content = null;
        row.IsSelected = false;
        row.IsHot = false;
    }
}

// A row drawn as the select's item that never takes the keyboard; the highlighted one takes the :ins-hot look.
internal sealed class ComboSearchRow : ComboBoxItem
{
    public const string HotPseudoClass = ":ins-hot";

    public ComboSearchRow()
    {
        Focusable = false;
        Classes.Add("search-row");
    }

    protected override Type StyleKeyOverride => typeof(ComboBoxItem);

    public ComboSearchEntry? Entry { get; set; }

    public bool IsHot
    {
        get => PseudoClasses.Contains(HotPseudoClass);
        set => PseudoClasses.Set(HotPseudoClass, value);
    }
}
