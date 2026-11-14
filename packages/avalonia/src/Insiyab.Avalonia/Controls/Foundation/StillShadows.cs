using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

internal enum ShadowMode
{
    Full,
    Simple,
    Flat,
}

/// <summary>The shadow tokens taken again at the window: a still window drops its soft shadows, keeping their inset highlights and blur-free lines so focus rings and error glows stay; light effects keep one smaller drop of each.</summary>
internal static class StillShadows
{
    // The tokens that carry a blurred drop.
    private static readonly string[] Keys =
    [
        "InsShSm", "InsShMd", "InsShLg", "InsShSubtle", "InsShPanel", "InsShGlass", "InsShBubble",
        "InsShBrandSm", "InsShBrandMd", "InsShBrandLg",
        "InsIslandShadow", "InsIslandShadowHover", "InsZonePillShadow", "InsTopbarFrostShadow",
        "InsSegOnShadow", "InsShellLinkActiveShadow", "InsBottomNavShadow", "InsNavRailShadow", "InsRefreshShadow",
    ];

    // The most blur a light drop keeps.
    internal const double LightBlur = 12;

    private sealed class Flat : ResourceDictionary;

    private sealed class Simple : ResourceDictionary;

    internal static void Set(TopLevel top, ShadowMode mode)
    {
        var merged = top.Resources.MergedDictionaries;
        var flat = merged.OfType<Flat>().FirstOrDefault();
        var simple = merged.OfType<Simple>().FirstOrDefault();
        if (mode != ShadowMode.Flat && flat is not null) merged.Remove(flat);
        if (mode != ShadowMode.Simple && simple is not null) merged.Remove(simple);
        if (mode == ShadowMode.Flat && flat is null) merged.Add(Build(top, new Flat(), Hard));
        if (mode == ShadowMode.Simple && simple is null) merged.Add(Build(top, new Simple(), Light));
    }

    private static ResourceDictionary Build(TopLevel top, ResourceDictionary into, Func<BoxShadows, BoxShadows> take)
    {
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var themed = new ResourceDictionary();
            foreach (var key in Keys)
                if (top.TryFindResource(key, variant, out var value) && value is BoxShadows shadows)
                    themed[key] = take(shadows);
            into.ThemeDictionaries[variant] = themed;
        }
        return into;
    }

    // Keeps the inset highlights and the lines drawn without blur; drops every blurred drop.
    private static BoxShadows Hard(BoxShadows shadows) => Join(Kept(shadows));

    // Keeps what Hard keeps and the widest blurred drop, its blur held to LightBlur.
    private static BoxShadows Light(BoxShadows shadows)
    {
        var widest = -1;
        for (var i = 0; i < shadows.Count; i++)
            if (!shadows[i].IsInset && shadows[i].Blur > 0 && (widest < 0 || shadows[i].Blur > shadows[widest].Blur)) widest = i;
        var kept = new List<BoxShadow>();
        for (var i = 0; i < shadows.Count; i++)
        {
            var shadow = shadows[i];
            if (i == widest) shadow.Blur = Math.Min(shadow.Blur, LightBlur);
            if (i == widest || shadow.IsInset || shadow.Blur == 0) kept.Add(shadow);
        }
        return Join(kept);
    }

    private static List<BoxShadow> Kept(BoxShadows shadows)
    {
        var kept = new List<BoxShadow>();
        foreach (var shadow in shadows)
            if (shadow.IsInset || shadow.Blur == 0) kept.Add(shadow);
        return kept;
    }

    private static BoxShadows Join(List<BoxShadow> kept) => kept.Count switch
    {
        0 => default,
        1 => new BoxShadows(kept[0]),
        _ => new BoxShadows(kept[0], kept.Skip(1).ToArray()),
    };
}
