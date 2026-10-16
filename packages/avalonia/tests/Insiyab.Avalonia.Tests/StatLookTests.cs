using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The stat tile by look classes: every state pictured and dumped for the before/after comparison, runtime class changes, overrides, and what the tile relays to its children.
public class StatLookTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");

    private static Border Tile(string mods, bool hint = false, bool trend = false)
    {
        var body = new StackPanel { Classes = { "stat-body" }, Children = { new TextBlock { Classes = { "stat-label" }, Text = "الملفات" }, new TextBlock { Classes = { "stat-value" }, Text = "128" } } };
        if (hint) body.Children.Add(new TextBlock { Classes = { "stat-hint" }, Text = "هذا الشهر" });
        if (trend) body.Children.Add(new StackPanel { Classes = { "stat-trend" }, Children = { new Delta { Classes = { "up" }, Content = "12%" }, new TextBlock { Classes = { "delta-ref" }, Text = "عن الشهر الماضي" } } });
        var tile = new Border { Classes = { "stat" }, Child = new DockPanel { Children = { new Border { Classes = { "stat-ico" }, Child = new Icon { Data = Glyph } }, body } } };
        foreach (var c in mods.Split(' ', StringSplitOptions.RemoveEmptyEntries)) tile.Classes.Add(c);
        return tile;
    }

    private static Border Ico(Visual tile) => tile.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("stat-ico"));

    private static TextBlock Text(Visual tile, string cls) => tile.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains(cls));

    private static TextBlock Value(Visual tile) => Text(tile, "stat-value");

    private static TextBlock Label(Visual tile) => Text(tile, "stat-label");

    private static TextBlock Hint(Visual tile) => Text(tile, "stat-hint");

    private static DockPanel Dock(Visual tile) => tile.GetVisualDescendants().OfType<DockPanel>().First();

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static bool Moved(Visual v) => v.RenderTransform is { } t && !t.Value.IsIdentity;

    private static bool Near(Color a, Color b) =>
        Math.Abs(a.A - b.A) <= 6 && Math.Abs(a.R - b.R) <= 6 && Math.Abs(a.G - b.G) <= 6 && Math.Abs(a.B - b.B) <= 6;

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Stat_tiles_keep_their_look(string theme, string dir)
    {
        var grid = new UniformGrid { Columns = 3, ColumnSpacing = 12, RowSpacing = 12 };
        grid.Children.Add(Tile("glass", hint: true));
        grid.Children.Add(Tile("glass ok", trend: true));
        foreach (var m in new[] { "glass warn", "glass bad", "glass info", "glass mute", "glass zero" }) grid.Children.Add(Tile(m));
        grid.Children.Add(Tile("glass sm", hint: true));
        grid.Children.Add(Tile("glass lg", trend: true));
        var hover = Tile("glass hoverable");
        var link = new Button { Classes = { "stat-link" }, Content = Tile("glass info") };
        var inPanel = new Border { Classes = { "panel" }, Padding = new Thickness(16), Child = Tile("warn sm") };
        var row = new UniformGrid { Columns = 3, ColumnSpacing = 12, Children = { hover, link, inPanel } };
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 860, HorizontalAlignment = HorizontalAlignment.Left, Children = { grid, row } };
        var w = Show(page, Dir(dir), Variant(theme), 900, 820);
        LookCheck.Keep(w, page, $"stat-{theme}-{dir}");
        var tiles = grid.Children.Cast<Border>().ToList();
        AssertBrush(tiles[0], "InsToneBrandSoft", () => Value(tiles[0]).Foreground);
        AssertBrush(tiles[1], "InsToneOk", () => Value(tiles[1]).Foreground);
        AssertBrush(tiles[2], "InsToneWarnBg", () => Ico(tiles[2]).Background);
        AssertBrush(tiles[6], "InsText2", () => Value(tiles[6]).Foreground);
        AssertBrush(tiles[6], "InsToneMuteBg", () => Ico(tiles[6]).Background);
        Assert.Equal(32, Ico(tiles[7]).Bounds.Width, 0);
        Assert.Equal(32, Value(tiles[8]).FontSize);
        Assert.Equal(new Thickness(16, 12), tiles[0].Padding);
        if (dir == "rtl") Assert.Equal(18.7, Hint(tiles[0]).LineHeight, 1);
        else Assert.True(double.IsNaN(Hint(tiles[0]).LineHeight));

        w.MouseMove(Center(hover, w));
        Settle();
        LookCheck.Keep(w, page, $"stat-hover-{theme}-{dir}");
        Assert.True(Moved(Ico(hover)));
        Assert.Equal(0.12, Value(hover).LetterSpacing, 2);

        var inner = (Border)link.Content!;
        w.MouseMove(Center(link, w));
        Settle();
        LookCheck.Keep(w, page, $"stat-link-{theme}-{dir}");
        Assert.True(Moved(Ico(inner)));
        Assert.False(Moved(Ico(hover)));

        Motion.SetStill(w, true);
        Settle();
        LookCheck.Keep(w, page, $"stat-still-{theme}-{dir}");
        Assert.False(Moved(Ico(inner)));
        Assert.Equal(0.12, Value(inner).LetterSpacing, 2);
    }

    [AvaloniaFact]
    public void Stat_modifiers_follow_classes_changed_at_runtime()
    {
        var tile = Tile("glass");
        var w = Show(tile, FlowDirection.RightToLeft);
        tile.Classes.Add("ok");
        AssertBrush(tile, "InsToneOk", () => Value(tile).Foreground);
        AssertBrush(tile, "InsToneOkBg", () => Ico(tile).Background);
        tile.Classes.Remove("ok");
        AssertBrush(tile, "InsToneBrandSoft", () => Value(tile).Foreground);
        tile.Classes.Add("sm");
        Settle();
        Assert.Equal(32, Ico(tile).Bounds.Width, 0);
        Assert.Equal(new Thickness(12, 10), tile.Padding);
        Assert.Equal(10, Dock(tile).HorizontalSpacing);
        tile.Classes.Remove("sm");
        Settle();
        Assert.Equal(38, Ico(tile).Bounds.Width, 0);
        Assert.Equal(12, Dock(tile).HorizontalSpacing);
        tile.Classes.Add("hoverable");
        w.MouseMove(Center(tile, w));
        Settle();
        Assert.True(Moved(Ico(tile)));
    }

    [AvaloniaFact]
    public void Stat_follows_the_theme_switch_and_SetPrimary()
    {
        var brand = Tile("glass");
        var ok = Tile("glass ok");
        var w = Show(new StackPanel { Spacing = 12, Children = { brand, ok } }, FlowDirection.RightToLeft);
        var light = ColorOf(Ico(brand).Background);
        w.RequestedThemeVariant = ThemeVariant.Dark;
        Settle();
        AssertBrush(ok, "InsToneOk", () => Value(ok).Foreground);
        AssertBrush(ok, "InsToneOkBg", () => Ico(ok).Background);
        AssertBrush(brand, "InsToneBrandSoft", () => Value(brand).Foreground);
        AssertBrush(brand, "InsStatBrandBg", () => Ico(brand).Background);
        var dark = ColorOf(Ico(brand).Background);
        try
        {
            InsiyabTheme.SetPrimary(Color.Parse("#0D9488"));
            Settle();
            AssertBrush(brand, "InsStatBrandBg", () => Ico(brand).Background);
            AssertBrush(brand, "InsToneBrandSoft", () => Value(brand).Foreground);
            Assert.False(Near(dark, ColorOf(Ico(brand).Background)), "the chip kept the old primary");
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
        Settle();
        Assert.True(Near(dark, ColorOf(Ico(brand).Background)));
        w.RequestedThemeVariant = ThemeVariant.Light;
        Settle();
        Assert.True(Near(light, ColorOf(Ico(brand).Background)));
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_stat_look()
    {
        var a = Tile("glass ok");
        var b = Tile("glass warn");
        var c = Tile("glass");
        var page = new StackPanel { Spacing = 12, Children = { a, b, c } };
        page.Styles.Add(App<Border>("stat", Border.PaddingProperty, new Thickness(4)));
        page.Styles.Add(App<TextBlock>("stat-value", TextBlock.ForegroundProperty, Brushes.Red));
        page.Styles.Add(App<Border>("stat-ico", Border.BackgroundProperty, Brushes.Blue));
        var appStyle = App<TextBlock>("stat-label", TextBlock.FontSizeProperty, 30d);
        var typeOnly = new Style(x => x.OfType<TextBlock>()) { Setters = { new Setter(TextBlock.LineHeightProperty, 99d) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(typeOnly);
        try
        {
            Show(page, FlowDirection.RightToLeft);
            Label(c).Foreground = Brushes.Green;
            Settle();
            Assert.Equal(new Thickness(4), a.Padding);
            Assert.True(Near(Colors.Red, ColorOf(Value(a).Foreground)));
            Assert.True(Near(Colors.Blue, ColorOf(Ico(b).Background)));
            Assert.Equal(30, Label(a).FontSize);
            Assert.True(Near(Colors.Green, ColorOf(Label(c).Foreground)));
            Assert.Equal(24, Value(a).LineHeight);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(typeOnly);
        }
        Settle();
        Assert.Equal(12, Label(a).FontSize);
    }

    [AvaloniaFact]
    public void Stat_classes_added_at_runtime_take_their_look()
    {
        var value = new TextBlock { Text = "128" };
        var ico = new Border { Child = new Icon { Data = Glyph } };
        var tile = new Border { Child = new DockPanel { Children = { ico, new StackPanel { Children = { new TextBlock { Text = "الملفات" }, value } } } } };
        Show(tile, FlowDirection.RightToLeft);
        var plainSize = value.FontSize;
        tile.Classes.Add("stat");
        ico.Classes.Add("stat-ico");
        value.Classes.Add("stat-value");
        Settle();
        Assert.Equal(new Thickness(16, 12), tile.Padding);
        Assert.Equal(38, ico.Bounds.Width, 0);
        Assert.Equal(24, value.FontSize);
        Assert.Equal(12, Dock(tile).HorizontalSpacing);
        AssertBrush(tile, "InsToneBrandSoft", () => value.Foreground);
        tile.Classes.Add("bad");
        AssertBrush(tile, "InsToneBad", () => value.Foreground);
        value.Classes.Remove("stat-value");
        tile.Classes.Remove("stat");
        Settle();
        Assert.Equal(plainSize, value.FontSize);
        Assert.Equal(new Thickness(0), tile.Padding);
        Assert.Equal(0, Dock(tile).HorizontalSpacing);
        Assert.Null(Dock(tile).Theme);
    }

    [AvaloniaFact]
    public void Only_the_stat_parts_take_a_theme()
    {
        Assert.False(Application.Current!.TryFindResource(typeof(Border), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(TextBlock), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(StackPanel), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(DockPanel), out _));
        var plain = new Border { Child = new TextBlock { Text = "أ" } };
        var tile = Tile("glass", hint: true, trend: true);
        Show(new StackPanel { Children = { plain, new DockPanel(), tile } }, FlowDirection.RightToLeft);
        Assert.Null(plain.Theme);
        Assert.Null(((TextBlock)plain.Child!).Theme);
        Assert.NotNull(tile.Theme);
        Assert.NotNull(Ico(tile).Theme);
        Assert.NotNull(Value(tile).Theme);
        Assert.NotNull(Label(tile).Theme);
        Assert.NotNull(Hint(tile).Theme);
        Assert.NotNull(Text(tile, "delta-ref").Theme);
        Assert.All(tile.GetVisualDescendants().OfType<StackPanel>().Where(s => s.Classes.Contains("stat-body") || s.Classes.Contains("stat-trend")), s => Assert.NotNull(s.Theme));
        Assert.NotNull(Dock(tile).Theme);
    }

    [AvaloniaFact]
    public void The_chip_centres_its_icon_and_lets_go_of_a_child_that_leaves()
    {
        var tile = Tile("glass");
        var w = Show(tile, FlowDirection.RightToLeft);
        var chip = Ico(tile);
        var icon = (Icon)chip.Child!;
        Assert.Equal(HorizontalAlignment.Center, icon.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, icon.VerticalAlignment);
        chip.Child = null;
        Assert.Equal(HorizontalAlignment.Stretch, icon.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, icon.VerticalAlignment);
        var next = new Icon { Data = Glyph };
        chip.Child = next;
        Settle();
        Assert.Equal(HorizontalAlignment.Center, next.HorizontalAlignment);
        next.HorizontalAlignment = HorizontalAlignment.Left;
        Assert.Equal(HorizontalAlignment.Left, next.HorizontalAlignment);
        chip.Classes.Remove("stat-ico");
        Settle();
        Assert.Equal(VerticalAlignment.Stretch, next.VerticalAlignment);
        Assert.NotNull(w);
    }

    [AvaloniaFact]
    public void No_global_rule_names_a_stat_class()
    {
        var found = LookCheck.GlobalSelectors()
            .Where(s => Regex.IsMatch(s, @"(?<![\w|])(Border|TextBlock|StackPanel)\.(stat|delta-ref)"))
            .Distinct().ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
