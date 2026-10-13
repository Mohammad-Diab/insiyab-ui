using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Brand;

namespace Insiyab.Avalonia.Tests;

public class BrandTests
{
    private static readonly Color Teal = Color.Parse("#0F766E");

    private static string FixturePath([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "Fixtures", "brand-parity.json");

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Fixture = new(() =>
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FixturePath()));
        var cases = new Dictionary<string, Dictionary<string, string>>();
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var tokens = new Dictionary<string, string>();
            foreach (var t in c.GetProperty("tokens").EnumerateObject())
                tokens[t.Name] = t.Value.GetString()!;
            cases[c.GetProperty("hex").GetString() + "|" + c.GetProperty("theme").GetString()] = tokens;
        }
        return cases;
    });

    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var key in Fixture.Value.Keys)
        {
            var parts = key.Split('|');
            data.Add(parts[0], parts[1]);
        }
        return data;
    }

    [Fact]
    public void Fixture_covers_both_themes_and_both_labels()
    {
        var all = Fixture.Value;
        Assert.True(all.Count >= 80, $"only {all.Count} cases");
        Assert.Contains(all.Values, t => t["on-primary"] == "#111111");
        Assert.Contains(all.Values, t => t["on-primary"] == "#ffffff");
        Assert.Contains(all.Keys, k => k.EndsWith("|dark"));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Derive_matches_the_web(string hex, string theme)
    {
        var expected = Fixture.Value[hex + "|" + theme];
        var actual = BrandDerivation.Derive(Color.Parse(hex), theme == "dark");

        Assert.Equal(Color.Parse(expected["primary"]), actual.Primary);
        Assert.Equal(Color.Parse(expected["primary-dark"]), actual.PrimaryDark);
        Assert.Equal(Color.Parse(expected["primary-darker"]), actual.PrimaryDarker);
        Assert.Equal(Triplet(expected["primary-rgb"]), actual.PrimaryRgb);
        Assert.Equal(Triplet(expected["grad-rgb"]), actual.GradRgb);
        Assert.Equal(Triplet(expected["accent-rgb"]), actual.AccentRgb);
        Assert.Equal(Color.Parse(expected["on-primary"]), actual.OnPrimary);
        Assert.Equal(Color.Parse(expected["wash-1"]), actual.Wash1);
        Assert.Equal(Color.Parse(expected["wash-2"]), actual.Wash2);
        Assert.Equal(Color.Parse(expected["primary-mid"]), actual.PrimaryMid);
        Assert.Equal(Color.Parse(expected["on-primary-mid"]), actual.OnPrimaryMid);
    }

    [Fact]
    public void Derive_ignores_alpha()
    {
        var opaque = BrandDerivation.Derive(Color.FromRgb(0x9B, 0x2C, 0x5E), false);
        var faded = BrandDerivation.Derive(Color.FromArgb(0x40, 0x9B, 0x2C, 0x5E), false);
        Assert.Equal(opaque, faded);
    }

    [AvaloniaFact]
    public void A_later_merged_dictionary_wins_over_an_earlier_one()
    {
        var early = new ResourceDictionary { ["K"] = "early-root" };
        early.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary { ["K"] = "early-dark" };
        var late = new ResourceDictionary();
        late.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary { ["K"] = "late-light" };
        late.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary { ["K"] = "late-dark" };
        var host = new ResourceDictionary();
        host.MergedDictionaries.Add(early);
        host.MergedDictionaries.Add(late);

        Assert.True(host.TryGetResource("K", ThemeVariant.Light, out var light));
        Assert.Equal("late-light", light);
        Assert.True(host.TryGetResource("K", ThemeVariant.Dark, out var dark));
        Assert.Equal("late-dark", dark);
    }

    [AvaloniaFact]
    public void Replacing_a_merged_theme_dictionary_reaches_a_dynamic_resource()
    {
        var theme = Application.Current!.Styles.OfType<InsiyabTheme>().First();
        var merged = theme.Resources.MergedDictionaries;
        static ResourceDictionary Probe(Color c)
        {
            var d = new ResourceDictionary();
            d.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary { ["BrandProbe"] = new SolidColorBrush(c) };
            d.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary { ["BrandProbe"] = new SolidColorBrush(c) };
            return d;
        }
        var first = Probe(Colors.Red);
        merged.Add(first);
        var border = new Border();
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable("BrandProbe"));
        var window = new Window { Content = border };
        window.Show();
        try
        {
            Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(border.Background).Color);
            merged[merged.IndexOf(first)] = Probe(Teal);
            Assert.Equal(Teal, Assert.IsAssignableFrom<ISolidColorBrush>(border.Background).Color);
        }
        finally
        {
            window.Close();
            merged.RemoveAt(merged.Count - 1);
        }
    }

    [AvaloniaFact]
    public void The_brand_dictionary_wins_inside_the_real_theme()
    {
        var theme = new InsiyabTheme();
        var rival = new ResourceDictionary { ["InsPrimaryColor"] = Colors.Red };
        rival.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary { ["InsPrimaryColor"] = Colors.Red };
        theme.Resources.MergedDictionaries.Insert(0, rival);
        theme.BrandPrimary = Teal;

        Assert.True(theme.TryGetResource("InsPrimaryColor", ThemeVariant.Light, out var value));
        Assert.Equal(Teal, value);
    }

    // The middle shade small accents take: the default primary 20% toward white on light, 45% on dark, with the label each needs.
    [Fact]
    public void The_middle_shade_shifts_toward_white_per_theme()
    {
        var light = BrandDerivation.Derive(InsiyabTheme.DefaultPrimary, false);
        var dark = BrandDerivation.Derive(InsiyabTheme.DefaultPrimary, true);
        Assert.Equal(Color.Parse("#704AAA"), light.PrimaryMid);
        Assert.Equal(Color.Parse("#9D83C5"), dark.PrimaryMid);
        Assert.Equal(BrandDerivation.Shift(InsiyabTheme.DefaultPrimary, BrandDerivation.MidShiftLight), light.PrimaryMid);
        Assert.Equal(BrandDerivation.Shift(InsiyabTheme.DefaultPrimary, BrandDerivation.MidShiftDark), dark.PrimaryMid);
        Assert.Equal(Colors.White, light.OnPrimaryMid);
        Assert.Equal(Color.Parse("#111111"), dark.OnPrimaryMid);
        Assert.True(BrandDerivation.Contrast(light.PrimaryMid, light.OnPrimaryMid) >= 4.5 && BrandDerivation.Contrast(dark.PrimaryMid, dark.OnPrimaryMid) >= 4.5);
        // The better of white and near-black never measures under about 4.35, which only a near-black primary on dark comes close to.
        foreach (var hex in Fixture.Value.Keys.Select(k => k.Split('|')[0]).Distinct())
        {
            foreach (var set in new[] { BrandDerivation.Derive(Color.Parse(hex), false), BrandDerivation.Derive(Color.Parse(hex), true) })
                Assert.True(BrandDerivation.Contrast(set.PrimaryMid, set.OnPrimaryMid) >= 4.35, $"{hex}: {set.OnPrimaryMid} on {set.PrimaryMid}");
        }
    }

    [AvaloniaFact]
    public void SetPrimary_follows_with_the_middle_shade()
    {
        var app = Application.Current!;
        try
        {
            Assert.True(app.TryGetResource("InsPrimaryMidColor", ThemeVariant.Light, out var before));
            Assert.Equal(Color.Parse("#704AAA"), before);
            InsiyabTheme.SetPrimary(Teal);
            Assert.True(app.TryGetResource("InsPrimaryMidColor", ThemeVariant.Light, out var light));
            Assert.Equal(BrandDerivation.Shift(Teal, BrandDerivation.MidShiftLight), light);
            Assert.True(app.TryGetResource("InsPrimaryMidColor", ThemeVariant.Dark, out var dark));
            Assert.Equal(BrandDerivation.Shift(Teal, BrandDerivation.MidShiftDark), dark);
            Assert.True(app.TryGetResource("InsOnPrimaryMid", ThemeVariant.Dark, out var label));
            Assert.Equal(BrandDerivation.LabelFor(BrandDerivation.Shift(Teal, BrandDerivation.MidShiftDark)), Assert.IsAssignableFrom<ISolidColorBrush>(label).Color);
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    [AvaloniaFact]
    public void SetPrimary_rederives_light_and_dark()
    {
        var app = Application.Current!;
        try
        {
            Assert.True(app.TryGetResource("InsPrimaryColor", ThemeVariant.Light, out var before));
            Assert.Equal(InsiyabTheme.DefaultPrimary, before);
            Assert.True(app.TryGetResource("InsToneBrandBgColor", ThemeVariant.Dark, out var toneBefore));

            InsiyabTheme.SetPrimary(Teal);

            Assert.Equal(Teal, InsiyabTheme.Primary);
            Assert.True(app.TryGetResource("InsPrimaryColor", ThemeVariant.Light, out var light));
            Assert.Equal(Teal, light);
            Assert.True(app.TryGetResource("InsPrimaryColor", ThemeVariant.Dark, out var dark));
            Assert.Equal(Teal, dark);
            Assert.True(app.TryGetResource("InsToneBrandBgColor", ThemeVariant.Dark, out var toneAfter));
            Assert.NotEqual(toneBefore, toneAfter);
            var tone = Assert.IsType<Color>(toneAfter);
            Assert.Equal((Teal.R, Teal.G, Teal.B), (tone.R, tone.G, tone.B));
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    [AvaloniaFact]
    public void SetPrimary_reaches_a_dynamic_resource_and_every_live_theme()
    {
        var other = new InsiyabTheme();
        var border = new Border();
        border.Bind(Border.BackgroundProperty, border.GetResourceObservable("InsPrimary"));
        var window = new Window { Content = border };
        window.Show();
        try
        {
            Assert.Equal(InsiyabTheme.DefaultPrimary, Assert.IsAssignableFrom<ISolidColorBrush>(border.Background).Color);

            InsiyabTheme.SetPrimary(Teal);

            Assert.Equal(Teal, Assert.IsAssignableFrom<ISolidColorBrush>(border.Background).Color);
            Assert.True(other.TryGetResource("InsPrimaryColor", ThemeVariant.Dark, out var fromOther));
            Assert.Equal(Teal, fromOther);
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void An_instance_primary_is_kept_through_SetPrimary()
    {
        var theme = new InsiyabTheme { BrandPrimary = Colors.Navy };
        try
        {
            InsiyabTheme.SetPrimary(Teal);
            Assert.True(theme.TryGetResource("InsPrimaryColor", ThemeVariant.Light, out var value));
            Assert.Equal(Colors.Navy, value);
        }
        finally
        {
            InsiyabTheme.SetPrimary(InsiyabTheme.DefaultPrimary);
        }
    }

    private static Color Triplet(string rgb)
    {
        var p = rgb.Split(',');
        return Color.FromRgb(byte.Parse(p[0].Trim()), byte.Parse(p[1].Trim()), byte.Parse(p[2].Trim()));
    }
}
