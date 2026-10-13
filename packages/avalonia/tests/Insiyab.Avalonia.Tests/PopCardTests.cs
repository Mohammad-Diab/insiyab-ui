using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
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
}
