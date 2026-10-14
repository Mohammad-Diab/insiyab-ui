using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Insiyab.Avalonia.Controls;

// What a person typed into a date or time field, read the way insiyab.js reads it.
internal static partial class PickerParse
{
    public static string ToLatin(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var b = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c >= '٠' && c <= '٩') b.Append((char)('0' + c - '٠'));
            else if (c >= '۰' && c <= '۹') b.Append((char)('0' + c - '۰'));
            else b.Append(c);
        }
        return b.ToString();
    }

    public static List<string> Numbers(string s) => [.. Digits().Matches(s).Select(m => m.Value)];

    public static DateTime? RealGregorian(int y, int m, int d) =>
        y is >= 1 and <= 9999 && m is >= 1 and <= 12 && d >= 1 && d <= DateTime.DaysInMonth(y, m) ? new DateTime(y, m, d) : null;

    public static DateTime? ParseIso(string? s)
    {
        var m = Iso().Match(s ?? string.Empty);
        if (!m.Success) return null;
        return RealGregorian(I(m.Groups[1].Value), I(m.Groups[2].Value), I(m.Groups[3].Value));
    }

    public static string IsoOf(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string HhMm(TimeSpan t) => $"{t.Hours:00}:{t.Minutes:00}";

    // An ISO date, or three numbers in the locale's order, read in the calendar on screen; anything else is not guessed at.
    public static DateTime? ParseDate(string text, PickerCalendar cal, PickerLocale locale)
    {
        var s = ToLatin(text).Trim();
        var iso = ParseIso(s);
        if (iso is not null && cal.IsGregorian) return iso;
        var own = cal.Parse(s, out var handled);
        if (handled) return own;
        if (iso is not null) return iso;
        var parts = Numbers(s);
        if (parts.Count != 3) return null;
        int y, m, d;
        if (parts[0].Length == 4) { y = I(parts[0]); m = I(parts[1]); d = I(parts[2]); }
        else if (locale.MonthFirst) { m = I(parts[0]); d = I(parts[1]); y = I(parts[2]); }
        else { d = I(parts[0]); m = I(parts[1]); y = I(parts[2]); }
        if (cal.IsGregorian && y < 100) y += 2000;
        if (m < 1 || m > 12 || d < 1 || d > 31) return null;
        if (cal.IsGregorian) return RealGregorian(y, m, d);
        var outDate = cal.FromParts(y, m - 1, d);
        var back = cal.Parts(outDate);
        return back.Y == y && back.M == m - 1 && back.D == d ? outDate : null;
    }

    // "14:30", "1430", "2:30 م", "2 pm", in either set of digits.
    public static TimeSpan? ParseTime(string? text)
    {
        var t = ToLatin(text).ToLowerInvariant().Replace((char)0x200E, ' ').Replace((char)0x200F, ' ').Replace((char)0x202F, ' ').Trim();
        if (t.Length == 0) return null;
        var pm = PmMark().IsMatch(t);
        var am = AmMark().IsMatch(t);
        int h, min;
        var m = Clock().Match(t);
        if (m.Success) { h = I(m.Groups[1].Value); min = I(m.Groups[2].Value); }
        else
        {
            var digits = Digits().Match(t);
            if (!digits.Success || digits.Value.Length > 4) return null;
            var v = digits.Value;
            if (v.Length > 2) { h = I(v[..^2]); min = I(v[^2..]); }
            else { h = I(v); min = 0; }
        }
        if ((pm || am) && (h < 1 || h > 12)) return null;
        if (pm && h < 12) h += 12;
        if (am && h == 12) h = 0;
        if (h > 23 || min > 59) return null;
        return new TimeSpan(h, min, 0);
    }

    // A date and a time typed together: the time found and taken out, the rest read as the day.
    public static (DateTime? Day, TimeSpan? Time) ParseDateTime(string text, PickerCalendar cal, PickerLocale locale)
    {
        var latin = ToLatin(text);
        var tm = TimeInText().Match(latin);
        TimeSpan? at = tm.Success ? ParseTime(tm.Value) : null;
        var rest = tm.Success ? TrailingJoin().Replace(latin.Replace(tm.Value, string.Empty), string.Empty).Trim() : latin;
        return (ParseDate(rest, cal, locale), at);
    }

    private static int I(string s) => Int(s);

    private static int Longest(string s) => Digits().Matches(s).Select(m => m.Length).DefaultIfEmpty(0).Max();

    // A typed number; one too long for an int, or in digits int cannot read, comes back as int.MaxValue, which every range check refuses.
    internal static int Int(string s) => s.Length <= 9 && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : int.MaxValue;

    // Whether typing <paramref name="input"/> over the box's selection would make a run of digits longer than four, or than the longest already there.
    public static bool Overlong(global::Avalonia.Controls.TextBox box, string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        var text = box.Text ?? string.Empty;
        var a = Math.Clamp(Math.Min(box.SelectionStart, box.SelectionEnd), 0, text.Length);
        var b = Math.Clamp(Math.Max(box.SelectionStart, box.SelectionEnd), 0, text.Length);
        if (a == b) a = b = Math.Clamp(box.CaretIndex, 0, text.Length);
        return Longest(text[..a] + input + text[b..]) > Math.Max(4, Longest(text));
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();

    [GeneratedRegex(@"^(\d{4})-(\d{2})-(\d{2})$")]
    private static partial Regex Iso();

    [GeneratedRegex(@"(\d{1,2})\s*[:.٫]\s*(\d{2})")]
    private static partial Regex Clock();

    [GeneratedRegex(@"(pm|p\.m|م|مساء)")]
    private static partial Regex PmMark();

    [GeneratedRegex(@"(am|a\.m|ص|صباح)")]
    private static partial Regex AmMark();

    [GeneratedRegex(@"\d{1,2}\s*[:.٫]\s*\d{2}(\s*(am|pm|a\.m\.|p\.m\.|صباحًا|صباحا|مساءً|مساء|ص|م))?", RegexOptions.IgnoreCase)]
    private static partial Regex TimeInText();

    [GeneratedRegex(@"(في|at|،|,)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingJoin();
}
