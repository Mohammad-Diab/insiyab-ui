using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Insiyab.Avalonia.Controls;

/// <summary>Keeps a top bar's side columns at least as wide as their zones' content, as the web's 1fr does, so a long end zone never runs under the caption buttons. A bar whose parts do not fit at their full width folds, as the new Outlook and Teams do (<see cref="ShellLook.FoldProperty"/>): the account island to its picture, the title's second line and the brand away; then the back and status islands to their mark; then an island marked <c>folds-away</c> away, with the gaps, the title's side room and the caption island tightened; then an island marked <c>folds-away-last</c> away (only where that bar still lacks the room); and the title takes what is left (a <see cref="Marquee"/> title glides; still, its ellipsis). Each step is undone once the width it saved is back.</summary>
internal static class TopbarColumns
{
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<Grid, bool>("IsOn", typeof(TopbarColumns));

    // the last fold step, and the narrowest the title is ever given
    internal const int MaxFold = 4;
    internal const double MinTitle = 56;

    private static readonly ConditionalWeakTable<Grid, EventHandler> s_handlers = new();
    private static readonly ConditionalWeakTable<Grid, FoldState> s_states = new();

    // what each fold step saved (measured in the pass after it was taken), and the step just taken with the width it was taken at
    private sealed class FoldState
    {
        public readonly double[] Saved = new double[MaxFold];
        public int Taken = -1;
        public double NeedBefore;
        public double Full = -1; // the head's whole width, measured while it was not capped (a head that is not a gliding text shrinks with its cap)
    }

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
        var left = Natural(grid, 0);
        var right = Natural(grid, 2);
        SetMin(cols[0], left);
        SetMin(cols[2], right);
        Fold(grid, left, right);
    }

    // The fold: the whole bar's width with the title at its full length against the bar's room; a step at a time, so each pass measures what the last one saved.
    private static void Fold(Grid grid, double left, double right)
    {
        var room = grid.Bounds.Width;
        if (room <= 0 || Title(grid) is not { } title) return;
        var (zone, island, head) = title;
        var level = ShellLook.GetFold(grid);
        var state = s_states.GetOrCreateValue(grid);

        // the head's full width: a gliding title measures its text whole; the other lines as they are (the second line is 0 wide once folded). A head of another kind (a views switch in the name's place) measures as wide as its cap lets it: its whole width is the one taken while it was not capped, or the cap would follow its own measure pass after pass
        var full = 0.0;
        var glides = false;
        foreach (var line in head.Children)
            if (line.IsVisible)
            {
                glides |= line is Marquee;
                full = Math.Max(full, line is Marquee { Child: { } text } ? text.DesiredSize.Width : line.DesiredSize.Width);
            }
        // a gliding title measures whole even while capped: its width is its own (a new, shorter name while folded must give the room back); only another kind of head keeps the width it had while uncapped
        if (glides || double.IsInfinity(head.MaxWidth))
            state.Full = full;
        else if (state.Full > full)
            full = state.Full;
        var chrome = island.DesiredSize.Width - head.DesiredSize.Width;
        var islandFull = Math.Min(island.MaxWidth + island.Margin.Left + island.Margin.Right, full + chrome);
        var center = Width(zone) - island.DesiredSize.Width + islandFull;
        var cols = grid.ColumnDefinitions;
        var extra = 0.0;
        for (var i = 3; i < cols.Count; i++)
            extra += Natural(grid, i);
        var need = left + center + right + extra + grid.ColumnSpacing * (cols.Count - 1);

        if (state.Taken >= 0 && level == state.Taken + 1)
            state.Saved[state.Taken] = Math.Max(0, state.NeedBefore - need);
        state.Taken = -1;

        if (need > room + 0.5 && level < MaxFold)
        {
            state.Taken = level;
            state.NeedBefore = need;
            Cap(head, double.PositiveInfinity);
            ShellLook.SetFold(grid, level + 1);
            // a step that changes nothing in this bar (no account to fold, no brand) would leave no layout pass to take the next one: ask for one
            grid.InvalidateMeasure();
            return;
        }
        if (level > 0 && need + state.Saved[level - 1] <= room - 0.5)
        {
            Cap(head, double.PositiveInfinity);
            ShellLook.SetFold(grid, level - 1);
            grid.InvalidateMeasure();
            return;
        }
        Cap(head, need > room + 0.5 ? Math.Max(MinTitle, full - (need - room)) : double.PositiveInfinity);
    }

    private static void Cap(Control head, double width)
    {
        if (!(Math.Abs(head.MaxWidth - width) <= 0.5 || double.IsInfinity(head.MaxWidth) && double.IsInfinity(width)))
            head.MaxWidth = width;
    }

    // The middle zone, its title island and the island's head (StackPanel.shell-head); none: the bar has no title to fold around.
    private static (StackPanel Zone, Control Island, Panel Head)? Title(Grid grid)
    {
        foreach (var child in grid.Children)
        {
            if (Grid.GetColumn(child) != 1 || child is not StackPanel zone || !zone.IsVisible) continue;
            foreach (var c in zone.Children)
            {
                if (!c.IsVisible || !c.Classes.Contains("title")) continue;
                var head = c switch
                {
                    Decorator d => d.Child,
                    ContentControl cc => cc.Content as Control,
                    _ => null,
                };
                if (head is Panel p && p.Classes.Contains("shell-head"))
                    return (zone, c, p);
            }
        }
        return null;
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
