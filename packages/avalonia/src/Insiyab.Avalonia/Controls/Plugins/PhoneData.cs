using System.Text;

namespace Insiyab.Avalonia.Controls;

/// <summary>A country the phone field knows: its two-letter code, its dial code, and its names.</summary>
internal sealed class PhoneCountry
{
    internal PhoneCountry(string iso, string dial, string[]? lead, string arabic, string english)
    {
        Iso = iso;
        Dial = dial;
        Lead = lead;
        ArabicName = arabic;
        EnglishName = english;
    }

    public string Iso { get; }
    public string Dial { get; }
    public string ArabicName { get; }
    public string EnglishName { get; }
    internal string[]? Lead { get; }

    public string Name(bool arabic) => arabic ? ArabicName : EnglishName;

    public override string ToString() => $"{Iso} +{Dial}";
}

// The phone plugin's data and number rules, ported from src/plugins/phone.js.
internal static partial class PhoneData
{
    internal sealed record Rule(int[] Lens, string Trunk, Dictionary<int, string>? Fmt, string Ex);

    internal sealed record Reading(PhoneCountry Country, string Nsn, bool Intl, bool Partial, int Cut, string Typed);

    public static readonly List<PhoneCountry> Countries = [];
    public static readonly Dictionary<string, PhoneCountry> ByIso = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, List<PhoneCountry>> ByDial = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, string> Primary = new()
    {
        ["1"] = "US", ["7"] = "RU", ["39"] = "IT", ["44"] = "GB", ["47"] = "NO", ["61"] = "AU",
        ["212"] = "MA", ["262"] = "RE", ["290"] = "SH", ["358"] = "FI", ["590"] = "GP", ["599"] = "CW",
    };

    private static Dictionary<int, string> F(params (int Len, string Pattern)[] list) => list.ToDictionary(x => x.Len, x => x.Pattern);

    private static readonly Dictionary<string, Rule> Rules = new()
    {
        ["SA"] = new([9], "0", F((9, "0XX XXX XXXX")), "501234567"),
        ["AE"] = new([8, 9], "0", F((8, "0X XXX XXXX"), (9, "0XX XXX XXXX")), "501234567"),
        ["KW"] = new([8], "", F((8, "XXXX XXXX")), "50012345"),
        ["QA"] = new([8], "", F((8, "XXXX XXXX")), "33123456"),
        ["BH"] = new([8], "", F((8, "XXXX XXXX")), "36001234"),
        ["OM"] = new([8], "", F((8, "XXXX XXXX")), "92123456"),
        ["JO"] = new([8, 9], "0", F((8, "0X XXX XXXX"), (9, "0XX XXX XXXX")), "790123456"),
        ["LB"] = new([7, 8], "0", F((7, "0X XXX XXX"), (8, "XX XXX XXX")), "71123456"),
        ["SY"] = new([8, 9], "0", F((8, "0XX XXX XXX"), (9, "0XXX XXX XXX")), "944567890"),
        ["IQ"] = new([8, 9, 10], "0", F((8, "0X XXX XXXX"), (9, "0XX XXX XXXX"), (10, "0XXX XXX XXXX")), "7912345678"),
        ["PS"] = new([8, 9], "0", F((8, "0X XXX XXXX"), (9, "0XX XXX XXXX")), "599123456"),
        ["YE"] = new([7, 8, 9], "0", F((7, "0X XXX XXX"), (8, "0X XXX XXXX"), (9, "XXX XXX XXX")), "712345678"),
        ["EG"] = new([8, 9, 10], "0", F((8, "0XX XXX XXX"), (9, "0XX XXX XXXX"), (10, "0XXX XXX XXXX")), "1001234567"),
        ["LY"] = new([8, 9], "0", F((8, "0XX XXX XXX"), (9, "0XX XXX XXXX")), "912345678"),
        ["SD"] = new([9], "0", F((9, "0XX XXX XXXX")), "911231234"),
        ["SS"] = new([9], "0", F((9, "0XX XXX XXXX")), "977123456"),
        ["TN"] = new([8], "", F((8, "XX XXX XXX")), "20123456"),
        ["DZ"] = new([8, 9], "0", F((8, "0XX XX XX XX"), (9, "0XXX XX XX XX")), "551234567"),
        ["MA"] = new([9], "0", F((9, "0X XX XX XX XX")), "612345678"),
        ["MR"] = new([8], "", F((8, "XX XX XX XX")), "22123456"),
        ["SO"] = new([7, 8, 9], "0", F((7, "X XXX XXX"), (8, "X XXX XXXX"), (9, "XX XXX XXXX")), "612345678"),
        ["DJ"] = new([8], "", F((8, "XX XX XX XX")), "77831001"),
        ["KM"] = new([7], "", F((7, "XXX XX XX")), "3212345"),
        ["US"] = new([10], "1", F((10, "(XXX) XXX-XXXX")), "2015550123"),
        ["GB"] = new([9, 10], "0", F((9, "0XXXX XXXXX"), (10, "0XXXX XXXXXX")), "7400123456"),
        ["FR"] = new([9], "0", F((9, "0X XX XX XX XX")), "612345678"),
        ["DE"] = new([6, 7, 8, 9, 10, 11, 12, 13], "0", null, "15123456789"),
        ["TR"] = new([10], "0", F((10, "0XXX XXX XX XX")), "5012345678"),
        ["IN"] = new([10], "0", F((10, "XXXXX XXXXX")), "8123456789"),
        ["PK"] = new([9, 10], "0", F((9, "0XX XXXXXXX"), (10, "0XXX XXXXXXX")), "3012345678"),
    };

    private static readonly HashSet<string> KeepZero = ["IT", "SM", "VA"];

    // Short and everyday names people type that the official names do not contain.
    public static readonly Dictionary<string, string> Alias = new()
    {
        ["AE"] = "uae emirates dubai abu dhabi امارات دبي ابوظبي", ["SA"] = "ksa saudi سعوديه", ["US"] = "usa america امريكا",
        ["GB"] = "uk britain england scotland wales بريطانيا انجلترا انكلترا",
        ["KW"] = "kuwait كويت", ["QA"] = "qatar قطر", ["BH"] = "bahrain بحرين", ["OM"] = "oman عمان", ["EG"] = "egypt مصر",
        ["JO"] = "jordan اردن", ["PS"] = "palestine فلسطين", ["SY"] = "syria سوريه", ["LB"] = "lebanon لبنان", ["IQ"] = "iraq عراق",
        ["YE"] = "yemen يمن", ["SD"] = "sudan سودان", ["MA"] = "morocco مغرب", ["DZ"] = "algeria جزائر", ["TN"] = "tunisia تونس",
        ["LY"] = "libya ليبيا", ["NL"] = "holland", ["KR"] = "korea south korea كوريا", ["CZ"] = "czech republic تشيك",
        ["CI"] = "ivory coast", ["TR"] = "turkey", ["RU"] = "russia", ["MM"] = "burma", ["CD"] = "drc", ["VA"] = "vatican",
    };

    static PhoneData()
    {
        var names = new Dictionary<string, (string Ar, string En)>(StringComparer.Ordinal);
        foreach (var row in Names.Split(';'))
        {
            var parts = row.Split('|');
            if (parts.Length == 3) names[parts[0]] = (parts[1], parts[2]);
        }
        foreach (var entry in Data.Split(','))
        {
            var iso = entry[..2];
            var rest = entry[2..];
            var colon = rest.IndexOf(':');
            var dial = colon < 0 ? rest : rest[..colon];
            var lead = colon < 0 ? null : rest[(colon + 1)..].Split('.');
            var (ar, en) = names.TryGetValue(iso, out var n) ? n : (iso, iso);
            var c = new PhoneCountry(iso, dial, lead, ar, en);
            Countries.Add(c);
            ByIso[iso] = c;
            if (!ByDial.TryGetValue(dial, out var list)) ByDial[dial] = list = [];
            list.Add(c);
        }
    }

    public static PhoneCountry? Find(string? iso) =>
        iso is not null && ByIso.TryGetValue(iso.Trim().ToUpperInvariant(), out var c) ? c : null;

    public static List<PhoneCountry> ParseList(string? list)
    {
        var result = new List<PhoneCountry>();
        if (string.IsNullOrWhiteSpace(list)) return result;
        foreach (var part in list.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries))
            if (Find(part) is { } c && !result.Contains(c)) result.Add(c);
        return result;
    }

    public static Rule? RuleOf(PhoneCountry c) =>
        Rules.TryGetValue(c.Iso, out var r) ? r : c.Dial == "1" ? Rules["US"] : null;

    public static int MaxLen(PhoneCountry c) => RuleOf(c) is { } r ? r.Lens[^1] : 15 - c.Dial.Length;

    private static string Digits(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s) if (ch >= '0' && ch <= '9') sb.Append(ch);
        return sb.ToString();
    }

    // The country an international number belongs to: dial codes are prefix-free, and a shared one is narrowed by what follows it.
    public static (PhoneCountry Country, string Nsn)? Detect(string digits)
    {
        for (var len = 1; len <= 3 && len <= digits.Length; len++)
        {
            if (!ByDial.TryGetValue(digits[..len], out var list)) continue;
            var rest = digits[len..];
            PhoneCountry? pick = null;
            foreach (var c in list)
            {
                if (c.Lead is null) continue;
                if (c.Lead.Any(l => rest.StartsWith(l, StringComparison.Ordinal))) { pick = c; break; }
            }
            pick ??= Primary.TryGetValue(list[0].Dial, out var p) ? ByIso[p] : list[0];
            return (pick, rest);
        }
        return null;
    }

    // What was typed, read as a number in a country: the national number, the country (another, after + or 00), and the leading digits cut.
    public static Reading Read(string? value, PhoneCountry country)
    {
        var t = PluginText.Latin(value).Trim();
        var digits = Digits(t);
        if (t.StartsWith('+') || t.StartsWith("00", StringComparison.Ordinal))
        {
            if (!t.StartsWith('+')) digits = digits[Math.Min(2, digits.Length)..];
            var d = Detect(digits);
            if (d is null) return new(country, string.Empty, true, digits.Length > 0 || t == "+", 0, digits);
            var nsn = d.Value.Nsn;
            var max = 15 - d.Value.Country.Dial.Length;
            return new(d.Value.Country, nsn.Length > max ? nsn[..max] : nsn, true, false, 0, digits);
        }
        var r = RuleOf(country);
        var cut = 0;
        if (r is not null && !r.Lens.Contains(digits.Length) && digits.StartsWith(country.Dial, StringComparison.Ordinal) &&
            r.Lens.Contains(digits.Length - country.Dial.Length))
            cut = country.Dial.Length;
        else if (r is not null
                     ? r.Trunk.Length > 0 && digits.StartsWith(r.Trunk, StringComparison.Ordinal) && (r.Trunk == "0" || digits.Length > MaxLen(country))
                     : digits.StartsWith('0') && !KeepZero.Contains(country.Iso))
            cut = r is not null ? r.Trunk.Length : 1;
        var national = digits[cut..];
        var limit = MaxLen(country);
        return new(country, national.Length > limit ? national[..limit] : national, false, false, cut, digits);
    }

    // The pattern for a number of this many digits so far: its own length's, or the next one up while it is still being typed.
    private static string? Pattern(PhoneCountry c, int n)
    {
        var r = RuleOf(c);
        if (r?.Fmt is null) return null;
        string? best = null;
        foreach (var len in r.Lens)
        {
            if (!r.Fmt.TryGetValue(len, out var f)) continue;
            best = f;
            if (len >= n) break;
        }
        return best;
    }

    // The national number written out, and where each of its digits ends up, so the caret can go back after the same digit.
    public static (string Text, List<int> At) Format(string nsn, PhoneCountry c)
    {
        var p = Pattern(c, nsn.Length);
        var r = RuleOf(c);
        var sb = new StringBuilder();
        var at = new List<int>();
        if (p is null)
        {
            if (r is { Trunk: "0" } && nsn.Length > 0) sb.Append('0');
            foreach (var ch in nsn) { sb.Append(ch); at.Add(sb.Length); }
            return (sb.ToString(), at);
        }
        var di = 0;
        for (var i = 0; i < p.Length && di < nsn.Length; i++)
        {
            if (p[i] == 'X') { sb.Append(nsn[di++]); at.Add(sb.Length); }
            else sb.Append(p[i]);
        }
        for (; di < nsn.Length; di++) { sb.Append(nsn[di]); at.Add(sb.Length); }
        return (sb.ToString(), at);
    }

    public static bool Valid(string nsn, PhoneCountry c) =>
        RuleOf(c) is { } r ? r.Lens.Contains(nsn.Length) : nsn.Length >= 4 && nsn.Length <= 15 - c.Dial.Length;

    public static string? Placeholder(PhoneCountry c) => RuleOf(c) is { } r ? Format(r.Ex, c).Text : null;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> SearchKeys = new(StringComparer.Ordinal);

    // Folded once per country: the list is rebuilt on every open.
    public static string SearchKey(PhoneCountry c) =>
        SearchKeys.GetOrAdd(c.Iso, _ => ArabicSearch.Normalize(string.Join(' ', c.ArabicName, c.EnglishName, c.Iso, Alias.GetValueOrDefault(c.Iso, string.Empty))));
}
