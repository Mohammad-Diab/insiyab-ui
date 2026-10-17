using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Brand;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// On dark every brand tint drawn at an alpha takes the primary's middle shade; on light it stays the primary.
public class DarkTintTests
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly Regex Primary = new(@"\{DynamicResource (InsPrimary|InsPrimaryColor|InsPrimaryRgbColor)\}");

    private static readonly string[] BigFills = ["InsButtonPrimaryFill", "InsAvatarFill", "InsPickerBrandFill", "InsShellMarkFill"];

    private static string ThemesDir([CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../src/Insiyab.Avalonia/Themes"));

    private static bool IsPrimaryKey(string? key) => key is "InsPrimary" or "InsPrimaryColor" or "InsPrimaryRgbColor";

    private static bool Allowed(XElement el)
    {
        if (el.Ancestors().Any(a => a.Name.LocalName == "ResourceDictionary" && (string?)a.Attribute(X + "Key") == "Light")) return true;
        var keyed = el.AncestorsAndSelf().FirstOrDefault(a => a.Attribute(X + "Key") is not null);
        if (keyed is not null && BigFills.Contains((string?)keyed.Attribute(X + "Key"))) return true;
        return el.Name.LocalName == "Setter" && (string?)el.Attribute("Property") == "IconBackground";
    }

    [Fact]
    public void No_theme_tints_with_the_primary_outside_a_light_dictionary()
    {
        var found = new List<string>();
        foreach (var path in Directory.EnumerateFiles(ThemesDir(), "*.axaml", SearchOption.AllDirectories))
        {
            var doc = XDocument.Load(path, LoadOptions.SetLineInfo);
            foreach (var el in doc.Descendants())
            {
                if (Allowed(el)) continue;
                var at = ((IXmlLineInfo)el).LineNumber;
                foreach (var a in el.Attributes())
                    foreach (Match m in Primary.Matches(a.Value))
                        found.Add($"{Path.GetFileName(path)}:{at} {a.Name.LocalName}={m.Value}");
                if (el.Name.LocalName == "DynamicResource" && IsPrimaryKey((string?)el.Attribute("ResourceKey")))
                    found.Add($"{Path.GetFileName(path)}:{at} DynamicResource {(string?)el.Attribute("ResourceKey")}");
            }
        }
        Assert.True(found.Count == 0, "brand tints outside a light dictionary: " + string.Join("; ", found));
    }

    public static IEnumerable<object[]> TintKeys() => new[]
    {
        "InsBrandTint", "InsChart1", "InsSelection", "InsScrollbarThumb", "InsScrollbarThumbHover", "InsScrollbarThumbPressed",
        "InsButtonGhostBorder", "InsButtonGhostHover", "InsButtonGhostPressed", "InsButtonToggleOn", "InsButtonToggleOnBorder",
        "InsCheckBoxHoverBorder", "InsCheckBoxRing", "InsTileHoverBorder", "InsMatrixCardHover", "InsStatBrandBg",
        "InsTableRule", "InsTableGrip", "InsTableSortHover", "InsTableEditRing", "InsToolbarOn", "InsTabLineGlow", "InsTreeSelected",
        "InsAlertInfoBorder", "InsSpinnerTrack", "InsProgressSweep", "InsSelectHoverBorder", "InsSelectFocusBorder",
        "InsSpinHoverBorder", "InsSpinFocusBorder", "InsRangeThumbRing", "InsChipHoverBorder", "InsPaletteFocusBorder", "InsPaletteSelection",
        "InsPopItemHover", "InsPopItemRest", "InsTagsHoverBorder", "InsTagsFocusBorder", "InsTagsSelection",
        "InsNotifyIcoBg", "InsNotifyFootHover", "InsNotifyUnreadBd", "InsToastInfoBd", "InsPanelIcoBrush",
        "InsPickerHoverBorder", "InsPickerFocusBorder", "InsPickerOptionHover", "InsPickerFocusOutline",
        "InsPluginsHoverBorder", "InsPluginsFocusBorder", "InsPluginsTint", "InsFileOverBorder",
        "InsTextBoxHoverBorder", "InsTextBoxFocusBorder", "InsTextBoxSelection", "InsSwitchHoverBorder", "InsSwitchRing",
    }.Select(k => new object[] { k });

    [AvaloniaTheory]
    [MemberData(nameof(TintKeys))]
    public void A_brand_tint_is_the_mid_shade_on_dark_and_the_primary_on_light(string key)
    {
        var anchor = new Border();
        var window = Show(anchor, theme: ThemeVariant.Dark);
        Assert.Equal(ColorOf((IBrush)Res(anchor, "InsPrimaryMid")), ColorOf((IBrush)Res(anchor, key)));
        window.RequestedThemeVariant = ThemeVariant.Light;
        Settle(50);
        Assert.Equal(ColorOf((IBrush)Res(anchor, "InsPrimary")), ColorOf((IBrush)Res(anchor, key)));
    }

    [AvaloniaFact]
    public void The_first_chart_series_turns_to_the_mid_shade_with_the_theme()
    {
        var chart = new Chart { Type = ChartType.Bar, Labels = "أ,ب,ج", Width = 400 };
        chart.Series.Add(new ChartSeries("الأول", 1, 2, 3));
        chart.Series.Add(new ChartSeries("الثاني", 3, 2, 1));
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { chart } });
        AssertBrush(chart, "InsPrimary", () => chart.Brush1);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Settle(100);
        AssertBrush(chart, "InsPrimaryMid", () => chart.Brush1);
        var key = Part<WrapPanel>(chart, "PART_Legend").GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("chart-key"));
        AssertBrush(chart, "InsPrimaryMid", () => key.Background);
    }

    [AvaloniaFact]
    public void The_table_bar_is_washed_with_the_mid_shade_on_dark()
    {
        var table = new Table();
        table.Columns.AddRange([new TableColumn { Header = "الاسم" }, new TableColumn { Header = "المدينة" }]);
        var row = new TableRow();
        row.Cells.Add(new TableCell { Content = "ليلى" });
        row.Cells.Add(new TableCell { Content = "الرياض" });
        table.Rows.Add(row);
        Show(table, FlowDirection.RightToLeft, ThemeVariant.Dark, width: 600);
        var bar = Part<Border>(table, "PART_Bar");
        var wash = Assert.IsType<LinearGradientBrush>(bar.Background);
        var mid = ColorOf((IBrush)Res(table, "InsPrimaryMid"));
        var top = wash.GradientStops[0].Color;
        Assert.Equal((mid.R, mid.G, mid.B), (top.R, top.G, top.B));
        Assert.Equal(51, top.A);
        var rim = Assert.IsAssignableFrom<ISolidColorBrush>(bar.BorderBrush);
        Assert.Equal(mid, rim.Color);
        Assert.Equal(0.24, rim.Opacity, 2);
    }

    [AvaloniaFact]
    public void The_scrollbar_thumb_reads_on_dark()
    {
        var scroll = new ScrollViewer { Width = 300, Height = 200, VerticalScrollBarVisibility = ScrollBarVisibility.Visible, Content = new Border { Height = 2000, Width = 100 } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { scroll } }, theme: ThemeVariant.Dark);
        var bar = scroll.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == Orientation.Vertical);
        var thumb = bar.GetVisualDescendants().OfType<Thumb>().First();
        var mid = ColorOf((IBrush)Res(scroll, "InsPrimaryMid"));
        var rest = Assert.IsAssignableFrom<ISolidColorBrush>(thumb.Background);
        Assert.Equal(mid, rest.Color);
        Assert.Equal(0.45, rest.Opacity, 2);
        window.MouseMove(Center(thumb, window));
        Settle(100);
        var hover = Assert.IsAssignableFrom<ISolidColorBrush>(thumb.Background);
        Assert.Equal(mid, hover.Color);
        Assert.Equal(0.65, hover.Opacity, 2);
    }

    [AvaloniaFact]
    public void A_ghost_button_rim_is_the_mid_shade_on_dark()
    {
        var ghost = new Button { Content = "تصدير", Classes = { "ghost" } };
        Show(new StackPanel { Margin = new Thickness(20), Children = { ghost } }, theme: ThemeVariant.Dark);
        AssertBrush(ghost, "InsPrimaryMid", () => ghost.BorderBrush);
        Assert.Equal(0.45, ghost.BorderBrush!.Opacity, 2);
    }

    [AvaloniaFact]
    public void Tints_follow_SetPrimary_on_dark()
    {
        var teal = Color.Parse("#0F766E");
        try
        {
            InsiyabTheme.SetPrimary(teal);
            var ghost = new Button { Content = "تصدير", Classes = { "ghost" } };
            Show(new StackPanel { Margin = new Thickness(20), Children = { ghost } }, theme: ThemeVariant.Dark);
            var want = BrandDerivation.Derive(teal, true).PrimaryMid;
            Settle(200);
            Assert.Equal(want, ColorOf(ghost.BorderBrush));
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }
}
