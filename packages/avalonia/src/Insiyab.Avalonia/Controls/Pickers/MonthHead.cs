using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>The month calendar's header row, three groups: the start group at the start edge, the end group at the end edge, the middle group truly centred between them. When the three do not fit on one line, the start and end groups share the first line and the middle group takes its own line under them, and the Tab order follows. With no middle group, an end group that does not fit goes under the start group.</summary>
internal sealed class MonthHead : Panel
{
    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<MonthHead, double>(nameof(Gap), 12);

    private bool _wrapped;

    static MonthHead() => AffectsMeasure<MonthHead>(GapProperty);

    protected override Type StyleKeyOverride => typeof(MonthHead);

    /// <summary>The room between the groups, and between the lines when they wrap.</summary>
    public double Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }

    internal bool IsWrapped => _wrapped;

    private bool HasMiddle => Children[1].IsVisible;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return base.MeasureOverride(availableSize);
        foreach (var child in Children) child.Measure(Size.Infinity);
        var (start, middle, end) = (Children[0].DesiredSize, Children[1].DesiredSize, Children[2].DesiredSize);
        var line = HasMiddle ? 2 * Math.Max(start.Width, end.Width) + middle.Width + 2 * Gap : start.Width + end.Width + Gap;
        _wrapped = line > availableSize.Width;
        KeyboardNavigation.SetTabIndex(Children[0], 0);
        KeyboardNavigation.SetTabIndex(Children[1], _wrapped ? 2 : 1);
        KeyboardNavigation.SetTabIndex(Children[2], _wrapped ? 1 : 2);
        var width = double.IsInfinity(availableSize.Width) ? line : availableSize.Width;
        if (!HasMiddle) return _wrapped ? new Size(width, start.Height + Gap + end.Height) : new Size(width, Math.Max(start.Height, end.Height));
        var top = Math.Max(start.Height, end.Height);
        return _wrapped ? new Size(width, top + Gap + middle.Height) : new Size(width, Math.Max(top, middle.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3) return base.ArrangeOverride(finalSize);
        var (start, middle, end) = (Children[0], Children[1], Children[2]);
        if (!HasMiddle)
        {
            var first = _wrapped ? start.DesiredSize.Height : finalSize.Height;
            start.Arrange(new Rect(0, 0, Math.Min(start.DesiredSize.Width, finalSize.Width), first));
            var endTop = _wrapped ? first + Gap : 0;
            var endWidth = Math.Min(end.DesiredSize.Width, finalSize.Width);
            end.Arrange(new Rect(finalSize.Width - endWidth, endTop, endWidth, finalSize.Height - endTop));
            middle.Arrange(default);
            return finalSize;
        }
        var top = _wrapped ? Math.Max(start.DesiredSize.Height, end.DesiredSize.Height) : finalSize.Height;
        start.Arrange(new Rect(0, 0, start.DesiredSize.Width, top));
        end.Arrange(new Rect(Math.Max(0, finalSize.Width - end.DesiredSize.Width), 0, end.DesiredSize.Width, top));
        var middleTop = _wrapped ? top + Gap : 0;
        var middleWidth = Math.Min(middle.DesiredSize.Width, finalSize.Width);
        middle.Arrange(new Rect((finalSize.Width - middleWidth) / 2, middleTop, middleWidth, finalSize.Height - middleTop));
        return finalSize;
    }
}
