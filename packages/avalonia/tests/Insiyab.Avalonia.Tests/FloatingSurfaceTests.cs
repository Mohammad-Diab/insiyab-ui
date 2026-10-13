using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Avalonia cannot blur the app's own content, so every glass surface that floats over it takes the web's no-blur fallback: opaque.
public class FloatingSurfaceTests
{
    public static IEnumerable<object[]> Themes() => [["Light"], ["Dark"]];

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static void AssertGround(Visual anchor, string glassKey, IBrush? brush)
    {
        var glass = (Color)Res(anchor, glassKey + "Color");
        var wash = (Color)Res(anchor, "InsWash1Color");
        var got = ColorOf(brush);
        Assert.Equal(255, got.A);
        Assert.Equal(InsiyabTheme.Over(glass, wash), got);
        AssertBrush(anchor, glassKey + "Ground", brush);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void A_scrolled_top_bar_is_opaque(string theme)
    {
        var bar = new Border { Classes = { "topbar" }, VerticalAlignment = VerticalAlignment.Top, Child = new Border { Height = 40 } };
        var scroller = new ScrollViewer { Content = new Border { Height = 2000 } };
        Shell.SetFrostsTopbar(scroller, true);
        Show(new Panel { Children = { scroller, bar } }, theme: Variant(theme));
        scroller.Offset = new Vector(0, 200);
        Settle();
        Assert.Contains("scrolled", bar.Classes);
        Settle(400);
        AssertGround(bar, "InsGlass", bar.Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void Select_and_autocomplete_lists_are_opaque(string theme)
    {
        var combo = new ComboBox { ItemsSource = new[] { "الرياض", "عمّان" }, Width = 220 };
        var auto = new AutoCompleteBox { ItemsSource = new[] { "الرياض", "عمّان" }, Width = 220, MinimumPrefixLength = 0 };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { combo, auto } }, theme: Variant(theme));
        combo.IsDropDownOpen = true;
        Settle(300);
        AssertGround(combo, "InsGlassStrong", ((Border)Part<Popup>(combo, "PART_Popup").Child!).Background);
        combo.IsDropDownOpen = false;
        AssertGround(auto, "InsGlassStrong", ((Border)Part<Popup>(auto, "PART_Popup").Child!).Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void Tag_list_and_chart_tip_are_opaque(string theme)
    {
        var list = new Border { Classes = { "combo-list" }, Width = 100, Height = 40 };
        var chart = new Chart { Values = "3,5,4", SeriesName = "س", Width = 300 };
        Show(new StackPanel { Children = { list, chart } }, theme: Variant(theme));
        AssertGround(list, "InsGlassStrong", list.Background);
        AssertGround(chart, "InsGlassStrong", Part<Border>(chart, "PART_Tip").Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void A_toast_lies_on_the_opaque_wash(string theme)
    {
        var button = new Button { Content = "x" };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, theme: Variant(theme), width: 900, height: 600);
        Toast.Show(button, "تم الحفظ", ToastTone.Ok);
        Settle();
        var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
        var ground = Part<Border>(card, "PART_Ground");
        AssertBrush(card, "InsWash1", () => ground.Background);
        Assert.Equal(255, ColorOf(ground.Background).A);
        Assert.Equal(Part<Border>(card, "PART_Card").Bounds, ground.Bounds);
        window.Close();
    }

    [AvaloniaFact]
    public void Grounds_follow_the_brand()
    {
        var host = new Border();
        Show(host);
        var before = ColorOf((IBrush)Res(host, "InsGlassGround"));
        InsiyabTheme.SetPrimary(Color.Parse("#1D4ED8"));
        try
        {
            var wash = (Color)Res(host, "InsWash1Color");
            Assert.Equal(InsiyabTheme.Over((Color)Res(host, "InsGlassColor"), wash), ColorOf((IBrush)Res(host, "InsGlassGround")));
            Assert.NotEqual(before, ColorOf((IBrush)Res(host, "InsGlassGround")));
        }
        finally { InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary); }
    }
}
