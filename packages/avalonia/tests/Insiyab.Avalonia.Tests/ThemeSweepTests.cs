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

    private static (Window, Button) Rig(FlowDirection dir = FlowDirection.LeftToRight)
    {
        var origin = new Button { Content = "الوضع", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 20, 20, 0) };
        var window = new Window { Width = 800, Height = 500, FlowDirection = dir, Content = new Panel { Children = { origin } } };
        window.Show();
        Track(window);
        Settle();
        return (window, origin);
    }

    [AvaloniaFact]
    public void Sweep_applies_at_once_and_wipes_the_old_picture_away_from_the_origin()
    {
        try
        {
            var (window, origin) = Rig();
            var applied = false;
            ThemeSweep.Run(origin, () => { applied = true; Application.Current!.RequestedThemeVariant = ThemeVariant.Dark; });
            Assert.True(applied);
            var picture = Picture(window);
            Assert.NotNull(picture);
            Assert.Equal(FlowDirection.LeftToRight, picture!.FlowDirection);
            Assert.False(picture.IsHitTestVisible);
            Settle(150);
            var centre = origin.TranslatePoint(new Point(origin.Bounds.Width / 2, origin.Bounds.Height / 2), window)!.Value;
            Assert.NotNull(picture.Clip);
            Assert.False(picture.Clip!.FillContains(centre));
            Assert.True(picture.Clip.FillContains(new Point(2, 498)));
            Settle(900);
            Assert.Null(Picture(window));
        }
        finally { Application.Current!.RequestedThemeVariant = ThemeVariant.Light; }
    }

    [AvaloniaFact]
    public void Sweep_starts_from_the_origin_in_rtl_too()
    {
        try
        {
            var (window, origin) = Rig(FlowDirection.RightToLeft);
            ThemeSweep.Run(origin, () => Application.Current!.RequestedThemeVariant = ThemeVariant.Dark);
            Settle(150);
            var picture = Picture(window)!;
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
            ThemeSweep.Run(origin, () => Application.Current!.RequestedThemeVariant = ThemeVariant.Dark);
            ThemeSweep.Run(origin, () => Application.Current!.RequestedThemeVariant = ThemeVariant.Light);
            Assert.Single(window.GetVisualDescendants().OfType<Image>(), i => i.Name == "PART_ThemeSweep");
            Settle(900);
            Assert.Null(Picture(window));
        }
        finally { Application.Current!.RequestedThemeVariant = ThemeVariant.Light; }
    }
}
