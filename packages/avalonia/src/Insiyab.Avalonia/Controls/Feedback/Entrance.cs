using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

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
    /// <summary>Its background lights up in the brand's soft tint and fades back over 1.8s, to draw the eye to a row that has just arrived or was brought into view.</summary>
    Flash,
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
    private static readonly Easing s_fade = new SplineEasing(0.4, 0, 0.2, 1);
    private static readonly ConditionalWeakTable<Control, Moving> s_running = new();
    private static readonly ConditionalWeakTable<Control, Moving> s_flashing = new();
    private static readonly List<Moving> s_active = [];
    private static readonly Stopwatch s_clock = Stopwatch.StartNew();
    private static readonly HashSet<TopLevel> s_frames = [];

    // For tests: the milliseconds every entrance is timed by, which a test can hold and step.
    internal static Func<double> Now { get; set; } = () => s_clock.Elapsed.TotalMilliseconds;

    // One running motion, stepped by the shared frame loop until it says it is done.
    private abstract class Moving(Control target)
    {
        public readonly Control Target = target;

        public abstract bool Step(double now, bool frame = true);

        public abstract void Stop();
    }

    // One element's entrance: from-values that ease to rest (opacity 1, no offset, scale 1) after a delay.
    private sealed class Arrival(Control target, double delay, double duration, Easing easing, double opacity, double x, double y, double scale) : Moving(target)
    {
        private IDisposable? _opacity;
        private IDisposable? _transform;

        // Timed from its first frame, as a CSS animation is, so a slow first layout or render never eats the motion.
        private double _start = double.NaN;

        public override bool Step(double now, bool frame = true)
        {
            if (frame && double.IsNaN(_start)) _start = now;
            var t = double.IsNaN(_start) ? -delay : now - _start - delay;
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

        public override void Stop()
        {
            _opacity?.Dispose();
            _transform?.Dispose();
            _opacity = _transform = null;
        }
    }

    // A flash: the tint laid over the element's own background at animation priority, up in 200ms, held, and gone by 1.8s, so its own background (and its hover) comes back untouched.
    private sealed class Glow(Control target, double delay, StyledProperty<IBrush?> background) : Moving(target)
    {
        public const double Rise = 200, Hold = 900, Length = 1800;
        private IDisposable? _set;
        private Color? _tint;
        private double _start = double.NaN;

        public override bool Step(double now, bool frame = true)
        {
            if (frame && double.IsNaN(_start)) _start = now;
            var t = double.IsNaN(_start) ? -delay : now - _start - delay;
            if (t >= Length || Motion.IsStill(Target))
            {
                Stop();
                return false;
            }
            var k = t <= 0 ? 0 : t < Rise ? s_ease.Ease(t / Rise) : t < Hold ? 1 : 1 - s_fade.Ease((t - Hold) / (Length - Hold));
            _set?.Dispose();
            _set = null;
            if (k <= 0 || (_tint ??= Tint()) is not { } tint) return true;
            var own = Target.GetValue(background) as ISolidColorBrush;
            _set = Target.SetValue(background, new ImmutableSolidColorBrush(Mix(own?.Color ?? Colors.Transparent, tint, k)), BindingPriority.Animation);
            return true;
        }

        private Color? Tint() => Target.TryFindResource("InsToneBrandBg", Target.ActualThemeVariant, out var found) && found is ISolidColorBrush b ? b.Color : null;

        public override void Stop()
        {
            _set?.Dispose();
            _set = null;
        }
    }

    // The tint laid over the ground and blended in by k, in premultiplied colour, so a half-way flash never darkens.
    private static Color Mix(Color ground, Color tint, double k)
    {
        double ga = ground.A / 255d, ta = tint.A / 255d;
        double Over(byte t, byte g) => t / 255d * ta + g / 255d * ga * (1 - ta);
        var oa = ta + ga * (1 - ta);
        double Lerp(double from, double to) => from + (to - from) * k;
        var a = Lerp(ga, oa);
        if (a <= 0) return Colors.Transparent;
        byte Channel(byte t, byte g) => (byte)Math.Round(Math.Clamp(Lerp(g / 255d * ga, Over(t, g)) / a, 0, 1) * 255);
        return Color.FromArgb((byte)Math.Round(a * 255), Channel(tint.R, ground.R), Channel(tint.G, ground.G), Channel(tint.B, ground.B));
    }

    // The background a flash lights: the element's own, whatever kind of element it is.
    private static StyledProperty<IBrush?>? Background(Control c) => c switch
    {
        Border => Border.BackgroundProperty,
        Panel => Panel.BackgroundProperty,
        TemplatedControl => TemplatedControl.BackgroundProperty,
        TextBlock => TextBlock.BackgroundProperty,
        ContentPresenter => ContentPresenter.BackgroundProperty,
        _ => null,
    };

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

    /// <summary>Runs one entrance on <paramref name="c"/> now, whatever its <c>Effect</c>; out of sight it waits and runs when the element is first seen.</summary>
    public static void Play(Control c, EntranceEffect effect)
    {
        if (Motion.IsStill(c)) PlayNow(c, effect, 0);
        else Seen.WhenSeen(c, () => PlayNow(c, effect, 0));
    }

    // The same, starting after delay ms: the table's rows arrive one after another, and a row out of view simply appears.
    internal static void Play(Control c, EntranceEffect effect, double delay)
    {
        if (c.IsAttachedToVisualTree() && Seen.Arranged(c) && !Seen.Now(c)) return;
        PlayNow(c, effect, delay);
    }

    private static void PlayNow(Control c, EntranceEffect effect, double delay)
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
            case EntranceEffect.Flash:
                Flash(c, delay);
                break;
        }
    }

    private static void Run(Control c)
    {
        if (!GetReveal(c) && GetEffect(c) == EntranceEffect.None) return;
        if (Motion.IsStill(c)) RunNow(c);
        else Seen.WhenSeen(c, () => RunNow(c));
    }

    private static void RunNow(Control c)
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
        PlayNow(c, GetEffect(c), 0);
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
        var motion = new Arrival(c, delay, duration, easing, opacity, x, y, scale);
        s_running.AddOrUpdate(c, motion);
        s_active.Add(motion);
        motion.Step(Now(), frame: false);
        RequestFrame(c);
    }

    // A flash runs beside a moving entrance on the same element; a second flash starts it again.
    private static void Flash(Control c, double delay)
    {
        if (s_flashing.TryGetValue(c, out var old))
        {
            old.Stop();
            s_active.Remove(old);
            s_flashing.Remove(c);
        }
        if (Motion.IsStill(c) || Background(c) is not { } background) return;
        var glow = new Glow(c, delay, background);
        s_flashing.AddOrUpdate(c, glow);
        s_active.Add(glow);
        glow.Step(Now(), frame: false);
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
        var now = Now();
        for (var i = s_active.Count - 1; i >= 0; i--)
        {
            var motion = s_active[i];
            if (motion.Step(now)) continue;
            s_active.RemoveAt(i);
            if (motion is Glow) s_flashing.Remove(motion.Target);
            else s_running.Remove(motion.Target);
        }
        foreach (var motion in s_active)
            RequestFrame(motion.Target);
    }
}
