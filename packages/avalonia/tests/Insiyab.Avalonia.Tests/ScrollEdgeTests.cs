using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The scroll bars: thin at rest, grown with their track under the pointer as Windows 11's, at once in a still window; and a scrolling list inside a card: its bar along the card's own edge, the card's padding inside the scrolled content.
public class ScrollEdgeTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static StackPanel Rows(int count = 30)
    {
        var rows = new StackPanel { Spacing = 4 };
        for (var i = 0; i < count; i++)
            rows.Children.Add(new TextBlock { Text = "Row " + i, HorizontalAlignment = HorizontalAlignment.Stretch });
        return rows;
    }

    private static Rect Box(Visual v, Visual to)
    {
        var a = v.TranslatePoint(default, to)!.Value;
        var b = v.TranslatePoint(new Point(v.Bounds.Width, v.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static ScrollBar Bar(ScrollViewer s) => s.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == Orientation.Vertical);

    // a card (a panel) with a padded body holding a heading and a list that scrolls on its own
    private static (Border Card, ScrollViewer Scroller, TextBlock Head, StackPanel Rows) Card(bool edge)
    {
        var head = new TextBlock { Text = "Items" };
        var rows = Rows();
        var scroller = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = rows };
        if (edge)
            ScrollEdge.SetCard(scroller, true);
        DockPanel.SetDock(head, Dock.Top);
        var card = new Border { Classes = { "panel" }, Height = 260, Width = 280, Child = new Border { Classes = { "panel-body" }, Child = new DockPanel { Children = { head, scroller } } } };
        return (card, scroller, head, rows);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_list_in_a_card_runs_its_bar_along_the_cards_edge_and_keeps_its_rows_in_place(string dir)
    {
        var (card, scroller, head, rows) = Card(edge: true);
        var w = Show(new Border { Padding = new Thickness(30), Child = card }, Dir(dir));
        Settle(300);
        var c = Box(card, w);
        var bar = Box(Bar(scroller), w);
        Assert.True(Bar(scroller).IsEffectivelyVisible, "thirty rows scroll");
        // the bar's outer edge on the card's own edge, inside its 1 px rim, on the end side
        var gap = dir == "rtl" ? bar.Left - c.Left : c.Right - bar.Right;
        Assert.InRange(gap, 0, 2);
        // the rows keep the room the card's padding gave them: as far from the card's edges as its heading, less the bar
        var r = Box(rows, w);
        var h = Box(head, w);
        var (startH, startR, endH, endR) = dir == "rtl" ? (h.Right, r.Right, h.Left, r.Left) : (h.Left, r.Left, h.Right, r.Right);
        Assert.Equal(startH, startR, 1.0);
        Assert.Equal(endH, endR, 6.0);
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height);
    }

    [AvaloniaFact]
    public void Without_it_the_bar_stays_inside_the_cards_padding_and_turning_it_off_puts_it_back()
    {
        var (card, scroller, _, _) = Card(edge: false);
        var w = Show(new Border { Padding = new Thickness(30), Child = card });
        var inset = Box(card, w).Right - Box(Bar(scroller), w).Right;
        Assert.True(inset > 15, $"the plain bar sits in the padding ({inset})");

        ScrollEdge.SetCard(scroller, true);
        Settle(300);
        Assert.InRange(Box(card, w).Right - Box(Bar(scroller), w).Right, 0, 2);
        ScrollEdge.SetCard(scroller, false);
        Settle(300);
        Assert.Equal(inset, Box(card, w).Right - Box(Bar(scroller), w).Right, 1.0);
        Assert.Equal(default, scroller.Margin);
        Assert.Equal(default, scroller.Padding);
    }

    [AvaloniaFact]
    public void A_number_lists_rows_scroll_with_their_bar_on_the_cards_edge()
    {
        var list = new NumberList { Header = "Items", Footer = "12 left", OpenText = "Show", CloseText = "Hide" };
        for (var i = 1; i <= 30; i++)
            list.Items.Add(new NumberListItem { Number = i, Text = "Item " + i });
        var card = new Border { Classes = { "panel" }, Height = 300, Width = 280, Child = new Border { Classes = { "panel-body" }, Child = list } };
        var w = Show(new Border { Padding = new Thickness(30), Child = card }, FlowDirection.RightToLeft);
        Settle(300);
        var scroller = Part<ScrollViewer>(list, "PART_Scroller");
        Assert.True(ScrollEdge.GetCard(scroller));
        Assert.InRange(Box(Bar(scroller), w).Left - Box(card, w).Left, 0, 2);
        // the strip has none: it scrolls sideways under its own edge
        list.Orientation = Orientation.Horizontal;
        Settle(300);
        Assert.False(ScrollEdge.GetCard(Part<ScrollViewer>(list, "PART_Scroller")));
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_bar_grows_with_its_track_under_the_pointer_and_thins_again_after(bool still)
    {
        var (card, scroller, _, _) = Card(edge: true);
        var w = Show(new Border { Padding = new Thickness(30), Child = card });
        Motion.SetStill(w, still);
        Settle(200);
        var bar = Bar(scroller);
        var rail = Part<Border>(bar, "PART_Rail");
        var thumb = Part<Thumb>(bar, "PART_Thumb");
        Assert.Equal((double)Res(w, "InsScrollbarSize"), bar.Bounds.Width);
        Assert.Equal((double)Res(w, "InsScrollbarSize"), rail.Bounds.Width, 0.6);
        Assert.Equal(0, ColorOf(rail.Background).A);
        var thin = thumb.Bounds.Width;

        // the pointer a little inside the content's edge, in the grown band: found
        var c = Box(card, w);
        var at = new Point(c.Right - 8, c.Top + c.Height / 2);
        w.MouseMove(at);
        if (still)
        {
            Dispatcher(30); // at once: no motion to wait for
            Assert.Equal((double)Res(w, "InsScrollbarHoverSize"), rail.Width, 0.5);
            Assert.Equal((double)Res(w, "InsScrollbarHoverSize"), rail.Bounds.Width, 0.5);
        }
        else
            Settle(500);
        Assert.Equal((double)Res(w, "InsScrollbarHoverSize"), rail.Bounds.Width, 0.5);
        AssertBrush(w, "InsScrollbarTrack", () => rail.Background);
        Assert.True(thumb.Bounds.Width > thin + 1, $"the thumb grows too ({thin} → {thumb.Bounds.Width})");
        Assert.Equal((double)Res(w, "InsScrollbarSize"), bar.Bounds.Width); // the layout's slot stays thin

        w.MouseMove(new Point(c.Left + 20, c.Top + c.Height / 2));
        Settle(500);
        Assert.Equal((double)Res(w, "InsScrollbarSize"), rail.Bounds.Width, 0.6);
    }

    [AvaloniaFact]
    public void A_bar_stays_grown_while_its_thumb_or_track_is_held_though_the_pointer_leaves_it()
    {
        var (card, scroller, _, _) = Card(edge: true);
        var w = Show(new Border { Padding = new Thickness(30), Child = card });
        Motion.SetStill(w, true);
        Settle(200);
        var bar = Bar(scroller);
        var rail = Part<Border>(bar, "PART_Rail");
        var thumb = Part<Thumb>(bar, "PART_Thumb");
        var grown = (double)Res(w, "InsScrollbarHoverSize");
        var thin = (double)Res(w, "InsScrollbarSize");

        var t = Box(thumb, w);
        var on = t.Center;
        w.MouseMove(on);
        Dispatcher(30);
        w.MouseDown(on, MouseButton.Left);
        Dispatcher(30);
        Assert.Equal(grown, rail.Bounds.Width, 0.5);
        // dragged down and far to the side, out of the bar and the card
        w.MouseMove(new Point(on.X - 200, on.Y + 40));
        Dispatcher(30);
        w.MouseMove(new Point(on.X - 260, on.Y + 60));
        Dispatcher(30);
        Assert.True(scroller.Offset.Y > 0, "the drag scrolled");
        Assert.Equal(grown, rail.Bounds.Width, 0.5);
        Assert.Equal(grown, rail.Width, 0.5);
        w.MouseUp(new Point(on.X - 260, on.Y + 60), MouseButton.Left);
        Dispatcher(30);
        Assert.Equal(thin, rail.Bounds.Width, 0.6);

        // a press held on the track pages and keeps it grown while it is held
        var c = Box(card, w);
        var track = new Point(Box(rail, w).Center.X, c.Bottom - 30);
        w.MouseMove(track);
        Dispatcher(30);
        w.MouseDown(track, MouseButton.Left);
        Dispatcher(30);
        Assert.Equal(grown, rail.Bounds.Width, 0.5);
        w.MouseUp(track, MouseButton.Left);
        Dispatcher(30);
        Assert.Equal(grown, rail.Bounds.Width, 0.5);
        w.MouseMove(new Point(c.Left + 20, c.Top + c.Height / 2));
        Dispatcher(30);
        Assert.Equal(thin, rail.Bounds.Width, 0.6);
    }

    [AvaloniaFact]
    public void A_bar_eases_as_it_grows_and_as_it_thins_again()
    {
        var (card, scroller, _, _) = Card(edge: true);
        var w = Show(new Border { Padding = new Thickness(30), Child = card });
        Settle(200);
        var rail = Part<Border>(Bar(scroller), "PART_Rail");
        var grown = (double)Res(w, "InsScrollbarHoverSize");
        var thin = (double)Res(w, "InsScrollbarSize");
        var c = Box(card, w);

        bool Between(Action move)
        {
            move();
            var seen = false;
            for (var i = 0; i < 20; i++)
            {
                Dispatcher(12);
                if (rail.Bounds.Width > thin + 0.5 && rail.Bounds.Width < grown - 0.5) seen = true;
            }
            return seen;
        }

        Assert.True(Between(() => w.MouseMove(new Point(c.Right - 8, c.Top + c.Height / 2))), "a width between thin and grown on the way up");
        Settle(300);
        Assert.Equal(grown, rail.Bounds.Width, 0.5);
        Assert.True(Between(() => w.MouseMove(new Point(c.Left + 20, c.Top + c.Height / 2))), "a width between grown and thin on the way down");
        Settle(300);
        Assert.Equal(thin, rail.Bounds.Width, 0.6);
    }

    [AvaloniaTheory]
    [InlineData("window")]
    [InlineData("bar")]
    public void Thin_scroll_keeps_a_bar_thin_under_the_pointer_and_while_dragged(string where)
    {
        var (card, scroller, _, _) = Card(edge: true);
        var w = Show(new Border { Padding = new Thickness(30), Child = card });
        Motion.SetStill(w, true);
        Settle(200);
        var bar = Bar(scroller);
        if (where == "window") w.Classes.Add("thin-scroll");
        else bar.Classes.Add("thin-scroll");
        Settle(60);
        var rail = Part<Border>(bar, "PART_Rail");
        var thumb = Part<Thumb>(bar, "PART_Thumb");
        var thin = (double)Res(w, "InsScrollbarSize");
        var width = thumb.Bounds.Width;

        var on = Box(thumb, w).Center;
        w.MouseMove(on);
        Dispatcher(30);
        Assert.Equal(thin, rail.Bounds.Width, 0.6);
        Assert.Equal(width, thumb.Bounds.Width, 0.5);
        Assert.Equal(0, ColorOf(rail.Background).A);
        w.MouseDown(on, MouseButton.Left);
        w.MouseMove(new Point(on.X - 200, on.Y + 40));
        Dispatcher(30);
        Assert.Equal(thin, rail.Bounds.Width, 0.6);
        w.MouseUp(new Point(on.X - 200, on.Y + 40), MouseButton.Left);
        Dispatcher(30);
        // the band over the content's edge is gone too: a press there reaches the content
        Assert.Null(Part<Panel>(bar, "PART_Zone").Background);
    }

    private static void Dispatcher(int ms)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Thread.Sleep(ms);
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }

    [AvaloniaFact]
    public void A_dense_tile_is_a_fingers_row_high_at_least()
    {
        var tile = new RadioButton { Classes = { "tile", "dense" }, Content = "33" };
        var plain = new RadioButton { Classes = { "tile" }, Content = "33" };
        Show(new StackPanel { Width = 300, Spacing = 8, Children = { tile, plain } }, FlowDirection.RightToLeft);
        Assert.True(tile.Bounds.Height >= 48 - 0.5, $"dense {tile.Bounds.Height}");
        Assert.True(tile.Bounds.Height <= plain.Bounds.Height, $"no taller than the plain tile ({plain.Bounds.Height})");
    }
}
