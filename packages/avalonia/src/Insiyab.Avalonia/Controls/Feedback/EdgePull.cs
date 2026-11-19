using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A pull to refresh that starts as Android's does (AndroidX <c>SwipeRefreshLayout</c>): only when the finger lands with the list at its edge and then goes clearly the pull's way past the touch slop, measured from the end of the slop; the pull is half the finger's way (<see cref="DragRate"/>) and refreshes past <see cref="TriggerDistance"/>, at any pace. A wheel or a touchpad's scroll never pulls (<see cref="Wheeled"/>): a stroke that meets one gives the pull up. Nor does the Android emulator's wheel (<see cref="IsMachineStroke"/>), which it turns into a drag no finger makes: dead straight, at one steady fast pace from its first step.</summary>
internal sealed class EdgePull : GestureRecognizer
{
    // Avalonia's own ids come from 1 upward; these never meet them.
    private static int s_nextId = 1 << 24;

    private readonly RefreshContainer _container;
    private IPointer? _tracking;
    private Point _start;
    private bool _mayPull;   // the stroke landed at the edge and has not given the pull up
    private bool _pulling;
    private bool _captured;
    private double _origin;  // where along the pull's axis the pull is measured from
    private int _id;
    private readonly List<(long At, double Along, double Across)> _steps = new(); // the stroke's moves, from its landing

    /// <summary><c>SwipeRefreshLayout.DRAG_RATE</c>: the pull ("overscroll") is this much of the finger's way past the slop.</summary>
    internal const double DragRate = 0.5;

    /// <summary><c>SwipeRefreshLayout.DEFAULT_CIRCLE_TARGET</c> (dp), its <c>mTotalDragDistance</c>: a pull past this refreshes (a finger's 128 dp).</summary>
    internal const double TriggerDistance = 64;

    // Avalonia's RefreshVisualizer refreshes past this share of its own size (RefreshInfoProvider's execution ratio).
    private const double AvaloniaRatio = 0.8;

    // The Android emulator's wheel: every step at the same pace, faster than this (dp per ms), with not a hair across.
    private const double MachinePace = 2.0;

    // After a wheel turned over a container, for this long no stroke there starts a pull (a touchpad's scroll can come as a finger's).
    private static readonly TimeSpan WheelQuiet = TimeSpan.FromMilliseconds(600);

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RefreshContainer, object> s_wheeled = new();

    private EdgePull(RefreshContainer container) => _container = container;

    /// <summary>A wheel (or a touchpad's scroll) turned over the container: no pull starts or goes on for a moment.</summary>
    internal static void Wheeled(RefreshContainer container)
    {
        s_wheeled.AddOrUpdate(container, DateTime.UtcNow);
        var scroller = container.Content as ScrollViewer ?? (container.Content as Visual)?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroller?.Content is not Visual { Parent: InputElement source }) return;
        foreach (var r in source.GestureRecognizers)
            if (r is EdgePull { _pulling: false } pull) pull._mayPull = false;
    }

    private bool RecentlyWheeled => s_wheeled.TryGetValue(_container, out var at) && at is DateTime t && DateTime.UtcNow - t < WheelQuiet;

    /// <summary>Puts this pull in place of Avalonia's on the container's scroller, as each press comes.</summary>
    public static void Use(RefreshContainer container)
    {
        var scroller = container.Content as ScrollViewer ?? (container.Content as Visual)?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroller?.Content is not Visual { Parent: InputElement source }) return;
        GestureRecognizer? theirs = null;
        var ours = false;
        foreach (var r in source.GestureRecognizers)
        {
            if (r is EdgePull) ours = true;
            else if (r.GetType().Name == "ScrollablePullGestureRecognizer") theirs = r;
        }
        if (theirs is not null) source.GestureRecognizers.Remove(theirs);
        if (!ours && theirs is not null) source.GestureRecognizers.Add(new EdgePull(container));
    }

    private PullDirection Direction => _container.PullDirection;

    private bool Vertical => Direction is PullDirection.TopToBottom or PullDirection.BottomToTop;

    // How far along the pull's way, and across it, the finger is from where it landed.
    private (double Along, double Across) Moved(Point at)
    {
        var d = at - _start;
        return Direction switch
        {
            PullDirection.TopToBottom => (d.Y, d.X),
            PullDirection.BottomToTop => (-d.Y, d.X),
            PullDirection.LeftToRight => (d.X, d.Y),
            _ => (-d.X, d.Y),
        };
    }

    private bool AtEdge()
    {
        if (Target is not IScrollable s) return false;
        const double near = 1;
        return Direction switch
        {
            PullDirection.TopToBottom => s.Offset.Y < near,
            PullDirection.BottomToTop => Math.Abs(s.Offset.Y + s.Viewport.Height - s.Extent.Height) <= near,
            PullDirection.LeftToRight => s.Offset.X < near,
            _ => Math.Abs(s.Offset.X + s.Viewport.Width - s.Extent.Width) <= near,
        };
    }

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        var usable = e.Pointer.Type is PointerType.Touch or PointerType.Pen || _container.IsMouseEnabled;
        if (Target is not Visual visual || !usable) return;
        _tracking = e.Pointer;
        _start = e.GetPosition(visual);
        _steps.Clear();
        _steps.Add((Now(e), 0, 0));
        _mayPull = AtEdge() && !RecentlyWheeled;
        _pulling = false;
        _captured = false;
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (_tracking != e.Pointer || Target is not Visual visual) return;
        if (!_pulling && !_mayPull) return;
        // the list left its edge during the stroke (it scrolled): this stroke never pulls again
        if (!_pulling && !AtEdge())
        {
            _mayPull = false;
            return;
        }
        var (along, across) = Moved(e.GetPosition(visual));
        _steps.Add((Now(e), along, across));
        // the emulator's wheel, not a finger: the stroke gives the pull up, and a pull already shown goes back without a refresh
        if (IsMachineStroke(_steps))
        {
            GiveUp(e.Pointer);
            return;
        }
        if (!_pulling)
        {
            if (Math.Max(Math.Abs(along), Math.Abs(across)) < Touch.Slop) return; // too small to tell yet
            if (along <= 0 || Math.Abs(across) >= along)
            {
                _mayPull = false; // the other way first, or mostly across: a scroll or a swipe, not a pull
                return;
            }
            _pulling = true;
            _origin = Touch.Slop;
            _id = Interlocked.Increment(ref s_nextId);
        }
        var by = ToDelta(_container, (along - _origin) * DragRate);
        if (by <= 0)
        {
            if (!_captured)
            {
                // nothing pulled yet: still at the dead zone's edge it waits; back inside it the stroke gives the pull up
                if (by < 0)
                    _pulling = _mayPull = false;
                return;
            }
            // back past where it began: the pull ends; down again while the list is still at its edge pulls again, a scroll meanwhile ends that
            End();
            return;
        }
        Capture(e.Pointer);
        _captured = true;
        var args = new PullGestureEventArgs(_id, Vertical ? new Vector(0, by) : new Vector(by, 0), Direction);
        Target?.RaiseEvent(args);
        e.Handled = args.Handled;
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        if (_tracking != e.Pointer) return;
        try
        {
            if (_pulling) End();
        }
        finally
        {
            if (_captured) e.Pointer.Capture(null);
            _captured = false;
            _tracking = null;
            _mayPull = false;
        }
    }

    protected override void PointerCaptureLost(IPointer pointer)
    {
        if (_tracking == pointer && _pulling) End();
        _captured = false;
        _tracking = null;
        _mayPull = false;
    }

    /// <summary>Whether a stroke is the Android emulator's wheel rather than a finger (seen with getevent): a touch at the cursor that runs at once, from its first step, at one steady pace above 2 dp/ms (about 8% of the screen every 15 ms), exactly along the pull with nothing across. A finger starts from rest, speeds up and slows down, and never stays on one pixel column; so after three steps a stroke that matches all of it is the wheel's.</summary>
    internal static bool IsMachineStroke(IReadOnlyList<(long At, double Along, double Across)> steps)
    {
        if (steps.Count < 4) return false; // the landing and three moves
        double? first = null;
        for (var i = 1; i < steps.Count; i++)
        {
            if (steps[i].Across != 0) return false;
            var dt = steps[i].At - steps[i - 1].At;
            if (dt <= 0) return false;
            var pace = (steps[i].Along - steps[i - 1].Along) / dt;
            if (pace <= MachinePace) return false;
            first ??= pace;
            if (Math.Abs(pace - first.Value) > first.Value * 0.25) return false;
        }
        return true;
    }

    /// <summary>The pull Avalonia's visualizer is told for an Android overscroll (dp): <see cref="TriggerDistance"/> lands on its refresh point.</summary>
    internal static double ToDelta(RefreshContainer container, double overscroll) => overscroll * AvaloniaRatio * VisualizerSize(container) / TriggerDistance;

    /// <summary>The Android overscroll (dp) for a pull Avalonia's visualizer was told (<see cref="ToDelta"/> back).</summary>
    internal static double ToOverscroll(RefreshContainer container, double delta) => delta * TriggerDistance / (AvaloniaRatio * VisualizerSize(container));

    private static double VisualizerSize(RefreshContainer container)
    {
        var v = container.Visualizer;
        var size = v is null ? 0 : container.PullDirection is PullDirection.TopToBottom or PullDirection.BottomToTop ? v.Bounds.Height : v.Bounds.Width;
        return size > 0 ? size : 160;
    }

    // the event's own time (the platform's clock), else the moment it is handled
    private static long Now(PointerEventArgs e) => e.Timestamp > 0 ? (long)e.Timestamp : System.Diagnostics.Stopwatch.GetTimestamp() * 1000 / System.Diagnostics.Stopwatch.Frequency;

    private void GiveUp(IPointer pointer)
    {
        if (_pulling)
        {
            if (_captured) Target?.RaiseEvent(new PullGestureEventArgs(_id, default, Direction)); // nothing pulled: the end refreshes nothing
            End();
        }
        if (_captured) pointer.Capture(null);
        _captured = false;
        _mayPull = false;
    }

    private void End()
    {
        _pulling = false;
        Target?.RaiseEvent(new PullGestureEndedEventArgs(_id, Direction));
    }
}
