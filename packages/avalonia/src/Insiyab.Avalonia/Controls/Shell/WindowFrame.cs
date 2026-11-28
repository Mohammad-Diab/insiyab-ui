using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The edge of a window that draws its own title bar on Linux (TitleBar.Extends with Avalonia's drawn decorations), at the root of the window's template. As GTK draws its own windows: a soft shadow in a see-through margin, a hairline round the window and the desktop's rounded corners, and _GTK_FRAME_EXTENTS so the window manager snaps, tiles and places the visible frame, not the shadow. With no compositor (no see-through pixels) a 1 px line and square corners; maximised or full screen nothing. Avalonia's resize grips cover the margin (TitleBar sets its room). Elsewhere it does nothing.</summary>
internal sealed class WindowFrame : Decorator
{
    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<WindowFrame, IBrush?>(nameof(LineBrush));

    /// <summary>The see-through room around a window for its shadow, on each side (DIP); the resize grips cover it.</summary>
    internal const double ShadowRoom = 12;

    /// <summary>With no compositor: the band along the window's edge that resizes it (DIP), over the edge of the content.</summary>
    internal const double SolidGrip = 4;

    // a focused window's shadow is deeper than the others', as GNOME and KDE draw theirs; both fit in ShadowRoom (blur + offset)
    internal static readonly BoxShadows ActiveShadow = BoxShadows.Parse("0 2 9 0 #59000000");
    internal static readonly BoxShadows IdleShadow = BoxShadows.Parse("0 1 6 0 #30000000");

    /// <summary>The radius of the desktop's own window corners (settable for tests).</summary>
    internal static double Radius { get; set; } = DesktopRadius(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));

    private Window? _window;
    private Look _look;
    private int? _extents;
    private Size _clipSize;
    private double _clipRadius;

    static WindowFrame()
    {
        AffectsRender<WindowFrame>(LineBrushProperty);
    }

    public IBrush? LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    /// <summary>What the frame draws: the content's inset, its corner radius, the hairline's width (0: none), the shadow, and the shadow's width for the window manager (device pixels).</summary>
    internal readonly record struct Look(Thickness Padding, double Radius, double Hairline, bool Shadow, int Extents);

    /// <summary>GNOME's windows (libadwaita) round their corners by 12, KDE's Breeze and the other desktops' by about 8.</summary>
    internal static double DesktopRadius(string? desktop) =>
        desktop?.Contains("GNOME", StringComparison.OrdinalIgnoreCase) == true ? 12 : 8;

    /// <summary>The frame for a window: <paramref name="margin"/> is its WindowDecorationMargin, whose bottom is the drawn frame plus the shadow's room (the top also holds the title bar's band) and zero when maximised or full screen.</summary>
    internal static Look For(bool on, bool seeThrough, Thickness margin, double scaling, double radius)
    {
        var edge = margin.Bottom;
        if (!on || edge <= 0 || scaling <= 0) return default;
        var pixel = 1 / scaling;
        // no compositor: the margin is the grip band over the content's edge; only the outer pixel is the line
        if (!seeThrough) return new Look(new Thickness(pixel), 0, pixel, false, 0);
        // the content inside the margin; the hairline is its outer pixel's ring, the rest of the margin is the shadow
        return new Look(new Thickness(edge), radius, pixel, true, Math.Max(0, (int)Math.Round(edge * scaling) - 1));
    }

    /// <summary>The radius the frame gives a Linux window's corners now; 0 when they are square.</summary>
    internal static double RadiusOf(Window window) =>
        For(IsOn(window), SeesThrough(window), window.WindowDecorationMargin, window.RenderScaling, Radius).Radius;

    /// <summary>How far in from the window's edge its content starts on each side (DIP): the shadow's room and the hairline, or with no compositor the 1 px line; none elsewhere.</summary>
    internal static Thickness InsetOf(Window window) =>
        For(IsOn(window), SeesThrough(window), window.WindowDecorationMargin, window.RenderScaling, Radius).Padding;

    private static bool IsOn(Window window) => Os.IsLinux && !Os.IsAndroid && TitleBar.IsActive(window);

    private static bool SeesThrough(Window window) => window.ActualTransparencyLevel != WindowTransparencyLevel.None;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!Os.IsLinux || Os.IsAndroid || TemplatedParent is not Window window) return;
        _window = window;
        window.PropertyChanged += OnWindowChanged;
        window.ScalingChanged += OnScalingChanged;
        Update();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_window is { } window)
        {
            window.PropertyChanged -= OnWindowChanged;
            window.ScalingChanged -= OnScalingChanged;
            _window = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void OnWindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        var p = e.Property;
        if (p == Window.WindowDecorationMarginProperty || p == TopLevel.ActualTransparencyLevelProperty || p == Window.IsExtendedIntoWindowDecorationsProperty
            || p == TitleBar.ExtendsProperty || p == Window.WindowStateProperty)
            Update();
        else if (p == WindowBase.IsActiveProperty && _look.Shadow)
            InvalidateVisual();
    }

    private void OnScalingChanged(object? sender, EventArgs e) => Update();

    private void Update()
    {
        if (_window is not { } window) return;
        var on = IsOn(window);
        var look = For(on, SeesThrough(window), window.WindowDecorationMargin, window.RenderScaling, Radius);
        if (look != _look)
        {
            _look = look;
            Padding = look.Padding;
            InvalidateArrange();
            InvalidateVisual();
        }
        int? extents = on ? look.Extents : null;
        if (extents != _extents && OperatingSystem.IsLinux() && TitleBarX11.SetFrameExtents(window, extents))
            _extents = extents;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (Child is { } child)
        {
            var radius = _look.Radius;
            var clip = child.Bounds.Size;
            if (radius != _clipRadius || (radius > 0 && clip != _clipSize))
            {
                _clipRadius = radius;
                _clipSize = clip;
                child.Clip = radius > 0 ? new RectangleGeometry(new Rect(clip), radius, radius) : null;
            }
        }
        return size;
    }

    public override void Render(DrawingContext context)
    {
        var look = _look;
        if (look.Hairline <= 0) return;
        var pixel = look.Hairline;
        var inner = new Rect(Bounds.Size).Deflate(look.Padding);
        if (inner.Width <= 0 || inner.Height <= 0) return;
        var rounded = look.Radius > 0;
        if (look.Shadow)
            context.DrawRectangle(null, null, new RoundedRect(inner.Inflate(pixel), rounded ? look.Radius + pixel : 0),
                _window?.IsActive == true ? ActiveShadow : IdleShadow);
        if (LineBrush is { } line)
            context.DrawRectangle(null, new Pen(line, pixel), new RoundedRect(inner.Inflate(pixel / 2), rounded ? look.Radius + pixel / 2 : 0));
    }
}
