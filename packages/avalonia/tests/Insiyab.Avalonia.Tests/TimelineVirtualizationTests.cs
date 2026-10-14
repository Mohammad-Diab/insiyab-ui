using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TimelineVirtualizationTests
{
    private const int Count = 2000;

    private static List<TimelineItem> Rendered(Visual root) =>
        root.GetVisualDescendants().OfType<TimelineItem>().Where(i => i.IsVisible).ToList();

    private static Timeline Big()
    {
        var tl = new Timeline();
        for (var i = 0; i < Count; i++) tl.Items.Add(new TimelineItem { Header = $"حدث {i}", Time = "8:02 ص" });
        return tl;
    }

    private static Timeline Mixed()
    {
        var tl = new Timeline();
        tl.Items.Add(new TimelineDay { Content = "24 سبتمبر" });
        tl.Items.Add(new TimelineItem { Header = "أ" });
        tl.Items.Add(new TimelineItem { Header = "ب", IsPending = true });
        tl.Items.Add(new TimelineItem { Header = "ج" });
        tl.Items.Add(new TimelineDay { Content = "25 سبتمبر" });
        tl.Items.Add(new TimelineItem { Header = "د" });
        return tl;
    }

    private static List<Control> Entries(Timeline tl) => tl.Items.Cast<Control>().ToList();

    private static bool Has(Control c, string pseudo) => c.Classes.Contains(pseudo);

    [AvaloniaFact]
    public void Two_thousand_events_in_a_scroller_realize_only_those_in_view()
    {
        var tl = Big();
        Show(new ScrollViewer { Height = 400, Content = tl });
        var realized = Rendered(tl).Count;
        Assert.InRange(realized, 1, 99);
    }

    [AvaloniaFact]
    public void Places_match_the_old_panel_for_a_mixed_list()
    {
        var tl = Mixed();
        tl.Classes.Add("split");
        Show(tl, width: 800);
        var all = Entries(tl);
        var events = all.OfType<TimelineItem>().ToList();

        Assert.True(Has(all[0], ":first"));
        Assert.False(Has(all[4], ":first"));
        Assert.Equal(new[] { true, false, false, false }, events.Select(e => Has(e, ":next-pending")));
        Assert.Equal(new[] { false, false, false, true }, events.Select(e => Has(e, ":last")));
        Assert.All(all, c => Assert.True(Has(c, ":split")));
        Assert.Equal(new[] { false, true, false, true }, events.Select(e => Has(e, ":split-start")));
        for (var k = 0; k < events.Count; k++)
        {
            Assert.Equal(k % 2 == 0 ? 400 : 0, events[k].Bounds.X);
            Assert.Equal(400, events[k].Bounds.Width);
        }
        foreach (var day in all.OfType<TimelineDay>())
        {
            Assert.Equal(0, day.Bounds.X);
            Assert.Equal(800, day.Bounds.Width);
        }

        var narrow = Mixed();
        narrow.Classes.Add("split");
        Show(narrow, width: 600);
        Assert.All(Entries(narrow), c =>
        {
            Assert.False(Has(c, ":split"));
            Assert.Equal(0, c.Bounds.X);
            Assert.Equal(600, c.Bounds.Width);
        });
        Assert.All(Entries(narrow).OfType<TimelineItem>(), e => Assert.False(Has(e, ":split-start")));
    }

    [AvaloniaFact]
    public void The_last_event_after_scrolling_to_the_end_has_no_rail()
    {
        var tl = Big();
        var scroller = new ScrollViewer { Height = 400, Content = tl };
        Show(scroller);
        for (var i = 0; i < 6; i++)
        {
            scroller.ScrollToEnd();
            Settle(200);
        }
        var last = Assert.IsType<TimelineItem>(tl.Items[Count - 1]);
        var shown = Rendered(tl);
        Assert.InRange(shown.Count, 1, 99);
        Assert.Contains(last, shown);
        Assert.True(Has(last, ":last"));
        Assert.False(Part<Border>(last, "PART_Rail").IsVisible);
        Assert.Single(shown, e => Has(e, ":last"));
    }

    [AvaloniaFact]
    public void Turning_an_event_pending_redraws_the_rail_above_it()
    {
        var tl = new Timeline();
        for (var i = 0; i < 5; i++) tl.Items.Add(new TimelineItem { Header = $"حدث {i}" });
        Show(tl);
        var second = Assert.IsType<TimelineItem>(tl.Items[2]);
        var third = Assert.IsType<TimelineItem>(tl.Items[3]);
        Assert.False(Has(second, ":next-pending"));

        third.IsPending = true;
        Settle(200);
        Assert.True(Has(second, ":next-pending"));
        Assert.False(Has(third, ":next-pending"));
        Assert.False(Has(Assert.IsType<TimelineItem>(tl.Items[1]), ":next-pending"));

        third.IsPending = false;
        Settle(200);
        Assert.False(Has(second, ":next-pending"));
    }

    [AvaloniaFact]
    public void Resizing_below_640_leaves_split()
    {
        var tl = Mixed();
        tl.Classes.Add("split");
        var w = Show(tl, width: 800);
        var events = Entries(tl).OfType<TimelineItem>().ToList();
        Assert.All(Entries(tl), c => Assert.True(Has(c, ":split")));
        Assert.Equal(400, events[0].Bounds.X);

        w.Width = 600;
        Settle();
        Assert.All(Entries(tl), c =>
        {
            Assert.False(Has(c, ":split"));
            Assert.Equal(default(Thickness), c.Margin);
            Assert.Equal(0, c.Bounds.X);
        });
        Assert.All(events, e => Assert.Equal(600, e.Bounds.Width));
    }
}
