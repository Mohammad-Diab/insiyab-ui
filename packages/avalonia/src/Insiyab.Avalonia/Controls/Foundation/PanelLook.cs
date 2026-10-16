using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a panel's look hands down to its parts: whether the panel needs attention and whether its body runs flush to the edges.</summary>
internal static class PanelLook
{
    /// <summary>Set by an alert panel: its head, title and icon take the danger tint.</summary>
    public static readonly AttachedProperty<bool> AlertProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Alert", typeof(PanelLook), inherits: true);

    /// <summary>Set by a flush body: a note inside it drops its rule and keeps a smaller inset.</summary>
    public static readonly AttachedProperty<bool> FlushProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Flush", typeof(PanelLook), inherits: true);

    public static bool GetAlert(StyledElement element) => element.GetValue(AlertProperty);
    public static void SetAlert(StyledElement element, bool value) => element.SetValue(AlertProperty, value);
    public static bool GetFlush(StyledElement element) => element.GetValue(FlushProperty);
    public static void SetFlush(StyledElement element, bool value) => element.SetValue(FlushProperty, value);
}
