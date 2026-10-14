using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Gallery;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A page opened from the sidebar starts at its top, whatever the page before it was scrolled to.
public class PageScrollTests
{
    private static void LoadIcons()
    {
        var app = Application.Current!;
        if (app.TryGetResource("IconCheck", null, out _)) return;
        app.Resources.MergedDictionaries.Add((ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Insiyab.Avalonia.Gallery/Icons.axaml")));
    }

    [AvaloniaTheory]
    [InlineData("tables", "shell")]
    [InlineData("shell", "buttons")]
    [InlineData("inputs", "hijri")]
    public void Opening_a_page_starts_at_its_top(string first, string next)
    {
        LoadIcons();
        var shell = new GalleryShell();
        Show(shell, FlowDirection.RightToLeft, width: 1280, height: 800);
        Assert.True(shell.Select(first));
        Settle(1000);
        var scroller = shell.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == "Scroller");
        scroller.Offset = new Vector(0, Math.Max(0, scroller.Extent.Height - scroller.Viewport.Height) * 0.7);
        Settle(300);
        Assert.True(scroller.Offset.Y > 200, $"the first page scrolled only to {scroller.Offset.Y}");
        Assert.True(shell.Select(next));
        Settle(1500);
        Assert.Equal(0, scroller.Offset.Y, 1);
    }
}
