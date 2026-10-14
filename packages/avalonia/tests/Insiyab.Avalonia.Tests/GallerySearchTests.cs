using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The gallery's page search: a button beside the page title and Ctrl+K, both over a palette of every page.
public class GallerySearchTests
{
    private static (GalleryShell Shell, Window Window) Open()
    {
        PageScrollTests.LoadIcons();
        var shell = new GalleryShell();
        var window = Show(shell, FlowDirection.RightToLeft, width: 1280, height: 800);
        Settle(500);
        return (shell, window);
    }

    private static CommandPalette PaletteOf(Window window) => OverlayLayer.GetOverlayLayer(window)!.Children.OfType<CommandPalette>().Single();

    private static string TitleOf(GalleryShell shell) => shell.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "PageTitle").Text!;

    [AvaloniaFact]
    public void The_search_button_opens_a_palette_of_every_page_and_choosing_one_opens_it()
    {
        var (shell, window) = Open();
        var button = shell.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "SearchButton");
        Assert.Equal("بحث", AutomationProperties.GetName(button));
        Click(button, window);

        var palette = PaletteOf(window);
        Assert.True(palette.IsOpen);
        Assert.Equal(GalleryPages.All.Count, palette.Items.Count);
        var page = GalleryPages.Find("tables")!;
        var item = palette.Items.Single(i => i.Key == page.Slug);
        Assert.Equal(page.Title, item.Label);
        Assert.Equal(page.Group, item.Group);
        Assert.Equal(page.En, item.Keywords);
        Assert.NotNull(item.Icon);
        Assert.NotEqual(page.Title, TitleOf(shell));

        palette.Choose(item);
        Settle(500);
        Assert.False(palette.IsOpen);
        Assert.Equal(page.Title, TitleOf(shell));
        Assert.Equal(page.Slug, palette.Items.Single(i => i.IsCurrent).Key);
    }

    [AvaloniaFact]
    public void Ctrl_K_opens_the_palette_and_slash_stays_with_the_sidebar_filter()
    {
        var (_, window) = Open();
        window.KeyPress(Key.Oem2, RawInputModifiers.None, PhysicalKey.Slash, "/");
        window.KeyRelease(Key.Oem2, RawInputModifiers.None, PhysicalKey.Slash, "/");
        Settle(200);
        Assert.DoesNotContain(OverlayLayer.GetOverlayLayer(window)!.Children, c => c is CommandPalette { IsOpen: true });

        window.KeyPress(Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");
        window.KeyRelease(Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");
        Settle(200);
        Assert.True(PaletteOf(window).IsOpen);
    }
}
