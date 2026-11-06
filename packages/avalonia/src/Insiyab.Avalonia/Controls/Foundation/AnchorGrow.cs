using Avalonia;
using Avalonia.Media.Transformation;

namespace Insiyab.Avalonia.Controls;

/// <summary>The transform that lays a surface over the control it grows out of, for a dialog or a card that opens from its anchor and shrinks back into it.</summary>
internal static class AnchorGrow
{
    // About the surface's centre (its default RenderTransformOrigin), in the surface's own space, so a mirrored layer needs nothing more; Avalonia applies the operations in the order written.
    public static TransformOperations? From(Visual surface, Visual origin)
    {
        var w = surface.Bounds.Width;
        var h = surface.Bounds.Height;
        if (w <= 0 || h <= 0 || origin.Bounds.Width <= 0 || origin.Bounds.Height <= 0) return null;
        var keep = surface.RenderTransform;
        surface.RenderTransform = null;
        var p1 = origin.TranslatePoint(default, surface);
        var p2 = origin.TranslatePoint(new Point(origin.Bounds.Width, origin.Bounds.Height), surface);
        surface.RenderTransform = keep;
        if (p1 is not { } a1 || p2 is not { } a2) return null;
        var anchor = new Rect(new Point(Math.Min(a1.X, a2.X), Math.Min(a1.Y, a2.Y)), new Point(Math.Max(a1.X, a2.X), Math.Max(a1.Y, a2.Y)));
        var sx = Math.Max(0.02, anchor.Width / w);
        var sy = Math.Max(0.02, anchor.Height / h);
        var dx = anchor.Center.X - w / 2;
        var dy = anchor.Center.Y - h / 2;
        return TransformOperations.Parse(FormattableString.Invariant($"scale({sx:0.####}, {sy:0.####}) translate({dx:0.###}px, {dy:0.###}px)"));
    }
}
