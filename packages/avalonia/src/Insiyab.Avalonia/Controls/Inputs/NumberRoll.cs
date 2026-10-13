using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A step of a <see cref="NumericUpDown"/> rolls the number like an odometer, the web's <c>.ins-spin-roll</c>; the theme turns it on and needs <c>PART_Roll</c>, <c>PART_RollOut</c> and <c>PART_RollIn</c> over the text.</summary>
public static class NumberRoll
{
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<NumericUpDown, bool>("Enabled", typeof(NumberRoll));

    private static readonly AttachedProperty<State?> StateProperty =
        AvaloniaProperty.RegisterAttached<NumericUpDown, State?>("State", typeof(NumberRoll));

    private static readonly Easing Ease = new SplineEasing(0.22, 1, 0.36, 1);
    private static readonly Easing Spring = new SplineEasing(0.34, 1.56, 0.64, 1);
    private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

    /// <summary>For tests: the milliseconds that tell a burst of steps from single ones.</summary>
    internal static Func<long> Now { get; set; } = () => Clock.ElapsedMilliseconds;

    // Steps closer than this are a held button or a spun wheel: the number just counts, since a roll restarted every 100ms would hide it.
    private const long Burst = 150;

    private sealed class State
    {
        public string? Before;
        public long LastStep = -Burst;
        public CancellationTokenSource? Run;
    }

    static NumberRoll()
    {
        TransformOperationsAnimator.Ensure();
        EnabledProperty.Changed.AddClassHandler<NumericUpDown>((box, e) =>
        {
            box.PropertyChanged -= OnBoxPropertyChanged;
            box.Spinned -= OnSpinned;
            if (!e.GetNewValue<bool>()) return;
            box.SetValue(StateProperty, new State { Before = box.Text });
            box.PropertyChanged += OnBoxPropertyChanged;
            box.Spinned += OnSpinned;
        });
    }

    public static bool GetEnabled(NumericUpDown box) => box.GetValue(EnabledProperty);

    public static void SetEnabled(NumericUpDown box, bool value) => box.SetValue(EnabledProperty, value);

    private static void OnBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != NumericUpDown.TextProperty || sender is not NumericUpDown box || box.GetValue(StateProperty) is not { } state) return;
        state.Before = e.GetOldValue<string?>();
    }

    private static void OnSpinned(object? sender, SpinEventArgs e)
    {
        if (sender is not NumericUpDown box || box.GetValue(StateProperty) is not { } state) return;
        var now = Now();
        var burst = now - state.LastStep < Burst;
        state.LastStep = now;
        var from = state.Before;
        var to = box.Text;
        state.Before = to;
        if (burst || Motion.IsStill(box) || string.IsNullOrEmpty(from) || from == to)
        {
            Stop(box, state);
            return;
        }
        Roll(box, state, from, to ?? "", e.Direction == SpinDirection.Increase);
    }

    private static T? Find<T>(NumericUpDown box, string name) where T : Control =>
        box.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name && c.TemplatedParent == box);

    private static void Stop(NumericUpDown box, State state)
    {
        state.Run?.Cancel();
        state.Run = null;
        if (Find<Panel>(box, "PART_Roll") is { } roll) roll.IsVisible = false;
        ((IPseudoClasses)box.Classes).Set(":rolling", false);
    }

    private static void Roll(NumericUpDown box, State state, string from, string to, bool up)
    {
        if (Find<Panel>(box, "PART_Roll") is not { } roll || Find<TextBlock>(box, "PART_RollOut") is not { } gone || Find<TextBlock>(box, "PART_RollIn") is not { } next
            || Find<TextBox>(box, "PART_TextBox") is not { } field) return;
        Stop(box, state);
        var run = state.Run = new CancellationTokenSource();
        gone.Text = from;
        next.Text = to;
        roll.IsVisible = true;
        ((IPseudoClasses)box.Classes).Set(":rolling", true);
        // 90% of the field's height, as the web's translateY(90%); Avalonia applies the first operation first, so the scale is written before the move.
        var dy = Math.Max(1, field.Bounds.Height) * 0.9 * (up ? 1 : -1);
        var rest = TransformOperations.Parse("scale(1) translateY(0px)");
        string At(double y) => FormattableString.Invariant($"scale(0.6) translateY({y}px)");

        var leave = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(260),
            Easing = Ease,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 1d), new Setter(Visual.RenderTransformProperty, rest) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 0d), new Setter(Visual.RenderTransformProperty, TransformOperations.Parse(At(-dy))) } },
            },
        };
        var arrive = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(440),
            Easing = Spring,
            FillMode = FillMode.Backward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.RenderTransformProperty, TransformOperations.Parse(At(dy))) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.RenderTransformProperty, rest) } },
            },
        };
        // The spring overshoots past 1, and an opacity above 1 wraps to almost clear, so the fade takes the plain curve.
        var appear = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(440),
            Easing = Ease,
            FillMode = FillMode.Backward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 0d) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 1d) } },
            },
        };
        _ = leave.RunAsync(gone, run.Token);
        _ = appear.RunAsync(next, run.Token);
        _ = Tint(box, next, run.Token);
        _ = Finish(box, state, run, arrive.RunAsync(next, run.Token));
    }

    // The new number arrives in the brand tint and holds it until 55% of the way, then returns to the text colour.
    private static async Task Tint(NumericUpDown box, TextBlock next, CancellationToken token)
    {
        if (box.Foreground is not ISolidColorBrush ink || !box.TryFindResource("InsToneBrandSoft", box.ActualThemeVariant, out var found) || found is not ISolidColorBrush brand) return;
        var tint = new SolidColorBrush(brand.Color);
        next.Foreground = tint;
        var back = new Animation
        {
            Delay = TimeSpan.FromMilliseconds(242),
            Duration = TimeSpan.FromMilliseconds(198),
            Easing = Ease,
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(SolidColorBrush.ColorProperty, brand.Color) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(SolidColorBrush.ColorProperty, ink.Color) } },
            },
        };
        try { await back.RunAsync(tint, token); }
        finally { next.ClearValue(TextBlock.ForegroundProperty); }
    }

    private static async Task Finish(NumericUpDown box, State state, CancellationTokenSource run, Task arrival)
    {
        await arrival;
        if (state.Run != run) return;
        state.Run = null;
        if (Find<Panel>(box, "PART_Roll") is { } roll) roll.IsVisible = false;
        ((IPseudoClasses)box.Classes).Set(":rolling", false);
    }
}
