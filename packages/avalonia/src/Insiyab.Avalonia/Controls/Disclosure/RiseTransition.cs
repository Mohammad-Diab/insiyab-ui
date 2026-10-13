using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media.Transformation;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>A page transition for a newly shown panel: the old one is gone at once and the new one rises a few pixels as it fades in, the web's <c>ins-rise-sm</c> on <c>.ins-tabpanel.is-active</c>.</summary>
public class RiseTransition : IPageTransition
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(280);

    public double Distance { get; set; } = 6;

    public Easing Easing { get; set; } = new SplineEasing(0.22, 1, 0.36, 1);

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (from is not null) from.IsVisible = false;
        if (to is null) return;
        to.IsVisible = true;
        TransformOperationsAnimator.Ensure();
        var animation = new Animation
        {
            Duration = Duration,
            Easing = Easing,
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 0d), new Setter(Visual.RenderTransformProperty, TransformOperations.Parse(FormattableString.Invariant($"translateY({Distance}px)"))) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 1d), new Setter(Visual.RenderTransformProperty, TransformOperations.Identity) } },
            },
        };
        await animation.RunAsync(to, cancellationToken);
    }
}
