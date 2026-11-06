using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A control whose built-in words are written once and must be written again when the app's language changes.</summary>
internal interface IWords
{
    void Reword();
}

/// <summary>Which words a control speaks: Arabic or English built-ins, and whether the app's own words (<see cref="InsiyabTheme.Strings"/>) take their place.</summary>
internal readonly struct Speech(bool arabic, bool app)
{
    public bool Arabic { get; } = arabic;

    public bool App { get; } = app;

    public string this[string key] => Words.Say(key, Arabic, App);

    public string Format(string key, object? a, object? b = null) => Words.Fill(this[key], a, b);

    /// <summary>A counted text: the app's <c>key.one</c> for one and <c>key</c> for the rest, else the Arabic forms from <paramref name="arabic"/> or the English ones.</summary>
    public string Count(string key, int n, Func<int, string>? arabic = null, object? b = null)
    {
        if (App && Words.Given(n == 1 ? key + ".one" : key, Arabic) is { } own) return Words.Fill(own, n, b);
        if (App && n == 1 && Words.Given(key, Arabic) is { } many) return Words.Fill(many, n, b);
        if (Arabic && arabic is not null) return arabic(n);
        var builtin = n == 1 && Words.Has(key + ".one") ? Words.Builtin(key + ".one", Arabic) : Words.Builtin(key, Arabic);
        return Words.Fill(builtin, n, b);
    }
}

/// <summary>The library's own words in Arabic and English, in one table, and the app's language and words that replace them.</summary>
internal static class Words
{
    private static string? s_language;
    private static IReadOnlyDictionary<string, string> s_strings = new Dictionary<string, string>();
    private static readonly List<WeakReference<TopLevel>> s_tops = [];
    private static readonly HashSet<string> s_ours = new(StringComparer.Ordinal);
    private static bool s_registered;

    /// <summary>Bumped by every <see cref="Set"/>; 0 while the app has never set a language, so controls skip their word work.</summary>
    internal static int Version { get; private set; }

    internal static string? Language => s_language;

    internal static IReadOnlyDictionary<string, string> Strings => s_strings;

    internal static IEnumerable<string> Keys => Table.Keys;

    internal static bool IsArabicCode(string code) => code.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    /// <summary>The words of a control with no locale of its own: the app's language, else Arabic when it reads right to left.</summary>
    internal static Speech Of(Visual? at) => new(s_language is { } l ? IsArabicCode(l) : at?.FlowDirection == FlowDirection.RightToLeft, true);

    /// <summary>The words of a control that may name its own locale: the app's words apply when that locale is the app's language, or when the app named none.</summary>
    internal static Speech Of(Visual? at, string? locale)
    {
        if (string.IsNullOrEmpty(locale)) return Of(at);
        var ar = IsArabicCode(locale);
        var app = s_language is not { } l || string.Compare(l, 0, locale, 0, 2, StringComparison.OrdinalIgnoreCase) == 0;
        return new Speech(ar, app);
    }

    /// <summary>A word that had one fixed Arabic default before the app could name a language: Arabic until a language is set.</summary>
    internal static Speech Fixed => new(s_language is not { } l || IsArabicCode(l), true);

    internal static bool Has(string key) => Table.ContainsKey(key);

    internal static string Builtin(string key, bool arabic) => Table.TryGetValue(key, out var w) ? arabic ? w.Ar : w.En : key;

    // The app's words speak its language; with no language named they apply everywhere.
    internal static string? Given(string key, bool arabic) =>
        s_strings.Count > 0 && (s_language is not { } l || IsArabicCode(l) == arabic) && s_strings.TryGetValue(key, out var own) ? own : null;

    internal static string Say(string key, bool arabic, bool app) => (app ? Given(key, arabic) : null) ?? Builtin(key, arabic);

    internal static string Fill(string text, object? a, object? b = null)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (a is not null) text = text.Replace("{0}", Convert.ToString(a, inv));
        if (b is not null) text = text.Replace("{1}", Convert.ToString(b, inv));
        return text;
    }

    /// <summary>A control's own word properties, each with its key.</summary>
    internal readonly record struct Own(AvaloniaProperty Property, string Key);

    /// <summary>Whether the app has named a language or given words of its own.</summary>
    internal static bool IsSet => s_language is not null || s_strings.Count > 0;

    /// <summary>As a control is made: its word properties in the app's language, when the app set one.</summary>
    internal static void Fill(AvaloniaObject target, Own[] words)
    {
        if (IsSet) Refill(target, words);
    }

    /// <summary>Writes each word property the app has not given in the app's language, leaving values the app or its styles set.</summary>
    internal static void Refill(AvaloniaObject target, Own[] words)
    {
        if (Version == 0) return;
        var speech = Fixed;
        foreach (var (property, key) in words)
        {
            if (target.GetValue(property) is not string now || !s_ours.Contains(now)) continue;
            var word = speech[key];
            s_ours.Add(word);
            if (!string.Equals(now, word, StringComparison.Ordinal)) target.SetCurrentValue(property, word);
        }
    }

    internal static void Register()
    {
        if (s_registered) return;
        s_registered = true;
        foreach (var (ar, en) in Table.Values)
        {
            s_ours.Add(ar);
            s_ours.Add(en);
        }
        Control.LoadedEvent.AddClassHandler<TopLevel>((top, _) => Track(top));
    }

    private static void Track(TopLevel top)
    {
        lock (s_tops)
        {
            s_tops.RemoveAll(w => !w.TryGetTarget(out var t) || t == top);
            s_tops.Add(new WeakReference<TopLevel>(top));
        }
    }

    internal static void Set(string? language, IReadOnlyDictionary<string, string>? strings)
    {
        Dispatcher.UIThread.VerifyAccess();
        s_language = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
        s_strings = strings is null ? new Dictionary<string, string>() : new Dictionary<string, string>(strings.ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal);
        foreach (var word in s_strings.Values) s_ours.Add(word);
        Version++;
        PickerLocale.Forget();
        InsiyabTheme.RewordThemes();
        List<TopLevel> tops;
        lock (s_tops)
        {
            s_tops.RemoveAll(w => !w.TryGetTarget(out _));
            tops = [.. s_tops.Select(w => w.TryGetTarget(out var t) ? t : null).OfType<TopLevel>()];
        }
        foreach (var top in tops)
        {
            if (top is IWords self) self.Reword();
            foreach (var v in top.GetVisualDescendants())
            {
                if (v is IWords w) w.Reword();
                else if (v is ListBox list) Shell.RewordFilter(list);
                else if (v is Carousel carousel) CarouselNav.Reword(carousel);
            }
        }
    }

    // Every built-in word: key, then Arabic and English. Counted texts take {0} for the number; their Arabic forms are written in code and a key.one entry gives the English form for one.
    private static readonly Dictionary<string, (string Ar, string En)> Table = new(StringComparer.Ordinal)
    {
        ["text.cut"] = ("قص", "Cut"),
        ["text.copy"] = ("نسخ", "Copy"),
        ["text.paste"] = ("لصق", "Paste"),

        ["dialog.close"] = ("إغلاق", "Close"),
        ["dialog.ok"] = ("تأكيد", "OK"),
        ["dialog.cancel"] = ("إلغاء", "Cancel"),
        ["alert.close"] = ("إغلاق التنبيه", "Close alert"),
        ["toast.close"] = ("إغلاق", "Close"),

        ["notify.title"] = ("الإشعارات", "Notifications"),
        ["notify.readAll"] = ("تعليم الكل مقروءًا", "Mark all as read"),
        ["notify.empty"] = ("لا إشعارات", "No notifications"),
        ["notify.unread"] = ("{0} غير مقروءة", "{0} unread"),

        ["palette.placeholder"] = ("ابحث عن صفحة أو أمر…", "Search for a page or command…"),
        ["palette.empty"] = ("لا نتائج", "No results"),
        ["palette.recent"] = ("الأخيرة", "Recent"),
        ["palette.move"] = ("للتنقل", "to move"),
        ["palette.choose"] = ("للاختيار", "to choose"),
        ["palette.close"] = ("للإغلاق", "to close"),

        ["shell.filter"] = ("بحث", "Search"),
        ["shell.filterEmpty"] = ("لا نتائج", "No results"),
        ["caption.minimize"] = ("تصغير", "Minimize"),
        ["caption.maximize"] = ("تكبير", "Maximize"),
        ["caption.restore"] = ("استعادة", "Restore"),
        ["caption.close"] = ("إغلاق", "Close"),
        ["split.resize"] = ("تغيير حجم اللوحين", "Resize the panes"),
        ["page.previous"] = ("السابق", "Previous"),
        ["page.next"] = ("التالي", "Next"),
        ["page.compact"] = ("{0} من {1}", "{0} of {1}"),
        ["toolbar.more"] = ("المزيد", "More"),
        ["toolbar.moreOptions"] = ("خيارات أخرى", "More options"),
        ["tabs.close"] = ("إغلاق التبويب", "Close tab"),
        ["tabs.new"] = ("تبويب جديد", "New tab"),

        ["wizard.previous"] = ("السابق", "Previous"),
        ["wizard.next"] = ("التالي", "Next"),
        ["wizard.finish"] = ("إرسال", "Submit"),
        ["wizard.required"] = ("هذا الحقل مطلوب.", "This field is required."),

        ["save.saving"] = ("جارٍ الحفظ…", "Saving…"),
        ["save.saved"] = ("حُفظ", "Saved"),
        ["save.error"] = ("تعذر الحفظ", "Couldn't save"),

        ["chip.remove"] = ("إزالة", "Remove"),
        ["tags.empty"] = ("لا نتائج", "No results"),
        ["tags.remove"] = ("إزالة", "Remove"),
        ["select.search"] = ("ابحث", "Search"),
        ["select.empty"] = ("لا نتائج", "No results"),

        ["find.placeholder"] = ("ابحث في الصفحة", "Find in page"),
        ["find.name"] = ("البحث في الصفحة", "Find in page"),
        ["find.previous"] = ("النتيجة السابقة", "Previous match"),
        ["find.next"] = ("النتيجة التالية", "Next match"),
        ["find.close"] = ("إغلاق البحث", "Close find"),
        ["find.empty"] = ("لا نتائج", "No results"),
        ["find.count"] = ("{0} من {1}", "{0} of {1}"),

        ["table.selectAll"] = ("تحديد الكل", "Select all"),
        ["table.selectRow"] = ("تحديد الصف", "Select row"),
        ["bulk.selected"] = ("المحدد: {0}", "{0} selected"),
        ["chart.copyValue"] = ("نسخ القيمة", "Copy value"),
        ["chart.copyValues"] = ("نسخ القيم", "Copy values"),
        ["chart.copyData"] = ("نسخ البيانات", "Copy data"),
        ["chart.thousand"] = ("{0} ألف", "{0}K"),
        ["chart.million"] = ("{0} مليون", "{0}M"),
        ["chart.billion"] = ("{0} مليار", "{0}B"),
        ["countdown.days"] = ("{0} يوم", "{0}d"),

        ["check.required"] = ("املأ هذا الحقل.", "Fill in this field."),
        ["check.email"] = ("اكتب بريدًا إلكترونيًا صحيحًا.", "Enter a valid email address."),
        ["check.minLength"] = ("اكتب {0} حرف على الأقل.", "Enter at least {0} characters."),
        ["check.minLength.one"] = ("اكتب حرفًا واحدًا على الأقل.", "Enter at least {0} character."),
        ["check.pattern"] = ("اكتب القيمة بالصيغة المطلوبة.", "Match the requested format."),
        ["count.words"] = ("{0}/{1} كلمة", "{0}/{1} words"),
        ["count.wordsFree"] = ("{0} كلمة", "{0} words"),
        ["count.wordsFree.one"] = ("كلمة واحدة", "{0} word"),
        ["count.tooMany"] = ("لا تتجاوز {0} كلمة.", "Use at most {0} words."),
        ["count.tooMany.one"] = ("لا تتجاوز كلمة واحدة.", "Use at most {0} word."),

        ["date.today"] = ("اليوم", "Today"),
        ["date.clear"] = ("مسح", "Clear"),
        ["date.done"] = ("تم", "Done"),
        ["date.cancel"] = ("إلغاء", "Cancel"),
        ["date.badDate"] = ("اكتب تاريخًا صحيحًا.", "Enter a valid date."),
        ["date.badTime"] = ("اكتب وقتًا صحيحًا.", "Enter a valid time."),
        ["date.pickStart"] = ("اختر البداية", "Choose the start"),
        ["date.pickEnd"] = ("اختر النهاية", "Choose the end"),
        ["date.choose"] = ("اختر تاريخًا", "Choose a date"),
        ["date.chooseTime"] = ("اختر وقتًا", "Choose a time"),
        ["date.calendar"] = ("التقويم", "Calendar"),
        ["date.calendarSystem"] = ("نظام التقويم", "Calendar system"),
        ["date.prevMonth"] = ("الشهر السابق", "Previous month"),
        ["date.nextMonth"] = ("الشهر التالي", "Next month"),
        ["date.month"] = ("الشهر", "Month"),
        ["date.year"] = ("السنة", "Year"),
        ["date.day"] = ("اليوم", "Day"),
        ["date.hour"] = ("الساعة", "Hour"),
        ["date.minute"] = ("الدقيقة", "Minute"),
        ["date.second"] = ("الثانية", "Second"),
        ["date.period"] = ("الفترة", "AM/PM"),
        ["date.gregorian"] = ("ميلادي", "Gregorian"),
        ["date.hijri"] = ("هجري", "Hijri"),
        ["date.hijriYear"] = ("{0} هـ", "{0} AH"),
        ["date.am"] = ("ص", "AM"),
        ["date.pm"] = ("م", "PM"),
        ["date.at"] = ("{0} في {1}", "{0} at {1}"),
        ["date.early"] = ("اختر {0} أو ما بعده.", "Choose {0} or later."),
        ["date.late"] = ("اختر {0} أو ما قبله.", "Choose {0} or earlier."),
        ["date.month.1"] = ("يناير", "January"),
        ["date.month.2"] = ("فبراير", "February"),
        ["date.month.3"] = ("مارس", "March"),
        ["date.month.4"] = ("أبريل", "April"),
        ["date.month.5"] = ("مايو", "May"),
        ["date.month.6"] = ("يونيو", "June"),
        ["date.month.7"] = ("يوليو", "July"),
        ["date.month.8"] = ("أغسطس", "August"),
        ["date.month.9"] = ("سبتمبر", "September"),
        ["date.month.10"] = ("أكتوبر", "October"),
        ["date.month.11"] = ("نوفمبر", "November"),
        ["date.month.12"] = ("ديسمبر", "December"),
        ["date.monthShort.1"] = ("يناير", "Jan"),
        ["date.monthShort.2"] = ("فبراير", "Feb"),
        ["date.monthShort.3"] = ("مارس", "Mar"),
        ["date.monthShort.4"] = ("أبريل", "Apr"),
        ["date.monthShort.5"] = ("مايو", "May"),
        ["date.monthShort.6"] = ("يونيو", "Jun"),
        ["date.monthShort.7"] = ("يوليو", "Jul"),
        ["date.monthShort.8"] = ("أغسطس", "Aug"),
        ["date.monthShort.9"] = ("سبتمبر", "Sep"),
        ["date.monthShort.10"] = ("أكتوبر", "Oct"),
        ["date.monthShort.11"] = ("نوفمبر", "Nov"),
        ["date.monthShort.12"] = ("ديسمبر", "Dec"),
        ["date.hijriMonth.1"] = ("محرم", "Muharram"),
        ["date.hijriMonth.2"] = ("صفر", "Safar"),
        ["date.hijriMonth.3"] = ("ربيع الأول", "Rabiʻ I"),
        ["date.hijriMonth.4"] = ("ربيع الآخر", "Rabiʻ II"),
        ["date.hijriMonth.5"] = ("جمادى الأولى", "Jumada I"),
        ["date.hijriMonth.6"] = ("جمادى الآخرة", "Jumada II"),
        ["date.hijriMonth.7"] = ("رجب", "Rajab"),
        ["date.hijriMonth.8"] = ("شعبان", "Shaʻban"),
        ["date.hijriMonth.9"] = ("رمضان", "Ramadan"),
        ["date.hijriMonth.10"] = ("شوال", "Shawwal"),
        ["date.hijriMonth.11"] = ("ذو القعدة", "Dhuʻl-Qiʻdah"),
        ["date.hijriMonth.12"] = ("ذو الحجة", "Dhuʻl-Hijjah"),
        ["date.weekday.0"] = ("الأحد", "Sunday"),
        ["date.weekday.1"] = ("الاثنين", "Monday"),
        ["date.weekday.2"] = ("الثلاثاء", "Tuesday"),
        ["date.weekday.3"] = ("الأربعاء", "Wednesday"),
        ["date.weekday.4"] = ("الخميس", "Thursday"),
        ["date.weekday.5"] = ("الجمعة", "Friday"),
        ["date.weekday.6"] = ("السبت", "Saturday"),
        ["date.weekdayShort.0"] = ("الأحد", "Sun"),
        ["date.weekdayShort.1"] = ("الاثنين", "Mon"),
        ["date.weekdayShort.2"] = ("الثلاثاء", "Tue"),
        ["date.weekdayShort.3"] = ("الأربعاء", "Wed"),
        ["date.weekdayShort.4"] = ("الخميس", "Thu"),
        ["date.weekdayShort.5"] = ("الجمعة", "Fri"),
        ["date.weekdayShort.6"] = ("السبت", "Sat"),
        ["date.weekdayNarrow.0"] = ("ح", "S"),
        ["date.weekdayNarrow.1"] = ("ن", "M"),
        ["date.weekdayNarrow.2"] = ("ث", "T"),
        ["date.weekdayNarrow.3"] = ("ر", "W"),
        ["date.weekdayNarrow.4"] = ("خ", "T"),
        ["date.weekdayNarrow.5"] = ("ج", "F"),
        ["date.weekdayNarrow.6"] = ("س", "S"),

        ["birth.start"] = ("اكتب التاريخ أو اسحب الأعمدة", "Type the date or drag the columns"),
        ["birth.far"] = ("هذا التاريخ أبعد من المسموح", "This date is further back than allowed"),
        ["birth.incomplete"] = ("أكمل تاريخ الميلاد", "Finish the date of birth"),
        ["birth.required"] = ("اكتب تاريخ ميلادك", "Enter your date of birth"),
        ["birth.pick"] = ("اختر من الأعمدة", "Pick from the columns"),
        ["birth.group"] = ("تاريخ الميلاد", "Date of birth"),
        ["birth.left"] = ("بقي {0}", "Still to choose: {0}"),
        ["birth.and"] = (" و", " and "),
        ["birth.age"] = ("العمر {0} سنة", "{0} years old"),
        ["birth.age.one"] = ("العمر سنة واحدة", "{0} year old"),
        ["birth.young"] = ("يجب أن يكون العمر {0} سنة على الأقل", "You must be at least {0} years old"),
        ["birth.young.one"] = ("يجب أن يكون العمر سنة واحدة على الأقل", "You must be at least {0} year old"),
        ["birth.dayHint"] = ("يوم", "Day"),
        ["birth.monthHint"] = ("شهر", "Month"),
        ["birth.yearHint"] = ("سنة", "Year"),

        ["month.month"] = ("شهر", "Month"),
        ["month.week"] = ("أسبوع", "Week"),
        ["month.day"] = ("يوم", "Day"),
        ["month.view"] = ("طريقة العرض", "View"),
        ["month.workHours"] = ("ساعات الدوام", "Work hours"),
        ["month.workHoursTip"] = ("اعرض ساعات الدوام وحدها، من {0} إلى {1}", "Show only the work hours, {0} to {1}"),
        ["month.prevWeek"] = ("الأسبوع السابق", "Previous week"),
        ["month.nextWeek"] = ("الأسبوع التالي", "Next week"),
        ["month.prevDay"] = ("اليوم السابق", "Previous day"),
        ["month.nextDay"] = ("اليوم التالي", "Next day"),
        ["month.allDay"] = ("طوال اليوم", "All day"),
        ["month.before"] = ("قبل {0}", "Before {0}"),
        ["month.after"] = ("بعد {0}", "After {0}"),
        ["month.events"] = ("{0} موعد", "{0} events"),
        ["month.events.one"] = ("موعد واحد", "{0} event"),
        ["month.eventsBefore"] = ("{0} قبل {1}", "{0} before {1}"),
        ["month.eventsAfter"] = ("{0} بعد {1}", "{0} after {1}"),

        ["time.now"] = ("الآن", "now"),
        ["time.today"] = ("اليوم", "today"),
        ["time.yesterday"] = ("أمس", "yesterday"),
        ["time.tomorrow"] = ("غدًا", "tomorrow"),
        ["time.ago"] = ("قبل {0}", "{0} ago"),
        ["time.in"] = ("خلال {0}", "in {0}"),
        ["time.minutes"] = ("{0} دقيقة", "{0} minutes"),
        ["time.minutes.one"] = ("دقيقة واحدة", "{0} minute"),
        ["time.hours"] = ("{0} ساعة", "{0} hours"),
        ["time.hours.one"] = ("ساعة واحدة", "{0} hour"),
        ["time.days"] = ("{0} يوم", "{0} days"),
        ["time.days.one"] = ("يوم واحد", "{0} day"),
        ["time.weeks"] = ("{0} أسبوع", "{0} weeks"),
        ["time.weeks.one"] = ("أسبوع واحد", "{0} week"),
        ["time.months"] = ("{0} شهر", "{0} months"),
        ["time.months.one"] = ("شهر واحد", "{0} month"),
        ["time.years"] = ("{0} سنة", "{0} years"),
        ["time.years.one"] = ("سنة واحدة", "{0} year"),
        ["time.thisMinute"] = ("هذه الدقيقة", "this minute"),
        ["time.thisHour"] = ("الساعة الحالية", "this hour"),
        ["time.lastWeek"] = ("الأسبوع الماضي", "last week"),
        ["time.thisWeek"] = ("هذا الأسبوع", "this week"),
        ["time.nextWeek"] = ("الأسبوع القادم", "next week"),
        ["time.lastMonth"] = ("الشهر الماضي", "last month"),
        ["time.thisMonth"] = ("هذا الشهر", "this month"),
        ["time.nextMonth"] = ("الشهر القادم", "next month"),
        ["time.lastYear"] = ("السنة الماضية", "last year"),
        ["time.thisYear"] = ("السنة الحالية", "this year"),
        ["time.nextYear"] = ("السنة القادمة", "next year"),

        ["carousel.previous"] = ("السابق", "Previous"),
        ["carousel.next"] = ("التالي", "Next"),
        ["carousel.slide"] = ("الشريحة {0}", "Slide {0}"),
        ["carousel.of"] = ("{0} من {1}", "{0} of {1}"),
        ["media.seek"] = ("موضع التشغيل", "Playback position"),
        ["media.play"] = ("تشغيل", "Play"),
        ["media.pause"] = ("إيقاف مؤقت", "Pause"),
        ["media.mute"] = ("كتم الصوت", "Mute"),
        ["media.unmute"] = ("إعادة الصوت", "Unmute"),
        ["media.volume"] = ("مستوى الصوت", "Volume"),
        ["media.speed"] = ("السرعة", "Speed"),
        ["media.back"] = ("رجوع {0} ثانية", "Back {0} seconds"),
        ["media.back.one"] = ("رجوع ثانية", "Back {0} second"),
        ["media.forward"] = ("تقديم {0} ثانية", "Forward {0} seconds"),
        ["media.forward.one"] = ("تقديم ثانية", "Forward {0} second"),
        ["media.subtitles"] = ("الترجمة", "Subtitles"),
        ["media.fill"] = ("ملء الإطار", "Fill the frame"),
        ["media.fullScreen"] = ("ملء الشاشة", "Full screen"),
        ["media.exitFullScreen"] = ("الخروج من ملء الشاشة", "Exit full screen"),
        ["media.more"] = ("المزيد", "More"),
        ["media.live"] = ("مباشر", "Live"),
        ["media.backToLive"] = ("العودة إلى البث المباشر", "Back to live"),
        ["picture.title"] = ("عرض الصورة", "View picture"),
        ["picture.zoomIn"] = ("تكبير", "Zoom in"),
        ["picture.zoomOut"] = ("تصغير", "Zoom out"),
        ["picture.fit"] = ("الصورة كاملة", "Fit"),
        ["picture.actual"] = ("الحجم الأصلي", "Actual size"),
        ["color.bad"] = ("اكتب لونًا مثل #4C1D95.", "Enter a color like #4C1D95."),
        ["color.contrast"] = ("التباين", "Contrast"),
        ["color.onWhite"] = ("على الأبيض", "on white"),
        ["color.onBlack"] = ("على الأسود", "on black"),
        ["color.fine"] = ("يصلح للنص", "fine for text"),
        ["color.low"] = ("لا يكفي للنص", "too low for text"),
        ["otp.digits"] = ("اكتب الرمز كاملًا: {0} أرقام.", "Enter all {0} digits."),
        ["otp.chars"] = ("اكتب الرمز كاملًا: {0} خانات.", "Enter all {0} characters."),
        ["phone.country"] = ("الدولة", "Country"),
        ["phone.search"] = ("ابحث عن دولة أو رمز", "Search for a country or code"),
        ["phone.empty"] = ("لا دولة بهذا الاسم", "No country by that name"),
        ["phone.elsewhere"] = ("لا تُقبل هنا أرقام {0}.", "Numbers from {0} are not accepted here."),
        ["phone.partial"] = ("اكتب رمز الدولة كاملًا بعد +.", "Enter the full country code after +."),
        ["phone.invalid"] = ("اكتب رقم هاتف صحيحًا في {0}.", "Enter a valid phone number for {0}."),
        ["file.dragMany"] = ("اسحب الملفات إلى هنا أو", "Drag files here or"),
        ["file.dragOne"] = ("اسحب الملف إلى هنا أو", "Drag a file here or"),
        ["file.pickMany"] = ("اختر ملفات", "choose files"),
        ["file.pickOne"] = ("اختر ملفًا", "choose a file"),
        ["file.maxSize"] = ("حتى {0} للملف الواحد", "up to {0} each"),
        ["file.maxCount"] = ("{0} على الأكثر", "{0} at most"),
        ["file.files"] = ("{0} ملف", "{0} files"),
        ["file.files.one"] = ("ملف واحد", "{0} file"),
        ["file.badType"] = ("نوع «{0}» غير مقبول.", "“{0}” is not a type this field accepts."),
        ["file.tooBig"] = ("«{0}» أكبر من {1}.", "“{0}” is larger than {1}."),
        ["file.tooMany"] = ("لم يُضف «{0}»: {1} على الأكثر.", "“{0}” was not added: {1} at most."),
        ["file.failedOnes"] = ("أزِل الملفات التي تعذر رفعها أو أعد رفعها.", "Remove the files that failed to upload, or upload them again."),
        ["file.sending"] = ("انتظر حتى يكتمل رفع الملفات.", "Wait for the files to finish uploading."),
        ["file.remove"] = ("إزالة {0}", "Remove {0}"),
        ["file.failed"] = ("تعذر الرفع.", "Upload failed."),
        ["file.bytes"] = ("{0} بايت", "{0} byte"),
        ["file.kb"] = ("{0} كيلوبايت", "{0} kB"),
        ["file.mb"] = ("{0} م.ب", "{0} MB"),
    };
}
