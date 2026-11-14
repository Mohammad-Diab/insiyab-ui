using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The command palette, toasts, the segmented row and the chip row at a phone's width.
public class PhoneControlsTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static Rect BoxOf(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static bool Back(TopLevel top)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        top.RaiseEvent(e);
        return e.Handled;
    }

    private static void Swipe(TopLevel w, Point from, Vector by, int steps = 8)
    {
        Settle(550);
        var t = w.TouchBegin(from);
        Settle(40);
        for (var k = 1; k <= steps; k++)
        {
            w.TouchMove(t, from + by * k / steps);
            Settle(12);
        }
        w.TouchEnd(t, from + by);
        Settle(80);
    }

    private static void Tap(TopLevel w, Point p)
    {
        Settle(550);
        var t = w.TouchBegin(p);
        Settle(40);
        w.TouchEnd(t, p);
        Settle(80);
    }

    private static Point CenterIn(Visual c, Visual top) => BoxOf(c, top).Center;

    private static CommandPalette Palette() => new()
    {
        Items =
        {
            new PaletteItem { Label = "الطلبات", Group = "الصفحات" },
            new PaletteItem { Label = "المنتجات", Group = "الصفحات" },
            new PaletteItem { Label = "الأماكن", Group = "الصفحات" },
            new PaletteItem { Label = "رحلة جديدة", Group = "الأوامر" },
        },
    };

    [AvaloniaFact]
    public void The_palette_fills_a_phone_with_its_results_above_the_keyboard_and_back_closes_it()
    {
        var fake = new FakeInsets();
        fake.Set(new Thickness(0, 24, 0, 16));
        FakeInsets.With(fake, () =>
        {
            var opener = new Button { Content = "بحث" };
            var root = OverlaysTests.SingleView(new StackPanel { Children = { opener } }, 360, 740);
            try
            {
                var palette = Palette();
                palette.Open(opener);
                OverlaysTests.Pump(root);
                Assert.True(palette.IsFull);
                var dialog = Part<Panel>(palette, "PART_Dialog");
                Assert.Equal(new Size(360, 740), palette.Bounds.Size);
                Assert.Equal(new Rect(0, 24, 360, 700), BoxOf(dialog, palette));
                Assert.False(Part<Border>(palette, "PART_Foot").IsVisible);
                var close = Part<Button>(palette, "PART_Close");
                Assert.True(close.IsVisible);
                Assert.Equal("إغلاق البحث", global::Avalonia.Automation.AutomationProperties.GetName(close));
                Assert.Same(palette.InputBox, root.FocusManager!.GetFocusedElement());

                fake.Set(new Thickness(0, 24, 0, 16), new Rect(0, 440, 360, 300));
                OverlaysTests.Pump(root, 300);
                var list = Part<ScrollViewer>(palette, "PART_Scroll");
                Assert.True(BoxOf(dialog, palette).Bottom <= 440.5, $"the palette ends at {BoxOf(dialog, palette).Bottom}");
                Assert.True(BoxOf(list, palette).Bottom <= 440.5);

                Assert.True(Back(root));
                Assert.False(palette.IsOpen);
                Assert.False(Back(root));

                palette.Open(opener);
                OverlaysTests.Pump(root, 200);
                close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.False(palette.IsOpen);
            }
            finally { root.StopRendering(); }
        });
    }

    [AvaloniaFact]
    public void A_desktop_window_keeps_the_palette_box_at_any_width_unless_asked_for_full()
    {
        var opener = new Button { Content = "Search" };
        var w = Show(new StackPanel { Children = { opener } }, width: 420, height: 640);
        var palette = Palette();
        palette.Open(opener);
        Settle();
        Assert.False(palette.IsFull);
        Assert.False(Part<Button>(palette, "PART_Close").IsVisible);
        Assert.Equal(96, BoxOf(Part<Panel>(palette, "PART_Dialog"), w).Top, 1);
        palette.Close();

        palette.Classes.Add("full");
        palette.Open(opener);
        Settle();
        Assert.True(palette.IsFull);
        Assert.Equal(new Rect(0, 0, 420, 640), BoxOf(Part<Panel>(palette, "PART_Dialog"), w));
        palette.Close();
    }

    [AvaloniaFact]
    public void A_finger_that_scrolls_the_results_chooses_nothing_and_a_tap_does()
    {
        var opener = new Button { Content = "بحث" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { opener } }, 360, 500);
        try
        {
            var palette = new CommandPalette();
            for (var i = 0; i < 30; i++) palette.Items.Add(new PaletteItem { Label = "مكان " + (i + 1) });
            var chosen = new List<string>();
            palette.Choosing += (_, e) => chosen.Add(e.Label);
            palette.Open(opener);
            OverlaysTests.Pump(root);
            var rows = palette.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("palette-item")).ToList();
            var row = rows[3];
            Swipe(root, CenterIn(row, root), new Vector(0, -120));
            Assert.Empty(chosen);
            Assert.True(palette.IsOpen);
            var target = palette.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("palette-item") && BoxOf(b, root).Top > 150);
            Tap(root, CenterIn(target, root));
            Assert.Single(chosen);
            Assert.False(palette.IsOpen);
        }
        finally { root.StopRendering(); }
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_toast_at_phone_width_sits_centred_above_a_bottom_nav_and_comes_down_when_it_goes(string dir)
    {
        var nav = new BottomNav { Items = { new BottomNavItem { Label = "الرئيسية" }, new BottomNavItem { Label = "الطلبات" }, new BottomNavItem { Label = "الحساب" } } };
        DockPanel.SetDock(nav, Dock.Bottom);
        var anchor = new Button { Content = "احفظ" };
        var w = Show(new DockPanel { Children = { nav, new StackPanel { Children = { anchor } } } }, Dir(dir), width: 390, height: 700);
        Toast.Show(anchor, "تم الحفظ", ToastTone.Ok);
        Settle();
        var manager = w.GetVisualDescendants().OfType<WindowNotificationManager>().Single();
        var navTop = BoxOf(nav, w).Top;
        Assert.Equal(700 - navTop, manager.Margin.Bottom, 1);
        var card = w.GetVisualDescendants().OfType<NotificationCard>().Single();
        var box = BoxOf(card, w);
        Assert.True(box.Bottom <= navTop + 0.5, $"the toast ends at {box.Bottom}, the bar starts at {navTop}");
        Assert.Equal(195, box.Center.X, 1);

        nav.IsVisible = false;
        Settle(300);
        Assert.Equal(0, manager.Margin.Bottom, 1);
    }

    private static Segmented Seg(params string[] labels)
    {
        var seg = new Segmented();
        foreach (var l in labels) seg.Items.Add(new SegmentedItem { Content = l });
        seg.SelectedIndex = 0;
        return seg;
    }

    private static readonly string[] Long = ["الطلبات الجديدة", "قيد التوصيل اليوم", "المسلَّمة هذا الأسبوع", "الملغاة والمرتجعة"];

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_segmented_row_too_wide_scrolls_sideways_and_keeps_the_choice_in_view(string dir)
    {
        var seg = Seg(Long);
        var page = new StackPanel { Margin = new Thickness(16), Children = { seg } };
        var w = Show(page, Dir(dir), width: 360, height: 300);
        var scroll = seg.Scroller!;
        Assert.Contains(":overflow", seg.Classes);
        Assert.True(scroll.Extent.Width > scroll.Viewport.Width + 100);
        Assert.True(scroll.Presenter!.ClipToBounds);
        Assert.Equal(328, seg.Bounds.Width, 1);
        var words = seg.GetVisualDescendants().OfType<TextBlock>().Where(t => Long.Contains(t.Text)).ToList();
        Assert.Equal(4, words.Count);
        Assert.All(words, t => Assert.Equal(t.DesiredSize.Width, t.Bounds.Width, 1));

        seg.SelectedIndex = 3;
        Settle(600);
        var last = (Control)seg.ContainerFromIndex(3)!;
        var view = BoxOf(scroll, w);
        var item = BoxOf(last, w);
        Assert.True(item.Left >= view.Left - 0.5 && item.Right <= view.Right + 0.5, $"{item} in {view}");

        // a finger dragged along the row scrolls it and chooses nothing
        var before = scroll.Offset.X;
        Swipe(w, view.Center, new Vector(dir == "rtl" ? -120 : 120, 0));
        Assert.Equal(3, seg.SelectedIndex);
        Assert.True(Math.Abs(scroll.Offset.X - before) > 40, $"scrolled from {before} to {scroll.Offset.X}");

        // a tap chooses the item under it
        var target = Enumerable.Range(0, 3).Select(i => (Control)seg.ContainerFromIndex(i)!).First(c => view.Contains(CenterIn(c, w)));
        var at = seg.IndexFromContainer(target);
        Tap(w, CenterIn(target, w));
        Assert.Equal(at, seg.SelectedIndex);

        // a choice made from code comes into view
        seg.SelectedIndex = 0;
        Settle(600);
        var first = (Control)seg.ContainerFromIndex(0)!;
        item = BoxOf(first, w);
        Assert.True(item.Left >= view.Left - 0.5 && item.Right <= view.Right + 0.5, $"{item} in {view}");

        // the arrows move the choice along the row as it reads, and keep it in view
        first.Focus(NavigationMethod.Tab);
        w.KeyPress(dir == "rtl" ? Key.Left : Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
        w.KeyRelease(dir == "rtl" ? Key.Left : Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(600);
        Assert.Equal(1, seg.SelectedIndex);
        item = BoxOf((Control)seg.ContainerFromIndex(1)!, w);
        Assert.True(item.Left >= view.Left - 0.5 && item.Right <= view.Right + 0.5, $"{item} in {view}");
        Capture(w, $"segmented-scroll-{dir}");
    }

    [AvaloniaFact]
    public void A_segmented_row_that_fits_draws_as_before_and_does_not_clip()
    {
        var seg = Seg("شهر", "أسبوع", "يوم");
        Show(new StackPanel { Margin = new Thickness(16), Children = { seg } }, FlowDirection.RightToLeft, width: 640, height: 300);
        Assert.DoesNotContain(":overflow", seg.Classes);
        Assert.False(seg.Scroller!.ClipToBounds);
        Assert.False(seg.Scroller.Presenter!.ClipToBounds);
        Assert.Equal(seg.Scroller.Extent.Width, seg.Scroller.Viewport.Width, 1);
        Assert.Equal(new Thickness(4), seg.Scroller.Padding);
    }

    [AvaloniaTheory]
    [InlineData("rtl", "Light")]
    [InlineData("ltr", "Dark")]
    public void A_chip_row_is_one_choice_in_the_chip_look_and_scrolls_when_it_does_not_fit(string dir, string theme)
    {
        var chips = Seg("الكل", "المكاتب", "المستودعات", "المتاجر", "المطاعم", "الحدائق");
        chips.Classes.Add("chips");
        var w = Show(new StackPanel { Margin = new Thickness(16), Children = { chips } }, Dir(dir), theme == "Dark" ? global::Avalonia.Styling.ThemeVariant.Dark : global::Avalonia.Styling.ThemeVariant.Light, 360, 300);
        var on = (SegmentedItem)chips.ContainerFromIndex(0)!;
        var off = (SegmentedItem)chips.ContainerFromIndex(1)!;
        AssertBrush(on, "InsToneBrandBg", () => on.Background);
        AssertBrush(on, "InsToneBrandBd", () => on.BorderBrush);
        AssertBrush(off, "InsSurface", () => off.Background);
        AssertBrush(off, "InsCtlBd", () => off.BorderBrush);
        Assert.Equal(new Thickness(1), off.BorderThickness);
        Assert.Equal(0, chips.BorderThickness.Left);
        Assert.Equal(8, Math.Max(BoxOf(off, w).Left - BoxOf(on, w).Right, BoxOf(on, w).Left - BoxOf(off, w).Right), 1);
        Assert.Contains(":overflow", chips.Classes);

        // screen readers hear one choice in a list
        var peer = ControlAutomationPeer.CreatePeerForElement(chips);
        var selection = Assert.IsAssignableFrom<ISelectionProvider>(peer.GetProvider<ISelectionProvider>());
        Assert.False(selection.CanSelectMultiple);
        Assert.True(selection.IsSelectionRequired);
        var itemPeer = ControlAutomationPeer.CreatePeerForElement(off);
        Assert.Equal("المكاتب", itemPeer.GetName());
        Assert.NotNull(itemPeer.GetProvider<ISelectionItemProvider>());

        // a choice changes colour in place with no face in flight, and comes into view
        chips.SelectedIndex = 5;
        Settle(100);
        Assert.False(Part<Border>(chips, "PART_Flight").IsVisible);
        Assert.DoesNotContain("moving", chips.Classes);
        Settle(600);
        var view = BoxOf(chips.Scroller!, w);
        var item = BoxOf((Control)chips.ContainerFromIndex(5)!, w);
        Assert.True(item.Left >= view.Left - 0.5 && item.Right <= view.Right + 0.5, $"{item} in {view}");
        Capture(w, $"chip-row-{dir}-{theme}");
    }

    [AvaloniaFact]
    public void A_chip_row_binds_its_choice_and_a_finger_drag_never_chooses()
    {
        var places = new[] { "الكل", "المكاتب", "المستودعات", "المتاجر", "المطاعم", "الحدائق", "المتاحف" };
        var chips = new Segmented { Classes = { "chips" }, ItemsSource = places, SelectedItem = "المتاجر" };
        var w = Show(new StackPanel { Margin = new Thickness(16), Children = { chips } }, FlowDirection.RightToLeft, width: 360, height: 300);
        Settle(300);
        Assert.Equal(3, chips.SelectedIndex);
        var view = BoxOf(chips.Scroller!, w);
        var shown = BoxOf((Control)chips.ContainerFromIndex(3)!, w);
        Assert.True(shown.Left >= view.Left - 0.5 && shown.Right <= view.Right + 0.5, "a choice made before the row is seen is in view when it is");

        var visible = Enumerable.Range(0, places.Length).Select(i => (Control)chips.ContainerFromIndex(i)!).First(c => BoxOf(c, w).Left > view.Left + 10 && BoxOf(c, w).Right < view.Right - 10 && chips.IndexFromContainer(c) != 3);
        Swipe(w, CenterIn(visible, w), new Vector(-90, 0));
        Assert.Equal("المتاجر", chips.SelectedItem);
        var target = Enumerable.Range(0, places.Length).Select(i => (Control)chips.ContainerFromIndex(i)!).First(c => BoxOf(c, w).Left > view.Left + 10 && BoxOf(c, w).Right < view.Right - 10 && chips.IndexFromContainer(c) != 3);
        var at = chips.IndexFromContainer(target);
        Tap(w, CenterIn(target, w));
        Assert.Equal(places[at], chips.SelectedItem);
    }
}
