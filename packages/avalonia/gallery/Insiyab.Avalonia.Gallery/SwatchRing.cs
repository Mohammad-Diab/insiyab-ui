using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Insiyab.Avalonia.Gallery;

/// <summary>The one ring that marks the chosen brand swatch: it rolls along the row to a newly chosen swatch like a wheel, settles on it, and the chosen disc pops as it arrives. A still window or a first display places it without motion.</summary>
internal sealed class SwatchRing
{
    private const double Half = 18;
    private const double Flight = 380;
    private const double Settle = 180;
    private const double PopFrom = 140;
    private const double PopFor = 260;

    private static readonly Easing Spring = new SplineEasing(0.34, 1.56, 0.64, 1);
    private static readonly Easing Out = new SplineEasing(0.22, 1, 0.36, 1);
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    /// <summary>For tests: the milliseconds the motion runs on.</summary>
    internal static Func<double> Now { get; set; } = () => Clock.Elapsed.TotalMilliseconds;

    private readonly ShapePath _ring;
    private Control? _swatch;
    private Control? _face;
    private Point _at;
    private double _turn;
    private double _scale = 1;
    private int _run;
    private bool _flying;
    private Point _to;
    private double _turnTo;

    public SwatchRing(ShapePath ring)
    {
        _ring = ring;
        ring.DetachedFromVisualTree += (_, _) => Land();
    }

    /// <summary>Follows a swatch's place in the row, so the ring stays on it when the row is laid out or resized, and grows with it while it is hovered.</summary>
    public void Watch(Control swatch) =>
        swatch.PropertyChanged += (_, e) =>
        {
            if (swatch != _swatch) return;
            if (e.Property == Visual.RenderTransformProperty && !_flying && _ring.Opacity > 0)
            {
                _scale = Scale(swatch);
                _ring.RenderTransform = Transform(_at, _turn, _scale);
                return;
            }
            if (e.Property != Visual.BoundsProperty || Target(swatch) is not { } to) return;
            Stop();
            Place(to, _turn);
        };

    // The swatch's own size against its rest, which grows on hover.
    private static double Scale(Control? swatch) => swatch?.RenderTransform?.Value.M11 ?? 1;

    /// <summary>Marks this swatch, or none: the ring flies there from the swatch it marked, unless it is shown for the first time or motion is still.</summary>
    public void Mark(Control? swatch)
    {
        var before = _swatch;
        _swatch = swatch;
        if (swatch is null)
        {
            Stop();
            _ring.Opacity = 0;
            return;
        }
        if (swatch is Button { Background: { } fill }) _ring.Stroke = fill;
        if (swatch == before || Target(swatch) is not { } to) return;
        if (before is not null && _ring.Opacity > 0 && TopLevel.GetTopLevel(_ring) is { } top && !Motion.IsStill(_ring)) Fly(top, swatch, to);
        else
        {
            Stop();
            Place(to, _turn);
        }
    }

    // The ring's box is laid at the row's origin, so its place is the transform that carries its centre onto the swatch's.
    private static Point? Target(Control swatch) =>
        swatch.Bounds is { Width: >= 1 } b ? new Point(b.X + b.Width / 2 - Half, b.Y + b.Height / 2 - Half) : null;

    private void Place(Point at, double turn)
    {
        _at = at;
        _turn = Math.Round(turn / 90) * 90 % 360;
        _scale = Scale(_swatch);
        _ring.RenderTransform = Transform(at, _turn, _scale);
        _ring.Opacity = 1;
    }

    private void Stop()
    {
        _run++;
        _flying = false;
        if (_face is not null) _face.RenderTransform = null;
        _face = null;
    }

    private void Land()
    {
        if (!_flying) return;
        var (to, turn) = (_to, _turnTo);
        Stop();
        Place(to, turn);
    }

    // A quarter turn for each stretch of the ring's own circumference it rolls, at least one, towards the side it travels so the roll reads the same in either direction.
    private void Fly(TopLevel top, Control swatch, Point to)
    {
        Stop();
        var run = _run;
        var (from, turn0, scale0) = (_at, _turn, _scale);
        var (dx, dy) = (to.X - from.X, to.Y - from.Y);
        var quarters = Math.Max(1, (int)Math.Round(double.Hypot(dx, dy) / (Math.PI / 2 * Half)));
        var turn1 = turn0 + (dx < 0 ? -1 : 1) * 90 * quarters;
        (_flying, _to, _turnTo) = (true, to, turn1);
        var face = swatch.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "PART_Face");
        _face = face;
        var start = Now();

        void Frame(TimeSpan _)
        {
            if (run != _run) return;
            var ms = Now() - start;
            if (ms >= Flight + Settle)
            {
                Land();
                return;
            }
            var k = Spring.Ease(Math.Clamp(ms / Flight, 0, 1));
            _at = new Point(from.X + dx * k, from.Y + dy * k);
            _turn = turn0 + (turn1 - turn0) * k;
            _scale = ms < Flight ? scale0 + (1.12 - scale0) * k : 1.12 - (1.12 - Scale(swatch)) * Out.Ease((ms - Flight) / Settle);
            _ring.RenderTransform = Transform(_at, _turn, _scale);
            if (face is not null) face.RenderTransform = Pop(ms - PopFrom) is var p && p != 1 ? Transform(default, 0, p) : null;
            top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }

    // The chosen disc's press and pop: 1 to 0.9, up to 1.06, back to 1.
    private static double Pop(double ms)
    {
        var p = ms / PopFor;
        if (p <= 0 || p >= 1) return 1;
        if (p < 0.15) return 1 - 0.1 * Out.Ease(p / 0.15);
        if (p < 0.55) return 0.9 + 0.16 * Out.Ease((p - 0.15) / 0.4);
        return 1.06 - 0.06 * Out.Ease((p - 0.55) / 0.45);
    }

    // One matrix, scaled and turned about the ring's centre first and carried to the swatch last, whatever order a list of operations would compose in.
    private static ITransform Transform(Point at, double turn, double scale)
    {
        var ops = new TransformOperations.Builder(1);
        ops.AppendMatrix(Matrix.CreateScale(scale, scale) * Matrix.CreateRotation(turn * Math.PI / 180) * Matrix.CreateTranslation(at.X, at.Y));
        return ops.Build();
    }
}
