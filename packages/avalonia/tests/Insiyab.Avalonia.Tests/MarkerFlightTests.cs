using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// One marker motion everywhere, the web's flyMarker: the far edge reaches the new item first, then the near edge follows; none in a still window.
public class MarkerFlightTests
{
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

    // Flights run on a clock this holds and steps, so a slow run sees the same frames as a fast one.
    private sealed class HeldClock : IDisposable
    {
        private readonly Func<double> _real = MarkerFlight.Now;
        private readonly double _from;
        private double _at;

        public HeldClock()
        {
            _from = _at = _real();
            MarkerFlight.Now = () => _at;
        }

        public List<(int Ms, T Value)> Step<T>(Window window, int milliseconds, int every, Func<T> read)
        {
            var samples = new List<(int, T)>();
            for (var ms = 0; ms <= milliseconds; ms += every)
            {
                _at = _from + ms;
                window.InvalidateVisual();
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                samples.Add((ms, read()));
            }
            return samples;
        }

        public void Dispose() => MarkerFlight.Now = _real;
    }

    [Fact]
    public void The_far_edge_arrives_first_then_the_near_edge_follows()
    {
        (double, double) a = (0, 20), b = (100, 30);
        Assert.Equal(a, MarkerFlight.At(0, a, b));
        Assert.Equal(b, MarkerFlight.At(1, a, b));
        var third = MarkerFlight.At(1d / 3, a, b);
        Assert.Equal(0, third.Item1, 6);
        Assert.Equal(130, third.Item2, 6);
        var early = MarkerFlight.At(0.1, a, b);
        Assert.True(early.Item2 < 20 + 0.3 * 110, $"far edge at {early.Item2} too soon");
        var up = MarkerFlight.At(1d / 3, b, a);
        Assert.Equal(0, up.Item1, 6);
        Assert.Equal(130, up.Item2, 6);
    }

    private static TableRow Row(params string[] cells)
    {
        var row = new TableRow();
        foreach (var c in cells) row.Cells.Add(new TableCell { Content = c });
        return row;
    }

    private static Table People()
    {
        var table = new Table { SelectionMode = TableSelectionMode.Single };
        table.Columns.AddRange([new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }]);
        foreach (var (n, c) in new[] { ("ليلى", "الرياض"), ("عمر", "عمّان"), ("سارة", "القاهرة"), ("يوسف", "دمشق") }) table.Rows.Add(Row(n, c));
        return table;
    }

    private static Border? Bar(Table table) =>
        Part<Canvas>(table, "PART_MarkerLayer").Children.OfType<Border>().FirstOrDefault(b => b.Classes.Contains("table-marker-flight"));

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_table_selection_marker_flies_between_rows(string dir)
    {
        var table = People();
        var window = Show(table, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 700, height: 500);
        var rows = table.DisplayedRows;
        Click(rows[0].Cells[0], window);
        Assert.Null(Bar(table));
        // Measured from the rows: a resting marker is scaled to nothing, which would move a point taken through it.
        var layer = Part<Canvas>(table, "PART_MarkerLayer");
        var from = rows[0].TranslatePoint(new Point(0, 8), layer)!.Value;
        var to = Part<Border>(rows[3], "PART_Marker");
        var toAt = rows[3].TranslatePoint(new Point(0, 8), layer)!.Value;

        var p = Center(rows[3].Cells[0], window);
        using var clock = new HeldClock();
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        var samples = clock.Step(window, 700, 5, () => Bar(table) is { } b ? (Top: Canvas.GetTop(b), Bottom: Canvas.GetTop(b) + b.Height, Left: Canvas.GetLeft(b), Moving: table.Classes.Contains("moving")) : (Top: double.NaN, Bottom: double.NaN, Left: double.NaN, Moving: table.Classes.Contains("moving")));
        var flying = samples.Where(s => !double.IsNaN(s.Value.Top)).ToList();
        Assert.NotEmpty(flying);
        Assert.All(flying, s => Assert.True(s.Value.Moving));
        Assert.All(flying, s => Assert.Equal(toAt.X, s.Value.Left, 1));
        // Moving down, the old end holds while the far edge reaches the new row, then lets go.
        var trace = $"from {from.Y} to {toAt.Y}+{to.Bounds.Height}: " + string.Join(" ", flying.Select(s => $"{s.Ms}:{s.Value.Top:0}-{s.Value.Bottom:0}"));
        Assert.True(flying.Where(s => s.Ms < 100).All(s => Math.Abs(s.Value.Top - from.Y) <= 1.5), trace);
        var bottom = toAt.Y + to.Bounds.Height;
        Assert.True(flying.Any(s => s.Value.Bottom >= bottom - 1.5), $"bottom {bottom} from {from.Y} samples " + string.Join(" ", flying.Select(s => $"{s.Ms}:{s.Value.Top:0}-{s.Value.Bottom:0}")));
        var reached = flying.First(s => s.Value.Bottom >= bottom - 1.5);
        Assert.InRange(reached.Ms, 80, 400);
        Assert.True(flying.Max(s => s.Value.Bottom) <= bottom + 1.5);
        Assert.Contains(flying, s => Math.Abs(s.Value.Top - toAt.Y) < 3);
        Assert.Null(Bar(table));
        Assert.DoesNotContain("moving", table.Classes);
        Assert.Equal(1, to.Opacity);
    }

    [AvaloniaFact]
    public void No_marker_flies_in_a_still_window()
    {
        var table = People();
        var tabs = new TabControl { Items = { new TabItem { Header = "الأول", Content = "أ" }, new TabItem { Header = "الثاني", Content = "ب" }, new TabItem { Header = "الثالث", Content = "ج" } } };
        var seg = new Segmented { Items = { new SegmentedItem { Content = "اليوم" }, new SegmentedItem { Content = "الأسبوع" } } };
        var nav = new ListBox { Classes = { "shell-nav" }, Width = 240, Items = { new ListBoxItem { Content = "الرئيسية" }, new ListBoxItem { Content = "الطلبات" } } };
        var window = Show(new StackPanel { Spacing = 12, Children = { table, tabs, seg, nav } }, FlowDirection.RightToLeft, width: 800, height: 900);
        Motion.SetStill(window, true);
        var rows = table.DisplayedRows;
        Click(rows[0].Cells[0], window);
        nav.SelectedIndex = 0;
        Settle(200);

        Click(rows[2].Cells[0], window);
        tabs.SelectedIndex = 2;
        seg.SelectedIndex = 1;
        nav.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        Assert.Null(Bar(table));
        foreach (var c in new Control[] { table, tabs, seg, nav }) Assert.DoesNotContain("moving", c.Classes);
        Assert.False(Part<Border>(tabs, "PART_Flight").IsVisible);
    }

    [AvaloniaFact]
    public void Tabs_fly_as_the_sidebar_does()
    {
        var tabs = new TabControl { Items = { new TabItem { Header = "الأول", Content = "أ" }, new TabItem { Header = "الثاني", Content = "ب" }, new TabItem { Header = "الثالث", Content = "ج" } } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { tabs } }, FlowDirection.LeftToRight, width: 700, height: 400);
        var face = Part<Border>(tabs, "PART_Flight");
        var first = (TabItem)tabs.ContainerFromIndex(0)!;
        var third = (TabItem)tabs.ContainerFromIndex(2)!;
        var start = first.Bounds.Width;
        var span = third.Bounds.Right - first.Bounds.Left;
        tabs.SelectedIndex = 2;
        var samples = Sample(window, 800, () => face.IsVisible ? face.Width : double.NaN);
        var widest = samples.Where(s => !double.IsNaN(s.Value)).MaxBy(s => s.Value);
        Assert.InRange(widest.Ms, 120, 380);
        Assert.InRange(widest.Value, start + 0.6 * (span - start), span + 0.5);
        Assert.False(face.IsVisible);
    }
}
