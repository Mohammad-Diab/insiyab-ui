using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>What happened, in order: the timeline plugin's <c>ol.ins-timeline</c>. Holds <see cref="TimelineItem"/>s and <see cref="TimelineDay"/> headings. Classes <c>compact</c> (smaller dots, tighter) and <c>split</c> (both sides of a centre rule once the list is 640px wide).</summary>
public class Timeline : ItemsControl
{
    private readonly Dictionary<Control, Thickness> _margins = [];
    private readonly List<Control> _watched = [];
    private TimelineSlot?[]? _plan;
    private ItemsSourceView? _source;
    private bool _stale;
    private bool _split;
    private double _splitWidth;

    public Timeline()
    {
        Watch();
        Classes.CollectionChanged += (_, _) => Resplit();
        SizeChanged += (_, _) => Resplit();
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not (TimelineItem or TimelineDay);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new TimelineItem();

    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);
        Place(container, index);
    }

    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        Place(container, newIndex);
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        Unmargin(container);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != ItemsSourceProperty) return;
        Watch();
        Invalidate();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_stale)
        {
            _stale = false;
            Reapply();
        }
        return base.MeasureOverride(availableSize);
    }

    /// <summary>Works the places out again after an event was added, removed, shown, hidden or made pending.</summary>
    internal void Invalidate()
    {
        Forget();
        if (ItemsPanelRoot is TimelinePanel panel)
        {
            panel.InvalidateMeasure();
            return;
        }
        _stale = true;
        InvalidateMeasure();
    }

    private void Watch()
    {
        if (ReferenceEquals(_source, ItemsView)) return;
        if (_source is not null) _source.CollectionChanged -= OnItemsChanged;
        _source = ItemsView;
        _source.CollectionChanged += OnItemsChanged;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Invalidate();

    private void OnEntryChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TimelineItem.IsPendingProperty || e.Property == IsVisibleProperty) Invalidate();
    }

    private void Forget()
    {
        foreach (var c in _watched) c.PropertyChanged -= OnEntryChanged;
        _watched.Clear();
        _plan = null;
    }

    private TimelineSlot?[] Plan()
    {
        if (_plan is { } cached) return cached;
        var count = ItemsView.Count;
        var shown = new List<(TimelineKind Kind, bool Pending)>(count);
        var at = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            var item = ItemsView[i];
            if (item is Control c && c is TimelineItem or TimelineDay)
            {
                c.PropertyChanged += OnEntryChanged;
                _watched.Add(c);
                if (!c.IsVisible) continue;
            }
            shown.Add((item is TimelineDay ? TimelineKind.Day : TimelineKind.Event, item is TimelineItem { IsPending: true }));
            at.Add(i);
        }
        var slots = TimelinePlan.Of(shown);
        var plan = new TimelineSlot?[count];
        for (var j = 0; j < slots.Length; j++) plan[at[j]] = slots[j];
        return _plan = plan;
    }

    private void Resplit()
    {
        var split = Classes.Contains("split") && Bounds.Width >= TimelinePanel.SplitWidth;
        var width = split ? Bounds.Width : 0;
        if (split == _split && width == _splitWidth) return;
        _split = split;
        _splitWidth = width;
        Reapply();
    }

    private void Reapply()
    {
        foreach (var container in GetRealizedContainers()) Place(container, IndexFromContainer(container));
    }

    private void Place(Control container, int index)
    {
        if (ItemsPanelRoot is TimelinePanel) return;
        var plan = Plan();
        if ((uint)index >= (uint)plan.Length || plan[index] is not { } slot) return;
        if (container is TimelineDay day) day.SetPlace(slot.First, _split);
        if (container is not TimelineItem item) return;
        var start = _split && slot.K % 2 == 1;
        item.SetPlace(slot.Last, slot.NextPending, _split, start);
        if (!_split) Unmargin(item);
        else SetMargin(item, start ? new Thickness(0, 0, _splitWidth / 2, 0) : new Thickness(_splitWidth / 2, 0, 0, 0));
    }

    private void SetMargin(Control container, Thickness margin)
    {
        if (container.Margin != margin) container.Margin = margin;
        _margins[container] = margin;
    }

    private void Unmargin(Control container)
    {
        if (_margins.Remove(container, out var set) && container.Margin == set) container.Margin = default;
    }
}

/// <summary>Where an entry sits in the list, as the timeline's rail and sides need it.</summary>
internal enum TimelineKind { Other, Day, Event }

/// <summary>One shown entry's place: first or last of the list, ahead of a pending event, and its turn among the events.</summary>
internal readonly record struct TimelineSlot(TimelineKind Kind, bool First, bool Last, bool NextPending, int K);

/// <summary>The one rule for which flags each shown entry gets, shared by <see cref="Timeline"/> and <see cref="TimelinePanel"/>.</summary>
internal static class TimelinePlan
{
    public static TimelineSlot[] Of(IReadOnlyList<(TimelineKind Kind, bool Pending)> shown)
    {
        var slots = new TimelineSlot[shown.Count];
        var k = 0;
        for (var i = 0; i < shown.Count; i++)
        {
            var next = i + 1 < shown.Count && shown[i + 1] is { Kind: TimelineKind.Event, Pending: true };
            slots[i] = new TimelineSlot(shown[i].Kind, i == 0, i == shown.Count - 1, next, shown[i].Kind == TimelineKind.Event ? k++ : -1);
        }
        return slots;
    }
}

/// <summary>A date heading between events, the web's <c>.ins-timeline-day</c>.</summary>
[PseudoClasses(":first", ":split")]
public class TimelineDay : ContentControl
{
    internal void SetPlace(bool first, bool split)
    {
        PseudoClasses.Set(":first", first);
        PseudoClasses.Set(":split", split);
    }
}

/// <summary>One event: a dot on the rule, a title (<c>Header</c>), a time and a body (<c>Content</c>, text or a panel). Classes <c>ok bad warn info</c> for its tone; <see cref="IsCurrent"/> wears a halo and <see cref="IsPending"/> is hollow on a dashed rule; an <see cref="Icon"/> replaces the dot with a chip.</summary>
[PseudoClasses(":current", ":pending", ":next-pending", ":last", ":icon", ":split", ":split-start", ":has-time", ":has-body")]
public class TimelineItem : HeaderedContentControl
{
    public static readonly StyledProperty<string?> TimeProperty =
        AvaloniaProperty.Register<TimelineItem, string?>(nameof(Time));

    public static readonly StyledProperty<DateTime?> AtProperty =
        AvaloniaProperty.Register<TimelineItem, DateTime?>(nameof(At));

    public static readonly StyledProperty<TimeMode> TimeModeProperty =
        AvaloniaProperty.Register<TimelineItem, TimeMode>(nameof(TimeMode));

    public static readonly DirectProperty<TimelineItem, string?> TimeTextProperty =
        AvaloniaProperty.RegisterDirect<TimelineItem, string?>(nameof(TimeText), o => o.TimeText);

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<TimelineItem, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<bool> IsCurrentProperty =
        AvaloniaProperty.Register<TimelineItem, bool>(nameof(IsCurrent));

    public static readonly StyledProperty<bool> IsPendingProperty =
        AvaloniaProperty.Register<TimelineItem, bool>(nameof(IsPending));

    private static readonly List<WeakReference<TimelineItem>> Live = [];
    private static DispatcherTimer? _timer;
    private string? _timeText;

    /// <summary>Overrides the clock relative times are measured from; for tests and screenshots.</summary>
    internal static Func<DateTime> Now { get; set; } = () => DateTime.Now;

    static TimelineItem()
    {
        IsCurrentProperty.Changed.AddClassHandler<TimelineItem>((x, e) => x.PseudoClasses.Set(":current", e.GetNewValue<bool>()));
        IsPendingProperty.Changed.AddClassHandler<TimelineItem>((x, e) =>
        {
            x.PseudoClasses.Set(":pending", e.GetNewValue<bool>());
            (ItemsControl.ItemsControlFromItemContainer(x) as Timeline)?.Invalidate();
        });
        IconProperty.Changed.AddClassHandler<TimelineItem>((x, e) => x.PseudoClasses.Set(":icon", e.GetNewValue<Geometry?>() is not null));
        ContentProperty.Changed.AddClassHandler<TimelineItem>((x, e) => x.PseudoClasses.Set(":has-body", e.GetNewValue<object?>() is not null));
        TimeProperty.Changed.AddClassHandler<TimelineItem>((x, _) => x.Refresh());
        AtProperty.Changed.AddClassHandler<TimelineItem>((x, _) => x.Refresh());
        TimeModeProperty.Changed.AddClassHandler<TimelineItem>((x, _) => x.Refresh());
        FlowDirectionProperty.Changed.AddClassHandler<TimelineItem>((x, _) => x.Refresh());
    }

    /// <summary>The time as written: "8:02 ص".</summary>
    public string? Time { get => GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    /// <summary>When it happened, for <see cref="TimeMode"/> Relative or Date; also the full date in the tooltip.</summary>
    public DateTime? At { get => GetValue(AtProperty); set => SetValue(AtProperty, value); }

    public TimeMode TimeMode { get => GetValue(TimeModeProperty); set => SetValue(TimeModeProperty, value); }

    /// <summary>What the time line shows: <see cref="Time"/>, or <see cref="At"/> written by <see cref="TimeMode"/>. Arabic in a right-to-left item, English otherwise.</summary>
    public string? TimeText { get => _timeText; private set => SetAndRaise(TimeTextProperty, ref _timeText, value); }

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public bool IsCurrent { get => GetValue(IsCurrentProperty); set => SetValue(IsCurrentProperty, value); }

    public bool IsPending { get => GetValue(IsPendingProperty); set => SetValue(IsPendingProperty, value); }

    internal void SetPlace(bool last, bool nextPending, bool split, bool splitStart)
    {
        PseudoClasses.Set(":last", last);
        PseudoClasses.Set(":next-pending", nextPending);
        PseudoClasses.Set(":split", split);
        PseudoClasses.Set(":split-start", splitStart);
    }

    /// <summary>Writes the relative times again: every minute on its own, or at once after the clock changed.</summary>
    internal static void RefreshAll()
    {
        foreach (var weak in Live.ToList())
            if (weak.TryGetTarget(out var item)) item.Refresh();
            else Live.Remove(weak);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Refresh();
        if (TimeMode == TimeMode.Text || At is null) return;
        Live.Add(new WeakReference<TimelineItem>(this));
        _timer ??= new DispatcherTimer(TimeSpan.FromMinutes(1), DispatcherPriority.Background, (_, _) => RefreshAll());
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Live.RemoveAll(w => !w.TryGetTarget(out var t) || t == this);
        if (Live.Count == 0) _timer?.Stop();
    }

    private void Refresh()
    {
        var arabic = FlowDirection == FlowDirection.RightToLeft;
        var dayOnly = At is { } d && d.TimeOfDay == TimeSpan.Zero && TimeMode != TimeMode.Text;
        TimeText = TimeMode == TimeMode.Text || At is null
            ? Time
            : RelativeTime.Format(At.Value, Now(), arabic, TimeMode, dayOnly);
        ToolTip.SetTip(this, At is { } at ? RelativeTime.FullDate(at, arabic, !dayOnly) : null);
        PseudoClasses.Set(":has-time", !string.IsNullOrEmpty(TimeText));
    }
}

/// <summary>The timeline's column: one event under another, or, in a <c>split</c> timeline at least 640px wide, the events taking turns on either side of a centre rule.</summary>
internal class TimelinePanel : Panel
{
    public const double SplitWidth = 640;

    private bool Split(double width) => this.FindAncestorOfType<Timeline>()?.Classes.Contains("split") == true && width >= SplitWidth;

    private void Place(bool split)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        var slots = TimelinePlan.Of(shown.Select(c => (c is TimelineDay ? TimelineKind.Day : c is TimelineItem ? TimelineKind.Event : TimelineKind.Other, c is TimelineItem { IsPending: true })).ToList());
        for (var i = 0; i < shown.Count; i++)
        {
            if (shown[i] is TimelineDay day) day.SetPlace(slots[i].First, split);
            if (shown[i] is TimelineItem item) item.SetPlace(slots[i].Last, slots[i].NextPending, split, split && slots[i].K % 2 == 1);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var split = Split(availableSize.Width);
        Place(split);
        double height = 0, width = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var w = split && child is TimelineItem ? availableSize.Width / 2 : availableSize.Width;
            child.Measure(new Size(w, double.PositiveInfinity));
            height += child.DesiredSize.Height;
            width = Math.Max(width, child.DesiredSize.Width * (split && child is TimelineItem ? 2 : 1));
        }
        return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var split = Split(finalSize.Width);
        double y = 0;
        var k = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var h = child.DesiredSize.Height;
            if (split && child is TimelineItem)
            {
                var half = finalSize.Width / 2;
                child.Arrange(new Rect(k % 2 == 0 ? half : 0, y, half, h));
                k++;
            }
            else child.Arrange(new Rect(0, y, finalSize.Width, h));
            y += h;
        }
        return finalSize;
    }
}
