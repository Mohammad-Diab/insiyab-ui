using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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

namespace Insiyab.Avalonia.Tests;

public class ButtonsTests
{
    private static void Tab(Window window)
    {
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(300);
    }

    // Transitions run on the real clock, so under a loaded suite a state is awaited rather than assumed after a fixed delay.
    private static void WaitFor(Func<bool> condition, int milliseconds = 4000)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && clock.ElapsedMilliseconds < milliseconds) Settle(40);
        Settle(40);
    }

    // A spring transition passes its target before it settles there, so the state must hold for longer than the whole transition, not merely be met once.
    private static void WaitSteady(Func<bool> condition, int holdMilliseconds = 400, int milliseconds = 6000)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var held = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            if (!condition()) held.Restart();
            else if (held.ElapsedMilliseconds >= holdMilliseconds) break;
            Settle(16);
        }
    }

    // A brush transition can stop a few units short of its target, more so on a loaded machine.
    private static void AssertNear(Visual anchor, string key, IBrush? actual, int tolerance = 12)
    {
        var want = ColorOf((IBrush)Res(anchor, key));
        var got = ColorOf(actual);
        Assert.True(Math.Abs(want.A - got.A) <= tolerance && Math.Abs(want.R - got.R) <= tolerance && Math.Abs(want.G - got.G) <= tolerance && Math.Abs(want.B - got.B) <= tolerance, $"{key}: expected {want}, got {got}");
    }

    private static void AssertNear(Visual anchor, string key, Func<IBrush?> read, int tolerance = 12)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 2000)
        {
            var want = ColorOf((IBrush)Res(anchor, key));
            if (read() is ISolidColorBrush b && Math.Abs(want.A - b.Color.A) <= tolerance && Math.Abs(want.R - b.Color.R) <= tolerance && Math.Abs(want.G - b.Color.G) <= tolerance && Math.Abs(want.B - b.Color.B) <= tolerance) break;
            Settle(16);
        }
        AssertNear(anchor, key, read(), tolerance);
    }

    private static void AssertTint(Visual anchor, string key, double alpha, IBrush? actual)
    {
        var want = (Color)Res(anchor, key);
        var got = ColorOf(actual);
        Assert.Equal(want.R, got.R);
        Assert.Equal(want.G, got.G);
        Assert.Equal(want.B, got.B);
        Assert.InRange(got.A / 255.0 * actual!.Opacity, alpha - 0.02, alpha + 0.02);
    }

    private static Rect Physical(Control control, Window window)
    {
        var a = control.TranslatePoint(default, window)!.Value;
        var b = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), window)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaFact]
    public void Button_sizes_follow_the_web_heights()
    {
        var normal = new Button { Content = "حفظ", Classes = { "primary" } };
        var sm = new Button { Content = "حفظ", Classes = { "primary", "sm" } };
        var lg = new Button { Content = "حفظ", Classes = { "primary", "lg" } };
        var icon = new Button { Content = new Icon(), Classes = { "primary", "icon" } };
        var iconSm = new Button { Content = new Icon(), Classes = { "primary", "icon", "sm" } };
        var full = new Button { Content = "حفظ", Classes = { "primary", "full" } };
        Show(new StackPanel { Width = 500, Spacing = 8, Children = { normal, sm, lg, icon, iconSm, full } });

        Assert.Equal(38, normal.Bounds.Height);
        Assert.Equal(new Thickness(16, 6), normal.Padding);
        Assert.Equal(Res(normal, "InsRMd"), normal.CornerRadius);
        Assert.Equal(30, sm.Bounds.Height);
        Assert.Equal(Res(sm, "InsRSm"), sm.CornerRadius);
        Assert.Equal(13, sm.FontSize);
        Assert.Equal(48, lg.Bounds.Height);
        Assert.Equal(16, lg.FontSize);
        Assert.Equal(38, icon.Bounds.Width);
        Assert.Equal(30, iconSm.Bounds.Width);
        Assert.Equal(500, full.Bounds.Width);
    }

    [AvaloniaFact]
    public void Bare_button_has_no_face_and_no_height()
    {
        var bare = new Button { Content = "إلغاء", Classes = { "bare" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { bare } });

        Assert.True(bare.Bounds.Height < 30);
        Assert.Equal(FontWeight.Bold, bare.GetVisualDescendants().OfType<TextBlock>().Single().FontWeight);
        Assert.Equal(new Thickness(0), bare.Padding);
        Assert.False(Part<Border>(bare, "PART_Sweep").IsVisible);
        AssertBrush(bare, "InsText2", () => bare.Foreground);
        window.MouseMove(Center(bare, window));
        Settle();
        AssertBrush(bare, "InsText1", () => bare.Foreground);
    }

    [AvaloniaFact]
    public void Ghost_hover_takes_the_brand_wash()
    {
        var ghost = new Button { Content = "تصدير", Classes = { "ghost" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { ghost } });

        AssertTint(ghost, "InsPrimaryColor", 0.45, ghost.BorderBrush);
        window.MouseMove(Center(ghost, window));
        Settle();
        AssertTint(ghost, "InsPrimaryColor", 0.12, ghost.Background);
        AssertBrush(ghost, "InsPrimaryDarker", () => ghost.Foreground);
    }

    [AvaloniaFact]
    public void Press_scales_and_lift_rises()
    {
        var plain = new Button { Content = "حفظ", Classes = { "primary" } };
        var lift = new Button { Content = "ابدأ", Classes = { "primary", "lift" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 20, Children = { plain, lift } });
        var plainRoot = Part<Panel>(plain, "PART_Root");
        var liftRoot = Part<Panel>(lift, "PART_Root");

        window.MouseMove(Center(plain, window));
        window.MouseDown(Center(plain, window), MouseButton.Left);
        WaitFor(() => Math.Abs(plainRoot.RenderTransform!.Value.M11 - 0.97) < 0.005);
        Assert.Equal(0.97, plainRoot.RenderTransform!.Value.M11, 2);
        window.MouseUp(Center(plain, window), MouseButton.Left);
        WaitFor(() => Math.Abs(plainRoot.RenderTransform!.Value.M11 - 1) < 0.005);

        window.MouseMove(Center(lift, window));
        WaitFor(() => Math.Abs(liftRoot.RenderTransform!.Value.M32 + 2) < 0.05);
        Assert.Equal(-2, liftRoot.RenderTransform!.Value.M32, 1);
        Assert.Equal(1, liftRoot.RenderTransform!.Value.M11, 2);
    }

    [AvaloniaFact]
    public void Disabled_button_dims_and_loses_the_sweep()
    {
        var button = new Button { Content = "حفظ", Classes = { "secondary" }, IsEnabled = false };
        Show(button);
        Assert.Equal(0.5, button.Opacity);
        Assert.False(Part<Border>(button, "PART_Sweep").IsVisible);
    }

    [AvaloniaFact]
    public void Toggle_button_reads_pressed_in_and_primary_keeps_its_fill()
    {
        var toggle = new ToggleButton { Content = "حفظ", Classes = { "secondary" } };
        var primary = new ToggleButton { Content = "حفظ", Classes = { "primary" }, IsChecked = true };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { toggle, primary } });

        Part<Border>(toggle, "PART_Face");
        AssertBrush(toggle, "InsCtlBg", () => toggle.Background);
        Click(toggle, window);
        Assert.True(toggle.IsChecked);
        AssertTint(toggle, "InsPrimaryColor", 0.14, toggle.Background);
        AssertTint(toggle, "InsPrimaryColor", 0.40, toggle.BorderBrush);
        AssertBrush(toggle, "InsPrimaryDarker", () => toggle.Foreground);
        Assert.Equal(0, Part<Border>(toggle, "PART_Shadow").BoxShadow.Count);

        Click(toggle, window);
        Assert.False(toggle.IsChecked);

        Assert.IsAssignableFrom<ILinearGradientBrush>(primary.Background);
    }

    [AvaloniaFact]
    public void Repeat_and_link_buttons_share_the_button_face()
    {
        var repeat = new RepeatButton { Content = "+", Classes = { "secondary" } };
        var link = new HyperlinkButton { Content = "حفظ", Classes = { "primary" }, NavigateUri = new Uri("https://example.com/orders") };
        Show(new StackPanel { Children = { repeat, link } });
        link.Theme = (ControlTheme)Res(link, "InsLinkButton");
        Settle(100);

        Part<Border>(repeat, "PART_Fill");
        Assert.Equal(38, repeat.Bounds.Height);
        Part<Border>(link, "PART_Fill");
        Assert.IsAssignableFrom<ILinearGradientBrush>(link.Background);
        Assert.Equal(38, link.Bounds.Height);
    }

    [AvaloniaFact]
    public void Icon_nudges_toward_the_trailing_edge_on_hover()
    {
        var icon = new Icon();
        var button = new Button { Classes = { "primary" }, Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { icon, new TextBlock { Text = "حفظ" } } } };
        var square = new Icon();
        var only = new Button { Classes = { "primary", "icon" }, Content = square };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 20, Children = { button, only } });

        window.MouseMove(Center(button, window));
        WaitFor(() => icon.Margin == new Thickness(2, 0, -2, 0));
        Assert.Equal(new Thickness(2, 0, -2, 0), icon.Margin);

        window.MouseMove(Center(only, window));
        WaitFor(() => icon.Margin == new Thickness(0));
        var presenter = Part<ContentPresenter>(only, "PART_ContentPresenter");
        WaitFor(() => presenter.RenderTransform is { } t && Math.Abs(t.Value.M11 - 1.15) < 0.005);
        Assert.Equal(new Thickness(0), square.Margin);
        Assert.Equal(1.15, presenter.RenderTransform!.Value.M11, 2);
    }

    [AvaloniaFact]
    public void Close_button_draws_its_cross_and_turns_on_hover()
    {
        var close = new Button { Classes = { "close" } };
        var sm = new Button { Classes = { "close", "sm" } };
        var withIcon = new Button { Classes = { "close" }, Content = new Icon() };
        var plain = new Button { Content = "حفظ" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { close, sm, withIcon, plain } });

        Assert.Equal(new Size(30, 30), close.Bounds.Size);
        Assert.Equal(new Size(24, 24), sm.Bounds.Size);
        Assert.True(Part<Grid>(close, "PART_Cross").IsEffectivelyVisible);
        Assert.False(Part<Grid>(withIcon, "PART_Cross").IsEffectivelyVisible);
        Assert.False(Part<Panel>(plain, "PART_CrossHost").IsVisible);
        AssertBrush(close, "InsCtlBg", () => close.Background);
        AssertBrush(close, "InsText2", () => close.Foreground);
        Assert.Equal(0, Part<Border>(close, "PART_Shadow").BoxShadow.Count);

        window.MouseMove(Center(close, window));
        var closeRoot = Part<Panel>(close, "PART_Root");
        WaitSteady(() => closeRoot.RenderTransform is { } t && Math.Abs(t.Value.M11) < 0.005);
        AssertNear(close, "InsCtlBgHover", () => close.Background);
        AssertNear(close, "InsText1", () => close.Foreground);
        Assert.Equal(0, closeRoot.RenderTransform!.Value.M11, 2);
    }

    [AvaloniaFact]
    public void Close_button_bare_and_keyboard_outline()
    {
        var bare = new Button { Classes = { "close", "bare" } };
        var first = new Button { Content = "أول" };
        var close = new Button { Classes = { "close" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { bare, first, close } });

        Assert.Equal(0.6, bare.Opacity, 2);
        Assert.Equal(Colors.Transparent, ColorOf(bare.Background));

        first.Focus(NavigationMethod.Tab);
        Tab(window);
        Assert.True(close.IsFocused);
        Assert.True(Part<Border>(close, "PART_Outline").IsVisible);
        Assert.Equal(0, Part<Border>(close, "PART_FocusRing").Opacity);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Button_group_rounds_only_its_outer_corners(FlowDirection direction)
    {
        var a = new Button { Content = "السابق", Classes = { "secondary" } };
        var b = new Button { Content = "اليوم", Classes = { "secondary" } };
        var c = new Button { Content = "التالي", Classes = { "secondary" } };
        var group = new StackPanel { Classes = { "btn-group" }, Children = { a, b, c } };
        var single = new StackPanel { Classes = { "btn-group" }, Children = { new Button { Content = "وحده" } } };
        var sm = new StackPanel { Classes = { "btn-group", "sm" }, Children = { new Button { Content = "أ", Classes = { "sm" } }, new Button { Content = "ب", Classes = { "sm" } } } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { group, single, sm } }, direction);

        Assert.Equal(new CornerRadius(14, 0, 0, 14), a.CornerRadius);
        Assert.Equal(new CornerRadius(0), b.CornerRadius);
        Assert.Equal(new CornerRadius(0, 14, 14, 0), c.CornerRadius);
        Assert.Equal(new CornerRadius(14), ((Button)single.Children[0]).CornerRadius);
        Assert.Equal(new CornerRadius(10, 0, 0, 10), ((Button)sm.Children[0]).CornerRadius);

        var ra = Physical(a, window);
        var rb = Physical(b, window);
        Assert.Equal(direction == FlowDirection.LeftToRight, ra.X < rb.X);
        Assert.Equal(1, direction == FlowDirection.LeftToRight ? ra.Right - rb.X : rb.Right - ra.X, 1);

        window.MouseMove(Center(b, window));
        Settle(100);
        Assert.Equal(2, b.ZIndex);
    }

    [AvaloniaFact]
    public void Dismiss_closes_the_nearest_alert_or_a_named_target()
    {
        var close = new Button { Classes = { "close" } };
        Dismiss.SetNearest(close, true);
        var alert = new Border { Classes = { "alert" }, Child = close };
        var notice = new Border { Child = new TextBlock { Text = "تنبيه" } };
        var named = new Button { Content = "إغلاق" };
        Dismiss.SetTarget(named, notice);
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { alert, notice, named } });
        Control? closed = null;
        window.AddHandler(Dismiss.DismissedEvent, (_, e) => closed = (Control?)e.Source);

        Click(close, window);
        Assert.False(alert.IsVisible);
        Assert.Same(alert, closed);

        Click(named, window);
        Assert.False(notice.IsVisible);
        Assert.Same(notice, closed);
    }

    [AvaloniaFact]
    public void Radio_selects_one_and_shows_its_dot()
    {
        var a = new RadioButton { Content = "الأول", GroupName = "g1", IsChecked = true };
        var b = new RadioButton { Content = "الثاني", GroupName = "g1" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 16, Children = { a, b } });

        Part<Border>(a, "NormalRectangle");
        Assert.Equal(1, Part<Border>(a, "CheckFill").Opacity);
        Assert.Equal(1, Part<global::Avalonia.Controls.Shapes.Ellipse>(a, "CheckGlyph").RenderTransform!.Value.M11, 2);
        Assert.Equal(0, Part<Border>(b, "CheckFill").Opacity);

        Click(b, window);
        Assert.True(b.IsChecked);
        Assert.False(a.IsChecked);

        Click(b, window);
        Assert.True(b.IsChecked);
    }

    [AvaloniaFact]
    public void Deselect_clears_the_chosen_radio_on_a_second_click_or_space()
    {
        var a = new RadioButton { Content = "الأول", GroupName = "g2", IsChecked = true };
        var b = new RadioButton { Content = "الثاني", GroupName = "g2" };
        var group = new StackPanel { Margin = new Thickness(20), Spacing = 16, Children = { a, b } };
        Choice.SetDeselect(group, true);
        var window = Show(group);

        Click(a, window);
        Assert.False(a.IsChecked);
        Click(a, window);
        Assert.True(a.IsChecked);

        a.Focus(NavigationMethod.Tab);
        PressSpace(window);
        Assert.False(a.IsChecked);
    }

    [AvaloniaFact]
    public void Radio_focus_ring_is_round_for_keyboard_focus()
    {
        var first = new Button { Content = "أول" };
        var radio = new RadioButton { Content = "الأول" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 16, Children = { first, radio } });

        first.Focus(NavigationMethod.Tab);
        Tab(window);
        Assert.True(radio.IsFocused);
        var ring = Part<Border>(radio, "FocusRing");
        Assert.True(ring.IsVisible);
        Assert.Equal(new Thickness(-5), ring.Margin);
    }

    [AvaloniaFact]
    public void Disabled_choice_dims_its_box_and_keeps_its_label()
    {
        var check = new CheckBox { Content = "الأول", IsChecked = true, IsEnabled = false };
        var radio = new RadioButton { Content = "الأول", IsEnabled = false };
        Show(new StackPanel { Children = { check, radio } });

        Assert.Equal(1, check.Opacity);
        Assert.Equal(0.5, Part<Panel>(check, "BoxHost").Opacity);
        Assert.Equal(0.5, Part<Panel>(radio, "BoxHost").Opacity);
    }

    [AvaloniaFact]
    public void Check_hover_brightens_the_box_border_and_blooms()
    {
        var check = new CheckBox { Content = "الأول" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { check } });

        window.MouseMove(Center(Part<Panel>(check, "BoxHost"), window));
        Settle();
        AssertTint(check, "InsPrimaryColor", 0.45, Part<Border>(check, "NormalRectangle").BorderBrush);
        Assert.Equal(1, Part<Border>(check, "HoverGlow").Opacity);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tile_draws_its_mark_ring_and_description(bool radio)
    {
        ToggleButton Make(string text, bool on) => radio
            ? new RadioButton { Content = text, GroupName = "t", Classes = { "tile" }, IsChecked = on }
            : new CheckBox { Content = text, Classes = { "tile" }, IsChecked = on };
        var off = Make("الأول", false);
        var on = Make("الثاني", true);
        Choice.SetDescription(on, "وصف قصير");
        var window = Show(new ChoiceGrid { Classes = { "tile-grid" }, Width = 600, Children = { off, on } });

        Assert.Equal(0, Part<Border>(off, "TileRing").Opacity);
        Assert.False(Part<ContentPresenter>(off, "TileDesc").IsVisible);
        AssertBrush(off, "InsCtlBg", () => off.Background);
        Assert.Equal(1, Part<Border>(on, "TileRing").Opacity);
        Assert.Equal(1, Part<Border>(on, "CheckFill").Opacity);
        Assert.Equal(1, Part<Panel>(on, "TileMark").RenderTransform!.Value.M11, 2);
        Assert.True(Part<ContentPresenter>(on, "TileDesc").IsVisible);
        AssertBrush(on, "InsToneBrandBg", () => on.Background);
        AssertBrush(on, "InsToneBrand", () => Part<ContentPresenter>(on, "PART_ContentPresenter").Foreground);
        Assert.Equal(radio, Part<global::Avalonia.Controls.Shapes.Ellipse>(on, "TileDot").IsVisible);
        Assert.Equal(!radio, Part<global::Avalonia.Controls.Shapes.Path>(on, "TileTick").IsVisible);

        Click(off, window);
        Assert.True(off.IsChecked);
        Assert.Equal(radio ? false : true, on.IsChecked);
    }

    [AvaloniaFact]
    public void Disabled_tile_dims_whole()
    {
        var tile = new RadioButton { Content = "الثالث", Classes = { "tile" }, IsEnabled = false };
        Show(tile);
        Assert.Equal(0.5, tile.Opacity);
        Assert.Equal(1, Part<Panel>(tile, "BoxHost").Opacity);
    }

    [AvaloniaFact]
    public void Switch_shows_a_description_and_no_on_off_words_by_default()
    {
        var plain = new ToggleSwitch { Content = "الإشعارات" };
        var described = new ToggleSwitch { Content = "الإشعارات", IsChecked = true };
        Choice.SetDescription(described, "وصف قصير");
        var card = new ToggleSwitch { Content = "الإشعارات", Classes = { "card" } };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { plain, described, card } });

        Assert.Null(plain.OnContent);
        Assert.False(Part<ContentPresenter>(plain, "DescPresenter").IsVisible);
        Assert.True(Part<ContentPresenter>(described, "DescPresenter").IsVisible);
        AssertBrush(described, "InsText2", () => Part<ContentPresenter>(described, "DescPresenter").Foreground);
        Assert.Equal(26, Part<Panel>(plain, "SwitchAreaGrid").Bounds.Height);
        Assert.Equal(48, Part<Panel>(plain, "SwitchAreaGrid").Bounds.Width);

        AssertBrush(card, "InsCtlBg", () => card.Background);
        AssertBrush(card, "InsLine", () => card.BorderBrush);
        Assert.Equal(new Thickness(14, 12), card.Padding);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Choice_grid_fits_columns_and_cards(FlowDirection direction)
    {
        var a = new ToggleSwitch { Content = "الإشعارات", IsChecked = true };
        Choice.SetDescription(a, "وصف قصير");
        var b = new ToggleSwitch { Content = "التقرير اليومي" };
        var grid = new ChoiceGrid { Classes = { "switch-grid" }, Width = 600, Children = { a, b } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { grid } }, direction);

        Assert.Equal(296, a.Bounds.Width, 1);
        Assert.Equal(a.Bounds.Height, b.Bounds.Height);
        Assert.Equal(Res(a, "InsRMd"), a.CornerRadius);
        var ltr = direction == FlowDirection.LeftToRight;
        Assert.Equal(ltr, Physical(a, window).X < Physical(b, window).X);

        grid.Width = 300;
        Settle(100);
        Assert.Equal(300, a.Bounds.Width, 1);
        Assert.True(b.Bounds.Y > a.Bounds.Y);
    }

    [AvaloniaFact]
    public void Segmented_selects_by_click_and_the_face_flies()
    {
        var seg = new Segmented { Items = { new SegmentedItem { Content = "اليوم" }, new SegmentedItem { Content = "الأسبوع" }, new SegmentedItem { Content = "الشهر" } } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { seg } });
        var changes = 0;
        seg.SelectionChanged += (_, _) => changes++;

        Assert.Equal(0, seg.SelectedIndex);
        var first = (SegmentedItem)seg.Items[0]!;
        var third = (SegmentedItem)seg.Items[2]!;
        Assert.Equal(38, first.Bounds.Height);
        AssertNear(first, "InsSegOnBg", () => first.Background);
        AssertNear(first, "InsSegOnFg", () => first.Foreground);
        AssertNear(third, "InsText2", () => third.Foreground);
        var p = Center(third, window);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Assert.Equal(2, seg.SelectedIndex);
        Assert.Equal(1, changes);
        Assert.True(Part<Border>(seg, "PART_Flight").IsVisible);
        Assert.Contains("moving", seg.Classes);

        var flight = Part<Border>(seg, "PART_Flight");
        var x0 = Canvas.GetLeft(flight);
        WaitFor(() => !flight.IsVisible);
        Settle(300);
        Assert.False(flight.IsVisible, $"x0={x0} x={Canvas.GetLeft(flight)} w={flight.Width} classes={string.Join(',', seg.Classes)} sel={seg.SelectedIndex}");
        Assert.DoesNotContain("moving", seg.Classes);
        AssertNear(third, "InsSegOnBg", () => third.Background);
    }

    [AvaloniaFact]
    public void Segmented_keyboard_tab_focuses_and_space_selects()
    {
        var before = new Button { Content = "قبل" };
        var seg = new Segmented { Items = { new SegmentedItem { Content = "اليوم" }, new SegmentedItem { Content = "الأسبوع" } } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { before, seg } });
        var second = (SegmentedItem)seg.Items[1]!;

        second.Focus(NavigationMethod.Tab);
        Settle(100);
        Assert.Equal(0, seg.SelectedIndex);
        Assert.True(Part<Border>(second, "PART_Outline").IsVisible);

        PressSpace(window);
        Assert.Equal(1, seg.SelectedIndex);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Ui.Looks), MemberType = typeof(Ui))]
    public void Capture_choices(string theme, string dir)
    {
        var tileOn = new RadioButton { Content = "الثاني", GroupName = "c", Classes = { "tile" }, IsChecked = true };
        Choice.SetDescription(tileOn, "وصف قصير");
        var page = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
            Children =
            {
                Row(new RadioButton { Content = "الأول", IsChecked = true, GroupName = "r" }, new RadioButton { Content = "الثاني", GroupName = "r" }, new ToggleButton { Content = "حفظ", IsChecked = true }),
                new StackPanel { Classes = { "btn-group" }, Children = { new Button { Content = "السابق" }, new Button { Content = "اليوم" }, new Button { Content = "التالي" } } },
                Row(new Button { Classes = { "close" } }, new Button { Classes = { "close", "sm" } }, new Button { Classes = { "close", "bare" } }),
                new ChoiceGrid { Classes = { "tile-grid" }, Children = { new RadioButton { Content = "الأول", GroupName = "c", Classes = { "tile" } }, tileOn } },
                new Segmented { Items = { new SegmentedItem { Content = "اليوم" }, new SegmentedItem { Content = "الأسبوع" }, new SegmentedItem { Content = "الشهر" } } },
            },
        };
        var window = Show(page, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 560, 480);
        Capture(window, $"choices-{theme}-{dir}");
    }
}
