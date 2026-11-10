using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Carries the item a <see cref="ChoicePicker"/> row was picked for.</summary>
public sealed class ChoicePickedEventArgs : RoutedEventArgs
{
    internal ChoicePickedEventArgs(object item) : base(ChoicePicker.PickedEvent) => Item = item;

    /// <summary>The picked item, as it is in <see cref="ChoicePicker.ItemsSource"/>.</summary>
    public object Item { get; }
}

/// <summary>A list to pick one item from under a search that keeps the keyboard (Arabic folded, keywords, group headings), with loading, failure and retry states: the body of a <see cref="ChoiceDialog"/> or a <see cref="PopCard"/>.</summary>
[TemplatePart("PART_Search", typeof(TextBox))]
[TemplatePart("PART_Rows", typeof(Decorator))]
[TemplatePart("PART_Loading", typeof(StackPanel))]
[TemplatePart("PART_Failure", typeof(Control))]
[TemplatePart("PART_Retry", typeof(Button))]
[TemplatePart("PART_Empty", typeof(TextBlock))]
[PseudoClasses(":loading", ":failed", ":empty")]
public class ChoicePicker : TemplatedControl
{
    private const int PageRows = 8;
    private const int SkeletonRows = 5;

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ChoicePicker, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<ChoicePicker, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<BindingBase?> TextBindingProperty =
        AvaloniaProperty.Register<ChoicePicker, BindingBase?>(nameof(TextBinding));

    public static readonly StyledProperty<BindingBase?> KeywordsBindingProperty =
        AvaloniaProperty.Register<ChoicePicker, BindingBase?>(nameof(KeywordsBinding));

    public static readonly StyledProperty<BindingBase?> GroupBindingProperty =
        AvaloniaProperty.Register<ChoicePicker, BindingBase?>(nameof(GroupBinding));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<ChoicePicker, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<ChoicePicker, string?>(nameof(SearchText), "", defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<ChoicePicker, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<string?> EmptyTextProperty =
        AvaloniaProperty.Register<ChoicePicker, string?>(nameof(EmptyText));

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<ChoicePicker, bool>(nameof(IsLoading));

    public static readonly StyledProperty<string?> ErrorProperty =
        AvaloniaProperty.Register<ChoicePicker, string?>(nameof(Error));

    public static readonly StyledProperty<string?> RetryTextProperty =
        AvaloniaProperty.Register<ChoicePicker, string?>(nameof(RetryText));

    public static readonly StyledProperty<ICommand?> RetryCommandProperty =
        AvaloniaProperty.Register<ChoicePicker, ICommand?>(nameof(RetryCommand));

    public static readonly StyledProperty<object?> FootProperty =
        AvaloniaProperty.Register<ChoicePicker, object?>(nameof(Foot));

    public static readonly StyledProperty<int> MaxRowsProperty =
        AvaloniaProperty.Register<ChoicePicker, int>(nameof(MaxRows), coerce: (_, v) => Math.Max(0, v));

    public static readonly DirectProperty<ChoicePicker, object?> HighlightedItemProperty =
        AvaloniaProperty.RegisterDirect<ChoicePicker, object?>(nameof(HighlightedItem), p => p.HighlightedItem);

    /// <summary>Raised from the picker when a row is picked, by Enter or a click; bubbles (a <see cref="ChoiceDialog"/> closes on it).</summary>
    public static readonly RoutedEvent<ChoicePickedEventArgs> PickedEvent =
        RoutedEvent.Register<ChoicePicker, ChoicePickedEventArgs>(nameof(Picked), RoutingStrategies.Bubble);

    private readonly ComboSearchList _list = new();
    private readonly ScrollViewer _scroll = new() { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    private readonly List<ComboSearchEntry> _options = [];
    private readonly Dictionary<string, ComboSearchHeading> _headings = [];
    private readonly Dictionary<BindingBase, Probe> _probes = [];
    private List<ComboSearchEntry>? _entries;
    private INotifyCollectionChanged? _watched;
    private TextBox? _search;
    private Decorator? _rows;
    private StackPanel? _loading;
    private Control? _failure;
    private Button? _retry;
    private TextBlock? _empty;
    private ContentPresenter? _foot;
    private Control? _footShown;
    private object? _highlighted;
    private ComboSearchRow? _pressed;
    private bool _refilterPosted;
    private double _rowHeight;
    private double _headingHeight;

    public ChoicePicker()
    {
        _scroll.Content = _list;
        _list.PointerMoved += OnListMoved;
        _list.AddHandler(PointerPressedEvent, OnListPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        _list.AddHandler(PointerReleasedEvent, OnListReleased, RoutingStrategies.Bubble);
        _list.ContainerPrepared += (_, _) =>
        {
            if (MaxRows > 0 && (_rowHeight <= 0 || _headingHeight <= 0)) Dispatcher.UIThread.Post(Cap, DispatcherPriority.Loaded);
        };
    }

    /// <summary>The items to pick from. An <see cref="INotifyCollectionChanged"/> list is followed while the picker is shown.</summary>
    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>How a row draws its item; unset, the row shows the item's text.</summary>
    public IDataTemplate? ItemTemplate { get => GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }

    /// <summary>The item's text, searched and shown when there is no <see cref="ItemTemplate"/>: a binding to the item; unset, its <c>ToString()</c>.</summary>
    [AssignBinding]
    public BindingBase? TextBinding { get => GetValue(TextBindingProperty); set => SetValue(TextBindingProperty, value); }

    /// <summary>Extra words each item is also found by (a code, a name in the other language): a binding to the item, text or a list of words.</summary>
    [AssignBinding]
    public BindingBase? KeywordsBinding { get => GetValue(KeywordsBindingProperty); set => SetValue(KeywordsBindingProperty, value); }

    /// <summary>The heading each item is listed under: a binding to the item; null or empty is no heading. Groups keep the order their first items come in.</summary>
    [AssignBinding]
    public BindingBase? GroupBinding { get => GetValue(GroupBindingProperty); set => SetValue(GroupBindingProperty, value); }

    /// <summary>The committed item: it wears the check, and it is highlighted and in view while nothing is typed. A pick sets it.</summary>
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    /// <summary>What is typed in the search.</summary>
    public string? SearchText { get => GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }

    /// <summary>The search's placeholder; «ابحث» or "Search" in the app's language when unset.</summary>
    public string? PlaceholderText { get => GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    /// <summary>The line shown when nothing matches; «لا نتائج» or "No results" in the app's language when unset.</summary>
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>True while the items are on their way: skeleton rows stand in the list's place, and nothing can be picked.</summary>
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    /// <summary>Why the items could not come; shown with the retry button in the list's place while not loading. Null or empty: no failure.</summary>
    public string? Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>The retry button's text; «أعد المحاولة» or "Try again" in the app's language when unset.</summary>
    public string? RetryText { get => GetValue(RetryTextProperty); set => SetValue(RetryTextProperty, value); }

    /// <summary>Run by the retry button.</summary>
    public ICommand? RetryCommand { get => GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    /// <summary>Content under the list (a hint, a button to more items); none when null.</summary>
    public object? Foot { get => GetValue(FootProperty); set => SetValue(FootProperty, value); }

    /// <summary>How many rows show before the list scrolls, for a picker in a pop. 0 (the default) gives the list the height the picker is given, as in a <see cref="ChoiceDialog"/>, and holds that height while the matches change.</summary>
    public int MaxRows { get => GetValue(MaxRowsProperty); set => SetValue(MaxRowsProperty, value); }

    /// <summary>The item the keyboard's highlight is on, which Enter picks.</summary>
    public object? HighlightedItem { get => _highlighted; private set => SetAndRaise(HighlightedItemProperty, ref _highlighted, value); }

    public event EventHandler<ChoicePickedEventArgs>? Picked
    {
        add => AddHandler(PickedEvent, value);
        remove => RemoveHandler(PickedEvent, value);
    }

    // For tests: the rows as listed, headings and items.
    internal IReadOnlyList<object> Shown => _list.ItemsSource as IReadOnlyList<object> ?? [];

    internal TextBox? Search => _search;

    /// <summary>Puts the keyboard in the search, the caret at its end.</summary>
    public void FocusSearch()
    {
        if (_search is null) return;
        _search.Focus(NavigationMethod.Tab);
        _search.CaretIndex = _search.Text?.Length ?? 0;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_search is not null)
        {
            _search.RemoveHandler(KeyDownEvent, OnSearchKey);
        }
        if (_rows is not null) _rows.Child = null;
        _search = e.NameScope.Find<TextBox>("PART_Search");
        _rows = e.NameScope.Find<Decorator>("PART_Rows");
        _loading = e.NameScope.Find<StackPanel>("PART_Loading");
        _failure = e.NameScope.Find<Control>("PART_Failure");
        _retry = e.NameScope.Find<Button>("PART_Retry");
        _empty = e.NameScope.Find<TextBlock>("PART_Empty");
        _foot = e.NameScope.Find<ContentPresenter>("PART_Foot");
        TrackFoot();
        _search?.AddHandler(KeyDownEvent, OnSearchKey, RoutingStrategies.Tunnel);
        if (_rows is not null) _rows.Child = _scroll;
        Texts();
        Skeletons();
        Refilter(scroll: true);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Watch(ItemsSource);
        // the items were not followed while the picker was away: read them again, the highlight kept on its item
        if (_entries is null) return;
        _entries = null;
        Refilter(keep: true, scroll: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Watch(null);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // In a dialog or a pop the picker is there to be typed into; laid out, the choice is brought into view.
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsLoaded) return;
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
            if (InOverlay() && (focused is null || !this.IsVisualAncestorOf(focused))) FocusSearch();
            Cap();
            ShowHot();
        }, DispatcherPriority.Loaded);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        // With no row cap the picker takes the height it is given, so a dialog keeps its size while the matches change.
        return MaxRows == 0 && !double.IsInfinity(availableSize.Height) ? size.WithHeight(Math.Max(size.Height, availableSize.Height)) : size;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ItemsSourceProperty)
        {
            if (IsAttachedToVisualTree()) Watch(ItemsSource);
            _entries = null;
            Refilter(keep: true, scroll: true);
        }
        else if (p == TextBindingProperty || p == KeywordsBindingProperty || p == GroupBindingProperty || p == ItemTemplateProperty)
        {
            _entries = null;
            Refilter(keep: true);
        }
        else if (p == SearchTextProperty) Refilter(scroll: true);
        else if (p == SelectedItemProperty)
        {
            _list.Chosen = SelectedItem;
            if (ArabicSearch.Terms(SearchText).Length == 0 && _options.FirstOrDefault(o => Equals(o.Item, SelectedItem)) is { } chosen) SetHot(chosen);
        }
        else if (p == IsLoadingProperty || p == ErrorProperty) State();
        else if (p == FootProperty) TrackFoot();
        else if (p == MaxRowsProperty)
        {
            Skeletons();
            Cap();
            InvalidateMeasure();
        }
        else if (p == PlaceholderTextProperty || p == EmptyTextProperty || p == RetryTextProperty || p == FlowDirectionProperty) Texts();
    }

    // The foot takes no room, and leaves no gap above it, while it is null or a control that is hidden.
    private void TrackFoot()
    {
        if (_footShown is not null) _footShown.PropertyChanged -= OnFootChanged;
        _footShown = Foot as Control;
        if (_footShown is not null) _footShown.PropertyChanged += OnFootChanged;
        if (_foot is not null) _foot.IsVisible = Foot is not null && _footShown?.IsVisible != false;
    }

    private void OnFootChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && _foot is not null) _foot.IsVisible = _footShown?.IsVisible != false;
    }

    private bool IsAttachedToVisualTree() => TopLevel.GetTopLevel(this) is not null;

    private bool InOverlay() => this.FindAncestorOfType<Dialog>() is not null || TopLevel.GetTopLevel(this) is PopupRoot || this.FindAncestorOfType<FlyoutPresenter>() is not null;

    private void Watch(IEnumerable? items)
    {
        if (_watched is not null) _watched.CollectionChanged -= OnItemsChanged;
        _watched = items as INotifyCollectionChanged;
        if (_watched is not null) _watched.CollectionChanged += OnItemsChanged;
    }

    // Items that change while the picker is shown (names arriving late, a slow list) are searched again once, the highlight kept on its item.
    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _entries = null;
        if (_refilterPosted) return;
        _refilterPosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _refilterPosted = false;
            Refilter(keep: true);
        });
    }

    private void Texts()
    {
        var words = Controls.Words.Of(this);
        if (_search is not null)
        {
            var placeholder = PlaceholderText ?? words["select.search"];
            _search.PlaceholderText = placeholder;
            AutomationProperties.SetName(_search, placeholder);
        }
        if (_empty is not null) _empty.Text = EmptyText ?? words["select.empty"];
        if (_retry is not null) _retry.Content = RetryText ?? words["choice.retry"];
    }

    // Skeleton rows as tall as the rows they stand for: the row cap's count, else a few.
    private void Skeletons()
    {
        if (_loading is null) return;
        var count = MaxRows > 0 ? MaxRows : SkeletonRows;
        if (_loading.Children.Count == count) return;
        _loading.Children.Clear();
        // the select's row padding, from the spacing tokens
        double Space(string key) => this.TryFindResource(key, ActualThemeVariant, out var v) && v is double d ? d : 0;
        var padding = new Thickness(Space("InsSp3"), Space("InsSp2"));
        for (var i = 0; i < count; i++)
            _loading.Children.Add(new Border { Padding = padding, Child = new Skeleton { Classes = { "text" } } });
    }

    // The list at the row cap's height once a row has been measured: that many items, and the headings listed among them; no cap gives it the room the picker has.
    private void Cap()
    {
        if (MaxRows == 0)
        {
            _scroll.ClearValue(MaxHeightProperty);
            return;
        }
        var realized = _list.GetRealizedContainers().ToList();
        if (_rowHeight <= 0 && realized.OfType<ComboSearchRow>().FirstOrDefault(r => r.Bounds.Height > 0) is { } row) _rowHeight = row.Bounds.Height;
        if (_headingHeight <= 0 && realized.OfType<TextBlock>().FirstOrDefault(t => t.Bounds.Height > 0) is { } heading) _headingHeight = heading.Bounds.Height;
        if (_rowHeight <= 0) return;
        var headings = 0;
        var items = 0;
        foreach (var shown in Shown)
        {
            if (shown is ComboSearchHeading) headings++;
            else if (++items == MaxRows) break;
        }
        _scroll.MaxHeight = _rowHeight * MaxRows + _headingHeight * headings + _list.Margin.Top + _list.Margin.Bottom;
    }

    private void OnSearchKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) return;
        switch (e.Key)
        {
            case Key.Down: Move(1); break;
            case Key.Up: Move(-1); break;
            case Key.PageDown: Move(PageRows); break;
            case Key.PageUp: Move(-PageRows); break;
            case Key.Enter:
                if (_list.Hot is { } hot) Pick(hot);
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private void OnListMoved(object? sender, PointerEventArgs e)
    {
        if (RowOf(e.Source) is { Entry: { IsEnabled: true } entry }) SetHot(entry);
    }

    private void OnListPressed(object? sender, PointerPressedEventArgs e) =>
        _pressed = e.GetCurrentPoint(_list).Properties.IsLeftButtonPressed ? RowOf(e.Source) : null;

    // A click picks only the row it was pressed and released on.
    private void OnListReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pressed = _pressed;
        _pressed = null;
        if (e.InitialPressMouseButton != MouseButton.Left || pressed is not { Entry: { } entry }) return;
        if (!new Rect(pressed.Bounds.Size).Contains(e.GetPosition(pressed))) return;
        Pick(entry);
        e.Handled = true;
    }

    private static ComboSearchRow? RowOf(object? source) => (source as Visual)?.FindAncestorOfType<ComboSearchRow>(includeSelf: true);

    private void Pick(ComboSearchEntry entry)
    {
        if (!entry.IsEnabled || IsLoading) return;
        SetCurrentValue(SelectedItemProperty, entry.Item);
        RaiseEvent(new ChoicePickedEventArgs(entry.Item));
    }

    private void Move(int by)
    {
        if (_options.Count == 0 || IsLoading) return;
        var at = _list.Hot is { } hot ? _options.IndexOf(hot) : -1;
        var to = Math.Clamp(at < 0 ? (by > 0 ? 0 : _options.Count - 1) : at + by, 0, _options.Count - 1);
        var step = by > 0 ? 1 : -1;
        while (to >= 0 && to < _options.Count && !_options[to].IsEnabled) to += step;
        if (to < 0 || to >= _options.Count) return;
        SetHot(_options[to]);
        ShowHot();
    }

    private void SetHot(ComboSearchEntry? entry)
    {
        _list.SetHot(entry);
        HighlightedItem = entry?.Item;
    }

    // The highlighted row in view; at the first item, the top, so its heading shows too.
    private void ShowHot()
    {
        if (_list.Hot is not { } hot) return;
        if (_options.IndexOf(hot) == 0) _list.ScrollIntoView(0);
        else _list.ScrollIntoView(hot);
    }

    // The items, each with its text folded once until they change.
    private List<ComboSearchEntry> Entries()
    {
        if (_entries is not null) return _entries;
        var entries = new List<ComboSearchEntry>();
        if (ItemsSource is { } items)
        {
            foreach (var item in items)
            {
                if (item is null) continue;
                var text = TextBinding is { } t ? Read(t, item)?.ToString() ?? "" : item.ToString() ?? "";
                var words = KeywordsBinding is { } k ? Words(Read(k, item)) : null;
                var group = GroupBinding is { } g ? Read(g, item)?.ToString() : null;
                entries.Add(new ComboSearchEntry(item, text, string.IsNullOrEmpty(group) ? null : group, ArabicSearch.Normalize(words is null ? text : text + " " + words), true));
            }
        }
        return _entries = entries;
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

    // Lists the matches under their headings. keep: the highlight stays on its item (or its place) through new items; scroll: the highlight is brought into view.
    private void Refilter(bool keep = false, bool scroll = false)
    {
        var kept = keep ? _list.Hot : null;
        var keptAt = kept is null ? -1 : _options.IndexOf(kept);
        var terms = ArabicSearch.Terms(SearchText);
        var groups = new List<(string? Key, List<ComboSearchEntry> Items)>();
        var at = new Dictionary<string, int>();
        var loose = -1;
        foreach (var entry in Entries())
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
        var rows = new List<object>();
        _options.Clear();
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
        _list.ItemTemplate = RowTemplate();
        _list.Chosen = SelectedItem;
        _list.SetHot(null);
        _list.ItemsSource = rows;
        var hot = kept is not null && _options.FirstOrDefault(o => Equals(o.Item, kept.Item)) is { } same ? same
            : keptAt >= 0 && _options.Count > 0 ? _options[Math.Min(keptAt, _options.Count - 1)]
            : terms.Length == 0 && _options.FirstOrDefault(o => Equals(o.Item, SelectedItem)) is { } chosen ? chosen
            : _options.FirstOrDefault(o => o.IsEnabled);
        SetHot(hot);
        State();
        if (MaxRows > 0) Dispatcher.UIThread.Post(Cap, DispatcherPriority.Loaded);
        if (scroll) Dispatcher.UIThread.Post(ShowHot, DispatcherPriority.Loaded);
    }

    private static bool Holds(ComboSearchEntry entry, string[] terms)
    {
        foreach (var term in terms)
            if (!entry.Folded.Contains(term, StringComparison.Ordinal)) return false;
        return true;
    }

    // Rows draw the item with the ItemTemplate, else its text.
    private IDataTemplate? RowTemplate()
    {
        if (ItemTemplate is { } template) return template;
        if (TextBinding is not { } text) return null;
        if (_list.ItemTemplate is TextTemplate own && own.Binding == text) return own;
        return new TextTemplate(text);
    }

    // One of the list, the skeleton rows, the failure or the empty line shows.
    private void State()
    {
        var loading = IsLoading;
        var failed = !loading && !string.IsNullOrEmpty(Error);
        var empty = !loading && !failed && _options.Count == 0;
        PseudoClasses.Set(":loading", loading);
        PseudoClasses.Set(":failed", failed);
        PseudoClasses.Set(":empty", empty);
        if (_loading is not null) _loading.IsVisible = loading;
        if (_failure is not null) _failure.IsVisible = failed;
        if (_empty is not null) _empty.IsVisible = empty;
        _scroll.IsVisible = !loading && !failed && !empty;
    }

    // Reads a binding's value for an item, off the tree.
    private sealed class Probe : Control
    {
        public static readonly StyledProperty<object?> ValueProperty =
            AvaloniaProperty.Register<Probe, object?>("Value");
    }

    private sealed class TextTemplate(BindingBase binding) : IRecyclingDataTemplate
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

/// <summary>A centred <see cref="Dialog"/> holding a <see cref="ChoicePicker"/>: only the list scrolls, a pick closes it with the item as the result, and Escape, the cross or a press outside close it with null.</summary>
public class ChoiceDialog : Dialog
{
    static ChoiceDialog()
    {
        ClosedByProperty.OverrideDefaultValue<ChoiceDialog>(DialogClosedBy.Any);
    }

    public ChoiceDialog()
    {
        Classes.Add("choice");
        AddHandler(ChoicePicker.PickedEvent, OnPicked);
    }

    protected override Type StyleKeyOverride => typeof(Dialog);

    private void OnPicked(object? sender, ChoicePickedEventArgs e)
    {
        if (e.Source is not ChoicePicker picker || picker != Content) return;
        e.Handled = true;
        Close(e.Item);
    }
}
