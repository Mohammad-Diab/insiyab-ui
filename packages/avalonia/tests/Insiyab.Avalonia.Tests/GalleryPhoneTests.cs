using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The gallery at a phone's width: the menu or back, the title, the bell and the account in the top bar; search floats at the foot; the theme moves into the account pop.
public class GalleryPhoneTests
{
    private static (GalleryShell Shell, Window Window) Open(double width)
    {
        PageScrollTests.LoadIcons();
        var shell = new GalleryShell();
        var window = Show(shell, FlowDirection.RightToLeft, width: width, height: 800);
        Settle(500);
        return (shell, window);
    }

    private static T Named<T>(Visual root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static string TitleOf(GalleryShell shell) => Named<TextBlock>(shell, "PageTitle").Text!;

    private static Rect BoxOf(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static bool Back(TopLevel top)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        top.RaiseEvent(e);
        return e.Handled;
    }

    // The islands the top bar shows, by name or kind.
    private static List<string> Shown(GalleryShell shell)
    {
        var bar = shell.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("topbar"));
        var shown = new List<string>();
        foreach (var c in bar.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("island") || c is Notify or DayNight))
        {
            if (!c.IsEffectivelyVisible || c.Bounds.Width <= 0 || c.GetVisualAncestors().OfType<Notify>().Any()) continue;
            shown.Add(c switch
            {
                Notify => "bell",
                DayNight => "theme",
                Border b when b.Classes.Contains("title") => "title",
                Border b when b.Classes.Contains("site-version") => "version",
                Border b when b.Classes.Contains("brand") => "brand",
                _ => c.Name ?? c.GetType().Name,
            });
        }
        return shown.Distinct().ToList();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void At_a_phone_width_the_bar_holds_the_menu_the_title_the_bell_and_the_account(string dir)
    {
        var (shell, w) = Open(400);
        if (dir == "ltr")
        {
            shell.FindControl<Segmented>("DirectionSeg")!.SelectedIndex = 1;
            Settle(900);
            Assert.Equal(FlowDirection.LeftToRight, w.FlowDirection);
        }
        Assert.True(shell.IsPhone);
        Assert.Equal(["MenuButton", "title", "bell", "AppearanceButton"], Shown(shell));

        var bar = shell.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("topbar"));
        var islands = bar.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("island") && c.IsEffectivelyVisible && c.Bounds.Width > 0).Select(c => BoxOf(c, w)).ToList();
        Assert.All(islands, r => Assert.True(r.Left >= 0 && r.Right <= 400, $"an island at {r.Left}-{r.Right} leaves the 400px bar"));

        var fab = Named<Button>(shell, "SearchFab");
        Assert.True(fab.IsEffectivelyVisible);
        Assert.Contains("fab", fab.Classes);
        var box = BoxOf(fab, w);
        Assert.Equal(800 - 16, box.Bottom, 1);
        if (dir == "rtl") Assert.Equal(16, box.Left, 1);
        else Assert.Equal(400 - 16, box.Right, 1);
        Assert.Equal("بحث", global::Avalonia.Automation.AutomationProperties.GetName(fab));

        var theme = shell.FindControl<Button>("ThemeButton");
        Assert.NotNull(theme);
        Assert.True(theme!.IsVisible);
        Assert.Equal(ThemeMode.Toggle, ThemeToggle.GetMode(theme));
        Assert.Equal("الوضع الداكن", theme.Content);
    }

    [AvaloniaFact]
    public void A_wide_window_keeps_its_bar_and_has_no_floating_search()
    {
        var (shell, _) = Open(1280);
        Assert.False(shell.IsPhone);
        Assert.Equal(["MenuButton", "version", "title", "SearchButton", "theme", "bell", "AppearanceButton"], Shown(shell));
        Assert.False(Named<Button>(shell, "SearchFab").IsEffectivelyVisible);
        Assert.False(shell.FindControl<Button>("ThemeButton")!.IsVisible);
        Assert.False(shell.GoBack());
    }

    [AvaloniaFact]
    public void Search_from_the_floating_button_leaves_a_way_back_and_a_sidebar_link_starts_over()
    {
        var (shell, w) = Open(400);
        var first = TitleOf(shell);
        Click(Named<Button>(shell, "SearchFab"), w);
        var palette = OverlayLayer.GetOverlayLayer(w)!.Children.OfType<CommandPalette>().Single();
        Assert.True(palette.IsOpen);
        palette.Choose(palette.Items.Single(i => i.Key == "tables"));
        Settle(400);
        Assert.Equal(GalleryPages.Find("tables")!.Title, TitleOf(shell));
        Assert.Equal(["BackButton", "title", "bell", "AppearanceButton"], Shown(shell));

        palette.Open(Named<Button>(shell, "SearchFab"));
        palette.Choose(palette.Items.Single(i => i.Key == "chips"));
        Settle(400);
        Assert.True(Back(w));
        Settle(300);
        Assert.Equal(GalleryPages.Find("tables")!.Title, TitleOf(shell));
        Click(Named<Button>(shell, "BackButton"), w);
        Assert.Equal(first, TitleOf(shell));
        Assert.Equal(["MenuButton", "title", "bell", "AppearanceButton"], Shown(shell));
        Assert.False(Back(w));

        palette.Open(Named<Button>(shell, "SearchFab"));
        palette.Choose(palette.Items.Single(i => i.Key == "tables"));
        Settle(300);
        Assert.Contains("BackButton", Shown(shell));
        Assert.True(shell.Select("buttons"));
        Settle(300);
        Assert.DoesNotContain("BackButton", Shown(shell));
        Assert.False(Back(w));
    }

    [AvaloniaFact]
    public void A_single_view_is_the_phone_form_at_any_width()
    {
        PageScrollTests.LoadIcons();
        var shell = new GalleryShell();
        var root = OverlaysTests.SingleView(shell, 1000, 800);
        try
        {
            OverlaysTests.Pump(root);
            Assert.True(shell.IsPhone);
            Assert.True(Named<Button>(shell, "SearchFab").IsEffectivelyVisible);
            Assert.DoesNotContain("SearchButton", Shown(shell));
            Assert.DoesNotContain("theme", Shown(shell));
        }
        finally { root.StopRendering(); }
    }
}
