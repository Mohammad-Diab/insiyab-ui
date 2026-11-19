using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A tone class that comes from data (ins:Tone.Class), and the "something new" dot on any control (ins:Dot.IsShown) or its count (ins:Dot.Count).
public class ToneDotTests
{
    private static Rect BoxOf(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaFact]
    public void A_bound_tone_puts_its_class_and_takes_the_one_before_off()
    {
        var pill = new Pill { Content = "تنتهي غدًا", Classes = { "sm" } };
        var bar = new ProgressBar { Value = 40 };
        Show(new StackPanel { Children = { pill, bar } });
        var plain = pill.Background;

        Tone.SetClass(pill, "warn");
        Tone.SetClass(bar, "bad");
        Settle(50);
        Assert.Contains("warn", pill.Classes);
        Assert.Contains("sm", pill.Classes);
        Assert.Contains("bad", bar.Classes);
        Assert.True(pill.TryFindResource("InsToneWarnBg", pill.ActualThemeVariant, out var warn));
        Assert.Equal(((ISolidColorBrush)warn!).Color, ((ISolidColorBrush)pill.Background!).Color);

        Tone.SetClass(pill, "ok");
        Assert.DoesNotContain("warn", pill.Classes);
        Assert.Contains("ok", pill.Classes);

        Tone.SetClass(pill, null);
        Settle(50);
        Assert.DoesNotContain("ok", pill.Classes);
        Assert.Contains("sm", pill.Classes);
        Assert.Equal(((ISolidColorBrush?)plain)?.Color, ((ISolidColorBrush?)pill.Background)?.Color);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void A_dot_stands_at_the_controls_top_end_corner_without_changing_it(FlowDirection dir)
    {
        var bell = new Button { Classes = { "island", "icon" }, Content = new Icon { Data = Geometry.Parse("M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9") } };
        var w = Show(new StackPanel { Margin = new Thickness(40), HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center, Children = { bell } }, dir);
        Settle(100);
        var before = bell.Bounds;
        Assert.Null(Dot.MarkOf(bell));

        Dot.SetIsShown(bell, true);
        Settle(100);
        Assert.Equal(before, bell.Bounds);
        var mark = Dot.MarkOf(bell)!;
        Assert.True(mark.IsEffectivelyVisible);
        Assert.False(mark.IsHitTestVisible);
        var dot = mark.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("ins-dot"));
        var box = BoxOf(dot, w);
        var host = BoxOf(bell, w);
        var inset = Math.Max(4, Math.Round(Math.Min(host.Width, host.Height) * 0.146));
        Assert.Equal(9, box.Width, 1);
        Assert.Equal(host.Top + inset, box.Top, 1);
        if (dir == FlowDirection.RightToLeft)
            Assert.Equal(host.Left + inset, box.Left, 1);
        else
            Assert.Equal(host.Right - inset, box.Right, 1);
        Assert.True(dot.TryFindResource("InsNotifyCountBg", dot.ActualThemeVariant, out var red));
        Assert.Equal(((ISolidColorBrush)red!).Color, ((ISolidColorBrush)dot.Background!).Color);

        Dot.SetIsShown(bell, false);
        Settle(50);
        Assert.False(mark.IsVisible);
        Assert.Equal(before, bell.Bounds);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void A_count_stands_out_at_the_top_end_corner_as_the_notification_bells(FlowDirection dir)
    {
        var bell = new Button { Classes = { "island", "icon" }, Content = new Icon { Data = Geometry.Parse("M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9") } };
        var w = Show(new StackPanel { Margin = new Thickness(40), HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center, Children = { bell } }, dir);
        Settle(100);
        var before = bell.Bounds;

        Dot.SetCount(bell, 3);
        Settle(100);
        Assert.Equal(before, bell.Bounds);
        var mark = Dot.MarkOf(bell)!;
        var count = mark.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("ins-dot"));
        Assert.Contains("count", count.Classes);
        Assert.Equal("3", count.GetVisualDescendants().OfType<TextBlock>().Single().Text);
        var box = BoxOf(count, w);
        var host = BoxOf(bell, w);
        // Notify's PART_Count: a 20px badge ringed 2px by the page's colour (outside the badge's box, a shadow with no blur), its ring 7px past the top and the end
        Assert.Equal(20, box.Height, 1);
        Assert.Equal(20, box.Width, 1);
        Assert.Equal(host.Top - 5, box.Top, 1);
        if (dir == FlowDirection.RightToLeft)
            Assert.Equal(host.Left - 5, box.Left, 1);
        else
            Assert.Equal(host.Right + 5, box.Right, 1);
        Assert.Equal(default, count.BorderThickness);
        Assert.Equal(1, count.BoxShadow.Count);
        var ring = count.BoxShadow[0];
        Assert.Equal(2, ring.Spread);
        Assert.Equal(0, ring.Blur);
        Assert.False(ring.IsInset);
        Assert.True(count.TryFindResource("InsWash1", count.ActualThemeVariant, out var page));
        Assert.Equal(((ISolidColorBrush)page!).Color, ring.Color);

        Dot.SetCount(bell, 12);
        Settle(50);
        Assert.Equal("9+", count.GetVisualDescendants().OfType<TextBlock>().Single().Text);
        Assert.True(BoxOf(count, w).Width > 20);

        Dot.SetCount(bell, 0);
        Settle(50);
        Assert.False(mark.IsVisible);
    }

    // the adorner layer is drawn over every page and takes none of their opacity: a count takes its control's, so a page fading out or in takes its count with it
    [AvaloniaFact]
    public void A_count_fades_with_its_page()
    {
        var bell = new Button { Classes = { "island", "icon" }, Content = new Icon { Data = Geometry.Parse("M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9") } };
        var page = new Border { Opacity = 0.4, Padding = new Thickness(40), Child = new StackPanel { HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center, Children = { bell } } };
        Show(page, FlowDirection.RightToLeft);
        Dot.SetCount(bell, 3);
        Settle(100);
        var mark = Dot.MarkOf(bell)!;
        Assert.True(mark.IsVisible);
        Assert.Equal(0.4, mark.Opacity, 3);
        page.Opacity = 0;
        Assert.Equal(0, mark.Opacity, 3);
        page.Opacity = 1;
        Assert.Equal(1, mark.Opacity, 3);
    }
}
