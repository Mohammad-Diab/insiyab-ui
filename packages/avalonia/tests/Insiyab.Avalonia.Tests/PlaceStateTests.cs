using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A row's place in its list (first, last) is state the row keeps itself, so a removed row is let go.
public class PlaceStateTests
{
    private static ListBoxItem Row(string title) => new() { Content = new TextBlock { Text = title } };

    private static ListBox Rows(int count)
    {
        var list = new ListBox();
        for (var i = 0; i < count; i++) list.Items.Add(Row("بند " + i));
        return list;
    }

    private static List<bool> Dividers(ListBox list) =>
        list.Items.OfType<ListBoxItem>().Select(item => Part<Border>(item, "PART_Divider").IsVisible).ToList();

    private static DescriptionItem Pair(string term) => new() { Term = term, Content = "قيمة" };

    private static DescriptionList Pairs(int count)
    {
        var dl = new DescriptionList();
        for (var i = 0; i < count; i++) dl.Children.Add(Pair("حقل " + i));
        return dl;
    }

    [AvaloniaFact]
    public void The_first_list_row_has_no_divider_after_inserts_and_removals()
    {
        var list = Rows(3);
        Show(list, width: 500);
        Assert.Equal([false, true, true], Dividers(list));
        list.Items.Insert(0, Row("جديد"));
        Settle(200);
        Assert.Equal([false, true, true, true], Dividers(list));
        list.Items.RemoveAt(0);
        Settle(200);
        Assert.Equal([false, true, true], Dividers(list));
        list.Items.RemoveAt(0);
        Settle(200);
        Assert.Equal([false, true], Dividers(list));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference TakeRow(ListBox list, int index)
    {
        var row = (ListBoxItem)list.Items[index]!;
        list.Items.RemoveAt(index);
        return new WeakReference(row);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference TakePair(DescriptionList dl, int index)
    {
        var item = dl.Children[index];
        dl.Children.RemoveAt(index);
        return new WeakReference(item);
    }

    private static bool Gone(WeakReference weak)
    {
        Settle(500);
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        return !weak.IsAlive;
    }

    [AvaloniaFact]
    public void A_removed_list_row_is_let_go()
    {
        var list = Rows(4);
        Show(list, width: 500);
        Assert.True(Gone(TakeRow(list, 2)), "a middle row");
        Assert.True(Gone(TakeRow(list, 0)), "the first row");
        Assert.Equal([false, true], Dividers(list));
    }

    [AvaloniaFact]
    public void Description_pairs_keep_first_and_last_padding_after_changes()
    {
        var dl = Pairs(3);
        Show(dl, width: 700);
        DescriptionItem At(int i) => (DescriptionItem)dl.Children[i];
        Assert.Equal(new Thickness(0, 0, 0, 12), At(0).Padding);
        Assert.Equal(new Thickness(0, 12, 0, 0), At(2).Padding);
        Assert.Equal(new Thickness(0), At(2).BorderThickness);
        dl.Children.Insert(0, Pair("جديد"));
        Settle(200);
        Assert.Equal(new Thickness(0, 0, 0, 12), At(0).Padding);
        Assert.Equal(new Thickness(0, 0, 0, 1), At(1).BorderThickness);
        Assert.Equal(new Thickness(0, 12, 0, 0), At(3).Padding);
        dl.Children.RemoveAt(3);
        Settle(200);
        Assert.Equal(new Thickness(0, 12, 0, 0), At(2).Padding);
        Assert.Equal(new Thickness(0), At(2).BorderThickness);
        Assert.Equal(new Thickness(0, 0, 0, 12), At(0).Padding);
        dl.Children.RemoveAt(2);
        dl.Children.RemoveAt(1);
        Settle(200);
        Assert.Equal(new Thickness(0), At(0).Padding);
    }

    [AvaloniaFact]
    public void A_removed_description_item_is_let_go()
    {
        var dl = Pairs(4);
        Show(dl, width: 700);
        Assert.True(Gone(TakePair(dl, 1)), "a middle pair");
        Assert.True(Gone(TakePair(dl, 2)), "the last pair");
        Assert.Equal(new Thickness(0, 12, 0, 0), ((DescriptionItem)dl.Children[1]).Padding);
    }
}
