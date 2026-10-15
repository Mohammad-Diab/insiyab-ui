using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using WindowDecorationProperties = Avalonia.Controls.Chrome.WindowDecorationProperties;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// What the performance pass fixed: big tables, controls kept alive after their page left, and per-control style costs.
public class PerfTests
{
    private static Table BigTable(int count)
    {
        var table = new Table { Classes = { "sticky" }, MaxHeight = 400 };
        table.Columns.Add(new TableColumn { Header = "الرقم", CellText = o => o?.ToString() });
        table.Columns.Add(new TableColumn { Header = "الحالة", CellText = o => (int)o! % 2 == 0 ? "مكتمل" : "جديد" });
        table.ItemsSource = Enumerable.Range(1, count).ToList();
        return table;
    }

    [AvaloniaFact]
    public void Table_of_thousands_of_rows_puts_only_a_screenful_in_the_tree()
    {
        var table = BigTable(5000);
        Show(table, width: 600, height: 500);
        Settle(200);
        var rows = table.GetVisualDescendants().OfType<TableRow>().Count();
        Assert.InRange(rows, 1, 60);
        Assert.Equal(5000, table.DisplayedRows.Count);
    }

    [AvaloniaFact]
    public void Table_scrolled_to_its_end_brings_the_last_rows_and_drops_the_first()
    {
        var table = BigTable(2000);
        var window = Show(table, width: 600, height: 500);
        table.Focus();
        Settle(200);
        window.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        window.KeyRelease(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        Settle(300);
        var last = table.DisplayedRows[^1];
        Assert.Same(last, table.CurrentRow);
        Assert.True(last.IsAttachedToVisualTree());
        var first = table.DisplayedRows[0];
        Assert.False(first.IsAttachedToVisualTree() && first.IsVisible);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        var bottom = last.TranslatePoint(new Point(0, last.Bounds.Height), scroll)!.Value.Y;
        Assert.True(bottom <= scroll.Viewport.Height + 1, $"last row ends at {bottom}, view is {scroll.Viewport.Height}");
    }

    [AvaloniaFact]
    public void Table_rows_out_of_the_tree_keep_their_stripes_by_place()
    {
        var table = BigTable(300);
        Show(table, width: 600, height: 500);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        scroll.Offset = new Vector(0, 3000);
        Settle(300);
        var shown = table.GetVisualDescendants().OfType<TableRow>().Where(r => r.IsVisible).ToList();
        Assert.NotEmpty(shown);
        var all = table.DisplayedRows.ToList();
        Assert.DoesNotContain(all[0], shown);
        foreach (var row in shown)
            Assert.Equal(all.IndexOf(row) % 2 == 1, Part<Border>(row, "PART_Tint").Background is not null);
    }

    private static int Hidden(Table table) => Part<TableRowsPanel>(table, "PART_Rows").Children.OfType<TableRow>().Count(r => !r.IsVisible);

    [AvaloniaFact]
    public void Rows_scrolled_past_come_back_without_leaving_the_tree()
    {
        var table = BigTable(300);
        Show(table, width: 600, height: 500);
        Settle(200);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        var detached = 0;
        foreach (var row in table.DisplayedRows) row.DetachedFromVisualTree += (_, _) => detached++;
        scroll.Offset = new Vector(0, scroll.Viewport.Height);
        Settle(100);
        Assert.True(Hidden(table) > 0, "rows scrolled past wait hidden");
        scroll.Offset = default;
        Settle(100);
        Assert.Equal(0, detached);
        Assert.Contains(table.DisplayedRows[0], Part<TableRowsPanel>(table, "PART_Rows").Children);
        Assert.True(table.DisplayedRows[0].IsVisible);
    }

    [AvaloniaFact]
    public void A_row_coming_or_going_relinks_at_most_a_block_of_eight_rows()
    {
        var table = BigTable(300);
        Show(table, width: 600, height: 500);
        Settle(200);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        var panel = Part<TableRowsPanel>(table, "PART_Rows");
        for (var i = 1; i <= 4; i++)
        {
            scroll.Offset = new Vector(0, i * scroll.Viewport.Height / 2);
            Settle(50);
            var rows = panel.Children.OfType<TableRow>().ToList();
            Assert.Contains(rows, r => !r.IsVisible);
            Assert.Equal(rows.Count, table.GetVisualDescendants().OfType<TableRow>().Count());
            Assert.All(rows, r => Assert.InRange(r.GetVisualParent()!.GetVisualChildren().Count(), 1, 8));
        }
    }

    [AvaloniaFact]
    public void Hidden_rows_leave_the_tree_once_the_scroll_rests()
    {
        var table = BigTable(300);
        Show(table, width: 600, height: 500);
        Settle(200);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        var first = table.DisplayedRows[0];
        scroll.Offset = new Vector(0, scroll.Viewport.Height * 2);
        Settle(100);
        Assert.True(Hidden(table) > 0);
        Assert.True(first.IsAttachedToVisualTree());
        Settle(900);
        Assert.Equal(0, Hidden(table));
        Assert.False(first.IsAttachedToVisualTree());
        Assert.True(first.IsVisible);
        Assert.Equal(Part<TableRowsPanel>(table, "PART_Rows").Children.Count, table.GetVisualDescendants().OfType<TableRow>().Count());
        scroll.Offset = default;
        Settle(200);
        Assert.True(first.IsAttachedToVisualTree());
        Assert.True(first.Bounds.Height > 10, $"row came back {first.Bounds.Height} high");
    }

    [AvaloniaFact]
    public void The_waiting_rows_never_pass_their_cap()
    {
        var table = BigTable(2000);
        Show(table, width: 600, height: 500);
        Settle(200);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        var panel = Part<TableRowsPanel>(table, "PART_Rows");
        var most = 0;
        for (var i = 1; i <= 60; i++)
        {
            scroll.Offset = new Vector(0, i * 90);
            Settle(20);
            var rows = panel.Children.OfType<TableRow>().ToList();
            var hidden = rows.Count(r => !r.IsVisible);
            most = Math.Max(most, hidden);
            Assert.True(hidden <= Math.Max(32, 2 * (rows.Count - hidden)), $"{hidden} waiting beside {rows.Count - hidden} shown");
        }
        Assert.True(most > 8, $"at most {most} rows waited");
    }

    [AvaloniaFact]
    public void A_sort_moves_only_rows_in_view()
    {
        var table = BigTable(300);
        Show(table, width: 600, height: 500);
        Settle(200);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        scroll.Offset = new Vector(0, scroll.Viewport.Height);
        Settle(100);
        var panel = Part<TableRowsPanel>(table, "PART_Rows");
        var waiting = panel.Children.OfType<TableRow>().Where(r => !r.IsVisible).ToList();
        Assert.NotEmpty(waiting);
        table.Sort(table.Columns[0], SortDirection.Descending);
        static bool Moved(Control r) => r.RenderTransform is { } t && !t.Value.IsIdentity;
        Assert.All(waiting, r => Assert.False(Moved(r), "a hidden row was moved"));
        Assert.Contains(panel.Children.OfType<TableRow>(), r => r.IsVisible && Moved(r));
    }

    [AvaloniaFact]
    public void A_row_back_from_waiting_is_pinned_with_the_frozen_columns()
    {
        var table = BigTable(300);
        table.FrozenColumns = 1;
        table.TableMinWidth = 1000;
        Show(table, width: 600, height: 500);
        Settle(200);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        var first = table.DisplayedRows[0];
        scroll.Offset = new Vector(0, scroll.Viewport.Height);
        Settle(100);
        Assert.False(first.IsVisible, "the first row waits hidden");
        scroll.Offset = new Vector(150, scroll.Offset.Y);
        Settle(100);
        scroll.Offset = new Vector(150, 0);
        Settle(100);
        Assert.True(first.IsVisible);
        Assert.Equal(150, Assert.IsType<TranslateTransform>(first.Cells[0].RenderTransform).X, 0);
        Assert.Null(first.Cells[1].RenderTransform);
    }

    [AvaloniaFact]
    public void Table_cell_text_is_template_text()
    {
        var table = BigTable(3);
        Show(table);
        var cell = table.DisplayedRows[0].Cells[0];
        Assert.IsType<TemplateText>(cell.GetVisualDescendants().OfType<TextBlock>().Single());
        Assert.Equal("1", cell.Text);
    }

    [AvaloniaFact]
    public void Template_parts_are_template_borders_and_app_borders_keep_their_classes()
    {
        var button = new Button { Content = "حفظ" };
        var card = new Border { Classes = { "card" }, Child = button };
        Show(card);
        Assert.DoesNotContain(button.GetVisualDescendants(), v => v.GetType() == typeof(Border));
        Assert.NotEqual(default, card.CornerRadius);
    }

    // The control is made in here, so a debug build's caller holds no hidden reference to it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Shown(Panel host, Func<Control> make)
    {
        var control = make();
        host.Children.Add(control);
        Settle(300);
        host.Children.Remove(control);
        return new WeakReference(control);
    }

    private static bool Gone(WeakReference weak)
    {
        Settle(500);
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        return !weak.IsAlive;
    }

    [AvaloniaFact]
    public void Spinner_skeleton_and_progress_bar_are_let_go_once_removed()
    {
        var host = new StackPanel();
        Show(host);
        Assert.True(Gone(Shown(host, () => new LoadingSpinner())), "spinner");
        Assert.True(Gone(Shown(host, () => new Skeleton { Height = 12 })), "skeleton");
        Assert.True(Gone(Shown(host, () => new ProgressBar { Value = 40 })), "progress bar");
        Assert.True(Gone(Shown(host, () => new ProgressBar { IsIndeterminate = true })), "indeterminate bar");
    }

    [AvaloniaFact]
    public void Determinate_bar_runs_no_hidden_sweep()
    {
        var bar = new ProgressBar { Value = 40, Width = 200 };
        Show(bar);
        var sweep = Part<Shimmer>(bar, "IndeterminateRoot");
        Settle(400);
        Assert.False(sweep.IsVisible);
        Assert.Equal(0, sweep.Phase);
        bar.IsIndeterminate = true;
        Settle(400);
        Assert.True(sweep.Phase > 0);
    }

    [AvaloniaFact]
    public void Viewbox_in_a_shell_is_let_go_once_removed()
    {
        var host = new StackPanel();
        Show(new SplitView { IsPaneOpen = true, Content = new Border { Classes = { "topbar" }, Child = host } });
        var weak = Shown(host, () => new Viewbox { Width = 16, Height = 16, Child = new TextBlock { Text = "x" } });
        Assert.True(Gone(weak));
    }

    [AvaloniaFact]
    public void Control_added_later_inside_a_drag_area_stays_clickable()
    {
        var bar = new StackPanel();
        TitleBar.SetIsDragArea(bar, true);
        Show(bar);
        var button = new Button { Content = "x" };
        var text = new TextBlock { Text = "y" };
        bar.Children.Add(button);
        bar.Children.Add(text);
        Settle(200);
        Assert.Equal(WindowDecorationsElementRole.User, WindowDecorationProperties.GetElementRole(button));
        Assert.Equal(WindowDecorationsElementRole.None, WindowDecorationProperties.GetElementRole(text));
    }

    [AvaloniaFact]
    public void Month_calendar_taken_out_of_a_right_to_left_page_does_not_throw()
    {
        var host = new ContentControl { Content = new MonthCalendar { Month = new DateTime(2026, 9, 1) } };
        var window = Show(host, FlowDirection.RightToLeft);
        host.Content = null;
        Settle(100);
        host.Content = new MonthCalendar { Month = new DateTime(2026, 9, 1), View = MonthCalendarView.Week };
        Settle(200);
        Assert.NotEmpty(((MonthCalendar)host.Content).GetVisualDescendants());
        window.Close();
    }
}
