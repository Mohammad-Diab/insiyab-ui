using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The gallery's top bar: a notifications bell between the day/night toggle and the appearance button.
public class GalleryTopbarTests
{
    private static Rect BoundsIn(Control c, Visual root)
    {
        var a = c.TranslatePoint(default, root)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), root)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_end_zone_has_a_notifications_bell_between_the_toggle_and_the_appearance_button(bool rtl)
    {
        PageScrollTests.LoadIcons();
        var shell = new GalleryShell();
        var window = Show(shell, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 1280, height: 800);
        Settle(500);
        var zone = shell.GetVisualDescendants().OfType<StackPanel>().Single(z => z.Classes.Contains("zone") && z.Classes.Contains("end"));
        Assert.Collection(zone.Children,
            c => Assert.IsType<DayNight>(c),
            c => Assert.IsType<Notify>(c),
            c => Assert.Equal("AppearanceButton", Assert.IsType<Button>(c).Name));

        var notify = (Notify)zone.Children[1];
        Assert.False(notify.IsEnd);
        Assert.InRange(notify.Items.Count, 4, 5);
        Assert.All(notify.Items, i => Assert.IsType<NotifyItem>(i));
        Assert.True(notify.UnreadCount > 0);

        var bell = BoundsIn(Part<Button>(notify, "PART_Bell"), window);
        var appearance = BoundsIn((Control)zone.Children[2], window);
        Assert.Equal(appearance.Height, bell.Height, 1);
        Assert.Equal(appearance.Center.Y, bell.Center.Y, 1);

        Click(Part<Button>(notify, "PART_Bell"), window);
        Assert.True(notify.IsOpen);
        var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(notify, "PART_Popup").Child);
        var body = BoundsIn(presenter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "LayoutRoot"), window);
        Assert.True(body.X >= 0 && body.Right <= window.Width, $"panel {body.X}-{body.Right} outside the {window.Width}px window");
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Short_of_room_the_bell_panel_slides_by_the_shortfall_and_stays_under_the_bell(bool rtl)
    {
        PageScrollTests.LoadIcons();
        var shell = new GalleryShell();
        var window = Show(shell, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 760, height: 800);
        Settle(500);
        var notify = shell.GetVisualDescendants().OfType<Notify>().Single();
        var bellButton = Part<Button>(notify, "PART_Bell");
        var bell = BoundsIn(bellButton, window);

        Click(bellButton, window);
        Assert.True(notify.IsOpen);
        var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(notify, "PART_Popup").Child);
        var body = BoundsIn(presenter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "LayoutRoot"), window);
        Assert.True(body.X >= 0 && body.Right <= window.Width, $"panel {body.X}-{body.Right} outside the {window.Width}px window");
        Assert.True(body.X <= bell.X && body.Right >= bell.Right, $"bell {bell.X}-{bell.Right} not over the panel {body.X}-{body.Right}");
        var inset = shell.GetVisualDescendants().OfType<Border>().Single(x => x.Classes.Contains("topbar")).Padding;
        if (notify.FlowDirection == FlowDirection.RightToLeft) Assert.Equal(inset.Left, body.X, 1.5);
        else Assert.Equal(window.Width - inset.Right, body.Right, 1.5);
    }
}
