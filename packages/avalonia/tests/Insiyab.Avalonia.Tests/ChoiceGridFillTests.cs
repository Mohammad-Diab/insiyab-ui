using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ChoiceGridFillTests
{
    private static Border Card(double height = 60) => new() { Height = height };

    [AvaloniaFact]
    public void A_lone_card_keeps_a_column_width_when_filling()
    {
        var lone = Card();
        var fill = new ChoiceGrid { Fill = true, MinColumnWidth = 300, Spacing = 12, Width = 1000, Children = { lone } };
        var wide = Card();
        var fit = new ChoiceGrid { MinColumnWidth = 300, Spacing = 12, Width = 1000, Children = { wide } };
        Show(new StackPanel { Children = { fill, fit } }, width: 1100);

        Assert.Equal((1000 - 24) / 3.0, lone.Bounds.Width, 1.0);
        Assert.Equal(1000, wide.Bounds.Width, 0.5);
    }

    [AvaloniaFact]
    public void MaxColumns_caps_the_count()
    {
        var cards = Enumerable.Range(0, 5).Select(_ => Card()).ToArray();
        var grid = new ChoiceGrid { Fill = true, MinColumnWidth = 200, MaxColumns = 2, Spacing = 10, Width = 1000 };
        foreach (var c in cards) grid.Children.Add(c);
        var auto = Enumerable.Range(0, 5).Select(_ => Card()).ToArray();
        var capped = new ChoiceGrid { MinColumnWidth = 100, MaxColumns = 3, Spacing = 10, Width = 1000 };
        foreach (var c in auto) capped.Children.Add(c);
        Show(new StackPanel { Children = { grid, capped } }, width: 1100, height: 600);

        Assert.Equal(495, cards[0].Bounds.Width, 0.5);
        Assert.Equal(cards[0].Bounds.Y + 70, cards[2].Bounds.Y, 0.5);
        Assert.Equal(60 * 3 + 20, grid.Bounds.Height, 0.5);
        Assert.Equal((1000 - 20) / 3.0, auto[0].Bounds.Width, 0.5);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Masonry_puts_each_card_under_the_shortest_column(FlowDirection dir)
    {
        var a = Card(100);
        var b = Card(40);
        var c = Card(40);
        var d = Card(40);
        var grid = new ChoiceGrid { IsMasonry = true, Fill = true, MinColumnWidth = 200, MaxColumns = 2, Spacing = 12, Width = 412, Children = { a, b, c, d } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { grid } }, dir);

        Assert.Equal(new Rect(0, 0, 200, 100), a.Bounds);
        Assert.Equal(new Rect(212, 0, 200, 40), b.Bounds);
        Assert.Equal(new Rect(212, 52, 200, 40), c.Bounds);
        Assert.Equal(new Rect(212, 104, 200, 40), d.Bounds);
        Assert.Equal(144, grid.Bounds.Height, 0.5);
        var ax = Center(a, window).X;
        var bx = Center(b, window).X;
        Assert.True(dir == FlowDirection.LeftToRight ? ax < bx : ax > bx, $"first {ax}, second {bx}");
    }

    [AvaloniaFact]
    public void Masonry_ties_keep_the_first_column()
    {
        var cards = Enumerable.Range(0, 4).Select(_ => Card(50)).ToArray();
        var grid = new ChoiceGrid { IsMasonry = true, MinColumnWidth = 100, Spacing = 10, Width = 210 };
        foreach (var c in cards) grid.Children.Add(c);
        Show(new StackPanel { Children = { grid } });
        Assert.Equal(new Point(0, 0), cards[0].Bounds.Position);
        Assert.Equal(new Point(110, 0), cards[1].Bounds.Position);
        Assert.Equal(new Point(0, 60), cards[2].Bounds.Position);
        Assert.Equal(new Point(110, 60), cards[3].Bounds.Position);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Arranged_narrower_than_measured_it_measures_the_cards_again(bool masonry)
    {
        var text = new TextBlock { Text = string.Concat(Enumerable.Repeat("كلمة ", 30)), TextWrapping = TextWrapping.Wrap };
        var grid = new ChoiceGrid { Fill = true, IsMasonry = masonry, MinColumnWidth = 300, Spacing = 12, Children = { text } };
        Show(new StackPanel { Width = 590, Children = { grid } });
        Assert.True(text.DesiredSize.Width > 450, $"card first measured at {text.DesiredSize.Width}");

        grid.Measure(new Size(590, double.PositiveInfinity));
        grid.Arrange(new Rect(0, 0, 400, grid.DesiredSize.Height));
        Assert.True(text.DesiredSize.Width <= 400.5, $"card measured at {text.DesiredSize.Width}");
        Assert.Equal(400, text.Bounds.Width, 0.5);
    }

    // 924 is three columns of 300 with 12 between; at 150% the measure can be handed a hair under it and the arrange 924 itself.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_hair_under_a_column_edge_it_keeps_the_rows_it_was_measured_for(bool masonry)
    {
        var cards = Enumerable.Range(0, 6).Select(_ => Card()).ToArray();
        var grid = new ChoiceGrid { Fill = true, IsMasonry = masonry, MinColumnWidth = 300, MaxColumns = 4, Spacing = 12 };
        foreach (var c in cards) grid.Children.Add(c);
        Show(new StackPanel { Width = 924, Children = { grid } }, width: 1000, height: 600);

        grid.Measure(new Size(Math.BitDecrement(924.0), double.PositiveInfinity));
        grid.Arrange(new Rect(0, 0, 924, grid.DesiredSize.Height));
        Assert.Equal(grid.DesiredSize.Height, cards.Max(c => c.Bounds.Bottom), 0.5);
    }

    [AvaloniaFact]
    public void Measured_with_no_width_it_keeps_the_width_it_was_given()
    {
        var cards = Enumerable.Range(0, 3).Select(_ => Card()).ToArray();
        var grid = new ChoiceGrid { Fill = true, MinColumnWidth = 150, Spacing = 10 };
        foreach (var c in cards) grid.Children.Add(c);
        Show(new StackPanel { Width = 500, Children = { grid } });
        Assert.Equal(160, cards[0].Bounds.Width, 0.5);

        grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(500, grid.DesiredSize.Width, 0.5);
        Assert.Equal(60, grid.DesiredSize.Height, 0.5);
    }
}
