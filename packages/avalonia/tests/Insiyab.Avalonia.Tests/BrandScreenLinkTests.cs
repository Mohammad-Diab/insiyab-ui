using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class BrandScreenLinkTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Theme(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static StackPanel Brand(out Border mark, out TextBlock name, out TextBlock sub)
    {
        mark = new Border { Classes = { "shell-mark" }, Child = new TextBlock { Text = "م" } };
        name = new TextBlock { Classes = { "brand-name" }, Text = "متجري" };
        sub = new TextBlock { Classes = { "brand-sub" }, Text = "تسجيل الدخول" };
        return new StackPanel { Classes = { "brand" }, Children = { mark, name, sub } };
    }

    // ---------------------------------------------------------------- brand

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Brand_has_the_web_sizes_and_colours(string theme, string dir)
    {
        var brand = Brand(out var mark, out var name, out var sub);
        Show(new Border { Width = 382, Child = brand }, Dir(dir), Theme(theme));

        Assert.Equal(6, brand.Spacing);
        Assert.Equal(new Thickness(0, 0, 0, 24), brand.Margin);
        Assert.Equal(new Size(60, 60), mark.Bounds.Size);
        Assert.Equal(new Thickness(0, 0, 0, 8), mark.Margin);
        Assert.Equal(161, mark.Bounds.X, 1);
        var letter = (TextBlock)mark.Child!;
        Assert.Equal(26, letter.FontSize);
        Assert.Equal(FontWeight.ExtraBold, letter.FontWeight);

        Assert.Equal(24, name.FontSize);
        Assert.Equal(FontWeight.ExtraBold, name.FontWeight);
        Assert.Equal(40.8, name.LineHeight);
        Assert.Equal(191, name.Bounds.Center.X, 1);
        AssertBrush(name, theme == "Dark" ? "InsText1" : "InsPrimaryDarker", () => name.Foreground);

        Assert.Equal(15, sub.FontSize);
        Assert.Equal(25.5, sub.LineHeight);
        Assert.Equal(191, sub.Bounds.Center.X, 1);
        AssertBrush(sub, "InsText2", () => sub.Foreground);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Brand_name_takes_the_display_face_and_tracking_only_in_ltr(string theme, string dir)
    {
        var name = new TextBlock { Classes = { "brand-name" }, Text = "متجري" };
        Show(new StackPanel { Children = { name } }, Dir(dir), Theme(theme));

        if (dir == "ltr")
        {
            Assert.Equal(Res(name, "InsFontHeadingEn"), name.FontFamily);
            Assert.Equal(-0.48, name.LetterSpacing, 2);
        }
        else
        {
            Assert.Equal(Res(name, "InsFontArabic"), name.FontFamily);
            Assert.Equal(0, name.LetterSpacing);
        }
    }

    [AvaloniaFact]
    public void Plain_shell_mark_keeps_its_sidebar_size()
    {
        var mark = new Border { Classes = { "shell-mark" }, Child = new TextBlock { Text = "م" } };
        Show(new StackPanel { Children = { mark } });
        Assert.Equal(34, mark.Bounds.Width);
        Assert.Equal(14.4, ((TextBlock)mark.Child!).FontSize);
    }

    // ---------------------------------------------------------------- screen

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Screen_centres_one_card_at_most_448_wide(string dir)
    {
        var card = new Border { Classes = { "glass" }, Height = 200 };
        Show(new Panel { Classes = { "screen" }, Children = { card } }, Dir(dir), width: 1000, height: 700);

        Assert.Equal(448, card.Bounds.Width);
        Assert.Equal(276, card.Bounds.X, 1);
        Assert.Equal(250, card.Bounds.Y, 1);
    }

    [AvaloniaFact]
    public void Screen_keeps_32_clear_of_a_narrow_window()
    {
        var card = new Border { Classes = { "glass" }, Height = 200 };
        Show(new Panel { Classes = { "screen" }, Children = { card } }, width: 400, height: 700);

        Assert.Equal(new Thickness(32), card.Margin);
        Assert.Equal(336, card.Bounds.Width);
        Assert.Equal(32, card.Bounds.X, 1);
    }

    [AvaloniaFact]
    public void Screen_centres_in_a_scroll_viewer_and_scrolls_when_taller()
    {
        var card = new Border { Classes = { "glass" }, Height = 200 };
        var tall = new Border { Classes = { "glass" }, Height = 800 };
        var short_ = new ScrollViewer { Content = new Panel { Classes = { "screen" }, Children = { card } } };
        var window = Show(short_, width: 900, height: 700);
        Assert.Equal(250, card.Bounds.Y, 1);

        window.Content = new ScrollViewer { Content = new Panel { Classes = { "screen" }, Children = { tall } } };
        Settle();
        Assert.Equal(32, tall.Bounds.Y, 1);
        Assert.Equal(864, ((ScrollViewer)window.Content).Extent.Height, 1);
    }

    // ---------------------------------------------------------------- link

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Link_is_the_web_a_at_rest_and_under_the_pointer(string theme, string dir)
    {
        var link = new HyperlinkButton { Content = "نسيت كلمة المرور" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { link } }, Dir(dir), Theme(theme));

        Assert.Equal(new Thickness(0), link.Padding);
        Assert.Equal(new Thickness(0), link.BorderThickness);
        Assert.Equal(Colors.Transparent, ColorOf(link.Background));
        AssertBrush(link, "InsPrimaryDarker", () => link.Foreground);
        var text = link.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.True(text.TextDecorations is null || text.TextDecorations.Count == 0);
        Assert.True(link.Bounds.Width < 200);

        window.MouseMove(Center(link, window));
        Settle(100);
        Assert.Contains(":pointerover", link.Classes);
        AssertBrush(link, theme == "Dark" ? "InsText1" : "InsToneBrandSoft", () => link.Foreground);
        Assert.True(text.TextDecorations is null || text.TextDecorations.Count == 0);
    }

    [AvaloniaFact]
    public void Link_shows_the_shared_focus_ring_from_the_keyboard()
    {
        var before = new Button { Content = "قبل" };
        var link = new HyperlinkButton { Content = "نسيت كلمة المرور" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { before, link } });

        link.Focus(NavigationMethod.Tab);
        Settle(100);
        var ring = window.GetVisualDescendants().OfType<FocusRing>().Single(r => r.IsEffectivelyVisible);
        Assert.Equal(new Thickness(2), ring.BorderThickness);
        AssertBrush(link, "InsToneBrandSoft", () => ring.BorderBrush);
    }

    [AvaloniaFact]
    public void Quote_source_and_chip_links_keep_their_own_looks()
    {
        var source = new HyperlinkButton { Content = "دليل الاستخدام" };
        var quote = new ContentControl { Classes = { "quote-source" }, Content = source };
        var chip = new HyperlinkButton { Classes = { "chip" }, Content = "وسم" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { quote, chip } });

        AssertBrush(source, "InsQuoteLink", () => source.Foreground);
        Assert.Equal(FontWeight.Bold, source.FontWeight);
        var text = source.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.True(text.TextDecorations is null || text.TextDecorations.Count == 0);
        window.MouseMove(Center(source, window));
        Settle(100);
        Assert.Contains(":pointerover", source.Classes);
        AssertBrush(source, "InsQuoteLink", () => source.Foreground);
        Assert.Equal(TextDecorations.Underline, text.TextDecorations);

        Assert.Same(Res(chip, "InsChipButton"), chip.Theme);
    }
}
