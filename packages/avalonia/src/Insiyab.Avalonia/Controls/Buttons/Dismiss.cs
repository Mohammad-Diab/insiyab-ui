using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A button that closes what it sits in, the web's <c>data-ins-dismiss</c>: <c>Nearest="True"</c> closes the nearest window, popup, <see cref="Alert"/> or element classed <c>alert</c>, <c>dialog</c> or <c>drawer</c>; <c>Target</c> names one instead.</summary>
public static class Dismiss
{
    public static readonly AttachedProperty<bool> NearestProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("Nearest", typeof(Dismiss));

    public static readonly AttachedProperty<Control?> TargetProperty =
        AvaloniaProperty.RegisterAttached<Button, Control?>("Target", typeof(Dismiss));

    /// <summary>Raised on what was closed, and bubbles from there: the web's <c>ins:dismiss</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> DismissedEvent =
        RoutedEvent.Register<RoutedEventArgs>("Dismissed", RoutingStrategies.Bubble, typeof(Dismiss));

    private static readonly string[] Closable = ["alert", "dialog", "drawer"];

    static Dismiss()
    {
        Button.ClickEvent.AddClassHandler<Button>((b, _) =>
        {
            var target = GetTarget(b) ?? (GetNearest(b) ? FindNearest(b) : null);
            if (target is not null) Close(target);
        });
    }

    public static bool GetNearest(Button element) => element.GetValue(NearestProperty);
    public static void SetNearest(Button element, bool value) => element.SetValue(NearestProperty, value);

    public static Control? GetTarget(Button element) => element.GetValue(TargetProperty);
    public static void SetTarget(Button element, Control? value) => element.SetValue(TargetProperty, value);

    public static void AddDismissedHandler(Interactive element, EventHandler<RoutedEventArgs> handler) => element.AddHandler(DismissedEvent, handler);
    public static void RemoveDismissedHandler(Interactive element, EventHandler<RoutedEventArgs> handler) => element.RemoveHandler(DismissedEvent, handler);

    private static Control? FindNearest(Control from)
    {
        foreach (var v in from.GetVisualAncestors())
        {
            if (v is Window or Popup or Alert) return (Control)v;
            if (v is PopupRoot root && root.Parent is Popup owner) return owner;
            if (v is Control c && Array.Exists(Closable, c.Classes.Contains)) return c;
        }
        return null;
    }

    private static void Close(Control target)
    {
        target.RaiseEvent(new RoutedEventArgs(DismissedEvent, target));
        switch (target)
        {
            case Window w: w.Close(); break;
            case Popup p: p.IsOpen = false; break;
            case Alert a: a.Dismiss(); break;
            default: target.IsVisible = false; break;
        }
    }
}
