using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The alert's message line (<see cref="Alert"/>'s template): the text, then the alert's action (a retry, a way out) at the line's end, centred on the text, as Material's banner and Primer's banner place theirs. The action goes under the text, at its start, only where the text beside it would have less than <see cref="MinText"/> of room and still not fit on one line. No action (none given, or a hidden one): the text alone, as before.</summary>
internal sealed class AlertLine : Panel
{
    /// <summary>Between the text and the action on one line.</summary>
    internal const double Gap = 12;

    /// <summary>Between the text and the action under it.</summary>
    internal const double Under = 8;

    /// <summary>The text keeps at least this much room beside the action (a short sentence on two lines at most), else the action goes under it.</summary>
    internal const double MinText = 240;

    private bool _under;

    /// <summary>The action is under the text (as last measured).</summary>
    internal bool IsUnder => _under;

    private (Control? Text, Control? Action) Parts() =>
        (Children.Count > 0 ? Children[0] : null, Children.Count > 1 ? Children[1] : null);

    protected override Size MeasureOverride(Size availableSize)
    {
        var (text, action) = Parts();
        _under = false;
        if (text is null)
            return default;
        action?.Measure(Size.Infinity);
        var a = action is { IsVisible: true } ? action.DesiredSize : default;
        if (a.Width <= 0)
        {
            text.Measure(availableSize);
            return text.DesiredSize;
        }
        // the text on one line, then the room the line leaves it beside the action
        text.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var oneLine = text.DesiredSize.Width;
        var room = availableSize.Width - a.Width - Gap;
        _under = oneLine > room && room < MinText;
        if (_under)
        {
            text.Measure(availableSize);
            action!.Measure(availableSize);
            var t = text.DesiredSize;
            a = action.DesiredSize;
            return new Size(Math.Max(t.Width, a.Width), t.Height + Under + a.Height);
        }
        text.Measure(new Size(Math.Max(0, room), availableSize.Height));
        var d = text.DesiredSize;
        return new Size(d.Width + Gap + a.Width, Math.Max(d.Height, a.Height));
    }

    // Avalonia mirrors a right-to-left layout, so x = 0 is the line's start in both directions.
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (text, action) = Parts();
        if (text is null)
            return finalSize;
        var a = action is { IsVisible: true } ? action.DesiredSize : default;
        if (a.Width <= 0)
        {
            text.Arrange(new Rect(finalSize));
            action?.Arrange(default);
            return finalSize;
        }
        if (_under)
        {
            var th = text.DesiredSize.Height;
            text.Arrange(new Rect(0, 0, finalSize.Width, th));
            action!.Arrange(new Rect(0, th + Under, Math.Min(a.Width, finalSize.Width), a.Height));
            return finalSize;
        }
        var textWidth = Math.Max(0, finalSize.Width - a.Width - Gap);
        text.Arrange(new Rect(0, 0, textWidth, finalSize.Height));
        action!.Arrange(new Rect(finalSize.Width - a.Width, (finalSize.Height - a.Height) / 2, a.Width, a.Height));
        return finalSize;
    }
}
