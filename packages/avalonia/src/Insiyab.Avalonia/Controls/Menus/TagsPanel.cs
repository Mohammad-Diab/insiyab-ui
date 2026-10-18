using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>A wrapping row whose last child grows into what is left of its line, and drops to a line of its own below <see cref="LastMinWidth"/>: the chips, then the box, of <c>.ins-tags</c>.</summary>
internal class TagsPanel : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<TagsPanel, double>(nameof(Spacing), 5);

    public static readonly StyledProperty<double> LastMinWidthProperty =
        AvaloniaProperty.Register<TagsPanel, double>(nameof(LastMinWidth), 64);

    static TagsPanel() => AffectsMeasure<TagsPanel>(SpacingProperty, LastMinWidthProperty);

    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public double LastMinWidth { get => GetValue(LastMinWidthProperty); set => SetValue(LastMinWidthProperty, value); }

    protected override Size MeasureOverride(Size availableSize) => Layout(availableSize, false);

    protected override Size ArrangeOverride(Size finalSize)
    {
        Layout(finalSize, true);
        return finalSize;
    }

    private Size Layout(Size size, bool arrange)
    {
        var width = double.IsInfinity(size.Width) ? double.MaxValue : size.Width;
        double x = 0, y = 0, line = 0, widest = 0;
        var row = new List<(Control c, double x, double w)>();

        void Flush()
        {
            if (arrange)
                foreach (var (c, cx, cw) in row)
                    c.Arrange(new Rect(cx, y + (line - c.DesiredSize.Height) / 2, cw, c.DesiredSize.Height));
            row.Clear();
        }

        var visible = Children.Where(c => c.IsVisible).ToList();
        for (var i = 0; i < visible.Count; i++)
        {
            var child = visible[i];
            var last = i == visible.Count - 1;
            if (!arrange) child.Measure(new Size(width, double.PositiveInfinity));
            var w = child.DesiredSize.Width;
            var need = last ? Math.Max(LastMinWidth, w) : w;
            if (row.Count > 0 && x + Spacing + need > width)
            {
                Flush();
                y += line + Spacing;
                x = 0;
                line = 0;
            }
            var at = row.Count > 0 ? x + Spacing : 0;
            if (last && width < double.MaxValue) w = Math.Max(need, width - at);
            row.Add((child, at, w));
            x = at + w;
            line = Math.Max(line, child.DesiredSize.Height);
            widest = Math.Max(widest, x);
        }
        Flush();
        return new Size(double.IsInfinity(size.Width) ? widest : Math.Min(widest, width), y + line);
    }
}
