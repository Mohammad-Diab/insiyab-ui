using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a list row hands down to its parts: whether the row is selected and whether its list is display-only.</summary>
internal static class ListLook
{
    /// <summary>Set by a selected row: its title takes the brand colour.</summary>
    public static readonly AttachedProperty<bool> SelectedProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Selected", typeof(ListLook), inherits: true);

    /// <summary>Set by a display-only list: its rows keep the plain title colour even when selected.</summary>
    public static readonly AttachedProperty<bool> StaticProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Static", typeof(ListLook), inherits: true);

    public static bool GetSelected(StyledElement element) => element.GetValue(SelectedProperty);
    public static void SetSelected(StyledElement element, bool value) => element.SetValue(SelectedProperty, value);
    public static bool GetStatic(StyledElement element) => element.GetValue(StaticProperty);
    public static void SetStatic(StyledElement element, bool value) => element.SetValue(StaticProperty, value);
}
