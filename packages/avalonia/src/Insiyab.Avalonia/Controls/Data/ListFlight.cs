using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A master list's selection mark flies from the old row to the new one as the sidebar's does; the rows' own marks wait while it flies.</summary>
internal static class ListFlight
{
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsOn", typeof(ListFlight));

    private static readonly AttachedProperty<MarkerFlight?> FlightProperty =
        AvaloniaProperty.RegisterAttached<ListBox, MarkerFlight?>("Flight", typeof(ListFlight));

    static ListFlight()
    {
        IsOnProperty.Changed.AddClassHandler<ListBox>((list, e) =>
        {
            list.SelectionChanged -= OnSelectionChanged;
            if (e.GetNewValue<bool>()) list.SelectionChanged += OnSelectionChanged;
            else list.GetValue(FlightProperty)?.Land();
        });
    }

    public static bool GetIsOn(ListBox list) => list.GetValue(IsOnProperty);
    public static void SetIsOn(ListBox list, bool value) => list.SetValue(IsOnProperty, value);

    private static void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox list || e.Source != list || e.RemovedItems.Count != 1 || e.AddedItems.Count != 1) return;
        Fly(list, list.ContainerFromItem(e.RemovedItems[0]!), list.ContainerFromItem(e.AddedItems[0]!));
    }

    internal static bool Fly(ListBox list, Control? from, Control? to)
    {
        if (from is null || to is null || from == to) return false;
        var layer = list.GetVisualDescendants().OfType<Canvas>().FirstOrDefault(c => c.Name == "PART_Flight" && c.TemplatedParent == list);
        if (layer is null || MarkSpan(from, layer) is not { } a || MarkSpan(to, layer) is not { } b) return false;
        var start = (a.Y, a.Height);
        if (list.GetValue(FlightProperty) is { IsDone: false } previous)
        {
            start = (previous.Start, previous.Length);
            previous.Land();
        }
        var bar = new Border { Classes = { "table-marker-flight" }, IsHitTestVisible = false };
        Canvas.SetLeft(bar, b.X);
        MarkerFlight? run = null;
        run = MarkerFlight.Run(list, start, (b.Y, b.Height), (y, h) =>
        {
            Canvas.SetTop(bar, y);
            bar.Height = h;
        }, () =>
        {
            layer.Children.Remove(bar);
            if (list.GetValue(FlightProperty) != run) return;
            list.ClearValue(FlightProperty);
            list.ClearValue(ListLook.MovingProperty);
        });
        if (run is null) return false;
        layer.Children.Add(bar);
        list.SetValue(FlightProperty, run);
        list.SetValue(ListLook.MovingProperty, true);
        return true;
    }

    // A row's mark in the flight layer, where its background draws it.
    private static Rect? MarkSpan(Control row, Visual layer)
    {
        if (row.GetVisualDescendants().OfType<RowBack>().FirstOrDefault() is not { } back) return null;
        var m = back.MarkRect;
        if (m.Height <= 0 || back.TranslatePoint(m.TopLeft, layer) is not { } p) return null;
        return new Rect(p.X, p.Y, m.Width, m.Height);
    }
}
