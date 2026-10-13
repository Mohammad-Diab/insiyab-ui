using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The application shell's behaviour as attached properties on plain controls: a SplitView is the shell, a <c>Border.topbar</c> holds the islands, a <c>ListBox.shell-nav</c> holds the links.</summary>
public static class Shell
{
    /// <summary>Below this width of its own the SplitView becomes a drawer (Overlay, closed); at or above it, a column (Inline). 0 turns the switch off. The Insiyab SplitView theme sets 900, the web's breakpoint. While on, the shell also takes the class <c>narrow</c> at 640px or less and <c>xnarrow</c> at 544px or less, the web's phone rules.</summary>
    public static readonly AttachedProperty<double> DrawerWidthProperty =
        AvaloniaProperty.RegisterAttached<SplitView, double>("DrawerWidth", typeof(Shell));

    /// <summary>On a SplitView: the sidebar's head scrolls away with its links, as on the web, instead of staying pinned above them while only the links scroll.</summary>
    public static readonly AttachedProperty<bool> HeadScrollsProperty =
        AvaloniaProperty.RegisterAttached<SplitView, bool>("HeadScrolls", typeof(Shell));

    /// <summary>On a Button: a click collapses or restores the nearest shell's sidebar, or opens and closes it while it is a drawer (the web's <c>data-ins-sidebar</c>).</summary>
    public static readonly AttachedProperty<bool> TogglesSidebarProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("TogglesSidebar", typeof(Shell));

    /// <summary>On a Button with <see cref="TogglesSidebarProperty"/>: what the click asks for, "toggle" (unset), "open", "collapsed" or "closed" (the value of the web's <c>data-ins-sidebar</c>).</summary>
    public static readonly AttachedProperty<string?> SidebarActionProperty =
        AvaloniaProperty.RegisterAttached<Button, string?>("SidebarAction", typeof(Shell));

    /// <summary>On a ScrollViewer: past 8px of scroll the nearest <c>.topbar</c> beside it takes the <c>scrolled</c> class and frosts.</summary>
    public static readonly AttachedProperty<bool> FrostsTopbarProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("FrostsTopbar", typeof(Shell));

    /// <summary>On a ListBox whose template has a <c>Canvas#PART_Flight</c>: a change of selection flies the marker from the old item to the new one. Set by the <c>shell-nav</c> theme.</summary>
    public static readonly AttachedProperty<bool> FlyMarkerProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("FlyMarker", typeof(Shell));


    /// <summary>Raised on the shell each time <see cref="Sidebar"/> changes it (the web's <c>ins:sidebar</c>).</summary>
    public static readonly RoutedEvent<SidebarChangedEventArgs> SidebarChangedEvent =
        RoutedEvent.Register<SplitView, SidebarChangedEventArgs>("SidebarChanged", RoutingStrategies.Bubble);

    public static void AddSidebarChangedHandler(Interactive element, EventHandler<SidebarChangedEventArgs> handler) => element.AddHandler(SidebarChangedEvent, handler);
    public static void RemoveSidebarChangedHandler(Interactive element, EventHandler<SidebarChangedEventArgs> handler) => element.RemoveHandler(SidebarChangedEvent, handler);

    /// <summary>On a title island: it fades and settles in when it arrives and again each time its <c>TextBlock.shell-title</c> or its content changes (the web's <c>ins-island-in</c> on every page). Set by the topbar theme.</summary>
    public static readonly AttachedProperty<bool> EntersProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Enters", typeof(Shell));

    private static readonly AttachedProperty<CancellationTokenSource?> EntranceProperty =
        AvaloniaProperty.RegisterAttached<Control, CancellationTokenSource?>("Entrance", typeof(Shell));

    private static readonly AttachedProperty<IDisposable?> WatchProperty =
        AvaloniaProperty.RegisterAttached<Control, IDisposable?>("Watch", typeof(Shell));

    private static readonly AttachedProperty<bool> IsDrawerModeProperty =
        AvaloniaProperty.RegisterAttached<SplitView, bool>("IsDrawerMode", typeof(Shell));

    private static readonly AttachedProperty<bool> WideOpenProperty =
        AvaloniaProperty.RegisterAttached<SplitView, bool>("WideOpen", typeof(Shell), true);

    private static readonly AttachedProperty<Flight?> FlightProperty =
        AvaloniaProperty.RegisterAttached<ListBox, Flight?>("Flight", typeof(Shell));

    /// <summary>Turns a SplitView's OpenPaneLength into the drawer's closed offset, a little past its own width.</summary>
    public static IValueConverter DrawerOffset { get; } = new FuncValueConverter<double, ITransform>(len =>
        TransformOperations.Parse(string.Create(CultureInfo.InvariantCulture, $"translateX({-(len + 16)}px)")));

    /// <summary>The same offset for a pane placed on the end side, which slides out the other way.</summary>
    public static IValueConverter DrawerOffsetEnd { get; } = new FuncValueConverter<double, ITransform>(len =>
        TransformOperations.Parse(string.Create(CultureInfo.InvariantCulture, $"translateX({len + 16}px)")));

    static Shell()
    {
        DrawerWidthProperty.Changed.AddClassHandler<SplitView>((view, _) => WatchWidth(view));
        TogglesSidebarProperty.Changed.AddClassHandler<Button>((button, e) =>
        {
            button.Click -= OnToggleClick;
            if (e.GetNewValue<bool>()) button.Click += OnToggleClick;
        });
        FrostsTopbarProperty.Changed.AddClassHandler<ScrollViewer>((scroller, e) =>
        {
            scroller.ScrollChanged -= OnScrollChanged;
            if (e.GetNewValue<bool>()) scroller.ScrollChanged += OnScrollChanged;
        });
        FlyMarkerProperty.Changed.AddClassHandler<ListBox>((list, e) =>
        {
            list.SelectionChanged -= OnNavSelectionChanged;
            if (e.GetNewValue<bool>()) list.SelectionChanged += OnNavSelectionChanged;
        });
        EntersProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            c.AttachedToVisualTree -= OnEnterAttached;
            if (e.GetNewValue<bool>()) c.AttachedToVisualTree += OnEnterAttached;
        });
        TextBlock.TextProperty.Changed.AddClassHandler<TextBlock>((t, e) =>
        {
            if (t.Classes.Contains("shell-title") && !Equals(e.OldValue, e.NewValue) && t.GetVisualAncestors().OfType<Control>().FirstOrDefault(GetEnters) is { } island) Replay(island);
        });
        ContentControl.ContentProperty.Changed.AddClassHandler<ContentControl>((c, e) =>
        {
            if (GetEnters(c) && !Equals(e.OldValue, e.NewValue)) Replay(c);
        });
    }

    public static double GetDrawerWidth(SplitView view) => view.GetValue(DrawerWidthProperty);
    public static void SetDrawerWidth(SplitView view, double value) => view.SetValue(DrawerWidthProperty, value);
    public static bool GetHeadScrolls(SplitView view) => view.GetValue(HeadScrollsProperty);
    public static void SetHeadScrolls(SplitView view, bool value) => view.SetValue(HeadScrollsProperty, value);
    public static bool GetTogglesSidebar(Button button) => button.GetValue(TogglesSidebarProperty);
    public static void SetTogglesSidebar(Button button, bool value) => button.SetValue(TogglesSidebarProperty, value);
    public static bool GetEnters(Control control) => control.GetValue(EntersProperty);
    public static void SetEnters(Control control, bool value) => control.SetValue(EntersProperty, value);
    public static string? GetSidebarAction(Button button) => button.GetValue(SidebarActionProperty);
    public static void SetSidebarAction(Button button, string? value) => button.SetValue(SidebarActionProperty, value);
    public static bool GetFrostsTopbar(ScrollViewer scroller) => scroller.GetValue(FrostsTopbarProperty);
    public static void SetFrostsTopbar(ScrollViewer scroller, bool value) => scroller.SetValue(FrostsTopbarProperty, value);
    public static bool GetFlyMarker(ListBox list) => list.GetValue(FlyMarkerProperty);
    public static void SetFlyMarker(ListBox list, bool value) => list.SetValue(FlyMarkerProperty, value);

    /// <summary>Whether the shell is in its drawer form right now.</summary>
    public static bool IsDrawer(SplitView view) => view.GetValue(IsDrawerModeProperty);

    /// <summary>The web's <c>Insiyab.sidebar()</c>: "toggle", "open" or "collapsed" (a drawer takes "closed" too). Returns the new state.</summary>
    public static string Sidebar(SplitView view, string next = "toggle")
    {
        var open = next switch
        {
            "open" => true,
            "collapsed" or "closed" => false,
            _ => !view.IsPaneOpen,
        };
        view.SetCurrentValue(SplitView.IsPaneOpenProperty, open);
        if (!IsDrawer(view)) view.SetValue(WideOpenProperty, open);
        var state = open ? "open" : IsDrawer(view) ? "closed" : "collapsed";
        view.RaiseEvent(new SidebarChangedEventArgs(SidebarChangedEvent, state, IsDrawer(view)));
        return state;
    }

    /// <summary>The shell a control belongs to: the SplitView around it, or else the first one in its window.</summary>
    public static SplitView? Find(Visual from) =>
        from.FindAncestorOfType<SplitView>()
        ?? (TopLevel.GetTopLevel(from) as Visual)?.GetVisualDescendants().OfType<SplitView>().FirstOrDefault();

    private static void OnToggleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Visual button && Find(button) is { } view) Sidebar(view, sender is Button b ? GetSidebarAction(b) ?? "toggle" : "toggle");
    }

    private static void WatchWidth(SplitView view)
    {
        view.GetValue(WatchProperty)?.Dispose();
        view.SetValue(WatchProperty, view.GetObservable(Visual.BoundsProperty).Subscribe(new Observer<Rect>(_ => ApplyWidth(view))));
        ApplyWidth(view);
    }

    private static void ApplyWidth(SplitView view)
    {
        var w = view.Bounds.Width;
        var on = GetDrawerWidth(view) > 0 && w > 0;
        view.Classes.Set("narrow", on && w <= 640);
        view.Classes.Set("xnarrow", on && w <= 544);
        var limit = GetDrawerWidth(view);
        var width = view.Bounds.Width;
        var drawer = limit > 0 && width > 0 && width < limit;
        if (drawer == IsDrawer(view)) return;
        view.SetValue(IsDrawerModeProperty, drawer);
        if (drawer)
        {
            view.SetValue(WideOpenProperty, view.IsPaneOpen);
            view.SetCurrentValue(SplitView.IsPaneOpenProperty, false);
            view.SetCurrentValue(SplitView.DisplayModeProperty, SplitViewDisplayMode.Overlay);
        }
        else
        {
            view.SetCurrentValue(SplitView.DisplayModeProperty, SplitViewDisplayMode.Inline);
            view.SetCurrentValue(SplitView.IsPaneOpenProperty, view.GetValue(WideOpenProperty));
        }
    }

    private static void OnEnterAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control c) Enter(c);
    }

    // A new page title while the island is on screen plays its entrance again.
    private static void Replay(Control island)
    {
        if (TopLevel.GetTopLevel(island) is not null) Enter(island);
    }

    private static void Enter(Control c)
    {
        c.GetValue(EntranceProperty)?.Cancel();
        c.SetValue(EntranceProperty, null);
        if (Motion.IsStill(c)) return;
        var run = new CancellationTokenSource();
        c.SetValue(EntranceProperty, run);
        TransformOperationsAnimator.Ensure();
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(280),
            Easing = new SplineEasing(0.22, 1, 0.36, 1),
            FillMode = FillMode.Backward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 0d), new Setter(Visual.RenderTransformProperty, TransformOperations.Parse("scale(0.88) translateY(-6px)")) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 1d), new Setter(Visual.RenderTransformProperty, TransformOperations.Identity) } },
            },
        };
        _ = RunEntrance(animation, c, run);
    }

    private static async Task RunEntrance(Animation animation, Control c, CancellationTokenSource run)
    {
        try { await animation.RunAsync(c, run.Token); }
        catch (OperationCanceledException) { }
        if (c.GetValue(EntranceProperty) == run) c.SetValue(EntranceProperty, null);
    }

    private static void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scroller) return;
        var bar = FindTopbar(scroller);
        bar?.Classes.Set("scrolled", scroller.Offset.Y > 8);
    }

    private static Control? FindTopbar(Control from)
    {
        for (var node = from.Parent; node is not null; node = node.Parent)
            if (node is Panel panel)
                foreach (var child in panel.Children)
                    if (child != from && child.Classes.Contains("topbar")) return child;
        return null;
    }

    private static readonly KeySpline EaseOut = new(0.9, 0.1, 1, 0.2);
    private static readonly KeySpline EaseIn = new(0.1, 0.9, 0.2, 1);

    private static void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox list || e.RemovedItems.Count == 0 || e.AddedItems.Count == 0) return;
        var from = list.ContainerFromItem(e.RemovedItems[0]!);
        var to = list.ContainerFromItem(e.AddedItems[0]!);
        Fly(list, from, to);
    }

    // The marker's rectangle in the flight layer: 3px wide on the item's start edge, inset 8px top and bottom.
    private static Rect MarkerRect(Control item, Visual layer)
    {
        var p = item.TranslatePoint(new Point(0, 8), layer) ?? default;
        return new Rect(p.X, p.Y, 3, Math.Max(0, item.Bounds.Height - 16));
    }

    /// <summary>Flies a stand-in bar from one item's marker to another's: it stretches to reach the new item, then lets go of the old end.</summary>
    internal static bool Fly(ListBox list, Control? from, Control? to)
    {
        if (from is null || to is null || from == to) return false;
        var layer = list.GetVisualDescendants().OfType<Canvas>().FirstOrDefault(c => c.Name == "PART_Flight");
        if (layer is null || list.Bounds.Width < 8) return false;
        var a = MarkerRect(from, layer);
        var b = MarkerRect(to, layer);
        if (a.Height <= 0 || b.Height <= 0) return false;

        var prev = list.GetValue(FlightProperty);
        if (prev is not null)
        {
            if (prev.Bar.Parent is not null && prev.Bar.Height > 0) a = new Rect(a.X, Canvas.GetTop(prev.Bar), 3, prev.Bar.Height);
            prev.Land();
        }

        var bar = new Border { Classes = { "shell-indicator" }, Width = b.Width, Height = a.Height, IsHitTestVisible = false };
        Canvas.SetLeft(bar, b.X);
        Canvas.SetTop(bar, a.Y);
        layer.Children.Add(bar);
        list.Classes.Add("moving");

        var down = b.Y > a.Y;
        var spanTop = down ? a.Y : b.Y;
        var spanHeight = down ? b.Bottom - a.Y : a.Bottom - b.Y;
        var flight = new Flight(list, layer, bar);
        list.SetValue(FlightProperty, flight);
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(600),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Canvas.TopProperty, a.Y), new Setter(Layoutable.HeightProperty, a.Height) } },
                new KeyFrame { Cue = new Cue(1d / 3), KeySpline = EaseOut, Setters = { new Setter(Canvas.TopProperty, spanTop), new Setter(Layoutable.HeightProperty, spanHeight) } },
                new KeyFrame { Cue = new Cue(1), KeySpline = EaseIn, Setters = { new Setter(Canvas.TopProperty, b.Y), new Setter(Layoutable.HeightProperty, b.Height) } },
            },
        };
        _ = RunFlight(animation, flight);
        return true;
    }

    private static async Task RunFlight(Animation animation, Flight flight)
    {
        try { await animation.RunAsync(flight.Bar, flight.Cancel.Token); }
        catch (OperationCanceledException) { }
        flight.Land();
    }

    private sealed class Flight(ListBox list, Canvas layer, Border bar)
    {
        public Border Bar { get; } = bar;
        public CancellationTokenSource Cancel { get; } = new();

        public void Land()
        {
            if (!Cancel.IsCancellationRequested) Cancel.Cancel();
            layer.Children.Remove(Bar);
            if (list.GetValue(FlightProperty) != this) return;
            list.SetValue(FlightProperty, null);
            list.Classes.Remove("moving");
        }
    }

    private sealed class Observer<T>(Action<T> next) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => next(value);
    }
}

/// <summary>The sidebar's new state, "open", "collapsed" or "closed", and whether the shell is a drawer.</summary>
public sealed class SidebarChangedEventArgs(RoutedEvent routedEvent, string state, bool isDrawer) : RoutedEventArgs(routedEvent)
{
    public string State { get; } = state;
    public bool IsDrawer { get; } = isDrawer;
}
