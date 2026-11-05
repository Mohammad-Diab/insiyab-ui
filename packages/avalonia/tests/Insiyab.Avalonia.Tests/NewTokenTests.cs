using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The glass rim that keeps a bright top only, and the tokens batch 2 adds: the warn signal, the panel shadow, the brand tint and the eight categories.
public class NewTokenTests
{
    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static double Linear(byte channel)
    {
        var c = channel / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static double Luminance(Color c) => 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);

    private static double Contrast(Color a, Color b)
    {
        var hi = Math.Max(Luminance(a), Luminance(b));
        var lo = Math.Min(Luminance(a), Luminance(b));
        return (hi + 0.05) / (lo + 0.05);
    }

    // A brush colour at its alpha times its Opacity, laid over an opaque ground.
    private static Color Over(Color top, double opacity, Color under)
    {
        var a = top.A / 255.0 * opacity;
        byte Mix(byte t, byte u) => (byte)Math.Round(t * a + u * (1 - a));
        return Color.FromRgb(Mix(top.R, under.R), Mix(top.G, under.G), Mix(top.B, under.B));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_rim_keeps_a_bright_top_and_fades_to_the_hairline(string theme)
    {
        var anchor = new Border();
        Show(anchor, theme: Variant(theme));
        var rim = ColorOf((IBrush)Res(anchor, "InsRim"));
        var top = ColorOf((IBrush)Res(anchor, "InsRimTop"));
        Assert.Equal(ColorOf((IBrush)Res(anchor, "InsLineGlass")), rim);
        var gradient = Assert.IsAssignableFrom<IGradientBrush>(Res(anchor, "InsGlassRim"));
        Assert.Equal(top, gradient.GradientStops[0].Color);
        Assert.Equal(rim, gradient.GradientStops[^1].Color);
        var frost = (BoxShadows)Res(anchor, "InsTopbarFrostShadow");
        Assert.Equal(rim, frost[0].Color);
        Assert.True(top.A > rim.A, $"the top {top} should be brighter than the rim {rim}");
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_new_tokens_reach_both_themes(string theme)
    {
        var anchor = new Border();
        Show(anchor, theme: Variant(theme));
        Assert.IsAssignableFrom<ISolidColorBrush>(Res(anchor, "InsToneWarnSignal"));
        for (var n = 1; n <= 8; n++)
            foreach (var suffix in new[] { "", "Bg", "Ink" })
                Assert.IsAssignableFrom<ISolidColorBrush>(Res(anchor, $"InsCat{n}{suffix}"));
        Assert.Equal(2, ((BoxShadows)Res(anchor, "InsShPanel")).Count);
        Assert.IsAssignableFrom<ISolidColorBrush>(Res(anchor, "InsBrandTintBg"));
        Assert.IsAssignableFrom<ISolidColorBrush>(Res(anchor, "InsBrandTintBd"));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Category_inks_and_marks_read_on_their_grounds(string theme)
    {
        var anchor = new Border();
        Show(anchor, theme: Variant(theme));
        var ground = theme == "Dark" ? Color.Parse("#1F2026") : Colors.White;
        var marks = new List<Color>();
        for (var n = 1; n <= 8; n++)
        {
            var bg = Assert.IsAssignableFrom<ISolidColorBrush>(Res(anchor, $"InsCat{n}Bg"));
            var fill = Over(bg.Color, bg.Opacity, ground);
            var ink = ColorOf((IBrush)Res(anchor, $"InsCat{n}Ink"));
            var mark = ColorOf((IBrush)Res(anchor, $"InsCat{n}"));
            Assert.True(Contrast(ink, fill) >= 4.5, $"cat{n} ink {ink} on its fill {fill}: {Contrast(ink, fill):0.00}");
            Assert.True(Contrast(mark, ground) >= 3.0, $"cat{n} mark {mark} on {ground}: {Contrast(mark, ground):0.00}");
            marks.Add(mark);
        }
        var signal = ColorOf((IBrush)Res(anchor, "InsToneWarnSignal"));
        Assert.True(Contrast(signal, ground) >= 3.0, $"warn signal {signal} on {ground}: {Contrast(signal, ground):0.00}");
        Assert.Equal(8, marks.Distinct().Count());
    }
}
