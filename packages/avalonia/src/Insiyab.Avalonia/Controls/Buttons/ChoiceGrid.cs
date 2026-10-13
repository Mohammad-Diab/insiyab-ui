using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>Equal columns as many as fit at <see cref="MinColumnWidth"/>, each row as tall as its tallest card: CSS <c>repeat(auto-fit, minmax(min, 1fr))</c>. Classes <c>switch-grid</c> and <c>tile-grid</c> set the web's sizes.</summary>
public class ChoiceGrid : Panel
{
    public static readonly StyledProperty<double> MinColumnWidthProperty =
        AvaloniaProperty.Register<ChoiceGrid, double>(nameof(MinColumnWidth), 192);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ChoiceGrid, double>(nameof(Spacing), 10);

    static ChoiceGrid() => AffectsMeasure<ChoiceGrid>(MinColumnWidthProperty, SpacingProperty);

    public double MinColumnWidth { get => GetValue(MinColumnWidthProperty); set => SetValue(MinColumnWidthProperty, value); }

    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    private int Columns(double width)
    {
        var count = 0;
        foreach (var c in Children) if (c.IsVisible) count++;
        if (count == 0) return 1;
        if (double.IsInfinity(width)) return count;
        var fit = (int)Math.Floor((width + Spacing) / (Math.Min(MinColumnWidth, width) + Spacing));
        return Math.Clamp(fit, 1, count);
    }

    private double ColumnWidth(double width, int columns) => Math.Max(0, (width - Spacing * (columns - 1)) / columns);

    protected override Size MeasureOverride(Size availableSize)
    {
        var columns = Columns(availableSize.Width);
        var cell = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : ColumnWidth(availableSize.Width, columns);
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

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = Columns(finalSize.Width);
        var cell = ColumnWidth(finalSize.Width, columns);
        var visible = Children.Where(c => c.IsVisible).ToList();
        double y = 0;
        for (var start = 0; start < visible.Count; start += columns)
        {
            var rowItems = visible.Skip(start).Take(columns).ToList();
            var row = rowItems.Max(c => c.DesiredSize.Height);
            for (var k = 0; k < rowItems.Count; k++)
                rowItems[k].Arrange(new Rect(k * (cell + Spacing), y, cell, row));
            y += row + Spacing;
        }
        return finalSize;
    }
}
