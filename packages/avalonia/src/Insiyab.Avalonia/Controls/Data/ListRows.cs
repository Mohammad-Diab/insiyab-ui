using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Marks the row after a hovered or selected list row with the <c>after-hot</c> class, so its divider can step aside as the web's <c>.ins-list-item:hover + .ins-list-item</c> does, and the first row with <c>:first</c>, so it has no divider above it.</summary>
internal static class ListRows
{
    private static readonly ConditionalWeakTable<IChildIndexProvider, Tracker> s_lists = new();
    private static readonly ConditionalWeakTable<ListBoxItem, Tracker> s_rows = new();

    /// <summary>Set by the ListBoxItem theme; turns the tracking on for that row.</summary>
    public static readonly AttachedProperty<bool> TrackProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, bool>("Track", typeof(ListRows));

    static ListRows()
    {
        TrackProperty.Changed.AddClassHandler<ListBoxItem>(OnTrackChanged);
    }

    public static bool GetTrack(ListBoxItem item) => item.GetValue(TrackProperty);

    public static void SetTrack(ListBoxItem item, bool value) => item.SetValue(TrackProperty, value);

    private static void OnTrackChanged(ListBoxItem item, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            item.PropertyChanged += OnItemChanged;
            item.AttachedToLogicalTree += OnAttached;
            item.DetachedFromLogicalTree += OnDetached;
            if (((ILogical)item).IsAttachedToLogicalTree) Tracker.Join(item);
        }
        else
        {
            item.PropertyChanged -= OnItemChanged;
            item.AttachedToLogicalTree -= OnAttached;
            item.DetachedFromLogicalTree -= OnDetached;
            Tracker.Leave(item);
        }
    }

    private static void OnAttached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (sender is ListBoxItem item) Tracker.Join(item);
    }

    private static void OnDetached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (sender is ListBoxItem item) Tracker.Leave(item);
    }

    private static void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        if (e.Property != InputElement.IsPointerOverProperty && e.Property != ListBoxItem.IsSelectedProperty) return;
        Refresh(item);
    }

    // One watcher per list follows the indexes of all its tracked rows, so a shift costs one handler call, not one per row; a row leaves it when it leaves the list, which an :nth-child rule never did.
    private sealed class Tracker
    {
        private readonly IChildIndexProvider _list;
        private readonly HashSet<ListBoxItem> _rows = [];

        private Tracker(IChildIndexProvider list)
        {
            _list = list;
            list.ChildIndexChanged += OnChanged;
        }

        public static void Join(ListBoxItem item)
        {
            Leave(item);
            if (((ILogical)item).LogicalParent is not IChildIndexProvider list) return;
            if (!s_lists.TryGetValue(list, out var tracker))
            {
                tracker = new Tracker(list);
                s_lists.Add(list, tracker);
            }
            tracker._rows.Add(item);
            s_rows.AddOrUpdate(item, tracker);
            SetFirst(item, list.GetChildIndex(item) == 0);
        }

        public static void Leave(ListBoxItem item)
        {
            SetFirst(item, false);
            if (!s_rows.TryGetValue(item, out var tracker)) return;
            s_rows.Remove(item);
            tracker._rows.Remove(item);
            if (tracker._rows.Count > 0) return;
            tracker._list.ChildIndexChanged -= tracker.OnChanged;
            s_lists.Remove(tracker._list);
        }

        private static void SetFirst(ListBoxItem item, bool first) => ((IPseudoClasses)item.Classes).Set(":first", first);

        private void OnChanged(object? sender, ChildIndexChangedEventArgs e)
        {
            if (e.Action == ChildIndexChangedAction.ChildIndexChanged)
            {
                if (e.Child is ListBoxItem item && _rows.Contains(item))
                    SetFirst(item, (e.Index >= 0 ? e.Index : _list.GetChildIndex(item)) == 0);
            }
            else if (e.Action == ChildIndexChangedAction.ChildIndexesReset)
            {
                foreach (var row in _rows) SetFirst(row, _list.GetChildIndex(row) == 0);
            }
        }
    }

    internal static void Refresh(ListBoxItem item)
    {
        if (ItemsControl.ItemsControlFromItemContainer(item) is not { } owner) return;
        var index = owner.IndexFromContainer(item);
        if (index < 0) return;
        if (owner.ContainerFromIndex(index + 1) is { } next)
            next.Classes.Set("after-hot", item.IsPointerOver || item.IsSelected);
    }
}
