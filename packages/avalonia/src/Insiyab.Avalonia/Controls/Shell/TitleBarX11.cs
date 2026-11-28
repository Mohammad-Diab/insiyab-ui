using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The parts of X11's WM_NORMAL_HINTS the library writes, kept apart from the calls so the tests reach them on any system.</summary>
internal static class X11SizeHints
{
    // XSizeHints on 64-bit Linux
    [StructLayout(LayoutKind.Explicit, Size = 80)]
    internal struct SizeHints
    {
        [FieldOffset(0)] public nint Flags;
        [FieldOffset(48)] public int MinAspectX;
        [FieldOffset(52)] public int MinAspectY;
        [FieldOffset(56)] public int MaxAspectX;
        [FieldOffset(60)] public int MaxAspectY;
        [FieldOffset(64)] public int BaseWidth;
        [FieldOffset(68)] public int BaseHeight;
    }

    internal const int PAspect = 1 << 7, PBaseSize = 1 << 8;

    /// <summary>The aspect part of WM_NORMAL_HINTS for a window that keeps its inside's shape: the same least and most aspect (<paramref name="aspect"/> as a fraction over 10000) and the frame round the inside as the base size, which ICCCM takes off before the aspect is checked. 0 or less takes both away. The other hints (Avalonia's least and most size) are kept.</summary>
    internal static SizeHints WithAspect(SizeHints hints, double aspect, PixelSize frame)
    {
        if (aspect > 0 && double.IsFinite(aspect))
        {
            var x = (int)Math.Round(Math.Clamp(aspect, 0.01, 100) * 10000);
            hints.Flags |= PAspect | PBaseSize;
            hints.MinAspectX = hints.MaxAspectX = x;
            hints.MinAspectY = hints.MaxAspectY = 10000;
            hints.BaseWidth = Math.Max(0, frame.Width);
            hints.BaseHeight = Math.Max(0, frame.Height);
        }
        else hints.Flags &= ~(nint)(PAspect | PBaseSize);
        return hints;
    }
}

/// <summary>What a title bar the app draws on Linux asks of the window manager, as GTK does: its own window menu (_GTK_SHOW_WINDOW_MENU), which GNOME, KDE, Xfce and Cinnamon answer with their menu in the system's language, and the drawn shadow's width round the window (_GTK_FRAME_EXTENTS).</summary>
[SupportedOSPlatform("linux")]
internal static partial class TitleBarX11
{
    private const int ClientMessage = 33;
    private const int PropModeReplace = 0;
    private const nuint XaCardinal = 6;
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
    [LibraryImport("libX11.so.6")] private static partial int XChangeProperty(nint display, nuint window, nuint property, nuint type, int format, int mode, nint[] data, int count);
    [LibraryImport("libX11.so.6")] private static partial int XDeleteProperty(nint display, nuint window, nuint property);
    [LibraryImport("libX11.so.6")] private static partial int XGetWMNormalHints(nint display, nuint window, ref X11SizeHints.SizeHints hints, out nint supplied);
    [LibraryImport("libX11.so.6")] private static partial void XSetWMNormalHints(nint display, nuint window, ref X11SizeHints.SizeHints hints);

    private static nint s_display;
    private static nuint s_atom;
    private static nuint s_extentsAtom;

    // the window's X id, where it has one (X11 and XWayland)
    private static nuint Xid(Window window) =>
        window.TryGetPlatformHandle() is { Handle: not 0 } handle && handle.HandleDescriptor == "XID" ? (nuint)handle.Handle : 0;

    /// <summary>Tells the window manager how wide the drawn shadow round the window is, on each side (device pixels), so snapping, tiling and placing use the visible frame; null takes the hint away. False where it cannot be told.</summary>
    public static bool SetFrameExtents(Window window, int? pixels)
    {
        var xid = Xid(window);
        if (xid == 0) return false;
        try
        {
            if (s_display == 0) s_display = XOpenDisplay(0);
            if (s_display == 0) return false;
            if (s_extentsAtom == 0) s_extentsAtom = XInternAtom(s_display, "_GTK_FRAME_EXTENTS", 0);
            // left, right, top, bottom
            if (pixels is { } p) XChangeProperty(s_display, xid, s_extentsAtom, XaCardinal, 32, PropModeReplace, [p, p, p, p], 4);
            else XDeleteProperty(s_display, xid, s_extentsAtom);
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

    /// <summary>Asks the window manager to keep the window's inside at <paramref name="aspect"/> while it is resized (<see cref="X11SizeHints.WithAspect"/>); false where it cannot be asked.</summary>
    public static bool SetAspect(Window window, double aspect, PixelSize frame)
    {
        var xid = Xid(window);
        if (xid == 0) return false;
        try
        {
            if (s_display == 0) s_display = XOpenDisplay(0);
            if (s_display == 0) return false;
            var hints = default(X11SizeHints.SizeHints);
            if (XGetWMNormalHints(s_display, xid, ref hints, out _) == 0) hints = default;
            var wanted = X11SizeHints.WithAspect(hints, aspect, frame);
            if (wanted.Flags == hints.Flags && wanted.MinAspectX == hints.MinAspectX && wanted.MinAspectY == hints.MinAspectY
                && wanted.BaseWidth == hints.BaseWidth && wanted.BaseHeight == hints.BaseHeight) return true;
            XSetWMNormalHints(s_display, xid, ref wanted);
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

    /// <summary>Asks the window manager to show its window menu for <paramref name="window"/> at a point on the screen (device pixels); false where it cannot be asked.</summary>
    public static bool ShowMenu(Window window, PixelPoint at)
    {
        var xid = Xid(window);
        if (xid == 0) return false;
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
                Window = xid,
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
