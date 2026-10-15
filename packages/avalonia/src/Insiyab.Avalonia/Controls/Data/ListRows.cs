using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Marks the row after a hovered or selected list row with the <c>after-hot</c> class, so its divider can step aside as the web's <c>.ins-list-item:hover + .ins-list-item</c> does, and the first row with <c>:first</c>, so it has no divider above it.</summary>
public static class ListRows
{
    private static readonly ConditionalWeakTable<ListBoxItem, Follow> s_following = new();

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
            if (((ILogical)item).IsAttachedToLogicalTree) Follow.Start(item);
        }
        else
        {
            item.PropertyChanged -= OnItemChanged;
            item.AttachedToLogicalTree -= OnAttached;
            item.DetachedFromLogicalTree -= OnDetached;
            Follow.Stop(item);
        }
    }

    private static void OnAttached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (sender is ListBoxItem item) Follow.Start(item);
    }

    private static void OnDetached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (sender is ListBoxItem item) Follow.Stop(item);
    }

    private static void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        if (e.Property != InputElement.IsPointerOverProperty && e.Property != ListBoxItem.IsSelectedProperty) return;
        Refresh(item);
    }

    // Watches the list's row indexes for as long as the row is in it, and unsubscribes when it leaves: an :nth-child rule in the theme held a removed row through its list. It reads the index the way that rule does, from the event the list raises for this row.
    private sealed class Follow
    {
        private readonly ListBoxItem _item;
        private readonly IChildIndexProvider _list;

        private Follow(ListBoxItem item, IChildIndexProvider list)
        {
            _item = item;
            _list = list;
            list.ChildIndexChanged += OnChanged;
        }

        public static void Start(ListBoxItem item)
        {
            Unhook(item);
            var list = ((ILogical)item).LogicalParent as IChildIndexProvider;
            if (list is not null) s_following.AddOrUpdate(item, new Follow(item, list));
            SetFirst(item, list?.GetChildIndex(item) == 0);
        }

        public static void Stop(ListBoxItem item)
        {
            Unhook(item);
            SetFirst(item, false);
        }

        private static void Unhook(ListBoxItem item)
        {
            if (!s_following.TryGetValue(item, out var follow)) return;
            follow._list.ChildIndexChanged -= follow.OnChanged;
            s_following.Remove(item);
        }

        private static void SetFirst(ListBoxItem item, bool first) => ((IPseudoClasses)item.Classes).Set(":first", first);

        private void OnChanged(object? sender, ChildIndexChangedEventArgs e)
        {
            int index;
            if (e.Action == ChildIndexChangedAction.ChildIndexChanged)
            {
                if (e.Child != _item) return;
                index = e.Index >= 0 ? e.Index : _list.GetChildIndex(_item);
            }
            else if (e.Action == ChildIndexChangedAction.ChildIndexesReset) index = _list.GetChildIndex(_item);
            else return;
            SetFirst(_item, index == 0);
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
