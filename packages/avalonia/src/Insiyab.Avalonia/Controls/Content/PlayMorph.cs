using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>Turns the play glyph into pause and back: the triangle's two halves slide apart into the two bars, eased, over <see cref="Duration"/>; at once in a still window or before the glyph is shown.</summary>
internal sealed class PlayMorph
{
    internal static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(220);

    // The halves of the triangle M6 3L20 12L6 21Z, and the bars they become, corner by corner.
    private static readonly Point[] PlayLeft = [new(6, 3), new(13, 7.5), new(13, 16.5), new(6, 21)];
    private static readonly Point[] PauseLeft = [new(6, 4), new(10, 4), new(10, 20), new(6, 20)];
    private static readonly Point[] PlayRight = [new(13, 7.5), new(20, 12), new(20, 12), new(13, 16.5)];
    private static readonly Point[] PauseRight = [new(14, 4), new(18, 4), new(18, 20), new(14, 20)];

    private readonly Icon _icon;
    private double _t;
    private double _from;
    private double _to;
    private TimeSpan? _start;
    private bool _running;

    internal PlayMorph(Icon icon, bool playing)
    {
        _icon = icon;
        _t = _to = playing ? 1 : 0;
        _icon.Data = Shape(_t);
    }

    /// <summary>How far the glyph is from play (0) to pause (1).</summary>
    internal double Progress => _t;

    internal void To(bool playing)
    {
        var target = playing ? 1.0 : 0.0;
        if (target == _to && (_running || _t == target)) return;
        _to = target;
        if (Motion.IsStill(_icon) || !_icon.IsEffectivelyVisible || TopLevel.GetTopLevel(_icon) is not { } top)
        {
            _t = target;
            _running = false;
            _icon.Data = Shape(_t);
            return;
        }
        _from = _t;
        _start = null;
        if (_running) return;
        _running = true;
        top.RequestAnimationFrame(Tick);
    }

    private void Tick(TimeSpan now)
    {
        if (!_running) return;
        _start ??= now;
        var k = Math.Clamp((now - _start.Value).TotalMilliseconds / Duration.TotalMilliseconds, 0, 1);
        var eased = 1 - Math.Pow(1 - k, 3);
        _t = _from + (_to - _from) * eased;
        if (k >= 1 || Motion.IsStill(_icon) || TopLevel.GetTopLevel(_icon) is not { } top)
        {
            _t = _to;
            _running = false;
            _icon.Data = Shape(_t);
            return;
        }
        _icon.Data = Shape(_t);
        top.RequestAnimationFrame(Tick);
    }

    private static Point Mix(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>The glyph at <paramref name="t"/>: the triangle at 0, the two bars at 1; the edge between the halves is drawn only once they part.</summary>
    internal static Geometry Shape(double t)
    {
        var l = new Point[4];
        var r = new Point[4];
        for (var i = 0; i < 4; i++)
        {
            l[i] = Mix(PlayLeft[i], PauseLeft[i], t);
            r[i] = Mix(PlayRight[i], PauseRight[i], t);
        }
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            if (t <= 0)
            {
                c.BeginFigure(l[0], true);
                c.LineTo(r[1]);
                c.LineTo(l[3]);
                c.EndFigure(true);
                return g;
            }
            foreach (var q in new[] { l, r })
            {
                c.BeginFigure(q[0], true);
                c.LineTo(q[1]);
                c.LineTo(q[2]);
                c.LineTo(q[3]);
                c.EndFigure(true);
            }
        }
        return g;
    }
}
