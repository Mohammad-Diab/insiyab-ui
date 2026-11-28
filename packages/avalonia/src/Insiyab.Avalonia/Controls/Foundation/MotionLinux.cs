using System.Globalization;
using System.Runtime.Versioning;
using Avalonia.Threading;
using Tmds.DBus.Protocol;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a Linux desktop said about motion: the portal's own reduced-motion (1 = less), GNOME's animations switch, KDE's animation speed (0 = instant). Null: not said.</summary>
internal readonly record struct LinuxMotionState(uint? ReducedMotion, bool? GnomeAnimations, double? KdeFactor);

/// <summary>The pure parts of reading reduced motion on Linux, kept apart so the tests reach them on any system.</summary>
internal static class LinuxMotionSettings
{
    internal const string Appearance = "org.freedesktop.appearance", ReducedMotion = "reduced-motion";
    internal const string Gnome = "org.gnome.desktop.interface", GnomeAnimations = "enable-animations";
    internal const string Kde = "org.kde.kdeglobals.KDE", KdeFactor = "AnimationDurationFactor";

    /// <summary>The settings read from the portal, each a namespace and a key.</summary>
    internal static readonly (string Namespace, string Key)[] Watched = [(Appearance, ReducedMotion), (Gnome, GnomeAnimations), (Kde, KdeFactor)];

    /// <summary>Any one of them asking for less motion wins.</summary>
    internal static bool Reduces(LinuxMotionState state) =>
        state.ReducedMotion == 1 || state.GnomeAnimations == false || state.KdeFactor is { } f && f <= 0.0001;

    /// <summary>The state with one setting's new value (the portal's answer or its SettingChanged signal); a setting it does not watch leaves it as it was.</summary>
    internal static LinuxMotionState With(LinuxMotionState state, string ns, string key, VariantValue value)
    {
        var plain = Plain(value);
        if (ns == Appearance && key == ReducedMotion) return state with { ReducedMotion = AsUInt(plain) };
        if (ns == Gnome && key == GnomeAnimations) return state with { GnomeAnimations = plain as bool? };
        if (ns == Kde && key == KdeFactor) return state with { KdeFactor = AsDouble(plain) };
        return state;
    }

    /// <summary>A portal value without its variant wrappings (the older <c>Read</c> wraps it twice): a bool, a whole number, a double or a string; null for anything else.</summary>
    internal static object? Plain(VariantValue value)
    {
        for (var i = 0; i < 4 && value.Type == VariantValueType.Variant; i++) value = value.GetVariantValue();
        return value.Type switch
        {
            VariantValueType.Bool => value.GetBool(),
            VariantValueType.Byte => (long)value.GetByte(),
            VariantValueType.Int16 => (long)value.GetInt16(),
            VariantValueType.UInt16 => (long)value.GetUInt16(),
            VariantValueType.Int32 => (long)value.GetInt32(),
            VariantValueType.UInt32 => (long)value.GetUInt32(),
            VariantValueType.Int64 => value.GetInt64(),
            VariantValueType.UInt64 => (long)Math.Min(value.GetUInt64(), long.MaxValue),
            VariantValueType.Double => value.GetDouble(),
            VariantValueType.String => value.GetString(),
            _ => null,
        };
    }

    private static uint? AsUInt(object? plain) => plain switch
    {
        long n when n is >= 0 and <= uint.MaxValue => (uint)n,
        string s when uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) => n,
        _ => null,
    };

    // KDE's portal gives its settings as the file's text
    private static double? AsDouble(object? plain) => plain switch
    {
        double d => d,
        long n => n,
        string s => Number(s),
        _ => null,
    };

    private static double? Number(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d) ? d : null;

    /// <summary>KDE's animation speed from the text of <c>kdeglobals</c> (group <c>[KDE]</c>, key <c>AnimationDurationFactor</c>); null when it is not there.</summary>
    internal static double? KdeFactorFromFile(string text)
    {
        var inGroup = false;
        double? found = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';') continue;
            if (line[0] == '[')
            {
                inGroup = line == "[KDE]";
                continue;
            }
            if (!inGroup) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            // a key written with [$i] (locked) or a locale is the same setting
            if (key.IndexOf('[') is var at and > 0) key = key[..at];
            if (key == KdeFactor) found = Number(line[(eq + 1)..]);
        }
        return found;
    }

    /// <summary>Where <c>kdeglobals</c> is: <c>$XDG_CONFIG_HOME</c>, else <c>~/.config</c>.</summary>
    internal static string? KdeFile(string? configHome, string? home) =>
        configHome is { Length: > 0 } c && Path.IsPathRooted(c) ? Path.Combine(c, "kdeglobals")
        : home is { Length: > 0 } h ? Path.Combine(h, ".config", "kdeglobals") : null;

    /// <summary>Whether the session is KDE's (whose file counts only there).</summary>
    internal static bool IsKde(string? desktop) => desktop?.Contains("KDE", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>The system's reduced motion on a Linux desktop, read through the desktop portal (<c>org.freedesktop.portal.Settings</c>): its own <c>org.freedesktop.appearance reduced-motion</c>, GNOME's <c>enable-animations</c> and KDE's <c>AnimationDurationFactor</c>, and in a KDE session <c>kdeglobals</c> itself where the portal does not say. The portal is asked off the UI thread, once, then followed by its <c>SettingChanged</c> signal; <see cref="Read"/> never waits (until the portal answers it knows only the file). A desktop with no bus or no portal reads as not reduced.</summary>
[SupportedOSPlatform("linux")]
internal static class LinuxMotion
{
    private const string Portal = "org.freedesktop.portal.Desktop", PortalPath = "/org/freedesktop/portal/desktop", Settings = "org.freedesktop.portal.Settings";

    private static readonly Lock s_gate = new();
    private static LinuxMotionState s_portal;
    private static bool s_started;

    /// <summary>Whether the desktop asks for less motion now, from what the portal said so far and KDE's file.</summary>
    internal static bool Read()
    {
        Start();
        LinuxMotionState portal;
        lock (s_gate) portal = s_portal;
        var kde = portal.KdeFactor;
        if (kde is null && LinuxMotionSettings.IsKde(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")))
            kde = ReadKdeFile();
        return LinuxMotionSettings.Reduces(portal with { KdeFactor = kde });
    }

    private static double? ReadKdeFile()
    {
        try
        {
            var path = LinuxMotionSettings.KdeFile(Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"), Environment.GetEnvironmentVariable("HOME"));
            if (path is null || !File.Exists(path)) return null;
            var info = new FileInfo(path);
            return info.Length > 1 << 20 ? null : LinuxMotionSettings.KdeFactorFromFile(File.ReadAllText(path));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void Start()
    {
        lock (s_gate)
        {
            if (s_started) return;
            s_started = true;
        }
        Task.Run(StartAsync);
    }

    private static async Task StartAsync()
    {
        try
        {
            if (await LinuxSession.BusAsync().ConfigureAwait(false) is not { } bus) return;
            await bus.AddMatchAsync(
                new MatchRule { Type = MessageType.Signal, Path = PortalPath, Interface = Settings, Member = "SettingChanged" },
                static (m, _) =>
                {
                    var r = m.GetBodyReader();
                    return (Namespace: r.ReadString(), Key: r.ReadString(), Value: r.ReadVariantValue());
                },
                (Notification<(string Namespace, string Key, VariantValue Value)> n) =>
                {
                    if (n.HasValue) Take(n.Value.Namespace, n.Value.Key, n.Value.Value);
                }, false, ObserverFlags.None, null).AsTask().WaitAsync(LinuxSession.Limit).ConfigureAwait(false);
            foreach (var (ns, key) in LinuxMotionSettings.Watched)
                if (await ReadAsync(bus, ns, key).ConfigureAwait(false) is { } value) Take(ns, key, value);
        }
        catch (Exception)
        {
        }
    }

    // ReadOne (the portal's version 2), else the older Read; null when the portal has no such setting or does not answer
    private static async Task<VariantValue?> ReadAsync(DBusConnection bus, string ns, string key)
    {
        foreach (var member in (string[])["ReadOne", "Read"])
            try
            {
                return await LinuxSession.CallAsync(bus, Portal, PortalPath, Settings, member, "ss", (ref MessageWriter w) =>
                {
                    w.WriteString(ns);
                    w.WriteString(key);
                }, static (m, _) => (VariantValue?)m.GetBodyReader().ReadVariantValue()).ConfigureAwait(false);
            }
            catch (DBusErrorReplyException e) when (member == "ReadOne" && e.ErrorName == "org.freedesktop.DBus.Error.UnknownMethod")
            {
            }
            catch (Exception)
            {
                return null;
            }
        return null;
    }

    // a watched setting came or changed: Motion reads again on the UI thread, and the windows follow when the answer differs
    private static void Take(string ns, string key, VariantValue value)
    {
        lock (s_gate)
        {
            var now = LinuxMotionSettings.With(s_portal, ns, key, value);
            if (now == s_portal) return;
            s_portal = now;
        }
        Dispatcher.UIThread.Post(Motion.SystemSettingChanged);
    }
}
