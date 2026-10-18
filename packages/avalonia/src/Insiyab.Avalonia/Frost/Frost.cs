using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia;

/// <summary>The window material. Every window asks the OS for Mica, then acrylic; while one is granted the window carries <c>:ins-material</c> and its ground turns into a readable veil over it. Set <c>ins:Frost.Material="False"</c> to keep a window opaque.</summary>
public static class Frost
{
    /// <summary>The pseudo-class a window carries while the OS material shows through it.</summary>
    public const string MaterialPseudoClass = ":ins-material";

    /// <summary>False keeps the window opaque: it asks the OS for no material.</summary>
    public static readonly AttachedProperty<bool> MaterialProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("Material", typeof(Frost), true);

    private static bool _registered;

    public static bool GetMaterial(Window window) => window.GetValue(MaterialProperty);

    public static void SetMaterial(Window window, bool value) => window.SetValue(MaterialProperty, value);

    /// <summary>True for the levels that blur what is behind the window; None and Transparent get the opaque ground.</summary>
    internal static bool IsMaterialLevel(WindowTransparencyLevel level) =>
        level == WindowTransparencyLevel.Mica || level == WindowTransparencyLevel.AcrylicBlur || level == WindowTransparencyLevel.Blur;

    /// <summary>Whether the material shows through this window right now.</summary>
    public static bool IsMaterialOn(Window window) => GetMaterial(window) && IsMaterialLevel(window.ActualTransparencyLevel);

    internal static void Register()
    {
        if (_registered)
            return;
        _registered = true;
        TopLevel.ActualTransparencyLevelProperty.Changed.AddClassHandler<Window>((w, _) => Update(w));
        MaterialProperty.Changed.AddClassHandler<Window>((w, _) => Update(w));
        Visual.IsVisibleProperty.Changed.AddClassHandler<Window>((w, _) => Update(w));
    }

    private static void Update(Window window) => ((IPseudoClasses)window.Classes).Set(MaterialPseudoClass, IsMaterialOn(window));
}
