using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Brand;
using Insiyab.Avalonia.Gallery;
using static Insiyab.Avalonia.Tests.Ui;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Insiyab.Avalonia.Tests;

// The gallery's brand marks are the library's logo, drawn as vectors in the brand colour and never mirrored.
public class BrandMarkTests
{
    private static readonly Color Teal = Color.Parse("#0F766E");

    private static (GalleryShell Shell, Window Window) Open(FlowDirection direction)
    {
        PageScrollTests.LoadIcons();
        var shell = new GalleryShell();
        var window = Show(shell, direction, width: 1280, height: 800);
        Settle(500);
        return (shell, window);
    }

    private static List<Border> Marks(GalleryShell shell) =>
        shell.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("shell-mark")).ToList();

    private static Border SidebarMark(GalleryShell shell) => Marks(shell).First(b => b.IsEffectivelyVisible);

    [AvaloniaFact]
    public void Both_gallery_brand_marks_are_the_logo()
    {
        var (shell, _) = Open(FlowDirection.RightToLeft);
        var marks = Marks(shell);
        Assert.Equal(2, marks.Count);
        foreach (var mark in marks)
        {
            Assert.Equal(Colors.Transparent, ColorOf(mark.Background));
            Assert.NotNull(Assert.IsType<TemplatedControl>(mark.Child).Template);
        }
        Assert.Equal(5, SidebarMark(shell).GetVisualDescendants().OfType<ShapePath>().Count());
    }

    [AvaloniaFact]
    public void The_brand_mark_takes_the_brand_colour_and_follows_SetPrimary()
    {
        var (shell, _) = Open(FlowDirection.RightToLeft);
        var paths = SidebarMark(shell).GetVisualDescendants().OfType<ShapePath>().ToList();
        GradientStops Deep() => Assert.IsType<LinearGradientBrush>(paths[0].Fill).GradientStops;
        try
        {
            Assert.Equal(InsiyabTheme.DefaultPrimary, Deep()[0].Color);
            Assert.Equal(InsiyabTheme.DefaultPrimary, ColorOf(paths[2].Fill));

            InsiyabTheme.SetPrimary(Teal);

            Assert.Equal(Teal, Deep()[0].Color);
            Assert.Equal(BrandDerivation.Derive(Teal, false).PrimaryDark, Deep()[2].Color);
            Assert.Equal(Teal, ColorOf(paths[2].Fill));
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void The_brand_mark_is_never_mirrored(FlowDirection direction)
    {
        var (shell, window) = Open(direction);
        var mark = SidebarMark(shell);
        var a = mark.TranslatePoint(default, window)!.Value;
        var b = mark.TranslatePoint(new Point(mark.Bounds.Width, mark.Bounds.Height), window)!.Value;
        var box = new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
        Point At(double x, double y) => new(box.X + x / 224 * box.Width, box.Y + y / 224 * box.Height);
        var frame = window.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var gap = PixelAt(buffer, (int)At(40, 102).X, (int)At(40, 102).Y);
        var pane = PixelAt(buffer, (int)At(184, 102).X, (int)At(184, 102).Y);
        Assert.True(gap.R > 200 && gap.G > 200, $"{direction}: the wave's gap on the left should show the light sidebar, got {gap}");
        Assert.True(pane.R < 190 && pane.B - pane.G > 40, $"{direction}: the light pane on the right should be brand-tinted, got {pane}");
    }
}
