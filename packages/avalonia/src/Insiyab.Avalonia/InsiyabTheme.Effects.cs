using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia;

/// <summary>How much the library draws for depth: <see cref="Full"/> blurs and soft shadows, or <see cref="Light"/> for weak devices.</summary>
public enum EffectsLevel
{
    /// <summary>Backdrop blur behind dialogs, the window material and the full soft shadows.</summary>
    Full,

    /// <summary>No backdrop blur or window material (solid grounds instead) and one smaller drop per shadow; layout, colours and motion stay.</summary>
    Light,
}

public partial class InsiyabTheme
{
    /// <summary>The pseudo-class a TopLevel carries while the effects are light.</summary>
    public const string LightEffectsPseudoClass = ":ins-light";

    private static EffectsLevel? s_effects;

    /// <summary>The effects every window and view draws. Full unless the device reports itself low on memory (or has under 3.5 GB), where it starts Light; setting it overrides that and updates what is open at once.</summary>
    public static EffectsLevel Effects
    {
        get => s_effects ??= AndroidDevice.IsLowMemory ? EffectsLevel.Light : EffectsLevel.Full;
        set
        {
            if (s_effects == value) return;
            s_effects = value;
            Motion.UpdateAll();
        }
    }

    // Tests go back to the device's own choice.
    internal static void ResetEffects()
    {
        s_effects = null;
        Motion.UpdateAll();
    }
}
