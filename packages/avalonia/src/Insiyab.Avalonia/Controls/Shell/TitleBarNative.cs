using System.Runtime.InteropServices;

namespace Insiyab.Avalonia.Controls;

internal static class TitleBarNative
{
    internal const uint WM_NCHITTEST = 0x0084, WM_NCRBUTTONUP = 0x00A5, WM_CONTEXTMENU = 0x007B, WM_SYSCOMMAND = 0x0112, WM_GETTITLEBARINFOEX = 0x033F;
    internal const int STATE_SYSTEM_INVISIBLE = 0x8000, TitleBarMinimize = 2, TitleBarMaximize = 3, TitleBarClose = 5;
    internal const int HTCAPTION = 2, SC_KEYMENU = 0xF100;
    internal const uint SC_SIZE = 0xF000, SC_MOVE = 0xF010, SC_MINIMIZE = 0xF020, SC_MAXIMIZE = 0xF030, SC_CLOSE = 0xF060, SC_RESTORE = 0xF120;
    internal const uint MF_ENABLED = 0x0, MF_GRAYED_DISABLED = 0x3, TPM_RIGHTBUTTON = 0x0002, TPM_RIGHTALIGN = 0x0008, TPM_RETURNCMD = 0x0100;

    [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] internal static extern nint GetSystemMenu(nint hWnd, int revert);
    [DllImport("user32.dll")] internal static extern int EnableMenuItem(nint hMenu, uint id, uint flags);
    [DllImport("user32.dll")] internal static extern int SetMenuDefaultItem(nint hMenu, uint item, uint byPosition);
    [DllImport("user32.dll")] internal static extern int TrackPopupMenu(nint hMenu, uint flags, int x, int y, int reserved, nint hWnd, nint rect);
    [DllImport("user32.dll")] internal static extern int PostMessageW(nint hWnd, uint msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] internal static extern nint SendMessageW(nint hWnd, uint msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] internal static extern int GetCursorPos(out POINT point);
    [DllImport("user32.dll")] internal static extern int GetClientRect(nint hWnd, out RECT rect);
    [DllImport("user32.dll")] internal static extern int ClientToScreen(nint hWnd, ref POINT point);
    [DllImport("user32.dll")] internal static extern int GetWindowRect(nint hWnd, out RECT rect);
    [DllImport("user32.dll")] internal static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    // TITLEBARINFOEX: cbSize, rcTitleBar, rgstate[6], rgrect[6]
    internal static void WriteTitleBarRect(nint info, RECT r) => WriteRect(info, 4, r);

    internal static void WriteTitleBarPart(nint info, int index, RECT r, bool visible)
    {
        Marshal.WriteInt32(info, 20 + 4 * index, visible ? 0 : STATE_SYSTEM_INVISIBLE);
        WriteRect(info, 44 + 16 * index, r);
    }

    private static void WriteRect(nint info, int offset, RECT r)
    {
        Marshal.WriteInt32(info, offset, r.Left);
        Marshal.WriteInt32(info, offset + 4, r.Top);
        Marshal.WriteInt32(info, offset + 8, r.Right);
        Marshal.WriteInt32(info, offset + 12, r.Bottom);
    }
}
