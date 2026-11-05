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

public class TextMenuTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static void RightClick(Window w, Control c)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Right);
        w.MouseUp(p, MouseButton.Right);
        Settle();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_selectable_text_menu_is_a_pop_menu_in_the_page_language(string dir)
    {
        var text = new SelectableTextBlock { Text = "نص يمكن نسخه", HorizontalAlignment = HorizontalAlignment.Left };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { text } }, Dir(dir));
        RightClick(w, text);
        var menu = Assert.IsAssignableFrom<PopMenu>(text.ContextFlyout);
        Assert.True(menu.IsOpen);
        var presenter = (Control)menu.Popup.Child!;
        Assert.Contains("pop", presenter.Classes);
        Assert.Equal(new Thickness(40, 16, 40, 64), Part<Border>(presenter, "PART_Room").Padding);
        var item = Assert.Single(menu.Items.OfType<MenuItem>());
        Assert.Equal(dir == "rtl" ? "نسخ" : "Copy", item.Header);
        Capture(w, $"text-menu-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_text_box_menu_is_a_pop_menu_in_the_page_language(string dir)
    {
        var box = new TextBox { Text = "نص", Width = 200 };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { box } }, Dir(dir));
        RightClick(w, box);
        var menu = Assert.IsAssignableFrom<PopMenu>(box.ContextFlyout);
        Assert.True(menu.IsOpen);
        var presenter = (Control)menu.Popup.Child!;
        Assert.Contains("pop", presenter.Classes);
        var heads = menu.Items.OfType<MenuItem>().Select(i => i.Header).ToList();
        Assert.Equal(dir == "rtl" ? new object[] { "قص", "نسخ", "لصق" } : new object[] { "Cut", "Copy", "Paste" }, heads);
    }
}
