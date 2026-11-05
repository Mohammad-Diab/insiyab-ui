using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Insiyab.Avalonia.Controls;

/// <summary>Keeps a top bar's side columns at least as wide as their zones' content, as the web's 1fr does, so a long end zone never runs under the caption buttons.</summary>
internal static class TopbarColumns
{
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<Grid, bool>("IsOn", typeof(TopbarColumns));

    private static readonly ConditionalWeakTable<Grid, EventHandler> s_handlers = new();

    static TopbarColumns()
    {
        IsOnProperty.Changed.AddClassHandler<Grid>((grid, e) =>
        {
            if (s_handlers.TryGetValue(grid, out var old))
            {
                grid.LayoutUpdated -= old;
                s_handlers.Remove(grid);
            }
            if (!e.GetNewValue<bool>()) return;
            EventHandler handler = (_, _) => Fit(grid);
            s_handlers.Add(grid, handler);
            grid.LayoutUpdated += handler;
        });
    }

    public static bool GetIsOn(Grid grid) => grid.GetValue(IsOnProperty);
    public static void SetIsOn(Grid grid, bool value) => grid.SetValue(IsOnProperty, value);

    private static void Fit(Grid grid)
    {
        var cols = grid.ColumnDefinitions;
        if (cols.Count < 3 || !cols[0].Width.IsStar || !cols[1].Width.IsAuto || !cols[2].Width.IsStar) return;
        SetMin(cols[0], Natural(grid, 0));
        SetMin(cols[2], Natural(grid, 2));
    }

    private static void SetMin(ColumnDefinition column, double width)
    {
        if (Math.Abs(column.MinWidth - width) > 0.5) column.MinWidth = width;
    }

    // The widest content of a column's zones: a zone's children are measured at their own width, so their sum is what the zone needs.
    private static double Natural(Grid grid, int column)
    {
        var widest = 0.0;
        foreach (var child in grid.Children)
        {
            if (!child.IsVisible || Grid.GetColumn(child) != column || Grid.GetColumnSpan(child) != 1) continue;
            widest = Math.Max(widest, Width(child));
        }
        return widest;
    }

    private static double Width(Control c)
    {
        if (c is not StackPanel { Orientation: Orientation.Horizontal } row) return c.DesiredSize.Width;
        var sum = 0.0;
        var shown = 0;
        foreach (var k in row.Children)
        {
            if (!k.IsVisible) continue;
            sum += k.DesiredSize.Width;
            shown++;
        }
        return sum + Math.Max(0, shown - 1) * row.Spacing + c.Margin.Left + c.Margin.Right;
    }
}
