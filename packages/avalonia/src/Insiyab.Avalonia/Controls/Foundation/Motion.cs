using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's «تخفيف المؤثرات» (<c>data-ins-fx="off"</c>) together with the system's reduced motion: while a window is still, hovers and presses lose their movement and keep their colour, and motion run from code is skipped. A still window carries <c>:ins-still</c>.</summary>
public static class Motion
{
    /// <summary>The pseudo-class a TopLevel carries while it is still.</summary>
    public const string StillPseudoClass = ":ins-still";

    /// <summary>On a TopLevel: true turns its effects off, the app's own preference whatever the system says.</summary>
    public static readonly AttachedProperty<bool> StillProperty =
        AvaloniaProperty.RegisterAttached<TopLevel, bool>("Still", typeof(Motion));

    private static bool? s_system;
    private static bool _registered;

    public static bool GetStill(TopLevel top) => top.GetValue(StillProperty);
    public static void SetStill(TopLevel top, bool value) => top.SetValue(StillProperty, value);

    /// <summary>Whether the system asks for less motion: Windows' "Show animations in Windows" turned off. False on other systems for now.</summary>
    public static bool SystemReducesMotion => SystemOverride ?? (s_system ??= ReadSystem());

    // Tests stand in for the system setting.
    internal static bool? SystemOverride { get; set; }

    /// <summary>Whether motion should be skipped where this visual is: its window is still, or the system reduces motion.</summary>
    public static bool IsStill(Visual? visual) =>
        SystemReducesMotion || (OverlayHost.Top(visual) is { } top && GetStill(top));

    /// <summary>Reads the system setting again, after the user changed it, and updates the given windows.</summary>
    public static void Refresh(params TopLevel[] tops)
    {
        s_system = null;
        foreach (var top in tops) Update(top);
    }

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        StillProperty.Changed.AddClassHandler<TopLevel>((top, _) => Update(top));
        Control.LoadedEvent.AddClassHandler<TopLevel>((top, _) => Update(top));
    }

    internal static void Update(TopLevel top) => ((IPseudoClasses)top.Classes).Set(StillPseudoClass, IsStill(top));

    // The motion tokens, read where the control is so a theme can retime them.
    internal static TimeSpan Enter(Control at) => Time(at, "InsTEnter", 280);
    internal static TimeSpan Exit(Control at) => Time(at, "InsTExit", 180);
    internal static TimeSpan Pop(Control at) => Time(at, "InsTPop", 220);

    internal static Easing Ease(Control at) =>
        at.TryFindResource("InsEase", at.ActualThemeVariant, out var value) && value is Easing easing ? easing : new SplineEasing(0.22, 1, 0.36, 1);

    private static TimeSpan Time(Control at, string key, int fallback) =>
        at.TryFindResource(key, at.ActualThemeVariant, out var value) && value is TimeSpan time ? time : TimeSpan.FromMilliseconds(fallback);

    private static bool ReadSystem()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try { return SystemParametersInfo(SpiGetClientAreaAnimation, 0, out var on, 0) != 0 && on == 0; }
        catch (Exception) { return false; }
    }

    private const uint SpiGetClientAreaAnimation = 0x1042;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static extern int SystemParametersInfo(uint action, uint param, out int value, uint winIni);
}
