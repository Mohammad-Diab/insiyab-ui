using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>What the shell's containers hand down to its parts: the split view's states, the brand island and the screen's brand block, and whether the brand link is under the pointer.</summary>
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
}
