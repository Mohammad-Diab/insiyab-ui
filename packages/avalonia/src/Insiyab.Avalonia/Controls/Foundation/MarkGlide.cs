using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Insiyab.Avalonia.Controls;

/// <summary>A selection mark an app draws itself (a <see cref="Canvas"/> child over its items, hidden at rest) glides to the new choice with the library's selection flight, the far edge first; none while still or unseen.</summary>
public static class MarkGlide
{
    private static readonly AttachedProperty<MarkerFlight?> FlightProperty =
        AvaloniaProperty.RegisterAttached<Control, MarkerFlight?>("Flight", typeof(MarkGlide));

    /// <summary>Flies <paramref name="mark"/> from <paramref name="from"/> to <paramref name="to"/> (canvas coordinates) along <paramref name="along"/>, then runs <paramref name="landed"/>; false, and nothing runs, when nothing flies.</summary>
    public static bool Fly(Control mark, Rect from, Rect to, Orientation along = Orientation.Horizontal, Action? landed = null)
    {
        var across = along == Orientation.Horizontal;
        (double, double) start = across ? (from.X, from.Width) : (from.Y, from.Height);
        if (mark.GetValue(FlightProperty) is { IsDone: false } previous)
        {
            start = (previous.Start, previous.Length);
            previous.Land();
        }
        mark.IsVisible = true; // seen while it flies (a hidden mark would not fly)
        MarkerFlight? run = null;
        run = MarkerFlight.Run(mark, start, across ? (to.X, to.Width) : (to.Y, to.Height), (at, length) =>
        {
            if (across)
            {
                Canvas.SetLeft(mark, at);
                mark.Width = length;
            }
            else
            {
                Canvas.SetTop(mark, at);
                mark.Height = length;
            }
        }, () =>
        {
            mark.IsVisible = false;
            if (mark.GetValue(FlightProperty) == run) mark.ClearValue(FlightProperty);
            landed?.Invoke();
        });
        if (run is null)
        {
            mark.IsVisible = false;
            return false;
        }
        if (across)
        {
            Canvas.SetTop(mark, to.Y);
            mark.Height = to.Height;
        }
        else
        {
            Canvas.SetLeft(mark, to.X);
            mark.Width = to.Width;
        }
        mark.SetValue(FlightProperty, run);
        return true;
    }

    /// <summary>Whether <paramref name="mark"/> is flying now.</summary>
    public static bool IsFlying(Control mark) => mark.GetValue(FlightProperty) is { IsDone: false };
}
