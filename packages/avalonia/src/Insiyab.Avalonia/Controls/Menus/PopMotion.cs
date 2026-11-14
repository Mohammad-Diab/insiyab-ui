using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The menu's entrance, set from a theme: the body fades and settles from 6px above (or below, flipped), then the items in sight rise in one after another (rules stay put). Skipped while <see cref="Motion.IsStill"/>: the body and its items are simply there.</summary>
internal static class PopMotion
{
    /// <summary>Plays the entrance each time the element is shown.</summary>
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Enabled", typeof(PopMotion));

    /// <summary>Whether the body arrives from above (it opened downward) rather than from below.</summary>
    public static readonly AttachedProperty<bool> FromAboveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FromAbove", typeof(PopMotion), true);

    /// <summary>Whether the items in sight rise in after the body, as a menu's do.</summary>
    public static readonly AttachedProperty<bool> StaggerProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Stagger", typeof(PopMotion));

    /// <summary>Set by a pop that places itself: its entrance waits for the placement, which knows the side, instead of playing on attach.</summary>
    public static readonly AttachedProperty<bool> HeldProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Held", typeof(PopMotion));

    public static bool GetEnabled(Control c) => c.GetValue(EnabledProperty);
    public static void SetEnabled(Control c, bool value) => c.SetValue(EnabledProperty, value);
    public static bool GetFromAbove(Control c) => c.GetValue(FromAboveProperty);
    public static void SetFromAbove(Control c, bool value) => c.SetValue(FromAboveProperty, value);
    public static bool GetStagger(Control c) => c.GetValue(StaggerProperty);
    public static void SetStagger(Control c, bool value) => c.SetValue(StaggerProperty, value);
    public static bool GetHeld(Control c) => c.GetValue(HeldProperty);
    public static void SetHeld(Control c, bool value) => c.SetValue(HeldProperty, value);

    static PopMotion()
    {
        EnabledProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            c.AttachedToVisualTree -= OnAttached;
            if (e.GetNewValue<bool>()) c.AttachedToVisualTree += OnAttached;
        });
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control c && !GetHeld(c)) Play(c);
    }

    private static T Res<T>(Control c, string key, T fallback) =>
        c.TryFindResource(key, c.ActualThemeVariant, out var v) && v is T t ? t : fallback;

    /// <summary>Plays the entrance now, or nothing while the window is still.</summary>
    public static void Play(Control body)
    {
        if (Motion.IsStill(body)) return;
        var enter = Res(body, "InsTEnter", TimeSpan.FromMilliseconds(180));
        var pop = Res(body, "InsTPop", TimeSpan.FromMilliseconds(120));
        var ease = Res<Easing>(body, "InsEase", new CubicEaseOut());
        var dy = GetFromAbove(body) ? -6 : 6;
        Run(body, enter, ease, TimeSpan.Zero, $"translateY({dy}px) scale(0.97)");
        if (!GetStagger(body)) return;
        var panel = body.GetVisualDescendants().OfType<ItemsPresenter>().FirstOrDefault()?.Panel;
        if (panel is null) return;
        var i = 0;
        foreach (var child in panel.Children)
        {
            // every item in sight rises, one step after another up to the sixth, the rest with it; rules stay put and out-of-sight items are just there
            if (!child.IsVisible || child is Separator || !InSight(body, child)) continue;
            Run(child, pop, ease, TimeSpan.FromMilliseconds(30 * Math.Min(i, MaxStep)), "translateY(6px)");
            i++;
        }
    }

    private const int MaxStep = 5;

    // Whether any of the item shows inside the menu's body; before the first layout everything counts.
    private static bool InSight(Control body, Control item)
    {
        if (item.Bounds.Height <= 0 || body.Bounds.Height <= 0) return true;
        var top = item.TranslatePoint(default, body);
        return top is not { } p || (p.Y < body.Bounds.Height && p.Y + item.Bounds.Height > 0);
    }

    private static void Run(Visual target, TimeSpan duration, Easing ease, TimeSpan delay, string from)
    {
        TransformOperationsAnimator.Ensure();
        var start = TransformOperations.Parse(from);
        var animation = new Animation
        {
            Duration = duration,
            Delay = delay,
            Easing = ease,
            FillMode = FillMode.Backward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 0d), new Setter(Visual.RenderTransformProperty, start) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 1d), new Setter(Visual.RenderTransformProperty, TransformOperations.Identity) } },
            },
        };
        _ = animation.RunAsync(target, CancellationToken.None);
    }
}
