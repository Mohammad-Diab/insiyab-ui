using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ProseDataTests
{
    private const string Arabic = "فقرة عربية طويلة تكفي لتلتف على أكثر من سطر واحد في هذا العرض الضيق من الصفحة.";
    private const string English = "An English paragraph long enough to wrap onto more than one line at this width.";

    [AvaloniaFact]
    public void Selectable_paragraphs_take_the_reading_look()
    {
        var para = new SelectableTextBlock { Text = Arabic };
        var prose = new Prose { Children = { para } };
        Show(new StackPanel { Width = 360, Children = { prose } }, FlowDirection.RightToLeft);

        Assert.Equal(28.8, para.LineHeight, 0.01);
        Assert.Equal(TextWrapping.Wrap, para.TextWrapping);
        Assert.Equal((double)Res(prose, "InsFsLg"), para.FontSize, 0.01);
        Assert.Same(Res(prose, "InsFontArabic"), para.FontFamily);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void Each_paragraph_takes_the_face_and_line_of_its_own_direction(FlowDirection page)
    {
        var ar = new SelectableTextBlock { Text = Arabic, FlowDirection = FlowDirection.RightToLeft };
        var en = new TextBlock { Text = English, FlowDirection = FlowDirection.LeftToRight };
        var prose = new Prose { Children = { ar, en } };
        Show(new StackPanel { Width = 360, Children = { prose } }, page);

        Assert.Same(Res(prose, "InsFontArabic"), ar.FontFamily);
        Assert.Equal(28.8, ar.LineHeight, 0.01);
        Assert.Same(Res(prose, "InsFontBodyEn"), en.FontFamily);
        Assert.Equal(26.4, en.LineHeight, 0.01);
    }

    [AvaloniaFact]
    public void Headings_and_code_keep_their_own_faces()
    {
        var head = new TextBlock { Text = "Getting started", Classes = { "h2" }, FlowDirection = FlowDirection.LeftToRight };
        var code = new TextBlock { Text = "dotnet build" };
        var pre = new Border { Classes = { "pre" }, Child = code };
        var prose = new Prose { Children = { head, pre } };
        Show(new StackPanel { Width = 480, Children = { prose } }, FlowDirection.RightToLeft);

        Assert.Same(Res(prose, "InsFontHeadingEn"), head.FontFamily);
        Assert.Equal(28.6, head.LineHeight, 0.01);
        Assert.Same(Res(prose, "InsFontMono"), code.FontFamily);
        Assert.Equal(20.8, code.LineHeight, 0.01);
    }

    public sealed record Para(string Text, FlowDirection Direction);

    [AvaloniaFact]
    public void Data_bound_paragraphs_read_as_prose()
    {
        var list = new ItemsControl
        {
            Classes = { "prose" },
            ItemsSource = new[] { new Para(Arabic, FlowDirection.RightToLeft), new Para(English, FlowDirection.LeftToRight) },
            ItemTemplate = new FuncDataTemplate<Para>((p, _) => p is null ? null : new SelectableTextBlock { Text = p.Text, FlowDirection = p.Direction }),
        };
        Show(new StackPanel { Width = 1400, Children = { list } }, FlowDirection.RightToLeft, width: 1500);
        var paras = list.GetVisualDescendants().OfType<SelectableTextBlock>().ToList();

        Assert.Equal(2, paras.Count);
        Assert.IsType<Prose>(list.ItemsPanelRoot);
        Assert.Equal((double)Res(list, "InsFsLg"), paras[0].FontSize, 0.01);
        AssertBrush(list, "InsText1", () => paras[0].Foreground);
        Assert.Equal(28.8, paras[0].LineHeight, 0.01);
        Assert.Same(Res(list, "InsFontBodyEn"), paras[1].FontFamily);
        Assert.Equal(26.4, paras[1].LineHeight, 0.01);
        Assert.Equal(TextWrapping.Wrap, paras[1].TextWrapping);
        Assert.True(paras[1].Bounds.Width < 1000, $"the line is capped: {paras[1].Bounds.Width}");
        var gap = paras[1].TranslatePoint(default, list)!.Value.Y - paras[0].TranslatePoint(new Point(0, paras[0].Bounds.Height), list)!.Value.Y;
        Assert.Equal(paras[1].FontSize, gap, 0.5);
    }

    private static TextBlock Block(string text)
    {
        var block = new TextBlock { Text = text };
        if (text == "عنوان") block.Classes.Add("h3");
        return block;
    }

    [AvaloniaFact]
    public void A_heading_paragraph_from_data_takes_the_heading_gap()
    {
        var list = new ItemsControl
        {
            Classes = { "prose" },
            ItemsSource = new[] { "فقرة أولى", "عنوان", "فقرة ثانية" },
            ItemTemplate = new FuncDataTemplate<string>((t, _) => t is null ? null : Block(t)),
        };
        Show(new StackPanel { Width = 600, Children = { list } }, FlowDirection.RightToLeft);
        var blocks = list.GetVisualDescendants().OfType<TextBlock>().ToList();
        var top = blocks[1].TranslatePoint(default, list)!.Value.Y;
        var above = blocks[0].TranslatePoint(new Point(0, blocks[0].Bounds.Height), list)!.Value.Y;
        Assert.Equal(1.75 * blocks[1].FontSize, top - above, 0.5);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void A_marker_written_on_an_item_replaces_its_number(FlowDirection dir)
    {
        var a = new TextBlock { Text = "البند الأول" };
        var b = new TextBlock { Text = "البند الثاني" };
        var c = new TextBlock { Text = "البند الثالث" };
        ProseList.SetMarker(a, "٣)");
        ProseList.SetMarker(b, "(iv)");
        var list = new ProseList { IsOrdered = true, Children = { a, b, c } };
        Show(new Prose { Children = { list } }, dir);

        var markers = list.Markers.Cast<TextBlock>().ToList();
        Assert.Equal(["٣)", "(iv)", "3."], markers.Select(m => m.Text ?? "").ToArray());
        var inset = a.Bounds.X;
        Assert.True(inset >= markers.Max(m => m.DesiredSize.Width) + list.FontSize * .25 - 0.5, $"inset {inset}");
        Assert.Equal(inset, c.Bounds.X, 0.5);
    }

    [AvaloniaFact]
    public void A_short_marker_keeps_the_usual_inset()
    {
        var a = new TextBlock { Text = "أولًا" };
        ProseList.SetMarker(a, "١-");
        var list = new ProseList { IsOrdered = true, Children = { a } };
        Show(new Prose { Children = { list } }, FlowDirection.RightToLeft);
        Assert.Equal(1.5 * list.FontSize, a.Bounds.X, 0.5);
    }

    public sealed record Point2(string Marker, string Text);

    [AvaloniaFact]
    public void Markers_come_from_the_items_of_a_bound_list()
    {
        var list = new ItemsControl
        {
            ItemsSource = new[] { new Point2("أ.", "الأول"), new Point2("ب.", "الثاني") },
            ItemsPanel = new FuncTemplate<Panel?>(() => new ProseList { IsOrdered = true }),
            ItemTemplate = new FuncDataTemplate<Point2>((p, _) =>
            {
                if (p is null) return null;
                var t = new SelectableTextBlock { Text = p.Text };
                ProseList.SetMarker(t, p.Marker);
                return t;
            }),
        };
        Show(new Prose { Children = { list } }, FlowDirection.RightToLeft);
        var panel = Assert.IsType<ProseList>(list.ItemsPanelRoot);
        Assert.Equal(["أ.", "ب."], panel.Markers.Cast<TextBlock>().Select(m => m.Text ?? "").ToArray());

        var item = list.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        ProseList.SetMarker(item, "ج.");
        Settle(100);
        Assert.Equal("ج.", panel.Markers.Cast<TextBlock>().First().Text);
    }
}
