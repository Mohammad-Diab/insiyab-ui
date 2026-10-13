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
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not (TimelineItem or TimelineDay);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new TimelineItem();
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
    public static Func<DateTime> Now { get; set; } = () => DateTime.Now;

    static TimelineItem()
    {
        IsCurrentProperty.Changed.AddClassHandler<TimelineItem>((x, e) => x.PseudoClasses.Set(":current", e.GetNewValue<bool>()));
        IsPendingProperty.Changed.AddClassHandler<TimelineItem>((x, e) =>
        {
            x.PseudoClasses.Set(":pending", e.GetNewValue<bool>());
            (x.Parent as Timeline)?.ItemsPanelRoot?.InvalidateMeasure();
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
    public static void RefreshAll()
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
public class TimelinePanel : Panel
{
    public const double SplitWidth = 640;

    private bool Split(double width) => this.FindAncestorOfType<Timeline>()?.Classes.Contains("split") == true && width >= SplitWidth;

    private void Place(bool split)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        var events = shown.OfType<TimelineItem>().ToList();
        for (var i = 0; i < shown.Count; i++)
        {
            if (shown[i] is TimelineDay day) day.SetPlace(i == 0, split);
            if (shown[i] is not TimelineItem item) continue;
            var next = i + 1 < shown.Count ? shown[i + 1] as TimelineItem : null;
            var k = events.IndexOf(item);
            item.SetPlace(i == shown.Count - 1, next?.IsPending == true, split, split && k % 2 == 1);
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
