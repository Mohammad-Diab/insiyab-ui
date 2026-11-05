using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TocMarginTests
{
    private sealed record Page(Panel Root, ScrollViewer Scroller, Toc Toc, List<Border> Sections, Grid Column);

    // A page that scrolls under a top bar, its sections in a column with the contents list beside them.
    private static Page Make(Action<Toc>? set = null, bool frosted = false, double barHeight = 60)
    {
        var sections = new List<Border>();
        var column = new StackPanel();
        var toc = new Toc { Title = "المحتويات", Width = 160, VerticalAlignment = VerticalAlignment.Top };
        for (var i = 0; i < 5; i++)
        {
            var section = new Border { Height = 400, Child = new TextBlock { Text = "القسم " + (i + 1), FontSize = 16, FontWeight = FontWeight.Bold } };
            sections.Add(section);
            column.Children.Add(section);
            toc.Items.Add(new TocLink { Content = "القسم " + (i + 1), Section = section });
        }
        set?.Invoke(toc);
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("*,20,160") };
        body.Children.Add(column);
        Grid.SetColumn(toc, 2);
        body.Children.Add(toc);
        var page = new StackPanel { Children = { new Border { Height = 100 }, body, new Border { Height = 300 } } };
        var scroller = new ScrollViewer { Content = page };
        var root = new Panel { Height = 500, Children = { scroller } };
        if (frosted)
        {
            Shell.SetFrostsTopbar(scroller, true);
            root.Children.Add(new Border { Classes = { "topbar" }, Height = barHeight, VerticalAlignment = VerticalAlignment.Top });
        }
        return new Page(root, scroller, toc, sections, body);
    }

    private static void ClickLink(Window window, TocLink link)
    {
        var p = Center(link, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Settle(800);
    }

    private static double TopIn(Control c, ScrollViewer s) => c.TranslatePoint(default, s)!.Value.Y;

    [AvaloniaFact]
    public void A_click_lands_the_section_scroll_margin_below_the_top()
    {
        var x = Make(t => t.ScrollMargin = 40);
        var window = Show(x.Root, FlowDirection.RightToLeft, height: 600);
        ClickLink(window, x.Toc.Links.ElementAt(2));
        Assert.Equal(40, TopIn(x.Sections[2], x.Scroller), 1);
        Assert.Same(x.Toc.Links.ElementAt(2), x.Toc.ActiveLink);
    }

    [AvaloniaFact]
    public void Under_a_frosted_top_bar_the_margin_clears_the_bar()
    {
        var x = Make(frosted: true, barHeight: 60);
        var window = Show(x.Root, FlowDirection.RightToLeft, height: 600);
        ClickLink(window, x.Toc.Links.ElementAt(1));
        Assert.Equal(76, TopIn(x.Sections[1], x.Scroller), 1);
    }

    [AvaloniaFact]
    public void With_no_bar_and_no_margin_a_click_lands_the_section_at_the_top()
    {
        var x = Make();
        var window = Show(x.Root, FlowDirection.RightToLeft, height: 600);
        ClickLink(window, x.Toc.Links.ElementAt(1));
        Assert.Equal(0, TopIn(x.Sections[1], x.Scroller), 1);
    }

    [AvaloniaFact]
    public void The_reading_line_is_measured_below_the_margin()
    {
        var x = Make(t => { t.ScrollMargin = 120; t.Offset = 40; });
        Show(x.Root, FlowDirection.RightToLeft, height: 600);
        x.Scroller.Offset = new Vector(0, 330);
        Settle(250);
        Assert.Same(x.Toc.Links.ElementAt(0), x.Toc.ActiveLink);
        x.Scroller.Offset = new Vector(0, 350);
        Settle(250);
        Assert.Same(x.Toc.Links.ElementAt(1), x.Toc.ActiveLink);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_sticky_list_stays_in_view_within_its_parent(bool frosted)
    {
        var x = Make(t => t.IsSticky = true, frosted);
        Show(x.Root, FlowDirection.RightToLeft, height: 600);
        var stay = frosted ? 76 : 16;
        Assert.Equal(100, TopIn(x.Toc, x.Scroller), 1);

        x.Scroller.Offset = new Vector(0, 700);
        Settle(100);
        Assert.Equal(stay, TopIn(x.Toc, x.Scroller), 1);

        x.Scroller.Offset = new Vector(0, x.Scroller.Extent.Height - x.Scroller.Viewport.Height);
        Settle(100);
        var bottom = x.Toc.TranslatePoint(new Point(0, x.Toc.Bounds.Height), x.Column)!.Value.Y;
        Assert.Equal(x.Column.Bounds.Height, bottom, 1);

        x.Scroller.Offset = default;
        Settle(100);
        Assert.Equal(100, TopIn(x.Toc, x.Scroller), 1);
    }

    [AvaloniaFact]
    public void A_list_that_is_not_sticky_scrolls_with_the_page()
    {
        var x = Make();
        Show(x.Root, FlowDirection.RightToLeft, height: 600);
        x.Scroller.Offset = new Vector(0, 700);
        Settle(100);
        Assert.Equal(100 - 700, TopIn(x.Toc, x.Scroller), 1);
    }
}
