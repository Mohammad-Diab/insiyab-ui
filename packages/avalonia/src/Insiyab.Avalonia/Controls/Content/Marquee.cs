using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>One line (its child, usually a one-line text with an ellipsis at its end) that always shows its start first, in either direction. When it does not fit it rests at its start, glides slowly to its end, rests there and glides back, again and again while it is seen (a marquee); in a still window, or with the system's motion off, it is given only its room (the text's own ellipsis cuts its end). The glide moves the child by its render transform.</summary>
public class Marquee : Decorator
{
    // how fast it glides (px a second), and how long it rests at each end
    internal const double Speed = 36;
    internal const double RestAtStart = 1800, RestAtEnd = 1400;

    private static readonly Stopwatch s_clock = Stopwatch.StartNew();

    // For tests: the milliseconds the glide is timed by, which a test can hold and step.
    internal static Func<double> Now { get; set; } = () => s_clock.Elapsed.TotalMilliseconds;

    private static readonly Easing s_ease = new SplineEasing(0.45, 0, 0.55, 1);

    private readonly TranslateTransform _shift = new();
    private double _full;      // the text's own width
    private double _ran;       // the width the glide in progress was started for
    private bool _moving;      // it glides (it does not fit, and motion is on)
    private int _run;          // the glide in progress; a new one ends the old
    private IDisposable? _rest;


    // how far past its sides the text may still show: a glyph's ink at the line's start (an Arabic letter's head) reaches past its advance
    internal const double Bleed = 4;

    /// <summary>A template that shows a string as a marquee (a dialog's title). A control given as the content (a title made of a name and a button) is shown as it is: a template set on a presenter is used for a control too, so without this it showed the control's type name; any other content falls through to the usual templates.</summary>
    public static IDataTemplate TextTemplate { get; } = new FuncDataTemplate(
        content => content is string or Control,
        (content, _) => content as Control ?? new Marquee
        {
            Child = new TextBlock { Text = (string?)content, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis },
        });

    /// <summary>Whether the text is wider than its room and glides now.</summary>
    public bool IsMoving => _moving;

    /// <summary>How far the text has glided now (0 at its start).</summary>
    public double Shift => -_shift.X;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildProperty)
        {
            if (change.OldValue is Control old && ReferenceEquals(old.RenderTransform, _shift))
                old.RenderTransform = null;
            if (change.NewValue is Control child)
                child.RenderTransform = _shift;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
            return default;
        child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        _full = child.DesiredSize.Width;
        return new Size(Math.Min(_full, availableSize.Width), child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var moving = _full > finalSize.Width + 0.5 && !Motion.IsStill(this);
        // gliding, the text keeps its whole width, its start at the start; else it fits its room (cut with the ellipsis at its end when it is still)
        Child?.Arrange(new Rect(0, 0, moving ? _full : finalSize.Width, finalSize.Height));
        // gliding, it is cut at its room (a few px past it, so the ink at the start shows); otherwise it is not cut at all, as a plain text
        Clip = moving ? new RectangleGeometry(new Rect(-Bleed, -Bleed, finalSize.Width + 2 * Bleed, finalSize.Height + 2 * Bleed)) : null;
        if (moving != _moving || moving && Math.Abs(_full - _ran) > 0.5) // another text starts again from its start
        {
            _moving = moving;
            _ran = _full;
            Restart();
        }
        return finalSize;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Restart();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Stop();
    }

    private void Stop()
    {
        _run++;
        _rest?.Dispose();
        _rest = null;
        _shift.X = 0;
    }

    // back to the start, then the rest there before the first glide
    private void Restart()
    {
        Stop();
        if (_moving && TopLevel.GetTopLevel(this) is not null)
            Rest(RestAtStart, toEnd: true, _run);
    }

    private void Rest(double ms, bool toEnd, int run)
    {
        _rest?.Dispose();
        _rest = DispatcherTimer.RunOnce(() =>
        {
            if (run == _run)
                Glide(toEnd, run);
        }, TimeSpan.FromMilliseconds(ms));
    }

    // one glide to the end (or back to the start), timed by the frames; the rests between run on a timer, so nothing is drawn while it rests
    private void Glide(bool toEnd, int run)
    {
        if (TopLevel.GetTopLevel(this) is not { } top || !_moving)
            return;
        if (Motion.IsStill(this))
        {
            InvalidateArrange(); // the window turned still: the ellipsis instead
            return;
        }
        var distance = _full - Bounds.Width;
        if (distance <= 0)
            return;
        var length = Math.Max(1200, distance / Speed * 1000);
        var start = -1d;
        void Frame(TimeSpan _)
        {
            if (run != _run)
                return;
            var now = Now();
            if (start < 0)
                start = now;
            var t = Math.Clamp((now - start) / length, 0, 1);
            var p = s_ease.Ease(t);
            _shift.X = -distance * (toEnd ? p : 1 - p);
            if (t < 1)
                top.RequestAnimationFrame(Frame);
            else
                Rest(toEnd ? RestAtEnd : RestAtStart, !toEnd, run);
        }
        top.RequestAnimationFrame(Frame);
    }
}
