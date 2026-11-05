using System.Globalization;
using System.Text;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web library's Arabic-aware matching (<c>Insiyab.norm</c>) and the command palette's ranking.</summary>
internal static class ArabicSearch
{
    /// <summary>Compatibility forms unfolded, lower case, no marks (diacritics, the hamza forms of alef through the decomposition), no tatweel or invisible direction marks, ٱ as ا, ة as ه, ى and ی as ي, ک as ك, Arabic-Indic and Persian digits as Latin ones, and white space collapsed and trimmed.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        var space = false;
        foreach (var c in Unfold(text))
        {
            if (Dropped(c)) continue;
            var m = Map(c);
            if (char.IsWhiteSpace(m))
            {
                space = sb.Length > 0;
                continue;
            }
            if (space) sb.Append(' ');
            space = false;
            sb.Append(m);
        }
        return Composed(sb.ToString());
    }

    /// <summary>A text in compatibility form, lower case and decomposed, or lower case as typed when it holds a lone surrogate.</summary>
    internal static string Unfold(string text)
    {
        try { return text.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Normalize(NormalizationForm.FormD); }
        catch (ArgumentException) { return text.ToLowerInvariant(); }
    }

    private static string Composed(string text)
    {
        try { return text.Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { return text; }
    }

    /// <summary>Whether the fold leaves the character out: a mark, tatweel, or an invisible direction or width character.</summary>
    internal static bool Dropped(char c) =>
        c is 'ـ' or (>= '\u200B' and <= '\u200F') or (>= '\u202A' and <= '\u202E') or (>= '\u2066' and <= '\u2069') or '\uFEFF'
        || CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark;

    /// <summary>The letter the fold writes for <paramref name="c"/>.</summary>
    internal static char Map(char c) => c switch
    {
        'ٱ' => 'ا',
        'ة' => 'ه',
        'ى' or 'ی' => 'ي',
        'ک' => 'ك',
        >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
        >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
        _ => c,
    };

    /// <summary>Whether <paramref name="query"/>, normalised, occurs in any of <paramref name="fields"/>.</summary>
    public static bool Matches(string? query, params string?[] fields)
    {
        var q = Normalize(query);
        if (q.Length == 0) return true;
        foreach (var f in fields)
            if (Normalize(f).Contains(q, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>The search terms of a query: normalised, split on white space.</summary>
    public static string[] Terms(string? query) =>
        Normalize(query).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static bool Boundary(string hay, int at)
    {
        if (at < 0 || at >= hay.Length) return true;
        var ch = hay[at];
        return char.IsWhiteSpace(ch) || "-_/.,:;()«»\"'".IndexOf(ch) >= 0;
    }

    /// <summary>How well <paramref name="term"/> sits in <paramref name="hay"/> (both normalised): 100 whole, 80 at the start, 60 at a word start (also after «ال»), 40 inside, 0 absent.</summary>
    public static int Place(string hay, string term)
    {
        var at = hay.IndexOf(term, StringComparison.Ordinal);
        if (at == -1) return 0;
        if (hay == term) return 100;
        if (at == 0) return 80;
        for (var i = at; i != -1; i = hay.IndexOf(term, i + 1, StringComparison.Ordinal))
        {
            if (Boundary(hay, i - 1)) return 60;
            if (i >= 2 && hay[i - 2] == 'ا' && hay[i - 1] == 'ل' && Boundary(hay, i - 3)) return 60;
        }
        return 40;
    }

    /// <summary>The palette's score of an item for the terms: every term must be found in the name, the keywords or the group; 0 when one is not.</summary>
    public static int Score(string? label, string? keywords, string? group, IReadOnlyList<string> terms)
    {
        string name = Normalize(label), words = Normalize(keywords), where = Normalize(group);
        var total = 0;
        foreach (var term in terms)
        {
            var s = Place(name, term);
            if (s == 0)
            {
                var w = Place(words, term);
                s = w != 0 ? Math.Min(w, 60) / 2 : 0;
            }
            if (s == 0 && where.Contains(term, StringComparison.Ordinal)) s = 10;
            if (s == 0) return 0;
            total += s;
        }
        return total;
    }
}
