using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The table's column grips and its selected row: what a person sees at rest, under the pointer, while dragging and from the keyboard.
public class TableLookTests
{
    private static ThemeVariant Theme(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static TableRow Row(params string[] cells)
    {
        var row = new TableRow();
        foreach (var c in cells) row.Cells.Add(new TableCell { Content = c });
        return row;
    }

    private static Table People()
    {
        var table = new Table();
        table.Columns.AddRange([new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }, new TableColumn { Header = "القيمة" }]);
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

    private static TableHeader Head(Table table, int i) => (TableHeader)Part<ColumnsPanel>(table, "PART_HeadCells").Children[i];

    // The table on its own, in a panel, or in a flush panel body.
    private static Control Framed(Table table, string where)
    {
        table.VerticalAlignment = VerticalAlignment.Top;
        if (where == "bare") return table;
        var body = new Border { Classes = { "panel-body" }, Child = table };
        if (where == "flush") body.Classes.Add("flush");
        return new Border { Classes = { "panel" }, Child = new DockPanel { Children = { body } } };
    }

    private static double CardCorner(string where) => where == "bare" ? 21 : where == "panel" ? 22 : 11;

    // The cell's focus ring rounds one corner only, by r, and that corner sits in the card's corner on the given side of the screen.
    private static void AssertCorner(TableCell cell, Table table, Window window, double r, bool top, bool left)
    {
        var focus = Part<Border>(cell, "PART_Focus");
        var c = focus.CornerRadius;
        double w = focus.Bounds.Width, h = focus.Bounds.Height;
        var round = new[] { (c.TopLeft, new Point(0, 0)), (c.TopRight, new Point(w, 0)), (c.BottomRight, new Point(w, h)), (c.BottomLeft, new Point(0, h)) }
            .Where(x => x.Item1 != 0).ToList();
        Assert.True(round.Count == 1, $"{cell.Text}: corners {c}");
        Assert.Equal(r, round[0].Item1);
        var at = focus.TranslatePoint(round[0].Item2, window)!.Value;
        var card = Part<Border>(table, "PART_Card");
        var box = BoundsIn(card, window).Deflate(card.BorderThickness);
        var want = new Point(left ? box.Left : box.Right, top ? box.Top : box.Bottom);
        Assert.True(Math.Abs(at.X - want.X) <= 1 && Math.Abs(at.Y - want.Y) <= 1, $"{cell.Text}: the round corner is at {at}, the card's corner at {want}");
    }

    private static void AssertSquare(TableCell cell) =>
        Assert.True(Part<Border>(cell, "PART_Focus").CornerRadius == new CornerRadius(0), $"{cell.Text}: corners {Part<Border>(cell, "PART_Focus").CornerRadius}");

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Resizable_edges_show_a_divider_at_rest_and_a_brand_bar_under_the_pointer(string theme, string dir)
    {
        var table = People();
        var window = Show(table, Dir(dir), Theme(theme), width: 760);
        var head = Head(table, 0);
        var line = Part<Border>(head, "PART_GripLine");
        Assert.True(line.IsEffectivelyVisible);
        Assert.Equal(1, line.Opacity);
        Assert.True(line.Bounds.Width >= 1 && line.Bounds.Height >= 16);
        Assert.True(ColorOf(line.Background).A >= 0x60, $"divider alpha {ColorOf(line.Background).A}");
        var edge = BoundsIn(head, window);
        var mark = BoundsIn(line, window);
        if (dir == "rtl") Assert.Equal(edge.Left, mark.Left, 1);
        else Assert.Equal(edge.Right, mark.Right, 1);
        Assert.False(Part<Border>(Head(table, 2), "PART_GripLine").IsEffectivelyVisible);

        window.MouseMove(Center(Part<Border>(head, "PART_Grip"), window));
        Settle();
        AssertBrush(table, "InsBrandTint", () => line.Background);
        Assert.Equal(3, line.Bounds.Width, 1);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Dragging_an_edge_runs_a_guide_down_the_table(string dir)
    {
        var table = People();
        var window = Show(table, Dir(dir), width: 760);
        var head = Head(table, 0);
        var guide = Part<Border>(table, "PART_Guide");
        Assert.False(guide.IsVisible);
        var p = Center(Part<Border>(head, "PART_Grip"), window);
        var dx = dir == "rtl" ? -40 : 40;
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseMove(new Point(p.X + dx, p.Y));
        Settle(200);
        Assert.True(guide.IsVisible);
        Assert.Contains(":resizing", head.Classes);
        var g = BoundsIn(guide, window);
        var h = BoundsIn(head, window);
        Assert.Equal(dir == "rtl" ? h.Left : h.Right, g.Center.X, 1.5);
        Assert.True(g.Height >= BoundsIn(table, window).Height - 1);
        window.MouseUp(new Point(p.X + dx, p.Y), MouseButton.Left);
        Settle(200);
        Assert.False(guide.IsVisible);
        Assert.DoesNotContain(":resizing", head.Classes);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void A_clicked_row_shows_the_tint_and_marker_and_the_keyboard_adds_a_rounded_ring(string theme, string dir)
    {
        var table = People();
        table.SelectionMode = TableSelectionMode.Single;
        var window = Show(table, Dir(dir), Theme(theme), width: 760);
        var rows = table.DisplayedRows;
        Click(rows[0].Cells[0], window);
        Assert.True(rows[0].IsSelected);
        Assert.Equal(0, Part<Border>(rows[0], "PART_Ring").Opacity);
        Assert.NotNull(Part<Border>(rows[0], "PART_Tint").Background);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), Part<Border>(rows[0], "PART_Ring").CornerRadius);
        Assert.Equal(new CornerRadius(0, 0, 21, 21), Part<Border>(rows[^1], "PART_Ring").CornerRadius);
        Assert.Equal(new CornerRadius(0), Part<Border>(rows[1], "PART_Ring").CornerRadius);

        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(300);
        Assert.Equal(1, Part<Border>(rows[1], "PART_Ring").Opacity);
        var ring = BoundsIn(Part<Border>(rows[1], "PART_Ring"), window);
        var row = BoundsIn(rows[1], window);
        Assert.True(row.Contains(ring), $"ring {ring} outside row {row}");

        Click(rows[2].Cells[0], window);
        Assert.Equal(0, Part<Border>(rows[2], "PART_Ring").Opacity);
    }

    [AvaloniaTheory]
    [InlineData("bare", "rtl")]
    [InlineData("bare", "ltr")]
    [InlineData("panel", "rtl")]
    [InlineData("panel", "ltr")]
    [InlineData("flush", "rtl")]
    [InlineData("flush", "ltr")]
    public void The_marker_keeps_clear_of_the_card_corners_on_the_first_and_last_rows(string where, string dir)
    {
        var table = People();
        table.SelectionMode = TableSelectionMode.Single;
        var window = Show(Framed(table, where), Dir(dir), width: 760, height: 500);
        var card = Part<Border>(table, "PART_Card");
        var r = CardCorner(where);
        var rows = table.DisplayedRows;
        var box = BoundsIn(card, window);

        Click(rows[0].Cells[0], window);
        var first = Part<Border>(rows[0], "PART_Marker");
        Assert.Equal(new Thickness(0, r, 0, 8), first.Margin);
        var top = BoundsIn(first, window);
        Assert.True(top.Top >= box.Top + r - 0.5, $"the marker starts at {top.Top} in a card from {box.Top}");
        if (dir == "rtl") Assert.Equal(box.Right - card.BorderThickness.Right, top.Right, 1);
        else Assert.Equal(box.Left + card.BorderThickness.Left, top.Left, 1);

        Click(rows[1].Cells[0], window);
        Assert.Equal(new Thickness(0, 8), Part<Border>(rows[1], "PART_Marker").Margin);

        Click(rows[2].Cells[0], window);
        var last = Part<Border>(rows[2], "PART_Marker");
        Assert.Equal(new Thickness(0, 8, 0, r), last.Margin);
        var bottom = BoundsIn(last, window);
        Assert.True(bottom.Bottom <= box.Bottom - r + 0.5, $"the marker ends at {bottom.Bottom} in a card to {box.Bottom}");
        if (dir == "rtl") Assert.Equal(box.Right - card.BorderThickness.Right, bottom.Right, 1);
        else Assert.Equal(box.Left + card.BorderThickness.Left, bottom.Left, 1);
    }

    [AvaloniaTheory]
    [InlineData("bare", "rtl")]
    [InlineData("bare", "ltr")]
    [InlineData("panel", "rtl")]
    [InlineData("panel", "ltr")]
    [InlineData("flush", "rtl")]
    [InlineData("flush", "ltr")]
    public void The_corner_cells_round_their_focus_ring_into_the_card_corners(string where, string dir)
    {
        var table = People();
        table.Columns[2].Classes = "num";
        var window = Show(Framed(table, where), Dir(dir), width: 760, height: 500);
        var r = CardCorner(where);
        var rows = table.DisplayedRows;
        var rtl = dir == "rtl";

        AssertCorner(rows[0].Cells[0], table, window, r, top: true, left: !rtl);
        AssertCorner(rows[0].Cells[2], table, window, r, top: true, left: rtl);
        AssertCorner(rows[2].Cells[0], table, window, r, top: false, left: !rtl);
        AssertCorner(rows[2].Cells[2], table, window, r, top: false, left: rtl);
        foreach (var cell in new[] { rows[0].Cells[1], rows[1].Cells[0], rows[1].Cells[1], rows[1].Cells[2], rows[2].Cells[1] }) AssertSquare(cell);

        Assert.Contains(":first-row", rows[0].Cells[1].Classes);
        Assert.Contains(":last-row", rows[2].Cells[1].Classes);
        Assert.Contains(":first-column", rows[1].Cells[0].Classes);
        Assert.Contains(":last-column", rows[1].Cells[2].Classes);
        Assert.DoesNotContain(":first-row", rows[1].Cells[0].Classes);
        Assert.DoesNotContain(":last-column", rows[1].Cells[1].Classes);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Hiding_the_last_column_or_sorting_moves_the_round_corners(string dir)
    {
        var table = People();
        var window = Show(Framed(table, "bare"), Dir(dir), width: 760, height: 500);
        var r = CardCorner("bare");
        var rtl = dir == "rtl";
        var laila = table.DisplayedRows[0];
        var sara = table.DisplayedRows[2];

        AssertCorner(laila.Cells[0], table, window, r, top: true, left: !rtl);
        AssertCorner(sara.Cells[2], table, window, r, top: false, left: rtl);
        table.Sort(table.Columns[0], SortDirection.Ascending);
        Settle();
        Assert.Same(sara, table.DisplayedRows[0]);
        Assert.Same(laila, table.DisplayedRows[2]);
        Assert.DoesNotContain(":last-row", sara.Cells[0].Classes);
        AssertCorner(sara.Cells[0], table, window, r, top: true, left: !rtl);
        AssertCorner(sara.Cells[2], table, window, r, top: true, left: rtl);
        AssertCorner(laila.Cells[0], table, window, r, top: false, left: !rtl);
        AssertSquare(table.DisplayedRows[1].Cells[0]);

        foreach (var row in table.DisplayedRows) row.Cells[2].IsVisible = false;
        Head(table, 2).IsVisible = false;
        Settle();
        var first = table.DisplayedRows[0];
        AssertSquare(first.Cells[2]);
        Assert.DoesNotContain(":last-column", first.Cells[2].Classes);
        Assert.Contains(":last-column", first.Cells[1].Classes);
        AssertCorner(first.Cells[1], table, window, r, top: true, left: rtl);
        AssertCorner(laila.Cells[1], table, window, r, top: false, left: rtl);

        foreach (var row in table.DisplayedRows) row.Cells[2].IsVisible = true;
        Head(table, 2).IsVisible = true;
        Settle();
        AssertSquare(first.Cells[1]);
        AssertCorner(first.Cells[2], table, window, r, top: true, left: rtl);
    }
}
