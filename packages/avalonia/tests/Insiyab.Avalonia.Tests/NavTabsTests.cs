using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class NavTabsTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ListBoxItem Tab(string name, string? badge = null, bool foot = false)
    {
        var row = new DockPanel { Children = { new Icon { Data = Geometry.Parse("M4 4h16v16H4z") } } };
        if (badge is not null)
        {
            var b = new Border { Classes = { "shell-link-badge" }, Child = new TextBlock { Text = badge } };
            DockPanel.SetDock(b, Dock.Right);
            row.Children.Add(b);
        }
        row.Children.Add(new TextBlock { Text = name });
        var tab = new ListBoxItem { Content = row };
        ToolTip.SetTip(tab, name);
        if (foot) tab.Classes.Add("foot");
        return tab;
    }

    private static ListBox Nav()
    {
        var nav = new ListBox { Classes = { "nav" }, Width = 220, Height = 360, Items = { Tab("الملف الشخصي"), Tab("الإشعارات", "3"), Tab("الأمان"), Tab("المساعدة", foot: true) } };
        nav.SelectedIndex = 0;
        return nav;
    }

    private static ListBoxItem Item(ListBox nav, int i) => (ListBoxItem)nav.ContainerFromIndex(i)!;

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Nav_tabs_wear_the_sidebar_look_in_a_rail_with_the_foot_tab_apart(string dir)
    {
        var nav = Nav();
        var page = new Border { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(20), Child = new DockPanel { Children = { nav, new TextBlock { Text = "محتوى التبويب", Margin = new Thickness(24) } } } };
        page.Bind(Border.BackgroundProperty, page.GetResourceObservable("InsSurface"));
        var w = Show(page, Dir(dir), width: 600, height: 420);
        var rail = nav.GetVisualDescendants().OfType<Border>().First();
        AssertBrush(nav, "InsCtlBg", () => rail.Background);
        Assert.Equal(new Thickness(0, 0, 1, 0), rail.BorderThickness);
        Assert.IsType<NavStack>(nav.ItemsPanelRoot);

        var chosen = Item(nav, 0);
        AssertBrush(chosen, "InsSurface", () => chosen.Background);
        Assert.Equal(0, chosen.CornerRadius.TopLeft);
        var face = Part<Border>(Item(nav, 1), "PART_Face");
        Assert.Equal(new Thickness(0, 0, 0, 1), face.BorderThickness);
        AssertBrush(face, "InsLine", () => face.BorderBrush);
        Assert.Equal(20, Item(nav, 1).GetVisualDescendants().OfType<Icon>().First().Size);
        Assert.Equal(0, Item(nav, 1).GetVisualDescendants().OfType<DockPanel>().First().HorizontalSpacing);
        Assert.False(ToolTip.GetServiceEnabled(Item(nav, 1)));

        var foot = Item(nav, 3);
        Assert.Equal(nav.ItemsPanelRoot!.Bounds.Height, foot.Bounds.Bottom, 0.5);
        Assert.True(foot.Bounds.Top > Item(nav, 2).Bounds.Bottom + 100);
        Assert.Equal(new Thickness(0, 1, 0, 0), Part<Border>(foot, "PART_Face").BorderThickness);

        w.MouseMove(Center(Item(nav, 2), w));
        AssertBrush(Item(nav, 2), "InsLine", () => Item(nav, 2).Background);
        Capture(w, $"nav-tabs-{dir}");

        Item(nav, 2).Focus(NavigationMethod.Tab);
        w.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        w.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Settle(100);
        Assert.True(foot.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Compact_nav_tabs_keep_their_icons_alone_with_their_names_as_tips(string dir)
    {
        var nav = Nav();
        nav.Width = double.NaN;
        nav.Classes.Add("compact");
        var w = Show(new Panel { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, Children = { nav } }, Dir(dir), width: 400, height: 420);
        var tab = Item(nav, 1);
        Assert.Equal(49, nav.Bounds.Width, 0.5);
        Assert.Equal(49, tab.Bounds.Height, 0.5);
        var icon = tab.GetVisualDescendants().OfType<Icon>().First();
        Assert.Equal(24, icon.Size);
        Assert.Equal(new Thickness(0), icon.Margin);
        var content = Part<ContentPresenter>(tab, "PART_ContentPresenter");
        Assert.True(content.ClipToBounds);
        var ink = icon.TranslatePoint(new Point(12, 12), tab)!.Value;
        Assert.Equal(tab.Bounds.Width / 2, ink.X, 0.5);
        Assert.Equal(new Rect(12, 12, 24, 24), content.Bounds);
        Assert.False(tab.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("shell-link-badge")).IsVisible);
        Assert.True(ToolTip.GetServiceEnabled(tab));
        Capture(w, $"nav-tabs-compact-{dir}");
    }
}
