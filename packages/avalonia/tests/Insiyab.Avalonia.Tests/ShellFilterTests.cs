using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ShellFilterTests
{
    private static ListBoxItem Link(string text, string? keywords = null, string? badge = null)
    {
        object content = text;
        if (badge is not null)
        {
            var row = new DockPanel();
            var b = new Border { Classes = { "shell-link-badge" }, Child = new TextBlock { Text = badge } };
            DockPanel.SetDock(b, Dock.Right);
            row.Children.Add(b);
            row.Children.Add(new TextBlock { Text = text });
            content = row;
        }
        var item = new ListBoxItem { Content = content };
        if (keywords is not null) Shell.SetKeywords(item, keywords);
        return item;
    }

    private static (SplitView Shell, ListBox Nav, Border Head, Window Window) NewShell(bool filter = true, double width = 1200, bool headScrolls = false)
    {
        var nav = new ListBox
        {
            Classes = { "shell-nav" },
            Items =
            {
                new ListBoxItem { Classes = { "shell-group" }, Content = "التخطيط" },
                Link("هيكل التطبيق", "App shell"),
                Link("الشبكة", "Grid"),
                new ListBoxItem { Classes = { "shell-group" }, Content = "النماذج" },
                Link("الحقول", "Form controls"),
                Link("أدوات الاختيار", "Pickers", badge: "جديد"),
                new ListBoxItem { Classes = { "shell-group" }, Content = "المكونات" },
                Link("الأزرار", "Buttons"),
                Link("التحميل والحالة الفارغة", "Loading & empty states"),
            },
            SelectedIndex = 1,
        };
        for (var i = 0; i < 20; i++) nav.Items.Add(Link($"صفحة {i}", $"page {i}"));
        Shell.SetShowFilter(nav, filter);
        var head = new Border { Classes = { "shell-brand" }, Child = new TextBlock { Classes = { "shell-brand-name" }, Text = "تطبيقي" } };
        DockPanel.SetDock(head, Dock.Top);
        var shell = new SplitView { IsPaneOpen = true, Pane = new DockPanel { Children = { head, nav } }, Content = new Panel() };
        Shell.SetHeadScrolls(shell, headScrolls);
        var w = Show(shell, FlowDirection.RightToLeft, width: width, height: 500);
        return (shell, nav, head, w);
    }

    private static TextBox Field(ListBox nav) => Part<TextBox>(nav, "PART_Filter");

    private static List<string> Shown(ListBox nav) =>
        Enumerable.Range(0, nav.ItemCount).Select(i => (ListBoxItem)nav.ContainerFromIndex(i)!)
            .Where(c => c.IsVisible && !c.Classes.Contains("shell-group")).Select(Label).ToList();

    private static List<string> Heads(ListBox nav) =>
        Enumerable.Range(0, nav.ItemCount).Select(i => (ListBoxItem)nav.ContainerFromIndex(i)!)
            .Where(c => c.IsVisible && c.Classes.Contains("shell-group")).Select(c => (string)c.Content!).ToList();

    private static string Label(ListBoxItem c) =>
        c.Content as string ?? ((DockPanel)c.Content!).Children.OfType<TextBlock>().Last().Text!;

    private static void Query(ListBox nav, string text)
    {
        Field(nav).Text = text;
        Settle(100);
    }

    private static void Press(Window w, Key key, PhysicalKey physical, string? symbol = null)
    {
        w.KeyPress(key, RawInputModifiers.None, physical, symbol);
        w.KeyRelease(key, RawInputModifiers.None, physical, symbol);
        Settle(200);
    }

    private static Rect InWindow(Visual v, Visual w) => new Rect(v.Bounds.Size).TransformToAABB(v.TransformToVisual(w)!.Value);

    [AvaloniaFact]
    public void HasFilter_puts_a_small_search_field_over_the_links_pinned_with_the_head()
    {
        var (_, nav, head, w) = NewShell();
        var field = Field(nav);
        Assert.True(field.IsEffectivelyVisible);
        Assert.Contains("search", field.Classes);
        Assert.Contains("sm", field.Classes);
        Assert.Equal("بحث", field.PlaceholderText);
        var fieldAt = InWindow(field, w);
        Assert.True(fieldAt.Top >= InWindow(head, w).Bottom - 0.5);
        Assert.True(fieldAt.Bottom <= InWindow(nav.ContainerFromIndex(0)!, w).Top + 0.5);

        Part<ScrollViewer>(nav, "PART_ScrollViewer").Offset = new Vector(0, 200);
        Settle(100);
        Assert.Equal(fieldAt, InWindow(field, w));

        Shell.SetFilterPlaceholderText(nav, "ابحث في الصفحات");
        Assert.Equal("ابحث في الصفحات", field.PlaceholderText);
    }

    [AvaloniaFact]
    public void Without_HasFilter_there_is_no_field()
    {
        var (_, nav, _, _) = NewShell(filter: false);
        Assert.False(Part<Panel>(nav, "PART_FilterBox").IsVisible);
    }

    [AvaloniaFact]
    public void HeadScrolls_scrolls_the_field_away_with_the_links()
    {
        var (shell, nav, _, w) = NewShell(headScrolls: true);
        var field = Field(nav);
        var at = InWindow(field, w);
        Part<ScrollViewer>(shell, "PART_PaneScroll").Offset = new Vector(0, 120);
        Settle(100);
        Assert.Equal(at.Top - 120, InWindow(field, w).Top, 0.5);
    }

    [AvaloniaFact]
    public void Filters_by_text_and_keywords_and_hides_empty_headings()
    {
        var (_, nav, _, _) = NewShell();
        Query(nav, "PICKERS");
        Assert.Equal(["أدوات الاختيار"], Shown(nav));
        Assert.Equal(["النماذج"], Heads(nav));
        Query(nav, "الشبكة");
        Assert.Equal(["الشبكة"], Shown(nav));
        Assert.Equal(["التخطيط"], Heads(nav));
        Query(nav, "جديد");
        Assert.Empty(Shown(nav));
    }

    [AvaloniaFact]
    public void Matching_ignores_hamza_forms_harakat_and_ta_marbuta()
    {
        var (_, nav, _, _) = NewShell();
        Query(nav, "الازرار");
        Assert.Equal(["الأزرار"], Shown(nav));
        Query(nav, "الأَزْرار");
        Assert.Equal(["الأزرار"], Shown(nav));
        Query(nav, "والحاله الفارغه");
        Assert.Equal(["التحميل والحالة الفارغة"], Shown(nav));
    }

    [AvaloniaFact]
    public void Nothing_found_shows_the_empty_line()
    {
        var (_, nav, _, _) = NewShell();
        var empty = Part<TextBlock>(nav, "PART_FilterEmpty");
        Assert.False(empty.IsVisible);
        Query(nav, "zzqq");
        Assert.Empty(Shown(nav));
        Assert.Empty(Heads(nav));
        Assert.True(empty.IsVisible);
        Assert.Equal("لا نتائج", empty.Text);
        Query(nav, "");
        Assert.False(empty.IsVisible);
    }

    [AvaloniaFact]
    public void Escape_clears_then_leaves_and_clearing_restores_the_marker()
    {
        var (_, nav, _, w) = NewShell();
        var all = Shown(nav).Count;
        var field = Field(nav);
        field.Focus();
        Query(nav, "grid");
        Assert.False(nav.ContainerFromIndex(1)!.IsVisible);

        Press(w, Key.Escape, PhysicalKey.Escape);
        Assert.Equal(string.Empty, field.Text);
        Assert.True(field.IsFocused);
        Assert.Equal(all, Shown(nav).Count);
        Assert.Equal(3, Heads(nav).Count);
        var active = (ListBoxItem)nav.ContainerFromIndex(1)!;
        Assert.True(active.IsVisible);
        Assert.Equal(1, Part<Border>(active, "PART_Marker").RenderTransform!.Value.M22, 2);
        Assert.Empty(Part<Canvas>(nav, "PART_Flight").Children);

        Press(w, Key.Escape, PhysicalKey.Escape);
        Assert.False(field.IsFocused);
        Assert.Same(active, w.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void Filtering_lands_a_flight_at_once()
    {
        var (_, nav, _, _) = NewShell();
        nav.SelectedIndex = 2;
        Settle(80);
        Assert.Single(Part<Canvas>(nav, "PART_Flight").Children);
        Query(nav, "الشبكة");
        Assert.Empty(Part<Canvas>(nav, "PART_Flight").Children);
        Assert.DoesNotContain("moving", nav.Classes);
    }

    [AvaloniaFact]
    public void Down_moves_into_the_first_link_shown_and_Enter_opens_it()
    {
        var (_, nav, _, w) = NewShell();
        var field = Field(nav);
        field.Focus();
        Query(nav, "Buttons");
        Press(w, Key.Down, PhysicalKey.ArrowDown);
        Assert.Same(nav.ContainerFromIndex(7), w.FocusManager!.GetFocusedElement());
        Assert.Equal(1, nav.SelectedIndex);

        field.Focus();
        Query(nav, "الحقول");
        Press(w, Key.Enter, PhysicalKey.Enter);
        Assert.Equal(4, nav.SelectedIndex);
    }

    [AvaloniaFact]
    public void Slash_focuses_the_field_unless_typing_or_a_palette_claims_it()
    {
        var (shell, nav, _, w) = NewShell();
        var field = Field(nav);
        var other = new TextBox();
        ((Panel)shell.Content!).Children.Add(other);
        Settle(100);

        nav.ContainerFromIndex(1)!.Focus();
        Press(w, Key.Oem2, PhysicalKey.Slash, "/");
        Assert.True(field.IsFocused);

        other.Focus();
        Press(w, Key.Oem2, PhysicalKey.Slash, "/");
        Assert.True(other.IsFocused);

        var palette = new CommandPalette { Items = { new PaletteItem { Label = "الأزرار" } } };
        using var keys = palette.AttachTo(w);
        nav.ContainerFromIndex(1)!.Focus();
        Press(w, Key.Oem2, PhysicalKey.Slash, "/");
        Assert.True(palette.IsOpen);
        Assert.False(field.IsFocused);
        palette.Close();
    }

    [AvaloniaFact]
    public void In_a_drawer_slash_opens_it_on_the_field()
    {
        var (shell, nav, _, w) = NewShell(width: 800);
        Assert.True(Shell.IsDrawer(shell));
        Assert.False(shell.IsPaneOpen);
        Press(w, Key.Oem2, PhysicalKey.Slash, "/");
        Settle();
        Assert.True(shell.IsPaneOpen);
        Assert.True(Field(nav).IsFocused);
        Assert.True(Field(nav).IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void An_icon_rail_hides_the_field()
    {
        var (shell, nav, _, _) = NewShell();
        Shell.SetDrawerWidth(shell, 0);
        shell.DisplayMode = SplitViewDisplayMode.CompactInline;
        shell.IsPaneOpen = false;
        Settle();
        Assert.False(Part<Panel>(nav, "PART_FilterBox").IsVisible);
        shell.IsPaneOpen = true;
        Settle();
        Assert.True(Part<Panel>(nav, "PART_FilterBox").IsVisible);
    }
}
