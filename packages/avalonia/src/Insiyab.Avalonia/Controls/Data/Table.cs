using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A data table, the web's <c>.ins-table</c> in its <c>.ins-table-wrap</c>: a header bar over a card of rows; classes <c>sticky</c> (header kept in view inside MaxHeight, 32rem unless set) and <c>pin</c> (first column kept in view, as <see cref="FrozenColumns"/> 1).</summary>
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[TemplatePart("PART_Head", typeof(Control))]
[TemplatePart("PART_Guide", typeof(Control))]
[TemplatePart("PART_MarkerLayer", typeof(Canvas))]
[TemplatePart("PART_HeadCells", typeof(ColumnsPanel))]
[TemplatePart("PART_Rows", typeof(Panel))]
[PseudoClasses(":empty", ":selectable", ":in-panel", ":flush", ":cell-cursor", ":keyboard")]
public partial class Table : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<Table, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> IsSelectableProperty =
        AvaloniaProperty.Register<Table, bool>(nameof(IsSelectable));

    public static readonly StyledProperty<bool> ShowSelectAllProperty =
        AvaloniaProperty.Register<Table, bool>(nameof(ShowSelectAll), true);

    public static readonly StyledProperty<object?> EmptyProperty =
        AvaloniaProperty.Register<Table, object?>(nameof(Empty));

    public static readonly StyledProperty<double> TableMinWidthProperty =
        AvaloniaProperty.Register<Table, double>(nameof(TableMinWidth));

    public static readonly StyledProperty<TableSelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<Table, TableSelectionMode>(nameof(SelectionMode), TableSelectionMode.Single);

    public static readonly StyledProperty<ContextMenu?> RowContextMenuProperty =
        AvaloniaProperty.Register<Table, ContextMenu?>(nameof(RowContextMenu));

    public static readonly StyledProperty<bool> CanResizeColumnsProperty =
        AvaloniaProperty.Register<Table, bool>(nameof(CanResizeColumns), true);

    public static readonly StyledProperty<bool> IsCellNavigationProperty =
        AvaloniaProperty.Register<Table, bool>(nameof(IsCellNavigation));

    public static readonly StyledProperty<int> FrozenColumnsProperty =
        AvaloniaProperty.Register<Table, int>(nameof(FrozenColumns));

    public static readonly DirectProperty<Table, TableRow?> CurrentRowProperty =
        AvaloniaProperty.RegisterDirect<Table, TableRow?>(nameof(CurrentRow), o => o.CurrentRow);

    public static readonly DirectProperty<Table, int> CurrentColumnProperty =
        AvaloniaProperty.RegisterDirect<Table, int>(nameof(CurrentColumn), o => o.CurrentColumn, unsetValue: -1);

    private readonly List<TableRow> _display = [];
    private readonly List<TableRow> _generated = [];
    private ScrollViewer? _scroll;
    private Control? _head;
    private Border? _card;
    private Control? _guide;
    private Canvas? _markerLayer;
    private TableRow? _marked;
    private MarkerFlight? _markerFlight;
    private ColumnsPanel? _headCells;
    private Panel? _rows;
    private CheckBox? _all;
    private bool _syncing;
    private bool _syncPending;
    private TableRow? _anchor;
    private TableRow? _current;
    private int _currentColumn = -1;
    private bool _shift;
    private bool _inPanel;
    private int _pinned;
    private double _pinX = double.NaN;

    static Table()
    {
        FocusableProperty.OverrideDefaultValue<Table>(true);
    }

    public Table()
    {
        ColumnWidths = new ColumnLayout { IsFit = IsFitColumn, KeepsDetached = true };
        Columns.CollectionChanged += (_, _) =>
        {
            ColumnWidths.ClearWidths();
            Rebuild();
        };
        Rows.CollectionChanged += (_, _) => Rebuild();
        Classes.CollectionChanged += (_, e) =>
        {
            if (Touches(e, "sticky") || Touches(e, "pin")) Dispatcher.UIThread.Post(UpdateSticky, DispatcherPriority.Render);
        };
        AddHandler(ContextRequestedEvent, OnContextRequested);
    }

    private static bool Touches(NotifyCollectionChangedEventArgs e, string name) =>
        e.Action == NotifyCollectionChangedAction.Reset || (e.NewItems?.Contains(name) ?? false) || (e.OldItems?.Contains(name) ?? false);

    /// <summary>The columns, in order.</summary>
    public AvaloniaList<TableColumn> Columns { get; } = [];

    /// <summary>The rows written out by hand. Ignored while <see cref="ItemsSource"/> is set.</summary>
    [Content]
    public AvaloniaList<TableRow> Rows { get; } = [];

    /// <summary>Items to make one row each from, with each column's <see cref="TableColumn.CellText"/> or <see cref="TableColumn.CellTemplate"/>.</summary>
    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Adds a column of checkboxes that select rows: the web's <c>.ins-table-check</c> with <c>data-ins-select</c>.</summary>
    public bool IsSelectable { get => GetValue(IsSelectableProperty); set => SetValue(IsSelectableProperty, value); }

    /// <summary>A checkbox in the header that selects every row, checked, mixed or clear to match them: <c>data-ins-select="all"</c>.</summary>
    public bool ShowSelectAll { get => GetValue(ShowSelectAllProperty); set => SetValue(ShowSelectAllProperty, value); }

    /// <summary>What the table shows when it has no rows: an empty state, in place of the header and the rows.</summary>
    public object? Empty { get => GetValue(EmptyProperty); set => SetValue(EmptyProperty, value); }

    /// <summary>The narrowest the table gets before its box scrolls sideways: the web's <c>min-inline-size</c> on the table.</summary>
    public double TableMinWidth { get => GetValue(TableMinWidthProperty); set => SetValue(TableMinWidthProperty, value); }

    /// <summary>How clicks and keys select rows; Extended whenever IsSelectable is on.</summary>
    public TableSelectionMode SelectionMode { get => GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }

    /// <summary>The menu a right-click or the Menu key opens on a row.</summary>
    public ContextMenu? RowContextMenu { get => GetValue(RowContextMenuProperty); set => SetValue(RowContextMenuProperty, value); }

    /// <summary>Lets the user drag a heading's trailing edge to size its column.</summary>
    public bool CanResizeColumns { get => GetValue(CanResizeColumnsProperty); set => SetValue(CanResizeColumnsProperty, value); }

    /// <summary>Moves the keyboard from cell to cell as a spreadsheet does: arrows, Home/End in the row, Ctrl+Home/End to the corners, Tab across cells, Enter, F2 or typing to edit.</summary>
    public bool IsCellNavigation { get => GetValue(IsCellNavigationProperty); set => SetValue(IsCellNavigationProperty, value); }

    /// <summary>How many leading columns stay in place while the rows scroll sideways under them; the selection column stays with them.</summary>
    public int FrozenColumns { get => GetValue(FrozenColumnsProperty); set => SetValue(FrozenColumnsProperty, value); }

    /// <summary>The row the keyboard is on.</summary>
    public TableRow? CurrentRow => _current;

    /// <summary>The column of the keyboard's cell, an index into Columns; -1 unless a column is editable or <see cref="IsCellNavigation"/> is on.</summary>
    public int CurrentColumn => _currentColumn;

    /// <summary>The widths the header and every row share.</summary>
    internal ColumnLayout ColumnWidths { get; }

    /// <summary>The rows in the order they are shown, after any sort.</summary>
    public IReadOnlyList<TableRow> DisplayedRows => _display;

    /// <summary>The selected rows, in the order they are shown.</summary>
    public IReadOnlyList<TableRow> SelectedRows => _display.Where(r => r.IsSelected).ToList();

    /// <summary>Raised before a sort. Cancel it when the server sorts, and only the arrow moves: the web's <c>ins:sort</c>.</summary>
    public event EventHandler<TableSortingEventArgs>? Sorting;

    /// <summary>Raised before an edited cell keeps its new value: the web's <c>ins:edit</c>.</summary>
    public event EventHandler<TableEditEventArgs>? CellEditing;

    /// <summary>Raised on every change to the selection: the web's <c>ins:select</c>.</summary>
    public event EventHandler<TableSelectionEventArgs>? SelectionChanged;

    /// <summary>Raised by Enter or a double-click on a cell that is not editable: the desktop's "open".</summary>
    public event EventHandler<TableRowEventArgs>? RowActivated;

    private int Offset => IsSelectable ? 1 : 0;

    private TableSelectionMode Mode => IsSelectable ? TableSelectionMode.Extended : SelectionMode;

    private bool HasEditable => Columns.Any(c => c.Edit != TableEdit.None);

    private int FirstEditable => Columns.ToList().FindIndex(c => c.Edit != TableEdit.None);

    private bool CellCursor => (IsCellNavigation || HasEditable) && Columns.Count > 0;

    private int PinnedCells => FrozenColumns > 0 ? Math.Min(FrozenColumns, Columns.Count) + Offset : Classes.Contains("pin") ? 1 : 0;

    internal TableCell? CurrentCell => _current is { } r && _currentColumn >= 0 && _currentColumn + Offset < r.Cells.Count ? r.Cells[_currentColumn + Offset] : null;

    private bool IsFitColumn(int i) => IsSelectable && i == 0 || (i - Offset >= 0 && i - Offset < Columns.Count && Columns[i - Offset].IsFit);

    internal void RaiseCellEditing(TableEditEventArgs args) => CellEditing?.Invoke(this, args);

    internal Control? Guide => _guide;

    /// <summary>The card's inner corner radius, which the corner cells' focus rings follow.</summary>
    internal double CardCorner => _card is { } card ? Math.Max(0, card.CornerRadius.TopLeft - card.BorderThickness.Left) : 0;

    // A panel or a flush body changes the card's corner: the corner cells follow it.
    private void OnCardChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Border.CornerRadiusProperty && e.Property != Border.BorderThicknessProperty || _display.Count == 0) return;
        _display[0].RoundCells();
        _display[^1].RoundCells();
    }

    // While a column is dragged wider, a line runs down the table at the edge being moved; null hides it.
    internal void ShowGuide(TableHeader? header)
    {
        if (_guide is null) return;
        _guide.IsVisible = header is not null;
        if (header is null || _guide.GetVisualParent() is not Visual box) return;
        header.UpdateLayout();
        var edge = header.IsGripFlipped ? 0 : header.Bounds.Width;
        if (header.TranslatePoint(new Point(edge, 0), box) is { } at) _guide.RenderTransform = new TranslateTransform(Math.Round(at.X - _guide.Bounds.Width / 2), 0);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_scroll is not null) _scroll.ScrollChanged -= OnScroll;
        if (_card is not null)
        {
            _card.SizeChanged -= OnCardSized;
            _card.PropertyChanged -= OnCardChanged;
        }
        if (_rows is TableRowsPanel oldRows) oldRows.Realized -= OnRealized;
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _head = e.NameScope.Find<Control>("PART_Head");
        _card = e.NameScope.Find<Border>("PART_Card");
        if (_card is not null)
        {
            _card.SizeChanged += OnCardSized;
            _card.PropertyChanged += OnCardChanged;
        }
        _guide = e.NameScope.Find<Control>("PART_Guide");
        _markerLayer = e.NameScope.Find<Canvas>("PART_MarkerLayer");
        _headCells = e.NameScope.Find<ColumnsPanel>("PART_HeadCells");
        _rows = e.NameScope.Find<Panel>("PART_Rows");
        if (_scroll is not null) _scroll.ScrollChanged += OnScroll;
        if (_rows is TableRowsPanel rowsPanel) rowsPanel.Realized += OnRealized;
        if (_headCells is not null) _headCells.Layout = ColumnWidths;
        Rebuild();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var flush = false;
        _inPanel = false;
        foreach (var a in this.GetVisualAncestors())
        {
            if (a is not Border b) continue;
            if (b.Classes.Contains("panel-body") && b.Classes.Contains("flush")) flush = true;
            if (b.Classes.Contains("panel")) { _inPanel = true; break; }
        }
        PseudoClasses.Set(":in-panel", _inPanel);
        PseudoClasses.Set(":flush", flush);
        foreach (var row in _display) row.SetInPanel(_inPanel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged old) old.CollectionChanged -= OnItemsChanged;
            if (change.NewValue is INotifyCollectionChanged now) now.CollectionChanged += OnItemsChanged;
            Rebuild();
        }
        else if (change.Property == IsSelectableProperty || change.Property == ShowSelectAllProperty || change.Property == EmptyProperty
            || change.Property == CanResizeColumnsProperty || change.Property == SelectionModeProperty || change.Property == IsCellNavigationProperty)
        {
            Rebuild();
        }
        else if (change.Property == FrozenColumnsProperty)
        {
            Dispatcher.UIThread.Post(UpdateSticky, DispatcherPriority.Render);
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Generate()
    {
        _generated.Clear();
        if (ItemsSource is null) return;
        foreach (var item in ItemsSource)
        {
            var row = new TableRow { Item = item };
            foreach (var column in Columns)
                row.Cells.Add(column.CellTemplate is { } template
                    ? new TableCell { Content = item, ContentTemplate = template }
                    : new TableCell { Content = column.CellText?.Invoke(item) });
            _generated.Add(row);
        }
    }

    /// <summary>Lays the header and the rows out again from <see cref="Columns"/> and <see cref="Rows"/> (or <see cref="ItemsSource"/>).</summary>
    public void Rebuild()
    {
        if (_headCells is null || _rows is null) return;
        if (ItemsSource is not null) Generate();
        var source = ItemsSource is not null ? _generated : (IList<TableRow>)Rows;

        var shown = _display.Select(KeyOf).ToHashSet();
        foreach (var old in _display) old.Attach(null);
        _display.Clear();
        if (_rows is not TableRowsPanel) _rows.Children.Clear();
        _headCells.Children.Clear();
        _all = null;
        PseudoClasses.Set(":selectable", IsSelectable);

        if (IsSelectable)
        {
            var head = new TableHeader { Owner = this, Index = 0 };
            head.MarkCheck();
            if (ShowSelectAll)
            {
                _all = new CheckBox { Classes = { "table-check" }, Focusable = false };
                AutomationProperties.SetName(_all, Word("selectAll"));
                _all.Click += OnAllClick;
                head.Content = _all;
            }
            _headCells.Children.Add(head);
        }
        foreach (var column in Columns)
        {
            var head = new TableHeader { Column = column, Owner = this, Index = _headCells.Children.Count };
            foreach (var cls in column.ClassList()) head.Classes.Add(cls);
            head.Content = column.Sort == TableSort.None ? column.Header : SortButton(column);
            _headCells.Children.Add(head);
        }
        var heads = _headCells.Children.OfType<TableHeader>().ToList();
        for (var i = 0; i < heads.Count; i++)
            heads[i].SetResizable(CanResizeColumns && heads[i].Column is not null && i < heads.Count - 1);

        foreach (var row in source)
        {
            if (IsSelectable && (row.Cells.Count == 0 || !row.Cells[0].IsCheck)) row.Cells.Insert(0, CheckCell(row));
            if (!IsSelectable && row.Cells.Count > 0 && row.Cells[0].IsCheck) row.Cells.RemoveAt(0);
            for (var i = 0; i < row.Cells.Count; i++)
            {
                var cell = row.Cells[i];
                var column = i - Offset >= 0 && i - Offset < Columns.Count ? Columns[i - Offset] : null;
                cell.Column = column;
                if (column is not null)
                    foreach (var cls in column.ClassList())
                        if (!cell.Classes.Contains(cls)) cell.Classes.Add(cls);
            }
            row.Attach(this);
            row.SetInPanel(_inPanel);
            _display.Add(row);
            if (_rows is not TableRowsPanel) _rows.Children.Add(row);
        }
        if (_rows is TableRowsPanel panel) panel.SetRows(_display);
        ColumnWidths.Keep(_display.Select(r => r.CellsPanel));
        _pinX = double.NaN;

        Place();
        Arrive(shown);
        PseudoClasses.Set(":empty", _display.Count == 0 && Empty is not null);
        PseudoClasses.Set(":cell-cursor", CellCursor);
        if (_current is null || !_display.Contains(_current))
        {
            SetCurrent(null, -1);
            _anchor = null;
        }
        else
        {
            SetCurrent(_current, _currentColumn);
        }
        ColumnWidths.Invalidate();
        SyncSelection(true);
        Dispatcher.UIThread.Post(UpdateSticky, DispatcherPriority.Render);
    }

    private void Place()
    {
        for (var i = 0; i < _display.Count; i++) _display[i].SetPlace(i, _display.Count);
    }

    private static object KeyOf(TableRow row) => row.Item ?? row;

    // Rows new to the table rise in: on a fresh table every row in view, the step shrinking from 40ms so the last starts by 320ms, rows out of view simply appear; a sort never replays this.
    private void Arrive(HashSet<object> shown)
    {
        if (Motion.IsStill(this)) return;
        var fresh = _display.Where(r => !shown.Contains(KeyOf(r))).ToList();
        if (fresh.Count == 0) return;
        if (fresh.Count == _display.Count) { StaggerInView(fresh); return; }
        for (var i = 0; i < _display.Count; i++)
            if (!shown.Contains(KeyOf(_display[i]))) Rise(_display[i], i < 6 ? i * 40 : 0);
    }

    // Waits for the first layout that places the rows, then staggers the ones in view.
    private void StaggerInView(List<TableRow> rows)
    {
        var asked = Environment.TickCount64;
        void Placed(object? sender, EventArgs e)
        {
            var seen = rows.Where(r => InView(r) && Seen.Now(r)).ToList();
            if (seen.Count == 0 && Environment.TickCount64 - asked < 1000) return;
            LayoutUpdated -= Placed;
            var step = seen.Count > 1 ? Math.Min(40, 320.0 / (seen.Count - 1)) : 0;
            for (var k = 0; k < seen.Count; k++) Entrance.Play(seen[k], EntranceEffect.Rise, k * step);
        }
        LayoutUpdated += Placed;
    }

    // A row added while the template is still being built is not in the tree yet, and an entrance needs its frames, so it starts when the row arrives there; a row out of view that arrives later, scrolled to, simply appears.
    private void Rise(TableRow row, double delay)
    {
        if (InView(row))
        {
            Entrance.Play(row, EntranceEffect.Rise, delay);
            return;
        }
        var asked = Environment.TickCount64;
        void Attached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            row.AttachedToVisualTree -= Attached;
            if (Environment.TickCount64 - asked < 1000) Entrance.Play(row, EntranceEffect.Rise, delay);
        }
        row.AttachedToVisualTree += Attached;
    }

    // Whether the row is in the tree and shown; the rows panel keeps rows it has just scrolled past in the tree, hidden.
    private bool InView(TableRow row) => _rows is TableRowsPanel panel ? panel.InView(row) : row.IsAttachedToVisualTree();

    private string Word(string key)
    {
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        return key switch
        {
            "selectAll" => rtl ? "تحديد الكل" : "Select all",
            "selectRow" => rtl ? "تحديد الصف" : "Select row",
            _ => key,
        };
    }

    private TableSortButton SortButton(TableColumn column)
    {
        var button = new TableSortButton { Content = column.Header, Direction = column.SortDirection };
        column.PropertyChanged += (_, e) =>
        {
            if (e.Property == TableColumn.SortDirectionProperty) button.Direction = column.SortDirection;
        };
        button.Click += (_, _) => Sort(column);
        return button;
    }

    private TableCell CheckCell(TableRow row)
    {
        var box = new CheckBox { Classes = { "table-check" }, IsChecked = row.IsSelected, Focusable = false };
        AutomationProperties.SetName(box, Word("selectRow"));
        box.AddHandler(PointerPressedEvent, (_, e) => _shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift), RoutingStrategies.Tunnel);
        box.AddHandler(KeyDownEvent, (_, e) => _shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift), RoutingStrategies.Tunnel);
        box.Click += (_, _) => OnBoxClick(row, box.IsChecked == true);
        row.PropertyChanged += (_, e) =>
        {
            if (e.Property == TableRow.IsSelectedProperty) box.IsChecked = row.IsSelected;
        };
        var cell = new TableCell { Content = box };
        cell.MarkCheck();
        return cell;
    }

    private void OnBoxClick(TableRow row, bool on)
    {
        var shift = _shift;
        _shift = false;
        var from = _anchor is null ? -1 : _display.IndexOf(_anchor);
        var to = _display.IndexOf(row);
        _syncing = true;
        if (shift && from >= 0 && to >= 0)
            for (var i = Math.Min(from, to); i <= Math.Max(from, to); i++) _display[i].IsSelected = on;
        else
            row.IsSelected = on;
        _syncing = false;
        _anchor = row;
        SyncSelection(false);
        SetCurrent(row, _currentColumn);
    }

    private void OnAllClick(object? sender, RoutedEventArgs e)
    {
        var all = _display.Count > 0 && _display.All(r => r.IsSelected);
        SelectAll(!all);
    }

    /// <summary>Selects every row, or clears the selection.</summary>
    public void SelectAll(bool on)
    {
        _syncing = true;
        foreach (var row in _display) row.IsSelected = on;
        _syncing = false;
        SyncSelection(false);
    }

    internal void OnRowSelectionChanged(TableRow row)
    {
        if (_syncing || _syncPending) return;
        _syncPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _syncPending = false;
            SyncSelection(false);
        });
    }

    private void SyncSelection(bool quiet)
    {
        FollowMarker();
        var count = _display.Count(r => r.IsSelected);
        if (_all is not null)
            _all.IsChecked = count == 0 ? false : count == _display.Count ? true : null;
        if (!quiet || count > 0) SelectionChanged?.Invoke(this, new TableSelectionEventArgs(SelectedRows));
    }

    // When the one selected row changes, its marker flies to the new row as the sidebar's does, instead of one shrinking while the other grows.
    private void FollowMarker()
    {
        var selected = _display.Where(r => r.IsSelected).Take(2).ToList();
        var only = selected.Count == 1 ? selected[0] : null;
        var from = _marked;
        _marked = only;
        if (only is null || from is null || from == only || from.IsSelected || !_display.Contains(from) || _markerLayer is null) return;
        if (MarkerSpan(from) is not { } a || MarkerSpan(only) is not { } b) return;
        var start = (a.Y, a.Height);
        if (_markerFlight is { IsDone: false } previous)
        {
            start = (previous.Start, previous.Length);
            previous.Land();
        }
        var bar = new Border { Classes = { "table-marker-flight" } };
        Canvas.SetLeft(bar, b.X);
        var layer = _markerLayer;
        MarkerFlight? run = null;
        run = MarkerFlight.Run(this, start, (b.Y, b.Height), (y, h) =>
        {
            Canvas.SetTop(bar, y);
            bar.Height = h;
        }, () =>
        {
            layer.Children.Remove(bar);
            if (_markerFlight != run) return;
            _markerFlight = null;
            Classes.Remove("moving");
        });
        if (run is null) return;
        layer.Children.Add(bar);
        _markerFlight = run;
        Classes.Add("moving");
    }

    // A row's marker in the flight layer: 3px on its leading edge, inset as the row's own marker is.
    private Rect? MarkerSpan(TableRow row)
    {
        var m = row.MarkerMargin;
        var height = row.Bounds.Height - m.Top - m.Bottom;
        if (_markerLayer is null || !InView(row) || height <= 0 || row.TranslatePoint(new Point(0, m.Top), _markerLayer) is not { } p) return null;
        return new Rect(p.X, p.Y, 3, height);
    }

    internal void SetCurrent(TableRow? row, int column)
    {
        if (_current is { } oldRow)
        {
            oldRow.SetCurrent(false);
            CurrentCell?.SetCurrent(false);
        }
        SetAndRaise(CurrentRowProperty, ref _current, row);
        SetAndRaise(CurrentColumnProperty, ref _currentColumn, row is not null && CellCursor ? Math.Clamp(column, 0, Columns.Count - 1) : -1);
        (_rows as TableRowsPanel)?.Keep(_current);
        if (_current is null) return;
        _current.SetCurrent(true);
        CurrentCell?.SetCurrent(true);
        ScrollIntoView(_current);
        // A row brought into the tree just now is placed by the next layout, so it is checked again once that has run.
        if (_rows is TableRowsPanel && !InView(_current))
        {
            var pending = _current;
            Dispatcher.UIThread.Post(() => { if (_current == pending) ScrollIntoView(pending); }, DispatcherPriority.Loaded);
        }
        if (CurrentCell is { } cell) ScrollIntoView(cell);
    }

    // Sideways, the cell comes clear of the frozen columns as well as of the box's edges.
    private void ScrollIntoView(TableCell cell)
    {
        if (_scroll is null || _scroll.Extent.Width <= _scroll.Viewport.Width + 0.5 || cell.Bounds.Width <= 0 || cell.Row is not { } row) return;
        var pinned = PinnedCells;
        if (row.Cells.IndexOf(cell) < pinned) return;
        if (cell.TranslatePoint(default, _scroll) is not { } p) return;
        var frozen = 0.0;
        for (var i = 0; i < pinned && i < row.Cells.Count; i++) frozen += row.Cells[i].Bounds.Width;
        var view = _scroll.Viewport.Width;
        var right = p.X + cell.Bounds.Width;
        var dx = p.X < frozen ? p.X - frozen : right > view ? Math.Min(right - view, p.X - frozen) : 0;
        if (Math.Abs(dx) >= 0.5) _scroll.Offset = new Vector(Math.Max(0, _scroll.Offset.X + dx), _scroll.Offset.Y);
    }

    private void ScrollIntoView(TableRow row)
    {
        var sticky = Classes.Contains("sticky");
        var viewer = sticky ? _scroll : this.FindAncestorOfType<ScrollViewer>();
        if (viewer is null) return;
        double? top;
        double height;
        // A row out of view may not be in the tree, so its place comes from the rows panel.
        if (_rows is TableRowsPanel panel && panel.Rows.Count > 0)
        {
            top = panel.TranslatePoint(new Point(0, panel.OffsetOf(row)), viewer)?.Y;
            height = panel.HeightOf(row);
        }
        else
        {
            top = row.TranslatePoint(default, viewer)?.Y;
            height = row.Bounds.Height;
        }
        if (height <= 0 || top is not { } y) return;
        var head = sticky && _head is not null ? _head.Bounds.Height + 8 : 0;
        var dy = y < head ? y - head : y + height > viewer.Viewport.Height ? y + height - viewer.Viewport.Height : 0;
        if (dy != 0) viewer.Offset = new Vector(viewer.Offset.X, Math.Max(0, viewer.Offset.Y + dy));
    }

    private int PageSize()
    {
        var sticky = Classes.Contains("sticky");
        var viewer = sticky ? _scroll : this.FindAncestorOfType<ScrollViewer>();
        var rowHeight = _rows is TableRowsPanel panel && panel.Rows.Count > 0 ? panel.RowHeight : _display.Count > 0 ? _display[0].Bounds.Height : 0;
        var head = _head is not null ? _head.Bounds.Height + 8 : 0;
        if (viewer is null || viewer.Viewport.Height <= 0 || rowHeight <= 0) return 10;
        return Math.Max(1, (int)Math.Floor((viewer.Viewport.Height - head) / rowHeight) - 1);
    }

    private void SelectOnly(TableRow row)
    {
        _syncing = true;
        foreach (var r in _display) r.IsSelected = r == row;
        _syncing = false;
        SyncSelection(false);
    }

    private void ToggleRow(TableRow row)
    {
        _syncing = true;
        row.IsSelected = !row.IsSelected;
        _syncing = false;
        SyncSelection(false);
    }

    private void SelectRange(TableRow from, TableRow to, bool add)
    {
        var a = _display.IndexOf(from);
        var b = _display.IndexOf(to);
        if (a < 0) a = b;
        _syncing = true;
        for (var i = 0; i < _display.Count; i++)
        {
            var inside = i >= Math.Min(a, b) && i <= Math.Max(a, b);
            if (inside) _display[i].IsSelected = true;
            else if (!add) _display[i].IsSelected = false;
        }
        _syncing = false;
        SyncSelection(false);
    }

    private void MoveTo(int index, KeyModifiers mods, int? column = null)
    {
        if (_display.Count == 0) return;
        var row = _display[Math.Clamp(index, 0, _display.Count - 1)];
        var ctrl = mods.HasFlag(KeyModifiers.Control) || mods.HasFlag(KeyModifiers.Meta);
        var shift = mods.HasFlag(KeyModifiers.Shift);
        if (!ctrl)
        {
            if (shift && Mode == TableSelectionMode.Extended)
            {
                SelectRange(_anchor ?? row, row, false);
            }
            else
            {
                if (Mode != TableSelectionMode.None) SelectOnly(row);
                _anchor = row;
            }
        }
        SetCurrent(row, column ?? _currentColumn);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var row = (e.Source as Visual)?.FindAncestorOfType<TableRow>(true);
        if (row is null || row.Owner != this) return;
        var cell = (e.Source as Visual)?.FindAncestorOfType<TableCell>(true);
        var col = cell?.Column is { } c ? Columns.IndexOf(c) : -1;
        var editable = cell?.Column is { Edit: not TableEdit.None };
        var wasCurrent = cell is not null && cell == CurrentCell;
        e.Handled = true;
        PseudoClasses.Set(":keyboard", false);
        Focus(NavigationMethod.Pointer);
        if (e.ClickCount >= 2)
        {
            if (editable) cell!.BeginEdit();
            else RowActivated?.Invoke(this, new TableRowEventArgs(row));
            return;
        }
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (wasCurrent && editable && !ctrl && !shift)
        {
            cell!.BeginEdit();
            return;
        }
        switch (Mode)
        {
            case TableSelectionMode.Single:
                SelectOnly(row);
                _anchor = row;
                break;
            case TableSelectionMode.Extended:
                if (ctrl && shift)
                {
                    SelectRange(_anchor ?? row, row, true);
                }
                else if (ctrl)
                {
                    ToggleRow(row);
                    _anchor = row;
                }
                else if (shift)
                {
                    SelectRange(_anchor ?? row, row, false);
                }
                else
                {
                    SelectOnly(row);
                    _anchor = row;
                }
                break;
        }
        SetCurrent(row, CellCursor && col >= 0 ? col : _currentColumn);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (ReferenceEquals(e.Source, this) && e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional) PseudoClasses.Set(":keyboard", true);
        if (ReferenceEquals(e.Source, this) && _current is null && _display.Count > 0)
            SetCurrent(_display.FirstOrDefault(r => r.IsSelected) ?? _display[0], FirstEditable);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !ReferenceEquals(e.Source, this) || _display.Count == 0) return;
        PseudoClasses.Set(":keyboard", true);
        var i = _current is null ? -1 : _display.IndexOf(_current);
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var sheet = IsCellNavigation && CellCursor;
        switch (e.Key)
        {
            case Key.Home or Key.End when sheet && !ctrl:
                SetCurrent(_current ?? _display[0], e.Key == Key.Home ? 0 : Columns.Count - 1);
                e.Handled = true;
                break;
            case Key.Home or Key.End when sheet:
                MoveTo(e.Key == Key.Home ? 0 : _display.Count - 1, e.KeyModifiers & ~(KeyModifiers.Control | KeyModifiers.Meta), e.Key == Key.Home ? 0 : Columns.Count - 1);
                e.Handled = true;
                break;
            case Key.Tab when sheet && (e.KeyModifiers & ~KeyModifiers.Shift) == KeyModifiers.None:
                e.Handled = Step(i, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                break;
            case Key.Down:
                MoveTo(i + 1, e.KeyModifiers);
                e.Handled = true;
                break;
            case Key.Up:
                MoveTo(Math.Max(0, i - 1), e.KeyModifiers);
                e.Handled = true;
                break;
            case Key.Home:
                MoveTo(0, e.KeyModifiers);
                e.Handled = true;
                break;
            case Key.End:
                MoveTo(_display.Count - 1, e.KeyModifiers);
                e.Handled = true;
                break;
            case Key.PageDown:
                MoveTo(i + PageSize(), e.KeyModifiers);
                e.Handled = true;
                break;
            case Key.PageUp:
                MoveTo(Math.Max(0, i - PageSize()), e.KeyModifiers);
                e.Handled = true;
                break;
            case Key.Left or Key.Right when CellCursor:
                var forward = (e.Key == Key.Right) != rtl;
                SetCurrent(_current ?? _display[0], (_currentColumn < 0 ? 0 : _currentColumn) + (forward ? 1 : -1));
                e.Handled = true;
                break;
            case Key.Space when _current is not null && Mode != TableSelectionMode.None:
                if (Mode == TableSelectionMode.Extended) ToggleRow(_current);
                else SelectOnly(_current);
                _anchor = _current;
                e.Handled = true;
                break;
            case Key.A when ctrl && Mode == TableSelectionMode.Extended:
                SelectAll(true);
                e.Handled = true;
                break;
            case Key.F2 when CurrentCell is { Column.Edit: not TableEdit.None } cell:
                cell.BeginEdit();
                e.Handled = true;
                break;
            case Key.Enter when e.KeyModifiers == KeyModifiers.None && CurrentCell is { Column.Edit: not TableEdit.None } edit:
                edit.BeginEdit();
                e.Handled = true;
                break;
            case Key.Enter when _current is not null:
                RowActivated?.Invoke(this, new TableRowEventArgs(_current));
                e.Handled = true;
                break;
        }
    }

    // Tab's next cell, on into the next row at the end of one; false past the table's last cell, so focus leaves it.
    private bool Step(int row, bool back)
    {
        if (row < 0)
        {
            SetCurrent(_display[0], 0);
            return true;
        }
        var c = _currentColumn + (back ? -1 : 1);
        var r = row;
        if (c >= Columns.Count)
        {
            c = 0;
            r++;
        }
        if (c < 0)
        {
            c = Columns.Count - 1;
            r--;
        }
        if (r < 0 || r >= _display.Count) return false;
        if (r == row) SetCurrent(_current, c);
        else MoveTo(r, KeyModifiers.None, c);
        return true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Handled || !ReferenceEquals(e.Source, this)) return;
        if (e.Key != Key.Apps && !(e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.Shift)) return;
        e.Handled = true;
        OpenRowMenu(null);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (!e.Handled && ReferenceEquals(e.Source, this) && CurrentCell is { Column.Edit: TableEdit.Text or TableEdit.Number } cell
            && !string.IsNullOrEmpty(e.Text) && !char.IsControl(e.Text[0]) && e.Text != " ")
        {
            cell.BeginEdit(e.Text);
            e.Handled = true;
        }
    }

    internal void AfterEdit(TableCell cell, Key key, bool back)
    {
        Focus();
        if (cell.Row is not { } row) return;
        var r = _display.IndexOf(row);
        var c = cell.Column is { } col ? Columns.IndexOf(col) : _currentColumn;
        if (key == Key.Enter)
        {
            _currentColumn = c;
            MoveTo(Math.Min(r + 1, _display.Count - 1), KeyModifiers.None);
            return;
        }
        if (key != Key.Tab) return;
        var editable = Enumerable.Range(0, Columns.Count).Where(i => IsCellNavigation || Columns[i].Edit != TableEdit.None).ToList();
        var at = editable.IndexOf(c) + (back ? -1 : 1);
        if (at >= editable.Count)
        {
            at = 0;
            r++;
        }
        if (at < 0)
        {
            at = editable.Count - 1;
            r--;
        }
        if (r < 0 || r >= _display.Count)
        {
            SetCurrent(row, c);
            return;
        }
        SetCurrent(_display[r], editable[at]);
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        var row = (e.Source as Visual)?.FindAncestorOfType<TableRow>(true);
        var pointer = e.TryGetPosition(this, out _);
        if (row is null && !pointer) row = _current;
        if (row is null || row.Owner != this || RowContextMenu is null) return;
        e.Handled = true;
        OpenRowMenu(pointer ? row : null);
    }

    private void OpenRowMenu(TableRow? clicked)
    {
        if (RowContextMenu is not { } menu || menu.IsOpen) return;
        var row = clicked ?? _current;
        if (row is null) return;
        if (!row.IsSelected && Mode != TableSelectionMode.None)
        {
            SelectOnly(row);
            _anchor = row;
        }
        SetCurrent(row, _currentColumn);
        if (clicked is not null)
        {
            menu.Placement = PlacementMode.Pointer;
            menu.PlacementTarget = row;
        }
        else
        {
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = row.Cells.Count > Offset ? row.Cells[Offset] : row;
        }
        menu.Open(row);
    }

    /// <summary>Sorts the rows by a column: <paramref name="direction"/>, or the other way from now when it is <see cref="SortDirection.None"/>. Rows with nothing to sort by go last.</summary>
    public SortDirection Sort(TableColumn column, SortDirection direction = SortDirection.None)
    {
        if (direction == SortDirection.None)
            direction = column.SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
        foreach (var c in Columns)
            if (c.Sort != TableSort.None || c == column) c.SortDirection = c == column ? direction : SortDirection.None;
        var index = Columns.IndexOf(column);
        var args = new TableSortingEventArgs(column, index, direction);
        Sorting?.Invoke(this, args);
        if (args.Cancel || index < 0 || _rows is null) return direction;

        var cellIndex = index + Offset;
        var keys = _display.Select(r => cellIndex < r.Cells.Count ? r.Cells[cellIndex].SortKey : "").ToArray();
        var numeric = column.Sort == TableSort.Number || (column.Sort != TableSort.Text && keys.Any(k => k.Length > 0) && keys.All(k => k.Length == 0 || !double.IsNaN(NumberOf(k))));
        var numbers = numeric ? keys.Select(NumberOf).ToArray() : [];
        var sign = direction == SortDirection.Descending ? -1 : 1;
        var order = Enumerable.Range(0, _display.Count).ToList();
        order.Sort((a, b) =>
        {
            var ea = numeric ? double.IsNaN(numbers[a]) : keys[a].Length == 0;
            var eb = numeric ? double.IsNaN(numbers[b]) : keys[b].Length == 0;
            if (ea || eb) return ea == eb ? a - b : ea ? 1 : -1;
            var c = numeric ? numbers[a].CompareTo(numbers[b]) : CompareText(keys[a], keys[b]);
            return c != 0 ? c * sign : a - b;
        });

        var rowsPanel = _rows as TableRowsPanel;
        var before = _display.ToDictionary(r => r, r => rowsPanel?.OffsetOf(r) ?? r.Bounds.Y);
        var sorted = order.Select(i => _display[i]).ToList();
        _display.Clear();
        _display.AddRange(sorted);
        if (rowsPanel is not null) rowsPanel.SetRows(sorted);
        else
        {
            _rows.Children.Clear();
            foreach (var row in sorted) _rows.Children.Add(row);
        }
        Place();
        Slide(before);
        return direction;
    }

    private void Slide(Dictionary<TableRow, double> before)
    {
        var y = 0.0;
        var duration = this.TryFindResource("InsTEnter", ActualThemeVariant, out var d) && d is TimeSpan t ? t : TimeSpan.FromSeconds(0.28);
        var easing = this.TryFindResource("InsEase", ActualThemeVariant, out var es) && es is Easing ease ? ease : new SplineEasing(0.2, 0.7, 0.2, 1);
        var rowsPanel = _rows as TableRowsPanel;
        foreach (var row in _display)
        {
            var dy = before[row] - y;
            y += rowsPanel?.HeightOf(row) ?? row.Bounds.Height;
            // Only the rows in the tree move; the rest are out of view.
            if (Math.Abs(dy) < 1 || (rowsPanel is not null && !rowsPanel.InView(row))) continue;
            row.Transitions = null;
            row.RenderTransform = TransformOperations.Parse($"translateY({dy.ToString(CultureInfo.InvariantCulture)}px)");
            Dispatcher.UIThread.Post(() =>
            {
                row.Transitions = [new TransformOperationsTransition { Property = RenderTransformProperty, Duration = duration, Easing = easing }];
                row.RenderTransform = TransformOperations.Identity;
            }, DispatcherPriority.Render);
        }
    }

    private static int CompareText(string a, string b)
    {
        try
        {
            return CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);
        }
        catch (Exception e) when (e is ArgumentException or PlatformNotSupportedException)
        {
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>A figure as a number, in either set of digits, with its separators and any sign or unit around it; NaN when the text is not a figure.</summary>
    internal static double NumberOf(string text)
    {
        var chars = text.Select(c => c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c).ToArray();
        var t = new string(chars).Replace("٫", ".").Replace("−", "-");
        t = Spaces().Replace(t, "");
        var m = Figure().Match(t);
        return m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.NaN;
    }

    [GeneratedRegex(@"[\s,٬ ]")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"^[^\d.-]*(-?\d*\.?\d+)[^\d]*$")]
    private static partial Regex Figure();

    private void OnScroll(object? sender, ScrollChangedEventArgs e) => UpdateSticky();

    private void OnCardSized(object? sender, SizeChangedEventArgs e) => ClipRows();

    // Under a kept header the rows show from 8px below the bar, the card's corners rounding their top, so nothing square pokes out beside the bar's ends.
    private void ClipRows()
    {
        if (_card is null || _scroll is null) return;
        var y = Classes.Contains("sticky") ? _scroll.Offset.Y : 0;
        if (y <= 0) { _card.Clip = null; return; }
        var r = _card.CornerRadius.TopLeft;
        _card.Clip = new RectangleGeometry(new Rect(0, y, _card.Bounds.Width, Math.Max(0, _card.Bounds.Height - y))) { RadiusX = r, RadiusY = r };
    }

    private void UpdateSticky()
    {
        if (_scroll is null || _headCells is null) return;
        var sticky = Classes.Contains("sticky");
        var offset = _scroll.Offset;
        if (_head is not null)
            _head.RenderTransform = sticky && offset.Y > 0 ? new TranslateTransform(0, offset.Y) : null;
        ClipRows();
        var n = PinnedCells;
        var x = n > 0 ? offset.X : 0;
        // A scroll straight down moves no frozen cell, so the rows are left alone.
        if (n == _pinned && x == _pinX) return;
        Pin(_headCells.Children, n, x);
        // Rows out of the tree, or waiting hidden, are pinned when they come back.
        foreach (var row in _rows is TableRowsPanel panel ? panel.Children.OfType<TableRow>().Where(r => !panel.Waiting(r)) : _display) Pin(row.Cells, n, x);
        _pinned = n;
        _pinX = x;
    }

    private void OnRealized(TableRow row)
    {
        if (!double.IsNaN(_pinX)) Pin(row.Cells, _pinned, _pinX);
    }

    // The frozen cells ride along with the scroll, and the last of them casts a shade on the cells passing under it.
    private void Pin(IReadOnlyList<Control> cells, int n, double x)
    {
        var upto = Math.Min(cells.Count, Math.Max(n, _pinned));
        for (var i = 0; i < upto; i++)
        {
            var cell = cells[i];
            var on = i < n;
            var moved = on && x > 0;
            var edge = moved && i == n - 1;
            var flip = cell.FlowDirection != FlowDirection;
            cell.ZIndex = on ? 1 : 0;
            cell.RenderTransform = moved ? new TranslateTransform(x, 0) : null;
            if (cell is TableCell c) c.SetPinned(moved, edge, flip);
            else if (cell is TableHeader h) h.SetPinned(moved, edge, flip);
        }
    }
}

/// <summary>A sort about to happen: cancel it to sort elsewhere (on the server) and leave only the arrow moved.</summary>
public sealed class TableSortingEventArgs : System.ComponentModel.CancelEventArgs
{
    internal TableSortingEventArgs(TableColumn column, int index, SortDirection direction)
    {
        Column = column;
        ColumnIndex = index;
        Direction = direction;
    }

    public TableColumn Column { get; }
    public int ColumnIndex { get; }
    public SortDirection Direction { get; }
}

/// <summary>The selection after a change.</summary>
public sealed class TableSelectionEventArgs : EventArgs
{
    internal TableSelectionEventArgs(IReadOnlyList<TableRow> rows)
    {
        Rows = rows;
    }

    public IReadOnlyList<TableRow> Rows { get; }
    public int Count => Rows.Count;
}

/// <summary>The row an action is about.</summary>
public sealed class TableRowEventArgs : EventArgs
{
    internal TableRowEventArgs(TableRow row)
    {
        Row = row;
    }

    public TableRow Row { get; }
}
