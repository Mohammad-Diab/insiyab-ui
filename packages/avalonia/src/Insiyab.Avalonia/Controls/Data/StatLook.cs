using Avalonia;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a stat tile's look hands down to its parts: its tone, its size and whether it is under the pointer.</summary>
internal static class StatLook
{
    /// <summary>The tile's tone class (ok, warn, bad, info, mute, zero).</summary>
    public static readonly AttachedProperty<string?> ToneProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Tone", typeof(StatLook), inherits: true);

    /// <summary>The tile's size class (sm, lg).</summary>
    public static readonly AttachedProperty<string?> SizeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Size", typeof(StatLook), inherits: true);

    /// <summary>Whether the tile, or the button around it, is under the pointer.</summary>
    public static readonly AttachedProperty<bool> HotProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Hot", typeof(StatLook), inherits: true);

    public static string? GetTone(StyledElement element) => element.GetValue(ToneProperty);
    public static void SetTone(StyledElement element, string? value) => element.SetValue(ToneProperty, value);
    public static string? GetSize(StyledElement element) => element.GetValue(SizeProperty);
    public static void SetSize(StyledElement element, string? value) => element.SetValue(SizeProperty, value);
    public static bool GetHot(StyledElement element) => element.GetValue(HotProperty);
    public static void SetHot(StyledElement element, bool value) => element.SetValue(HotProperty, value);
}
