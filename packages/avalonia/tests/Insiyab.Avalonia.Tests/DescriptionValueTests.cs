using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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

public class DescriptionValueTests
{
    private static void Press(Window w, Key key, RawInputModifiers mods = RawInputModifiers.None, PhysicalKey physical = PhysicalKey.None, string? symbol = null)
    {
        w.KeyPress(key, mods, physical, symbol);
        w.KeyRelease(key, mods, physical, symbol);
        Settle(200);
    }

    private static Dialog MakeDialog(Control content)
    {
        var dialog = new Dialog { Title = "عنوان الحوار", Content = content };
        var no = new Button { Content = "إلغاء", Classes = { "bare" } };
        var yes = new Button { Content = "موافق", Classes = { "primary" } };
        Dialog.SetResult(no, "cancel");
        Dialog.SetResult(yes, "ok");
        dialog.Foot = new DialogFoot { Children = { no, yes } };
        return dialog;
    }

    [AvaloniaFact]
    public void A_control_given_as_a_value_shows_as_itself()
    {
        var button = new Button { Content = "فتح" };
        var item = new DescriptionItem { Term = "الرابط", Content = button };
        Show(new DescriptionList { Children = { item } }, FlowDirection.RightToLeft);
        Assert.Same(button, Part<ContentPresenter>(item, "PART_Value").Child);
        Assert.DoesNotContain(item.GetVisualDescendants().OfType<TextBlock>(), t => t.Text?.Contains("Avalonia") == true);
    }

    [AvaloniaFact]
    public void A_text_value_is_selectable_with_its_ring_inside()
    {
        var first = new Button { Content = "قبل" };
        var item = new DescriptionItem { Term = "الحالة", Content = "قيد المراجعة" };
        var w = Show(new StackPanel { Margin = new Thickness(40), Spacing = 12, Children = { first, new DescriptionList { Children = { item } } } }, FlowDirection.RightToLeft);
        first.Focus();
        Press(w, Key.Tab, physical: PhysicalKey.Tab);
        var value = Assert.IsType<DescriptionText>(Part<ContentPresenter>(item, "PART_Value").Child);
        Assert.True(value.IsFocused);
        Assert.Contains(":focus-visible", value.Classes);
        Assert.Null(value.FocusAdorner);
        Assert.DoesNotContain(w.GetVisualDescendants().OfType<FocusRing>(), r => r.IsEffectivelyVisible);
        Assert.False(item.ClipToBounds);
        Assert.Equal(new Thickness(-6, -2), value.Margin);
        Capture(w, "description-ring-inside");
    }

    [AvaloniaFact]
    public void A_dialog_skips_read_only_text_when_it_opens()
    {
        var facts = new DescriptionList
        {
            Children =
            {
                new DescriptionItem { Term = "الحالة", Content = "قيد المراجعة" },
                new DescriptionItem { Term = "المرجع", Content = "طلب ٤٢" },
            },
        };
        var dialog = MakeDialog(facts);
        var opener = new Button { Content = "فتح الحوار" };
        var w = Show(new StackPanel { Margin = new Thickness(24), Children = { opener } }, FlowDirection.RightToLeft, null, 900, 700);
        _ = dialog.ShowAsync(opener);
        Settle();
        var focused = w.FocusManager!.GetFocusedElement();
        Assert.Same(((DialogFoot)dialog.Foot!).Children[1], focused);
        Assert.IsNotType<SelectableTextBlock>(focused);
        Assert.IsNotType<DescriptionText>(focused);
    }

    [AvaloniaFact]
    public void A_dialog_opens_on_its_first_field_past_the_facts()
    {
        var field = new TextBox();
        var body = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new DescriptionList { Children = { new DescriptionItem { Term = "الحالة", Content = "قيد المراجعة" } } },
                field,
            },
        };
        var dialog = MakeDialog(body);
        var opener = new Button { Content = "فتح الحوار" };
        var w = Show(new StackPanel { Margin = new Thickness(24), Children = { opener } }, FlowDirection.RightToLeft, null, 900, 700);
        _ = dialog.ShowAsync(opener);
        Settle();
        Assert.Same(field, w.FocusManager!.GetFocusedElement());
    }
}
