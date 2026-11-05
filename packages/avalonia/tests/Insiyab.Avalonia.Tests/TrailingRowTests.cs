using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TrailingRowTests
{
    private static TextBlock Title(string text) => new() { Text = text, TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 1, VerticalAlignment = VerticalAlignment.Center };

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void A_long_title_gives_way_and_the_pill_follows_its_end(FlowDirection dir)
    {
        var title = Title(string.Concat(Enumerable.Repeat("عنوان طويل جدًا لا يتسع ", 8)));
        var pill = new Border { Width = 60, Height = 20 };
        var row = new TrailingRow { Width = 300, Children = { title, pill } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, dir);

        Assert.InRange(title.Bounds.Width, 220, 232.5);
        Assert.Equal(60, pill.Bounds.Width, 0.5);
        Assert.Equal(title.Bounds.Width + 8, pill.Bounds.X, 0.5);
        var titleX = Center(title, window).X;
        var pillX = Center(pill, window).X;
        Assert.True(dir == FlowDirection.LeftToRight ? pillX > titleX : pillX < titleX, $"pill {pillX}, title {titleX}");
    }

    [AvaloniaFact]
    public void A_short_title_keeps_the_pill_right_behind_it()
    {
        var title = Title("قصير");
        var pill = new Border { Width = 60, Height = 20 };
        var row = new TrailingRow { Spacing = 6, Children = { title, pill } };
        Show(new StackPanel { Width = 400, Children = { row } });

        Assert.True(title.Bounds.Width < 100, $"title {title.Bounds.Width}");
        Assert.Equal(title.Bounds.Width + 6, pill.Bounds.X, 0.5);
        Assert.Equal(title.Bounds.Width + 66, row.DesiredSize.Width, 0.5);
    }

    [AvaloniaFact]
    public void A_hidden_first_child_hands_the_giving_way_to_the_next()
    {
        var hidden = Title("مخفي");
        hidden.IsVisible = false;
        var title = Title(string.Concat(Enumerable.Repeat("نص طويل ", 40)));
        var pill = new Border { Width = 50, Height = 20 };
        var row = new TrailingRow { Width = 200, Children = { hidden, title, pill } };
        Show(new StackPanel { Children = { row } });

        Assert.InRange(title.Bounds.Width, 130, 142.5);
        Assert.Equal(title.Bounds.Width + 8, pill.Bounds.X, 0.5);
    }

    [AvaloniaFact]
    public void A_tail_wider_than_the_row_leaves_the_first_child_no_room()
    {
        var title = Title("عنوان");
        var pill = new Border { Width = 260, Height = 20 };
        var row = new TrailingRow { Width = 200, Children = { title, pill } };
        Show(new StackPanel { Children = { row } });
        Assert.Equal(0, title.Bounds.Width, 0.5);
    }

    [AvaloniaFact]
    public void With_no_width_limit_it_takes_every_child_whole()
    {
        var title = Title("عنوان قصير");
        var pill = new Border { Width = 40, Height = 30 };
        var row = new TrailingRow { Children = { title, pill } };
        Show(new StackPanel { Orientation = Orientation.Horizontal, Children = { row } });
        Assert.Equal(title.DesiredSize.Width + 48, row.DesiredSize.Width, 0.5);
        Assert.Equal(30, row.DesiredSize.Height, 0.5);
    }
}
