using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A <see cref="PopCard.GrowsFromTarget"/> card's motion: it grows out of its trigger's box into its place, and shrinks back into it before it closes.</summary>
internal sealed class PopGrow(PopCard card)
{
    private Control? _surface;
    private bool _shrinking;
    private bool _letGo;
    private IDisposable? _landing;

    /// <summary>The transform the last opening started from, for tests: it lays the card over its trigger.</summary>
    internal TransformOperations? Start { get; private set; }

    /// <summary>The card's moving part: the panel inside its shadow room, holding the card and its ground.</summary>
    internal Control? Surface => _surface;

    internal bool IsShrinking => _shrinking;

    // Its size and place are known only after a layout: the card is held unseen, then grows out of its trigger.
    public bool Open(Control body)
    {
        _letGo = false;
        Stop();
        if (card.Target is not Control target || Motion.IsStill(target) || body.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "PART_Room")?.Child is not Control surface) return false;
        _surface = surface;
        Start = null;
        surface.Transitions = null;
        surface.RenderTransform = TransformOperations.Identity;
        surface.Opacity = 0;
        Dispatcher.UIThread.Post(() =>
        {
            if (!card.IsOpen || _shrinking || surface != _surface) return;
            Unclip(surface);
            if (AnchorGrow.From(surface, target) is not { } from)
            {
                surface.Opacity = 1;
                PopMotion.Play(body);
                return;
            }
            Start = from;
            surface.RenderTransform = from;
            Move(surface, TransformOperations.Identity, 1, Motion.Enter(target), target);
        }, DispatcherPriority.Loaded);
        return true;
    }

    // Returns whether the card shrinks first, in which case it closes itself when it lands; a close asked for meanwhile waits for it.
    public bool Shrink()
    {
        if (_letGo)
        {
            _letGo = false;
            Reset();
            return false;
        }
        if (_shrinking) return true;
        if (!card.IsOpen || _surface is not { } surface || card.Target is not Control target || !target.IsEffectivelyVisible || Motion.IsStill(target)
            || AnchorGrow.From(surface, target) is not { } to)
        {
            Reset();
            return false;
        }
        _shrinking = true;
        var exit = Motion.Exit(target);
        Move(surface, to, 0, exit, target);
        _landing = DispatcherTimer.RunOnce(Land, exit + TimeSpan.FromMilliseconds(20));
        return true;
    }

    /// <summary>Cuts a shrink short and lets the next close through at once.</summary>
    public void Stop()
    {
        _landing?.Dispose();
        _landing = null;
        if (_shrinking) Reset();
        _shrinking = false;
    }

    /// <summary>The next close goes through without shrinking.</summary>
    public void LetGo() => _letGo = card.IsOpen;

    private void Land()
    {
        _landing = null;
        if (!_shrinking) return;
        _shrinking = false;
        _letGo = true;
        card.Hide();
        _letGo = false;
    }

    // The card at rest again, for an opening that does not grow.
    private void Reset()
    {
        if (_surface is not { } surface) return;
        surface.Transitions = null;
        surface.RenderTransform = TransformOperations.Identity;
        surface.Opacity = 1;
    }

    private static void Move(Control surface, TransformOperations to, double opacity, TimeSpan duration, Control at)
    {
        var ease = Motion.Ease(at);
        surface.Transitions =
        [
            new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = duration, Easing = ease },
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * 0.6), Easing = ease },
        ];
        surface.RenderTransform = to;
        surface.Opacity = opacity;
    }

    // A popup clips to its own box, but the card starts over its trigger, which can lie outside it.
    private static void Unclip(Visual surface)
    {
        if (surface.FindAncestorOfType<OverlayPopupHost>() is null) return;
        foreach (var v in surface.GetVisualAncestors())
        {
            if (v.ClipToBounds) v.ClipToBounds = false;
            if (v is OverlayPopupHost) break;
        }
    }
}
