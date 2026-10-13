using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a desktop file tree adds to a TreeView of TreeViewItems: selection that follows the arrow keys, inward and outward arrows that follow the direction, numpad + - *, type-ahead, double-click to open, a context menu on the row, cascading check boxes and a tree that only navigates. The TreeView theme switches <see cref="NavigationProperty"/> on.</summary>
public static class Tree
{
    /// <summary>Up and Down move to the next row showing and select it (with Ctrl they only move), the inward arrow opens a branch then steps into it, the outward one closes it or steps out, Home and End, numpad + and - open and close, * opens everything under the row, typed letters jump to the next row starting with them, Enter opens a branch or activates a leaf, Space selects or ticks. A click selects, a double-click opens a branch or activates a leaf, a right-click or the menu key opens the tree's menu on the row.</summary>
    public static readonly AttachedProperty<bool> NavigationProperty =
        AvaloniaProperty.RegisterAttached<TreeView, bool>("Navigation", typeof(Tree));

    /// <summary>The web's <c>data-ins-tree-checks</c>: a CheckBox as each item's header; ticking a branch ticks everything in it, and a branch partly ticked shows as mixed. Rows are not selected.</summary>
    public static readonly AttachedProperty<bool> ChecksProperty =
        AvaloniaProperty.RegisterAttached<TreeView, bool>("Checks", typeof(Tree));

    /// <summary>The web's <c>data-ins-tree-select="none"</c>: no row is ever selected, for a tree that only navigates.</summary>
    public static readonly AttachedProperty<bool> SelectNoneProperty =
        AvaloniaProperty.RegisterAttached<TreeView, bool>("SelectNone", typeof(Tree));

    /// <summary>Raised on a leaf row by Enter or a double-click: the desktop "open" of a file in a tree.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ActivatedEvent =
        RoutedEvent.Register<TreeViewItem, RoutedEventArgs>("Activated", RoutingStrategies.Bubble);

    private static readonly AttachedProperty<bool> SettlingProperty =
        AvaloniaProperty.RegisterAttached<TreeView, bool>("Settling", typeof(Tree));

    private static readonly AttachedProperty<string> TypedProperty =
        AvaloniaProperty.RegisterAttached<TreeView, string>("Typed", typeof(Tree), "");

    private static readonly AttachedProperty<long> TypedAtProperty =
        AvaloniaProperty.RegisterAttached<TreeView, long>("TypedAt", typeof(Tree));

    private const long TypeAheadMs = 1000;

    static Tree()
    {
        TransformOperationsAnimator.Ensure();
        NavigationProperty.Changed.AddClassHandler<TreeView>((tree, e) =>
        {
            tree.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            tree.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            tree.RemoveHandler(Control.ContextRequestedEvent, OnContextRequested);
            if (!e.GetNewValue<bool>()) return;
            tree.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            tree.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            tree.AddHandler(Control.ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
        });
        ChecksProperty.Changed.AddClassHandler<TreeView>((tree, e) =>
        {
            tree.RemoveHandler(ToggleButton.IsCheckedChangedEvent, OnCheckedChanged);
            tree.Loaded -= OnLoaded;
            if (!e.GetNewValue<bool>()) return;
            tree.AddHandler(ToggleButton.IsCheckedChangedEvent, OnCheckedChanged, RoutingStrategies.Bubble, handledEventsToo: true);
            tree.Loaded += OnLoaded;
            if (tree.IsLoaded) SettleAll(tree);
        });
    }

    public static bool GetNavigation(TreeView element) => element.GetValue(NavigationProperty);

    public static void SetNavigation(TreeView element, bool value) => element.SetValue(NavigationProperty, value);

    public static bool GetChecks(TreeView element) => element.GetValue(ChecksProperty);

    public static void SetChecks(TreeView element, bool value) => element.SetValue(ChecksProperty, value);

    public static bool GetSelectNone(TreeView element) => element.GetValue(SelectNoneProperty);

    public static void SetSelectNone(TreeView element, bool value) => element.SetValue(SelectNoneProperty, value);

    public static void AddActivatedHandler(Interactive element, EventHandler<RoutedEventArgs> handler) => element.AddHandler(ActivatedEvent, handler);

    public static void RemoveActivatedHandler(Interactive element, EventHandler<RoutedEventArgs> handler) => element.RemoveHandler(ActivatedEvent, handler);

    private static bool Selects(TreeView tree) => !GetSelectNone(tree) && !GetChecks(tree);

    private static IEnumerable<TreeViewItem> Children(ItemsControl parent)
    {
        for (var i = 0; i < parent.ItemCount; i++)
            if ((parent.ContainerFromIndex(i) ?? parent.Items[i]) is TreeViewItem item) yield return item;
    }

    private static bool IsBranch(TreeViewItem item) => item.ItemCount > 0;

    private static ContentPresenter? HeaderPresenter(TreeViewItem item) =>
        item.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(c => c.Name == "PART_HeaderPresenter");

    /// <summary>The rows showing, in order: every item whose ancestors are all open.</summary>
    public static List<TreeViewItem> VisibleRows(TreeView tree)
    {
        var rows = new List<TreeViewItem>();
        void Walk(ItemsControl parent)
        {
            foreach (var item in Children(parent))
            {
                if (!item.IsVisible) continue;
                rows.Add(item);
                if (item.IsExpanded) Walk(item);
            }
        }
        Walk(tree);
        return rows;
    }

    /// <summary>An item's label as text: its header string, else the first text shown in its header, else the header's ToString.</summary>
    public static string LabelOf(TreeViewItem item)
    {
        if (item.Header is string s) return s;
        if (HeaderPresenter(item)?.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text is { } shown) return shown;
        return item.Header switch
        {
            ContentControl { Content: string c } => c,
            Control c => c.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault()?.Text ?? "",
            _ => item.Header?.ToString() ?? "",
        };
    }

    private static CheckBox? BoxOf(TreeViewItem item) => item.Header switch
    {
        CheckBox b => b,
        Control c => c.GetLogicalDescendants().OfType<CheckBox>().FirstOrDefault(),
        _ => HeaderPresenter(item)?.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault(),
    };

    private static void FocusRow(TreeViewItem item) => item.Focus(NavigationMethod.Tab);

    private static void Select(TreeView tree, TreeViewItem item)
    {
        if (Selects(tree)) item.SetCurrentValue(TreeViewItem.IsSelectedProperty, true);
    }

    private static void Toggle(TreeViewItem item) => item.SetCurrentValue(TreeViewItem.IsExpandedProperty, !item.IsExpanded);

    private static void Activate(TreeViewItem item) => item.RaiseEvent(new RoutedEventArgs(ActivatedEvent, item));

    private static void ExpandAll(TreeView tree, TreeViewItem item)
    {
        if (IsBranch(item)) tree.ExpandSubTree(item);
    }

    private static TreeViewItem? ItemFrom(object? source) =>
        (source as Visual)?.FindAncestorOfType<TreeViewItem>(includeSelf: true);

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TreeView tree || e.Handled) return;
        if ((e.KeyModifiers & (KeyModifiers.Alt | KeyModifiers.Meta)) != 0) return;
        var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        if (ctrl && e.Key is not (Key.Up or Key.Down or Key.Home or Key.End)) return;
        if (ItemFrom(e.Source) is not { } item || !item.IsFocused && e.Source is not TreeViewItem) return;
        if (e.Source is TextBox) return;
        var rows = VisibleRows(tree);
        var at = rows.IndexOf(item);
        var rtl = tree.FlowDirection == FlowDirection.RightToLeft;
        TreeViewItem? go = null;
        switch (e.Key)
        {
            case Key.Down: go = at + 1 < rows.Count ? rows[at + 1] : null; break;
            case Key.Up: go = at > 0 ? rows[at - 1] : null; break;
            case Key.Home: go = rows.FirstOrDefault(); break;
            case Key.End: go = rows.LastOrDefault(); break;
            case Key.Left or Key.Right:
                var inward = (e.Key == Key.Right) != rtl;
                if (inward)
                {
                    if (IsBranch(item))
                    {
                        if (!item.IsExpanded) item.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
                        else go = Children(item).FirstOrDefault(c => c.IsVisible);
                    }
                }
                else if (IsBranch(item) && item.IsExpanded) item.SetCurrentValue(TreeViewItem.IsExpandedProperty, false);
                else go = item.Parent as TreeViewItem;
                break;
            case Key.Enter:
                Select(tree, item);
                if (!IsBranch(item)) Activate(item);
                else if (!GetChecks(tree)) Toggle(item);
                break;
            case Key.Space:
                if (GetChecks(tree) && BoxOf(item) is { IsEnabled: true } box) box.SetCurrentValue(ToggleButton.IsCheckedProperty, box.IsChecked != true);
                else Select(tree, item);
                break;
            case Key.Add:
                if (IsBranch(item)) item.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
                break;
            case Key.Subtract:
                if (IsBranch(item)) item.SetCurrentValue(TreeViewItem.IsExpandedProperty, false);
                break;
            case Key.Multiply:
                ExpandAll(tree, item);
                break;
            default:
                var ch = e.KeySymbol;
                if (ch is "*")
                {
                    ExpandAll(tree, item);
                    break;
                }
                if (ch is not { Length: 1 } || char.IsWhiteSpace(ch[0]) || char.IsControl(ch[0])) return;
                go = TypeAhead(tree, rows, at, ch);
                if (go is null) return;
                break;
        }
        e.Handled = true;
        if (go is null) return;
        FocusRow(go);
        if (!ctrl) Select(tree, go);
    }

    private static TreeViewItem? TypeAhead(TreeView tree, List<TreeViewItem> rows, int at, string ch)
    {
        if (rows.Count == 0) return null;
        at = Math.Max(at, 0);
        var now = Environment.TickCount64;
        var typed = (now - tree.GetValue(TypedAtProperty) <= TypeAheadMs ? tree.GetValue(TypedProperty) : "") + ch;
        tree.SetValue(TypedProperty, typed);
        tree.SetValue(TypedAtProperty, now);
        var repeat = typed.Length > 1 && typed.All(c => c == typed[0]);
        var query = ArabicSearch.Normalize(repeat ? ch : typed);
        var from = typed.Length > 1 && !repeat ? 0 : 1;
        for (var i = from; i < rows.Count + from; i++)
        {
            var row = rows[(at + i) % rows.Count];
            if (ArabicSearch.Normalize(LabelOf(row)).StartsWith(query, StringComparison.Ordinal)) return row;
        }
        return null;
    }

    private static bool InToggle(object? source, TreeViewItem item)
    {
        for (var v = source as Visual; v is not null && v != item; v = v.GetVisualParent())
            if (v is ToggleButton) return true;
        return false;
    }

    private static bool InRow(object? source, TreeViewItem item)
    {
        for (var v = source as Visual; v is not null && v != item; v = v.GetVisualParent())
        {
            if (v is Control { Name: "PART_ItemsPresenter" or "PART_Branch" }) return false;
            if (v is Control { Name: "PART_LayoutRoot" }) return true;
        }
        return false;
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not TreeView tree || ItemFrom(e.Source) is not { } item) return;
        var left = e.GetCurrentPoint(item).Properties.IsLeftButtonPressed;
        if (InToggle(e.Source, item))
        {
            if (left) FocusRow(item);
            return;
        }
        if (!left || !InRow(e.Source, item)) return;
        if (e.ClickCount == 2)
        {
            e.Handled = true;
            if (!IsBranch(item)) Activate(item);
            else if (!GetChecks(tree)) Toggle(item);
            return;
        }
        if (Selects(tree)) return;
        FocusRow(item);
        e.Handled = true;
    }

    private static void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not TreeView tree || e.Handled || ItemFrom(e.Source) is not { } item) return;
        var pointer = e.TryGetPosition(tree, out _);
        if (pointer && !InRow(e.Source, item)) return;
        if (tree.ContextMenu is null && tree.ContextFlyout is null) return;
        e.Handled = true;
        Select(tree, item);
        FocusRow(item);
        var anchor = item.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == "PART_LayoutRoot") ?? item;
        if (tree.ContextMenu is { } menu)
        {
            menu.Placement = pointer ? PlacementMode.Pointer : PlacementMode.Bottom;
            menu.PlacementTarget = anchor;
            menu.Open(tree);
        }
        else tree.ContextFlyout?.ShowAt(anchor);
    }

    /// <summary>The web's Insiyab.tree(item, 'select'): opens every branch above the item, selects it and scrolls it into view.</summary>
    public static void Reveal(TreeViewItem item)
    {
        for (var p = item.Parent as TreeViewItem; p is not null; p = p.Parent as TreeViewItem) p.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
        if (item.FindLogicalAncestorOfType<TreeView>() is { } tree) Select(tree, item);
        item.BringIntoView();
    }

    /// <summary>The web's Insiyab.tree(tree) on a tree of check boxes: every item whose box is ticked.</summary>
    public static List<TreeViewItem> CheckedItems(TreeView tree)
    {
        var found = new List<TreeViewItem>();
        void Walk(ItemsControl parent)
        {
            foreach (var item in Children(parent))
            {
                if (BoxOf(item)?.IsChecked == true) found.Add(item);
                Walk(item);
            }
        }
        Walk(tree);
        return found;
    }

    private static void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TreeView tree) SettleAll(tree);
    }

    /// <summary>A branch ticked in the markup ticks what is in it; then every branch is worked out again from what is under it.</summary>
    private static void SettleAll(TreeView tree)
    {
        tree.SetValue(SettlingProperty, true);
        try
        {
            void Down(ItemsControl parent)
            {
                foreach (var item in Children(parent))
                {
                    if (BoxOf(item)?.IsChecked == true) Cascade(item, true);
                    else Down(item);
                }
            }
            Down(tree);
            void Up(ItemsControl parent)
            {
                foreach (var item in Children(parent))
                {
                    Up(item);
                    Settle(item);
                }
            }
            Up(tree);
        }
        finally
        {
            tree.SetValue(SettlingProperty, false);
        }
    }

    private static void Cascade(TreeViewItem item, bool value)
    {
        foreach (var child in Children(item))
        {
            if (BoxOf(child) is { } b) b.SetCurrentValue(ToggleButton.IsCheckedProperty, value);
            Cascade(child, value);
        }
    }

    /// <summary>A branch from its children: all ticked, ticked; none, not; else mixed.</summary>
    private static void Settle(TreeViewItem item)
    {
        if (BoxOf(item) is not { } box) return;
        var boxes = Children(item).Select(BoxOf).Where(b => b is not null).ToList();
        if (boxes.Count == 0) return;
        var on = boxes.Count(b => b!.IsChecked == true);
        var mixed = boxes.Count(b => b!.IsChecked is null);
        bool? value = on == boxes.Count ? true : on > 0 || mixed > 0 ? null : false;
        box.SetCurrentValue(ToggleButton.IsCheckedProperty, value);
    }

    private static void OnCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not TreeView tree || tree.GetValue(SettlingProperty) || e.Source is not CheckBox box) return;
        if (ItemFrom(box) is not { } item || BoxOf(item) != box) return;
        if (box.IsChecked is null) return;
        tree.SetValue(SettlingProperty, true);
        try
        {
            Cascade(item, box.IsChecked == true);
            for (var p = item.Parent as TreeViewItem; p is not null; p = p.Parent as TreeViewItem) Settle(p);
        }
        finally
        {
            tree.SetValue(SettlingProperty, false);
        }
    }
}
