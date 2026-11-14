using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class BackStackTests
{
    // Raises the system Back on the TopLevel and says whether something took it.
    private static bool Back(TopLevel top)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        top.RaiseEvent(e);
        Settle();
        return e.Handled;
    }

    [AvaloniaFact]
    public void Back_closes_the_newest_overlay_first_one_layer_at_a_time()
    {
        var inner = new Button { Content = "خيارات" };
        var card = new PopCard { Content = new TextBlock { Text = "بطاقة" } };
        var choice = new ComboBox { ItemsSource = new[] { "أحمر", "أخضر", "أزرق" }, SelectedIndex = 0 };
        var dialog = new Dialog { Title = "إعدادات", Content = new StackPanel { Spacing = 8, Children = { inner, choice } } };
        var opener = new Button { Content = "فتح" };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { opener } }, width: 700, height: 600);

        dialog.Open(opener);
        Settle();
        card.ShowAt(inner);
        Settle();
        choice.IsDropDownOpen = true;
        Settle();

        Assert.True(Back(window));
        Assert.False(choice.IsDropDownOpen);
        Assert.True(card.IsOpen);
        Assert.True(dialog.IsOpen);

        Assert.True(Back(window));
        Assert.False(card.IsOpen);
        Assert.True(dialog.IsOpen);

        Assert.True(Back(window));
        Assert.False(dialog.IsOpen);

        Assert.False(Back(window));
    }

    [AvaloniaFact]
    public void An_app_handler_is_not_reached_while_an_overlay_takes_back_and_gets_it_otherwise()
    {
        var opener = new Button { Content = "قائمة", HorizontalAlignment = HorizontalAlignment.Left };
        var menu = new PopMenu { Items = { new MenuItem { Header = "نسخ" }, new MenuItem { Header = "لصق" } } };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { opener } });
        var seen = new List<bool>();
        window.BackRequested += (_, e) => seen.Add(e.Handled);

        menu.ShowAt(opener);
        Settle();
        Assert.True(menu.IsOpen);
        Back(window);
        Assert.False(menu.IsOpen);

        Back(window);
        Assert.Equal(new[] { false }, seen);
    }

    [AvaloniaFact]
    public void Back_closes_the_palette_and_the_shell_drawer()
    {
        var nav = new ListBox { Classes = { "shell-nav" }, Items = { new ListBoxItem { Content = "الرئيسية" }, new ListBoxItem { Content = "الملفات" } } };
        var shell = new SplitView { IsPaneOpen = true, Pane = nav, Content = new Panel() };
        var window = Show(shell, width: 600, height: 500);
        Assert.True(Shell.IsDrawer(shell));
        Shell.Sidebar(shell, SidebarState.Open);
        Settle();
        var palette = new CommandPalette();
        palette.Items.Add(new PaletteItem { Label = "الإعدادات" });
        palette.Open(window);
        Settle();

        Assert.True(Back(window));
        Assert.False(palette.IsOpen);
        Assert.True(shell.IsPaneOpen);

        Assert.True(Back(window));
        Assert.False(shell.IsPaneOpen);

        Assert.False(Back(window));
    }

    [AvaloniaFact]
    public void Pages_go_back_after_the_overlays_and_leave_back_to_the_app_at_their_root()
    {
        var opener = new Button { Content = "فتح" };
        var window = Show(new StackPanel { Children = { opener } });
        var pages = 2;
        var stack = new object();
        BackStack.Push(window, stack, BackTier.Page, () =>
        {
            if (pages == 0) return false;
            pages--;
            return true;
        });
        try
        {
            var dialog = new Dialog { Title = "حوار", Content = new TextBlock { Text = "نص" } };
            dialog.Open(opener);
            Settle();

            Assert.True(Back(window));
            Assert.False(dialog.IsOpen);
            Assert.Equal(2, pages);

            Assert.True(Back(window));
            Assert.True(Back(window));
            Assert.Equal(0, pages);
            Assert.False(Back(window));
        }
        finally { BackStack.Remove(stack); }
    }

    [AvaloniaFact]
    public void Back_closes_a_dialog_in_a_single_view()
    {
        var opener = new Button { Content = "فتح" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { opener } });
        try
        {
            var dialog = new Dialog { Title = "حوار", Content = new TextBlock { Text = "نص" } };
            dialog.Open(opener);
            OverlaysTests.Pump(root);
            var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            root.RaiseEvent(e);
            OverlaysTests.Pump(root);
            Assert.True(e.Handled);
            Assert.False(dialog.IsOpen);
        }
        finally { root.StopRendering(); }
    }
}
