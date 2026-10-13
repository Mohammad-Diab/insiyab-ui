using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Insiyab.Avalonia.Tests;

// Shared helpers for the headless control tests; copy one into your own file rather than changing it here.
public static class Ui
{
    public static Window Show(Control content, FlowDirection direction = FlowDirection.LeftToRight, ThemeVariant? theme = null, double width = 640, double height = 420)
    {
        var window = new Window
        {
            Width = width,
            Height = height,
            FlowDirection = direction,
            RequestedThemeVariant = theme ?? ThemeVariant.Light,
            Content = content,
        };
        window.Show();
        Settle();
        return window;
    }

    // The headless clock is real time, so transitions need real time to finish.
    public static void Settle(int milliseconds = 750)
    {
        // The first pass can spend its time on JIT and font loading, so the clock starts after it.
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var clock = Stopwatch.StartNew();
        do
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(8);
        }
        while (clock.ElapsedMilliseconds < milliseconds);
        Dispatcher.UIThread.RunJobs();
    }

    public static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    public static object Res(Visual anchor, string key)
    {
        var theme = (anchor as IThemeVariantHost)?.ActualThemeVariant ?? TopLevel.GetTopLevel(anchor)!.ActualThemeVariant;
        Assert.True(Application.Current!.TryFindResource(key, theme, out var value), $"missing resource {key}");
        return value!;
    }

    public static Point Center(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    public static void Click(Control control, Window window)
    {
        var p = Center(control, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Settle();
    }

    public static void PressSpace(Window window)
    {
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Settle();
    }

    public static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // A finished brush transition can land a few units off its target, so colours are compared with a small tolerance.
    public static void AssertBrush(Visual anchor, string key, IBrush? actual)
    {
        var expected = ColorOf((IBrush)Res(anchor, key));
        var got = ColorOf(actual);
        Assert.True(Math.Abs(expected.A - got.A) <= 6 && Math.Abs(expected.R - got.R) <= 6 && Math.Abs(expected.G - got.G) <= 6 && Math.Abs(expected.B - got.B) <= 6, $"{key}: expected {expected}, got {got}");
    }

    public static IEnumerable<object[]> Looks() =>
        from theme in new[] { "Light", "Dark" }
        from dir in new[] { "rtl", "ltr" }
        select new object[] { theme, dir };

    public static string ShotsDir()
    {
        var dir = Environment.GetEnvironmentVariable("INS_SHOTS_DIR") ?? Path.Combine(Path.GetTempPath(), "insiyab-shots-controls");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void Capture(Window window, string name)
    {
        Settle();
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame!.Save(Path.Combine(ShotsDir(), name + ".png"), PngBitmapEncoderOptions.Default);
    }

    public static StackPanel Row(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        row.Children.AddRange(children);
        return row;
    }
}
