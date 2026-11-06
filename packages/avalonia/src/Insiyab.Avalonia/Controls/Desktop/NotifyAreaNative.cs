using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Insiyab.Avalonia.Controls;

/// <summary>The pure parts of the Win32 notification area, kept apart so the tests can reach them on any system.</summary>
internal static class NotifyArea
{
    internal const uint NIIF_INFO = 0x01, NIIF_WARNING = 0x02, NIIF_ERROR = 0x03, NIIF_USER = 0x04, NIIF_LARGE_ICON = 0x20, NIIF_RESPECT_QUIET_TIME = 0x80;
    internal const uint WM_USER = 0x0400, WM_CONTEXTMENU = 0x007B;
    internal const uint NIN_SELECT = WM_USER, NIN_KEYSELECT = WM_USER + 1, NIN_BALLOONUSERCLICK = WM_USER + 5;

    /// <summary>A balloon's flags: the system's error or warning icon, else the app's own large icon (the system's information icon when there is none), and quiet hours kept.</summary>
    internal static uint BalloonFlags(NotifyAreaLevel level, bool ownIcon) => level switch
    {
        NotifyAreaLevel.Error => NIIF_ERROR | NIIF_RESPECT_QUIET_TIME,
        NotifyAreaLevel.Warning => NIIF_WARNING | NIIF_RESPECT_QUIET_TIME,
        _ when ownIcon => NIIF_USER | NIIF_LARGE_ICON | NIIF_RESPECT_QUIET_TIME,
        _ => NIIF_INFO | NIIF_RESPECT_QUIET_TIME,
    };

    /// <summary>What an icon's callback message means under version 4: the event is the low word of lParam, the menu's point is in wParam (0,0 from the keyboard, where the cursor stands in).</summary>
    internal static (NotifyAreaEvent Kind, int X, int Y)? Decode(nint wParam, nint lParam, Func<(int X, int Y)?> cursor)
    {
        switch ((uint)((long)lParam & 0xFFFF))
        {
            case NIN_SELECT or NIN_KEYSELECT:
                return (NotifyAreaEvent.Activated, 0, 0);
            case WM_CONTEXTMENU:
                int x = (short)((long)wParam & 0xFFFF), y = (short)(((long)wParam >> 16) & 0xFFFF);
                if (x == 0 && y == 0 && cursor() is { } at) (x, y) = at;
                return (NotifyAreaEvent.Menu, x, y);
            case NIN_BALLOONUSERCLICK:
                return (NotifyAreaEvent.BalloonClicked, 0, 0);
            default:
                return null;
        }
    }

    /// <summary>The picture of an .ico file nearest <paramref name="size"/> pixels: the smallest at least that big, else the biggest; null when the bytes are not an icon.</summary>
    internal static (int Offset, int Bytes)? Pick(byte[] ico, int size)
    {
        if (ico.Length < 22 || BitConverter.ToUInt16(ico, 0) != 0 || BitConverter.ToUInt16(ico, 2) != 1) return null;
        var count = BitConverter.ToUInt16(ico, 4);
        (int Width, int Bytes, int Offset)? best = null;
        for (var i = 0; i < count && 6 + 16 * (i + 1) <= ico.Length; i++)
        {
            var entry = 6 + 16 * i;
            var width = ico[entry] == 0 ? 256 : ico[entry];
            var bytes = BitConverter.ToInt32(ico, entry + 8);
            var offset = BitConverter.ToInt32(ico, entry + 12);
            if (offset < 0 || bytes <= 0 || (long)offset + bytes > ico.Length) continue;
            var better = best is not { } b || (b.Width < size ? width > b.Width : width >= size && width < b.Width);
            if (better) best = (width, bytes, offset);
        }
        return best is { } pick ? (pick.Offset, pick.Bytes) : null;
    }

    /// <summary>Copies a text into a fixed buffer of UTF-16 units, cut so a zero always ends it.</summary>
    internal static void Write(Span<ushort> buffer, string text)
    {
        var cut = NotifyAreaIcon.Cut(text, buffer.Length - 1);
        for (var i = 0; i < cut.Length; i++) buffer[i] = cut[i];
        buffer[cut.Length] = 0;
    }
}

/// <summary>One icon in the Windows notification area: a message-only window made on the UI thread receives its messages, so every event already comes on the UI thread.</summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsNotifyArea : INotifyAreaShell
{
    private const string ClassName = "Insiyab.NotifyArea";
    private const uint IconId = 1;
    private const uint CallbackMessage = NotifyAreaNative.WM_APP + 1;

    // The window procedure is a static function pointer, so it finds its icon by its window.
    private static readonly Dictionary<nint, WindowsNotifyArea> s_icons = [];
    private static bool s_registered;
    private static uint s_taskbarCreated;

    private readonly nint _window, _smallIcon, _largeIcon;
    private bool _added, _disposed;

    private WindowsNotifyArea(nint window, nint small, nint large)
    {
        _window = window;
        _smallIcon = small;
        _largeIcon = large;
    }

    public Action<NotifyAreaEvent, int, int>? Raised { get; set; }

    /// <summary>The icon's window, or null when the bytes hold no usable picture or the window cannot be made.</summary>
    internal static WindowsNotifyArea? TryMake(byte[] ico)
    {
        var small = NotifyAreaNative.CreateIcon(ico, NotifyAreaNative.GetSystemMetrics(NotifyAreaNative.SM_CXSMICON));
        if (small == 0) return null;
        var large = NotifyAreaNative.CreateIcon(ico, NotifyAreaNative.GetSystemMetrics(NotifyAreaNative.SM_CXICON));
        var instance = NotifyAreaNative.GetModuleHandle(null);
        if (!s_registered)
        {
            s_taskbarCreated = NotifyAreaNative.RegisterWindowMessage("TaskbarCreated");
            var name = Marshal.StringToHGlobalUni(ClassName);
            var windowClass = new NotifyAreaNative.WindowClass
            {
                cbSize = (uint)sizeof(NotifyAreaNative.WindowClass),
                lpfnWndProc = (nint)(delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint>)&WindowProc,
                hInstance = instance,
                lpszClassName = name,
            };
            NotifyAreaNative.RegisterClassEx(ref windowClass);
            s_registered = true;
        }
        var window = NotifyAreaNative.CreateWindowEx(0, ClassName, null, 0, 0, 0, 0, 0, NotifyAreaNative.HWND_MESSAGE, 0, instance, 0);
        if (window == 0)
        {
            NotifyAreaNative.DestroyIcon(small);
            if (large != 0) NotifyAreaNative.DestroyIcon(large);
            return null;
        }
        var icon = new WindowsNotifyArea(window, small, large);
        s_icons[window] = icon;
        return icon;
    }

    public bool Add(string tip)
    {
        if (_disposed) return false;
        var data = Data(NotifyAreaNative.NIF_MESSAGE | NotifyAreaNative.NIF_ICON | NotifyAreaNative.NIF_TIP | NotifyAreaNative.NIF_SHOWTIP);
        NotifyArea.Write(data.szTip, tip);
        _added = NotifyAreaNative.Shell_NotifyIcon(NotifyAreaNative.NIM_ADD, ref data);
        if (_added)
        {
            data.uVersion = NotifyAreaNative.NOTIFYICON_VERSION_4;
            NotifyAreaNative.Shell_NotifyIcon(NotifyAreaNative.NIM_SETVERSION, ref data);
        }
        return _added;
    }

    public void SetTip(string tip)
    {
        if (!_added) return;
        var data = Data(NotifyAreaNative.NIF_TIP | NotifyAreaNative.NIF_SHOWTIP);
        NotifyArea.Write(data.szTip, tip);
        NotifyAreaNative.Shell_NotifyIcon(NotifyAreaNative.NIM_MODIFY, ref data);
    }

    public void ShowBalloon(string title, string text, NotifyAreaLevel level)
    {
        if (!_added) return;
        var data = Data(NotifyAreaNative.NIF_INFO);
        NotifyArea.Write(data.szInfoTitle, title);
        NotifyArea.Write(data.szInfo, text);
        data.dwInfoFlags = NotifyArea.BalloonFlags(level, _largeIcon != 0);
        if (level == NotifyAreaLevel.Info && _largeIcon != 0) data.hBalloonIcon = _largeIcon;
        NotifyAreaNative.Shell_NotifyIcon(NotifyAreaNative.NIM_MODIFY, ref data);
    }

    public int ShowMenu(IReadOnlyList<NotifyAreaMenuItem> rows, int x, int y, bool rightToLeft)
    {
        var menu = NotifyAreaNative.CreatePopupMenu();
        if (menu == 0) return 0;
        try
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.IsSeparator) NotifyAreaNative.AppendMenu(menu, NotifyAreaNative.MF_SEPARATOR, 0, null);
                else NotifyAreaNative.AppendMenu(menu, NotifyAreaNative.MF_STRING | (row.IsChecked ? NotifyAreaNative.MF_CHECKED : 0) | (row.IsEnabled ? 0 : NotifyAreaNative.MF_GRAYED), (nuint)(i + 1), row.Text);
            }
            // The menu closes on a press elsewhere only when its window is in front first and gets a message after.
            NotifyAreaNative.SetForegroundWindow(_window);
            var flags = NotifyAreaNative.TPM_RETURNCMD | NotifyAreaNative.TPM_NONOTIFY | NotifyAreaNative.TPM_RIGHTBUTTON | (rightToLeft ? NotifyAreaNative.TPM_LAYOUTRTL : 0);
            var picked = NotifyAreaNative.TrackPopupMenuEx(menu, flags, x, y, _window, 0);
            NotifyAreaNative.PostMessage(_window, NotifyAreaNative.WM_NULL, 0, 0);
            return picked;
        }
        finally
        {
            NotifyAreaNative.DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Raised = null;
        if (_added)
        {
            var data = Data(0);
            NotifyAreaNative.Shell_NotifyIcon(NotifyAreaNative.NIM_DELETE, ref data);
            _added = false;
        }
        s_icons.Remove(_window);
        NotifyAreaNative.DestroyWindow(_window);
        NotifyAreaNative.DestroyIcon(_smallIcon);
        if (_largeIcon != 0) NotifyAreaNative.DestroyIcon(_largeIcon);
    }

    private NotifyAreaNative.NotifyIconData Data(uint flags) => new()
    {
        cbSize = (uint)sizeof(NotifyAreaNative.NotifyIconData),
        hWnd = _window,
        uID = IconId,
        uFlags = flags,
        uCallbackMessage = CallbackMessage,
        hIcon = _smallIcon,
    };

    private nint Handle(nint hwnd, uint message, nint wParam, nint lParam)
    {
        if (message == CallbackMessage)
        {
            if (NotifyArea.Decode(wParam, lParam, Cursor) is { } e) Raised?.Invoke(e.Kind, e.X, e.Y);
            return 0;
        }
        if (s_taskbarCreated != 0 && message == s_taskbarCreated)
        {
            _added = false;
            Raised?.Invoke(NotifyAreaEvent.ShellRestarted, 0, 0);
            return 0;
        }
        return NotifyAreaNative.DefWindowProc(hwnd, message, wParam, lParam);
    }

    private static (int X, int Y)? Cursor() => NotifyAreaNative.GetCursorPos(out var p) ? (p.X, p.Y) : null;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam)
    {
        try
        {
            if (s_icons.TryGetValue(hwnd, out var icon)) return icon.Handle(hwnd, message, wParam, lParam);
        }
        catch (Exception)
        {
        }
        return NotifyAreaNative.DefWindowProc(hwnd, message, wParam, lParam);
    }
}

/// <summary>Source-generated imports for the notification area; every struct holds only blittable fields, its texts in inline arrays of UTF-16 units.</summary>
[SupportedOSPlatform("windows")]
internal static unsafe partial class NotifyAreaNative
{
    internal const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2, NIM_SETVERSION = 4;
    internal const uint NIF_MESSAGE = 0x01, NIF_ICON = 0x02, NIF_TIP = 0x04, NIF_INFO = 0x10, NIF_SHOWTIP = 0x80;
    internal const uint NOTIFYICON_VERSION_4 = 4;
    internal const uint WM_NULL = 0x0000, WM_APP = 0x8000;
    internal static readonly nint HWND_MESSAGE = -3;
    internal const uint MF_STRING = 0x0000, MF_GRAYED = 0x0001, MF_CHECKED = 0x0008, MF_SEPARATOR = 0x0800;
    internal const uint TPM_RIGHTBUTTON = 0x0002, TPM_NONOTIFY = 0x0080, TPM_RETURNCMD = 0x0100, TPM_LAYOUTRTL = 0x8000;
    internal const int SM_CXICON = 11, SM_CXSMICON = 49;

    [InlineArray(64)]
    internal struct Text64
    {
        private ushort _first;
    }

    [InlineArray(128)]
    internal struct Text128
    {
        private ushort _first;
    }

    [InlineArray(256)]
    internal struct Text256
    {
        private ushort _first;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NotifyIconData
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        public Text128 szTip;
        public uint dwState;
        public uint dwStateMask;
        public Text256 szInfo;
        public uint uVersion;
        public Text64 szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowClass
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public nint lpszMenuName;
        public nint lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandle(string? moduleName);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW")]
    internal static partial ushort RegisterClassEx(ref WindowClass windowClass);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateWindowEx(uint exStyle, string className, string? windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    internal static partial nint DefWindowProc(nint hwnd, uint message, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint RegisterWindowMessage(string name);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessage(nint hwnd, uint message, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    internal static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AppendMenu(nint menu, uint flags, nuint id, string? text);

    [LibraryImport("user32.dll")]
    internal static partial int TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint hwnd, nint tpmParams);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyMenu(nint menu);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out Point point);

    [LibraryImport("user32.dll")]
    internal static partial nint CreateIconFromResourceEx(byte* bits, uint size, [MarshalAs(UnmanagedType.Bool)] bool icon, uint version, int width, int height, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint icon);

    [LibraryImport("user32.dll")]
    internal static partial int GetSystemMetrics(int index);

    /// <summary>An HICON of about <paramref name="size"/> pixels from an .ico's bytes; 0 when there is no usable picture.</summary>
    internal static nint CreateIcon(byte[] ico, int size)
    {
        if (size <= 0 || NotifyArea.Pick(ico, size) is not { } pick) return 0;
        fixed (byte* bits = &ico[pick.Offset])
            return CreateIconFromResourceEx(bits, (uint)pick.Bytes, true, 0x00030000, size, size, 0);
    }
}
