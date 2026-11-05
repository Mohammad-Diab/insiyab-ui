using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ChipOverflowTests
{
    private static readonly string[] Names = ["الكل", "التصميم", "البرمجة", "الشبكات", "قواعد البيانات", "الأمن", "الذكاء الاصطناعي", "الرسوميات"];

    private static (Toolbar Row, ToggleButton[] Chips) Row(double width, int checkedAt = 0)
    {
        var row = new Toolbar { Classes = { "chips" }, Width = width };
        var chips = Names.Select((n, i) => new ToggleButton { Content = n, Classes = { "chip" }, IsChecked = i == checkedAt }).ToArray();
        foreach (var c in chips) row.Items.Add(c);
        return (row, chips);
    }

    [AvaloniaFact]
    public void Chips_that_do_not_fit_go_into_a_more_chip_with_their_count()
    {
        var (row, chips) = Row(360);
        Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, FlowDirection.RightToLeft, width: 800);
        var more = row.MoreButton!;

        var moved = row.OverflowItems.ToList();
        Assert.NotEmpty(moved);
        Assert.True(more.IsVisible);
        Assert.Contains("chip", more.Classes);
        Assert.Equal(30, more.Bounds.Height, 0.5);
        Assert.Contains(more.GetVisualDescendants().OfType<TextBlock>(), t => t.IsEffectivelyVisible && t.Text == "+" + moved.Count);
        Assert.Equal("المزيد", AutomationProperties.GetName(more));
        Assert.True(row.DesiredSize.Width <= 360.5);
        Assert.All(chips.Where(c => !moved.Contains(c)), c => Assert.True(c.IsVisible));
    }

    [AvaloniaFact]
    public void The_chip_row_has_no_bar_of_its_own()
    {
        var (row, _) = Row(900);
        Show(new StackPanel { Children = { row } }, width: 1000);
        Assert.Equal(new Thickness(0), row.Padding);
        Assert.Equal(new Thickness(0), row.BorderThickness);
        Assert.Equal(Colors.Transparent, ColorOf(row.Background));
        Assert.Equal(6, Part<ToolbarPanel>(row, "PART_Panel").Spacing);
        Assert.Empty(row.OverflowItems);
        Assert.False(row.MoreButton!.IsVisible);
    }

    [AvaloniaFact]
    public void The_checked_chip_always_stays_in_the_row()
    {
        var (row, chips) = Row(360, checkedAt: Names.Length - 1);
        Show(new StackPanel { Children = { row } }, FlowDirection.RightToLeft, width: 800);

        Assert.True(chips[^1].IsVisible);
        Assert.DoesNotContain(chips[^1], row.OverflowItems);
        Assert.Contains(chips[^2], row.OverflowItems);
        Assert.True(chips[0].IsVisible);
        Assert.True(row.DesiredSize.Width <= 360.5);
    }

    [AvaloniaFact]
    public void A_chip_chosen_in_the_menu_comes_into_the_row()
    {
        var (row, chips) = Row(360);
        Show(new StackPanel { Children = { row } }, FlowDirection.RightToLeft, width: 800);
        var hidden = (ToggleButton)row.OverflowItems.Last();
        var entry = row.MenuEntries().OfType<MenuItem>().Single(m => (string?)m.Header == (string)hidden.Content!);
        Assert.Equal(MenuItemToggleType.CheckBox, entry.ToggleType);
        Assert.False(entry.IsChecked);

        entry.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        chips[0].IsChecked = false;
        Settle();
        Assert.True(hidden.IsChecked);
        Assert.True(hidden.IsVisible);
        Assert.DoesNotContain(hidden, row.OverflowItems);
    }
}
