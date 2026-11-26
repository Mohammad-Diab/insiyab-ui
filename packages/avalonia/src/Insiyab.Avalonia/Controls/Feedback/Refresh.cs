using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>How far a pull to refresh's work has come, set on its <see cref="RefreshContainer"/> by the app: 0 to 1 turns the turning ring of a refresh under way into a ring that fills (<c>CircularProgressDrawable</c>'s trim from the top, clockwise), easing to each new value; <c>NaN</c> (the default) is the turning ring. Back to <c>NaN</c> while it refreshes, the ring keeps its last fill until the disc leaves.</summary>
public static class RefreshProgress
{
    public static readonly AttachedProperty<double> ValueProperty =
        AvaloniaProperty.RegisterAttached<RefreshContainer, double>("Value", typeof(RefreshProgress), double.NaN);

    public static double GetValue(RefreshContainer container) => container.GetValue(ValueProperty);
    public static void SetValue(RefreshContainer container, double value) => container.SetValue(ValueProperty, value);
}

/// <summary>The brand spinner's ring drawn by a pull to refresh, with Android's arrow at the arc's end while it follows the finger (<c>CircularProgressDrawable</c>).</summary>
internal sealed class RefreshArc : LoadingSpinner
{
    // CircularProgressDrawable's DEFAULT size: the ring's radius 7.5, the arrow 10 wide and 5 high; scaled here to this ring's radius.
    private const double AndroidRadius = 7.5, ArrowWidth = 10, ArrowHeight = 5;

    public static readonly StyledProperty<double> ArrowScaleProperty =
        AvaloniaProperty.Register<RefreshArc, double>(nameof(ArrowScale));

    static RefreshArc() => AffectsRender<RefreshArc>(ArrowScaleProperty);

    /// <summary>How big the arrow at the arc's end is, 0 (none) to 1 (<c>setArrowScale</c>).</summary>
    public double ArrowScale { get => GetValue(ArrowScaleProperty); set => SetValue(ArrowScaleProperty, value); }

    // The ring as CircularProgressDrawable draws it, with round caps and no track; then the arrow at the arc's end.
    public override void Render(DrawingContext context)
    {
        var side = Math.Min(Bounds.Width, Bounds.Height);
        if (side <= 0 || ArcLength <= 0 || (Arc ?? Foreground) is not { } brush) return;
        var t = Thickness;
        var r = (side - t) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var length = Math.Min(ArcLength, 359.9);
        var from = (Angle - 90 - length / 2) * Math.PI / 180;
        var to = from + length * Math.PI / 180;
        var ring = new StreamGeometry();
        using (var g = ring.Open())
        {
            g.BeginFigure(new Point(center.X + r * Math.Cos(from), center.Y + r * Math.Sin(from)), false);
            g.ArcTo(new Point(center.X + r * Math.Cos(to), center.Y + r * Math.Sin(to)), new Size(r, r), 0, length > 180, SweepDirection.Clockwise);
            g.EndFigure(false);
        }
        var solid = brush.ToImmutable();
        context.DrawGeometry(null, new ImmutablePen(solid, t, lineCap: PenLineCap.Round), ring);
        var scale = ArrowScale;
        if (scale <= 0) return;
        var k = r / AndroidRadius;
        var w = ArrowWidth * k * scale;
        var h = ArrowHeight * k * scale;
        // drawTriangle: the base across the ring at the arc's end, half the stroke past it, the point along the turn; its corners softened by a hairline
        var end = to;
        Point At(double x, double y) => new(center.X + x * Math.Cos(end) - y * Math.Sin(end), center.Y + x * Math.Sin(end) + y * Math.Cos(end));
        var down = t / 2;
        var arrow = new StreamGeometry();
        using (var g = arrow.Open())
        {
            g.BeginFigure(At(r - w / 2, down), true);
            g.LineTo(At(r + w / 2, down));
            g.LineTo(At(r, down + h));
            g.EndFigure(true);
        }
        context.DrawGeometry(solid, new ImmutablePen(solid, 0.75, lineJoin: PenLineJoin.Round), arrow);
    }
}

/// <summary>The small round surface a <see cref="RefreshVisualizer"/> brings down, holding a <see cref="RefreshArc"/>. A downward (or upward) pull is AndroidX <c>SwipeRefreshLayout</c>'s, value for value: the disc slides over the list, which stays put, from just under the top bar the list scrolls under (<c>Shell.FrostsTopbar</c>) or the container's edge, and is cut there; it follows the pull to its trigger point, then with the slingshot's tension; the ring grows with an arrow at its end, faint before the trigger and full past it; let go short it slides back, past the trigger it settles where it spins while the app refreshes, and shrinks away once the refresh is done. A sideways pull keeps Avalonia's own offsets.</summary>
internal sealed class RefreshDisc : Border
{
    // CircularProgressDrawable's indeterminate run: ANIMATION_DURATION, MIN_PROGRESS_ARC, RING_ROTATION, GROUP_FULL_ROTATION, MATERIAL_INTERPOLATOR.
    private const double Cycle = 1332, MinArc = 0.01, RingRotation = 1 - (MaxProgressAngle - MinArc), GroupFullRotation = 1080.0 / 5;
    private static readonly SplineEasing FastOutSlowIn = new(0.4, 0, 0.2, 1);

    // SwipeRefreshLayout's values (androidx.swiperefreshlayout.widget.SwipeRefreshLayout).
    private const double CircleDiameter = 40;         // CIRCLE_DIAMETER: mOriginalOffsetTop = -mCircleDiameter
    private const double SpinnerOffsetEnd = 64;       // DEFAULT_CIRCLE_TARGET: mSpinnerOffsetEnd (= mTotalDragDistance)
    private const double MaxProgressAngle = 0.8;      // MAX_PROGRESS_ANGLE
    private const double StartingAlpha = 0.3;         // STARTING_PROGRESS_ALPHA / MAX_ALPHA
    private const double AlphaDuration = 300;         // ALPHA_ANIMATION_DURATION
    private const double ScaleDownDuration = 150;     // SCALE_DOWN_DURATION
    private const double ToTriggerDuration = 200;     // ANIMATE_TO_TRIGGER_DURATION
    private const double ToStartDuration = 200;       // ANIMATE_TO_START_DURATION
    private const double ScaleUpDuration = 400;       // mMediumAnimationDuration (config_mediumAnimTime): setRefreshing(true) from code

    private RefreshVisualizer? _visualizer;
    private RefreshContainer? _container;
    private RefreshVisualizerState _state = RefreshVisualizerState.Idle;
    private double _ratio;
    private long _spin;
    private long _move;     // the running slide or scale (a newer one stops it)
    private long _fade;     // the running alpha change
    private long _fill;     // the running fill of a determinate ring (RefreshProgress)
    private double _filled = -1; // the determinate ring's fill as last drawn (turns); -1: the ring turns
    private double _alphaTo = -1;
    private double _top = -CircleDiameter; // the circle's top past the pull's edge (SwipeRefreshLayout's mCurrentTargetOffsetTop)
    private double _startTrim, _endTrim, _rotation, _group; // the ring's trims and rotation (turns) and the drawable's turn (degrees), as last drawn
    private double _scale = 1;
    private readonly TranslateTransform _shift = new();
    private readonly ScaleTransform _size = new();

    internal RefreshVisualizerState State => _state;

    /// <summary>The circle's top past the pull's edge (the bar's foot), in dp: −40 is out of sight, 24 is where it spins.</summary>
    internal double Top => _top;

    internal double Scale => _scale;

    private RefreshArc? Arc => Child as RefreshArc;

    public RefreshDisc()
    {
        RenderTransformOrigin = RelativePoint.Center;
        RenderTransform = new TransformGroup { Children = { _size, _shift } };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _visualizer = TemplatedParent as RefreshVisualizer ?? this.FindAncestorOfType<RefreshVisualizer>();
        if (_visualizer is not null) _visualizer.PropertyChanged += OnVisualizerChanged;
        _container = this.FindAncestorOfType<RefreshContainer>();
        if (_container is not null)
        {
            _container.PropertyChanged += OnContainerChanged;
            // The disc sits at the visualizer's far edge from the pull, or fills it across a sideways pull.
            if (Parent is Control root)
            {
                if (IsVertical(_container.PullDirection))
                    root.VerticalAlignment = _container.PullDirection == PullDirection.BottomToTop ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                else
                {
                    root.Height = double.NaN;
                    root.VerticalAlignment = VerticalAlignment.Stretch;
                }
            }
            _container.AddHandler(InputElement.PullGestureEvent, OnPull, RoutingStrategies.Bubble, handledEventsToo: true);
            _container.AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Bubble);
            // the pull starts as Android's does (EdgePull): taken over as each press comes, before the scroller's recognisers see it
            _container.AddHandler(PointerPressedEvent, OnPress, RoutingStrategies.Tunnel, handledEventsToo: true);
            // a wheel or a touchpad never pulls to refresh (EdgePull.Wheeled)
            _container.AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        Show();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_visualizer is not null) _visualizer.PropertyChanged -= OnVisualizerChanged;
        if (_container is not null)
        {
            _container.PropertyChanged -= OnContainerChanged;
            _container.RemoveHandler(InputElement.PullGestureEvent, OnPull);
            _container.RemoveHandler(KeyDownEvent, OnKey);
            _container.RemoveHandler(PointerPressedEvent, OnPress);
            _container.RemoveHandler(PointerWheelChangedEvent, OnWheel);
        }
        _visualizer = null;
        _container = null;
        _spin++;
        _move++;
        _fade++;
        _fill++;
    }

    // The app's progress (RefreshProgress) while the disc spins: the ring fills to it; NaN before any fill turns as before, after one keeps it.
    private void OnContainerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RefreshProgress.ValueProperty || _state != RefreshVisualizerState.Refreshing || Arc is not { } arc) return;
        if (double.IsFinite(Progress)) Fill(arc);
    }

    private double Progress => _container is { } c ? RefreshProgress.GetValue(c) : double.NaN;

    // The determinate ring: from its fill now to the progress, from the top, clockwise (setStartEndTrim(0, p) turned a quarter back), over 300 ms; a sliver shows before anything is done, so the disc is never empty.
    private void Fill(RefreshArc arc)
    {
        _spin++; // the turning ring stops where it is
        arc.ArrowScale = 0;
        var to = Math.Max(MinFill, Math.Clamp(Progress, 0, 1));
        var from = _filled < 0 ? MinFill : _filled;
        _filled = to;
        var run = ++_fill;
        if (Motion.IsStill(this) || TopLevel.GetTopLevel(this) is not { } top)
        {
            Ring(arc, 0, to, -0.25, 0);
            return;
        }
        TimeSpan? first = null;
        void Frame(TimeSpan now)
        {
            if (run != _fill) return;
            first ??= now;
            var t = Math.Clamp((now - first.Value).TotalMilliseconds / AlphaDuration, 0, 1);
            Ring(arc, 0, from + (to - from) * FastOutSlowIn.Ease(t), -0.25, 0);
            if (t < 1) top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }

    // The sliver a determinate ring shows before anything is done (turns).
    private const double MinFill = 0.03;

    private void OnPress(object? sender, PointerPressedEventArgs e)
    {
        if (_container is { } container) EdgePull.Use(container);
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (_container is { } container) EdgePull.Wheeled(container);
    }

    // A pull down or up is SwipeRefreshLayout's; a sideways one Avalonia's.
    private bool Android => _container is { } c && IsVertical(c.PullDirection);

    // Avalonia keeps the state property itself internal, so its change is read by name.
    private void OnVisualizerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != "RefreshVisualizerState" || e.NewValue is not RefreshVisualizerState state) return;
        var was = _state;
        _state = state;
        if (state == RefreshVisualizerState.Idle) _ratio = 0;
        if (Android)
        {
            if (state == RefreshVisualizerState.Refreshing && was != RefreshVisualizerState.Refreshing)
                StartRefreshing(fromCode: was == RefreshVisualizerState.Idle);
            else if (state == RefreshVisualizerState.Idle && was == RefreshVisualizerState.Refreshing)
                ScaleDown();
            else if (state == RefreshVisualizerState.Idle && was is RefreshVisualizerState.Interacting or RefreshVisualizerState.Pending)
                Cancel();
            return;
        }
        Show();
    }

    // Avalonia has the pull's share of its visualizer (EdgePull.ToDelta); a vertical one is laid as SwipeRefreshLayout's moveSpinner lays it, a sideways one as Avalonia does.
    private void OnPull(object? sender, PullGestureEventArgs e)
    {
        if (_visualizer is not { } visualizer || _container is not { } container) return;
        var vertical = IsVertical(e.PullDirection);
        var size = vertical ? visualizer.Bounds.Height : visualizer.Bounds.Width;
        if (size <= 0) return;
        _ratio = Math.Clamp(Math.Abs(vertical ? e.Delta.Y : e.Delta.X) / size, 0, 1);
        if (_state is not (RefreshVisualizerState.Interacting or RefreshVisualizerState.Pending)) return;
        if (vertical) MoveSpinner(EdgePull.ToOverscroll(container, Math.Abs(e.Delta.Y)));
        else Show();
    }

    /// <summary>SwipeRefreshLayout.moveSpinner for a pull of <paramref name="overscroll"/> dp: the circle's top, and the ring's trim, arrow and turn (in turns).</summary>
    internal static (double Top, double Trim, double Arrow, double Rotation) Spinner(double overscroll)
    {
        var total = EdgePull.TriggerDistance;
        var dragPercent = Math.Min(1, Math.Abs(overscroll / total));
        var adjustedPercent = Math.Max(dragPercent - .4, 0) * 5 / 3;
        var extraOS = Math.Abs(overscroll) - total;
        var slingshotDist = SpinnerOffsetEnd;
        var tensionSlingshotPercent = Math.Max(0, Math.Min(extraOS, slingshotDist * 2) / slingshotDist);
        var tensionPercent = (tensionSlingshotPercent / 4 - Math.Pow(tensionSlingshotPercent / 4, 2)) * 2;
        var extraMove = slingshotDist * tensionPercent * 2;
        var targetY = -CircleDiameter + (slingshotDist * dragPercent + extraMove);
        var strokeStart = adjustedPercent * .8;
        var rotation = (-0.25 + .4 * adjustedPercent + tensionPercent * 2) * .5;
        return (targetY, Math.Min(MaxProgressAngle, strokeStart), Math.Min(1, adjustedPercent), rotation);
    }

    private void MoveSpinner(double overscroll)
    {
        if (Arc is not { } arc) return;
        _move++;
        _spin++;
        var (top, trim, arrow, rotation) = Spinner(overscroll);
        Opacity = 1;
        SetScale(1);
        SetTop(top);
        Trim(arc, trim, rotation);
        arc.ArrowScale = arrow;
        FadeArc(overscroll < EdgePull.TriggerDistance ? StartingAlpha : 1);
    }

    // CircularProgressDrawable draws the arc from the turn to the turn plus the trim (in turns, from three o'clock, clockwise); this ring is centred on Angle from twelve.
    private void Trim(RefreshArc arc, double trim, double rotation) => Ring(arc, 0, trim, rotation, 0);

    // The ring as CircularProgressDrawable holds it: its start and end trims and its rotation (in turns), and the whole drawable's turn (in degrees).
    private void Ring(RefreshArc arc, double start, double end, double rotation, double group)
    {
        (_startTrim, _endTrim, _rotation) = (start, end, rotation);
        _group = group;
        arc.ArcLength = Math.Max(0, end - start) * 360;
        arc.Angle = (start + rotation) * 360 + group + 90 + arc.ArcLength / 2;
    }

    // Past the trigger: the circle settles where it spins (animateOffsetToCorrectPosition), at full alpha, and turns. From code (F5) it grows there instead.
    private void StartRefreshing(bool fromCode)
    {
        if (Arc is not { } arc) return;
        var endTarget = SpinnerOffsetEnd - CircleDiameter;
        arc.ArrowScale = 0;
        FadeArc(1);
        Opacity = 1;
        if (fromCode)
        {
            SetTop(endTarget);
            SetScale(0);
            Trim(arc, MaxProgressAngle * 0.75, 0);
            Animate(ScaleUpDuration, AccelerateDecelerate, SetScale, null);
        }
        else
        {
            var from = _top;
            Animate(ToTriggerDuration, Decelerate, t => SetTop(from + (endTarget - from) * t), null);
        }
        if (double.IsFinite(Progress)) Fill(arc);
        else Spin(arc);
    }

    // Let go short of the trigger (finishSpinner): the ring empties, the arrow goes, and the circle slides back out of sight.
    private void Cancel()
    {
        if (Arc is not { } arc) return;
        _spin++;
        Trim(arc, 0, 0);
        arc.ArrowScale = 0;
        var from = _top;
        Animate(ToStartDuration, Decelerate, t => SetTop(from + (-CircleDiameter - from) * t), Reset);
    }

    // The refresh is done (startScaleDownAnimation): the circle shrinks away where it is, then is put back out of sight (reset).
    private void ScaleDown() => Animate(ScaleDownDuration, AccelerateDecelerate, t => SetScale(1 - t), Reset);

    private void Reset()
    {
        _spin++;
        _move++;
        _fade++;
        _fill++;
        _filled = -1;
        _alphaTo = -1;
        Opacity = 0;
        SetScale(1);
        SetTop(-CircleDiameter);
        if (Arc is { } arc)
        {
            arc.ArrowScale = 0;
            arc.Opacity = StartingAlpha;
        }
    }

    // DecelerateInterpolator(DECELERATE_INTERPOLATION_FACTOR = 2): 1 − (1 − t)^(2·2).
    private static double Decelerate(double t) => 1 - Math.Pow(1 - t, 4);

    // Android's AccelerateDecelerateInterpolator, an Animation's own when none is set.
    private static double AccelerateDecelerate(double t) => Math.Cos((t + 1) * Math.PI) / 2 + 0.5;

    private void Animate(double ms, Func<double, double> ease, Action<double> step, Action? done)
    {
        var run = ++_move;
        if (Motion.IsStill(this) || TopLevel.GetTopLevel(this) is not { } top)
        {
            step(1);
            done?.Invoke();
            return;
        }
        TimeSpan? first = null;
        void Frame(TimeSpan now)
        {
            if (run != _move) return;
            first ??= now;
            var t = Math.Clamp((now - first.Value).TotalMilliseconds / ms, 0, 1);
            step(ease(t));
            if (t < 1) top.RequestAnimationFrame(Frame);
            else done?.Invoke();
        }
        top.RequestAnimationFrame(Frame);
    }

    // startProgressAlphaStartAnimation / startProgressAlphaMaxAnimation: the ring's alpha goes to 0.3 or 1 over 300 ms.
    private void FadeArc(double to)
    {
        if (Arc is not { } arc || to == _alphaTo) return;
        _alphaTo = to;
        var run = ++_fade;
        var from = arc.Opacity;
        if (Motion.IsStill(this) || TopLevel.GetTopLevel(this) is not { } top)
        {
            arc.Opacity = to;
            return;
        }
        TimeSpan? first = null;
        void Frame(TimeSpan now)
        {
            if (run != _fade) return;
            first ??= now;
            var t = Math.Clamp((now - first.Value).TotalMilliseconds / AlphaDuration, 0, 1);
            arc.Opacity = from + (to - from) * AccelerateDecelerate(t);
            if (t < 1) top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }

    private void SetScale(double s)
    {
        _scale = s;
        _size.ScaleX = _size.ScaleY = s;
    }

    // Places the circle's top that far past the pull's edge, and cuts the visualizer at that edge: the circle comes out from under the bar, never over it.
    private void SetTop(double top)
    {
        _top = top;
        if (_container is not { } container || _visualizer is not { } visualizer || !Android) return;
        var up = container.PullDirection == PullDirection.BottomToTop;
        var edge = Edge(container);
        var height = Bounds.Height > 0 ? Bounds.Height : CircleDiameter;
        // where the circle's centre is with no shift, in the container
        if (this.TranslatePoint(new Point(Bounds.Width / 2, height / 2), container) is not { } at) return;
        var rest = at.Y - _shift.Y;
        var want = up ? container.Bounds.Height - edge - top - height / 2 : edge + top + height / 2;
        _shift.Y = want - rest;
        var a = container.TranslatePoint(new Point(0, up ? 0 : edge), visualizer);
        var b = container.TranslatePoint(new Point(0, up ? container.Bounds.Height - edge : container.Bounds.Height), visualizer);
        if (a is { } from && b is { } to)
            visualizer.Clip = new RectangleGeometry(new Rect(-4000, from.Y, 8000 + container.Bounds.Width, Math.Max(0, to.Y - from.Y)));
    }

    /// <summary>The pull's edge in the container: the foot of the top bar the list scrolls under (<c>Shell.FrostsTopbar</c>), else the container's own edge.</summary>
    internal static double Edge(RefreshContainer container)
    {
        if (container.PullDirection != PullDirection.TopToBottom) return 0;
        var scroller = container.Content as ScrollViewer ?? (container.Content as Visual)?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroller is null || !Shell.GetFrostsTopbar(scroller) || Shell.FindTopbar(scroller) is not { } bar) return 0;
        return bar.TranslatePoint(new Point(0, bar.Bounds.Height), container) is { } foot ? Math.Max(0, foot.Y) : 0;
    }

    private static bool IsVertical(PullDirection direction) => direction is PullDirection.TopToBottom or PullDirection.BottomToTop;

    // F5 refreshes from the keyboard, as a browser does.
    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.F5 || e.KeyModifiers != KeyModifiers.None || _container is null) return;
        e.Handled = true;
        _container.RequestRefresh();
    }

    // A sideways pull: Avalonia moves the visualizer; the disc only shows the state. A vertical one at rest is out of sight.
    private void Show()
    {
        if (Arc is not { } arc) return;
        if (Android)
        {
            if (_state == RefreshVisualizerState.Idle) Reset();
            return;
        }
        switch (_state)
        {
            case RefreshVisualizerState.Idle:
                _spin++;
                Opacity = 0;
                break;
            case RefreshVisualizerState.Interacting:
                _spin++;
                Opacity = Math.Clamp(_ratio * 2.5, 0, 1);
                arc.Opacity = 0.55;
                arc.ArcLength = 30 + 240 * _ratio;
                arc.Angle = 270 * _ratio;
                break;
            case RefreshVisualizerState.Pending:
                _spin++;
                Opacity = 1;
                arc.Opacity = 1;
                arc.ArcLength = 270;
                arc.Angle = 270 * Math.Max(_ratio, 0.8);
                break;
            default:
                Opacity = 1;
                arc.Opacity = 1;
                Spin(arc);
                break;
        }
    }

    // CircularProgressDrawable's indeterminate run (start()): an arc left by the pull first draws its tail up to a dot at its head (half a cycle); then each 1332 ms cycle the head runs out to 0.8 of the ring (fast out, slow in) and the tail follows it, the ring turning 0.21 more each cycle and the whole drawable 216° (three turns in five cycles), until the refresh ends. A spinner keeps turning in a still window too.
    private void Spin(RefreshArc arc)
    {
        if (TopLevel.GetTopLevel(this) is not { } top) return;
        var run = ++_spin;
        var finishing = Math.Abs(_endTrim - _startTrim) > 1e-6;
        var (from, to, turn) = finishing ? (_startTrim, _endTrim, _rotation) : (0.0, 0.0, 0.0);
        var group = finishing ? _group : 0;
        var count = 0;
        TimeSpan? first = null;
        void Step(double t)
        {
            if (finishing)
            {
                var target = Math.Floor(turn / MaxProgressAngle) + 1;
                Ring(arc, from + (to - MinArc - from) * t, to, turn + (target - turn) * t, group);
                return;
            }
            double start, end;
            if (t < 0.5)
            {
                start = from;
                end = start + (MaxProgressAngle - MinArc) * FastOutSlowIn.Ease(t / 0.5) + MinArc;
            }
            else
            {
                end = to + (MaxProgressAngle - MinArc);
                start = end - ((MaxProgressAngle - MinArc) * (1 - FastOutSlowIn.Ease((t - 0.5) / 0.5)) + MinArc);
            }
            Ring(arc, start, end, turn + RingRotation * t, GroupFullRotation * (t + count));
        }
        void Frame(TimeSpan now)
        {
            if (run != _spin) return;
            first ??= now;
            var ms = (now - first.Value).TotalMilliseconds;
            // onAnimationRepeat: the cycle ends where it is, which the next one starts from
            for (var length = finishing ? Cycle / 2 : Cycle; ms >= length; length = finishing ? Cycle / 2 : Cycle)
            {
                Step(1);
                (from, to, turn) = (_startTrim, _endTrim, _rotation);
                if (finishing) finishing = false;
                else count = (count + 1) % 5;
                ms -= length;
                first = now - TimeSpan.FromMilliseconds(ms);
            }
            Step(ms / (finishing ? Cycle / 2 : Cycle));
            top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }
}
