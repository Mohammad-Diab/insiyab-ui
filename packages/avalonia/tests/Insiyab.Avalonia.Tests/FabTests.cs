using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
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

// The floating action button: a round brand button at the end of its container's foot that keeps clear of what covers the screen's foot.
public class FabTests
{
    private static readonly Geometry Plus = Geometry.Parse("M12 5v14M5 12h14");

    private static Button Fab(params string[] more)
    {
        var fab = new Button { Classes = { "fab" }, Content = new Icon { Data = Plus } };
        foreach (var c in more) fab.Classes.Add(c);
        ToolTip.SetTip(fab, "ملف جديد");
        return fab;
    }

    private static Rect BoxOf(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static BoxShadows Token(Visual anchor, string key)
    {
        var top = TopLevel.GetTopLevel(anchor)!;
        Assert.True(anchor.TryFindResource(key, top.ActualThemeVariant, out var value), key);
        return Assert.IsType<BoxShadows>(value);
    }

    private static BoxShadows ShadowOf(Button fab) => Part<Border>(fab, "PART_Shadow").BoxShadow;

    private static List<BoxShadow> Drops(BoxShadows shadows)
    {
        var all = new List<BoxShadow>();
        foreach (var s in shadows) if (!s.IsInset && s.Blur > 0) all.Add(s);
        return all;
    }

    [AvaloniaTheory]
    [InlineData("rtl", "Light")]
    [InlineData("ltr", "Dark")]
    public void A_fab_is_a_round_brand_button_at_the_end_of_its_foot(string dir, string theme)
    {
        var fab = Fab();
        var w = Show(new Panel { Children = { fab } }, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 390, 700);
        var box = BoxOf(fab, w);
        Assert.Equal(new Size(56, 56), box.Size);
        Assert.Equal(700 - 16, box.Bottom, 1);
        if (dir == "rtl") Assert.Equal(16, box.Left, 1);
        else Assert.Equal(390 - 16, box.Right, 1);
        Assert.Equal(new CornerRadius(28), fab.CornerRadius);
        Assert.Equal(new CornerRadius(28), Part<Border>(fab, "PART_FocusRing").CornerRadius);
        Assert.Same(Res(fab, "InsButtonPrimaryFill"), Part<Border>(fab, "PART_Fill").Background);
        AssertBrush(fab, "InsOnPrimary", () => fab.Foreground);
        Assert.Equal(Token(fab, "InsShBrandLg"), ShadowOf(fab));
        var icon = fab.GetVisualDescendants().OfType<Icon>().Single();
        Assert.Equal(new Size(24, 24), icon.Bounds.Size);
        Assert.Equal(box.Center.X, BoxOf(icon, w).Center.X, 1);
        Assert.Equal(box.Center.Y, BoxOf(icon, w).Center.Y, 1);

        var plain = new Button { Content = "عادي" };
        ((Panel)fab.Parent!).Children.Add(plain);
        Settle(300);
        AssertBrush(plain, "InsCtlBg", () => plain.Background);
    }

    [AvaloniaFact]
    public void A_small_fab_is_44_and_neither_size_nudges_its_icon_under_the_pointer()
    {
        var fab = Fab("sm");
        var big = Fab();
        var w = Show(new Panel { Children = { fab, new Panel { Children = { big } } } }, width: 390, height: 300);
        big.HorizontalAlignment = HorizontalAlignment.Left;
        Settle(200);
        Assert.Equal(new Size(44, 44), fab.Bounds.Size);
        Assert.Equal(new CornerRadius(22), fab.CornerRadius);
        Assert.Equal(20, fab.GetVisualDescendants().OfType<Icon>().Single().Bounds.Width, 1);

        foreach (var b in new[] { fab, big })
        {
            w.MouseMove(Center(b, w));
            Settle(400);
            Assert.True(b.IsPointerOver);
            Assert.Equal(default, b.GetVisualDescendants().OfType<Icon>().Single().Margin);
        }
    }

    [AvaloniaFact]
    public void A_press_dips_it_and_lowers_its_shadow()
    {
        var fab = Fab();
        var w = Show(new Panel { Children = { fab } }, width: 390, height: 300);
        var p = Center(fab, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        Settle(500);
        Assert.True(fab.IsPressed);
        Assert.Equal(Token(fab, "InsShBrandMd"), ShadowOf(fab));
        var root = Part<Panel>(fab, "PART_Root");
        Assert.True(root.RenderTransform is { } t && t.Value.M11 < 1, "the face does not dip");
        w.MouseUp(p, MouseButton.Left);
        Settle(500);
        Assert.Equal(Token(fab, "InsShBrandLg"), ShadowOf(fab));
    }

    [AvaloniaFact]
    public void Light_effects_keep_a_smaller_drop_and_a_still_window_drops_it()
    {
        try
        {
            var fab = Fab();
            var w = Show(new Panel { Children = { fab } }, width: 390, height: 300);
            Assert.Single(Drops(ShadowOf(fab)));
            Assert.Equal(32, Drops(ShadowOf(fab))[0].Blur);

            InsiyabTheme.Effects = EffectsLevel.Light;
            Settle(400);
            var light = Assert.Single(Drops(ShadowOf(fab)));
            Assert.True(light.Blur <= 12, $"blur {light.Blur}");

            Motion.SetStill(w, true);
            Settle(400);
            Assert.True(Drops(Token(fab, "InsShBrandLg")).Count == 0, "token " + Token(fab, "InsShBrandLg") + " part " + ShadowOf(fab));
            Assert.Empty(Drops(ShadowOf(fab)));
            Motion.SetStill(w, false);
        }
        finally { InsiyabTheme.ResetEffects(); }
    }

    [AvaloniaFact]
    public void A_screen_reader_hears_its_tip_unless_it_is_named_and_the_keyboard_rings_it()
    {
        var fab = Fab();
        var named = Fab();
        AutomationProperties.SetName(named, "إضافة");
        var before = new Button { Content = "قبل" };
        var w = Show(new Panel { Children = { before, fab, new Panel { Children = { named } } } }, width: 390, height: 300);
        named.HorizontalAlignment = HorizontalAlignment.Left;
        Assert.Equal("ملف جديد", ControlAutomationPeer.CreatePeerForElement(fab).GetName());
        Assert.Equal("إضافة", ControlAutomationPeer.CreatePeerForElement(named).GetName());
        ToolTip.SetTip(fab, "بحث");
        Assert.Equal("بحث", ControlAutomationPeer.CreatePeerForElement(fab).GetName());

        before.Focus();
        w.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        w.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(500);
        Assert.True(fab.IsFocused);
        Assert.Equal(1, Part<Border>(fab, "PART_FocusRing").Opacity, 2);
    }

    [AvaloniaFact]
    public void It_rises_above_the_gesture_bar_a_bottom_nav_and_the_keyboard_and_comes_back_down()
    {
        var fake = new FakeInsets();
        fake.Set(new Thickness(0, 24, 0, 20));
        FakeInsets.With(fake, () =>
        {
            var fab = Fab();
            var nav = new BottomNav { VerticalAlignment = VerticalAlignment.Bottom, Items = { new BottomNavItem { Label = "الملفات" }, new BottomNavItem { Label = "الحساب" } } };
            var w = Show(new Panel { Children = { new Panel { Children = { fab } }, nav } }, FlowDirection.RightToLeft, width: 390, height: 700);
            Settle(300);
            var navTop = BoxOf(nav, w).Top;
            Assert.True(navTop < 700 - 20);
            Assert.Equal(700 - navTop, FabLook.LiftOf(fab), 1);
            Assert.Equal(navTop - 16, BoxOf(fab, w).Bottom, 1);
            Assert.Equal(16, BoxOf(fab, w).Left, 1);

            nav.IsVisible = false;
            Settle(300);
            Assert.Equal(20, FabLook.LiftOf(fab), 1);
            Assert.Equal(700 - 20 - 16, BoxOf(fab, w).Bottom, 1);

            fake.Set(new Thickness(0, 24, 0, 20), new Rect(0, 400, 390, 300));
            Settle(300);
            Assert.Equal(300, FabLook.LiftOf(fab), 1);
            Assert.Equal(400 - 16, BoxOf(fab, w).Bottom, 1);
            var p = Center(fab, w);
            Assert.Same(fab, w.InputHitTest(p) is Visual v ? global::Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<Button>(v, true) : null);

            fake.Set(new Thickness(0, 24, 0, 20));
            Settle(300);
            Assert.Equal(20, FabLook.LiftOf(fab), 1);
        });
    }

    [AvaloniaFact]
    public void A_fab_whose_page_ends_above_the_bar_or_is_placed_elsewhere_stays_put()
    {
        var fab = Fab();
        var top = Fab();
        top.VerticalAlignment = VerticalAlignment.Top;
        var nav = new BottomNav { Items = { new BottomNavItem { Label = "الملفات" }, new BottomNavItem { Label = "الحساب" } } };
        DockPanel.SetDock(nav, Dock.Bottom);
        var page = new Panel { Children = { fab, top } };
        var w = Show(new DockPanel { Children = { nav, page } }, width: 390, height: 700);
        Settle(300);
        Assert.Equal(0, FabLook.LiftOf(fab));
        Assert.Equal(0, FabLook.LiftOf(top));
        Assert.Equal(BoxOf(nav, w).Top - 16, BoxOf(fab, w).Bottom, 1);

        fab.Classes.Remove("fab");
        Settle(100);
        Assert.Equal(0, FabLook.LiftOf(fab));
        Assert.True(string.IsNullOrEmpty(AutomationProperties.GetName(fab)));
    }
}
