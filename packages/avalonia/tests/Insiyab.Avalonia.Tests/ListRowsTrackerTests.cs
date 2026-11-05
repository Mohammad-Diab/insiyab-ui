using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ListRowsTrackerTests
{
    // How many of the library's row handlers the list's ChildIndexChanged event holds.
    private static int Followers(ItemsControl list)
    {
        var count = 0;
        for (var t = list.GetType(); t is not null; t = t.BaseType)
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (f.FieldType == typeof(EventHandler<ChildIndexChangedEventArgs>) && f.GetValue(list) is Delegate d)
                    count += d.GetInvocationList().Count(h => h.Method.DeclaringType?.FullName?.Contains("ListRows") == true);
        return count;
    }

    private static (ListBox list, ObservableCollection<string> items) Rig()
    {
        var items = new ObservableCollection<string>(Enumerable.Range(1, 30).Select(i => $"الصف {i}"));
        var list = new ListBox { ItemsSource = items, Height = 400 };
        Show(list, FlowDirection.RightToLeft, null, 640, 500);
        return (list, items);
    }

    private static void AssertOnlyFirstIsMarked(ListBox list)
    {
        var marked = list.GetRealizedContainers().OfType<ListBoxItem>().Where(i => i.Classes.Contains(":first")).ToList();
        var first = Assert.Single(marked);
        Assert.Same(list.ContainerFromIndex(0), first);
    }

    [AvaloniaFact]
    public void One_tracker_follows_every_row_of_a_list()
    {
        var (list, _) = Rig();
        Assert.True(list.GetRealizedContainers().Count() > 1);
        Assert.Equal(1, Followers(list));
    }

    [AvaloniaFact]
    public void The_first_row_mark_follows_inserts_and_removes()
    {
        var (list, items) = Rig();
        items.RemoveAt(0);
        Settle(300);
        AssertOnlyFirstIsMarked(list);
        items.Insert(0, "صف جديد");
        Settle(300);
        AssertOnlyFirstIsMarked(list);
        items.Clear();
        Settle(300);
        Assert.Equal(0, Followers(list));
    }
}
