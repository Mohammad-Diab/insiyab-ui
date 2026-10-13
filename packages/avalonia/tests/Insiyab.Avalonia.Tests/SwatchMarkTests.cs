using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Gallery;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The gallery's chosen brand swatch: four arcs of its own colour on the diagonals, drawn as geometry, 2px clear of the swatch and never clipped.
public class SwatchMarkTests
{
    [AvaloniaTheory]
    [InlineData("Light", "rtl")]
    [InlineData("Dark", "ltr")]
    public void The_chosen_swatch_shows_four_clear_arcs(string theme, string dir)
    {
        var colour = Color.Parse("#0F766E");
        var themeKey = (ControlTheme)new GalleryShell().Resources["SiteSwatch"]!;
        var swatch = new Button { Theme = themeKey, Background = new SolidColorBrush(colour), Classes = { "on" } };
        var ground = new Border { Background = Brushes.White, Padding = new Thickness(40), Child = swatch, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left };
        var w = Show(ground, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 200, 200);
        Settle();

        Assert.False(swatch.ClipToBounds);
        var ring = Part<global::Avalonia.Controls.Shapes.Path>(swatch, "PART_Ring");
        Assert.Equal(1, ring.Opacity);
        Assert.Equal(colour, ColorOf(ring.Stroke));
        Assert.Equal(2, ring.StrokeThickness);

        var centre = swatch.TranslatePoint(new Point(15, 15), w)!.Value;
        var frame = w.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        Color At(double angle, double radius)
        {
            var x = (int)Math.Round(centre.X + radius * Math.Cos(angle * Math.PI / 180));
            var y = (int)Math.Round(centre.Y + radius * Math.Sin(angle * Math.PI / 180));
            return PixelAt(buffer, x, y);
        }
        static int Far(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
        int Nearest(double angle) => new[] { 17.2, 17.6, 18.0, 18.4, 18.8 }.Min(r => Far(At(angle, r), colour));
        foreach (var diagonal in new[] { 45.0, 135, 225, 315 })
        {
            Assert.True(Nearest(diagonal) < 60, $"no arc at {diagonal}°: {Nearest(diagonal)}");
            Assert.True(Far(At(diagonal, 16), colour) > 120, $"the arc runs into the swatch at {diagonal}°: {At(diagonal, 16)}");
        }
        foreach (var gap in new[] { 0.0, 90, 180, 270 })
            Assert.True(Nearest(gap) > 120, $"no gap between the arcs at {gap}°: {Nearest(gap)}");
    }
}
