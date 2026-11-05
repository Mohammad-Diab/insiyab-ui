using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>A panel's tone; <c>alert</c> is the bad tone.</summary>
internal enum PanelTone { None, Bad, Ok, Warn, Info, Brand }

/// <summary>What a panel's look hands down to its parts: its tone, whether its body runs flush to the edges, and whether it is dense.</summary>
internal static class PanelLook
{
    /// <summary>Set by a toned panel: its head, title and icon take the tone.</summary>
    public static readonly AttachedProperty<PanelTone> ToneProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, PanelTone>("Tone", typeof(PanelLook), inherits: true);

    /// <summary>Set by a flush body: a note inside it drops its rule and keeps a smaller inset.</summary>
    public static readonly AttachedProperty<bool> FlushProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Flush", typeof(PanelLook), inherits: true);

    /// <summary>Set by a dense panel: its parts take smaller insets and a smaller icon.</summary>
    public static readonly AttachedProperty<bool> DenseProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Dense", typeof(PanelLook), inherits: true);

    public static PanelTone GetTone(StyledElement element) => element.GetValue(ToneProperty);
    public static void SetTone(StyledElement element, PanelTone value) => element.SetValue(ToneProperty, value);
    public static bool GetFlush(StyledElement element) => element.GetValue(FlushProperty);
    public static void SetFlush(StyledElement element, bool value) => element.SetValue(FlushProperty, value);
    public static bool GetDense(StyledElement element) => element.GetValue(DenseProperty);
    public static void SetDense(StyledElement element, bool value) => element.SetValue(DenseProperty, value);
}
