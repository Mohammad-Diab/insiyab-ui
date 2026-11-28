using Avalonia.Animation.Easings;

namespace Insiyab.Avalonia.Controls;

/// <summary>A ring that fills while it keeps moving, as the Play Store's install ring does: its arc's length eases to each new share (fast out, slow in, over <see cref="EaseMs"/>), never shorter than <see cref="MinShare"/>, while the whole ring turns at a steady pace, so work that waits between its parts never looks stopped. Stepped by its owner's frames; a still window (<see cref="Motion"/>) takes each share at once (<see cref="Reach"/>) and still turns.</summary>
internal sealed class TurningFill
{
    /// <summary>The shortest arc (share of the ring): what turns before the first part is done, so the ring is never empty.</summary>
    internal const double MinShare = 0.12;

    /// <summary>How long the arc takes to reach a new share (ms): CircularProgressDrawable's alpha animation, the pull's own short ease.</summary>
    internal const double EaseMs = 300;

    /// <summary>The turn per ms: CircularProgressDrawable's indeterminate ring without its arc's run (the ring's 0.21 and the drawable's 216° each 1332 ms cycle), a turn in about 1.6 s.</summary>
    internal const double TurnsPerMs = (0.21 + 216.0 / 360) / 1332;

    private static readonly SplineEasing FastOutSlowIn = new(0.4, 0, 0.2, 1);

    private double _from, _to;
    private TimeSpan? _start, _last;

    /// <summary>The arc's length as last stepped (share of the ring); negative before <see cref="Begin"/>.</summary>
    public double Length { get; private set; } = -1;

    /// <summary>Where the arc starts (turns, from wherever its owner counts), as last stepped.</summary>
    public double Turn { get; private set; }

    /// <summary>The share the arc is easing to.</summary>
    public double Target => _to;

    public bool IsStarted => Length >= 0;

    /// <summary>The arc has not reached its share yet.</summary>
    public bool IsEasing => IsStarted && Math.Abs(Length - _to) > 1e-9;

    /// <summary>Starts from an arc already drawn (its length and start), or from the shortest arc.</summary>
    public void Begin(double length = MinShare, double turn = 0)
    {
        Length = _from = _to = Math.Clamp(length, 0, 1);
        Turn = turn;
        _start = _last = null;
    }

    /// <summary>Goes back to not started.</summary>
    public void Clear()
    {
        Length = -1;
        _start = _last = null;
    }

    /// <summary>A new share, 0 to 1: the arc eases from where it is to it (to the shortest arc at least).</summary>
    public void Aim(double share)
    {
        if (!IsStarted) Begin();
        var to = Math.Max(MinShare, Math.Clamp(double.IsFinite(share) ? share : 0, 0, 1));
        if (Math.Abs(to - _to) < 1e-9) return;
        _from = Length;
        _to = to;
        _start = null;
    }

    /// <summary>The arc at its share at once, its start at <paramref name="turn"/>, the ease over (a still window).</summary>
    public void Settle(double turn)
    {
        if (!IsStarted) Begin();
        Length = _from = _to;
        Turn = turn;
        _start = null;
    }

    /// <summary>The arc at its share at once, the turn kept (a still window: no ease, still turning).</summary>
    public void Reach()
    {
        if (!IsStarted) Begin();
        Length = _from = _to;
        _start = null;
    }

    /// <summary>Frames stopped for a while (a still window, a hidden ring): the next frame turns from there and does not jump.</summary>
    public void Pause() => _last = null;

    /// <summary>Moves to the frame at <paramref name="now"/>: the length along its ease, the start on by the time since the last frame.</summary>
    public void Step(TimeSpan now)
    {
        if (!IsStarted) Begin();
        _start ??= now;
        var dt = _last is { } last ? Math.Max(0, (now - last).TotalMilliseconds) : 0;
        _last = now;
        var t = Math.Clamp((now - _start.Value).TotalMilliseconds / EaseMs, 0, 1);
        Length = t >= 1 ? _to : _from + (_to - _from) * FastOutSlowIn.Ease(t);
        Turn = (Turn + dt * TurnsPerMs) % 1;
    }
}
