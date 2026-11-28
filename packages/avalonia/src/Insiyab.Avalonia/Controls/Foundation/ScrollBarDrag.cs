using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Insiyab.Avalonia.Controls;

/// <summary>A scroll bar keeps its grown look while its thumb is dragged, though the pointer leaves the bar: <see cref="PseudoClass"/> on the bar from the drag's start to its end.</summary>
internal static class ScrollBarDrag
{
    public const string PseudoClass = ":ins-drag";

    private static bool _registered;

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        Thumb.DragStartedEvent.AddClassHandler<ScrollBar>((bar, _) => Set(bar, true), RoutingStrategies.Bubble, handledEventsToo: true);
        Thumb.DragCompletedEvent.AddClassHandler<ScrollBar>((bar, _) => Set(bar, false), RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private static void Set(ScrollBar bar, bool on) => ((IPseudoClasses)bar.Classes).Set(PseudoClass, on);
}
