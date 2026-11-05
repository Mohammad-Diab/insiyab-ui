using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Insiyab.Avalonia.Controls;

/// <summary>How a column sorts: the web's <c>data-ins-sort</c>, <c>="number"</c> and <c>="text"</c>.</summary>
public enum TableSort
{
    None,
    Auto,
    Number,
    Text,
}

/// <summary>Which way a column is sorted: the web's <c>aria-sort</c>.</summary>
public enum SortDirection
{
    None,
    Ascending,
    Descending,
}

/// <summary>What a cell turns into when it is edited in place: the web's <c>data-ins-edit</c>, <c>="number"</c> and <c>="select"</c>.</summary>
public enum TableEdit
{
    None,
    Text,
    Number,
    Select,
}

/// <summary>How clicks and keys select rows in a <see cref="Table"/>.</summary>
public enum TableSelectionMode
{
    None,
    Single,
    Extended,
}

/// <summary>One column of a <see cref="Table"/>: its heading and what its cells can do.</summary>
public class TableColumn : AvaloniaObject
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<TableColumn, object?>(nameof(Header));

    public static readonly StyledProperty<string?> ClassesProperty =
        AvaloniaProperty.Register<TableColumn, string?>(nameof(Classes));

    public static readonly StyledProperty<TableSort> SortProperty =
        AvaloniaProperty.Register<TableColumn, TableSort>(nameof(Sort));

    public static readonly StyledProperty<SortDirection> SortDirectionProperty =
        AvaloniaProperty.Register<TableColumn, SortDirection>(nameof(SortDirection));

    public static readonly StyledProperty<TableEdit> EditProperty =
        AvaloniaProperty.Register<TableColumn, TableEdit>(nameof(Edit));

    public static readonly StyledProperty<string?> OptionsProperty =
        AvaloniaProperty.Register<TableColumn, string?>(nameof(Options));

    public static readonly StyledProperty<bool> IsFitProperty =
        AvaloniaProperty.Register<TableColumn, bool>(nameof(IsFit));

    public static readonly StyledProperty<double> MinWidthProperty =
        AvaloniaProperty.Register<TableColumn, double>(nameof(MinWidth), ColumnLayout.MinWidth);

    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<TableColumn, IDataTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<GridLength> WidthProperty =
        AvaloniaProperty.Register<TableColumn, GridLength>(nameof(Width), GridLength.Auto);

    /// <summary>The heading, text or a control.</summary>
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Classes given to the heading and to every cell of the column, space separated: <c>num</c> is the web's <c>.ins-num</c>.</summary>
    public string? Classes { get => GetValue(ClassesProperty); set => SetValue(ClassesProperty, value); }

    /// <summary>Makes the heading a button that sorts the rows by this column.</summary>
    public TableSort Sort { get => GetValue(SortProperty); set => SetValue(SortProperty, value); }

    /// <summary>The direction the rows are sorted by this column; set it for rows that arrive sorted.</summary>
    public SortDirection SortDirection { get => GetValue(SortDirectionProperty); set => SetValue(SortDirectionProperty, value); }

    /// <summary>Lets the column's cells be edited in place.</summary>
    public TableEdit Edit { get => GetValue(EditProperty); set => SetValue(EditProperty, value); }

    /// <summary>The choices of a <see cref="TableEdit.Select"/> column, separated by <c>|</c>.</summary>
    public string? Options { get => GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }

    /// <summary>Keeps the column as narrow as its content: the spare width goes to the others.</summary>
    public bool IsFit { get => GetValue(IsFitProperty); set => SetValue(IsFitProperty, value); }

    /// <summary>The narrowest the user can drag the column to; never under <see cref="ColumnLayout.MinWidth"/>.</summary>
    public double MinWidth { get => GetValue(MinWidthProperty); set => SetValue(MinWidthProperty, value); }

    /// <summary>For rows made from <see cref="Table.ItemsSource"/>: the template of each cell, given the row's item.</summary>
    public IDataTemplate? CellTemplate { get => GetValue(CellTemplateProperty); set => SetValue(CellTemplateProperty, value); }

    /// <summary>For rows made from <see cref="Table.ItemsSource"/>: the text of each cell, given the row's item. It is also what the column sorts by.</summary>
    public Func<object?, string?>? CellText { get; set; }

    /// <summary>For rows made from <see cref="Table.ItemsSource"/>: each cell's value, bound to the row's item, so an item that changes in place updates its cells.</summary>
    [AssignBinding]
    public BindingBase? CellBinding { get; set; }

    /// <summary>The column's width: Auto (its widest cell, sharing the spare room), pixels, or a share of the room the other columns leave (<c>*</c>, <c>2*</c>), never under its heading or <see cref="MinWidth"/>. A drag still sets it.</summary>
    public GridLength Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }

    internal IEnumerable<string> ClassList() =>
        (Classes ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
