using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class FindTests
{
    private const string First = "يصل التَّقرير الشهري إلى بريدك في أول يوم من كل شهر.";
    private const string Second = "وإن أردت تقريرًا لفترة أخرى فاختر بدايتها ونهايتها.";
    private const string Third = "ولا يُحذف أي تقرير قبل مرور سنة على إنشائه.";

    private static void Press(Window window, Key key, RawInputModifiers mods = RawInputModifiers.None)
    {
        window.KeyPress(key, mods, PhysicalKey.None, null);
        window.KeyRelease(key, mods, PhysicalKey.None, null);
        Settle(200);
    }

    private static (StackPanel Page, FindBar Bar, StackPanel Area) Page(bool open = true, string? text = "تقرير")
    {
        var bar = new FindBar { IsOpen = open, Text = text };
        var area = new StackPanel { Spacing = 8 };
        Find.SetIsSearchable(area, true);
        foreach (var t in new[] { First, Second, Third }) area.Children.Add(new TextBlock { Text = t, TextWrapping = TextWrapping.Wrap });
        var page = new StackPanel { Spacing = 12, Margin = new Thickness(20), Children = { bar, area, new TextBlock { Text = "تقرير خارج المنطقة" } } };
        return (page, bar, area);
    }

    [Fact]
    public void Matches_ignore_diacritics_and_map_back_to_the_text()
    {
        var text = "يصل التَّقرير، ثم تقريرًا آخر";
        var found = Find.Matches(text, "تقرير");
        Assert.Equal(2, found.Count);
        var a = text.Substring(found[0].Start, found[0].Length);
        Assert.Equal("تقرير", ArabicSearch.Normalize(a));
        Assert.Equal('ت', a[0]);
        Assert.Equal('ر', a[^1]);
        var b = text.Substring(found[1].Start, found[1].Length);
        Assert.Equal("تقرير", ArabicSearch.Normalize(b));
        Assert.Equal('ً', b[^1]);
        Assert.Single(Find.Matches("أحمد وإيمان", "احمد"));
        Assert.Single(Find.Matches("مستشفى", "مستشفي"));
        Assert.Single(Find.Matches("الطلب ١٢٣", "123"));
        Assert.Equal((4, 6), Find.Matches("The Report", "report").Single());
        Assert.Empty(Find.Matches("نص", "  "));
        Assert.Empty(Find.Matches(null, "نص"));
    }

    [AvaloniaFact]
    public void FindBar_counts_the_matches_in_searchable_areas_only_and_steps_round()
    {
        var (page, bar, _) = Page();
        var window = Show(page, FlowDirection.RightToLeft, width: 700, height: 500);
        Settle(200);
        Assert.True(bar.IsVisible);
        Assert.Equal(3, bar.MatchCount);
        Assert.Equal(1, bar.CurrentMatch);
        var count = Part<TextBlock>(bar, "PART_Count");
        Assert.Equal("1 من 3", count.Text);
        bar.Step(1);
        Assert.Equal(2, bar.CurrentMatch);
        bar.Step(-1);
        bar.Step(-1);
        Assert.Equal(3, bar.CurrentMatch);
        Assert.Equal("3 من 3", count.Text);
        bar.Text = "غير موجود";
        Settle(100);
        Assert.Equal(0, bar.MatchCount);
        Assert.Equal("لا نتائج", count.Text);
        Assert.Contains(":none", bar.Classes);
        Assert.False(Part<Button>(bar, "PART_Next").IsEnabled);
        window.Close();
    }

    [AvaloniaFact]
    public void FindBar_draws_its_highlights_over_the_text_without_touching_it()
    {
        var (page, bar, area) = Page();
        var window = Show(page, FlowDirection.RightToLeft, width: 700, height: 500);
        Settle(200);
        var layer = AdornerLayer.GetAdornerLayer(bar)!;
        Assert.Single(layer.Children, c => c.GetType().Name == "FindHighlights");
        var block = (TextBlock)area.Children[0];
        Assert.Equal(First, block.Text);
        Assert.Null(block.Inlines?.Count > 0 ? block.Inlines : null);
        Assert.All(bar.Found, m => Assert.Contains(m.Rects(), r => r.Width > 0 && r.Height > 0));
        var box = bar.Found[0].Rects().First();
        Assert.InRange(box.Right, 0, block.Bounds.Width + 1);
        bar.IsOpen = false;
        Settle(100);
        Assert.DoesNotContain(layer.Children, c => c.GetType().Name == "FindHighlights");
        Assert.False(bar.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void FindBar_opens_on_ctrl_f_steps_on_enter_and_closes_on_escape()
    {
        var (page, bar, _) = Page(open: false);
        var other = new Button { Content = "زر" };
        page.Children.Add(other);
        var window = Show(page, FlowDirection.RightToLeft, width: 700, height: 500);
        other.Focus();
        Settle(100);
        Press(window, Key.F, RawInputModifiers.Control);
        Assert.True(bar.IsOpen);
        var field = Part<TextBox>(bar, "PART_Field");
        Assert.True(field.IsFocused);
        Assert.Equal(3, bar.MatchCount);
        Press(window, Key.Enter);
        Assert.Equal(2, bar.CurrentMatch);
        Press(window, Key.Enter, RawInputModifiers.Shift);
        Assert.Equal(1, bar.CurrentMatch);
        Press(window, Key.F3);
        Assert.Equal(2, bar.CurrentMatch);
        Press(window, Key.Escape);
        Assert.False(bar.IsOpen);
        Assert.True(other.IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void FindBar_target_limits_the_search_and_the_current_match_scrolls_into_view()
    {
        var area = new StackPanel { Spacing = 8 };
        for (var i = 0; i < 40; i++) area.Children.Add(new TextBlock { Text = i == 35 ? Third : "سطر بلا شيء" });
        var viewer = new ScrollViewer { Height = 200, Content = area };
        var bar = new FindBar { IsOpen = true, Target = area, Text = "تقرير" };
        var page = new StackPanel { Children = { bar, viewer, new TextBlock { Text = "تقرير خارج الهدف" } } };
        Show(page, FlowDirection.RightToLeft, width: 600, height: 400);
        Settle(300);
        Assert.Equal(1, bar.MatchCount);
        Assert.True(viewer.Offset.Y > 0);
        var block = (TextBlock)area.Children[35];
        var top = block.TranslatePoint(default, viewer)!.Value.Y;
        Assert.InRange(top, 0, viewer.Viewport.Height);
    }
}
