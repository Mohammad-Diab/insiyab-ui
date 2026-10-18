using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

internal static unsafe class TaskbarNative
{
    internal const uint CLSCTX_INPROC_SERVER = 1, MSGFLT_ALLOW = 1, ICON_VERSION = 0x00030000;
    internal const int SM_CXSMICON = 49, SlotHrInit = 3, SlotSetOverlayIcon = 18, SlotRelease = 2;

    private static readonly Guid CLSID_TaskbarList = new("56FDF344-FD6D-11d0-958A-006097C9A090");
    private static readonly Guid IID_ITaskbarList3 = new("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF");

    private static bool _broken;
    private static uint _buttonCreated;

    /// <summary>The shell's taskbar list, made once on the first real badge and 0 until then.</summary>
    internal static nint List;

    [DllImport("ole32.dll")] internal static extern int CoCreateInstance(ref Guid clsid, nint outer, uint context, ref Guid iid, out nint instance);
    [DllImport("user32.dll")] internal static extern nint CreateIconFromResourceEx(byte[] bits, uint size, int icon, uint version, int width, int height, uint flags);
    [DllImport("user32.dll")] internal static extern int DestroyIcon(nint icon);
    [DllImport("user32.dll")] internal static extern uint RegisterWindowMessageW(char* name);
    [DllImport("user32.dll")] internal static extern int ChangeWindowMessageFilterEx(nint hWnd, uint message, uint action, nint filter);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint hWnd);
    [DllImport("user32.dll")] internal static extern int GetSystemMetricsForDpi(int index, uint dpi);

    /// <summary>The message the shell sends when a window's taskbar button appears, and again after Explorer restarts.</summary>
    internal static uint ButtonCreated
    {
        get
        {
            if (_buttonCreated == 0)
                fixed (char* name = "TaskbarButtonCreated")
                    _buttonCreated = RegisterWindowMessageW(name);
            return _buttonCreated;
        }
    }

    /// <summary>Creates the taskbar list once; a failure is final for the process, so nothing tries again.</summary>
    internal static bool EnsureList()
    {
        if (List != 0) return true;
        if (_broken) return false;
        try
        {
            var clsid = CLSID_TaskbarList;
            var iid = IID_ITaskbarList3;
            if (CoCreateInstance(ref clsid, 0, CLSCTX_INPROC_SERVER, ref iid, out var list) < 0 || list == 0)
            {
                _broken = true;
                return false;
            }
            if (((delegate* unmanaged[Stdcall]<nint, int>)Slot(list, SlotHrInit))(list) < 0)
            {
                ((delegate* unmanaged[Stdcall]<nint, uint>)Slot(list, SlotRelease))(list);
                _broken = true;
                return false;
            }
            List = list;
            return true;
        }
        catch (Exception)
        {
            _broken = true;
            return false;
        }
    }

    /// <summary>ITaskbarList3::SetOverlayIcon: an icon of 0 clears the overlay; gives the HRESULT.</summary>
    internal static int SetOverlay(nint hwnd, nint icon, string? description)
    {
        fixed (char* text = description)
            return ((delegate* unmanaged[Stdcall]<nint, nint, nint, char*, int>)Slot(List, SlotSetOverlayIcon))(List, hwnd, icon, text);
    }

    /// <summary>The small-icon size the shell shows on this window's monitor, in device pixels.</summary>
    internal static int IconSize(nint hwnd, double scaling)
    {
        try
        {
            var dpi = GetDpiForWindow(hwnd);
            var size = dpi == 0 ? 0 : GetSystemMetricsForDpi(SM_CXSMICON, dpi);
            if (size > 0) return size;
        }
        catch (Exception)
        {
        }
        return BadgeIcon.Pixels(scaling);
    }

    internal static nint IconFrom(byte[] png, int size) => CreateIconFromResourceEx(png, (uint)png.Length, 1, ICON_VERSION, size, size, 0);

    private static void* Slot(nint com, int index) => (*(void***)com)[index];
}

/// <summary>The Windows taskbar overlay icon, one per window button.</summary>
internal sealed class WindowsBadge : IBadgeSink
{
    private readonly ConditionalWeakTable<TopLevel, Entry> _entries = new();

    public void Apply(TopLevel top, string? text, string? label)
    {
        if (!OperatingSystem.IsWindows() || top.TryGetPlatformHandle() is not { Handle: not 0 } handle || handle.HandleDescriptor != "HWND") return;
        try
        {
            var found = _entries.TryGetValue(top, out var known);
            if (text is null && !found) return;
            if (!TaskbarNative.EnsureList()) return;
            var entry = known ?? Make(top, handle.Handle);
            entry.Hwnd = handle.Handle;
            var old = entry.Icon;
            if (text is null)
            {
                entry.Icon = 0;
                entry.Label = null;
                TaskbarNative.SetOverlay(entry.Hwnd, 0, null);
            }
            else
            {
                var size = TaskbarNative.IconSize(entry.Hwnd, top.RenderScaling);
                var icon = TaskbarNative.IconFrom(BadgeIcon.Png(text, size, Taskbar.FillFor(top), Taskbar.FontFor(top)), size);
                if (icon == 0) return;
                entry.Icon = icon;
                entry.Label = label ?? text;
                TaskbarNative.SetOverlay(entry.Hwnd, icon, entry.Label);
            }
            if (old != 0) TaskbarNative.DestroyIcon(old);
        }
        catch (Exception)
        {
        }
    }

    public void Forget(TopLevel top)
    {
        if (!_entries.TryGetValue(top, out var entry)) return;
        _entries.Remove(top);
        try
        {
            if (entry.Proc is not null) Win32Properties.RemoveWndProcHookCallback(top, entry.Proc);
            if (entry.Icon != 0) TaskbarNative.DestroyIcon(entry.Icon);
        }
        catch (Exception)
        {
        }
    }

    // The first real badge on a window listens for its taskbar button, which the shell makes after the window shows and again after Explorer restarts.
    private Entry Make(TopLevel top, nint hwnd)
    {
        var entry = new Entry { Hwnd = hwnd };
        entry.Proc = (nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled) =>
        {
            try
            {
                if (msg == TaskbarNative.ButtonCreated && entry.Icon != 0) TaskbarNative.SetOverlay(entry.Hwnd, entry.Icon, entry.Label);
            }
            catch (Exception)
            {
            }
            return 0;
        };
        _entries.Add(top, entry);
        TaskbarNative.ChangeWindowMessageFilterEx(entry.Hwnd, TaskbarNative.ButtonCreated, TaskbarNative.MSGFLT_ALLOW, 0);
        Win32Properties.AddWndProcHookCallback(top, entry.Proc);
        return entry;
    }

    private sealed class Entry
    {
        public nint Hwnd, Icon;
        public string? Label;
        public Win32Properties.CustomWndProcHookCallback? Proc;
    }
}
