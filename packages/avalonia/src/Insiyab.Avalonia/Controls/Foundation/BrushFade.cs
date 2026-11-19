using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Insiyab.Avalonia.Controls;

/// <summary>The library's brush transition: colours meet in premultiplied colour, as the web's do, so a clear rest (<c>Transparent</c>, which is a clear WHITE) fades in as the target colour made clear, never through grey or white (Avalonia's own <see cref="BrushTransition"/> flashed a dark page white mid-fade), and a tint given as a colour's alpha and one given as a brush's Opacity never darken halfway. Solid colours and linear gradients (stop by stop, a solid colour taken as a gradient of the other's shape) fade; any other pair switches halfway. Use it wherever a Background, BorderBrush, Foreground, Fill or Stroke fades.</summary>
public class BrushFade : InterpolatingTransitionBase<IBrush?>
{
    protected override IBrush? Interpolate(double progress, IBrush? from, IBrush? to)
    {
        if ((from is null or ISolidColorBrush) && (to is null or ISolidColorBrush))
            return Mix(progress, Premultiplied(from as ISolidColorBrush), Premultiplied(to as ISolidColorBrush)) is { } c ? new ImmutableSolidColorBrush(c) : Brushes.Transparent;
        if (Linear(from, to) is { } shape)
            return Gradient(progress, shape, from, to);
        return progress < 0.5 ? from : to;
    }

    // the gradient whose shape both ends take (a solid colour or nothing becomes that shape in one colour); null when the two are not linear gradients alike
    private static ILinearGradientBrush? Linear(IBrush? from, IBrush? to) => (from, to) switch
    {
        (ILinearGradientBrush a, ILinearGradientBrush b) => a.GradientStops.Count == b.GradientStops.Count && a.StartPoint.Unit == b.StartPoint.Unit && a.EndPoint.Unit == b.EndPoint.Unit ? a : null,
        (ILinearGradientBrush a, null or ISolidColorBrush) => a,
        (null or ISolidColorBrush, ILinearGradientBrush b) => b,
        _ => null,
    };

    private static ImmutableLinearGradientBrush Gradient(double progress, ILinearGradientBrush shape, IBrush? from, IBrush? to)
    {
        var a = from as ILinearGradientBrush;
        var b = to as ILinearGradientBrush;
        var stops = new List<ImmutableGradientStop>(shape.GradientStops.Count);
        for (var i = 0; i < shape.GradientStops.Count; i++)
        {
            var ca = StopColour(from, a, i);
            var cb = StopColour(to, b, i);
            var offset = Lerp(a?.GradientStops[i].Offset ?? shape.GradientStops[i].Offset, b?.GradientStops[i].Offset ?? shape.GradientStops[i].Offset, progress);
            stops.Add(new ImmutableGradientStop(offset, Mix(progress, ca, cb) ?? Colors.Transparent));
        }
        var start = Point(a?.StartPoint ?? shape.StartPoint, b?.StartPoint ?? shape.StartPoint, progress);
        var end = Point(a?.EndPoint ?? shape.EndPoint, b?.EndPoint ?? shape.EndPoint, progress);
        return new ImmutableLinearGradientBrush(stops, 1, null, null, shape.SpreadMethod, start, end);
    }

    // a stop's colour premultiplied, with the brush's own opacity; a solid colour or nothing is the same at every stop
    private static (double A, double R, double G, double B) StopColour(IBrush? brush, ILinearGradientBrush? gradient, int i)
    {
        if (gradient is not null)
        {
            var c = gradient.GradientStops[i].Color;
            var a = c.A / 255.0 * gradient.Opacity;
            return (a, c.R * a, c.G * a, c.B * a);
        }
        return Premultiplied(brush as ISolidColorBrush);
    }

    private static RelativePoint Point(RelativePoint a, RelativePoint b, double p) =>
        new(Lerp(a.Point.X, b.Point.X, p), Lerp(a.Point.Y, b.Point.Y, p), a.Unit);

    private static double Lerp(double a, double b, double p) => a + (b - a) * p;

    private static Color? Mix(double progress, (double A, double R, double G, double B) f, (double A, double R, double G, double B) t)
    {
        var alpha = Lerp(f.A, t.A, progress);
        if (alpha <= 0) return null;
        byte Channel(double x, double y) => (byte)Math.Clamp(Math.Round(Lerp(x, y, progress) / alpha), 0, 255);
        return Color.FromArgb((byte)Math.Round(alpha * 255), Channel(f.R, t.R), Channel(f.G, t.G), Channel(f.B, t.B));
    }

    private static (double A, double R, double G, double B) Premultiplied(ISolidColorBrush? brush)
    {
        if (brush is null) return (0, 0, 0, 0);
        var a = brush.Color.A / 255.0 * brush.Opacity;
        return (a, brush.Color.R * a, brush.Color.G * a, brush.Color.B * a);
    }
}
