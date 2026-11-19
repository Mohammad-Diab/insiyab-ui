using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A page transition for a newly shown panel: the old one is gone at once and the new one rises a few pixels as it fades in, the web's <c>ins-rise-sm</c> on <c>.ins-tabpanel.is-active</c>.</summary>
internal class RiseTransition : IPageTransition
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(280);

    public double Distance { get; set; } = 6;

    public Easing Easing { get; set; } = new SplineEasing(0.22, 1, 0.36, 1);

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (from is not null) from.IsVisible = false;
        if (to is null) return;
        to.IsVisible = true;
        if (Motion.IsStill(to) || (to.GetVisualParent() is { } host && !Seen.Now(host))) return;
        TransformOperationsAnimator.Ensure();
        var start = TransformOperations.Parse(FormattableString.Invariant($"translateY({Distance}px)"));
        var animation = new Animation
        {
            Duration = Duration,
            Easing = Easing,
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 0d), new Setter(Visual.RenderTransformProperty, start) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 1d), new Setter(Visual.RenderTransformProperty, TransformOperations.Identity) } },
            },
        };
        // The host starts this in the frame the new page is built in, and that frame's clock was read before the work: a slow-building page had its whole rise counted away unseen, so it waits for the next frame and runs from there.
        if (TopLevel.GetTopLevel(to) is not { } top)
        {
            await animation.RunAsync(to, cancellationToken);
            return;
        }
        var opacity = to.SetValue(Visual.OpacityProperty, 0d, BindingPriority.Animation);
        var transform = to.SetValue(Visual.RenderTransformProperty, start, BindingPriority.Animation);
        var done = new TaskCompletionSource();
        var waiting = true;
        void Release()
        {
            waiting = false;
            opacity?.Dispose();
            transform?.Dispose();
        }
        using var cancel = cancellationToken.Register(() =>
        {
            if (!waiting) return;
            Release();
            done.TrySetResult();
        });
        top.RequestAnimationFrame(_ =>
        {
            if (!waiting) return;
            Release();
            // started inside the frame's own pulse, so its first values are set before this frame is drawn: no frame shows the page at rest in between
            animation.RunAsync(to, cancellationToken).ContinueWith(_ => done.TrySetResult(), TaskContinuationOptions.ExecuteSynchronously);
        });
        await done.Task;
    }
}
