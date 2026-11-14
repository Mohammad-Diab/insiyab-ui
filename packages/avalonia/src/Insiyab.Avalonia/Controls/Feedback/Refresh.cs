using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The brand spinner's ring drawn by a pull to refresh: it grows and turns with the finger, and spins while the app refreshes.</summary>
internal sealed class RefreshArc : LoadingSpinner;

/// <summary>The small round surface a <see cref="RefreshVisualizer"/> brings down from the top, holding a <see cref="RefreshArc"/>; it follows the visualizer's state and the pull, and lets the list glide back once the refresh is done.</summary>
internal sealed class RefreshDisc : Border
{
    // One turn of the spinner, as LoadingSpinner turns.
    private const double Turn = 700;

    private RefreshVisualizer? _visualizer;
    private RefreshContainer? _container;
    private RefreshVisualizerState _state = RefreshVisualizerState.Idle;
    private double _ratio;
    private long _spin;

    internal RefreshVisualizerState State => _state;

    private RefreshArc? Arc => Child as RefreshArc;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _visualizer = TemplatedParent as RefreshVisualizer ?? this.FindAncestorOfType<RefreshVisualizer>();
        if (_visualizer is not null) _visualizer.PropertyChanged += OnVisualizerChanged;
        _container = this.FindAncestorOfType<RefreshContainer>();
        if (_container is not null)
        {
            _container.AddHandler(InputElement.PullGestureEvent, OnPull, RoutingStrategies.Bubble, handledEventsToo: true);
            _container.AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Bubble);
        }
        Show();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_visualizer is not null) _visualizer.PropertyChanged -= OnVisualizerChanged;
        if (_container is not null)
        {
            _container.RemoveHandler(InputElement.PullGestureEvent, OnPull);
            _container.RemoveHandler(KeyDownEvent, OnKey);
        }
        _visualizer = null;
        _container = null;
        _spin++;
    }

    // Avalonia keeps the state property itself internal, so its change is read by name.
    private void OnVisualizerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != "RefreshVisualizerState" || e.NewValue is not RefreshVisualizerState state) return;
        var was = _state;
        _state = state;
        if (state == RefreshVisualizerState.Idle) _ratio = 0;
        if (state == RefreshVisualizerState.Refreshing && was != RefreshVisualizerState.Refreshing) Glide(true);
        else if (state == RefreshVisualizerState.Interacting) Glide(false);
        // The list scrolls by its offset too, so the glide is taken off once the way back is done.
        if (state == RefreshVisualizerState.Idle && was == RefreshVisualizerState.Refreshing)
            DispatcherTimer.RunOnce(() => { if (_state == RefreshVisualizerState.Idle) Glide(false); }, Motion.Exit(this) + TimeSpan.FromMilliseconds(60));
        Show();
    }

    private void OnPull(object? sender, PullGestureEventArgs e)
    {
        var size = _visualizer?.Bounds.Height ?? 0;
        if (size <= 0) return;
        _ratio = Math.Clamp(Math.Abs(e.Delta.Y) / size, 0, 1);
        if (_state is RefreshVisualizerState.Interacting or RefreshVisualizerState.Pending) Show();
    }

    // F5 refreshes from the keyboard, as a browser does.
    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.F5 || e.KeyModifiers != KeyModifiers.None || _container is null) return;
        e.Handled = true;
        _container.RequestRefresh();
    }

    private void Show()
    {
        if (Arc is not { } arc) return;
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

    // The ring shortens to the spinner's quarter and turns until the refresh ends; a spinner keeps turning in a still window too.
    private void Spin(RefreshArc arc)
    {
        if (TopLevel.GetTopLevel(this) is not { } top) return;
        var run = ++_spin;
        var from = Motion.IsStill(this) ? 90 : arc.ArcLength;
        var shrink = Motion.Enter(this).TotalMilliseconds;
        var easing = Motion.Ease(this);
        arc.ArcLength = from;
        TimeSpan? first = null;
        TimeSpan? last = null;
        void Frame(TimeSpan now)
        {
            if (run != _spin) return;
            first ??= now;
            var dt = last is { } l ? (now - l).TotalMilliseconds : 0;
            last = now;
            arc.Angle = (arc.Angle + dt * 360 / Turn) % 360;
            var t = Math.Clamp((now - first.Value).TotalMilliseconds / shrink, 0, 1);
            arc.ArcLength = from + (90 - from) * easing.Ease(t);
            top.RequestAnimationFrame(Frame);
        }
        top.RequestAnimationFrame(Frame);
    }

    // While refreshing, the indicator and the list are told to ease their way back when it ends, rather than jump; a new pull takes the finger directly again.
    private void Glide(bool on)
    {
        var scroller = _container?.Content as ScrollViewer ?? (_container?.Content as Visual)?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        var content = scroller?.Presenter?.Child;
        var duration = Motion.Exit(this);
        var easing = Motion.Ease(this);
        foreach (var v in new Visual?[] { _visualizer, content })
        {
            if (v is null || ElementComposition.GetElementVisual(v) is not { } visual) continue;
            try
            {
                if (!on || Motion.IsStill(this))
                {
                    visual.ImplicitAnimations = null;
                    continue;
                }
                var c = visual.Compositor;
                var move = c.CreateVector3DKeyFrameAnimation();
                move.Target = "Offset";
                move.InsertExpressionKeyFrame(1f, "this.FinalValue", easing);
                move.Duration = duration;
                var set = c.CreateImplicitAnimationCollection();
                set["Offset"] = move;
                visual.ImplicitAnimations = set;
            }
            catch (Exception) { }
        }
    }
}
