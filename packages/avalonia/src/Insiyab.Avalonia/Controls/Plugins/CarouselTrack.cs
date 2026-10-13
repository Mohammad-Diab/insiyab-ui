using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The row of a <c>Carousel</c> in the Insiyab theme: slides side by side with a gap, one, two or three at a time as the carousel's own width allows (<c>Classes="two"</c>, <c>"three"</c>), or with the next one peeking (<c>"peek"</c>).</summary>
public class CarouselTrack : Panel
{
    /// <summary>How many slides at a time where there is room: 2 from 576px wide (36rem), 3 from 896px (56rem).</summary>
    public static readonly AttachedProperty<int> PerViewProperty =
        AvaloniaProperty.RegisterAttached<CarouselTrack, Control, int>("PerView", 1, inherits: true);

    /// <summary>The edge of the next slide showing: each slide takes 86% of the row.</summary>
    public static readonly AttachedProperty<bool> PeekProperty =
        AvaloniaProperty.RegisterAttached<CarouselTrack, Control, bool>("Peek", false, inherits: true);

    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<CarouselTrack, double>(nameof(Gap), 16);

    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<CarouselTrack, double>(nameof(Offset));

    private readonly List<double> _stops = [];
    private double _slide;

    static CarouselTrack()
    {
        ClipToBoundsProperty.OverrideDefaultValue<CarouselTrack>(true);
        AffectsMeasure<CarouselTrack>(PerViewProperty, PeekProperty, GapProperty);
        AffectsArrange<CarouselTrack>(OffsetProperty);
    }

    public CarouselTrack()
    {
        Transitions =
        [
            new DoubleTransition { Property = OffsetProperty, Duration = TimeSpan.FromMilliseconds(420), Easing = new SplineEasing(0.22, 1, 0.36, 1) },
        ];
    }

    public static int GetPerView(Control c) => c.GetValue(PerViewProperty);
    public static void SetPerView(Control c, int value) => c.SetValue(PerViewProperty, value);
    public static bool GetPeek(Control c) => c.GetValue(PeekProperty);
    public static void SetPeek(Control c, bool value) => c.SetValue(PeekProperty, value);

    public double Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <summary>How far along the row the view starts, from the row's starting edge.</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    /// <summary>Where each stop starts: the slides whose start the row can bring to its edge. A wide view has fewer stops than slides.</summary>
    public IReadOnlyList<double> Stops => _stops;

    /// <summary>The slides shown at a time, after the width decided.</summary>
    public int EffectivePerView { get; private set; } = 1;

    /// <summary>The stops were measured again.</summary>
    public event EventHandler? StopsChanged;

    /// <summary>Jumps to a stop at once, without the slide.</summary>
    public void Jump(double offset)
    {
        var transitions = Transitions;
        Transitions = null;
        Offset = offset;
        Transitions = transitions;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (this.FindAncestorOfType<Carousel>() is { } carousel) CarouselNav.Attach(carousel, this);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? 640 : availableSize.Width;
        var per = GetPerView(this);
        EffectivePerView = per >= 3 && w >= 896 ? 3 : per >= 2 && w >= 576 ? 2 : 1;
        _slide = GetPeek(this) ? w * 0.86 : (w - (EffectivePerView - 1) * Gap) / EffectivePerView;
        var h = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(_slide, availableSize.Height));
            h = Math.Max(h, child.DesiredSize.Height);
        }
        return new Size(w, h);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var step = _slide + Gap;
        for (var i = 0; i < Children.Count; i++)
            Children[i].Arrange(new Rect(i * step - Offset, 0, _slide, finalSize.Height));

        var total = Children.Count * _slide + Math.Max(0, Children.Count - 1) * Gap;
        var room = Math.Max(0, total - finalSize.Width);
        var stops = new List<double>();
        for (var i = 0; i < Children.Count; i++)
        {
            var at = i * step;
            if (at <= room + 2 || i == 0) stops.Add(Math.Min(at, room));
        }
        if (!stops.SequenceEqual(_stops))
        {
            _stops.Clear();
            _stops.AddRange(stops);
            StopsChanged?.Invoke(this, EventArgs.Empty);
        }
        return finalSize;
    }
}

// Drives a Carousel in the Insiyab theme: the previous and next buttons (gone at the ends), a dot per stop, and arrow keys that follow the reading direction.
internal sealed class CarouselNav
{
    private static readonly AttachedProperty<CarouselNav?> NavProperty =
        AvaloniaProperty.RegisterAttached<CarouselNav, Carousel, CarouselNav?>("Nav");

    private readonly Carousel _carousel;
    private CarouselTrack? _track;
    private Button? _prev, _next;
    private Panel? _dots;
    private Border? _trackBox;
    private int _stop;
    private bool _moving, _dragging, _wheelLocked;
    private double _dragStart, _dragFrom, _dragDx, _wheel;
    private IDisposable? _wheelQuiet;

    private CarouselNav(Carousel carousel)
    {
        _carousel = carousel;
        carousel.TemplateApplied += OnTemplateApplied;
        carousel.SelectionChanged += OnSelectionChanged;
        carousel.AddHandler(InputElement.KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
        carousel.AddHandler(InputElement.PointerWheelChangedEvent, OnWheel, RoutingStrategies.Bubble);
        carousel.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.FlowDirectionProperty || e.Property == CarouselOptions.LocaleProperty || e.Property == CarouselOptions.HideDotsProperty) Paint();
        };
    }

    public static CarouselNav For(Carousel carousel)
    {
        var nav = carousel.GetValue(NavProperty);
        if (nav is null)
        {
            nav = new CarouselNav(carousel);
            carousel.SetValue(NavProperty, nav);
        }
        return nav;
    }

    public static void Attach(Carousel carousel, CarouselTrack track) => For(carousel).SetTrack(track);

    public int Stop => _stop;

    public int StopCount => _track?.Stops.Count ?? 0;

    private bool Arabic => PluginText.IsArabic(_carousel, CarouselOptions.GetLocale(_carousel));

    private void SetTrack(CarouselTrack track)
    {
        if (_track == track) return;
        if (_track is not null) _track.StopsChanged -= OnStopsChanged;
        _track = track;
        track.StopsChanged += OnStopsChanged;
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (_prev is not null) _prev.Click -= OnPrev;
        if (_next is not null) _next.Click -= OnNext;
        if (_trackBox is not null)
        {
            _trackBox.PointerPressed -= OnTrackPressed;
            _trackBox.PointerMoved -= OnTrackMoved;
            _trackBox.PointerReleased -= OnTrackReleased;
            _trackBox.PointerCaptureLost -= OnTrackCaptureLost;
        }
        _prev = e.NameScope.Find<Button>("PART_Prev");
        _next = e.NameScope.Find<Button>("PART_Next");
        _dots = e.NameScope.Find<Panel>("PART_Dots");
        _trackBox = e.NameScope.Find<Border>("PART_Track");
        if (_prev is not null) _prev.Click += OnPrev;
        if (_next is not null) _next.Click += OnNext;
        if (_trackBox is not null)
        {
            _trackBox.PointerPressed += OnTrackPressed;
            _trackBox.PointerMoved += OnTrackMoved;
            _trackBox.PointerReleased += OnTrackReleased;
            _trackBox.PointerCaptureLost += OnTrackCaptureLost;
        }
        Paint();
    }

    // Shift+wheel and a sideways wheel or trackpad move one stop per gesture; a plain wheel is the page's.
    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        var sideways = Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y);
        var h = sideways ? e.Delta.X : e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? e.Delta.Y : 0;
        if (h == 0 || StopCount < 2) return;
        e.Handled = true;
        _wheelQuiet?.Dispose();
        _wheelQuiet = DispatcherTimer.RunOnce(() => { _wheelLocked = false; _wheel = 0; }, TimeSpan.FromMilliseconds(250));
        if (_wheelLocked) return;
        _wheel += h;
        if (Math.Abs(_wheel) < 1) return;
        Go(_stop + (_wheel < 0 ? 1 : -1));
        _wheel = 0;
        _wheelLocked = true;
    }

    // A finger or a pen drags the row; past 15% of the width it snaps to the next or previous stop, otherwise it goes back. The mouse does not drag.
    private void OnTrackPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_track is null || e.Pointer.Type is not (PointerType.Touch or PointerType.Pen) || StopCount < 2) return;
        _dragStart = e.GetPosition(_track).X;
        _dragFrom = _track.Offset;
        _dragDx = 0;
        _dragging = true;
        e.Pointer.Capture(_trackBox);
    }

    private void OnTrackMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || _track is null) return;
        _dragDx = e.GetPosition(_track).X - _dragStart;
        _track.Jump(Math.Clamp(_dragFrom - _dragDx, 0, _track.Stops[^1]));
    }

    private void OnTrackReleased(object? sender, PointerReleasedEventArgs e) => EndDrag();

    private void OnTrackCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndDrag();

    private void EndDrag()
    {
        if (!_dragging || _track is null) return;
        _dragging = false;
        Go(CarouselOptions.SwipeTarget(_stop, _dragDx, _track.Bounds.Width, StopCount));
    }

    private void OnPrev(object? sender, RoutedEventArgs e) => Go(_stop - 1);

    private void OnNext(object? sender, RoutedEventArgs e) => Go(_stop + 1);

    private void OnStopsChanged(object? sender, EventArgs e)
    {
        if (_track is null) return;
        var at = Math.Clamp(Math.Min(_carousel.SelectedIndex, _track.Stops.Count - 1), 0, Math.Max(0, _track.Stops.Count - 1));
        _stop = at;
        if (_track.Stops.Count > 0) _track.Jump(_track.Stops[at]);
        Paint();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_moving || _track is null || _track.Stops.Count == 0) return;
        var index = Math.Max(0, _carousel.SelectedIndex);
        Go(Math.Min(index, _track.Stops.Count - 1), select: false);
    }

    /// <summary>Moves to a stop: the row slides there, the selection follows, and the buttons and dots with it.</summary>
    public int Go(int stop, bool select = true)
    {
        if (_track is null || _track.Stops.Count == 0) return 0;
        stop = Math.Clamp(stop, 0, _track.Stops.Count - 1);
        _stop = stop;
        _track.Offset = _track.Stops[stop];
        if (select && _carousel.SelectedIndex != stop)
        {
            _moving = true;
            try { _carousel.SelectedIndex = stop; }
            finally { _moving = false; }
        }
        Paint();
        return stop;
    }

    // Arrows go the way the page reads: in Arabic, ← is next.
    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is KeyModifiers.Alt or KeyModifiers.Control or KeyModifiers.Meta) return;
        if (e.Source is TextBox) return;
        var rtl = _carousel.FlowDirection == FlowDirection.RightToLeft;
        switch (e.Key)
        {
            case Key.Home: Go(0); break;
            case Key.End: Go(StopCount - 1); break;
            case Key.Right: Go(_stop + (rtl ? -1 : 1)); break;
            case Key.Left: Go(_stop + (rtl ? 1 : -1)); break;
            default: return;
        }
        e.Handled = true;
    }

    private void Paint()
    {
        var count = StopCount;
        var ar = Arabic;
        if (_prev is not null)
        {
            _prev.IsEnabled = _stop > 0;
            _prev.IsVisible = count > 1;
            Automation(_prev, ar ? "السابق" : "Previous");
        }
        if (_next is not null)
        {
            _next.IsEnabled = _stop < count - 1;
            _next.IsVisible = count > 1;
            Automation(_next, ar ? "التالي" : "Next");
        }
        if (_dots is null) return;
        if (_dots.Children.Count != count)
        {
            foreach (var old in _dots.Children.OfType<Button>()) old.Click -= OnDot;
            _dots.Children.Clear();
            for (var i = 0; i < count; i++)
            {
                var dot = new Button { Tag = i, Classes = { "carousel-dot" } };
                dot.Click += OnDot;
                _dots.Children.Add(dot);
            }
        }
        for (var i = 0; i < _dots.Children.Count; i++)
        {
            var dot = (Button)_dots.Children[i];
            dot.Classes.Set("current", i == _stop);
            Automation(dot, ar ? $"الشريحة {i + 1}" : $"Slide {i + 1}");
        }
        _dots.IsVisible = count > 1 && !CarouselOptions.GetHideDots(_carousel);
        var n = _carousel.ItemCount;
        for (var i = 0; i < n; i++)
            if (_carousel.ContainerFromIndex(i) is Control c && global::Avalonia.Automation.AutomationProperties.GetName(c) is null or "")
                Automation(c, ar ? $"{i + 1} من {n}" : $"{i + 1} of {n}");
    }

    private void OnDot(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int i }) Go(i);
    }

    private static void Automation(Control c, string name) => global::Avalonia.Automation.AutomationProperties.SetName(c, name);
}

/// <summary>The Insiyab carousel's options as attached properties: its language for the button and dot names, and whether it shows dots.</summary>
public static class CarouselOptions
{
    /// <summary>Set by the Carousel theme: hooks up the buttons, dots and keys.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Carousel, bool>("IsEnabled", typeof(CarouselOptions));

    public static readonly AttachedProperty<string?> LocaleProperty =
        AvaloniaProperty.RegisterAttached<Carousel, string?>("Locale", typeof(CarouselOptions));

    /// <summary>No dots under the row, the buttons only (<c>data-ins-carousel-dots="off"</c>; also <c>Classes="no-dots"</c>).</summary>
    public static readonly AttachedProperty<bool> HideDotsProperty =
        AvaloniaProperty.RegisterAttached<Carousel, bool>("HideDots", typeof(CarouselOptions));

    static CarouselOptions()
    {
        IsEnabledProperty.Changed.AddClassHandler<Carousel>((c, e) => { if (e.NewValue is true) CarouselNav.For(c); });
    }

    public static bool GetIsEnabled(Carousel c) => c.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Carousel c, bool value) => c.SetValue(IsEnabledProperty, value);
    public static string? GetLocale(Carousel c) => c.GetValue(LocaleProperty);
    public static void SetLocale(Carousel c, string? value) => c.SetValue(LocaleProperty, value);
    public static bool GetHideDots(Carousel c) => c.GetValue(HideDotsProperty);
    public static void SetHideDots(Carousel c, bool value) => c.SetValue(HideDotsProperty, value);

    /// <summary>The stop in view, 0-based: <c>Insiyab.carousel(el)</c>.</summary>
    public static int GetStop(Carousel c) => CarouselNav.For(c).Stop;

    /// <summary>How many stops the row has at its current width.</summary>
    public static int GetStopCount(Carousel c) => CarouselNav.For(c).StopCount;

    /// <summary>Moves to stop n: <c>Insiyab.carousel(el, n)</c>.</summary>
    public static int GoTo(Carousel c, int stop) => CarouselNav.For(c).Go(stop);

    // Where a touch drag of dx ends: the next stop (dx negative) or the previous, once past 15% of the width, else back where it began.
    internal static int SwipeTarget(int stop, double dx, double width, int count) =>
        Math.Clamp(Math.Abs(dx) > width * 0.15 ? stop + (dx < 0 ? 1 : -1) : stop, 0, Math.Max(0, count - 1));
}
