using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A finger beside a mouse, in one place: which pointer each TopLevel saw last (<c>:ins-touch</c>), a tap told from a scroll and from a long press, a long press that opens a menu, a tip or a word, and a drag that keeps its finger from the scroller around it.</summary>
internal static class Touch
{
    /// <summary>The smallest box a finger is given to hit, as the platforms advise.</summary>
    public const double Target = 44;

    /// <summary>How far a finger may wander and still tap.</summary>
    public const double Slop = 10;

    // How far a finger goes before a drag picks its direction.
    private const double Decide = 3;

    internal static readonly TimeSpan TipFor = TimeSpan.FromMilliseconds(1500);

    internal sealed class State
    {
        public bool IsTouch;
        public bool Keys;        // a key went down since the last press: a focus ring may show
        public IPointer? Pointer;
        public Point Start;
        public bool Moved;
        public bool Used;
        public bool Text;
        public bool OwnsDrag;
        public bool Decided;
        public Orientation? Axis;
        public Slider? Slider;
        public double SliderValue;
        public Control? Tip;
        public DispatcherTimer? TipTimer;
    }

    private static readonly ConditionalWeakTable<TopLevel, State> s_states = new();
    private static bool _registered;

    /// <summary>A finger went down on a TopLevel; an open peek or tip that it did not land on closes.</summary>
    internal static event Action<TopLevel, PointerPressedEventArgs>? FingerDown;

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerMovedEvent.AddClassHandler<TopLevel>(OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>(OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.HoldingEvent.AddClassHandler<TopLevel>(OnHolding, RoutingStrategies.Bubble, handledEventsToo: true);
        InputElement.PointerCaptureLostEvent.AddClassHandler<Slider>(OnSliderLost, RoutingStrategies.Bubble, handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<TopLevel>((top, _) => StateOf(top).Keys = true, RoutingStrategies.Tunnel, handledEventsToo: true);
        // under a finger a focus ring shows only after a key (a sheet or a card that focuses its first control as it opens draws none); Avalonia has set the ring's pseudo-class in its own handler, which runs first
        InputElement.GotFocusEvent.AddClassHandler<Control>(OnGotFocus);
        MenuItem.IsSelectedProperty.Changed.AddClassHandler<MenuItem>(OnMenuItemSelected);
        ScrollViewer.ScrollChangedEvent.AddClassHandler<ScrollViewer>(TouchScrollbars.OnScrolled);
        Control.LoadedEvent.AddClassHandler<TopLevel>((top, _) => Show(top, StateOf(top)));
    }

    // A phone starts in touch, a desktop with its mouse; a desktop popup shares its window's state.
    internal static State StateOf(TopLevel top) => s_states.GetValue(OverlayHost.Top(top) ?? top, _ => new State { IsTouch = Os.IsAndroid });

    internal static State? StateOf(Visual? visual) => OverlayHost.Top(visual) is { } top ? StateOf(top) : null;

    /// <summary>Whether the last pointer on this visual's TopLevel was a finger.</summary>
    public static bool IsTouch(Visual? visual) => StateOf(visual)?.IsTouch == true;

    /// <summary>False for a finger's release that wandered past the slop or ended a long press that did something; true for every mouse and pen release.</summary>
    public static bool IsTap(PointerReleasedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Touch || StateOf(e.Source as Visual) is not { } st || st.Pointer != e.Pointer) return true;
        return !st.Moved && !st.Used;
    }

    /// <summary>Whether a finger's drag on a control has been given to that control rather than the scroller around it.</summary>
    public static bool OwnsDrag(PointerEventArgs e) =>
        e.Pointer.Type != PointerType.Touch || StateOf(e.Source as Visual) is { } st && st.Pointer == e.Pointer && st.OwnsDrag;

    /// <summary>Whether a move may extend a selection of text: always for a mouse, for a finger only after a long press picked a word.</summary>
    public static bool MaySelect(PointerEventArgs e) =>
        e.Pointer.Type != PointerType.Touch || StateOf(e.Source as Visual) is { } st && st.Pointer == e.Pointer && st.Text;

    private static void Show(TopLevel top, State st)
    {
        ((IPseudoClasses)top.Classes).Set(InsiyabTheme.TouchPseudoClass, st.IsTouch);
        ((IPseudoClasses)top.Classes).Set(TouchScrollbars.PseudoClass, st.IsTouch && Os.IsAndroid);
    }

    private static void OnGotFocus(Control c, FocusChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, c) || StateOf(c) is not { IsTouch: true, Keys: false }) return;
        if (c.Classes.Contains(":focus-visible")) ((IPseudoClasses)c.Classes).Set(":focus-visible", false);
    }

    // A menu that marks its first item as it opens marks it for the keyboard; under a finger with no key pressed the item is not drawn as chosen (a finger on it still shows its press).
    private static void OnMenuItemSelected(MenuItem item, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || item.IsPointerOver || StateOf(item) is not { IsTouch: true, Keys: false }) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (item.IsSelected && !item.IsPointerOver && StateOf(item) is { IsTouch: true, Keys: false })
                ((IPseudoClasses)item.Classes).Set(":selected", false);
        }, DispatcherPriority.Send);
    }

    // Tests take a TopLevel as touch before any finger came, as a phone starts.
    internal static void SetTouch(TopLevel top, bool on) => SetMode(top, StateOf(top), on);

    private static void SetMode(TopLevel top, State st, bool touch)
    {
        if (st.IsTouch == touch) return;
        st.IsTouch = touch;
        Show(OverlayHost.Top(top) ?? top, st);
    }

    private static void OnPressed(TopLevel top, PointerPressedEventArgs e)
    {
        var st = StateOf(top);
        var touch = e.Pointer.Type == PointerType.Touch;
        SetMode(top, st, touch);
        st.Keys = false;
        CloseTip(st);
        st.Pointer = null;
        st.Slider = null;
        if (!touch) return;
        st.Pointer = e.Pointer;
        st.Start = e.GetPosition(top);
        st.Moved = st.Used = st.Text = st.OwnsDrag = st.Decided = false;
        st.Axis = null;
        for (var v = e.Source as Visual; v is not null && v != top; v = v.GetVisualParent())
        {
            if (v is ScrollViewer) break;
            if (v is TextBox) break;
            if (v is Slider slider)
            {
                st.Axis = slider.Orientation;
                st.Slider = slider;
                st.SliderValue = slider.Value;
                break;
            }
            if (v is MediaBar)
            {
                st.Axis = Orientation.Horizontal;
                break;
            }
        }
        FingerDown?.Invoke(top, e);
    }

    private static void OnMoved(TopLevel top, PointerEventArgs e)
    {
        var st = StateOf(top);
        if (e.Pointer.Type != PointerType.Touch)
        {
            SetMode(top, st, false);
            return;
        }
        if (st.Pointer != e.Pointer) return;
        var d = e.GetPosition(top) - st.Start;
        var far = Math.Sqrt(d.X * d.X + d.Y * d.Y);
        if (far > Slop) st.Moved = true;
        if (st.OwnsDrag || st.Text)
        {
            e.PreventGestureRecognition();
            return;
        }
        if (st.Axis is { } axis && !st.Decided && far >= Decide)
        {
            st.Decided = true;
            var along = axis == Orientation.Horizontal ? Math.Abs(d.X) : Math.Abs(d.Y);
            var across = axis == Orientation.Horizontal ? Math.Abs(d.Y) : Math.Abs(d.X);
            if (along >= across)
            {
                st.OwnsDrag = true;
                e.PreventGestureRecognition();
                return;
            }
        }
        // A selectable text under a finger scrolls with it; only a long press starts a selection.
        if (!st.Text && InText(e.Source as Visual)) e.Handled = true;
    }

    private static void OnReleased(TopLevel top, PointerReleasedEventArgs e)
    {
        var st = StateOf(top);
        if (e.Pointer.Type != PointerType.Touch || st.Pointer != e.Pointer) return;
        st.Slider = null;
        if (st.Tip is not null) LeaveTip(st);
        // A long press that opened something is not a tap as well.
        if (st.Used && !st.Text) e.Handled = true;
    }

    // A scroller took a finger that started on a slider: the slider goes back to where it was.
    private static void OnSliderLost(Slider slider, PointerCaptureLostEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Touch || StateOf(slider) is not { } st || st.Pointer != e.Pointer || st.Slider != slider || st.OwnsDrag) return;
        slider.SetCurrentValue(RangeBase.ValueProperty, st.SliderValue);
        st.Slider = null;
    }

    private static void OnHolding(TopLevel top, HoldingRoutedEventArgs e)
    {
        if (e.HoldingState != HoldingState.Started || e.PointerType != PointerType.Touch) return;
        var st = StateOf(top);
        if (st.Pointer is null || st.Moved || st.Used) return;
        var source = e.Source as Visual;
        if (source?.FindAncestorOfType<TextBox>(includeSelf: true) is not null)
        {
            st.Used = st.Text = true;
            return;
        }
        if (source?.FindAncestorOfType<SelectableTextBlock>(includeSelf: true) is { } block)
        {
            if (top.TranslatePoint(st.Start, block) is { } at) SelectWord(block, at);
            st.Used = st.Text = true;
            return;
        }
        if (e.Handled || HasMenu(source, top))
        {
            Cancel(st);
            return;
        }
        if (TipOwner(source, top) is { } owner)
        {
            Cancel(st);
            OpenTip(st, owner);
        }
    }

    // The press under the finger lets go, so a button, row or chip does not act as the finger lifts.
    private static void Cancel(State st)
    {
        st.Used = true;
        if (st.Pointer?.Captured is not null) st.Pointer.Capture(null);
    }

    private static bool HasMenu(Visual? source, TopLevel top)
    {
        for (var v = source; v is not null && v != top; v = v.GetVisualParent())
            if (v is Control { ContextMenu: not null } or Control { ContextFlyout: not null }) return true;
        return false;
    }

    private static Control? TipOwner(Visual? source, TopLevel top)
    {
        for (var v = source; v is not null && v != top; v = v.GetVisualParent())
            if (v is Control c && ToolTip.GetTip(c) is not null && ToolTip.GetServiceEnabled(c) && c.IsEffectivelyEnabled) return c;
        return null;
    }

    private static bool InText(Visual? source) =>
        source?.FindAncestorOfType<SelectableTextBlock>(includeSelf: true) is not null && source.FindAncestorOfType<TextBox>(includeSelf: true) is null;

    private static void OpenTip(State st, Control owner)
    {
        st.Tip = owner;
        ToolTip.SetIsOpen(owner, true);
    }

    // The tip stays a moment after the finger lifts, so it can be read.
    private static void LeaveTip(State st)
    {
        st.TipTimer?.Stop();
        var timer = new DispatcherTimer { Interval = TipFor };
        timer.Tick += (_, _) => CloseTip(st);
        st.TipTimer = timer;
        timer.Start();
    }

    private static void CloseTip(State st)
    {
        st.TipTimer?.Stop();
        st.TipTimer = null;
        if (st.Tip is not { } tip) return;
        st.Tip = null;
        ToolTip.SetIsOpen(tip, false);
    }

    // The word under the finger, as a double click picks it: letters, digits and their marks.
    internal static void SelectWord(SelectableTextBlock block, Point at)
    {
        var text = (block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text) ?? "";
        if (text.Length == 0) return;
        var hit = block.TextLayout.HitTestPoint(at - new Point(block.Padding.Left, block.Padding.Top));
        var i = Math.Clamp(hit.TextPosition, 0, text.Length);
        if (i == text.Length || !IsWord(text[i])) i = Math.Max(0, i - 1);
        if (!IsWord(text[i])) return;
        var start = i;
        var end = i + 1;
        while (start > 0 && IsWord(text[start - 1])) start--;
        while (end < text.Length && IsWord(text[end])) end++;
        block.SetCurrentValue(SelectableTextBlock.SelectionStartProperty, start);
        block.SetCurrentValue(SelectableTextBlock.SelectionEndProperty, end);
    }

    private static bool IsWord(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || char.GetUnicodeCategory(c) is System.Globalization.UnicodeCategory.NonSpacingMark or System.Globalization.UnicodeCategory.SpacingCombiningMark;
}
