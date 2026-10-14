using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery;
using ShapePath = Avalonia.Controls.Shapes.Path;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The gallery's chosen brand swatch: one ring of four arcs in its own colour, 2px clear of the disc and never clipped; it rolls to a newly chosen swatch and jumps there in a still window.
public class SwatchMarkTests
{
    private static (Window Window, List<Button> Swatches, ShapePath Ring) OpenCard(FlowDirection dir, ThemeVariant theme, string brand = "#4C1D95")
    {
        PageScrollTests.LoadIcons();
        InsiyabTheme.SetPrimary(Color.Parse(brand));
        var shell = new GalleryShell();
        var window = Show(shell, dir, theme, 1280, 800);
        window.FlowDirection = dir;
        Settle(300);
        var open = shell.GetVisualDescendants().OfType<Button>().First(b => b.Name == "AppearanceButton");
        open.Flyout!.ShowAt(open);
        Settle(500);
        var swatches = window.GetVisualDescendants().OfType<Button>().Where(b => b.Tag is Color).ToList();
        Assert.Equal(6, swatches.Count);
        var ring = window.GetVisualDescendants().OfType<ShapePath>().Single(p => p.Opacity > 0 && p.Name is "SwatchMark" or "PART_Ring");
        return (window, swatches, ring);
    }

    [AvaloniaTheory]
    [InlineData("Light", "rtl")]
    [InlineData("Dark", "ltr")]
    public void The_chosen_swatch_shows_four_arcs_two_pixels_clear_of_its_disc(string theme, string dir)
    {
        try
        {
            Check(dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        }
        finally { InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary); }
    }

    private static void Check(FlowDirection dir, ThemeVariant theme)
    {
        var (w, swatches, ring) = OpenCard(dir, theme, "#0F766E");
        var swatch = swatches[1];
        var colour = ((SolidColorBrush)swatch.Background!).Color;

        Assert.False(swatch.ClipToBounds);
        Assert.Equal(1, ring.Opacity);
        Assert.Equal(colour, ColorOf(ring.Stroke));
        Assert.Equal(2, ring.StrokeThickness);

        var face = Part<Border>(swatch, "PART_Face");
        var disc = face.Bounds.Width / 2 - face.BorderThickness.Left;
        var middle = new Point(ring.Width / 2, ring.Height / 2);
        var pen = new Pen(ring.Stroke, ring.StrokeThickness, lineCap: ring.StrokeLineCap);
        double Inner(double angle)
        {
            for (var r = 0.0; r < 30; r += 0.05)
                if (ring.Data!.StrokeContains(pen, new Point(middle.X + r * Math.Cos(angle * Math.PI / 180), middle.Y + r * Math.Sin(angle * Math.PI / 180)))) return r;
            return double.NaN;
        }
        foreach (var diagonal in new[] { 45.0, 135, 225, 315 })
            Assert.Equal(disc + 2, Inner(diagonal), 0.5);

        var centre = swatch.TranslatePoint(new Point(15, 15), w)!.Value;
        var ringAt = ring.TranslatePoint(middle, w)!.Value;
        Assert.Equal(centre.X, ringAt.X, 0.5);
        Assert.Equal(centre.Y, ringAt.Y, 0.5);

        var frame = w.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        Color At(double angle, double radius)
        {
            var x = (int)Math.Round(centre.X + radius * Math.Cos(angle * Math.PI / 180));
            var y = (int)Math.Round(centre.Y + radius * Math.Sin(angle * Math.PI / 180));
            return PixelAt(buffer, x, y);
        }
        static int Far(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
        var body = disc + 3;
        int Nearest(double angle) => new[] { body - 0.6, body - 0.2, body, body + 0.2, body + 0.6 }.Min(r => Far(At(angle, r), colour));
        foreach (var diagonal in new[] { 45.0, 135, 225, 315 })
            Assert.True(Nearest(diagonal) < 60, $"no arc at {diagonal}°: {Nearest(diagonal)}");
        foreach (var gap in new[] { 0.0, 90, 180, 270 })
            Assert.True(Nearest(gap) > 120, $"no gap between the arcs at {gap}°: {Nearest(gap)}");
    }

    // Frames only run while something is dirty, so each pass invalidates; the motion reads its own clock, which the tests hold.
    private static void Pump(Window window, int frames = 3)
    {
        for (var i = 0; i < frames; i++)
        {
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    private static Point RingAt(ShapePath ring, Window w) => ring.TranslatePoint(new Point(ring.Width / 2, ring.Height / 2), w)!.Value;

    private static Point DiscAt(Control swatch, Window w) => swatch.TranslatePoint(new Point(15, 15), w)!.Value;

    // The on-screen angle of the ring's own right-hand point about its centre, in degrees, clockwise growing.
    private static double Spin(ShapePath ring, Window w)
    {
        var (c, p) = (RingAt(ring, w), ring.TranslatePoint(new Point(ring.Width / 2 + 16, ring.Height / 2), w)!.Value);
        return Math.Atan2(p.Y - c.Y, p.X - c.X) * 180 / Math.PI;
    }

    private static double Wrap(double degrees)
    {
        degrees %= 360;
        return degrees > 180 ? degrees - 360 : degrees <= -180 ? degrees + 360 : degrees;
    }

    private static double ScaleOf(Visual v) => v.RenderTransform?.Value is { } m ? double.Hypot(m.M11, m.M12) : 1;

    private static void Choose(Button swatch) => swatch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Choosing_another_swatch_rolls_the_ring_to_it_like_a_wheel_and_pops_its_disc(FlowDirection dir)
    {
        var real = SwatchRing.Now;
        var t = 0.0;
        SwatchRing.Now = () => t;
        try
        {
            var (w, swatches, ring) = OpenCard(dir, ThemeVariant.Light);
            var (from, to) = (swatches[0], swatches[1]);
            var face = Part<Border>(to, "PART_Face");
            var (a, b) = (DiscAt(from, w), DiscAt(to, w));
            Assert.Equal(a.X, RingAt(ring, w).X, 0.5);
            Assert.Equal(a.Y, RingAt(ring, w).Y, 0.5);
            Assert.Equal(1, ScaleOf(ring), 3);
            var spin0 = Spin(ring, w);

            Choose(to);
            Pump(w);
            Assert.Equal(a.X, RingAt(ring, w).X, 0.5);
            Assert.Equal(a.Y, RingAt(ring, w).Y, 0.5);

            var (line, len) = (b - a, double.Hypot(b.X - a.X, b.Y - a.Y));
            (double Along, double Off) Where()
            {
                var m = RingAt(ring, w) - a;
                return ((m.X * line.X + m.Y * line.Y) / (len * len), Math.Abs(m.X * line.Y - m.Y * line.X) / len);
            }

            t = 100;
            Pump(w);
            var early = Where();
            Assert.InRange(early.Along, 0.4, 1);
            Assert.True(early.Off < 0.75, $"the ring left the row: {early.Off}px off");
            var turned = Wrap(Spin(ring, w) - spin0);
            Assert.InRange(Math.Abs(turned), 30, 100);
            Assert.Equal(Math.Sign(b.X - a.X), Math.Sign(turned));

            t = 190;
            Pump(w);
            Assert.InRange(Where().Along, 1.01, 1.2);

            t = 179 + 0.5;
            Pump(w);
            Assert.Equal(0.9, ScaleOf(face), 0.02);
            t = 283;
            Pump(w);
            Assert.Equal(1.06, ScaleOf(face), 0.02);

            t = 380;
            Pump(w);
            Assert.Equal(1.12, ScaleOf(ring), 0.02);
            Assert.Equal(b.X, RingAt(ring, w).X, 0.5);

            t = 700;
            Pump(w);
            Assert.Equal(b.X, RingAt(ring, w).X, 0.5);
            Assert.Equal(b.Y, RingAt(ring, w).Y, 0.5);
            Assert.Equal(1, ScaleOf(ring), 3);
            Assert.Null(face.RenderTransform);
            Assert.True(Math.Abs(Wrap(Spin(ring, w) - spin0) % 90) < 0.5, "the arcs are off the diagonals");
        }
        finally
        {
            SwatchRing.Now = real;
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    [AvaloniaFact]
    public void Choosing_again_mid_flight_carries_on_from_where_the_ring_is()
    {
        var real = SwatchRing.Now;
        var t = 0.0;
        SwatchRing.Now = () => t;
        try
        {
            var (w, swatches, ring) = OpenCard(FlowDirection.RightToLeft, ThemeVariant.Light);
            Choose(swatches[1]);
            Pump(w);
            t = 100;
            Pump(w);
            var (turn, scale, mid) = (Spin(ring, w), ScaleOf(ring), RingAt(ring, w));
            Choose(swatches[2]);
            Pump(w);
            Assert.Equal(mid.X, RingAt(ring, w).X, 0.5);
            Assert.Equal(mid.Y, RingAt(ring, w).Y, 0.5);
            Assert.Equal(turn, Spin(ring, w), 0.5);
            Assert.Equal(scale, ScaleOf(ring), 0.01);

            t = 900;
            Pump(w);
            var b = DiscAt(swatches[2], w);
            Assert.Equal(b.X, RingAt(ring, w).X, 0.5);
            Assert.Equal(b.Y, RingAt(ring, w).Y, 0.5);
            Assert.Equal(1, ScaleOf(ring), 3);
        }
        finally
        {
            SwatchRing.Now = real;
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    [AvaloniaFact]
    public void In_a_still_window_the_ring_is_on_the_chosen_swatch_by_the_next_frame()
    {
        var real = SwatchRing.Now;
        SwatchRing.Now = () => 0;
        try
        {
            var (w, swatches, ring) = OpenCard(FlowDirection.RightToLeft, ThemeVariant.Light);
            Motion.SetStill(w, true);
            var to = swatches[3];
            Choose(to);
            Pump(w, 1);
            var b = DiscAt(to, w);
            Assert.Equal(b.X, RingAt(ring, w).X, 0.5);
            Assert.Equal(b.Y, RingAt(ring, w).Y, 0.5);
            Assert.Equal(1, ScaleOf(ring), 3);
            Assert.Null(Part<Border>(to, "PART_Face").RenderTransform);
            Assert.Equal(((SolidColorBrush)to.Background!).Color, ColorOf(ring.Stroke));
        }
        finally
        {
            SwatchRing.Now = real;
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }
}
