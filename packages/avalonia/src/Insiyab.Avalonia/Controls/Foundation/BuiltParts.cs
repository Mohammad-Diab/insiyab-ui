using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Rendering;

namespace Insiyab.Avalonia.Controls;

// The Borders, TextBlocks and StackPanels the library builds in code: as with TemplateBorder, the app's Border.*, TextBlock.* and StackPanel.* styles and looks never reach them, and each control's parts carry only that control's own styles.

/// <summary>A Border that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthBorder</c>.</summary>
internal class MonthBorder : Border
{
    protected override Type StyleKeyOverride => typeof(MonthBorder);
}

/// <summary>An event chip <see cref="MonthCalendar"/> builds: a pressable one reaches a finger around it. Its styles name it <c>ins|MonthBorder</c>.</summary>
internal sealed class MonthChip : MonthBorder, ICustomHitTest, IReach
{
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point) ? Reach.Inside(this) : Focusable && Reach.Claims(this, point);
}

/// <summary>A TextBlock that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthText</c>.</summary>
internal sealed class MonthText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(MonthText);
}

/// <summary>A StackPanel that <see cref="MonthCalendar"/> builds; its styles name it <c>ins|MonthStack</c>. A header group (<see cref="Wraps"/>) given less room than its row needs goes on over more lines, each packed toward the group's side.</summary>
internal sealed class MonthStack : StackPanel
{
    protected override Type StyleKeyOverride => typeof(MonthStack);

    public bool Wraps { get; init; }

    /// <summary>Wrapped lines sit at the end side, as the header's end group does.</summary>
    public bool AtEnd { get; init; }

    /// <summary>How many of the last children wrap together, as the arrows and Today do.</summary>
    public int KeepLast { get; set; }

    private double LineGap => Math.Max(Spacing, 8);

    private bool Wrapping(double width) => Wraps && Orientation == Orientation.Horizontal && double.IsFinite(width) && Row() > width + 0.5;

    // The one-line width of the shown children, measured unconstrained.
    private double Row()
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        return shown.Sum(c => c.DesiredSize.Width) + Math.Max(0, shown.Count - 1) * Spacing;
    }

    private List<List<Control>> Lines(double width)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        var keep = Math.Min(KeepLast, shown.Count);
        var units = shown.Take(shown.Count - keep).Select(c => new List<Control> { c }).ToList();
        if (keep > 0) units.Add(shown.Skip(shown.Count - keep).ToList());
        var lines = new List<List<Control>> { new() };
        var used = 0d;
        foreach (var unit in units)
        {
            var w = unit.Sum(c => c.DesiredSize.Width) + (unit.Count - 1) * Spacing;
            if (lines[^1].Count > 0 && used + Spacing + w > width + 0.5)
            {
                lines.Add([]);
                used = 0;
            }
            used += (lines[^1].Count > 0 ? Spacing : 0) + w;
            lines[^1].AddRange(unit);
        }
        return lines;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (!Wraps || Orientation != Orientation.Horizontal) return base.MeasureOverride(availableSize);
        foreach (var c in Children) c.Measure(Size.Infinity);
        if (!Wrapping(availableSize.Width)) return base.MeasureOverride(availableSize);
        var lines = Lines(availableSize.Width);
        var height = lines.Sum(l => l.Max(c => c.DesiredSize.Height)) + (lines.Count - 1) * LineGap;
        return new Size(availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!Wrapping(finalSize.Width)) return base.ArrangeOverride(finalSize);
        var y = 0d;
        foreach (var line in Lines(finalSize.Width))
        {
            var h = line.Max(c => c.DesiredSize.Height);
            var w = line.Sum(c => c.DesiredSize.Width) + (line.Count - 1) * Spacing;
            var x = AtEnd ? finalSize.Width - w : 0;
            foreach (var c in line)
            {
                c.Arrange(new Rect(x, y + (h - c.DesiredSize.Height) / 2, c.DesiredSize.Width, c.DesiredSize.Height));
                x += c.DesiredSize.Width + Spacing;
            }
            y += h + LineGap;
        }
        return finalSize;
    }
}

/// <summary>A Border that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthBorder</c>.</summary>
internal sealed class BirthBorder : Border
{
    protected override Type StyleKeyOverride => typeof(BirthBorder);
}

/// <summary>A TextBlock that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthText</c>.</summary>
internal sealed class BirthText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(BirthText);
}

/// <summary>A StackPanel that <see cref="BirthDateField"/> builds; its styles name it <c>ins|BirthStack</c>.</summary>
internal sealed class BirthStack : StackPanel
{
    protected override Type StyleKeyOverride => typeof(BirthStack);
}

/// <summary>A Border that <see cref="DateCalendar"/> and its switch turn build; its styles name it <c>ins|CalendarBorder</c>.</summary>
internal sealed class CalendarBorder : Border
{
    protected override Type StyleKeyOverride => typeof(CalendarBorder);
}

/// <summary>A TextBlock that <see cref="DateCalendar"/> builds; its styles name it <c>ins|CalendarText</c>.</summary>
internal sealed class CalendarText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(CalendarText);
}

/// <summary>A Border that <see cref="ChoiceMatrix"/> builds; its styles name it <c>ins|MatrixBorder</c>.</summary>
internal sealed class MatrixBorder : Border
{
    protected override Type StyleKeyOverride => typeof(MatrixBorder);
}

/// <summary>A TextBlock that <see cref="ChoiceMatrix"/> builds; its styles name it <c>ins|MatrixText</c>.</summary>
internal sealed class MatrixText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(MatrixText);
}

/// <summary>A Border that <see cref="CommandPalette"/> builds; its styles name it <c>ins|PaletteBorder</c>.</summary>
internal sealed class PaletteBorder : Border
{
    protected override Type StyleKeyOverride => typeof(PaletteBorder);
}

/// <summary>A TextBlock that <see cref="CommandPalette"/> builds; its styles name it <c>ins|PaletteText</c>.</summary>
internal sealed class PaletteText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(PaletteText);
}
