using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>Set by a card that is itself a button: the icons inside it keep still under the pointer, as the card moves as a whole.</summary>
internal static class NudgeLook
{
    public static readonly AttachedProperty<bool> IsOffProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsOff", typeof(NudgeLook), inherits: true);

    public static bool GetIsOff(StyledElement element) => element.GetValue(IsOffProperty);
    public static void SetIsOff(StyledElement element, bool value) => element.SetValue(IsOffProperty, value);
}
