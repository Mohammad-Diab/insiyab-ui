using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-split</c>: two panes that share the width, each scrolling on its own, with a handle between them that the pointer drags and the keyboard moves.</summary>
[TemplatePart("PART_Grid", typeof(Grid))]
[TemplatePart("PART_First", typeof(Control))]
[TemplatePart("PART_Handle", typeof(Control))]
[TemplatePart("PART_Second", typeof(Control))]
[PseudoClasses(":vertical", ":stacked", ":dragging")]
public class Split : TemplatedControl
{
    public static readonly StyledProperty<object?> FirstProperty =
        AvaloniaProperty.Register<Split, object?>(nameof(First));

    public static readonly StyledProperty<object?> SecondProperty =
        AvaloniaProperty.Register<Split, object?>(nameof(Second));

    /// <summary>The first pane's share of the length, in percent, kept within <see cref="Min"/> and <see cref="Max"/>.</summary>
    public static readonly StyledProperty<double> ShareProperty =
        AvaloniaProperty.Register<Split, double>(nameof(Share), 50, defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay, coerce: CoerceShare);

    /// <summary>Where a double click and Enter take the handle back to (the web's <c>data-ins-split-default</c>).</summary>
    public static readonly StyledProperty<double> DefaultShareProperty =
        AvaloniaProperty.Register<Split, double>(nameof(DefaultShare), 50);

    public static readonly StyledProperty<double> MinProperty =
        AvaloniaProperty.Register<Split, double>(nameof(Min), 20);

    public static readonly StyledProperty<double> MaxProperty =
        AvaloniaProperty.Register<Split, double>(nameof(Max), 80);

    /// <summary>Horizontal puts the panes side by side; Vertical stacks them with a horizontal handle (the web's <c>--v</c>).</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<Split, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>Narrower than this, side-by-side panes stack with no handle. The web stacks below a 56rem viewport; measured on the split itself, two 320px columns is the same point. 0 never stacks.</summary>
    public static readonly StyledProperty<double> StackWidthProperty =
        AvaloniaProperty.Register<Split, double>(nameof(StackWidth), 640);

    /// <summary>The handle's accessible name (the web's <c>aria-label</c> on the separator).</summary>
    public static readonly StyledProperty<string> ResizeLabelProperty =
        AvaloniaProperty.Register<Split, string>(nameof(ResizeLabel), "تغيير حجم اللوحين");

    /// <summary>Raised when a drag ends or a key moves the handle, with the new share (the web's <c>ins:split</c>).</summary>
    public static readonly RoutedEvent<SplitShareEventArgs> ShareCommittedEvent =
        RoutedEvent.Register<Split, SplitShareEventArgs>(nameof(ShareCommitted), RoutingStrategies.Bubble);

    private const double HandleLength = 14;
    private Grid? _grid;
    private Control? _first, _handle, _second;
    private bool _dragging;

    public object? First { get => GetValue(FirstProperty); set => SetValue(FirstProperty, value); }
    public object? Second { get => GetValue(SecondProperty); set => SetValue(SecondProperty, value); }
    public double Share { get => GetValue(ShareProperty); set => SetValue(ShareProperty, value); }
    public double DefaultShare { get => GetValue(DefaultShareProperty); set => SetValue(DefaultShareProperty, value); }
    public string ResizeLabel { get => GetValue(ResizeLabelProperty); set => SetValue(ResizeLabelProperty, value); }
    public double Min { get => GetValue(MinProperty); set => SetValue(MinProperty, value); }
    public double Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double StackWidth { get => GetValue(StackWidthProperty); set => SetValue(StackWidthProperty, value); }

    public event EventHandler<SplitShareEventArgs>? ShareCommitted
    {
        add => AddHandler(ShareCommittedEvent, value);
        remove => RemoveHandler(ShareCommittedEvent, value);
    }

    internal bool IsStacked => PseudoClasses.Contains(":stacked");

    static Split()
    {
        AffectsArrange<Split>(ShareProperty);
    }

    public Split()
    {
        UpdatePseudoClasses();
    }

    private static double CoerceShare(AvaloniaObject o, double value)
    {
        var s = (Split)o;
        return Math.Clamp(double.IsNaN(value) ? 50 : value, s.Min, Math.Max(s.Min, s.Max));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_handle is not null)
        {
            _handle.PointerPressed -= OnHandlePressed;
            _handle.PointerMoved -= OnHandleMoved;
            _handle.PointerReleased -= OnHandleReleased;
            _handle.PointerCaptureLost -= OnHandleCaptureLost;
            _handle.KeyDown -= OnHandleKeyDown;
            _handle.DoubleTapped -= OnHandleDoubleTapped;
        }
        base.OnApplyTemplate(e);
        _grid = e.NameScope.Find<Grid>("PART_Grid");
        _first = e.NameScope.Find<Control>("PART_First");
        _handle = e.NameScope.Find<Control>("PART_Handle");
        _second = e.NameScope.Find<Control>("PART_Second");
        if (_handle is not null)
        {
            _handle.PointerPressed += OnHandlePressed;
            _handle.PointerMoved += OnHandleMoved;
            _handle.PointerReleased += OnHandleReleased;
            _handle.PointerCaptureLost += OnHandleCaptureLost;
            _handle.KeyDown += OnHandleKeyDown;
            _handle.DoubleTapped += OnHandleDoubleTapped;
        }
        Relayout();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MinProperty || change.Property == MaxProperty) CoerceValue(ShareProperty);
        else if (change.Property == DefaultShareProperty && !IsSet(ShareProperty)) SetCurrentValue(ShareProperty, DefaultShare);
        else if (change.Property == OrientationProperty || change.Property == StackWidthProperty)
        {
            UpdatePseudoClasses();
            Relayout();
        }
        else if (change.Property == ShareProperty) Relayout();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var was = IsStacked;
        UpdatePseudoClasses();
        if (was != IsStacked || !IsStacked) Relayout();
    }

    private void UpdatePseudoClasses()
    {
        var vertical = Orientation == Orientation.Vertical;
        PseudoClasses.Set(":vertical", vertical);
        PseudoClasses.Set(":stacked", !vertical && StackWidth > 0 && Bounds.Width > 0 && Bounds.Width < StackWidth);
    }

    // Exact lengths, as on the web: the first pane is Share% of the whole, the handle 14px, the second the rest.
    private void Relayout()
    {
        if (_grid is null || _first is null || _handle is null || _second is null) return;
        var vertical = Orientation == Orientation.Vertical;
        var total = vertical ? Bounds.Height : Bounds.Width;
        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();
        if (IsStacked)
        {
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Place(_first, 0, 0);
            Place(_second, 1, 0);
            _handle.IsVisible = false;
            return;
        }
        _handle.IsVisible = true;
        GridLength firstLength = total > HandleLength
            ? new GridLength(Math.Max(0, total * Share / 100), GridUnitType.Pixel)
            : new GridLength(Share, GridUnitType.Star);
        GridLength secondLength = total > HandleLength ? new GridLength(1, GridUnitType.Star) : new GridLength(100 - Share, GridUnitType.Star);
        if (vertical)
        {
            _grid.RowDefinitions.Add(new RowDefinition(firstLength));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.RowDefinitions.Add(new RowDefinition(secondLength));
            Place(_first, 0, 0);
            Place(_handle, 1, 0);
            Place(_second, 2, 0);
        }
        else
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(firstLength));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(secondLength));
            Place(_first, 0, 0);
            Place(_handle, 0, 1);
            Place(_second, 0, 2);
        }
    }

    private static void Place(Control c, int row, int column)
    {
        Grid.SetRow(c, row);
        Grid.SetColumn(c, column);
    }

    /// <summary>Sets the share, kept within the limits, and reports it as the web's <c>Insiyab.split(el, size)</c> does.</summary>
    public double SetShare(double share)
    {
        SetCurrentValue(ShareProperty, share);
        RaiseEvent(new SplitShareEventArgs(ShareCommittedEvent, this, Share));
        return Share;
    }

    // From the start edge: Avalonia mirrors an RTL split, so x = 0 is already the start in its own coordinates.
    private double ShareAt(PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        return Orientation == Orientation.Vertical
            ? (Bounds.Height > 0 ? p.Y / Bounds.Height * 100 : Share)
            : (Bounds.Width > 0 ? p.X / Bounds.Width * 100 : Share);
    }

    private void OnHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (_handle is null || !e.GetCurrentPoint(_handle).Properties.IsLeftButtonPressed) return;
        _handle.Focus(NavigationMethod.Pointer);
        e.Pointer.Capture(_handle);
        SetDragging(true);
        e.Handled = true;
    }

    private void OnHandleMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        SetCurrentValue(ShareProperty, ShareAt(e));
        e.Handled = true;
    }

    private void OnHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging) return;
        e.Pointer.Capture(null);
        EndDrag();
        e.Handled = true;
    }

    private void OnHandleCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_dragging) EndDrag();
    }

    private void EndDrag()
    {
        SetDragging(false);
        SetShare(Share);
    }

    private void SetDragging(bool on)
    {
        _dragging = on;
        PseudoClasses.Set(":dragging", on);
        _handle?.Classes.Set("dragging", on);
    }

    private void OnHandleDoubleTapped(object? sender, TappedEventArgs e)
    {
        SetShare(DefaultShare);
        e.Handled = true;
    }

    // The arrows move the line the way they point, so in RTL the left arrow widens the first pane, which is on the right.
    private void OnHandleKeyDown(object? sender, KeyEventArgs e)
    {
        var vertical = Orientation == Orientation.Vertical;
        var rtl = FlowDirection == global::Avalonia.Media.FlowDirection.RightToLeft;
        var grow = vertical ? Key.Down : rtl ? Key.Left : Key.Right;
        var shrink = vertical ? Key.Up : rtl ? Key.Right : Key.Left;
        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 2;
        double? to = e.Key == grow ? Share + step
            : e.Key == shrink ? Share - step
            : e.Key == Key.Home ? Min
            : e.Key == Key.End ? Max
            : e.Key == Key.Enter ? DefaultShare
            : null;
        if (to is null) return;
        SetShare(to.Value);
        e.Handled = true;
    }
}

public sealed class SplitShareEventArgs : RoutedEventArgs
{
    internal SplitShareEventArgs(RoutedEvent routedEvent, object source, double share) : base(routedEvent, source)
    {
        Share = share;
    }

    public double Share { get; }
}
