using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Every menu-like item hovers as the web's .ins-pop-item does: one brand tint that only fades in, the hover ink and a 3px nudge.
public class MenuHoverTests
{
    private static void Hover(Window w, Control item, Func<IBrush?> back, Func<IBrush?> ink, Func<Control>? moved = null)
    {
        var tint = Assert.IsAssignableFrom<ISolidColorBrush>(Res(item, "InsPopItemHover")).Color;
        w.MouseMove(Center(item, w));
        for (var i = 0; i < 40; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var seen = Assert.IsAssignableFrom<ISolidColorBrush>(back()).Color;
            Assert.True(Math.Abs(seen.R - tint.R) <= 3 && Math.Abs(seen.G - tint.G) <= 3 && Math.Abs(seen.B - tint.B) <= 3, $"frame {i}: the hover passed through {seen}, not only {tint}");
            Thread.Sleep(4);
        }
        Settle(300);
        AssertBrush(item, "InsPopItemHover", back);
        Assert.Equal(0.10, ((ISolidColorBrush)back()!).Opacity, 2);
        AssertBrush(item, "InsPopItemHoverFg", ink);
        Assert.Equal(3, (moved?.Invoke() ?? item).RenderTransform!.Value.M31, 1);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Pop_card_item(string theme)
    {
        var item = new Button { Classes = { "pop-item" }, Content = "إعادة الضبط" };
        var card = new PopCard { Content = new StackPanel { Children = { new Button { Classes = { "pop-item" }, Content = "تخفيف المؤثرات" }, item } } };
        var trigger = new Button { Content = "المظهر", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { trigger } }, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 800, 600);
        Click(trigger, w);
        Hover(w, item, () => item.Background, () => item.Foreground);
    }

    [AvaloniaFact]
    public void Menu_and_menu_bar_items()
    {
        var edit = new MenuItem { Header = "تعديل" };
        var menu = new PopMenu { Items = { new MenuItem { Header = "نسخ" }, edit } };
        var trigger = new Button { Content = "إجراءات", Flyout = menu, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var file = new MenuItem { Header = "ملف", Items = { new MenuItem { Header = "جديد" } } };
        var bar = new Menu { Items = { file, new MenuItem { Header = "عرض" } }, HorizontalAlignment = HorizontalAlignment.Right };
        var w = Show(new StackPanel { Children = { bar, trigger } }, FlowDirection.RightToLeft, null, 800, 600);
        Click(trigger, w);
        Hover(w, edit, () => edit.Background, () => edit.Foreground);
        menu.Hide();
        Settle(300);
        Hover(w, file, () => file.Background, () => file.Foreground);
    }

    [AvaloniaFact]
    public void Select_and_autocomplete_options()
    {
        var combo = new ComboBox { Items = { "الأول", "الثاني", "الثالث" }, SelectedIndex = 0 };
        var auto = new AutoCompleteBox { ItemsSource = new[] { "أسامة", "أسماء" }, MinimumPopulateDelay = TimeSpan.Zero };
        var w = Show(new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Children = { combo, auto } }, FlowDirection.RightToLeft, null, 800, 600);
        combo.IsDropDownOpen = true;
        Settle();
        var option = (ComboBoxItem)combo.ContainerFromIndex(2)!;
        Hover(w, option, () => option.Background, () => option.Foreground);
        combo.IsDropDownOpen = false;
        Settle(300);

        Part<TextBox>(auto, "PART_TextBox").Focus();
        w.KeyTextInput("أس");
        Settle();
        var row = w.GetVisualDescendants().OfType<ListBoxItem>().Last(i => i.IsEffectivelyVisible);
        Hover(w, row, () => row.Background, () => row.Foreground);
    }

    [AvaloniaFact]
    public void Tags_options_and_palette_rows()
    {
        var tags = new TagsInput { Width = 400, Options = { new TagsOption("a", "الأول"), new TagsOption("b", "الثاني"), new TagsOption("c", "الثالث") } };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { tags } }, FlowDirection.RightToLeft, null, 800, 640);
        tags.InputBox!.Focus();
        Settle();
        var option = w.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("tags-option")).ElementAt(2);
        var text = (TextBlock)option.Child!;
        Hover(w, option, () => option.Background, () => text.Foreground);

        var palette = new CommandPalette { Items = { new PaletteItem { Label = "الأزرار" }, new PaletteItem { Label = "الجداول" } } };
        w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        var rows = palette.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("palette-item")).ToList();
        var second = rows[1];
        Hover(w, second, () => second.Background, () => second.GetValue(TextElement.ForegroundProperty));
    }
}
