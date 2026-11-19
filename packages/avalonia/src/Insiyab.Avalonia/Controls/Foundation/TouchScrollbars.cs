using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A phone's scrollbars, as Android draws them: under a finger (<see cref="PseudoClass"/> on the TopLevel) a scroller's bar lies over its content, thin, shown only while it scrolls and fading a moment after; a finger never takes it. A mouse, and every desktop, keep the web's bars.</summary>
internal static class TouchScrollbars
{
    /// <summary>The TopLevel's pseudo-class while its scrollbars are a phone's: the last pointer was a finger, on Android.</summary>
    public const string PseudoClass = ":ins-touch-scroll";

    /// <summary>How long a bar stays after the scroll stops, before it fades.</summary>
    public static readonly TimeSpan Linger = TimeSpan.FromMilliseconds(900);

    private static readonly AttachedProperty<DispatcherTimer?> TimerProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, DispatcherTimer?>("Timer", typeof(TouchScrollbars));

    internal static void OnScrolled(ScrollViewer scroller, ScrollChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, scroller) || TopLevel.GetTopLevel(scroller) is not { } top || !top.Classes.Contains(PseudoClass)) return;
        if (e.OffsetDelta == default) return; // a new size, not a scroll
        Show(scroller, true);
        var timer = scroller.GetValue(TimerProperty);
        if (timer is null)
        {
            timer = new DispatcherTimer { Interval = Linger };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Show(scroller, false);
            };
            scroller.SetValue(TimerProperty, timer);
        }
        timer.Stop();
        timer.Start();
    }

    private static void Show(ScrollViewer scroller, bool on)
    {
        foreach (var bar in scroller.GetVisualDescendants().OfType<ScrollBar>().Where(b => ReferenceEquals(b.TemplatedParent, scroller)))
            bar.Classes.Set("ins-shown", on);
    }
}
