using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>One page at a time, sliding to the next as a settings menu's levels do (YouTube's settings menu): going in (<see cref="Go"/>), the new page comes in from the end side while the old one leaves to the start; going back, the other way; the box's height eases from the old page's to the new one's. Right to left mirrors the sides. At once in a still window or before it shows. Put it in a <see cref="PopCard"/> for a menu with levels.</summary>
public class SlidePages : Panel
{
    private Control? _page;
    private Control? _leaving;
    private bool _back;
    private double _from;      // the height the box had when the slide began
    private double _t = 1;     // the slide's progress, eased, 0 to 1
    private TimeSpan? _start;
    private bool _running;
    private int _run;

    public SlidePages() => ClipToBounds = true;

    /// <summary>The page shown (the one coming in while a slide runs).</summary>
    public Control? Page => _page;

    /// <summary>Whether a slide is under way.</summary>
    public bool IsSliding => _running;

    /// <summary>How far the slide has gone, eased, 0 to 1 (1 at rest), for tests.</summary>
    internal double Progress => _t;

    /// <summary>For tests: the slide's length.</summary>
    internal TimeSpan Duration => Motion.Base(this);

    /// <summary>Shows <paramref name="page"/> at once, without a slide (the first page).</summary>
    public void Show(Control page)
    {
        Finish();
        if (_page is not null) Children.Remove(_page);
        _page = page;
        Children.Add(page);
    }

    /// <summary>Slides to <paramref name="page"/>: in from the end side, or with <paramref name="back"/> from the start side, the height easing to its own.</summary>
    public void Go(Control page, bool back = false)
    {
        if (_page is null || !IsEffectivelyVisible || Motion.IsStill(this) || TopLevel.GetTopLevel(this) is not { } top || Bounds.Height <= 0)
        {
            Show(page);
            return;
        }
        Finish();
        _from = Bounds.Height;
        _leaving = _page;
        _leaving.IsHitTestVisible = false;
        _page = page;
        _back = back;
        Children.Add(page);
        _t = 0;
        _start = null;
        _running = true;
        var run = ++_run;
        InvalidateMeasure();
        top.RequestAnimationFrame(now => Tick(now, run));
    }

    // The slide under way lands at once: the old page goes.
    private void Finish()
    {
        _running = false;
        _run++;
        _t = 1;
        if (_leaving is not null)
        {
            Children.Remove(_leaving);
            _leaving.IsHitTestVisible = true;
            _leaving = null;
        }
        InvalidateMeasure();
    }

    private void Tick(TimeSpan now, int run)
    {
        if (!_running || run != _run) return;
        _start ??= now;
        var k = Math.Clamp((now - _start.Value).TotalMilliseconds / Duration.TotalMilliseconds, 0, 1);
        _t = Motion.Ease(this).Ease(k);
        if (k >= 1 || Motion.IsStill(this) || TopLevel.GetTopLevel(this) is not { } top)
        {
            Finish();
            return;
        }
        InvalidateMeasure();
        InvalidateArrange();
        top.RequestAnimationFrame(n => Tick(n, run));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var room = availableSize.WithHeight(double.PositiveInfinity);
        var width = 0.0;
        foreach (var child in Children)
        {
            child.Measure(room);
            width = Math.Max(width, child.DesiredSize.Width);
        }
        var height = _page?.DesiredSize.Height ?? 0;
        if (_running) height = _from + (height - _from) * _t;
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var w = finalSize.Width;
        // forward: the new page from the end side (+w, mirrored by right to left), the old one to the start (−w); back the other way
        var side = _back ? -1 : 1;
        foreach (var child in Children)
        {
            var x = 0.0;
            if (_running && ReferenceEquals(child, _page)) x = side * (1 - _t) * w;
            else if (_running && ReferenceEquals(child, _leaving)) x = -side * _t * w;
            child.Arrange(new Rect(x, 0, w, Math.Max(child.DesiredSize.Height, ReferenceEquals(child, _page) ? finalSize.Height : 0)));
        }
        return finalSize;
    }
}
