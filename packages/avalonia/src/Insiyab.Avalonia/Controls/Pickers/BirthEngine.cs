using System.Globalization;
using System.Text.RegularExpressions;

namespace Insiyab.Avalonia.Controls;

/// <summary>A copy of what the birth date engine holds, kept while the pop-up is open so Cancel can put it back.</summary>
internal sealed class BirthSnapshot(List<(char Ch, bool Auto)>[] parts, bool[] set, int[] val)
{
    public List<(char Ch, bool Auto)>[] Parts { get; } = parts;
    public bool[] Set { get; } = set;
    public int[] Val { get; } = val;
}

// The typing rules of the birth date plugin (birthdate.js): which digits a part takes, when it moves on, what it fills in by itself. It has no UI.
internal sealed partial class BirthEngine
{
    private const string Order = "dmy";
    private static readonly int[] Len = [2, 2, 4];
    private static readonly System.Globalization.UmAlQuraCalendar Hijri = new();

    private readonly List<(char Ch, bool Auto)>[] _parts = [[], [], []];
    private readonly bool[] _set = new bool[3];
    private readonly int[] _val = new int[3];
    private readonly int[] _start = new int[3];
    private readonly string[] _all;
    private readonly DateTime _today;
    private readonly DateTime _first;

    public BirthEngine(int minAge, int maxAge, int startAge, Func<DateTime> today)
    {
        _today = today().Date;
        MinAge = Math.Max(0, minAge);
        var oldest = Math.Max(MinAge + 1, maxAge);
        LastYear = _today.Year - MinAge;
        FirstYear = _today.Year - oldest;
        Limit = new DateTime(LastYear, _today.Month, Math.Min(_today.Day, DateTime.DaysInMonth(LastYear, _today.Month)));
        _first = new DateTime(FirstYear, 1, 1);
        _start[0] = 16;
        _start[1] = 6;
        _start[2] = Math.Max(FirstYear, Math.Min(LastYear, _today.Year - startAge));
        var all = new List<string>((int)(Limit - _first).TotalDays + 1);
        for (var d = _first; d <= Limit; d = d.AddDays(1)) all.Add($"{d.Day:00}{d.Month:00}{d.Year}");
        _all = [.. all];
        Reset();
    }

    public int MinAge { get; }

    public int FirstYear { get; }

    public int LastYear { get; }

    /// <summary>The latest date that can be chosen: today, LastYear's.</summary>
    public DateTime Limit { get; }

    public DateTime Today => _today;

    /// <summary>The last Press was refused: its part should flash red.</summary>
    public bool Flashed => FlashedPart != '\0';

    /// <summary>The part the refused Press was for (after a hop to the next part, that part), or the null character.</summary>
    public char FlashedPart { get; private set; }

    /// <summary>The last change altered the number of days the day column shows.</summary>
    public bool DayItemsChanged { get; private set; }

    /// <summary>The days of the chosen month and year: 31 until the month is set, February of a leap year until the year is.</summary>
    public int DaysInMonth => _set[1] ? DateTime.DaysInMonth(_set[2] ? Math.Clamp(_val[2], 1, 9999) : 2000, Math.Clamp(_val[1], 1, 12)) : 31;

    /// <summary>The date, when all three parts are set, real and within the limits.</summary>
    public DateTime? Date => Real is { } d && d <= Limit && d >= _first ? d : null;

    private DateTime? Real => _set[0] && _set[1] && _set[2] ? PickerParse.RealGregorian(_val[2], _val[1], _val[0]) : null;

    private static int I(char k) => Order.IndexOf(k);

    private static char? Next(char k) => k == 'd' ? 'm' : k == 'm' ? 'y' : null;

    private static char? Prev(char k) => k == 'm' ? 'd' : k == 'y' ? 'm' : null;

    public string Str(char k) => string.Concat(_parts[I(k)].Select(c => c.Ch));

    public bool IsAuto(char k, int i) => _parts[I(k)][i].Auto;

    public bool IsSet(char k) => _set[I(k)];

    public int Val(char k) => _val[I(k)];

    public int Count(char k) => _parts[I(k)].Count;

    /// <summary>Whether any digit has been typed.</summary>
    public bool Any => _parts.Any(p => p.Count > 0);

    public int AgeOf(DateTime d)
    {
        var a = _today.Year - d.Year;
        if (_today.Month < d.Month || (_today.Month == d.Month && _today.Day < d.Day)) a--;
        return a;
    }

    private bool Possible(char k, string s)
    {
        var d = k == 'd' ? s : Str('d');
        var m = k == 'm' ? s : Str('m');
        var y = k == 'y' ? s : Str('y');
        foreach (var a in _all)
            if (a.AsSpan(0, 2).StartsWith(d) && a.AsSpan(2, 2).StartsWith(m) && a.AsSpan(4).StartsWith(y)) return true;
        return false;
    }

    private static List<(char Ch, bool Auto)> Chars(string s, bool auto) => [.. s.Select(c => (c, auto))];

    private void Begin()
    {
        FlashedPart = '\0';
        DayItemsChanged = false;
    }

    /// <summary>A digit typed into part <paramref name="k"/>; returns the part focus should move to, or null.</summary>
    public char? Press(char k, char digit)
    {
        Begin();
        return PressCore(k, digit);
    }

    private char? PressCore(char k, char digit)
    {
        var cur = _parts[I(k)];
        var s = Str(k);
        var key = digit.ToString();
        var tries = new List<List<(char Ch, bool Auto)>>();
        tries.Add([.. cur, .. Chars(key, false)]);
        if (k != 'y' && s.Length == 0) tries.Add([.. Chars("0", true), .. Chars(key, false)]);
        if (k == 'y' && s.Length == 0) tries.Add([.. Chars(digit == '0' ? "20" : "19", true), .. Chars(key, false)]);
        if (k == 'y' && s.Length == 1 && !cur[0].Auto)
            foreach (var c in new[] { "20", "19" }) tries.Add([.. Chars(c, true), .. cur, .. Chars(key, false)]);
        foreach (var t in tries)
        {
            var ns = string.Concat(t.Select(c => c.Ch));
            if (ns.Length <= Len[I(k)] && Possible(k, ns))
            {
                _parts[I(k)] = t;
                Changed();
                return ns.Length == Len[I(k)] ? Next(k) : null;
            }
        }
        if (Next(k) is { } next && s.Length == 1 && !cur[0].Auto && SeparateCore(k)) return PressCore(next, digit) ?? next;
        FlashedPart = k;
        return null;
    }

    /// <summary>A separator (slash, dot, Enter, Tab) after part <paramref name="k"/>: a lone digit gets its zero. Returns the part to focus, or null when nothing was done.</summary>
    public char? Separate(char k)
    {
        Begin();
        return SeparateCore(k) ? Next(k) : null;
    }

    private bool SeparateCore(char k)
    {
        var s = Str(k);
        if (k == 'y' || s.Length != 1 || !Possible(k, "0" + s)) return false;
        _parts[I(k)].Insert(0, ('0', true));
        Changed();
        return true;
    }

    private void Trim(char k)
    {
        var p = _parts[I(k)];
        while (p.Count > 0 && p[^1].Auto) p.RemoveAt(p.Count - 1);
    }

    /// <summary>Backspace in part <paramref name="k"/>; an empty part hands it to the one before. Returns the part that now has focus.</summary>
    public char Backspace(char k)
    {
        Begin();
        if (_parts[I(k)].Count == 0)
        {
            if (Prev(k) is not { } p) return k;
            k = p;
        }
        Trim(k);
        var list = _parts[I(k)];
        if (list.Count > 0) list.RemoveAt(list.Count - 1);
        Trim(k);
        Changed();
        return k;
    }

    // changed: a part at its full length is set to its value, a shorter one is not; a new month or year may change the days.
    private void Changed()
    {
        var before = DaysInMonth;
        var refill = false;
        for (var i = 0; i < 3; i++)
        {
            var s = Str(Order[i]);
            if (s.Length == Len[i] && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var v))
            {
                if (!_set[i] || _val[i] != v)
                {
                    _val[i] = v;
                    _set[i] = true;
                    if (i != 0) refill = true;
                }
            }
            else _set[i] = false;
        }
        Days(refill, before);
    }

    private void Days(bool refill, int before)
    {
        var now = DaysInMonth;
        if (refill || now != before)
            if (!_set[0] && _val[0] > now) _val[0] = now;
        if (now != before) DayItemsChanged = true;
    }

    /// <summary>A drum settled on <paramref name="value"/> for part <paramref name="k"/>: the part is set and padded; the day is kept inside the new month.</summary>
    public void Choose(char k, int value)
    {
        Begin();
        var before = DaysInMonth;
        var i = I(k);
        _val[i] = value;
        _set[i] = true;
        _parts[i] = Chars(k == 'y' ? value.ToString(CultureInfo.InvariantCulture) : value.ToString("00", CultureInfo.InvariantCulture), false);
        if (k != 'd')
        {
            var now = DaysInMonth;
            if (!_set[0] && _val[0] > now) _val[0] = now;
            if (_set[0])
            {
                _val[0] = Math.Min(_val[0], now);
                _parts[0] = Chars(_val[0].ToString("00", CultureInfo.InvariantCulture), false);
            }
        }
        if (DaysInMonth != before) DayItemsChanged = true;
    }

    public void FillAll(DateTime d)
    {
        Begin();
        var before = DaysInMonth;
        _parts[0] = Chars(d.Day.ToString("00", CultureInfo.InvariantCulture), false);
        _parts[1] = Chars(d.Month.ToString("00", CultureInfo.InvariantCulture), false);
        _parts[2] = Chars(d.Year.ToString(CultureInfo.InvariantCulture), false);
        Changed();
        if (DaysInMonth != before) DayItemsChanged = true;
    }

    public void Clear()
    {
        Begin();
        var before = DaysInMonth;
        Reset();
        if (DaysInMonth != before) DayItemsChanged = true;
    }

    private void Reset()
    {
        for (var i = 0; i < 3; i++)
        {
            _parts[i] = [];
            _set[i] = false;
            _val[i] = _start[i];
        }
    }

    public BirthSnapshot Save() => new([.. _parts.Select(p => new List<(char Ch, bool Auto)>(p))], [.. _set], [.. _val]);

    public void Restore(BirthSnapshot s)
    {
        Begin();
        var before = DaysInMonth;
        for (var i = 0; i < 3; i++)
        {
            _parts[i] = [.. s.Parts[i]];
            _set[i] = s.Set[i];
            _val[i] = s.Val[i];
        }
        if (DaysInMonth != before) DayItemsChanged = true;
    }

    /// <summary>What is wrong with the date as it stands, in the page's words: too young, too far back, unfinished, or missing. Empty when it is fine.</summary>
    public string Message(bool required, PickerLocale words)
    {
        if (Real is { } d)
        {
            if (d > Limit) return MinAge > 0 ? words.BirthYoung(MinAge) : words.BirthFar;
            if (d < _first) return words.BirthFar;
            return string.Empty;
        }
        return Any ? words.BirthIncomplete : required ? words.BirthRequired : string.Empty;
    }

    private static readonly (string Word, char Cal, int N)[] Names = BuildNames();

    private static (string Word, char Cal, int N)[] BuildNames()
    {
        var list = new List<(string Word, char Cal, int N)>();
        void Add(char cal, string[] months)
        {
            for (var i = 0; i < months.Length; i++)
                foreach (var w in months[i].Split('|')) list.Add((Clean(w), cal, i + 1));
        }
        Add('g', ["يناير|جانفي|كانون الثاني|january|jan", "فبراير|فيفري|شباط|february|feb", "مارس|اذار|march|mar", "ابريل|افريل|نيسان|april|apr",
            "مايو|ماي|ايار|may", "يونيو|يونيه|جوان|حزيران|june|jun", "يوليو|يوليه|جويليه|تموز|july|jul", "اغسطس|اوت|اب|august|aug",
            "سبتمبر|ايلول|september|sept|sep", "اكتوبر|تشرين الاول|october|oct", "نوفمبر|تشرين الثاني|november|nov", "ديسمبر|كانون الاول|december|dec"]);
        Add('h', ["محرم", "صفر", "ربيع الاول|ربيع اول", "ربيع الثاني|ربيع الاخر|ربيع ثاني", "جمادى الاولى|جمادى الاول|جمادى اولى",
            "جمادى الثانية|جمادى الاخرة|جمادى الثاني|جمادى الاخر", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة|ذي القعدة", "ذو الحجة|ذي الحجة"]);
        return [.. list.OrderByDescending(n => n.Word.Length)];
    }

    // clean: Latin digits, no vowel marks or tatweel, one spelling of alef, teh marbuta and alef maksura, separators as spaces, digits apart from words.
    private static string Clean(string s)
    {
        var t = PickerParse.ToLatin(s).ToLowerInvariant();
        t = Marks().Replace(t, string.Empty);
        t = AlefForms().Replace(t, "ا").Replace('ة', 'ه').Replace('ى', 'ي');
        t = Separators().Replace(t, " ");
        t = DigitThenOther().Replace(t, "$1 $2");
        t = OtherThenDigit().Replace(t, "$1 $2");
        return Spaces().Replace(t, " ").Trim();
    }

    /// <summary>Pasted text in any common form: digits, Egyptian, Levantine, Maghrebi or English month names, or a Hijri date.</summary>
    public static DateTime? Understand(string raw, DateTime today)
    {
        var t = " " + Clean(raw) + " ";
        var month = 0;
        var cal = '\0';
        foreach (var n in Names)
        {
            var at = t.IndexOf(" " + n.Word + " ", StringComparison.Ordinal);
            if (at < 0) continue;
            month = n.N;
            cal = n.Cal;
            t = t[..at] + " " + t[(at + n.Word.Length + 1)..];
            break;
        }
        if (t.Contains(" ه ") || t.Contains(" هجري ") || t.Contains(" هجريه ")) cal = 'h';
        var nums = Numbers().Matches(t).Select(m => m.Value).ToList();
        int d = 0, m2 = month;
        var ys = string.Empty;
        if (month > 0)
        {
            var big = nums.FirstOrDefault(n => n.Length >= 3 || Num(n) > 31);
            if (big is not null)
            {
                ys = big;
                var other = nums.FirstOrDefault(n => n != big);
                d = other is null ? 0 : Num(other);
            }
            else
            {
                d = nums.Count > 0 ? Num(nums[0]) : 0;
                ys = nums.Count > 1 ? nums[1] : string.Empty;
            }
        }
        else if (nums.Count >= 3)
        {
            if (nums[0].Length >= 3) { ys = nums[0]; m2 = Num(nums[1]); d = Num(nums[2]); }
            else { d = Num(nums[0]); m2 = Num(nums[1]); ys = nums[2]; }
        }
        if (d == 0 || m2 == 0 || m2 > 12 || !(ys.Length == 2 || ys.Length == 4)) return null;
        var y = Num(ys);
        if (ys.Length == 2)
        {
            if (cal == 'h')
            {
                y += 1400;
                if (y > HijriYear(today)) y -= 100;
            }
            else
            {
                y += 2000;
                if (y > today.Year) y -= 100;
            }
        }
        if (cal == '\0') cal = y < 1800 ? 'h' : 'g';
        if (cal == 'g') return PickerParse.RealGregorian(y, m2, d);
        try { return Hijri.ToDateTime(y, m2, d, 0, 0, 0, 0); }
        catch (ArgumentException) { return null; }
    }

    private static int HijriYear(DateTime today)
    {
        try { return Hijri.GetYear(today); }
        catch (ArgumentException) { return 1448; }
    }

    private static int Num(string s) => s.Length <= 9 && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : int.MaxValue;

    [GeneratedRegex("[\u064B-\u0652\u0640]")]
    private static partial Regex Marks();

    [GeneratedRegex("[\u0623\u0625\u0622]")]
    private static partial Regex AlefForms();

    [GeneratedRegex(@"[،,.\-/\\_|]+")]
    private static partial Regex Separators();

    [GeneratedRegex(@"([0-9])([^0-9\s])")]
    private static partial Regex DigitThenOther();

    [GeneratedRegex(@"([^0-9\s])([0-9])")]
    private static partial Regex OtherThenDigit();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex("[0-9]+")]
    private static partial Regex Numbers();
}
