using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class PopCardTests
{
    private static (Window, Button, PopCard, Button, Button, ToggleButton) Rig()
    {
        var item = new Button { Classes = { "pop-item" }, Content = "اتبع وضع النظام" };
        var plain = new Button { Content = "لون" };
        var toggle = new ToggleButton { Classes = { "pop-item" }, Content = "خيار" };
        var card = new PopCard
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Classes = { "pop-label" }, Text = "لون الهوية" },
                    plain,
                    new Separator(),
                    toggle,
                    item,
                },
            },
        };
        var trigger = new Button { Content = "المظهر", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { trigger } }, FlowDirection.RightToLeft, null, 800, 600);
        return (window, trigger, card, item, plain, toggle);
    }

    [AvaloniaFact]
    public void A_card_holds_labels_items_and_a_rule_in_the_web_look()
    {
        var (window, trigger, card, item, _, _) = Rig();
        Click(trigger, window);
        var body = Assert.IsType<FlyoutPresenter>(card.Popup.Child);
        var label = body.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("pop-label"));
        Assert.Equal(new Thickness(12, 8, 12, 4), label.Padding);
        Assert.Equal(FontWeight.Bold, label.FontWeight);
        AssertBrush(label, "InsLabel", () => label.Foreground);
        Assert.Equal(new Thickness(12, 8), item.Padding);
        Assert.Equal(14, item.FontSize);
        AssertBrush(item, "InsText1", () => item.Foreground);
        var rule = body.GetVisualDescendants().OfType<Separator>().First();
        Assert.Equal(1, rule.Bounds.Height, 1);
        AssertBrush(rule, "InsLine", () => rule.Background);

        window.MouseMove(Center(item, window));
        Settle();
        AssertBrush(item, "InsPopItemHover", () => item.Background);
        AssertBrush(item, "InsPopItemHoverFg", () => item.Foreground);
        Assert.Equal(3, item.RenderTransform!.Value.M31, 1);
        Capture(window, "pop-card-items-rtl");
    }

    [AvaloniaFact]
    public void Choosing_an_item_closes_the_card_and_returns_focus_to_its_trigger()
    {
        var (window, trigger, card, item, plain, toggle) = Rig();
        Click(trigger, window);
        Click(plain, window);
        Assert.True(card.IsOpen);
        Click(toggle, window);
        Assert.True(card.IsOpen);
        Click(item, window);
        Assert.False(card.IsOpen);
        Assert.Same(trigger, window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void Menus_and_cards_lie_on_the_wash()
    {
        var menu = new PopMenu { Items = { new MenuItem { Header = "نسخ" } } };
        var card = new PopCard { Content = new TextBlock { Text = "نص" } };
        var context = new ContextMenu { Items = { new MenuItem { Header = "لصق" } } };
        var a = new Button { Content = "قائمة", Flyout = menu, HorizontalAlignment = HorizontalAlignment.Left };
        var b = new Button { Content = "بطاقة", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left };
        var target = new Border { Width = 100, Height = 40, Background = Brushes.Transparent, ContextMenu = context };
        var window = Show(new StackPanel { Spacing = 8, Margin = new Thickness(20), Children = { a, b, target } }, width: 800, height: 600);

        Click(a, window);
        var menuBody = (Control)menu.Popup.Child!;
        AssertBrush(menuBody, "InsWash1", () => Part<Border>(menuBody, "PART_Ground").Background);
        menu.Hide();
        Click(b, window);
        var cardBody = (Control)card.Popup.Child!;
        var ground = Part<Border>(cardBody, "PART_Ground");
        AssertBrush(cardBody, "InsWash1", () => ground.Background);
        Assert.Equal(Part<Border>(cardBody, "LayoutRoot").Bounds.Size, ground.Bounds.Size);
        card.Hide();
        context.Open(target);
        Settle();
        AssertBrush(context, "InsWash1", () => Part<Border>(context, "PART_Ground").Background);
        context.Close();
    }

    // A segmented given inline is its own containers; a card that reopens with a styled panel prepares them again, and the choice must survive it.
    [AvaloniaFact]
    public void A_segmented_in_a_card_keeps_its_choice_when_the_card_opens_again()
    {
        var seg = new Segmented { Classes = { "wide" }, SelectedIndex = 0, Items = { new SegmentedItem { Content = "من اليمين" }, new SegmentedItem { Content = "من اليسار" } } };
        var wide = new Style(x => x.OfType<Segmented>().Class("wide"))
        {
            Setters = { new Setter(ItemsControl.ItemsPanelProperty, new FuncTemplate<Panel?>(() => new UniformGrid { Rows = 1 })) },
        };
        var card = new PopCard { Content = new StackPanel { Styles = { wide }, Children = { seg } } };
        var trigger = new Button { Content = "المظهر", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { trigger } }, FlowDirection.RightToLeft, null, 800, 600);
        Click(trigger, window);
        Click((Control)seg.ContainerFromIndex(1)!, window);
        Assert.Equal(1, seg.SelectedIndex);
        card.Hide();
        Settle();
        var changes = 0;
        seg.SelectionChanged += (_, _) => changes++;
        Click(trigger, window);
        Assert.True(card.IsOpen);
        Assert.Equal(1, seg.SelectedIndex);
        Assert.Equal(0, changes);
    }

    // The page can turn while the card is open: it moves to its trigger's other edge, as the web's anchored card does.
    [AvaloniaFact]
    public void An_open_card_follows_its_trigger_when_the_page_turns()
    {
        var card = new PopCard { IsEnd = true, Content = new StackPanel { Width = 240, Children = { new TextBlock { Classes = { "pop-label" }, Text = "اتجاه الصفحة" } } } };
        var trigger = new Button { Content = "المظهر", Flyout = card, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { trigger } }, FlowDirection.RightToLeft, null, 800, 600);
        Click(trigger, window);
        var body = Part<Border>((Control)card.Popup.Child!, "LayoutRoot");
        var a = Rect(trigger, window);
        var b = Rect(body, window);
        Assert.Equal(a.Left, b.Left, 1);

        window.FlowDirection = FlowDirection.LeftToRight;
        Settle();
        Assert.True(card.IsOpen);
        a = Rect(trigger, window);
        b = Rect(body, window);
        Assert.Equal(a.Right, b.Right, 1);
        Assert.True(b.Top >= a.Bottom, "the card still hangs below its trigger");
    }

    // Where a control is drawn in the window, whichever corner its origin is in RTL.
    private static Rect Rect(Control c, Window window)
    {
        var p = c.TranslatePoint(default, window)!.Value;
        var q = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), window)!.Value;
        return new Rect(new Point(Math.Min(p.X, q.X), Math.Min(p.Y, q.Y)), new Point(Math.Max(p.X, q.X), Math.Max(p.Y, q.Y)));
    }
}
