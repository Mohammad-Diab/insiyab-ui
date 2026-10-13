using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>Which edge a drawer comes from: <see cref="Start"/> and <see cref="End"/> follow the reading direction.</summary>
public enum DrawerSide
{
    Start,
    End,
    Bottom,
}

/// <summary>Whether a drawer opens over the window (<see cref="Temporary"/>) or takes a column beside the content (<see cref="Pinned"/>).</summary>
public enum DrawerMode
{
    Temporary,
    Pinned,
}

/// <summary>A <see cref="Dialog"/> used as a drawer beside the page content. Temporary it opens over the window like any drawer; pinned it takes a resizable column and pushes the content. A pinned drawer needs <see cref="PinnedMinWidth"/> of window width, and below it opens as a temporary one.</summary>
[TemplatePart("PART_Grid", typeof(Grid))]
[TemplatePart("PART_Slot", typeof(Decorator))]
[TemplatePart("PART_Splitter", typeof(GridSplitter))]
[PseudoClasses(":pinned")]
public class DrawerHost : ContentControl
{
    public static readonly StyledProperty<Dialog?> PaneProperty =
        AvaloniaProperty.Register<DrawerHost, Dialog?>(nameof(Pane));

    public static readonly StyledProperty<DrawerSide> SideProperty =
        AvaloniaProperty.Register<DrawerHost, DrawerSide>(nameof(Side));

    public static readonly StyledProperty<DrawerMode> ModeProperty =
        AvaloniaProperty.Register<DrawerHost, DrawerMode>(nameof(Mode));

    public static readonly StyledProperty<bool> IsPaneOpenProperty =
        AvaloniaProperty.Register<DrawerHost, bool>(nameof(IsPaneOpen), false, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> PaneLengthProperty =
        AvaloniaProperty.Register<DrawerHost, double>(nameof(PaneLength), 320);

    public static readonly StyledProperty<double> MinPaneLengthProperty =
        AvaloniaProperty.Register<DrawerHost, double>(nameof(MinPaneLength), 240);

    public static readonly StyledProperty<double> MaxPaneLengthProperty =
        AvaloniaProperty.Register<DrawerHost, double>(nameof(MaxPaneLength), 560);

    public static readonly StyledProperty<double> PinnedMinWidthProperty =
        AvaloniaProperty.Register<DrawerHost, double>(nameof(PinnedMinWidth), 900);

    public static readonly DirectProperty<DrawerHost, bool> IsPinnedProperty =
        AvaloniaProperty.RegisterDirect<DrawerHost, bool>(nameof(IsPinned), h => h.IsPinned);

    /// <summary>On a button: toggles this host's pane, and focus returns to the button when a temporary pane closes.</summary>
    public static readonly AttachedProperty<DrawerHost?> TogglesProperty =
        AvaloniaProperty.RegisterAttached<DrawerHost, Button, DrawerHost?>("Toggles");

    private Grid? _grid;
    private Decorator? _slot;
    private GridSplitter? _splitter;
    private ContentPresenter? _content;
    private DefinitionBase? _paneDef;
    private Control? _from;
    private TopLevel? _top;
    private IDisposable? _context;
    private bool _isPinned;
    private bool _syncing;
    private bool _ready;
    private bool _queued;

    static DrawerHost()
    {
        Button.ClickEvent.AddClassHandler<Button>((b, e) =>
        {
            if (e.Source == b && GetToggles(b) is { } host) host.Toggle(b);
        });
    }

    /// <summary>The drawer: a <see cref="Dialog"/> that gets the <c>drawer</c> class.</summary>
    public Dialog? Pane { get => GetValue(PaneProperty); set => SetValue(PaneProperty, value); }

    public DrawerSide Side { get => GetValue(SideProperty); set => SetValue(SideProperty, value); }

    public DrawerMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    public bool IsPaneOpen { get => GetValue(IsPaneOpenProperty); set => SetValue(IsPaneOpenProperty, value); }

    /// <summary>The pinned pane's width (its height at the bottom); dragging the handle changes it.</summary>
    public double PaneLength { get => GetValue(PaneLengthProperty); set => SetValue(PaneLengthProperty, value); }

    public double MinPaneLength { get => GetValue(MinPaneLengthProperty); set => SetValue(MinPaneLengthProperty, value); }

    public double MaxPaneLength { get => GetValue(MaxPaneLengthProperty); set => SetValue(MaxPaneLengthProperty, value); }

    /// <summary>The window width a pinned pane needs; narrower, it opens over the page instead.</summary>
    public double PinnedMinWidth { get => GetValue(PinnedMinWidthProperty); set => SetValue(PinnedMinWidthProperty, value); }

    /// <summary>True while the pane is taking a column rather than opening over the window.</summary>
    public bool IsPinned => _isPinned;

    public static DrawerHost? GetToggles(Button button) => button.GetValue(TogglesProperty);
    public static void SetToggles(Button button, DrawerHost? value) => button.SetValue(TogglesProperty, value);

    public void Toggle(Control? from = null)
    {
        _from = from;
        SetCurrentValue(IsPaneOpenProperty, !IsPaneOpen);
    }

    internal void OpenPane(Control anchor)
    {
        _from = anchor;
        SetCurrentValue(IsPaneOpenProperty, true);
    }

    internal void OnPaneClosed()
    {
        SetCurrentValue(IsPaneOpenProperty, false);
        Sync();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _grid = e.NameScope.Find<Grid>("PART_Grid");
        _slot = e.NameScope.Find<Decorator>("PART_Slot");
        _splitter = e.NameScope.Find<GridSplitter>("PART_Splitter");
        _content = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _ready = true;
        Sync();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = OverlayHost.Top(this);
        if (_top is not null) _top.PropertyChanged += OnTopChanged;
        if (!IsPinnedNow() && IsPaneOpen) SetCurrentValue(IsPaneOpenProperty, false);
        Sync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_top is not null) _top.PropertyChanged -= OnTopChanged;
        _top = null;
        if (Pane is { IsOpen: true, IsPinned: false } open) open.Close();
    }

    // The window's size changes inside a layout pass, which must not rebuild the grid it is measuring, so the change is applied right after it.
    private void OnTopChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != BoundsProperty && e.Property != TopLevel.ClientSizeProperty) return;
        if (_queued || IsPinnedNow() == IsPinned) return;
        _queued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _queued = false;
            Sync();
        }, DispatcherPriority.Loaded);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PaneProperty)
        {
            OnPaneChanged(change.GetOldValue<Dialog?>(), change.GetNewValue<Dialog?>());
        }
        else if (change.Property == PaneLengthProperty)
        {
            if (!_syncing) ApplyLength();
        }
        else if (change.Property == SideProperty || change.Property == ModeProperty || change.Property == IsPaneOpenProperty
            || change.Property == MinPaneLengthProperty || change.Property == MaxPaneLengthProperty || change.Property == PinnedMinWidthProperty)
        {
            Sync();
        }
    }

    private void OnPaneChanged(Dialog? old, Dialog? pane)
    {
        _context?.Dispose();
        _context = null;
        if (old is not null)
        {
            old.Closed -= OnPaneClosedEvent;
            if (old.IsPinned)
            {
                old.SetPinnedOpen(false);
                old.Pin(null);
            }
            if (_slot is not null && ReferenceEquals(_slot.Child, old)) _slot.Child = null;
            if (old.IsOpen) old.Close();
        }
        if (pane is not null)
        {
            pane.Closed += OnPaneClosedEvent;
            if (!pane.IsSet(DataContextProperty)) _context = pane.Bind(DataContextProperty, this.GetObservable(DataContextProperty));
        }
        Sync();
    }

    private void OnPaneClosedEvent(object? sender, DialogClosedEventArgs e)
    {
        if (Pane is { IsPinned: false }) SetCurrentValue(IsPaneOpenProperty, false);
    }

    // Before the host knows its window (width 0) it counts as pinned, so the first real measure decides.
    private bool IsPinnedNow()
    {
        if (Mode != DrawerMode.Pinned) return false;
        var width = OverlayHost.Width(this);
        return width <= 0 || width >= PinnedMinWidth;
    }

    private void Sync()
    {
        if (!_ready || _grid is null || _slot is null || _splitter is null || _content is null) return;
        var pane = Pane;
        var pinned = IsPinnedNow();
        var wasPinned = IsPinned;
        SetAndRaise(IsPinnedProperty, ref _isPinned, pinned);
        PseudoClasses.Set(":pinned", pinned);
        if (pane is not null)
        {
            OverlayHost.SetClass(pane.Classes, "drawer", true);
            OverlayHost.SetClass(pane.Classes, "end", Side == DrawerSide.End);
            OverlayHost.SetClass(pane.Classes, "bottom", Side == DrawerSide.Bottom);
        }
        if (wasPinned && !pinned && VisualRoot is not null && IsPaneOpen && pane is not null && pane.IsOpen)
            SetCurrentValue(IsPaneOpenProperty, false);
        if (pinned && pane is not null)
        {
            if (pane.IsOpen && !pane.IsPinned) DialogStack.Drop(pane);
            pane.Pin(this);
            _slot.Child = IsPaneOpen ? pane : null;
            pane.SetPinnedOpen(IsPaneOpen);
        }
        else if (pane is not null)
        {
            if (pane.IsPinned)
            {
                _slot.Child = null;
                pane.SetPinnedOpen(false);
                pane.Pin(null);
            }
            if (IsPaneOpen && !pane.IsOpen) pane.Open(_from ?? this);
            else if (!IsPaneOpen && pane.IsOpen) pane.Close();
        }
        ApplyLayout(pinned && IsPaneOpen && pane is not null);
    }

    private void ApplyLayout(bool show)
    {
        var g = _grid!;
        if (_paneDef is not null) _paneDef.PropertyChanged -= OnPaneDefChanged;
        _paneDef = null;
        g.ColumnDefinitions.Clear();
        g.RowDefinitions.Clear();
        _slot!.IsVisible = show;
        _splitter!.IsVisible = show;
        if (!show)
        {
            foreach (var part in (Control[])[_content!, _splitter, _slot])
            {
                Grid.SetColumn(part, 0);
                Grid.SetRow(part, 0);
            }
            return;
        }
        var length = Math.Clamp(PaneLength, MinPaneLength, MaxPaneLength);
        if (Side == DrawerSide.Bottom)
        {
            var def = new RowDefinition(length, GridUnitType.Pixel) { MinHeight = MinPaneLength, MaxHeight = MaxPaneLength };
            g.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            g.RowDefinitions.Add(def);
            _paneDef = def;
            Grid.SetRow(_content!, 0);
            Grid.SetRow(_splitter, 1);
            Grid.SetRow(_slot, 2);
            Grid.SetColumn(_content!, 0);
            Grid.SetColumn(_splitter, 0);
            Grid.SetColumn(_slot, 0);
            _splitter.ResizeDirection = GridResizeDirection.Rows;
        }
        else
        {
            var def = new ColumnDefinition(length, GridUnitType.Pixel) { MinWidth = MinPaneLength, MaxWidth = MaxPaneLength };
            var start = Side == DrawerSide.Start;
            g.ColumnDefinitions.Add(start ? def : new ColumnDefinition(1, GridUnitType.Star));
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            g.ColumnDefinitions.Add(start ? new ColumnDefinition(1, GridUnitType.Star) : def);
            _paneDef = def;
            Grid.SetColumn(_slot, start ? 0 : 2);
            Grid.SetColumn(_splitter, 1);
            Grid.SetColumn(_content!, start ? 2 : 0);
            Grid.SetRow(_content!, 0);
            Grid.SetRow(_splitter, 0);
            Grid.SetRow(_slot, 0);
            _splitter.ResizeDirection = GridResizeDirection.Columns;
        }
        _paneDef.PropertyChanged += OnPaneDefChanged;
    }

    private void ApplyLength()
    {
        var length = Math.Clamp(PaneLength, MinPaneLength, MaxPaneLength);
        if (_paneDef is ColumnDefinition c) c.Width = new GridLength(length);
        else if (_paneDef is RowDefinition r) r.Height = new GridLength(length);
    }

    private void OnPaneDefChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ColumnDefinition.WidthProperty && e.Property != RowDefinition.HeightProperty) return;
        var value = sender is ColumnDefinition c ? c.Width : ((RowDefinition)sender!).Height;
        if (!value.IsAbsolute) return;
        _syncing = true;
        SetCurrentValue(PaneLengthProperty, value.Value);
        _syncing = false;
    }
}
