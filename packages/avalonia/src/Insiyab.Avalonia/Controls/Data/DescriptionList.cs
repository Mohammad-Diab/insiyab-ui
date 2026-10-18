using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.LogicalTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Facts about one thing as label and value, the web's <c>.ins-dl</c>: a column of labels with the values beside them, each label above its value when the list is narrower than 26rem. Class <c>grid</c> sets the pairs out in columns instead, as <c>.ins-dl--grid</c>.</summary>
public class DescriptionList : Panel
{
    /// <summary>Narrower than this, a label goes above its value (26rem).</summary>
    internal const double StackBelow = 416;

    private const double GridColumn = 192;
    private const double GridGapX = 24;
    private const double GridGapY = 20;

    public DescriptionList()
    {
        Classes.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset || (e.NewItems?.Contains("grid") ?? false) || (e.OldItems?.Contains("grid") ?? false)) InvalidateMeasure();
        };
    }

    private bool IsGrid => Classes.Contains("grid");

    private int ColumnsFor(double width) =>
        !IsGrid || double.IsInfinity(width) ? 1 : Math.Max(1, (int)Math.Floor((width + GridGapX) / (GridColumn + GridGapX)));

    protected override Size MeasureOverride(Size availableSize)
    {
        var grid = IsGrid;
        var stacked = grid || availableSize.Width < StackBelow;
        foreach (var child in Children)
            if (child is DescriptionItem item) item.SetLayout(stacked, grid);

        var columns = ColumnsFor(availableSize.Width);
        var cellWidth = columns == 1 ? availableSize.Width : Math.Floor((availableSize.Width - (columns - 1) * GridGapX) / columns);
        var height = 0.0;
        var rowHeight = 0.0;
        var width = 0.0;
        var visible = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(cellWidth, double.PositiveInfinity));
            if (!child.IsVisible) continue;
            width = Math.Max(width, child.DesiredSize.Width);
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            visible++;
            if (visible % columns == 0)
            {
                height += rowHeight + (grid ? GridGapY : 0);
                rowHeight = 0;
            }
        }
        height += rowHeight;
        if (grid && visible > 0 && visible % columns == 0) height -= GridGapY;
        return new Size(columns == 1 ? width : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var grid = IsGrid;
        var columns = ColumnsFor(finalSize.Width);
        var cellWidth = columns == 1 ? finalSize.Width : Math.Floor((finalSize.Width - (columns - 1) * GridGapX) / columns);
        var y = 0.0;
        var rowHeight = 0.0;
        var col = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Arrange(new Rect(col * (cellWidth + GridGapX), y, cellWidth, child.DesiredSize.Height));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            if (++col == columns)
            {
                col = 0;
                y += rowHeight + (grid ? GridGapY : 0);
                rowHeight = 0;
            }
        }
        return finalSize;
    }
}

/// <summary>One label and its value in a <see cref="DescriptionList"/>: the web's <c>&lt;div&gt;&lt;dt&gt;&lt;dd&gt;</c>. Class <c>num</c> keeps a figure in its own direction, starting where the other values start.</summary>
[PseudoClasses(":stacked", ":grid", ":first", ":last")]
public class DescriptionItem : ContentControl
{
    public static readonly StyledProperty<object?> TermProperty =
        AvaloniaProperty.Register<DescriptionItem, object?>(nameof(Term));

    private IChildIndexProvider? _siblings;

    /// <summary>The label: the web's <c>&lt;dt&gt;</c>.</summary>
    public object? Term { get => GetValue(TermProperty); set => SetValue(TermProperty, value); }

    internal void SetLayout(bool stacked, bool grid)
    {
        PseudoClasses.Set(":stacked", stacked);
        PseudoClasses.Set(":grid", grid);
    }

    // The first and last pair (:first, :last) are watched here: an :nth-child rule in the theme held a removed item through its parent.
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _siblings = ((ILogical)this).LogicalParent as IChildIndexProvider;
        if (_siblings is not null) _siblings.ChildIndexChanged += OnSiblings;
        Place();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        if (_siblings is not null) _siblings.ChildIndexChanged -= OnSiblings;
        _siblings = null;
    }

    private void OnSiblings(object? sender, ChildIndexChangedEventArgs e) => Place();

    private void Place()
    {
        var i = _siblings?.GetChildIndex(this) ?? -1;
        PseudoClasses.Set(":first", i == 0);
        PseudoClasses.Set(":last", i >= 0 && _siblings!.TryGetTotalCount(out var count) && i == count - 1);
    }
}
