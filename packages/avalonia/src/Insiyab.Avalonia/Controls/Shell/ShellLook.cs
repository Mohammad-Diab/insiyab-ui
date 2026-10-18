using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Insiyab.Avalonia.Controls;

/// <summary>What the shell's containers hand down to its parts: the split view's states, the brand island and the screen's brand block, whether the brand link is under the pointer, and what its themes switch on.</summary>
internal static class ShellLook
{
    /// <summary>Set by a split view: its content and pane parts take their split-view insets.</summary>
    public static readonly AttachedProperty<bool> InSplitViewProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("InSplitView", typeof(ShellLook), inherits: true);

    /// <summary>Set by a split view whose pane is closed.</summary>
    public static readonly AttachedProperty<bool> ClosedProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Closed", typeof(ShellLook), inherits: true);

    /// <summary>Set by a split view that is a drawer.</summary>
    public static readonly AttachedProperty<bool> OverlayProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Overlay", typeof(ShellLook), inherits: true);

    /// <summary>Set by a split view at phone width: the bar tightens and the brand goes.</summary>
    public static readonly AttachedProperty<bool> NarrowProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Narrow", typeof(ShellLook), inherits: true);

    /// <summary>Set by a split view at the narrowest width: the line under the title goes.</summary>
    public static readonly AttachedProperty<bool> XNarrowProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("XNarrow", typeof(ShellLook), inherits: true);

    /// <summary>Set by the brand island of the top bar: its mark is the small one and its row runs horizontally.</summary>
    public static readonly AttachedProperty<bool> InBrandProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("InBrand", typeof(ShellLook), inherits: true);

    /// <summary>Set by the brand block of a screen: its mark is the large one.</summary>
    public static readonly AttachedProperty<bool> InScreenBrandProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("InScreenBrand", typeof(ShellLook), inherits: true);

    /// <summary>Set by a brand link under the pointer: its mark tilts.</summary>
    public static readonly AttachedProperty<bool> HotProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Hot", typeof(ShellLook), inherits: true);

    /// <summary>On a ListBox whose template has a <c>Canvas#PART_Flight</c>: a change of selection flies the marker from the old item to the new one. Set by the <c>shell-nav</c> theme.</summary>
    public static readonly AttachedProperty<bool> FlyMarkerProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("FlyMarker", typeof(ShellLook));

    /// <summary>On a title island: it fades and settles in when it arrives and again each time its <c>TextBlock.shell-title</c> or its content changes (the web's <c>ins-island-in</c> on every page). Set by the topbar theme.</summary>
    public static readonly AttachedProperty<bool> EntersProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Enters", typeof(ShellLook));

    /// <summary>Turns a SplitView's OpenPaneLength into the drawer's closed offset, a little past its own width.</summary>
    public static IValueConverter DrawerOffset { get; } = new FuncValueConverter<double, ITransform>(len =>
        TransformOperations.Parse(string.Create(CultureInfo.InvariantCulture, $"translateX({-(len + 16)}px)")));

    /// <summary>The same offset for a pane placed on the end side, which slides out the other way.</summary>
    public static IValueConverter DrawerOffsetEnd { get; } = new FuncValueConverter<double, ITransform>(len =>
        TransformOperations.Parse(string.Create(CultureInfo.InvariantCulture, $"translateX({len + 16}px)")));

    static ShellLook()
    {
        FlyMarkerProperty.Changed.AddClassHandler<ListBox>(Shell.FlyMarkerChanged);
        EntersProperty.Changed.AddClassHandler<Control>(Shell.EntersChanged);
    }

    public static bool GetInSplitView(StyledElement element) => element.GetValue(InSplitViewProperty);
    public static void SetInSplitView(StyledElement element, bool value) => element.SetValue(InSplitViewProperty, value);
    public static bool GetClosed(StyledElement element) => element.GetValue(ClosedProperty);
    public static void SetClosed(StyledElement element, bool value) => element.SetValue(ClosedProperty, value);
    public static bool GetOverlay(StyledElement element) => element.GetValue(OverlayProperty);
    public static void SetOverlay(StyledElement element, bool value) => element.SetValue(OverlayProperty, value);
    public static bool GetNarrow(StyledElement element) => element.GetValue(NarrowProperty);
    public static void SetNarrow(StyledElement element, bool value) => element.SetValue(NarrowProperty, value);
    public static bool GetXNarrow(StyledElement element) => element.GetValue(XNarrowProperty);
    public static void SetXNarrow(StyledElement element, bool value) => element.SetValue(XNarrowProperty, value);
    public static bool GetInBrand(StyledElement element) => element.GetValue(InBrandProperty);
    public static void SetInBrand(StyledElement element, bool value) => element.SetValue(InBrandProperty, value);
    public static bool GetInScreenBrand(StyledElement element) => element.GetValue(InScreenBrandProperty);
    public static void SetInScreenBrand(StyledElement element, bool value) => element.SetValue(InScreenBrandProperty, value);
    public static bool GetHot(StyledElement element) => element.GetValue(HotProperty);
    public static void SetHot(StyledElement element, bool value) => element.SetValue(HotProperty, value);
    public static bool GetFlyMarker(ListBox list) => list.GetValue(FlyMarkerProperty);
    public static void SetFlyMarker(ListBox list, bool value) => list.SetValue(FlyMarkerProperty, value);
    public static bool GetEnters(Control control) => control.GetValue(EntersProperty);
    public static void SetEnters(Control control, bool value) => control.SetValue(EntersProperty, value);
}
