using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia;

/// <summary>The page ground of the web body: a 135deg wash, two brand radials and three soft orbs. With <see cref="Frosted"/> the wash becomes a translucent veil over the OS material.</summary>
public class FrostGround : Control
{
    public static readonly StyledProperty<IBrush?> Wash1Property = AvaloniaProperty.Register<FrostGround, IBrush?>(nameof(Wash1));
    public static readonly StyledProperty<IBrush?> Wash2Property = AvaloniaProperty.Register<FrostGround, IBrush?>(nameof(Wash2));
    public static readonly StyledProperty<IBrush?> Orb1Property = AvaloniaProperty.Register<FrostGround, IBrush?>(nameof(Orb1));
    public static readonly StyledProperty<IBrush?> Orb2Property = AvaloniaProperty.Register<FrostGround, IBrush?>(nameof(Orb2));
    public static readonly StyledProperty<bool> FrostedProperty = AvaloniaProperty.Register<FrostGround, bool>(nameof(Frosted));
    public static readonly StyledProperty<Color> VeilColorProperty = AvaloniaProperty.Register<FrostGround, Color>(nameof(VeilColor), Colors.White);
    public static readonly StyledProperty<double> VeilOpacityProperty = AvaloniaProperty.Register<FrostGround, double>(nameof(VeilOpacity), 1d);

    private static readonly Color LightWash1 = Color.FromRgb(0xF2, 0xEF, 0xF8);
    private static readonly Color LightWash2 = Color.FromRgb(0xF3, 0xF1, 0xF6);
    private static readonly Color DarkWash1 = Color.FromRgb(0x0F, 0x11, 0x15);
    private static readonly Color DarkWash2 = Color.FromRgb(0x12, 0x14, 0x1A);
    private static readonly Color DefaultPrimary = Color.FromRgb(76, 29, 149);
    private static readonly Color DefaultAccent = Color.FromRgb(52, 20, 101);

    private const double OrbBlur = 24;

    static FrostGround()
    {
        AffectsRender<FrostGround>(Wash1Property, Wash2Property, Orb1Property, Orb2Property, FrostedProperty, VeilColorProperty, VeilOpacityProperty, FlowDirectionProperty);
    }

    public IBrush? Wash1 { get => GetValue(Wash1Property); set => SetValue(Wash1Property, value); }

    public IBrush? Wash2 { get => GetValue(Wash2Property); set => SetValue(Wash2Property, value); }

    public IBrush? Orb1 { get => GetValue(Orb1Property); set => SetValue(Orb1Property, value); }

    public IBrush? Orb2 { get => GetValue(Orb2Property); set => SetValue(Orb2Property, value); }

    /// <summary>True while the window shows an OS material: the opaque wash gives way to the veil.</summary>
    public bool Frosted { get => GetValue(FrostedProperty); set => SetValue(FrostedProperty, value); }

    public Color VeilColor { get => GetValue(VeilColorProperty); set => SetValue(VeilColorProperty, value); }

    public double VeilOpacity { get => GetValue(VeilOpacityProperty); set => SetValue(VeilOpacityProperty, value); }

    public FrostGround() => ActualThemeVariantChanged += (_, _) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        var dark = ActualThemeVariant == ThemeVariant.Dark;
        var wash1 = ColorOf(Wash1, dark ? DarkWash1 : LightWash1);
        var wash2 = ColorOf(Wash2, dark ? DarkWash2 : LightWash2);
        var orb1 = ColorOf(Orb1, WithAlpha(DefaultPrimary, dark ? .30 : .22));
        var orb2 = ColorOf(Orb2, WithAlpha(DefaultAccent, dark ? .26 : .20));
        var rect = new Rect(0, 0, w, h);

        if (Frosted)
            context.FillRectangle(new SolidColorBrush(VeilColor, Math.Clamp(VeilOpacity, 0, 1)), rect);
        else
            context.FillRectangle(Wash(wash1, wash2, w, h), rect);

        // Background positions are physical on the web, so they are un-mirrored here; the orbs use logical insets and mirror.
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        Radial(context, rect, rtl ? w * .85 : w * .15, h * .10, .30, WithAlpha(orb1, .18));
        Radial(context, rect, rtl ? w * .15 : w * .85, h * .15, .28, WithAlpha(orb2, .16));

        Orb(context, rect, w + 80 - 170, -90 + 170, 170, orb1, .55);
        Orb(context, rect, -70 + 140, h - 40 - 140, 140, orb2, .55);
        Orb(context, rect, w * .22 + 110, h * .42 + 110, 110, orb1, .35);
    }

    private static IBrush Wash(Color from, Color to, double w, double h)
    {
        var half = (w + h) * Math.Sqrt(.5) / 2;
        var d = Math.Sqrt(.5) * half;
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(w / 2 - d, h / 2 - d, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(w / 2 + d, h / 2 + d, RelativeUnit.Absolute),
            GradientStops = { new GradientStop(from, 0), new GradientStop(to, 1) },
        };
    }

    private static void Radial(DrawingContext context, Rect rect, double cx, double cy, double stop, Color color)
    {
        var fx = Math.Max(cx, rect.Width - cx);
        var fy = Math.Max(cy, rect.Height - cy);
        var r = Math.Sqrt(fx * fx + fy * fy) * stop;
        if (r <= 0)
            return;
        var center = new RelativePoint(cx, cy, RelativeUnit.Absolute);
        var brush = new RadialGradientBrush
        {
            Center = center,
            GradientOrigin = center,
            RadiusX = new RelativeScalar(r, RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(r, RelativeUnit.Absolute),
            GradientStops = { new GradientStop(color, 0), new GradientStop(WithAlpha(color, 0), 1) },
        };
        context.FillRectangle(brush, rect);
    }

    private static void Orb(DrawingContext context, Rect rect, double cx, double cy, double radius, Color color, double opacity)
    {
        // A disc under blur(24px) has a Gaussian edge; the stops sample that edge from -2σ to +3σ.
        var outer = radius + 3 * OrbBlur;
        var center = new RelativePoint(cx, cy, RelativeUnit.Absolute);
        var brush = new RadialGradientBrush
        {
            Center = center,
            GradientOrigin = center,
            RadiusX = new RelativeScalar(outer, RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(outer, RelativeUnit.Absolute),
        };
        brush.GradientStops.Add(new GradientStop(Fade(color, opacity), 0));
        ReadOnlySpan<double> sigmas = [-2, -1, 0, 1, 2, 3];
        ReadOnlySpan<double> edge = [.977, .841, .5, .159, .023, 0];
        for (var i = 0; i < sigmas.Length; i++)
        {
            var at = Math.Max(0, radius + sigmas[i] * OrbBlur) / outer;
            brush.GradientStops.Add(new GradientStop(Fade(color, opacity * edge[i]), at));
        }
        var bounds = new Rect(cx - outer, cy - outer, outer * 2, outer * 2).Intersect(rect);
        if (bounds.Width > 0 && bounds.Height > 0)
            context.FillRectangle(brush, bounds);
    }

    private static Color ColorOf(IBrush? brush, Color fallback) => brush is ISolidColorBrush solid
        ? WithAlpha(solid.Color, solid.Color.A / 255d * solid.Opacity)
        : fallback;

    private static Color WithAlpha(Color c, double alpha) => Color.FromArgb((byte)Math.Round(Math.Clamp(alpha, 0, 1) * 255), c.R, c.G, c.B);

    private static Color Fade(Color c, double factor) => WithAlpha(c, c.A / 255d * factor);
}
