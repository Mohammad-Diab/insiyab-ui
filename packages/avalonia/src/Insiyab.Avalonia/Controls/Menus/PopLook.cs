using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>What a pop hands its parts: how tall its card may grow, whether a row sits in a card, and a select's room that closes it.</summary>
internal static class PopLook
{
    // The least room a card is given, in a window shorter than any menu.
    private const double MinCard = 120;

    /// <summary>On a pop: the most its card may grow, from its MaxCardHeight; NaN leaves only the window's room.</summary>
    public static readonly AttachedProperty<double> CardMaxProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("CardMax", typeof(PopLook), double.NaN);

    /// <summary>On a pop: how tall its card may be this time it shows; the card's MaxHeight is bound to it.</summary>
    public static readonly AttachedProperty<double> CardHeightProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("CardHeight", typeof(PopLook), double.PositiveInfinity);

    /// <summary>On a card part: each time it is shown, its pop's CardHeight becomes the window's room less the edges, within CardMax.</summary>
    public static readonly AttachedProperty<bool> FitsWindowProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FitsWindow", typeof(PopLook));

    /// <summary>Set by a pop card: a pop item inside it reaches out so its corners are concentric with the card's.</summary>
    public static readonly AttachedProperty<bool> InCardProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("InCard", typeof(PopLook), inherits: true);

    /// <summary>On a select's shadow room: a press there closes the list without choosing.</summary>
    public static readonly AttachedProperty<bool> RoomClosesProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("RoomCloses", typeof(PopLook));

    static PopLook()
    {
        FitsWindowProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            c.AttachedToVisualTree -= OnShown;
            if (e.GetNewValue<bool>()) c.AttachedToVisualTree += OnShown;
        });
        RoomClosesProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            c.RemoveHandler(InputElement.PointerPressedEvent, OnRoomPressed);
            if (e.GetNewValue<bool>()) c.AddHandler(InputElement.PointerPressedEvent, OnRoomPressed);
        });
    }

    public static double GetCardMax(Control c) => c.GetValue(CardMaxProperty);
    public static void SetCardMax(Control c, double value) => c.SetValue(CardMaxProperty, value);
    public static double GetCardHeight(Control c) => c.GetValue(CardHeightProperty);
    public static void SetCardHeight(Control c, double value) => c.SetValue(CardHeightProperty, value);
    public static bool GetFitsWindow(Control c) => c.GetValue(FitsWindowProperty);
    public static void SetFitsWindow(Control c, bool value) => c.SetValue(FitsWindowProperty, value);
    public static bool GetInCard(StyledElement e) => e.GetValue(InCardProperty);
    public static void SetInCard(StyledElement e, bool value) => e.SetValue(InCardProperty, value);
    public static bool GetRoomCloses(Control c) => c.GetValue(RoomClosesProperty);
    public static void SetRoomCloses(Control c, bool value) => c.SetValue(RoomClosesProperty, value);

    private static void OnShown(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control part || part.TemplatedParent is not Control owner) return;
        var room = Math.Max(MinCard, OverlayHost.Height(part) - 2 * Pop.Edge);
        var cap = owner.GetValue(CardMaxProperty);
        owner.SetValue(CardHeightProperty, double.IsNaN(cap) ? room : Math.Min(cap, room));
    }

    private static void OnRoomPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control room || e.Source != room || room.TemplatedParent is not ComboBox box) return;
        e.Handled = true;
        box.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
    }
}
