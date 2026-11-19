using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's page turn for a change of direction (a language switch, the demo's and the site's <c>site-dir-flip</c>): a picture of the window as it was frosts away (0.3s: it fades, blurs to 16 and darkens to 0.9) while the new page opens like a book's page from the start edge of its new direction (0.5s: from a 28° turn in a 1600 perspective, blurred 16, to flat and sharp; fully shown by 60%), on the library's ease. A second change while one runs lands the first and starts again from what is on screen. Instant, with no picture, while <see cref="Motion.IsStill"/> or while the window is minimised or hidden; with light effects (<see cref="InsiyabTheme.Effects"/>), or when the app asks for none on a device it finds weak, the page still turns, with no blur.</summary>
public static class DirectionFlip
{
    private const double OutTime = 300;
    private const double InTime = 500;
    private const double ShownBy = 0.6;
    private const double Turn = 28;
    private const double Depth = 1600;
    private const double Blur = 16;
    private const double Dim = 0.1;

    private static readonly System.Diagnostics.Stopwatch s_clock = System.Diagnostics.Stopwatch.StartNew();
    private static Flip? s_current;
    private static Waiting? s_waiting;

    /// <summary>For tests: while set, a flip that starts keeps its first frame and does not land on its own, so it can be read mid-way.</summary>
    internal static bool Held { get; set; }

    /// <summary>For tests and frame strips: the clock the flip reads, in milliseconds.</summary>
    internal static Func<double> Now { get; set; } = () => s_clock.Elapsed.TotalMilliseconds;

    /// <summary>Takes a picture of the window <paramref name="within"/> is in, runs <paramref name="change"/> (the language and the direction, the views built again) once the picture is taken (a frame later, as the render thread draws it), then turns the new page in over the picture as it frosts away. The page opens from the start edge of the window's direction after the change: the left in a left-to-right window, the right in a right-to-left one.</summary>
    /// <param name="within">Any visual in the window that turns.</param>
    /// <param name="change">The change, run once inside the motion (at once when there is none).</param>
    /// <param name="blur">False drops the blur of both pages (a device the app finds weak); the page still turns. Light effects drop it anyway.</param>
    public static void Run(Visual within, Action change, bool blur = true)
    {
        ArgumentNullException.ThrowIfNull(within);
        ArgumentNullException.ThrowIfNull(change);
        s_current?.Land();
        s_waiting?.Apply();
        var top = OverlayHost.Top(within);
        var layer = top is null ? null : OverlayHost.Layer(top);
        var size = top?.Bounds.Size ?? default;
        if (top is null || layer is null || Content(top) is null || Motion.IsStill(top) || !Seen.WindowShown(top) || size.Width < 1 || size.Height < 1)
        {
            change();
            return;
        }
        if (ElementComposition.GetElementVisual(top) is { } visual) Capture(top, layer, size, visual, change, blur);
        else Start(top, layer, size, Snapshot.Take(top, size, top.RenderScaling), change, blur);
    }

    // What turns: everything the window shows over its ground (the content presenter under the layers of adorners and overlays), as a picture of its page.
    private static Control? Content(TopLevel top) =>
        top.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault()?.Child as Control;

    // The render thread draws the picture, so the UI thread keeps its frames meanwhile; the change runs when it arrives.
    private static async void Capture(TopLevel top, AdornerLayer layer, Size size, CompositionVisual visual, Action change, bool blur)
    {
        var waiting = s_waiting = new Waiting(change);
        Bitmap? bitmap;
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
        if (bitmap is null || Motion.IsStill(top) || !Seen.WindowShown(top) || top.Bounds.Size != size)
        {
            bitmap?.Dispose();
            change();
            return;
        }
        Start(top, layer, size, bitmap, change, blur);
    }

    private static void Start(TopLevel top, AdornerLayer layer, Size size, Bitmap bitmap, Action change, bool blur)
    {
        s_current?.Land();
        var light = !blur || Motion.IsLight(top);
        // the old window: its picture, darkened by a veil of black (brightness 0.9 is a tenth of black over it), blurred and faded as one
        var veil = new Border { Background = Brushes.Black, Opacity = 0, IsHitTestVisible = false };
        var old = new Panel
        {
            IsHitTestVisible = false,
            Children = { new Image { Source = new ThemeSweep.Pixels(bitmap), Width = size.Width, Height = size.Height, Stretch = Stretch.Fill }, veil },
        };
        var picture = new Panel
        {
            Name = "PART_DirectionFlip",
            Width = size.Width,
            Height = size.Height,
            FlowDirection = FlowDirection.LeftToRight,
            IsHitTestVisible = false,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
        };
        // Behind the turning page is the window's ground, as the web's page canvas. A window of the library's draws its own under its content; a view with none (a phone's, whose page paints its own ground and turns with it) gets the ground's colour here, under the old picture, for the motion's time.
        if (!top.GetVisualDescendants().OfType<FrostGround>().Any())
        {
            var ground = new Border { IsHitTestVisible = false };
            ground.Bind(Border.BackgroundProperty, ground.GetResourceObservable("InsWash1"));
            picture.Children.Add(ground);
        }
        picture.Children.Add(old);
        // A plain child, not an adorner of the layer: the layer places its adorners mirrored in RTL.
        layer.Children.Add(picture);
        var flip = s_current = new Flip(layer, picture, bitmap);
        // the page shows nothing until its own picture turns in: the old picture stands in for it meanwhile
        var content = Content(top);
        if (content is not null) flip.Hold(content);
        change();
        // The new page turns as a picture of the window, as the web's view transition does: a page drawn live under a turn and a blur is drawn whole again on every frame (about 30 frames a second for the desktop's shell), its picture once. It is taken once the change is laid out, after what the change itself put off to its first layout (a phone's page put back where it was).
        if (content is null || !ReferenceEquals(content, Content(top)) || (content as ContentPresenter)?.Child is not { } page)
        {
            flip.Land();
            return;
        }
        Dispatcher.UIThread.Post(() => TakeNew(top, page, flip, old, veil, light), DispatcherPriority.Loaded);
    }

    private static void TakeNew(TopLevel top, Control page, Flip flip, Panel old, Border veil, bool light)
    {
        if (flip.Done) return;
        if (!ReferenceEquals(TopLevel.GetTopLevel(page), top) || Motion.IsStill(top) || !Seen.WindowShown(top))
        {
            flip.Land();
            return;
        }
        top.UpdateLayout();
        var size = top.Bounds.Size;
        // The whole window as the change left it, drawn here on the UI thread with the old picture out of the way and the page shown, both put back before the frame ends, so the screen never sees it: its ground, its bar's badges and its overlays too, as the web's new root (a page drawn alone over nothing came out with its text heavier than on screen).
        Bitmap? bitmap = null;
        flip.Unveiled(() =>
        {
            try
            {
                bitmap = size.Width < 1 || size.Height < 1 ? null : Snapshot.Take(top, size, top.RenderScaling);
            }
            catch (Exception)
            {
                bitmap = null;
            }
        });
        if (bitmap is null)
        {
            flip.Land();
            return;
        }
        var rtl = top.FlowDirection == FlowDirection.RightToLeft;
        var image = new Image { Source = new ThemeSweep.Pixels(bitmap), Width = size.Width, Height = size.Height, Stretch = Stretch.Fill };
        var turn = new MatrixTransform(Turned(Turn, rtl));
        var shot = new Border
        {
            Name = "PART_DirectionFlipNew",
            Child = image,
            Width = size.Width,
            Height = size.Height,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
            FlowDirection = FlowDirection.LeftToRight,
            IsHitTestVisible = false,
            Opacity = 0,
            // the page opens from its direction's start edge: the screen's left in English, its right in Arabic
            RenderTransformOrigin = new RelativePoint(rtl ? 1 : 0, 0.5, RelativeUnit.Relative),
            RenderTransform = turn,
        };
        flip.Add(shot, bitmap);

        var easing = Motion.Ease(top);
        var oldBlur = light ? null : new BlurEffect { Radius = 0 };
        var newBlur = light ? null : new BlurEffect { Radius = Blur };
        old.Effect = oldBlur;
        shot.Effect = newBlur;
        var start = Now();
        void Step()
        {
            var ms = Held ? 0 : Now() - start;
            var gone = easing.Ease(Math.Clamp(ms / OutTime, 0, 1));
            var turned = easing.Ease(Math.Clamp(ms / InTime, 0, 1));
            var shown = easing.Ease(Math.Clamp(ms / (InTime * ShownBy), 0, 1));
            old.Opacity = 1 - gone;
            veil.Opacity = Dim * gone;
            if (oldBlur is not null) oldBlur.Radius = Blur * gone;
            shot.Opacity = shown;
            if (newBlur is not null) newBlur.Radius = Blur * (1 - turned);
            turn.Matrix = Turned(Turn * (1 - turned), rtl);
            // a blur costs a frame its time: once the old picture is all but gone, and the page all but sharp, they are drawn without one
            old.IsVisible = old.Opacity > 0.01;
            if (newBlur is not null && newBlur.Radius < 0.5) shot.Effect = null;
        }
        Step();
        DispatcherTimer.RunOnce(() => { if (!Held && Now() - start >= InTime) flip.Land(); }, TimeSpan.FromMilliseconds(InTime + 150));
        void Frame(TimeSpan _)
        {
            if (flip.Done) return;
            Step();
            if (!Held && Now() - start >= InTime) flip.Land(); else top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }

    // CSS's perspective(1600px) rotateY(angle) about the picture's start edge (its render origin; the web's transform-origin: left or right, 50%): the far edge goes back and shrinks toward the middle, the start edge stays. About the right edge the turn is the web's rotateY(-angle), the same seen in a mirror.
    internal static Matrix Turned(double degrees, bool rtl)
    {
        var a = degrees * Math.PI / 180;
        return new Matrix(Math.Cos(a), 0, (rtl ? -1 : 1) * Math.Sin(a) / Depth, 0, 1, 0, 0, 0, 1);
    }

    // A change whose picture is still being drawn: it is applied by whoever comes first, the picture's arrival or a second change.
    private sealed class Waiting(Action change)
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
            if (Take()) change();
        }
    }

    private sealed class Flip(AdornerLayer layer, Panel picture, Bitmap bitmap)
    {
        private Control? _content;
        private Kept _opacity;
        private Bitmap? _newBitmap;

        public bool Done { get; private set; }

        // The presenter shows nothing until the flip lands (the pictures stand in for it); its own opacity is given back then.
        public void Hold(Control content)
        {
            _content = content;
            _opacity = Kept.Of(content, Visual.OpacityProperty);
            content.Opacity = 0;
        }

        // Runs draw with the pictures hidden and the page shown, then holds them as they were (no frame is drawn in between).
        public void Unveiled(Action draw)
        {
            var shown = picture.IsVisible;
            picture.IsVisible = false;
            var held = _content?.Opacity;
            if (_content is { } c) _opacity.Restore(c);
            try { draw(); }
            finally
            {
                picture.IsVisible = shown;
                if (_content is { } d && held is { } o) d.Opacity = o;
            }
        }

        // The new page's picture, over the old one.
        public void Add(Control shot, Bitmap newBitmap)
        {
            _newBitmap = newBitmap;
            picture.Children.Add(shot);
        }

        public void Land()
        {
            if (Done) return;
            Done = true;
            layer.Children.Remove(picture);
            bitmap.Dispose();
            _newBitmap?.Dispose();
            if (_content is { } c) _opacity.Restore(c);
            if (s_current == this) s_current = null;
        }
    }

    // A property's own value before the flip, given back as it was: set again, or cleared.
    private readonly struct Kept
    {
        private readonly AvaloniaProperty? _property;
        private readonly bool _set;
        private readonly object? _value;

        private Kept(AvaloniaProperty property, bool set, object? value)
        {
            _property = property;
            _set = set;
            _value = value;
        }

        public static Kept Of(AvaloniaObject o, AvaloniaProperty property) => new(property, o.IsSet(property), o.GetValue(property));

        public void Restore(AvaloniaObject o)
        {
            if (_property is null) return;
            if (_set) o.SetValue(_property, _value);
            else o.ClearValue(_property);
        }
    }
}
