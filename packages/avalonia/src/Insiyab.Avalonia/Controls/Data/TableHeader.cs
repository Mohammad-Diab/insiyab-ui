using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>A column heading in a <see cref="Table"/>'s header bar: the web's <c>&lt;th&gt;</c>. The table makes these from its columns.</summary>
[TemplatePart("PART_Grip", typeof(Border))]
[PseudoClasses(":pinned", ":pin-edge", ":pin-flip", ":check", ":resizable", ":grip-flip", ":resizing")]
public class TableHeader : ContentControl
{
    private Border? _grip;
    private bool _drag;
    private double _startX;
    private double _startWidth;

    /// <summary>The column this heading names; <c>null</c> for the selection column.</summary>
    public TableColumn? Column { get; internal set; }

    internal void SetPinned(bool on, bool edge = false, bool flip = false)
    {
        PseudoClasses.Set(":pinned", on);
        PseudoClasses.Set(":pin-edge", edge);
        PseudoClasses.Set(":pin-flip", flip);
    }

    internal void MarkCheck() => PseudoClasses.Set(":check", true);

    internal Table? Owner { get; set; }

    internal int Index { get; set; }

    internal void SetResizable(bool on) => PseudoClasses.Set(":resizable", on);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_grip is not null)
        {
            _grip.PointerPressed -= OnGripPressed;
            _grip.PointerMoved -= OnGripMoved;
            _grip.PointerReleased -= OnGripReleased;
            _grip.PointerCaptureLost -= OnGripCaptureLost;
        }
        _grip = e.NameScope.Find<Border>("PART_Grip");
        if (_grip is null) return;
        _grip.PointerPressed += OnGripPressed;
        _grip.PointerMoved += OnGripMoved;
        _grip.PointerReleased += OnGripReleased;
        _grip.PointerCaptureLost += OnGripCaptureLost;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Align();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FlowDirectionProperty) Align();
    }

    // A heading that reads the other way from its table (a figures column) keeps its grip on the table's trailing edge.
    private void Align() => PseudoClasses.Set(":grip-flip", IsGripFlipped);

    internal bool IsGripFlipped => Parent is Visual p && p.FlowDirection != FlowDirection;

    private void Drag(bool on)
    {
        _drag = on;
        PseudoClasses.Set(":resizing", on);
        Owner?.ShowGuide(on ? this : null);
    }

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_grip).Properties.IsLeftButtonPressed || Owner is null) return;
        e.Handled = true;
        if (e.ClickCount >= 2)
        {
            Owner.ColumnWidths.Fit(Index, Column?.MinWidth ?? 0);
            return;
        }
        _startX = e.GetPosition(Parent as Visual).X;
        _startWidth = Bounds.Width;
        e.Pointer.Capture(_grip);
        Drag(true);
    }

    private void OnGripMoved(object? sender, PointerEventArgs e)
    {
        if (!_drag || Owner is null) return;
        Owner.ColumnWidths.SetWidth(Index, Math.Max(Column?.MinWidth ?? 0, _startWidth + e.GetPosition(Parent as Visual).X - _startX));
        Owner.ShowGuide(this);
        e.Handled = true;
    }

    private void OnGripReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        Drag(false);
    }

    private void OnGripCaptureLost(object? sender, PointerCaptureLostEventArgs e) { if (_drag) Drag(false); }
}

/// <summary>The button a sortable heading becomes: the web's <c>.ins-sort</c>. Its arrow follows <see cref="Direction"/>.</summary>
[PseudoClasses(":ascending", ":descending")]
public class TableSortButton : Button
{
    public static readonly StyledProperty<SortDirection> DirectionProperty =
        AvaloniaProperty.Register<TableSortButton, SortDirection>(nameof(Direction));

    public SortDirection Direction { get => GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }

    protected override Type StyleKeyOverride => typeof(TableSortButton);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DirectionProperty)
        {
            PseudoClasses.Set(":ascending", Direction == SortDirection.Ascending);
            PseudoClasses.Set(":descending", Direction == SortDirection.Descending);
        }
    }
}
