using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>The month calendar's header row, five groups: the lead group (an app's own first action, as an «add» button; empty unless the app fills it) and the start group (Today) at the start edge, the aside group (the work-hours switch) right after them, the end group (the views) at the end edge, the middle group (the date between its arrows) truly centred between them. When they do not fit on one line, the header wraps as Google Calendar and a phone's head group it, the views first: line 1 the lead group at the start and the aside and end groups at the end, line 2 Today with the date beside it (centred in the row when it has room, else right after Today); the Tab order follows. Narrower still, each part takes a line of its own (the lead first, the aside and the end group sharing one while they fit, then Today, then the date). With no middle group, an end group that does not fit goes under the start group. A group wider than the whole row wraps its own controls.</summary>
internal sealed class MonthHead : Panel
{
    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<MonthHead, double>(nameof(Gap), 12);

    // the room between the start group and the aside group on one line
    private const double AsideGap = 14;

    private int _lines = 1;

    static MonthHead() => AffectsMeasure<MonthHead>(GapProperty);

    protected override Type StyleKeyOverride => typeof(MonthHead);

    /// <summary>The room between the groups, and between the lines when they wrap.</summary>
    public double Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }

    internal bool IsWrapped => _lines > 1;

    /// <summary>More than two lines: the parts that shared a line take one each.</summary>
    internal bool IsStacked => _lines > 2;

    private bool HasMiddle => Children[1].IsVisible;

    private static Control? Shown(Control c) => c.IsVisible && c.DesiredSize.Width > 0 ? c : null;

    private Control? Aside => Children.Count > 3 ? Shown(Children[3]) : null;

    private Control? Lead => Children.Count > 4 ? Shown(Children[4]) : null;

    private static double W(Control? c) => c?.DesiredSize.Width ?? 0;

    private static double H(Control? c) => c?.DesiredSize.Height ?? 0;

    // the lead, Today and the hours, as one line has them at its start
    private double LeadWidth => (Lead is null ? 0 : W(Lead) + Gap) + W(Children[0]) + (Aside is null ? 0 : AsideGap + W(Aside));

    // the hours and the views at the end of a line
    private double TailWidth => (Aside is null ? 0 : W(Aside) + Gap) + W(Children[2]);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 3) return base.MeasureOverride(availableSize);
        foreach (var child in Children) child.Measure(Size.Infinity);
        var room = availableSize.Width;
        var line = HasMiddle ? 2 * Math.Max(LeadWidth, W(Children[2])) + W(Children[1]) + 2 * Gap : LeadWidth + W(Children[2]) + Gap;
        var width = double.IsInfinity(room) ? line : room;
        // A group wider than the row wraps inside itself.
        if (double.IsFinite(room))
            foreach (var child in Children)
                if (child.IsVisible && child.DesiredSize.Width > room + 0.5) child.Measure(new Size(room, double.PositiveInfinity));
        var plan = Plan(width, line <= room);
        _lines = plan.Lines;
        Order();
        return new Size(width, plan.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 3) return base.ArrangeOverride(finalSize);
        var plan = Plan(finalSize.Width, _lines == 1);
        foreach (var child in Children)
            child.Arrange(plan.Boxes.TryGetValue(child, out var box) ? box : default);
        return finalSize;
    }

    private readonly record struct Layout(int Lines, double Height, Dictionary<Control, Rect> Boxes);

    // Where each group goes for a row this wide: on one line, or wrapped (the views first, then Today and the date), each part on a line of its own where even that does not fit.
    private Layout Plan(double width, bool oneLine)
    {
        var boxes = new Dictionary<Control, Rect>();
        var (start, middle, end, aside, lead) = (Children[0], Children[1], Children[2], Aside, Lead);
        double Clip(Control? c) => Math.Min(W(c), width);
        if (oneLine)
        {
            var h = Math.Max(Math.Max(H(lead), H(start)), Math.Max(Math.Max(H(aside), H(end)), HasMiddle ? H(middle) : 0));
            var x = 0.0;
            if (lead is not null)
            {
                boxes[lead] = new Rect(0, 0, Clip(lead), h);
                x = W(lead) + Gap;
            }
            boxes[start] = new Rect(x, 0, Clip(start), h);
            x += W(start);
            if (aside is not null) boxes[aside] = new Rect(x + AsideGap, 0, Clip(aside), h);
            boxes[end] = new Rect(Math.Max(0, width - Clip(end)), 0, Clip(end), h);
            if (HasMiddle) boxes[middle] = new Rect((width - Clip(middle)) / 2, 0, Clip(middle), h);
            return new Layout(1, h, boxes);
        }
        var y = 0.0;
        var lines = 0;
        void Row(double height)
        {
            y += height + Gap;
            lines++;
        }
        if (!HasMiddle)
        {
            // the title (and the lead and the hours) on line 1, every control at the end of line 2
            var first = Math.Max(H(lead), Math.Max(H(start), H(aside)));
            var x = 0.0;
            if (lead is not null)
            {
                boxes[lead] = new Rect(0, 0, Clip(lead), first);
                x = W(lead) + Gap;
            }
            boxes[start] = new Rect(Math.Min(x, Math.Max(0, width - Clip(start))), 0, Clip(start), first);
            if (aside is not null) boxes[aside] = new Rect(Math.Min(width - Clip(aside), x + W(start) + AsideGap), 0, Clip(aside), first);
            Row(first);
            boxes[end] = new Rect(width - Clip(end), y, Clip(end), H(end));
            Row(H(end));
            return new Layout(lines, y - Gap, boxes);
        }
        // the views first: the lead at the start, the hours and the views at the end; on a line each where they do not fit together
        var tail = TailWidth;
        void Tail(double top, double height)
        {
            if (aside is not null) boxes[aside] = new Rect(Math.Max(0, width - tail), top, Clip(aside), height);
            boxes[end] = new Rect(Math.Max(0, width - Clip(end)), top, Clip(end), height);
        }
        if ((lead is null ? 0 : W(lead) + Gap) + tail <= width)
        {
            var h = Math.Max(H(lead), Math.Max(H(aside), H(end)));
            if (lead is not null) boxes[lead] = new Rect(0, y, Clip(lead), h);
            Tail(y, h);
            Row(h);
        }
        else
        {
            if (lead is not null)
            {
                boxes[lead] = new Rect(0, y, Clip(lead), H(lead));
                Row(H(lead));
            }
            if (tail <= width)
            {
                var h = Math.Max(H(aside), H(end));
                Tail(y, h);
                Row(h);
            }
            else
            {
                if (aside is not null)
                {
                    boxes[aside] = new Rect(Math.Max(0, width - Clip(aside)), y, Clip(aside), H(aside));
                    Row(H(aside));
                }
                boxes[end] = new Rect(Math.Max(0, width - Clip(end)), y, Clip(end), H(end));
                Row(H(end));
            }
        }
        // then Today with the date beside it, in the row's middle where it clears Today; under Today where the two do not fit
        var centred = (width - Clip(middle)) / 2;
        if (W(start) + Gap + W(middle) <= width)
        {
            var h = Math.Max(H(start), H(middle));
            boxes[start] = new Rect(0, y, Clip(start), h);
            boxes[middle] = new Rect(Math.Min(width - Clip(middle), Math.Max(centred, W(start) + Gap)), y, Clip(middle), h);
            Row(h);
        }
        else
        {
            boxes[start] = new Rect(0, y, Clip(start), H(start));
            Row(H(start));
            boxes[middle] = new Rect(centred, y, Clip(middle), H(middle));
            Row(H(middle));
        }
        return new Layout(lines, y - Gap, boxes);
    }

    // Tab follows the eye: one line from the start to the end; wrapped, line by line (the lead, the hours, the views, then Today and the date)
    private void Order()
    {
        var wrapped = _lines > 1 && HasMiddle;
        if (Children.Count > 4) KeyboardNavigation.SetTabIndex(Children[4], 0);
        KeyboardNavigation.SetTabIndex(Children[0], wrapped ? 3 : 1);
        KeyboardNavigation.SetTabIndex(Children[1], wrapped ? 4 : 3);
        KeyboardNavigation.SetTabIndex(Children[2], wrapped ? 2 : 4);
        if (Children.Count > 3) KeyboardNavigation.SetTabIndex(Children[3], wrapped ? 1 : 2);
    }
}
