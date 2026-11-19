using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using static Insiyab.Avalonia.Controls.TitleBarNative;

namespace Insiyab.Avalonia.Controls;

/// <summary>Where a <see cref="PictureWindow"/> stood: its top-left corner in screen pixels and its size in device-independent units.</summary>
public sealed record PicturePlace(int X, int Y, double Width, double Height);

/// <summary>A small window kept over the others, for a picture watched beside other work (a video, a call, a meeting): picture in picture on the desktop. It has no title bar and no buttons of its own; on Windows it keeps the system's frame (its rounded corners on Windows 11, its shadow, the resize edges). Any press that no control takes drags it; its edges resize it keeping <see cref="AspectRatio"/> (the picture's width over its height) as they are dragged. <see cref="Picture"/> fills it; <see cref="Overlay"/> (the app's buttons) shows over the picture while the pointer is on the window, fading unless <see cref="Motion"/> is still, and a tap shows it under a finger. With <see cref="PlaceKey"/> it opens where it was last left at the size it had (<see cref="LoadPlace"/>, <see cref="SavePlace"/>; kept for the run when the app keeps nothing); first, or when that place is off every screen, at the working area's bottom corner on the reading end. It never maximises. Put the content in <see cref="Picture"/>, not in <c>Content</c>. <see cref="ShowNote"/> shows a short note at its foot for a few seconds whatever the pointer does (a message that came, say), then fades it (at once when still); it sits over the picture, under the overlay, rising by <see cref="NoteLift"/> while the overlay shows so it never hides the overlay's own foot.</summary>
public class PictureWindow : Window
{
    /// <summary>The picture's width over its height; the window's inside keeps it while resized. 0: any shape.</summary>
    public static readonly StyledProperty<double> AspectRatioProperty = AvaloniaProperty.Register<PictureWindow, double>(nameof(AspectRatio), 16d / 9);

    /// <summary>What fills the window (the video).</summary>
    public static readonly StyledProperty<object?> PictureProperty = AvaloniaProperty.Register<PictureWindow, object?>(nameof(Picture));

    /// <summary>What shows over the picture while the pointer is on the window (the app's buttons).</summary>
    public static readonly StyledProperty<object?> OverlayProperty = AvaloniaProperty.Register<PictureWindow, object?>(nameof(Overlay));

    /// <summary>The name its size and place are kept under; null: it opens at the default place each time.</summary>
    public static readonly StyledProperty<string?> PlaceKeyProperty = AvaloniaProperty.Register<PictureWindow, string?>(nameof(PlaceKey));

    /// <summary>Its width the first time (device-independent units).</summary>
    public static readonly StyledProperty<double> DefaultWidthProperty = AvaloniaProperty.Register<PictureWindow, double>(nameof(DefaultWidth), 384);

    /// <summary>The overlay shows now.</summary>
    public static readonly DirectProperty<PictureWindow, bool> IsOverlayShownProperty =
        AvaloniaProperty.RegisterDirect<PictureWindow, bool>(nameof(IsOverlayShown), w => w.IsOverlayShown);

    /// <summary>How far the note rises while the overlay shows (the height the overlay's own foot takes); 0: it stays at the foot.</summary>
    public static readonly StyledProperty<double> NoteLiftProperty = AvaloniaProperty.Register<PictureWindow, double>(nameof(NoteLift));

    /// <summary>A note shows now (<see cref="ShowNote"/>), until its time is up or <see cref="HideNote"/>.</summary>
    public static readonly DirectProperty<PictureWindow, bool> IsNoteShownProperty =
        AvaloniaProperty.RegisterDirect<PictureWindow, bool>(nameof(IsNoteShown), w => w.IsNoteShown);

    /// <summary>How long a note shows when <see cref="ShowNote"/> is given no time.</summary>
    public static TimeSpan NoteTime { get; } = TimeSpan.FromSeconds(5);

    /// <summary>The gap between the note and the window's edges.</summary>
    internal const double NoteGap = 8;

    /// <summary>The gap to the working area's edges at the default place.</summary>
    internal const double EdgeGap = 24;

    private static readonly Dictionary<string, PicturePlace> Kept = new(StringComparer.Ordinal);

    private readonly ContentPresenter _picture = new();
    private readonly ContentPresenter _overlay = new() { Opacity = 0, IsHitTestVisible = false };
    private readonly ContentPresenter _note = new()
    {
        Opacity = 0,
        IsHitTestVisible = false,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Bottom,
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
        Margin = new Thickness(NoteGap),
    };
    private readonly global::Avalonia.Threading.DispatcherTimer _noteTimer = new();
    private int _noteTurn;  // the note shown last (a fading one's content is cleared only if no newer one came)
    private bool _noteShown;
    private SizingHook? _hook;
    private bool _overlayShown;
    private bool _placed;

    public PictureWindow()
    {
        Topmost = true;
        CanMaximize = false;
        MinWidth = 192;
        MinHeight = 108;
        Background = Brushes.Black;
        WindowStartupLocation = WindowStartupLocation.Manual;
        _picture[!ContentPresenter.ContentProperty] = this[!PictureProperty];
        _overlay[!ContentPresenter.ContentProperty] = this[!OverlayProperty];
        Content = new Panel { Children = { _picture, _note, _overlay } };
        _noteTimer.Tick += (_, _) => HideNote();
        TitleBar.SetExtends(this, true);
        if (Os.IsWindows)
        {
            _hook = new SizingHook(this);
            Win32Properties.AddWndProcHookCallback(this, _hook.Proc);
        }
        AddHandler(PointerPressedEvent, OnPressed, global::Avalonia.Interactivity.RoutingStrategies.Bubble);
        PointerEntered += (_, e) => ShowOverlay(e.Pointer.Type != PointerType.Touch || _overlayShown);
        PointerMoved += (_, e) =>
        {
            if (e.Pointer.Type != PointerType.Touch)
                ShowOverlay(true);
        };
        PointerExited += (_, e) =>
        {
            if (e.Pointer.Type != PointerType.Touch)
                ShowOverlay(false);
        };
        Closing += (_, _) => Keep();
    }

    public double AspectRatio { get => GetValue(AspectRatioProperty); set => SetValue(AspectRatioProperty, value); }
    public object? Picture { get => GetValue(PictureProperty); set => SetValue(PictureProperty, value); }
    public object? Overlay { get => GetValue(OverlayProperty); set => SetValue(OverlayProperty, value); }
    public string? PlaceKey { get => GetValue(PlaceKeyProperty); set => SetValue(PlaceKeyProperty, value); }
    public double DefaultWidth { get => GetValue(DefaultWidthProperty); set => SetValue(DefaultWidthProperty, value); }

    public bool IsOverlayShown
    {
        get => _overlayShown;
        private set => SetAndRaise(IsOverlayShownProperty, ref _overlayShown, value);
    }

    public double NoteLift { get => GetValue(NoteLiftProperty); set => SetValue(NoteLiftProperty, value); }

    public bool IsNoteShown
    {
        get => _noteShown;
        private set => SetAndRaise(IsNoteShownProperty, ref _noteShown, value);
    }

    /// <summary>The note showing (or fading) now; null when none.</summary>
    public object? Note => _note.Content;

    /// <summary>Reads a kept place (the app's own store); null: none. Unset, places are kept for the run only.</summary>
    public static Func<string, PicturePlace?>? LoadPlace { get; set; }

    /// <summary>Keeps a place when a window with a key closes (the app's own store). Unset, places are kept for the run only.</summary>
    public static Action<string, PicturePlace>? SavePlace { get; set; }

    /// <summary>Shows or hides the overlay (fading over 150 ms unless motion is still).</summary>
    public void ShowOverlay(bool shown)
    {
        _overlay.Transitions = Fading(150);
        _overlay.Opacity = shown ? 1 : 0;
        _overlay.IsHitTestVisible = shown;
        IsOverlayShown = shown;
        PlaceNote();
    }

    /// <summary>Shows <paramref name="content"/> at the foot over the picture for <paramref name="time"/> (<see cref="NoteTime"/> by default), whatever the pointer does; a newer note takes its place and its time starts again. It fades in over 150 ms and out over 300 ms, at once when motion is still.</summary>
    public void ShowNote(object? content, TimeSpan? time = null)
    {
        if (content is null)
        {
            HideNote();
            return;
        }
        _noteTurn++;
        _note.Transitions = Fading(150);
        _note.Content = content;
        _note.Opacity = 1;
        IsNoteShown = true;
        PlaceNote();
        _noteTimer.Stop();
        _noteTimer.Interval = time ?? NoteTime;
        _noteTimer.Start();
    }

    /// <summary>The note goes now (fading over 300 ms unless motion is still).</summary>
    public void HideNote()
    {
        _noteTimer.Stop();
        if (!_noteShown && _note.Content is null)
            return;
        IsNoteShown = false;
        var fade = Fading(300);
        _note.Transitions = fade;
        _note.Opacity = 0;
        if (fade is null)
        {
            _note.Content = null;
            return;
        }
        // its content cleared once it faded, unless a newer note came meanwhile
        var turn = _noteTurn;
        global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            if (turn == _noteTurn && !_noteShown)
                _note.Content = null;
        }, TimeSpan.FromMilliseconds(320));
    }

    // the note's place: at the foot, risen over the overlay's own foot while the overlay shows
    private void PlaceNote() => _note.Margin = new Thickness(NoteGap, NoteGap, NoteGap, NoteGap + (_overlayShown ? Math.Max(0, NoteLift) : 0));

    private Transitions? Fading(int milliseconds) => IsLoaded && !Motion.IsStill(this)
        ? [new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(milliseconds), Easing = new CubicEaseOut() }]
        : null;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PlaceFirst();
    }

    protected override void OnClosed(EventArgs e)
    {
        _noteTimer.Stop();
        if (_hook is not null)
            Win32Properties.RemoveWndProcHookCallback(this, _hook.Proc);
        _hook = null;
        base.OnClosed(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // a picture of another shape: the window takes it, keeping its width
        if (change.Property == NoteLiftProperty)
            PlaceNote();
        if (change.Property == AspectRatioProperty && _placed && AspectRatio > 0 && Math.Abs(Width / Height - AspectRatio) > 0.005)
            Height = Math.Max(MinHeight, Width / AspectRatio);
    }

    // A press nothing took drags the window; under a finger, the first tap only shows the overlay.
    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled)
            return;
        if (e.Pointer.Type == PointerType.Touch && !_overlayShown)
        {
            ShowOverlay(true);
            e.Handled = true;
            return;
        }
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.ClickCount > 1)
            return;
        try
        {
            BeginMoveDrag(e);
        }
        catch (InvalidOperationException)
        {
            // a platform without a system drag (headless): it stays where it is
        }
    }

    // where it was left (its key's place, when that is on a screen), else the default corner; its height from the picture's shape
    private void PlaceFirst()
    {
        var scaling = DesktopScaling;
        var saved = PlaceKey is { } key ? LoadPlace is { } load ? load(key) : Kept.GetValueOrDefault(key) : null;
        var areas = Screens.All.Select(s => s.WorkingArea).ToList();
        var home = (Screens.ScreenFromWindow(this) ?? Screens.Primary)?.WorkingArea ?? new PixelRect(0, 0, 1920, 1040);
        var (position, size) = Placing(saved, areas, home, scaling, AspectRatio, DefaultWidth, new Size(MinWidth, MinHeight), FlowDirection == FlowDirection.RightToLeft);
        Width = size.Width;
        Height = size.Height;
        Position = position;
        _placed = true;
    }

    private void Keep()
    {
        if (!_placed || PlaceKey is not { } key)
            return;
        var place = new PicturePlace(Position.X, Position.Y, Width, Height);
        if (SavePlace is { } save)
            save(key, place);
        else
            Kept[key] = place;
    }

    /// <summary>Where the window opens: a kept place whose centre is on one of the working areas, its width kept (at least the least, at most most of that area) and its height from the shape; else <paramref name="defaultWidth"/> at the bottom corner of <paramref name="home"/> on the reading end (the left in a right-to-left language), <see cref="EdgeGap"/> from its edges. Sizes are device-independent; places and areas in pixels.</summary>
    internal static (PixelPoint Position, Size Size) Placing(PicturePlace? saved, IReadOnlyList<PixelRect> areas, PixelRect home, double scaling, double aspect,
        double defaultWidth, Size least, bool rightToLeft)
    {
        Size Shaped(double width, PixelRect area)
        {
            width = Math.Max(least.Width, Math.Min(width, area.Width / scaling * 0.9));
            var height = aspect > 0 ? width / aspect : width * 9 / 16;
            if (height < least.Height)
                (height, width) = (least.Height, aspect > 0 ? least.Height * aspect : width);
            return new Size(Math.Round(width), Math.Round(height));
        }
        if (saved is { Width: > 0, Height: > 0 })
        {
            var centre = new PixelPoint(saved.X + (int)(saved.Width * scaling / 2), saved.Y + (int)(saved.Height * scaling / 2));
            foreach (var area in areas)
                if (area.Contains(centre))
                {
                    var kept = Shaped(saved.Width, area);
                    var x = Math.Clamp(saved.X, area.X, Math.Max(area.X, area.Right - (int)(kept.Width * scaling)));
                    var y = Math.Clamp(saved.Y, area.Y, Math.Max(area.Y, area.Bottom - (int)(kept.Height * scaling)));
                    return (new PixelPoint(x, y), kept);
                }
        }
        var size = Shaped(defaultWidth, home);
        var gap = (int)Math.Round(EdgeGap * scaling);
        var w = (int)Math.Round(size.Width * scaling);
        var h = (int)Math.Round(size.Height * scaling);
        var left = rightToLeft ? home.X + gap : home.Right - gap - w;
        return (new PixelPoint(left, home.Bottom - gap - h), size);
    }

    // WM_SIZING's edges
    internal const int EdgeLeft = 1, EdgeRight = 2, EdgeTop = 3, EdgeTopLeft = 4, EdgeTopRight = 5, EdgeBottom = 6, EdgeBottomLeft = 7, EdgeBottomRight = 8;

    /// <summary>The box an edge drag asks for (<paramref name="box"/>, the whole window in pixels), made to keep the inside's shape: the inside is the box less <paramref name="frame"/> (the border's share). A side edge leads with the width and moves the bottom; the top or the bottom edge leads with the height and moves the end it is not; a corner takes the larger of the two and moves the corner's own edges. The inside is never under <paramref name="least"/>.</summary>
    internal static PixelRect Keep(int edge, PixelRect box, PixelSize frame, double aspect, PixelSize least)
    {
        if (aspect <= 0)
            return box;
        double w = box.Width - frame.Width, h = box.Height - frame.Height;
        var widthLeads = edge switch
        {
            EdgeLeft or EdgeRight => true,
            EdgeTop or EdgeBottom => false,
            _ => w >= h * aspect,
        };
        if (widthLeads)
            h = w / aspect;
        else
            w = h * aspect;
        if (w < least.Width)
            (w, h) = (least.Width, least.Width / aspect);
        if (h < least.Height)
            (w, h) = (least.Height * aspect, least.Height);
        var width = (int)Math.Round(w) + frame.Width;
        var height = (int)Math.Round(h) + frame.Height;
        var x = edge is EdgeLeft or EdgeTopLeft or EdgeBottomLeft ? box.Right - width : box.X;
        var y = edge is EdgeTop or EdgeTopLeft or EdgeTopRight ? box.Bottom - height : box.Y;
        return new PixelRect(x, y, width, height);
    }

    // Windows: the box under an edge drag keeps the picture's shape while it is dragged (WM_SIZING), not after.
    private sealed class SizingHook(PictureWindow window)
    {
        private const uint WM_SIZING = 0x0214;

        public readonly Win32Properties.CustomWndProcHookCallback Proc = (nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled) =>
        {
            if (msg != WM_SIZING || lParam == 0 || window.AspectRatio <= 0 || GetWindowRect(hWnd, out var outer) == 0 || GetClientRect(hWnd, out var inner) == 0)
                return 0;
            var frame = new PixelSize(outer.Right - outer.Left - (inner.Right - inner.Left), outer.Bottom - outer.Top - (inner.Bottom - inner.Top));
            var asked = ReadRect(lParam);
            var scaling = window.RenderScaling;
            var least = new PixelSize((int)Math.Ceiling(window.MinWidth * scaling), (int)Math.Ceiling(window.MinHeight * scaling));
            var kept = Keep((int)wParam, new PixelRect(asked.Left, asked.Top, asked.Right - asked.Left, asked.Bottom - asked.Top), frame, window.AspectRatio, least);
            WriteRect(lParam, new RECT { Left = kept.X, Top = kept.Y, Right = kept.Right, Bottom = kept.Bottom });
            handled = true;
            return 1;
        };

        private static RECT ReadRect(nint p) => new()
        {
            Left = System.Runtime.InteropServices.Marshal.ReadInt32(p, 0),
            Top = System.Runtime.InteropServices.Marshal.ReadInt32(p, 4),
            Right = System.Runtime.InteropServices.Marshal.ReadInt32(p, 8),
            Bottom = System.Runtime.InteropServices.Marshal.ReadInt32(p, 12),
        };

        private static void WriteRect(nint p, RECT r)
        {
            System.Runtime.InteropServices.Marshal.WriteInt32(p, 0, r.Left);
            System.Runtime.InteropServices.Marshal.WriteInt32(p, 4, r.Top);
            System.Runtime.InteropServices.Marshal.WriteInt32(p, 8, r.Right);
            System.Runtime.InteropServices.Marshal.WriteInt32(p, 12, r.Bottom);
        }
    }
}
