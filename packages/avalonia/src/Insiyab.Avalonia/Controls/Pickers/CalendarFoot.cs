using Avalonia;
using Avalonia.Controls;

namespace Insiyab.Avalonia.Controls;

/// <summary>The calendar footer row (the web's <c>.ins-cal-foot</c>): parts in a row with a gap, auto gaps before the parts that push to the end, and the calendar switch on its own line above the rest when the row does not fit.</summary>
internal sealed class CalendarFoot : Panel
{
    internal const double Gap = 12;
    internal const double RowGap = 8;

    private readonly HashSet<Control> _push = new();

    /// <summary>The calendar switch, the part that moves to its own line when the row does not fit.</summary>
    internal Control? Switch { get; set; }

    /// <summary>True while the switch sits on its own line.</summary>
    internal bool IsStacked { get; private set; }

    protected override Type StyleKeyOverride => typeof(CalendarFoot);

    /// <summary>Adds a part; <paramref name="push"/> puts a stretching gap before it, as the web's margin-inline-start auto.</summary>
    internal void Add(Control part, bool push)
    {
        if (push) _push.Add(part);
        Children.Add(part);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var free = new Size(double.PositiveInfinity, availableSize.Height);
        foreach (var c in Children) c.Measure(free);
        var all = Children.ToList();
        var natural = RowWidth(all);
        IsStacked = Switch is not null && Children.Count > 1 && !double.IsInfinity(availableSize.Width) && natural > availableSize.Width + 0.5;
        if (!IsStacked) return new Size(natural, RowHeight(all));
        var rest = all.Where(c => c != Switch).ToList();
        return new Size(Math.Max(Switch!.DesiredSize.Width, RowWidth(rest)), Switch.DesiredSize.Height + RowGap + RowHeight(rest));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var all = Children.ToList();
        IsStacked = Switch is not null && Children.Count > 1 && RowWidth(all) > finalSize.Width + 0.5;
        if (!IsStacked)
        {
            ArrangeRow(all, _push, 0, finalSize.Width, finalSize.Height);
            return finalSize;
        }
        var top = Switch!.DesiredSize;
        Switch.Arrange(new Rect(Math.Max(0, (finalSize.Width - top.Width) / 2), 0, Math.Min(top.Width, finalSize.Width), top.Height));
        var rest = all.Where(c => c != Switch).ToList();
        var after = all.IndexOf(Switch) + 1;
        var push = new HashSet<Control>(_push.Where(c => c != Switch));
        if (_push.Contains(Switch) && after < all.Count && !push.Contains(all[after])) push.Add(all[after]);
        var y = top.Height + RowGap;
        ArrangeRow(rest, push, y, finalSize.Width, Math.Max(0, finalSize.Height - y));
        return finalSize;
    }

    private static double RowWidth(List<Control> row) => row.Count == 0 ? 0 : row.Sum(c => c.DesiredSize.Width) + Gap * (row.Count - 1);

    private static double RowHeight(List<Control> row) => row.Count == 0 ? 0 : row.Max(c => c.DesiredSize.Height);

    private static void ArrangeRow(List<Control> row, HashSet<Control> push, double y, double width, double height)
    {
        var pushes = row.Skip(1).Count(push.Contains);
        var extra = pushes > 0 ? Math.Max(0, width - RowWidth(row)) / pushes : 0;
        var x = 0.0;
        for (var i = 0; i < row.Count; i++)
        {
            var c = row[i];
            if (i > 0) x += Gap + (push.Contains(c) ? extra : 0);
            c.Arrange(new Rect(x, y, c.DesiredSize.Width, height));
            x += c.DesiredSize.Width;
        }
    }
}
