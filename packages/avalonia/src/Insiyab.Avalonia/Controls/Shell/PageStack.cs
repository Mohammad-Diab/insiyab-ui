using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>How the page on top of a <see cref="PageStack"/> changed.</summary>
public enum PageStackChange
{
    Pushed,
    Popped,
    Reset,
}

/// <summary>Carries the page a <see cref="PageStack"/> shows now, the one it left and how.</summary>
public sealed class PageStackChangedEventArgs : EventArgs
{
    internal PageStackChangedEventArgs(object? page, object? previous, PageStackChange change)
    {
        Page = page;
        Previous = previous;
        Change = change;
    }

    public object? Page { get; }

    public object? Previous { get; }

    public PageStackChange Change { get; }
}

/// <summary>Pages one over another, as a phone app moves into detail and back: <see cref="Push"/> slides a page in from the end side over the current one (from the left in right to left), and Back (Android's button or gesture, a mouse's back button), a swipe from the start edge or <see cref="Pop"/> slides it away, one page at a time and after any open overlay. A page is a control or any object shown through the data templates; the pages under the top one keep their state.</summary>
public class PageStack : Control
{
    /// <summary>The first page; setting it starts the stack again from it, at once.</summary>
    public static readonly StyledProperty<object?> RootProperty =
        AvaloniaProperty.Register<PageStack, object?>(nameof(Root));

    /// <summary>The ground each page is laid on, so the page under a moving one never shows through it.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<PageStack>();

    public static readonly DirectProperty<PageStack, object?> CurrentProperty =
        AvaloniaProperty.RegisterDirect<PageStack, object?>(nameof(Current), s => s.Current);

    public static readonly DirectProperty<PageStack, int> DepthProperty =
        AvaloniaProperty.RegisterDirect<PageStack, int>(nameof(Depth), s => s.Depth);

    public static readonly DirectProperty<PageStack, bool> CanGoBackProperty =
        AvaloniaProperty.RegisterDirect<PageStack, bool>(nameof(CanGoBack), s => s.CanGoBack);

    // How far in from the start edge a finger may begin a swipe back.
    internal const double EdgeWidth = 24;

    // The share of the width the page under moves while the top one slides.
    private const double Under = 0.3;

    private readonly List<Frame> _frames = [];
    private object? _current;
    private int _depth;
    private bool _canGoBack;
    private bool _resetting;
    private FrameTween? _motion;
    private Swipe? _swipe;

    static PageStack()
    {
        ClipToBoundsProperty.OverrideDefaultValue<PageStack>(true);
        FocusableProperty.OverrideDefaultValue<PageStack>(false);
    }

    public PageStack()
    {
        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    [Content]
    public object? Root { get => GetValue(RootProperty); set => SetValue(RootProperty, value); }

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The page on top, the one shown.</summary>
    public object? Current { get => _current; private set => SetAndRaise(CurrentProperty, ref _current, value); }

    /// <summary>How many pages are in the stack, the root included.</summary>
    public int Depth { get => _depth; private set => SetAndRaise(DepthProperty, ref _depth, value); }

    /// <summary>Whether there is a page under the current one to go back to.</summary>
    public bool CanGoBack { get => _canGoBack; private set => SetAndRaise(CanGoBackProperty, ref _canGoBack, value); }

    /// <summary>The pages from the root to the top.</summary>
    public IReadOnlyList<object> Pages => _frames.Where(f => !f.Leaving).Select(f => f.Page).ToList();

    /// <summary>Raised when the page on top changes, as the change starts.</summary>
    public event EventHandler<PageStackChangedEventArgs>? Changed;

    /// <summary>Opens <paramref name="page"/> over the current one.</summary>
    public void Push(object page)
    {
        ArgumentNullException.ThrowIfNull(page);
        Settle();
        var under = Top();
        var hadFocus = under is not null && HasFocusIn(under);
        if (under is not null) under.Focused = FocusedIn(under);
        var frame = Add(page);
        Changed?.Invoke(this, new PageStackChangedEventArgs(page, under?.Page, PageStackChange.Pushed));
        if (under is null) return;
        Slide(under, frame, 0, 1, () => under.IsVisible = false);
        if (hadFocus) Dispatcher.UIThread.Post(() => FocusInto(frame), DispatcherPriority.Loaded);
    }

    /// <summary>Goes back to the page under the current one; false at the root.</summary>
    public bool Pop()
    {
        Settle();
        var live = _frames.Where(f => !f.Leaving).ToList();
        if (live.Count < 2) return false;
        var top = live[^1];
        var under = live[^2];
        Leave(top, under);
        return true;
    }

    /// <summary>Goes back to the root, sliding the top page away over it and dropping those between.</summary>
    public void PopToRoot()
    {
        Settle();
        var live = _frames.Where(f => !f.Leaving).ToList();
        if (live.Count < 2) return;
        foreach (var between in live.Skip(1).Take(live.Count - 2)) Remove(between);
        Leave(live[^1], live[0]);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RootProperty && !_resetting) Reset(change.NewValue);
        else if (change.Property == BackgroundProperty)
            foreach (var f in _frames) f.Background = Background;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        BackStack.Push(this, this, BackTier.Page, () => IsEffectivelyVisible && IsEffectivelyEnabled && Pop());
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        BackStack.Remove(this);
        _swipe = null;
        Settle();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = default(Size);
        foreach (var f in _frames)
        {
            f.Measure(availableSize);
            size = new Size(Math.Max(size.Width, f.DesiredSize.Width), Math.Max(size.Height, f.DesiredSize.Height));
        }
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var f in _frames) f.Arrange(new Rect(finalSize));
        return finalSize;
    }

    private void Reset(object? root)
    {
        Settle();
        var previous = Top()?.Page;
        foreach (var f in _frames.ToList()) Remove(f);
        if (root is not null) Add(root);
        else Update();
        Changed?.Invoke(this, new PageStackChangedEventArgs(root, previous, PageStackChange.Reset));
    }

    private Frame Add(object page)
    {
        var frame = new Frame(page) { Background = Background };
        _frames.Add(frame);
        LogicalChildren.Add(frame);
        VisualChildren.Add(frame);
        InvalidateMeasure();
        Update();
        return frame;
    }

    private void Remove(Frame frame)
    {
        _frames.Remove(frame);
        VisualChildren.Remove(frame);
        LogicalChildren.Remove(frame);
        frame.Content = null;
        InvalidateMeasure();
        Update();
    }

    private Frame? Top() => _frames.LastOrDefault(f => !f.Leaving);

    private void Update()
    {
        var live = _frames.Where(f => !f.Leaving).ToList();
        Current = live.Count > 0 ? live[^1].Page : null;
        Depth = live.Count;
        CanGoBack = live.Count > 1;
        if (live.Count > 0 && !Equals(Root, live[0].Page))
        {
            _resetting = true;
            try { SetCurrentValue(RootProperty, live[0].Page); }
            finally { _resetting = false; }
        }
    }

    private void Leave(Frame top, Frame under)
    {
        var hadFocus = HasFocusIn(top);
        top.Leaving = true;
        under.IsVisible = true;
        Update();
        Changed?.Invoke(this, new PageStackChangedEventArgs(under.Page, top.Page, PageStackChange.Popped));
        Slide(under, top, _swipe is { Dragging: true } ? Progress(top) : 1, 0, () => Remove(top));
        if (hadFocus) Restore(under);
    }

    // Puts the motion in hand at its end.
    private void Settle()
    {
        var motion = _motion;
        _motion = null;
        motion?.Land();
    }

    // The top page sits a share p of the way in from the end side; the page under moves a little toward the start as it is covered.
    private void Lay(Frame under, Frame top, double p)
    {
        var w = Bounds.Width;
        top.RenderTransform = Move((1 - p) * w);
        under.RenderTransform = Move(-Under * p * w);
    }

    private static TransformOperations Move(double x)
    {
        var b = TransformOperations.CreateBuilder(1);
        b.AppendTranslate(x, 0);
        return b.Build();
    }

    private double Progress(Frame top)
    {
        var w = Bounds.Width;
        if (w <= 0 || top.RenderTransform is not TransformOperations ops) return 1;
        return Math.Clamp(1 - ops.Value.M31 / w, 0, 1);
    }

    private void Slide(Frame under, Frame top, double from, double to, Action landed)
    {
        Settle();
        top.Moving(true);
        under.IsHitTestVisible = top.IsHitTestVisible = false;
        var distance = Math.Abs(to - from);
        var duration = TimeSpan.FromMilliseconds(Math.Max(120, Motion.Enter(this).TotalMilliseconds * distance));
        FrameTween? run = null;
        run = FrameTween.Run(this, duration, Motion.Ease(this), e => Lay(under, top, from + (to - from) * e), () =>
        {
            if (_motion == run) _motion = null;
            top.Moving(false);
            top.RenderTransform = null;
            under.RenderTransform = null;
            under.IsHitTestVisible = top.IsHitTestVisible = true;
            landed();
        });
        _motion = run;
    }

    private bool HasFocusIn(Visual frame) =>
        TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is Visual focused && frame.IsVisualAncestorOf(focused);

    private Control? FocusedIn(Visual frame) =>
        TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is Control focused && frame.IsVisualAncestorOf(focused) ? focused : null;

    private static void FocusInto(Frame frame)
    {
        if (!frame.IsEffectivelyVisible) return;
        if (FocusManager.FindFirstFocusableElement(frame) is Control first) first.Focus(NavigationMethod.Tab);
    }

    private static void Restore(Frame frame)
    {
        if (frame.Focused is { IsEffectivelyVisible: true } was && frame.IsVisualAncestorOf(was)) was.Focus(NavigationMethod.Tab);
        else FocusInto(frame);
        frame.Focused = null;
    }

    // A finger from the start edge drags the top page back; past a third of the width, or flung, it goes.
    private sealed class Swipe(IPointer pointer, Point start, ulong time)
    {
        public IPointer Pointer { get; } = pointer;
        public Point Start { get; } = start;
        public bool Dragging { get; set; }
        public Point Last { get; set; } = start;
        public ulong LastTime { get; set; } = time;
        public double Velocity { get; set; }
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        _swipe = null;
        if (e.Pointer.Type != PointerType.Touch || !CanGoBack || _motion is not null) return;
        var at = e.GetPosition(this);
        if (at.X < 0 || at.X > EdgeWidth) return;
        _swipe = new Swipe(e.Pointer, at, e.Timestamp);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_swipe is not { } s || s.Pointer != e.Pointer) return;
        var at = e.GetPosition(this);
        var d = at - s.Start;
        if (!s.Dragging)
        {
            if (Math.Abs(d.X) < 6 && Math.Abs(d.Y) < 6) return;
            if (d.X <= 0 || Math.Abs(d.X) < Math.Abs(d.Y) || _frames.Count(f => !f.Leaving) < 2)
            {
                _swipe = null;
                return;
            }
            s.Dragging = true;
            e.Pointer.Capture(this);
            var live = _frames.Where(f => !f.Leaving).ToList();
            live[^2].IsVisible = true;
            live[^1].Moving(true);
        }
        e.PreventGestureRecognition();
        e.Handled = true;
        var dt = e.Timestamp > s.LastTime ? (e.Timestamp - s.LastTime) / 1000.0 : 0;
        if (dt > 0) s.Velocity = (at.X - s.Last.X) / dt;
        s.Last = at;
        s.LastTime = e.Timestamp;
        var frames = _frames.Where(f => !f.Leaving).ToList();
        var w = Math.Max(1, Bounds.Width);
        Lay(frames[^2], frames[^1], 1 - Math.Clamp(d.X / w, 0, 1));
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_swipe is not { Dragging: true } s || s.Pointer != e.Pointer) return;
        e.Handled = true;
        var d = e.GetPosition(this).X - s.Start.X;
        Finish(d > Bounds.Width / 3 || s.Velocity > 500);
        e.Pointer.Capture(null);
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_swipe is { Dragging: true } s && s.Pointer == e.Pointer) Finish(false);
    }

    private void Finish(bool back)
    {
        if (_swipe is not { Dragging: true }) return;
        var live = _frames.Where(f => !f.Leaving).ToList();
        if (live.Count < 2)
        {
            _swipe = null;
            return;
        }
        if (back)
        {
            Leave(live[^1], live[^2]);
            _swipe = null;
            return;
        }
        var top = live[^1];
        var under = live[^2];
        var from = Progress(top);
        _swipe = null;
        Slide(under, top, from, 1, () => under.IsVisible = false);
    }

    // One page and its ground.
    private sealed class Frame : ContentPresenter
    {
        public Frame(object page)
        {
            Page = page;
            Content = page;
            HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
        }

        public object Page { get; }

        public bool Leaving { get; set; }

        // What had the keyboard on this page when another covered it.
        public Control? Focused { get; set; }

        // A page in motion casts its shadow on the one under it.
        public void Moving(bool on) =>
            BoxShadow = on && this.TryFindResource("InsShLg", ActualThemeVariant, out var v) && v is BoxShadows s ? s : default;
    }
}
