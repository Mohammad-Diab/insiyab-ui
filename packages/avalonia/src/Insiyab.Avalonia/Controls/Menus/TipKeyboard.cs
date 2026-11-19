using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>What the web's tooltip does for the keyboard and Avalonia's does not: a control reached by Tab or the arrow keys shows its tip at once, losing focus hides it, and Escape hides it. Registered once, when the theme loads, so the very first tip already answers the keyboard.</summary>
internal static class TipKeyboard
{
    /// <summary>Set by the ToolTip theme; reading it is what registers the handlers.</summary>
    public static readonly AttachedProperty<bool> RegisteredProperty =
        AvaloniaProperty.RegisterAttached<ToolTip, bool>("Registered", typeof(TipKeyboard));

    public static bool GetRegistered(ToolTip t) => t.GetValue(RegisteredProperty);
    public static void SetRegistered(ToolTip t, bool value) => t.SetValue(RegisteredProperty, value);

    // Avalonia's own direct children of Control and the library's: every control below them takes the tip defaults from them.
    private static readonly Type[] Bases =
    [
        typeof(TemplatedControl), typeof(Panel), typeof(Decorator), typeof(TextBlock), typeof(Shape), typeof(Image), typeof(Viewbox),
        typeof(ContentPresenter), typeof(ItemsPresenter), typeof(TextPresenter), typeof(Popup), typeof(Track), typeof(TickBar), typeof(NativeControlHost),
        typeof(Icon), typeof(ChartPlot), typeof(DrumColumn), typeof(Shimmer), typeof(LoadingSpinner), typeof(ColorArea), typeof(ColorHueSlider), typeof(FrostGround),
    ];

    static TipKeyboard()
    {
        InputElement.GotFocusEvent.AddClassHandler<Control>(OnGotFocus);
        InputElement.LostFocusEvent.AddClassHandler<Control>(OnLostFocus);
        InputElement.KeyDownEvent.AddClassHandler<Control>(OnKeyDown, RoutingStrategies.Tunnel);
        ToolTip.ToolTipOpeningEvent.AddClassHandler<Control>(OnOpening);
        foreach (var type in Bases) Defaults(type);
        ToolTip.TipProperty.Changed.AddClassHandler<Control>((c, _) => Defaults(c.GetType()));
    }

    // Above the control and centred, not at the pointer; 400ms on hover, at once from one tip to the next; in the window's own layer, so its shadow is never cut. Defaults, not a style that every control would carry.
    private static void Defaults(Type type)
    {
        Default(ToolTip.PlacementProperty, type, PlacementMode.Top);
        Default(ToolTip.VerticalOffsetProperty, type, 0d);
        Default(ToolTip.ShowDelayProperty, type, 400);
        Default(ToolTip.BetweenShowDelayProperty, type, 0);
        Default(ToolTip.ShouldUseOverlayLayerProperty, type, true);
    }

    // Only a type that still reads Avalonia's default, kept on Control itself (which cannot take a second one); a type with a default of its own keeps it.
    private static void Default<T>(StyledProperty<T> property, Type type, T value)
    {
        if (type != typeof(Control) && ReferenceEquals(property.GetMetadata(type), property.GetMetadata(typeof(Control))))
            property.OverrideDefaultValue(type, value);
    }

    // Touching the class runs its static constructor, which adds the handlers.
    internal static void Register() { }

    private static void OnGotFocus(Control c, FocusChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, c) || ToolTip.GetTip(c) is null) return;
        // a control focused as a sheet or a card opens, under a finger with no key pressed, is not the keyboard's reach: no tip
        if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional && Touch.StateOf(c) is not { IsTouch: true, Keys: false })
        {
            s_keyboardTip = new WeakReference<Control>(c);
            ToolTip.SetIsOpen(c, true);
        }
    }

    private static WeakReference<Control>? s_keyboardTip; // the control the keyboard reached, whose tip it opened

    // Under a finger a tip opens only by a long press on its control (Touch) or from the keyboard: a tap leaves no hover behind, so a tip it started would show far from the control and never close. A mouse (DeX, a desktop) keeps its hover tips.
    private static void OnOpening(Control c, CancelRoutedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, c) || Touch.StateOf(c) is not { IsTouch: true } st) return;
        if (ReferenceEquals(st.Tip, c) || st.Keys || s_keyboardTip is { } k && k.TryGetTarget(out var reached) && ReferenceEquals(reached, c)) return;
        e.Cancel = true;
    }

    private static void OnLostFocus(Control c, FocusChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, c) && ToolTip.GetIsOpen(c)) ToolTip.SetIsOpen(c, false);
    }

    private static void OnKeyDown(Control c, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !ToolTip.GetIsOpen(c) || !ReferenceEquals(e.Source, c)) return;
        ToolTip.SetIsOpen(c, false);
        e.Handled = true;
    }
}
