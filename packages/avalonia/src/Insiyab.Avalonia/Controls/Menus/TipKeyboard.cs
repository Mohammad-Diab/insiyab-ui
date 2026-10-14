using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>What the web's tooltip does for the keyboard and Avalonia's does not: a control reached by Tab or the arrow keys shows its tip at once, losing focus hides it, and Escape hides it. Registered once, when the theme loads, so the very first tip already answers the keyboard.</summary>
public static class TipKeyboard
{
    /// <summary>Set by the ToolTip theme; reading it is what registers the handlers.</summary>
    public static readonly AttachedProperty<bool> RegisteredProperty =
        AvaloniaProperty.RegisterAttached<ToolTip, bool>("Registered", typeof(TipKeyboard));

    public static bool GetRegistered(ToolTip t) => t.GetValue(RegisteredProperty);
    public static void SetRegistered(ToolTip t, bool value) => t.SetValue(RegisteredProperty, value);

    static TipKeyboard()
    {
        InputElement.GotFocusEvent.AddClassHandler<Control>(OnGotFocus);
        InputElement.LostFocusEvent.AddClassHandler<Control>(OnLostFocus);
        InputElement.KeyDownEvent.AddClassHandler<Control>(OnKeyDown, RoutingStrategies.Tunnel);
    }

    // Touching the class runs its static constructor, which adds the handlers.
    internal static void Register() { }

    private static void OnGotFocus(Control c, FocusChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, c) || ToolTip.GetTip(c) is null) return;
        if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
            ToolTip.SetIsOpen(c, true);
    }

    private static void OnLostFocus(Control c, FocusChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, c) && ToolTip.GetIsOpen(c)) ToolTip.SetIsOpen(c, false);
    }

    private static void OnKeyDown(Control c, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !ToolTip.GetIsOpen(c) || !ReferenceEquals(e.Source, c)) return;
        ToolTip.SetIsOpen(c, false);
        e.Handled = true;
    }
}
