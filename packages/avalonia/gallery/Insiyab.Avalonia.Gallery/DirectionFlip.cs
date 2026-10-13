using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery;

/// <summary>The docs site's change of direction: the old page frosts over and fades in 300ms, the new one opens from its start edge like a book in 500ms. Skipped while the window is still.</summary>
internal static class DirectionFlip
{
    private const double Out = 300;
    private const double In = 500;
    private static readonly Easing Ease = new SplineEasing(0.22, 1, 0.36, 1);
    private static Action? s_land;

    public static void Run(Control page, Action apply)
    {
        var top = TopLevel.GetTopLevel(page);
        var layer = AdornerLayer.GetAdornerLayer(page);
        var size = top?.Bounds.Size ?? default;
        if (top is null || layer is null || Motion.IsStill(page) || size.Width < 1 || size.Height < 1)
        {
            apply();
            return;
        }
        s_land?.Invoke();

        var scale = top.RenderScaling;
        var bitmap = new RenderTargetBitmap(
            new PixelSize(Math.Max(1, (int)Math.Ceiling(size.Width * scale)), Math.Max(1, (int)Math.Ceiling(size.Height * scale))),
            new Vector(96 * scale, 96 * scale));
        bitmap.Render(top);
        var old = new Image
        {
            Source = bitmap,
            Width = size.Width,
            Height = size.Height,
            Stretch = Stretch.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            IsHitTestVisible = false,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
        };
        layer.Children.Add(old);
        AdornerLayer.SetAdornedElement(old, layer);

        apply();

        // The hinge is the page's start edge, which its own mirrored coordinates put at 0 on either side.
        var book = new Rotate3DTransform { Depth = 1600 };
        page.RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative);
        page.RenderTransform = book;
        var oldBlur = new BlurEffect();
        var newBlur = new BlurEffect();
        old.Effect = oldBlur;
        page.Effect = newBlur;
        page.Opacity = 0;

        var done = false;
        void Land()
        {
            if (done) return;
            done = true;
            s_land = null;
            layer.Children.Remove(old);
            bitmap.Dispose();
            page.RenderTransform = null;
            page.Effect = null;
            page.Opacity = 1;
        }
        s_land = Land;
        DispatcherTimer.RunOnce(Land, TimeSpan.FromMilliseconds(In + 150));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        void Frame(TimeSpan _)
        {
            if (done) return;
            var ms = clock.Elapsed.TotalMilliseconds;
            var o = Ease.Ease(Math.Clamp(ms / Out, 0, 1));
            old.Opacity = 1 - o;
            oldBlur.Radius = 16 * o;
            var e = Ease.Ease(Math.Clamp(ms / In, 0, 1));
            page.Opacity = Ease.Ease(Math.Clamp(ms / (In * 0.6), 0, 1));
            newBlur.Radius = 16 * (1 - e);
            book.AngleY = 28 * (1 - e);
            if (ms >= In) Land(); else top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }
}
