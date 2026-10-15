using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

// Every marker's move, as the web's flyMarker: from span A the far edge reaches span B first, in a third of the time, then the near edge follows; 600ms on two splines.
internal sealed class MarkerFlight
{
    public const double Duration = 600;

    private static readonly SplineEasing Out = new(0.9, 0.1, 1, 0.2);
    private static readonly SplineEasing In = new(0.1, 0.9, 0.2, 1);
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private readonly (double Start, double Length) _a;
    private readonly (double Start, double Length) _b;
    private readonly Action<double, double> _place;
    private readonly Action? _landed;
    private readonly double _start = Now();

    private MarkerFlight((double, double) a, (double, double) b, Action<double, double> place, Action? landed)
    {
        _a = a;
        _b = b;
        _place = place;
        _landed = landed;
    }

    // For tests: the milliseconds every flight is timed by, which a test can hold and step.
    internal static Func<double> Now { get; set; } = () => Clock.Elapsed.TotalMilliseconds;

    public bool IsDone { get; private set; }

    // Where the bar is now, so a move that starts before this one lands carries on from here.
    public double Start { get; private set; }

    public double Length { get; private set; }

    // The span at progress t (0 to 1) of a flight from a to b along one axis.
    public static (double Start, double Length) At(double t, (double Start, double Length) a, (double Start, double Length) b)
    {
        var lo = Math.Min(a.Start, b.Start);
        var hi = Math.Max(a.Start + a.Length, b.Start + b.Length);
        if (t < 1d / 3)
        {
            var k = Out.Ease(Math.Clamp(t * 3, 0, 1));
            return (a.Start + (lo - a.Start) * k, a.Length + (hi - lo - a.Length) * k);
        }
        var q = In.Ease(Math.Clamp((t - 1d / 3) * 1.5, 0, 1));
        return (lo + (b.Start - lo) * q, hi - lo + (b.Length - (hi - lo)) * q);
    }

    // Starts a flight where the owner is seen, placing the bar every frame; null when motion is still there, nobody can see the owner or there is nothing to fly.
    public static MarkerFlight? Run(Visual owner, (double Start, double Length) a, (double Start, double Length) b, Action<double, double> place, Action? landed = null)
    {
        if (Motion.IsStill(owner) || !Seen.Now(owner) || TopLevel.GetTopLevel(owner) is not { } top || a.Length <= 0 || b.Length <= 0) return null;
        var flight = new MarkerFlight(a, b, place, landed);
        flight.Set(0);
        // Frames stop arriving when the window is hidden, so the flight also lands on a timer.
        DispatcherTimer.RunOnce(flight.LandIfDue, TimeSpan.FromMilliseconds(Duration + 150));
        top.RequestAnimationFrame(_ => flight.Frame(top));
        return flight;
    }

    // The timer lands a flight only once its time is up, so a held clock is not cut short by it.
    private void LandIfDue()
    {
        if (Now() - _start >= Duration) Land();
    }

    private void Frame(TopLevel top)
    {
        if (IsDone) return;
        var t = Math.Clamp((Now() - _start) / Duration, 0, 1);
        Set(t);
        if (t >= 1) Land();
        else top.RequestAnimationFrame(_ => Frame(top));
    }

    private void Set(double t)
    {
        (Start, Length) = At(t, _a, _b);
        _place(Start, Math.Max(0, Length));
    }

    // Ends the flight where it is; the owner's landing runs once.
    public void Land()
    {
        if (IsDone) return;
        IsDone = true;
        _landed?.Invoke();
    }
}
