using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The four toast tones, as <c>data-ins-tone</c>: ok, info, warn, bad.</summary>
public enum ToastTone
{
    Info,
    Ok,
    Warn,
    Bad,
}

/// <summary>The web's <c>Insiyab.toast(text, tone)</c>: a short message in the bottom corner on the end side of the window, newest nearest the edge, that leaves on its own after five seconds, or when clicked. An error stays until it is closed. Hovering or focusing a toast holds the timer; <see cref="FocusGesture"/> (F8) moves focus to the toasts and back, the arrows walk them, and Enter, Space, Delete or Escape dismisses one. At a phone's width the strip spans the bottom. It is Avalonia's own <see cref="WindowNotificationManager"/> and <see cref="NotificationCard"/>, themed, one manager per window, above any open dialog. On a button, <see cref="TextProperty"/> and <see cref="ToneProperty"/> show one on each click, as <c>data-ins-toast</c>.</summary>
public sealed class Toast
{
    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<Toast, Button, string?>("Text");

    public static readonly AttachedProperty<ToastTone> ToneProperty =
        AvaloniaProperty.RegisterAttached<Toast, Button, ToastTone>("Tone");

    /// <summary>How long a toast stays: the web's 5s hold. An error with no duration of its own stays until closed.</summary>
    public static TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How many toasts show at once; the oldest leaves for a new one.</summary>
    public static int MaxItems { get; set; } = 5;

    /// <summary>The key that moves focus to the newest toast, and back to where it was.</summary>
    public static KeyGesture FocusGesture { get; set; } = new(Key.F8);

    static Toast()
    {
        TransformOperationsAnimator.Ensure();
        Button.ClickEvent.AddClassHandler(typeof(Button), OnAnyClick, RoutingStrategies.Bubble);
    }

    private Toast()
    {
    }

    public static string? GetText(Button button) => button.GetValue(TextProperty);
    public static void SetText(Button button, string? value) => button.SetValue(TextProperty, value);
    public static ToastTone GetTone(Button button) => button.GetValue(ToneProperty);
    public static void SetTone(Button button, ToastTone value) => button.SetValue(ToneProperty, value);

    /// <summary>Shows <paramref name="text"/> in the window <paramref name="anchor"/> is in; the text is shown as is, never parsed.</summary>
    public static void Show(Visual anchor, string text, ToastTone tone = ToastTone.Info) => Show(anchor, text, tone, null);

    /// <summary>As above, staying <paramref name="duration"/>; null is <see cref="Duration"/>, or until closed for an error.</summary>
    public static void Show(Visual anchor, string text, ToastTone tone, TimeSpan? duration)
    {
        TransformOperationsAnimator.Ensure();
        ToastStrip.For(anchor)?.Show(text, tone, duration, anchor.FlowDirection);
    }

    /// <summary>The window's toast strip, made the first time it is needed.</summary>
    public static WindowNotificationManager? ManagerFor(Visual anchor) => ToastStrip.For(anchor)?.Manager;

    /// <summary>The keyboard route: moves focus to the newest toast; false when there are none.</summary>
    public static bool FocusToasts(Visual anchor) => ToastStrip.For(anchor)?.Focus() ?? false;

    public static NotificationType TypeOf(ToastTone tone) => tone switch
    {
        ToastTone.Ok => NotificationType.Success,
        ToastTone.Warn => NotificationType.Warning,
        ToastTone.Bad => NotificationType.Error,
        _ => NotificationType.Information,
    };

    private static void OnAnyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || e.Source != button || e.Handled) return;
        if (GetText(button) is { } text) Show(button, text, GetTone(button));
    }
}

/// <summary>One window's toasts: the manager, the hold timers (paused while the pointer or focus is on a toast) and the keyboard route.</summary>
internal sealed class ToastStrip
{
    private static readonly ConditionalWeakTable<TopLevel, ToastStrip> Strips = new();
    private readonly TopLevel _top;
    private readonly List<ToastClock> _clocks = [];
    private bool _hover;
    private Control? _from;

    public WindowNotificationManager Manager { get; }

    private ToastStrip(TopLevel top)
    {
        _top = top;
        Manager = new WindowNotificationManager(top) { Position = NotificationPosition.BottomRight, MaxItems = Toast.MaxItems, ZIndex = 1 };
        Manager.ApplyTemplate();
        top.AddHandler(InputElement.KeyDownEvent, OnTopKey, RoutingStrategies.Bubble);
        top.PropertyChanged += (_, e) => { if (e.Property == Visual.BoundsProperty) Fit(); };
        Fit();
    }

    public static ToastStrip? For(Visual anchor)
    {
        if (OverlayHost.Top(anchor) is not { } top) return null;
        if (!Strips.TryGetValue(top, out var strip))
        {
            strip = new ToastStrip(top);
            Strips.Add(top, strip);
        }
        return strip;
    }

    private bool Paused => _hover || Manager.IsKeyboardFocusWithin;

    // At a phone width the strip spans the bottom, clear of the system's gesture area.
    private void Fit()
    {
        OverlayHost.SetClass(Manager.Classes, "phone", OverlayHost.IsPhone(_top));
        var inset = _top.InsetsManager?.SafeAreaPadding.Bottom ?? 0;
        Manager.Margin = new Thickness(0, 0, 0, inset);
    }

    public void Show(string text, ToastTone tone, TimeSpan? duration, FlowDirection direction)
    {
        Manager.FlowDirection = direction;
        Manager.MaxItems = Toast.MaxItems;
        var persist = duration is null && tone == ToastTone.Bad;
        Manager.Show(text, Toast.TypeOf(tone), TimeSpan.Zero, null, null, persist ? ["persist"] : null);
        if (Items()?.Children.OfType<NotificationCard>().LastOrDefault() is not { } card) return;
        AutomationProperties.SetLiveSetting(card, tone == ToastTone.Bad ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite);
        card.PointerEntered += (_, _) =>
        {
            _hover = true;
            PauseAll();
        };
        card.PointerExited += (_, _) =>
        {
            _hover = false;
            ResumeSoon();
        };
        card.LostFocus += (_, _) => ResumeSoon();
        card.GotFocus += (_, _) => PauseAll();
        card.AddHandler(InputElement.KeyDownEvent, OnCardKey, RoutingStrategies.Bubble);
        card.DetachedFromVisualTree += (_, _) => Forget(card);
        card.PropertyChanged += (_, e) => { if (e.Property == NotificationCard.IsClosingProperty && card.IsClosing) Forget(card); };
        if (persist) return;
        var clock = new ToastClock(card, duration ?? Toast.Duration);
        _clocks.Add(clock);
        if (!Paused) clock.Start();
    }

    private Panel? Items() => Manager.GetVisualDescendants().OfType<Panel>().FirstOrDefault(p => p.Name == "PART_Items");

    private List<NotificationCard> Cards() => Items()?.Children.OfType<NotificationCard>().Where(c => !c.IsClosing).ToList() ?? [];

    private void Forget(NotificationCard card)
    {
        foreach (var c in _clocks.Where(c => c.Card == card).ToList())
        {
            c.Pause();
            _clocks.Remove(c);
        }
    }

    private void PauseAll()
    {
        foreach (var c in _clocks) c.Pause();
    }

    private void ResumeSoon() => Dispatcher.UIThread.Post(() =>
    {
        if (Paused) return;
        foreach (var c in _clocks) c.Start();
    }, DispatcherPriority.Input);

    // The keyboard route: F8 reaches the newest toast and F8 again goes back.
    private void OnTopKey(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !Toast.FocusGesture.Matches(e)) return;
        if (Manager.IsKeyboardFocusWithin)
        {
            Return();
            e.Handled = true;
            return;
        }
        e.Handled = Focus();
    }

    public bool Focus()
    {
        var cards = Cards();
        if (cards.Count == 0) return false;
        _from = _top.FocusManager?.GetFocusedElement() as Control;
        cards[^1].Focus(NavigationMethod.Directional);
        return true;
    }

    private void Return()
    {
        var from = _from;
        _from = null;
        if (from is not null && from.IsAttachedToVisualTree() && from.IsEffectivelyVisible) from.Focus(NavigationMethod.Directional);
        else _top.Focus();
    }

    private void OnCardKey(object? sender, KeyEventArgs e)
    {
        if (sender is not NotificationCard card) return;
        var cards = Cards();
        var at = cards.IndexOf(card);
        switch (e.Key)
        {
            case Key.Up when at > 0:
                cards[at - 1].Focus(NavigationMethod.Directional);
                e.Handled = true;
                break;
            case Key.Down when at >= 0 && at < cards.Count - 1:
                cards[at + 1].Focus(NavigationMethod.Directional);
                e.Handled = true;
                break;
            case Key.Enter or Key.Space or Key.Delete or Key.Escape:
                var next = at + 1 < cards.Count ? cards[at + 1] : at > 0 ? cards[at - 1] : null;
                card.Close();
                if (next is not null) next.Focus(NavigationMethod.Directional);
                else Return();
                e.Handled = true;
                break;
        }
    }
}

/// <summary>The hold timer of one toast, which keeps the time it has left while paused.</summary>
internal sealed class ToastClock(NotificationCard card, TimeSpan left)
{
    private TimeSpan _left = left;
    private long _start;
    private IDisposable? _timer;

    public NotificationCard Card { get; } = card;

    public void Start()
    {
        if (_timer is not null) return;
        _start = Stopwatch.GetTimestamp();
        _timer = DispatcherTimer.RunOnce(() =>
        {
            _timer = null;
            if (!Card.IsClosing && Card.IsAttachedToVisualTree()) Card.Close();
        }, _left);
    }

    public void Pause()
    {
        if (_timer is null) return;
        _timer.Dispose();
        _timer = null;
        _left -= Stopwatch.GetElapsedTime(_start);
        if (_left < TimeSpan.Zero) _left = TimeSpan.Zero;
    }
}
