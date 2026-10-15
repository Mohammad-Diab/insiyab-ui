using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A window the OS leaves looks inactive: selected rows go grey, the title dims, the focus ring hides, and all of it comes back with the window at once.
public class InactiveWindowTests
{
    private static ThemeVariant Theme(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // The platform callbacks are internal in Avalonia 12, so the test reaches them the way FrostTests does; library code never does this.
    private static void Platform(Window w, string callback) =>
        ((Action?)w.PlatformImpl!.GetType().GetProperty(callback)!.GetValue(w.PlatformImpl))?.Invoke();

    // Windows sends WM_ACTIVATE before WM_KILLFOCUS: the window deactivates, then loses the keyboard.
    private static void Deactivate(Window w)
    {
        Assert.True(w.IsActive);
        Platform(w, "Deactivated");
        Platform(w, "LostFocus");
        Dispatcher.UIThread.RunJobs();
    }

    private static void Activate(Window w)
    {
        Platform(w, "Activated");
        Dispatcher.UIThread.RunJobs();
    }

    private static int Rings(Window w) => w.GetVisualDescendants().OfType<FocusRing>().Count();

    private static Table Three()
    {
        var table = new Table();
        table.Columns.Add(new TableColumn { Header = "الاسم" });
        foreach (var name in new[] { "ليلى", "عمر", "سارة" })
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell { Content = name });
            table.Rows.Add(row);
        }
        table.Rows[2].IsSelected = true;
        return table;
    }

    private static (TreeViewItem Notes, Window W) ShowTree(string theme)
    {
        var notes = new TreeViewItem { Header = "ملاحظات.txt" };
        var w = Show(new TreeView { Items = { new TreeViewItem { Header = "العقود" }, notes } }, FlowDirection.RightToLeft, Theme(theme));
        notes.IsSelected = true;
        Settle(400);
        return (notes, w);
    }

    [AvaloniaFact]
    public void An_inactive_window_marks_everything_in_it_and_activation_clears_it()
    {
        var inner = new Button { Content = "زر" };
        var w = Show(new StackPanel { Children = { inner } });
        Assert.False(WindowActivity.GetIsInactive(inner));

        Deactivate(w);
        Assert.True(WindowActivity.GetIsInactive(w));
        Assert.True(WindowActivity.GetIsInactive(inner));

        Activate(w);
        Assert.False(WindowActivity.GetIsInactive(inner));
        Assert.False(w.IsSet(WindowActivity.IsInactiveProperty));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Table_selected_row_turns_grey_at_once_and_back(string theme, string dir)
    {
        var table = Three();
        var w = Show(table, Dir(dir), Theme(theme), width: 700);
        var tint = Part<Border>(table.DisplayedRows[2], "PART_Tint");
        var marker = Part<Border>(table.DisplayedRows[2], "PART_Marker");
        AssertBrush(table, "InsToneBrandBg", () => tint.Background);

        Deactivate(w);
        AssertBrush(table, "InsToneMuteBg", tint.Background);
        AssertBrush(table, "InsToneMute", marker.Background);
        Assert.Equal(1, marker.RenderTransform!.Value.M22, 2);
        AssertBrush(table, "InsZebra", Part<Border>(table.DisplayedRows[1], "PART_Tint").Background);

        Activate(w);
        AssertBrush(table, "InsToneBrandBg", tint.Background);
        AssertBrush(table, "InsPrimaryMid", marker.Background);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Tree_selected_row_turns_grey_at_once_and_keeps_its_weight(string theme)
    {
        var (notes, w) = ShowTree(theme);
        var row = Part<Border>(notes, "PART_LayoutRoot");
        var header = Part<ContentPresenter>(notes, "PART_HeaderPresenter");
        AssertBrush(row, "InsTreeSelected", () => row.Background);

        Deactivate(w);
        AssertBrush(row, "InsToneMuteBg", row.Background);
        AssertBrush(header, "InsText1", header.Foreground);
        Assert.Equal(FontWeight.Bold, header.FontWeight);

        Activate(w);
        AssertBrush(row, "InsTreeSelected", row.Background);
        AssertBrush(header, "InsPrimaryDarker", header.Foreground);
    }

    [AvaloniaFact]
    public void A_still_window_switches_at_once_too()
    {
        var (notes, w) = ShowTree("Light");
        Motion.SetStill(w, true);
        Settle(100);
        var row = Part<Border>(notes, "PART_LayoutRoot");

        Deactivate(w);
        AssertBrush(row, "InsToneMuteBg", row.Background);

        Activate(w);
        AssertBrush(row, "InsTreeSelected", row.Background);
    }

    [AvaloniaFact]
    public void List_selected_title_leaves_the_brand_while_inactive()
    {
        var first = new TextBlock { Classes = { "list-title" }, Text = "الطلبات" };
        var second = new TextBlock { Classes = { "list-title" }, Text = "التقارير" };
        var list = new ListBox { Width = 300, Items = { new ListBoxItem { Content = first }, new ListBoxItem { Content = second } }, SelectedIndex = 0 };
        var w = Show(list, FlowDirection.RightToLeft);
        AssertBrush(first, "InsToneBrand", () => first.Foreground);

        Deactivate(w);
        AssertBrush(first, "InsText1", first.Foreground);
        AssertBrush(second, "InsText1", second.Foreground);

        Activate(w);
        AssertBrush(first, "InsToneBrand", first.Foreground);
        AssertBrush(second, "InsText1", second.Foreground);
    }

    [AvaloniaFact]
    public void Sidebar_current_link_loses_the_brand_at_once_and_gets_it_back()
    {
        var icon = new Icon { Data = Geometry.Parse("M0,0 L24,24") };
        var home = new ListBoxItem { Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { icon, new TextBlock { Text = "الرئيسية" } } } };
        var nav = new ListBox { Classes = { "shell-nav" }, Width = 240, Items = { home, new ListBoxItem { Content = "الطلبات" } }, SelectedIndex = 0 };
        var w = Show(nav, FlowDirection.RightToLeft, width: 400, height: 300);
        var marker = Part<Border>(home, "PART_Marker");
        AssertBrush(home, "InsShellInk", () => home.Foreground);
        AssertBrush(home, "InsToneBrandSoft", () => icon.Foreground);

        Deactivate(w);
        AssertBrush(home, "InsText1", home.Foreground);
        AssertBrush(home, "InsToneMute", marker.Background);
        AssertBrush(home, "InsToneMute", icon.Foreground);

        Activate(w);
        AssertBrush(home, "InsShellInk", home.Foreground);
        AssertBrush(home, "InsPrimaryMid", marker.Background);
        AssertBrush(home, "InsToneBrandSoft", icon.Foreground);
    }

    [AvaloniaFact]
    public void Palette_highlighted_row_turns_grey_while_inactive()
    {
        var palette = new CommandPalette { Items = { new PaletteItem { Label = "الأزرار" }, new PaletteItem { Label = "الجداول" } } };
        var w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        var row = palette.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("palette-item") && b.Classes.Contains("active"));

        Deactivate(w);
        Assert.True(palette.IsOpen);
        AssertBrush(row, "InsToneMuteBg", row.Background);
        AssertBrush(row, "InsText1", row.GetValue(TextElement.ForegroundProperty));

        Activate(w);
        AssertBrush(row, "InsPopItemHover", row.Background);
        AssertBrush(row, "InsPopItemHoverFg", row.GetValue(TextElement.ForegroundProperty));
    }

    [AvaloniaFact]
    public void Title_text_dims_and_the_brand_mark_keeps_its_colours()
    {
        var name = new TextBlock { Text = "تطبيقي" };
        var markText = new TextBlock { Text = "م" };
        var mark = new Border { Classes = { "shell-mark" }, Child = markText };
        var title = new TextBlock { Classes = { "shell-title" }, Text = "العنوان" };
        var bar = new Border
        {
            Classes = { "topbar" },
            VerticalAlignment = VerticalAlignment.Top,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Border { Classes = { "island", "brand" }, IsVisible = true, Child = new StackPanel { Children = { mark, name } } },
                    new Border { Classes = { "island", "title" }, Child = new StackPanel { Classes = { "shell-head" }, Children = { title } } },
                },
            },
        };
        var w = Show(bar, FlowDirection.RightToLeft, width: 800, height: 200);
        var markFill = mark.Background;
        var markInk = markText.Foreground;
        AssertBrush(title, "InsShellInk", () => title.Foreground);
        AssertBrush(name, "InsText1", () => name.Foreground);

        Deactivate(w);
        AssertBrush(title, "InsText3", title.Foreground);
        AssertBrush(name, "InsText3", name.Foreground);
        Assert.Same(markFill, mark.Background);
        Assert.Same(markInk, markText.Foreground);

        Activate(w);
        AssertBrush(title, "InsShellInk", title.Foreground);
        AssertBrush(name, "InsText1", name.Foreground);
    }

    // Guard: CaptionButtons already dims with its window; this drives it through the platform's own path, and a sample keeps its state.
    [AvaloniaFact]
    public void Caption_buttons_dim_with_the_window_and_a_sample_keeps_its_state()
    {
        var own = new CaptionButtons();
        var sample = new CaptionButtons { Preview = CaptionPreview.Normal };
        var w = Show(new StackPanel { Children = { own, sample } });
        Assert.DoesNotContain(":inactive", own.Classes);

        Deactivate(w);
        Assert.Contains(":inactive", own.Classes);
        Assert.DoesNotContain(":inactive", sample.Classes);

        Activate(w);
        Assert.DoesNotContain(":inactive", own.Classes);
    }

    [AvaloniaFact]
    public void The_focus_ring_hides_while_inactive_and_comes_back_with_the_window()
    {
        var first = new ListBoxItem { Content = "الأول" };
        var second = new ListBoxItem { Content = "الثاني" };
        var w = Show(new ListBox { Width = 300, Items = { first, second } });
        first.Focus(NavigationMethod.Tab);
        Settle(200);
        var ring = Res(first, "InsFocusRing");
        Assert.Contains(":focus-visible", first.Classes);
        Assert.Equal(1, Rings(w));
        Assert.Equal(ring, Part<Border>(first, "PART_Back").BoxShadow);

        Deactivate(w);
        Settle(100);
        Assert.False(first.IsFocused);
        Assert.Equal(0, Rings(w));
        Assert.NotEqual(ring, Part<Border>(first, "PART_Back").BoxShadow);

        Activate(w);
        Settle(100);
        Assert.True(first.IsFocused);
        Assert.Contains(":focus-visible", first.Classes);
        Assert.Equal(1, Rings(w));
        Assert.Equal(ring, Part<Border>(first, "PART_Back").BoxShadow);

        second.Focus(NavigationMethod.Pointer);
        Settle(100);
        Assert.Equal(0, Rings(w));
        Assert.DoesNotContain(":focus-visible", first.Classes);
    }

    // Guard: a ring the keyboard never showed does not appear on activation.
    [AvaloniaFact]
    public void A_ring_the_pointer_never_showed_stays_away_after_activation()
    {
        var first = new ListBoxItem { Content = "الأول" };
        var w = Show(new ListBox { Width = 300, Items = { first } });
        first.Focus(NavigationMethod.Pointer);
        Settle(100);

        Deactivate(w);
        Activate(w);
        Settle(100);
        Assert.True(first.IsFocused);
        Assert.DoesNotContain(":focus-visible", first.Classes);
        Assert.Equal(0, Rings(w));
    }

    [AvaloniaFact]
    public void A_tree_row_ring_comes_back_with_the_window_and_no_outer_ring()
    {
        var contracts = new TreeViewItem { Header = "العقود" };
        var w = Show(new TreeView { Items = { contracts, new TreeViewItem { Header = "الفواتير" } } }, FlowDirection.RightToLeft);
        contracts.Focus(NavigationMethod.Tab);
        Settle(200);
        var ring = Part<Border>(contracts, "PART_FocusRing");
        Assert.True(ring.IsVisible);

        Deactivate(w);
        Settle(100);
        Assert.False(ring.IsVisible);

        Activate(w);
        Settle(100);
        Assert.True(contracts.IsFocused);
        Assert.True(ring.IsVisible);
        Assert.Equal(0, Rings(w));
    }

    [AvaloniaFact]
    public void A_brand_change_while_inactive_keeps_the_grey_and_brings_the_new_brand_back()
    {
        try
        {
            var table = Three();
            var w = Show(table, width: 700);
            var tint = Part<Border>(table.DisplayedRows[2], "PART_Tint");
            Deactivate(w);
            InsiyabTheme.SetPrimary(Color.Parse("#0F766E"));
            Settle(200);
            AssertBrush(table, "InsToneMuteBg", tint.Background);

            Activate(w);
            AssertBrush(table, "InsToneBrandBg", tint.Background);
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    [AvaloniaFact]
    public void A_popup_left_open_follows_its_window()
    {
        var inside = new TextBlock { Text = "داخل" };
        var anchor = new Button { Content = "زر" };
        var popup = new Popup { PlacementTarget = anchor, IsLightDismissEnabled = false, Child = inside };
        var w = Show(new StackPanel { Children = { anchor, popup } });
        popup.IsOpen = true;
        Settle(200);
        Assert.False(WindowActivity.GetIsInactive(inside));

        Deactivate(w);
        Assert.True(popup.IsOpen);
        Assert.True(WindowActivity.GetIsInactive(inside));

        Activate(w);
        Assert.False(WindowActivity.GetIsInactive(inside));
    }
}
