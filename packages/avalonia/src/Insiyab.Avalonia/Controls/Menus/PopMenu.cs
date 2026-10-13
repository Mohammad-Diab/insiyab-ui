using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-pop</c> menu: a <see cref="MenuFlyout"/> that hangs from its trigger's leading edge, 6px below it, in the window's overlay layer, and turns upward or the other way at the window's edge.</summary>
public class PopMenu : MenuFlyout
{
    /// <summary>Lines the menu up with the trigger's trailing edge instead: <c>.ins-pop--end</c>.</summary>
    public static readonly StyledProperty<bool> IsEndProperty =
        AvaloniaProperty.Register<PopMenu, bool>(nameof(IsEnd));

    public PopMenu()
    {
        Pop.Init(this);
        FlyoutPresenterClasses.Add("pop");
    }

    public bool IsEnd { get => GetValue(IsEndProperty); set => SetValue(IsEndProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEndProperty) Pop.Align(this, IsEnd);
    }
}

/// <summary>The web's <c>.ins-pop-body--card</c>: a <see cref="Flyout"/> holding free content, placed like <see cref="PopMenu"/>.</summary>
public class PopCard : Flyout
{
    /// <summary>Lines the card up with the trigger's trailing edge instead: <c>.ins-pop--end</c>.</summary>
    public static readonly StyledProperty<bool> IsEndProperty =
        AvaloniaProperty.Register<PopCard, bool>(nameof(IsEnd));

    public PopCard()
    {
        Pop.Init(this);
        FlyoutPresenterClasses.Add("pop");
        FlyoutPresenterClasses.Add("card");
        Opened += (_, _) =>
        {
            if (_hooked || Popup.Child is not Control body) return;
            body.AddHandler(Button.ClickEvent, OnItemClick);
            _hooked = true;
        };
    }

    private bool _hooked;

    // Choosing a pop item closes the card and hands focus back to its trigger, as a menu does; a toggle item stays open for the next one.
    private void OnItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button item || item is ToggleButton || !item.Classes.Contains("pop-item")) return;
        Hide();
        (Target as InputElement)?.Focus();
    }

    public bool IsEnd { get => GetValue(IsEndProperty); set => SetValue(IsEndProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEndProperty) Pop.Align(this, IsEnd);
    }
}

internal static class Pop
{
    // The gap between the trigger and the body: .375rem.
    public const double Gap = 6;

    // The clear margin a `pop` body keeps around itself for its large shadow; the offsets take it back out.
    public static readonly Thickness Room = new(40, 16, 40, 64);

    public static void Init(PopupFlyoutBase flyout)
    {
        flyout.Placement = PlacementMode.BottomEdgeAlignedLeft;
        flyout.VerticalOffset = Gap - Room.Top;
        flyout.Popup.ShouldUseOverlayLayer = true;
        flyout.Opening += (_, _) => Offset(flyout);
        flyout.Opened += (_, _) => Flip(flyout);
    }

    private static void Offset(PopupFlyoutBase flyout)
    {
        var rtl = flyout.Target?.FlowDirection == FlowDirection.RightToLeft;
        var end = flyout.Placement == PlacementMode.BottomEdgeAlignedRight;
        flyout.HorizontalOffset = rtl ^ end ? Room.Right : -Room.Left;
        flyout.VerticalOffset = Gap - Room.Top;
    }

    // Avalonia's popup positions mirror in RTL, so Left is the leading edge on either page.
    public static void Align(PopupFlyoutBase flyout, bool end) =>
        flyout.Placement = end ? PlacementMode.BottomEdgeAlignedRight : PlacementMode.BottomEdgeAlignedLeft;

    // A flip upward puts the gap on the other side.
    private static void Flip(PopupFlyoutBase flyout)
    {
        if (flyout.Popup.Child is not Control body || flyout.Target is not Control target) return;
        var top = TopLevel.GetTopLevel(target);
        var at = target.TranslatePoint(default, (Visual?)top ?? target);
        if (top is null || at is null) return;
        var below = top.Bounds.Height - (at.Value.Y + target.Bounds.Height);
        var need = body.Bounds.Height - Room.Top - Room.Bottom + Gap;
        var up = below < need && at.Value.Y > below;
        flyout.Popup.VerticalOffset = up ? Room.Bottom - Gap : Gap - Room.Top;
        PopMotion.SetFromAbove(body, !up);
    }
}
