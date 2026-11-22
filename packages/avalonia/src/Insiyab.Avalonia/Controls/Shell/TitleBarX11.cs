using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The window manager's own window menu for a title bar the app draws on Linux: the request GTK sends (_GTK_SHOW_WINDOW_MENU), which GNOME, KDE, Xfce and Cinnamon answer with their menu in the system's language.</summary>
[SupportedOSPlatform("linux")]
internal static partial class TitleBarX11
{
    private const int ClientMessage = 33;
    private const long SubstructureNotifyMask = 1L << 19, SubstructureRedirectMask = 1L << 20;

    // XClientMessageEvent on 64-bit Linux, padded to the size of an XEvent (24 longs)
    [StructLayout(LayoutKind.Explicit, Size = 192)]
    private struct ClientMessageEvent
    {
        [FieldOffset(0)] public int Type;
        [FieldOffset(8)] public nuint Serial;
        [FieldOffset(16)] public int SendEvent;
        [FieldOffset(24)] public nint Display;
        [FieldOffset(32)] public nuint Window;
        [FieldOffset(40)] public nuint MessageType;
        [FieldOffset(48)] public int Format;
        [FieldOffset(56)] public nint Data0;
        [FieldOffset(64)] public nint Data1;
        [FieldOffset(72)] public nint Data2;
    }

    [LibraryImport("libX11.so.6")] private static partial nint XOpenDisplay(nint name);
    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)] private static partial nuint XInternAtom(nint display, string name, int onlyIfExists);
    [LibraryImport("libX11.so.6")] private static partial nuint XDefaultRootWindow(nint display);
    [LibraryImport("libX11.so.6")] private static partial int XSendEvent(nint display, nuint window, int propagate, nint mask, ref ClientMessageEvent ev);
    [LibraryImport("libX11.so.6")] private static partial int XFlush(nint display);

    private static nint s_display;
    private static nuint s_atom;

    /// <summary>Asks the window manager to show its window menu for <paramref name="window"/> at a point on the screen (device pixels); false where it cannot be asked.</summary>
    public static bool ShowMenu(Window window, PixelPoint at)
    {
        if (window.TryGetPlatformHandle() is not { Handle: not 0 } handle || handle.HandleDescriptor != "XID") return false;
        try
        {
            if (s_display == 0) s_display = XOpenDisplay(0);
            if (s_display == 0) return false;
            if (s_atom == 0) s_atom = XInternAtom(s_display, "_GTK_SHOW_WINDOW_MENU", 0);
            var ev = new ClientMessageEvent
            {
                Type = ClientMessage,
                SendEvent = 1,
                Display = s_display,
                Window = (nuint)handle.Handle,
                MessageType = s_atom,
                Format = 32,
                Data0 = 0,
                Data1 = at.X,
                Data2 = at.Y,
            };
            XSendEvent(s_display, XDefaultRootWindow(s_display), 0, (nint)(SubstructureNotifyMask | SubstructureRedirectMask), ref ev);
            XFlush(s_display);
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
