using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A context menu and a submenu keep room for their large shadow inside the popup's box, and still open where they did: at the pointer, beside the item, under the bar.
public class MenuRoomTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box on the screen, so a popup's card and the window's controls compare.
    private static Rect Screen(Visual v)
    {
        var a = v.PointToScreen(default);
        var b = v.PointToScreen(new Point(v.Bounds.Width, v.Bounds.Height));
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    // A menu item's popup lives in the overlay layer, not under the item: its room and card are found through the popup.
    private static (Border Room, Border Card) Card(MenuItem item)
    {
        var room = (Border)Part<Popup>(item, "PART_Popup").Child!;
        return (room, (Border)room.Child!);
    }

    private static void Near(double expected, double actual) => Assert.InRange(actual - expected, -1.5, 1.5);

    private static void AssertRoom(Border room, Border card)
    {
        Assert.Equal(new Thickness(40, 16, 40, 64), room.Padding);
        Assert.False(room.ClipToBounds && room.Padding == default);
        var r = Screen(room);
        var c = Screen(card);
        Assert.True(c.Left - r.Left >= 39 && r.Right - c.Right >= 39 && c.Top - r.Top >= 15 && r.Bottom - c.Bottom >= 63, $"card {c} in room {r}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_context_menu_opens_at_the_pointer_with_room_for_its_shadow(string dir)
    {
        var context = new ContextMenu { Items = { new MenuItem { Header = "نسخ" }, new MenuItem { Header = "لصق" } } };
        var target = new Border { Width = 300, Height = 200, Background = Brushes.Transparent, ContextMenu = context };
        var w = Show(new StackPanel { Margin = new Thickness(100), Children = { target } }, Dir(dir), width: 800, height: 700);
        var p = Center(target, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Right);
        w.MouseUp(p, MouseButton.Right);
        Settle();
        Assert.True(context.IsOpen);
        var card = Part<Border>(context, "LayoutRoot");
        AssertRoom(Part<Border>(context, "PART_Room"), card);
        var at = w.PointToScreen(p);
        var c = Screen(card);
        Near(at.Y, c.Top);
        if (dir == "rtl") Near(at.X, c.Right);
        else Near(at.X, c.Left);
        context.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_submenu_opens_beside_its_item_with_room_for_its_shadow(string dir)
    {
        var inner = new MenuItem { Header = "تصدير", Items = { new MenuItem { Header = "صورة" }, new MenuItem { Header = "ملف نصي" } } };
        var top = new MenuItem { Header = "ملف", Items = { new MenuItem { Header = "جديد" }, inner } };
        var bar = new Menu { Items = { top } };
        // The open item's 3px nudge is motion, not placement: held still so the boxes compare.
        top.RenderTransform = inner.RenderTransform = TransformOperations.Parse("none");
        var w = Show(new StackPanel { Margin = new Thickness(200, 40), Children = { bar } }, Dir(dir), width: 900, height: 700);
        top.Open();
        Settle(300);
        var (dropRoom, drop) = Card(top);
        AssertRoom(dropRoom, drop);
        var head = Screen(top);
        var d = Screen(drop);
        Near(head.Bottom + 6, d.Top);
        if (dir == "rtl") Near(head.Right, d.Right);
        else Near(head.Left, d.Left);

        inner.Open();
        Settle(300);
        var (subRoom, sub) = Card(inner);
        AssertRoom(subRoom, sub);
        var item = Screen(inner);
        var s = Screen(sub);
        Near(item.Top, s.Top);
        if (dir == "rtl") Near(item.Left - 4, s.Right);
        else Near(item.Right + 4, s.Left);
        top.Close();
    }
}
