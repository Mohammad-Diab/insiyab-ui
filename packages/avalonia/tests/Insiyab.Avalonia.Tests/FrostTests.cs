using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Tests;

public class FrostTests
{
    private static readonly Color Black = Colors.Black;
    private static readonly Color White = Colors.White;
    // The darkest a light material gets: Windows' light acrylic (#FCFCFC at .85 luminosity) over a black wallpaper; light Mica is brighter.
    private static readonly Color LightMaterialFloor = Color.FromRgb(214, 214, 214);
    private static readonly Color LightText2 = Color.Parse("#64748B");
    private static readonly Color DarkText2 = Color.Parse("#94A3B8");
    private static readonly Color Primary = Color.FromRgb(155, 44, 94);

    private static Window ShowWindow(bool? material = null)
    {
        var window = new Window { Width = 400, Height = 300, Content = new TextBlock { Text = "Sample" } };
        if (material is { } m)
            Frost.SetMaterial(window, m);
        window.Show();
        return window;
    }

    private static FrostGround GroundOf(Window window) =>
        window.GetVisualDescendants().OfType<FrostGround>().Single();

    // The platform callback is internal in Avalonia 12, so the test reaches it the one way it can; library code never does this.
    private static void SimulateLevel(Window window, WindowTransparencyLevel level)
    {
        var impl = window.PlatformImpl!;
        var callback = (Action<WindowTransparencyLevel>)impl.GetType().GetProperty("TransparencyLevelChanged")!.GetValue(impl)!;
        callback(level);
    }

    [AvaloniaFact]
    public void Headless_window_gets_the_opaque_ground_and_no_material()
    {
        var window = ShowWindow();

        Assert.False(Frost.IsMaterialLevel(window.ActualTransparencyLevel));
        Assert.DoesNotContain(Frost.MaterialPseudoClass, window.Classes);
        Assert.False(GroundOf(window).Frosted);

        using var frame = window.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var pixel = ReadPixel(buffer, frame.PixelSize.Width / 2, frame.PixelSize.Height - 2);
        Assert.Equal(255, pixel.A);
    }

    [AvaloniaFact]
    public void Window_asks_for_mica_then_acrylic()
    {
        var window = ShowWindow();

        Assert.Equal([WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None], window.TransparencyLevelHint);
    }

    [AvaloniaFact]
    public void A_change_of_the_granted_level_flips_the_material_class()
    {
        var window = ShowWindow();

        SimulateLevel(window, WindowTransparencyLevel.Mica);
        Assert.Contains(Frost.MaterialPseudoClass, window.Classes);
        Assert.True(GroundOf(window).Frosted);

        SimulateLevel(window, WindowTransparencyLevel.None);
        Assert.DoesNotContain(Frost.MaterialPseudoClass, window.Classes);
        Assert.False(GroundOf(window).Frosted);

        SimulateLevel(window, WindowTransparencyLevel.AcrylicBlur);
        Assert.Contains(Frost.MaterialPseudoClass, window.Classes);

        SimulateLevel(window, WindowTransparencyLevel.Transparent);
        Assert.DoesNotContain(Frost.MaterialPseudoClass, window.Classes);
    }

    [AvaloniaFact]
    public void Opt_out_keeps_the_window_opaque()
    {
        var window = ShowWindow(material: false);

        Assert.Equal([WindowTransparencyLevel.None], window.TransparencyLevelHint);
        SimulateLevel(window, WindowTransparencyLevel.Mica);
        Assert.DoesNotContain(Frost.MaterialPseudoClass, window.Classes);
        Assert.False(GroundOf(window).Frosted);

        Frost.SetMaterial(window, true);
        Assert.Contains(Frost.MaterialPseudoClass, window.Classes);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Text2_clears_AA_on_the_veil_over_any_wallpaper(string variant)
    {
        var theme = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var text2 = variant == "Dark" ? DarkText2 : LightText2;
        var app = Application.Current!;
        Assert.True(app.TryGetResource("InsFrostVeilColor", theme, out var veilColor));
        Assert.True(app.TryGetResource("InsFrostVeilOpacity", theme, out var veilOpacity));
        var veil = (Color)veilColor!;
        var alpha = (double)veilOpacity!;

        foreach (var backdrop in new[] { variant == "Dark" ? Black : LightMaterialFloor, White })
        {
            var ground = Over(veil, alpha, backdrop);
            Assert.True(Contrast(text2, ground) >= 4.5, $"{variant} over {backdrop}: {Contrast(text2, ground):F2}");
        }
    }

    [Fact]
    public void Dark_veil_keeps_AA_even_under_the_peak_of_the_brand_orbs()
    {
        var veil = Over(Color.Parse("#0F1115"), .88, White);
        var peak = Over(Primary, .30 * .55, Over(Primary, .18, veil));

        Assert.True(Contrast(DarkText2, peak) >= 4.5, $"{Contrast(DarkText2, peak):F2}");
    }

    [Fact]
    public void Contrast_maths_matches_the_wcag_reference_points()
    {
        Assert.Equal(21, Contrast(Black, White), 2);
        Assert.Equal(4.76, Contrast(LightText2, White), 2);
        Assert.Equal(1, Contrast(LightText2, LightText2), 5);
    }

    private static Color Over(Color top, double alpha, Color bottom) => Color.FromRgb(
        (byte)Math.Round(top.R * alpha + bottom.R * (1 - alpha)),
        (byte)Math.Round(top.G * alpha + bottom.G * (1 - alpha)),
        (byte)Math.Round(top.B * alpha + bottom.B * (1 - alpha)));

    private static double Linear(byte channel)
    {
        var c = channel / 255d;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static double Luminance(Color c) => 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);

    private static double Contrast(Color a, Color b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static Color ReadPixel(global::Avalonia.Platform.ILockedFramebuffer buffer, int x, int y)
    {
        var offset = y * buffer.RowBytes + x * 4;
        var bytes = new byte[4];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + offset, bytes, 0, 4);
        return buffer.Format == global::Avalonia.Platform.PixelFormat.Rgba8888
            ? Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2])
            : Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
    }
}
