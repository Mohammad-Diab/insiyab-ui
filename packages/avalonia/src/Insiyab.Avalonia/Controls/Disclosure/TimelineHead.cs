using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>A timeline event's title, with room for the unread dot it draws just after the title's text, on its first line.</summary>
internal sealed class TimelineHead : Decorator
{
    public static readonly StyledProperty<bool> DotProperty =
        AvaloniaProperty.Register<TimelineHead, bool>(nameof(Dot));

    public static readonly StyledProperty<IBrush?> DotBrushProperty =
        AvaloniaProperty.Register<TimelineHead, IBrush?>(nameof(DotBrush));

    internal const double DotSize = 8;
    internal const double Gap = 6;

    static TimelineHead()
    {
        AffectsMeasure<TimelineHead>(DotProperty);
        AffectsRender<TimelineHead>(DotProperty, DotBrushProperty);
    }

    public bool Dot { get => GetValue(DotProperty); set => SetValue(DotProperty, value); }

    public IBrush? DotBrush { get => GetValue(DotBrushProperty); set => SetValue(DotBrushProperty, value); }

    /// <summary>Where the dot is drawn, in this head's own box; empty without a dot.</summary>
    internal Rect DotRect { get; private set; }

    private double Room => Dot ? DotSize + Gap : 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child) return default;
        child.Measure(availableSize.WithWidth(Math.Max(0, availableSize.Width - Room)));
        return new Size(child.DesiredSize.Width + Room, child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0, finalSize.Width - Room);
        Child?.Arrange(new Rect(0, 0, width, finalSize.Height));
        if (!Dot)
        {
            DotRect = default;
            return finalSize;
        }
        var text = Math.Min(Child?.DesiredSize.Width ?? 0, width);
        var middle = Math.Min(finalSize.Height / 2, GetValue(TextElement.FontSizeProperty) * 0.8);
        DotRect = new Rect(text + Gap, Math.Max(0, middle - DotSize / 2), DotSize, DotSize);
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Dot && DotBrush is { } brush) context.DrawEllipse(brush, null, DotRect);
    }
}
