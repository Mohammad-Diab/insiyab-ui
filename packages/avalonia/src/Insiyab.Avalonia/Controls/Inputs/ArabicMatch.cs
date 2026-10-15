namespace Insiyab.Avalonia.Controls;

/// <summary>
/// The autocomplete's Arabic-aware matching, as insiyab.js does it: diacritics, tatweel and hamza seats are ignored, taa marbuta meets haa and alef maqsura meets yaa, and Arabic-Indic digits meet Latin ones, so a name typed without its hamza still finds it.
/// </summary>
public static class ArabicMatch
{
    /// <summary>For <c>AutoCompleteBox.TextFilter</c>: true when the item contains the search text once both are normalised by <see cref="ArabicSearch.Normalize"/>.</summary>
    public static readonly global::Avalonia.Controls.AutoCompleteFilterPredicate<string?> Contains = (search, item) =>
    {
        var q = ArabicSearch.Normalize(search);
        return q.Length == 0 || ArabicSearch.Normalize(item).Contains(q, StringComparison.Ordinal);
    };
}
