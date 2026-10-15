using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's theme sweep, over 600ms from the control that was pressed: switching to light, the new palette grows out of it in a circle to the window's furthest corner (the light switching on); switching to dark, the old light picture stays inside a circle that closes from the window's edges onto it (the light switching off). Skipped, and the theme simply applied, while <see cref="Motion.IsStill"/> or while the window is minimised or hidden.</summary>
public static class ThemeSweep
{
    private const double Duration = 600;
    private static Sweep? s_current;
    private static Waiting? s_waiting;

    /// <summary>For tests: while set, a sweep that starts keeps its first frame and does not land on its own, so it can be read mid-way.</summary>
    internal static bool Held { get; set; }

    /// <summary>Takes a picture of the window, runs <paramref name="apply"/> (the theme change) as soon as the picture is taken (a frame later, as the render thread draws it), then wipes the picture away from <paramref name="origin"/>, or closes it onto <paramref name="origin"/> when the window is now dark.</summary>
    public static void Run(Visual origin, Action apply)
    {
        var top = OverlayHost.Top(origin);
        var layer = top is null ? null : OverlayHost.Layer(top);
        var size = top?.Bounds.Size ?? default;
        if (top is null || layer is null || Motion.IsStill(origin) || !Seen.WindowShown(top) || size.Width < 1 || size.Height < 1)
        {
            apply();
            return;
        }
        s_current?.Land();
        s_waiting?.Apply();
        if (ElementComposition.GetElementVisual(top) is { } visual) Capture(origin, top, layer, size, visual, apply);
        else Start(origin, top, layer, size, Snapshot.Take(top, size, top.RenderScaling), apply);
    }

    // The render thread draws the picture, so the UI thread keeps its frames meanwhile; the theme changes when it arrives.
    private static async void Capture(Visual origin, TopLevel top, AdornerLayer layer, Size size, CompositionVisual visual, Action apply)
    {
        var waiting = s_waiting = new Waiting(apply);
        Bitmap? bitmap = null;
        try
        {
            bitmap = await visual.Compositor.CreateCompositionVisualSnapshot(visual, top.RenderScaling);
        }
        catch (Exception)
        {
            bitmap = null;
        }
        if (!waiting.Take())
        {
            bitmap?.Dispose();
            return;
        }
        if (bitmap is null || Motion.IsStill(origin) || !Seen.WindowShown(top) || top.Bounds.Size != size)
        {
            bitmap?.Dispose();
            apply();
            return;
        }
        Start(origin, top, layer, size, bitmap, apply);
    }

    private static void Start(Visual origin, TopLevel top, AdornerLayer layer, Size size, Bitmap bitmap, Action apply)
    {
        s_current?.Land();
        var centre = RestCentre(origin, top);
        var far = new[] { new Point(0, 0), new Point(size.Width, 0), new Point(0, size.Height), new Point(size.Width, size.Height) }
            .Max(p => Math.Sqrt((p.X - centre.X) * (p.X - centre.X) + (p.Y - centre.Y) * (p.Y - centre.Y)));
        var picture = new Image
        {
            Name = "PART_ThemeSweep",
            Source = new Pixels(bitmap),
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
        var closing = top.ActualThemeVariant == ThemeVariant.Dark;
        var radius = Math.Ceiling(far);

        var sweep = s_current = new Sweep(layer, picture, bitmap);
        var easing = Motion.Ease(origin as Control ?? top);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        picture.Clip = closing ? Circle(centre, radius) : Hole(size, centre, 0);
        DispatcherTimer.RunOnce(() => { if (!Held) sweep.Land(); }, TimeSpan.FromMilliseconds(Duration + 150));
        void Frame(TimeSpan _)
        {
            if (sweep.Done) return;
            var t = Held ? 0 : Math.Clamp(clock.Elapsed.TotalMilliseconds / Duration, 0, 1);
            var p = easing.Ease(t);
            picture.Clip = closing ? Circle(centre, radius * (1 - p)) : Hole(size, centre, radius * p);
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
        new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(size)), Circle(centre, radius));

    private static EllipseGeometry Circle(Point centre, double radius) =>
        new(new Rect(centre.X - radius, centre.Y - radius, radius * 2, radius * 2));

    // An Image reads a scaled bitmap's DIP size as pixels and crops it, so the bitmap is shown in pixels: the whole of it, whatever its density, over the Image's own box.
    private sealed class Pixels(Bitmap bitmap) : IImage
    {
        public Size Size => new(bitmap.PixelSize.Width, bitmap.PixelSize.Height);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) => ((IImage)bitmap).Draw(context, sourceRect, destRect);
    }

    // A click whose picture is still being drawn: its theme is applied by whoever comes first, the picture's arrival or a second click.
    private sealed class Waiting(Action apply)
    {
        private bool _done;

        public bool Take()
        {
            if (_done) return false;
            _done = true;
            if (s_waiting == this) s_waiting = null;
            return true;
        }

        public void Apply()
        {
            if (Take()) apply();
        }
    }

    private sealed class Sweep(AdornerLayer layer, Image picture, Bitmap bitmap)
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
