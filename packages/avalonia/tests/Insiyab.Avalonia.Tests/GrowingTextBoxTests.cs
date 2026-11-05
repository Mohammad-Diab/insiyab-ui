using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class GrowingTextBoxTests
{
    [AvaloniaFact]
    public void A_growing_text_box_starts_at_one_field_and_grows_to_four_lines()
    {
        var box = new TextBox { Classes = { "grow" }, Width = 300 };
        var plain = new TextBox { AcceptsReturn = true, Width = 300 };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { box, plain } }, FlowDirection.RightToLeft, height: 700);
        var field = (double)Res(box, "InsFieldH");

        Assert.True(box.AcceptsReturn);
        Assert.Equal(TextWrapping.Wrap, box.TextWrapping);
        Assert.Equal(4, box.MaxLines);
        Assert.Equal(VerticalAlignment.Center, box.VerticalContentAlignment);
        Assert.Equal(field, box.Bounds.Height, 0.5);
        Assert.Equal(88, plain.Bounds.Height, 0.5);

        box.Text = "سطر\nسطر\nسطر";
        Settle(100);
        var three = box.Bounds.Height;
        Assert.True(three > field + 20, $"three lines {three}");

        box.Text = string.Join("\n", Enumerable.Repeat("سطر", 9));
        Settle(100);
        var many = box.Bounds.Height;
        Assert.True(many > three && many < three + (three - field), $"many {many}, three {three}");
    }

    [AvaloniaFact]
    public void A_growing_text_box_keeps_a_max_lines_of_its_own()
    {
        var box = new TextBox { Classes = { "grow" }, MaxLines = 2 };
        Show(new StackPanel { Children = { box } });
        Assert.Equal(2, box.MaxLines);
    }
}
