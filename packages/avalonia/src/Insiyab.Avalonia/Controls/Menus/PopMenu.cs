using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-pop</c> menu: a <see cref="MenuFlyout"/> that hangs 6px below its trigger's leading edge in the window's overlay layer, slides at the window's edge, turns upward only when there is no room below, and opens beside its trigger when too tall for either side.</summary>
public class PopMenu : MenuFlyout
{
    /// <summary>Lines the menu up with the trigger's trailing edge instead: <c>.ins-pop--end</c>.</summary>
    public static readonly StyledProperty<bool> IsEndProperty =
        AvaloniaProperty.Register<PopMenu, bool>(nameof(IsEnd));

    /// <summary>The most the menu's card grows to before it scrolls; unset, as tall as the window allows.</summary>
    public static readonly StyledProperty<double> MaxCardHeightProperty =
        AvaloniaProperty.Register<PopMenu, double>(nameof(MaxCardHeight), double.NaN);

    public PopMenu()
    {
        Pop.Init(this);
        FlyoutPresenterClasses.Add("pop");
    }

    public bool IsEnd { get => GetValue(IsEndProperty); set => SetValue(IsEndProperty, value); }

    public double MaxCardHeight { get => GetValue(MaxCardHeightProperty); set => SetValue(MaxCardHeightProperty, value); }
}

/// <summary>The web's <c>.ins-pop-body--card</c>: a <see cref="Flyout"/> holding free content, placed like <see cref="PopMenu"/>.</summary>
public class PopCard : Flyout
{
    private const double CardWidth = 320;

    /// <summary>Lines the card up with the trigger's trailing edge instead: <c>.ins-pop--end</c>.</summary>
    public static readonly StyledProperty<bool> IsEndProperty =
        AvaloniaProperty.Register<PopCard, bool>(nameof(IsEnd));

    /// <summary>The most the card grows to before it scrolls; unset, as tall as the window allows.</summary>
    public static readonly StyledProperty<double> MaxCardHeightProperty =
        AvaloniaProperty.Register<PopCard, double>(nameof(MaxCardHeight), double.NaN);

    /// <summary>Makes the card as wide as its trigger, never narrower than its usual 320 nor wider than the window.</summary>
    public static readonly StyledProperty<bool> MatchTargetWidthProperty =
        AvaloniaProperty.Register<PopCard, bool>(nameof(MatchTargetWidth));

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

    public double MaxCardHeight { get => GetValue(MaxCardHeightProperty); set => SetValue(MaxCardHeightProperty, value); }

    public bool MatchTargetWidth { get => GetValue(MatchTargetWidthProperty); set => SetValue(MatchTargetWidthProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MatchTargetWidthProperty) FlyoutPresenterClasses.Set("match", change.GetNewValue<bool>());
    }

    // This opening's width: the trigger's, within the card's usual width and the window; the presenter also holds the shadow room.
    internal void FitWidth(Control body)
    {
        if (!MatchTargetWidth || Target is not Control target)
        {
            body.ClearValue(Layoutable.WidthProperty);
            return;
        }
        var room = OverlayHost.Width(target) - 2 * Pop.Edge;
        var width = Math.Max(0, Math.Min(Math.Max(CardWidth, target.Bounds.Width), room));
        body.Width = width + Pop.Room.Left + Pop.Room.Right;
    }
}

internal static class Pop
{
    // The gap between the trigger and the body: .375rem.
    public const double Gap = 6;

    // How far a body keeps inside the window's edges.
    public const double Edge = 8;

    // A submenu's card stands this far past its item.
    private const double SubGap = 4;

    // The clear margin a `pop` body keeps around itself for its large shadow; the placement takes it back out.
    public static readonly Thickness Room = new(40, 16, 40, 64);

    // Set in the templates: a submenu beside its item, a select's list under its field.
    public static readonly CustomPopupPlacementCallback Submenu = PlaceSubmenu;
    public static readonly CustomPopupPlacementCallback Select = PlaceSelect;

    // Per pop: whether this opening's entrance still waits, and whether the placement has run yet.
    private sealed class Entrance
    {
        public bool Pending;
        public bool Placed;
    }

    public static void Init(PopupFlyoutBase flyout)
    {
        var entrance = new Entrance();
        flyout.Placement = PlacementMode.Custom;
        flyout.CustomPopupPlacementCallback = place => Place(flyout, place, entrance);
        flyout.Popup.ShouldUseOverlayLayer = true;
        flyout.Opening += (_, _) => Opening(flyout, entrance);
        flyout.Opened += (_, _) =>
        {
            Opened(flyout, entrance);
            Follow(flyout);
        };
        flyout.Closed += (_, _) => CloseSubmenus(flyout);
    }

    private static bool IsEnd(PopupFlyoutBase flyout) => flyout switch
    {
        PopMenu m => m.IsEnd,
        PopCard c => c.IsEnd,
        PickerFlyout p => p.IsEnd,
        _ => false,
    };

    private static double MaxCardHeight(PopupFlyoutBase flyout) => flyout switch
    {
        PopMenu m => m.MaxCardHeight,
        PopCard c => c.MaxCardHeight,
        _ => double.NaN,
    };

    private static void Opening(PopupFlyoutBase flyout, Entrance entrance)
    {
        Offset(flyout);
        entrance.Pending = true;
        entrance.Placed = false;
        if (flyout.Popup.Child is not Control body) return;
        PopMotion.SetHeld(body, true);
        PopLook.SetCardMax(body, MaxCardHeight(flyout));
        CloseSubmenus(flyout);
        if (flyout is PopCard card) card.FitWidth(body);
    }

    // A pop opened at the pointer (a context menu) is never placed by the callback, so its entrance starts here; a placed one starts once its side is known.
    private static void Opened(PopupFlyoutBase flyout, Entrance entrance)
    {
        if (!entrance.Pending || flyout.Popup.Child is not Control body) return;
        if (!entrance.Placed && flyout.Popup.Placement == PlacementMode.Custom) return;
        entrance.Pending = false;
        PopMotion.Play(body);
    }

    // Avalonia opens a submenu a moment after the pointer rests on its item and never cancels it when the menu closes; landing on a closed menu crashed the next open, so every submenu is shut as the menu opens and closes.
    private static void CloseSubmenus(PopupFlyoutBase flyout)
    {
        if (flyout.Popup.Child is not Control body) return;
        foreach (var item in body.GetLogicalDescendants().OfType<MenuItem>())
            if (item.IsSubMenuOpen) item.SetCurrentValue(MenuItem.IsSubMenuOpenProperty, false);
    }

    // The page can turn while the body is open: the offset swaps sides, which makes the popup place itself again.
    private static void Follow(PopupFlyoutBase flyout)
    {
        if (flyout.Target is not Control target) return;
        void Turned(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != Visual.FlowDirectionProperty) return;
            Offset(flyout);
            flyout.Popup.HorizontalOffset = flyout.HorizontalOffset;
        }
        void Closed(object? sender, EventArgs e)
        {
            target.PropertyChanged -= Turned;
            flyout.Closed -= Closed;
        }
        target.PropertyChanged += Turned;
        flyout.Closed += Closed;
    }

    // Used when the pop opens at the pointer: the card's start edge on the pointer, 6px under it; the placement callback ignores it.
    private static void Offset(PopupFlyoutBase flyout)
    {
        var rtl = flyout.Target?.FlowDirection == FlowDirection.RightToLeft;
        flyout.HorizontalOffset = rtl ^ IsEnd(flyout) ? Room.Right : -Room.Left;
        flyout.VerticalOffset = Gap - Room.Top;
    }

    private static void Place(PopupFlyoutBase flyout, CustomPopupPlacement place, Entrance entrance)
    {
        if (!Fit(place, IsEnd(flyout), true, out var fromAbove)) return;
        entrance.Placed = true;
        if (flyout.Popup.Child is not Control body) return;
        PopMotion.SetFromAbove(body, fromAbove);
        if (!entrance.Pending || TopLevel.GetTopLevel(body) is null) return;
        entrance.Pending = false;
        PopMotion.Play(body);
    }

    private static void PlaceSelect(CustomPopupPlacement place) => Fit(place, false, false, out _);

    // Below the trigger when the card fits, else above, Gap from it either way, its start edge on the trigger's and sliding only as far as the window needs; too tall for both, beside the trigger (end side first), else the side with more room.
    private static bool Fit(CustomPopupPlacement place, bool end, bool beside, out bool fromAbove)
    {
        fromAbove = true;
        var box = place.PopupSize;
        if (box.Width <= 0 || box.Height <= 0 || OverlayHost.Top(place.Target) is not { } top) return false;
        var rtl = place.Target.FlowDirection == FlowDirection.RightToLeft;
        var screen = new Size(OverlayHost.Width(top), OverlayHost.Height(top));
        var at = place.AnchorRectangle;
        var lift = place.Deflate;
        var w = box.Width - lift.Left - lift.Right - Room.Left - Room.Right;
        var h = box.Height - lift.Top - lift.Bottom - Room.Top - Room.Bottom;
        var maxX = screen.Width - Edge - w;
        var maxY = screen.Height - Edge - h;
        var below = at.Bottom + Gap;
        var above = at.Top - Gap - h;
        var startX = Clamp(rtl != end ? at.Right - w : at.Left, Edge, maxX);
        double x, y;
        if (below <= maxY) (x, y) = (startX, below);
        else if (above >= Edge) (x, y, fromAbove) = (startX, above, false);
        else if (beside && Side(at, w, rtl, Gap, maxX) is { } sideX) (x, y) = (sideX, Clamp(at.Top, Edge, maxY));
        else
        {
            var up = at.Top > screen.Height - at.Bottom;
            (x, y, fromAbove) = (startX, Clamp(up ? above : below, Edge, maxY), !up);
        }
        Put(place, x, y);
        return true;
    }

    // A submenu beside its item, toward the end, else toward the start, its top on the item's.
    private static void PlaceSubmenu(CustomPopupPlacement place)
    {
        var box = place.PopupSize;
        if (box.Width <= 0 || box.Height <= 0 || OverlayHost.Top(place.Target) is not { } top) return;
        var rtl = place.Target.FlowDirection == FlowDirection.RightToLeft;
        var screen = new Size(OverlayHost.Width(top), OverlayHost.Height(top));
        var at = place.AnchorRectangle;
        var lift = place.Deflate;
        var w = box.Width - lift.Left - lift.Right - Room.Left - Room.Right;
        var h = box.Height - lift.Top - lift.Bottom - Room.Top - Room.Bottom;
        var maxX = screen.Width - Edge - w;
        var x = Side(at, w, rtl, SubGap, maxX) ?? Clamp(screen.Width - at.Right >= at.Left ? at.Right + SubGap : at.Left - SubGap - w, Edge, maxX);
        Put(place, x, Clamp(at.Top, Edge, screen.Height - Edge - h));
    }

    // Beside the anchor, its end side first, then its start side; null when neither fits.
    private static double? Side(Rect at, double w, bool rtl, double gap, double maxX)
    {
        var left = at.Left - gap - w;
        var right = at.Right + gap;
        var (first, second) = rtl ? (left, right) : (right, left);
        if (first >= Edge && first <= maxX) return first;
        if (second >= Edge && second <= maxX) return second;
        return null;
    }

    // The box goes exactly round the card at (x, y): anchored at its centre, so nothing is mirrored or adjusted.
    private static void Put(CustomPopupPlacement place, double x, double y)
    {
        var box = place.PopupSize;
        var lift = place.Deflate;
        place.AnchorRectangle = new Rect(x - Room.Left, y - Room.Top, box.Width - lift.Left - lift.Right, box.Height - lift.Top - lift.Bottom);
        place.Anchor = PopupAnchor.None;
        place.Gravity = PopupGravity.None;
        place.Offset = default;
        place.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
    }

    private static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(v, Math.Max(min, max)));
}
