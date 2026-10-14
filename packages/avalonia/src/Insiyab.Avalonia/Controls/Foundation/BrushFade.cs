using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Insiyab.Avalonia.Controls;

/// <summary>A brush transition that fades as the web does: solid colours meet in premultiplied colour, so a tint given as a colour's alpha and one given as a brush's Opacity never darken halfway. For solid brushes only; any other brush switches halfway.</summary>
public class BrushFade : InterpolatingTransitionBase<IBrush?>
{
    protected override IBrush? Interpolate(double progress, IBrush? from, IBrush? to)
    {
        if (from is not (null or ISolidColorBrush) || to is not (null or ISolidColorBrush)) return progress < 0.5 ? from : to;
        var (fa, fr, fg, fb) = Premultiplied(from as ISolidColorBrush);
        var (ta, tr, tg, tb) = Premultiplied(to as ISolidColorBrush);
        var alpha = fa + (ta - fa) * progress;
        if (alpha <= 0) return Brushes.Transparent;
        byte Channel(double x, double y) => (byte)Math.Clamp(Math.Round((x + (y - x) * progress) / alpha), 0, 255);
        return new ImmutableSolidColorBrush(Color.FromArgb((byte)Math.Round(alpha * 255), Channel(fr, tr), Channel(fg, tg), Channel(fb, tb)));
    }

    private static (double A, double R, double G, double B) Premultiplied(ISolidColorBrush? brush)
    {
        if (brush is null) return (0, 0, 0, 0);
        var a = brush.Color.A / 255.0 * brush.Opacity;
        return (a, brush.Color.R * a, brush.Color.G * a, brush.Color.B * a);
    }
}
