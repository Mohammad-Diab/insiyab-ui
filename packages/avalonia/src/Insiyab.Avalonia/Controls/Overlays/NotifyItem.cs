using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>One notification in a <see cref="Notify"/>, as the plugin's <c>.ins-list-item</c>: a lead (an icon chip, or an avatar's letter), a title over a line of detail, and the time at the end. <see cref="IsUnread"/> is <c>data-ins-unread</c>: a brand dot at the start edge, a faint tint and a bolder title.</summary>
[PseudoClasses(":unread", ":avatar")]
public class NotifyItem : Button
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<NotifyItem, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<string?> AvatarProperty =
        AvaloniaProperty.Register<NotifyItem, string?>(nameof(Avatar));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<NotifyItem, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<NotifyItem, string?>(nameof(Description));

    public static readonly StyledProperty<string?> EndProperty =
        AvaloniaProperty.Register<NotifyItem, string?>(nameof(End));

    public static readonly StyledProperty<bool> IsUnreadProperty =
        AvaloniaProperty.Register<NotifyItem, bool>(nameof(IsUnread));

    private const double RingRoom = 6;

    internal static readonly RoutedEvent<RoutedEventArgs> UnreadChangedEvent =
        RoutedEvent.Register<NotifyItem, RoutedEventArgs>("UnreadChanged", RoutingStrategies.Bubble);

    /// <summary>The icon chip's 24×24 stroked geometry, the web's <c>.ins-panel-ico</c>.</summary>
    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>The sender's letter in a small avatar in place of the icon, the web's <c>.ins-avatar--sm</c>.</summary>
    public string? Avatar { get => GetValue(AvatarProperty); set => SetValue(AvatarProperty, value); }

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The trailing text, usually when it arrived.</summary>
    public string? End { get => GetValue(EndProperty); set => SetValue(EndProperty, value); }

    public bool IsUnread { get => GetValue(IsUnreadProperty); set => SetValue(IsUnreadProperty, value); }

    // The focus ring is drawn outside the row, so a row reached by keyboard is scrolled in with room for it.
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
            this.BringIntoView(new Rect(Bounds.Size).Inflate(RingRoom));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsUnreadProperty)
        {
            PseudoClasses.Set(":unread", change.GetNewValue<bool>());
            RaiseEvent(new RoutedEventArgs(UnreadChangedEvent));
        }
        else if (change.Property == AvatarProperty)
        {
            PseudoClasses.Set(":avatar", change.NewValue is not null);
        }
    }
}
