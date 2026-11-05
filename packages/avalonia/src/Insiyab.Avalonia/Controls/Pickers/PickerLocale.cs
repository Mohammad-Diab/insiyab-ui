using System.Globalization;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

// The words and date formats of the pickers, written out as Intl gives them for "ar" and "en" with Latin digits, so they do not depend on the machine's culture data; the app's language and words take their place when it sets them.
internal sealed class PickerLocale
{
    public static readonly PickerLocale Arabic = new(new Speech(true, false));
    public static readonly PickerLocale English = new(new Speech(false, false));

    // The app's words in Arabic and in English, made again after the app's language changes.
    private static PickerLocale? s_appAr, s_appEn;

    internal static void Forget() => s_appAr = s_appEn = null;

    private readonly string[] _gregorian, _shortGregorian, _hijri, _narrow, _long, _short;

    private PickerLocale(Speech words)
    {
        Words = words;
        IsArabic = words.Arabic;
        string[] Names(string key, int from, int count) => [.. Enumerable.Range(from, count).Select(i => words[key + i])];
        _gregorian = Names("date.month.", 1, 12);
        _shortGregorian = Names("date.monthShort.", 1, 12);
        _hijri = Names("date.hijriMonth.", 1, 12);
        _narrow = Names("date.weekdayNarrow.", 0, 7);
        _long = Names("date.weekday.", 0, 7);
        _short = Names("date.weekdayShort.", 0, 7);
        Today = words["date.today"];
        Clear = words["date.clear"];
        BadDate = words["date.badDate"];
        BadTime = words["date.badTime"];
        PickStart = words["date.pickStart"];
        PickEnd = words["date.pickEnd"];
        Done = words["date.done"];
        Cancel = words["date.cancel"];
        Choose = words["date.choose"];
        ChooseTime = words["date.chooseTime"];
        Calendar = words["date.calendar"];
        CalendarSystem = words["date.calendarSystem"];
        PrevMonth = words["date.prevMonth"];
        NextMonth = words["date.nextMonth"];
        Month = words["date.month"];
        Year = words["date.year"];
        Day = words["date.day"];
        Hour = words["date.hour"];
        Minute = words["date.minute"];
        Second = words["date.second"];
        Period = words["date.period"];
        Gregorian = words["date.gregorian"];
        HijriLabel = words["date.hijri"];
        Am = words["date.am"];
        Pm = words["date.pm"];
    }

    /// <summary>Every word these pickers say, by key.</summary>
    public Speech Words { get; }

    /// <summary>The locale a picker hands to the calendar or wheels it opens, so they speak as it does.</summary>
    public string Code => Words.App && Controls.Words.Language is { } l ? l : IsArabic ? "ar" : "en";

    public bool IsArabic { get; }
    public string Today { get; }
    public string Clear { get; }
    public string BadDate { get; }
    public string BadTime { get; }
    public string PickStart { get; }
    public string PickEnd { get; }
    public string Done { get; }
    public string Cancel { get; }
    public string Choose { get; }
    public string ChooseTime { get; }
    public string Calendar { get; }
    public string CalendarSystem { get; }
    public string PrevMonth { get; }
    public string NextMonth { get; }
    public string Month { get; }
    public string Year { get; }
    public string Day { get; }
    public string Hour { get; }
    public string Minute { get; }
    public string Second { get; }
    public string Period { get; }
    public string Gregorian { get; }
    public string HijriLabel { get; }
    public string Am { get; }
    public string Pm { get; }

    public string BirthStart => Words["birth.start"];

    public string BirthFar => Words["birth.far"];

    public string BirthIncomplete => Words["birth.incomplete"];

    public string BirthRequired => Words["birth.required"];

    public string BirthPick => Words["birth.pick"];

    public string BirthGroup => Words["birth.group"];

    // English lowers the parts' captions inside its sentence; the app's own words stay as written.
    public string BirthLeft(IEnumerable<string> list)
    {
        var joined = string.Join(Words["birth.and"], list);
        var own = Words.App && Controls.Words.Given("birth.left", IsArabic) is not null;
        return Words.Format("birth.left", IsArabic || own ? joined : joined.ToLowerInvariant());
    }

    public string BirthAge(int n) => Words.Count("birth.age", n, k => "العمر " + ArYears(k));

    public string BirthYoung(int n) => Words.Count("birth.young", n, k => "يجب أن يكون العمر " + ArYears(k) + " على الأقل");

    /// <summary>The placeholder of a part of the birth date: <paramref name="k"/> is d, m or y.</summary>
    public string BirthWord(char k) => Words[k switch { 'd' => "birth.dayHint", 'm' => "birth.monthHint", _ => "birth.yearHint" }];

    /// <summary>The caption and the accessible name of a part of the birth date.</summary>
    public string BirthCap(char k) => k switch { 'd' => Day, 'm' => Month, _ => Year };

    private static string ArYears(int n)
    {
        var r = n % 100;
        return n == 0 ? "أقل من سنة" : n == 1 ? "سنة واحدة" : n == 2 ? "سنتان" : N(n) + (r >= 3 && r <= 10 ? " سنوات" : " سنة");
    }

    public string Early(string date) => Words.Format("date.early", date);

    public string Late(string date) => Words.Format("date.late", date);

    // An explicit locale wins; otherwise the app's language, else the page's language follows its direction, as the docs' ?dir= switch does.
    public static PickerLocale For(string? locale, FlowDirection direction)
    {
        var words = string.IsNullOrEmpty(locale)
            ? new Speech(Controls.Words.Language is { } l ? Controls.Words.IsArabicCode(l) : direction == FlowDirection.RightToLeft, true)
            : Controls.Words.Of(null, locale);
        if (!words.App || Controls.Words.Version == 0) return words.Arabic ? Arabic : English;
        return words.Arabic ? s_appAr ??= new PickerLocale(words) : s_appEn ??= new PickerLocale(words);
    }

    // Saturday for Arabic, Sunday for English, as Intl's week info answers.
    public DayOfWeek WeekStart => IsArabic ? DayOfWeek.Saturday : DayOfWeek.Sunday;

    public bool MonthFirst => !IsArabic;

    public string Narrow(DayOfWeek d) => _narrow[(int)d];

    public string LongDay(DayOfWeek d) => _long[(int)d];

    public string ShortDay(DayOfWeek d) => _short[(int)d];

    public string MonthName(PickerCalendar cal, int m) =>
        (cal.IsGregorian ? _gregorian : _hijri)[((m % 12) + 12) % 12];

    /// <summary>A Gregorian month as Intl's short month gives it: Jan in English, the whole name in Arabic.</summary>
    public string ShortMonth(int m) => _shortGregorian[((m % 12) + 12) % 12];

    private DateTimeFormatInfo? _format;

    /// <summary>These words as .NET date formatting, Gregorian with Latin digits, for Avalonia's own pickers.</summary>
    public DateTimeFormatInfo Format => _format ??= BuildFormat();

    private DateTimeFormatInfo BuildFormat()
    {
        var ar = IsArabic;
        var f = (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();
        string[] months = [.. _gregorian, ""];
        string[] shortMonths = [.. _shortGregorian, ""];
        f.MonthNames = months;
        f.MonthGenitiveNames = months;
        f.AbbreviatedMonthNames = shortMonths;
        f.AbbreviatedMonthGenitiveNames = shortMonths;
        f.DayNames = _long;
        f.AbbreviatedDayNames = _short;
        f.ShortestDayNames = _narrow;
        f.AMDesignator = Am;
        f.PMDesignator = Pm;
        f.ShortDatePattern = ar ? "d/M/yyyy" : "M/d/yyyy";
        f.LongDatePattern = ar ? "dddd، d MMMM yyyy" : "dddd, MMMM d, yyyy";
        f.YearMonthPattern = "MMMM yyyy";
        f.MonthDayPattern = ar ? "d MMMM" : "MMMM d";
        f.FirstDayOfWeek = ar ? DayOfWeek.Saturday : DayOfWeek.Sunday;
        return DateTimeFormatInfo.ReadOnly(f);
    }

    public string YearText(PickerCalendar cal, int y) =>
        cal.IsGregorian ? N(y) : Words.Format("date.hijriYear", N(y));

    public string MonthYear(PickerCalendar cal, DateTime d)
    {
        var p = cal.Parts(d);
        return $"{MonthName(cal, p.M)} {YearText(cal, p.Y)}";
    }

    public string FormatDay(PickerCalendar cal, DateTime d)
    {
        var p = cal.Parts(d);
        return IsArabic ? $"{N(p.D)} {MonthName(cal, p.M)} {YearText(cal, p.Y)}" : $"{MonthName(cal, p.M)} {N(p.D)}, {YearText(cal, p.Y)}";
    }

    public string FullDay(PickerCalendar cal, DateTime d) =>
        IsArabic ? $"{LongDay(d.DayOfWeek)}، {FormatDay(cal, d)}" : $"{LongDay(d.DayOfWeek)}, {FormatDay(cal, d)}";

    // Intl's formatRange: the shared month and year said once.
    public string FormatRange(PickerCalendar cal, DateTime a, DateTime b)
    {
        if (a == b) return FormatDay(cal, a);
        var p = cal.Parts(a);
        var q = cal.Parts(b);
        if (p.Y == q.Y && p.M == q.M)
            return IsArabic
                ? $"{N(p.D)}–{N(q.D)} {MonthName(cal, p.M)} {YearText(cal, p.Y)}"
                : $"{MonthName(cal, p.M)} {N(p.D)}{ThinDash}{N(q.D)}, {YearText(cal, p.Y)}";
        if (p.Y == q.Y)
            return IsArabic
                ? $"{N(p.D)} {MonthName(cal, p.M)} – {N(q.D)} {MonthName(cal, q.M)} {YearText(cal, p.Y)}"
                : $"{MonthName(cal, p.M)} {N(p.D)}{ThinDash}{MonthName(cal, q.M)} {N(q.D)}, {YearText(cal, p.Y)}";
        return IsArabic ? $"{FormatDay(cal, a)} – {FormatDay(cal, b)}" : $"{FormatDay(cal, a)}{ThinDash}{FormatDay(cal, b)}";
    }

    // English ranges set the dash between thin spaces.
    private static readonly string ThinDash = (char)0x2009 + "–" + (char)0x2009;

    public string FormatDateTime(PickerCalendar cal, DateTime d, TimeSpan t) =>
        Words.Format("date.at", FormatDay(cal, d), FormatTime(t));

    public string FormatTime(TimeSpan t)
    {
        var h = t.Hours;
        var h12 = h % 12 == 0 ? 12 : h % 12;
        return $"{N(h12)}:{t.Minutes:00} {(h < 12 ? Am : Pm)}";
    }

    public static string N(int n) => n.ToString(CultureInfo.InvariantCulture);
}
