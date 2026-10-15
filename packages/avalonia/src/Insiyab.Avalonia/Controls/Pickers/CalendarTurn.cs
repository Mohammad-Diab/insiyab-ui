using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The calendar switch's turn, the web's calTurn made calmer for a desktop picker: each day glides to its cell in the other calendar and folds flat halfway to show its other number, column by column; new days fade in, leaving days fade out as ghosts, the titles drop in and the switch's face flies to the new side.</summary>
internal sealed class CalendarTurn
{
    internal const double Length = 420, Wave = 20, TitleLength = 360, TitleDrop = 6, FaceLength = 600;

    private static readonly SplineEasing Ease = new(0.22, 1, 0.36, 1);
    private static readonly SplineEasing FoldIn = new(0.5, 0, 0.75, 0);
    private static readonly SplineEasing FoldOut = new(0.25, 1, 0.5, 1);

    /// <summary>A day as it stood before the switch: its box in the turn layer, its column, its number and its classes.</summary>
    internal readonly record struct Was(Rect At, int Column, string Label, string[] Classes);

    private readonly DateCalendar _cal;
    private readonly Canvas _layer;
    private Stopwatch? _clock;
    private readonly List<Mark> _marks = [];
    private bool _done;

    private CalendarTurn(DateCalendar cal, Canvas layer)
    {
        _cal = cal;
        _layer = layer;
    }

    /// <summary>Whether the turn still moves.</summary>
    internal bool IsRunning => !_done;

    /// <summary>For tests: while set, a turn that starts keeps its first frame, so where it starts from can be read.</summary>
    internal static bool Held { get; set; }

    /// <summary>Where every day is now, or null when the calendar cannot turn (no layer, or motion is still).</summary>
    internal static Dictionary<DateTime, Was>? Snap(DateCalendar cal, CalendarTurn? running)
    {
        if (cal.TurnLayer is not { } layer || Motion.IsStill(cal) || !Seen.Now(cal)) return null;
        var map = new Dictionary<DateTime, Was>();
        foreach (var (day, btn) in cal.DayButtons)
        {
            if (Center(btn, layer) is not { } c) continue;
            var size = btn.Bounds.Size;
            var label = running?.RealLabel(btn) ?? btn.Content as string ?? string.Empty;
            var column = btn.Parent is Control holder ? Grid.GetColumn(holder) : 0;
            map[day] = new Was(new Rect(c.X - size.Width / 2, c.Y - size.Height / 2, size.Width, size.Height), column, label, btn.Classes.Where(k => !k.StartsWith(':')).ToArray());
        }
        return map;
    }

    /// <summary>Starts the turn on the rebuilt calendar from the days it had before; returns null when nothing moves.</summary>
    internal static CalendarTurn? Play(DateCalendar cal, Dictionary<DateTime, Was>? before)
    {
        if (before is null || before.Count == 0 || cal.TurnLayer is not { } layer || TopLevel.GetTopLevel(cal) is null) return null;
        cal.UpdateLayout();
        var turn = new CalendarTurn(cal, layer);
        turn.Start(before);
        return turn;
    }

    // The middle of a control in the layer, which a fold about the middle leaves where it is.
    private static Point? Center(Control c, Visual layer) => c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), layer);

    private void Start(Dictionary<DateTime, Was> before)
    {
        var days = _cal.DayButtons;
        var shift = default(Vector);
        foreach (var (day, btn) in days)
            if (before.TryGetValue(day, out var w) && Center(btn, _layer) is { } c) { shift = w.At.Center - c; break; }

        foreach (var (day, btn) in days)
        {
            var delay = (btn.Parent is Control holder ? Grid.GetColumn(holder) : 0) * Wave;
            if (before.TryGetValue(day, out var w) && Center(btn, _layer) is { } c)
                _marks.Add(new Glide(btn, w.At.Center - c, delay, w.Label, btn.Content as string ?? string.Empty));
            else _marks.Add(new Arrive(btn, shift, delay));
        }

        foreach (var (day, w) in before)
        {
            if (days.ContainsKey(day)) continue;
            var ghost = new Button
            {
                Content = w.Label,
                Width = w.At.Width,
                Height = w.At.Height,
                Margin = default,
                IsHitTestVisible = false,
                Focusable = false,
                IsTabStop = false,
            };
            ghost.Classes.AddRange(w.Classes);
            AutomationProperties.SetAccessibilityView(ghost, AccessibilityView.Raw);
            Canvas.SetLeft(ghost, w.At.X);
            Canvas.SetTop(ghost, w.At.Y);
            _layer.Children.Add(ghost);
            _marks.Add(new Leave(ghost, shift, w.Column * Wave, _layer));
        }

        foreach (var c in _cal.HostControls())
            if (c.Classes.Contains("cal-title") || c.Classes.Contains("cal-title-next")) _marks.Add(new Drop(c));

        if (_cal.HostControls().OfType<Border>().FirstOrDefault(b => b.Classes.Contains("cal-switch")) is { Child: Panel panel } track
            && panel.Children.OfType<Canvas>().FirstOrDefault() is { } flight
            && panel.Children.OfType<StackPanel>().FirstOrDefault() is { } row)
        {
            var options = row.Children.OfType<Button>().ToList();
            var to = options.FirstOrDefault(o => o.Classes.Contains("active"));
            var from = options.FirstOrDefault(o => o != to);
            if (to is not null && from is not null && from.TranslatePoint(default, flight) is { } pa && to.TranslatePoint(default, flight) is { } pb && to.Bounds.Width > 0)
            {
                var face = new Border { Classes = { "cal-switch-face" }, Height = to.Bounds.Height, IsHitTestVisible = false };
                Canvas.SetTop(face, pb.Y);
                flight.Children.Add(face);
                track.Classes.Add("moving");
                _marks.Add(new Fly(face, flight, track, pa.X, from.Bounds.Width, pb.X, to.Bounds.Width));
            }
        }

        Step(0);
        var longest = Math.Max(Math.Max(Length + 6 * Wave, TitleLength), FaceLength);
        // Frames stop when the window hides, so the turn also lands on a timer.
        DispatcherTimer.RunOnce(Stop, TimeSpan.FromMilliseconds(longest + 150));
        Frame();
    }

    private string? RealLabel(Button btn) => _done ? null : _marks.OfType<Glide>().FirstOrDefault(g => g.Day == btn)?.Real;

    private void Frame()
    {
        if (_done) return;
        if (TopLevel.GetTopLevel(_cal) is not { } top) { Stop(); return; }
        top.RequestAnimationFrame(_ =>
        {
            // The clock starts with the first frame, so a slow first layout does not eat the start of the turn.
            if (!Held) _clock ??= Stopwatch.StartNew();
            if (Step(_clock?.Elapsed.TotalMilliseconds ?? 0)) Frame();
        });
    }

    private bool Step(double now)
    {
        if (_done) return false;
        for (var i = _marks.Count - 1; i >= 0; i--)
            if (!_marks[i].Step(now)) _marks.RemoveAt(i);
        if (_marks.Count > 0) return true;
        _done = true;
        return false;
    }

    /// <summary>Puts every day at rest with its own number at once and drops the ghosts and the flying face.</summary>
    internal void Stop()
    {
        if (_done) return;
        _done = true;
        foreach (var m in _marks) m.Stop();
        _marks.Clear();
    }

    private abstract class Mark
    {
        private IDisposable? _opacity;
        private IDisposable? _transform;

        public abstract bool Step(double now);

        public virtual void Stop()
        {
            _opacity?.Dispose();
            _transform?.Dispose();
            _opacity = _transform = null;
        }

        // Laid over the control at animation priority, so its own values come back untouched when the turn ends.
        protected void Place(Visual v, double opacity, Matrix matrix)
        {
            var o = opacity < 1 ? v.SetValue(Visual.OpacityProperty, opacity, BindingPriority.Animation) : null;
            _opacity?.Dispose();
            _opacity = o;
            var t = v.SetValue(Visual.RenderTransformProperty, new MatrixTransform(matrix), BindingPriority.Animation);
            _transform?.Dispose();
            _transform = t;
        }

        protected static double Progress(double now, double delay, double length) => Math.Clamp((now - delay) / length, 0, 1);
    }

    // A day in both calendars: from its old cell to its new one, folding flat at the middle, where its number changes.
    private sealed class Glide(Button day, Vector from, double delay, string back, string real) : Mark
    {
        public Button Day { get; } = day;
        public string Real { get; } = real;

        public override bool Step(double now)
        {
            var u = Progress(now, delay, Length);
            if (u >= 1) { Stop(); return false; }
            double move, fold;
            if (u < 0.5)
            {
                var q = FoldIn.Ease(u * 2);
                move = 1 - q / 2;
                fold = Math.Cos(q * Math.PI / 2);
                Label(back);
            }
            else
            {
                var q = FoldOut.Ease((u - 0.5) * 2);
                move = (1 - q) / 2;
                fold = Math.Cos((1 - q) * Math.PI / 2);
                Label(Real);
            }
            Place(Day, 1, Matrix.CreateScale(1, Math.Max(fold, 0.02)) * Matrix.CreateTranslation(from.X * move, from.Y * move));
            return true;
        }

        public override void Stop()
        {
            base.Stop();
            Label(Real);
        }

        private void Label(string text)
        {
            if (!Equals(Day.Content, text)) Day.Content = text;
        }
    }

    // A day only the new calendar shows: it comes in from where the old grid was, fading in after 40% of the way.
    private sealed class Arrive(Control day, Vector shift, double delay) : Mark
    {
        public override bool Step(double now)
        {
            var u = Progress(now, delay, Length);
            if (u >= 1) { Stop(); return false; }
            var p = Ease.Ease(u);
            Place(day, p < 0.4 ? 0 : (p - 0.4) / 0.6, Matrix.CreateTranslation(shift.X * (1 - p), shift.Y * (1 - p)));
            return true;
        }
    }

    // A day only the old calendar showed: a copy where it was that moves on with the grid and is gone by 45% of the way.
    private sealed class Leave(Control ghost, Vector shift, double delay, Canvas layer) : Mark
    {
        public override bool Step(double now)
        {
            var u = Progress(now, delay, Length);
            if (u >= 1) { Stop(); return false; }
            var p = Ease.Ease(u);
            Place(ghost, p < 0.45 ? 1 - p / 0.45 : 0, Matrix.CreateTranslation(-shift.X * p, -shift.Y * p));
            return true;
        }

        public override void Stop()
        {
            base.Stop();
            layer.Children.Remove(ghost);
        }
    }

    // The month and year drop 6px into place as they fade in.
    private sealed class Drop(Control title) : Mark
    {
        public override bool Step(double now)
        {
            var u = Progress(now, 0, TitleLength);
            if (u >= 1) { Stop(); return false; }
            var p = Ease.Ease(u);
            Place(title, p, Matrix.CreateTranslation(0, -TitleDrop * (1 - p)));
            return true;
        }
    }

    // The switch's face: its far edge reaches the new side in the first third, then its near edge follows.
    private sealed class Fly(Border face, Canvas layer, Border track, double ax, double aw, double bx, double bw) : Mark
    {
        public override bool Step(double now)
        {
            var t = Progress(now, 0, FaceLength);
            var (x, w) = MarkerFlight.At(t, (ax, aw), (bx, bw));
            Put(x, w);
            if (t < 1) return true;
            Stop();
            return false;
        }

        private void Put(double x, double w)
        {
            Canvas.SetLeft(face, x);
            face.Width = Math.Max(0, w);
        }

        public override void Stop()
        {
            base.Stop();
            layer.Children.Remove(face);
            track.Classes.Remove("moving");
        }
    }
}
