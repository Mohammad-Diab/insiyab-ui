using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>Equal columns as many as fit at <see cref="MinColumnWidth"/>, each row as tall as its tallest card: CSS <c>repeat(auto-fit, minmax(min, 1fr))</c>, or <c>auto-fill</c> with <see cref="Fill"/>. <see cref="MaxColumns"/> caps the count and <see cref="IsMasonry"/> stacks each card under the shortest column. Classes <c>switch-grid</c> and <c>tile-grid</c> set the web's sizes.</summary>
public class ChoiceGrid : Panel
{
    public static readonly StyledProperty<double> MinColumnWidthProperty =
        AvaloniaProperty.Register<ChoiceGrid, double>(nameof(MinColumnWidth), 192);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ChoiceGrid, double>(nameof(Spacing), 10);

    /// <summary>Counts the columns from the width alone, so a lone card keeps a column's width instead of stretching across the row (CSS <c>auto-fill</c>).</summary>
    public static readonly StyledProperty<bool> FillProperty =
        AvaloniaProperty.Register<ChoiceGrid, bool>(nameof(Fill));

    /// <summary>The most columns, however wide the grid gets; 0 for no cap.</summary>
    public static readonly StyledProperty<int> MaxColumnsProperty =
        AvaloniaProperty.Register<ChoiceGrid, int>(nameof(MaxColumns));

    /// <summary>Each card keeps its own height and goes under the shortest column, the first of equal ones, so the cards read across and then down.</summary>
    public static readonly StyledProperty<bool> IsMasonryProperty =
        AvaloniaProperty.Register<ChoiceGrid, bool>(nameof(IsMasonry));

    // The width and cell the cards were last measured at, and the arranged width a new measure was last asked for: once per width, so a parent that measures at one width and arranges at another never loops.
    private double _measuredWidth = double.NaN;
    private double _measuredCell = double.NaN;
    private double _remeasuredFor = double.NaN;

    static ChoiceGrid() => AffectsMeasure<ChoiceGrid>(MinColumnWidthProperty, SpacingProperty, FillProperty, MaxColumnsProperty, IsMasonryProperty);

    public double MinColumnWidth { get => GetValue(MinColumnWidthProperty); set => SetValue(MinColumnWidthProperty, value); }

    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    public bool Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    public int MaxColumns { get => GetValue(MaxColumnsProperty); set => SetValue(MaxColumnsProperty, value); }

    public bool IsMasonry { get => GetValue(IsMasonryProperty); set => SetValue(IsMasonryProperty, value); }

    private bool Counted => Fill || IsMasonry;

    private int Columns(double width)
    {
        var count = 0;
        foreach (var c in Children) if (c.IsVisible) count++;
        var cap = MaxColumns > 0 ? MaxColumns : int.MaxValue;
        if (double.IsInfinity(width)) return Counted ? 1 : Math.Clamp(count, 1, cap);
        var fit = (int)Math.Floor((width + Spacing) / (Math.Min(MinColumnWidth, width) + Spacing));
        return Math.Clamp(fit, 1, Math.Max(1, Math.Min(Fill ? int.MaxValue : Math.Max(count, 1), cap)));
    }

    private double ColumnWidth(double width, int columns) => Math.Max(0, (width - Spacing * (columns - 1)) / columns);

    protected override Size MeasureOverride(Size availableSize)
    {
        // Measured with no width, inside something that scrolls sideways or sizes to its content: the width it was last given.
        if (Counted && double.IsInfinity(availableSize.Width) && Bounds.Width > 0)
            availableSize = availableSize.WithWidth(Bounds.Width);
        var columns = Columns(availableSize.Width);
        var cell = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : ColumnWidth(availableSize.Width, columns);
        _measuredWidth = availableSize.Width;
        _measuredCell = cell;
        return IsMasonry ? MeasureMasonry(availableSize, columns, cell) : MeasureRows(availableSize, columns, cell);
    }

    private Size MeasureRows(Size availableSize, int columns, double cell)
    {
        double height = 0, row = 0, widest = 0;
        var i = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Measure(new Size(cell, double.PositiveInfinity));
            widest = Math.Max(widest, child.DesiredSize.Width);
            row = Math.Max(row, child.DesiredSize.Height);
            if (++i % columns == 0) { height += row + Spacing; row = 0; }
        }
        if (i % columns != 0) height += row; else if (i > 0) height -= Spacing;
        var width = double.IsInfinity(availableSize.Width) ? widest * columns + Spacing * (columns - 1) : availableSize.Width;
        return new Size(width, height);
    }

    private Size MeasureMasonry(Size availableSize, int columns, double cell)
    {
        var heights = new double[columns];
        double widest = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Measure(new Size(cell, double.PositiveInfinity));
            widest = Math.Max(widest, child.DesiredSize.Width);
            var k = Shortest(heights);
            heights[k] += (heights[k] > 0 ? Spacing : 0) + child.DesiredSize.Height;
        }
        var width = double.IsInfinity(availableSize.Width) ? widest * columns + Spacing * (columns - 1) : availableSize.Width;
        return new Size(width, heights.Max());
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Within a pixel of the measured width (layout rounding at a scale such as 150%, a hair either side of a column's edge), the measured count: the cards keep the rows the grid's height was measured for.
        var columns = Columns(Math.Abs(finalSize.Width - _measuredWidth) < 1 ? _measuredWidth : finalSize.Width);
        var cell = ColumnWidth(finalSize.Width, columns);
        // Arranged at another width than measured (a scroll bar came or went): the cards are measured again at their cells, and one more layout is asked for so the grid's own height follows.
        if (!(Math.Abs(cell - _measuredCell) <= 0.5))
        {
            foreach (var child in Children)
                if (child.IsVisible) child.Measure(new Size(cell, double.PositiveInfinity));
            if (finalSize.Width != _remeasuredFor)
            {
                _remeasuredFor = finalSize.Width;
                Dispatcher.UIThread.Post(InvalidateMeasure, DispatcherPriority.Render);
            }
        }
        if (IsMasonry) ArrangeMasonry(columns, cell);
        else ArrangeRows(columns, cell);
        return finalSize;
    }

    private void ArrangeRows(int columns, double cell)
    {
        var children = Children;
        double y = 0;
        var i = 0;
        while (i < children.Count)
        {
            double row = 0;
            var n = 0;
            var j = i;
            for (; j < children.Count && n < columns; j++)
                if (children[j].IsVisible) { row = Math.Max(row, children[j].DesiredSize.Height); n++; }
            if (n == 0) break;
            var at = 0;
            for (var m = i; m < j; m++)
                if (children[m].IsVisible) children[m].Arrange(new Rect(at++ * (cell + Spacing), y, cell, row));
            y += row + Spacing;
            i = j;
        }
    }

    private void ArrangeMasonry(int columns, double cell)
    {
        var heights = new double[columns];
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var k = Shortest(heights);
            var y = heights[k] > 0 ? heights[k] + Spacing : 0;
            child.Arrange(new Rect(k * (cell + Spacing), y, cell, child.DesiredSize.Height));
            heights[k] = y + child.DesiredSize.Height;
        }
    }

    // The first column of the lowest height, so equal columns fill from the start.
    private static int Shortest(double[] heights)
    {
        var best = 0;
        for (var k = 1; k < heights.Length; k++)
            if (heights[k] < heights[best] - 0.5) best = k;
        return best;
    }
}
