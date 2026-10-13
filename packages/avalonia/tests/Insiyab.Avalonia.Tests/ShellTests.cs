using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ShellTests
{
    private static SplitView NewShell(out ListBox nav, out Button menu, out Border bar)
    {
        nav = new ListBox
        {
            Classes = { "shell-nav" },
            Items =
            {
                new ListBoxItem { Classes = { "shell-group" }, Content = "القسم" },
                new ListBoxItem { Content = "الرئيسية" },
                new ListBoxItem { Content = "الطلبات" },
                new ListBoxItem { Content = "التقارير" },
            },
            SelectedIndex = 1,
        };
        menu = new Button { Classes = { "island", "icon" }, Content = "≡" };
        Shell.SetTogglesSidebar(menu, true);
        bar = new Border
        {
            Classes = { "topbar" },
            VerticalAlignment = VerticalAlignment.Top,
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
                Children =
                {
                    new StackPanel { Classes = { "zone", "start" }, Children = { menu, new Border { Name = "Brand", Classes = { "island", "brand" }, Child = new TextBlock { Text = "تطبيقي" } } } },
                    new StackPanel { Classes = { "zone", "center" }, Children = { new Border { Classes = { "island", "title" }, Child = new TextBlock { Classes = { "shell-title" }, Text = "العنوان" } } } },
                },
            },
        };
        return new SplitView { IsPaneOpen = true, Pane = nav, Content = new Panel { Children = { bar } } };
    }

    private static Control Find(Visual root, string name) => root.GetVisualDescendants().OfType<Control>().First(c => c.Name == name);

    [AvaloniaFact]
    public void SplitView_is_a_glass_sidebar_at_the_web_width()
    {
        var shell = NewShell(out _, out _, out _);
        Show(shell, width: 1200, height: 600);

        Assert.Equal(SplitViewDisplayMode.Inline, shell.DisplayMode);
        Assert.Equal(252, Part<Panel>(shell, "PART_PaneRoot").Bounds.Width, 1);
        var chrome = Part<Border>(shell, "PART_PaneChrome");
        AssertBrush(shell, "InsGlass", () => chrome.Background);
        AssertBrush(shell, "InsRim", () => chrome.BorderBrush);
        Assert.Equal(new Thickness(0, 0, 1, 0), chrome.BorderThickness);
        Assert.Equal(new Thickness(12, 18), Part<ContentPresenter>(shell, "PART_PanePresenter").Padding);
    }

    [AvaloniaFact]
    public void Menu_button_collapses_and_restores_the_column()
    {
        var shell = NewShell(out _, out var menu, out var bar);
        var window = Show(shell, width: 1200, height: 600);
        var brand = (Border)Find(bar, "Brand");
        Assert.False(brand.IsVisible);

        Click(menu, window);
        Assert.False(shell.IsPaneOpen);
        Assert.InRange(Part<Panel>(shell, "PART_PaneRoot").Bounds.Width, 0, 1);
        Assert.Equal(0, Part<Border>(shell, "PART_PaneChrome").Opacity, 2);
        Assert.True(brand.IsVisible);

        Click(menu, window);
        Assert.True(shell.IsPaneOpen);
        Assert.Equal(252, Part<Panel>(shell, "PART_PaneRoot").Bounds.Width, 1);
        Assert.False(brand.IsVisible);
    }

    [AvaloniaFact]
    public void Below_900_the_sidebar_is_a_closed_drawer_that_the_menu_opens_and_escape_closes()
    {
        var shell = NewShell(out var nav, out var menu, out var bar);
        var window = Show(shell, width: 800, height: 500);

        Assert.True(Shell.IsDrawer(shell));
        Assert.Equal(SplitViewDisplayMode.Overlay, shell.DisplayMode);
        Assert.False(shell.IsPaneOpen);
        Assert.True(((Border)Find(bar, "Brand")).IsVisible);
        var pane = Part<Panel>(shell, "PART_PaneRoot");
        var scrim = Part<Rectangle>(shell, "LightDismissLayer");
        Assert.True(pane.TranslatePoint(new Point(pane.Bounds.Width, 0), window)!.Value.X <= 0);
        Assert.Equal(0, scrim.Opacity, 2);

        Click(menu, window);
        Assert.True(shell.IsPaneOpen);
        Assert.Equal(0, pane.TranslatePoint(default, window)!.Value.X, 1);
        Assert.Equal(1, scrim.Opacity, 2);
        Assert.Equal(Res(shell, "InsShLg"), Part<Border>(shell, "PART_PaneChrome").BoxShadow);

        nav.Focus();
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Settle();
        Assert.False(shell.IsPaneOpen);

        window.Width = 1200;
        Settle();
        Assert.False(Shell.IsDrawer(shell));
        Assert.Equal(SplitViewDisplayMode.Inline, shell.DisplayMode);
        Assert.True(shell.IsPaneOpen);
    }

    [AvaloniaFact]
    public void Sidebar_action_and_event_follow_the_web()
    {
        var shell = NewShell(out _, out var menu, out _);
        Shell.SetSidebarAction(menu, "collapsed");
        var window = Show(shell, width: 1200, height: 600);
        var seen = new List<(string State, bool IsDrawer)>();
        Shell.AddSidebarChangedHandler(shell, (_, e) => seen.Add((e.State, e.IsDrawer)));

        Click(menu, window);
        Assert.False(shell.IsPaneOpen);
        Assert.Equal(("collapsed", false), seen[^1]);
        Click(menu, window);
        Assert.False(shell.IsPaneOpen);
        Assert.Equal("open", Shell.Sidebar(shell, "open"));
        Assert.Equal(("open", false), seen[^1]);

        window.Width = 800;
        Settle();
        Assert.Equal("open", Shell.Sidebar(shell, "open"));
        Assert.Equal(("open", true), seen[^1]);
        Assert.Equal("closed", Shell.Sidebar(shell));
        Assert.Equal(("closed", true), seen[^1]);
    }

    [AvaloniaFact]
    public void Scrim_click_closes_the_drawer()
    {
        var shell = NewShell(out _, out var menu, out _);
        var window = Show(shell, width: 800, height: 500);
        Click(menu, window);
        Assert.True(shell.IsPaneOpen);

        window.MouseDown(new Point(700, 300), MouseButton.Left);
        window.MouseUp(new Point(700, 300), MouseButton.Left);
        Settle();
        Assert.False(shell.IsPaneOpen);
        Assert.Equal(0, Part<Rectangle>(shell, "LightDismissLayer").Opacity, 2);
    }

    [AvaloniaFact]
    public void Phone_width_drops_the_brand_and_the_subtitle()
    {
        var shell = NewShell(out _, out _, out var bar);
        var titleIsland = (Border)((StackPanel)((Grid)bar.Child!).Children[1]).Children[0];
        var sub = new TextBlock { Classes = { "shell-sub" }, Text = "سطر فرعي" };
        titleIsland.Child = new StackPanel { Classes = { "shell-head" }, Children = { new TextBlock { Classes = { "shell-title" }, Text = "العنوان" }, sub } };
        var brand = (Border)((StackPanel)((Grid)bar.Child!).Children[0]).Children[1];
        var window = Show(shell, width: 600, height: 500);

        Assert.Contains("narrow", shell.Classes);
        Assert.DoesNotContain("xnarrow", shell.Classes);
        Assert.False(brand.IsVisible);
        Assert.Equal(new Thickness(12, 10), bar.Padding);
        Assert.True(sub.IsVisible);

        window.Width = 500;
        Settle();
        Assert.Contains("xnarrow", shell.Classes);
        Assert.False(sub.IsVisible);

        window.Width = 1200;
        Settle();
        Assert.DoesNotContain("narrow", shell.Classes);
        Assert.DoesNotContain("xnarrow", shell.Classes);
        Assert.Equal(new Thickness(16, 12), bar.Padding);
    }

    [AvaloniaFact]
    public void Rtl_puts_the_sidebar_and_the_drawer_on_the_right()
    {
        var shell = NewShell(out _, out var menu, out _);
        var window = Show(shell, FlowDirection.RightToLeft, width: 1000, height: 500);
        var pane = Part<Panel>(shell, "PART_PaneRoot");
        Assert.True(pane.TranslatePoint(new Point(pane.Bounds.Width / 2, 10), window)!.Value.X > 700);

        window.Width = 600;
        Settle();
        Assert.True(Shell.IsDrawer(shell));
        var hidden = pane.TranslatePoint(new Point(pane.Bounds.Width / 2, 10), window)!.Value.X;
        Assert.True(hidden > 600, $"closed drawer should be past the right edge, was at {hidden}");
        Click(menu, window);
        Assert.True(pane.TranslatePoint(new Point(pane.Bounds.Width / 2, 10), window)!.Value.X > 400);
    }

    private static Visual? TabFrom(Window window, Control from, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        from.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Tab, modifiers, PhysicalKey.Tab, null);
        Settle(100);
        return window.FocusManager?.GetFocusedElement() as Visual;
    }

    private static bool InNav(Visual? focused, ListBox nav) => focused is not null && (focused == nav || focused.FindAncestorOfType<ListBox>() == nav);

    [AvaloniaFact]
    public void Tab_reaches_the_sidebar_before_the_content_as_on_the_web()
    {
        var shell = NewShell(out var nav, out var menu, out _);
        var after = new Button { Content = "بعد" };
        var root = new DockPanel { Children = { after, shell } };
        DockPanel.SetDock(after, Dock.Bottom);
        var window = Show(root, width: 1200, height: 600);
        var link = (ListBoxItem)nav.ContainerFromIndex(1)!;

        Assert.Same(menu, TabFrom(window, link));
        Assert.True(InNav(TabFrom(window, after), nav));
    }

    [AvaloniaFact]
    public void Hidden_sidebar_leaves_the_tab_order()
    {
        var shell = NewShell(out var nav, out var menu, out _);
        var window = Show(shell, width: 1200, height: 600);
        Assert.True(InNav(TabFrom(window, menu, RawInputModifiers.Shift), nav));

        Shell.Sidebar(shell, "collapsed");
        Settle();
        Assert.False(InNav(TabFrom(window, menu, RawInputModifiers.Shift), nav));
        Assert.False(InNav(TabFrom(window, menu), nav));

        Shell.Sidebar(shell, "open");
        Settle();
        Assert.True(InNav(TabFrom(window, menu, RawInputModifiers.Shift), nav));

        window.Width = 800;
        Settle();
        Assert.True(Shell.IsDrawer(shell));
        Assert.False(InNav(TabFrom(window, menu, RawInputModifiers.Shift), nav));
        Click(menu, window);
        Assert.True(InNav(TabFrom(window, menu, RawInputModifiers.Shift), nav));
    }

    [AvaloniaFact]
    public void Drawer_glass_lies_on_the_wash_and_the_column_stays_clear()
    {
        var drawer = NewShell(out _, out _, out _);
        Show(drawer, width: 800, height: 500);
        AssertBrush(drawer, "InsWash1", () => Part<Panel>(drawer, "PART_PaneRoot").Background);

        var column = NewShell(out _, out _, out _);
        Show(column, width: 1200, height: 500);
        Assert.Null(Part<Panel>(column, "PART_PaneRoot").Background);
    }

    [AvaloniaFact]
    public void Shell_nav_marks_the_selected_link_and_skips_group_headings()
    {
        var shell = NewShell(out var nav, out _, out _);
        var window = Show(shell, width: 1200, height: 600);
        var heading = (ListBoxItem)nav.ContainerFromIndex(0)!;
        var home = (ListBoxItem)nav.ContainerFromIndex(1)!;
        var orders = (ListBoxItem)nav.ContainerFromIndex(2)!;

        Assert.Equal(44, home.Bounds.Height, 1);
        Assert.False(heading.IsEnabled);
        AssertBrush(home, "InsSurfaceHover", () => home.Background);
        AssertBrush(home, "InsPrimaryDarker", () => home.Foreground);
        Assert.Equal(Res(home, "InsShellLinkActiveShadow"), Part<Border>(home, "PART_Face").BoxShadow);
        Assert.Equal(1, Part<Border>(home, "PART_Marker").RenderTransform!.Value.M22, 2);
        Assert.Equal(0, Part<Border>(orders, "PART_Marker").RenderTransform!.Value.M22, 2);
        AssertBrush(orders, "InsText2", () => orders.Foreground);
        Click(heading, window);
        Assert.Equal(1, nav.SelectedIndex);
        Click(orders, window);
        Assert.Equal(2, nav.SelectedIndex);
    }

    [AvaloniaFact]
    public void Changing_the_selection_flies_the_marker_and_lands_it()
    {
        var shell = NewShell(out var nav, out _, out _);
        Show(shell, width: 1200, height: 600);
        var flight = Part<Canvas>(nav, "PART_Flight");

        nav.SelectedIndex = 3;
        Ui.Settle(100);
        var bar = Assert.Single(flight.Children);
        Assert.Contains("shell-indicator", bar.Classes);
        Assert.Contains("moving", nav.Classes);
        Assert.Equal(0, Part<Border>((ListBoxItem)nav.ContainerFromIndex(3)!, "PART_Marker").Opacity);

        Settle(900);
        Assert.Empty(flight.Children);
        Assert.DoesNotContain("moving", nav.Classes);
    }

    [AvaloniaFact]
    public void Link_hover_nudges_toward_the_content_and_lifts_the_icon()
    {
        var icon = new Icon { Data = Geometry.Parse("M0,0 L24,24") };
        var link = new ListBoxItem { Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { icon, new TextBlock { Text = "الطلبات" } } } };
        var nav = new ListBox { Classes = { "shell-nav" }, Items = { link }, Width = 240 };
        var window = Show(nav);
        Assert.Equal(18, icon.Bounds.Width);
        AssertBrush(icon, "InsText3", () => icon.Foreground);
        window.MouseMove(Center(link, window));
        Settle();
        AssertBrush(link, "InsGlassInner", () => link.Background);
        AssertBrush(icon, "InsPrimaryDarker", () => icon.Foreground);
        Assert.Equal(2, link.RenderTransform!.Value.M31, 1);
        Assert.Equal(1.12, icon.RenderTransform!.Value.M11, 2);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Island_shadow_is_the_bubble_and_the_bevel(string theme, string dir)
    {
        var island = new Border { Classes = { "island" }, Child = new TextBlock { Text = "مسودة" } };
        var button = new Button { Classes = { "island" }, Content = "حسابي" };
        Show(new StackPanel { Classes = { "zone" }, Children = { island, button } }, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var bubble = (BoxShadows)Res(island, "InsShBubble");
        var bevel = (BoxShadows)Res(island, "InsBevel");
        var expected = new BoxShadows(bubble[0], [bevel[0]]);
        Assert.Equal(expected, island.BoxShadow);
        Assert.Equal(expected, Part<Border>(button, "PART_Face").BoxShadow);
        Assert.Equal(48, island.Bounds.Height);
        Assert.Equal(48, button.Bounds.Height);
        AssertBrush(island, "InsGlass", () => island.Background);
        AssertBrush(island, "InsRim", () => island.BorderBrush);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Frost_and_hover_shadows_match_their_tokens(string theme, string dir)
    {
        var host = new Border();
        Show(host, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var md = (BoxShadows)Res(host, "InsShMd");
        var sm = (BoxShadows)Res(host, "InsShSm");
        var bevel = (BoxShadows)Res(host, "InsBevel");
        var rim = ColorOf((IBrush)Res(host, "InsRim"));
        var rimTop = ColorOf((IBrush)Res(host, "InsRimTop"));
        Assert.Equal(new BoxShadows(md[0], [md[1], bevel[0]]), Res(host, "InsIslandShadowHover"));
        Assert.Equal(new BoxShadows(new BoxShadow { OffsetY = 1, Color = rim }, [sm[0], sm[1]]), Res(host, "InsTopbarFrostShadow"));
        var active = (BoxShadows)Res(host, "InsShellLinkActiveShadow");
        Assert.Equal(rimTop, active[1].Color);
        Assert.True(active[1].IsInset);
    }

    [AvaloniaFact]
    public void Island_button_rises_under_the_pointer()
    {
        var button = new Button { Classes = { "island" }, Content = "حسابي" };
        var window = Show(new StackPanel { Margin = new Thickness(30), Children = { button } });
        window.MouseMove(Center(button, window));
        Settle();
        AssertBrush(button, "InsGlassHover", () => button.Background);
        Assert.Equal(-1, button.RenderTransform!.Value.M32, 1);
        Assert.Equal(Res(button, "InsIslandShadowHover"), Part<Border>(button, "PART_Face").BoxShadow);
    }

    [AvaloniaFact]
    public void Topbar_centres_its_title_and_frosts_once_scrolled()
    {
        var shell = NewShell(out _, out _, out var bar);
        var scroller = new ScrollViewer { Content = new Border { Height = 2000 } };
        Shell.SetFrostsTopbar(scroller, true);
        ((Panel)shell.Content!).Children.Insert(0, scroller);
        Show(shell, width: 1200, height: 600);

        var title = bar.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("title"));
        var mid = title.TranslatePoint(new Point(title.Bounds.Width / 2, 0), bar)!.Value.X;
        Assert.Equal(bar.Bounds.Width / 2, mid, 1);
        Assert.Equal(Brushes.Transparent, bar.Background);

        scroller.Offset = new Vector(0, 40);
        Settle();
        Assert.Contains("scrolled", bar.Classes);
        AssertBrush(bar, "InsGlass", () => bar.Background);
        scroller.Offset = default;
        Settle();
        Assert.DoesNotContain("scrolled", bar.Classes);
    }

    [AvaloniaFact]
    public void Brand_mark_takes_the_brand_gradient_left_to_right()
    {
        var mark = new Border { Classes = { "shell-mark" }, Child = new TextBlock { Text = "م" } };
        Show(new StackPanel { Children = { mark } }, FlowDirection.RightToLeft);
        Assert.Equal(34, mark.Bounds.Width);
        Assert.Equal(FlowDirection.LeftToRight, mark.FlowDirection);
        var fill = Assert.IsAssignableFrom<ILinearGradientBrush>(mark.Background);
        Assert.Equal(Res(mark, "InsPrimaryColor"), fill.GradientStops[0].Color);
        Assert.Equal(Res(mark, "InsPrimaryDarkColor"), fill.GradientStops[1].Color);
    }

    [AvaloniaFact]
    public void Brand_link_tilts_its_mark_under_the_pointer()
    {
        var mark = new Border { Classes = { "shell-mark" }, Child = new TextBlock { Text = "م" } };
        var brand = new Button { Classes = { "shell-brand" }, Content = new StackPanel { Children = { mark, new TextBlock { Classes = { "shell-brand-name" }, Text = "تطبيقي" } } } };
        var window = Show(new StackPanel { Width = 260, Margin = new Thickness(20), Children = { brand } });
        Assert.Equal(1, mark.RenderTransform!.Value.M11, 2);
        Assert.Equal(new Thickness(0, 0, 0, 1), brand.BorderThickness);

        window.MouseMove(Center(brand, window));
        Settle();
        Assert.Equal(1.06 * Math.Cos(4 * Math.PI / 180), mark.RenderTransform!.Value.M11, 2);
        Assert.NotEqual(0, mark.RenderTransform!.Value.M12);
        Assert.Equal(Res(mark, "InsShBrandMd"), mark.BoxShadow);
    }

    [AvaloniaFact]
    public void Title_island_settles_in_when_it_arrives()
    {
        var host = new Border();
        Show(host);
        var island = new Border { Classes = { "island", "title" }, Child = new TextBlock { Text = "العنوان" } };
        host.Child = island;
        Settle(60);
        Assert.True(island.Opacity < 1);

        Settle();
        Assert.Equal(1, island.Opacity, 2);
        Assert.True(island.RenderTransform is null || (Math.Abs(island.RenderTransform.Value.M11 - 1) < 0.01 && Math.Abs(island.RenderTransform.Value.M32) < 0.01));
    }

    [AvaloniaFact]
    public void Breadcrumb_marks_the_first_and_current_crumbs_and_reports_clicks()
    {
        var crumbs = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "الإعدادات", "الفواتير" } };
        var window = Show(crumbs);
        var items = crumbs.GetRealizedContainers().Cast<BreadcrumbItem>().ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(":first", items[0].Classes);
        Assert.False(Part<Path>(items[0], "PART_Separator").IsVisible);
        Assert.True(Part<Path>(items[1], "PART_Separator").IsVisible);
        Assert.True(items[2].IsCurrent);
        Assert.False(items[1].IsCurrent);
        AssertBrush(items[2], "InsSegOnBg", () => items[2].Background);
        AssertBrush(items[2], "InsSegOnFg", () => items[2].Foreground);
        Assert.Equal(Res(items[2], "InsSegOnShadow"), Part<Border>(items[2], "PART_Pill").BoxShadow);
        AssertBrush(items[0], "InsText2", () => items[0].Foreground);
        AssertBrush(crumbs, "InsGlass", () => Part<Border>(crumbs, "PART_Track").Background);
        Assert.InRange(Part<Border>(items[0], "PART_Pill").Bounds.Height, 34, 35);
        int? clicked = null;
        crumbs.ItemClick += (_, e) => clicked = e.Index;
        Click(items[1], window);
        Assert.Equal(1, clicked);
        clicked = null;
        Click(items[2], window);
        Assert.Null(clicked);
    }

    [AvaloniaFact]
    public void Breadcrumb_separator_points_toward_the_end_of_the_line()
    {
        var crumbs = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "الفواتير" } };
        var window = Show(crumbs, FlowDirection.RightToLeft);
        var items = crumbs.GetRealizedContainers().Cast<BreadcrumbItem>().ToList();
        var sep = Part<Path>(items[1], "PART_Separator");
        var tip = sep.TranslatePoint(new Point(4.5, 4), window)!.Value.X;
        var tail = sep.TranslatePoint(new Point(1, 0.5), window)!.Value.X;
        Assert.True(tip < tail, "in RTL the chevron's point is on its left");
        Assert.True(items[1].TranslatePoint(default, window)!.Value.X < items[0].TranslatePoint(default, window)!.Value.X);
    }

    [AvaloniaFact]
    public void Breadcrumb_cuts_a_long_current_page_with_an_ellipsis()
    {
        var crumbs = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "فواتير الربع الأول من العام الحالي وما بعده" } };
        Show(crumbs);
        var current = crumbs.GetRealizedContainers().Cast<BreadcrumbItem>().Last();
        Assert.True(Part<Border>(current, "PART_Pill").Bounds.Width <= 172);
        Assert.Equal(TextTrimming.CharacterEllipsis, Part<ContentPresenter>(current, "PART_ContentPresenter").TextTrimming);
    }

    [AvaloniaFact]
    public void Page_head_lays_out_title_sub_and_actions_and_stacks_when_narrow()
    {
        var save = new Button { Classes = { "primary" }, Content = "حفظ" };
        var head = new PageHead { Title = "عنوان الصفحة", Subtitle = "سطر وصف قصير", Actions = save, Breadcrumb = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "الصفحة" } } };
        var host = new Border { Width = 800, Child = head };
        var window = Show(host, width: 900);

        var title = Part<TextBlock>(head, "PART_Title");
        Assert.Equal(28, title.FontSize);
        Assert.Equal(FontWeight.Bold, title.FontWeight);
        AssertBrush(head, "InsLabel", () => Part<TextBlock>(head, "PART_Subtitle").Foreground);
        Assert.DoesNotContain(":stacked", head.Classes);
        var titleBottom = title.TranslatePoint(new Point(0, 0), head)!.Value.Y;
        Assert.True(save.TranslatePoint(default, head)!.Value.X > 600);
        Assert.True(Part<ContentPresenter>(head, "PART_Breadcrumb").TranslatePoint(default, head)!.Value.Y < titleBottom);

        host.Width = 300;
        Settle();
        Assert.Contains(":stacked", head.Classes);
        Assert.True(save.TranslatePoint(default, head)!.Value.Y > title.TranslatePoint(default, head)!.Value.Y + title.Bounds.Height);
    }

    [AvaloniaFact]
    public void Page_head_hides_what_it_is_not_given()
    {
        var head = new PageHead { Title = "عنوان الصفحة" };
        Show(head);
        Assert.False(Part<TextBlock>(head, "PART_Subtitle").IsVisible);
        Assert.False(Part<ContentPresenter>(head, "PART_Breadcrumb").IsVisible);
        Assert.False(Part<ContentPresenter>(head, "PART_Actions").IsVisible);
    }

    private static Navbar NewNavbar(double width)
    {
        var bar = new Navbar
        {
            Brand = "الموقع",
            ItemsSource = new[] { "الرئيسية", "المدونة", "تواصل معنا" },
            SelectedIndex = 0,
            End = new Button { Classes = { "secondary", "sm" }, Content = "دخول" },
            Width = width,
        };
        return bar;
    }

    [AvaloniaFact]
    public void Navbar_is_a_glass_island_with_the_current_link_underlined()
    {
        var bar = NewNavbar(1000);
        var window = Show(bar, width: 1100);
        Assert.False(bar.IsCollapsed);
        Assert.False(Part<ToggleButton>(bar, "PART_Toggle").IsVisible);
        var face = Part<Border>(bar, "PART_Bar");
        AssertBrush(bar, "InsGlass", () => face.Background);
        Assert.Equal(Res(bar, "InsIslandShadow"), face.BoxShadow);
        Assert.True(face.Bounds.Height >= 56);

        var links = bar.GetRealizedContainers().Cast<NavbarLink>().ToList();
        Assert.Equal(3, links.Count);
        AssertBrush(links[0], "InsToneBrand", () => links[0].Foreground);
        Assert.Equal(1, Part<Border>(links[0], "PART_Marker").RenderTransform!.Value.M11, 2);
        Assert.Equal(0, Part<Border>(links[1], "PART_Marker").RenderTransform!.Value.M11, 2);
        AssertBrush(links[1], "InsLabel", () => links[1].Foreground);
        window.MouseMove(Center(links[1], window));
        Settle();
        AssertBrush(links[1], "InsCtlBg", () => links[1].Background);
        Assert.Equal(0.35, Part<Border>(links[1], "PART_Marker").RenderTransform!.Value.M11, 2);
    }

    [AvaloniaFact]
    public void Narrow_navbar_folds_behind_a_toggle_that_opens_a_column()
    {
        var bar = NewNavbar(380);
        var events = 0;
        bar.Toggled += (_, _) => events++;
        var window = Show(bar);
        var toggle = Part<ToggleButton>(bar, "PART_Toggle");
        var menu = Part<Border>(bar, "PART_Menu");
        Assert.True(bar.IsCollapsed);
        Assert.True(toggle.IsVisible);
        Assert.False(menu.IsVisible);

        Click(toggle, window);
        Assert.True(bar.IsOpen);
        Assert.Equal(1, events);
        Assert.True(menu.IsVisible);
        Assert.Equal(1, Grid.GetRow(menu));
        var links = bar.GetRealizedContainers().Cast<NavbarLink>().ToList();
        Assert.True(links[1].TranslatePoint(default, bar)!.Value.Y > links[0].TranslatePoint(default, bar)!.Value.Y);
        AssertBrush(links[0], "InsToneBrandBg", () => links[0].Background);
        Assert.False(Part<Border>(links[0], "PART_Marker").IsVisible);

        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Settle();
        Assert.False(bar.IsOpen);

        Click(toggle, window);
        Assert.True(bar.IsOpen);
        Click(bar.GetRealizedContainers().Cast<NavbarLink>().ElementAt(2), window);
        Assert.False(bar.IsOpen);
        Assert.Equal(2, bar.SelectedIndex);
    }

    [AvaloniaFact]
    public void Navbar_toggle_turns_into_a_cross()
    {
        var bar = NewNavbar(380);
        bar.IsOpen = true;
        Show(bar);
        var toggle = Part<ToggleButton>(bar, "PART_Toggle");
        Assert.True(toggle.IsChecked);
        Assert.Equal(0, Part<Border>(toggle, "PART_Mid").Opacity, 2);
        var top = Part<Border>(toggle, "PART_Top").RenderTransform!.Value;
        Assert.Equal(Math.Cos(Math.PI / 4), top.M11, 2);
    }

    private static Split NewSplit(double width = 800) => new()
    {
        Width = width,
        Height = 200,
        First = new Border { Name = "One" },
        Second = new Border { Name = "Two" },
    };

    [AvaloniaFact]
    public void Split_shares_the_width_and_the_handle_sits_between()
    {
        var split = NewSplit();
        var window = Show(split, width: 900);
        var first = Part<ScrollViewer>(split, "PART_First");
        var handle = Part<Border>(split, "PART_Handle");
        Assert.Equal(400, first.Bounds.Width, 1);
        Assert.Equal(14, handle.Bounds.Width);
        Assert.Equal(386, Part<ScrollViewer>(split, "PART_Second").Bounds.Width, 1);
        var grip = Part<Border>(split, "PART_Grip");
        Assert.Equal(40, grip.Bounds.Height);
        AssertBrush(split, "InsCtlBd", () => grip.Background);
        window.MouseMove(Center(handle, window));
        Settle();
        Assert.Equal(64, grip.Bounds.Height, 1);
        AssertBrush(split, "InsToneBrandSoft", () => grip.Background);
    }

    [AvaloniaFact]
    public void Split_handle_drags_within_its_limits_and_reports_the_share()
    {
        var split = NewSplit();
        var window = Show(split, width: 900);
        var handle = Part<Border>(split, "PART_Handle");
        double? reported = null;
        split.ShareCommitted += (_, e) => reported = e.Share;

        var from = Center(handle, window);
        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(new Point(from.X + 160, from.Y));
        Settle(100);
        Assert.Contains(":dragging", split.Classes);
        Assert.Contains("dragging", handle.Classes);
        window.MouseUp(new Point(from.X + 160, from.Y), MouseButton.Left);
        Settle();
        var left = split.TranslatePoint(default, window)!.Value.X;
        Assert.Equal((from.X + 160 - left) / split.Bounds.Width * 100, split.Share, 1);
        Assert.Equal(split.Share, reported);
        Assert.DoesNotContain(":dragging", split.Classes);

        window.MouseDown(Center(handle, window), MouseButton.Left);
        window.MouseMove(new Point(790, from.Y));
        window.MouseUp(new Point(790, from.Y), MouseButton.Left);
        Settle();
        Assert.Equal(80, split.Share);
    }

    [AvaloniaFact]
    public void Split_keys_follow_the_arrows_in_rtl()
    {
        var split = NewSplit();
        var window = Show(split, FlowDirection.RightToLeft, width: 900);
        var handle = Part<Border>(split, "PART_Handle");
        handle.Focus(NavigationMethod.Tab);

        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
        Assert.Equal(52, split.Share);
        window.KeyPress(Key.Right, RawInputModifiers.Shift, PhysicalKey.ArrowRight, null);
        Assert.Equal(42, split.Share);
        window.KeyPress(Key.Home, RawInputModifiers.None, PhysicalKey.Home, null);
        Assert.Equal(20, split.Share);
        window.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        Assert.Equal(80, split.Share);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Assert.Equal(50, split.Share);

        var first = Part<ScrollViewer>(split, "PART_First");
        Assert.True(first.TranslatePoint(new Point(first.Bounds.Width / 2, 0), window)!.Value.X > 450, "the first pane is on the right in RTL");
        Settle();
        Assert.Equal(Res(split, "InsFocusRing"), Part<Border>(split, "PART_Grip").BoxShadow);
    }

    [AvaloniaFact]
    public void Split_keys_in_ltr_and_the_limits()
    {
        var split = NewSplit();
        split.Min = 60;
        split.Max = 75;
        var window = Show(split, width: 900);
        Assert.Equal(60, split.Share);
        Part<Border>(split, "PART_Handle").Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.Equal(62, split.Share);
        window.KeyPress(Key.Right, RawInputModifiers.Shift, PhysicalKey.ArrowRight, null);
        Assert.Equal(72, split.Share);
        window.KeyPress(Key.Right, RawInputModifiers.Shift, PhysicalKey.ArrowRight, null);
        Assert.Equal(75, split.Share);
    }

    [AvaloniaFact]
    public void Split_stacks_when_narrow_and_turns_for_vertical()
    {
        var split = NewSplit(500);
        Show(split, width: 600);
        Assert.True(split.IsStacked);
        Assert.False(Part<Border>(split, "PART_Handle").IsVisible);
        Assert.Equal(1, Grid.GetRow(Part<ScrollViewer>(split, "PART_Second")));

        var v = NewSplit();
        v.Orientation = Orientation.Vertical;
        v.Height = 300;
        var window = Show(v, width: 900, height: 400);
        var handle = Part<Border>(v, "PART_Handle");
        Assert.Equal(14, handle.Bounds.Height);
        Assert.Equal(150, Part<ScrollViewer>(v, "PART_First").Bounds.Height, 1);
        Assert.Equal(136, Part<ScrollViewer>(v, "PART_Second").Bounds.Height, 1);
        Assert.Equal(40, Part<Border>(v, "PART_Grip").Bounds.Width);
        handle.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Assert.Equal(52, v.Share);
    }

    [AvaloniaFact]
    public void Split_panes_keep_a_gutter_and_the_handle_is_named()
    {
        var split = NewSplit();
        Show(split, width: 900);
        Assert.Equal(new Thickness(5, 0), Part<ScrollViewer>(split, "PART_First").Padding);
        Assert.Equal(new Thickness(5, 0), Part<ScrollViewer>(split, "PART_Second").Padding);
        Assert.Equal("تغيير حجم اللوحين", AutomationProperties.GetName(Part<Border>(split, "PART_Handle")));
    }

    [AvaloniaFact]
    public void Grid_splitter_looks_like_the_split_handle()
    {
        var splitter = new GridSplitter();
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*"), Height = 200, Children = { new Border(), splitter, new Border() } };
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(grid.Children[2], 2);
        Show(grid);
        Assert.Equal(14, splitter.Bounds.Width);
        var grip = Part<Border>(splitter, "PART_Grip");
        Assert.Equal(40, grip.Bounds.Height);
        AssertBrush(splitter, "InsCtlBd", () => grip.Background);
    }

    [Fact]
    public void Pagination_window_matches_the_web()
    {
        Assert.Equal([1, 2, 3, 4, 0, 12], Pagination.Window(3, 12));
        Assert.Equal([1, 2, 3, 0, 12], Pagination.Window(1, 12));
        Assert.Equal([1, 0, 10, 11, 12], Pagination.Window(12, 12));
        Assert.Equal([1, 0, 5, 6, 7, 0, 12], Pagination.Window(6, 12));
        Assert.Equal([1, 2, 3, 4, 5], Pagination.Window(2, 5));
    }

    [AvaloniaFact]
    public void Pagination_builds_pages_and_moves_with_its_buttons()
    {
        var pages = new Pagination { PageCount = 12, CurrentPage = 3 };
        var window = Show(pages, width: 800);
        var buttons = pages.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(["السابق", "1", "2", "3", "4", "12", "التالي"], buttons.Select(b => (string)b.Content!));
        var current = buttons[3];
        Assert.Contains("current", current.Classes);
        AssertBrush(current, "InsSegOnBg", () => current.Background);
        Assert.Equal(Res(current, "InsSegOnShadow"), Part<Border>(current, "PART_Face").BoxShadow);
        Assert.Equal(34, buttons[1].Bounds.Height);
        Assert.True(buttons[1].Bounds.Width >= 34);
        AssertBrush(buttons[1], "InsLabel", () => buttons[1].Foreground);
        Assert.True(Part<Path>(buttons[0], "PART_PrevArrow").IsVisible);
        Assert.True(Part<Path>(buttons[^1], "PART_NextArrow").IsVisible);
        AssertBrush(pages, "InsCtlBgHover", () => Part<Border>(pages, "PART_Track").Background);
        var changed = 0;
        pages.PageChanged += (_, _) => changed++;
        Click(buttons[^1], window);
        Assert.Equal(4, pages.CurrentPage);
        Assert.Equal(1, changed);
        Click(pages.GetVisualDescendants().OfType<Button>().First(b => (string)b.Content! == "12"), window);
        Assert.Equal(12, pages.CurrentPage);
        Assert.False(pages.GetVisualDescendants().OfType<Button>().Last().IsEnabled);
        Assert.Equal(0.4, pages.GetVisualDescendants().OfType<Button>().Last().Opacity, 2);
    }

    [AvaloniaFact]
    public void Pagination_first_page_disables_previous_and_compact_shows_the_position()
    {
        var first = new Pagination { PageCount = 12, CurrentPage = 1 };
        var compact = new Pagination { PageCount = 12, CurrentPage = 3, IsCompact = true };
        Show(new StackPanel { Children = { first, compact } }, width: 800);
        Assert.False(first.GetVisualDescendants().OfType<Button>().First().IsEnabled);
        var gap = compact.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("page-gap"));
        Assert.Equal("3 من 12", gap.Text);
        Assert.Equal(2, compact.GetVisualDescendants().OfType<Button>().Count());
    }

    [AvaloniaFact]
    public void Pagination_arrows_point_toward_start_and_end_in_rtl()
    {
        var pages = new Pagination { PageCount = 12, CurrentPage = 3 };
        var window = Show(pages, FlowDirection.RightToLeft, width: 800);
        var buttons = pages.GetVisualDescendants().OfType<Button>().ToList();
        Assert.True(buttons[0].TranslatePoint(default, window)!.Value.X > buttons[^1].TranslatePoint(default, window)!.Value.X, "previous is on the right in RTL");
        var arrow = Part<Path>(buttons[0], "PART_PrevArrow");
        var tip = arrow.TranslatePoint(new Point(1.5, 5), window)!.Value.X;
        var tail = arrow.TranslatePoint(new Point(5.5, 1), window)!.Value.X;
        Assert.True(tip > tail, "previous points right in RTL");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Shell_pieces_render_in_every_look(string theme, string dir)
    {
        var shell = NewShell(out _, out _, out _);
        var content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new Border { Height = 300, Child = shell },
                new PageHead { Title = "عنوان الصفحة", Subtitle = "سطر وصف قصير", Breadcrumb = new Breadcrumb { ItemsSource = new[] { "الرئيسية", "القسم", "الصفحة" } }, Actions = new Button { Classes = { "primary" }, Content = "حفظ" } },
                NewNavbar(760),
                new Split { Height = 120, First = new TextBlock { Text = "اللوح الأول" }, Second = new TextBlock { Text = "اللوح الثاني" } },
                new Pagination { PageCount = 12, CurrentPage = 3 },
            },
        };
        var window = Show(content, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, width: 880, height: 900);
        Capture(window, $"shell-{theme}-{dir}");
        Assert.True(Shell.IsDrawer(shell));
    }
}
