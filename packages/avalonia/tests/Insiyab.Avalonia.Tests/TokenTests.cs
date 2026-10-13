using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Brand;
using Insiyab.Avalonia.Generated;

namespace Insiyab.Avalonia.Tests;

public class TokenTests
{
    private static object? Resolve(string key, ThemeVariant theme)
    {
        var window = new Window { RequestedThemeVariant = theme };
        window.Show();
        try
        {
            Assert.Equal(theme, window.ActualThemeVariant);
            Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var value), $"{key} did not resolve in {theme}");
            return value;
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Radius_is_a_corner_radius_in_both_themes()
    {
        Assert.Equal(new CornerRadius(22), Resolve("InsRLg", ThemeVariant.Light));
        Assert.Equal(new CornerRadius(22), Resolve("InsRLg", ThemeVariant.Dark));
    }

    [AvaloniaFact]
    public void Text_colour_follows_the_theme()
    {
        Assert.Equal(Color.Parse("#0F172A"), Resolve("InsText1Color", ThemeVariant.Light));
        Assert.Equal(Color.Parse("#F1F5F9"), Resolve("InsText1Color", ThemeVariant.Dark));
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(Resolve("InsText1", ThemeVariant.Dark));
        Assert.Equal(Color.Parse("#F1F5F9"), brush.Color);
    }

    [AvaloniaFact]
    public void Multi_layer_shadow_keeps_its_inset_layer()
    {
        var light = Assert.IsType<BoxShadows>(Resolve("InsSegOnShadow", ThemeVariant.Light));
        Assert.Equal(2, light.Count);
        Assert.Equal(new BoxShadow { OffsetY = 2, Blur = 8, Color = Color.FromArgb(20, 15, 23, 42) }, light[0]);
        Assert.Equal(new BoxShadow { OffsetY = 1, Color = Color.FromArgb(242, 255, 255, 255), IsInset = true }, light[1]);

        var dark = Assert.IsType<BoxShadows>(Resolve("InsSegOnShadow", ThemeVariant.Dark));
        Assert.Equal(Color.FromArgb(89, 0, 0, 0), dark[0].Color);
        Assert.True(dark[1].IsInset);
    }

    [AvaloniaFact]
    public void Negative_spread_survives()
    {
        var shadows = Assert.IsType<BoxShadows>(Resolve("InsShMd", ThemeVariant.Light));
        Assert.Equal(-1, shadows[0].Spread);
        Assert.Equal(-2, shadows[1].Spread);
    }

    [AvaloniaFact]
    public void Rem_spacing_is_px_at_16()
    {
        Assert.Equal(16d, Resolve("InsSp4", ThemeVariant.Light));
        Assert.Equal(24d, Resolve("InsDlgPad", ThemeVariant.Dark));
    }

    [AvaloniaFact]
    public void Seconds_are_a_time_span()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(180), Resolve("InsTExit", ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void Cubic_bezier_is_a_spline_easing()
    {
        var ease = Assert.IsType<SplineEasing>(Resolve("InsEase", ThemeVariant.Light));
        Assert.Equal(0.22, ease.X1, 6);
        Assert.Equal(1, ease.Y1, 6);
        Assert.Equal(0.36, ease.X2, 6);
        Assert.Equal(1, ease.Y2, 6);
    }

    [AvaloniaFact]
    public void Gradient_is_a_horizontal_linear_brush()
    {
        var brush = Assert.IsType<LinearGradientBrush>(Resolve("InsShimmer", ThemeVariant.Light));
        Assert.Equal(new RelativePoint(0, 0.5, RelativeUnit.Relative), brush.StartPoint);
        Assert.Equal(new RelativePoint(1, 0.5, RelativeUnit.Relative), brush.EndPoint);
        Assert.Equal(3, brush.GradientStops.Count);
    }

    [AvaloniaFact]
    public void Brand_tokens_take_the_theme_alpha()
    {
        var primaryRgb = Color.FromRgb(15, 118, 110);
        var brand = new BrandSet(primaryRgb, Colors.Black, Colors.Black, primaryRgb, Colors.Black, Colors.Black, Colors.White, Colors.White, Colors.White, Colors.White, Colors.Black);

        var light = new ResourceDictionary();
        BrandTokens.Apply(light, brand, dark: false);
        Assert.Equal(Color.FromArgb(31, 15, 118, 110), light["InsToneBrandBgColor"]);
        Assert.Equal(Color.FromArgb(31, 15, 118, 110), Assert.IsAssignableFrom<ISolidColorBrush>(light["InsToneBrandBg"]).Color);

        var dark = new ResourceDictionary();
        BrandTokens.Apply(dark, brand, dark: true);
        Assert.Equal(Color.FromArgb(51, 15, 118, 110), dark["InsToneBrandBgColor"]);
        Assert.IsType<BoxShadows>(dark["InsFocusRing"]);
    }
}
