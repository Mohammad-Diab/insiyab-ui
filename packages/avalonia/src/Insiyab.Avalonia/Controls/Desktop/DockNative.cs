using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

internal static unsafe class DockNative
{
    private const string Objc = "/usr/lib/libobjc.A.dylib";

    [DllImport(Objc)] internal static extern nint objc_getClass(byte* name);
    [DllImport(Objc)] internal static extern nint sel_registerName(byte* name);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] internal static extern nint Send(nint receiver, nint selector);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] internal static extern nint Send(nint receiver, nint selector, nint argument);

    /// <summary>Sets NSApp.dockTile.badgeLabel; null clears it.</summary>
    internal static void SetBadge(string? text)
    {
        var app = Send(Class("NSApplication"), Selector("sharedApplication"));
        if (app == 0) return;
        var tile = Send(app, Selector("dockTile"));
        if (tile == 0) return;
        nint label = 0;
        if (text is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(text + "\0");
            fixed (byte* utf8 = bytes)
                label = Send(Send(Class("NSString"), Selector("alloc")), Selector("initWithUTF8String:"), (nint)utf8);
        }
        Send(tile, Selector("setBadgeLabel:"), label);
        if (label != 0) Send(label, Selector("release"));
    }

    private static nint Class(string name)
    {
        fixed (byte* utf8 = Encoding.UTF8.GetBytes(name + "\0"))
            return objc_getClass(utf8);
    }

    private static nint Selector(string name)
    {
        fixed (byte* utf8 = Encoding.UTF8.GetBytes(name + "\0"))
            return sel_registerName(utf8);
    }
}

/// <summary>The macOS Dock badge: one for the whole app, showing the last window that still has a badge.</summary>
internal sealed class DockBadge : IBadgeSink
{
    private readonly DockOrder _order = new();
    private bool _used;

    public void Apply(TopLevel top, string? text, string? label)
    {
        if (!OperatingSystem.IsMacOS() || top.TryGetPlatformHandle() is not { Handle: not 0 } handle || handle.HandleDescriptor != "NSWindow") return;
        _used = true;
        Show(_order.Set(top, text));
    }

    public void Forget(TopLevel top)
    {
        var shown = _order.Forget(top);
        if (_used) Show(shown);
    }

    private static void Show(string? text)
    {
        try
        {
            DockNative.SetBadge(text);
        }
        catch (Exception)
        {
        }
    }
}
