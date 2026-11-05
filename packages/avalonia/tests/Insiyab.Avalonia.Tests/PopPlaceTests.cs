using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class PopPlaceTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Border Card(PopupFlyoutBase flyout) => Part<Border>((Control)flyout.Popup.Child!, "LayoutRoot");

    private static PopMenu SixItems() => new()
    {
        Items =
        {
            new MenuItem { Header = "تعديل" },
            new MenuItem { Header = "نسخ" },
            new MenuItem { Header = "نقل" },
            new MenuItem { Header = "مشاركة" },
            new MenuItem { Header = "تصدير" },
            new MenuItem { Header = "حذف" },
        },
    };

    private static (Button trigger, PopMenu menu, MenuItem inner) Submenus(bool end)
    {
        var inner = new MenuItem { Header = "تصدير", Items = { new MenuItem { Header = "صورة" }, new MenuItem { Header = "ملف نصي" } } };
        var menu = new PopMenu { IsEnd = end, Items = { new MenuItem { Header = "نسخ" }, inner } };
        var trigger = new Button { Content = "إجراءات", Flyout = menu };
        return (trigger, menu, inner);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_pop_with_no_room_below_stands_six_pixels_above_its_trigger(string dir)
    {
        var menu = SixItems();
        var trigger = new Button { Content = "إجراءات", Flyout = menu, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(40) };
        var w = Show(new Panel { Children = { trigger } }, Dir(dir));
        Click(trigger, w);
        Assert.True(menu.IsOpen);
        var presenter = (Control)menu.Popup.Child!;
        var t = Box(trigger, w);
        var c = Box(Card(menu), w);
        Assert.InRange(t.Top - c.Bottom, 5, 7);
        if (dir == "rtl") Assert.InRange(c.Right - t.Right, -1, 1);
        else Assert.InRange(c.Left - t.Left, -1, 1);
        Assert.False(PopMotion.GetFromAbove(presenter));
        Capture(w, $"pop-above-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_pop_at_the_window_end_slides_and_never_flips_sideways(string dir)
    {
        var card = new PopCard { Content = new TextBlock { Text = "نص قصير" } };
        var trigger = new Button { Content = "⋯", Flyout = card, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(12) };
        var w = Show(new Panel { Children = { trigger } }, Dir(dir));
        Click(trigger, w);
        Assert.True(card.IsOpen);
        var c = Box(Card(card), w);
        if (dir == "ltr") Assert.InRange(c.Right, 631, 633);
        else Assert.InRange(c.Left, 7, 9);
        Assert.InRange(c.Top - Box(trigger, w).Bottom, 5, 7);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_pop_too_tall_for_either_side_opens_beside_its_trigger(string dir)
    {
        var card = new PopCard { Content = new Border { Height = 300 } };
        var trigger = new Button { Content = "فتح", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(40) };
        var w = Show(new Panel { Children = { trigger } }, Dir(dir), null, 800, 420);
        Click(trigger, w);
        Assert.True(card.IsOpen);
        var t = Box(trigger, w);
        var c = Box(Card(card), w);
        if (dir == "ltr") Assert.InRange(c.Left - t.Right, 5, 7);
        else Assert.InRange(t.Left - c.Right, 5, 7);
        Assert.True(c.Top >= 7, $"top {c.Top}");
        Assert.True(c.Bottom <= 413, $"bottom {c.Bottom}");
        Capture(w, $"pop-beside-{dir}");
    }

    [AvaloniaFact]
    public void A_tall_card_grows_to_the_window_and_MaxCardHeight_caps_it()
    {
        var card = new PopCard { Content = new Border { Height = 600 } };
        var trigger = new Button { Content = "فتح", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var w = Show(new Panel { Children = { trigger } }, FlowDirection.RightToLeft, null, 800, 900);
        Click(trigger, w);
        Assert.True(card.IsOpen);
        Assert.Equal(634, Card(card).Bounds.Height, 1);
        card.Hide();
        Settle();
        card.MaxCardHeight = 300;
        Click(trigger, w);
        Assert.True(card.IsOpen);
        Assert.Equal(300, Card(card).Bounds.Height, 0.5);
    }

    [AvaloniaFact]
    public void A_long_menu_grows_to_the_window_before_it_scrolls()
    {
        var menu = new PopMenu();
        for (var i = 1; i <= 20; i++) menu.Items.Add(new MenuItem { Header = $"عنصر {i}" });
        var trigger = new Button { Content = "قائمة", Flyout = menu, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var w = Show(new Panel { Children = { trigger } }, FlowDirection.RightToLeft, null, 640, 900);
        Click(trigger, w);
        Assert.True(menu.IsOpen);
        Assert.True(Card(menu).Bounds.Height > 384, $"height {Card(menu).Bounds.Height}");
    }

    private static double CardWidth(double triggerWidth, bool match)
    {
        var card = new PopCard { Content = new TextBlock { Text = "بطاقة" }, MatchTargetWidth = match };
        var trigger = new Button { Content = "فتح", Width = triggerWidth, Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var w = Show(new Panel { Children = { trigger } }, FlowDirection.RightToLeft, null, 800, 600);
        Click(trigger, w);
        Assert.True(card.IsOpen);
        var width = Card(card).Bounds.Width;
        w.Close();
        return width;
    }

    [AvaloniaFact]
    public void A_card_can_take_its_trigger_width()
    {
        Assert.Equal(480, CardWidth(480, true), 0.5);
        Assert.Equal(320, CardWidth(200, true), 0.5);
        Assert.Equal(320, CardWidth(480, false), 0.5);
    }

    [AvaloniaFact]
    public void Pops_never_take_their_trigger_hand_cursor()
    {
        var arrow = new Cursor(StandardCursorType.Arrow).ToString();
        var card = new PopCard { Content = new TextBlock { Text = "بطاقة" } };
        var (trigger, menu, inner) = Submenus(false);
        var cardTrigger = new Button { Content = "بطاقة", Flyout = card, Cursor = new Cursor(StandardCursorType.Hand) };
        trigger.Cursor = new Cursor(StandardCursorType.Hand);
        var w = Show(new StackPanel { Margin = new Thickness(40), Spacing = 12, Children = { trigger, cardTrigger } }, FlowDirection.RightToLeft);
        Click(cardTrigger, w);
        Assert.Equal(arrow, ((Control)card.Popup.Child!).Cursor?.ToString());
        card.Hide();
        Settle();
        Click(trigger, w);
        Assert.Equal(arrow, ((Control)menu.Popup.Child!).Cursor?.ToString());
        inner.Open();
        Settle(300);
        var room = (Border)Part<Popup>(inner, "PART_Popup").Child!;
        Assert.Equal(arrow, room.Cursor?.ToString());
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Pop_items_in_a_card_sit_concentric_with_its_corners(string dir)
    {
        var item = new Button { Classes = { "pop-item" }, Content = "خيار" };
        var card = new PopCard { Content = new StackPanel { Children = { item } } };
        var trigger = new Button { Content = "فتح", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var w = Show(new Panel { Children = { trigger } }, Dir(dir));
        Click(trigger, w);
        Assert.True(card.IsOpen);
        Assert.Equal(new Thickness(22, 8), item.Padding);
        Assert.Equal(new Thickness(-10, 0), item.Margin);
        var c = Box(Card(card), w);
        var i = Box(item, w);
        Assert.Equal(7, i.Left - c.Left, 0.5);
        Assert.Equal(7, c.Right - i.Right, 0.5);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_submenu_with_no_room_at_its_end_opens_on_the_start_side_touching_its_item(string dir)
    {
        var (trigger, menu, inner) = Submenus(true);
        trigger.HorizontalAlignment = HorizontalAlignment.Right;
        trigger.Margin = new Thickness(12);
        inner.RenderTransform = TransformOperations.Parse("none");
        var w = Show(new Panel { Children = { trigger } }, Dir(dir));
        Click(trigger, w);
        Assert.True(menu.IsOpen);
        inner.Open();
        Settle(300);
        var sub = Box((Border)((Border)Part<Popup>(inner, "PART_Popup").Child!).Child!, w);
        var item = Box(inner, w);
        if (dir == "ltr") Assert.InRange(sub.Right - (item.Left - 4), -1.5, 1.5);
        else Assert.InRange(sub.Left - (item.Right + 4), -1.5, 1.5);
        Assert.InRange(sub.Top - item.Top, -1.5, 1.5);
        Capture(w, $"pop-submenu-{dir}");
    }

    [AvaloniaFact]
    public void A_closed_menu_never_opens_its_submenu()
    {
        var (trigger, menu, inner) = Submenus(true);
        trigger.HorizontalAlignment = HorizontalAlignment.Left;
        trigger.VerticalAlignment = VerticalAlignment.Top;
        trigger.Margin = new Thickness(40);
        var w = Show(new Panel { Children = { trigger } }, FlowDirection.RightToLeft);
        Click(trigger, w);
        menu.Hide();
        Settle(300);
        // what Avalonia's delayed submenu open does after the menu has closed
        inner.IsSubMenuOpen = true;
        Settle(100);
        Click(trigger, w);
        Assert.True(menu.IsOpen);
        Assert.False(inner.IsSubMenuOpen);
    }

    [AvaloniaFact]
    public void Danger_items_take_the_danger_tone_and_bad_no_longer_does()
    {
        var danger = new MenuItem { Header = "حذف", Classes = { "danger" } };
        var bad = new MenuItem { Header = "قديم", Classes = { "bad" } };
        var menu = new PopMenu { Items = { danger, bad } };
        var row = new Button { Classes = { "pop-item", "danger" }, Content = "حذف" };
        var card = new PopCard { Content = new StackPanel { Children = { row } } };
        var trigger = new Button { Content = "قائمة", Flyout = menu, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var cardTrigger = new Button { Content = "بطاقة", Flyout = card, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var w = Show(new Panel { Children = { trigger, cardTrigger } }, FlowDirection.RightToLeft);
        Click(trigger, w);
        AssertBrush(danger, "InsToneBad", () => danger.Foreground);
        AssertBrush(bad, "InsText1", () => bad.Foreground);
        w.MouseMove(Center(danger, w));
        Settle();
        AssertBrush(danger, "InsToneBadBg", () => danger.Background);
        menu.Hide();
        Settle();
        Click(cardTrigger, w);
        AssertBrush(row, "InsToneBad", () => row.Foreground);
    }
}
