using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A surface that lifts under the pointer stays up while the pointer rests in the strip its lift uncovered, so a pointer on its bottom edge does not set it bobbing.</summary>
internal static class HoverLift
{
    /// <summary>Set by the hoverable look: the surface follows the pointer.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(HoverLift));

    /// <summary>Whether the surface is up: the pointer is over it, or over the box it had before it rose.</summary>
    public static readonly AttachedProperty<bool> IsLiftedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsLifted", typeof(HoverLift));

    private static Control? s_held;
    private static TopLevel? s_top;

    static HoverLift()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            c.PointerEntered -= OnEntered;
            c.PointerExited -= OnExited;
            c.DetachedFromVisualTree -= OnDetached;
            if (!e.GetNewValue<bool>())
            {
                Drop(c);
                return;
            }
            c.PointerEntered += OnEntered;
            c.PointerExited += OnExited;
            c.DetachedFromVisualTree += OnDetached;
        });
    }

    public static bool GetIsEnabled(Control c) => c.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control c, bool value) => c.SetValue(IsEnabledProperty, value);
    public static bool GetIsLifted(Control c) => c.GetValue(IsLiftedProperty);
    public static void SetIsLifted(Control c, bool value) => c.SetValue(IsLiftedProperty, value);

    private static void OnEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Control c) return;
        if (s_held is { } held)
        {
            if (held == c) Unhold();
            else Drop(held);
        }
        c.SetValue(IsLiftedProperty, true);
    }

    private static void OnExited(object? sender, PointerEventArgs e)
    {
        if (sender is not Control c) return;
        if (InLaidOutBox(c, e)) Hold(c);
        else Drop(c);
    }

    // The box the surface was laid out in, before its lift moved it: the pointer there still means the surface.
    private static bool InLaidOutBox(Control c, PointerEventArgs e) =>
        c.GetVisualParent() is Visual parent && c.Bounds.Contains(e.GetPosition(parent));

    private static void Hold(Control c)
    {
        Unhold();
        if (TopLevel.GetTopLevel(c) is not { } top)
        {
            Drop(c);
            return;
        }
        s_held = c;
        s_top = top;
        top.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.PointerExited += OnTopLeft;
    }

    private static void OnMoved(object? sender, PointerEventArgs e)
    {
        if (s_held is { } c && !InLaidOutBox(c, e)) Drop(c);
    }

    private static void OnTopLeft(object? sender, PointerEventArgs e)
    {
        if (s_held is { } c) Drop(c);
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control c) Drop(c);
    }

    private static void Drop(Control c)
    {
        if (s_held == c) Unhold();
        c.SetValue(IsLiftedProperty, false);
    }

    private static void Unhold()
    {
        if (s_top is { } top)
        {
            top.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
            top.PointerExited -= OnTopLeft;
        }
        s_held = null;
        s_top = null;
    }
}
