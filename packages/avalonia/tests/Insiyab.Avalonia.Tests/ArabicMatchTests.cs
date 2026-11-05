using Avalonia.Headless.XUnit;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Tests;

public class ArabicMatchTests
{
    [AvaloniaTheory]
    [InlineData("أَحْمَد", "احمد")]
    [InlineData("إيمان آمال", "ايمان امال")]
    [InlineData("مدرسة", "مدرسه")]
    [InlineData("مستشفى", "مستشفي")]
    [InlineData("کتاب فارسی", "كتاب فارسي")]
    [InlineData("ٱلرحمن", "الرحمن")]
    [InlineData("كـتـاب", "كتاب")]
    [InlineData("الطلب ١٢٣ و۴۵", "الطلب 123 و45")]
    [InlineData("  Two   Words\t", "two words")]
    [InlineData("‏نص⁦عربي⁩", "نصعربي")]
    [InlineData("ﻻ", "لا")]
    [InlineData("Café", "cafe")]
    [InlineData(null, "")]
    public void Normalize_folds_spellings_that_should_meet(string? text, string folded)
    {
        Assert.Equal(folded, ArabicMatch.Normalize(text));
    }

    [AvaloniaFact]
    public void Normalize_keeps_a_lone_surrogate_as_typed()
    {
        Assert.Equal("a\uD800b", ArabicMatch.Normalize("A\uD800B"));
    }

    [AvaloniaFact]
    public void Every_typed_word_is_found_anywhere_in_the_fields()
    {
        Assert.True(ArabicMatch.Matches("احمد علي", "علي أحمد"));
        Assert.True(ArabicMatch.Matches("601", "مبادئ الحاسب", "MDA601"));
        Assert.True(ArabicMatch.Matches("mda حاسب", "مبادئ الحاسب", "MDA601"));
        Assert.True(ArabicMatch.Matches("حديقه", null, "حديقة البيت"));
        Assert.False(ArabicMatch.Matches("احمد سامي", "علي أحمد"));
        Assert.False(ArabicMatch.Matches("سامي"));
    }

    [AvaloniaFact]
    public void Nothing_typed_matches_everything()
    {
        Assert.True(ArabicMatch.Matches(null, "علي"));
        Assert.True(ArabicMatch.Matches("  ", "علي"));
        Assert.True(ArabicMatch.Matches("ً", "علي"));
    }

    [AvaloniaFact]
    public void Find_folds_text_as_the_helper_does()
    {
        Assert.Single(Find.Matches("کتاب", "كتاب"));
        Assert.Equal((0, 4), Find.Matches("ٱلله", "الله").Single());
        Assert.Equal((0, 8), Find.Matches("نص  عربي", "نص عربي").Single());
    }
}
