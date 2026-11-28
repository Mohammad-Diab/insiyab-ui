using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a <see cref="NumberListItem"/> stands for: not done yet, done (no verdict), done and judged right, or done and judged wrong.</summary>
public enum NumberState
{
    Open,
    Done,
    Right,
    Wrong,
}

/// <summary>One numbered item of a <see cref="NumberList"/>: its number, an optional line beside it (the vertical list's rows), its state, a flag, and the words a screen reader says for it.</summary>
public class NumberListItem : AvaloniaObject
{
    public static readonly StyledProperty<int> NumberProperty =
        AvaloniaProperty.Register<NumberListItem, int>(nameof(Number));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<NumberListItem, string?>(nameof(Text));

    public static readonly StyledProperty<NumberState> StateProperty =
        AvaloniaProperty.Register<NumberListItem, NumberState>(nameof(State));

    public static readonly StyledProperty<bool> IsFlaggedProperty =
        AvaloniaProperty.Register<NumberListItem, bool>(nameof(IsFlagged));

    public static readonly StyledProperty<string?> NameProperty =
        AvaloniaProperty.Register<NumberListItem, string?>(nameof(Name));

    /// <summary>The number shown (1, 2, …).</summary>
    public int Number { get => GetValue(NumberProperty); set => SetValue(NumberProperty, value); }

    /// <summary>A line beside the number in the vertical list (the start of the item's words); none in the strip.</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public NumberState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }

    /// <summary>Marked to come back to: a small flag on the number's corner.</summary>
    public bool IsFlagged { get => GetValue(IsFlaggedProperty); set => SetValue(IsFlaggedProperty, value); }

    /// <summary>What a screen reader says for the item (its number and state in words); the number alone when unset.</summary>
    public string? Name { get => GetValue(NameProperty); set => SetValue(NameProperty, value); }
}

/// <summary>A list of numbered items to move between, each showing where it stands: the current one, done, judged right (the ok tone) or wrong (the bad tone), flagged, as a long form's or a survey's list of questions or a run of checks. <see cref="Orientation"/> Vertical (the default) is a side list of rows with its own scroll, a head (<see cref="Header"/>) and a foot (<see cref="Footer"/>), and a button in its head that folds it to that button alone (<see cref="IsOpen"/>); Horizontal is a strip of numbers that scrolls sideways under a bar. A tap on an item (a press let go without moving past the touch slop), or Enter/Space on it, makes it <see cref="SelectedIndex"/>; a finger or a mouse that moves past the slop along the list's scroll drags it instead (a finger's drag flings on), even when it started on a number, and picks nothing. In the strip a number's press face and its hit area are its badge exactly. The arrow keys move between the items (the reading direction's way in a strip), Home and End to the first and last. The current item is scrolled into view. Light and dark, either direction, a finger's size inside a host marked <c>touch</c>.</summary>
[TemplatePart("PART_Host", typeof(Border))]
[TemplatePart("PART_Scroller", typeof(ScrollViewer))]
[TemplatePart("PART_Toggle", typeof(Button))]
[PseudoClasses(":horizontal", ":closed", ":wraps")]
public class NumberList : TemplatedControl
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<NumberList, int>(nameof(SelectedIndex), -1, defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<NumberList, Orientation>(nameof(Orientation), Orientation.Vertical);

    public static readonly StyledProperty<bool> WrapsProperty =
        AvaloniaProperty.Register<NumberList, bool>(nameof(Wraps));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<NumberList, bool>(nameof(IsOpen), true, defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<NumberList, object?>(nameof(Header));

    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<NumberList, object?>(nameof(Footer));

    public static readonly StyledProperty<string?> OpenTextProperty =
        AvaloniaProperty.Register<NumberList, string?>(nameof(OpenText));

    public static readonly StyledProperty<string?> CloseTextProperty =
        AvaloniaProperty.Register<NumberList, string?>(nameof(CloseText));

    /// <summary>Raised when a press or a key picks an item (its index is then <see cref="SelectedIndex"/>).</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ItemInvokedEvent =
        RoutedEvent.Register<NumberList, RoutedEventArgs>(nameof(ItemInvoked), RoutingStrategies.Bubble);

    // Lucide's flag and panel-left open / close, at the 24 grid
    internal static readonly Geometry FlagGlyph = Geometry.Parse("M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1zM4 22v-7");
    internal static readonly Geometry FoldGlyph = Geometry.Parse("M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2ZM9 3v18M16 15l-3-3 3-3");
    internal static readonly Geometry UnfoldGlyph = Geometry.Parse("M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2ZM9 3v18M14 9l3 3-3 3");

    private Border? _host;
    private Panel? _items;
    private ScrollViewer? _scroller;
    private Button? _toggle;
    private Icon? _toggleIcon;
    private readonly List<Button> _cells = [];

    // a press that may become a drag: its pointer, where it went down (then, once a drag, where the scroller it moves would have it start), the scroller it moves (the list's own, or the one around it for a drag across) and along which axis, that scroller's offset as the drag began, the last two moves for a fling
    private IPointer? _pointer;
    private Point _from;
    private Vector _fromOffset;
    private bool _decided, _alongX;
    private ScrollViewer? _target, _flingTarget;
    private (Point At, ulong Time) _last, _before;
    private DispatcherTimer? _fling;
    private Vector _flingSpeed;
    private FrameTween? _glide; // the list's own scroll gliding to the current item

    // a fling slows by this share each frame of 16 ms (as a platform list's) and stops below this speed (px per ms)
    private const double FlingKeep = 0.95, FlingStop = 0.02, FlingFrame = 16;

    public NumberList()
    {
        Items.CollectionChanged += OnItemsChanged;
        AddHandler(PointerPressedEvent, OnDragPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnDragMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnDragReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, OnDragLost, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>The items, in order.</summary>
    [Content]
    public AvaloniaList<NumberListItem> Items { get; } = [];

    /// <summary>The current item's index (-1: none).</summary>
    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The numbers wrap onto more rows instead of scrolling sideways (horizontal only): the whole list at once, as in a sheet.</summary>
    public bool Wraps { get => GetValue(WrapsProperty); set => SetValue(WrapsProperty, value); }

    /// <summary>The vertical list unfolded (the default); folded it shows its head's button alone.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>The vertical list's head, before its fold button (its title).</summary>
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Under the vertical list's rows (its counts).</summary>
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>The fold button's words while folded (its tip and its spoken name).</summary>
    public string? OpenText { get => GetValue(OpenTextProperty); set => SetValue(OpenTextProperty, value); }

    /// <summary>The fold button's words while unfolded.</summary>
    public string? CloseText { get => GetValue(CloseTextProperty); set => SetValue(CloseTextProperty, value); }

    public event EventHandler<RoutedEventArgs>? ItemInvoked
    {
        add => AddHandler(ItemInvokedEvent, value);
        remove => RemoveHandler(ItemInvokedEvent, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_toggle is not null)
            _toggle.Click -= OnToggle;
        _host = e.NameScope.Find<Border>("PART_Host");
        _scroller = e.NameScope.Find<ScrollViewer>("PART_Scroller");
        _toggle = e.NameScope.Find<Button>("PART_Toggle");
        _toggleIcon = e.NameScope.Find<Icon>("PART_ToggleIcon");
        if (_toggle is not null)
            _toggle.Click += OnToggle;
        ShowOrientation();
        ShowOpen();
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty)
        {
            ShowStates();
            BringCurrentIntoView();
        }
        else if (change.Property == OrientationProperty || change.Property == WrapsProperty)
        {
            ShowOrientation();
            Build();
        }
        else if (change.Property == IsOpenProperty || change.Property == OpenTextProperty || change.Property == CloseTextProperty)
            ShowOpen();
    }

    private void OnToggle(object? sender, RoutedEventArgs e) => SetCurrentValue(IsOpenProperty, !IsOpen);

    // the items' panel for the orientation: a stack (a list, or a strip scrolling sideways), or a wrap (a strip on more rows, scrolling down)
    private void ShowOrientation()
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var wraps = horizontal && Wraps;
        PseudoClasses.Set(":horizontal", horizontal);
        PseudoClasses.Set(":wraps", wraps);
        if (_host is not null)
        {
            Panel panel = wraps ? new WrapPanel { ItemSpacing = 6, LineSpacing = 6 } : new StackPanel { Orientation = Orientation, Spacing = horizontal ? 6 : 2 };
            panel.Name = "PART_Items";
            panel.ClipToBounds = false;
            panel.Margin = horizontal ? new Thickness(2, 6) : default;
            _host.Child = _items = panel;
        }
        if (_scroller is not null)
        {
            _scroller.HorizontalScrollBarVisibility = horizontal && !wraps ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled;
            _scroller.VerticalScrollBarVisibility = horizontal && !wraps ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        }
    }

    private void ShowOpen()
    {
        PseudoClasses.Set(":closed", !IsOpen);
        var words = IsOpen ? CloseText : OpenText;
        if (_toggle is not null)
        {
            ToolTip.SetTip(_toggle, words);
            AutomationProperties.SetName(_toggle, words);
        }
        if (_toggleIcon is not null)
            _toggleIcon.Data = IsOpen ? FoldGlyph : UnfoldGlyph;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (NumberListItem item in e.OldItems)
                item.PropertyChanged -= OnItemChanged;
        if (e.NewItems is not null)
            foreach (NumberListItem item in e.NewItems)
                item.PropertyChanged += OnItemChanged;
        if (e.Action == NotifyCollectionChangedAction.Reset)
            foreach (var item in Items)
            {
                item.PropertyChanged -= OnItemChanged;
                item.PropertyChanged += OnItemChanged;
            }
        Build();
    }

    private void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is NumberListItem item && Items.IndexOf(item) is var i and >= 0 && i < _cells.Count)
            Fill(_cells[i], item, i);
    }

    // one cell per item: a bare button holding the number's badge (its press layer over it, its flag on its corner) and, in the vertical list, the item's line
    private void Build()
    {
        if (_items is null)
            return;
        _items.Children.Clear();
        _cells.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            var cell = MakeCell(i);
            _cells.Add(cell);
            _items.Children.Add(cell);
            Fill(cell, Items[i], i);
        }
    }

    private Button MakeCell(int index)
    {
        var number = new TextBlock { Classes = { "number-text" }, FontFeatures = FontFeatureCollection.Parse("+tnum"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var badge = new Border { Classes = { "number-badge" }, Child = number };
        // the strip's press face: over the badge and exactly its box (the badge's own fill would hide a layer under it)
        var press = new Border { Classes = { "number-press" }, IsHitTestVisible = false };
        var flag = new Icon { Classes = { "number-flag" }, Data = FlagGlyph, IsFilled = true, IsVisible = false, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
        // the flag's halo: the same glyph under it, drawn wider in a colour set apart from the flag's, so the flag reads on every badge's fill and ring
        var halo = new Icon { Classes = { "number-flag-halo" }, Data = FlagGlyph, IsFilled = true, IsVisible = false, IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
        // the mark is laid out left to right in either reading direction, so the flag stands on the badge's right on the screen (a number reads the same both ways)
        var mark = new Panel { Classes = { "number-mark" }, ClipToBounds = false, FlowDirection = FlowDirection.LeftToRight, Children = { badge, press, halo, flag } };
        var text = new TextBlock { Classes = { "number-line" }, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        var row = new DockPanel { Classes = { "number-row" }, ClipToBounds = false };
        DockPanel.SetDock(mark, Dock.Left);
        row.Children.Add(mark);
        row.Children.Add(text);
        var cell = new Button { Classes = { "bare", "number-cell" }, Content = row, Tag = index, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        // the strip's number shows its press on the layer over its badge: the cell's own ground stays clear in every state (a local value, over the bare button's pressed ground, which would lie under the badge's fill)
        if (Orientation == Orientation.Horizontal)
            cell.Background = Brushes.Transparent;
        cell.Click += OnCellClick;
        cell.KeyDown += OnCellKey;
        return cell;
    }

    private void Fill(Button cell, NumberListItem item, int index)
    {
        var row = (DockPanel)cell.Content!;
        var mark = (Panel)row.Children[0];
        ((TextBlock)((Border)mark.Children[0]).Child!).Text = item.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        mark.Children[2].IsVisible = item.IsFlagged; // the halo
        mark.Children[3].IsVisible = item.IsFlagged;
        var line = (TextBlock)row.Children[1];
        line.Text = item.Text;
        line.IsVisible = !string.IsNullOrEmpty(item.Text) && Orientation == Orientation.Vertical; // the strip shows the number alone
        cell.Classes.Set("done", item.State == NumberState.Done);
        cell.Classes.Set("right", item.State == NumberState.Right);
        cell.Classes.Set("wrong", item.State == NumberState.Wrong);
        cell.Classes.Set("flagged", item.IsFlagged);
        cell.Classes.Set("current", index == SelectedIndex);
        AutomationProperties.SetName(cell, string.IsNullOrEmpty(item.Name) ? item.Number.ToString(System.Globalization.CultureInfo.InvariantCulture) : item.Name);
    }

    private void ShowStates()
    {
        for (var i = 0; i < _cells.Count; i++)
            _cells[i].Classes.Set("current", i == SelectedIndex);
    }

    // the current number with the next one whole and a part of the one after it in view, so the list never ends on a cell's edge while more follow; the scroll moves only when that is not in view already (a list the user scrolled stays where it is until the current one nears its visible end)
    private void BringCurrentIntoView()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _cells.Count)
            return;
        var current = _cells[SelectedIndex];
        if (current.GetVisualParent() is not Control panel || _cells.Any(c => !ReferenceEquals(c.GetVisualParent(), panel)))
        {
            Glide(() => current.BringIntoView());
            return;
        }
        var room = current.Bounds;
        if (SelectedIndex + 1 < _cells.Count)
            room = room.Union(_cells[SelectedIndex + 1].Bounds);
        if (SelectedIndex + 2 < _cells.Count)
        {
            var after = _cells[SelectedIndex + 2].Bounds;
            var vertical = Orientation == Orientation.Vertical;
            room = room.Union(vertical ? new Rect(after.X, after.Y, after.Width, after.Height * PeekShare) : new Rect(after.X, after.Y, after.Width * PeekShare, after.Height));
        }
        Glide(() => panel.BringIntoView(room));
    }

    // the list's scroll moves to where bringing the item into view puts it, gliding there on the motion tokens' curve and time (at once in a still window)
    private void Glide(Action bring)
    {
        StopGlide();
        if (_scroller is not { } scroll)
        {
            bring();
            return;
        }
        var from = scroll.Offset;
        bring();
        var to = scroll.Offset;
        if (to == from)
            return;
        scroll.Offset = from;
        _glide = FrameTween.Run(this, Motion.Enter(this), Motion.Ease(this), t => scroll.Offset = from + (to - from) * t, () => _glide = null);
    }

    private void StopGlide()
    {
        _glide?.Stop();
        _glide = null;
    }

    // how much of the number after the next one shows: enough to tell that more follow
    private const double PeekShare = 0.4;

    private void OnCellClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int index })
            Invoke(index);
    }

    private void Invoke(int index)
    {
        SetCurrentValue(SelectedIndexProperty, index);
        RaiseEvent(new RoutedEventArgs(ItemInvokedEvent, this));
    }

    // the arrows move the focus between the cells: up and down in the list; in the strip the reading direction's way (in right-to-left, Left is the next one)
    private void OnCellKey(object? sender, KeyEventArgs e)
    {
        if (sender is not Button { Tag: int index } || _cells.Count == 0)
            return;
        var horizontal = Orientation == Orientation.Horizontal;
        var forward = horizontal && FlowDirection == FlowDirection.RightToLeft ? Key.Left : horizontal ? Key.Right : Key.Down;
        var back = horizontal && FlowDirection == FlowDirection.RightToLeft ? Key.Right : horizontal ? Key.Left : Key.Up;
        int? to = e.Key == forward ? Math.Min(index + 1, _cells.Count - 1)
            : e.Key == back ? Math.Max(index - 1, 0)
            : e.Key == Key.Home ? 0
            : e.Key == Key.End ? _cells.Count - 1
            : null;
        if (to is not { } target)
            return;
        e.Handled = true;
        _cells[target].Focus(NavigationMethod.Directional);
        _cells[target].BringIntoView();
    }

    // A drag of the list: it drags itself, finger and mouse alike: the platform's touch scroll does not move a sideways scroller in a right-to-left window, and a mouse has none. A drag across the list (up or down on the strip) moves the scroller around it instead: the strip's own scroll recognizer would hold that finger and the page under the strip would not move.

    private bool Sideways => Orientation == Orientation.Horizontal && !Wraps;

    private static double Room(ScrollViewer s, bool alongX) => alongX ? s.Extent.Width - s.Viewport.Width : s.Extent.Height - s.Viewport.Height;

    // the nearest scroller around the list that can move along that axis
    private ScrollViewer? Around(bool alongX)
    {
        for (var v = this.GetVisualParent(); v is not null; v = v.GetVisualParent())
            if (v is ScrollViewer s && Room(s, alongX) > 0.5)
                return s;
        return null;
    }

    // a press inside the rows (not the head's button): it may become a drag; a fling under way stops
    private void OnDragPressed(object? sender, PointerPressedEventArgs e)
    {
        StopFling();
        StopGlide();
        if (_scroller is null || e.Source is not Visual source || !_scroller.IsVisualAncestorOf(source))
            return;
        if (e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        _pointer = e.Pointer;
        _target = null;
        _from = e.GetPosition(_scroller);
        _decided = false;
    }

    // past the slop the press is a drag: the press under it lets go (nothing is picked) and the list (along its scroll) or the scroller around it (across) follows the pointer; with nothing to move that way the press lets go all the same and the pointer is left to whatever takes it
    private void OnDragMoved(object? sender, PointerEventArgs e)
    {
        if (_scroller is null || !ReferenceEquals(e.Pointer, _pointer))
            return;
        if (!_decided)
        {
            var d = e.GetPosition(_scroller) - _from;
            var along = Math.Abs(Sideways ? d.X : d.Y);
            var across = Math.Abs(Sideways ? d.Y : d.X);
            // still a tap (a scroller's own recognizer waits longer than the slop, so the list decides first)
            if (Math.Max(along, across) <= Touch.Slop)
                return;
            _decided = true;
            var alongX = along >= across ? Sideways : !Sideways;
            _target = along >= across && Room(_scroller, alongX) > 0.5 ? _scroller : Around(alongX);
            if (_target is null)
            {
                _pointer = null;
                if (e.Pointer.Captured is Visual held && (held == this || this.IsVisualAncestorOf(held)))
                    e.Pointer.Capture(null);
                return;
            }
            _alongX = alongX;
            e.Pointer.Capture(_scroller); // the cell under the pointer loses it: no click
            // the scroller moves by the way past the slop, so it starts from where it is rather than jump
            var at = e.GetPosition(_target);
            var way = alongX ? d.X : d.Y;
            var past = way - Math.Sign(way) * Touch.Slop;
            _from = alongX ? new Point(at.X - past, at.Y) : new Point(at.X, at.Y - past);
            _fromOffset = _target.Offset;
            _last = _before = (_from, e.Timestamp);
        }
        if (_target is null)
            return;
        e.PreventGestureRecognition(); // the drag is the list's: no scroller's own recognizer takes this press now
        e.Handled = true;
        var now = e.GetPosition(_target);
        var by = now - _from;
        ScrollTo(_target, _alongX ? _fromOffset - new Vector(by.X, 0) : _fromOffset - new Vector(0, by.Y));
        _before = _last;
        _last = (now, e.Timestamp);
    }

    private void OnDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!ReferenceEquals(e.Pointer, _pointer))
            return;
        var target = _target;
        var (last, before) = (_last, _before);
        _pointer = null;
        _target = null;
        _decided = false;
        if (target is null)
            return;
        e.Handled = true;
        if (ReferenceEquals(e.Pointer.Captured, _scroller))
            e.Pointer.Capture(null);
        // a finger let go while moving: the scroller goes on and slows down
        var ms = (double)(last.Time - before.Time);
        if (e.Pointer.Type == PointerType.Touch && ms is > 0 and < 100)
        {
            var v = (last.At - before.At) / ms;
            var speed = _alongX ? new Vector(-v.X, 0) : new Vector(0, -v.Y);
            if (Math.Abs(speed.X) + Math.Abs(speed.Y) > FlingStop * 4)
                StartFling(target, speed);
        }
    }

    // the drag's capture went elsewhere (a window lost the pointer): the drag ends
    private void OnDragLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_target is not null && ReferenceEquals(e.Pointer, _pointer) && ReferenceEquals(e.Source, _scroller))
        {
            _pointer = null;
            _target = null;
            _decided = false;
        }
    }

    private static void ScrollTo(ScrollViewer target, Vector offset)
    {
        var max = new Vector(Math.Max(0, Room(target, true)), Math.Max(0, Room(target, false)));
        target.Offset = new Vector(Math.Clamp(offset.X, 0, max.X), Math.Clamp(offset.Y, 0, max.Y));
    }

    private void StartFling(ScrollViewer target, Vector speed)
    {
        _flingTarget = target;
        _flingSpeed = speed;
        _fling ??= new DispatcherTimer(TimeSpan.FromMilliseconds(FlingFrame), DispatcherPriority.Render, OnFlingTick);
        _fling.Start();
    }

    private void OnFlingTick(object? sender, EventArgs e)
    {
        if (_flingTarget is not { } target || !this.IsAttachedToVisualTree() || Math.Abs(_flingSpeed.X) + Math.Abs(_flingSpeed.Y) < FlingStop)
        {
            StopFling();
            return;
        }
        var before = target.Offset;
        ScrollTo(target, before + _flingSpeed * FlingFrame);
        _flingSpeed *= FlingKeep;
        if (target.Offset == before)
            StopFling(); // at an end
    }

    private void StopFling()
    {
        _fling?.Stop();
        _flingTarget = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopFling();
        StopGlide();
        _pointer = null;
        _target = null;
        _decided = false;
    }

    /// <summary>For the tests: the cell built for an item.</summary>
    internal Button? CellAt(int index) => index >= 0 && index < _cells.Count ? _cells[index] : null;
}
