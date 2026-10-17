using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class DataMotionTests
{
    // The B1 switch that turns motion off; if B1 named its setter differently, this is the one line to change.
    private static void Still(Window w, bool on) => Motion.SetStill(w, on);

    private static List<(long Ms, T Value)> Sample<T>(Window window, int milliseconds, Func<T> read)
    {
        var samples = new List<(long, T)>();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            window.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            samples.Add((clock.ElapsedMilliseconds, read()));
            Thread.Sleep(6);
        }
        return samples;
    }

    private static Window Open(Control content) =>
        Track(new Window { Width = 700, Height = 520, FlowDirection = FlowDirection.RightToLeft, Content = content });

    private static double Y(Control c) => c.RenderTransform?.Value.M32 ?? 0;

    private static Table Cities(int count)
    {
        var table = new Table();
        table.Columns.Add(new TableColumn { Header = "المدينة" });
        table.Columns.Add(new TableColumn { Header = "القيمة", Sort = TableSort.Auto });
        for (var i = 0; i < count; i++)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell { Content = "مدينة " + (i + 1) });
            row.Cells.Add(new TableCell { Content = ((count - i) * 10).ToString() });
            table.Rows.Add(row);
        }
        return table;
    }

    // Entrances run on a clock this holds and steps, so a slow run sees the same frames as a fast one.
    private sealed class HeldEntrance : IDisposable
    {
        private readonly Func<double> _real = Entrance.Now;
        private double _at = 1000;

        public HeldEntrance() => Entrance.Now = () => _at;

        public void To(Window window, double milliseconds)
        {
            _at = 1000 + milliseconds;
            for (var i = 0; i < 2; i++)
            {
                window.InvalidateVisual();
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            }
        }

        public void Dispose() => Entrance.Now = _real;
    }

    private static int InView(Window window, IReadOnlyList<TableRow> rows) =>
        rows.Count(r => r.IsEffectivelyVisible && r.TranslatePoint(new Point(0, 0), window) is { } p && p.Y < window.Bounds.Height);

    [AvaloniaFact]
    public void Every_row_in_view_rises_and_the_last_starts_by_320ms()
    {
        using var held = new HeldEntrance();
        var table = Cities(30);
        var window = Show(table, FlowDirection.RightToLeft, width: 700, height: 900);
        var rows = table.DisplayedRows;
        var n = InView(window, rows);
        Assert.True(n >= 12 && n < rows.Count, $"{n} rows in view");
        var step = Math.Min(40, 320.0 / (n - 1));
        var times = new SortedSet<double>();
        for (var t = 0; t <= 900; t += 5) times.Add(t);
        for (var k = 1; k < n; k++)
        {
            times.Add(k * step - 5);
            times.Add(k * step + 140);
        }
        times.Add(350);
        var seen = new Dictionary<double, double[]>();
        var moved = new Dictionary<double, double[]>();
        foreach (var t in times)
        {
            held.To(window, t);
            seen[t] = rows.Select(r => r.Opacity).ToArray();
            moved[t] = rows.Select(Y).ToArray();
        }
        for (var k = 1; k < n; k++)
        {
            Assert.Equal(0, seen[k * step - 5][k], 3);
            var during = seen[k * step + 140][k];
            Assert.True(during > 0 && during < 1, $"row {k} at {k * step + 140}ms has opacity {during}");
        }
        Assert.True(seen[350][n - 1] > 0, $"the last row in view has opacity {seen[350][n - 1]} at 350ms");
        foreach (var t in times)
            for (var k = n; k < rows.Count; k++)
            {
                Assert.Equal(1, seen[t][k], 3);
                Assert.Equal(0, moved[t][k], 3);
            }
        Assert.All(seen[900], o => Assert.Equal(1, o, 3));
        Assert.All(moved[900], y => Assert.Equal(0, y, 3));
    }

    [AvaloniaFact]
    public void Eight_rows_in_view_rise_40ms_apart()
    {
        using var held = new HeldEntrance();
        var table = Cities(8);
        var window = Show(table, FlowDirection.RightToLeft, width: 700, height: 900);
        var rows = table.DisplayedRows;
        Assert.Equal(8, InView(window, rows));
        double? start = null;
        for (var t = 0; t <= 600; t += 5)
        {
            held.To(window, t);
            if (start is null && rows[7].Opacity > 0) start = t;
        }
        Assert.NotNull(start);
        Assert.InRange(start!.Value, 270, 290);
        Assert.All(rows, r => Assert.Equal(1, r.Opacity, 3));
    }

    [AvaloniaFact]
    public void The_sixth_row_starts_last()
    {
        var table = Cities(0);
        var window = Show(table, FlowDirection.RightToLeft, width: 700);
        var filled = Cities(6);
        var rows = filled.Rows.ToList();
        filled.Rows.Clear();
        table.Rows.AddRange(rows);
        var samples = Sample(window, 600, () => rows.Select(r => r.Opacity).ToArray());
        Assert.All(samples, s =>
        {
            for (var i = 1; i < 6; i++) Assert.True(s.Value[i] <= s.Value[i - 1] + 0.001, $"at {s.Ms}ms row {i} is ahead of row {i - 1}");
        });
        Assert.Contains(samples, s => s.Value[0] > 0.5 && s.Value[5] == 0);
        Assert.All(samples[^1].Value, o => Assert.Equal(1, o));
    }

    [AvaloniaFact]
    public void A_sort_moves_rows_without_replaying_their_arrival()
    {
        var table = Cities(4);
        var window = Show(table, FlowDirection.RightToLeft, width: 700);
        table.Sort(table.Columns[1], SortDirection.Ascending);
        var samples = Sample(window, 200, () => table.DisplayedRows.Min(r => r.Opacity));
        Assert.All(samples, s => Assert.Equal(1, s.Value));
    }

    [AvaloniaFact]
    public void Only_a_new_row_arrives_when_the_items_change()
    {
        var items = new ObservableCollection<string> { "الرياض", "عمّان", "القاهرة" };
        var table = new Table { ItemsSource = items };
        table.Columns.Add(new TableColumn { Header = "المدينة", CellText = o => o as string });
        var window = Show(table, FlowDirection.RightToLeft, width: 700);
        items.Insert(1, "تونس");
        var rows = table.DisplayedRows;
        Assert.Equal("تونس", rows[1].Item);
        var samples = Sample(window, 120, () => (Old: rows[0].Opacity + rows[2].Opacity + rows[3].Opacity, New: rows[1].Opacity));
        Assert.All(samples, s => Assert.Equal(3, s.Value.Old, 3));
        Assert.Contains(samples, s => s.Value.New < 0.9);
    }

    [AvaloniaFact]
    public void Rows_do_not_move_in_a_still_window()
    {
        var table = Cities(3);
        var window = Open(table);
        Still(window, true);
        window.Show();
        var samples = Sample(window, 120, () => table.DisplayedRows.Min(r => r.Opacity));
        Assert.All(samples, s => Assert.Equal(1, s.Value));
    }

    [AvaloniaFact]
    public void The_bulk_bar_rises_in_each_time_it_comes_up()
    {
        var table = Cities(3);
        table.IsSelectable = true;
        var bar = new BulkBar { Table = table };
        var window = Show(new StackPanel { Children = { table, bar } }, FlowDirection.RightToLeft, width: 700);
        Assert.False(bar.IsVisible);
        table.DisplayedRows[0].IsSelected = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(bar.IsVisible);
        var samples = Sample(window, 400, () => (Y: Y(bar), O: bar.Opacity));
        Assert.True(samples[0].Value.O < 0.9, $"opacity {samples[0].Value.O}");
        Assert.InRange(samples[0].Value.Y, 0.5, 6.01);
        Assert.Equal(1, samples[^1].Value.O, 3);
        Assert.Equal(0, samples[^1].Value.Y, 3);

        table.DisplayedRows[1].IsSelected = true;
        Dispatcher.UIThread.RunJobs();
        var steady = Sample(window, 60, () => bar.Opacity);
        Assert.All(steady, s => Assert.Equal(1, s.Value, 3));

        table.SelectAll(false);
        Dispatcher.UIThread.RunJobs();
        Assert.False(bar.IsVisible);
        table.DisplayedRows[2].IsSelected = true;
        Dispatcher.UIThread.RunJobs();
        var again = Sample(window, 60, () => bar.Opacity);
        Assert.Contains(again, s => s.Value < 0.9);
    }

    [AvaloniaFact]
    public void A_grid_reveal_can_step_70ms_for_the_first_eight()
    {
        var grid = new UniformGrid { Columns = 5 };
        for (var i = 0; i < 10; i++) grid.Children.Add(new Border { Height = 40 });
        Entrance.SetReveal(grid, true);
        Entrance.SetRevealStep(grid, TimeSpan.FromMilliseconds(70));
        Entrance.SetRevealCount(grid, 8);
        var window = Open(grid);
        window.Show();
        var kids = grid.Children.ToList();
        var samples = Sample(window, 1000, () => kids.Select(Y).ToArray());
        Assert.All(samples, s =>
        {
            for (var i = 1; i < 8; i++) Assert.True(s.Value[i] >= s.Value[i - 1] - 0.001, $"at {s.Ms}ms child {i} is ahead of child {i - 1}");
            Assert.InRange(Math.Abs(s.Value[8] - s.Value[0]), 0, 0.5);
            Assert.InRange(Math.Abs(s.Value[9] - s.Value[0]), 0, 0.5);
        });
        Assert.Contains(samples, s => s.Value[0] < 3 && s.Value[7] > 11.9);
        Assert.All(samples[^1].Value, y => Assert.Equal(0, y, 3));
    }
}
