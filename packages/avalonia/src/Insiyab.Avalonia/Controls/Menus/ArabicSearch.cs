using System.Text;

namespace Insiyab.Avalonia.Controls;

/// <summary>The web library's Arabic-aware matching (<c>Insiyab.norm</c>) and the command palette's ranking.</summary>
public static class ArabicSearch
{
    /// <summary>Lower case, no diacritics or tatweel, ة as ه, ى as ي, Arabic-Indic and Persian digits as Latin ones; the hamza forms of alef fold to ا through the decomposition.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var s = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c is >= '̀' and <= 'ͯ' or >= 'ً' and <= 'ٟ' or 'ٰ' or 'ـ') continue;
            if (c == 'ة') sb.Append('ه');
            else if (c == 'ى') sb.Append('ي');
            else if (c is >= '٠' and <= '٩') sb.Append((char)('0' + (c - '٠')));
            else if (c is >= '۰' and <= '۹') sb.Append((char)('0' + (c - '۰')));
            else sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

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
