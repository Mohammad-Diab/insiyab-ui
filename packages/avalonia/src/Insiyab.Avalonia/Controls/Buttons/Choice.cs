using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>Options for checks, radios, switches and tiles: a description line under the label, and a radio a second click clears (the web's <c>data-ins-deselect</c>).</summary>
public static class Choice
{
    /// <summary>A short line under the label of a <see cref="ToggleSwitch"/> or a tile (<c>Classes="tile"</c>): the web's <c>ins-switch-desc</c> and <c>ins-tile-desc</c>.</summary>
    public static readonly AttachedProperty<object?> DescriptionProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>("Description", typeof(Choice));

    /// <summary>On a radio button or any element holding one: clicking the chosen radio again (or pressing Space on it) clears it.</summary>
    public static readonly AttachedProperty<bool> DeselectProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Deselect", typeof(Choice), inherits: true);

    private static readonly AttachedProperty<bool> WasCheckedProperty =
        AvaloniaProperty.RegisterAttached<RadioButton, bool>("WasChecked", typeof(Choice));

    static Choice()
    {
        InputElement.PointerPressedEvent.AddClassHandler<RadioButton>((r, _) => Remember(r), RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<RadioButton>((r, e) => { if (e.Key == Key.Space) Remember(r); }, RoutingStrategies.Tunnel, handledEventsToo: true);
        Button.ClickEvent.AddClassHandler<RadioButton>((r, _) =>
        {
            if (r.GetValue(WasCheckedProperty) && GetDeselect(r)) r.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
            r.SetValue(WasCheckedProperty, false);
        });
    }

    private static void Remember(RadioButton radio) => radio.SetValue(WasCheckedProperty, radio.IsChecked == true);

    public static object? GetDescription(Control element) => element.GetValue(DescriptionProperty);
    public static void SetDescription(Control element, object? value) => element.SetValue(DescriptionProperty, value);

    public static bool GetDeselect(Control element) => element.GetValue(DeselectProperty);
    public static void SetDeselect(Control element, bool value) => element.SetValue(DeselectProperty, value);
}
