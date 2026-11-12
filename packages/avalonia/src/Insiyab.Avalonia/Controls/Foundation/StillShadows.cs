using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>A still window drops its soft shadows: the shadow tokens are taken again at the window with only their inset highlights and blur-free lines, so focus rings and error glows stay.</summary>
internal static class StillShadows
{
    // The tokens that carry a blurred drop.
    private static readonly string[] Keys =
    [
        "InsShSm", "InsShMd", "InsShLg", "InsShSubtle", "InsShPanel", "InsShGlass", "InsShBubble",
        "InsShBrandSm", "InsShBrandMd", "InsShBrandLg",
        "InsIslandShadow", "InsIslandShadowHover", "InsZonePillShadow", "InsTopbarFrostShadow",
        "InsSegOnShadow", "InsShellLinkActiveShadow",
    ];

    private sealed class Flat : ResourceDictionary;

    internal static void Set(TopLevel top, bool still)
    {
        var merged = top.Resources.MergedDictionaries;
        var flat = merged.OfType<Flat>().FirstOrDefault();
        if (still == (flat is not null)) return;
        if (flat is not null) { merged.Remove(flat); return; }
        merged.Add(Build(top));
    }

    private static Flat Build(TopLevel top)
    {
        var flat = new Flat();
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var themed = new ResourceDictionary();
            foreach (var key in Keys)
                if (top.TryFindResource(key, variant, out var value) && value is BoxShadows shadows)
                    themed[key] = Hard(shadows);
            flat.ThemeDictionaries[variant] = themed;
        }
        return flat;
    }

    // Keeps the inset highlights and the lines drawn without blur; drops every blurred drop.
    private static BoxShadows Hard(BoxShadows shadows)
    {
        var kept = new List<BoxShadow>();
        foreach (var shadow in shadows)
            if (shadow.IsInset || shadow.Blur == 0) kept.Add(shadow);
        return kept.Count switch
        {
            0 => default,
            1 => new BoxShadows(kept[0]),
            _ => new BoxShadows(kept[0], kept.Skip(1).ToArray()),
        };
    }
}
