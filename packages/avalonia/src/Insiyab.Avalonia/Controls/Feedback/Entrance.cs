using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>An entrance for one element, the web's <c>.ins-anim-*</c> classes.</summary>
public enum EntranceEffect
{
    None,
    /// <summary>Rises 6px as it fades in, <c>ins-anim-rise</c>.</summary>
    Rise,
    /// <summary>Grows from .82 with a light spring, <c>ins-anim-pop</c>.</summary>
    Pop,
    /// <summary>Slides 10px in from the leading edge as it fades in, <c>ins-anim-slide</c>.</summary>
    Slide,
}

/// <summary>Entrance motion, the web's <c>data-ins-reveal</c> and <c>.ins-anim-*</c>. <c>Entrance.Reveal="True"</c> on a panel brings its children in one after another (the first ten 50ms apart, or as RevealCount and RevealStep say) as the panel fades in; <c>Entrance.Effect</c> brings one element in. Both run when the element appears, and <see cref="Replay"/> runs them again. The motion is laid over the element's own transform and opacity at animation priority for its duration only, so a control's own transitions are never touched.</summary>
public static class Entrance
{
    public static readonly AttachedProperty<bool> RevealProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Reveal", typeof(Entrance));

    public static readonly AttachedProperty<EntranceEffect> EffectProperty =
        AvaloniaProperty.RegisterAttached<Control, EntranceEffect>("Effect", typeof(Entrance));

    /// <summary>How far apart a revealing panel's children start: 50ms, or 70ms for the web's <c>.ins-grid</c>.</summary>
    public static readonly AttachedProperty<TimeSpan> RevealStepProperty =
        AvaloniaProperty.RegisterAttached<Control, TimeSpan>("RevealStep", typeof(Entrance), TimeSpan.FromMilliseconds(50));

    /// <summary>How many of a revealing panel's children start one after another; the rest start with the first. 10, or 8 for <c>.ins-grid</c>.</summary>
    public static readonly AttachedProperty<int> RevealCountProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("RevealCount", typeof(Entrance), 10);

    private static readonly Easing s_ease = new SplineEasing(0.22, 1, 0.36, 1);
    private static readonly Easing s_spring = new SplineEasing(0.34, 1.56, 0.64, 1);
    private static readonly ConditionalWeakTable<Control, Arrival> s_running = new();
    private static readonly List<Arrival> s_active = [];
    private static readonly Stopwatch s_clock = Stopwatch.StartNew();
    private static readonly HashSet<TopLevel> s_frames = [];

    // One element's entrance: from-values that ease to rest (opacity 1, no offset, scale 1) after a delay.
    private sealed class Arrival(Control target, double start, double delay, double duration, Easing easing, double opacity, double x, double y, double scale)
    {
        public readonly Control Target = target;
        private IDisposable? _opacity;
        private IDisposable? _transform;

        public bool Step(double now)
        {
            var t = now - start - delay;
            var p = t <= 0 ? 0 : t >= duration ? 1 : easing.Ease(t / duration);
            if (t >= duration)
            {
                Stop();
                return false;
            }
            var k = 1 - p;
            _opacity?.Dispose();
            _opacity = opacity < 1 ? Target.SetValue(Visual.OpacityProperty, 1 - (1 - opacity) * k, BindingPriority.Animation) : null;
            var s = 1 - (1 - scale) * k;
            ITransform transform = s == 1
                ? new TranslateTransform(x * k, y * k)
                : new TransformGroup { Children = { new ScaleTransform(s, s), new TranslateTransform(x * k, y * k) } };
            _transform?.Dispose();
            _transform = Target.SetValue(Visual.RenderTransformProperty, transform, BindingPriority.Animation);
            return true;
        }

        public void Stop()
        {
            _opacity?.Dispose();
            _transform?.Dispose();
            _opacity = _transform = null;
        }
    }

    static Entrance()
    {
        RevealProperty.Changed.AddClassHandler<Control>((c, _) => Hook(c));
        EffectProperty.Changed.AddClassHandler<Control>((c, _) => Hook(c));
    }

    public static bool GetReveal(Control c) => c.GetValue(RevealProperty);

    public static void SetReveal(Control c, bool value) => c.SetValue(RevealProperty, value);

    public static EntranceEffect GetEffect(Control c) => c.GetValue(EffectProperty);

    public static void SetEffect(Control c, EntranceEffect value) => c.SetValue(EffectProperty, value);

    public static TimeSpan GetRevealStep(Control c) => c.GetValue(RevealStepProperty);

    public static void SetRevealStep(Control c, TimeSpan value) => c.SetValue(RevealStepProperty, value);

    public static int GetRevealCount(Control c) => c.GetValue(RevealCountProperty);

    public static void SetRevealCount(Control c, int value) => c.SetValue(RevealCountProperty, value);

    /// <summary>Runs the entrance of <paramref name="root"/> and of every element inside it again.</summary>
    public static void Replay(Control root)
    {
        Run(root);
        foreach (var child in root.GetLogicalDescendants().OfType<Control>())
            Run(child);
    }

    private static void Hook(Control c)
    {
        c.AttachedToVisualTree -= OnAttached;
        if (!GetReveal(c) && GetEffect(c) == EntranceEffect.None) return;
        c.AttachedToVisualTree += OnAttached;
        if (TopLevel.GetTopLevel(c) is not null) Run(c);
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control c) Run(c);
    }

    /// <summary>Runs one entrance on <paramref name="c"/> now, whatever its <c>Effect</c>.</summary>
    public static void Play(Control c, EntranceEffect effect) => Play(c, effect, 0);

    // The same, starting after delay ms: the table's rows arrive one after another.
    internal static void Play(Control c, EntranceEffect effect, double delay)
    {
        switch (effect)
        {
            case EntranceEffect.Rise:
                Play(c, delay, 280, s_ease, opacity: 0, y: 6);
                break;
            case EntranceEffect.Pop:
                Play(c, delay, 220, s_spring, scale: 0.82);
                break;
            case EntranceEffect.Slide:
                Play(c, delay, 280, s_ease, opacity: 0, x: -10);
                break;
        }
    }

    private static void Run(Control c)
    {
        if (GetReveal(c))
        {
            Play(c, 0, 400, s_ease, opacity: 0);
            var step = GetRevealStep(c).TotalMilliseconds;
            var count = GetRevealCount(c);
            if (c is Panel panel)
                for (var i = 0; i < panel.Children.Count; i++)
                    Play(panel.Children[i], i < count ? i * step : 0, 400, s_ease, y: 12);
        }
        Play(c, GetEffect(c));
    }

    // RTL mirrors the layout, so a negative x is the leading edge in both directions.
    private static void Play(Control c, double delay, double duration, Easing easing, double opacity = 1, double x = 0, double y = 0, double scale = 1)
    {
        if (s_running.TryGetValue(c, out var old))
        {
            old.Stop();
            s_active.Remove(old);
            s_running.Remove(c);
        }
        if (Motion.IsStill(c)) return;
        var motion = new Arrival(c, s_clock.Elapsed.TotalMilliseconds, delay, duration, easing, opacity, x, y, scale);
        s_running.AddOrUpdate(c, motion);
        s_active.Add(motion);
        motion.Step(s_clock.Elapsed.TotalMilliseconds);
        RequestFrame(c);
    }

    // Frames come from the element's top level, so the motion runs with the renderer and stops with it.
    private static void RequestFrame(Control c)
    {
        if (TopLevel.GetTopLevel(c) is not { } top || !s_frames.Add(top)) return;
        top.RequestAnimationFrame(_ =>
        {
            s_frames.Remove(top);
            Tick();
        });
    }

    private static void Tick()
    {
        var now = s_clock.Elapsed.TotalMilliseconds;
        for (var i = s_active.Count - 1; i >= 0; i--)
        {
            var motion = s_active[i];
            if (motion.Step(now)) continue;
            s_active.RemoveAt(i);
            s_running.Remove(motion.Target);
        }
        foreach (var motion in s_active)
            RequestFrame(motion.Target);
    }
}
