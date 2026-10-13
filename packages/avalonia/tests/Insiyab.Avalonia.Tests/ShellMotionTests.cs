using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ShellMotionTests
{
    // Pumps frames for a while and returns the lowest value seen, so a short entrance is caught whatever the frame timing.
    private static double Lowest(Window w, Func<double> read, int ms = 400)
    {
        var low = read();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < ms)
        {
            w.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            low = Math.Min(low, read());
            Thread.Sleep(8);
        }
        return low;
    }

    private static void Still(Window w, bool on) => Motion.SetStill(w, on);

    private static (Border Island, TextBlock Title) TitleIsland()
    {
        var title = new TextBlock { Classes = { "shell-title" }, Text = "العنوان" };
        var island = new Border { Classes = { "island", "title" }, Child = new StackPanel { Classes = { "shell-head" }, Children = { title } } };
        return (island, title);
    }

    [AvaloniaFact]
    public void Title_island_enters_again_when_the_title_changes()
    {
        var (island, title) = TitleIsland();
        var w = Show(new StackPanel { Children = { island } }, FlowDirection.RightToLeft);
        Settle();
        Assert.Equal(1, island.Opacity);

        title.Text = "الإعدادات";
        var low = Lowest(w, () => island.Opacity);
        Assert.True(low < 0.9, $"lowest opacity {low}");
        Settle();
        Assert.Equal(1, island.Opacity);
        Assert.True(island.RenderTransform is null || island.RenderTransform.Value.IsIdentity);
    }

    [AvaloniaFact]
    public void Title_island_stays_put_for_the_same_title()
    {
        var (island, title) = TitleIsland();
        var w = Show(new StackPanel { Children = { island } });
        Settle();
        title.Text = "العنوان";
        Assert.Equal(1, Lowest(w, () => island.Opacity, 200));
    }

    [AvaloniaFact]
    public void Title_island_does_not_move_when_motion_is_still()
    {
        var (island, title) = TitleIsland();
        var w = Show(new StackPanel { Children = { island } });
        Settle();
        Still(w, true);
        try
        {
            title.Text = "الإعدادات";
            Assert.Equal(1, Lowest(w, () => island.Opacity, 200));
        }
        finally { Still(w, false); }
    }

    [AvaloniaFact]
    public void Shell_content_host_rises_each_new_page_in()
    {
        var host = new TransitioningContentControl { Classes = { "shell-content" }, Content = new TextBlock { Text = "الأولى" } };
        var w = Show(host, FlowDirection.RightToLeft);
        Settle();
        Assert.IsType<RiseTransition>(host.PageTransition);

        var page = new TextBlock { Text = "الثانية" };
        host.Content = page;
        Dispatcher.UIThread.RunJobs();
        Visual Holder() => (Visual?)page.GetVisualParent() ?? page;
        var lowOpacity = Lowest(w, () => Holder().Opacity, 120);
        Assert.True(lowOpacity < 0.9, $"lowest opacity {lowOpacity}");
        Settle();
        Assert.Equal(1, Holder().Opacity);
        Assert.True(page.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Shell_content_host_swaps_at_once_when_motion_is_still()
    {
        var host = new TransitioningContentControl { Classes = { "shell-content" }, Content = new TextBlock { Text = "الأولى" } };
        var w = Show(host);
        Settle();
        Still(w, true);
        try
        {
            var page = new TextBlock { Text = "الثانية" };
            host.Content = page;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, Lowest(w, () => ((Visual?)page.GetVisualParent() ?? page).Opacity, 200));
            Assert.True(page.IsEffectivelyVisible);
        }
        finally { Still(w, false); }
    }

    private static Navbar Folded() => new()
    {
        Brand = "الموقع",
        ItemsSource = new[] { "الرئيسية", "المدونة", "تواصل معنا" },
        SelectedIndex = 0,
        Width = 600,
    };

    [AvaloniaFact]
    public void Folded_navbar_menu_rises_in_when_it_opens()
    {
        var bar = Folded();
        var w = Show(bar, FlowDirection.RightToLeft, width: 640, height: 480);
        Assert.True(bar.IsCollapsed);
        var menu = Part<Border>(bar, "PART_Menu");
        Assert.False(menu.IsVisible);

        bar.IsOpen = true;
        var low = Lowest(w, () => menu.Opacity, 200);
        Assert.True(low < 0.9, $"lowest opacity {low}");
        Settle();
        Assert.True(menu.IsEffectivelyVisible);
        Assert.Equal(1, menu.Opacity);
        Assert.True(menu.RenderTransform is null || menu.RenderTransform.Value.IsIdentity);
    }

    [AvaloniaFact]
    public void Folded_navbar_menu_opens_still_when_motion_is_still()
    {
        var bar = Folded();
        var w = Show(bar, width: 640, height: 480);
        var menu = Part<Border>(bar, "PART_Menu");
        Still(w, true);
        try
        {
            bar.IsOpen = true;
            Assert.Equal(1, Lowest(w, () => menu.Opacity, 200));
            Assert.True(menu.IsEffectivelyVisible);
        }
        finally { Still(w, false); }
    }
}
