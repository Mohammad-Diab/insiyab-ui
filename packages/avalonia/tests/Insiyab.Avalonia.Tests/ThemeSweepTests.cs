using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ThemeSweepTests
{
    private static Image? Picture(Window window) =>
        window.GetVisualDescendants().OfType<Image>().FirstOrDefault(i => i.Name == "PART_ThemeSweep");

    // The picture is drawn on the render thread, so the theme changes a frame or two after the click.
    private static void WaitFor(Func<bool> condition, int milliseconds = 4000)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && clock.ElapsedMilliseconds < milliseconds) Settle(40);
        Settle(40);
    }

    private static (Window, Button) Rig(FlowDirection dir = FlowDirection.LeftToRight)
    {
        var origin = new Button { Content = "الوضع", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 20, 20, 0) };
        var window = new Window { Width = 800, Height = 500, FlowDirection = dir, Content = new Panel { Children = { origin } } };
        window.Show();
        Track(window);
        Settle();
        return (window, origin);
    }

    // Held, at half way, once the picture has arrived and the theme is applied: switching to light the new palette has opened around the origin; switching to dark the old picture is a circle closing onto it.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Sweep_applies_at_once_and_runs_the_way_of_the_new_theme(string to)
    {
        try
        {
            Application.Current!.RequestedThemeVariant = to == "Dark" ? ThemeVariant.Light : ThemeVariant.Dark;
            var (window, origin) = Rig();
            origin.Resources["InsEase"] = new Fixed(0.5);
            var applied = false;
            ThemeSweep.Held = true;
            ThemeSweep.Run(origin, () => { applied = true; Application.Current!.RequestedThemeVariant = to == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light; });
            WaitFor(() => applied);
            Assert.True(applied);
            var picture = Picture(window);
            Assert.NotNull(picture);
            Assert.False(picture!.IsHitTestVisible);
            Settle(150);
            Assert.Equal(Matrix.Identity, picture.TransformToVisual(window));
            var centre = origin.TranslatePoint(new Point(origin.Bounds.Width / 2, origin.Bounds.Height / 2), window)!.Value;
            Assert.NotNull(picture.Clip);
            Assert.Equal(to == "Dark", picture.Clip!.FillContains(centre));
            Assert.Equal(to == "Light", picture.Clip.FillContains(new Point(2, 498)));
            ThemeSweep.Held = false;
            Settle(900);
            Assert.Null(Picture(window));
        }
        finally
        {
            ThemeSweep.Held = false;
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        }
    }

    // What lands on screen mid-sweep, unmirrored and at its own size: to light, the old frame outside a small circle of the new one around the origin; to dark, the old frame inside a closing circle round the origin and the new one beyond it.
    [AvaloniaTheory]
    [InlineData("Light", "rtl", 1.0)]
    [InlineData("Light", "ltr", 1.0)]
    [InlineData("Light", "rtl", 1.25)]
    [InlineData("Light", "ltr", 1.25)]
    [InlineData("Light", "rtl", 1.5)]
    [InlineData("Light", "ltr", 1.5)]
    [InlineData("Dark", "rtl", 1.0)]
    [InlineData("Dark", "ltr", 1.0)]
    [InlineData("Dark", "rtl", 1.25)]
    [InlineData("Dark", "ltr", 1.25)]
    [InlineData("Dark", "rtl", 1.5)]
    [InlineData("Dark", "ltr", 1.5)]
    public void Mid_sweep_the_screen_shows_each_frame_on_its_side_of_the_circle(string to, string dir, double scale)
    {
        try
        {
            Application.Current!.RequestedThemeVariant = to == "Dark" ? ThemeVariant.Light : ThemeVariant.Dark;
            var origin = new Button { Content = "الوضع", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 20, 20, 0) };
            origin.Resources["InsEase"] = new Fixed(to == "Dark" ? 0.5 : 0.1);
            var marker = new Border { Width = 120, Height = 80, Background = Brushes.Red, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, RenderTransform = TransformOperations.Parse("scale(1)") };
            var near = new Border { Width = 60, Height = 40, Background = Brushes.Red, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 100, 20, 0) };
            var window = new Window
            {
                Width = 800,
                Height = 500,
                FlowDirection = dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                Background = Brushes.White,
                Content = new Panel { Children = { marker, near, origin } },
            };
            window.Show();
            window.SetRenderScaling(scale);
            Track(window);
            Settle();
            PixelRect Mirror(PixelRect r) => new((int)(800 * scale) - r.Right, r.Y, r.Width, r.Height);
            var box = Box(marker, window, scale);
            var nearBox = Box(near, window, scale);
            var below = Center(origin, window) + new Point(0, 40);
            var at = new PixelRect((int)(below.X * scale) - 2, (int)(below.Y * scale) - 2, 4, 4);

            // The sweep is held mid-way, as a capture under load can take longer than its whole run.
            ThemeSweep.Held = true;
            ThemeSweep.Run(origin, () =>
            {
                Application.Current!.RequestedThemeVariant = to == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
                window.Background = Brushes.Black;
                marker.Background = Brushes.Blue;
                near.Background = Brushes.Blue;
            });
            WaitFor(() => Picture(window) is not null);
            Settle(30);
            using var frame = window.CaptureRenderedFrame()!;
            if (to == "Light")
            {
                Assert.True(Share(frame, box, IsRed) > 0.9, "the old marker is where it was, at its size");
                Assert.True(Share(frame, Mirror(box), IsWhite) > 0.9, "nothing is drawn mirrored");
                Assert.True(Share(frame, at, IsBlack) > 0.9, "the circle opens around the origin");
            }
            else
            {
                Assert.True(Share(frame, nearBox, IsRed) > 0.9, "inside the circle the old block is where it was, at its size");
                Assert.True(Share(frame, at, IsWhite) > 0.9, "the old picture stays around the origin");
                Assert.True(Share(frame, Mirror(nearBox), IsBlack) > 0.9, "nothing is drawn mirrored, and beyond the circle is the new frame");
                Assert.True(Share(frame, box, IsBlue) > 0.9, "the far corner already shows the new frame");
            }
        }
        finally
        {
            ThemeSweep.Held = false;
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        }
    }

    private static PixelRect Box(Control c, Window window, double scale)
    {
        var a = c.TranslatePoint(default, window)!.Value;
        var z = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), window)!.Value;
        return new PixelRect((int)Math.Round(Math.Min(a.X, z.X) * scale) + 4, (int)Math.Round(Math.Min(a.Y, z.Y) * scale) + 4, (int)(c.Bounds.Width * scale) - 8, (int)(c.Bounds.Height * scale) - 8);
    }

    private static bool IsRed(int r, int g, int b) => r > 200 && g < 60 && b < 60;
    private static bool IsBlue(int r, int g, int b) => r < 60 && g < 60 && b > 200;
    private static bool IsWhite(int r, int g, int b) => r > 230 && g > 230 && b > 230;
    private static bool IsBlack(int r, int g, int b) => r < 40 && g < 40 && b < 40;

    // Pins the eased progress at a fixed share until the end, so a held sweep shows both palettes.
    private sealed class Fixed(double share) : Easing
    {
        public override double Ease(double progress) => progress < 1 ? share : 1;
    }

    // The share of a box's pixels that pass a test on their colour.
    private static double Share(WriteableBitmap frame, PixelRect box, Func<int, int, int, bool> test)
    {
        using var buffer = frame.Lock();
        var rgba = buffer.Format == global::Avalonia.Platform.PixelFormat.Rgba8888;
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        var hits = 0;
        for (var y = box.Y; y < box.Bottom; y++)
            for (var x = box.X; x < box.Right; x++)
            {
                var i = y * buffer.RowBytes + x * 4;
                if (rgba ? test(bytes[i], bytes[i + 1], bytes[i + 2]) : test(bytes[i + 2], bytes[i + 1], bytes[i])) hits++;
            }
        return (double)hits / (box.Width * box.Height);
    }

    [AvaloniaFact]
    public void A_still_window_gets_the_theme_with_no_sweep()
    {
        try
        {
            var (window, origin) = Rig();
            Motion.SetStill(window, true);
            var applied = false;
            ThemeSweep.Run(origin, () => applied = true);
            Assert.True(applied);
            Assert.Null(Picture(window));
        }
        finally { Application.Current!.RequestedThemeVariant = ThemeVariant.Light; }
    }

    [AvaloniaFact]
    public void A_second_sweep_lands_the_first()
    {
        try
        {
            var (window, origin) = Rig();
            var applies = new List<string>();
            ThemeSweep.Run(origin, () => { applies.Add("dark"); Application.Current!.RequestedThemeVariant = ThemeVariant.Dark; });
            WaitFor(() => Picture(window) is not null);
            ThemeSweep.Run(origin, () => { applies.Add("light"); Application.Current!.RequestedThemeVariant = ThemeVariant.Light; });
            WaitFor(() => applies.Count == 2);
            WaitFor(() => Picture(window) is not null);
            Assert.Equal(["dark", "light"], applies);
            Assert.Single(window.GetVisualDescendants().OfType<Image>(), i => i.Name == "PART_ThemeSweep");
            Settle(900);
            Assert.Null(Picture(window));
        }
        finally { Application.Current!.RequestedThemeVariant = ThemeVariant.Light; }
    }

    [AvaloniaFact]
    public void A_second_click_while_the_picture_is_pending_applies_the_first_theme_at_once()
    {
        try
        {
            var (window, origin) = Rig();
            var applies = new List<string>();
            ThemeSweep.Run(origin, () => { applies.Add("dark"); Application.Current!.RequestedThemeVariant = ThemeVariant.Dark; });
            Assert.Empty(applies);
            ThemeSweep.Run(origin, () => { applies.Add("light"); Application.Current!.RequestedThemeVariant = ThemeVariant.Light; });
            Assert.Equal(["dark"], applies);
            WaitFor(() => applies.Count == 2);
            Assert.Equal(["dark", "light"], applies);
            WaitFor(() => Picture(window) is not null);
            Assert.Single(window.GetVisualDescendants().OfType<Image>(), i => i.Name == "PART_ThemeSweep");
            Settle(900);
            Assert.Null(Picture(window));
        }
        finally { Application.Current!.RequestedThemeVariant = ThemeVariant.Light; }
    }
}
