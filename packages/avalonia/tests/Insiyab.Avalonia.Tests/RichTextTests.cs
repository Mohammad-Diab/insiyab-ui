using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class RichTextTests
{
    private static SelectableTextBlock Para(string text) => new() { Text = text };

    private static (int, int) Sel(SelectableTextBlock b) => (b.SelectionStart, b.SelectionEnd);

    private static Point At(SelectableTextBlock block, int index, Window w)
    {
        var r = block.TextLayout.HitTestTextPosition(index);
        return block.TranslatePoint(new Point(r.X + block.Padding.Left + 1, r.Center.Y + block.Padding.Top), w)!.Value;
    }

    private static void Keys(Window w, Key key, RawInputModifiers mods)
    {
        w.KeyPress(key, mods, PhysicalKey.None, null);
        w.KeyRelease(key, mods, PhysicalKey.None, null);
        Settle(30);
    }

    private static string Nl => Environment.NewLine;

    [AvaloniaFact]
    public void A_drag_from_one_paragraph_into_another_selects_across_them()
    {
        var a = Para("الفقرة الأولى من النص");
        var b = Para("الفقرة الثانية كاملة");
        var c = Para("الفقرة الثالثة والأخيرة");
        var doc = new RichText { Children = { a, b, c } };
        var w = Show(doc, FlowDirection.RightToLeft, width: 600, height: 400);
        var from = At(a, 3, w);
        var to = At(c, 6, w);
        w.MouseMove(from);
        w.MouseDown(from, MouseButton.Left);
        w.MouseMove(new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2), RawInputModifiers.LeftMouseButton);
        w.MouseMove(to, RawInputModifiers.LeftMouseButton);
        w.MouseUp(to, MouseButton.Left);
        Settle(30);
        Assert.Equal((3, a.Text!.Length), Sel(a));
        Assert.Equal((0, b.Text!.Length), Sel(b));
        Assert.Equal(0, c.SelectionStart);
        Assert.InRange(c.SelectionEnd, 5, 7);
        var said = RichText.SelectionOf(doc)!.SelectedText();
        Assert.StartsWith(a.Text[3..] + Nl + b.Text + Nl, said);
    }

    [AvaloniaFact]
    public void Select_all_takes_every_paragraph_and_copies_list_items_with_their_markers()
    {
        var first = Para("مقدمة");
        var list = new ProseList { IsOrdered = true, Children = { Para("البند الأول"), Para("البند الثاني") } };
        var bullets = new ProseList { Children = { Para("نقطة") } };
        var last = Para("خاتمة");
        var doc = new RichText { Children = { first, list, bullets, last } };
        var w = Show(doc, FlowDirection.RightToLeft);
        first.Focus();
        Keys(w, Key.A, RawInputModifiers.Control);
        Assert.Equal((0, last.Text!.Length), Sel(last));
        Assert.Equal("مقدمة" + Nl + "1. البند الأول" + Nl + "2. البند الثاني" + Nl + "• نقطة" + Nl + "خاتمة", RichText.SelectionOf(doc)!.SelectedText());
    }

    [AvaloniaFact]
    public void Shift_and_the_arrows_carry_the_selection_over_paragraphs_the_way_each_reads()
    {
        var a = Para("ab");
        var b = Para("cd");
        var ar = new SelectableTextBlock { Text = "هو", FlowDirection = FlowDirection.RightToLeft };
        var doc = new RichText { Children = { a, b, ar } };
        var w = Show(doc);
        a.Focus();
        a.SelectionStart = 1;
        a.SelectionEnd = 1;
        Keys(w, Key.Right, RawInputModifiers.Shift);
        Assert.Equal((1, 2), Sel(a));
        Keys(w, Key.Right, RawInputModifiers.Shift);
        Keys(w, Key.Right, RawInputModifiers.Shift);
        Assert.Equal((1, 2), Sel(a));
        Assert.Equal((0, 1), Sel(b));
        Keys(w, Key.Down, RawInputModifiers.Shift);
        Assert.Equal((0, 2), Sel(b));
        Assert.Equal(0, ar.SelectionStart);
        Keys(w, Key.Left, RawInputModifiers.Shift);
        Assert.True(ar.SelectionEnd > 0, $"rtl end {ar.SelectionEnd}");
        Keys(w, Key.Home, RawInputModifiers.Shift | RawInputModifiers.Control);
        Assert.Equal((1, 0), Sel(a));
        Assert.Equal((0, 0), Sel(b));
        Keys(w, Key.End, RawInputModifiers.Shift | RawInputModifiers.Control);
        Assert.Equal((0, 2), Sel(ar));
    }

    [AvaloniaFact]
    public void A_drag_past_the_bottom_scrolls_the_page_and_the_selection_follows()
    {
        var doc = new RichText();
        for (var i = 0; i < 30; i++) doc.Children.Add(Para($"فقرة رقم {i} في نص طويل يمتد أسفل الصفحة"));
        var scroller = new ScrollViewer { Content = doc, Height = 200 };
        var w = Show(scroller, FlowDirection.RightToLeft, height: 300);
        var first = (SelectableTextBlock)doc.Children[0];
        var p = At(first, 2, w);
        var below = scroller.TranslatePoint(new Point(100, scroller.Bounds.Height + 30), w)!.Value;
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseMove(below, RawInputModifiers.LeftMouseButton);
        var selection = RichText.SelectionOf(doc)!;
        Assert.True(selection.IsScrolling);
        var reachedBefore = selection.Span?.To;
        for (var i = 0; i < 10; i++) selection.ScrollStep();
        Assert.True(scroller.Offset.Y > 0, $"offset {scroller.Offset.Y}");
        Assert.NotNull(selection.Span);
        Assert.True(doc.Children.IndexOf(selection.Span!.To) > doc.Children.IndexOf(reachedBefore ?? first));
        w.MouseUp(below, MouseButton.Left);
        Assert.False(selection.IsScrolling);
    }

    [AvaloniaFact]
    public void Plain_text_becomes_paragraphs_in_their_own_direction_with_their_addresses_as_links()
    {
        var doc = new RichText { Text = "مرحبًا، الموقع www.example.com اليوم.\n\nSee https://example.org/docs." };
        Show(doc, FlowDirection.RightToLeft);
        Assert.Equal(2, doc.Children.Count);
        var ar = Assert.IsType<SelectableTextBlock>(doc.Children[0]);
        var en = Assert.IsType<SelectableTextBlock>(doc.Children[1]);
        Assert.Equal(FlowDirection.RightToLeft, ar.FlowDirection);
        Assert.Equal(FlowDirection.LeftToRight, en.FlowDirection);
        var link = Assert.Single(ar.Inlines!.OfType<Span>());
        Assert.Equal("https://www.example.com", RichText.GetLink(link));
        Assert.Contains("a", link.Classes);
        var other = Assert.Single(en.Inlines!.OfType<Span>());
        Assert.Equal("https://example.org/docs", RichText.GetLink(other));
        Assert.EndsWith(".", Assert.IsType<Run>(en.Inlines!.Last()).Text);
    }

    [AvaloniaFact]
    public void A_click_on_a_link_raises_the_event_and_a_drag_over_it_does_not()
    {
        var block = new SelectableTextBlock();
        RichText.SetSpans(block, [new RichSpan("اقرأ "), new RichSpan("الدليل") { Link = "https://example.com/guide" }, new RichSpan(" هنا.")]);
        var doc = new RichText { Children = { block } };
        var w = Show(doc, FlowDirection.RightToLeft);
        var clicks = new List<RichLinkEventArgs>();
        doc.LinkClick += (_, e) => clicks.Add(e);
        var r = block.TextLayout.HitTestTextRange(5, 6).First();
        var p = block.TranslatePoint(r.Center, w)!.Value;
        w.MouseMove(p);
        Settle(30);
        Assert.NotNull(block.Cursor);
        Assert.Contains("hot", block.Inlines!.OfType<Span>().Single().Classes);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle(30);
        var click = Assert.Single(clicks);
        Assert.Equal("https://example.com/guide", click.Url);
        Assert.Equal("الدليل", click.Text);
        w.MouseDown(p, MouseButton.Left);
        w.MouseMove(p + new Vector(-30, 0), RawInputModifiers.LeftMouseButton);
        w.MouseUp(p + new Vector(-30, 0), MouseButton.Left);
        Settle(30);
        Assert.Single(clicks);
    }

    [AvaloniaFact]
    public void Spans_set_their_look_and_keep_an_arabic_word_in_one_piece()
    {
        var block = new SelectableTextBlock();
        RichText.SetSpans(block,
        [
            new RichSpan("كت") { Bold = true }, new RichSpan("اب جديد "), new RichSpan("x = 1") { Code = true },
            new RichSpan(" تحذير") { Tone = "bad" }, new RichSpan(" خط") { Underline = true, Strike = true },
        ]);
        var w = Show(new RichText { Children = { block } }, FlowDirection.RightToLeft);
        var runs = block.Inlines!.OfType<Run>().ToList();
        Assert.Equal("كتاب", runs[0].Text);
        Assert.Contains("strong", runs[0].Classes);
        Assert.Equal(" جديد ", runs[1].Text);
        Assert.Contains("code", runs[2].Classes);
        AssertBrush(block, "InsToneBad", () => runs[3].Foreground);
        Assert.Equal(2, runs[4].TextDecorations!.Count);
        Assert.Equal(FontWeight.Bold, runs[0].FontWeight);
    }

    [AvaloniaFact]
    public void A_bound_prose_list_selects_across_its_items()
    {
        var items = new ItemsControl
        {
            Classes = { "prose" },
            ItemsSource = new[] { "السطر الأول", "السطر الثاني", "السطر الثالث" },
            ItemTemplate = new FuncDataTemplate<string>((s, _) => new SelectableTextBlock { Text = s }),
        };
        RichText.SetSelectsAcross(items, true);
        var w = Show(items, FlowDirection.RightToLeft);
        var blocks = items.GetVisualDescendants().OfType<SelectableTextBlock>().ToList();
        Assert.Equal(3, blocks.Count);
        blocks[1].Focus();
        Keys(w, Key.A, RawInputModifiers.Control);
        Assert.All(blocks, b => Assert.Equal((0, b.Text!.Length), Sel(b)));
        Assert.Equal(string.Join(Nl, blocks.Select(b => b.Text)), RichText.SelectionOf(items)!.SelectedText());
    }

    [AvaloniaFact]
    public void A_link_written_in_markup_is_found_and_marked()
    {
        var link = new Span { Inlines = { new Run("الموقع") } };
        RichText.SetLink(link, "https://example.com");
        var block = new SelectableTextBlock { Inlines = { new Run("زر "), link } };
        var clicks = 0;
        var w = Show(new StackPanel { Children = { block } }, FlowDirection.RightToLeft);
        block.AddHandler(RichText.LinkClickEvent, (_, _) => clicks++);
        Assert.Contains("a", link.Classes);
        var r = block.TextLayout.HitTestTextRange(5, 3).First();
        var p = block.TranslatePoint(r.Center, w)!.Value;
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle(30);
        Assert.Equal(1, clicks);
    }
}
