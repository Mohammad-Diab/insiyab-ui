using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>A row whose first shown child gives way, cut with its own ellipsis, while the children after it keep their widths right behind it: a title with a pill that follows its end. A Grid's Auto column never cuts the title and a star column pushes the pill to the far end. Mirrors with the page; when the others alone are wider than the row the first child gets no room.</summary>
public class TrailingRow : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<TrailingRow, double>(nameof(Spacing), 8);

    static TrailingRow() => AffectsMeasure<TrailingRow>(SpacingProperty);

    /// <summary>The space between two children, 8px unless set.</summary>
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    private Control? First()
    {
        foreach (var c in Children)
            if (c.IsVisible) return c;
        return null;
    }

    // The width of every shown child after the first, each with the space before it.
    private double Tail(Control first, Size measure, ref double height)
    {
        double tail = 0;
        foreach (var c in Children)
        {
            if (c == first || !c.IsVisible) continue;
            c.Measure(measure);
            tail += c.DesiredSize.Width + Spacing;
            height = Math.Max(height, c.DesiredSize.Height);
        }
        return tail;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (First() is not { } first) return default;
        double height = 0;
        var tail = Tail(first, new Size(double.PositiveInfinity, availableSize.Height), ref height);
        var room = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : Math.Max(0, availableSize.Width - tail);
        first.Measure(new Size(room, availableSize.Height));
        height = Math.Max(height, first.DesiredSize.Height);
        return new Size(Math.Min(first.DesiredSize.Width + tail, availableSize.Width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (First() is not { } first) return finalSize;
        double tail = 0;
        foreach (var c in Children)
            if (c != first && c.IsVisible) tail += c.DesiredSize.Width + Spacing;
        var width = Math.Min(first.DesiredSize.Width, Math.Max(0, finalSize.Width - tail));
        first.Arrange(new Rect(0, 0, width, finalSize.Height));
        var x = width + Spacing;
        foreach (var c in Children)
        {
            if (c == first || !c.IsVisible) continue;
            c.Arrange(new Rect(x, 0, c.DesiredSize.Width, finalSize.Height));
            x += c.DesiredSize.Width + Spacing;
        }
        return finalSize;
    }
}
