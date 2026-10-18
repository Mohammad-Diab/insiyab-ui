using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A click too quick to show the held press still dips once on release, as on the web; a theme sets <c>Depth</c> on the template part the dip scales.</summary>
internal static class PressDip
{
    /// <summary>The scale at the bottom of the dip, on a button's template part; 0 turns the dip off.</summary>
    public static readonly AttachedProperty<double> DepthProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("Depth", typeof(PressDip));

    private static readonly AttachedProperty<CancellationTokenSource?> RunProperty =
        AvaloniaProperty.RegisterAttached<Control, CancellationTokenSource?>("Run", typeof(PressDip));

    private static readonly Easing EaseOut = new SplineEasing(0, 0, 0.58, 1);
    private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
    private static Button? s_button;

    /// <summary>For tests: the milliseconds that tell a tap from a held press.</summary>
    internal static Func<long> Now { get; set; } = () => Clock.ElapsedMilliseconds;

    /// <summary>For tests: sees each dip as it starts, with the part it scales.</summary>
    internal static Action<Control, Animation>? Started { get; set; }
    private static long s_at;

    static PressDip()
    {
        TransformOperationsAnimator.Ensure();
        InputElement.PointerPressedEvent.AddClassHandler<Button>(OnPointerPressed, RoutingStrategies.Tunnel, true);
        InputElement.PointerReleasedEvent.AddClassHandler<Button>(OnPointerReleased, RoutingStrategies.Tunnel, true);
        InputElement.KeyDownEvent.AddClassHandler<Button>(OnKeyDown, RoutingStrategies.Tunnel, true);
        InputElement.KeyUpEvent.AddClassHandler<Button>(OnKeyUp, RoutingStrategies.Tunnel, true);
    }

    public static double GetDepth(Control c) => c.GetValue(DepthProperty);

    public static void SetDepth(Control c, double value) => c.SetValue(DepthProperty, value);

    private static bool Owns(Button button, object? source) =>
        (source as Visual)?.FindAncestorOfType<Button>(true) == button;

    private static void Start(Button button)
    {
        if (s_button == button) return;
        s_button = button;
        s_at = Now();
    }

    private static void End(Button button, bool inside)
    {
        if (s_button != button) return;
        s_button = null;
        if (inside && Now() - s_at < 150) Dip(button);
    }

    private static void OnPointerPressed(Button button, PointerPressedEventArgs e)
    {
        if (Owns(button, e.Source) && e.GetCurrentPoint(button).Properties.IsLeftButtonPressed) Start(button);
    }

    private static void OnPointerReleased(Button button, PointerReleasedEventArgs e)
    {
        if (!Owns(button, e.Source) && s_button != button) return;
        End(button, new Rect(button.Bounds.Size).Contains(e.GetPosition(button)));
    }

    private static void OnKeyDown(Button button, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space && Owns(button, e.Source)) Start(button);
    }

    private static void OnKeyUp(Button button, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space) End(button, true);
    }

    private static void Dip(Button button)
    {
        if (!button.IsEffectivelyEnabled || Motion.IsStill(button)) return;
        var part = button.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.TemplatedParent == button && GetDepth(c) is > 0 and < 1);
        if (part is null) return;
        var depth = GetDepth(part);
        part.GetValue(RunProperty)?.Cancel();
        var run = new CancellationTokenSource();
        part.SetValue(RunProperty, run);
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(240),
            Easing = EaseOut,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.RenderTransformProperty, TransformOperations.Parse("scale(1)")) } },
                new KeyFrame { Cue = new Cue(0.35), Setters = { new Setter(Visual.RenderTransformProperty, TransformOperations.Parse(FormattableString.Invariant($"scale({depth})"))) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.RenderTransformProperty, TransformOperations.Parse("scale(1)")) } },
            },
        };
        Started?.Invoke(part, animation);
        _ = animation.RunAsync(part, run.Token);
    }
}
