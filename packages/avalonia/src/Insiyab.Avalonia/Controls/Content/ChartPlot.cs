using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Insiyab.Avalonia.Controls;

/// <summary>The drawing of a <see cref="Chart"/>. Positions are physical, worked out for either direction as the web plugin does, so this part stays left to right.</summary>
public class ChartPlot : Control
{
    private const double BarGrow = 280, LineDraw = 800, SliceFade = 280, LitFade = 220;

    private readonly List<Hit> _hits = [];
    private readonly Stopwatch _clock = new();
    private readonly SplineEasing _ease = new(0.22, 1, 0.36, 1);
    private readonly Dictionary<(IBrush?, double, PenLineCap), IPen> _pens = new();
    private int _lit = -1;
    private int _cross = -1;
    private bool _framing;
    private (double X0, double Step, int N, bool Rtl, double W, double Y0, double Y1) _axis;
    private readonly Stopwatch _litClock = Stopwatch.StartNew();
    private readonly Dictionary<(int Kind, int Series, int Index), Fade> _fades = new();
    private bool _fading;
    private bool _fadeFrame;

    // What hovering changes on a mark: a bar's opacity, a dot's radius, a slice's dimming.
    internal const int BarKind = 0, DotKind = 1, SliceKind = 2;

    private sealed class Fade(double value)
    {
        public double From = value, To = value, Start = double.MinValue;
    }

    internal Chart? Owner { get; set; }

    /// <summary>The index of the mark under the pointer, or -1.</summary>
    internal int LitIndex => _lit;

    /// <summary>The category the crosshair is on (line and area), or -1.</summary>
    internal int CrossIndex => _cross;

    /// <summary>The mark that is lit, or null.</summary>
    internal Hit? LitHit => _lit >= 0 && _lit < _hits.Count ? _hits[_lit] : null;

    internal int PenCount => _pens.Count;

    internal IReadOnlyList<Hit> Hits => _hits;

    internal void ResetPens() => _pens.Clear();

    // One immutable pen per brush, thickness and cap, kept until the brushes or the size change.
    internal IPen PenFor(IBrush? brush, double thickness, PenLineCap cap = PenLineCap.Flat)
    {
        var key = (brush, Math.Round(thickness, 2), cap);
        if (!_pens.TryGetValue(key, out var pen))
            _pens[key] = pen = new ImmutablePen((brush ?? Brushes.Gray).ToImmutable(), thickness, null, cap, cap == PenLineCap.Round ? PenLineJoin.Round : PenLineJoin.Miter);
        return pen;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ResetPens();
    }

    /// <summary>A mark the pointer can land on: its box (or ring sector), and what the tooltip says.</summary>
    internal sealed record Hit(Rect Box, string Label, string Name, double Value, int Series, int Index, Point? Center = null, double R0 = 0, double R1 = 0, double A0 = 0, double A1 = 0, double Radius = 0, Rect? Area = null)
    {
        public bool Contains(Point p)
        {
            if (Center is { } c)
            {
                if (A1 > A0)
                {
                    var dx = p.X - c.X;
                    var dy = p.Y - c.Y;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < R0 || d > R1) return false;
                    var a = Math.Atan2(dy, dx) + Math.PI / 2;
                    if (a < 0) a += 2 * Math.PI;
                    return a >= A0 && a <= A1;
                }
                var ex = p.X - c.X;
                var ey = p.Y - c.Y;
                return ex * ex + ey * ey <= (Radius + 4) * (Radius + 4);
            }
            return (Area ?? Box).Contains(p);
        }
    }

    public ChartPlot()
    {
        ClipToBounds = false;
    }

    // Draws in when first seen; out of sight the clock stays stopped and the plot shows its end state.
    internal void Replay() => Seen.WhenSeen(this, ReplayNow);

    internal bool IsDrawing => _clock.IsRunning;

    private void ReplayNow()
    {
        _fades.Clear();
        _clock.Restart();
        Frame();
    }

    private double Elapsed => _clock.IsRunning ? _clock.Elapsed.TotalMilliseconds : double.MaxValue;

    private void Frame()
    {
        if (_framing) return;
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        _framing = true;
        top.RequestAnimationFrame(_ =>
        {
            _framing = false;
            InvalidateVisual();
            if (Elapsed < LineDraw + 400) Frame();
            else _clock.Stop();
        });
    }

    /// <summary>A mark's hover value as it is now (a bar's opacity, a dot's radius, a slice's dimming), or NaN before it is drawn.</summary>
    internal double FadeOf(int kind, int series, int index) =>
        _fades.TryGetValue((kind, series, index), out var f) ? FadeValue(f, _litClock.Elapsed.TotalMilliseconds) : double.NaN;

    // A mark eases to its lit or dimmed look over 220ms, as the web's opacity and r transitions do.
    private double Toward(int kind, int series, int index, double target)
    {
        var key = (kind, series, index);
        var now = _litClock.Elapsed.TotalMilliseconds;
        if (!_fades.TryGetValue(key, out var f) || Motion.IsStill(this))
        {
            _fades[key] = new Fade(target);
            return target;
        }
        var current = FadeValue(f, now);
        if (f.To != target)
        {
            f.From = current;
            f.To = target;
            f.Start = now;
        }
        if (now - f.Start < LitFade) _fading = true;
        return current;
    }

    private double FadeValue(Fade f, double now)
    {
        var p = (now - f.Start) / LitFade;
        return p >= 1 ? f.To : p <= 0 ? f.From : f.From + (f.To - f.From) * _ease.Ease(p);
    }

    private void FadeFrame()
    {
        if (_fadeFrame || TopLevel.GetTopLevel(this) is not { } top) return;
        _fadeFrame = true;
        top.RequestAnimationFrame(_ =>
        {
            _fadeFrame = false;
            InvalidateVisual();
        });
    }

    private double Progress(double delay, double length)
    {
        var t = (Elapsed - delay) / length;
        return t >= 1 ? 1 : t <= 0 ? 0 : _ease.Ease(t);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Track(e.GetPosition(this));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Pointer.Type == PointerType.Mouse) return;
        Owner?.Focus(NavigationMethod.Pointer);
        Track(e.GetPosition(this));
    }

    private void Track(Point p)
    {
        if (Owner is { Type: ChartType.Line or ChartType.Area } && _axis.N > 0)
        {
            var along = _axis.Rtl ? _axis.W - p.X : p.X;
            var i = _axis.N == 1 ? 0 : (int)Math.Round((along - _axis.X0) / _axis.Step);
            LightCategory(Math.Clamp(i, 0, _axis.N - 1));
            return;
        }
        var at = -1;
        for (var i = _hits.Count - 1; i >= 0; i--)
            if (_hits[i].Contains(p)) { at = i; break; }
        if (at == _lit) return;
        _lit = at;
        InvalidateVisual();
        if (at < 0) Owner?.HideTip();
        else
        {
            var h = _hits[at];
            Owner?.ShowTip(h.Label, h.Name, h.Value, h.Box);
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (e.Pointer.Type == PointerType.Mouse) Clear();
    }

    // The value under the pointer for a test or a keyboard-less caller: lights mark i as hovering it would.
    internal void LightAt(int index)
    {
        _cross = -1;
        _lit = index;
        InvalidateVisual();
        if (index >= 0 && index < _hits.Count)
        {
            var h = _hits[index];
            Owner?.ShowTip(h.Label, h.Name, h.Value, h.Box);
        }
        else Owner?.HideTip();
    }

    // The crosshair on one category: a line across the plot, every series' point lit, one tip listing them all.
    internal void LightCategory(int index)
    {
        if (index == _cross && _lit < 0) return;
        _cross = index;
        _lit = -1;
        InvalidateVisual();
        if (index < 0 || _axis.N == 0) { Owner?.HideTip(); return; }
        var along = _axis.X0 + _axis.Step * index;
        Owner?.ShowCategoryTip(index, _axis.Rtl ? _axis.W - along : along, _axis.Y0);
    }

    internal void LightMark(int series, int index) => LightAt(_hits.FindIndex(h => h.Series == series && h.Index == index));

    internal void Clear()
    {
        _lit = -1;
        _cross = -1;
        InvalidateVisual();
        Owner?.HideTip();
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, double.IsNaN(Height) ? 0 : Height);

    public override void Render(DrawingContext context)
    {
        var owner = Owner;
        var litHit = _lit >= 0 && _lit < _hits.Count ? _hits[_lit] : null;
        _hits.Clear();
        _fading = false;
        if (owner is null) return;
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        var box = new Box(Bounds.Width, Bounds.Height, owner.IsRtl);
        var labels = owner.DataLabels;
        var series = owner.DataSeries;
        if (box.W < 10 || box.H < 10 || series.Count == 0) return;
        var d = new Draw(context, owner, this, litHit);
        _axis = default;
        switch (owner.Type)
        {
            case ChartType.Line or ChartType.Area: Lines(d, box, labels, series, owner.Type == ChartType.Area, false); break;
            case ChartType.Spark: Lines(d, box, labels, series, true, true); break;
            case ChartType.Donut: Donut(d, box, labels, series); break;
            default: Bars(d, box, labels, series, owner.Type); break;
        }
        if (litHit is not null)
        {
            _lit = _hits.FindIndex(h => h.Series == litHit.Series && h.Index == litHit.Index);
        }
        if (_fading) FadeFrame();
    }

    private readonly record struct Box(double W, double H, bool Rtl);

    private sealed record Ticks(double Lo, double Hi, List<double> List);

    private sealed record Frame0(Ticks T, double X0, double X1, double Y0, double Y1, bool Rtl, double W)
    {
        public double X(double along) => Rtl ? W - along : along;
    }

    // Everything one Render needs: the context, the owner's brushes and fonts, and the lit mark.
    private sealed class Draw(DrawingContext ctx, Chart owner, ChartPlot plot, Hit? lit)
    {
        public DrawingContext Ctx { get; } = ctx;
        public Chart Owner { get; } = owner;
        public ChartPlot Plot { get; } = plot;
        public Hit? Lit { get; } = lit;
        public bool AnyLit => Lit is not null;

        public bool IsLit(int series, int index) => Lit is { } l && l.Series == series && l.Index == index;

        public FormattedText Text(string text, bool numbers, double size, IBrush? brush, FontWeight weight = FontWeight.Normal)
        {
            var family = numbers ? Owner.NumberFontFamily : Owner.FontFamily;
            return new FormattedText(text, CultureInfo.CurrentCulture, numbers || !Owner.IsRtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft,
                new Typeface(family, FontStyle.Normal, weight), size, brush ?? Brushes.Gray);
        }

        // SVG text: x by its anchor, y on the baseline.
        public void TextAt(FormattedText ft, double x, double baseline, string anchor)
        {
            var left = anchor switch { "middle" => x - ft.Width / 2, "end" => x - ft.Width, _ => x };
            Ctx.DrawText(ft, new Point(left, baseline - ft.Baseline));
        }
    }

    private static Ticks MakeTicks(double lo, double hi, int want)
    {
        if (lo == hi) hi = lo + 1;
        var raw = (hi - lo) / want;
        var mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var step = new[] { 1.0, 2, 5, 10 }.Select(m => m * mag).First(s => s >= raw);
        var a = Math.Floor(lo / step) * step;
        var b = Math.Ceiling(hi / step) * step;
        var list = new List<double>();
        for (var v = a; v <= b + step / 2; v += step) list.Add(Math.Round(v / step) * step);
        return new Ticks(a, b, list);
    }

    private static (double Lo, double Hi) Range(IReadOnlyList<ChartSeries> series, bool stacked)
    {
        double lo = 0, hi = 0;
        if (stacked)
        {
            var n = series[0].Data.Count;
            for (var i = 0; i < n; i++)
            {
                double up = 0, down = 0;
                foreach (var s in series)
                {
                    var v = i < s.Data.Count ? s.Data[i] : double.NaN;
                    if (v > 0) up += v;
                    else if (v < 0) down += v;
                }
                hi = Math.Max(hi, up);
                lo = Math.Min(lo, down);
            }
        }
        else
        {
            foreach (var s in series)
                foreach (var v in s.Data)
                    if (!double.IsNaN(v)) { hi = Math.Max(hi, v); lo = Math.Min(lo, v); }
        }
        return (lo, hi);
    }

    private static double Val(ChartSeries s, int i) => i < s.Data.Count ? s.Data[i] : double.NaN;

    private static Frame0 MakeFrame(Chart owner, Box box, IReadOnlyList<string> labels, IReadOnlyList<ChartSeries> series, bool horizontal, bool stacked)
    {
        var (lo, hi) = Range(series, stacked);
        var t = MakeTicks(lo, hi, box.H < 140 ? 3 : 4);
        var labelW = t.List.Max(v => owner.Format(v, true).Length) * 7 + 12;
        double start, end, top, bottom;
        if (horizontal)
        {
            start = Math.Min(box.W * .35, Math.Max(2, labels.Count > 0 ? labels.Max(l => l.Length) : 0) * 7 + 14);
            end = 12; top = 6; bottom = 22;
        }
        else
        {
            start = labelW; end = 8; top = 10; bottom = 26;
        }
        return new Frame0(t, start, box.W - end, top, box.H - bottom, box.Rtl, box.W);
    }

    private static void Grid(Draw d, Frame0 fr, bool horizontal)
    {
        var span = fr.T.Hi - fr.T.Lo;
        var grid = d.Plot.PenFor(d.Owner.GridBrush, 1);
        var zero = d.Plot.PenFor(d.Owner.ZeroBrush, 1);
        foreach (var v in fr.T.List)
        {
            var at = (v - fr.T.Lo) / span;
            var pen = v == 0 ? zero : grid;
            var label = d.Text(d.Owner.Format(v, true), true, 11, d.Owner.AxisBrush);
            if (horizontal)
            {
                var x = Crisp(fr.X(fr.X0 + at * (fr.X1 - fr.X0)));
                d.Ctx.DrawLine(pen, new Point(x, fr.Y0), new Point(x, fr.Y1));
                d.TextAt(label, x, fr.Y1 + 16, "middle");
            }
            else
            {
                var y = Crisp(fr.Y1 - at * (fr.Y1 - fr.Y0));
                d.Ctx.DrawLine(pen, new Point(fr.X(fr.X0), y), new Point(fr.X(fr.X1), y));
                d.TextAt(label, fr.X(fr.X0 - 8), y + 4, fr.Rtl ? "start" : "end");
            }
        }
    }

    // A one-pixel line on the pixel grid, as shape-rendering: crispEdges does.
    private static double Crisp(double v) => Math.Floor(v) + 0.5;

    private void Bars(Draw d, Box box, IReadOnlyList<string> labels, IReadOnlyList<ChartSeries> series, ChartType type)
    {
        var horizontal = type == ChartType.HBar;
        var touching = type == ChartType.Histogram;
        var stacked = d.Owner.Stacked && (type is ChartType.Bar or ChartType.HBar) && series.Count > 1;
        var fr = MakeFrame(d.Owner, box, labels, series, horizontal, stacked);
        Grid(d, fr, horizontal);
        var n = labels.Count;
        var span = fr.T.Hi - fr.T.Lo;
        var along = horizontal ? fr.Y1 - fr.Y0 : fr.X1 - fr.X0;
        var slot = along / Math.Max(n, 1);
        var sets = series.Count;
        var overlap = touching && sets > 1;
        var groupW = touching ? slot - 1 : Math.Min(slot * .72, sets * 34);
        var barW = stacked || overlap ? groupW : groupW / sets;
        var zero = (0 - fr.T.Lo) / span;
        var every = (int)Math.Ceiling(n / Math.Max(1, Math.Floor(along / (horizontal ? 18 : 44))));
        var bars = new List<(RoundedRect Rect, IBrush? Brush, double Opacity, double P, int S, int I)>();
        for (var i = 0; i < n; i++)
        {
            var mid = slot * i + slot / 2;
            double up = 0, down = 0;
            for (var k = 0; k < sets; k++)
            {
                var v = Val(series[k], i);
                if (double.IsNaN(v)) continue;
                double from = zero, to;
                if (stacked)
                {
                    if (v >= 0) { from = (up - fr.T.Lo) / span; up += v; to = (up - fr.T.Lo) / span; }
                    else { from = (down - fr.T.Lo) / span; down += v; to = (down - fr.T.Lo) / span; }
                }
                else to = (v - fr.T.Lo) / span;
                var offset = stacked || overlap ? -groupW / 2 : -groupW / 2 + barW * k;
                double a = Math.Min(from, to), b = Math.Max(from, to);
                Rect rect;
                double rx;
                if (horizontal)
                {
                    var len = fr.X1 - fr.X0;
                    double x0 = fr.X0 + a * len, x1 = fr.X0 + b * len;
                    var y = fr.Y0 + mid + offset;
                    rect = new Rect(Math.Min(fr.X(x0), fr.X(x1)), y, Math.Max(1, x1 - x0), Math.Max(1, barW - 2));
                    rx = Math.Min(4, barW / 3);
                }
                else
                {
                    var h = fr.Y1 - fr.Y0;
                    double top = fr.Y1 - b * h, bottom = fr.Y1 - a * h;
                    var left = fr.X0 + mid + offset + (touching ? 0 : 1);
                    var w = Math.Max(1, barW - (touching ? 1 : 2));
                    rect = new Rect(fr.Rtl ? box.W - left - w : left, top, w, Math.Max(1, bottom - top));
                    rx = touching ? 1 : Math.Min(4, w / 3);
                }
                var p = Progress(Math.Min(i * 30, 300), BarGrow);
                var opacity = overlap ? .55 : 1;
                if (d.AnyLit) opacity = d.IsLit(k, i) ? 1 : .45;
                opacity = Toward(BarKind, k, i, opacity);
                bars.Add((new RoundedRect(rect, rx), d.Owner.BrushAt(k), opacity, p, k, i));
                Rect? area = null;
                if (!stacked && !overlap)
                    area = horizontal
                        ? new Rect(Math.Min(fr.X(fr.X0), fr.X(fr.X1)), rect.Y, fr.X1 - fr.X0, rect.Height)
                        : new Rect(rect.X, fr.Y0, rect.Width, fr.Y1 - fr.Y0);
                _hits.Add(new Hit(rect, labels[i], series[k].Name, v, k, i, Area: area));
            }
            if (i % every != 0) continue;
            var text = d.Text(labels[i], false, 11, d.Owner.LabelBrush);
            if (horizontal) d.TextAt(text, fr.X(fr.X0 - 8), fr.Y0 + mid + 4, fr.Rtl ? "start" : "end");
            else d.TextAt(text, fr.X(fr.X0 + mid), fr.Y1 + 17, "middle");
        }
        foreach (var bar in bars)
        {
            if (bar.P <= 0) continue;
            var r = bar.Rect.Rect;
            Matrix grow;
            if (horizontal)
            {
                var originX = box.Rtl ? r.Right : r.Left;
                grow = Matrix.CreateTranslation(-originX, 0) * Matrix.CreateScale(bar.P, 1) * Matrix.CreateTranslation(originX, 0);
            }
            else grow = Matrix.CreateTranslation(0, -r.Bottom) * Matrix.CreateScale(1, bar.P) * Matrix.CreateTranslation(0, r.Bottom);
            using (d.Ctx.PushTransform(grow))
            using (d.Ctx.PushOpacity(bar.Opacity))
                d.Ctx.DrawRectangle(bar.Brush, null, bar.Rect);
        }
    }

    private void Lines(Draw d, Box box, IReadOnlyList<string> labels, IReadOnlyList<ChartSeries> series, bool area, bool bare)
    {
        double x0, x1, y0, y1, lo, span;
        Func<double, double> X;
        if (bare)
        {
            var (rlo, rhi) = Range(series, false);
            var vals = series[0].Data.Where(v => !double.IsNaN(v)).ToArray();
            lo = Math.Min(rlo, vals.Length > 0 ? vals.Min() : 0);
            span = rhi - lo is var s && s != 0 ? s : 1;
            x0 = 2; x1 = box.W - 2; y0 = 3; y1 = box.H - 3;
            X = a => box.Rtl ? box.W - a : a;
        }
        else
        {
            var fr = MakeFrame(d.Owner, box, labels, series, false, false);
            Grid(d, fr, false);
            x0 = fr.X0; x1 = fr.X1; y0 = fr.Y0; y1 = fr.Y1; lo = fr.T.Lo; span = fr.T.Hi - fr.T.Lo;
            X = fr.X;
        }
        var n = labels.Count;
        var step = n > 1 ? (x1 - x0) / (n - 1) : 0;
        var draw = Progress(0, LineDraw);
        if (!bare)
        {
            _axis = (x0, step, n, box.Rtl, box.W, y0, y1);
            if (_cross >= 0 && _cross < n)
            {
                var cx = Crisp(X(x0 + step * _cross));
                d.Ctx.DrawLine(PenFor(d.Owner.ZeroBrush, 1), new Point(cx, y0), new Point(cx, y1));
            }
        }
        for (var k = 0; k < series.Count; k++)
        {
            var s = series[k];
            var brush = d.Owner.BrushAt(k);
            var pts = new List<(Point P, double V, string L, int I)>();
            for (var i = 0; i < s.Data.Count && i < Math.Max(n, s.Data.Count); i++)
            {
                var v = s.Data[i];
                if (double.IsNaN(v)) continue;
                pts.Add((new Point(X(x0 + step * i), y1 - (v - lo) / span * (y1 - y0)), v, i < labels.Count ? labels[i] : "", i));
            }
            if (pts.Count == 0) continue;
            if (area || bare)
            {
                var baseY = bare ? y1 : y1 - (0 - lo) / span * (y1 - y0);
                var fill = new StreamGeometry();
                using (var g = fill.Open())
                {
                    g.BeginFigure(pts[0].P, true);
                    foreach (var q in pts.Skip(1)) g.LineTo(q.P);
                    g.LineTo(new Point(pts[^1].P.X, baseY));
                    g.LineTo(new Point(pts[0].P.X, baseY));
                    g.EndFigure(true);
                }
                using (d.Ctx.PushOpacity(bare ? .16 : .12))
                    d.Ctx.DrawGeometry(brush, null, fill);
            }
            var line = new StreamGeometry();
            using (var g = line.Open())
            {
                g.BeginFigure(pts[0].P, false);
                foreach (var q in pts.Skip(1)) g.LineTo(q.P);
                g.EndFigure(false);
            }
            var pen = PenFor(brush, bare ? 2 : 2.5, PenLineCap.Round);
            if (draw >= 1) d.Ctx.DrawGeometry(null, pen, line);
            else if (draw > 0)
            {
                // The line draws itself from its first point, as the web's dash offset does.
                var reach = box.W * draw;
                var clip = box.Rtl ? new Rect(box.W - reach - 4, -8, reach + 8, box.H + 16) : new Rect(-4, -8, reach + 8, box.H + 16);
                using (d.Ctx.PushClip(clip))
                    d.Ctx.DrawGeometry(null, pen, line);
            }
            if (bare) continue;
            var ring = PenFor(brush, 2);
            foreach (var q in pts)
            {
                var lit = d.IsLit(k, q.I) || q.I == _cross;
                var rest = lit ? 5.5 : 3.5;
                var r = Toward(DotKind, k, q.I, rest);
                d.Ctx.DrawEllipse(lit ? brush : d.Owner.DotBrush, ring, q.P, r, r);
                _hits.Add(new Hit(new Rect(q.P.X - rest, q.P.Y - rest, rest * 2, rest * 2), q.L, s.Name, q.V, k, q.I, q.P, Radius: 5.5));
            }
        }
        if (bare) return;
        var every = (int)Math.Ceiling(n / Math.Max(1, Math.Floor((x1 - x0) / 48)));
        for (var i = 0; i < n; i += every)
            d.TextAt(d.Text(labels[i], false, 11, d.Owner.LabelBrush), X(x0 + step * i), y1 + 17, "middle");
    }

    private void Donut(Draw d, Box box, IReadOnlyList<string> labels, IReadOnlyList<ChartSeries> series)
    {
        var values = series[0].Data;
        var total = values.Sum(v => double.IsNaN(v) || v < 0 ? 0 : v);
        var size = Math.Min(box.W, box.H);
        var r = size / 2 - 6;
        double cx = box.W / 2, cy = box.H / 2;
        var width = Math.Max(10, r * .32);
        var ring = r - width / 2;
        var circ = 2 * Math.PI * ring;
        var center = new Point(cx, cy);
        d.Ctx.DrawEllipse(null, PenFor(d.Owner.TrackBrush, width), center, ring, ring);
        double at = 0;
        for (var i = 0; i < values.Count; i++)
        {
            var v = values[i];
            if (double.IsNaN(v) || v <= 0 || total == 0) continue;
            var len = v / total * circ;
            var a0 = at / ring;
            var a1 = (at + Math.Max(0, len - 2)) / ring;
            var opacity = Progress(Math.Min(i * 60, 360), SliceFade);
            opacity *= Toward(SliceKind, 0, i, d.AnyLit && !d.IsLit(0, i) ? .45 : 1);
            var arc = Arc(center, ring, a0, a1);
            using (d.Ctx.PushOpacity(opacity))
                d.Ctx.DrawGeometry(null, PenFor(d.Owner.BrushAt(i), width), arc);
            var mid = (a0 + a1) / 2;
            var mp = new Point(cx + ring * Math.Sin(mid), cy - ring * Math.Cos(mid));
            var boxAt = new Rect(mp.X - width / 2, mp.Y - width / 2, width, width);
            _hits.Add(new Hit(boxAt, i < labels.Count ? labels[i] : "", series[0].Name, v, 0, i, center, ring - width / 2, ring + width / 2, a0, (at + len) / ring));
            at += len;
        }
        var sub = d.Owner.CenterLabel;
        var said = !string.IsNullOrEmpty(d.Owner.Center) ? d.Owner.Center! : d.Owner.Format(total, total >= 10000);
        var totalText = d.Text(said, true, 24, d.Owner.TotalBrush, FontWeight.ExtraBold);
        var middle = string.IsNullOrEmpty(sub) ? cy + 2 : cy - 6;
        d.Ctx.DrawText(totalText, new Point(cx - totalText.Width / 2, middle - totalText.Height / 2));
        if (!string.IsNullOrEmpty(sub)) d.TextAt(d.Text(sub, false, 11, d.Owner.AxisBrush), cx, cy + 14, "middle");
    }

    // An arc clockwise from the top, angles in radians.
    private static StreamGeometry Arc(Point c, double r, double a0, double a1)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        var p0 = new Point(c.X + r * Math.Sin(a0), c.Y - r * Math.Cos(a0));
        var p1 = new Point(c.X + r * Math.Sin(a1), c.Y - r * Math.Cos(a1));
        ctx.BeginFigure(p0, false);
        ctx.ArcTo(p1, new Size(r, r), 0, a1 - a0 > Math.PI, SweepDirection.Clockwise);
        ctx.EndFigure(false);
        return g;
    }
}
