using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

namespace PerfLab.Pages;

public partial class ListsPage : UserControl
{
    public ListsPage()
    {
        InitializeComponent();
        Orders.ItemsSource = Sample.Orders(300);
        var folder = Geometry("IconFolder");
        var file = Geometry("IconPages");
        for (var f = 0; f < 6; f++)
        {
            var branch = Node(folder, $"{Sample.Cities[f]}", f == 0);
            for (var g = 0; g < 4; g++)
            {
                var sub = Node(folder, $"الربع {g + 1}", f == 0 && g == 0);
                for (var d = 0; d < 5; d++) sub.Items.Add(Node(file, $"فاتورة {10400 + f * 20 + g * 5 + d}.pdf", false));
                branch.Items.Add(sub);
            }
            Files.Items.Add(branch);
        }
    }

    private static Geometry? Geometry(string key) =>
        Application.Current!.TryFindResource(key, out var v) ? v as Geometry : null;

    private static TreeViewItem Node(Geometry? icon, string text, bool open)
    {
        var label = new StackPanel { Classes = { "tree-label" } };
        label.Children.Add(new Icon { Data = icon });
        label.Children.Add(new TextBlock { Text = text });
        return new TreeViewItem { Header = label, IsExpanded = open };
    }
}
