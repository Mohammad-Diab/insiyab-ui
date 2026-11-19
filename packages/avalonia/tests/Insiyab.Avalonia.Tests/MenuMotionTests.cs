using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A menu that grows from its corner, its levels sliding, and the play glyph that turns into pause outside the media row.
public class MenuMotionTests
{
    private static (Window, Button, PopCard, SlidePages) Rig(FlowDirection dir)
    {
        var pages = new SlidePages();
        pages.Show(Page(2));
        var card = new PopCard { IsEnd = true, GrowsFromCorner = true, Content = pages };
        var trigger = new Button { Content = "⋮", Flyout = card, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { trigger } }, dir, null, 800, 600);
        return (window, trigger, card, pages);
    }

    private static StackPanel Page(int rows)
    {
        var page = new StackPanel();
        for (var i = 0; i < rows; i++) page.Children.Add(new Button { Classes = { "pop-item" }, Content = "خيار " + i, Height = 40 });
        return page;
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void The_card_grows_from_its_corner_by_the_trigger_and_shrinks_back_into_it(FlowDirection dir)
    {
        var (window, trigger, card, _) = Rig(dir);
        Click(trigger, window);
        var surface = card.Grow.Surface!;
        Assert.Equal(PopGrow.CornerScale, card.Grow.Start!.Value.M11, 3);
        // the corner nearest the trigger: the trigger's centre held inside the card, on the card's top edge (it hangs below)
        var at = card.Grow.CornerAt!.Value;
        var centre = trigger.TranslatePoint(new Point(trigger.Bounds.Width / 2, trigger.Bounds.Height / 2), surface)!.Value;
        Assert.Equal(0, at.Y, 1);
        Assert.Equal(Math.Clamp(centre.X, 0, surface.Bounds.Width), at.X, 1);
        Assert.True(Math.Abs(at.X - centre.X) < trigger.Bounds.Width, "the card grows from the trigger's side");
        Settle(600);
        Assert.Equal(Matrix.Identity, (surface.RenderTransform as TransformOperations)?.Value ?? Matrix.Identity);
        Assert.Equal(1, surface.Opacity);

        card.Hide();
        Assert.True(card.IsOpen);
        Assert.True(card.Grow.IsShrinking);
        Settle(400);
        Assert.False(card.IsOpen);
    }

    [AvaloniaFact]
    public void A_still_window_opens_the_card_at_once()
    {
        Motion.SystemOverride = true;
        try
        {
            var (window, trigger, card, _) = Rig(FlowDirection.RightToLeft);
            Click(trigger, window);
            Assert.Null(card.Grow.Start);
            card.Hide();
            Assert.False(card.IsOpen);
        }
        finally
        {
            Motion.SystemOverride = null;
        }
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void A_level_slides_in_from_the_end_and_back_from_the_start_as_the_height_eases(FlowDirection dir)
    {
        var (window, trigger, card, pages) = Rig(dir);
        Click(trigger, window);
        Settle(600);
        var first = pages.Page!;
        var low = pages.Bounds.Height;
        var next = Page(5);
        pages.Go(next);
        Assert.True(pages.IsSliding);
        Settle(60);
        Assert.True(pages.IsSliding, "still under way");
        // mid-way: the new level stands to the end side of the old one, the height between the two
        var end = dir == FlowDirection.RightToLeft ? -1 : 1;
        var newX = next.TranslatePoint(default, window)!.Value.X;
        var oldX = first.TranslatePoint(default, window)!.Value.X;
        Assert.True((newX - oldX) * end > 0, $"the new level comes from the end side ({newX} vs {oldX})");
        Assert.InRange(pages.Bounds.Height, low, next.DesiredSize.Height);
        Settle(600);
        Assert.False(pages.IsSliding);
        Assert.DoesNotContain(first, pages.Children);
        Assert.Equal(next.DesiredSize.Height, pages.Bounds.Height, 1);

        var back = Page(2);
        pages.Go(back, back: true);
        Settle(60);
        newX = back.TranslatePoint(default, window)!.Value.X;
        oldX = next.TranslatePoint(default, window)!.Value.X;
        Assert.True((newX - oldX) * end < 0, "going back, the level comes from the start side");
        Settle(600);
        Assert.Equal(low, pages.Bounds.Height, 1);
        Assert.True(card.IsOpen);
    }

    [AvaloniaFact]
    public void A_still_window_swaps_the_level_at_once()
    {
        var (window, trigger, _, pages) = Rig(FlowDirection.RightToLeft);
        Click(trigger, window);
        Settle(600);
        Motion.SystemOverride = true;
        try
        {
            var next = Page(4);
            pages.Go(next);
            Assert.False(pages.IsSliding);
            Assert.Same(next, Assert.Single(pages.Children));
        }
        finally
        {
            Motion.SystemOverride = null;
        }
    }

    [AvaloniaFact]
    public void The_play_glyph_turns_into_pause_and_back_outside_the_media_row()
    {
        var icon = new PlayPauseIcon { Size = 34, IsFilled = true, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Border { Child = icon });
        Assert.Equal(PlayMorph.Shape(0).Bounds, icon.Data!.Bounds);
        icon.IsPlaying = true;
        Settle(60);
        Assert.InRange(icon.Progress, 0.01, 0.99);
        Settle(500);
        Assert.Equal(PlayMorph.Shape(1).Bounds, icon.Data!.Bounds);
        Assert.Equal(34, icon.Bounds.Width, 1);
        icon.IsPlaying = false;
        Settle(500);
        Assert.Equal(PlayMorph.Shape(0).Bounds, icon.Data!.Bounds);
        Capture(window, "play-pause-icon");
    }
}
