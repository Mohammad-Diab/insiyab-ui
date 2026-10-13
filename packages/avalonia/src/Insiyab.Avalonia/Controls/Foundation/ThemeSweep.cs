using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's theme sweep: the new palette is revealed through a circle that grows from the control that was pressed out to the window's furthest corner, over 600ms. Skipped, and the theme simply applied, while <see cref="Motion.IsStill"/>.</summary>
public static class ThemeSweep
{
    private const double Duration = 600;
    private static Sweep? s_current;

    /// <summary>Takes a picture of the window, runs <paramref name="apply"/> (the theme change) at once, then wipes the picture away from <paramref name="origin"/>.</summary>
    public static void Run(Visual origin, Action apply)
    {
        var top = OverlayHost.Top(origin);
        var layer = top is null ? null : OverlayHost.Layer(top);
        var size = top?.Bounds.Size ?? default;
        if (top is null || layer is null || Motion.IsStill(origin) || size.Width < 1 || size.Height < 1)
        {
            apply();
            return;
        }
        s_current?.Land();

        var bitmap = Snapshot.Take(top, size, top.RenderScaling);

        var centre = RestCentre(origin, top);
        var far = new[] { new Point(0, 0), new Point(size.Width, 0), new Point(0, size.Height), new Point(size.Width, size.Height) }
            .Max(p => Math.Sqrt((p.X - centre.X) * (p.X - centre.X) + (p.Y - centre.Y) * (p.Y - centre.Y)));
        var picture = new Image
        {
            Name = "PART_ThemeSweep",
            Source = bitmap,
            Width = size.Width,
            Height = size.Height,
            Stretch = Stretch.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            IsHitTestVisible = false,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
        };
        // A plain child, not an adorner of the layer: the layer places its adorners mirrored in RTL.
        layer.Children.Add(picture);

        apply();

        var sweep = s_current = new Sweep(layer, picture, bitmap);
        var easing = Motion.Ease(origin as Control ?? top);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        picture.Clip = Hole(size, centre, 0);
        DispatcherTimer.RunOnce(sweep.Land, TimeSpan.FromMilliseconds(Duration + 150));
        void Frame(TimeSpan _)
        {
            if (sweep.Done) return;
            var t = Math.Clamp(clock.Elapsed.TotalMilliseconds / Duration, 0, 1);
            picture.Clip = Hole(size, centre, Math.Ceiling(far) * easing.Ease(t));
            if (t >= 1) sweep.Land(); else top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }

    // From where the control rests, not where its hover lift or press dip has moved it this instant.
    private static Point RestCentre(Visual origin, TopLevel top)
    {
        var parent = origin.GetVisualParent();
        var at = parent is null ? null : parent.TranslatePoint(origin.Bounds.Center, top);
        return at ?? new Point(top.Bounds.Width / 2, 0);
    }

    private static Geometry Hole(Size size, Point centre, double radius) =>
        new CombinedGeometry(GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(size)),
            new EllipseGeometry(new Rect(centre.X - radius, centre.Y - radius, radius * 2, radius * 2)));

    private sealed class Sweep(AdornerLayer layer, Image picture, RenderTargetBitmap bitmap)
    {
        public bool Done { get; private set; }

        public void Land()
        {
            if (Done) return;
            Done = true;
            layer.Children.Remove(picture);
            bitmap.Dispose();
            if (s_current == this) s_current = null;
        }
    }
}
