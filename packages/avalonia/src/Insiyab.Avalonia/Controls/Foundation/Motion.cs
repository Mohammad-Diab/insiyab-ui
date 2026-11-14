using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's «تخفيف المؤثرات» (<c>data-ins-fx="off"</c>) together with the system's reduced motion (Windows' animations off, Android's "Remove animations"): while a window is still, hovers and presses lose their movement and keep their colour, and motion run from code is skipped. A still window carries <c>:ins-still</c>.</summary>
public static class Motion
{
    /// <summary>The pseudo-class a TopLevel carries while it is still.</summary>
    public const string StillPseudoClass = ":ins-still";

    /// <summary>On a TopLevel: true turns its effects off, the app's own preference whatever the system says.</summary>
    public static readonly AttachedProperty<bool> StillProperty =
        AvaloniaProperty.RegisterAttached<TopLevel, bool>("Still", typeof(Motion));

    private static bool? s_system;
    private static bool _registered;
    private static bool s_watchingReturn;
    private static readonly List<WeakReference<TopLevel>> s_tops = [];

    public static bool GetStill(TopLevel top) => top.GetValue(StillProperty);
    public static void SetStill(TopLevel top, bool value) => top.SetValue(StillProperty, value);

    /// <summary>Whether the system asks for less motion: Windows' "Show animations in Windows" turned off, or Android's animator duration scale at 0 ("Remove animations"). False on other systems.</summary>
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
        Control.LoadedEvent.AddClassHandler<TopLevel>((top, _) =>
        {
            Remember(top);
            WatchReturn();
            Update(top);
        });
    }

    // A setting changed while the app was away is read again when it comes back.
    private static void WatchReturn()
    {
        if (s_watchingReturn || !Os.IsAndroid || Application.Current?.TryGetFeature<IActivatableLifetime>() is not { } life) return;
        s_watchingReturn = true;
        life.Activated += (_, e) => { if (e.Kind == ActivationKind.Background) Refresh([.. Tops()]); };
    }

    private static void Remember(TopLevel top)
    {
        s_tops.RemoveAll(w => !w.TryGetTarget(out var t) || t == top);
        s_tops.Add(new WeakReference<TopLevel>(top));
    }

    // The TopLevels shown so far that are still alive.
    internal static IEnumerable<TopLevel> Tops()
    {
        foreach (var w in s_tops.ToArray())
            if (w.TryGetTarget(out var top)) yield return top;
    }

    // After the app-wide effects change.
    internal static void UpdateAll()
    {
        foreach (var top in Tops()) Update(top);
    }

    internal static void Update(TopLevel top)
    {
        var still = IsStill(top);
        ((IPseudoClasses)top.Classes).Set(StillPseudoClass, still);
        if (still) top.SetValue(ClassTheme.StillProperty, true);
        else top.ClearValue(ClassTheme.StillProperty);
        var light = InsiyabTheme.Effects == EffectsLevel.Light;
        ((IPseudoClasses)top.Classes).Set(InsiyabTheme.LightEffectsPseudoClass, light);
        StillShadows.Set(top, still ? ShadowMode.Flat : light ? ShadowMode.Simple : ShadowMode.Full);
    }

    /// <summary>Whether the effects are light where this visual is (<see cref="InsiyabTheme.Effects"/>).</summary>
    internal static bool IsLight(Visual? visual) => InsiyabTheme.Effects == EffectsLevel.Light;

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
        if (Os.IsAndroid) return AndroidDevice.AnimatorScale == 0;
        if (!Os.IsWindows) return false;
        try { return SystemParametersInfo(SpiGetClientAreaAnimation, 0, out var on, 0) != 0 && on == 0; }
        catch (Exception) { return false; }
    }

    private const uint SpiGetClientAreaAnimation = 0x1042;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    private static extern int SystemParametersInfo(uint action, uint param, out int value, uint winIni);
}
