using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>The month calendar's header row, three groups: the start group at the start edge, the end group at the end edge, the middle group truly centred between them. When the three do not fit on one line, the start and end groups share the first line and the middle group takes its own line under them, and the Tab order follows; when even the start and end groups do not fit together, the end group takes a line of its own between them. With no middle group, an end group that does not fit goes under the start group. A group wider than the whole row wraps its own controls.</summary>
internal sealed class MonthHead : Panel
{
    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<MonthHead, double>(nameof(Gap), 12);

    private int _lines = 1;

    static MonthHead() => AffectsMeasure<MonthHead>(GapProperty);

    protected override Type StyleKeyOverride => typeof(MonthHead);

    /// <summary>The room between the groups, and between the lines when they wrap.</summary>
    public double Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }

    internal bool IsWrapped => _lines > 1;

    /// <summary>The start, end and middle groups each on a line of their own.</summary>
    internal bool IsStacked => _lines > 2;

    private bool HasMiddle => Children[1].IsVisible;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return base.MeasureOverride(availableSize);
        foreach (var child in Children) child.Measure(Size.Infinity);
        var (start, middle, end) = (Children[0].DesiredSize, Children[1].DesiredSize, Children[2].DesiredSize);
        var line = HasMiddle ? 2 * Math.Max(start.Width, end.Width) + middle.Width + 2 * Gap : start.Width + end.Width + Gap;
        var room = availableSize.Width;
        _lines = line <= room ? 1 : !HasMiddle || start.Width + end.Width + Gap <= room ? 2 : 3;
        KeyboardNavigation.SetTabIndex(Children[0], 0);
        KeyboardNavigation.SetTabIndex(Children[1], _lines > 1 ? 2 : 1);
        KeyboardNavigation.SetTabIndex(Children[2], _lines > 1 ? 1 : 2);
        var width = double.IsInfinity(room) ? line : room;
        // A group wider than the row wraps inside itself.
        if (double.IsFinite(room))
            foreach (var child in Children)
                if (child.IsVisible && child.DesiredSize.Width > room + 0.5) child.Measure(new Size(room, double.PositiveInfinity));
        (start, middle, end) = (Children[0].DesiredSize, Children[1].DesiredSize, Children[2].DesiredSize);
        if (!HasMiddle) return _lines > 1 ? new Size(width, start.Height + Gap + end.Height) : new Size(width, Math.Max(start.Height, end.Height));
        return _lines switch
        {
            1 => new Size(width, Math.Max(Math.Max(start.Height, end.Height), middle.Height)),
            2 => new Size(width, Math.Max(start.Height, end.Height) + Gap + middle.Height),
            _ => new Size(width, start.Height + Gap + end.Height + Gap + middle.Height),
        };
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3) return base.ArrangeOverride(finalSize);
        var (start, middle, end) = (Children[0], Children[1], Children[2]);
        var startWidth = Math.Min(start.DesiredSize.Width, finalSize.Width);
        var endWidth = Math.Min(end.DesiredSize.Width, finalSize.Width);
        if (!HasMiddle)
        {
            var first = _lines > 1 ? start.DesiredSize.Height : finalSize.Height;
            start.Arrange(new Rect(0, 0, startWidth, first));
            var endTop = _lines > 1 ? first + Gap : 0;
            end.Arrange(new Rect(finalSize.Width - endWidth, endTop, endWidth, finalSize.Height - endTop));
            middle.Arrange(default);
            return finalSize;
        }
        double middleTop;
        if (_lines > 2)
        {
            start.Arrange(new Rect(0, 0, startWidth, start.DesiredSize.Height));
            var endTop = start.DesiredSize.Height + Gap;
            end.Arrange(new Rect(finalSize.Width - endWidth, endTop, endWidth, end.DesiredSize.Height));
            middleTop = endTop + end.DesiredSize.Height + Gap;
        }
        else
        {
            var top = _lines > 1 ? Math.Max(start.DesiredSize.Height, end.DesiredSize.Height) : finalSize.Height;
            start.Arrange(new Rect(0, 0, startWidth, top));
            end.Arrange(new Rect(Math.Max(0, finalSize.Width - endWidth), 0, endWidth, top));
            middleTop = _lines > 1 ? top + Gap : 0;
        }
        var middleWidth = Math.Min(middle.DesiredSize.Width, finalSize.Width);
        middle.Arrange(new Rect((finalSize.Width - middleWidth) / 2, middleTop, middleWidth, Math.Max(0, finalSize.Height - middleTop)));
        return finalSize;
    }
}
