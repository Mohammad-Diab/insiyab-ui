using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The bottom drawer as a phone sheet: its handle, its height, the safe area and the keyboard, and the pull that closes it or springs back.
public class BottomSheetTests
{
    private static StackPanel Rows(int count)
    {
        var rows = new StackPanel { Spacing = 0 };
        for (var i = 0; i < count; i++) rows.Children.Add(new Border { Height = 40, Child = new TextBlock { Text = "Option " + i } });
        return rows;
    }

    private static Dialog Sheet(Control content, string? title = "Filters", bool forced = false)
    {
        var d = new Dialog { Title = title, Content = content, Classes = { "drawer", "bottom" } };
        if (forced) d.Classes.Add("sheet");
        return d;
    }

    private static Rect BoxOf(Control c, TopLevel top)
    {
        var a = c.TranslatePoint(default, top)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), top)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static bool Back(TopLevel top)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        top.RaiseEvent(e);
        return e.Handled;
    }

    [AvaloniaFact]
    public void A_bottom_drawer_in_a_desktop_window_stays_as_it_was()
    {
        var opener = new Button { Content = "Open" };
        var w = Show(new StackPanel { Children = { opener } }, width: 800, height: 600);
        var drawer = Sheet(Rows(40));
        drawer.Open(opener);
        Settle();
        Assert.DoesNotContain(":sheet", drawer.Classes);
        Assert.False(Part<Panel>(drawer, "PART_Grip").IsVisible);
        Assert.Equal(800, drawer.Bounds.Width, 1);
        Assert.Equal(600 * 0.85, drawer.Bounds.Height, 1);
        Assert.Equal(new Thickness(0, 1, 0, 0), drawer.BorderThickness);
        Assert.Equal(default, drawer.SheetFoot);
        drawer.Close();
        Settle();
    }

    [AvaloniaFact]
    public void A_sheet_in_a_single_view_has_a_handle_and_opens_at_its_content_height()
    {
        var opener = new Button { Content = "فتح" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { opener } }, 390, 800);
        try
        {
            var sheet = Sheet(new TextBlock { Text = "تفاصيل قصيرة" }, "التفاصيل");
            sheet.Open(opener);
            OverlaysTests.Pump(root);
            Assert.Contains(":sheet", sheet.Classes);
            var grip = Part<Panel>(sheet, "PART_Grip");
            Assert.True(grip.IsVisible);
            var handle = Part<Border>(sheet, "PART_Handle");
            Assert.Equal(new Size(36, 4), handle.Bounds.Size);
            Assert.True(sheet.Bounds.Height < 260, $"a short sheet is {sheet.Bounds.Height} tall");
            var box = BoxOf(sheet, root);
            Assert.Equal(800, box.Bottom, 1);
            Assert.Equal(390, box.Width, 1);
            Assert.Equal(new CornerRadius(32, 32, 0, 0), Part<Border>(sheet, "PART_Fill").CornerRadius);
            Assert.True(BoxOf(handle, root).Top < BoxOf(Part<Border>(sheet, "PART_Head"), root).Top);
        }
        finally { root.StopRendering(); }
    }

    [AvaloniaFact]
    public void A_long_sheet_stops_at_nine_tenths_and_only_its_body_scrolls()
    {
        var opener = new Button { Content = "Open" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { opener } }, 390, 800, FlowDirection.LeftToRight);
        try
        {
            var sheet = Sheet(Rows(60));
            sheet.Open(opener);
            OverlaysTests.Pump(root);
            Assert.Equal(720, sheet.Bounds.Height, 1);
            var body = Part<ScrollViewer>(sheet, "PART_Scroll");
            Assert.True(body.Extent.Height > body.Viewport.Height + 100);
            var head = Part<Border>(sheet, "PART_Head");
            body.Offset = new Vector(0, 300);
            OverlaysTests.Pump(root, 100);
            Assert.Equal(80, BoxOf(sheet, root).Top, 1);
            Assert.True(BoxOf(head, root).Top < BoxOf(body, root).Top, "the head stays above the body as it scrolls");
        }
        finally { root.StopRendering(); }
    }

    [AvaloniaFact]
    public void A_sheet_runs_under_the_gesture_bar_and_rises_above_the_keyboard()
    {
        var fake = new FakeInsets();
        fake.Set(new Thickness(0, 24, 0, 30));
        FakeInsets.With(fake, () =>
        {
            var opener = new Button { Content = "Open" };
            var root = OverlaysTests.SingleView(new StackPanel { Children = { opener } }, 390, 800, FlowDirection.LeftToRight);
            try
            {
                var field = new TextBox { Text = "Note" };
                var sheet = Sheet(new StackPanel { Children = { new TextBlock { Text = "Name" }, field } }, "Edit");
                sheet.Open(opener);
                OverlaysTests.Pump(root);
                Assert.Equal(800, BoxOf(sheet, root).Bottom, 1);
                Assert.Equal(new Thickness(0, 0, 0, 30), sheet.SheetFoot);
                var dock = Part<DockPanel>(sheet, "PART_Dock");
                Assert.Equal(770, BoxOf(dock, root).Bottom, 1);

                fake.Set(new Thickness(0, 24, 0, 30), new Rect(0, 500, 390, 300));
                OverlaysTests.Pump(root, 300);
                Assert.Equal(500, BoxOf(sheet, root).Bottom, 1);
                Assert.Equal(default, sheet.SheetFoot);

                fake.Set(new Thickness(0, 24, 0, 30));
                OverlaysTests.Pump(root, 300);
                Assert.Equal(800, BoxOf(sheet, root).Bottom, 1);
            }
            finally { root.StopRendering(); }
        });
    }

    [AvaloniaFact]
    public void Pulling_the_handle_past_the_threshold_closes_the_sheet_and_a_short_pull_springs_back()
    {
        var opener = new Button { Content = "Open" };
        var w = Show(new StackPanel { Children = { opener } }, width: 420, height: 800);
        var sheet = Sheet(Rows(8), forced: true);
        sheet.Open(opener);
        Settle();
        Assert.Contains(":sheet", sheet.Classes);
        var grip = Part<Panel>(sheet, "PART_Grip");
        var start = Center(grip, w);

        void Pull(double by, int steps)
        {
            w.MouseMove(start);
            w.MouseDown(start, MouseButton.Left);
            for (var k = 1; k <= steps; k++)
            {
                w.MouseMove(start + new Vector(0, by * k / steps), RawInputModifiers.LeftMouseButton);
                Settle(30);
            }
        }

        Pull(60, 6);
        Assert.Equal(60, sheet.RenderTransform!.Value.M32, 1);
        var scrim = OverlaysTestsLayer(sheet).Scrim;
        Assert.True(scrim.Opacity < 1 && scrim.Opacity > 0.5, $"the scrim is at {scrim.Opacity}");
        w.MouseUp(start + new Vector(0, 60), MouseButton.Left);
        Settle();
        Assert.True(sheet.IsOpen);
        Assert.Equal(0, sheet.RenderTransform!.Value.M32, 1);
        Assert.Equal(1, scrim.Opacity, 2);

        var head = Part<Border>(sheet, "PART_Head");
        start = new Point(Center(head, w).X - 60, Center(head, w).Y);
        Pull(200, 10);
        w.MouseUp(start + new Vector(0, 200), MouseButton.Left);
        Settle();
        Assert.False(sheet.IsOpen);
    }

    [AvaloniaFact]
    public void A_finger_in_the_body_pulls_the_sheet_at_the_top_and_scrolls_it_below()
    {
        var opener = new Button { Content = "Open" };
        var w = Show(new StackPanel { Children = { opener } }, width: 420, height: 800);
        Touch.SetTouch(w, true);
        var sheet = Sheet(Rows(60), forced: true);
        sheet.Open(opener);
        Settle();
        var body = Part<ScrollViewer>(sheet, "PART_Scroll");
        body.IsScrollInertiaEnabled = false;

        void Swipe(Vector by, int steps = 10)
        {
            Settle(400);
            var from = Center(body, w);
            var t = w.TouchBegin(from);
            Settle(30);
            for (var k = 1; k <= steps; k++)
            {
                w.TouchMove(t, from + by * k / steps);
                Settle(20);
            }
            w.TouchEnd(t, from + by);
            Settle(700);
        }

        body.Offset = new Vector(0, 400);
        Settle(100);
        Swipe(new Vector(0, 200));
        Assert.True(sheet.IsOpen);
        Assert.True(body.Offset.Y < 400, $"the body scrolled back to {body.Offset.Y}");

        body.Offset = default;
        Settle(100);
        Swipe(new Vector(0, -150));
        Assert.True(sheet.IsOpen);
        Assert.True(body.Offset.Y > 50, "a swipe up at the top scrolls the body");

        body.Offset = default;
        Settle(100);
        Swipe(new Vector(0, 260));
        Assert.False(sheet.IsOpen);
    }

    [AvaloniaFact]
    public void A_quick_flick_closes_it_and_closed_by_none_keeps_it()
    {
        var opener = new Button { Content = "Open" };
        var w = Show(new StackPanel { Children = { opener } }, width: 420, height: 800);
        Touch.SetTouch(w, true);
        var sheet = Sheet(Rows(12), forced: true);
        sheet.Open(opener);
        Settle();
        var grip = Part<Panel>(sheet, "PART_Grip");
        var from = Center(grip, w);
        var t = w.TouchBegin(from);
        Settle(30);
        for (var k = 1; k <= 3; k++)
        {
            w.TouchMove(t, from + new Vector(0, 35 * k));
            Settle(8);
        }
        w.TouchEnd(t, from + new Vector(0, 105));
        Settle();
        Assert.True(105 < SheetDrag.Threshold(sheet.Bounds.Height), "the flick was shorter than the threshold");
        Assert.False(sheet.IsOpen);

        var kept = Sheet(Rows(6), forced: true);
        kept.ClosedBy = DialogClosedBy.None;
        kept.Open(opener);
        Settle();
        grip = Part<Panel>(kept, "PART_Grip");
        from = Center(grip, w);
        Settle(400);
        t = w.TouchBegin(from);
        Settle(30);
        for (var k = 1; k <= 10; k++)
        {
            w.TouchMove(t, from + new Vector(0, 30 * k));
            Settle(20);
        }
        w.TouchEnd(t, from + new Vector(0, 300));
        Settle();
        Assert.True(kept.IsOpen);
        Assert.Equal(0, kept.RenderTransform!.Value.M32, 1);
        kept.Close();
        Settle();
    }

    [AvaloniaFact]
    public void Back_and_a_tap_on_the_scrim_close_a_sheet()
    {
        var opener = new Button { Content = "Open" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { opener } }, 390, 800);
        try
        {
            var sheet = Sheet(Rows(4));
            sheet.Open(opener);
            OverlaysTests.Pump(root);
            Assert.True(Back(root));
            OverlaysTests.Pump(root, 400);
            Assert.False(sheet.IsOpen);

            sheet.Open(opener);
            OverlaysTests.Pump(root);
            var t = root.TouchBegin(new Point(195, 100));
            OverlaysTests.Pump(root, 40);
            root.TouchEnd(t, new Point(195, 100));
            OverlaysTests.Pump(root, 400);
            Assert.False(sheet.IsOpen);
        }
        finally { root.StopRendering(); }
    }

    private static DialogLayer OverlaysTestsLayer(Dialog dialog) => DialogStack.Of(dialog)!.Layers.Single(l => l.Dialog == dialog);
}
