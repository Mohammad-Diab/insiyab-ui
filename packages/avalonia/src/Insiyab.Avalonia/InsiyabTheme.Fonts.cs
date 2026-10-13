using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Insiyab.Avalonia;

public partial class InsiyabTheme
{
    private static FontManager? s_fontsOn;

    partial void OnLoadingFonts()
    {
        FontManager manager;
        try { manager = FontManager.Current; }
        catch (InvalidOperationException) { return; }
        if (ReferenceEquals(s_fontsOn, manager)) return;
        manager.AddFontCollection(new InsiyabFonts());
        s_fontsOn = manager;
    }
}

// The embedded faces stop at Bold, so a heavier weight takes the Bold face as a browser does, never a smeared synthetic bold.
internal sealed class InsiyabFonts() : EmbeddedFontCollection(new Uri("fonts:Insiyab"), new Uri("avares://Insiyab.Avalonia/Assets/Fonts"))
{
    public override bool TryCreateSyntheticGlyphTypeface(GlyphTypeface glyphTypeface, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface) =>
        base.TryCreateSyntheticGlyphTypeface(glyphTypeface, style, glyphTypeface.Weight, stretch, out syntheticGlyphTypeface);
}
