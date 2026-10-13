using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>Marks the row after a hovered or selected list row with the <c>after-hot</c> class, so its divider can step aside as the web's <c>.ins-list-item:hover + .ins-list-item</c> does.</summary>
public static class ListRows
{
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
        if (e.NewValue is true) item.PropertyChanged += OnItemChanged;
        else item.PropertyChanged -= OnItemChanged;
    }

    private static void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        if (e.Property != InputElement.IsPointerOverProperty && e.Property != ListBoxItem.IsSelectedProperty) return;
        Refresh(item);
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
