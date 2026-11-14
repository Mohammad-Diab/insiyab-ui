using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

// Where overlays live: the anchor's own TopLevel (never a popup's), its adorner layer, and its width for the phone rules.
internal static class OverlayHost
{
    public const double PhoneWidth = 544;

    public static TopLevel? Top(Visual? visual)
    {
        var top = visual is null ? null : TopLevel.GetTopLevel(visual);
        while (top is PopupRoot popup && popup.ParentTopLevel is { } parent) top = parent;
        return top;
    }

    public static AdornerLayer? Layer(TopLevel top)
    {
        var manager = top.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
        return manager is null ? null : AdornerLayer.GetAdornerLayer(manager.Child ?? (Visual)manager);
    }

    public static double Width(Visual visual)
    {
        var top = Top(visual);
        if (top is null) return 0;
        return top.Bounds.Width > 0 ? top.Bounds.Width : top.ClientSize.Width;
    }

    public static double Height(Visual visual)
    {
        var top = Top(visual);
        if (top is null) return 0;
        return top.Bounds.Height > 0 ? top.Bounds.Height : top.ClientSize.Height;
    }

    public static void SetClass(Classes classes, string name, bool on)
    {
        if (on)
        {
            if (!classes.Contains(name)) classes.Add(name);
        }
        else
        {
            classes.Remove(name);
        }
    }

    // The height of the bars that run along the TopLevel's foot, a status bar or a bottom navigation bar; a bottom bar's ground under the gesture bar is counted with it.
    public static double FootRoom(TopLevel top)
    {
        var room = 0d;
        var height = top.Bounds.Height;
        foreach (var bar in top.GetVisualDescendants().OfType<Control>().Where(c => c is StatusBar or BottomNav))
        {
            if (!bar.IsEffectivelyVisible || bar.TranslatePoint(default, top) is not { } at) continue;
            if (height - (at.Y + bar.Bounds.Height) <= 1) room = Math.Max(room, height - at.Y);
        }
        return room;
    }

    public static bool IsPhone(Visual visual)
    {
        var w = Width(visual);
        return w > 0 && w <= PhoneWidth;
    }
}
