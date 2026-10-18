using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The menu's entrance, set from a theme: the body fades and settles from 6px above (or below, flipped), then its first items rise in one after another.</summary>
internal static class PopMotion
{
    /// <summary>Plays the entrance each time the element is shown.</summary>
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Enabled", typeof(PopMotion));

    /// <summary>Whether the body arrives from above (it opened downward) rather than from below.</summary>
    public static readonly AttachedProperty<bool> FromAboveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FromAbove", typeof(PopMotion), true);

    /// <summary>Whether the first items rise in after the body, as a menu's do.</summary>
    public static readonly AttachedProperty<bool> StaggerProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Stagger", typeof(PopMotion));

    public static bool GetEnabled(Control c) => c.GetValue(EnabledProperty);
    public static void SetEnabled(Control c, bool value) => c.SetValue(EnabledProperty, value);
    public static bool GetFromAbove(Control c) => c.GetValue(FromAboveProperty);
    public static void SetFromAbove(Control c, bool value) => c.SetValue(FromAboveProperty, value);
    public static bool GetStagger(Control c) => c.GetValue(StaggerProperty);
    public static void SetStagger(Control c, bool value) => c.SetValue(StaggerProperty, value);

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
        if (sender is Control c) Play(c);
    }

    private static T Res<T>(Control c, string key, T fallback) =>
        c.TryFindResource(key, c.ActualThemeVariant, out var v) && v is T t ? t : fallback;

    /// <summary>Plays the entrance now.</summary>
    public static void Play(Control body)
    {
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
            if (i > 4) break;
            Run(child, pop, ease, TimeSpan.FromMilliseconds(30 * i), "translateY(6px)");
            i++;
        }
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
