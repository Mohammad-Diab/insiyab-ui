using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>One mark on a <see cref="MediaBar"/>: its place from 0 to 1, the words its hover shows, and a tone.</summary>
public sealed class MediaMarker
{
    /// <summary>The place on the bar, from 0 (the start) to 1 (the end).</summary>
    public double At { get; set; }

    /// <summary>Shown after the time while the pointer is over the mark, as a chapter's name.</summary>
    public string? Tip { get; set; }

    /// <summary><c>ok</c>, <c>warn</c>, <c>bad</c>, <c>info</c> or <c>brand</c>; unset, the mark is muted.</summary>
    public string? Tone { get; set; }
}

/// <summary>The data of <see cref="MediaBar.SeekRequestedEvent"/>: where the person asked to go.</summary>
public sealed class MediaSeekEventArgs : RoutedEventArgs
{
    internal MediaSeekEventArgs(RoutedEvent routedEvent, double value, TimeSpan time, bool isDragging) : base(routedEvent)
    {
        Value = value;
        Time = time;
        IsDragging = isDragging;
    }

    /// <summary>The place, from 0 to 1.</summary>
    public double Value { get; }

    /// <summary>The place as a time of <see cref="MediaBar.Duration"/>; zero when no duration is set.</summary>
    public TimeSpan Time { get; }

    /// <summary>True while a drag goes on; false for a press, a key and the drag's last place.</summary>
    public bool IsDragging { get; }
}

/// <summary>A player's time bar: the played part, the buffered part, marks, a time bubble over the pointer, drag and keys. It always runs left to right, as time does on every player.</summary>
[PseudoClasses(":dragging")]
public class MediaBar : Control
{
    /// <summary>Where the media is, from 0 to 1; a press, a drag or a key moves it.</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MediaBar, double>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How far the media has loaded, from 0 to 1.</summary>
    public static readonly StyledProperty<double> BufferedProperty =
        AvaloniaProperty.Register<MediaBar, double>(nameof(Buffered));

    /// <summary>A part at the start that cannot be played, from 0 to 1: drawn thinner, with an upright mark where it ends; nothing seeks into it.</summary>
    public static readonly StyledProperty<double> LeadProperty =
        AvaloniaProperty.Register<MediaBar, double>(nameof(Lead));

    /// <summary>What the bubble says over the lead part.</summary>
    public static readonly StyledProperty<string?> LeadTipProperty =
        AvaloniaProperty.Register<MediaBar, string?>(nameof(LeadTip));

    /// <summary>The media's length: the bubble's time, the keys' steps and what a screen reader hears.</summary>
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<MediaBar, TimeSpan>(nameof(Duration));

    public static readonly StyledProperty<IList<MediaMarker>?> MarkersProperty =
        AvaloniaProperty.Register<MediaBar, IList<MediaMarker>?>(nameof(Markers));

    /// <summary>The index of the mark drawn in the brand colour and taller, as the chapter playing now; -1 for none.</summary>
    public static readonly StyledProperty<int> CurrentMarkerProperty =
        AvaloniaProperty.Register<MediaBar, int>(nameof(CurrentMarker), -1);

    /// <summary>The step of the arrow keys; a hundredth of the bar while no duration is set.</summary>
    public static readonly StyledProperty<TimeSpan> SmallStepProperty =
        AvaloniaProperty.Register<MediaBar, TimeSpan>(nameof(SmallStep), TimeSpan.FromSeconds(5));

    /// <summary>The step of Page Up and Page Down; a tenth of the bar while no duration is set.</summary>
    public static readonly StyledProperty<TimeSpan> LargeStepProperty =
        AvaloniaProperty.Register<MediaBar, TimeSpan>(nameof(LargeStep), TimeSpan.FromSeconds(30));

    public static readonly DirectProperty<MediaBar, bool> IsDraggingProperty =
        AvaloniaProperty.RegisterDirect<MediaBar, bool>(nameof(IsDragging), b => b.IsDragging);

    /// <summary>Raised on a press, on every move of a drag, at the drag's end and on a key: seek the media there.</summary>
    public static readonly RoutedEvent<MediaSeekEventArgs> SeekRequestedEvent =
        RoutedEvent.Register<MediaBar, MediaSeekEventArgs>(nameof(SeekRequested), RoutingStrategies.Bubble);

    // The track's centre sits this far above the bottom: the marks take the room above it.
    internal const double TrackFromBottom = 9;

    private static Cursor? s_hand;

    private bool _dragging;
    private double? _dragAt;
    private Popup? _bubble;
    private TextBlock? _bubbleText;

    static MediaBar()
    {
        AffectsRender<MediaBar>(ValueProperty, BufferedProperty, LeadProperty, MarkersProperty, CurrentMarkerProperty);
        FocusableProperty.OverrideDefaultValue<MediaBar>(true);
        FlowDirectionProperty.OverrideDefaultValue<MediaBar>(FlowDirection.LeftToRight);
    }

    public MediaBar()
    {
        SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);
        // a current value, not a local one: a local value beat the seek row's template binding, so a row's marks never reached its bar
        SetCurrentValue(MarkersProperty, new AvaloniaList<MediaMarker>());
        Cursor = Hand;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    private static Cursor Hand => s_hand ??= new Cursor(StandardCursorType.Hand);

    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public double Buffered { get => GetValue(BufferedProperty); set => SetValue(BufferedProperty, value); }

    public double Lead { get => GetValue(LeadProperty); set => SetValue(LeadProperty, value); }

    public string? LeadTip { get => GetValue(LeadTipProperty); set => SetValue(LeadTipProperty, value); }

    public TimeSpan Duration { get => GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    /// <summary>The marks, written inside the bar in markup or given as a list; replace the list to change them.</summary>
    public IList<MediaMarker>? Markers { get => GetValue(MarkersProperty); set => SetValue(MarkersProperty, value); }

    public int CurrentMarker { get => GetValue(CurrentMarkerProperty); set => SetValue(CurrentMarkerProperty, value); }

    public TimeSpan SmallStep { get => GetValue(SmallStepProperty); set => SetValue(SmallStepProperty, value); }

    public TimeSpan LargeStep { get => GetValue(LargeStepProperty); set => SetValue(LargeStepProperty, value); }

    /// <summary>True while the thumb is dragged; the bar then draws the drag's place, whatever <see cref="Value"/> the player reports.</summary>
    public bool IsDragging
    {
        get => _dragging;
        private set
        {
            if (SetAndRaise(IsDraggingProperty, ref _dragging, value)) PseudoClasses.Set(":dragging", value);
        }
    }

    public event EventHandler<MediaSeekEventArgs>? SeekRequested
    {
        add => AddHandler(SeekRequestedEvent, value);
        remove => RemoveHandler(SeekRequestedEvent, value);
    }

    /// <summary>A time as players write it, in Latin digits: <c>4:05</c>, or <c>1:02:09</c> from an hour up.</summary>
    public static string Clock(TimeSpan time)
    {
        if (time < TimeSpan.Zero) time = TimeSpan.Zero;
        var inv = CultureInfo.InvariantCulture;
        var hours = (int)time.TotalHours;
        return hours > 0
            ? string.Format(inv, "{0}:{1:00}:{2:00}", hours, time.Minutes, time.Seconds)
            : string.Format(inv, "{0}:{1:00}", time.Minutes, time.Seconds);
    }

    internal double LeadPart => Math.Clamp(Lead, 0, 1);

    internal double Shown => Math.Clamp(_dragAt ?? Value, 0, 1);

    internal TimeSpan TimeAt(double value) => Duration > TimeSpan.Zero ? TimeSpan.FromTicks((long)(Duration.Ticks * Math.Clamp(value, 0, 1))) : TimeSpan.Zero;

    /// <summary>The bubble while it shows: its text and the x it points at, for the tests.</summary>
    internal Control? BubbleFace => _bubble?.Child;

    internal (string Text, double X)? Bubble => _bubble is { IsOpen: true } b && _bubbleText is not null ? (_bubbleText.Text ?? "", (b.PlacementRect?.X ?? 0) + 0.5) : null;

    // The bubble's words for a place: the time, a mark's tip after it, or the lead's tip.
    internal string BubbleText(double over)
    {
        if (over < LeadPart && !string.IsNullOrEmpty(LeadTip)) return LeadTip!;
        var text = Duration > TimeSpan.Zero ? Clock(TimeAt(over)) : Math.Round(over * 100).ToString(CultureInfo.InvariantCulture) + "%";
        if (MarkerNear(over) is { Tip: { Length: > 0 } tip }) text += " · " + tip;
        return text;
    }

    private MediaMarker? MarkerNear(double over)
    {
        if (Markers is not { Count: > 0 } marks || Bounds.Width <= 0) return null;
        var x = over * Bounds.Width;
        MediaMarker? best = null;
        var gap = 5.0;
        foreach (var mark in marks)
        {
            var d = Math.Abs(Math.Clamp(mark.At, 0, 1) * Bounds.Width - x);
            if (d > gap) continue;
            gap = d;
            best = mark;
        }
        return best;
    }

    private double Over(PointerEventArgs e) => Bounds.Width <= 0 ? 0 : Math.Clamp(e.GetPosition(this).X / Bounds.Width, 0, 1);

    private double At(double over) => Math.Max(LeadPart, over);

    private void Seek(double value, bool dragging)
    {
        value = Math.Clamp(value, LeadPart, 1);
        if (!dragging) SetCurrentValue(ValueProperty, value);
        RaiseEvent(new MediaSeekEventArgs(SeekRequestedEvent, value, TimeAt(value), dragging));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        Focus(NavigationMethod.Pointer);
        IsDragging = true;
        e.Pointer.Capture(this);
        var over = Over(e);
        _dragAt = At(over);
        InvalidateVisual();
        ShowBubble(over);
        Seek(_dragAt.Value, true);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var over = Over(e);
        Cursor = over < LeadPart && !_dragging ? Cursor.Default : Hand;
        ShowBubble(_dragging ? At(over) : over);
        if (!_dragging) return;
        _dragAt = At(over);
        InvalidateVisual();
        Seek(_dragAt.Value, true);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        var end = _dragAt ?? At(Over(e));
        e.Pointer.Capture(null);
        EndDrag();
        Seek(end, false);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndDrag();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!_dragging) HideBubble();
    }

    private void EndDrag()
    {
        if (!_dragging) return;
        IsDragging = false;
        _dragAt = null;
        if (!IsPointerOver) HideBubble();
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None) return;
        var small = StepOf(SmallStep, 0.01);
        var large = StepOf(LargeStep, 0.1);
        double? to = e.Key switch
        {
            Key.Left or Key.Down => Value - small,
            Key.Right or Key.Up => Value + small,
            Key.PageDown => Value - large,
            Key.PageUp => Value + large,
            Key.Home => LeadPart,
            Key.End => 1,
            _ => null,
        };
        if (to is not { } value) return;
        Seek(value, false);
        e.Handled = true;
    }

    private double StepOf(TimeSpan step, double share) =>
        Duration > TimeSpan.Zero && step > TimeSpan.Zero ? step.TotalSeconds / Duration.TotalSeconds : share;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkersProperty)
        {
            if (change.OldValue is System.Collections.Specialized.INotifyCollectionChanged old) old.CollectionChanged -= OnMarkersChanged;
            if (change.NewValue is System.Collections.Specialized.INotifyCollectionChanged now) now.CollectionChanged += OnMarkersChanged;
        }
        else if (change.Property == IsEnabledProperty && !IsEnabled)
        {
            EndDrag();
            HideBubble();
        }
    }

    private void OnMarkersChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        HideBubble();
    }

    private void ShowBubble(double over)
    {
        if (_bubble is null)
        {
            _bubbleText = new TextBlock { FlowDirection = FlowDirection.LeftToRight };
            var face = new Border { Child = _bubbleText, IsHitTestVisible = false };
            face.Classes.Add("media-time");
            _bubble = new Popup
            {
                Child = face,
                Placement = PlacementMode.Top,
                PlacementTarget = this,
                IsLightDismissEnabled = false,
                IsHitTestVisible = false,
                FlowDirection = FlowDirection.LeftToRight,
            };
            LogicalChildren.Add(_bubble);
        }
        _bubbleText!.Text = BubbleText(over);
        var x = over * Bounds.Width;
        var y = Math.Max(0, Bounds.Height - TrackFromBottom - 18);
        _bubble.PlacementRect = new Rect(x - 0.5, y, 1, 1);
        if (!_bubble.IsOpen) _bubble.IsOpen = true;
    }

    private void HideBubble()
    {
        if (_bubble is { IsOpen: true }) _bubble.IsOpen = false;
    }

    private IBrush Res(string key, IBrush fallback) => this.TryFindResource(key, ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    private IBrush ToneBrush(string? tone) => tone?.Trim() switch
    {
        "ok" => Res("InsToneOk", Brushes.Green),
        "warn" => Res("InsToneWarnSignal", Brushes.Orange),
        "bad" => Res("InsToneBad", Brushes.Red),
        "info" => Res("InsToneInfo", Brushes.Blue),
        "brand" => Res("InsToneBrandSoft", Brushes.Teal),
        _ => Res("InsText3", Brushes.Gray),
    };

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var y = Bounds.Height - TrackFromBottom;
        var shown = Shown;
        var brand = Res("InsPrimaryMid", Brushes.Teal);
        var track = Res("InsLine", Brushes.LightGray);
        var loaded = Res("InsHr", Brushes.Gray);
        var lead = LeadPart * w;
        var buffered = Math.Clamp(Buffered, 0, 1) * w;
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));
        if (lead > 0) context.DrawRectangle(track, null, new RoundedRect(new Rect(0, y - 1, lead, 2), 1));
        context.DrawRectangle(track, null, new RoundedRect(new Rect(lead, y - 2, Math.Max(0, w - lead), 4), 2));
        if (buffered > lead) context.DrawRectangle(loaded, null, new RoundedRect(new Rect(lead, y - 2, buffered - lead, 4), 2));
        context.DrawRectangle(brand, null, new RoundedRect(new Rect(lead, y - 2, Math.Max(0, w * shown - lead), 4), 2));
        if (lead > 0) context.DrawRectangle(Res("InsText2", Brushes.Gray), null, new RoundedRect(new Rect(lead - 1, y - 6, 2, 12), 1));
        if (Markers is { } marks)
            for (var i = 0; i < marks.Count; i++)
            {
                var x = Math.Clamp(marks[i].At, 0, 1) * w;
                var current = i == CurrentMarker;
                context.DrawRectangle(current ? brand : ToneBrush(marks[i].Tone), null, new RoundedRect(new Rect(x - 1.5, y - (current ? 16 : 14), 3, current ? 10 : 7), 1.5));
            }
        context.DrawEllipse(Res("InsSurface", Brushes.White), new Pen(brand, 3), new Point(w * shown, y), 7, 7);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MediaBarAutomationPeer(this);

    internal string SpokenName => AutomationProperties.GetName(this) is { Length: > 0 } name ? name : Words.Of(TopLevel.GetTopLevel(this) ?? (Visual)this)["media.seek"];

    internal void SeekFromPeer(double seconds) =>
        Seek(Duration > TimeSpan.Zero ? seconds / Duration.TotalSeconds : seconds / 100, false);
}

/// <summary>A screen reader reads and moves the bar as a slider: in seconds when the length is known, else in hundredths.</summary>
internal sealed class MediaBarAutomationPeer(MediaBar owner) : ControlAutomationPeer(owner), IRangeValueProvider
{
    private new MediaBar Owner => (MediaBar)base.Owner;

    private double Scale => Owner.Duration > TimeSpan.Zero ? Owner.Duration.TotalSeconds : 100;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    protected override string GetNameCore() => Owner.SpokenName;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    public bool IsReadOnly => !Owner.IsEffectivelyEnabled;

    public double Minimum => Owner.LeadPart * Scale;

    public double Maximum => Scale;

    public double Value => Owner.Shown * Scale;

    public double LargeChange => Owner.Duration > TimeSpan.Zero ? Owner.LargeStep.TotalSeconds : 10;

    public double SmallChange => Owner.Duration > TimeSpan.Zero ? Owner.SmallStep.TotalSeconds : 1;

    public void SetValue(double value) => Owner.SeekFromPeer(value);
}
