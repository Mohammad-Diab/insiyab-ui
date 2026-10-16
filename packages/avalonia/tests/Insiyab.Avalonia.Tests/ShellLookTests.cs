using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The shell's looks by class: the top bar, the sidebar and the screen in every state, pictured and dumped for the before/after comparison, runtime class changes, overrides, and which elements take a theme.
public class ShellLookTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");

    private static readonly string[] BorderClasses = ["topbar", "island", "shell-mark", "shell-brand", "shell-indicator", "shell-link-badge", "shell-bottom", "shell-content"];

    private static readonly string[] TextClasses = ["shell-title", "shell-sub", "shell-brand-name", "shell-brand-role", "shell-group", "shell-filter-empty", "brand-name", "brand-sub"];

    private static readonly string[] StackClasses = ["zone", "shell-head", "brand"];

    private static T Cls<T>(T element, string classes) where T : StyledElement
    {
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) element.Classes.Add(c);
        return element;
    }

    private static TextBlock Text(string text, string classes = "") => Cls(new TextBlock { Text = text }, classes);

    private static Border Mark(string text = "م") => Cls(new Border { Child = new TextBlock { Text = text } }, "shell-mark");

    private static Border LogoMark()
    {
        var logo = new TemplatedControl
        {
            Template = new FuncControlTemplate<TemplatedControl>((_, _) => new Viewbox
            {
                FlowDirection = FlowDirection.LeftToRight,
                Child = new Canvas { Width = 224, Height = 224, Children = { new Border { Width = 224, Height = 224, Background = Brushes.White, CornerRadius = new CornerRadius(60) } } },
            }),
        };
        return Cls(new Border { Child = logo }, "shell-mark");
    }

    private static Border Dot(double size) => new() { Width = size, Height = size, Background = Brushes.Gray };

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Rig BarOfBorders()
    {
        var menu = Cls(new Button { Content = new Icon { Data = Glyph } }, "island icon");
        var brand = Cls(new Border { Child = new StackPanel { Children = { Mark(), Text("تطبيقي") } } }, "island brand");
        var logo = Cls(new Border { Child = new StackPanel { Children = { LogoMark(), Text("شعار") } } }, "island brand");
        var version = Cls(new Border { Child = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { Text("الإصدار"), Text("0.1.0") } } }, "island");
        var start = Cls(new StackPanel { Children = { menu, brand, logo, version } }, "zone start");
        var heading = Text("العنوان", "shell-title");
        var sub = Text("سطر فرعي", "shell-sub");
        var title = Cls(new Border { Child = Cls(new StackPanel { Children = { heading, sub } }, "shell-head") }, "island title");
        var center = Cls(new StackPanel { Children = { title } }, "zone center");
        var draft = Cls(new Border { Child = Text("مسودة") }, "island");
        var custom = Cls(new Button { Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { Dot(24), Text("المظهر") } } }, "island");
        var end = Cls(new StackPanel { Children = { draft, custom } }, "zone end");
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*"), Children = { start, center, end } };
        return new Rig(Cls(new Border { Child = grid }, "topbar"), grid, start, brand, logo, title, heading, sub, custom, draft);
    }

    private static Rig BarOfControls()
    {
        var brandContent = Cls(new ContentControl { Content = new StackPanel { Children = { Mark(), Text("محتوى") } } }, "island brand");
        var brandButton = Cls(new Button { Content = new StackPanel { Children = { Mark("ب"), Text("زر") } } }, "island brand");
        var start = Cls(new StackPanel { Children = { brandContent, brandButton } }, "zone start");
        var titleContent = Cls(new ContentControl { Content = Text("عنوان محتوى") }, "island title");
        var titleButton = Cls(new Button { Content = Text("عنوان زر") }, "island title");
        var center = Cls(new StackPanel { Children = { titleContent, titleButton } }, "zone center");
        var row = Cls(new ContentControl { Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { Dot(18), Text("محتوى") } } }, "island");
        var icon = Cls(new Button { Content = new Icon { Data = Glyph } }, "island icon");
        var plain = new Button { Content = "إجراء" };
        var end = Cls(new StackPanel { Children = { row, icon, new Pill { Content = "مباشر" }, plain } }, "zone end");
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*"), Children = { start, center, end } };
        return new Rig(Cls(new Border { Child = grid }, "topbar"), grid, start, brandContent, brandButton, titleContent, null, null, row, null);
    }

    private sealed record Rig(Border Bar, Grid Grid, StackPanel Start, Control Brand, Control Brand2, Control Title, TextBlock? Heading, TextBlock? Sub, Control Island, Control? Island2);

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Topbar_and_islands_keep_their_look(string theme, string dir)
    {
        var one = BarOfBorders();
        var two = BarOfControls();
        var split = new SplitView { IsPaneOpen = true, Pane = new Border { Child = Text("الشريط") }, Content = new StackPanel { Children = { one.Bar, two.Bar } } };
        var w = Show(split, Dir(dir), Variant(theme), 1200, 460);
        LookCheck.Keep(w, split, $"shell-topbar-rest-{theme}-{dir}");
        Assert.Equal(new Thickness(16, 12), one.Bar.Padding);
        Assert.Equal(Orientation.Horizontal, one.Start.Orientation);
        Assert.Equal(8, one.Start.Spacing);
        Assert.Equal(10, one.Grid.ColumnSpacing);
        Assert.False(one.Brand.IsVisible);
        Assert.False(one.Brand2.IsVisible);
        Assert.False(two.Brand.IsVisible);
        Assert.False(two.Brand2.IsVisible);
        Assert.Equal(new Thickness(20, 0), ((Border)one.Title).Padding);
        Assert.Equal(416, ((Border)one.Title).MaxWidth);
        Assert.Equal(VerticalAlignment.Center, ((Control)((Border)one.Title).Child!).VerticalAlignment);

        w.MouseMove(Center(one.Island, w));
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-hover-{theme}-{dir}");
        Assert.Equal(-1, one.Island.RenderTransform!.Value.M32, 1);
        w.MouseMove(new Point(1, 1));

        one.Bar.Classes.Add("scrolled");
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-scrolled-{theme}-{dir}");
        AssertBrush(one.Bar, "InsGlassGround", () => one.Bar.Background);
        one.Bar.Classes.Remove("scrolled");

        split.IsPaneOpen = false;
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-closed-{theme}-{dir}");
        Assert.True(one.Brand.IsVisible);
        Assert.True(one.Brand2.IsVisible);
        Assert.True(two.Brand.IsVisible);
        Assert.True(two.Brand2.IsVisible);
        Assert.Equal(28, ((Border)((Panel)((Border)one.Brand).Child!).Children[0]).Width);

        w.MouseMove(Center(two.Brand2, w));
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-closed-hover-{theme}-{dir}");
        Motion.SetStill(w, true);
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-closed-still-{theme}-{dir}");
        Motion.SetStill(w, false);
        w.MouseMove(new Point(1, 1));

        w.SetValue(WindowActivity.IsInactiveProperty, true);
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-closed-inactive-{theme}-{dir}");
        AssertBrush(one.Brand, "InsText3", () => one.Brand.GetValue(TextElement.ForegroundProperty));
        AssertBrush(one.Heading!, "InsText3", () => one.Heading!.Foreground);
        w.ClearValue(WindowActivity.IsInactiveProperty);

        split.DisplayMode = SplitViewDisplayMode.Overlay;
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-overlay-{theme}-{dir}");
        Assert.True(one.Brand.IsVisible);
        split.IsPaneOpen = true;
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-overlay-open-{theme}-{dir}");
        Assert.True(one.Brand.IsVisible);
        split.IsPaneOpen = false;
        split.DisplayMode = SplitViewDisplayMode.Inline;

        split.Classes.Add("narrow");
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-narrow-{theme}-{dir}");
        Assert.False(one.Brand.IsVisible);
        Assert.False(two.Brand2.IsVisible);
        Assert.Equal(new Thickness(12, 10), one.Bar.Padding);
        Assert.Equal(6, one.Grid.ColumnSpacing);
        Assert.Equal(new Thickness(14, 0), ((Border)one.Title).Padding);
        Assert.Equal(double.PositiveInfinity, ((Border)one.Title).MaxWidth);
        Assert.True(one.Sub!.IsVisible);
        split.Classes.Add("xnarrow");
        Settle();
        LookCheck.Keep(w, split, $"shell-topbar-xnarrow-{theme}-{dir}");
        Assert.False(one.Sub.IsVisible);
        split.Classes.Remove("xnarrow");
        split.Classes.Remove("narrow");
        Settle();
        Assert.True(one.Sub.IsVisible);
        Assert.True(one.Brand.IsVisible);
        Assert.Equal(new Thickness(16, 12), one.Bar.Padding);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Sidebar_keeps_its_look(string theme, string dir)
    {
        var label = Cls(new Border { Child = new StackPanel { Children = { Mark(), new StackPanel { Children = { Text("تطبيقي", "shell-brand-name"), Text("الإدارة", "shell-brand-role") } } } } }, "shell-brand");
        DockPanel.SetDock(label, Dock.Top);
        var link = Cls(new Button { Content = new StackPanel { Children = { Mark("ر"), new StackPanel { Children = { Text("رابط الرئيسية", "shell-brand-name"), Text("الصفحة الأولى", "shell-brand-role") } } } } }, "shell-brand");
        DockPanel.SetDock(link, Dock.Top);
        var badge = Cls(new Border { Child = new TextBlock { Text = "8" } }, "shell-link-badge");
        DockPanel.SetDock(badge, Dock.Right);
        var nav = Cls(new ListBox(), "shell-nav");
        nav.Items.Add(Cls(new ListBoxItem { Content = "القسم" }, "shell-group"));
        nav.Items.Add(new ListBoxItem { Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new Icon { Data = Glyph }, Text("الرئيسية") } } });
        nav.Items.Add(new ListBoxItem { Content = new DockPanel { Children = { new Icon { Data = Glyph }, badge, Text("الطلبات") } } });
        nav.SelectedIndex = 2;
        Shell.SetHasFilter(nav, true);
        var bottom = Cls(new Border { Child = Cls(new Button { Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new Icon { Data = Glyph }, Text("خروج") } } }, "shell-link") }, "shell-bottom");
        DockPanel.SetDock(bottom, Dock.Bottom);
        var pane = new DockPanel { Children = { label, link, bottom, nav } };
        var indicator = Cls(new Border { Width = 3, Height = 24, HorizontalAlignment = HorizontalAlignment.Left }, "shell-indicator");
        var content = Cls(new Border { Child = new StackPanel { Spacing = 8, Children = { Text("المحتوى"), indicator, Text("مجموعة", "shell-group") } } }, "shell-content");
        var split = new SplitView { IsPaneOpen = true, Pane = pane, Content = content };
        var w = Show(split, Dir(dir), Variant(theme), 1100, 640);
        var empty = Part<TextBlock>(nav, "PART_FilterEmpty");
        empty.IsVisible = true;
        LookCheck.Keep(w, split, $"shell-sidebar-rest-{theme}-{dir}");
        Assert.Equal(new Thickness(6, 0, 6, 16), label.Padding);
        Assert.Equal(new Thickness(0, 0, 0, 1), link.BorderThickness);
        Assert.Equal(34, Assert.IsType<Border>(((Panel)label.Child!).Children[0]).Bounds.Width);
        Assert.Equal(new Thickness(26, 8), empty.Margin);
        Assert.Equal(new Thickness(28, 8, 28, 32), content.Padding);
        Assert.Equal(20, badge.MinWidth);

        var mark = (Border)((Panel)link.Content!).Children[0];
        w.MouseMove(Center(link, w));
        Settle();
        LookCheck.Keep(w, split, $"shell-sidebar-hover-{theme}-{dir}");
        Assert.Equal(1.06 * Math.Cos(4 * Math.PI / 180), mark.RenderTransform!.Value.M11, 2);
        Assert.Equal(1, ((Border)((Panel)label.Child!).Children[0]).RenderTransform!.Value.M11, 2);

        Motion.SetStill(w, true);
        Settle();
        LookCheck.Keep(w, split, $"shell-sidebar-still-{theme}-{dir}");
        Assert.Equal(1, mark.RenderTransform!.Value.M11, 2);
        Motion.SetStill(w, false);
        w.MouseMove(new Point(1, 1));

        split.DisplayMode = SplitViewDisplayMode.Overlay;
        Settle();
        LookCheck.Keep(w, split, $"shell-sidebar-overlay-{theme}-{dir}");
        Assert.Equal(new Thickness(16, 8, 16, 24), content.Padding);
        split.IsPaneOpen = false;
        Settle();
        LookCheck.Keep(w, split, $"shell-sidebar-overlay-closed-{theme}-{dir}");
        split.DisplayMode = SplitViewDisplayMode.Inline;
        Settle();
        LookCheck.Keep(w, split, $"shell-sidebar-closed-{theme}-{dir}");
        Assert.Equal(new Thickness(28, 8, 28, 32), content.Padding);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Screens_keep_their_look(string theme, string dir)
    {
        static StackPanel Brand(Control mark) => Cls(new StackPanel { Children = { mark, Text("متجري", "brand-name"), Text("تسجيل الدخول", "brand-sub") } }, "brand");
        var brandA = Brand(Mark());
        var markB = LogoMark();
        var brandB = Brand(markB);
        var cardA = Cls(new Border { Width = 448, Padding = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = new StackPanel { Children = { brandA, Text("نموذج") } } }, "glass");
        var cardB = Cls(new Border { Padding = new Thickness(20), Child = new StackPanel { Children = { brandB, Text("نموذج") } } }, "card");
        var screenA = Cls(new Panel { MinHeight = 340, Children = { cardA } }, "screen");
        var screenB = Cls(new Panel { MinHeight = 340, Children = { cardB } }, "screen");
        var page = new StackPanel { Children = { screenA, screenB } };
        var w = Show(page, Dir(dir), Variant(theme), 900, 720);
        LookCheck.Keep(w, page, $"shell-screen-{theme}-{dir}");
        Assert.Equal(448, cardB.MaxWidth);
        Assert.Equal(new Thickness(32), cardB.Margin);
        Assert.Equal(HorizontalAlignment.Stretch, cardB.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, cardB.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, cardA.HorizontalAlignment);
        Assert.Equal(6, brandA.Spacing);
        Assert.Equal(new Thickness(0, 0, 0, 24), brandA.Margin);
        var markA = (Border)brandA.Children[0];
        Assert.Equal(60, markA.Width);
        Assert.Equal(60, markA.Height);
        Assert.Equal(new Thickness(0, 0, 0, 8), markA.Margin);
        Assert.Equal(26, markA.GetValue(TextElement.FontSizeProperty));
        Assert.Equal(HorizontalAlignment.Center, markA.HorizontalAlignment);
        Assert.Equal(24, ((TextBlock)brandA.Children[1]).FontSize);
        Assert.Equal(HorizontalAlignment.Center, ((TextBlock)brandA.Children[2]).HorizontalAlignment);

        var narrowCard = Cls(new Border { Padding = new Thickness(20), Child = new StackPanel { Children = { Brand(Mark("ن")), Text("نموذج") } } }, "glass");
        var narrow = new ScrollViewer { Content = Cls(new Panel { MinHeight = 800, Children = { narrowCard } }, "screen") };
        var w2 = Show(narrow, Dir(dir), Variant(theme), 360, 640);
        LookCheck.Keep(w2, narrow, $"shell-screen-narrow-{theme}-{dir}");
        Assert.Equal(new Thickness(32), narrowCard.Margin);
        Assert.True(narrowCard.Bounds.Width <= 296.5, $"card is {narrowCard.Bounds.Width} wide");
    }

    [AvaloniaFact]
    public void Shell_classes_added_at_runtime_take_their_look()
    {
        var zone = new StackPanel { Children = { new Border(), new Border() } };
        var island = new Border { Child = new TextBlock { Text = "جزيرة" } };
        var mark = new Border { Child = new TextBlock { Text = "م" } };
        var title = new TextBlock { Text = "العنوان" };
        var holder = new Panel { Children = { new Border { Child = new TextBlock { Text = "بطاقة" } } } };
        var brand = new StackPanel { Children = { new Border(), new TextBlock { Text = "اسم" } } };
        Show(new StackPanel { Children = { zone, island, mark, title, holder, brand } }, FlowDirection.RightToLeft, height: 700);
        var markText = (TextBlock)mark.Child!;
        var islandText = (TextBlock)island.Child!;
        var card = (Border)holder.Children[0];
        Assert.Equal(Orientation.Vertical, zone.Orientation);
        Assert.Equal(0, zone.Spacing);
        zone.Classes.Add("zone");
        Settle();
        Assert.Equal(Orientation.Horizontal, zone.Orientation);
        Assert.Equal(8, zone.Spacing);
        Assert.Equal(VerticalAlignment.Center, zone.VerticalAlignment);
        zone.Classes.Add("center");
        Settle();
        Assert.Equal(HorizontalAlignment.Center, zone.HorizontalAlignment);
        Assert.Equal(1, Grid.GetColumn(zone));
        zone.Classes.Remove("center");
        zone.Classes.Add("end");
        Settle();
        Assert.Equal(HorizontalAlignment.Right, zone.HorizontalAlignment);
        Assert.Equal(2, Grid.GetColumn(zone));
        zone.Classes.Remove("end");
        zone.Classes.Remove("zone");
        Settle();
        Assert.Equal(Orientation.Vertical, zone.Orientation);
        Assert.Equal(HorizontalAlignment.Stretch, zone.HorizontalAlignment);
        Assert.Equal(0, Grid.GetColumn(zone));

        Assert.Equal(new Thickness(0), island.Padding);
        island.Classes.Add("island");
        Settle();
        Assert.Equal(new Thickness(14, 0), island.Padding);
        Assert.Equal(new Thickness(1), island.BorderThickness);
        Assert.Equal(Res(island, "InsAppbarH"), island.Height);
        Assert.Equal(VerticalAlignment.Center, islandText.VerticalAlignment);
        island.Classes.Add("title");
        Settle();
        Assert.Equal(new Thickness(20, 0), island.Padding);
        Assert.Equal(416, island.MaxWidth);
        island.Classes.Remove("title");
        Settle();
        Assert.Equal(new Thickness(14, 0), island.Padding);
        Assert.Equal(double.PositiveInfinity, island.MaxWidth);
        island.Classes.Add("brand");
        Settle();
        Assert.False(island.IsVisible);
        Assert.Equal(new Thickness(16, 0), island.Padding);
        island.Classes.Remove("brand");
        Settle();
        Assert.True(island.IsVisible);
        island.Classes.Remove("island");
        Settle();
        Assert.Equal(new Thickness(0), island.Padding);
        Assert.Equal(VerticalAlignment.Stretch, islandText.VerticalAlignment);

        Assert.True(double.IsNaN(mark.Width));
        mark.Classes.Add("shell-mark");
        Settle();
        Assert.Equal(34, mark.Width);
        Assert.Equal(FlowDirection.LeftToRight, mark.FlowDirection);
        Assert.Equal(HorizontalAlignment.Center, markText.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, markText.VerticalAlignment);
        mark.Classes.Remove("shell-mark");
        Settle();
        Assert.True(double.IsNaN(mark.Width));
        Assert.Equal(HorizontalAlignment.Stretch, markText.HorizontalAlignment);

        var plainSize = title.FontSize;
        title.Classes.Add("shell-title");
        Settle();
        Assert.Equal(15, title.FontSize);
        Assert.Equal(TextTrimming.CharacterEllipsis, title.TextTrimming);
        title.Classes.Remove("shell-title");
        Settle();
        Assert.Equal(plainSize, title.FontSize);

        Assert.Equal(new Thickness(0), card.Margin);
        holder.Classes.Add("screen");
        Settle();
        Assert.Equal(new Thickness(32), card.Margin);
        Assert.Equal(448, card.MaxWidth);
        holder.Classes.Remove("screen");
        Settle();
        Assert.Equal(new Thickness(0), card.Margin);
        Assert.Equal(double.PositiveInfinity, card.MaxWidth);

        brand.Classes.Add("brand");
        Settle();
        Assert.Equal(6, brand.Spacing);
        Assert.Equal(HorizontalAlignment.Center, ((TextBlock)brand.Children[1]).HorizontalAlignment);
        brand.Classes.Remove("brand");
        Settle();
        Assert.Equal(0, brand.Spacing);
        Assert.Equal(HorizontalAlignment.Stretch, ((TextBlock)brand.Children[1]).HorizontalAlignment);
    }

    [AvaloniaFact]
    public void Split_view_states_reach_the_parts_at_runtime()
    {
        var bar = Cls(new Border { Child = new Grid { Children = { Cls(new StackPanel { Children = { Cls(new Border { Child = Text("هوية") }, "island brand") } }, "zone") } } }, "topbar");
        var sub = Text("سطر فرعي", "shell-sub");
        var empty = Text("لا شيء", "shell-filter-empty");
        var content = Cls(new Border { Child = new StackPanel { Children = { sub, empty } } }, "shell-content");
        var outside = Text("خارج", "shell-filter-empty");
        var outsideSub = Text("خارج", "shell-sub");
        var split = new SplitView { IsPaneOpen = true, Pane = Text("القائمة"), Content = new StackPanel { Children = { bar, content } } };
        var below = new StackPanel { Children = { outside, outsideSub } };
        Grid.SetRow(below, 1);
        Show(new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { split, below } }, FlowDirection.RightToLeft, width: 1200, height: 700);
        var island = (Border)((Panel)((Panel)bar.Child!).Children[0]).Children[0];
        Assert.False(island.IsVisible);
        Assert.Equal(new Thickness(26, 8), empty.Margin);
        Assert.Equal(new Thickness(14, 8), outside.Margin);
        Assert.Equal(new Thickness(28, 8, 28, 32), content.Padding);
        split.IsPaneOpen = false;
        Settle();
        Assert.True(island.IsVisible);
        split.DisplayMode = SplitViewDisplayMode.Overlay;
        Settle();
        Assert.True(island.IsVisible);
        Assert.Equal(new Thickness(16, 8, 16, 24), content.Padding);
        split.IsPaneOpen = true;
        Settle();
        Assert.True(island.IsVisible);
        split.IsPaneOpen = false;
        split.DisplayMode = SplitViewDisplayMode.Inline;
        split.IsPaneOpen = true;
        Settle();
        Assert.False(island.IsVisible);
        split.IsPaneOpen = false;
        Settle();
        Assert.True(island.IsVisible);
        split.Classes.Add("narrow");
        Settle();
        Assert.False(island.IsVisible);
        Assert.Equal(new Thickness(12, 10), bar.Padding);
        Assert.True(sub.IsVisible);
        split.Classes.Add("xnarrow");
        Settle();
        Assert.False(sub.IsVisible);
        Assert.True(outsideSub.IsVisible);
        split.Classes.Remove("xnarrow");
        split.Classes.Remove("narrow");
        Settle();
        Assert.True(sub.IsVisible);
        Assert.True(island.IsVisible);
        Assert.Equal(new Thickness(16, 12), bar.Padding);
    }

    [AvaloniaFact]
    public void Marks_and_islands_let_go_of_a_child_that_leaves()
    {
        var mark = Cls(new Border { Child = new TextBlock { Text = "م" } }, "shell-mark");
        var island = Cls(new Border { Child = new TextBlock { Text = "جزيرة" } }, "island");
        var badge = Cls(new Border { Child = new TextBlock { Text = "8" } }, "shell-link-badge");
        var logo = LogoMark();
        Show(new StackPanel { Children = { mark, island, badge, logo } }, FlowDirection.RightToLeft, height: 500);
        Assert.Equal(HorizontalAlignment.Stretch, ((Control)logo.Child!).HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, ((Control)logo.Child!).VerticalAlignment);
        var markText = (TextBlock)mark.Child!;
        var islandText = (TextBlock)island.Child!;
        var badgeText = (TextBlock)badge.Child!;
        Assert.Equal(HorizontalAlignment.Center, markText.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, islandText.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, badgeText.HorizontalAlignment);
        Assert.Equal(18, badgeText.LineHeight);
        mark.Child = null;
        island.Child = null;
        badge.Child = null;
        Assert.Equal(HorizontalAlignment.Stretch, markText.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, islandText.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Stretch, badgeText.HorizontalAlignment);
        Assert.True(double.IsNaN(badgeText.LineHeight));
        var next = new TextBlock { Text = "ن" };
        mark.Child = next;
        Settle();
        Assert.Equal(HorizontalAlignment.Center, next.HorizontalAlignment);
        next.HorizontalAlignment = HorizontalAlignment.Left;
        Assert.Equal(HorizontalAlignment.Left, next.HorizontalAlignment);
        mark.Classes.Remove("shell-mark");
        Settle();
        Assert.Equal(VerticalAlignment.Stretch, next.VerticalAlignment);
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_shell_looks()
    {
        var bar = Cls(new Border { Child = new Grid() }, "topbar");
        var zone = Cls(new StackPanel(), "zone");
        var island = Cls(new Border { Child = new TextBlock { Text = "جزيرة" } }, "island");
        var mark = Mark();
        var local = Mark("ل");
        var title = Text("العنوان", "shell-title");
        var sub = Text("سطر", "shell-sub");
        var page = new StackPanel { Spacing = 12, Children = { bar, zone, island, mark, local, title, sub } };
        page.Styles.Add(App<Border>("topbar", Border.PaddingProperty, new Thickness(4)));
        page.Styles.Add(App<StackPanel>("zone", StackPanel.SpacingProperty, 3d));
        page.Styles.Add(App<Border>("shell-mark", Border.WidthProperty, 50d));
        page.Styles.Add(App<TextBlock>("shell-title", TextBlock.ForegroundProperty, Brushes.Red));
        var appStyle = App<TextBlock>("shell-sub", TextBlock.FontSizeProperty, 30d);
        var typeOnly = new Style(x => x.OfType<Border>()) { Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(9)) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(typeOnly);
        try
        {
            local.Width = 70;
            Show(page, FlowDirection.RightToLeft, height: 700);
            Assert.Equal(new Thickness(4), bar.Padding);
            Assert.Equal(3, zone.Spacing);
            Assert.Equal(Orientation.Horizontal, zone.Orientation);
            Assert.Equal(50, mark.Width);
            Assert.Equal(70, local.Width);
            Assert.True(Near(Colors.Red, ColorOf(title.Foreground)));
            Assert.Equal(30, sub.FontSize);
            Assert.NotEqual(new CornerRadius(9), island.CornerRadius);
            Assert.Equal(Res(island, "InsRPill"), island.CornerRadius);
            Assert.NotEqual(new CornerRadius(9), mark.CornerRadius);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(typeOnly);
        }
        Settle();
        Assert.Equal(11, sub.FontSize);
        Assert.Equal(new Thickness(4), bar.Padding);
    }

    private static bool Near(Color a, Color b) =>
        Math.Abs(a.A - b.A) <= 6 && Math.Abs(a.R - b.R) <= 6 && Math.Abs(a.G - b.G) <= 6 && Math.Abs(a.B - b.B) <= 6;

    [AvaloniaFact]
    public void Only_the_shell_parts_take_a_theme()
    {
        Assert.False(Application.Current!.TryFindResource(typeof(Border), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(TextBlock), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(StackPanel), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(Grid), out _));
        var plain = new Border { Child = new TextBlock { Text = "أ" } };
        var plainStack = new StackPanel();
        var plainGrid = new Grid();
        var plainPanel = new Panel();
        var modifiers = new Border { Classes = { "title", "brand", "scrolled" } };
        var borders = BorderClasses.Select(c => Cls(new Border(), c)).ToList();
        var texts = TextClasses.Select(c => Cls(new TextBlock { Text = c }, c)).ToList();
        var stacks = StackClasses.Select(c => Cls(new StackPanel(), c)).ToList();
        var screen = Cls(new Panel(), "screen");
        var island = Cls(new Border(), "island brand");
        var page = new StackPanel { Children = { plain, plainStack, plainGrid, plainPanel, modifiers, screen, island } };
        foreach (var b in borders) page.Children.Add(b);
        foreach (var t in texts) page.Children.Add(t);
        foreach (var s in stacks) page.Children.Add(s);
        var bar = Cls(new Border { Child = new Grid() }, "topbar");
        var islandRow = Cls(new Border { Child = new StackPanel { Children = { new Border() } } }, "island");
        var brandRow = Cls(new Border { Child = new StackPanel { Children = { new Border() } } }, "shell-brand");
        var strayGrid = new Border { Child = new Grid() };
        page.Children.Add(bar);
        page.Children.Add(islandRow);
        page.Children.Add(brandRow);
        page.Children.Add(strayGrid);
        Show(page, FlowDirection.RightToLeft, height: 900);
        Assert.Null(plain.Theme);
        Assert.Null(((TextBlock)plain.Child!).Theme);
        Assert.Null(plainStack.Theme);
        Assert.Null(plainGrid.Theme);
        Assert.Null(plainPanel.Theme);
        Assert.Null(modifiers.Theme);
        Assert.All(borders, b => Assert.NotNull(b.Theme));
        Assert.All(texts, t => Assert.NotNull(t.Theme));
        Assert.All(stacks, s => Assert.NotNull(s.Theme));
        Assert.NotNull(screen.Theme);
        Assert.NotNull(island.Theme);
        Assert.NotNull(((Grid)bar.Child!).Theme);
        Assert.NotNull(((StackPanel)islandRow.Child!).Theme);
        Assert.NotNull(((StackPanel)brandRow.Child!).Theme);
        Assert.Null(((Grid)strayGrid.Child!).Theme);
    }

    [AvaloniaFact]
    public void No_global_rule_names_shell_classes()
    {
        var found = new List<string>();
        foreach (var cls in BorderClasses.Concat(["title", "brand", "scrolled"]))
            found.AddRange(LookCheck.SubjectRules(typeof(Border), cls, "ins-still", "IsInactive"));
        foreach (var cls in TextClasses)
            found.AddRange(LookCheck.SubjectRules(typeof(TextBlock), cls, "ins-still", "IsInactive"));
        foreach (var cls in StackClasses.Concat(["start", "center", "end"]))
            found.AddRange(LookCheck.SubjectRules(typeof(StackPanel), cls, "ins-still", "IsInactive"));
        found.AddRange(LookCheck.GlobalSelectors()
            .SelectMany(s => s.Split(", ").Select(alt => alt.Trim()))
            .Where(alt => !alt.Contains("ins-still") && !alt.Contains("IsInactive") && Regex.IsMatch(alt, @"(?<![\w|])(Border\.(topbar|island|shell-brand|shell-mark|shell-link-badge)|Panel\.screen|StackPanel\.brand)(?![\w-])") && !alt.Contains("ins|Pill"))
            .Distinct());
        found = found.Distinct().ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
