using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

// A value eased from 0 to 1 over a few frames, for motion that a finger can also hold (a page, a sheet): it starts from wherever it is told and lands at once when still or unseen.
internal sealed class FrameTween
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private readonly double _duration;
    private readonly Easing _easing;
    private readonly Action<double> _step;
    private readonly Action? _done;
    private readonly double _start = Now();

    private FrameTween(double duration, Easing easing, Action<double> step, Action? done)
    {
        _duration = duration;
        _easing = easing;
        _step = step;
        _done = done;
    }

    // For tests: the milliseconds every tween is timed by, which a test can hold and step.
    internal static Func<double> Now { get; set; } = () => Clock.Elapsed.TotalMilliseconds;

    public bool IsDone { get; private set; }

    // Runs step with the eased progress every frame and done once at the end; null when it landed at once.
    public static FrameTween? Run(Visual owner, TimeSpan duration, Easing easing, Action<double> step, Action? done = null)
    {
        if (duration <= TimeSpan.Zero || Motion.IsStill(owner) || !Seen.Now(owner) || TopLevel.GetTopLevel(owner) is not { } top)
        {
            step(1);
            done?.Invoke();
            return null;
        }
        var tween = new FrameTween(duration.TotalMilliseconds, easing, step, done);
        step(0);
        // Frames stop arriving when the window is hidden, so the tween also lands on a timer.
        DispatcherTimer.RunOnce(tween.LandIfDue, duration + TimeSpan.FromMilliseconds(150));
        top.RequestAnimationFrame(_ => tween.Frame(top));
        return tween;
    }

    private void LandIfDue()
    {
        if (Now() - _start >= _duration) Land();
    }

    private void Frame(TopLevel top)
    {
        if (IsDone) return;
        var t = Math.Clamp((Now() - _start) / _duration, 0, 1);
        if (t >= 1)
        {
            Land();
            return;
        }
        _step(_easing.Ease(t));
        top.RequestAnimationFrame(_ => Frame(top));
    }

    // Puts it at its end now; the end runs once.
    public void Land()
    {
        if (IsDone) return;
        IsDone = true;
        _step(1);
        _done?.Invoke();
    }

    // Leaves it where it is, with no end.
    public void Stop() => IsDone = true;
}
