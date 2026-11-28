using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A scrolling list inside a card (a list in a panel's body, a sheet's body): with <c>Card</c> on, its scroller reaches the card's own edge on the side of its bar (the end side), so the bar runs along the card's edge, and the room the card's padding and the parts between gave it there goes inside the scrolled content instead (the scroller's own <c>Padding</c>), so the content stays where it was. The card is the nearest ancestor that draws a surface: a <see cref="Border"/> with a ground of its own (a colour that is not see-through). Kept up to date as the paddings change (a finger's sizes, a dense look).</summary>
public static class ScrollEdge
{
    /// <summary>The scroller's bar runs along its card's own edge. Off by default.</summary>
    public static readonly AttachedProperty<bool> CardProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("Card", typeof(ScrollEdge));

    private sealed class State
    {
        public double Room; // what is applied now: the margin's end out by it, the padding's end in by it
        public EventHandler? Layout;
        public EventHandler<VisualTreeAttachmentEventArgs>? Attached;
    }

    private static readonly ConditionalWeakTable<ScrollViewer, State> s_states = new();

    static ScrollEdge()
    {
        CardProperty.Changed.AddClassHandler<ScrollViewer>((s, e) => Hook(s, e.GetNewValue<bool>()));
    }

    public static bool GetCard(ScrollViewer element) => element.GetValue(CardProperty);

    public static void SetCard(ScrollViewer element, bool value) => element.SetValue(CardProperty, value);

    private static void Hook(ScrollViewer scroller, bool on)
    {
        if (on)
        {
            var state = s_states.GetValue(scroller, _ => new State());
            if (state.Layout is not null)
                return;
            state.Layout = (_, _) => Apply(scroller, state, RoomOf(scroller));
            state.Attached = (_, _) => Apply(scroller, state, RoomOf(scroller));
            scroller.LayoutUpdated += state.Layout;
            scroller.AttachedToVisualTree += state.Attached;
            Apply(scroller, state, RoomOf(scroller));
        }
        else if (s_states.TryGetValue(scroller, out var state))
        {
            scroller.LayoutUpdated -= state.Layout;
            scroller.AttachedToVisualTree -= state.Attached;
            Apply(scroller, state, 0);
            s_states.Remove(scroller);
        }
    }

    private static void Apply(ScrollViewer scroller, State state, double room)
    {
        if (Math.Abs(room - state.Room) < 0.01)
            return;
        var shift = room - state.Room;
        state.Room = room;
        var margin = scroller.Margin;
        var padding = scroller.Padding;
        scroller.Margin = new Thickness(margin.Left, margin.Top, margin.Right - shift, margin.Bottom);
        scroller.Padding = new Thickness(padding.Left, padding.Top, padding.Right + shift, padding.Bottom);
    }

    /// <summary>The room between the scroller's end edge and its card's inner edge that the layout's paddings and margins give: each part's between them, and the card's own padding. None when no card holds it, or when another scroller stands between (its content scrolls; its edge is not the card's).</summary>
    internal static double RoomOf(ScrollViewer scroller)
    {
        double room = 0;
        foreach (var a in scroller.GetVisualAncestors())
        {
            if (a is TopLevel || a is ScrollViewer || a is ScrollContentPresenter)
                return 0;
            var end = a is Layoutable l && l.FlowDirection != scroller.FlowDirection; // a part laid out the other way: its end is its Left
            if (IsSurface(a))
                return room + End(PaddingOf(a), end);
            if (a is Layoutable layoutable)
                room += End(layoutable.Margin, end) + End(PaddingOf(a), end);
        }
        return 0;
    }

    private static double End(Thickness t, bool mirrored) => mirrored ? t.Left : t.Right;

    private static Thickness PaddingOf(Visual v) => v switch
    {
        Decorator d => d.Padding,
        TemplatedControl t => t.Padding,
        ContentPresenter p => p.Padding,
        _ => default,
    };

    /// <summary>A card: a border with a ground of its own (a colour that is not see-through).</summary>
    internal static bool IsSurface(Visual v) => v is Border { Background: { } ground } && !IsClear(ground);

    private static bool IsClear(IBrush brush) => brush.Opacity <= 0 || brush is ISolidColorBrush { Color.A: 0 };
}
