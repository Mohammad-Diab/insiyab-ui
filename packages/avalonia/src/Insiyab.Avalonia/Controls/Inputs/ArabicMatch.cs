using System.Text;

namespace Insiyab.Avalonia.Controls;

/// <summary>
/// The autocomplete's Arabic-aware matching, as insiyab.js does it: diacritics, tatweel and hamza seats are ignored, taa marbuta meets haa and alef maqsura meets yaa, and Arabic-Indic digits meet Latin ones, so a name typed without its hamza still finds it.
/// </summary>
public static class ArabicMatch
{
    /// <summary>For <c>AutoCompleteBox.TextFilter</c>: true when <paramref name="item"/> contains <paramref name="search"/> once both are normalised.</summary>
    public static readonly global::Avalonia.Controls.AutoCompleteFilterPredicate<string?> Contains = (search, item) =>
    {
        var q = Normalize(search);
        return q.Length == 0 || Normalize(item).Contains(q, StringComparison.Ordinal);
    };

    /// <summary>The form both sides are compared in.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (ch is >= '̀' and <= 'ͯ' or >= 'ً' and <= 'ٟ' or 'ٰ' or 'ـ') continue;
            sb.Append(ch switch
            {
                'ة' => 'ه',
                'ى' => 'ي',
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
                _ => ch,
            });
        }
        return sb.ToString().Trim();
    }
}
