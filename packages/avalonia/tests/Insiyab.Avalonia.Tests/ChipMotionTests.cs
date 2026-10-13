using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ChipMotionTests
{
    // The B1 switch that turns motion off; if B1 named its setter differently, this is the one line to change.
    private static void Still(Window w, bool on) => Motion.SetStill(w, on);

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static List<(long Ms, T Value)> Sample<T>(Window window, int milliseconds, Func<T> read)
    {
        var samples = new List<(long, T)>();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            samples.Add((clock.ElapsedMilliseconds, read()));
            Thread.Sleep(6);
        }
        return samples;
    }

    // One frame: layout runs, then whatever starts after layout is in place before the picture.
    private static void Frame(Window window)
    {
        window.InvalidateVisual();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Snap(Window window, string name) =>
        window.CaptureRenderedFrame()?.Save(Path.Combine(ShotsDir(), name + ".png"), PngBitmapEncoderOptions.Default);

    private static Point Drawn(Control c, Window window) => c.TranslatePoint(default, window)!.Value;

    private static IEnumerable<Border> Ghosts(Window window) =>
        AdornerLayer.GetAdornerLayer(window.GetVisualDescendants().OfType<Control>().First())!.Children.OfType<Border>().Where(b => b.Child is Image);

    private static (WrapPanel Row, Chip A, Chip B, Chip C, Window Window) Row(string dir)
    {
        var a = new Chip { Content = "الرياض", IsRemovable = true };
        var b = new Chip { Content = "عمّان", IsRemovable = true };
        var c = new Chip { Content = "القاهرة", IsRemovable = true };
        var row = new WrapPanel { Classes = { "chips" }, Children = { a, b, c } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, Dir(dir));
        return (row, a, b, c, window);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_removed_chip_leaves_the_row_at_once_and_its_neighbour_glides_from_where_it_was(string dir)
    {
        var (row, _, b, c, window) = Row(dir);
        var before = Drawn(c, window);
        Assert.True(b.Remove());
        Assert.DoesNotContain(b, row.Children);
        window.UpdateLayout();
        var start = Drawn(c, window);
        Assert.Equal(before.X, start.X, 0);
        Assert.Equal(before.Y, start.Y, 0);
        Snap(window, $"chips-leave-{dir}");
        var samples = Sample(window, 500, () => Drawn(c, window).X);
        Assert.Contains(samples, s => Math.Abs(s.Value - before.X) > 4 && Math.Abs(s.Value - samples[^1].Value) > 4);
        Assert.Null(c.RenderTransform);
        Assert.NotEqual(before.X, samples[^1].Value, 0);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_leaving_picture_sits_on_the_chip_then_falls_and_fades(string dir)
    {
        var (_, _, b, _, window) = Row(dir);
        var place = Drawn(b, window);
        var size = b.Bounds.Size;
        b.Remove();
        window.UpdateLayout();
        var ghost = Assert.Single(Ghosts(window));
        Assert.False(ghost.IsHitTestVisible);
        Assert.Equal(size, new Size(ghost.Width, ghost.Height));
        var at = ghost.TranslatePoint(default, window)!.Value;
        Assert.Equal(place.X, at.X, 0);
        Assert.Equal(place.Y, at.Y, 0);
        var samples = Sample(window, 450, () => (O: ghost.Opacity, Y: ghost.RenderTransform?.Value.M32 ?? 0));
        Assert.Contains(samples, s => s.Value.O < 0.9 && s.Value.O > 0);
        Assert.Contains(samples, s => s.Value.Y > 1);
        Assert.Empty(Ghosts(window));
    }

    [AvaloniaFact]
    public void The_picture_of_an_RTL_chip_reads_the_right_way_round()
    {
        var (_, _, b, _, window) = Row("rtl");
        Settle(100);
        var box = Box(b, window);
        var chip = Red(window.CaptureRenderedFrame()!, box);
        b.Remove();
        Frame(window);
        var ghost = Red(window.CaptureRenderedFrame()!, box);
        var same = chip.Zip(ghost).Count(p => Math.Abs(p.First - p.Second) < 40);
        Assert.True(same > chip.Length * 0.85, $"{same} of {chip.Length} pixels match");
    }

    // Where a control is drawn in the window, whichever corner its origin is in RTL.
    private static PixelRect Box(Control c, Window window)
    {
        var a = c.TranslatePoint(default, window)!.Value;
        var z = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), window)!.Value;
        return new PixelRect((int)Math.Round(Math.Min(a.X, z.X)), (int)Math.Round(Math.Min(a.Y, z.Y)), (int)c.Bounds.Width, (int)c.Bounds.Height);
    }

    // The red channel of a box of a frame, row by row.
    private static int[] Red(WriteableBitmap frame, PixelRect box)
    {
        using var buffer = frame.Lock();
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        var red = new int[box.Width * box.Height];
        for (var y = 0; y < box.Height; y++)
            for (var x = 0; x < box.Width; x++)
                red[y * box.Width + x] = bytes[(box.Y + y) * buffer.RowBytes + (box.X + x) * 4 + 2];
        return red;
    }

    [AvaloniaFact]
    public void A_new_tag_drops_in_and_the_box_glides_aside()
    {
        var tags = new TagsInput { PlaceholderText = "اكتب واضغط Enter", Width = 420 };
        tags.Values.Add("أول");
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { tags } }, FlowDirection.RightToLeft);
        var box = tags.InputBox!;
        var before = Drawn(box, window);
        tags.Values.Add("ثان");
        var chip = tags.GetVisualDescendants().OfType<Chip>().Last();
        window.UpdateLayout();
        Assert.Equal(before.X, Drawn(box, window).X, 0);
        var samples = Sample(window, 600, () => (Box: Drawn(box, window).X, O: chip.Opacity));
        Assert.Contains(samples, s => s.Value.O < 0.9);
        Assert.All(samples, s => Assert.InRange(s.Value.O, 0, 1));
        Assert.Equal(1, samples[^1].Value.O, 3);
        Assert.NotEqual(before.X, samples[^1].Value.Box, 0);
        Assert.Contains(samples, s => Math.Abs(s.Value.Box - before.X) > 4 && Math.Abs(s.Value.Box - samples[^1].Value.Box) > 4);
    }

    [AvaloniaFact]
    public void A_chip_never_goes_clear_while_it_arrives()
    {
        var chip = new Chip { Content = "الرياض" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { chip } });
        chip.PlayEnter();
        var samples = Sample(window, 600, () => chip.Opacity);
        Assert.All(samples, s => Assert.InRange(s.Value, 0, 1));
        Assert.Equal(1, samples[^1].Value, 3);
    }

    [AvaloniaFact]
    public void A_still_window_drops_the_chip_with_no_picture_and_no_glide()
    {
        var (row, _, b, c, window) = Row("rtl");
        Still(window, true);
        b.Remove();
        Assert.DoesNotContain(b, row.Children);
        Assert.Empty(Ghosts(window));
        Frame(window);
        Assert.Null(c.RenderTransform);
    }
}
