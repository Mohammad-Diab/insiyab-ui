using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A long table that rises in on a page that rises in itself: no row on screen is ever left hidden by its entrance.
public class TableEntranceTests
{
    private sealed class HeldEntrance : IDisposable
    {
        private readonly Func<double> _real = Entrance.Now;
        public double At = 1000;

        public HeldEntrance() => Entrance.Now = () => At;

        public void Frame(Window window, double step = 16)
        {
            At += step;
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => Entrance.Now = _real;
    }

    private static Table Payments(ObservableCollection<string> items)
    {
        var table = new Table { ItemsSource = items };
        table.Columns.Add(new TableColumn { Header = "الدفعة", CellText = o => o as string });
        table.Columns.Add(new TableColumn { Header = "المبلغ", CellText = o => "100 USD" });
        return table;
    }

    // Rows whose box meets the scroller's view, with what keeps any of them from being seen.
    private static List<string> Hidden(ScrollViewer scroll, Table table)
    {
        var bad = new List<string>();
        var rows = table.DisplayedRows;
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            // a row out of the tree or waiting hidden is out of view (the gap check below catches one missing in view)
            if (!row.IsAttachedToVisualTree() || !row.IsVisible) continue;
            if (row.TranslatePoint(new Point(0, 0), scroll) is not { } p) continue;
            if (p.Y + row.Bounds.Height <= 0 || p.Y >= scroll.Bounds.Height) continue;
            if (row.Opacity < 0.999) bad.Add($"row {i} at y {p.Y:0} opacity {row.Opacity:0.00}");
        }
        // the view must be covered by rows that are shown: no gap between the first and last shown row inside the view
        var shown = rows.Where(r => r.IsAttachedToVisualTree() && r.IsEffectivelyVisible)
            .Select(r => r.TranslatePoint(new Point(0, 0), scroll) is { } p ? (Top: p.Y, Bottom: p.Y + r.Bounds.Height) : (Top: double.NaN, Bottom: double.NaN))
            .Where(b => !double.IsNaN(b.Top) && b.Bottom > 0 && b.Top < scroll.Bounds.Height)
            .OrderBy(b => b.Top).ToList();
        for (var k = 1; k < shown.Count; k++)
            if (shown[k].Top - shown[k - 1].Bottom > 1) bad.Add($"gap {shown[k - 1].Bottom:0}..{shown[k].Top:0}");
        return bad;
    }

    // A long table on its own in a scroller, its stagger stepped to where the first rows are nearly in, the third half way and the ninth on still waiting.
    private static (Window Window, ScrollViewer Scroll, Table Table) Staggering(HeldEntrance held)
    {
        var items = new ObservableCollection<string>(Enumerable.Range(1, 40).Select(i => "دفعة " + i));
        var table = Payments(items);
        var scroll = new ScrollViewer { Content = table };
        var window = Track(new Window { Width = 900, Height = 700, FlowDirection = FlowDirection.RightToLeft, Content = scroll });
        window.Show();
        held.Frame(window, 0);
        held.Frame(window, 0);
        for (var t = 0; t < 120; t += 20) held.Frame(window, 20);
        var rows = table.DisplayedRows;
        var o = string.Join(" ", rows.Take(10).Select(r => r.Opacity.ToString("0.00")));
        Assert.True(rows[0].Opacity > 0.9, o);
        Assert.True(rows[2].Opacity is > 0 and < 1, "the third row is half in: " + o);
        Assert.True(rows[8].Opacity < 0.001, o);
        return (window, scroll, table);
    }

    // The page (or the table) taken out of the window mid-entrance and put back: the rows waiting or half in must be at rest, not frozen where they were.
    [AvaloniaFact]
    public void Rows_taken_out_of_the_window_mid_entrance_are_at_rest_when_back()
    {
        using var held = new HeldEntrance();
        var (window, scroll, table) = Staggering(held);
        window.Content = null;
        held.Frame(window);
        held.Frame(window);
        window.Content = scroll;
        for (var f = 0; f < 60; f++) held.Frame(window);
        var bad = Hidden(scroll, table);
        Assert.True(bad.Count == 0, string.Join("; ", bad));
    }

    // Rows scrolled out of the view mid-entrance wait hidden for a scroll back (TableRowsPanel); back in view they are at rest, not half way.
    [AvaloniaFact]
    public void Rows_scrolled_away_mid_entrance_are_at_rest_when_scrolled_back()
    {
        using var held = new HeldEntrance();
        var (window, scroll, table) = Staggering(held);
        scroll.Offset = new Vector(0, 2000);
        held.Frame(window, 0);
        held.Frame(window, 0);
        scroll.Offset = new Vector(0, 0);
        held.Frame(window, 0);
        held.Frame(window, 0);
        var bad = Hidden(scroll, table);
        Assert.True(bad.Count == 0, string.Join("; ", bad));
    }

    // The page as «الدفعات» builds it: six rows, then three a frame, while the page's parts rise in; scrolled mid-entrance. No row in view is left hidden.
    [AvaloniaFact]
    public void A_long_table_filled_in_steps_and_scrolled_mid_entrance_shows_every_row_in_view()
    {
        using var held = new HeldEntrance();
        var items = new ObservableCollection<string>(Enumerable.Range(1, 6).Select(i => "دفعة " + i));
        var table = Payments(items);
        var lead = new Border { Height = 160 };
        Entrance.SetEffect(lead, EntranceEffect.Rise);
        var history = new Panel { Children = { table } };
        Entrance.SetEffect(history, EntranceEffect.Rise);
        var scroll = new ScrollViewer { Content = new StackPanel { Spacing = 16, Children = { lead, history } } };
        var window = Track(new Window { Width = 900, Height = 700, FlowDirection = FlowDirection.RightToLeft, Content = scroll });
        window.Show();
        void More()
        {
            if (items.Count >= 40) return;
            window.RequestAnimationFrame(_ =>
            {
                for (var k = 0; k < 3 && items.Count < 40; k++) items.Add("دفعة " + (items.Count + 1));
                More();
            });
        }
        More();
        for (var f = 0; f < 8; f++) held.Frame(window);
        scroll.Offset = new Vector(0, 260);
        for (var f = 0; f < 120; f++) held.Frame(window);

        Assert.Equal(40, table.DisplayedRows.Count);
        var bad = Hidden(scroll, table);
        Assert.True(bad.Count == 0, string.Join("; ", bad));
    }
}
