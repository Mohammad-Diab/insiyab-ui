using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Keeps the focused field above the on-screen keyboard: the scroller it is in gets room at its foot for the part the keyboard covers (and the gap), and scrolls the field into what is left. Only where a keyboard can cover the view (a phone); a desktop window is never watched.</summary>
internal static class KeyboardRoom
{
    // Space kept between the field and the keyboard.
    internal const double Gap = 16;

    private sealed class State
    {
        public ScrollViewer? Scroller;
        public IDisposable? Room;
        public bool Posted;
    }

    private static readonly ConditionalWeakTable<TopLevel, State> s_states = new();
    private static bool _registered;

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        Control.LoadedEvent.AddClassHandler<TopLevel>((top, _) => Watch(top));
    }

    private static void Watch(TopLevel top)
    {
        if (s_states.TryGetValue(top, out _)) return;
        var insets = Insets.For(top);
        if (!insets.HasSource) return;
        var state = new State();
        s_states.Add(top, state);
        insets.Changed += (_, _) => Schedule(top, state);
        top.AddHandler(InputElement.GotFocusEvent, (_, _) => Schedule(top, state), RoutingStrategies.Bubble, handledEventsToo: true);
    }

    // After the layout the change brings (a dialog moving above the keyboard), so the scroller is measured where it now is.
    private static void Schedule(TopLevel top, State state)
    {
        if (state.Posted) return;
        state.Posted = true;
        Dispatcher.UIThread.Post(() =>
        {
            state.Posted = false;
            Update(top, state);
        }, DispatcherPriority.Loaded);
    }

    // Tests run it at once.
    internal static void Update(TopLevel top)
    {
        if (s_states.TryGetValue(top, out var state)) Update(top, state);
    }

    private static void Update(TopLevel top, State state)
    {
        var insets = Insets.For(top);
        var field = insets.KeyboardOpen ? top.FocusManager?.GetFocusedElement() as Control : null;
        var scroller = field is null ? null : ScrollerOf(field);
        if (state.Scroller != scroller) Release(state);
        if (scroller is null || field is null || scroller.TranslatePoint(default, top) is not { } at) return;
        var covered = at.Y + scroller.Bounds.Height - insets.Keyboard.Top;
        if (covered <= 0)
        {
            Release(state);
            return;
        }
        state.Scroller = scroller;
        var own = scroller.Padding;
        if (state.Room is not null)
        {
            state.Room.Dispose();
            own = scroller.Padding;
        }
        state.Room = scroller.SetValue(TemplatedControl.PaddingProperty, own + new Thickness(0, 0, 0, covered + Gap), BindingPriority.Animation);
        Dispatcher.UIThread.Post(() => Reveal(top, scroller, field, insets.Keyboard.Top), DispatcherPriority.Loaded);
    }

    // Scrolls so the field sits Gap above the keyboard, or its top at the scroller's top when it is taller than what is left.
    private static void Reveal(TopLevel top, ScrollViewer scroller, Control field, double keyboardTop)
    {
        if (field.TranslatePoint(default, top) is not { } f || scroller.TranslatePoint(default, top) is not { } at) return;
        var visibleTop = at.Y;
        var visibleBottom = Math.Min(at.Y + scroller.Viewport.Height, keyboardTop) - Gap;
        var shift = 0d;
        if (f.Y + field.Bounds.Height > visibleBottom) shift = f.Y + field.Bounds.Height - visibleBottom;
        if (f.Y - shift < visibleTop) shift = f.Y - visibleTop;
        if (Math.Abs(shift) < 0.5) return;
        var offset = scroller.Offset;
        scroller.Offset = new Vector(offset.X, Math.Max(0, offset.Y + shift));
    }

    private static void Release(State state)
    {
        state.Room?.Dispose();
        state.Room = null;
        state.Scroller = null;
    }

    // The nearest scroller around the field that scrolls up and down.
    private static ScrollViewer? ScrollerOf(Control field)
    {
        foreach (var scroller in field.GetVisualAncestors().OfType<ScrollViewer>())
            if (scroller.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled) return scroller;
        return null;
    }
}
