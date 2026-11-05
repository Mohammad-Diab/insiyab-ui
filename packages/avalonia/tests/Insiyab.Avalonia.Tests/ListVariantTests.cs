using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The master, ruled and compact lists, group headings and their sticky pin, a row's place in time, the inner focus ring and the master list's marker flight.
public class ListVariantTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static void Press(Window w, Key key)
    {
        w.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        w.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(200);
    }

    private static ListBoxItem Row(string title) =>
        new() { Content = new StackPanel { Classes = { "list-text" }, Children = { new TextBlock { Classes = { "list-title" }, Text = title } } } };

    private static ListBoxItem Heading(string text)
    {
        var h = new ListBoxItem { Content = text };
        ListRow.SetIsHeading(h, true);
        return h;
    }

    private static TextBlock Title(ListBoxItem row) => row.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("list-title"));

    public sealed class Entry
    {
        public required string Text { get; init; }
        public bool IsHead { get; init; }
        public override string ToString() => Text;
    }

    private static Style HeadingStyle() => new(x => x.OfType<ListBoxItem>()) { Setters = { new Setter(ListRow.IsHeadingProperty, new Binding(nameof(Entry.IsHead))) } };

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

    private static Border? Bar(Canvas layer) => layer.Children.OfType<Border>().FirstOrDefault(b => b.Classes.Contains("table-marker-flight"));

    private static ListBox Rows(string classes, int count, int selected = -1)
    {
        var list = new ListBox();
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) list.Classes.Add(c);
        for (var i = 0; i < count; i++) list.Items.Add(Row($"الصف {i + 1}"));
        if (selected >= 0) list.SelectedIndex = selected;
        return list;
    }

    private static ListBoxItem At(ListBox list, int index) => (ListBoxItem)list.ContainerFromIndex(index)!;

    [AvaloniaFact]
    public void Master_rows_drop_the_chevron_and_mark_the_selection()
    {
        var list = new ListBox { Classes = { "master" } };
        foreach (var t in new[] { "الأول", "الثاني", "الثالث" }) list.Items.Add(Row(t));
        list.SelectedIndex = 1;
        var w = Show(new Border { Classes = { "panel" }, Child = list }, FlowDirection.RightToLeft, width: 500, height: 600);
        var rows = Enumerable.Range(0, 3).Select(i => At(list, i)).ToList();
        foreach (var r in rows) Assert.False(Part<Border>(r, "PART_Chevron").IsVisible);
        AssertBrush(list, "InsBrandTintBg", () => rows[1].Background);
        AssertBrush(list, "InsPrimaryMid", () => Part<RowBack>(rows[1], "PART_Back").Mark);
        Assert.Null(Part<RowBack>(rows[0], "PART_Back").Mark);
        w.MouseMove(Center(rows[2], w));
        Settle();
        AssertBrush(list, "InsCtlBg", () => rows[2].Background);
        AssertBrush(list, "InsBrandTintBg", () => rows[1].Background);
        AssertBrush(list, "InsToneBrand", () => Title(rows[1]).Foreground);
        w.SetValue(WindowActivity.IsInactiveProperty, true);
        Settle();
        AssertBrush(list, "InsCtlBg", () => rows[1].Background);
        AssertBrush(list, "InsToneMute", () => Part<RowBack>(rows[1], "PART_Back").Mark);
    }

    [AvaloniaFact]
    public void Ruled_rows_keep_their_dividers_under_the_pointer_and_the_selection()
    {
        var list = Rows("ruled", 3, 1);
        var w = Show(new Border { Classes = { "panel" }, Child = list }, FlowDirection.RightToLeft, width: 500, height: 600);
        var rows = Enumerable.Range(0, 3).Select(i => At(list, i)).ToList();
        w.MouseMove(Center(rows[2], w));
        Settle();
        Assert.Equal(1, Part<Border>(rows[1], "PART_Divider").Opacity);
        Assert.Equal(1, Part<Border>(rows[2], "PART_Divider").Opacity);
        Assert.False(Part<Border>(rows[0], "PART_Divider").IsVisible);
        Assert.Equal(new Thickness(0, 3), Part<RowBack>(rows[1], "PART_Back").Margin);
    }

    [AvaloniaFact]
    public void Compact_rows_are_short_and_have_no_dividers()
    {
        var list = new ListBox { Classes = { "compact" } };
        foreach (var t in new[] { "الأول", "الثاني", "الثالث" }) list.Items.Add(t);
        Show(new Border { Classes = { "panel" }, Child = list }, FlowDirection.RightToLeft, width: 500, height: 600);
        var rows = Enumerable.Range(0, 3).Select(i => At(list, i)).ToList();
        Assert.Equal(36, rows[1].MinHeight);
        Assert.Equal(new Thickness(12, 6), rows[1].Padding);
        Assert.True(rows[1].Bounds.Height < 52, $"height {rows[1].Bounds.Height}");
        Assert.False(Part<Border>(rows[1], "PART_Divider").IsVisible);
    }

    [AvaloniaFact]
    public void A_heading_row_is_skipped_by_the_keys_and_parts_no_divider()
    {
        var list = new ListBox();
        foreach (var item in new[] { Heading("الأحدث"), Row("الأول"), Row("الثاني"), Heading("الأقدم"), Row("الثالث") }) list.Items.Add(item);
        list.SelectedIndex = 1;
        var w = Show(new Border { Classes = { "panel" }, Child = list }, FlowDirection.RightToLeft, width: 500, height: 600);
        var rows = Enumerable.Range(0, 5).Select(i => At(list, i)).ToList();
        rows[1].Focus(NavigationMethod.Tab);
        Settle(100);
        Press(w, Key.Down);
        Assert.Equal(2, list.SelectedIndex);
        Press(w, Key.Down);
        Assert.Equal(4, list.SelectedIndex);
        Press(w, Key.Up);
        Assert.Equal(2, list.SelectedIndex);
        var head = rows[3];
        Assert.False(head.IsEnabled);
        Assert.False(head.IsHitTestVisible);
        Assert.Equal(1, head.Opacity);
        Assert.Equal(12, head.FontSize);
        AssertBrush(list, "InsToneBrand", () => head.Foreground);
        Assert.False(Part<Border>(head, "PART_Chevron").IsVisible);
        Assert.False(Part<Border>(head, "PART_Divider").IsVisible);
        Assert.Contains(":after-heading", rows[1].Classes);
        Assert.Contains(":after-heading", rows[4].Classes);
        Assert.DoesNotContain(":after-heading", rows[2].Classes);
        Assert.False(Part<Border>(rows[4], "PART_Divider").IsVisible);
        Assert.True(Part<Border>(rows[2], "PART_Divider").IsVisible);
    }

    [AvaloniaFact]
    public void The_row_after_a_heading_follows_inserts_and_removes()
    {
        var items = new ObservableCollection<Entry> { new() { Text = "أ" }, new() { Text = "ب" }, new() { Text = "ج" } };
        var list = new ListBox { ItemsSource = items, ItemTemplate = new FuncDataTemplate<Entry>((e, _) => new TextBlock { Text = e?.Text }) };
        list.Styles.Add(HeadingStyle());
        Show(list, FlowDirection.RightToLeft, width: 500, height: 600);
        items.Insert(1, new Entry { Text = "مجموعة", IsHead = true });
        Settle(200);
        Assert.Contains(":after-heading", list.ContainerFromIndex(2)!.Classes);
        Assert.DoesNotContain(":after-heading", list.ContainerFromIndex(3)!.Classes);
        items.RemoveAt(1);
        Settle(200);
        Assert.DoesNotContain(":after-heading", list.ContainerFromIndex(1)!.Classes);
    }

    [AvaloniaFact]
    public void Past_rows_quiet_and_the_current_row_wears_the_mark()
    {
        var past = Row("مضى");
        var current = Row("الآن");
        var plain = Row("عادي");
        ListRow.SetState(past, ListRowState.Past);
        ListRow.SetState(current, ListRowState.Current);
        var list = new ListBox { Items = { past, current, plain } };
        Show(new Border { Classes = { "panel" }, Child = list }, FlowDirection.RightToLeft, width: 500, height: 600);
        AssertBrush(list, "InsText2", () => past.Foreground);
        AssertBrush(list, "InsText2", () => Title(past).Foreground);
        AssertBrush(list, "InsPrimaryMid", () => Part<RowBack>(current, "PART_Back").Mark);
        Assert.Null(Part<RowBack>(plain, "PART_Back").Mark);
        ListRow.SetState(past, ListRowState.None);
        Settle();
        AssertBrush(list, "InsText1", () => Title(past).Foreground);
    }

    [AvaloniaFact]
    public void List_rows_draw_their_focus_ring_inside()
    {
        var list = Rows("", 3);
        Show(new Border { Classes = { "panel" }, Child = list }, FlowDirection.RightToLeft, width: 500, height: 600);
        var row = At(list, 0);
        Assert.Null(row.FocusAdorner);
        row.Focus(NavigationMethod.Tab);
        Settle(200);
        Assert.Contains(":focus-visible", row.Classes);
        AssertBrush(list, "InsToneBrandSoft", () => Part<RowBack>(row, "PART_Back").Ring);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_master_marker_flies_between_rows(string dir)
    {
        var list = Rows("master", 6, 0);
        list.Width = 400;
        var w = Show(new Border { Classes = { "panel" }, Child = list }, Dir(dir), width: 500, height: 500);
        var layer = list.GetVisualDescendants().OfType<Canvas>().First(c => c.Name == "PART_Flight");
        var back3 = Part<RowBack>(At(list, 3), "PART_Back");
        var toAt = back3.TranslatePoint(back3.MarkRect.TopLeft, layer)!.Value;
        using var clock = new HeldClock();
        list.SelectedIndex = 3;
        var samples = clock.Step(w, 700, 5, () => Bar(layer) is { } b ? (Top: Canvas.GetTop(b), Left: Canvas.GetLeft(b), Moving: ListLook.GetMoving(list)) : (Top: double.NaN, Left: double.NaN, Moving: ListLook.GetMoving(list)));
        var flying = samples.Where(s => !double.IsNaN(s.Value.Top)).ToList();
        Assert.NotEmpty(flying);
        Assert.All(flying, s => Assert.True(s.Value.Moving, $"at {s.Ms} ms the rows' own marks should wait"));
        Assert.All(flying, s => Assert.InRange(s.Value.Left, toAt.X - 1, toAt.X + 1));
        Assert.Contains(flying, s => Math.Abs(s.Value.Top - toAt.Y) <= 2);
        Assert.Null(Bar(layer));
        Assert.False(ListLook.GetMoving(list));
        AssertBrush(list, "InsPrimaryMid", () => back3.Mark);
    }

    [AvaloniaFact]
    public void No_master_marker_flies_in_a_still_window()
    {
        var list = Rows("master", 6, 0);
        list.Width = 400;
        var w = Show(new Border { Classes = { "panel" }, Child = list }, FlowDirection.RightToLeft, width: 500, height: 500);
        var layer = list.GetVisualDescendants().OfType<Canvas>().First(c => c.Name == "PART_Flight");
        Motion.SetStill(w, true);
        list.SelectedIndex = 3;
        Dispatcher.UIThread.RunJobs();
        Assert.Null(Bar(layer));
        Assert.False(ListLook.GetMoving(list));
    }

    [AvaloniaFact]
    public void Sticky_headings_pin_the_group_and_the_next_pushes_it()
    {
        var list = new ListBox { Classes = { "sticky-headings" }, Height = 220 };
        var heads = new List<ListBoxItem>();
        foreach (var g in new[] { "اليوم", "أمس", "الأسبوع الماضي" })
        {
            var h = Heading(g);
            heads.Add(h);
            list.Items.Add(h);
            for (var i = 1; i <= 5; i++) list.Items.Add(new ListBoxItem { Content = $"{g} {i}" });
        }
        var w = Show(list, FlowDirection.RightToLeft, width: 500, height: 400);
        var scroll = Part<ScrollViewer>(list, "PART_ScrollViewer");
        var pin = Part<Border>(list, "PART_Pin");
        var text = Part<TextBlock>(list, "PART_PinText");
        Assert.False(pin.IsVisible);
        scroll.Offset = new Vector(0, 30);
        Settle(150);
        Assert.True(pin.IsVisible);
        Assert.Equal("اليوم", text.Text);
        Assert.InRange(pin.Bounds.Height, heads[0].Bounds.Height - 0.5, heads[0].Bounds.Height + 0.5);
        scroll.Offset = new Vector(0, 250);
        Settle(150);
        var at = heads[1].TranslatePoint(default, scroll)!.Value.Y;
        scroll.Offset = new Vector(0, scroll.Offset.Y + at - pin.Bounds.Height / 2);
        Settle(150);
        Assert.Equal("اليوم", text.Text);
        var half = pin.Bounds.Height / 2;
        Assert.InRange(pin.RenderTransform!.Value.M32, -half - 1.5, -half + 1.5);
        scroll.Offset = new Vector(0, scroll.Offset.Y + pin.Bounds.Height);
        Settle(150);
        Assert.Equal("أمس", text.Text);
        Assert.InRange(pin.RenderTransform!.Value.M32, -0.5, 0.5);
        Capture(w, "list-sticky");
        scroll.Offset = default;
        Settle(150);
        Assert.False(pin.IsVisible);
    }

    [AvaloniaFact]
    public void Sticky_headings_follow_a_virtualized_list_scrolled_down()
    {
        var items = new List<Entry>();
        for (var g = 0; g < 40; g++)
        {
            items.Add(new Entry { Text = $"الأسبوع {g + 1}", IsHead = true });
            for (var i = 0; i < 9; i++) items.Add(new Entry { Text = $"رسالة {g + 1}.{i}" });
        }
        var list = new ListBox { Classes = { "sticky-headings" }, Height = 300, ItemsSource = items, ItemTemplate = new FuncDataTemplate<Entry>((e, _) => new TextBlock { Text = e?.Text }) };
        list.Styles.Add(HeadingStyle());
        Show(list, FlowDirection.RightToLeft, width: 500, height: 400);
        var scroll = Part<ScrollViewer>(list, "PART_ScrollViewer");
        var pin = Part<Border>(list, "PART_Pin");
        var text = Part<TextBlock>(list, "PART_PinText");
        for (var y = 120; y <= 4000; y += 120)
        {
            scroll.Offset = new Vector(0, y);
            Settle(16);
        }
        Settle(150);
        Assert.True(list.GetRealizedContainers().Count() < 60);
        var top = list.GetRealizedContainers().OfType<ListBoxItem>().Where(r => r.TranslatePoint(new Point(0, r.Bounds.Height), scroll)!.Value.Y > 0).OrderBy(list.IndexFromContainer).First();
        if (ListRow.GetIsHeading(top) && top.TranslatePoint(default, scroll)!.Value.Y >= 0) Assert.False(pin.IsVisible);
        else
        {
            Assert.True(pin.IsVisible);
            Assert.Equal($"الأسبوع {list.IndexFromContainer(top) / 10 + 1}", text.Text);
        }
    }
}
