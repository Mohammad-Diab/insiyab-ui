using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>A column heading in a <see cref="Table"/>'s header bar: the web's <c>&lt;th&gt;</c>. The table makes these from its columns.</summary>
[TemplatePart("PART_Grip", typeof(Border))]
[PseudoClasses(":pinned", ":check", ":resizable")]
public class TableHeader : ContentControl
{
    private Border? _grip;
    private bool _drag;
    private double _startX;
    private double _startWidth;

    /// <summary>The column this heading names; <c>null</c> for the selection column.</summary>
    public TableColumn? Column { get; internal set; }

    internal void SetPinned(bool on) => PseudoClasses.Set(":pinned", on);

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

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_grip).Properties.IsLeftButtonPressed || Owner is null) return;
        e.Handled = true;
        if (e.ClickCount >= 2)
        {
            Owner.ColumnWidths.ClearWidth(Index);
            return;
        }
        _drag = true;
        _startX = e.GetPosition(Parent as Visual).X;
        _startWidth = Bounds.Width;
        e.Pointer.Capture(_grip);
    }

    private void OnGripMoved(object? sender, PointerEventArgs e)
    {
        if (!_drag || Owner is null) return;
        Owner.ColumnWidths.SetWidth(Index, _startWidth + e.GetPosition(Parent as Visual).X - _startX);
        e.Handled = true;
    }

    private void OnGripReleased(object? sender, PointerReleasedEventArgs e)
    {
        _drag = false;
        e.Pointer.Capture(null);
    }

    private void OnGripCaptureLost(object? sender, PointerCaptureLostEventArgs e) => _drag = false;
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
