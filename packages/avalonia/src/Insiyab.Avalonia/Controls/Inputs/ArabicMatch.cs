namespace Insiyab.Avalonia.Controls;

/// <summary>
/// The autocomplete's Arabic-aware matching, as insiyab.js does it: diacritics, tatweel and hamza seats are ignored, taa marbuta meets haa and alef maqsura meets yaa, and Arabic-Indic digits meet Latin ones, so a name typed without its hamza still finds it.
/// </summary>
public static class ArabicMatch
{
    /// <summary>For <c>AutoCompleteBox.TextFilter</c>: true when the item contains the search text once both are normalised by <see cref="ArabicSearch.Normalize"/>.</summary>
    public static global::Avalonia.Controls.AutoCompleteFilterPredicate<string?> Contains { get; } = (search, item) =>
    {
        var q = ArabicSearch.Normalize(search);
        return q.Length == 0 || ArabicSearch.Normalize(item).Contains(q, StringComparison.Ordinal);
    };

    /// <summary>The text folded for a search: compatibility forms unfolded, lower case, no diacritics, tatweel or invisible direction marks, the hamza seats and ٱ as ا, ة as ه, ى and ی as ي, ک as ك, Arabic-Indic and Persian digits as Latin ones, white space collapsed and trimmed.</summary>
    public static string Normalize(string? text) => ArabicSearch.Normalize(text);

    /// <summary>True when every word of <paramref name="query"/>, folded by <see cref="Normalize"/>, is found anywhere in one of <paramref name="fields"/>, such as a name, its other-language name and a code; nothing typed matches everything.</summary>
    public static bool Matches(string? query, params string?[] fields)
    {
        var terms = ArabicSearch.Terms(query);
        if (terms.Length == 0) return true;
        var words = ArabicSearch.Normalize(string.Join(' ', fields.Where(f => !string.IsNullOrWhiteSpace(f))));
        foreach (var term in terms)
            if (!words.Contains(term, StringComparison.Ordinal)) return false;
        return true;
    }
}
