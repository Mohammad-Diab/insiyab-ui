using System.Collections;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a desktop file tree adds to a TreeView of TreeViewItems: selection that follows the arrow keys, inward and outward arrows that follow the direction, numpad + - *, type-ahead, double-click to open, a context menu on the row, cascading check boxes, a tree that only navigates, and a level of 50 items or more builds only the rows in or near view. The TreeView theme switches <see cref="NavigationProperty"/> on.</summary>
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

    private const int VirtualizeFrom = 50;

    private static readonly ITemplate<Panel?> VirtualPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

    private static readonly AttachedProperty<bool> HookedProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, bool>("Hooked", typeof(Tree));

    private static readonly AttachedProperty<object?> ItemOfProperty =
        AvaloniaProperty.RegisterAttached<TreeViewItem, object?>("ItemOf", typeof(Tree));

    private static readonly AttachedProperty<object[]?> CurrentProperty =
        AvaloniaProperty.RegisterAttached<TreeView, object[]?>("Current", typeof(Tree));

    private static readonly AttachedProperty<bool> ParkedProperty =
        AvaloniaProperty.RegisterAttached<TreeView, bool>("Parked", typeof(Tree));

    private static readonly AttachedProperty<bool> LentFocusProperty =
        AvaloniaProperty.RegisterAttached<TreeView, bool>("LentFocus", typeof(Tree));

    private static readonly AttachedProperty<bool> RecycledProperty =
        AvaloniaProperty.RegisterAttached<TreeViewItem, bool>("Recycled", typeof(Tree));

    private static readonly AttachedProperty<NavigationMethod> MethodProperty =
        AvaloniaProperty.RegisterAttached<TreeView, NavigationMethod>("Method", typeof(Tree), NavigationMethod.Tab);

    private static readonly AttachedProperty<Anchor?> AnchorProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, Anchor?>("Anchor", typeof(Tree));

    private static readonly ConditionalWeakTable<TreeView, ConditionalWeakTable<object, StrongBox<bool>>> Opened = new();

    private static int _preparing;

    private sealed record Anchor(TreeView Tree, object[] Path, double Top, bool Focus, NavigationMethod Method);

    static Tree()
    {
        TransformOperationsAnimator.Ensure();
        NavigationProperty.Changed.AddClassHandler<TreeView>((tree, e) =>
        {
            tree.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            tree.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            tree.RemoveHandler(Control.ContextRequestedEvent, OnContextRequested);
            tree.RemoveHandler(InputElement.GotFocusEvent, OnGotFocus);
            tree.RemoveHandler(InputElement.LostFocusEvent, OnLostFocus);
            if (!e.GetNewValue<bool>())
            {
                Refit(tree);
                return;
            }
            tree.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            tree.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            tree.AddHandler(Control.ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
            tree.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
            tree.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Bubble, handledEventsToo: true);
            Refit(tree);
        });
        ChecksProperty.Changed.AddClassHandler<TreeView>((tree, e) =>
        {
            tree.RemoveHandler(ToggleButton.IsCheckedChangedEvent, OnCheckedChanged);
            tree.Loaded -= OnLoaded;
            Refit(tree);
            if (!e.GetNewValue<bool>()) return;
            tree.AddHandler(ToggleButton.IsCheckedChangedEvent, OnCheckedChanged, RoutingStrategies.Bubble, handledEventsToo: true);
            tree.Loaded += OnLoaded;
            if (tree.IsLoaded) SettleAll(tree);
        });
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TreeView>((t, _) => Fit(t));
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TreeViewItem>((t, _) => Fit(t));
        ItemsControl.ItemCountProperty.Changed.AddClassHandler<TreeView>((t, _) => Fit(t));
        ItemsControl.ItemCountProperty.Changed.AddClassHandler<TreeViewItem>((t, _) => Fit(t));
        TreeViewItem.IsExpandedProperty.Changed.AddClassHandler<TreeViewItem>((t, _) =>
        {
            Restored(t, false);
            Hold(t);
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

    private static TreeView? TreeOf(ItemsControl level) => level as TreeView ?? level.FindLogicalAncestorOfType<TreeView>();

    private static bool IsVirtual(ItemsControl level) => ReferenceEquals(level.ItemsPanel, VirtualPanel);

    private static void Fit(ItemsControl level)
    {
        if (TreeOf(level) is not { } tree) return;
        var nav = GetNavigation(tree);
        if (nav && !level.GetValue(HookedProperty))
        {
            level.SetValue(HookedProperty, true);
            level.ContainerPrepared += OnPrepared;
            level.ContainerClearing += OnClearing;
        }
        var want = nav && level.ItemCount >= VirtualizeFrom && !GetChecks(tree);
        if (want == IsVirtual(level)) return;
        if (!want) level.ClearValue(ItemsControl.ItemsPanelProperty);
        else if (!level.IsSet(ItemsControl.ItemsPanelProperty)) level.SetCurrentValue(ItemsControl.ItemsPanelProperty, VirtualPanel);
    }

    private static void Refit(TreeView tree)
    {
        Fit(tree);
        foreach (var item in tree.GetLogicalDescendants().OfType<TreeViewItem>().ToList()) Fit(item);
    }

    private static void Restored(TreeViewItem row, bool on) => ((IPseudoClasses)row.Classes).Set(":ins-restored", on);

    private static void OnPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (sender is not ItemsControl level || e.Container is not TreeViewItem row) return;
        row.ClearValue(RecycledProperty);
        var item = e.Index >= 0 && e.Index < level.ItemCount ? level.ItemsView[e.Index] : null;
        if (item is null || ReferenceEquals(item, row)) return;
        row.SetValue(ItemOfProperty, item);
        if (row.IsSet(TreeViewItem.IsExpandedProperty) || TreeOf(level) is not { } tree) return;
        if (!Opened.TryGetValue(tree, out var open) || !open.TryGetValue(item, out var was) || !was.Value) return;
        _preparing++;
        try
        {
            row.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
        }
        finally
        {
            _preparing--;
        }
        Restored(row, true);
    }

    private static void OnClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (sender is not ItemsControl level || e.Container is not TreeViewItem row || row.GetValue(ItemOfProperty) is not { } item) return;
        row.SetValue(RecycledProperty, true);
        if (TreeOf(level) is { } tree) Opened.GetOrCreateValue(tree).AddOrUpdate(item, new StrongBox<bool>(row.IsExpanded));
        row.ClearValue(ItemOfProperty);
        row.ClearValue(TreeViewItem.IsExpandedProperty);
        Restored(row, false);
    }

    private static ItemsControl? OwnerOf(TreeViewItem row) => ItemsControl.ItemsControlFromItemContainer(row) ?? row.Parent as ItemsControl;

    private static bool InVirtual(TreeViewItem row)
    {
        for (var level = OwnerOf(row); level is not null; level = level is TreeViewItem up ? OwnerOf(up) : null)
            if (IsVirtual(level)) return true;
        return false;
    }

    private static void Hold(TreeViewItem row)
    {
        if (_preparing > 0 || row.GetValue(RecycledProperty) || row.Bounds.Height <= 0) return;
        if (row.FindLogicalAncestorOfType<TreeView>() is not { } tree || !GetNavigation(tree) || !InVirtual(row)) return;
        if (row.FindAncestorOfType<ScrollViewer>() is not { } view || view.GetValue(AnchorProperty) is not null) return;
        if (row.TranslatePoint(default, view) is not { } at || at.Y + row.Bounds.Height < 0 || at.Y > view.Viewport.Height) return;
        view.SetValue(AnchorProperty, new Anchor(tree, PathOf(row), at.Y, row.IsKeyboardFocusWithin, tree.GetValue(MethodProperty)));
        view.LayoutUpdated += OnLaidOut;
    }

    private static void OnLaidOut(object? sender, EventArgs e)
    {
        if (sender is not ScrollViewer view) return;
        view.LayoutUpdated -= OnLaidOut;
        if (view.GetValue(AnchorProperty) is not { } anchor) return;
        view.ClearValue(AnchorProperty);
        if (anchor.Focus && Locate(anchor.Tree, anchor.Path, out var found) is { } held && found && !held.IsFocused) held.Focus(anchor.Method);
        Seat(view, anchor);
    }

    private static void Seat(ScrollViewer view, Anchor anchor)
    {
        for (var pass = 0; pass < 3; pass++)
        {
            if (Locate(anchor.Tree, anchor.Path, out var whole) is not { } row || !whole || row.TranslatePoint(default, view) is not { } at) return;
            var delta = at.Y - anchor.Top;
            if (Math.Abs(delta) <= 0.5) return;
            var max = Math.Max(0, view.Extent.Height - view.Viewport.Height);
            view.Offset = new Vector(view.Offset.X, Math.Clamp(view.Offset.Y + delta, 0, max));
            view.UpdateLayout();
        }
    }

    private static IEnumerable<TreeViewItem> Children(ItemsControl parent)
    {
        for (var i = 0; i < parent.ItemCount; i++)
            if ((parent.ContainerFromIndex(i) ?? parent.Items[i]) is TreeViewItem item) yield return item;
    }

    private static bool IsBranch(TreeViewItem item) => item.ItemCount > 0;

    private static ContentPresenter? HeaderPresenter(TreeViewItem item) =>
        item.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(c => c.Name == "PART_HeaderPresenter");

    private sealed record Row(Row? Parent, ItemsControl? Level, int Index, object Item, TreeViewItem? Container);

    private sealed class KidsProbe : AvaloniaObject
    {
        public static readonly StyledProperty<IEnumerable?> KidsProperty = AvaloniaProperty.Register<KidsProbe, IEnumerable?>(nameof(Kids));

        public IEnumerable? Kids => GetValue(KidsProperty);
    }

    private static bool IsOpen(TreeView tree, Row r) =>
        r.Container?.IsExpanded ?? (r.Item as TreeViewItem)?.IsExpanded
        ?? (Opened.TryGetValue(tree, out var open) && open.TryGetValue(r.Item, out var was) && was.Value);

    private static IEnumerable<Row> Kids(TreeView tree, Row? parent)
    {
        ItemsControl? level = parent is null ? tree : parent.Container ?? parent.Item as TreeViewItem;
        IReadOnlyList<object?> items = level is not null ? level.ItemsView : ChildrenOf(tree, parent!);
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is not { } item) continue;
            var c = level?.ContainerFromIndex(i) as TreeViewItem;
            if (c is { IsVisible: false })
            {
                if (!IsVirtual(level!)) continue;
                c = null;
            }
            yield return new Row(parent, level, i, item, c);
        }
    }

    private static List<Row> Rows(TreeView tree)
    {
        var rows = new List<Row>();
        void Walk(Row? parent)
        {
            foreach (var r in Kids(tree, parent))
            {
                rows.Add(r);
                if (IsOpen(tree, r)) Walk(r);
            }
        }
        Walk(null);
        return rows;
    }

    private static IReadOnlyList<object?> ChildrenOf(TreeView tree, Row parent)
    {
        if (tree.FindDataTemplate(parent.Item, parent.Level?.ItemTemplate ?? tree.ItemTemplate) is not ITreeDataTemplate template) return [];
        var probe = new KidsProbe();
        using (template.BindChildren(probe, KidsProbe.KidsProperty, parent.Item))
            return probe.Kids?.Cast<object?>().ToList() ?? [];
    }

    private static TreeViewItem? RealizeAt(ItemsControl level, int index)
    {
        if (level.ContainerFromIndex(index) is TreeViewItem { IsVisible: true } c) return c;
        level.ScrollIntoView(index);
        if (level.ContainerFromIndex(index) is TreeViewItem { IsVisible: true } d) return d;
        level.UpdateLayout();
        return level.ContainerFromIndex(index) as TreeViewItem;
    }

    private static TreeViewItem? Realize(TreeView tree, Row row)
    {
        if (row.Container is { IsVisible: true } shown) return shown;
        ItemsControl? level = row.Parent is null ? tree : Realize(tree, row.Parent);
        return level is null ? null : RealizeAt(level, row.Index);
    }

    private static int IndexOf(List<Row> rows, TreeViewItem item) =>
        rows.FindIndex(r => ReferenceEquals(r.Container, item) || ReferenceEquals(r.Item, item));

    private static string LabelOf(TreeView tree, Row r)
    {
        if (r.Container is { } c) return LabelOf(c);
        if (r.Item is TreeViewItem t) return LabelOf(t);
        if (tree.FindDataTemplate(r.Item, r.Level?.ItemTemplate ?? tree.ItemTemplate)?.Build(r.Item) is { } built)
        {
            built.DataContext = r.Item;
            var text = built as TextBlock ?? built.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault();
            if (text?.Text is { Length: > 0 } s) return s;
        }
        return r.Item.ToString() ?? "";
    }

    /// <summary>The rows showing, in order: every item whose ancestors are all open; a bound row out of view has no TreeViewItem and is left out.</summary>
    public static List<TreeViewItem> VisibleRows(TreeView tree) =>
        Rows(tree).Select(r => r.Container ?? r.Item as TreeViewItem).OfType<TreeViewItem>().ToList();

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

    private static void ExpandAll(TreeView tree, Row row)
    {
        var kids = Kids(tree, row).ToList();
        if (kids.Count == 0) return;
        if ((row.Container ?? row.Item as TreeViewItem) is { } t) t.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
        else Opened.GetOrCreateValue(tree).AddOrUpdate(row.Item, new StrongBox<bool>(true));
        foreach (var k in kids) ExpandAll(tree, k);
    }

    private static TreeViewItem? ItemFrom(object? source) =>
        (source as Visual)?.FindAncestorOfType<TreeViewItem>(includeSelf: true);

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TreeView tree || e.Handled) return;
        if ((e.KeyModifiers & (KeyModifiers.Alt | KeyModifiers.Meta)) != 0) return;
        var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        if (ctrl && e.Key is not (Key.Up or Key.Down or Key.Home or Key.End)) return;
        TreeViewItem? item;
        if (ReferenceEquals(e.Source, tree) && tree.GetValue(ParkedProperty)) item = Resume(tree);
        else
        {
            item = ItemFrom(e.Source);
            if (item is not null && !item.IsFocused && e.Source is not TreeViewItem) item = null;
        }
        if (item is null || e.Source is TextBox) return;
        var rows = Rows(tree);
        var at = IndexOf(rows, item);
        var rtl = tree.FlowDirection == FlowDirection.RightToLeft;
        Row? goRow = null;
        TreeViewItem? goItem = null;
        switch (e.Key)
        {
            case Key.Down: goRow = at + 1 < rows.Count ? rows[at + 1] : null; break;
            case Key.Up: goRow = at > 0 ? rows[at - 1] : null; break;
            case Key.Home: goRow = rows.FirstOrDefault(); break;
            case Key.End: goRow = rows.LastOrDefault(); break;
            case Key.Left or Key.Right:
                var inward = (e.Key == Key.Right) != rtl;
                if (inward)
                {
                    if (IsBranch(item))
                    {
                        if (!item.IsExpanded) item.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
                        else goRow = at >= 0 && at + 1 < rows.Count && ReferenceEquals(rows[at + 1].Parent, rows[at]) ? rows[at + 1] : null;
                    }
                }
                else if (IsBranch(item) && item.IsExpanded) item.SetCurrentValue(TreeViewItem.IsExpandedProperty, false);
                else goItem = ItemsControl.ItemsControlFromItemContainer(item) as TreeViewItem ?? item.Parent as TreeViewItem;
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
                ExpandAll(tree, at >= 0 ? rows[at] : new Row(null, null, -1, item, item));
                break;
            default:
                var ch = e.KeySymbol;
                if (ch is "*")
                {
                    ExpandAll(tree, at >= 0 ? rows[at] : new Row(null, null, -1, item, item));
                    break;
                }
                if (ch is not { Length: 1 } || char.IsWhiteSpace(ch[0]) || char.IsControl(ch[0])) return;
                goRow = TypeAhead(tree, rows, at, ch);
                if (goRow is null) return;
                break;
        }
        e.Handled = true;
        var go = goItem ?? (goRow is null ? null : Realize(tree, goRow));
        if (go is not { IsVisible: true }) return;
        FocusRow(go);
        if (!ctrl) Select(tree, go);
    }

    private static Row? TypeAhead(TreeView tree, List<Row> rows, int at, string ch)
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
            if (ArabicSearch.Normalize(LabelOf(tree, row)).StartsWith(query, StringComparison.Ordinal)) return row;
        }
        return null;
    }

    private static object[] PathOf(TreeViewItem row)
    {
        var path = new List<object>();
        for (var x = row; x is not null; x = OwnerOf(x) as TreeViewItem) path.Add(OwnerOf(x)?.ItemFromContainer(x) ?? x);
        path.Reverse();
        return path.ToArray();
    }

    private static TreeViewItem? Locate(TreeView tree, object[] path, out bool whole)
    {
        TreeViewItem? row = null;
        ItemsControl level = tree;
        var depth = 0;
        foreach (var item in path)
        {
            if (row is { IsExpanded: false }) break;
            var i = level.ItemsView.IndexOf(item);
            if (i < 0 || RealizeAt(level, i) is not { IsVisible: true } c) break;
            row = c;
            level = c;
            depth++;
        }
        whole = depth == path.Length;
        return row;
    }

    private static TreeViewItem? Resume(TreeView tree)
    {
        var row = tree.GetValue(CurrentProperty) is { } path ? Locate(tree, path, out _) : null;
        if (row is null && Rows(tree).FirstOrDefault() is { } first) row = Realize(tree, first);
        if (row is not null) FocusRow(row);
        return row;
    }

    private static void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not TreeView tree || e.Source is not TreeViewItem row || !row.IsFocused) return;
        tree.SetValue(CurrentProperty, PathOf(row));
        tree.SetValue(MethodProperty, e.NavigationMethod);
        Unpark(tree);
    }

    private static void OnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not TreeView tree) return;
        if (ReferenceEquals(e.Source, tree))
        {
            Unpark(tree);
            return;
        }
        if (e.Source is not TreeViewItem || e.NewFocusedElement is not null) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (TopLevel.GetTopLevel(tree)?.FocusManager?.GetFocusedElement() is not null) return;
            if (!tree.IsEffectivelyVisible || tree.GetValue(CurrentProperty) is null) return;
            // The window lost the keyboard to another window, not a recycled row: activation gives the row its focus back.
            if (WindowActivity.GetIsInactive(tree)) return;
            tree.SetValue(ParkedProperty, true);
            if (!tree.Focusable)
            {
                tree.SetValue(LentFocusProperty, true);
                tree.SetCurrentValue(InputElement.FocusableProperty, true);
            }
            tree.Focus(NavigationMethod.Unspecified);
        });
    }

    private static void Unpark(TreeView tree)
    {
        if (!tree.GetValue(ParkedProperty)) return;
        tree.SetValue(ParkedProperty, false);
        if (!tree.GetValue(LentFocusProperty)) return;
        tree.SetValue(LentFocusProperty, false);
        tree.ClearValue(InputElement.FocusableProperty);
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
        var chain = new List<TreeViewItem>();
        for (var p = item; p is not null; p = p.Parent as TreeViewItem) chain.Insert(0, p);
        foreach (var p in chain.Take(chain.Count - 1)) p.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
        var tree = item.FindLogicalAncestorOfType<TreeView>();
        tree?.UpdateLayout();
        foreach (var p in chain) (p.Parent as ItemsControl)?.ScrollIntoView(p);
        if (tree is not null) Select(tree, item);
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
