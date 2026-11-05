using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>Where a row stands in time: <c>Past</c> is quieter, <c>Current</c> wears the brand's start mark.</summary>
public enum ListRowState
{
    None,
    Past,
    Current,
}

/// <summary>What a <c>ListBoxItem</c> is in its list: a group heading (<c>IsHeading</c>: the eyebrow look, never selected or focused, skipped by the arrow keys, no divider before the row after it) or a row's place in time (<c>State</c>). Set it on the item, or from data through an <c>ItemContainerTheme</c> setter.</summary>
public static class ListRow
{
    public static readonly AttachedProperty<bool> IsHeadingProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, bool>("IsHeading", typeof(ListRow));

    public static readonly AttachedProperty<ListRowState> StateProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, ListRowState>("State", typeof(ListRow));

    static ListRow()
    {
        IsHeadingProperty.Changed.AddClassHandler<ListBoxItem>((item, e) => ListRows.HeadingChanged(item, e.GetNewValue<bool>()));
    }

    public static bool GetIsHeading(ListBoxItem item) => item.GetValue(IsHeadingProperty);
    public static void SetIsHeading(ListBoxItem item, bool value) => item.SetValue(IsHeadingProperty, value);
    public static ListRowState GetState(ListBoxItem item) => item.GetValue(StateProperty);
    public static void SetState(ListBoxItem item, ListRowState value) => item.SetValue(StateProperty, value);
}
