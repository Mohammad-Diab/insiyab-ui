using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Insiyab.Avalonia.Brand;
using Insiyab.Avalonia.Generated;

namespace Insiyab.Avalonia;

public partial class InsiyabTheme
{
    /// <summary>The primary colour every theme starts with, as on the web.</summary>
    public static Color DefaultPrimary { get; } = Color.FromRgb(0x4C, 0x1D, 0x95);

    private static readonly List<WeakReference<InsiyabTheme>> s_live = [];
    private static Color s_primary = DefaultPrimary;

    private ResourceDictionary? _brand;
    private Color? _brandPrimary;

    /// <summary>The app-wide primary colour last set by <see cref="SetPrimary"/>.</summary>
    public static Color Primary => s_primary;

    /// <summary>Re-derives the brand tokens for light and dark from one colour, in every live <see cref="InsiyabTheme"/>. Call on the UI thread.</summary>
    public static void SetPrimary(Color primary)
    {
        Dispatcher.UIThread.VerifyAccess();
        s_primary = Color.FromRgb(primary.R, primary.G, primary.B);
        foreach (var theme in LiveThemes())
        {
            if (theme._brandPrimary is null)
                theme.ApplyBrand(s_primary);
        }
    }

    /// <summary>A primary colour for this theme only; null follows <see cref="Primary"/>.</summary>
    public Color? BrandPrimary
    {
        get => _brandPrimary;
        set
        {
            _brandPrimary = value is { } c ? Color.FromRgb(c.R, c.G, c.B) : null;
            ApplyBrand(_brandPrimary ?? s_primary);
        }
    }

    partial void OnLoadedBrand()
    {
        lock (s_live)
            s_live.Add(new WeakReference<InsiyabTheme>(this));
        ApplyBrand(s_primary);
    }

    private static List<InsiyabTheme> LiveThemes()
    {
        var themes = new List<InsiyabTheme>();
        lock (s_live)
        {
            s_live.RemoveAll(w => !w.TryGetTarget(out _));
            foreach (var weak in s_live)
            {
                if (weak.TryGetTarget(out var theme))
                    themes.Add(theme);
            }
        }
        return themes;
    }

    // The brand dictionary is merged after Tokens.axaml, and a later merged dictionary wins over an earlier one.
    private void ApplyBrand(Color primary)
    {
        var next = CreateBrandDictionary(primary);
        AddGrounds(next);
        var merged = Resources.MergedDictionaries;
        var at = _brand is null ? -1 : merged.IndexOf(_brand);
        if (at >= 0)
            merged[at] = next;
        else
            merged.Add(next);
        _brand = next;
    }

    // A surface floating over the app's own content cannot blur it, so it takes the web's no-blur fallback: its glass laid on the opaque wash.
    private void AddGrounds(ResourceDictionary brand)
    {
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var target = (ResourceDictionary)brand.ThemeDictionaries[variant];
            var wash = (Color)target["InsWash1Color"]!;
            foreach (var key in new[] { "InsGlass", "InsGlassStrong" })
            {
                if (!Resources.TryGetResource(key + "Color", variant, out var value) || value is not Color glass) continue;
                var ground = Over(glass, wash);
                target[key + "Ground"] = new SolidColorBrush(ground);
                target[key + "GroundColor"] = ground;
            }
        }
    }

    internal static Color Over(Color top, Color under)
    {
        var a = top.A / 255.0;
        byte Mix(byte t, byte u) => (byte)Math.Round(t * a + u * (1 - a));
        return Color.FromRgb(Mix(top.R, under.R), Mix(top.G, under.G), Mix(top.B, under.B));
    }

    internal static ResourceDictionary CreateBrandDictionary(Color primary)
    {
        var light = new ResourceDictionary();
        BrandTokens.Apply(light, BrandDerivation.Derive(primary, false), false);
        var dark = new ResourceDictionary();
        BrandTokens.Apply(dark, BrandDerivation.Derive(primary, true), true);
        var brand = new ResourceDictionary();
        brand.ThemeDictionaries[ThemeVariant.Light] = light;
        brand.ThemeDictionaries[ThemeVariant.Dark] = dark;
        return brand;
    }
}
