using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The panel a pill's face sits in: as it is, or for <c>dot peek</c> measured as the dot's ring alone, the face growing out of the ring over its neighbours when open.</summary>
internal sealed class PillPeek : Panel
{
    public const double Dot = 8;

    // opening: the face grows, the words fade in once it has mostly grown; closing: the words fade out, then the face shrinks
    private const double GrowMs = 220, WordsInDelayMs = 100, WordsInMs = 150;
    private const double WordsOutMs = 100, ShrinkDelayMs = 120, ShrinkMs = 200;

    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly Easing Ease = new SplineEasing(0.22, 1, 0.36, 1);

    public static readonly StyledProperty<bool> IsPeekProperty =
        AvaloniaProperty.Register<PillPeek, bool>(nameof(IsPeek));

    public static readonly StyledProperty<bool> BackProperty =
        AvaloniaProperty.Register<PillPeek, bool>(nameof(Back));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<PillPeek, bool>(nameof(IsOpen));

    public static readonly DirectProperty<PillPeek, double> WordsProperty =
        AvaloniaProperty.RegisterDirect<PillPeek, double>(nameof(Words), o => o.Words);

    private double _reveal;
    private double _words = 1;
    private double _fromReveal, _fromWords, _start;
    private int _run;

    static PillPeek() => AffectsMeasure<PillPeek>(IsPeekProperty, BackProperty);

    public bool IsPeek { get => GetValue(IsPeekProperty); set => SetValue(IsPeekProperty, value); }

    /// <summary>The face opens toward the start, its dot at its end.</summary>
    public bool Back { get => GetValue(BackProperty); set => SetValue(BackProperty, value); }

    /// <summary>Whether a peeking face is open (pointer or keyboard focus on the pill).</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>How much the face's words show: always 1 for a plain pill; for a peeking one it follows the open and close motion.</summary>
    public double Words { get => _words; private set => SetAndRaise(WordsProperty, ref _words, value); }

    private Control? Ring => Children.Count > 1 ? Children[1] : null;
    private Control? RingMark => Children.Count > 2 ? Children[2] : null;

    // the mark's width: the dot's, or the icon's that stands in for it (the face keeps the same room for it)
    private double MarkWidth => RingMark is { IsVisible: true } m && m.DesiredSize.Width > 0 ? m.DesiredSize.Width : Dot;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsPeekProperty) Jump();
        else if (change.Property == IsOpenProperty) Move();
    }

    // straight to the end state: a plain pill shows its words; a peeking one shows the ring, or the whole pill while open
    private void Jump()
    {
        _run++;
        _reveal = IsPeek && !IsOpen ? 0 : 1;
        Words = IsPeek && !IsOpen ? 0 : 1;
        InvalidateArrange();
    }

    private void Move()
    {
        if (!IsPeek || Motion.IsStill(this) || TopLevel.GetTopLevel(this) is not { } top || !Seen.Now(this))
        {
            Jump();
            return;
        }
        _fromReveal = _reveal;
        _fromWords = _words;
        _start = Clock.Elapsed.TotalMilliseconds;
        var run = ++_run;
        top.RequestAnimationFrame(_ => Frame(top, run));
    }

    private void Frame(TopLevel top, int run)
    {
        if (run != _run) return;
        var t = Clock.Elapsed.TotalMilliseconds - _start;
        bool done;
        if (IsOpen)
        {
            _reveal = _fromReveal + (1 - _fromReveal) * Ease.Ease(Math.Clamp(t / GrowMs, 0, 1));
            Words = _fromWords + (1 - _fromWords) * Math.Clamp((t - WordsInDelayMs) / WordsInMs, 0, 1);
            done = t >= Math.Max(GrowMs, WordsInDelayMs + WordsInMs);
        }
        else
        {
            Words = _fromWords * (1 - Math.Clamp(t / WordsOutMs, 0, 1));
            _reveal = _fromReveal * (1 - Ease.Ease(Math.Clamp((t - ShrinkDelayMs) / ShrinkMs, 0, 1)));
            done = t >= ShrinkDelayMs + ShrinkMs;
        }
        InvalidateArrange();
        if (done) Jump();
        else top.RequestAnimationFrame(_ => Frame(top, run));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0) return default;
        var face = Children[0];
        if (!IsPeek)
        {
            foreach (var child in Children) child.Measure(availableSize);
            return face.DesiredSize;
        }
        face.Measure(Size.Infinity);
        var slot = Dot;
        if (Ring is { IsVisible: true } ring)
        {
            ring.Measure(Size.Infinity);
            slot = Math.Max(ring.DesiredSize.Width, ring.DesiredSize.Height);
        }
        RingMark?.Measure(Size.Infinity);
        return new Size(slot, slot);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0) return finalSize;
        var face = Children[0];
        if (!IsPeek)
        {
            foreach (var child in Children) child.Arrange(new Rect(finalSize));
            face.Opacity = 1;
            return finalSize;
        }
        var slot = Math.Min(finalSize.Width, finalSize.Height);
        Ring?.Arrange(new Rect(0, 0, slot, slot));
        if (Ring is not null) Ring.Opacity = 1 - _reveal;
        if (RingMark is { } mark)
            mark.Arrange(new Rect((slot - mark.DesiredSize.Width) / 2, (slot - mark.DesiredSize.Height) / 2, mark.DesiredSize.Width, mark.DesiredSize.Height));
        var size = face.DesiredSize;
        // the face's own border sits inside the ground (PART_Ground > PART_Face)
        var inner = face is Decorator { Child: Border b } ? b : face as Border;
        var (pad, edge) = inner is not null ? (inner.Padding, inner.BorderThickness) : (default(Thickness), default(Thickness));
        var half = MarkWidth / 2;
        var dotCentre = Back ? size.Width - pad.Right - edge.Right - half : pad.Left + edge.Left + half;
        // from the ring (0) to the whole face with its dot on the ring's (1)
        var r = Math.Clamp(_reveal, 0, 1);
        var x = (slot / 2 - dotCentre) * r;
        var width = slot + (size.Width - slot) * r;
        var height = slot + (size.Height - slot) * r;
        face.Opacity = r > 0.001 ? 1 : 0;
        face.Arrange(new Rect(x, (slot - height) / 2, width, height));
        return finalSize;
    }
}
