using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The ring a pulsing current step sends out: at <see cref="Phase"/> it reaches 9px past the circle and fades from .44 to nothing.</summary>
internal sealed class StepsRing : Control
{
    public static readonly StyledProperty<double> PhaseProperty =
        AvaloniaProperty.Register<StepsRing, double>(nameof(Phase));

    public static readonly StyledProperty<bool> IsOnProperty =
        AvaloniaProperty.Register<StepsRing, bool>(nameof(IsOn));

    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<StepsRing, IBrush?>(nameof(Brush));

    internal const double Spread = 9;
    internal const double Alpha = 0x70 / 255d;

    private IDisposable? _seen;

    static StepsRing()
    {
        AffectsRender<StepsRing>(PhaseProperty, IsOnProperty, BrushProperty);
        IsOnProperty.Changed.AddClassHandler<StepsRing>((ring, _) => ring.Track());
    }

    public StepsRing() => IsHitTestVisible = false;

    public double Phase { get => GetValue(PhaseProperty); set => SetValue(PhaseProperty, value); }

    public bool IsOn { get => GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }

    public IBrush? Brush { get => GetValue(BrushProperty); set => SetValue(BrushProperty, value); }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Track();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _seen?.Dispose();
        _seen = null;
    }

    // Only a ring that is on watches whether it is seen; the theme's loop runs only while it is.
    private void Track()
    {
        if (IsOn && this.IsAttachedToVisualTree()) _seen ??= Seen.Track(this);
        else
        {
            _seen?.Dispose();
            _seen = null;
        }
    }

    public override void Render(DrawingContext context)
    {
        var p = Math.Clamp(Phase, 0, 1);
        if (!IsOn || p <= 0 || p >= 1 || Brush is not ISolidColorBrush brush) return;
        var c = brush.Color;
        var alpha = (byte)Math.Round(c.A * brush.Opacity * Alpha * (1 - p));
        var radius = Bounds.Width / 2 + Spread * p;
        context.DrawEllipse(new ImmutableSolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B)), null, new Rect(Bounds.Size).Center, radius, radius);
    }
}
