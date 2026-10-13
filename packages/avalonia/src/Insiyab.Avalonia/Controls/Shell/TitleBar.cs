using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using static Insiyab.Avalonia.Controls.TitleBarNative;

namespace Insiyab.Avalonia.Controls;

/// <summary>A title bar the app draws itself on Windows, so the caption buttons follow the window's direction and sit in the app's top bar.</summary>
public static class TitleBar
{
    /// <summary>On a Window: on Windows the client area takes over the title bar (place a CaptionButtons and mark drag areas); elsewhere nothing changes.</summary>
    public static readonly AttachedProperty<bool> ExtendsProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("Extends", typeof(TitleBar));

    /// <summary>On any control: dragging it moves the window and a double-click maximises; focusable controls inside stay clickable. A right-click opens nothing.</summary>
    public static readonly AttachedProperty<bool> IsDragAreaProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsDragArea", typeof(TitleBar));

    /// <summary>On the app icon (the brand mark) in the title bar: a right-click opens the window's system menu. The icon is not a drag handle.</summary>
    public static readonly AttachedProperty<bool> IsSystemMenuAreaProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsSystemMenuArea", typeof(TitleBar));

    internal const double BandHeight = 40;

    private static readonly AttachedProperty<MenuHook?> HookProperty =
        AvaloniaProperty.RegisterAttached<Window, MenuHook?>("Hook", typeof(TitleBar));

    internal static readonly AttachedProperty<CaptionButtons?> CaptionsProperty =
        AvaloniaProperty.RegisterAttached<Window, CaptionButtons?>("Captions", typeof(TitleBar));

    private static readonly ControlTheme Bare = new(typeof(WindowDrawnDecorations))
    {
        Setters =
        {
            new Setter(WindowDrawnDecorations.TemplateProperty, new BareDecorations()),
            new Setter(WindowDrawnDecorations.DefaultTitleBarHeightProperty, BandHeight),
        },
    };

    public static bool GetExtends(Window window) => window.GetValue(ExtendsProperty);
    public static void SetExtends(Window window, bool value) => window.SetValue(ExtendsProperty, value);
    public static bool GetIsDragArea(Control control) => control.GetValue(IsDragAreaProperty);
    public static void SetIsDragArea(Control control, bool value) => control.SetValue(IsDragAreaProperty, value);
    public static bool GetIsSystemMenuArea(Control control) => control.GetValue(IsSystemMenuAreaProperty);
    public static void SetIsSystemMenuArea(Control control, bool value) => control.SetValue(IsSystemMenuAreaProperty, value);

    /// <summary>True while the window draws its own title bar: Extends is on, the OS is Windows and Avalonia has extended it.</summary>
    public static bool IsActive(Window window) => GetExtends(window) && OperatingSystem.IsWindows() && window.IsExtendedIntoWindowDecorations;

    static TitleBar()
    {
        ExtendsProperty.Changed.AddClassHandler<Window>((window, e) => Apply(window, e.GetNewValue<bool>()));
        IsDragAreaProperty.Changed.AddClassHandler<Control>((control, e) =>
            WindowDecorationProperties.SetElementRole(control, e.GetNewValue<bool>() ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.None));
        IsSystemMenuAreaProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            var on = e.GetNewValue<bool>();
            WindowDecorationProperties.SetElementRole(control, on ? WindowDecorationsElementRole.User : WindowDecorationsElementRole.None);
            control.PointerReleased -= OnIconReleased;
            if (on) control.PointerReleased += OnIconReleased;
        });
    }

    private static void Apply(Window window, bool on)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (on)
        {
            window.ExtendClientAreaToDecorationsHint = true;
            window.ExtendClientAreaTitleBarHeightHint = BandHeight;
            window.WindowDecorationsTheme = Bare;
            if (window.GetValue(HookProperty) is null)
            {
                var hook = new MenuHook(window);
                window.SetValue(HookProperty, hook);
                Win32Properties.AddWndProcHookCallback(window, hook.Proc);
            }
        }
        else
        {
            window.ClearValue(Window.ExtendClientAreaToDecorationsHintProperty);
            window.ClearValue(Window.ExtendClientAreaTitleBarHeightHintProperty);
            window.ClearValue(Window.WindowDecorationsThemeProperty);
            if (window.GetValue(HookProperty) is { } hook)
            {
                Win32Properties.RemoveWndProcHookCallback(window, hook.Proc);
                window.ClearValue(HookProperty);
            }
        }
    }

    internal static WindowDecorationsElementRole RoleAt(Visual? visual)
    {
        for (; visual is not null; visual = visual.GetVisualParent())
        {
            var role = WindowDecorationProperties.GetElementRole(visual);
            if (role != WindowDecorationsElementRole.None) return role;
        }
        return WindowDecorationsElementRole.None;
    }

    /// <summary>Writes the caption buttons' boxes into a TITLEBARINFOEX, the band from the window's top edge to the island's foot as its title bar.</summary>
    internal static void FillTitleBarInfo(nint info, CaptionButtons caps, RECT frame)
    {
        WriteTitleBarRect(info, new RECT { Left = frame.Left, Top = frame.Top, Right = frame.Right, Bottom = ScreenRect(caps).Bottom });
        foreach (var (index, button) in new[] { (TitleBarMinimize, caps.MinimizeButton), (TitleBarMaximize, caps.MaximizeButton), (TitleBarClose, caps.CloseButton) })
        {
            var shown = button is { IsEffectivelyVisible: true };
            WriteTitleBarPart(info, index, shown ? ScreenRect(button!) : default, shown);
        }
    }

    /// <summary>A control's box on the screen in device pixels, whichever way the window flows.</summary>
    internal static RECT ScreenRect(Control control)
    {
        var a = control.PointToScreen(default);
        var b = control.PointToScreen(new Point(control.Bounds.Width, control.Bounds.Height));
        return new RECT { Left = Math.Min(a.X, b.X), Top = Math.Min(a.Y, b.Y), Right = Math.Max(a.X, b.X), Bottom = Math.Max(a.Y, b.Y) };
    }

    private static void OnIconReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right || sender is not Control control) return;
        if (TopLevel.GetTopLevel(control) is not Window window || !IsActive(window) || window.GetValue(HookProperty) is not { } hook) return;
        GetCursorPos(out var p);
        hook.ShowMenu(p.X, p.Y);
        e.Handled = true;
    }

    private sealed class BareDecorations : IWindowDrawnDecorationsTemplate
    {
        public TemplateResult<WindowDrawnDecorationsContent> Build() => new(new WindowDrawnDecorationsContent(), new NameScope());
        object? ITemplate.Build() => Build();
    }

    internal sealed class MenuHook
    {
        private readonly Window _window;
        private long _closedAt;
        public readonly Win32Properties.CustomWndProcHookCallback Proc;

        public MenuHook(Window window)
        {
            _window = window;
            Proc = WndProc;
        }

        private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
        {
            if (!IsActive(_window)) return 0;
            if (msg == WM_NCRBUTTONUP && (int)wParam == HTCAPTION)
                handled = true;
            else if (msg == WM_CONTEXTMENU && lParam != -1 && (int)SendMessageW(hWnd, WM_NCHITTEST, 0, lParam) == HTCAPTION)
                handled = true;
            else if (msg == WM_SYSCOMMAND && ((int)wParam & 0xFFF0) == SC_KEYMENU && (int)lParam == ' ')
            {
                var corner = StartCorner(hWnd);
                ShowMenu(corner.X, corner.Y);
                handled = true;
            }
            else if (msg == WM_GETTITLEBARINFOEX && lParam != 0 && _window.GetValue(CaptionsProperty) is { IsVisible: true } caps)
            {
                // Windows' own answer names no caption buttons for a drawn title bar, so this reports ours, where Snap layouts can find the maximise button
                var result = DefWindowProcW(hWnd, msg, wParam, lParam);
                GetWindowRect(hWnd, out var frame);
                FillTitleBarInfo(lParam, caps, frame);
                handled = true;
                return result;
            }
            return 0;
        }

        private POINT StartCorner(nint hWnd)
        {
            GetClientRect(hWnd, out var r);
            var p = new POINT { X = _window.FlowDirection == FlowDirection.RightToLeft ? r.Right : 0, Y = (int)(BandHeight * _window.RenderScaling) };
            ClientToScreen(hWnd, ref p);
            return p;
        }

        internal void ShowMenu(int x, int y)
        {
            if (Environment.TickCount64 - _closedAt < 300) return;
            var hWnd = _window.TryGetPlatformHandle()?.Handle ?? 0;
            if (hWnd == 0) return;
            var menu = GetSystemMenu(hWnd, 0);
            var max = _window.WindowState == WindowState.Maximized;
            void Item(uint id, bool on) => EnableMenuItem(menu, id, on ? MF_ENABLED : MF_GRAYED_DISABLED);
            Item(SC_RESTORE, max);
            Item(SC_MOVE, !max);
            Item(SC_SIZE, !max && _window.CanResize);
            Item(SC_MINIMIZE, _window.CanMinimize);
            Item(SC_MAXIMIZE, !max && _window.CanMaximize && _window.CanResize);
            Item(SC_CLOSE, true);
            SetMenuDefaultItem(menu, SC_CLOSE, 0);
            var flags = TPM_RETURNCMD | TPM_RIGHTBUTTON | (_window.FlowDirection == FlowDirection.RightToLeft ? TPM_RIGHTALIGN : 0u);
            var command = TrackPopupMenu(menu, flags, x, y, 0, hWnd, 0);
            _closedAt = Environment.TickCount64;
            if (command != 0) PostMessageW(hWnd, WM_SYSCOMMAND, command, 0);
        }
    }
}
