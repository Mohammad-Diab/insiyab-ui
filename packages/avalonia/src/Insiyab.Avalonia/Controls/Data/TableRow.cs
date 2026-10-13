using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Insiyab.Avalonia.Controls;

/// <summary>One row of a <see cref="Table"/>: the web's <c>&lt;tr&gt;</c>. <see cref="IsSelected"/> is its <c>aria-selected</c>, <c>aria-current</c> and <c>.is-selected</c>.</summary>
[TemplatePart("PART_Cells", typeof(ColumnsPanel))]
[PseudoClasses(":selected", ":in-panel", ":current")]
public class TableRow : TemplatedControl
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<TableRow, bool>(nameof(IsSelected));

    private ColumnsPanel? _panel;

    public TableRow()
    {
        Cells.CollectionChanged += (_, _) => Fill();
    }

    /// <summary>The row's cells, in column order.</summary>
    [Content]
    public AvaloniaList<TableCell> Cells { get; } = [];

    /// <summary>Selected, picked or current: the row takes the brand tint and the marker on its leading edge.</summary>
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    /// <summary>The item this row was made from, for a table filled from <see cref="Table.ItemsSource"/>.</summary>
    public object? Item { get; internal set; }

    /// <summary>The table the row is in.</summary>
    public Table? Owner { get; internal set; }

    internal ColumnsPanel? CellsPanel => _panel;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _panel?.Children.Clear();
        _panel = e.NameScope.Find<ColumnsPanel>("PART_Cells");
        if (_panel is not null) _panel.Layout = Owner?.ColumnWidths;
        Fill();
    }

    internal void Attach(Table? owner)
    {
        Owner = owner;
        if (_panel is not null) _panel.Layout = owner?.ColumnWidths;
    }

    internal void SetInPanel(bool on) => PseudoClasses.Set(":in-panel", on);

    internal void SetCurrent(bool on) => PseudoClasses.Set(":current", on);

    internal void Fill()
    {
        if (_panel is null) return;
        _panel.Children.Clear();
        foreach (var cell in Cells)
        {
            cell.Row = this;
            _panel.Children.Add(cell);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", IsSelected);
            Owner?.OnRowSelectionChanged(this);
        }
    }
}
