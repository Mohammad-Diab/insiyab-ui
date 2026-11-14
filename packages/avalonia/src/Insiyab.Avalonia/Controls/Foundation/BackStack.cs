using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>The order Back undoes things in: overlays first, then pages.</summary>
internal enum BackTier
{
    Overlay,
    Page,
}

/// <summary>What the system Back (Android's button and gesture, a mouse's back button) closes: the last library overlay opened on that TopLevel, one at a time, then the page stack; with nothing open it is left unhandled for the app and the system.</summary>
internal static class BackStack
{
    private sealed class Entry(object owner, BackTier tier, long order, Func<bool> close)
    {
        public object Owner { get; } = owner;
        public BackTier Tier { get; } = tier;
        public long Order { get; } = order;
        public Func<bool> Close { get; } = close;
    }

    private static readonly ConditionalWeakTable<TopLevel, List<Entry>> s_stacks = new();
    private static readonly ConditionalWeakTable<object, List<Entry>> s_owners = new();
    private static long s_order;
    private static bool _registered;

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        // A class handler runs before the handlers an app adds, so an app sees Back already handled while a library overlay took it.
        TopLevel.BackRequestedEvent.AddClassHandler<TopLevel>(OnBack);
        Popup.IsOpenProperty.Changed.AddClassHandler<Popup>((popup, e) => OnPopup(popup, e.GetNewValue<bool>()));
        FlyoutBase.IsOpenProperty.Changed.AddClassHandler<FlyoutBase>((flyout, e) => OnFlyout(flyout, e.GetNewValue<bool>()));
        SplitView.IsPaneOpenProperty.Changed.AddClassHandler<SplitView>((view, e) => OnPane(view, e.GetNewValue<bool>()));
    }

    /// <summary>Puts an open overlay (or a page stack) on its TopLevel's order, newest last; <paramref name="close"/> closes one layer and says whether it did.</summary>
    internal static void Push(Visual anchor, object owner, BackTier tier, Func<bool> close)
    {
        Remove(owner);
        if (OverlayHost.Top(anchor) is not { } top) return;
        var stack = s_stacks.GetValue(top, _ => []);
        var entry = new Entry(owner, tier, ++s_order, close);
        stack.Add(entry);
        s_owners.AddOrUpdate(owner, stack);
    }

    internal static void Remove(object owner)
    {
        if (!s_owners.TryGetValue(owner, out var stack)) return;
        stack.RemoveAll(e => ReferenceEquals(e.Owner, owner));
        s_owners.Remove(owner);
    }

    /// <summary>Undoes the newest thing open on the TopLevel; false when nothing was. An overlay found already closed leaves the order; a page stack at its root stays on it for its next page.</summary>
    internal static bool Back(TopLevel top)
    {
        if (!s_stacks.TryGetValue(top, out var stack)) return false;
        foreach (var entry in stack.Where(e => e.Tier == BackTier.Overlay).OrderByDescending(e => e.Order).ToList())
        {
            if (Try(entry)) return true;
            Remove(entry.Owner);
        }
        foreach (var entry in stack.Where(e => e.Tier == BackTier.Page).OrderByDescending(e => e.Order).ToList())
            if (Try(entry)) return true;
        return false;
    }

    private static bool Try(Entry entry)
    {
        try { return entry.Close(); }
        catch (Exception) { return false; }
    }

    private static void OnBack(TopLevel top, RoutedEventArgs e)
    {
        if (e.Handled) return;
        if (Back(top)) e.Handled = true;
    }

    // A flyout's popup is closed through its flyout, so a card can shrink back first; a tooltip is not a layer.
    private static void OnPopup(Popup popup, bool open)
    {
        if (!open)
        {
            Remove(popup);
            return;
        }
        if (popup.Child is ToolTip || popup.TemplatedParent is null && popup.Child is FlyoutPresenter or MenuFlyoutPresenter) return;
        Push(popup.PlacementTarget ?? (Visual)popup, popup, BackTier.Overlay, () =>
        {
            if (!popup.IsOpen || !(popup.IsLightDismissEnabled || popup.TemplatedParent is MenuItem)) return false;
            popup.Close();
            return true;
        });
    }

    private static void OnFlyout(FlyoutBase flyout, bool open)
    {
        if (!open)
        {
            Remove(flyout);
            return;
        }
        if (flyout.Target is not { } target) return;
        Push(target, flyout, BackTier.Overlay, () =>
        {
            if (!flyout.IsOpen) return false;
            flyout.Hide();
            return true;
        });
    }

    // The shell's sidebar while it is a drawer over the page.
    private static void OnPane(SplitView view, bool open)
    {
        if (!open || !Shell.IsDrawer(view))
        {
            Remove(view);
            return;
        }
        Push(view, view, BackTier.Overlay, () =>
        {
            if (!view.IsPaneOpen || !Shell.IsDrawer(view)) return false;
            Shell.Sidebar(view, SidebarState.Closed);
            return true;
        });
    }
}
