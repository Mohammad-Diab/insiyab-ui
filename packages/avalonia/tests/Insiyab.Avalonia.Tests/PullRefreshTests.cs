using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Pull to refresh in the Insiyab look: the brand spinner on a small glass disc that comes down with the pull and leaves when the app is done.
public class PullRefreshTests
{
    private static (RefreshContainer Container, ScrollViewer Scroller) Make()
    {
        var rows = new StackPanel();
        for (var i = 0; i < 40; i++) rows.Children.Add(new Border { Height = 44, Child = new TextBlock { Text = "File " + i } });
        var scroller = new ScrollViewer { Content = rows };
        return (new RefreshContainer { Content = scroller }, scroller);
    }

    private static List<BoxShadow> Each(BoxShadows shadows)
    {
        var all = new List<BoxShadow>();
        for (var i = 0; i < shadows.Count; i++) all.Add(shadows[i]);
        return all;
    }

    private static RefreshDisc Disc(RefreshContainer c) => c.Visualizer!.GetVisualDescendants().OfType<RefreshDisc>().Single();

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_pull_brings_the_disc_down_raises_refresh_and_the_disc_leaves_when_the_deferral_completes(string dir)
    {
        var (container, _) = Make();
        var requests = 0;
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) =>
        {
            requests++;
            deferral = e.GetDeferral();
        };
        var w = Show(container, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 360, height: 600);
        var disc = Disc(container);
        var arc = Assert.IsType<RefreshArc>(disc.Child);
        Assert.IsAssignableFrom<LoadingSpinner>(arc);
        Assert.Equal(0, disc.Opacity);
        Assert.Equal(180, Center(disc, w).X, 1);
        Assert.Equal(FlowDirection.LeftToRight, arc.FlowDirection);

        var from = new Point(180, 120);
        var t = w.TouchBegin(from);
        Settle(40);
        for (var k = 1; k <= 4; k++)
        {
            w.TouchMove(t, from + new Vector(0, 10 * k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Interacting, disc.State);
        var early = disc.Opacity;
        var shortArc = arc.ArcLength;
        Assert.True(early > 0, "the disc shows as the pull begins");
        for (var k = 5; k <= 16; k++)
        {
            w.TouchMove(t, from + new Vector(0, 10 * k));
            Settle(16);
        }
        Assert.Equal(RefreshVisualizerState.Pending, disc.State);
        Assert.Equal(1, disc.Opacity);
        Assert.True(arc.ArcLength > shortArc, "the ring grows with the pull");
        var visual = ElementComposition.GetElementVisual(container.Visualizer!)!;
        Assert.True(visual.Offset.Y > 40, $"the visualizer came down {visual.Offset.Y}");

        w.TouchEnd(t, from + new Vector(0, 160));
        Settle(400);
        Assert.Equal(1, requests);
        Assert.Equal(RefreshVisualizerState.Refreshing, disc.State);
        var a1 = arc.Angle;
        Settle(200);
        Assert.NotEqual(a1, arc.Angle);
        Assert.Equal(90, arc.ArcLength, 1);
        Assert.Equal(180, Center(disc, w).X, 1);

        deferral!.Complete();
        Settle(400);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
        Assert.Equal(0, disc.Opacity);
        Assert.Equal(0, ElementComposition.GetElementVisual(container.Visualizer!)!.Offset.Y);
    }

    [AvaloniaFact]
    public void The_disc_is_glass_with_the_brand_ring_and_simpler_under_light_effects()
    {
        var (container, _) = Make();
        var w = Show(container, width: 360, height: 600);
        var disc = Disc(container);
        Assert.Equal(new Size(40, 40), disc.Bounds.Size);
        Assert.Equal(new CornerRadius(20), disc.CornerRadius);
        AssertBrush(disc, "InsPrimaryMid", () => ((RefreshArc)disc.Child!).Arc);
        Assert.Equal(1, disc.ZIndex);
        var full = disc.BoxShadow;
        Assert.Contains(Each(full), s => s.Blur > 0);
        try
        {
            InsiyabTheme.Effects = EffectsLevel.Light;
            Settle(100);
            Assert.All(Each(disc.BoxShadow), s => Assert.True(s.Blur <= 12));
            Motion.SetStill(w, true);
            Settle(100);
            Assert.All(Each(disc.BoxShadow), s => Assert.True(s.Blur == 0 || s.IsInset));
        }
        finally { InsiyabTheme.Effects = EffectsLevel.Full; }
    }

    [AvaloniaFact]
    public void F5_refreshes_from_the_keyboard_and_a_still_window_draws_the_spinner_at_once()
    {
        var (container, scroller) = Make();
        var requests = 0;
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) =>
        {
            requests++;
            deferral = e.GetDeferral();
        };
        var field = new TextBox();
        ((StackPanel)scroller.Content!).Children.Insert(0, field);
        var w = Show(container, width: 360, height: 600);
        Motion.SetStill(w, true);
        field.Focus();
        w.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
        Settle(300);
        Assert.Equal(1, requests);
        var disc = Disc(container);
        Assert.Equal(RefreshVisualizerState.Refreshing, disc.State);
        Assert.Equal(90, ((RefreshArc)disc.Child!).ArcLength, 1);
        deferral!.Complete();
        Settle(200);
        Assert.Equal(RefreshVisualizerState.Idle, disc.State);
    }
}
