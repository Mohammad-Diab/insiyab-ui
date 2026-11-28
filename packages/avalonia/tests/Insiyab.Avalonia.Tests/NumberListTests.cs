using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Insiyab.Avalonia.Tests;

// A numbered list to move between (a survey's questions): each item's state on its badge, the current one, a flag on the corner, the vertical list that folds and scrolls on its own, the horizontal strip, the keys; and a choice tile's verdict (ok / bad).
public class NumberListTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static NumberList Make(int count = 12, Orientation orientation = Orientation.Vertical)
    {
        var list = new NumberList { Orientation = orientation, Header = "Items", Footer = "4 done", OpenText = "Show", CloseText = "Hide" };
        for (var i = 1; i <= count; i++)
            list.Items.Add(new NumberListItem
            {
                Number = i,
                Text = "Item " + i,
                State = i switch { 1 => NumberState.Right, 2 => NumberState.Wrong, 3 => NumberState.Done, _ => NumberState.Open },
                IsFlagged = i == 3,
                Name = "Item " + i,
            });
        list.SelectedIndex = 4;
        return list;
    }

    private static Border Badge(Button cell) => cell.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("number-badge"));

    private static Rect Box(Visual v, Visual to)
    {
        var a = v.TranslatePoint(default, to)!.Value;
        var b = v.TranslatePoint(new Point(v.Bounds.Width, v.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Each_item_shows_its_state_and_the_current_one_is_marked(string theme)
    {
        var list = Make();
        Show(new Border { Height = 300, Width = 260, Child = list }, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        Assert.Equal("1", list.CellAt(0)!.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("number-text")).Text);
        Assert.Contains("right", list.CellAt(0)!.Classes);
        Assert.Contains("wrong", list.CellAt(1)!.Classes);
        Assert.Contains("done", list.CellAt(2)!.Classes);
        Assert.Contains("current", list.CellAt(4)!.Classes);
        AssertBrush(list, "InsToneOkBg", () => Badge(list.CellAt(0)!).Background);
        AssertBrush(list, "InsToneBadBg", () => Badge(list.CellAt(1)!).Background);
        AssertBrush(list, "InsToneBrandBg", () => Badge(list.CellAt(2)!).Background);
        AssertBrush(list, "InsPrimaryMid", () => Badge(list.CellAt(4)!).Background); // the current one, not done: the brand's fill
        Assert.Equal("Item 2", AutomationProperties.GetName(list.CellAt(1)!));

        // a state changed in place is shown at once; the current one moves with SelectedIndex
        list.Items[5].State = NumberState.Wrong;
        list.SelectedIndex = 5;
        Settle(300);
        Assert.Contains("wrong", list.CellAt(5)!.Classes);
        Assert.Contains("current", list.CellAt(5)!.Classes);
        Assert.DoesNotContain("current", list.CellAt(4)!.Classes);
    }

    // the flag: solid, in the badge's top right corner on the screen in either direction, inside the badge (no row or strip edge clips it)
    [AvaloniaTheory]
    [InlineData("rtl", false)]
    [InlineData("ltr", false)]
    [InlineData("rtl", true)]
    [InlineData("ltr", true)]
    public void The_flag_is_solid_and_sits_inside_the_badges_right_corner(string dir, bool strip)
    {
        var list = Make(orientation: strip ? Orientation.Horizontal : Orientation.Vertical);
        var w = Show(new Border { Height = 300, Width = 260, Child = list }, Dir(dir));
        var flagged = list.CellAt(2)!;
        var flag = flagged.GetVisualDescendants().OfType<Icon>().Single(i => i.Classes.Contains("number-flag"));
        Assert.True(flag.IsVisible);
        Assert.True(flag.IsFilled, "a solid flag");
        Assert.False(list.CellAt(0)!.GetVisualDescendants().OfType<Icon>().Single(i => i.Classes.Contains("number-flag")).IsVisible);
        var badge = Box(Badge(flagged), w);
        var mark = Box(flag, w);
        Assert.True(badge.Contains(mark), $"the flag {mark} inside its badge {badge}");
        Assert.True(mark.Center.X > badge.Center.X, "on the badge's right on the screen");
        Assert.True(mark.Center.Y < badge.Center.Y, "at its top");
        // the number keeps clear of it
        var number = Box(flagged.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("number-text")), w);
        Assert.True(number.Center.X < badge.Center.X, "the number makes room for the flag");
    }

    // the flag reads on every badge (331zr): a halo of the surface's colour sets it apart from each badge's fill; on the current badge's brand fill it is light, outlined in the brand's darker shade
    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void The_flag_has_a_halo_on_every_badge_and_is_light_on_the_current_fill(string theme, bool strip)
    {
        var list = Make(orientation: strip ? Orientation.Horizontal : Orientation.Vertical);
        foreach (var item in list.Items)
            item.IsFlagged = true; // right, wrong, done, open and the current one (open: the brand's fill)
        Show(new Border { Height = 300, Width = 260, Child = list }, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        Settle(300);
        Icon Of(int i, string cls) => list.CellAt(i)!.GetVisualDescendants().OfType<Icon>().Single(x => x.Classes.Contains(cls));
        foreach (var i in new[] { 0, 1, 2, 3, 4 })
        {
            var flag = Of(i, "number-flag");
            var halo = Of(i, "number-flag-halo");
            Assert.True(flag.IsVisible && halo.IsVisible, $"item {i}");
            Assert.True(halo.StrokeThickness > flag.StrokeThickness + 3, "the halo reaches past the flag");
            Assert.Equal(flag.Size, halo.Size);
            Assert.Equal(flag.Margin, halo.Margin);
            // under the flag in the mark
            var mark = (Panel)flag.GetVisualParent()!;
            Assert.True(mark.Children.IndexOf(halo) < mark.Children.IndexOf(flag));
        }
        foreach (var i in new[] { 0, 1, 2, 3 })
        {
            AssertBrush(list, "InsToneWarn", () => Of(i, "number-flag").Foreground);
            AssertBrush(list, "InsSurface", () => Of(i, "number-flag-halo").Foreground);
        }
        AssertBrush(list, "InsOnPrimaryMid", () => Of(4, "number-flag").Foreground);
        AssertBrush(list, "InsPrimaryDarker", () => Of(4, "number-flag-halo").Foreground);
        // a flag taken off hides its halo with it
        list.Items[4].IsFlagged = false;
        Settle(100);
        Assert.False(Of(4, "number-flag").IsVisible);
        Assert.False(Of(4, "number-flag-halo").IsVisible);
    }

    [AvaloniaFact]
    public void A_press_picks_an_item_and_says_so()
    {
        var list = Make();
        var w = Show(new Border { Height = 400, Width = 260, Child = list });
        var invoked = 0;
        list.ItemInvoked += (_, _) => invoked++;
        Click(list.CellAt(7)!, w);
        Assert.Equal(7, list.SelectedIndex);
        Assert.Equal(1, invoked);
    }

    [AvaloniaFact]
    public void The_vertical_list_scrolls_on_its_own_and_folds_to_its_button()
    {
        var list = Make(40);
        var w = Show(new Border { Height = 300, Width = 260, Child = list });
        var scroller = Part<ScrollViewer>(list, "PART_Scroller");
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height, "forty rows scroll inside the list");
        var toggle = Part<Button>(list, "PART_Toggle");
        Assert.Equal("Hide", AutomationProperties.GetName(toggle));

        Click(toggle, w);
        Assert.False(list.IsOpen);
        Assert.False(scroller.IsEffectivelyVisible);
        Assert.False(Part<ContentPresenter>(list, "PART_Header").IsVisible);
        Assert.Equal("Show", AutomationProperties.GetName(toggle));
        Assert.True(list.DesiredSize.Height < 80, "folded, the list is its button alone");

        Click(toggle, w);
        Assert.True(list.IsOpen);
        Assert.True(scroller.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void The_current_item_is_scrolled_into_view()
    {
        var list = Make(40);
        Show(new Border { Height = 240, Width = 260, Child = list });
        list.SelectedIndex = 35;
        Settle(300);
        var scroller = Part<ScrollViewer>(list, "PART_Scroller");
        Assert.True(scroller.Offset.Y > 0);
    }

    // the move to the current item glides on the motion tokens (its first frames between where it was and where it goes); a still window jumps
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_move_to_the_current_item_glides_and_a_still_window_jumps(bool still)
    {
        Motion.SystemOverride = false;
        try
        {
            var list = Make(40, Orientation.Horizontal);
            list.SelectedIndex = 0;
            var w = Show(new Border { Width = 300, Height = 120, Child = list }, FlowDirection.RightToLeft);
            Motion.SetStill(w, still);
            Settle(100);
            var scroller = Part<ScrollViewer>(list, "PART_Scroller");
            Assert.Equal(0, scroller.Offset.X, 0.5);
            list.SelectedIndex = 30;
            var first = scroller.Offset.X;
            Settle(900);
            var end = scroller.Offset.X;
            Assert.True(end > 100, $"the strip moved to the current number ({end})");
            if (still)
                Assert.Equal(end, first, 0.5);
            else
                Assert.True(first < end - 1, $"it starts where it was and glides ({first} -> {end})");
        }
        finally { Motion.SystemOverride = null; }
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_strip_lays_the_numbers_in_the_reading_direction_and_the_arrows_follow_it(string dir)
    {
        var list = Make(20, Orientation.Horizontal);
        var w = Show(new Border { Width = 360, Child = list }, Dir(dir));
        Assert.False(Part<DockPanel>(list, "PART_Head").IsVisible);
        var first = Box(list.CellAt(0)!, w);
        var second = Box(list.CellAt(1)!, w);
        Assert.Equal(first.Top, second.Top, 1);
        if (dir == "rtl")
            Assert.True(second.Right <= first.Left + 0.5, "the second number stands left of the first");
        else
            Assert.True(second.Left >= first.Right - 0.5, "the second number stands right of the first");
        Assert.Equal(40, Badge(list.CellAt(0)!).Bounds.Width, 1);

        list.CellAt(0)!.Focus(NavigationMethod.Tab);
        w.KeyPress(dir == "rtl" ? Key.Left : Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(200);
        Assert.True(list.CellAt(1)!.IsFocused, "the arrow toward the reading direction's end moves to the next number");
        w.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        Settle(200);
        Assert.True(list.CellAt(19)!.IsFocused);
    }

    [AvaloniaFact]
    public void A_wrapping_strip_shows_every_number_on_more_rows()
    {
        var list = Make(20, Orientation.Horizontal);
        list.Wraps = true;
        var w = Show(new Border { Width = 300, Height = 400, Child = list }, FlowDirection.RightToLeft);
        Assert.IsType<WrapPanel>(Part<ScrollViewer>(list, "PART_Scroller").Content is Border host ? host.Child : null);
        var first = Box(list.CellAt(0)!, w);
        var last = Box(list.CellAt(19)!, w);
        Assert.True(last.Top > first.Bottom, "the numbers go on onto the next rows");
        Assert.True(last.Bottom <= 400, "every number is in view, none scrolled sideways");
    }

    [AvaloniaFact]
    public void Under_a_finger_the_strip_numbers_are_a_touch_target()
    {
        var list = Make(10, Orientation.Horizontal);
        Show(new Border { Classes = { "touch" }, Width = 360, Child = list }, FlowDirection.RightToLeft);
        var touch = (double)Res(list, "InsTouchH");
        Assert.Equal(touch, Badge(list.CellAt(0)!).Bounds.Width, 1);
        Assert.Equal(touch, Badge(list.CellAt(0)!).Bounds.Height, 1);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void A_tile_takes_the_verdicts_tone_and_mark_and_stays_legible_when_locked(string theme)
    {
        var right = new RadioButton { Classes = { "tile", "ok" }, Content = "(0, 1)", IsEnabled = false };
        var wrong = new RadioButton { Classes = { "tile", "bad" }, Content = "(-1, 1)", IsChecked = true, IsEnabled = false };
        var plain = new RadioButton { Classes = { "tile" }, Content = "[0, 1]", IsEnabled = false };
        var panel = new StackPanel { Width = 300, Spacing = 10, Children = { right, wrong, plain } };
        Show(panel, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        ShapePath Mark(RadioButton r, string name) => Part<ShapePath>(r, name);
        Assert.True(Mark(right, "VerdictTick").IsEffectivelyVisible);
        Assert.False(Mark(right, "VerdictCross").IsEffectivelyVisible);
        Assert.True(Mark(wrong, "VerdictCross").IsEffectivelyVisible);
        Assert.False(Mark(plain, "VerdictTick").IsEffectivelyVisible);
        Assert.False(Mark(plain, "VerdictCross").IsEffectivelyVisible);
        AssertBrush(right, "InsToneOkBg", () => right.Background);
        AssertBrush(right, "InsToneOk", () => right.BorderBrush);
        AssertBrush(wrong, "InsToneBadBg", () => wrong.Background);
        AssertBrush(wrong, "InsToneBad", () => wrong.BorderBrush);
        Assert.Equal(1, right.Opacity);
        Assert.Equal(1, wrong.Opacity);
        Assert.Equal(0.5, plain.Opacity);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_verdicts_mark_stands_at_the_tiles_end_clear_of_its_words(string dir)
    {
        var tile = new RadioButton { Classes = { "tile", "ok" }, Content = "a long choice that wraps onto more than one line in a narrow tile" };
        var w = Show(new StackPanel { Width = 260, Children = { tile } }, Dir(dir));
        var mark = Box(Part<Panel>(tile, "TileVerdict"), w);
        var words = Box(Part<StackPanel>(tile, "TileText"), w);
        var box = Box(tile, w);
        if (dir == "rtl")
            Assert.True(mark.Left - box.Left < 24, "the mark is at the tile's end (its left in right-to-left)");
        else
            Assert.True(box.Right - mark.Right < 24, "the mark is at the tile's end");
        Assert.False(mark.Intersects(words), "the words leave room for the mark");
    }

    // a number wholly in the strip's view after a scroll (not the current one)
    private static int InView(NumberList list, Window w)
    {
        var view = Box(Part<ScrollViewer>(list, "PART_Scroller"), w);
        for (var i = 1; list.CellAt(i) is { } cell; i++)
            if (view.Contains(Box(cell, w)) && i != list.SelectedIndex)
                return i;
        throw new InvalidOperationException("no number in view");
    }

    private static Border PressLayer(Button cell) => cell.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("number-press"));

    private static void AssertSameBox(Rect expected, Rect actual, string what)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < 0.5 && Math.Abs(expected.Y - actual.Y) < 0.5 && Math.Abs(expected.Width - actual.Width) < 0.5
                    && Math.Abs(expected.Height - actual.Height) < 0.5, $"{what}: {actual} is not {expected}");
    }

    [AvaloniaTheory]
    [InlineData("rtl", false)]
    [InlineData("ltr", false)]
    [InlineData("rtl", true)]
    public void In_the_strip_a_numbers_hit_area_and_press_face_are_its_badge_exactly(string dir, bool wraps)
    {
        var list = Make(20, Orientation.Horizontal);
        list.Wraps = wraps;
        var w = Show(new Border { Classes = { "touch" }, Width = 360, Height = 300, Child = list }, Dir(dir));
        foreach (var i in new[] { 0, 2, 4, 5 })
        {
            var cell = list.CellAt(i)!;
            var badge = Box(Badge(cell), w);
            AssertSameBox(badge, Box(cell, w), $"cell {i + 1}");
            AssertSameBox(badge, Box(PressLayer(cell), w), $"the press layer of {i + 1}");
            Assert.Same(cell, w.InputHitTest(new Point(badge.X + 2, badge.Y + 2)) is Visual v ? global::Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<Button>(v, includeSelf: true) : null);
        }

        // pressed: the layer is drawn over the badge (its own fill would hide one under it), the cell's ground stays clear
        var third = list.CellAt(2)!;
        var at = Center(third, w);
        w.MouseMove(at);
        w.MouseDown(at, MouseButton.Left);
        Settle(60);
        Assert.Contains(":pressed", third.Classes);
        AssertBrush(third, "InsPressLayer", () => PressLayer(third).Background);
        Assert.Equal(Colors.Transparent, ColorOf(third.Background));
        w.MouseUp(at, MouseButton.Left);
        Settle(300);
        Assert.Equal(2, list.SelectedIndex);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_finger_that_starts_on_a_number_and_moves_scrolls_the_strip_and_picks_nothing(string dir)
    {
        var list = Make(30, Orientation.Horizontal);
        list.SelectedIndex = 0;
        var invoked = 0;
        list.ItemInvoked += (_, _) => invoked++;
        var w = Show(new Border { Classes = { "touch" }, Width = 360, Child = list }, Dir(dir));
        var scroller = Part<ScrollViewer>(list, "PART_Scroller");
        Assert.True(scroller.Extent.Width > scroller.Viewport.Width + 100);
        var start = Center(list.CellAt(1)!, w);
        // toward the strip's end: left in right-to-left, right in left-to-right (the content moves the other way)
        var way = dir == "rtl" ? 1 : -1;
        using (var finger = w.TouchBegin(start))
        {
            for (var step = 1; step <= 10; step++)
            {
                w.TouchMove(finger, start + new Point(way * step * 20, 0));
                Settle(16);
            }
            w.TouchEnd(finger, start + new Point(way * 200, 0));
        }
        Settle(100);
        Assert.True(scroller.Offset.X >= 190, $"the strip followed the finger ({scroller.Offset.X})");
        Assert.Equal(0, invoked);
        Assert.Equal(0, list.SelectedIndex);
        Assert.DoesNotContain(":pressed", list.CellAt(1)!.Classes);

        // a tap without real movement picks the number under it
        var shown = InView(list, w);
        var target = list.CellAt(shown)!;
        var p = Center(target, w);
        using (var finger = w.TouchBegin(p))
        {
            w.TouchMove(finger, p + new Point(3, 2));
            w.TouchEnd(finger, p + new Point(3, 2));
        }
        Settle(300);
        Assert.Equal(1, invoked);
        Assert.Equal(shown, list.SelectedIndex);
    }

    [AvaloniaFact]
    public void A_mouse_drag_scrolls_the_strip_and_a_click_still_picks()
    {
        var list = Make(30, Orientation.Horizontal);
        list.SelectedIndex = 0;
        var invoked = 0;
        list.ItemInvoked += (_, _) => invoked++;
        var w = Show(new Border { Width = 360, Child = list }, FlowDirection.RightToLeft);
        var scroller = Part<ScrollViewer>(list, "PART_Scroller");
        var start = Center(list.CellAt(2)!, w);
        w.MouseMove(start);
        w.MouseDown(start, MouseButton.Left);
        w.MouseMove(start + new Point(60, 0));
        w.MouseMove(start + new Point(150, 0));
        w.MouseUp(start + new Point(150, 0), MouseButton.Left);
        Settle(100);
        Assert.Equal(150 - Touch.Slop, scroller.Offset.X, 1);
        Assert.Equal(0, invoked);
        Assert.Equal(0, list.SelectedIndex);

        var shown = InView(list, w);
        Click(list.CellAt(shown)!, w);
        Assert.Equal(1, invoked);
        Assert.Equal(shown, list.SelectedIndex);
    }

    [AvaloniaFact]
    public void A_finger_moving_across_the_strip_leaves_it_to_the_page_and_picks_nothing()
    {
        var list = Make(30, Orientation.Horizontal);
        list.SelectedIndex = 0;
        var invoked = 0;
        list.ItemInvoked += (_, _) => invoked++;
        var page = new ScrollViewer
        {
            Content = new StackPanel { Children = { list, new Border { Height = 1200 } } },
        };
        var w = Show(new Border { Classes = { "touch" }, Width = 360, Height = 400, Child = page }, FlowDirection.RightToLeft);
        var strip = Part<ScrollViewer>(list, "PART_Scroller");
        var start = Center(list.CellAt(1)!, w);
        using (var finger = w.TouchBegin(start))
        {
            for (var step = 1; step <= 10; step++)
            {
                w.TouchMove(finger, start - new Point(0, step * 15));
                Settle(16);
            }
            w.TouchEnd(finger, start - new Point(0, 150));
        }
        Settle(300);
        Assert.Equal(0, strip.Offset.X, 1);
        Assert.Equal(0, invoked);
        Assert.True(page.Offset.Y > 50, $"the page scrolled under the finger ({page.Offset.Y})");
    }

    [AvaloniaFact]
    public void A_drag_on_the_vertical_list_scrolls_it_and_picks_no_row()
    {
        var list = Make(40);
        var invoked = 0;
        list.ItemInvoked += (_, _) => invoked++;
        var w = Show(new Border { Height = 300, Width = 260, Child = list });
        var scroller = Part<ScrollViewer>(list, "PART_Scroller");
        scroller.Offset = default;
        Settle(60);
        var start = Center(list.CellAt(3)!, w);
        w.MouseMove(start);
        w.MouseDown(start, MouseButton.Left);
        w.MouseMove(start - new Point(0, 40));
        w.MouseMove(start - new Point(0, 120));
        w.MouseUp(start - new Point(0, 120), MouseButton.Left);
        Settle(100);
        Assert.Equal(120 - Touch.Slop, scroller.Offset.Y, 1);
        Assert.Equal(0, invoked);
    }
}
