using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The quote by its look class: the passage, its source line and links, in both directions, runtime class changes, overrides, and which elements take a theme.
public class QuoteLookTests
{
    private const string Passage = "يُحفظ الملف كل خمس دقائق، ويمكنك استعادة أي نسخة سابقة منه من قائمة السجل في أي وقت.";

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Border Quote(Control child) => new() { Classes = { "quote" }, Child = child };

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Quote_keeps_its_look(string theme, string dir)
    {
        var plain = Quote(new TextBlock { Text = "نص مقتبس قصير." });
        var link = new HyperlinkButton { Content = "دليل الاستخدام" };
        var withLink = Quote(new StackPanel
        {
            Children =
            {
                new TextBlock { Text = Passage },
                new ContentControl { Classes = { "quote-source" }, Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { link, new TextBlock { Text = " · ص 12" } } } },
            },
        });
        var withText = Quote(new StackPanel
        {
            Children =
            {
                new TextBlock { Text = Passage },
                new ContentControl { Classes = { "quote-source" }, Content = "دليل الاستخدام · ص 12" },
            },
        });
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 14, Width = 420, HorizontalAlignment = HorizontalAlignment.Left, Children = { plain, withLink, withText } };
        var w = Show(page, Dir(dir), Variant(theme), 480, 560);
        LookCheck.Keep(w, page, $"quote-{theme}-{dir}");
        Assert.Equal(new Thickness(3, 1, 1, 1), plain.BorderThickness);
        Assert.Equal(new Thickness(18, 14, 16, 14), plain.Padding);
        Assert.Equal(Res(plain, "InsRMd"), plain.CornerRadius);
        AssertBrush(plain, "InsGlassInner", () => plain.Background);
        var text = (TextBlock)plain.Child!;
        Assert.Equal((double)Res(text, "InsFsMd"), text.FontSize);
        Assert.Equal(26.6, text.LineHeight, 2);
        Assert.Equal(TextWrapping.Wrap, text.TextWrapping);
        AssertBrush(text, "InsText1", () => text.Foreground);
        var passage = (TextBlock)((StackPanel)withLink.Child!).Children[0];
        Assert.Equal(TextWrapping.Wrap, passage.TextWrapping);
        Assert.True(passage.Bounds.Height > 40);

        w.MouseMove(Center(link, w));
        Settle(300);
        LookCheck.Keep(w, page, $"quote-hover-{theme}-{dir}");
        AssertBrush(link, "InsQuoteLink", () => link.Foreground);
    }

    [AvaloniaFact]
    public void A_button_or_link_label_in_a_quote_keeps_its_own_leading_and_no_wrapping()
    {
        var button = new Button { Content = "اقرأ المزيد" };
        var link = new HyperlinkButton { Content = "دليل الاستخدام" };
        var quote = Quote(new StackPanel { Width = 300, Children = { new TextBlock { Text = Passage }, button, link } });
        Show(new StackPanel { Children = { quote } }, FlowDirection.RightToLeft);
        foreach (var host in new Control[] { button, link })
        {
            var label = host.GetVisualDescendants().OfType<AccessText>().Single();
            Assert.True(double.IsNaN(label.LineHeight));
            Assert.Equal(TextWrapping.NoWrap, label.TextWrapping);
        }
    }

    [AvaloniaFact]
    public void The_quote_class_added_at_runtime_takes_its_look()
    {
        var text = new TextBlock { Text = Passage };
        var quote = new Border { Child = new StackPanel { Children = { text } } };
        Show(new StackPanel { Width = 300, Children = { quote } }, FlowDirection.RightToLeft);
        Assert.Equal(new Thickness(0), quote.Padding);
        Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
        Assert.True(double.IsNaN(text.LineHeight));

        quote.Classes.Add("quote");
        Settle(300);
        Assert.Equal(new Thickness(18, 14, 16, 14), quote.Padding);
        Assert.Equal(new Thickness(3, 1, 1, 1), quote.BorderThickness);
        Assert.Equal(TextWrapping.Wrap, text.TextWrapping);
        Assert.Equal(26.6, text.LineHeight, 2);
        Assert.Equal((double)Res(text, "InsFsMd"), text.FontSize);

        quote.Classes.Remove("quote");
        Settle(300);
        Assert.Equal(new Thickness(0), quote.Padding);
        Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
        Assert.True(double.IsNaN(text.LineHeight));
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_quote_look()
    {
        var quote = Quote(new TextBlock { Text = "نص" });
        var local = new Border { Classes = { "quote" }, Padding = new Thickness(2), Child = new TextBlock { Text = "نص" } };
        var inner = new TextBlock { Classes = { "own" }, Text = "نص بحجمه" };
        var third = Quote(inner);
        var page = new StackPanel { Children = { quote, local, third } };
        page.Styles.Add(App<Border>("quote", Border.CornerRadiusProperty, new CornerRadius(3)));
        page.Styles.Add(App<TextBlock>("own", TextBlock.FontSizeProperty, 19.0));
        var typeOnly = new Style(x => x.OfType<Border>()) { Setters = { new Setter(Border.BorderThicknessProperty, new Thickness(7)) } };
        Application.Current!.Styles.Add(typeOnly);
        try
        {
            Show(page, FlowDirection.RightToLeft, height: 400);
            Assert.Equal(new CornerRadius(3), quote.CornerRadius);
            Assert.Equal(new Thickness(2), local.Padding);
            Assert.Equal(new Thickness(3, 1, 1, 1), quote.BorderThickness);
            Assert.Equal(19, inner.FontSize);
        }
        finally
        {
            Application.Current.Styles.Remove(typeOnly);
        }
    }

    [AvaloniaFact]
    public void Only_the_quote_takes_a_theme()
    {
        var plain = new Border { Child = new TextBlock { Text = "أ" } };
        var text = new TextBlock { Text = "ب" };
        var quote = Quote(text);
        var source = new ContentControl { Classes = { "quote-source" }, Content = "مصدر" };
        Show(new StackPanel { Children = { plain, quote, source } }, FlowDirection.RightToLeft);
        Assert.Null(plain.Theme);
        Assert.Null(((TextBlock)plain.Child!).Theme);
        Assert.NotNull(quote.Theme);
        Assert.Null(text.Theme);
    }

    [AvaloniaFact]
    public void No_global_rule_names_the_quote_class()
    {
        // The one rule left restores an AccessText label inside a quote to its own leading and wrapping.
        var subject = new Regex(@"(?:^|[\s>])Border(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\.quote(?![\w-])");
        var found = LookCheck.GlobalSelectors().SelectMany(s => s.Split(", ")).Select(a => a.Trim())
            .Where(a => subject.IsMatch(a) && !a.Contains("ins-still") && !a.Contains("IsInactive") && !a.EndsWith(" AccessText", StringComparison.Ordinal)).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
