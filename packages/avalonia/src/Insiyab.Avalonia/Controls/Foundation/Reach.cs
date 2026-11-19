using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A part whose hit test reaches past its edges under a finger (<see cref="Reach.Claims"/>).</summary>
internal interface IReach
{
}

/// <summary>A small control's reach under a finger: while its TopLevel is in touch, a press in the box <see cref="Touch.Target"/> wide and tall around it is its own, unless another control is there or a nearer reach claims it; a mouse finds it exactly as drawn, and nothing moves.</summary>
internal static class Reach
{
    private enum Mode
    {
        Normal,
        Off,
        Raw,
    }

    [ThreadStatic] private static Mode t_mode;
    [ThreadStatic] private static Visual? t_skip;

    /// <summary>Whether a point outside the part (in its own coordinates) is the part's under a finger; a grip keeps its reach over the controls beside it.</summary>
    public static bool Claims(Visual part, Point p, bool grip = false)
    {
        if (t_mode == Mode.Off || part == t_skip) return false;
        var size = part.Bounds.Size;
        var target = Math.Max(Touch.Target, TouchTarget.GetSize(part));
        var dx = Math.Max(0, (target - size.Width) / 2);
        var dy = Math.Max(0, (target - size.Height) / 2);
        if (dx == 0 && dy == 0 || !new Rect(size).Inflate(new Thickness(dx, dy)).Contains(p) || !Touch.IsTouch(part)) return false;
        if (t_mode == Mode.Raw) return true;
        if (TopLevel.GetTopLevel(part) is not { } top || part.TranslatePoint(p, top) is not { } at) return false;
        try
        {
            t_mode = Mode.Off;
            if (!grip && top.InputHitTest(at) is Visual real && Blocks(real, part)) return false;
            t_mode = Mode.Raw;
            t_skip = part;
            return !(top.InputHitTest(at) is Visual other && other is IReach && Distance(other, top, at) < Distance(part, top, at));
        }
        finally
        {
            t_mode = Mode.Normal;
            t_skip = null;
        }
    }

    // Another control under the point keeps it, unless it is one the part sits in (a row, a card, a chip around its remove button).
    private static bool Blocks(Visual real, Visual part)
    {
        for (var v = real; v is not null && v is not TopLevel; v = v.GetVisualParent())
        {
            if (v == part || v.IsVisualAncestorOf(part)) return false;
            if (IsControl(v)) return true;
        }
        return false;
    }

    private static bool IsControl(Visual v) =>
        v is Button or Thumb or RangeBase or TextBox or ListBoxItem or ComboBox or IReach
        || v is InputElement { Focusable: true } and not (ScrollViewer or ContentPresenter);

    private static double Distance(Visual v, TopLevel top, Point at)
    {
        if (v.TransformToVisual(top) is not { } m) return double.MaxValue;
        var r = new Rect(v.Bounds.Size).TransformToAABB(m);
        var dx = Math.Max(Math.Max(r.Left - at.X, 0), at.X - r.Right);
        var dy = Math.Max(Math.Max(r.Top - at.Y, 0), at.Y - r.Bottom);
        return dx * dx + dy * dy;
    }

    /// <summary>A border's own hit test inside its box: where it draws a background or an edge.</summary>
    internal static bool Inside(Border b) => b.Background is not null || b.BorderBrush is not null && b.BorderThickness != default;
}

/// <summary>A template's root panel with a finger's reach: a button's press panel, a chip's frame. Styles still name it <c>Panel</c>.</summary>
internal sealed class ReachPanel : Panel, ICustomHitTest, IReach
{
    protected override Type StyleKeyOverride => typeof(Panel);

    // inside its box with no background (a choice whose face is a child) the reach still answers, so the edge a child's box leaves out is never a gap
    public bool HitTest(Point point) => Background is not null && new Rect(Bounds.Size).Contains(point) || Reach.Claims(this, point);
}

/// <summary>A template border with a finger's reach: a grip, a small round button. Styles name it <c>:is(Border)</c>.</summary>
internal sealed class ReachBorder : TemplateBorder, ICustomHitTest, IReach
{
    /// <summary>A drag grip: its reach also covers the controls beside it.</summary>
    public bool IsGrip { get; set; }

    public bool HitTest(Point point) => Reach.Inside(this) && new Rect(Bounds.Size).Contains(point) || Reach.Claims(this, point, IsGrip);
}
