using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The panel a pill's face sits in: as it is, or for <c>dot peek</c> measured as the dot alone, with the face laid over its neighbours so its dot lands on that spot.</summary>
internal sealed class PillPeek : Panel
{
    public const double Dot = 8;

    public static readonly StyledProperty<bool> IsPeekProperty =
        AvaloniaProperty.Register<PillPeek, bool>(nameof(IsPeek));

    public static readonly StyledProperty<bool> BackProperty =
        AvaloniaProperty.Register<PillPeek, bool>(nameof(Back));

    static PillPeek() => AffectsMeasure<PillPeek>(IsPeekProperty, BackProperty);

    public bool IsPeek { get => GetValue(IsPeekProperty); set => SetValue(IsPeekProperty, value); }

    /// <summary>The face opens toward the start, its dot at its end.</summary>
    public bool Back { get => GetValue(BackProperty); set => SetValue(BackProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0) return default;
        var face = Children[0];
        if (!IsPeek)
        {
            face.Measure(availableSize);
            return face.DesiredSize;
        }
        face.Measure(Size.Infinity);
        return new Size(Dot, Dot);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0) return finalSize;
        var face = Children[0];
        if (!IsPeek)
        {
            face.Arrange(new Rect(finalSize));
            return finalSize;
        }
        var size = face.DesiredSize;
        var (pad, edge) = face is Border b ? (b.Padding, b.BorderThickness) : (default(Thickness), default(Thickness));
        var x = Back ? Dot - (size.Width - pad.Right - edge.Right) : -(pad.Left + edge.Left);
        face.Arrange(new Rect(x, (Dot - size.Height) / 2, size.Width, size.Height));
        return finalSize;
    }
}
