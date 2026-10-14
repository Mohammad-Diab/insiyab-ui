using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>What insiyab.js adds to a tab strip: the arrows follow the strip's direction (in RTL the left arrow is "next"), Home and End, wrap-around, and the selected face flying between tabs. The TabControl and TabStrip themes switch both on.</summary>
public static class Tabs
{
    /// <summary>Arrow, Home and End keys that select the tab they land on, and Ctrl+Tab, Ctrl+Shift+Tab, Ctrl+PageDown and Ctrl+PageUp from anywhere inside, skipping <see cref="TabLink"/>s and disabled tabs.</summary>
    public static readonly AttachedProperty<bool> KeysProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, bool>("Keys", typeof(Tabs));

    /// <summary>The selected face (or the underline of the <c>line</c> look) travels to the new tab: the far edge first, then the near edge. Needs a <c>PART_Flight</c> in a Canvas named <c>PART_FlightLayer</c>.</summary>
    public static readonly AttachedProperty<bool> FlyProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, bool>("Fly", typeof(Tabs));

    private static readonly AttachedProperty<MarkerFlight?> FlightProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, MarkerFlight?>("Flight", typeof(Tabs));

    // Set while a strip reshuffles its own items (DocumentTabs moving a tab), so the selection it restores does not fly.
    private static readonly AttachedProperty<bool> HushProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, bool>("Hush", typeof(Tabs));

    internal static void Hush(SelectingItemsControl strip, bool on) => strip.SetValue(HushProperty, on);

    static Tabs()
    {
        KeysProperty.Changed.AddClassHandler<SelectingItemsControl>((c, e) =>
        {
            if (e.GetNewValue<bool>()) c.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            else c.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        });
        FlyProperty.Changed.AddClassHandler<SelectingItemsControl>((c, e) =>
        {
            if (e.GetNewValue<bool>()) c.SelectionChanged += OnSelectionChanged;
            else c.SelectionChanged -= OnSelectionChanged;
        });
    }

    public static bool GetKeys(SelectingItemsControl element) => element.GetValue(KeysProperty);

    public static void SetKeys(SelectingItemsControl element, bool value) => element.SetValue(KeysProperty, value);

    public static bool GetFly(SelectingItemsControl element) => element.GetValue(FlyProperty);

    public static void SetFly(SelectingItemsControl element, bool value) => element.SetValue(FlyProperty, value);

    private static bool Selectable(Control? c) => c is { IsEnabled: true, IsVisible: true } and not TabLink;

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not SelectingItemsControl strip || e.Handled) return;
        if (OwnerOf(e.Source as Visual) != strip) return;
        var step = Chord(e);
        if (step != 0)
        {
            e.Handled = true;
            Move(strip, step);
            return;
        }
        if (e.KeyModifiers != KeyModifiers.None) return;
        var container = (e.Source as Visual)?.FindAncestorOfType<ContentControl>(true);
        while (container is not null && strip.IndexFromContainer(container) < 0) container = container.FindAncestorOfType<ContentControl>();
        if (container is null) return;
        var tabs = SelectableTabs(strip);
        if (tabs.Count == 0) return;
        var at = tabs.IndexOf(strip.IndexFromContainer(container));
        var rtl = strip.FlowDirection == FlowDirection.RightToLeft;
        int next;
        switch (e.Key)
        {
            case Key.Right: next = at < 0 ? 0 : rtl ? at - 1 : at + 1; break;
            case Key.Left: next = at < 0 ? 0 : rtl ? at + 1 : at - 1; break;
            case Key.Home: next = 0; break;
            case Key.End: next = tabs.Count - 1; break;
            default: return;
        }
        e.Handled = true;
        var index = tabs[(next + tabs.Count) % tabs.Count];
        strip.SelectedIndex = index;
        strip.ContainerFromIndex(index)?.Focus(NavigationMethod.Tab);
    }

    private static SelectingItemsControl? OwnerOf(Visual? source)
    {
        for (var v = source; v is not null; v = v.GetVisualParent())
            if (v is SelectingItemsControl s && GetKeys(s)) return s;
        return null;
    }

    private static int Chord(KeyEventArgs e) => (e.Key, e.KeyModifiers) switch
    {
        (Key.Tab, KeyModifiers.Control) => 1,
        (Key.Tab, KeyModifiers.Control | KeyModifiers.Shift) => -1,
        (Key.PageDown, KeyModifiers.Control) => 1,
        (Key.PageUp, KeyModifiers.Control) => -1,
        _ => 0,
    };

    private static List<int> SelectableTabs(SelectingItemsControl strip) =>
        Enumerable.Range(0, strip.ItemCount).Where(i => Selectable(strip.ContainerFromIndex(i))).ToList();

    private static void Move(SelectingItemsControl strip, int step)
    {
        var tabs = SelectableTabs(strip);
        if (tabs.Count == 0) return;
        var at = tabs.IndexOf(strip.SelectedIndex);
        var next = at < 0 ? (step > 0 ? 0 : tabs.Count - 1) : (at + step + tabs.Count) % tabs.Count;
        strip.SelectedIndex = tabs[next];
        strip.ContainerFromIndex(tabs[next])?.Focus(NavigationMethod.Tab);
    }

    private static void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not SelectingItemsControl strip || e.Source != strip || strip.GetValue(HushProperty)) return;
        if (e.RemovedItems.Count != 1 || e.AddedItems.Count != 1) return;
        var from = Container(strip, e.RemovedItems[0]);
        var to = Container(strip, e.AddedItems[0]);
        if (from is null || to is null || from == to) return;
        Fly(strip, from, to);
    }

    private static Control? Container(SelectingItemsControl strip, object? item) =>
        item as Control is { } c && strip.IndexFromContainer(c) >= 0 ? c : strip.ContainerFromItem(item!);

    private static void Fly(SelectingItemsControl strip, Control from, Control to)
    {
        if (strip.Bounds.Width <= 0) return;
        var layer = strip.GetVisualDescendants().OfType<Canvas>().FirstOrDefault(c => c.Name == "PART_FlightLayer");
        var face = layer?.Children.OfType<Border>().FirstOrDefault(c => c.Name == "PART_Flight");
        if (layer is null || face is null) return;
        var line = strip.Classes.Contains("line");
        var inset = line ? 10 : 0;
        var a = Span(from, layer, inset);
        var b = Span(to, layer, inset);
        if (a is null || b is null || to.TranslatePoint(default, layer) is not { } origin) return;
        var start = a.Value;
        if (strip.GetValue(FlightProperty) is { IsDone: false } previous)
        {
            start = (previous.Start, previous.Length);
            previous.Land();
        }
        MarkerFlight? run = null;
        run = MarkerFlight.Run(strip, start, b.Value, (x, w) =>
        {
            Canvas.SetLeft(face, x);
            face.Width = w;
        }, () =>
        {
            if (strip.GetValue(FlightProperty) != run) return;
            face.IsVisible = false;
            strip.ClearValue(FlightProperty);
            strip.Classes.Set("moving", false);
        });
        if (run is null) return;
        Canvas.SetTop(face, line ? origin.Y + to.Bounds.Height - 2 : origin.Y);
        if (!line) face.Height = to.Bounds.Height;
        face.IsVisible = true;
        strip.SetValue(FlightProperty, run);
        strip.Classes.Set("moving", true);
    }

    private static (double X, double W)? Span(Control tab, Visual layer, double inset)
    {
        if (tab.TranslatePoint(default, layer) is not { } p) return null;
        return (p.X + inset, tab.Bounds.Width - 2 * inset);
    }
}
