namespace Insiyab.Avalonia.Controls;

/// <summary>How a <see cref="TimelineItem"/> writes its time.</summary>
public enum TimeMode
{
    /// <summary><see cref="TimelineItem.Time"/> as written.</summary>
    Text,

    /// <summary>As people say it: «قبل 5 دقائق», «أمس»; a date once it is a week away. The web's <c>data-ins-time</c>.</summary>
    Relative,

    /// <summary>Always the date. The web's <c>data-ins-time="date"</c>.</summary>
    Date,
}

/// <summary>The timeline plugin's times, in Arabic or English (or the app's words for them), written the way Intl.RelativeTimeFormat (numeric: auto) and Intl.DateTimeFormat write them, in Latin digits and the Gregorian calendar.</summary>
internal static class RelativeTime
{
    private enum Unit { Minute, Hour, Day, Week, Month, Year }

    private static readonly (Unit Unit, double Ms)[] Steps =
    [
        (Unit.Year, 31536000000), (Unit.Month, 2592000000), (Unit.Week, 604800000),
        (Unit.Day, 86400000), (Unit.Hour, 3600000), (Unit.Minute, 60000),
    ];

    private const double WeekMs = 7 * 86400000d;

    private static PickerLocale Words(bool arabic) => arabic ? PickerLocale.Arabic : PickerLocale.English;

    public static string Format(DateTime at, DateTime now, bool arabic, TimeMode mode = TimeMode.Relative, bool dayOnly = false) =>
        Format(at, now, Words(arabic), mode, dayOnly);

    /// <summary>The text for <paramref name="at"/> seen from <paramref name="now"/>; <paramref name="dayOnly"/> compares days, not hours ("yesterday", not "20 hours ago").</summary>
    public static string Format(DateTime at, DateTime now, PickerLocale words, TimeMode mode = TimeMode.Relative, bool dayOnly = false)
    {
        var diff = (at - now).TotalMilliseconds;
        if (mode == TimeMode.Date || Math.Abs(diff) >= WeekMs) return ShortDate(at, now, words);
        if (dayOnly) return Say((int)Math.Floor((at.Date - now.Date).TotalDays + 0.5), Unit.Day, words.Words);
        if (Math.Abs(diff) < 45000) return words.Words["time.now"];
        foreach (var (unit, ms) in Steps)
            if (Math.Abs(diff) >= ms || unit == Unit.Minute)
                return Say((int)Math.Floor(diff / ms + 0.5), unit, words.Words);
        return "";
    }

    /// <summary>"24 سبتمبر" or "September 24", with the year when it is not this year.</summary>
    public static string ShortDate(DateTime at, DateTime now, PickerLocale words)
    {
        var year = at.Year != now.Year;
        var month = words.MonthName(PickerCalendar.Gregorian, at.Month - 1);
        if (words.IsArabic) return year ? $"{at.Day} {month} {at.Year}" : $"{at.Day} {month}";
        return year ? $"{month} {at.Day}, {at.Year}" : $"{month} {at.Day}";
    }

    public static string DayHeading(DateTime day, DateTime now, bool arabic) => DayHeading(day, now, Words(arabic));

    /// <summary>A day's heading: «اليوم», «أمس» or «غدًا» (Today, Yesterday, Tomorrow), else the short date.</summary>
    public static string DayHeading(DateTime day, DateTime now, PickerLocale words)
    {
        var n = (int)Math.Round((day.Date - now.Date).TotalDays);
        if (n is < -1 or > 1) return ShortDate(day, now, words);
        var said = Say(n, Unit.Day, words.Words);
        return words.IsArabic || said.Length == 0 ? said : char.ToUpperInvariant(said[0]) + said[1..];
    }

    public static string FullDate(DateTime at, bool arabic, bool withTime = true) => FullDate(at, Words(arabic), withTime);

    /// <summary>The full date and time, for the tooltip: "الخميس، 24 سبتمبر 2026 في 8:05 ص".</summary>
    public static string FullDate(DateTime at, PickerLocale words, bool withTime = true)
    {
        var date = words.FullDay(PickerCalendar.Gregorian, at.Date);
        return withTime ? words.Words.Format("date.at", date, words.FormatTime(at.TimeOfDay)) : date;
    }

    private static string Say(int n, Unit unit, Speech words) => words.Arabic ? Arabic(n, unit, words) : Spoken(n, unit, words);

    private static string Spoken(int n, Unit unit, Speech words)
    {
        switch (unit, n)
        {
            case (Unit.Day, -1): return words["time.yesterday"];
            case (Unit.Day, 0): return words["time.today"];
            case (Unit.Day, 1): return words["time.tomorrow"];
            case (Unit.Minute, 0): return words["time.thisMinute"];
            case (Unit.Hour, 0): return words["time.thisHour"];
            case (Unit.Week, -1): return words["time.lastWeek"];
            case (Unit.Week, 0): return words["time.thisWeek"];
            case (Unit.Week, 1): return words["time.nextWeek"];
            case (Unit.Month, -1): return words["time.lastMonth"];
            case (Unit.Month, 0): return words["time.thisMonth"];
            case (Unit.Month, 1): return words["time.nextMonth"];
            case (Unit.Year, -1): return words["time.lastYear"];
            case (Unit.Year, 0): return words["time.thisYear"];
            case (Unit.Year, 1): return words["time.nextYear"];
        }
        var amount = words.Count(Key(unit), Math.Abs(n));
        return words.Format(n < 0 ? "time.ago" : "time.in", amount);
    }

    private static string Key(Unit unit) => unit switch
    {
        Unit.Minute => "time.minutes",
        Unit.Hour => "time.hours",
        Unit.Day => "time.days",
        Unit.Week => "time.weeks",
        Unit.Month => "time.months",
        _ => "time.years",
    };

    // Each unit's forms: one, two, few (3-10), many (11-99), other (100, 0, 101, 102).
    private static (string One, string Two, string Few, string Many, string Other) ArabicForms(Unit unit) => unit switch
    {
        Unit.Minute => ("دقيقة واحدة", "دقيقتين", "دقائق", "دقيقة", "دقيقة"),
        Unit.Hour => ("ساعة واحدة", "ساعتين", "ساعات", "ساعة", "ساعة"),
        Unit.Day => ("يوم واحد", "يومين", "أيام", "يومًا", "يوم"),
        Unit.Week => ("أسبوع واحد", "أسبوعين", "أسابيع", "أسبوعًا", "أسبوع"),
        Unit.Month => ("شهر واحد", "شهرين", "أشهر", "شهرًا", "شهر"),
        _ => ("سنة واحدة", "سنتين", "سنوات", "سنة", "سنة"),
    };

    // Arabic says the near days and the next and last units in words of their own, and counts the rest in its number forms; a phrase the app gives replaces the library's.
    private static string Arabic(int n, Unit unit, Speech words)
    {
        switch (unit, n)
        {
            case (Unit.Day, -2): return "أول أمس";
            case (Unit.Day, 2): return "بعد الغد";
            case (Unit.Day, >= -1 and <= 1):
            case (Unit.Minute or Unit.Hour, 0):
            case (Unit.Week or Unit.Month or Unit.Year, >= -1 and <= 1):
                return Spoken(n, unit, words);
        }
        var abs = Math.Abs(n);
        var amount = words.Count(Key(unit), abs, k =>
        {
            var f = ArabicForms(unit);
            var mod = k % 100;
            return k switch
            {
                1 => f.One,
                2 => f.Two,
                _ when mod is >= 3 and <= 10 => $"{k} {f.Few}",
                _ when mod is >= 11 and <= 99 => $"{k} {f.Many}",
                _ => $"{k} {f.Other}",
            };
        });
        return words.Format(n < 0 ? "time.ago" : "time.in", amount);
    }
}
