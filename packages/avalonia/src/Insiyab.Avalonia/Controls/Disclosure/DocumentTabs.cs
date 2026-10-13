using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Tabs for open items, as in a browser or an editor: the tab strip's look and keys (<see cref="Tabs"/>), plus a close button on each tab, Ctrl+W, Ctrl+F4, Delete on a focused tab and a middle click to close, a drag (or Ctrl+Shift+PageUp/PageDown) to reorder, a strip that scrolls with arrows when the tabs don't fit, and an optional new-tab button. The app owns the items: a close or a move nobody handles is done here, on <c>Items</c> or on an <see cref="IList"/> <c>ItemsSource</c>; <see cref="NewTabRequested"/> is the app's alone.</summary>
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[TemplatePart("PART_Back", typeof(Button))]
[TemplatePart("PART_Forward", typeof(Button))]
[TemplatePart("PART_New", typeof(Button))]
[PseudoClasses(":overflow")]
public class DocumentTabs : TabControl
{
    /// <summary>Shows the "+" button after the tabs; pressing it raises <see cref="NewTabRequested"/>.</summary>
    public static readonly StyledProperty<bool> ShowNewButtonProperty =
        AvaloniaProperty.Register<DocumentTabs, bool>(nameof(ShowNewButton));

    /// <summary>The close button's name for a screen reader.</summary>
    public static readonly StyledProperty<string> CloseLabelProperty =
        AvaloniaProperty.Register<DocumentTabs, string>(nameof(CloseLabel), "إغلاق التبويب");

    /// <summary>The "+" button's name for a screen reader.</summary>
    public static readonly StyledProperty<string> NewLabelProperty =
        AvaloniaProperty.Register<DocumentTabs, string>(nameof(NewLabel), "تبويب جديد");

    /// <summary>A tab asks to close (its button, Ctrl+W, Delete, a middle click). Set <c>Handled</c> to close it yourself, or to keep it open.</summary>
    public static readonly RoutedEvent<DocumentTabEventArgs> TabCloseRequestedEvent =
        RoutedEvent.Register<DocumentTabs, DocumentTabEventArgs>(nameof(TabCloseRequested), RoutingStrategies.Bubble);

    /// <summary>A tab was dropped at a new place. Set <c>Handled</c> to move the item yourself, or to refuse the move.</summary>
    public static readonly RoutedEvent<DocumentTabMoveEventArgs> TabMoveRequestedEvent =
        RoutedEvent.Register<DocumentTabs, DocumentTabMoveEventArgs>(nameof(TabMoveRequested), RoutingStrategies.Bubble);

    /// <summary>The "+" button was pressed: add the new item, and select it.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> NewTabRequestedEvent =
        RoutedEvent.Register<DocumentTabs, RoutedEventArgs>(nameof(NewTabRequested), RoutingStrategies.Bubble);

    private ScrollViewer? _scroll;
    private Button? _back;
    private Button? _forward;
    private Button? _new;
    private Drag? _drag;

    public DocumentTabs()
    {
        AddHandler(KeyDownEvent, OnKeys);
        SelectionChanged += (_, e) =>
        {
            if (e.Source == this) Dispatcher.UIThread.Post(() => ContainerFromIndex(SelectedIndex)?.BringIntoView(), DispatcherPriority.Loaded);
        };
    }

    public bool ShowNewButton { get => GetValue(ShowNewButtonProperty); set => SetValue(ShowNewButtonProperty, value); }

    public string CloseLabel { get => GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    public string NewLabel { get => GetValue(NewLabelProperty); set => SetValue(NewLabelProperty, value); }

    public event EventHandler<DocumentTabEventArgs>? TabCloseRequested { add => AddHandler(TabCloseRequestedEvent, value); remove => RemoveHandler(TabCloseRequestedEvent, value); }

    public event EventHandler<DocumentTabMoveEventArgs>? TabMoveRequested { add => AddHandler(TabMoveRequestedEvent, value); remove => RemoveHandler(TabMoveRequestedEvent, value); }

    public event EventHandler<RoutedEventArgs>? NewTabRequested { add => AddHandler(NewTabRequestedEvent, value); remove => RemoveHandler(NewTabRequestedEvent, value); }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<DocumentTab>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new DocumentTab();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_scroll is not null) _scroll.PropertyChanged -= OnScrollChanged;
        if (_back is not null) _back.Click -= OnBack;
        if (_forward is not null) _forward.Click -= OnForward;
        if (_new is not null) _new.Click -= OnNew;
        base.OnApplyTemplate(e);
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _back = e.NameScope.Find<Button>("PART_Back");
        _forward = e.NameScope.Find<Button>("PART_Forward");
        _new = e.NameScope.Find<Button>("PART_New");
        if (_scroll is not null) _scroll.PropertyChanged += OnScrollChanged;
        if (_back is not null) _back.Click += OnBack;
        if (_forward is not null) _forward.Click += OnForward;
        if (_new is not null) _new.Click += OnNew;
        UpdateArrows();
    }

    private void OnScrollChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.ExtentProperty || e.Property == ScrollViewer.ViewportProperty || e.Property == ScrollViewer.OffsetProperty) UpdateArrows();
    }

    private void UpdateArrows()
    {
        if (_scroll is null) return;
        var room = _scroll.Extent.Width - _scroll.Viewport.Width;
        PseudoClasses.Set(":overflow", room > 0.5);
        if (_back is not null) _back.IsEnabled = _scroll.Offset.X > 0.5;
        if (_forward is not null) _forward.IsEnabled = _scroll.Offset.X < room - 0.5;
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ScrollBy(-1);

    private void OnForward(object? sender, RoutedEventArgs e) => ScrollBy(1);

    // A page of the strip at a time, at least one tab's width; the offset is logical, so "back" is the start side in either direction.
    private void ScrollBy(int direction)
    {
        if (_scroll is null) return;
        var step = Math.Max(120, _scroll.Viewport.Width * 0.6);
        var max = Math.Max(0, _scroll.Extent.Width - _scroll.Viewport.Width);
        _scroll.Offset = _scroll.Offset.WithX(Math.Clamp(_scroll.Offset.X + direction * step, 0, max));
    }

    private void OnNew(object? sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(NewTabRequestedEvent, this));

    private void OnKeys(object? sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        var ctrl = e.KeyModifiers == KeyModifiers.Control;
        if (ctrl && e.Key is Key.W or Key.F4)
        {
            e.Handled = true;
            Close(SelectedIndex);
        }
        else if (e.Key == Key.Delete && e.KeyModifiers == KeyModifiers.None && e.Source is DocumentTab tab && IndexFromContainer(tab) >= 0)
        {
            e.Handled = true;
            Close(IndexFromContainer(tab));
        }
        else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key is Key.PageUp or Key.PageDown && SelectedIndex >= 0)
        {
            e.Handled = true;
            var to = SelectedIndex + (e.Key == Key.PageDown ? 1 : -1);
            if (to < 0 || to >= ItemCount) return;
            var focused = IsKeyboardFocusWithin;
            Move(SelectedIndex, to);
            if (focused) ContainerFromIndex(SelectedIndex)?.Focus(NavigationMethod.Tab);
        }
    }

    /// <summary>Asks to close the tab at <paramref name="index"/>, as its close button does.</summary>
    public void Close(int index)
    {
        if (index < 0 || index >= ItemCount) return;
        var container = ContainerFromIndex(index);
        if (container is DocumentTab { IsClosable: false }) return;
        var item = ItemsView[index];
        var args = new DocumentTabEventArgs(TabCloseRequestedEvent, item, index);
        RaiseEvent(args);
        if (args.Handled) return;
        var selected = index == SelectedIndex;
        var focused = (container?.IsKeyboardFocusWithin ?? false) || (selected && IsKeyboardFocusWithin);
        var next = selected && ItemCount > 1 ? ItemsView[index + 1 < ItemCount ? index + 1 : index - 1] : null;
        if (!Edit(list => list.RemoveAt(index))) return;
        if (next is not null) SelectedItem = next;
        if (focused) Dispatcher.UIThread.Post(() => ContainerFromIndex(SelectedIndex)?.Focus(NavigationMethod.Tab), DispatcherPriority.Loaded);
    }

    /// <summary>Asks to move the tab at <paramref name="from"/> to <paramref name="to"/>, as a drag does; the selected tab stays selected.</summary>
    public void Move(int from, int to)
    {
        if (from == to || from < 0 || to < 0 || from >= ItemCount || to >= ItemCount) return;
        var item = ItemsView[from];
        var selected = SelectedItem;
        var transition = PageTransition;
        Tabs.Hush(this, true);
        PageTransition = null;
        try
        {
            var args = new DocumentTabMoveEventArgs(TabMoveRequestedEvent, item, from, to);
            RaiseEvent(args);
            if (!args.Handled)
                Edit(list =>
                {
                    list.RemoveAt(from);
                    list.Insert(to, item);
                });
            if (selected is not null && !Equals(SelectedItem, selected)) SelectedItem = selected;
        }
        finally
        {
            PageTransition = transition;
            Tabs.Hush(this, false);
        }
    }

    private bool Edit(Action<IList> change)
    {
        if (ItemsSource is null)
        {
            change(Items);
            return true;
        }
        if (ItemsSource is IList { IsReadOnly: false, IsFixedSize: false } list)
        {
            change(list);
            return true;
        }
        return false;
    }

    internal void DragStart(DocumentTab tab, PointerPressedEventArgs e)
    {
        var index = IndexFromContainer(tab);
        if (index < 0 || tab.GetVisualParent() is not Panel panel) return;
        _drag = new Drag(tab, index, e.GetPosition(panel).X, panel);
        e.Pointer.Capture(tab);
    }

    internal void DragMove(DocumentTab tab, PointerEventArgs e)
    {
        if (_drag is not { } d || d.Tab != tab) return;
        var x = e.GetPosition(d.Panel).X - d.StartX;
        if (!d.Live)
        {
            if (Math.Abs(x) < 6 || ItemCount < 2) return;
            Begin(d);
        }
        Follow(d, x);
    }

    internal void DragEnd(DocumentTab tab)
    {
        if (_drag is not { } d || d.Tab != tab) return;
        _drag = null;
        if (!d.Live) return;
        Settle(d);
        if (d.To != d.From)
        {
            Move(d.From, d.To);
            ContainerFromIndex(SelectedIndex)?.Focus(NavigationMethod.Pointer);
        }
    }

    internal void DragCancel(DocumentTab tab)
    {
        if (_drag is not { } d || d.Tab != tab) return;
        _drag = null;
        if (d.Live) Settle(d);
    }

    private void Begin(Drag d)
    {
        d.Live = true;
        d.Tabs = Enumerable.Range(0, ItemCount).Select(i => ContainerFromIndex(i) as DocumentTab).ToArray();
        d.X = d.Tabs.Select(t => t?.Bounds.X ?? 0).ToArray();
        d.W = d.Tabs.Select(t => t?.Bounds.Width ?? 0).ToArray();
        d.Gap = d.X.Length > 1 ? d.X[1] - d.X[0] - d.W[0] : 0;
        d.Tab.SetDragging(true);
    }

    // The dragged tab follows the pointer inside the strip; each tab whose middle its far edge crosses slides over by the dragged tab's width.
    private static void Follow(Drag d, double x)
    {
        var n = d.X.Length;
        var from = d.From;
        x = Math.Clamp(x, d.X[0] - d.X[from], d.X[n - 1] + d.W[n - 1] - d.W[from] - d.X[from]);
        var lead = d.X[from] + x;
        var trail = lead + d.W[from];
        var to = from;
        for (var i = from + 1; i < n; i++)
            if (trail > d.X[i] + d.W[i] / 2) to = i;
        for (var i = from - 1; i >= 0; i--)
            if (lead < d.X[i] + d.W[i] / 2) to = i;
        d.To = to;
        var shift = d.W[from] + d.Gap;
        for (var i = 0; i < n; i++)
            d.Tabs[i]?.SlideTo(i == from ? x : i > from && i <= to ? -shift : i < from && i >= to ? shift : 0);
    }

    // Back to rest at once, with no slide, since the move that follows puts every tab in its new place.
    private static void Settle(Drag d)
    {
        foreach (var t in d.Tabs)
        {
            if (t is null) continue;
            t.SetDragging(true);
            t.SlideTo(0);
            t.SetDragging(false);
        }
    }

    private sealed class Drag(DocumentTab tab, int from, double startX, Panel panel)
    {
        public DocumentTab Tab { get; } = tab;
        public int From { get; } = from;
        public double StartX { get; } = startX;
        public Panel Panel { get; } = panel;
        public bool Live { get; set; }
        public int To { get; set; } = from;
        public DocumentTab?[] Tabs { get; set; } = [];
        public double[] X { get; set; } = [];
        public double[] W { get; set; } = [];
        public double Gap { get; set; }
    }
}

/// <summary>A tab of <see cref="DocumentTabs"/>: a <see cref="TabItem"/> with a close button, shown on the selected tab and under the pointer.</summary>
[TemplatePart("PART_Close", typeof(Button))]
[TemplatePart("PART_Slide", typeof(Panel))]
[PseudoClasses(":dragging")]
public class DocumentTab : TabItem
{
    /// <summary>False keeps the tab open: no close button, and Ctrl+W, Delete and a middle click pass it by.</summary>
    public static readonly StyledProperty<bool> IsClosableProperty =
        AvaloniaProperty.Register<DocumentTab, bool>(nameof(IsClosable), true);

    private Button? _close;
    private Panel? _slide;
    private bool _middle;

    public bool IsClosable { get => GetValue(IsClosableProperty); set => SetValue(IsClosableProperty, value); }

    private DocumentTabs? Owner => this.FindLogicalAncestorOfType<DocumentTabs>();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_close is not null) _close.Click -= OnClose;
        base.OnApplyTemplate(e);
        _close = e.NameScope.Find<Button>("PART_Close");
        _slide = e.NameScope.Find<Panel>("PART_Slide");
        if (_close is not null) _close.Click += OnClose;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Owner is { } owner) owner.Close(owner.IndexFromContainer(this));
    }

    internal void SetDragging(bool on) => PseudoClasses.Set(":dragging", on);

    internal void SlideTo(double x)
    {
        if (_slide is null) return;
        var shift = TransformOperations.CreateBuilder(1);
        shift.AppendTranslate(x, 0);
        _slide.RenderTransform = shift.Build();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsMiddleButtonPressed)
        {
            _middle = true;
            e.Handled = true;
            return;
        }
        base.OnPointerPressed(e);
        if (point.Properties.IsLeftButtonPressed) Owner?.DragStart(this, e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Owner?.DragMove(this, e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_middle && e.InitialPressMouseButton == MouseButton.Middle)
        {
            _middle = false;
            e.Handled = true;
            if (IsClosable && new Rect(Bounds.Size).Contains(e.GetPosition(this)) && Owner is { } owner) owner.Close(owner.IndexFromContainer(this));
            return;
        }
        base.OnPointerReleased(e);
        Owner?.DragEnd(this);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _middle = false;
        Owner?.DragCancel(this);
    }
}

/// <summary>The tab a <see cref="DocumentTabs"/> event is about: its item and its place.</summary>
public class DocumentTabEventArgs(RoutedEvent routedEvent, object? item, int index) : RoutedEventArgs(routedEvent)
{
    public object? Item { get; } = item;

    public int Index { get; } = index;
}

/// <summary>A tab dropped at a new place: its item, where it was and where it goes.</summary>
public class DocumentTabMoveEventArgs(RoutedEvent routedEvent, object? item, int from, int to) : RoutedEventArgs(routedEvent)
{
    public object? Item { get; } = item;

    public int From { get; } = from;

    public int To { get; } = to;
}
