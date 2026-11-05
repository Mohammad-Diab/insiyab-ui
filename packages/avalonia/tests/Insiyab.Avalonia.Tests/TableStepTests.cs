using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A table over a live list takes each change in place: the other rows, the selection, the keyboard and a press all stay; and its columns take fixed and star widths.
public class TableStepTests
{
    public sealed class Order(int id, string status) : INotifyPropertyChanged
    {
        private string _status = status;

        public int Id { get; } = id;

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                Changed();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static ObservableCollection<Order> Orders(int count) => new(Enumerable.Range(1, count).Select(i => new Order(i, i % 2 == 0 ? "مكتمل" : "جديد")));

    private static Table Live(IEnumerable<Order> items, bool sticky = false)
    {
        var table = new Table { ItemsSource = items };
        if (sticky)
        {
            table.Classes.Add("sticky");
            table.MaxHeight = 400;
        }
        table.Columns.Add(new TableColumn { Header = "الرقم", CellText = o => ((Order)o!).Id.ToString(), Sort = TableSort.Auto });
        table.Columns.Add(new TableColumn { Header = "الحالة", CellText = o => ((Order)o!).Status });
        return table;
    }

    private static string Ids(Table t) => string.Join(",", t.DisplayedRows.Select(r => ((Order)r.Item!).Id));

    [AvaloniaFact]
    public void An_added_item_makes_one_row_and_keeps_the_others()
    {
        var items = Orders(3);
        var table = Live(items);
        Show(table, width: 600);
        var before = table.DisplayedRows.ToList();
        items.Add(new Order(4, "جديد"));
        items.Insert(1, new Order(9, "جديد"));
        Settle(100);
        Assert.Equal("1,9,2,3,4", Ids(table));
        Assert.Same(before[0], table.DisplayedRows[0]);
        Assert.Same(before[1], table.DisplayedRows[2]);
        Assert.Same(before[2], table.DisplayedRows[3]);
    }

    [AvaloniaFact]
    public void A_removed_item_takes_only_its_row()
    {
        var items = Orders(4);
        var table = Live(items);
        Show(table, width: 600);
        var before = table.DisplayedRows.ToList();
        items.RemoveAt(1);
        Settle(100);
        Assert.Equal("1,3,4", Ids(table));
        Assert.Equal([before[0], before[2], before[3]], table.DisplayedRows);
        Assert.False(before[1].IsAttachedToVisualTree());
    }

    [AvaloniaFact]
    public void A_moved_item_moves_its_row()
    {
        var items = Orders(4);
        var table = Live(items);
        Show(table, width: 600);
        var first = table.DisplayedRows[0];
        items.Move(0, 2);
        Settle(100);
        Assert.Equal("2,3,1,4", Ids(table));
        Assert.Same(first, table.DisplayedRows[2]);
    }

    [AvaloniaFact]
    public void A_replaced_item_keeps_its_row_its_selection_and_the_keyboard()
    {
        var items = Orders(3);
        var table = Live(items);
        var window = Show(table, width: 600);
        var row = table.DisplayedRows[1];
        Click(row.Cells[1], window);
        Assert.True(row.IsSelected);
        Assert.Same(row, table.CurrentRow);
        var raised = 0;
        table.SelectionChanged += (_, _) => raised++;
        items[1] = new Order(2, "ملغى");
        Settle(100);
        Assert.Same(row, table.DisplayedRows[1]);
        Assert.Same(items[1], row.Item);
        Assert.True(row.IsSelected);
        Assert.Same(row, table.CurrentRow);
        Assert.Equal("ملغى", row.Cells[1].Text);
        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void A_press_on_a_row_being_replaced_still_clicks()
    {
        var items = Orders(3);
        var table = Live(items);
        var clicks = 0;
        table.Columns.Add(new TableColumn
        {
            Header = "",
            IsFit = true,
            CellTemplate = new FuncDataTemplate<Order>((_, _) =>
            {
                var b = new Button { Content = "فتح" };
                b.Click += (_, _) => clicks++;
                return b;
            }, supportsRecycling: true),
        });
        var window = Show(table, width: 600);
        var button = table.DisplayedRows[0].GetVisualDescendants().OfType<Button>().Single();
        var at = Center(button, window);
        window.MouseMove(at);
        window.MouseDown(at, MouseButton.Left);
        items[0] = new Order(1, "ملغى");
        Settle(100);
        window.MouseUp(at, MouseButton.Left);
        Settle(100);
        Assert.Equal(1, clicks);
        Assert.Same(button, table.DisplayedRows[0].GetVisualDescendants().OfType<Button>().Single());
    }

    [AvaloniaFact]
    public void Taking_the_current_or_a_selected_row_away_lets_them_go()
    {
        var items = Orders(3);
        var table = Live(items);
        var window = Show(table, width: 600);
        Click(table.DisplayedRows[2].Cells[0], window);
        TableSelectionEventArgs? last = null;
        table.SelectionChanged += (_, e) => last = e;
        items.RemoveAt(2);
        Settle(100);
        Assert.Null(table.CurrentRow);
        Assert.NotNull(last);
        Assert.Equal(0, last!.Count);
    }

    [AvaloniaFact]
    public void An_item_added_while_sorted_goes_to_its_sorted_place()
    {
        var items = new ObservableCollection<Order>([new Order(10, "أ"), new Order(30, "ب"), new Order(20, "ج")]);
        var table = Live(items);
        Show(table, width: 600);
        table.Sort(table.Columns[0], SortDirection.Descending);
        Assert.Equal("30,20,10", Ids(table));
        items.Add(new Order(25, "د"));
        items.Insert(0, new Order(5, "هـ"));
        Settle(100);
        Assert.Equal("30,25,20,10,5", Ids(table));
        items[2] = new Order(40, "و");
        Settle(100);
        Assert.Equal("40,25,20,10,5", Ids(table));
    }

    [AvaloniaFact]
    public void A_refill_keeps_the_rows_and_the_selection_of_items_still_there()
    {
        var items = Orders(4).ToList();
        var table = Live(items);
        var window = Show(table, width: 600);
        var kept = table.DisplayedRows[2];
        Click(kept.Cells[0], window);
        table.ItemsSource = new List<Order> { items[0], items[2] };
        Settle(100);
        Assert.Same(kept, table.DisplayedRows[1]);
        Assert.True(kept.IsSelected);
    }

    [AvaloniaFact]
    public void Rows_beside_a_change_take_their_new_places()
    {
        var items = Orders(4);
        var table = Live(items);
        Show(table, width: 600);
        items.Insert(0, new Order(0, "جديد"));
        items.Add(new Order(5, "جديد"));
        items.RemoveAt(3);
        Settle(100);
        var rows = table.DisplayedRows;
        for (var i = 0; i < rows.Count; i++)
        {
            Assert.Equal(i == 0, rows[i].Classes.Contains(":first"));
            Assert.Equal(i == rows.Count - 1, rows[i].Classes.Contains(":last"));
            Assert.Equal(i % 2 == 1, rows[i].Classes.Contains(":stripe"));
        }
    }

    [AvaloniaFact]
    public void A_big_table_taking_rows_one_at_a_time_keeps_only_a_screenful_in_the_tree()
    {
        var items = Orders(2000);
        var table = Live(items, sticky: true);
        Show(table, width: 600, height: 500);
        var first = table.DisplayedRows[0];
        for (var i = 0; i < 20; i++)
        {
            items.Add(new Order(3000 + i, "جديد"));
            items.Insert(0, new Order(-i, "جديد"));
            items.RemoveAt(1000);
        }
        Settle(200);
        Assert.Equal(2020, table.DisplayedRows.Count);
        Assert.Same(first, table.DisplayedRows[20]);
        Assert.InRange(table.GetVisualDescendants().OfType<TableRow>().Count(), 1, 80);
    }

    [AvaloniaFact]
    public void Rows_written_by_hand_are_added_in_place_too()
    {
        var table = new Table();
        table.Columns.Add(new TableColumn { Header = "الاسم" });
        var a = new TableRow { Cells = { new TableCell { Content = "ليلى" } } };
        var b = new TableRow { Cells = { new TableCell { Content = "عمر" } } };
        table.Rows.Add(a);
        table.Rows.Add(b);
        var window = Show(table, width: 600);
        Click(b.Cells[0], window);
        table.Rows.Insert(0, new TableRow { Cells = { new TableCell { Content = "سارة" } } });
        table.Rows.Remove(a);
        Settle(100);
        Assert.Equal(2, table.DisplayedRows.Count);
        Assert.Same(b, table.DisplayedRows[1]);
        Assert.True(b.IsSelected);
        Assert.Same(b, table.CurrentRow);
    }

    [AvaloniaFact]
    public void A_bound_column_follows_an_item_that_changes_in_place_and_sorts_rows_never_shown()
    {
        var items = Orders(300);
        var table = new Table { ItemsSource = items, Classes = { "sticky" }, MaxHeight = 300 };
        table.Columns.Add(new TableColumn { Header = "الرقم", CellBinding = new Binding(nameof(Order.Id)), Sort = TableSort.Auto });
        table.Columns.Add(new TableColumn { Header = "الحالة", CellBinding = new Binding(nameof(Order.Status)) });
        Show(table, width: 600, height: 400);
        Assert.Equal("جديد", table.DisplayedRows[0].Cells[1].Text);
        items[0].Status = "ملغى";
        Settle(50);
        Assert.Equal("ملغى", table.DisplayedRows[0].Cells[1].Text);
        table.Sort(table.Columns[0], SortDirection.Descending);
        Assert.Equal("300", table.DisplayedRows[0].Cells[0].Text);
    }

    private static Table Widths(double width, params TableColumn[] columns)
    {
        var table = new Table { Width = width };
        table.Columns.AddRange(columns);
        table.Rows.Add(new TableRow { Cells = { new TableCell { Content = "1" }, new TableCell { Content = "قصير" }, new TableCell { Content = "نص" } } });
        table.Rows.Add(new TableRow { Cells = { new TableCell { Content = "2" }, new TableCell { Content = "نص أطول قليلًا من غيره" }, new TableCell { Content = "آخر" } } });
        return table;
    }

    [AvaloniaFact]
    public void Pixel_and_star_columns_share_the_width()
    {
        var table = Widths(640,
            new TableColumn { Header = "الرقم", Width = new GridLength(90) },
            new TableColumn { Header = "العنوان", Width = new GridLength(1, GridUnitType.Star) },
            new TableColumn { Header = "الملاحظة", Width = new GridLength(2, GridUnitType.Star) });
        Show(table, width: 700);
        var cells = table.DisplayedRows[1].Cells;
        Assert.Equal(90, cells[0].Bounds.Width, 1);
        var heads = table.GetVisualDescendants().OfType<TableHeader>().ToList();
        Assert.Equal(90, heads[0].Bounds.Width, 1);
        var one = cells[1].Bounds.Width;
        var two = cells[2].Bounds.Width;
        var before = table.ColumnWidths.Natural();
        var spare = cells.Sum(c => c.Bounds.Width) - before.Sum();
        Assert.True(spare > 50, $"spare {spare}");
        Assert.Equal(before[1] + spare / 3, one, 1.5);
        Assert.Equal(before[2] + spare * 2 / 3, two, 1.5);
    }

    [AvaloniaFact]
    public void A_star_column_never_goes_under_its_heading()
    {
        var table = Widths(260,
            new TableColumn { Header = "الرقم", Width = new GridLength(160) },
            new TableColumn { Header = "عنوان طويل للعمود", Width = new GridLength(1, GridUnitType.Star) },
            new TableColumn { Header = "آخر", Width = new GridLength(60) });
        Show(table, width: 700);
        var head = table.GetVisualDescendants().OfType<TableHeader>().ElementAt(1);
        Assert.True(head.Bounds.Width >= head.DesiredSize.Width - 0.5, $"{head.Bounds.Width} < {head.DesiredSize.Width}");
        Assert.Equal(head.Bounds.Width, table.DisplayedRows[0].Cells[1].Bounds.Width, 1);
        Assert.Equal(60, table.DisplayedRows[0].Cells[2].Bounds.Width, 1);
    }

    [AvaloniaFact]
    public void A_width_set_later_lays_the_columns_out_again()
    {
        var column = new TableColumn { Header = "العنوان" };
        var table = Widths(640, new TableColumn { Header = "الرقم" }, column, new TableColumn { Header = "الملاحظة" });
        Show(table, width: 700);
        column.Width = new GridLength(200);
        Settle(100);
        Assert.Equal(200, table.DisplayedRows[0].Cells[1].Bounds.Width, 1);
    }
}
