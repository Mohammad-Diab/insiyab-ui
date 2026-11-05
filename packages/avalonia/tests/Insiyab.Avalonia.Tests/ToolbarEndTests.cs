using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ToolbarEndTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4 12h16");

    private static ToolbarButton Command(string label, bool atEnd = false)
    {
        var b = new ToolbarButton { Icon = Glyph, Label = label, ShowLabel = true };
        Toolbar.SetAtEnd(b, atEnd);
        return b;
    }

    private static (Toolbar Bar, ToolbarButton[] Start, ToolbarButton End) Bar(double width)
    {
        var start = new[] { Command("جديد"), Command("فتح"), Command("نسخ"), Command("مشاركة") };
        var end = Command("الإعدادات", true);
        var bar = new Toolbar { Classes = { "plain" }, Width = width };
        foreach (var c in start) bar.Items.Add(c);
        bar.Items.Add(end);
        return (bar, start, end);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void The_end_group_sits_at_the_far_end(FlowDirection dir)
    {
        var (bar, start, end) = Bar(700);
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { bar } }, dir, width: 800);
        var panel = Part<Panel>(bar, "PART_Panel");

        Assert.Empty(bar.OverflowItems);
        Assert.Equal(panel.Bounds.Width, end.Bounds.Right, 0.5);
        Assert.Equal(0, start[0].Bounds.X, 0.5);
        Assert.True(start[^1].Bounds.Right < end.Bounds.X - 100);
        var s = Center(start[0], window).X;
        var e = Center(end, window).X;
        Assert.True(dir == FlowDirection.LeftToRight ? e > s : e < s, $"end {e}, start {s}");
    }

    [AvaloniaFact]
    public void The_overflow_counts_the_end_group_and_moves_the_start_first()
    {
        var (bar, start, end) = Bar(330);
        Show(new StackPanel { Margin = new Thickness(20), Children = { bar } }, FlowDirection.RightToLeft, width: 800);
        var panel = Part<Panel>(bar, "PART_Panel");
        var more = bar.MoreButton!;

        Assert.NotEmpty(bar.OverflowItems);
        Assert.DoesNotContain(end, bar.OverflowItems);
        Assert.Contains(start[^1], bar.OverflowItems);
        Assert.True(end.IsVisible);
        Assert.True(more.IsVisible);
        Assert.Equal(panel.Bounds.Width, end.Bounds.Right, 0.5);
        Assert.True(more.Bounds.Right <= end.Bounds.X + 0.5, $"more ends {more.Bounds.Right}, end group starts {end.Bounds.X}");
        var shown = start.Where(c => c.IsVisible).ToList();
        Assert.NotEmpty(shown);
        Assert.True(shown[^1].Bounds.Right <= more.Bounds.X + 0.5);
        Assert.True(bar.DesiredSize.Width <= 330.5);
    }

    [AvaloniaFact]
    public void The_arrows_go_from_the_start_to_the_more_button_to_the_end()
    {
        var (bar, start, end) = Bar(330);
        Show(new StackPanel { Margin = new Thickness(20), Children = { bar } }, FlowDirection.LeftToRight, width: 800);
        var stops = bar.Stops();
        Assert.Same(end, stops[^1]);
        Assert.Same(bar.MoreButton, stops[^2]);
        Assert.Same(start[0], stops[0]);
    }

    [AvaloniaFact]
    public void Separators_never_open_or_close_the_end_group()
    {
        var bar = new Toolbar { Width = 600 };
        var a = Command("جديد");
        var gap = new Separator();
        var b = Command("تنزيل", true);
        var tail = new Separator();
        Toolbar.SetAtEnd(tail, true);
        foreach (var c in new Control[] { a, gap, b, tail }) bar.Items.Add(c);
        Show(new StackPanel { Children = { bar } }, width: 700);
        Assert.False(gap.IsVisible);
        Assert.False(tail.IsVisible);
        Assert.True(b.IsVisible);
    }
}
