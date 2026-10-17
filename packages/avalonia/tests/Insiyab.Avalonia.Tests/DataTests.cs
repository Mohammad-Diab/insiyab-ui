using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class DataTests
{
    private static ThemeVariant Theme(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static TableRow Row(params string[] cells)
    {
        var row = new TableRow();
        foreach (var c in cells) row.Cells.Add(new TableCell { Content = c });
        return row;
    }

    private static Table People(params TableColumn[] columns)
    {
        var table = new Table();
        if (columns.Length == 0) columns = [new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" }];
        table.Columns.AddRange(columns);
        table.Rows.Add(Row("ليلى", "الرياض", "249.00"));
        table.Rows.Add(Row("عمر", "عمّان", "1,180.00"));
        table.Rows.Add(Row("سارة", "القاهرة", "-89.50"));
        return table;
    }

    private static Rect BoundsIn(Visual v, Visual root)
    {
        var a = v.TranslatePoint(new Point(0, 0), root)!.Value;
        var b = v.TranslatePoint(new Point(v.Bounds.Width, v.Bounds.Height), root)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static string[] Names(Table table) => table.DisplayedRows.Select(r => r.Cells[table.IsSelectable ? 1 : 0].Text).ToArray();

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(300);
    }

    private static void Press(Window window, Key key, RawInputModifiers mods)
    {
        window.KeyPress(key, mods, PhysicalKey.None, null);
        window.KeyRelease(key, mods, PhysicalKey.None, null);
        Settle(300);
    }

    private static void ClickWith(Control c, Window w, RawInputModifiers mods)
    {
        var p = Center(c, w);
        w.MouseMove(p, mods);
        w.MouseDown(p, MouseButton.Left, mods);
        w.MouseUp(p, MouseButton.Left, mods);
        Settle(300);
    }

    private static void DoubleClick(Control c, Window w)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle(300);
    }

    private static void RightClick(Control c, Window w)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Right);
        w.MouseUp(p, MouseButton.Right);
        Settle(300);
    }

    private static int[] Selected(Table t) => t.DisplayedRows.Select((r, i) => r.IsSelected ? i : -1).Where(i => i >= 0).ToArray();

    private static Table Five(params TableColumn[] columns)
    {
        var table = People(columns);
        table.Rows.Add(Row("كريم", "دبي", "10"));
        table.Rows.Add(Row("نور", "الدوحة", "20"));
        return table;
    }

    private static Control CellOf(Table t, int row, int cell = 0) => t.DisplayedRows[row].Cells[cell];

    private static ContextMenu RowMenu() => new() { Items = { new MenuItem { Header = "تعديل" }, new MenuItem { Header = "حذف" } } };

    // ---------------------------------------------------------------- table

    [AvaloniaFact]
    public void Table_has_a_header_bar_over_a_card_of_rows()
    {
        var table = People();
        var window = Show(table, width: 700);
        Settle();
        var head = Part<Border>(table, "PART_Bar");
        var card = Part<Border>(table, "PART_Card");
        Assert.Equal(38, head.Bounds.Height, 0);
        Assert.Equal(8, BoundsIn(card, table).Top - BoundsIn(head, table).Bottom, 0);
        Assert.Equal(new CornerRadius(22), card.CornerRadius);
        Assert.Equal(3, table.DisplayedRows.Count);
        Assert.Equal(44, table.DisplayedRows[0].Bounds.Height, 0);
        Assert.Equal(43, table.DisplayedRows[2].Bounds.Height, 0);
        Assert.Equal(3, Part<ColumnsPanel>(table, "PART_HeadCells").Children.Count);
        var header = (TableHeader)Part<ColumnsPanel>(table, "PART_HeadCells").Children[0];
        Assert.Equal(FontWeight.ExtraBold, header.FontWeight);
        Assert.Equal(12, header.FontSize);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_columns_line_up_and_share_the_spare_width(string dir)
    {
        var table = People();
        var window = Show(table, Dir(dir), width: 760);
        Settle();
        var heads = Part<ColumnsPanel>(table, "PART_HeadCells").Children;
        foreach (var row in table.DisplayedRows)
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(heads[i].Bounds.Width, row.Cells[i].Bounds.Width, 0);
                Assert.Equal(BoundsIn(heads[i], window).Left, BoundsIn(row.Cells[i], window).Left, 0);
            }
        Assert.True(heads.Sum(h => h.Bounds.Width) > 700);
        var first = BoundsIn(table.DisplayedRows[0].Cells[0], window);
        var last = BoundsIn(table.DisplayedRows[0].Cells[2], window);
        Assert.Equal(dir == "rtl", first.Left > last.Left);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_fit_column_keeps_its_natural_width()
    {
        var table = People(new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة", IsFit = true }, new TableColumn { Header = "القيمة" });
        var window = Show(table, width: 760);
        Settle();
        var heads = Part<ColumnsPanel>(table, "PART_HeadCells").Children;
        Assert.True(heads[1].Bounds.Width < 110, $"fit column is {heads[1].Bounds.Width}");
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Table_rows_stripe_and_selected_row_takes_the_brand(string theme, string dir)
    {
        var table = People();
        table.Rows[2].IsSelected = true;
        var window = Show(table, Dir(dir), Theme(theme), width: 700);
        Settle();
        AssertBrush(table, "InsSurface", () => Part<Border>(table.DisplayedRows[0], "PART_Fill").Background);
        AssertBrush(table, "InsZebra", () => Part<Border>(table.DisplayedRows[1], "PART_Tint").Background);
        AssertBrush(table, "InsToneBrandBg", () => Part<Border>(table.DisplayedRows[2], "PART_Tint").Background);
        var marker = Part<Border>(table.DisplayedRows[2], "PART_Marker");
        Assert.Equal(3, marker.Bounds.Width);
        Assert.Equal(0, marker.TranslatePoint(default, table.DisplayedRows[2])!.Value.X, 0);
        var markerX = marker.TranslatePoint(new Point(1.5, 2), window)!.Value.X;
        var rowMid = table.DisplayedRows[2].TranslatePoint(new Point(table.DisplayedRows[2].Bounds.Width / 2, 0), window)!.Value.X;
        Assert.Equal(dir == "rtl", markerX > rowMid);
        Assert.False(Part<Border>(table.DisplayedRows[2], "PART_Line").IsVisible);
        Capture(window, $"data-table-{theme}-{dir}");
        window.Close();
    }

    [AvaloniaFact]
    public void Table_row_under_the_pointer_fills_over_the_stripe()
    {
        var table = People();
        var window = Show(table, width: 700);
        var row = table.DisplayedRows[1];
        window.MouseMove(Center(row, window));
        Settle();
        AssertBrush(table, "InsSurfaceHover", () => Part<Border>(row, "PART_Fill").Background);
        Assert.Equal(Colors.Transparent, ColorOf(Part<Border>(row, "PART_Tint").Background));
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_figures_column_aligns_right_in_its_own_direction(string dir)
    {
        var table = People(new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة", Classes = "num" });
        var window = Show(table, Dir(dir), width: 700);
        Settle();
        var cell = table.DisplayedRows[0].Cells[2];
        Assert.Contains("num", cell.Classes);
        Assert.Equal(FlowDirection.LeftToRight, cell.FlowDirection);
        Assert.Equal(FontWeight.SemiBold, cell.FontWeight);
        var text = cell.GetVisualDescendants().OfType<TextBlock>().First();
        var cellAt = BoundsIn(cell, window);
        var textAt = BoundsIn(text, window);
        Assert.Equal(cellAt.Right - 16, textAt.Right, 1);
        var head = (TableHeader)Part<ColumnsPanel>(table, "PART_HeadCells").Children[2];
        Assert.Contains("num", head.Classes);
        Assert.Equal(FlowDirection.LeftToRight, head.FlowDirection);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_sorts_by_a_column_both_ways()
    {
        var table = People(new TableColumn { Header = "الاسم", Sort = TableSort.Auto }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة", Sort = TableSort.Auto, Classes = "num" });
        var window = Show(table, FlowDirection.RightToLeft, width: 700);
        var buttons = table.GetVisualDescendants().OfType<TableSortButton>().ToList();
        Assert.Equal(2, buttons.Count);
        Click(buttons[0], window);
        Assert.Equal(["سارة", "عمر", "ليلى"], Names(table));
        Assert.Equal(SortDirection.Ascending, table.Columns[0].SortDirection);
        Assert.Contains(":ascending", buttons[0].Classes);
        Click(buttons[0], window);
        Assert.Equal(["ليلى", "عمر", "سارة"], Names(table));
        Assert.Contains(":descending", buttons[0].Classes);
        Click(buttons[1], window);
        Assert.Equal(["سارة", "ليلى", "عمر"], Names(table));
        Assert.Equal(SortDirection.None, table.Columns[0].SortDirection);
        Assert.DoesNotContain(":ascending", buttons[0].Classes);
        Settle();
        Assert.Equal(0, table.DisplayedRows[0].Bounds.Y, 0);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_sort_uses_the_sort_value_and_puts_empty_cells_last()
    {
        var table = new Table();
        table.Columns.Add(new TableColumn { Header = "التاريخ", Sort = TableSort.Auto });
        table.Rows.Add(new TableRow { Cells = { new TableCell { Content = "15 يناير", SortValue = "2026-01-15" } } });
        table.Rows.Add(new TableRow { Cells = { new TableCell { Content = "" } } });
        table.Rows.Add(new TableRow { Cells = { new TableCell { Content = "2 مارس", SortValue = "2026-03-02" } } });
        table.Rows.Add(new TableRow { Cells = { new TableCell { Content = "20 فبراير", SortValue = "2026-02-20" } } });
        var window = Show(table, width: 500);
        table.Sort(table.Columns[0]);
        Assert.Equal(["15 يناير", "20 فبراير", "2 مارس", ""], Names(table));
        table.Sort(table.Columns[0]);
        Assert.Equal(["2 مارس", "20 فبراير", "15 يناير", ""], Names(table));
        window.Close();
    }

    [AvaloniaFact]
    public void Table_sort_cancelled_moves_only_the_arrow()
    {
        var table = People(new TableColumn { Header = "الاسم", Sort = TableSort.Auto }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" });
        TableSortingEventArgs? seen = null;
        table.Sorting += (_, e) => { seen = e; e.Cancel = true; };
        var window = Show(table, width: 700);
        table.Sort(table.Columns[0]);
        Assert.NotNull(seen);
        Assert.Equal(0, seen!.ColumnIndex);
        Assert.Equal(SortDirection.Ascending, seen.Direction);
        Assert.Equal(["ليلى", "عمر", "سارة"], Names(table));
        Assert.Equal(SortDirection.Ascending, table.Columns[0].SortDirection);
        window.Close();
    }

    [Theory]
    [InlineData("1,180.00", 1180)]
    [InlineData("-89.50", -89.5)]
    [InlineData("١٢٣", 123)]
    [InlineData("12%", 12)]
    [InlineData("SAR 249", 249)]
    public void Table_reads_figures_as_numbers(string text, double value) => Assert.Equal(value, Table.NumberOf(text), 3);

    [Fact]
    public void Table_text_is_not_a_figure() => Assert.True(double.IsNaN(Table.NumberOf("ليلى")));

    [AvaloniaFact]
    public void Table_selection_column_selects_rows_and_drives_the_bulk_bar()
    {
        var table = People();
        table.IsSelectable = true;
        table.Rows[0].IsSelected = true;
        var bar = new BulkBar { Table = table, Content = new StackPanel { Children = { new Button { Content = "إلغاء", [BulkBar.ClearsSelectionProperty] = true } } } };
        var window = Show(new StackPanel { Children = { table, bar } }, FlowDirection.RightToLeft, width: 700);
        Settle();
        var boxes = table.GetVisualDescendants().OfType<CheckBox>().ToList();
        Assert.Equal(4, boxes.Count);
        Assert.Null(boxes[0].IsChecked);
        Assert.True(bar.IsVisible);
        Assert.Equal("المحدد: 1", bar.CountText);
        Assert.True(table.DisplayedRows[0].Cells[0].Bounds.Width < 70);

        Click(boxes[2], window);
        Assert.True(table.DisplayedRows[1].IsSelected);
        Assert.Equal("المحدد: 2", bar.CountText);

        Click(boxes[0], window);
        Assert.All(table.DisplayedRows, r => Assert.True(r.IsSelected));
        Assert.True(boxes[0].IsChecked);

        Click(boxes[0], window);
        Assert.All(table.DisplayedRows, r => Assert.False(r.IsSelected));
        Assert.False(bar.IsVisible);

        Click(boxes[1], window);
        Assert.True(bar.IsVisible);
        Click(bar.GetVisualDescendants().OfType<Button>().First(), window);
        Assert.Empty(table.SelectedRows);
        Assert.False(bar.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_shift_click_selects_the_rows_between()
    {
        var table = People();
        table.Rows.Add(Row("كريم", "دبي", "10"));
        table.IsSelectable = true;
        var window = Show(table, width: 700, height: 500);
        var boxes = table.GetVisualDescendants().OfType<CheckBox>().ToList();
        Click(boxes[1], window);
        var p = Center(boxes[4], window);
        window.MouseMove(p, RawInputModifiers.Shift);
        window.MouseDown(p, MouseButton.Left, RawInputModifiers.Shift);
        window.MouseUp(p, MouseButton.Left, RawInputModifiers.Shift);
        Settle();
        Assert.Equal(4, table.SelectedRows.Count);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_bulk_bar_takes_its_own_count_text()
    {
        var table = People();
        table.IsSelectable = true;
        table.Rows[1].IsSelected = true;
        var bar = new BulkBar { Table = table, Text = "الصفوف المحددة: {n}" };
        var window = Show(new StackPanel { Children = { table, bar } }, width: 700);
        Settle();
        Assert.Equal("الصفوف المحددة: 1", bar.CountText);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_cell_edits_in_place_and_keeps_or_drops_the_change()
    {
        var table = People(new TableColumn { Header = "الاسم", Edit = TableEdit.Text }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" });
        TableEditEventArgs? seen = null;
        table.CellEditing += (_, e) => seen = e;
        var window = Show(table, width: 700);
        var cell = table.DisplayedRows[0].Cells[0];
        Assert.Contains(":editable", cell.Classes);
        Assert.False(cell.Focusable);
        Assert.True(table.Focusable);

        DoubleClick(cell, window);
        Assert.True(cell.IsEditing);
        var box = cell.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.Equal("ليلى", box.Text);
        Assert.Equal(32, box.Bounds.Height, 0);
        box.Text = "ليلى حداد";
        Press(window, Key.Enter);
        Assert.False(cell.IsEditing);
        Assert.Equal("ليلى حداد", cell.Text);
        Assert.Equal("ليلى", seen!.Old);
        Assert.Equal("ليلى حداد", seen.Value);
        Assert.Same(table.DisplayedRows[1], table.CurrentRow);
        Assert.True(table.IsFocused);

        table.DisplayedRows[0].Cells[0].BeginEdit();
        Settle(200);
        cell.GetVisualDescendants().OfType<TextBox>().Single().Text = "غيره";
        Press(window, Key.Escape);
        Assert.Equal("ليلى حداد", cell.Text);

        seen.Revert();
        Assert.Equal("ليلى", cell.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_cell_edit_can_be_refused()
    {
        var table = People(new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة", Edit = TableEdit.Number, Classes = "num" });
        table.CellEditing += (_, e) => e.Cancel = true;
        var window = Show(table, width: 700);
        var cell = table.DisplayedRows[0].Cells[2];
        cell.BeginEdit();
        Settle(200);
        var box = cell.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.Equal(FlowDirection.LeftToRight, box.FlowDirection);
        box.Text = "300";
        Assert.False(cell.EndEdit(true));
        Assert.Equal("249.00", cell.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_select_column_edits_with_a_list()
    {
        var table = People(new TableColumn { Header = "الاسم" }, new TableColumn { Header = "الحالة", Edit = TableEdit.Select, Options = "نشط|موقوف" }, new TableColumn { Header = "القيمة" });
        table.Rows[0].Cells[1].Content = "نشط";
        var window = Show(table, width: 700);
        var cell = table.DisplayedRows[0].Cells[1];
        Click(cell, window);
        Press(window, Key.F2);
        var combo = cell.GetVisualDescendants().OfType<ComboBox>().Single();
        Assert.Equal("نشط", combo.SelectedItem);
        combo.SelectedItem = "موقوف";
        Assert.True(cell.EndEdit(true));
        Assert.Equal("موقوف", cell.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_without_rows_shows_its_empty_state_in_place_of_the_header()
    {
        var table = new Table { Empty = new TextBlock { Text = "لا بيانات", Classes = { "empty-title" } } };
        table.Columns.Add(new TableColumn { Header = "الاسم" });
        var window = Show(table, width: 600);
        Assert.Contains(":empty", table.Classes);
        Assert.False(Part<Border>(table, "PART_Head").IsVisible);
        var empty = Part<ContentPresenter>(table, "PART_Empty");
        Assert.True(empty.IsVisible);
        Assert.Equal(new Thickness(24, 44), empty.Padding);
        table.Rows.Add(Row("ليلى"));
        Settle(200);
        Assert.True(Part<Border>(table, "PART_Head").IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_in_a_flush_panel_flattens_its_rows()
    {
        var table = People();
        var panel = new Border { Classes = { "panel" }, Child = new Border { Classes = { "panel-body", "flush" }, Child = table } };
        var window = Show(panel, width: 700);
        Settle();
        Assert.Contains(":in-panel", table.Classes);
        Assert.Contains(":flush", table.Classes);
        Assert.Equal(new Thickness(0), Part<Border>(table, "PART_Card").BorderThickness);
        Assert.Equal(new CornerRadius(11), Part<Border>(table, "PART_Card").CornerRadius);
        AssertBrush(table, "InsRowInner", () => Part<Border>(table.DisplayedRows[0], "PART_Fill").Background);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_sticky_header_and_pinned_column_stay_in_view(string dir)
    {
        var table = People();
        for (var i = 0; i < 6; i++) table.Rows.Add(Row("كريم", "دبي", i.ToString()));
        table.Classes.Add("sticky");
        table.Classes.Add("pin");
        table.MaxHeight = 256;
        table.TableMinWidth = 900;
        var window = Show(table, Dir(dir), width: 600, height: 400);
        Settle();
        Assert.Equal(256, table.Bounds.Height, 0);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        scroll.Offset = new Vector(120, 80);
        Settle(300);
        var head = Part<Border>(table, "PART_Head");
        Assert.Equal(80, Assert.IsType<TranslateTransform>(head.RenderTransform).Y, 0);
        var first = table.DisplayedRows[0].Cells[0];
        Assert.Equal(120, Assert.IsType<TranslateTransform>(first.RenderTransform).X, 0);
        Assert.Contains(":pinned", first.Classes);
        AssertBrush(table, "InsGlassStrong", () => Part<Border>(first, "PART_Back").Background);
        Assert.Null(table.DisplayedRows[0].Cells[1].RenderTransform);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_sticky_rows_show_from_below_the_kept_bar_with_rounded_corners(string dir)
    {
        var table = People();
        for (var i = 0; i < 27; i++) table.Rows.Add(Row("كريم", "دبي", i.ToString()));
        table.Classes.Add("sticky");
        table.MaxHeight = 300;
        var window = Show(table, Dir(dir), width: 600, height: 400);
        var card = Part<Border>(table, "PART_Card");
        Assert.Null(card.Clip);
        Part<ScrollViewer>(table, "PART_Scroll").Offset = new Vector(0, 200);
        Settle(100);
        var clip = Assert.IsType<RectangleGeometry>(card.Clip);
        Assert.Equal(200, clip.Rect.Y, 0.5);
        Assert.Equal(card.CornerRadius.TopLeft, clip.RadiusX);
        Part<ScrollViewer>(table, "PART_Scroll").Offset = new Vector(0, 0);
        Settle(100);
        Assert.Null(card.Clip);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_builds_rows_from_items()
    {
        var table = new Table();
        table.Columns.Add(new TableColumn { Header = "الاسم", CellText = o => ((string[])o!)[0], Sort = TableSort.Auto });
        table.Columns.Add(new TableColumn { Header = "العدد", CellText = o => ((string[])o!)[1], Sort = TableSort.Auto });
        table.ItemsSource = new[] { new[] { "ليلى", "12" }, new[] { "عمر", "7" } };
        var window = Show(table, width: 600);
        Assert.Equal(2, table.DisplayedRows.Count);
        Assert.Equal("12", table.DisplayedRows[0].Cells[1].Text);
        table.Sort(table.Columns[1]);
        Assert.Equal("عمر", table.DisplayedRows[0].Cells[0].Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_is_one_tab_stop_and_its_parts_are_not()
    {
        var table = People(new TableColumn { Header = "الاسم", Sort = TableSort.Auto }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" });
        table.IsSelectable = true;
        var window = Show(table, width: 700);
        Settle();
        Assert.True(table.Focusable);
        var buttons = table.GetVisualDescendants().OfType<TableSortButton>().ToList();
        Assert.NotEmpty(buttons);
        Assert.All(buttons, b => Assert.False(b.Focusable));
        var boxes = table.GetVisualDescendants().OfType<CheckBox>().ToList();
        Assert.Equal(4, boxes.Count);
        Assert.All(boxes, b => Assert.False(b.Focusable));
        Assert.All(table.DisplayedRows.SelectMany(r => r.Cells), c => Assert.False(c.Focusable));
        window.Close();
    }

    [AvaloniaFact]
    public void Table_arrow_keys_move_the_current_row_and_select_it()
    {
        var table = Five();
        table.Rows[2].IsSelected = true;
        var window = Show(table, width: 700, height: 500);
        var rows = table.DisplayedRows;
        table.Focus();
        Settle(200);
        Assert.Same(rows[2], table.CurrentRow);
        Press(window, Key.Down);
        Assert.Same(rows[3], table.CurrentRow);
        Assert.Equal([3], Selected(table));
        Assert.Contains(":current", rows[3].Classes);
        Assert.DoesNotContain(":current", rows[2].Classes);
        Press(window, Key.End);
        Assert.Same(rows[4], table.CurrentRow);
        Assert.Equal([4], Selected(table));
        Press(window, Key.Home);
        Assert.Same(rows[0], table.CurrentRow);
        Assert.Equal([0], Selected(table));
        Press(window, Key.Up);
        Assert.Same(rows[0], table.CurrentRow);
        Assert.Equal(1, Part<Border>(rows[0], "PART_Ring").Opacity);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_page_keys_move_by_a_screenful_and_keep_the_row_in_view()
    {
        var table = People();
        for (var i = 0; i < 27; i++) table.Rows.Add(Row("ليلى", "الرياض", i.ToString()));
        var window = Show(new ScrollViewer { Content = table }, width: 600, height: 300);
        Settle();
        table.Focus();
        Settle(200);
        var rowH = table.DisplayedRows[0].Bounds.Height;
        var head = Part<Border>(table, "PART_Head").Bounds.Height + 8;
        var viewer = (ScrollViewer)table.Parent!;
        var page = Math.Max(1, (int)Math.Floor((viewer.Viewport.Height - head) / rowH) - 1);
        var rows = table.DisplayedRows.ToList();
        Press(window, Key.Home);
        Press(window, Key.PageDown);
        Assert.Equal(page, rows.IndexOf(table.CurrentRow!));
        Press(window, Key.PageDown);
        Assert.Equal(2 * page, rows.IndexOf(table.CurrentRow!));
        Press(window, Key.PageUp);
        Assert.Equal(page, rows.IndexOf(table.CurrentRow!));
        Press(window, Key.End);
        Assert.Equal(29, rows.IndexOf(table.CurrentRow!));
        Assert.True(viewer.Offset.Y > 0);
        Assert.True(rows[29].TranslatePoint(new Point(0, rowH), viewer)!.Value.Y <= viewer.Viewport.Height + 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_ctrl_and_shift_clicks_select_like_a_desktop_list()
    {
        var table = Five();
        table.SelectionMode = TableSelectionMode.Extended;
        var window = Show(table, width: 700, height: 500);
        var rows = table.DisplayedRows;
        Click(CellOf(table, 0), window);
        Assert.Equal([0], Selected(table));
        ClickWith(CellOf(table, 2), window, RawInputModifiers.Control);
        Assert.Equal([0, 2], Selected(table));
        ClickWith(CellOf(table, 3), window, RawInputModifiers.Shift);
        Assert.Equal([2, 3], Selected(table));
        ClickWith(CellOf(table, 0), window, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.Equal([0, 1, 2, 3], Selected(table));
        foreach (var i in Selected(table))
            AssertBrush(table, "InsToneBrandBg", () => Part<Border>(rows[i], "PART_Tint").Background);
        Click(CellOf(table, 1), window);
        Assert.Equal([1], Selected(table));
        Assert.Same(rows[1], table.CurrentRow);
        AssertBrush(table, "InsToneBrandBg", () => Part<Border>(rows[1], "PART_Tint").Background);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_shift_arrows_extend_ctrl_arrows_move_and_space_toggles()
    {
        var table = Five();
        table.SelectionMode = TableSelectionMode.Extended;
        var raised = 0;
        table.SelectionChanged += (_, _) => raised++;
        var window = Show(table, width: 700, height: 500);
        table.Focus();
        Settle(200);
        Assert.Same(table.DisplayedRows[0], table.CurrentRow);
        Assert.Empty(Selected(table));
        Press(window, Key.Down);
        Assert.Equal([1], Selected(table));
        Press(window, Key.Down, RawInputModifiers.Shift);
        Assert.Equal([1, 2], Selected(table));
        Press(window, Key.Down, RawInputModifiers.Shift);
        Assert.Equal([1, 2, 3], Selected(table));
        Press(window, Key.Down, RawInputModifiers.Control);
        Assert.Same(table.DisplayedRows[4], table.CurrentRow);
        Assert.Equal([1, 2, 3], Selected(table));
        Press(window, Key.Space);
        Assert.Equal([1, 2, 3, 4], Selected(table));
        Press(window, Key.Space);
        Assert.Equal([1, 2, 3], Selected(table));
        Press(window, Key.A, RawInputModifiers.Control);
        Assert.Equal([0, 1, 2, 3, 4], Selected(table));
        Assert.True(raised > 0);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_without_selection_moves_the_cursor_only()
    {
        var table = Five();
        table.SelectionMode = TableSelectionMode.None;
        var window = Show(table, width: 700, height: 500);
        table.Focus();
        Settle(200);
        Press(window, Key.Down);
        Click(CellOf(table, 3), window);
        Assert.Empty(table.SelectedRows);
        Assert.Same(table.DisplayedRows[3], table.CurrentRow);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_right_click_selects_the_row_and_opens_its_menu()
    {
        var table = Five();
        table.SelectionMode = TableSelectionMode.Extended;
        var menu = RowMenu();
        table.RowContextMenu = menu;
        var window = Show(table, width: 700, height: 500);
        RightClick(CellOf(table, 2), window);
        Assert.True(menu.IsOpen);
        Assert.Equal([2], Selected(table));
        Assert.Same(table.DisplayedRows[2], table.CurrentRow);
        Assert.Equal(PlacementMode.Pointer, menu.Placement);
        menu.Close();
        Settle(200);
        Click(CellOf(table, 0), window);
        ClickWith(CellOf(table, 1), window, RawInputModifiers.Control);
        Assert.Equal([0, 1], Selected(table));
        RightClick(CellOf(table, 1), window);
        Assert.Equal([0, 1], Selected(table));
        Assert.True(menu.IsOpen);
        menu.Close();
        Settle(200);
        RightClick(Part<ColumnsPanel>(table, "PART_HeadCells").Children[0], window);
        Assert.False(menu.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_menu_key_opens_the_menu_at_the_current_row()
    {
        var table = Five();
        var menu = RowMenu();
        table.RowContextMenu = menu;
        var window = Show(table, width: 700, height: 500);
        table.Focus();
        Settle(200);
        Press(window, Key.Down);
        Press(window, Key.Apps);
        Assert.True(menu.IsOpen);
        Assert.Equal(PlacementMode.Bottom, menu.Placement);
        Assert.Same(table.DisplayedRows[1].Cells[0], menu.PlacementTarget);
        menu.Close();
        Settle(200);
        table.Focus();
        Settle(200);
        Press(window, Key.F10, RawInputModifiers.Shift);
        Assert.True(menu.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_double_click_or_enter_activates_a_row()
    {
        var table = Five();
        var seen = new List<TableRow>();
        table.RowActivated += (_, e) => seen.Add(e.Row);
        var window = Show(table, width: 700, height: 500);
        DoubleClick(CellOf(table, 1, 1), window);
        Assert.Equal([table.DisplayedRows[1]], seen);
        table.Focus();
        Press(window, Key.Down);
        Press(window, Key.Enter);
        Assert.Equal([table.DisplayedRows[1], table.DisplayedRows[2]], seen);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_typing_on_an_editable_cell_starts_editing_with_that_text()
    {
        var table = People(new TableColumn { Header = "الاسم", Edit = TableEdit.Text }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" });
        var window = Show(table, width: 700);
        table.Focus();
        Settle(200);
        Assert.Equal(0, table.CurrentColumn);
        window.KeyTextInput("س");
        Settle(300);
        var cell = table.DisplayedRows[0].Cells[0];
        Assert.True(cell.IsEditing);
        Assert.Equal("س", cell.GetVisualDescendants().OfType<TextBox>().Single().Text);
        Press(window, Key.Escape);
        Assert.False(cell.IsEditing);
        Assert.Equal("ليلى", cell.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_clicking_the_current_cell_again_edits_it_and_leaving_keeps_the_value()
    {
        var table = People(new TableColumn { Header = "الاسم", Edit = TableEdit.Text }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" });
        var window = Show(table, width: 700);
        var cell = table.DisplayedRows[0].Cells[0];
        Click(CellOf(table, 1), window);
        Click(cell, window);
        Assert.False(cell.IsEditing);
        Click(cell, window);
        Assert.True(cell.IsEditing);
        cell.GetVisualDescendants().OfType<TextBox>().Single().Text = "ليلى حداد";
        table.Focus();
        Settle(300);
        Assert.False(cell.IsEditing);
        Assert.Equal("ليلى حداد", cell.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_tab_in_an_editor_keeps_the_value_and_moves_to_the_next_editable_cell()
    {
        var table = People(new TableColumn { Header = "الاسم", Edit = TableEdit.Text }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة", Edit = TableEdit.Number });
        var window = Show(table, width: 700);
        var rows = table.DisplayedRows;
        DoubleClick(rows[0].Cells[0], window);
        rows[0].Cells[0].GetVisualDescendants().OfType<TextBox>().Single().Text = "ليلى حداد";
        Press(window, Key.Tab);
        Assert.Equal("ليلى حداد", rows[0].Cells[0].Text);
        Assert.Same(rows[0], table.CurrentRow);
        Assert.Equal(2, table.CurrentColumn);
        Press(window, Key.F2);
        Press(window, Key.Tab);
        Assert.Same(rows[1], table.CurrentRow);
        Assert.Equal(0, table.CurrentColumn);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_left_and_right_move_the_cell_cursor_in_reading_order(string dir)
    {
        var table = People(new TableColumn { Header = "الاسم", Edit = TableEdit.Text }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة", Edit = TableEdit.Number });
        var window = Show(table, Dir(dir), width: 700);
        table.Focus();
        Settle(200);
        Assert.Equal(0, table.CurrentColumn);
        Press(window, dir == "rtl" ? Key.Left : Key.Right);
        Assert.Equal(1, table.CurrentColumn);
        var row = table.DisplayedRows[0];
        Assert.Contains(":current", row.Cells[1].Classes);
        Assert.Equal(1, Part<Border>(row.Cells[1], "PART_Focus").Opacity);
        Assert.Equal(0, Part<Border>(row, "PART_Ring").Opacity);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_columns_resize_from_the_heading_edge(string dir)
    {
        var table = People();
        var window = Show(table, Dir(dir), width: 760);
        Settle();
        var heads = Part<ColumnsPanel>(table, "PART_HeadCells").Children;
        var w0 = heads[0].Bounds.Width;
        var grip = Part<Border>((TableHeader)heads[0], "PART_Grip");
        Assert.True(grip.IsVisible);
        Assert.False(Part<Border>((TableHeader)heads[2], "PART_Grip").IsVisible);
        var p = Center(grip, window);
        var dx = dir == "rtl" ? -40 : 40;
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseMove(new Point(p.X + dx, p.Y));
        window.MouseUp(new Point(p.X + dx, p.Y), MouseButton.Left);
        Settle(300);
        Assert.Equal(w0 + 40, heads[0].Bounds.Width, 1);
        foreach (var row in table.DisplayedRows) Assert.Equal(heads[0].Bounds.Width, row.Cells[0].Bounds.Width, 1);
        DoubleClick(grip, window);
        var content = Math.Ceiling(table.DisplayedRows.Select(r => r.Cells[0].DesiredSize.Width).Append(heads[0].DesiredSize.Width).Max());
        Assert.Equal(Math.Max(ColumnLayout.MinWidth, content), heads[0].Bounds.Width, 1);
        Assert.True(heads[0].Bounds.Width < w0);
        Assert.Equal(heads[0].Bounds.Width, table.ColumnWidths.FixedWidths[0], 1);
        foreach (var row in table.DisplayedRows) Assert.Equal(heads[0].Bounds.Width, row.Cells[0].Bounds.Width, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_column_drag_stops_at_its_min_width()
    {
        var table = People(new TableColumn { Header = "الاسم", MinWidth = 120 }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" });
        var window = Show(table, width: 760);
        Settle();
        var head = (TableHeader)Part<ColumnsPanel>(table, "PART_HeadCells").Children[0];
        var grip = Part<Border>(head, "PART_Grip");
        Assert.NotNull(grip.Cursor);
        var p = Center(grip, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseMove(new Point(p.X - 400, p.Y));
        window.MouseUp(new Point(p.X - 400, p.Y), MouseButton.Left);
        Settle(300);
        Assert.Equal(120, head.Bounds.Width, 1);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_figures_heading_keeps_its_grip_on_the_trailing_edge(string dir)
    {
        var table = People(new TableColumn { Header = "الاسم" }, new TableColumn { Header = "القيمة", Classes = "num" }, new TableColumn { Header = "المدينة" });
        var window = Show(table, Dir(dir), width: 760);
        Settle();
        var head = (TableHeader)Part<ColumnsPanel>(table, "PART_HeadCells").Children[1];
        var grip = Part<Border>(head, "PART_Grip");
        var trailing = BoundsIn(grip, window);
        var box = BoundsIn(head, window);
        if (dir == "rtl")
        {
            Assert.Contains(":grip-flip", head.Classes);
            Assert.Equal(box.Left, trailing.Left, 1);
        }
        else
        {
            Assert.DoesNotContain(":grip-flip", head.Classes);
            Assert.Equal(box.Right, trailing.Right, 1);
        }
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_frozen_columns_stay_and_the_last_casts_a_shade(string dir)
    {
        var table = People(new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" }, new TableColumn { Header = "الحالة" });
        foreach (var row in table.Rows) row.Cells.Add(new TableCell { Content = "نشط" });
        table.FrozenColumns = 2;
        table.TableMinWidth = 1000;
        var window = Show(table, Dir(dir), width: 600, height: 400);
        Settle();
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        var cells = table.DisplayedRows[0].Cells;
        Assert.DoesNotContain(":pin-edge", cells[1].Classes);
        Assert.False(Part<Border>(cells[1], "PART_Shade").IsVisible);
        scroll.Offset = new Vector(150, 0);
        Settle(300);
        Assert.Equal(150, Assert.IsType<TranslateTransform>(cells[0].RenderTransform).X, 0);
        Assert.Equal(150, Assert.IsType<TranslateTransform>(cells[1].RenderTransform).X, 0);
        Assert.Null(cells[2].RenderTransform);
        Assert.Contains(":pinned", cells[0].Classes);
        Assert.DoesNotContain(":pin-edge", cells[0].Classes);
        Assert.Contains(":pin-edge", cells[1].Classes);
        Assert.True(Part<Border>(cells[1], "PART_Shade").IsVisible);
        var head = (TableHeader)Part<ColumnsPanel>(table, "PART_HeadCells").Children[1];
        Assert.Contains(":pin-edge", head.Classes);
        var shade = BoundsIn(Part<Border>(cells[1], "PART_Shade"), window);
        var edge = BoundsIn(cells[1], window);
        if (dir == "rtl") Assert.Equal(edge.Left, shade.Right, 1);
        else Assert.Equal(edge.Right, shade.Left, 1);
        scroll.Offset = new Vector(0, 0);
        Settle(300);
        Assert.DoesNotContain(":pin-edge", cells[1].Classes);
        Assert.Null(cells[1].RenderTransform);
        table.FrozenColumns = 0;
        scroll.Offset = new Vector(150, 0);
        Settle(300);
        Assert.Null(cells[0].RenderTransform);
        Assert.DoesNotContain(":pinned", cells[1].Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_frozen_columns_take_the_selection_column_with_them()
    {
        var table = People();
        table.IsSelectable = true;
        table.FrozenColumns = 1;
        table.TableMinWidth = 1000;
        var window = Show(table, width: 600, height: 400);
        Settle();
        Part<ScrollViewer>(table, "PART_Scroll").Offset = new Vector(100, 0);
        Settle(300);
        var cells = table.DisplayedRows[0].Cells;
        Assert.Contains(":pinned", cells[0].Classes);
        Assert.Contains(":pin-edge", cells[1].Classes);
        Assert.Null(cells[2].RenderTransform);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Table_cell_navigation_moves_like_a_spreadsheet(string dir)
    {
        var table = Five();
        table.IsCellNavigation = true;
        var window = Show(table, Dir(dir), width: 700, height: 500);
        var rows = table.DisplayedRows;
        table.Focus();
        Settle(200);
        Assert.Contains(":cell-cursor", table.Classes);
        Assert.Same(rows[0], table.CurrentRow);
        Assert.Equal(0, table.CurrentColumn);
        Press(window, dir == "rtl" ? Key.Left : Key.Right);
        Assert.Equal(1, table.CurrentColumn);
        Assert.Equal(1, Part<Border>(rows[0].Cells[1], "PART_Focus").Opacity);
        Assert.Equal(0, Part<Border>(rows[0], "PART_Ring").Opacity);
        Press(window, Key.End);
        Assert.Same(rows[0], table.CurrentRow);
        Assert.Equal(2, table.CurrentColumn);
        Press(window, Key.Home);
        Assert.Equal(0, table.CurrentColumn);
        Press(window, Key.Tab);
        Assert.Equal(1, table.CurrentColumn);
        Press(window, Key.Tab, RawInputModifiers.Shift);
        Assert.Equal(0, table.CurrentColumn);
        Press(window, Key.Tab, RawInputModifiers.Shift);
        Assert.Same(rows[0], table.CurrentRow);
        Assert.Equal(0, table.CurrentColumn);
        table.Focus();
        Settle(200);
        Press(window, Key.End);
        Press(window, Key.Tab);
        Assert.Same(rows[1], table.CurrentRow);
        Assert.Equal(0, table.CurrentColumn);
        Assert.Equal([1], Selected(table));
        Press(window, Key.Down);
        Assert.Same(rows[2], table.CurrentRow);
        Assert.Equal(0, table.CurrentColumn);
        Press(window, Key.End, RawInputModifiers.Control);
        Assert.Same(rows[4], table.CurrentRow);
        Assert.Equal(2, table.CurrentColumn);
        Press(window, Key.Home, RawInputModifiers.Control);
        Assert.Same(rows[0], table.CurrentRow);
        Assert.Equal(0, table.CurrentColumn);
        Assert.Equal([0], Selected(table));
        window.Close();
    }

    [AvaloniaFact]
    public void Table_enter_edits_the_current_cell_commits_and_moves_down()
    {
        var table = People(new TableColumn { Header = "الاسم", Edit = TableEdit.Text }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" });
        table.IsCellNavigation = true;
        var activated = 0;
        table.RowActivated += (_, _) => activated++;
        var window = Show(table, width: 700);
        table.Focus();
        Settle(200);
        Press(window, Key.Enter);
        var cell = table.DisplayedRows[0].Cells[0];
        Assert.True(cell.IsEditing);
        cell.GetVisualDescendants().OfType<TextBox>().Single().Text = "ليلى حداد";
        Press(window, Key.Enter);
        Assert.False(cell.IsEditing);
        Assert.Equal("ليلى حداد", cell.Text);
        Assert.Same(table.DisplayedRows[1], table.CurrentRow);
        Assert.Equal(0, table.CurrentColumn);
        Press(window, Key.Tab);
        Press(window, Key.Enter);
        Assert.Equal(1, activated);
        Press(window, Key.Home);
        window.KeyTextInput("ع");
        Settle(300);
        var next = table.DisplayedRows[1].Cells[0];
        Assert.True(next.IsEditing);
        Press(window, Key.Escape);
        Assert.False(next.IsEditing);
        Assert.Equal("عمر", next.Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_cell_navigation_scrolls_the_cell_clear_of_the_frozen_columns()
    {
        var table = People(new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" }, new TableColumn { Header = "الحالة" });
        foreach (var row in table.Rows) row.Cells.Add(new TableCell { Content = "نشط" });
        table.IsCellNavigation = true;
        table.FrozenColumns = 1;
        table.TableMinWidth = 1200;
        var window = Show(table, width: 600, height: 400);
        table.Focus();
        Settle(200);
        var scroll = Part<ScrollViewer>(table, "PART_Scroll");
        Press(window, Key.End);
        Assert.True(scroll.Offset.X > 0);
        var last = BoundsIn(table.DisplayedRows[0].Cells[3], scroll);
        Assert.True(last.Right <= scroll.Viewport.Width + 1);
        Press(window, Key.Home);
        Press(window, Key.Right);
        var second = BoundsIn(table.DisplayedRows[0].Cells[1], scroll);
        var frozen = BoundsIn(table.DisplayedRows[0].Cells[0], scroll);
        Assert.True(second.Left >= frozen.Right - 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Table_cell_text_trims_with_an_ellipsis_when_narrow()
    {
        var table = People();
        table.Rows[0] = Row("ملاحظات طويلة جدًا تتجاوز عرض العمود بكثير", "الرياض", "249.00");
        var window = Show(table, width: 700);
        table.ColumnWidths.SetWidth(0, 60);
        Settle(300);
        var cell = table.DisplayedRows[0].Cells[0];
        Assert.Equal(TextTrimming.CharacterEllipsis, cell.GetVisualDescendants().OfType<TextBlock>().First().TextTrimming);
        Assert.Equal(60, cell.Bounds.Width, 1);
        window.Close();
    }

    // ----------------------------------------------------------------- list

    private static ListBoxItem ListItem(string title, string? desc = null)
    {
        var text = new StackPanel { Classes = { "list-text" }, Children = { new TextBlock { Text = title, Classes = { "list-title" } } } };
        if (desc is not null) text.Children.Add(new TextBlock { Text = desc, Classes = { "list-desc" } });
        return new ListBoxItem { Content = new DockPanel { Children = { text } } };
    }

    private static ListBox Settings(string? cls = null)
    {
        var list = new ListBox();
        if (cls is not null) list.Classes.Add(cls);
        list.Items.Add(ListItem("عام", "الاسم واللغة"));
        list.Items.Add(ListItem("الحساب", "البريد وكلمة المرور"));
        list.Items.Add(ListItem("الأمان", "الجلسات والأجهزة"));
        return list;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void List_rows_have_dividers_and_a_chevron_toward_the_end(string theme, string dir)
    {
        var list = Settings();
        var window = Show(new Border { Classes = { "panel" }, Child = list }, Dir(dir), Theme(theme), width: 500);
        Settle();
        var items = list.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(new Thickness(4), list.Padding);
        Assert.False(Part<Border>(items[0], "PART_Divider").IsVisible);
        Assert.True(Part<Border>(items[1], "PART_Divider").IsVisible);
        AssertBrush(list, "InsLine", () => Part<Border>(items[1], "PART_Divider").Background);
        Assert.Equal(new CornerRadius(17), items[0].CornerRadius);
        Assert.True(items[0].Bounds.Height >= 52);
        var chevron = Part<Border>(items[0], "PART_Chevron");
        Assert.True(chevron.IsVisible);
        var itemAt = BoundsIn(items[0], window);
        var chevronAt = BoundsIn(chevron, window);
        Assert.Equal(dir == "rtl", chevronAt.Center.X < itemAt.Center.X);
        Capture(window, $"data-list-{theme}-{dir}");
        window.Close();
    }

    [AvaloniaFact]
    public void List_selected_row_fills_and_hides_the_dividers_around_it()
    {
        var list = Settings();
        var window = Show(list, FlowDirection.RightToLeft, width: 500);
        list.SelectedIndex = 1;
        Settle();
        var items = list.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        AssertBrush(list, "InsCtlBg", () => items[1].Background);
        Assert.Equal(0, Part<Border>(items[1], "PART_Divider").Opacity);
        Assert.Contains("after-hot", items[2].Classes);
        Assert.Equal(0, Part<Border>(items[2], "PART_Divider").Opacity);
        var title = items[1].GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("list-title"));
        AssertBrush(list, "InsToneBrand", () => title.Foreground);
        list.SelectedIndex = 0;
        Settle(300);
        Assert.DoesNotContain("after-hot", items[2].Classes);
        window.MouseMove(Center(items[2], window));
        Settle();
        AssertBrush(list, "InsCtlBg", () => items[2].Background);
        window.Close();
    }

    [AvaloniaFact]
    public void List_static_rows_neither_answer_the_pointer_nor_point_anywhere()
    {
        var list = Settings("static");
        var window = Show(list, width: 500);
        var items = list.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        window.MouseMove(Center(items[1], window));
        Settle();
        Assert.Equal(Colors.Transparent, ColorOf(items[1].Background));
        Assert.False(Part<Border>(items[1], "PART_Chevron").IsVisible);
        Assert.Equal(1, Part<Border>(items[2], "PART_Divider").Opacity);
        var desc = items[0].GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("list-desc"));
        Assert.Equal(12, desc.FontSize);
        AssertBrush(list, "InsLabel", () => desc.Foreground);
        window.Close();
    }

    [AvaloniaFact]
    public void List_keys_and_right_click_select_rows()
    {
        var list = Settings();
        list.SelectedIndex = 0;
        var window = Show(list, width: 500);
        var items = list.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        items[0].Focus(NavigationMethod.Tab);
        Settle(200);
        Press(window, Key.Down);
        Assert.Equal(1, list.SelectedIndex);
        Press(window, Key.End);
        Assert.Equal(2, list.SelectedIndex);
        Press(window, Key.Home);
        Assert.Equal(0, list.SelectedIndex);
        RightClick(items[2], window);
        Assert.Equal(2, list.SelectedIndex);
        window.Close();
        Assert.False(Settings("static").Focusable);
    }

    // ------------------------------------------------------- description list

    private static DescriptionList Facts(string? cls = null)
    {
        var dl = new DescriptionList();
        if (cls is not null) dl.Classes.Add(cls);
        dl.Children.Add(new DescriptionItem { Term = "الاسم", Content = "سلمى" });
        dl.Children.Add(new DescriptionItem { Term = "الفرق", Content = "-12", Classes = { "num" } });
        dl.Children.Add(new DescriptionItem { Term = "الحالة", Content = "مفتوح" });
        return dl;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Description_list_sets_labels_in_a_column_beside_their_values(string theme, string dir)
    {
        var dl = Facts();
        var window = Show(dl, Dir(dir), Theme(theme), width: 700);
        Settle();
        var first = (DescriptionItem)dl.Children[0];
        var term = Part<ContentPresenter>(first, "PART_Term");
        var value = Part<ContentPresenter>(first, "PART_Value");
        Assert.Equal(208, term.Bounds.Width, 0);
        Assert.Equal("سلمى", value.GetVisualDescendants().OfType<SelectableTextBlock>().Single().Text);
        var termAt = BoundsIn(term, window);
        var valueAt = BoundsIn(value, window);
        Assert.InRange(termAt.Top - valueAt.Top, 0, 2);
        if (dir == "rtl") Assert.Equal(termAt.Left - 16, valueAt.Right, 1);
        else Assert.Equal(termAt.Right + 16, valueAt.Left, 1);
        Assert.Equal(new Thickness(0, 0, 0, 12), first.Padding);
        Assert.Equal(new Thickness(0), ((DescriptionItem)dl.Children[2]).BorderThickness);
        Assert.Equal(new Thickness(0, 0, 0, 1), first.BorderThickness);
        var num = Part<ContentPresenter>((DescriptionItem)dl.Children[1], "PART_Value");
        Assert.Equal(FlowDirection.LeftToRight, num.FlowDirection);
        var numText = num.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        if (numText is not null) Assert.Equal(FlowDirection.LeftToRight, numText.FlowDirection);
        Capture(window, $"data-dl-{theme}-{dir}");
        window.Close();
    }

    [AvaloniaFact]
    public void Description_list_narrower_than_26rem_puts_each_label_above_its_value()
    {
        var dl = Facts();
        var window = Show(dl, FlowDirection.RightToLeft, width: 380);
        Settle();
        var first = (DescriptionItem)dl.Children[0];
        Assert.Contains(":stacked", first.Classes);
        var termAt = BoundsIn(Part<ContentPresenter>(first, "PART_Term"), window);
        var valueAt = BoundsIn(Part<ContentPresenter>(first, "PART_Value"), window);
        Assert.True(valueAt.Top >= termAt.Bottom + 3);
        window.Close();
    }

    [AvaloniaFact]
    public void Description_list_grid_sets_the_pairs_out_in_columns()
    {
        var dl = Facts("grid");
        dl.Children.Add(new DescriptionItem { Term = "التاريخ", Content = "26 سبتمبر 2026" });
        var window = Show(dl, FlowDirection.LeftToRight, width: 700);
        Settle();
        var a = BoundsIn(dl.Children[0], window);
        var b = BoundsIn(dl.Children[1], window);
        var d = BoundsIn(dl.Children[3], window);
        Assert.Equal(a.Top, b.Top, 0);
        Assert.Equal(24, b.Left - a.Right, 1);
        Assert.Equal(20, d.Top - a.Bottom, 1);
        Assert.Equal(new Thickness(0), ((DescriptionItem)dl.Children[0]).Padding);
        Assert.Contains(":grid", dl.Children[0].Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Description_values_are_selectable_text()
    {
        var dl = Facts();
        var window = Show(dl, FlowDirection.RightToLeft, width: 700);
        Settle();
        var first = Part<ContentPresenter>((DescriptionItem)dl.Children[0], "PART_Value");
        Assert.Equal("سلمى", first.GetVisualDescendants().OfType<SelectableTextBlock>().Single().Text);
        var num = Part<ContentPresenter>((DescriptionItem)dl.Children[1], "PART_Value");
        Assert.Equal(FlowDirection.LeftToRight, num.GetVisualDescendants().OfType<SelectableTextBlock>().Single().FlowDirection);
        window.Close();
    }

    // ---------------------------------------------------------------- stats

    private static Border Stat(string classes, out Border ico, out TextBlock value)
    {
        ico = new Border { Classes = { "stat-ico" }, Child = new Icon() };
        value = new TextBlock { Text = "128", Classes = { "stat-value" } };
        var tile = new Border { Child = new DockPanel { Children = { ico, new StackPanel { Classes = { "stat-body" }, Children = { new TextBlock { Text = "الملفات", Classes = { "stat-label" } }, value } } } } };
        foreach (var c in classes.Split(' ')) tile.Classes.Add(c);
        return tile;
    }

    [AvaloniaTheory]
    [InlineData("", "InsToneBrandSoft")]
    [InlineData("ok", "InsToneOk")]
    [InlineData("warn", "InsToneWarn")]
    [InlineData("bad", "InsToneBad")]
    [InlineData("info", "InsToneInfo")]
    [InlineData("mute", "InsToneMute")]
    [InlineData("zero", "InsText2")]
    public void Stat_tone_colours_the_chip_and_the_figure(string tone, string key)
    {
        var tile = Stat(("stat glass " + tone).Trim(), out var ico, out var value);
        var window = Show(tile, FlowDirection.RightToLeft);
        Settle();
        AssertBrush(tile, key, () => value.Foreground);
        Assert.Equal(38, ico.Bounds.Width, 0);
        Assert.Equal(24, value.FontSize);
        Assert.Equal(FontWeight.ExtraBold, value.FontWeight);
        Assert.Equal(new Thickness(16, 12), tile.Padding);
        var icon = (Icon)ico.Child!;
        Assert.Equal(ico.Bounds.Width / 2, icon.Bounds.Center.X, 1);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("sm", 32, 20)]
    [InlineData("lg", 46, 32)]
    public void Stat_sizes(string size, double chip, double figure)
    {
        var tile = Stat("stat glass " + size, out var ico, out var value);
        var window = Show(tile);
        Settle();
        Assert.Equal(chip, ico.Bounds.Width, 0);
        Assert.Equal(figure, value.FontSize);
        window.Close();
    }

    [AvaloniaFact]
    public void Stat_that_goes_somewhere_pops_its_chip()
    {
        var tile = Stat("stat glass hoverable", out var ico, out _);
        tile.HorizontalAlignment = HorizontalAlignment.Center;
        var window = Show(tile);
        window.MouseMove(Center(tile, window));
        Settle();
        Assert.False(ico.RenderTransform is null || ico.RenderTransform.Value.IsIdentity);
        window.Close();
    }

    [AvaloniaFact]
    public void Stat_link_is_a_button_that_pops_and_takes_focus()
    {
        var tile = Stat("stat glass", out var ico, out _);
        var button = new Button { Classes = { "stat-link" }, Content = tile, HorizontalAlignment = HorizontalAlignment.Center };
        var window = Show(button);
        Assert.Same(Res(button, "InsStatButton"), button.Theme);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        button.Focus(NavigationMethod.Tab);
        Settle(200);
        PressSpace(window);
        Assert.Equal(1, clicks);
        Assert.True(Part<Border>(button, "PART_Focus").BoxShadow.Count > 0);
        window.MouseMove(Center(button, window));
        Settle();
        Assert.False(ico.RenderTransform is null || ico.RenderTransform.Value.IsIdentity);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("up", "InsToneOk")]
    [InlineData("down", "InsToneBad")]
    [InlineData("flat", "InsToneMute")]
    [InlineData("up invert", "InsToneBad")]
    [InlineData("down invert", "InsToneOk")]
    public void Delta_arrow_and_colour_follow_its_classes(string classes, string key)
    {
        var delta = new Delta { Content = "12%" };
        foreach (var c in classes.Split(' ')) delta.Classes.Add(c);
        var window = Show(delta, FlowDirection.RightToLeft);
        Settle();
        AssertBrush(delta, key, () => delta.Foreground);
        AssertBrush(delta, key + "Bg", () => delta.Background);
        Assert.Equal(FlowDirection.LeftToRight, delta.FlowDirection);
        var arrow = Part<Icon>(delta, "PART_Arrow");
        var expected = classes.StartsWith("down") ? "InsDeltaDown" : classes == "flat" ? "InsDeltaFlat" : "InsDeltaUp";
        Assert.Same(Res(delta, expected), arrow.Data);
        var text = delta.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.True(BoundsIn(arrow, window).Left < BoundsIn(text, window).Left);
        window.Close();
    }

    // --------------------------------------------------------------- matrix

    private static ChoiceMatrix Survey(string? cls = null)
    {
        var m = new ChoiceMatrix();
        if (cls is not null) foreach (var c in cls.Split(' ')) m.Classes.Add(c);
        m.Columns.Add(new MatrixColumn { Header = "ضعيف", Value = "1", Description = "وصف قصير للمستوى.", Points = "1" });
        m.Columns.Add(new MatrixColumn { Header = "جيد", Value = "3", Description = "وصف قصير للمستوى.", Points = "3" });
        m.Columns.Add(new MatrixColumn { Header = "ممتاز", Value = "5", Description = "وصف قصير للمستوى.", Points = "5" });
        m.Rows.Add(new MatrixRow { Header = "الأول", SelectedIndex = 1 });
        m.Rows.Add(new MatrixRow { Header = "الثاني" });
        return m;
    }

    private static List<RadioButton> Radios(ChoiceMatrix m) => m.GetVisualDescendants().OfType<RadioButton>().ToList();

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Matrix_rows_are_radio_groups_and_the_chosen_cell_takes_the_brand(string theme, string dir)
    {
        var m = Survey();
        var window = Show(m, Dir(dir), Theme(theme), width: 700);
        Settle();
        var radios = Radios(m);
        Assert.Equal(6, radios.Count);
        Assert.True(radios[1].IsChecked);
        Assert.Equal(radios[0].GroupName, radios[2].GroupName);
        Assert.NotEqual(radios[0].GroupName, radios[3].GroupName);
        Assert.Equal("الأول: جيد", global::Avalonia.Automation.AutomationProperties.GetName(radios[1]));
        var cell = (Border)radios[1].Parent!;
        AssertBrush(m, "InsToneBrandBg", () => cell.Background);
        AssertBrush(m, "InsGlassInner", () => ((Border)radios[0].Parent!).Background);
        Assert.Equal(3.0, m.Sum);
        Assert.False(m.IsComplete);
        var th = m.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("matrix-th"));
        Assert.Equal(new CornerRadius(10, 0, 0, 10), th.CornerRadius);
        var firstAt = BoundsIn(radios[0], window);
        var lastAt = BoundsIn(radios[2], window);
        Assert.Equal(dir == "rtl", firstAt.Left > lastAt.Left);
        Capture(window, $"data-matrix-{theme}-{dir}");
        window.Close();
    }

    [AvaloniaFact]
    public void Matrix_choice_updates_the_row_the_sum_and_completeness()
    {
        var m = Survey();
        var window = Show(m, width: 700);
        var radios = Radios(m);
        Click(radios[5], window);
        Assert.Equal(2, m.Rows[1].SelectedIndex);
        Assert.Equal(8.0, m.Sum);
        Assert.True(m.IsComplete);
        Click(radios[3], window);
        Assert.Equal(4.0, m.Sum);
        Assert.False(radios[5].IsChecked);
        Assert.DoesNotContain("checked", ((Border)radios[5].Parent!).Classes);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl", Key.Left, 2)]
    [InlineData("rtl", Key.Right, 0)]
    [InlineData("ltr", Key.Right, 2)]
    [InlineData("ltr", Key.Left, 0)]
    [InlineData("ltr", Key.End, 2)]
    [InlineData("rtl", Key.Home, 0)]
    public void Matrix_arrow_keys_move_the_choice_along_the_row(string dir, Key key, int expected)
    {
        var m = Survey();
        var window = Show(m, Dir(dir), width: 700);
        var radios = Radios(m);
        radios[1].Focus(NavigationMethod.Tab);
        Settle(200);
        Press(window, key);
        Assert.Equal(expected, m.Rows[0].SelectedIndex);
        Assert.True(radios[expected].IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void Matrix_second_click_clears_where_deselect_is_on()
    {
        var m = Survey("deselect");
        var window = Show(m, width: 700);
        var radios = Radios(m);
        Click(radios[1], window);
        Assert.False(radios[1].IsChecked);
        Assert.Equal(-1, m.Rows[0].SelectedIndex);
        Assert.Equal(0.0, m.Sum);
        window.Close();

        var plain = Survey();
        window = Show(plain, width: 700);
        radios = Radios(plain);
        Click(radios[1], window);
        Assert.True(radios[1].IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void Matrix_cards_make_each_choice_a_card_with_its_points()
    {
        var m = Survey("cards");
        var window = Show(m, FlowDirection.RightToLeft, width: 700);
        Settle();
        var radios = Radios(m);
        Assert.All(radios, r => Assert.Contains("matrix-card", r.Classes));
        Assert.Same(Res(m, "InsMatrixCard"), radios[0].Theme);
        Assert.True(radios[0].Bounds.Width >= 128);
        AssertBrush(m, "InsToneBrandBg", () => radios[1].Background);
        AssertBrush(m, "InsPrimaryMid", () => radios[1].BorderBrush);
        Assert.Equal(1, Part<Border>(radios[1], "PART_Ring").Opacity);
        var pill = radios[1].GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("matrix-points"));
        AssertBrush(m, "InsPrimaryMid", () => pill.Background);
        Assert.Equal(Colors.Transparent, ColorOf(((Border)radios[0].Parent!).Background));
        Assert.Equal(radios[0].Bounds.Height, radios[2].Bounds.Height, 0);
        Capture(window, "data-matrix-cards");
        window.Close();
    }

    [AvaloniaFact]
    public void Matrix_up_and_down_move_focus_between_rows_without_choosing()
    {
        var m = Survey();
        var window = Show(m, width: 700);
        var radios = Radios(m);
        radios[1].Focus(NavigationMethod.Tab);
        Settle(200);
        Press(window, Key.Down);
        Assert.True(radios[4].IsFocused);
        Assert.Equal(1, m.Rows[0].SelectedIndex);
        Assert.Equal(-1, m.Rows[1].SelectedIndex);
        Click(radios[5], window);
        radios[3].Focus(NavigationMethod.Tab);
        Settle(200);
        Press(window, Key.Up);
        Assert.True(radios[1].IsFocused);
        Press(window, Key.Up);
        Assert.True(radios[1].IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void Matrix_left_and_right_stop_at_the_row_ends()
    {
        var m = Survey();
        var window = Show(m, width: 700);
        var radios = Radios(m);
        Click(radios[2], window);
        Press(window, Key.Right);
        Assert.Equal(2, m.Rows[0].SelectedIndex);
        Assert.True(radios[2].IsFocused);
        window.Close();
    }
}
