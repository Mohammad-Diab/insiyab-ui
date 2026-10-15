using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>A window that is not the active one looks it, as on Windows and macOS: the selected rows of tables, trees, lists, the sidebar and the palette turn grey, the title dims and the focus ring hides, at once and back at once; a single view never deactivates.</summary>
public static class WindowActivity
{
    /// <summary>True on everything inside a window that is not the active one, inherited from the window; an app's own selected rows can follow it with <c>[(ins|WindowActivity.IsInactive)=True]</c>.</summary>
    public static readonly AttachedProperty<bool> IsInactiveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsInactive", typeof(WindowActivity), inherits: true);

    // The control the keyboard reached last while the window was active, kept only while its ring showed.
    private static readonly AttachedProperty<WeakReference<Control>?> RingProperty =
        AvaloniaProperty.RegisterAttached<Window, WeakReference<Control>?>("Ring", typeof(WindowActivity));

    private static bool _registered;

    public static bool GetIsInactive(Control control) => control.GetValue(IsInactiveProperty);

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        WindowBase.IsActiveProperty.Changed.AddClassHandler<Window>((window, e) => Update(window, e.GetNewValue<bool>()));
        InputElement.GotFocusEvent.AddClassHandler<Window>(Remember, handledEventsToo: true);
    }

    // Set only while inactive and cleared after, so an active window carries no inherited value at all.
    private static void Update(Window window, bool active)
    {
        if (!active)
        {
            window.SetValue(IsInactiveProperty, true);
            return;
        }
        window.ClearValue(IsInactiveProperty);
        Restore(window);
    }

    private static void Remember(Window window, FocusChangedEventArgs e)
    {
        if (!window.IsActive || e.Source is not Control control) return;
        window.SetValue(RingProperty, control.Classes.Contains(":focus-visible") ? new WeakReference<Control>(control) : null);
    }

    // Avalonia gives the focus back without its ring, so the ring the keyboard left comes back with the window, raising no second GotFocus.
    private static void Restore(Window window)
    {
        if (window.GetValue(RingProperty) is not { } kept || !kept.TryGetTarget(out var control)) return;
        if (!ReferenceEquals(window.FocusManager?.GetFocusedElement(), control) || control.Classes.Contains(":focus-visible")) return;
        ((IPseudoClasses)control.Classes).Set(":focus-visible", true);
        var layer = AdornerLayer.GetAdornerLayer(control);
        var template = control.IsSet(Control.FocusAdornerProperty) ? control.FocusAdorner : layer?.DefaultFocusAdorner;
        if (layer is null || template?.Build() is not { } ring) return;
        AdornerLayer.SetAdornedElement(ring, control);
        layer.Children.Add(ring);
        EventHandler<FocusChangedEventArgs>? drop = null;
        drop = (_, _) =>
        {
            control.RemoveHandler(InputElement.LostFocusEvent, drop!);
            layer.Children.Remove(ring);
        };
        control.AddHandler(InputElement.LostFocusEvent, drop);
    }
}
