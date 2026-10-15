using System.Globalization;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

// The words and date formats of the pickers, written out as Intl gives them for "ar" and "en" with Latin digits, so they do not depend on the machine's culture data.
internal sealed class PickerLocale
{
    public static readonly PickerLocale Arabic = new(true);
    public static readonly PickerLocale English = new(false);

    private static readonly string[] GregorianAr = ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];
    private static readonly string[] GregorianEn = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    private static readonly string[] ShortGregorianEn = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    private static readonly string[] HijriAr = ["محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة"];
    private static readonly string[] HijriEn = ["Muharram", "Safar", "Rabiʻ I", "Rabiʻ II", "Jumada I", "Jumada II", "Rajab", "Shaʻban", "Ramadan", "Shawwal", "Dhuʻl-Qiʻdah", "Dhuʻl-Hijjah"];
    private static readonly string[] NarrowAr = ["ح", "ن", "ث", "ر", "خ", "ج", "س"];
    private static readonly string[] NarrowEn = ["S", "M", "T", "W", "T", "F", "S"];
    private static readonly string[] LongAr = ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];
    private static readonly string[] LongEn = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
    private static readonly string[] ShortEn = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    private PickerLocale(bool ar)
    {
        IsArabic = ar;
        Today = ar ? "اليوم" : "Today";
        Clear = ar ? "مسح" : "Clear";
        BadDate = ar ? "اكتب تاريخًا صحيحًا." : "Enter a valid date.";
        BadTime = ar ? "اكتب وقتًا صحيحًا." : "Enter a valid time.";
        PickStart = ar ? "اختر البداية" : "Choose the start";
        PickEnd = ar ? "اختر النهاية" : "Choose the end";
        Done = ar ? "تم" : "Done";
        Cancel = ar ? "إلغاء" : "Cancel";
        Choose = ar ? "اختر تاريخًا" : "Choose a date";
        ChooseTime = ar ? "اختر وقتًا" : "Choose a time";
        Calendar = ar ? "التقويم" : "Calendar";
        CalendarSystem = ar ? "نظام التقويم" : "Calendar system";
        PrevMonth = ar ? "الشهر السابق" : "Previous month";
        NextMonth = ar ? "الشهر التالي" : "Next month";
        Month = ar ? "الشهر" : "Month";
        Year = ar ? "السنة" : "Year";
        Day = ar ? "اليوم" : "Day";
        Hour = ar ? "الساعة" : "Hour";
        Minute = ar ? "الدقيقة" : "Minute";
        Second = ar ? "الثانية" : "Second";
        Period = ar ? "الفترة" : "AM/PM";
        Gregorian = ar ? "ميلادي" : "Gregorian";
        HijriLabel = ar ? "هجري" : "Hijri";
        Am = ar ? "ص" : "AM";
        Pm = ar ? "م" : "PM";
    }

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

    public string BirthStart => IsArabic ? "اكتب التاريخ أو اسحب الأعمدة" : "Type the date or drag the columns";

    public string BirthFar => IsArabic ? "هذا التاريخ أبعد من المسموح" : "This date is further back than allowed";

    public string BirthIncomplete => IsArabic ? "أكمل تاريخ الميلاد" : "Finish the date of birth";

    public string BirthRequired => IsArabic ? "اكتب تاريخ ميلادك" : "Enter your date of birth";

    public string BirthPick => IsArabic ? "اختر من الأعمدة" : "Pick from the columns";

    public string BirthGroup => IsArabic ? "تاريخ الميلاد" : "Date of birth";

    public string BirthLeft(IEnumerable<string> list) => IsArabic ? "بقي " + string.Join(" و", list) : "Still to choose: " + string.Join(" and ", list).ToLowerInvariant();

    public string BirthAge(int n) => IsArabic ? "العمر " + ArYears(n) : n == 1 ? "1 year old" : N(n) + " years old";

    public string BirthYoung(int n) => IsArabic ? "يجب أن يكون العمر " + ArYears(n) + " على الأقل" : "You must be at least " + N(n) + " years old";

    /// <summary>The placeholder of a part of the birth date: <paramref name="k"/> is d, m or y.</summary>
    public string BirthWord(char k) => IsArabic ? k switch { 'd' => "يوم", 'm' => "شهر", _ => "سنة" } : k switch { 'd' => "Day", 'm' => "Month", _ => "Year" };

    /// <summary>The caption and the accessible name of a part of the birth date.</summary>
    public string BirthCap(char k) => IsArabic ? k switch { 'd' => "اليوم", 'm' => "الشهر", _ => "السنة" } : k switch { 'd' => "Day", 'm' => "Month", _ => "Year" };

    private static string ArYears(int n)
    {
        var r = n % 100;
        return n == 0 ? "أقل من سنة" : n == 1 ? "سنة واحدة" : n == 2 ? "سنتان" : N(n) + (r >= 3 && r <= 10 ? " سنوات" : " سنة");
    }

    public string Early(string date) => IsArabic ? $"اختر {date} أو ما بعده." : $"Choose {date} or later.";

    public string Late(string date) => IsArabic ? $"اختر {date} أو ما قبله." : $"Choose {date} or earlier.";

    // An explicit locale wins; otherwise the page's language follows its direction, as the docs' ?dir= switch does.
    public static PickerLocale For(string? locale, FlowDirection direction)
    {
        if (!string.IsNullOrEmpty(locale)) return locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? Arabic : English;
        return direction == FlowDirection.RightToLeft ? Arabic : English;
    }

    // Saturday for Arabic, Sunday for English, as Intl's week info answers.
    public DayOfWeek WeekStart => IsArabic ? DayOfWeek.Saturday : DayOfWeek.Sunday;

    public bool MonthFirst => !IsArabic;

    public string Narrow(DayOfWeek d) => (IsArabic ? NarrowAr : NarrowEn)[(int)d];

    public string LongDay(DayOfWeek d) => (IsArabic ? LongAr : LongEn)[(int)d];

    public string ShortDay(DayOfWeek d) => (IsArabic ? LongAr : ShortEn)[(int)d];

    public string MonthName(PickerCalendar cal, int m) =>
        (cal.IsGregorian ? IsArabic ? GregorianAr : GregorianEn : IsArabic ? HijriAr : HijriEn)[((m % 12) + 12) % 12];

    /// <summary>A Gregorian month as Intl's short month gives it: Jan in English, the whole name in Arabic.</summary>
    public string ShortMonth(int m) => (IsArabic ? GregorianAr : ShortGregorianEn)[((m % 12) + 12) % 12];

    private DateTimeFormatInfo? _format;

    /// <summary>These words as .NET date formatting, Gregorian with Latin digits, for Avalonia's own pickers.</summary>
    public DateTimeFormatInfo Format => _format ??= BuildFormat(IsArabic);

    private static DateTimeFormatInfo BuildFormat(bool ar)
    {
        var f = (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();
        string[] months = [.. (ar ? GregorianAr : GregorianEn), ""];
        string[] shortMonths = [.. (ar ? GregorianAr : ShortGregorianEn), ""];
        f.MonthNames = months;
        f.MonthGenitiveNames = months;
        f.AbbreviatedMonthNames = shortMonths;
        f.AbbreviatedMonthGenitiveNames = shortMonths;
        f.DayNames = ar ? LongAr : LongEn;
        f.AbbreviatedDayNames = ar ? LongAr : ShortEn;
        f.ShortestDayNames = ar ? NarrowAr : NarrowEn;
        f.AMDesignator = ar ? "ص" : "AM";
        f.PMDesignator = ar ? "م" : "PM";
        f.ShortDatePattern = ar ? "d/M/yyyy" : "M/d/yyyy";
        f.LongDatePattern = ar ? "dddd، d MMMM yyyy" : "dddd, MMMM d, yyyy";
        f.YearMonthPattern = "MMMM yyyy";
        f.MonthDayPattern = ar ? "d MMMM" : "MMMM d";
        f.FirstDayOfWeek = ar ? DayOfWeek.Saturday : DayOfWeek.Sunday;
        return DateTimeFormatInfo.ReadOnly(f);
    }

    public string YearText(PickerCalendar cal, int y) =>
        cal.IsGregorian ? N(y) : IsArabic ? $"{N(y)} هـ" : $"{N(y)} AH";

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
        IsArabic ? $"{FormatDay(cal, d)} في {FormatTime(t)}" : $"{FormatDay(cal, d)} at {FormatTime(t)}";

    public string FormatTime(TimeSpan t)
    {
        var h = t.Hours;
        var h12 = h % 12 == 0 ? 12 : h % 12;
        return $"{N(h12)}:{t.Minutes:00} {(h < 12 ? Am : Pm)}";
    }

    public static string N(int n) => n.ToString(CultureInfo.InvariantCulture);
}
