using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The current link of a <see cref="Toc"/> changed: the web's <c>ins:scrollspy</c>.</summary>
public class ScrollspyEventArgs(RoutedEvent routedEvent, TocLink? link, Control? section) : RoutedEventArgs(routedEvent)
{
    public TocLink? Link { get; } = link;
    public Control? Section { get; } = section;
}

/// <summary>An entry of a <see cref="Toc"/>: a link to a section; <c>Classes="sub"</c> for a deeper heading, a step in.</summary>
[PseudoClasses(":active")]
public class TocLink : Button
{
    public static readonly StyledProperty<Control?> SectionProperty =
        AvaloniaProperty.Register<TocLink, Control?>(nameof(Section));

    protected override Type StyleKeyOverride => typeof(TocLink);

    /// <summary>The section this link goes to and follows.</summary>
    public Control? Section { get => GetValue(SectionProperty); set => SetValue(SectionProperty, value); }

    public bool IsActive { get; private set; }

    internal void SetActive(bool active)
    {
        IsActive = active;
        PseudoClasses.Set(":active", active);
    }
}

/// <summary>The contents list that follows the reader, the web's <c>.ins-toc</c> with <c>data-ins-scrollspy</c>: the link whose section is being read in a <see cref="ScrollViewer"/> is marked, and the brand marker flies to it.</summary>
[TemplatePart("PART_Marker", typeof(Border))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class Toc : ItemsControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Toc, string?>(nameof(Title));

    public static readonly StyledProperty<ScrollViewer?> ScrollViewerProperty =
        AvaloniaProperty.Register<Toc, ScrollViewer?>(nameof(ScrollViewer));

    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<Toc, double>(nameof(Offset), double.NaN);

    public static readonly DirectProperty<Toc, TocLink?> ActiveLinkProperty =
        AvaloniaProperty.RegisterDirect<Toc, TocLink?>(nameof(ActiveLink), o => o.ActiveLink);

    public static readonly RoutedEvent<ScrollspyEventArgs> ActiveChangedEvent =
        RoutedEvent.Register<Toc, ScrollspyEventArgs>(nameof(ActiveChanged), RoutingStrategies.Bubble);

    private ScrollViewer? _scroller;
    private Border? _marker;
    private Visual? _host;
    private TocLink? _active;
    private bool _held, _markerShown;
    private IDisposable? _release;
    private DispatcherTimer? _tween;

    /// <summary>A small heading at the top of the list (<c>.ins-toc-title</c>).</summary>
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>What scrolls the sections; unset, the nearest scroll viewer around the first section.</summary>
    public ScrollViewer? ScrollViewer { get => GetValue(ScrollViewerProperty); set => SetValue(ScrollViewerProperty, value); }

    /// <summary>How far below the top of the view the current section's line is, in px (<c>data-ins-scrollspy-offset</c>); NaN for a third of the view.</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    public TocLink? ActiveLink { get => _active; private set => SetAndRaise(ActiveLinkProperty, ref _active, value); }

    public event EventHandler<ScrollspyEventArgs>? ActiveChanged
    {
        add => AddHandler(ActiveChangedEvent, value);
        remove => RemoveHandler(ActiveChangedEvent, value);
    }

    public IEnumerable<TocLink> Links => Items.OfType<TocLink>();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not TocLink;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _marker = e.NameScope.Find<Border>("PART_Marker");
        _host = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        _markerShown = false;
        Dispatcher.UIThread.Post(() => Refresh(), DispatcherPriority.Loaded);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AddHandler(Button.ClickEvent, OnLinkClick);
        LayoutUpdated += OnLayoutUpdated;
        Dispatcher.UIThread.Post(() => Refresh(), DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        RemoveHandler(Button.ClickEvent, OnLinkClick);
        LayoutUpdated -= OnLayoutUpdated;
        Hook(null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ScrollViewerProperty || change.Property == OffsetProperty) Refresh();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_scroller is null) Refresh();
        PlaceMarker(false);
    }

    /// <summary>Measures again, after content above the sections grew, say, and returns the current link: <c>Insiyab.scrollspy(nav)</c>.</summary>
    public TocLink? Refresh()
    {
        Hook(ScrollViewer ?? Links.Select(l => l.Section).FirstOrDefault(s => s is not null)?.FindAncestorOfType<ScrollViewer>());
        _held = false;
        Update();
        return ActiveLink;
    }

    private void Hook(ScrollViewer? scroller)
    {
        if (_scroller == scroller) return;
        if (_scroller is not null) _scroller.ScrollChanged -= OnScroll;
        _scroller = scroller;
        if (_scroller is not null) _scroller.ScrollChanged += OnScroll;
    }

    private void OnScroll(object? sender, ScrollChangedEventArgs e)
    {
        if (_held)
        {
            _release?.Dispose();
            _release = DispatcherTimer.RunOnce(() => { _held = false; Update(); }, TimeSpan.FromMilliseconds(350));
            return;
        }
        Update();
    }

    private void Update()
    {
        if (_held) return;
        Mark(Current());
    }

    // The current section: the last whose top has passed a line a third of the way down the view, and at the very end the last one.
    private int Current()
    {
        var links = Links.ToList();
        if (_scroller is null || links.Count == 0) return -1;
        var viewport = _scroller.Viewport.Height;
        var line = double.IsNaN(Offset) ? viewport / 3 : Offset;
        var end = Math.Ceiling(_scroller.Offset.Y + viewport) >= _scroller.Extent.Height - 2;
        var start = _scroller.Offset.Y <= 0;
        if (end && !start) return links.Count - 1;
        var at = -1;
        for (var i = 0; i < links.Count; i++)
        {
            if (links[i].Section is not { } s) continue;
            var top = s.TranslatePoint(default, _scroller);
            if (top is { } p && p.Y <= line + 1) at = i;
        }
        return at;
    }

    private void Mark(int i)
    {
        var links = Links.ToList();
        var link = i >= 0 && i < links.Count ? links[i] : null;
        if (link == _active) return;
        _active?.SetActive(false);
        ActiveLink = link;
        link?.SetActive(true);
        PlaceMarker(true);
        RaiseEvent(new ScrollspyEventArgs(ActiveChangedEvent, link, link?.Section));
    }

    // The marker flies from the old entry to the new one, as the sidebar's does; the first time it grows in place.
    private void PlaceMarker(bool moved)
    {
        if (_marker is null || _host is null || _flight is not null && !moved) return;
        if (_active is null || !_active.IsArrangeValid)
        {
            if (_active is null) { _marker.Opacity = 0; _markerShown = false; }
            return;
        }
        var top = _active.TranslatePoint(default, _host);
        if (top is null) return;
        var y = top.Value.Y + 7;
        var h = Math.Max(0, _active.Bounds.Height - 14);
        if (!moved && Math.Abs(_marker.Height - h) < 0.5 && _marker.Tag is double last && Math.Abs(last - y) < 0.5) return;
        var flying = _flight is not null;
        var from = flying ? _flightY : _marker.Tag as double?;
        var fromHeight = flying ? _flightHeight : _marker.Height;
        _marker.Tag = y;
        if (!_markerShown || Motion.IsStill(this))
        {
            var transitions = _marker.Transitions;
            _marker.Transitions = null;
            _marker.Height = h;
            _marker.RenderTransform = Shift(y, _markerShown ? 1 : 0);
            _marker.Opacity = 1;
            _marker.Transitions = transitions;
            if (_markerShown) return;
            _markerShown = true;
            Dispatcher.UIThread.Post(() => _marker.RenderTransform = Shift(y, 1), DispatcherPriority.Render);
            return;
        }
        if (moved && from is double y0 && fromHeight > 0 && TopLevel.GetTopLevel(this) is { } level)
        {
            Fly(level, y0, fromHeight, y, h);
            return;
        }
        _marker.Height = h;
        _marker.RenderTransform = Shift(y, 1);
    }

    private static ITransform Shift(double y, double scale) =>
        TransformOperations.Parse(FormattableString.Invariant($"translateY({y}px) scaleY({scale})"));

    private MarkerFlight? _flight;
    private Transitions? _resting;
    private double _flightY, _flightHeight;

    // Stretches from the old entry to cover the new one, then lets go of the old end: the marker flight every marker shares.
    private void Fly(TopLevel level, double y0, double h0, double y1, double h1)
    {
        var marker = _marker!;
        _flight?.Land();
        _resting ??= marker.Transitions;
        marker.Transitions = null;
        MarkerFlight? run = null;
        run = MarkerFlight.Run(this, (y0, h0), (y1, h1), Set, () => Land(run));
        if (run is not null) _flight = run;
        else
        {
            Set(y1, h1);
            marker.Transitions = _resting;
            _resting = null;
        }
    }

    private void Set(double y, double h)
    {
        _flightY = y;
        _flightHeight = h;
        _marker!.Height = Math.Max(0, h);
        _marker.RenderTransform = Shift(y, 1);
    }

    private void Land(MarkerFlight? run)
    {
        if (run is null || _flight != run || _marker is null) return;
        _flight = null;
        _marker.Tag = null;
        PlaceMarker(true);
        _marker.Transitions = _resting;
        _resting = null;
    }

    // A click marks the link at once, and the marker stays on it through the scroll the click started.
    private void OnLinkClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Visual v) return;
        var link = v as TocLink ?? v.FindAncestorOfType<TocLink>(true);
        if (link is null) return;
        var index = Links.ToList().IndexOf(link);
        if (index < 0) return;
        Mark(index);
        _held = true;
        _release?.Dispose();
        _release = DispatcherTimer.RunOnce(() => { _held = false; Update(); }, TimeSpan.FromMilliseconds(1200));
        ScrollTo(link.Section);
    }

    private void ScrollTo(Control? section)
    {
        if (_scroller is null || section?.TranslatePoint(default, _scroller) is not { } p) return;
        var from = _scroller.Offset.Y;
        var max = Math.Max(0, _scroller.Extent.Height - _scroller.Viewport.Height);
        var to = Math.Clamp(from + p.Y, 0, max);
        _tween?.Stop();
        var start = DateTime.UtcNow;
        var duration = TimeSpan.FromMilliseconds(360);
        var ease = new global::Avalonia.Animation.Easings.SplineEasing(0.22, 1, 0.36, 1);
        _tween = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) =>
        {
            var t = Math.Min(1, (DateTime.UtcNow - start) / duration);
            _scroller.Offset = new Vector(_scroller.Offset.X, from + (to - from) * ease.Ease(t));
            if (t >= 1) _tween?.Stop();
        });
        _tween.Start();
    }
}
