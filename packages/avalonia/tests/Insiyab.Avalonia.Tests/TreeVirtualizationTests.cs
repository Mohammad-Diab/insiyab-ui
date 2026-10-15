using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TreeVirtualizationTests
{
    private const int Count = 2000;

    private sealed record Node(string Name, Node[] Kids);

    private sealed class Vm : INotifyPropertyChanged
    {
        private bool _open;

        public string Name { get; init; } = "";

        public Vm[] Kids { get; init; } = [];

        public bool Open
        {
            get => _open;
            set
            {
                if (_open == value) return;
                _open = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Open)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static void Press(Window w, Key key, RawInputModifiers mods = RawInputModifiers.None, string? symbol = null)
    {
        w.KeyPress(key, mods, PhysicalKey.None, symbol);
        w.KeyRelease(key, mods, PhysicalKey.None, symbol);
        Settle(200);
    }

    private static Control? Focused(Window w) => w.FocusManager?.GetFocusedElement() as Control;

    private static string FocusedLabel(Window w) => Tree.LabelOf(Assert.IsType<TreeViewItem>(Focused(w)));

    private static int Realized(TreeView t) => t.GetVisualDescendants().OfType<TreeViewItem>().Count(i => i.IsEffectivelyVisible);

    private static ScrollViewer Sv(TreeView t) => Part<ScrollViewer>(t, "PART_ScrollViewer");

    private static TreeViewItem Container(ItemsControl level, int index) => Assert.IsType<TreeViewItem>(level.ContainerFromIndex(index));

    private static double Top(TreeView t, Control row) => row.TranslatePoint(default, Sv(t))!.Value.Y;

    private static TreeViewItem? FindRow(TreeView t, object item) =>
        t.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(r => r.IsEffectivelyVisible && (ReferenceEquals(r, item) || ReferenceEquals(r.DataContext, item)));

    private static void ScrollEnd(TreeView t)
    {
        for (var i = 0; i < 3; i++)
        {
            Sv(t).ScrollToEnd();
            Settle(100);
        }
    }

    private static void ScrollHome(TreeView t)
    {
        for (var i = 0; i < 3; i++)
        {
            Sv(t).ScrollToHome();
            Settle(100);
        }
    }

    private static TreeView Roots(int count = Count)
    {
        var tree = new TreeView();
        for (var i = 0; i < count; i++)
        {
            var row = new TreeViewItem { Header = $"بند {i}" };
            row.Items.Add(new TreeViewItem { Header = $"بند {i}.0" });
            row.Items.Add(new TreeViewItem { Header = $"بند {i}.1" });
            tree.Items.Add(row);
        }
        return tree;
    }

    private static TreeView Branch(out TreeViewItem root, bool open = true)
    {
        var tree = new TreeView();
        root = new TreeViewItem { Header = "المجلد", IsExpanded = open };
        for (var i = 0; i < Count; i++) root.Items.Add(new TreeViewItem { Header = $"ملف {i}" });
        tree.Items.Add(root);
        tree.Items.Add(new TreeViewItem { Header = "بعده 1" });
        tree.Items.Add(new TreeViewItem { Header = "بعده 2" });
        return tree;
    }

    private static Node[] Nodes(int count = Count) =>
        Enumerable.Range(0, count)
            .Select(i => new Node($"عقد {i}", Enumerable.Range(0, 3).Select(k => new Node($"عقد {i}.{k}", [])).ToArray()))
            .ToArray();

    private static TreeView Bound(Node[] nodes) => new()
    {
        ItemsSource = nodes,
        ItemTemplate = new FuncTreeDataTemplate<Node>((n, _) => new TextBlock { Text = n.Name }, n => n.Kids),
    };

    [AvaloniaFact]
    public void Two_thousand_roots_build_only_the_rows_in_view()
    {
        var tree = Roots();
        Show(tree);
        Assert.InRange(Realized(tree), 1, 99);
        Assert.InRange(Sv(tree).Extent.Height, Count * 34 - 1, Count * 34 + 1);
    }

    [AvaloniaFact]
    public void A_branch_of_two_thousand_builds_only_the_rows_in_view_with_an_exact_extent()
    {
        var tree = Branch(out var root);
        Show(tree);
        Assert.InRange(Realized(tree), 1, 99);
        Assert.InRange(Sv(tree).Extent.Height, (Count + 3) * 34 - 1, (Count + 3) * 34 + 1);
        Assert.IsType<StackPanel>(tree.ItemsPanelRoot);
        Assert.IsType<VirtualizingStackPanel>(root.ItemsPanelRoot);
        ScrollEnd(tree);
        Assert.True(Container(tree, 2).IsEffectivelyVisible);
        Assert.InRange(Realized(tree), 1, 99);
    }

    [AvaloniaFact]
    public void A_bound_tree_of_two_thousand_builds_only_the_rows_in_view()
    {
        var tree = Bound(Nodes());
        Show(tree);
        Assert.InRange(Realized(tree), 1, 99);
        var late = new TreeView();
        Show(late);
        late.ItemsSource = Nodes();
        Settle();
        Assert.InRange(Realized(late), 1, 99);
    }

    [AvaloniaFact]
    public void Arrows_cross_the_edge_of_the_built_rows()
    {
        var tree = Branch(out var root);
        var w = Show(tree);
        Assert.IsType<TreeViewItem>(root.Items[0]).Focus(NavigationMethod.Tab);
        for (var i = 0; i < 15; i++) Press(w, Key.Down);
        var row = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal("ملف 15", Tree.LabelOf(row));
        Assert.True(row.IsSelected);
        Assert.True(row.IsEffectivelyVisible);
        Press(w, Key.End);
        Assert.Equal("بعده 2", FocusedLabel(w));
        Press(w, Key.Up);
        Assert.Equal("بعده 1", FocusedLabel(w));
        Press(w, Key.Up);
        row = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal("ملف 1999", Tree.LabelOf(row));
        Assert.True(row.IsEffectivelyVisible);
        Assert.True(row.IsSelected);
        Press(w, Key.Home);
        Assert.Equal("المجلد", FocusedLabel(w));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Inward_and_outward_arrows_far_down_a_bound_tree(string dir)
    {
        var tree = Bound(Nodes());
        var w = Show(tree, Dir(dir));
        var inward = dir == "rtl" ? Key.Left : Key.Right;
        var outward = dir == "rtl" ? Key.Right : Key.Left;
        Container(tree, 0).Focus(NavigationMethod.Tab);
        Press(w, Key.End);
        var last = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal("عقد 1999", Tree.LabelOf(last));
        Press(w, inward);
        var opened = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal("عقد 1999", Tree.LabelOf(opened));
        Assert.True(opened.IsExpanded);
        Assert.True(opened.IsEffectivelyVisible);
        Assert.InRange(Top(tree, opened), 0, Sv(tree).Viewport.Height);
        Press(w, inward);
        Assert.Equal("عقد 1999.0", FocusedLabel(w));
        Press(w, outward);
        Assert.Equal("عقد 1999", FocusedLabel(w));
        Press(w, outward);
        var closed = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal("عقد 1999", Tree.LabelOf(closed));
        Assert.False(closed.IsExpanded);
    }

    [AvaloniaFact]
    public void Type_ahead_reaches_a_row_out_of_view()
    {
        var nodes = Nodes();
        nodes[1500] = nodes[1500] with { Name = "زيتون" };
        var tree = Bound(nodes);
        var w = Show(tree);
        Container(tree, 0).Focus(NavigationMethod.Tab);
        Press(w, Key.Z, RawInputModifiers.None, "ز");
        var row = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal("زيتون", Tree.LabelOf(row));
        Assert.True(row.IsSelected);
        Assert.True(row.IsEffectivelyVisible);
    }

    [AvaloniaTheory]
    [InlineData("own")]
    [InlineData("bound")]
    public void Selection_stays_when_its_row_scrolls_away(string kind)
    {
        if (kind == "own")
        {
            var tree = Roots();
            Show(tree);
            var root3 = Assert.IsType<TreeViewItem>(tree.Items[3]);
            root3.IsSelected = true;
            Settle(100);
            ScrollEnd(tree);
            Assert.Same(root3, tree.SelectedItem);
            Assert.DoesNotContain(tree.GetVisualDescendants().OfType<TreeViewItem>().Where(i => i.IsEffectivelyVisible), i => i.IsSelected && !ReferenceEquals(i, root3));
            ScrollHome(tree);
            Assert.True(root3.IsSelected);
            AssertBrush(root3, "InsTreeSelected", () => Part<Border>(root3, "PART_LayoutRoot").Background);
        }
        else
        {
            var nodes = Nodes();
            var tree = Bound(nodes);
            Show(tree);
            tree.SelectedItem = nodes[3];
            Settle(100);
            ScrollEnd(tree);
            Assert.DoesNotContain(tree.GetVisualDescendants().OfType<TreeViewItem>().Where(i => i.IsEffectivelyVisible), i => i.IsSelected);
            ScrollHome(tree);
            Assert.True(Container(tree, 3).IsSelected);
            Assert.Same(nodes[3], tree.SelectedItem);
        }
    }

    [AvaloniaFact]
    public void A_bound_branch_stays_open_when_scrolled_away_and_opens_no_other()
    {
        var tree = Bound(Nodes());
        var w = Show(tree);
        var first = Container(tree, 0);
        Click(Part<ToggleButton>(first, "PART_ExpandCollapseChevron"), w);
        Assert.True(first.IsExpanded);
        ScrollEnd(tree);
        Assert.DoesNotContain(tree.GetVisualDescendants().OfType<TreeViewItem>().Where(i => i.IsEffectivelyVisible), i => i.IsExpanded);
        ScrollHome(tree);
        var back = Container(tree, 0);
        Assert.True(back.IsExpanded);
        Assert.Equal(3, back.GetRealizedContainers().Count());
        Assert.Contains(":ins-restored", back.Classes);
        Click(Part<ToggleButton>(back, "PART_ExpandCollapseChevron"), w);
        Click(Part<ToggleButton>(back, "PART_ExpandCollapseChevron"), w);
        Assert.True(back.IsExpanded);
        Assert.DoesNotContain(":ins-restored", back.Classes);
    }

    [AvaloniaFact]
    public void An_app_binding_IsExpanded_keeps_its_values()
    {
        var vms = Enumerable.Range(0, Count)
            .Select(i => new Vm { Name = $"عقد {i}", Kids = [new Vm { Name = $"عقد {i}.0" }, new Vm { Name = $"عقد {i}.1" }] })
            .ToArray();
        var tree = new TreeView
        {
            ItemsSource = vms,
            ItemTemplate = new FuncTreeDataTemplate<Vm>((v, _) => new TextBlock { Text = v.Name }, v => v.Kids),
        };
        tree.Styles.Add(new Style(x => x.OfType<TreeViewItem>())
        {
            Setters = { new Setter(TreeViewItem.IsExpandedProperty, new Binding("Open") { Mode = BindingMode.TwoWay }) },
        });
        var w = Show(tree);
        Click(Part<ToggleButton>(Container(tree, 0), "PART_ExpandCollapseChevron"), w);
        Assert.True(vms[0].Open);
        ScrollEnd(tree);
        Assert.True(vms[0].Open);
        Assert.Equal(1, vms.Count(v => v.Open));
        vms[5].Open = true;
        Settle(100);
        ScrollHome(tree);
        Assert.True(Container(tree, 0).IsExpanded);
        Assert.True(Container(tree, 5).IsExpanded);
        Assert.Equal(2, vms.Count(v => v.Open));
    }

    [AvaloniaFact]
    public void Closing_and_opening_a_big_branch_updates_the_rows()
    {
        var tree = Branch(out var root);
        var w = Show(tree);
        root.IsExpanded = false;
        Settle(100);
        Assert.Equal(3, Realized(tree));
        Assert.InRange(Sv(tree).Extent.Height, Sv(tree).Viewport.Height - 1, Sv(tree).Viewport.Height + 1);
        root.IsExpanded = true;
        Settle(100);
        Assert.InRange(Realized(tree), 1, 99);
        Assert.InRange(Sv(tree).Extent.Height, (Count + 3) * 34 - 1, (Count + 3) * 34 + 1);
        root.Focus(NavigationMethod.Tab);
        Press(w, Key.Left);
        Assert.False(root.IsExpanded);
        Press(w, Key.Right);
        Assert.True(root.IsExpanded);
    }

    [AvaloniaTheory]
    [InlineData("own")]
    [InlineData("bound")]
    public void Keys_continue_from_a_focused_row_scrolled_away(string kind)
    {
        var tree = kind == "own" ? Roots() : Bound(Nodes());
        var w = Show(tree);
        var first = kind == "own" ? Assert.IsType<TreeViewItem>(tree.Items[0]) : Container(tree, 0);
        first.Focus(NavigationMethod.Tab);
        ScrollEnd(tree);
        Settle();
        Press(w, Key.Down);
        var row = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal(kind == "own" ? "بند 1" : "عقد 1", Tree.LabelOf(row));
        Assert.True(row.IsEffectivelyVisible);
        Assert.True(row.IsSelected);
        Assert.False(tree.Focusable);
    }

    [AvaloniaTheory]
    [InlineData("own", 2000, 30000)]
    [InlineData("bound", 2000, 30000)]
    [InlineData("own", 60, 700)]
    [InlineData("own", 60, 1400)]
    [InlineData("bound", 60, 700)]
    [InlineData("bound", 60, 1400)]
    public void Opening_and_closing_a_row_in_a_virtual_level_keeps_it_in_place(string kind, int count, int offset)
    {
        var tree = kind == "own" ? Roots(count) : Bound(Nodes(count));
        var w = Show(tree);
        Sv(tree).Offset = new Vector(0, offset);
        Settle(150);
        var row = tree.GetVisualDescendants().OfType<TreeViewItem>().Where(r => r.IsEffectivelyVisible).Skip(5).First();
        var item = row.DataContext ?? row;
        var top = Top(tree, row);
        foreach (var open in new[] { true, false })
        {
            Click(Part<ToggleButton>(FindRow(tree, item)!, "PART_ExpandCollapseChevron"), w);
            var now = FindRow(tree, item);
            Assert.NotNull(now);
            Assert.Equal(open, now.IsExpanded);
            Assert.InRange(Top(tree, now), top - 1, top + 1);
            Assert.Same(now, Focused(w));
        }
    }

    [AvaloniaTheory]
    [InlineData("own")]
    [InlineData("bound")]
    public void Opening_and_closing_by_key_keeps_the_row_in_place(string kind)
    {
        var tree = kind == "own" ? Roots() : Bound(Nodes());
        var w = Show(tree);
        Sv(tree).Offset = new Vector(0, 30000);
        Settle(150);
        var row = tree.GetVisualDescendants().OfType<TreeViewItem>().Where(r => r.IsEffectivelyVisible).Skip(5).First();
        var item = row.DataContext ?? row;
        row.Focus(NavigationMethod.Tab);
        Settle(100);
        var top = Top(tree, row);
        foreach (var open in new[] { true, false })
        {
            Press(w, open ? Key.Right : Key.Left);
            var now = FindRow(tree, item);
            Assert.NotNull(now);
            Assert.Equal(open, now.IsExpanded);
            Assert.InRange(Top(tree, now), top - 1, top + 1);
            Assert.Same(now, Focused(w));
        }
    }

    [AvaloniaFact]
    public void Reveal_brings_a_far_row_into_view()
    {
        var tree = Branch(out var root, open: false);
        Show(tree);
        var kid = Assert.IsType<TreeViewItem>(root.Items[1500]);
        Tree.Reveal(kid);
        Settle();
        Assert.True(root.IsExpanded);
        Assert.True(kid.IsSelected);
        Assert.True(kid.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Small_levels_checks_and_app_panels_keep_their_panel()
    {
        var small = new TreeView();
        for (var i = 0; i < 3; i++) small.Items.Add(new TreeViewItem { Header = $"بند {i}" });
        Show(small);
        Assert.IsType<StackPanel>(small.ItemsPanelRoot);

        var checks = new TreeView();
        Tree.SetChecks(checks, true);
        for (var i = 0; i < 300; i++) checks.Items.Add(new TreeViewItem { Header = new CheckBox { Content = $"بند {i}" } });
        Show(checks);
        Assert.IsType<StackPanel>(checks.ItemsPanelRoot);

        var app = new TreeView { ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Spacing = 2 }) };
        for (var i = 0; i < 60; i++) app.Items.Add(new TreeViewItem { Header = $"بند {i}" });
        Show(app);
        Assert.Equal(2, Assert.IsType<StackPanel>(app.ItemsPanelRoot).Spacing);
    }

    [AvaloniaFact]
    public void Many_selected_rows_survive_scrolling()
    {
        var nodes = Nodes();
        var tree = Bound(nodes);
        tree.SelectionMode = SelectionMode.Multiple;
        Show(tree);
        tree.SelectedItems!.Add(nodes[1]);
        tree.SelectedItems!.Add(nodes[2]);
        Settle(100);
        ScrollEnd(tree);
        ScrollHome(tree);
        Assert.True(Container(tree, 1).IsSelected);
        Assert.True(Container(tree, 2).IsSelected);
        Assert.Equal(2, tree.SelectedItems!.Count);
    }

    [AvaloniaFact]
    public void Star_opens_rows_not_built_yet()
    {
        var nodes = Nodes();
        nodes[0] = new Node("جذر", Enumerable.Range(0, 120).Select(i => new Node($"فرع {i}", [new Node($"ورقة {i}.0", []), new Node($"ورقة {i}.1", [])])).ToArray());
        var tree = Bound(nodes);
        var w = Show(tree);
        var first = Container(tree, 0);
        first.Focus(NavigationMethod.Tab);
        Press(w, Key.Multiply);
        Assert.True(first.IsExpanded);
        first.ScrollIntoView(100);
        Settle();
        Assert.True(Container(first, 100).IsExpanded);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Rows_far_down_indent_toward_the_start(string dir)
    {
        var tree = Branch(out var root);
        Show(tree, Dir(dir));
        // TranslatePoint reports layout space, where the start edge is X in both directions; the mirror is applied when drawing.
        double Start(Control row) => Part<Border>(row, "PART_LayoutRoot").TranslatePoint(default, tree)!.Value.X;
        var top = Start(root);
        Sv(tree).Offset = new Vector(0, Sv(tree).Extent.Height / 2);
        Settle(200);
        var kid = root.GetVisualDescendants().OfType<TreeViewItem>().First(k => k.IsEffectivelyVisible && !ReferenceEquals(k, root));
        Assert.True(Start(kid) >= top + 20, $"kid starts {Start(kid)}, root started {top}");
    }
}
