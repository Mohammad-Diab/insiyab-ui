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

    public static bool IsPhone(Visual visual)
    {
        var w = Width(visual);
        return w > 0 && w <= PhoneWidth;
    }
}
