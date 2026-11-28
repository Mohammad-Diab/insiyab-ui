using System.Globalization;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Tmds.DBus.Protocol;

namespace Insiyab.Avalonia.Controls;

/// <summary>The pure parts of the Linux dock badge (Unity's launcher entry API), kept apart so the tests reach them on any system.</summary>
internal static class LauncherEntry
{
    internal const string Interface = "com.canonical.Unity.LauncherEntry";

    /// <summary>The app as the docks know it: <c>application://</c> and its menu entry's file name.</summary>
    internal static string AppUri(string desktopFile) => "application://" + desktopFile;

    /// <summary>The object path the signal comes from: any path will do; one per app, made of letters and digits only as D-Bus asks.</summary>
    internal static string ObjectPath(string desktopFile)
    {
        var hash = 2166136261u; // FNV-1a
        foreach (var c in desktopFile)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return "/com/canonical/unity/launcherentry/" + hash.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>The menu entry's file name: the one the app named (<see cref="Taskbar.LinuxDesktopFile"/>), else the one it was started from (GLib's <c>GIO_LAUNCHED_DESKTOP_FILE</c>, a path), else the program's name with <c>.desktop</c>; null when none can be told.</summary>
    internal static string? DesktopFile(string? named, string? launchedFrom, string? processPath)
    {
        if (named is { Length: > 0 }) return named.EndsWith(".desktop", StringComparison.Ordinal) ? named : named + ".desktop";
        if (launchedFrom is { Length: > 0 } && Path.GetFileName(launchedFrom) is { Length: > 0 } launched) return launched;
        return Path.GetFileNameWithoutExtension(processPath) is { Length: > 0 } program ? program + ".desktop" : null;
    }

    /// <summary>The count the docks get: the window's own number (they draw it their own way); 0 hides it.</summary>
    internal static long CountOf(string? shown) =>
        shown is not null && long.TryParse(shown, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0 ? n : 0;
}

/// <summary>The badge on a Linux dock (Ubuntu's dock, KDE Plasma's task manager, Plank, Dash to Dock): Unity's <c>com.canonical.Unity.LauncherEntry</c> <c>Update</c> signal on the session bus with <c>count</c> and <c>count-visible</c>, for the app's menu entry. One for the whole app, showing the last window that still has a badge, as the macOS Dock. Sent off the UI thread, latest count only; a desktop with no bus or no such dock shows nothing. The docks drop it when the app's connection ends.</summary>
[SupportedOSPlatform("linux")]
internal sealed class LauncherBadge : IBadgeSink
{
    private readonly DockOrder _order = new();
    private readonly Lock _gate = new();
    private long? _pending;
    private long _sent;
    private bool _sending, _used;

    public bool Counts => true;

    public void Apply(TopLevel top, string? text, string? label)
    {
        // the dock writes the number itself, so it gets the window's own count, not the 99+ the disc shows
        _used = true;
        Send(LauncherEntry.CountOf(_order.Set(top, text is null ? null : Taskbar.GetBadge(top).ToString(CultureInfo.InvariantCulture))));
    }

    public void Forget(TopLevel top)
    {
        var shown = _order.Forget(top);
        if (_used) Send(LauncherEntry.CountOf(shown));
    }

    private void Send(long count)
    {
        lock (_gate)
        {
            _pending = count;
            if (_sending) return;
            _sending = true;
        }
        Task.Run(SendAsync);
    }

    private async Task SendAsync()
    {
        DBusConnection? bus = null;
        try
        {
            bus = await LinuxSession.BusAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
        }
        var file = LauncherEntry.DesktopFile(Taskbar.LinuxDesktopFile, Environment.GetEnvironmentVariable("GIO_LAUNCHED_DESKTOP_FILE"), Environment.ProcessPath);
        while (true)
        {
            long count;
            lock (_gate)
            {
                if (_pending is not { } next || bus is null || file is null)
                {
                    _pending = null;
                    _sending = false;
                    return;
                }
                count = next;
                _pending = null;
            }
            if (count == _sent) continue;
            var uri = LauncherEntry.AppUri(file);
            if (LinuxSession.Emit(bus, LauncherEntry.ObjectPath(file), LauncherEntry.Interface, "Update", "sa{sv}", (ref MessageWriter w) =>
                {
                    w.WriteString(uri);
                    var d = w.WriteDictionaryStart();
                    w.WriteDictionaryEntryStart();
                    w.WriteString("count");
                    w.WriteVariantInt64(count);
                    w.WriteDictionaryEntryStart();
                    w.WriteString("count-visible");
                    w.WriteVariantBool(count > 0);
                    w.WriteDictionaryEnd(d);
                }))
                _sent = count;
        }
    }
}
