using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>A count on the app's taskbar button (the Dock icon on macOS), set on a window; Linux, the browser and single views show nothing and never fail.</summary>
public static class Taskbar
{
    /// <summary>On a window: the count on its taskbar button; 0 clears it and anything above 99 shows 99+.</summary>
    public static readonly AttachedProperty<int> BadgeProperty =
        AvaloniaProperty.RegisterAttached<TopLevel, int>("Badge", typeof(Taskbar));

    /// <summary>On a window: what a screen reader hears for the badge; the number itself when null.</summary>
    public static readonly AttachedProperty<string?> BadgeLabelProperty =
        AvaloniaProperty.RegisterAttached<TopLevel, string?>("BadgeLabel", typeof(Taskbar));

    private static readonly AttachedProperty<State?> StateProperty =
        AvaloniaProperty.RegisterAttached<TopLevel, State?>("State", typeof(Taskbar));

    private static IBadgeSink? _sink;

    public static int GetBadge(TopLevel top) => top.GetValue(BadgeProperty);
    public static void SetBadge(TopLevel top, int value) => top.SetValue(BadgeProperty, value);
    public static string? GetBadgeLabel(TopLevel top) => top.GetValue(BadgeLabelProperty);
    public static void SetBadgeLabel(TopLevel top, string? value) => top.SetValue(BadgeLabelProperty, value);

    /// <summary>Where the badge goes: the platform's own, picked once by OS, and replaced by the tests.</summary>
    internal static IBadgeSink Sink
    {
        get => _sink ??= OperatingSystem.IsWindows() ? new WindowsBadge() : OperatingSystem.IsMacOS() ? new DockBadge() : new NoBadge();
        set => _sink = value;
    }

    static Taskbar()
    {
        BadgeProperty.Changed.AddClassHandler<TopLevel>((top, _) => Schedule(top));
        BadgeLabelProperty.Changed.AddClassHandler<TopLevel>((top, _) => Schedule(top));
    }

    /// <summary>The number as it is written on the badge: null for none, 99+ above 99.</summary>
    internal static string? TextFor(int count) =>
        count <= 0 ? null : count > 99 ? "99+" : count.ToString(CultureInfo.InvariantCulture);

    /// <summary>The disc's colour: the bell's count red as the window sees it in its theme.</summary>
    internal static Color FillFor(TopLevel top) =>
        top.TryFindResource("InsNotifyCountBg", top.ActualThemeVariant, out var found) && found is ISolidColorBrush brush ? brush.Color : Color.Parse("#DC2626");

    internal static FontFamily FontFor(TopLevel top) =>
        top.TryFindResource("InsFontBodyEn", top.ActualThemeVariant, out var found) && found is FontFamily family ? family : FontFamily.Default;

    /// <summary>A Notify keeps the window's badge at its unread count with style-priority entries it owns, so a badge the app sets itself wins.</summary>
    internal static void Mirror(TopLevel top, int count, string label, ref IDisposable? badge, ref IDisposable? spoken)
    {
        Unmirror(ref badge, ref spoken);
        badge = top.SetValue(BadgeProperty, count, BindingPriority.Style);
        spoken = top.SetValue(BadgeLabelProperty, label, BindingPriority.Style);
    }

    internal static void Unmirror(ref IDisposable? badge, ref IDisposable? spoken)
    {
        badge?.Dispose();
        spoken?.Dispose();
        badge = null;
        spoken = null;
    }

    private static void Schedule(TopLevel top)
    {
        var state = top.GetValue(StateProperty);
        if (state is null)
        {
            state = new State();
            var made = state;
            made.Changed = (_, _) => Schedule(top);
            made.Opened = (_, _) =>
            {
                made.Force = true;
                Schedule(top);
            };
            made.Closing = (_, _) => Close(top, made);
            top.SetValue(StateProperty, state);
            top.Closed += made.Closing;
        }
        if (state.Closed || state.Posted) return;
        state.Posted = true;
        Dispatcher.UIThread.Post(() => Flush(top, state), DispatcherPriority.Background);
    }

    private static void Flush(TopLevel top, State state)
    {
        state.Posted = false;
        if (state.Closed) return;
        var text = TextFor(GetBadge(top));
        var label = text is null ? null : GetBadgeLabel(top) ?? text;
        var now = new Sent(text, label, BadgeIcon.Pixels(top.RenderScaling), FillFor(top));
        var same = state.Last is { } last ? last == now : text is null;
        Watch(top, state, text is not null);
        if (same && !state.Force) return;
        state.Force = false;
        state.Last = text is null ? null : now;
        try
        {
            Sink.Apply(top, text, label);
        }
        catch (Exception)
        {
        }
    }

    private static void Close(TopLevel top, State state)
    {
        if (state.Closed) return;
        state.Closed = true;
        state.Last = null;
        Watch(top, state, false);
        top.Closed -= state.Closing;
        try
        {
            Sink.Forget(top);
        }
        catch (Exception)
        {
        }
    }

    // The theme, the scale and the first show matter only while a badge is on.
    private static void Watch(TopLevel top, State state, bool on)
    {
        if (state.Watching == on) return;
        state.Watching = on;
        if (on)
        {
            top.ActualThemeVariantChanged += state.Changed;
            top.ScalingChanged += state.Changed;
            if (top is Window window) window.Opened += state.Opened;
        }
        else
        {
            top.ActualThemeVariantChanged -= state.Changed;
            top.ScalingChanged -= state.Changed;
            if (top is Window window) window.Opened -= state.Opened;
        }
    }

    private readonly record struct Sent(string? Text, string? Label, int Pixels, Color Fill);

    private sealed class State
    {
        public bool Posted, Watching, Closed, Force;
        public Sent? Last;
        public EventHandler Changed = null!;
        public EventHandler Opened = null!;
        public EventHandler Closing = null!;
    }
}

internal interface IBadgeSink
{
    void Apply(TopLevel top, string? text, string? label);
    void Forget(TopLevel top);
}

internal sealed class NoBadge : IBadgeSink
{
    public void Apply(TopLevel top, string? text, string? label) { }
    public void Forget(TopLevel top) { }
}

/// <summary>The window texts the Dock may show, newest set last: it shows the last window that still has a badge.</summary>
internal sealed class DockOrder
{
    private readonly List<(WeakReference<TopLevel> Owner, string Text)> _shown = [];

    public string? Set(TopLevel top, string? text)
    {
        Drop(top);
        if (text is not null) _shown.Add((new WeakReference<TopLevel>(top), text));
        return Current;
    }

    public string? Forget(TopLevel top)
    {
        Drop(top);
        return Current;
    }

    private string? Current => _shown.Count == 0 ? null : _shown[^1].Text;

    private void Drop(TopLevel top) => _shown.RemoveAll(e => !e.Owner.TryGetTarget(out var owner) || ReferenceEquals(owner, top));
}
