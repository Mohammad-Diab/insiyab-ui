using System.Globalization;

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

/// <summary>The timeline plugin's times, in Arabic or English, written the way Intl.RelativeTimeFormat (numeric: auto) and Intl.DateTimeFormat write them, in Latin digits and the Gregorian calendar.</summary>
internal static class RelativeTime
{
    private static readonly string[] ArMonths = ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];
    private static readonly string[] ArDays = ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];

    private enum Unit { Minute, Hour, Day, Week, Month, Year }

    private static readonly (Unit Unit, double Ms)[] Steps =
    [
        (Unit.Year, 31536000000), (Unit.Month, 2592000000), (Unit.Week, 604800000),
        (Unit.Day, 86400000), (Unit.Hour, 3600000), (Unit.Minute, 60000),
    ];

    private const double WeekMs = 7 * 86400000d;

    /// <summary>The text for <paramref name="at"/> seen from <paramref name="now"/>; <paramref name="dayOnly"/> compares days, not hours ("yesterday", not "20 hours ago").</summary>
    public static string Format(DateTime at, DateTime now, bool arabic, TimeMode mode = TimeMode.Relative, bool dayOnly = false)
    {
        var diff = (at - now).TotalMilliseconds;
        if (mode == TimeMode.Date || Math.Abs(diff) >= WeekMs) return ShortDate(at, now, arabic);
        if (dayOnly) return Say((int)Math.Floor((at.Date - now.Date).TotalDays + 0.5), Unit.Day, arabic);
        if (Math.Abs(diff) < 45000) return arabic ? "الآن" : "now";
        foreach (var (unit, ms) in Steps)
            if (Math.Abs(diff) >= ms || unit == Unit.Minute)
                return Say((int)Math.Floor(diff / ms + 0.5), unit, arabic);
        return "";
    }

    /// <summary>"24 سبتمبر" or "September 24", with the year when it is not this year.</summary>
    public static string ShortDate(DateTime at, DateTime now, bool arabic)
    {
        var year = at.Year != now.Year;
        if (arabic) return year ? $"{at.Day} {ArMonths[at.Month - 1]} {at.Year}" : $"{at.Day} {ArMonths[at.Month - 1]}";
        var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(at.Month);
        return year ? $"{month} {at.Day}, {at.Year}" : $"{month} {at.Day}";
    }

    /// <summary>The full date and time, for the tooltip: "الخميس، 24 سبتمبر 2026 في 8:05 ص".</summary>
    public static string FullDate(DateTime at, bool arabic, bool withTime = true)
    {
        var inv = CultureInfo.InvariantCulture;
        var h = at.Hour % 12 == 0 ? 12 : at.Hour % 12;
        if (arabic)
        {
            var date = $"{ArDays[(int)at.DayOfWeek]}، {at.Day} {ArMonths[at.Month - 1]} {at.Year}";
            return withTime ? $"{date} في {h}:{at.Minute:00} {(at.Hour < 12 ? "ص" : "م")}" : date;
        }
        var en = $"{inv.DateTimeFormat.GetDayName(at.DayOfWeek)}, {inv.DateTimeFormat.GetMonthName(at.Month)} {at.Day}, {at.Year}";
        return withTime ? $"{en} at {h}:{at.Minute:00} {(at.Hour < 12 ? "AM" : "PM")}" : en;
    }

    private static string Say(int n, Unit unit, bool arabic) => arabic ? Arabic(n, unit) : English(n, unit);

    private static string English(int n, Unit unit)
    {
        var name = unit.ToString().ToLowerInvariant();
        switch (unit, n)
        {
            case (Unit.Day, -1): return "yesterday";
            case (Unit.Day, 0): return "today";
            case (Unit.Day, 1): return "tomorrow";
            case (Unit.Week or Unit.Month or Unit.Year, -1): return "last " + name;
            case (Unit.Week or Unit.Month or Unit.Year, 1): return "next " + name;
            case (_, 0): return "this " + name;
        }
        var abs = Math.Abs(n);
        var words = $"{abs} {name}{(abs == 1 ? "" : "s")}";
        return n < 0 ? words + " ago" : "in " + words;
    }

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

    private static string Arabic(int n, Unit unit)
    {
        switch (unit, n)
        {
            case (Unit.Minute, 0): return "هذه الدقيقة";
            case (Unit.Hour, 0): return "الساعة الحالية";
            case (Unit.Day, -2): return "أول أمس";
            case (Unit.Day, -1): return "أمس";
            case (Unit.Day, 0): return "اليوم";
            case (Unit.Day, 1): return "غدًا";
            case (Unit.Day, 2): return "بعد الغد";
            case (Unit.Week, -1): return "الأسبوع الماضي";
            case (Unit.Week, 0): return "هذا الأسبوع";
            case (Unit.Week, 1): return "الأسبوع القادم";
            case (Unit.Month, -1): return "الشهر الماضي";
            case (Unit.Month, 0): return "هذا الشهر";
            case (Unit.Month, 1): return "الشهر القادم";
            case (Unit.Year, -1): return "السنة الماضية";
            case (Unit.Year, 0): return "السنة الحالية";
            case (Unit.Year, 1): return "السنة القادمة";
        }
        var abs = Math.Abs(n);
        var f = ArabicForms(unit);
        var mod = abs % 100;
        var words = abs switch
        {
            1 => f.One,
            2 => f.Two,
            _ when mod is >= 3 and <= 10 => $"{abs} {f.Few}",
            _ when mod is >= 11 and <= 99 => $"{abs} {f.Many}",
            _ => $"{abs} {f.Other}",
        };
        return (n < 0 ? "قبل " : "خلال ") + words;
    }
}
