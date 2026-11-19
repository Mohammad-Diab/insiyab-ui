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
        surface.ClearValue(Visual.RenderTransformOriginProperty);
        if (card.GrowsFromCorner && !card.GrowsFromTarget)
        {
            Dispatcher.UIThread.Post(() => FromCorner(surface, target), DispatcherPriority.Loaded);
            return true;
        }
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
            || (IsCorner ? Corner(surface, target) is null : AnchorGrow.From(surface, target) is null))
        {
            Reset();
            return false;
        }
        _shrinking = true;
        if (IsCorner)
        {
            var back = CornerExit(target);
            surface.RenderTransformOrigin = Corner(surface, target)!.Value;
            Move(surface, CornerStart, 0, back, target, back);
            _landing = DispatcherTimer.RunOnce(Land, back + TimeSpan.FromMilliseconds(20));
            return true;
        }
        var to = AnchorGrow.From(surface, target)!;
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

    // ---- from the corner (PopCard.GrowsFromCorner): Material's menu, scaled from 0.8 about the corner nearest the trigger, fading in ----

    /// <summary>The scale a corner-grown card starts from and shrinks back to (Material's menus).</summary>
    internal const double CornerScale = 0.8;

    private static readonly TransformOperations CornerStart = TransformOperations.Parse(FormattableString.Invariant($"scale({CornerScale})"));

    private bool IsCorner => card.GrowsFromCorner && !card.GrowsFromTarget;

    /// <summary>The point the last corner opening grew from, in the card's own space, for tests.</summary>
    internal Point? CornerAt { get; private set; }

    // Laid out but unseen: it starts at 0.8 about its corner by the trigger once a frame has shown that start, so a slow first frame on a weak phone does not eat the motion.
    private void FromCorner(Control surface, Control target)
    {
        if (!card.IsOpen || _shrinking || surface != _surface) return;
        if (Corner(surface, target) is not { } origin || TopLevel.GetTopLevel(surface) is not { } top)
        {
            surface.Opacity = 1;
            return;
        }
        CornerAt = origin.Point;
        surface.RenderTransformOrigin = origin;
        surface.RenderTransform = CornerStart;
        Start = CornerStart;
        top.RequestAnimationFrame(_ =>
        {
            if (!card.IsOpen || _shrinking || surface != _surface) return;
            var open = Motion.Pop(target);
            Move(surface, TransformOperations.Identity, 1, open, target, TimeSpan.FromMilliseconds(open.TotalMilliseconds * 0.5));
        });
    }

    // Closing is the opening backwards, quicker.
    private static TimeSpan CornerExit(Control at) => TimeSpan.FromMilliseconds(Motion.Pop(at).TotalMilliseconds * 0.6);

    // The surface's corner nearest the trigger's centre (the trigger's centre held inside the surface's box), in the surface's own space, so a mirrored layer needs nothing more.
    internal static RelativePoint? Corner(Visual surface, Visual target)
    {
        var w = surface.Bounds.Width;
        var h = surface.Bounds.Height;
        if (w <= 0 || h <= 0 || target.Bounds.Width <= 0) return null;
        var keep = surface.RenderTransform;
        surface.RenderTransform = null;
        var c = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), surface);
        surface.RenderTransform = keep;
        if (c is not { } p) return null;
        return new RelativePoint(Math.Clamp(p.X, 0, w), Math.Clamp(p.Y, 0, h), RelativeUnit.Absolute);
    }

    private static void Move(Control surface, TransformOperations to, double opacity, TimeSpan duration, Control at, TimeSpan? fade = null)
    {
        var ease = Motion.Ease(at);
        surface.Transitions =
        [
            new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = duration, Easing = ease },
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = fade ?? TimeSpan.FromMilliseconds(duration.TotalMilliseconds * 0.6), Easing = ease },
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
