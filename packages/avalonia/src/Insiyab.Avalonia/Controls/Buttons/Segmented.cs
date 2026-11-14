using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web's <c>.ins-seg</c>: one choice out of a few short ones, a raised face in a recessed track that flies to the segment you pick. Items are <see cref="SegmentedItem"/>s (or any content, wrapped in one); <c>SelectionChanged</c> is the web's <c>ins:seg</c>. Narrower than its items, the row scrolls sideways and keeps the chosen one in view; the class <c>chips</c> draws it as a row of filter chips.</summary>
[TemplatePart("PART_Flight", typeof(Border))]
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[PseudoClasses(":overflow")]
public class Segmented : SelectingItemsControl
{
    private static readonly FuncTemplate<Panel?> DefaultPanel =
        new(() => new SegmentedPanel { Orientation = Orientation.Horizontal, Spacing = 3 });

    // How much of the neighbour beyond the chosen item stays in view, so the row reads as one that goes on.
    internal const double Peek = 24;

    private Border? _flight;
    private ScrollViewer? _scroll;
    private FrameTween? _slide;
    private Drag? _drag;

    public Segmented()
    {
        AddHandler(PointerPressedEvent, OnDragPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnDragMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnDragReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, OnDragLost, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    static Segmented()
    {
        SelectionModeProperty.OverrideDefaultValue<Segmented>(SelectionMode.AlwaysSelected);
        FocusableProperty.OverrideDefaultValue(typeof(Segmented), false);
        ItemsPanelProperty.OverrideDefaultValue<Segmented>(DefaultPanel);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SegmentedItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SegmentedItem>(item, out recycleKey);

    // Segments given inline are their own containers and keep the IsSelected of their last showing; prepared again (a card that reopens) Avalonia reads that as a toggle, which drops the chosen one and falls back to the first.
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (container == item && container.IsSet(IsSelectedProperty) && container.GetValue(IsSelectedProperty) == Selection.IsSelected(index))
            container.ClearValue(IsSelectedProperty);
        base.PrepareContainerForItemOverride(container, item, index);
    }

    // A finger chooses only by a tap: one that dragged the row, wandered or held chooses nothing.
    protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs e) =>
        e.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased && base.ShouldTriggerSelection(selectable, e)
        && (e is not PointerReleasedEventArgs released || Touch.IsTap(released));

    public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
    {
        if (eventArgs is FocusChangedEventArgs { NavigationMethod: not NavigationMethod.Directional }) return false;
        return base.UpdateSelectionFromEvent(container, eventArgs);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _flight = e.NameScope.Find<Border>("PART_Flight");
        if (_scroll is not null) _scroll.ScrollChanged -= OnScrollChanged;
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        if (_scroll is not null) _scroll.ScrollChanged += OnScrollChanged;
    }

    internal ScrollViewer? Scroller => _scroll;

    // Only a row that does not fit clips and scrolls; one that fits draws its shadows and rings past its edge as before.
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scroll is not { } scroll) return;
        var overflow = scroll.Extent.Width > scroll.Viewport.Width + 0.5;
        PseudoClasses.Set(":overflow", overflow);
        if (scroll.Presenter is { } presenter) presenter.ClipToBounds = overflow;
        if (e.ExtentDelta.X != 0 || e.ViewportDelta.X != 0) Reveal(false);
    }

    // Scrolls the chosen item into view with a little of the next one past it; eased when the choice moved, at once when the row changed size.
    private void Reveal(bool ease)
    {
        if (_scroll is not { } scroll || scroll.Extent.Width <= scroll.Viewport.Width + 0.5) return;
        if (ContainerFromIndex(SelectedIndex) is not Control item || scroll.Content is not Visual content || item.TranslatePoint(default, content) is not { } at) return;
        var view = scroll.Viewport.Width;
        var room = Math.Max(0, Math.Min(Peek, (view - item.Bounds.Width) / 2));
        var left = at.X + scroll.Padding.Left;
        var from = scroll.Offset.X;
        var to = from;
        if (left - room < from) to = left - room;
        else if (left + item.Bounds.Width + room > from + view) to = left + item.Bounds.Width + room - view;
        to = Math.Clamp(to, 0, Math.Max(0, scroll.Extent.Width - view));
        _slide?.Stop();
        _slide = null;
        if (Math.Abs(to - from) < 0.5) return;
        if (!ease)
        {
            scroll.Offset = new Vector(to, scroll.Offset.Y);
            return;
        }
        _slide = FrameTween.Run(this, Motion.Enter(this), Motion.Ease(this), t => scroll.Offset = new Vector(from + (to - from) * t, scroll.Offset.Y));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty)
        {
            Fly(change.GetOldValue<int>(), change.GetNewValue<int>());
            Dispatcher.UIThread.Post(() => Reveal(true), DispatcherPriority.Loaded);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SegmentedAutomationPeer(this);

    // A finger drags a row that does not fit sideways, the same way in either direction; Avalonia's own scroller refuses a right-to-left row at its ends.
    private sealed class Drag(IPointer pointer, Point start, double offset, ulong time)
    {
        public IPointer Pointer { get; } = pointer;
        public Point Start { get; } = start;
        public double Offset { get; } = offset;
        public bool Dragging { get; set; }
        public double LastX { get; set; } = start.X;
        public ulong LastTime { get; set; } = time;
        public double Velocity { get; set; }
    }

    private void OnDragPressed(object? sender, PointerPressedEventArgs e)
    {
        _drag = null;
        if (e.Pointer.Type != PointerType.Touch || _scroll is not { } scroll || scroll.Extent.Width <= scroll.Viewport.Width + 0.5) return;
        _slide?.Stop();
        _slide = null;
        _drag = new Drag(e.Pointer, e.GetPosition(scroll), scroll.Offset.X, e.Timestamp);
    }

    private void OnDragMoved(object? sender, PointerEventArgs e)
    {
        if (_drag is not { } d || d.Pointer != e.Pointer || _scroll is not { } scroll) return;
        var at = e.GetPosition(scroll);
        var move = at - d.Start;
        if (!d.Dragging)
        {
            if (Math.Max(Math.Abs(move.X), Math.Abs(move.Y)) < 4) return;
            if (Math.Abs(move.X) <= Math.Abs(move.Y))
            {
                _drag = null;
                return;
            }
            d.Dragging = true;
            e.Pointer.Capture(this);
        }
        e.PreventGestureRecognition();
        e.Handled = true;
        var dt = e.Timestamp > d.LastTime ? (e.Timestamp - d.LastTime) / 1000.0 : 0;
        if (dt > 0) d.Velocity = (at.X - d.LastX) / dt;
        d.LastX = at.X;
        d.LastTime = e.Timestamp;
        scroll.Offset = new Vector(Math.Clamp(d.Offset - move.X, 0, Math.Max(0, scroll.Extent.Width - scroll.Viewport.Width)), scroll.Offset.Y);
    }

    private void OnDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_drag is not { Dragging: true } d || d.Pointer != e.Pointer) return;
        _drag = null;
        e.Handled = true;
        e.Pointer.Capture(null);
        Fling(d.Velocity);
    }

    private void OnDragLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_drag is { } d && d.Pointer == e.Pointer && d.Dragging) _drag = null;
    }

    // A flick carries the row on a little and eases to rest; a still row stops where the finger left it.
    private void Fling(double velocity)
    {
        if (_scroll is not { } scroll || Math.Abs(velocity) < 200 || Motion.IsStill(this)) return;
        var from = scroll.Offset.X;
        var to = Math.Clamp(from - velocity * 0.25, 0, Math.Max(0, scroll.Extent.Width - scroll.Viewport.Width));
        if (Math.Abs(to - from) < 0.5) return;
        _slide = FrameTween.Run(this, TimeSpan.FromMilliseconds(450), Motion.Ease(this), t => scroll.Offset = new Vector(from + (to - from) * t, scroll.Offset.Y));
    }

    private void Fly(int from, int to)
    {
        // Chips keep their own faces and change colour in place, as a face flying behind solid chips would show only in the gaps.
        if (Classes.Contains("chips")) return;
        if (_flight?.Parent is not Visual layer || from < 0 || to < 0 || from == to || !IsEffectivelyVisible) return;
        if (ContainerFromIndex(from) is not Control a || ContainerFromIndex(to) is not Control b) return;
        if (a.TranslatePoint(default, layer) is not Point pa || b.TranslatePoint(default, layer) is not Point pb || b.Bounds.Width <= 0) return;
        var start = (pa.X, a.Bounds.Width);
        if (_run is { IsDone: false } previous)
        {
            start = (previous.Start, previous.Length);
            previous.Land();
        }
        MarkerFlight? run = null;
        run = MarkerFlight.Run(this, start, (pb.X, b.Bounds.Width), Place, () => Land(run));
        if (run is null) return;
        _run = run;
        Canvas.SetTop(_flight, pb.Y);
        _flight.Height = b.Bounds.Height;
        _flight.IsVisible = true;
        Classes.Add("moving");
    }

    private MarkerFlight? _run;

    private void Place(double x, double width)
    {
        Canvas.SetLeft(_flight!, x);
        _flight!.Width = Math.Max(0, width);
    }

    private void Land(MarkerFlight? run)
    {
        if (run is null || _run != run) return;
        _run = null;
        _flight!.IsVisible = false;
        Classes.Remove("moving");
    }
}

/// <summary>The row of a <see cref="Segmented"/>: a horizontal StackPanel whose Left and Right keys follow the row as it is drawn, mirrored in RTL.</summary>
internal sealed class SegmentedPanel : StackPanel, INavigableContainer
{
    protected override Type StyleKeyOverride => typeof(StackPanel);

    IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var shown = Children.Where(c => c.IsVisible && c.IsEffectivelyEnabled).ToList();
        if (shown.Count == 0) return null;
        if (direction == NavigationDirection.First) return shown[0];
        if (direction == NavigationDirection.Last) return shown[^1];
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var step = direction switch
        {
            NavigationDirection.Left => rtl ? 1 : -1,
            NavigationDirection.Right => rtl ? -1 : 1,
            NavigationDirection.Next => 1,
            NavigationDirection.Previous => -1,
            _ => 0,
        };
        var at = from is Control c ? shown.IndexOf(c) : -1;
        if (step == 0 || at < 0) return null;
        var to = at + step;
        if (to < 0 || to >= shown.Count) return wrap ? shown[(to + shown.Count) % shown.Count] : null;
        return shown[to];
    }
}

// Screen readers hear one choice in a list: the items, which one is chosen, and that one always is.
internal sealed class SegmentedAutomationPeer(Segmented owner) : SelectingItemsControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
}

/// <summary>One segment of a <see cref="Segmented"/>; <c>:selected</c> is the web's <c>is-active</c>.</summary>
public class SegmentedItem : ListBoxItem
{
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        UpdateSelectionFromEvent(e);
    }
}
