using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class FoundationTests
{
    private static ThemeVariant Theme(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static FlowDirection Dir(string dir) => dir == "ltr" ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

    private static double X(Control c, Window w) => c.TranslatePoint(new Point(c.Bounds.Width / 2, 0), w)!.Value.X;

    private static double Y(Control c, Window w) => c.TranslatePoint(new Point(0, 0), w)!.Value.Y;

    // Layout rounding snaps each block to whole pixels, so a gap can land up to a pixel off the CSS value.
    private static void Near(double expected, double actual) => Assert.True(Math.Abs(expected - actual) <= 1, $"expected {expected}, got {actual}");

    [AvaloniaFact]
    public void Prose_spaces_blocks_as_the_web_does()
    {
        var h2 = new TextBlock { Classes = { "h2" }, Text = "عنوان" };
        var p1 = new TextBlock { Text = "فقرة أولى." };
        var p2 = new TextBlock { Text = "فقرة ثانية." };
        var h3 = new TextBlock { Classes = { "h3" }, Text = "عنوان فرعي" };
        var p3 = new TextBlock { Text = "فقرة ثالثة." };
        var rule = new Separator();
        var p4 = new TextBlock { Text = "فقرة رابعة." };
        var prose = new Prose { Children = { h2, p1, p2, h3, p3, rule, p4 } };
        var w = Show(prose, FlowDirection.RightToLeft, width: 900, height: 700);

        Assert.Equal(16, prose.FontSize);
        Assert.Equal(28.8, p1.LineHeight, 3);
        Assert.Equal(TextWrapping.Wrap, p1.TextWrapping);
        Assert.Equal(28.6, h2.LineHeight, 3);
        Near(8, Y(p1, w) - (Y(h2, w) + h2.Bounds.Height));
        Near(16, Y(p2, w) - (Y(p1, w) + p1.Bounds.Height));
        Near(18 * 1.75, Y(h3, w) - (Y(p2, w) + p2.Bounds.Height));
        Near(8, Y(p3, w) - (Y(h3, w) + h3.Bounds.Height));
        Near(32, Y(rule, w) - (Y(p3, w) + p3.Bounds.Height));
        Near(32, Y(p4, w) - (Y(rule, w) + rule.Bounds.Height));
    }

    [AvaloniaFact]
    public void Prose_caps_the_line_at_seventy_characters()
    {
        var p = new TextBlock { Text = string.Concat(Enumerable.Repeat("نص طويل يملأ السطر ", 40)) };
        var prose = new Prose { Children = { p } };
        Show(prose, FlowDirection.RightToLeft, width: 1400, height: 400);
        Assert.True(prose.MaxLine is > 400 and < 900, $"70ch was {prose.MaxLine}");
        Assert.True(p.Bounds.Width <= prose.MaxLine + 1, $"line {p.Bounds.Width} over cap {prose.MaxLine}");

        prose.LineLength = 0;
        Settle(50);
        Assert.True(double.IsPositiveInfinity(prose.MaxLine));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Prose_leading_follows_the_direction(string dir)
    {
        var p = new TextBlock { Text = "فقرة" };
        var code = new Border { Classes = { "pre" }, Child = new TextBlock { Text = "GET /api/items" } };
        Show(new Prose { Children = { p, code } }, Dir(dir));
        Assert.Equal(dir == "ltr" ? 26.4 : 28.8, p.LineHeight, 3);
        Assert.Equal(20.8, ((TextBlock)code.Child!).LineHeight, 3);
        Assert.Equal(FlowDirection.LeftToRight, code.FlowDirection);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Prose_list_marks_items_at_the_start_edge(string theme, string dir)
    {
        var first = new TextBlock { Text = "البند الأول" };
        var nestedItem = new TextBlock { Text = "بند متداخل" };
        var nested = new ProseList { Children = { nestedItem } };
        var second = new StackPanel { Children = { new TextBlock { Text = "البند الثاني" }, nested } };
        var ol = new ProseList { IsOrdered = true, Children = { first, second } };
        var w = Show(new Prose { Children = { ol } }, Dir(dir), Theme(theme), width: 700);

        Assert.Equal(2, ol.Markers.Count);
        Assert.Equal("1.", Assert.IsType<TextBlock>(ol.Markers[0]).Text);
        Assert.Equal("2.", Assert.IsType<TextBlock>(ol.Markers[1]).Text);
        AssertBrush(ol, "InsToneBrandSoft", () => ((TextBlock)ol.Markers[0]).Foreground);
        var ring = Assert.IsType<Ellipse>(Assert.Single(nested.Markers));
        Assert.Null(ring.Fill);
        Assert.NotNull(ring.Stroke);
        Assert.Equal(1, nested.Depth);
        Near(6, Y(nested, w) - (Y(second.Children[0], w) + second.Children[0].Bounds.Height));
        Near(6, Y(second, w) - (Y(first, w) + first.Bounds.Height));

        var markerX = X(ol.Markers[0], w);
        var textX = first.TranslatePoint(new Point(dir == "ltr" ? 0 : first.Bounds.Width, 0), w)!.Value.X;
        if (dir == "ltr")
            Assert.True(markerX < textX, "LTR: the number is left of the text");
        else
            Assert.True(markerX > textX, "RTL: the number is right of the text");
        Assert.Equal(ol.Bounds.Width - 24, first.Bounds.Width, 1);
    }

    [AvaloniaFact]
    public void An_unordered_list_takes_a_filled_disc()
    {
        var ul = new ProseList { Children = { new TextBlock { Text = "بند" } } };
        Show(ul);
        var disc = Assert.IsType<Ellipse>(Assert.Single(ul.Markers));
        Assert.NotNull(disc.Fill);
        Assert.Equal(0, ul.Depth);

        ul.Children.Add(new TextBlock { Text = "بند آخر" });
        Settle(50);
        Assert.Equal(2, ul.Markers.Count);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Divider_puts_its_word_between_two_lines(string theme, string dir)
    {
        var labelled = new Divider { Content = "أو", Width = 400 };
        var start = new Divider { Content = "أو", Classes = { "start" }, Width = 400 };
        var empty = new Divider { Width = 400 };
        var w = Show(new StackPanel { Children = { labelled, start, empty } }, Dir(dir), Theme(theme));

        Assert.Equal(new Thickness(0, 16, 0, 16), labelled.Margin);
        Assert.Equal(12, labelled.FontSize);
        Assert.Equal(FontWeight.Bold, labelled.FontWeight);
        AssertBrush(labelled, "InsLabel", () => labelled.Foreground);
        var a = Part<Border>(labelled, "PART_Start");
        var b = Part<Border>(labelled, "PART_End");
        Assert.Equal(1, a.Bounds.Height);
        Assert.Equal(a.Bounds.Width, b.Bounds.Width, 1);
        AssertBrush(labelled, "InsHr", () => a.Background);
        Assert.DoesNotContain(start.GetVisualDescendants().OfType<Border>(), x => x.Name == "PART_Start");
        var word = start.GetVisualDescendants().OfType<TextBlock>().First();
        var line = Part<Border>(start, "PART_End");
        if (dir == "ltr")
            Assert.True(X(word, w) < X(line, w), "LTR: the word leads at the left");
        else
            Assert.True(X(word, w) > X(line, w), "RTL: the word leads at the right");

        Assert.Contains(":empty", empty.Classes);
        Assert.DoesNotContain(":empty", labelled.Classes);
        Assert.Single(empty.GetVisualDescendants().OfType<Border>(), x => x.Height == 1);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Separator_is_the_web_rule_and_v_stands_in_a_row(string theme)
    {
        var rule = new Separator();
        var v = new Separator { Classes = { "v" } };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "ملف" }, v, new TextBlock { Text = "تعديل" } } };
        Show(new StackPanel { Width = 400, Children = { rule, row } }, theme: Theme(theme));

        Assert.Equal(1, rule.Bounds.Height);
        Assert.Equal(400, rule.Bounds.Width);
        Assert.Equal(new Thickness(0, 16, 0, 16), rule.Margin);
        AssertBrush(rule, "InsHr", () => rule.Background);
        Assert.Equal(1, v.Bounds.Width);
        AssertBrush(v, "InsHr", () => v.Background);
        Assert.Equal(row.Bounds.Height, v.Bounds.Height, 1);
        Assert.Equal(new Thickness(4, 0, 4, 0), v.Margin);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Inline_emphasis_is_colour_and_weight(string dir)
    {
        var strong = new Run("نص عريض") { Classes = { "strong" } };
        var em = new Run("توكيد") { Classes = { "em" } };
        var mark = new Run("تظليل") { Classes = { "mark" } };
        var link = new Run("رابط") { Classes = { "a" } };
        var code = new Run("⁦code⁩") { Classes = { "code" } };
        var block = new TextBlock { Inlines = new InlineCollection { new Run("فقرة فيها "), strong, new Run(" و"), em, new Run(" و"), mark, new Run(" و"), code, new Run(" و"), link } };
        Show(new Prose { Children = { block } }, Dir(dir));

        Assert.Equal(FontWeight.Bold, strong.FontWeight);
        Assert.Equal(FontStyle.Normal, em.FontStyle);
        AssertBrush(block, "InsToneBrand", () => em.Foreground);
        AssertBrush(block, "InsToneBrandBg", () => mark.Background);
        AssertBrush(block, "InsCtlBg", () => code.Background);
        Assert.Equal(14, code.FontSize);
        AssertBrush(block, "InsPrimaryDarker", () => link.Foreground);
        var underline = Assert.Single(link.TextDecorations!);
        Assert.Equal(TextDecorationLocation.Underline, underline.Location);
        AssertBrush(block, "InsToneBrandBd", () => underline.Stroke);
        // Avalonia falls back to the regular Arabic face for a bold Run inside a Latin stack, so the shaped weight is checked where Tajawal leads.
        if (dir == "ltr")
            return;
        var bold = block.TextLayout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>()
            .Where(r => r.Text.ToString().Contains("عريض")).ToList();
        Assert.NotEmpty(bold);
        Assert.All(bold, r => Assert.Equal(FontWeight.Bold, r.ShapedBuffer.GlyphTypeface.Weight));
    }

    [AvaloniaFact]
    public void Blockquote_bar_sits_on_the_start_edge()
    {
        var quote = new Border { Classes = { "blockquote" }, Child = new TextBlock { Text = "اقتباس قصير." } };
        Show(quote);
        Assert.Equal(new Thickness(3, 0, 0, 0), quote.BorderThickness);
        Assert.Equal(new CornerRadius(0, 10, 10, 0), quote.CornerRadius);
        Assert.Equal(new Thickness(20, 12, 20, 12), quote.Padding);
        AssertBrush(quote, "InsPrimaryMid", () => quote.BorderBrush);
        AssertBrush(quote, "InsToneBrandBg", () => quote.Background);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Scrollbar_is_a_thin_brand_thumb_that_takes_its_room(string theme)
    {
        var content = new Border { Height = 2000, Width = 300 };
        var scroller = new ScrollViewer { Height = 300, Content = content };
        Show(scroller, theme: Theme(theme));
        Assert.False(scroller.AllowAutoHide);
        var bar = Part<ScrollBar>(scroller, "PART_VerticalScrollBar");
        Assert.Equal(5, bar.Bounds.Width);
        var thumb = bar.GetVisualDescendants().OfType<Thumb>().Single();
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(thumb.Background);
        Assert.Equal(theme == "Dark" ? 0.45 : 0.28, brush.Opacity, 3);
        Assert.Equal(new CornerRadius(4), thumb.CornerRadius);
        Assert.DoesNotContain(bar.GetVisualDescendants().OfType<RepeatButton>(), r => r.Name == "PART_LineUpButton");
        var presenter = Part<ScrollContentPresenter>(scroller, "PART_ContentPresenter");
        Assert.True(presenter.Bounds.Width <= scroller.Bounds.Width - 5 + 0.5, "the bar takes its room beside the content");

        bar.Value = bar.Maximum;
        Settle(50);
        Assert.Equal(scroller.Extent.Height - scroller.Viewport.Height, scroller.Offset.Y, 1);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Scrollbar_sits_on_the_end_edge_of_the_direction(string dir)
    {
        var scroller = new ScrollViewer { Height = 200, Width = 300, Content = new Border { Height = 900 } };
        var w = Show(scroller, Dir(dir));
        var bar = Part<ScrollBar>(scroller, "PART_VerticalScrollBar");
        var barX = X(bar, w);
        if (dir == "ltr")
            Assert.True(barX > X(scroller, w));
        else
            Assert.True(barX < X(scroller, w));
    }

    [AvaloniaFact]
    public void Selection_takes_the_brand_tint()
    {
        var text = new SelectableTextBlock { Text = "نص يمكن تحديده" };
        Show(text);
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(text.SelectionBrush);
        Assert.Equal(0.22, brush.Opacity, 3);
        Assert.NotNull(text.ContextFlyout);
    }

    [AvaloniaFact]
    public void Focus_ring_follows_the_control_radius_two_pixels_out()
    {
        var target = new Border { Width = 100, Height = 40, CornerRadius = new CornerRadius(10) };
        var ring = new FocusRing { BorderThickness = new Thickness(2) };
        AdornerLayer.SetAdornedElement(ring, target);
        Show(new StackPanel { Children = { target, ring } });
        Assert.Equal(new Thickness(-4), ring.Margin);
        Assert.Equal(new CornerRadius(14), ring.CornerRadius);

        target.CornerRadius = new CornerRadius(0, 6, 6, 0);
        Assert.Equal(new CornerRadius(0, 10, 10, 0), ring.CornerRadius);
    }

    [AvaloniaFact]
    public void Keyboard_focus_shows_the_brand_ring()
    {
        var button = new Button { Content = "زر" };
        var first = new TextBox();
        var w = Show(new StackPanel { Children = { first, button } });
        button.Theme = (ControlTheme)Res(button, "InsCardButton");
        first.Focus();
        w.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(100);
        Assert.True(button.IsFocused);
        var layer = AdornerLayer.GetAdornerLayer(button);
        Assert.NotNull(layer);
        var ring = layer!.Children.OfType<FocusRing>().FirstOrDefault();
        Assert.NotNull(ring);
        AssertBrush(button, "InsToneBrandSoft", () => ring!.BorderBrush);
        Assert.Equal(new CornerRadius(18), ring.CornerRadius);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Card_button_lifts_and_gives(string theme, string dir)
    {
        Assert.True(Application.Current!.TryGetResource("InsCardButton", null, out var cardTheme));
        var card = new Button { Theme = (ControlTheme)cardTheme!, Content = "بطاقة تُنقر", Padding = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var w = Show(new Panel { Children = { card } }, Dir(dir), Theme(theme));
        var face = Part<Border>(card, "PART_Face");
        Assert.Equal(new CornerRadius(14), face.CornerRadius);
        Assert.Equal(2, face.BoxShadow.Count);
        AssertBrush(card, "InsPrimaryDarker", () => card.Foreground);
        var fill = Assert.IsAssignableFrom<ILinearGradientBrush>(card.Background);
        Assert.Equal(dir == "rtl", fill.StartPoint.Point.X > 0.5);

        card.Transitions = new Transitions();
        face.Transitions = new Transitions();
        w.MouseMove(new Point(1, 1));
        w.MouseMove(Center(card, w));
        Settle(100);
        Assert.True(card.IsPointerOver);
        Assert.Equal(-2, card.RenderTransform!.Value.M32, 3);
        Assert.Equal(((BoxShadows)Res(card, "InsShLg"))[0], face.BoxShadow[0]);
        w.MouseDown(Center(card, w), MouseButton.Left);
        Settle(100);
        Assert.Equal(0.995, card.RenderTransform!.Value.M11, 3);
        w.MouseUp(Center(card, w), MouseButton.Left);
    }

    [AvaloniaFact]
    public void Panel_note_inside_a_flush_body_drops_its_rule()
    {
        var plain = new Border { Classes = { "panel-note" }, Child = new TextBlock { Text = "سطر شرح." } };
        var flushNote = new Border { Classes = { "panel-note" }, Child = new TextBlock { Text = "سطر شرح." } };
        var body = new Border { Classes = { "panel-body", "flush" }, Child = flushNote };
        Show(new Border { Classes = { "panel" }, Child = new StackPanel { Children = { plain, body } } });
        Assert.Equal(new Thickness(18, 10, 18, 10), plain.Padding);
        Assert.Equal(new Thickness(0, 0, 0, 1), plain.BorderThickness);
        Assert.Equal(new Thickness(10, 6, 10, 12), flushNote.Padding);
        Assert.Equal(new Thickness(0), flushNote.BorderThickness);
        Assert.Equal(new Thickness(10), body.Padding);
    }

    [AvaloniaFact]
    public void Panel_icon_centres_its_glyph()
    {
        var icon = new Icon();
        var chip = new Border { Classes = { "panel-ico" }, Child = icon };
        var w = Show(chip);
        Assert.Equal(36, chip.Bounds.Width);
        Assert.Equal(15, icon.Bounds.Width, 1);
        Assert.True(Math.Abs(X(chip, w) - X(icon, w)) <= 1, "the glyph sits in the middle of its chip");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Gallery_pages_render(string theme, string dir)
    {
        var content = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new Prose { Children = { new TextBlock { Classes = { "h2" }, Text = "عنوان" }, new TextBlock { Text = "فقرة قصيرة." }, new ProseList { IsOrdered = true, Children = { new TextBlock { Text = "البند الأول" } } } } },
                new Divider { Content = "أو" },
                new Border { Classes = { "kbd" }, Child = new TextBlock { Text = "K" }, HorizontalAlignment = HorizontalAlignment.Left },
            },
        };
        var w = Show(content, Dir(dir), Theme(theme), width: 600, height: 400);
        Capture(w, $"foundation-{theme}-{dir}");
    }
}
