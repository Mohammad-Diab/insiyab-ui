using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The icon a balloon shows: the app's own icon for <see cref="Info"/>, the system's warning or error icon for the others.</summary>
public enum NotifyAreaLevel
{
    Info,
    Warning,
    Error,
}

/// <summary>A row of <see cref="NotifyAreaIcon.Menu"/>: a text the person picks (with a tick or greyed out), or a separator line.</summary>
public sealed class NotifyAreaMenuItem
{
    public NotifyAreaMenuItem()
    {
    }

    public NotifyAreaMenuItem(string text, EventHandler? click = null)
    {
        Text = text;
        if (click is not null) Click += click;
    }

    /// <summary>A separator line between rows.</summary>
    public static NotifyAreaMenuItem Separator() => new() { IsSeparator = true };

    public string Text { get; set; } = "";

    public bool IsChecked { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsSeparator { get; set; }

    /// <summary>Anything the app keeps with the row.</summary>
    public object? Tag { get; set; }

    /// <summary>Raised on the UI thread when the row is picked.</summary>
    public event EventHandler? Click;

    internal void RaiseClick() => Click?.Invoke(this, EventArgs.Empty);
}

/// <summary>The data of <see cref="NotifyAreaIcon.BalloonClicked"/>: the tag the balloon was shown with.</summary>
public sealed class NotifyAreaBalloonEventArgs : EventArgs
{
    internal NotifyAreaBalloonEventArgs(object? tag) => Tag = tag;

    public object? Tag { get; }
}

/// <summary>An icon in the Windows notification area with what Avalonia's <c>TrayIcon</c> lacks: balloons (Windows 10 and 11 show them as notifications) with a click that says which one, a native menu with ticks and greyed rows laid out right to left for Arabic, and the icon added again when Explorer restarts. Its events come on the UI thread. On other systems it does nothing and never fails (<see cref="IsAdded"/> stays false); use Avalonia's <c>TrayIcon</c> there.</summary>
public sealed class NotifyAreaIcon : IDisposable
{
    // Windows keeps a tip to 127 characters, a balloon's title to 63 and its text to 255.
    internal const int TipLength = 127, TitleLength = 63, TextLength = 255;

    private static readonly ConditionalWeakTable<Window, Hidden> s_hidden = new();

    private readonly INotifyAreaShell? _shell;
    private string _tip;
    private bool _disposed;
    private object? _lastTag;

    /// <summary>Makes the shell's side of an icon from .ico bytes; null where there is none. Replaced by the tests.</summary>
    internal static Func<byte[], INotifyAreaShell?> MakeShell { get; set; } = ico => OperatingSystem.IsWindows() ? WindowsNotifyArea.TryMake(ico) : null;

    /// <summary>Adds the icon, made from the bytes of an .ico file (the picture nearest each size Windows asks for is taken), with <paramref name="tip"/> as its tooltip. Make it on the UI thread.</summary>
    public NotifyAreaIcon(byte[] ico, string tip)
    {
        ArgumentNullException.ThrowIfNull(ico);
        Dispatcher.UIThread.VerifyAccess();
        _tip = tip ?? "";
        try
        {
            _shell = MakeShell(ico);
        }
        catch (Exception)
        {
            _shell = null;
        }
        if (_shell is null) return;
        _shell.Raised = OnShell;
        IsAdded = _shell.Add(Cut(_tip, TipLength));
    }

    /// <summary>True where the system has a notification area this icon can use: Windows.</summary>
    public static bool IsSupported => OperatingSystem.IsWindows();

    /// <summary>True while the icon shows in the notification area; false off Windows, while the shell is not ready, and after <see cref="Dispose"/>. Hide a window to the tray only while it is true.</summary>
    public bool IsAdded { get; private set; }

    /// <summary>The tooltip; cut at 127 characters.</summary>
    public string Tip
    {
        get => _tip;
        set
        {
            _tip = value ?? "";
            if (IsAdded) _shell?.SetTip(Cut(_tip, TipLength));
        }
    }

    /// <summary>The rows of the menu a right click (or the menu key) opens; empty, no menu opens.</summary>
    public IList<NotifyAreaMenuItem> Menu { get; } = new List<NotifyAreaMenuItem>();

    /// <summary>The menu's direction; null follows <see cref="InsiyabTheme.Language"/>, right to left for Arabic.</summary>
    public FlowDirection? MenuDirection { get; set; }

    /// <summary>A click on the icon, or Enter on it from the keyboard.</summary>
    public event EventHandler? Activated;

    /// <summary>Raised before the menu opens, to write its rows again (a new language, a tick).</summary>
    public event EventHandler? MenuOpening;

    /// <summary>A click on the last balloon shown; its args carry that balloon's tag.</summary>
    public event EventHandler<NotifyAreaBalloonEventArgs>? BalloonClicked;

    /// <summary>The icon was added or taken away by the shell, as when Explorer restarts.</summary>
    public event EventHandler? IsAddedChanged;

    /// <summary>Shows a balloon from the icon (a notification on Windows 10 and 11) with the icon <paramref name="level"/> picks; nothing while the icon is not added. Windows keeps quiet hours. <paramref name="tag"/> comes back with <see cref="BalloonClicked"/>.</summary>
    public void ShowBalloon(string title, string text, NotifyAreaLevel level = NotifyAreaLevel.Info, object? tag = null)
    {
        if (!IsAdded || _shell is null) return;
        _lastTag = tag;
        _shell.ShowBalloon(Cut(title ?? "", TitleLength), Cut(text ?? "", TextLength), level);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var was = IsAdded;
        IsAdded = false;
        if (_shell is not null)
        {
            _shell.Raised = null;
            try
            {
                _shell.Dispose();
            }
            catch (Exception)
            {
            }
        }
        if (was) IsAddedChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Takes a window off the screen and the taskbar while the app keeps running in the tray: minimised with no taskbar button, never <c>Hide()</c>, which can bring a window back as a blank frame after a long time hidden.</summary>
    public static void HideToTray(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (window.WindowState != WindowState.Minimized)
        {
            s_hidden.Remove(window);
            s_hidden.Add(window, new Hidden(window.WindowState));
        }
        window.ShowInTaskbar = false;
        window.WindowState = WindowState.Minimized;
    }

    /// <summary>Brings back a window <see cref="HideToTray"/> took away, in the state it had (normal or maximised), and activates it.</summary>
    public static void RestoreFromTray(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.ShowInTaskbar = true;
        window.WindowState = s_hidden.TryGetValue(window, out var was) ? was.State : WindowState.Normal;
        if (!window.IsVisible) window.Show();
        window.Activate();
    }

    /// <summary>A text cut to <paramref name="max"/> characters, never between the halves of a surrogate pair.</summary>
    internal static string Cut(string text, int max)
    {
        if (text.Length <= max) return text;
        var end = max;
        if (end > 0 && char.IsHighSurrogate(text[end - 1])) end--;
        return text[..end];
    }

    internal bool MenuRightToLeft => MenuDirection is { } d ? d == FlowDirection.RightToLeft : InsiyabTheme.Language is { } l && Words.IsArabicCode(l);

    private void OnShell(NotifyAreaEvent kind, int x, int y)
    {
        if (_disposed) return;
        switch (kind)
        {
            case NotifyAreaEvent.Activated:
                Activated?.Invoke(this, EventArgs.Empty);
                break;
            case NotifyAreaEvent.Menu:
                OpenMenu(x, y);
                break;
            case NotifyAreaEvent.BalloonClicked:
                BalloonClicked?.Invoke(this, new NotifyAreaBalloonEventArgs(_lastTag));
                break;
            case NotifyAreaEvent.ShellRestarted:
                var was = IsAdded;
                IsAdded = _shell is not null && _shell.Add(Cut(_tip, TipLength));
                if (was != IsAdded || IsAdded) IsAddedChanged?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void OpenMenu(int x, int y)
    {
        MenuOpening?.Invoke(this, EventArgs.Empty);
        if (_shell is null || Menu.Count == 0) return;
        var rows = Menu.ToArray();
        var picked = _shell.ShowMenu(rows, x, y, MenuRightToLeft);
        if (picked >= 1 && picked <= rows.Length && rows[picked - 1] is { IsSeparator: false, IsEnabled: true } row) row.RaiseClick();
    }

    private sealed record Hidden(WindowState State);
}

internal enum NotifyAreaEvent
{
    Activated,
    Menu,
    BalloonClicked,
    ShellRestarted,
}

/// <summary>The shell's side of one icon: the Win32 notification area, or a recorder in the tests.</summary>
internal interface INotifyAreaShell : IDisposable
{
    /// <summary>Where the shell's events go: what happened and, for the menu, the screen point.</summary>
    Action<NotifyAreaEvent, int, int>? Raised { get; set; }

    /// <summary>Adds the icon with its tip; false when the shell refused.</summary>
    bool Add(string tip);

    void SetTip(string tip);

    void ShowBalloon(string title, string text, NotifyAreaLevel level);

    /// <summary>Shows the menu at a screen point and gives the picked row's number from 1, or 0.</summary>
    int ShowMenu(IReadOnlyList<NotifyAreaMenuItem> rows, int x, int y, bool rightToLeft);
}
