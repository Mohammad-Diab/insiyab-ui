using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class DocumentTabsTests
{
    private static DocumentTabs NewTabs(params string[] names)
    {
        var tabs = new DocumentTabs();
        foreach (var n in names.Length > 0 ? names : ["الأول", "الثاني", "الثالث"])
            tabs.Items.Add(new DocumentTab { Header = n, Content = $"محتوى «{n}»." });
        return tabs;
    }

    private static DocumentTab Tab(DocumentTabs tabs, int index) => (DocumentTab)tabs.ContainerFromIndex(index)!;

    private static string Header(DocumentTabs tabs, int index) => (string)Tab(tabs, index).Header!;

    private static void Press(Window window, Key key, PhysicalKey physical, RawInputModifiers modifiers)
    {
        window.KeyPress(key, modifiers, physical, null);
        window.KeyRelease(key, modifiers, physical, null);
        Settle(200);
    }

    [AvaloniaFact]
    public void Close_button_shows_on_the_selected_tab_and_under_the_pointer()
    {
        var tabs = NewTabs();
        tabs.SelectedIndex = 1;
        var window = Show(tabs);
        Assert.Equal(1, Part<Panel>(Tab(tabs, 1), "PART_CloseHost").Opacity, 2);
        Assert.Equal(0, Part<Panel>(Tab(tabs, 0), "PART_CloseHost").Opacity, 2);
        var width = Tab(tabs, 0).Bounds.Width;
        window.MouseMove(Center(Tab(tabs, 0), window));
        Settle();
        Assert.Equal(1, Part<Panel>(Tab(tabs, 0), "PART_CloseHost").Opacity, 2);
        Assert.Equal(width, Tab(tabs, 0).Bounds.Width, 1);
        Assert.Equal(20, Part<Button>(Tab(tabs, 0), "PART_Close").Bounds.Width, 1);
    }

    [AvaloniaFact]
    public void Closing_the_selected_tab_selects_the_next_one()
    {
        var tabs = NewTabs();
        tabs.SelectedIndex = 1;
        var asked = new List<int>();
        tabs.TabCloseRequested += (_, e) => asked.Add(e.Index);
        var window = Show(tabs);
        Click(Part<Button>(Tab(tabs, 1), "PART_Close"), window);
        Assert.Equal(new[] { 1 }, asked);
        Assert.Equal(2, tabs.ItemCount);
        Assert.Equal("الثالث", (string)((DocumentTab)tabs.SelectedItem!).Header!);

        tabs.Close(1);
        Settle(100);
        Assert.Single(tabs.Items);
        Assert.Equal("الأول", (string)((DocumentTab)tabs.SelectedItem!).Header!);
    }

    [AvaloniaFact]
    public void A_handled_close_keeps_the_tab()
    {
        var tabs = NewTabs();
        tabs.TabCloseRequested += (_, e) => e.Handled = true;
        Show(tabs);
        tabs.Close(0);
        Settle(100);
        Assert.Equal(3, tabs.ItemCount);
    }

    [AvaloniaFact]
    public void Ctrl_W_and_Delete_close_but_a_pinned_tab_stays()
    {
        var tabs = NewTabs();
        Tab0Pinned(tabs);
        var window = Show(tabs);
        Assert.False(Part<Panel>(Tab(tabs, 0), "PART_CloseHost").IsVisible);

        Tab(tabs, 0).Focus(NavigationMethod.Tab);
        Press(window, Key.W, PhysicalKey.W, RawInputModifiers.Control);
        Assert.Equal(3, tabs.ItemCount);

        tabs.SelectedIndex = 2;
        Tab(tabs, 2).Focus(NavigationMethod.Tab);
        Settle(100);
        Press(window, Key.W, PhysicalKey.W, RawInputModifiers.Control);
        Assert.Equal(2, tabs.ItemCount);
        Assert.Equal("الثاني", (string)((DocumentTab)tabs.SelectedItem!).Header!);
        Settle(200);
        Assert.True(Tab(tabs, 1).IsKeyboardFocusWithin);

        Press(window, Key.Delete, PhysicalKey.Delete, RawInputModifiers.None);
        Assert.Single(tabs.Items);
    }

    private static void Tab0Pinned(DocumentTabs tabs) => ((DocumentTab)tabs.Items[0]!).IsClosable = false;

    [AvaloniaFact]
    public void A_middle_click_closes_a_tab()
    {
        var tabs = NewTabs();
        var window = Show(tabs);
        var p = Center(Tab(tabs, 2), window);
        window.MouseDown(p, MouseButton.Middle);
        window.MouseUp(p, MouseButton.Middle);
        Settle(100);
        Assert.Equal(2, tabs.ItemCount);
        Assert.Equal("الثاني", Header(tabs, 1));
        Assert.Equal(0, tabs.SelectedIndex);
    }

    private static void Drag(Window window, Control tab, double dx)
    {
        var from = Center(tab, window);
        window.MouseMove(from);
        window.MouseDown(from, MouseButton.Left);
        for (var i = 1; i <= 6; i++)
        {
            window.MouseMove(new Point(from.X + dx * i / 6, from.Y));
            Settle(30);
        }
    }

    [AvaloniaFact]
    public void Dragging_a_tab_past_its_neighbours_moves_it_and_keeps_it_selected()
    {
        var tabs = NewTabs();
        DocumentTabMoveEventArgs? moved = null;
        tabs.TabMoveRequested += (_, e) => moved = e;
        var window = Show(tabs);
        var first = Tab(tabs, 0);
        var span = Center(Tab(tabs, 2), window).X - Center(first, window).X + 10;
        Drag(window, first, span);
        Assert.Contains(":dragging", first.Classes);
        Assert.True(Part<Panel>(first, "PART_Slide").RenderTransform!.Value.M31 > span - 20);
        Settle(300);
        Assert.True(Part<Panel>(Tab(tabs, 1), "PART_Slide").RenderTransform!.Value.M31 < 0);

        var end = Center(first, window);
        window.MouseUp(new Point(end.X + span, end.Y), MouseButton.Left);
        Settle(200);
        Assert.NotNull(moved);
        Assert.Equal(0, moved!.From);
        Assert.Equal(2, moved.To);
        Assert.Equal(new[] { "الثاني", "الثالث", "الأول" }, Enumerable.Range(0, 3).Select(i => Header(tabs, i)));
        Assert.Equal("الأول", (string)((DocumentTab)tabs.SelectedItem!).Header!);
        Assert.DoesNotContain(":dragging", first.Classes);
        Assert.True(Part<Panel>(Tab(tabs, 0), "PART_Slide").RenderTransform!.Value.IsIdentity);
    }

    [AvaloniaFact]
    public void In_RTL_a_tab_is_dragged_toward_the_left_to_go_later()
    {
        var tabs = NewTabs();
        var window = Show(tabs, FlowDirection.RightToLeft);
        var first = Tab(tabs, 0);
        Assert.True(Center(first, window).X > Center(Tab(tabs, 1), window).X);
        var span = Center(Tab(tabs, 1), window).X - Center(first, window).X - 10;
        Drag(window, first, span);
        var end = Center(first, window);
        window.MouseUp(end, MouseButton.Left);
        Settle(200);
        Assert.Equal("الأول", Header(tabs, 1));
        Assert.Equal("الثاني", Header(tabs, 0));
    }

    [AvaloniaFact]
    public void Ctrl_Shift_PageDown_moves_the_selected_tab()
    {
        var tabs = NewTabs();
        var window = Show(tabs);
        Tab(tabs, 0).Focus(NavigationMethod.Tab);
        Press(window, Key.PageDown, PhysicalKey.PageDown, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.Equal("الأول", Header(tabs, 1));
        Assert.Equal(1, tabs.SelectedIndex);
        Press(window, Key.PageUp, PhysicalKey.PageUp, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.Equal("الأول", Header(tabs, 0));
    }

    [AvaloniaFact]
    public void Ctrl_Tab_still_cycles_the_tabs()
    {
        var tabs = NewTabs();
        var window = Show(tabs);
        Tab(tabs, 0).Focus(NavigationMethod.Tab);
        Press(window, Key.Tab, PhysicalKey.Tab, RawInputModifiers.Control);
        Assert.Equal(1, tabs.SelectedIndex);
        Press(window, Key.Tab, PhysicalKey.Tab, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.Equal(0, tabs.SelectedIndex);
    }

    [AvaloniaFact]
    public void An_overflowing_strip_scrolls_between_arrows()
    {
        var tabs = NewTabs("تقرير الربع الأول", "خطة المشروع", "ملاحظات الاجتماع", "الميزانية", "قائمة المهام", "العقد", "سجل التغييرات", "دليل الاستخدام");
        var window = Show(new Border { Width = 420, Child = tabs }, width: 520);
        Assert.Contains(":overflow", tabs.Classes);
        var back = Part<RepeatButton>(tabs, "PART_Back");
        var forward = Part<RepeatButton>(tabs, "PART_Forward");
        Assert.True(back.IsVisible);
        Assert.False(back.IsEnabled);
        Assert.True(forward.IsEnabled);
        Assert.True(Part<Border>(tabs, "PART_Strip").Bounds.Width <= 420.5);

        Click(forward, window);
        var scroll = Part<ScrollViewer>(tabs, "PART_Scroll");
        Assert.True(scroll.Offset.X > 0);
        Assert.True(back.IsEnabled);

        tabs.SelectedIndex = 7;
        Settle(300);
        Assert.Equal(scroll.Extent.Width - scroll.Viewport.Width, scroll.Offset.X, 1);
        Assert.False(forward.IsEnabled);
        tabs.SelectedIndex = 0;
        Settle(300);
        Assert.Equal(0, scroll.Offset.X, 1);

        var few = NewTabs();
        Show(few);
        Assert.DoesNotContain(":overflow", few.Classes);
        Assert.False(Part<RepeatButton>(few, "PART_Back").IsVisible);
    }

    [AvaloniaFact]
    public void The_new_button_asks_the_app_for_a_tab()
    {
        var tabs = NewTabs();
        tabs.ShowNewButton = true;
        var asked = 0;
        tabs.NewTabRequested += (_, _) => asked++;
        var window = Show(tabs);
        var add = Part<Button>(tabs, "PART_New");
        Assert.True(add.IsVisible);
        Assert.True(Center(add, window).X > Center(Tab(tabs, 2), window).X);
        Click(add, window);
        Assert.Equal(1, asked);
        Assert.Equal(3, tabs.ItemCount);

        var none = NewTabs();
        Show(none);
        Assert.False(Part<Button>(none, "PART_New").IsVisible);
    }

    [AvaloniaFact]
    public void A_bound_list_is_edited_in_place()
    {
        var files = new ObservableCollection<string> { "أ", "ب", "ج" };
        var tabs = new DocumentTabs { ItemsSource = files };
        Show(tabs);
        Assert.IsType<DocumentTab>(tabs.ContainerFromIndex(0));
        tabs.Move(0, 2);
        Settle(100);
        Assert.Equal(new[] { "ب", "ج", "أ" }, files);
        tabs.Close(0);
        Settle(100);
        Assert.Equal(new[] { "ج", "أ" }, files);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Document_tabs_render_in_every_look(string theme, string dir)
    {
        var tabs = NewTabs("تقرير الربع الأول", "خطة المشروع", "ملاحظات الاجتماع");
        tabs.ShowNewButton = true;
        tabs.SelectedIndex = 1;
        var many = NewTabs("تقرير الربع الأول", "خطة المشروع", "ملاحظات الاجتماع", "الميزانية", "قائمة المهام", "العقد", "سجل التغييرات");
        Tab0Pinned(many);
        var content = new StackPanel { Spacing = 16, Margin = new Thickness(24), Children = { tabs, new Border { Width = 520, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left, Child = many } } };
        var window = Show(content, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, width: 880, height: 360);
        Capture(window, $"doctabs-{theme}-{dir}");
    }
}
