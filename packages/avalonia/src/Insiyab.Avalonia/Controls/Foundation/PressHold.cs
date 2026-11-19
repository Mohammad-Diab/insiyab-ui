using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Effects off (<see cref="Motion.IsStill"/>) drops a press's movement, never its feedback, as Android, iOS and the web's reduced motion do. A tap's pressed look lasts only while the finger is down, and a quick tap's press and release often arrive before the next frame is drawn, so with the movement gone (the scale and the quick-click dip) a tap showed nothing. While a window is still, every element that was <c>:pressed</c> by a tap or a click keeps <c>:pressed</c> for <see cref="Hold"/> after the release, so its pressed colour (its state layer, <c>Themes/Foundation/Still.axaml</c> for the kinds whose pressed look was only movement) is drawn and then fades with its colour transition. A press that slid away (a scroll, a drag) or let go outside the element is not held. Effects on are left as they are (the dip is their quick-tap feedback), except for the kinds that had no press sign at all, with or without effects (a bare or link button, a chip, a list row, a menu row: <see cref="AlwaysProperty"/>, set by <c>Themes/Foundation/Press.axaml</c>): their press is held in every window.</summary>
internal static class PressHold
{
    private const string Pressed = ":pressed";

    /// <summary>How long a released press keeps its pressed look while the window is still.</summary>
    internal static TimeSpan Hold { get; set; } = TimeSpan.FromMilliseconds(150);

    // Beyond this many pixels between the press and the release the press was a scroll or a drag (Android's touch slop, 8dp, and a little).
    private const double Slop = 10;

    private sealed record Down(List<StyledElement> Pressed, Point At, long Since);

    private sealed class Token;

    /// <summary>On a pressable kind whose press shows nothing else (no dip, no movement): its press is held whatever the effects.</summary>
    public static readonly AttachedProperty<bool> AlwaysProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Always", typeof(PressHold));

    public static bool GetAlways(StyledElement element) => element.GetValue(AlwaysProperty);

    public static void SetAlways(StyledElement element, bool value) => element.SetValue(AlwaysProperty, value);

    private static readonly Dictionary<IPointer, Down> s_down = [];
    private static readonly ConditionalWeakTable<StyledElement, Token> s_held = new();
    private static bool _registered;

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        // At the window, after every element on the way has taken the press or the release (its own :pressed already set or cleared).
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnPressed, RoutingStrategies.Bubble, true);
        InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>(OnReleased, RoutingStrategies.Bubble, true);
    }

    /// <summary>Whether <paramref name="element"/> shows a press that has already been let go.</summary>
    internal static bool IsHeld(StyledElement element) => s_held.TryGetValue(element, out _);

    private static void OnPressed(TopLevel top, PointerPressedEventArgs e)
    {
        s_down.Remove(e.Pointer);
        // a new finger: an earlier touch still listed as down never let go where the window heard it (its view went away under it), and it kept every element it had pressed from ever letting go of its look (several sidebar links stayed grey); a touch is a new pointer each time, so it is let go now
        if (e.Pointer.Type == PointerType.Touch)
            foreach (var (pointer, old) in s_down.Where(p => p.Key.Type == PointerType.Touch).ToList())
            {
                s_down.Remove(pointer);
                LetGo(old.Pressed);
            }
        if (e.Source is not Visual source) return;
        var still = Motion.IsStill(top);
        var pressed = new List<StyledElement>();
        foreach (var v in source.GetSelfAndVisualAncestors())
        {
            if (ReferenceEquals(v, top)) break;
            if (v is StyledElement element && element.Classes.Contains(Pressed) && (still || GetAlways(element))) pressed.Add(element);
        }
        if (pressed.Count > 0) s_down[e.Pointer] = new Down(pressed, e.GetPosition(top), Environment.TickCount64);
        Forget();
    }

    private static void OnReleased(TopLevel top, PointerReleasedEventArgs e)
    {
        if (!s_down.Remove(e.Pointer, out var down)) return;
        var slid = Point.Distance(down.At, e.GetPosition(top)) > Slop;
        var inside = slid ? [] : down.Pressed.Where(element => element is Visual visual && visual.IsAttachedToVisualTree() && new Rect(visual.Bounds.Size).Contains(e.GetPosition(visual))).ToList();
        // Some kinds let go of their press after the release has passed (a list row clears it once its selection is made), so the hold is decided right after the release's own work, before the next frame. An element still showing the press then never saw its release (a tap that switched the page under a touch: its row was laid out again; a press a scroller took over): with the finger up nothing may stay pressed, so it is held as any other and then let go, or let go at once when the press slid away or ended outside it.
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var element in down.Pressed)
            {
                if (!((Visual)element).IsAttachedToVisualTree())
                    continue;
                if (inside.Contains(element))
                    Show(element);
                else if (element.Classes.Contains(Pressed) && !IsHeld(element) && !s_down.Values.Any(d => d.Pressed.Contains(element)))
                    ((IPseudoClasses)element.Classes).Set(Pressed, false);
            }
        }, DispatcherPriority.Send);
    }

    // a pointer whose release never reached the window (its view went away under it) would keep its elements "pressed" for good: after half a minute its entry goes, and the look it left with it (a touch goes at the next touch already: OnPressed)
    private static void Forget()
    {
        var now = Environment.TickCount64;
        foreach (var (pointer, old) in s_down.Where(p => now - p.Value.Since > 30_000).ToList())
        {
            s_down.Remove(pointer);
            LetGo(old.Pressed);
        }
    }

    // the looks of a press whose release never came: gone, unless a hold or another press still owns them
    private static void LetGo(List<StyledElement> pressed)
    {
        foreach (var element in pressed)
            if (element.Classes.Contains(Pressed) && !IsHeld(element) && !s_down.Values.Any(d => d.Pressed.Contains(element)))
                ((IPseudoClasses)element.Classes).Set(Pressed, false);
    }

    private static void Show(StyledElement element)
    {
        var token = new Token();
        s_held.AddOrUpdate(element, token);
        ((IPseudoClasses)element.Classes).Set(Pressed, true);
        DispatcherTimer.RunOnce(() => Release(element, token), Hold);
    }

    private static void Release(StyledElement element, Token token)
    {
        if (!s_held.TryGetValue(element, out var now) || !ReferenceEquals(now, token)) return;
        s_held.Remove(element);
        // pressed again meanwhile: the new press owns the look now (a button that missed its own release still says it is pressed: its look goes all the same)
        if (s_down.Values.Any(d => d.Pressed.Contains(element))) return;
        ((IPseudoClasses)element.Classes).Set(Pressed, false);
    }
}
