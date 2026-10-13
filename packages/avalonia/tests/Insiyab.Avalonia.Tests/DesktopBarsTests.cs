using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ToolbarTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M5 12h14");

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static ToolbarButton Cmd(string label, string? shortcut = null) => new() { Icon = Glyph, Label = label, Shortcut = shortcut };

    // Five commands with a separator, a toggle and a dropdown; the width decides what moves.
    private static Toolbar Bar(double width = double.NaN)
    {
        var bar = new Toolbar { Width = width, HorizontalAlignment = HorizontalAlignment.Left };
        bar.Items.Add(Cmd("جديد", "Ctrl+N"));
        bar.Items.Add(Cmd("فتح"));
        bar.Items.Add(new Separator());
        bar.Items.Add(new ToolbarToggleButton { Icon = Glyph, Label = "مميّز" });
        var share = Cmd("مشاركة");
        share.Flyout = new PopMenu { Items = { new MenuItem { Header = "بالبريد" }, new MenuItem { Header = "نسخ الرابط" } } };
        bar.Items.Add(share);
        bar.Items.Add(Cmd("حذف"));
        return bar;
    }

    private static Toolbar Shown(out Window window, double width = double.NaN, FlowDirection dir = FlowDirection.LeftToRight, ThemeVariant? theme = null)
    {
        var bar = Bar(width);
        window = Show(new StackPanel { Children = { bar } }, dir, theme);
        return bar;
    }

    private static string Label(Control c) => c is ToolbarButton b ? b.Label! : c is ToolbarToggleButton t ? t.Label! : c.GetType().Name;

    [AvaloniaFact]
    public void Everything_fits_in_a_wide_bar()
    {
        var bar = Shown(out _);
        Assert.Empty(bar.OverflowItems);
        Assert.False(bar.MoreButton!.IsVisible);
        Assert.All(bar.Items, c => Assert.True(c.IsVisible));
    }

    [AvaloniaTheory]
    [InlineData("ltr")]
    [InlineData("rtl")]
    public void A_narrow_bar_moves_commands_from_the_end(string dir)
    {
        var bar = Shown(out _, 150, Dir(dir));
        var moved = bar.OverflowItems.ToList();
        Assert.NotEmpty(moved);
        Assert.Contains(bar.Items[^1], moved);
        Assert.DoesNotContain(bar.Items[0], moved);
        Assert.True(bar.MoreButton!.IsVisible);
        Assert.All(moved, c => Assert.False(c.IsVisible));
        var first = bar.Items.IndexOf(moved[0]);
        Assert.All(bar.Items.Skip(first), c => Assert.Contains(c, moved));
        Assert.True(bar.Bounds.Width <= 150.5);
    }

    [AvaloniaFact]
    public void Widening_brings_the_commands_back()
    {
        var bar = Shown(out _, 150);
        Assert.NotEmpty(bar.OverflowItems);
        bar.Width = double.NaN;
        Settle();
        Assert.Empty(bar.OverflowItems);
        Assert.False(bar.MoreButton!.IsVisible);
    }

    [AvaloniaFact]
    public void Never_stays_and_always_moves()
    {
        var bar = Bar(150);
        Toolbar.SetOverflow(bar.Items[^1], ToolbarOverflow.Never);
        Toolbar.SetOverflow(bar.Items[0], ToolbarOverflow.Always);
        Show(new StackPanel { Children = { bar } });
        Assert.True(bar.Items[^1].IsVisible);
        Assert.Contains(bar.Items[0], bar.OverflowItems);
        Assert.False(bar.Items[0].IsVisible);
        Assert.True(bar.MoreButton!.IsVisible);
    }

    [AvaloniaFact]
    public void An_always_command_shows_the_more_button_in_a_wide_bar()
    {
        var bar = Bar();
        Toolbar.SetOverflow(bar.Items[1], ToolbarOverflow.Always);
        Show(new StackPanel { Children = { bar } });
        Assert.Equal(new[] { bar.Items[1] }, bar.OverflowItems);
        Assert.True(bar.MoreButton!.IsVisible);
    }

    [AvaloniaFact]
    public void A_search_field_never_moves()
    {
        var bar = Bar(150);
        var search = new TextBox { Width = 60 };
        bar.Items.Add(search);
        Show(new StackPanel { Children = { bar } });
        Assert.True(search.IsVisible);
        Assert.DoesNotContain(search, bar.OverflowItems);
    }

    [AvaloniaFact]
    public void A_separator_never_ends_the_row()
    {
        var bar = Shown(out _, 110);
        var shown = bar.Items.Where(c => c.IsVisible).ToList();
        Assert.IsNotType<Separator>(shown[^1]);
        Assert.IsNotType<Separator>(shown[0]);
    }

    [AvaloniaFact]
    public void The_menu_lists_what_moved_with_labels()
    {
        var bar = Bar();
        foreach (var c in bar.Items) Toolbar.SetOverflow(c, ToolbarOverflow.Always);
        Show(new StackPanel { Children = { bar } });
        var entries = bar.MenuEntries();
        var items = entries.OfType<MenuItem>().ToList();
        Assert.Equal(new[] { "جديد", "فتح", "مميّز", "مشاركة", "حذف" }, items.Select(i => (string)i.Header!));
        Assert.Single(entries.OfType<Separator>());
        Assert.Equal(MenuItemToggleType.CheckBox, items[2].ToggleType);
        Assert.Equal(2, items[3].Items.Count);
        Assert.NotNull(items[0].InputGesture);
        Assert.IsType<Icon>(items[0].Icon);
    }

    [AvaloniaFact]
    public void Choosing_from_the_menu_clicks_the_command()
    {
        var bar = Bar();
        foreach (var c in bar.Items) Toolbar.SetOverflow(c, ToolbarOverflow.Always);
        var clicks = 0;
        ((Button)bar.Items[0]).Click += (_, _) => clicks++;
        Show(new StackPanel { Children = { bar } });
        var items = bar.MenuEntries().OfType<MenuItem>().ToList();
        Toolbar.Invoke(bar.Items[0]);
        Assert.Equal(1, clicks);
        items[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(2, clicks);
        var toggle = (ToolbarToggleButton)bar.Items[3];
        items[2].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.True(toggle.IsChecked);
    }

    [AvaloniaFact]
    public void A_submenu_item_passes_its_click_on()
    {
        var bar = Bar();
        Toolbar.SetOverflow(bar.Items[4], ToolbarOverflow.Always);
        var original = (MenuItem)((PopMenu)((Button)bar.Items[4]).Flyout!).Items[1]!;
        var clicked = false;
        original.Click += (_, _) => clicked = true;
        Show(new StackPanel { Children = { bar } });
        var share = bar.MenuEntries().OfType<MenuItem>().Single();
        ((MenuItem)share.Items[1]!).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.True(clicked);
    }

    [AvaloniaFact]
    public void A_split_button_is_a_submenu_with_its_main_action_first()
    {
        var bar = new Toolbar();
        var split = new ToolbarSplitButton { Icon = Glyph, Label = "تصدير", Flyout = new PopMenu { Items = { new MenuItem { Header = "PDF" } } } };
        var clicks = 0;
        split.Click += (_, _) => clicks++;
        Toolbar.SetOverflow(split, ToolbarOverflow.Always);
        bar.Items.Add(split);
        Show(new StackPanel { Children = { bar } });
        var entry = bar.MenuEntries().OfType<MenuItem>().Single();
        Assert.Equal(3, entry.Items.Count);
        Assert.Equal("تصدير", ((MenuItem)entry.Items[0]!).Header);
        ((MenuItem)entry.Items[0]!).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(1, clicks);
    }

    [AvaloniaTheory]
    [InlineData("ltr")]
    [InlineData("rtl")]
    public void Arrows_move_along_the_row_in_the_page_direction(string dir)
    {
        var bar = Shown(out var w, dir: Dir(dir));
        var first = (Control)bar.Items[0];
        first.Focus(NavigationMethod.Tab);
        var forward = dir == "rtl" ? Key.Left : Key.Right;
        var back = dir == "rtl" ? Key.Right : Key.Left;
        w.KeyPress(forward, RawInputModifiers.None, forward == Key.Left ? PhysicalKey.ArrowLeft : PhysicalKey.ArrowRight, null);
        Settle(100);
        Assert.True(bar.Items[1].IsFocused);
        w.KeyPress(forward, RawInputModifiers.None, forward == Key.Left ? PhysicalKey.ArrowLeft : PhysicalKey.ArrowRight, null);
        Settle(100);
        Assert.True(bar.Items[3].IsFocused, "the separator is skipped");
        w.KeyPress(back, RawInputModifiers.None, back == Key.Left ? PhysicalKey.ArrowLeft : PhysicalKey.ArrowRight, null);
        Settle(100);
        Assert.True(bar.Items[1].IsFocused);
        w.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        Settle(100);
        Assert.True(bar.Items[^1].IsFocused);
        w.KeyPress(Key.Home, RawInputModifiers.None, PhysicalKey.Home, null);
        Settle(100);
        Assert.True(first.IsFocused);
    }

    [AvaloniaFact]
    public void The_bar_is_one_tab_stop()
    {
        var before = new Button { Content = "قبل" };
        var after = new Button { Content = "بعد" };
        var bar = Bar();
        var w = Show(new StackPanel { Children = { before, bar, after } });
        before.Focus(NavigationMethod.Tab);
        w.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(100);
        Assert.True(bar.Items[0].IsFocused);
        w.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(100);
        Assert.True(after.IsFocused);
    }

    [AvaloniaFact]
    public void Disabled_commands_are_skipped_by_the_arrows()
    {
        var bar = Bar();
        bar.Items[1].IsEnabled = false;
        var w = Show(new StackPanel { Children = { bar } });
        ((Control)bar.Items[0]).Focus(NavigationMethod.Tab);
        w.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Settle(100);
        Assert.True(bar.Items[3].IsFocused);
    }

    [AvaloniaFact]
    public void The_tip_carries_the_name_and_the_shortcut()
    {
        var bar = Shown(out _);
        var tip = Assert.IsType<StackPanel>(ToolTip.GetTip(bar.Items[0]));
        var texts = tip.Children.OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Equal(new[] { "جديد", "Ctrl+N" }, texts);
        Assert.Equal("فتح", ToolTip.GetTip(bar.Items[1]));
        Assert.Equal("جديد", AutomationProperties.GetName(bar.Items[0]));
    }

    [AvaloniaFact]
    public void A_shown_label_without_a_shortcut_needs_no_tip()
    {
        var b = new ToolbarButton { Icon = Glyph, Label = "فتح", ShowLabel = true };
        Assert.Null(ToolTip.GetTip(b));
        b.Shortcut = "Ctrl+O";
        Assert.NotNull(ToolTip.GetTip(b));
    }

    [AvaloniaFact]
    public void The_apps_own_tip_wins()
    {
        var b = new ToolbarButton { Icon = Glyph };
        ToolTip.SetTip(b, "تلميحي");
        b.Label = "فتح";
        Assert.Equal("تلميحي", ToolTip.GetTip(b));
    }

    [AvaloniaFact]
    public void A_dropdown_shows_a_chevron_and_the_more_button_does_not()
    {
        var bar = Shown(out _, 150);
        var share = (ToolbarButton)bar.Items[4];
        Icon Chevron(Control c) => c.GetVisualDescendants().OfType<Icon>().First(i => i.Classes.Contains("toolbar-chevron"));
        bar.Width = double.NaN;
        Settle();
        Assert.True(Chevron(share).IsVisible);
        Assert.False(Chevron((ToolbarButton)bar.Items[0]).IsVisible);
        Toolbar.SetOverflow(bar.Items[1], ToolbarOverflow.Always);
        Settle();
        Assert.False(Chevron(bar.MoreButton!).IsVisible);
    }

    [AvaloniaFact]
    public void Labels_show_beside_the_icon_only_when_asked()
    {
        var bar = Shown(out _);
        var b = (ToolbarButton)bar.Items[0];
        TextBlock LabelOf() => b.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("toolbar-label"));
        Assert.False(LabelOf().IsVisible);
        var narrow = b.Bounds.Width;
        b.ShowLabel = true;
        Settle();
        Assert.True(LabelOf().IsVisible);
        Assert.True(b.Bounds.Width > narrow + 10);
    }

    [AvaloniaFact]
    public void Items_belong_to_the_toolbar()
    {
        var bar = Bar();
        Assert.All(bar.Items, c => Assert.Same(bar, c.GetLogicalParent()));
        Show(new StackPanel { Children = { bar } });
        Assert.All(bar.Items, c => Assert.Same(bar, c.GetLogicalParent()));
        Assert.All(bar.Items, c => Assert.True(bar.IsVisualAncestorOf(c)));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void The_glass_strip_and_a_toggle_that_is_on(string theme, string dir)
    {
        var bar = Shown(out _, dir: Dir(dir), theme: Variant(theme));
        AssertBrush(bar, "InsGlass", () => bar.Background);
        var toggle = (ToolbarToggleButton)bar.Items[3];
        toggle.IsChecked = true;
        Settle();
        AssertBrush(bar, "InsPrimaryDarker", () => toggle.Foreground);
    }

    [AvaloniaFact]
    public void Plain_drops_the_glass()
    {
        var bar = Shown(out _);
        bar.Classes.Add("plain");
        Settle();
        Assert.Equal(Colors.Transparent, ColorOf(bar.Background));
        Assert.Equal(new Thickness(0, 0, 0, 1), bar.BorderThickness);
    }
}

public class StatusBarTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static StatusBar Bar(out StatusItem start, out StatusButton end)
    {
        var bar = new StatusBar();
        bar.Start.Add(start = new StatusItem { Content = "جاهز", Icon = Geometry.Parse("M5 12h14") });
        bar.Start.Add(new Separator());
        bar.End.Add(new SaveStatus { State = SaveState.Saved });
        bar.End.Add(end = new StatusButton { Content = "العربية" });
        return bar;
    }

    [AvaloniaTheory]
    [InlineData("ltr")]
    [InlineData("rtl")]
    public void Start_and_end_sit_on_their_sides(string dir)
    {
        var bar = Bar(out var start, out var end);
        var w = Show(new DockPanel { Children = { bar } }, Dir(dir));
        var s = Center(start, w).X;
        var e = Center(end, w).X;
        if (dir == "rtl") Assert.True(s > e, $"start {s} should be right of end {e}");
        else Assert.True(s < e, $"start {s} should be left of end {e}");
    }

    [AvaloniaFact]
    public void It_is_thin_and_quiet()
    {
        var bar = Bar(out var start, out _);
        Show(new DockPanel { Children = { bar } });
        Assert.InRange(bar.Bounds.Height, 26, 32);
        Assert.Equal(12, bar.FontSize);
        Assert.Same(bar, start.GetLogicalParent());
    }

    [AvaloniaFact]
    public void Docked_under_the_shell_it_covers_nothing()
    {
        var bar = Bar(out _, out _);
        DockPanel.SetDock(bar, Dock.Bottom);
        var shell = new SplitView { Content = new Border() };
        var w = Show(new DockPanel { Children = { bar, shell } }, width: 700, height: 400);
        var barTop = bar.TranslatePoint(default, w)!.Value.Y;
        var shellBottom = shell.TranslatePoint(new Point(0, shell.Bounds.Height), w)!.Value.Y;
        Assert.Equal(w.Bounds.Height, barTop + bar.Bounds.Height, 1);
        Assert.True(shellBottom <= barTop + 0.5, $"the shell ends at {shellBottom}, the bar starts at {barTop}");
    }

    // The start side cuts items past its end, yet a focus ring or glow keeps room above, below and at the start.
    [AvaloniaFact]
    public void The_start_side_cuts_its_end_but_not_a_ring()
    {
        var bar = Bar(out var start, out _);
        Show(new DockPanel { Children = { bar } });
        var zone = Part<StatusBarZone>(bar, "PART_Start");
        Assert.False(bar.ClipToBounds);
        Assert.False(zone.ClipToBounds);
        var clip = Assert.IsType<RectangleGeometry>(zone.Clip).Rect;
        Assert.True(clip.Top <= -3 && clip.Bottom >= zone.Bounds.Height + 3 && clip.Left <= -3, clip.ToString());
        Assert.Equal(zone.Bounds.Width, clip.Right, 1);
    }

    // Toasts live in the window's bottom end corner, so a status bar at the foot lifts them above it, and they drop back when it goes.
    [AvaloniaTheory]
    [InlineData("ltr")]
    [InlineData("rtl")]
    public void Toasts_sit_above_the_bar(string dir)
    {
        var bar = Bar(out _, out _);
        DockPanel.SetDock(bar, Dock.Bottom);
        var anchor = new Button { Content = "حفظ" };
        var root = new DockPanel { Children = { bar, new StackPanel { Margin = new Thickness(24), Children = { anchor } } } };
        var w = Show(root, Dir(dir), width: 900, height: 600);
        Toast.Show(anchor, "تم الحفظ", ToastTone.Ok);
        Settle();
        var card = Toast.ManagerFor(anchor)!.GetVisualDescendants().OfType<global::Avalonia.Controls.Notifications.NotificationCard>().Single();
        var barTop = bar.TranslatePoint(default, w)!.Value.Y;
        var cardBottom = card.TranslatePoint(new Point(0, card.Bounds.Height), w)!.Value.Y;
        Assert.Equal(barTop - 16, cardBottom, 1);

        root.Children.Remove(bar);
        Settle();
        cardBottom = card.TranslatePoint(new Point(0, card.Bounds.Height), w)!.Value.Y;
        Assert.Equal(600 - 16, cardBottom, 1);
    }

    [AvaloniaFact]
    public void A_busy_item_spins_instead_of_its_icon()
    {
        var bar = Bar(out var start, out _);
        Show(new DockPanel { Children = { bar } });
        var icon = Part<Icon>(start, "PART_Icon");
        var spinner = Part<LoadingSpinner>(start, "PART_Spinner");
        Assert.True(icon.IsEffectivelyVisible);
        Assert.False(spinner.IsVisible);
        start.IsBusy = true;
        Settle();
        Assert.False(icon.IsVisible);
        Assert.True(spinner.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void A_clickable_item_lights_under_the_pointer()
    {
        var bar = Bar(out _, out var end);
        var w = Show(new DockPanel { Children = { bar } });
        Assert.Equal(Colors.Transparent, ColorOf(end.Background));
        w.MouseMove(Center(end, w));
        Settle();
        AssertBrush(end, "InsCtlBgHover", () => end.Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void The_strip_is_frosted_with_a_hairline(string theme, string dir)
    {
        var bar = Bar(out _, out _);
        Show(new DockPanel { Children = { bar } }, Dir(dir), Variant(theme));
        AssertBrush(bar, "InsLine", () => bar.BorderBrush);
        AssertBrush(bar, "InsText2", () => bar.Foreground);
        Assert.Equal(new Thickness(0, 1, 0, 0), bar.BorderThickness);
    }

    [AvaloniaFact]
    public void Items_added_later_show_up()
    {
        var bar = Bar(out _, out _);
        Show(new DockPanel { Children = { bar } });
        var late = new StatusItem { Content = "السطر 4" };
        bar.Start.Add(late);
        Settle();
        Assert.True(bar.IsVisualAncestorOf(late));
        bar.Start.Remove(late);
        Settle();
        Assert.False(bar.IsVisualAncestorOf(late));
        Assert.Null(late.GetLogicalParent());
    }
}
