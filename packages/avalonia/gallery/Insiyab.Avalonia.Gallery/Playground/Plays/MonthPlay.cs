using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class MonthPlay
{
    public const string Example =
        """
        <ins:MonthCalendar x:Name="Calendar" Month="2026-09-01" />
        <!-- والمواعيد في Events من الشيفرة، كما في «الأحداث» -->
        """;

    public static string Code { get; } =
        """
        Calendar.Events =
        [
            new() { Date = new(2026, 9, 3), Title = "تسليم الطلب 1042", Tone = MonthEventTone.Warn, Href = "/orders/1042" },
            new() { Date = new(2026, 9, 8), Time = new(10, 0, 0), Duration = TimeSpan.FromMinutes(90), Title = "اجتماع الفريق" },
        ];

        Calendar.MonthChanged += async (_, month) => Calendar.Events = await LoadAsync(month);
        Calendar.DateChanged += async (_, day) => Calendar.Events = await LoadWeekAsync(day);   // في الأسبوع واليوم
        Calendar.DayInvoked += (_, day) => OpenNew(day);         // يوم نُقر في موضع خال من مواعيده، ومعه الساعة في الأسبوع واليوم
        Calendar.EventInvoked += (_, e) => Navigate(e.Href);     // موعد له رابط
        """;

    private static MonthEvent Event(int day, TimeSpan? time, string title, MonthEventTone tone = MonthEventTone.Brand, string? href = null, int minutes = 0) =>
        new() { Date = new DateTime(2026, 9, day), Time = time, Title = title, Tone = tone, Href = href, Duration = minutes > 0 ? TimeSpan.FromMinutes(minutes) : null };

    /// <summary>The events of the page's main example.</summary>
    public static List<MonthEvent> Sample() =>
    [
        Event(3, null, "تسليم الطلب 1042", MonthEventTone.Warn, "/orders/1042"),
        Event(8, new TimeSpan(10, 0, 0), "اجتماع الفريق", minutes: 90),
        Event(8, new TimeSpan(14, 30, 0), "مراجعة العقد", MonthEventTone.Info),
        Event(15, null, "إطلاق الحملة", MonthEventTone.Ok),
        Event(21, new TimeSpan(9, 0, 0), "زيارة المورد", minutes: 90),
        Event(21, new TimeSpan(10, 0, 0), "مكالمة العميل"),
        Event(21, new TimeSpan(13, 0, 0), "تدريب", minutes: 120),
        Event(21, new TimeSpan(16, 0, 0), "موعد نهائي", MonthEventTone.Bad, minutes: 30),
        Event(26, null, "جرد المخزون", MonthEventTone.Info),
    ];

    private static readonly (int Day, int Hour, int Minute, int Word, int Length)[] Rows =
        [(3, -1, 0, 0, 0), (8, 10, 0, 1, 90), (8, 14, 30, 2, 0), (15, -1, 0, 3, 0), (21, 9, 0, 4, 90)];

    private static readonly (int Day, int Hour, int Minute, int Word, int Length)[] More =
        [(21, 10, 0, 5, 0), (21, 13, 0, 6, 120), (21, 16, 0, 7, 30)];

    private static IEnumerable<(int Day, int Hour, int Minute, int Word, int Length)> List(PlayState s) =>
        Rows.Concat(s.On("more") ? More : []).Append((26, -1, 0, 8, 0));

    private static string[] Words(PlayState s) => s.On("en")
        ? ["Delivery", "Meeting", "Review", "Workshop", "Call", "Training", "Visit", "Deadline", "Holiday"]
        : ["تسليم", "اجتماع", "مراجعة", "ورشة عمل", "مكالمة", "تدريب", "زيارة", "موعد نهائي", "إجازة"];

    public static PlayDef Month { get; } = new()
    {
        Id = "month",
        Title = "تقويم الشهر",
        Wide = _ => true,
        Controls =
        [
            Seg("view", "العرض", "Month",
                Choice("Month", "الشهر"),
                Choice("Week", "الأسبوع", "View=\"Week\"", "أسبوع بالساعات، أيامه أعمدة تبدأ من أول أيام الأسبوع في لغة الواجهة، ويقع كل موعد في ساعته بطول مدته. ويُعرض الأسبوع الذي فيه `Date`.", OptionKind.Property),
                Choice("Day", "اليوم", "View=\"Day\"", "يوم واحد بالساعات، هو `Date`.", OptionKind.Property)),
            Select("tone", "النبرة", "Brand",
                Choice("Brand", "الأساسي"),
                Choice("Ok", "نجاح", "Tone", "نبرة الموعد: `Ok` و`Warn` و`Bad` و`Info`، وهو بلون الهوية إن لم تذكرها.", OptionKind.Property),
                Choice("Warn", "تحذير"),
                Choice("Bad", "خطر"),
                Choice("Info", "معلومة")),
            Switch("time", "بوقته", "Time", "وقت الموعد، مثل `14:30`، يُكتب قبل عنوانه وتُرتب به مواعيد اليوم. والموعد بلا وقت يأخذ اليوم كله، فيقع في الأسبوع واليوم في صف «طوال اليوم».", OptionKind.Property, "الموعد", on: true),
            Switch("link", "رابط", "Href", "يصير الموعد رابطًا يُنقر، فيمتلئ بلونه تحت المؤشر ويُطلق `EventInvoked`.", OptionKind.Property, "الموعد"),
            Switch("more", "أكثر من ثلاثة في يوم"),
            Switch("hours", "ساعات الدوام", "DayStartHour", "أول ساعة يعرضها الأسبوع واليوم، وهي 8 إن لم تذكرها، ومعها `DayEndHour` الساعة التي ينتهيان عندها، وهي 20.", OptionKind.Property, "الأسبوع واليوم"),
            Switch("noswitch", "بلا مبدّل العرض", "HasViewSwitch", "اكتب `HasViewSwitch=\"False\"` ليختفي مبدّل الشهر والأسبوع واليوم من الرأس، ويبقى العرض الذي في `View`.", OptionKind.Property),
            Switch("en", "بالإنجليزية", "Locale=\"en\"", "لغة الشهر، ومنها أسماء الأيام والشهر وأول أيام الأسبوع. وإن تركتها فالعربية في الواجهة العربية والإنجليزية في غيرها.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:MonthCalendar", "شهر على شبكة أسابيع يميز اليوم الحالي، ويعرض في كل يوم ثلاثة مواعيد وعدد ما بقي، وعلى الهاتف تصير المواعيد نقاطًا. وفي رأسه مبدّل بين الشهر والأسبوع واليوم.", OptionKind.Element, true),
            Row("الأساس", "Month", "الشهر الذي يُعرض، وأي يوم فيه يكفي، وبدونه الشهر الحالي.", OptionKind.Property),
            Row("الأساس", "Events", "المواعيد، لكل موعد `MonthEvent` بيومه `Date` وعنوانه `Title`. وهي قائمة فارغة من البداية، فلك أن تكتب المواعيد `ins:MonthEvent` داخل التقويم في XAML.", OptionKind.Property),
            Row("الأسبوع واليوم", "Date", "اليوم الذي يعرضه اليوم، والأسبوع الذي فيه، وهو اليوم الحالي إن لم تذكره.", OptionKind.Property),
            Row("الأسبوع واليوم", "Duration", "على `MonthEvent`: مدة الموعد، ومنها طوله في الأسبوع واليوم، وهي ساعة إن لم تذكرها. وإذا تداخل موعدان صارا جنبًا إلى جنب.", OptionKind.Property),
            Row("الأسبوع واليوم", "HourHeight", "ارتفاع الساعة، وهو 48 إن لم تذكره.", OptionKind.Property),
            Row("الأحداث", "MonthChanged", "يُطلق إذا انتقلت إلى شهر آخر، ومعه أول يوم فيه.", OptionKind.Event),
            Row("الأحداث", "DateChanged", "يُطلق إذا انتقلت إلى أسبوع آخر أو يوم آخر، ومعه `Date` الجديد.", OptionKind.Event),
            Row("الأحداث", "ViewChanged", "يُطلق إذا بدّل المستخدم العرض، أو نقر اسم يوم في الأسبوع ففتح يومه.", OptionKind.Event),
            Row("الأحداث", "DayInvoked", "يُطلق إذا نُقر يوم في موضع خال من مواعيده، ومعه تاريخه، وفي الأسبوع واليوم ساعته أيضًا.", OptionKind.Event),
        ],
        Disable = s => s["view"] == "Month" ? ["hours"] : [],
        Axaml = s =>
        {
            var words = Words(s);
            var tone = s["tone"] == "Brand" ? "" : $", Tone = MonthEventTone.{s["tone"]}";
            var lines = List(s).Select((e, i) =>
                $"  new() {{ Date = new(2026, 9, {e.Day}){(s.On("time") && e.Hour >= 0 ? $", Time = new({e.Hour}, {e.Minute}, 0)" : "")}{(s.On("time") && e.Hour >= 0 && e.Length > 0 ? $", Duration = TimeSpan.FromMinutes({e.Length})" : "")}, Title = \"{words[e.Word]}\"{tone}{(s.On("link") && i == 0 ? ", Href = \"/events/1\"" : "")} }},");
            var view = s["view"] == "Month" ? "" : $" View=\"{s["view"]}\" Date=\"2026-09-21\"";
            var hours = s["view"] != "Month" && s.On("hours") ? " DayStartHour=\"9\" DayEndHour=\"17\"" : "";
            var sw = s.On("noswitch") ? " HasViewSwitch=\"False\"" : "";
            return $"<ins:MonthCalendar x:Name=\"Calendar\" Month=\"2026-09-01\"{view}{hours}{sw}{(s.On("en") ? " Locale=\"en\" FlowDirection=\"LeftToRight\"" : "")} />\n" +
                "<!-- المواعيد من الشيفرة:\nCalendar.Events =\n[\n" + string.Join("\n", lines) + "\n];\n-->";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["view"] != "Month")
            {
                notes.Add("يقع كل موعد في ساعته بطول مدته، وإذا تداخل موعدان صارا جنبًا إلى جنب، والمواعيد بلا وقت في صف «طوال اليوم». ويمر خط أحمر على الساعة الحالية في اليوم الحالي.");
                if (s["view"] == "Week") notes.Add("انقر اسم يوم في رأس الأسبوع فيفتح يومه.");
                if (!s.On("more")) notes.Add("شغّل «أكثر من ثلاثة في يوم» لترى موعدين متداخلين يوم 21.");
            }
            if (s.On("link")) notes.Add("صار «تسليم» يوم 3 رابطًا يشبه غيره من المواعيد، فمرر عليه المؤشر يمتلئ بلونه.");
            if (s.On("more") && s["view"] == "Month") notes.Add("لا يعرض اليوم أكثر من ثلاثة مواعيد، فإن زادت كتب عدد ما بقي، مثل «+2».");
            if (s.On("en")) notes.Add("صار الأسبوع يبدأ بالأحد كما يبدؤه أهل هذه اللغة، ومعها `FlowDirection` ليجري الشهر من اليسار.");
            notes.Add("انتقل إلى شهر آخر أو أسبوع آخر، أو بدّل العرض، أو انقر يومًا خاليًا، فيظهر هنا الحدث.");
            return notes;
        },
        Stage = (view, s, say) =>
        {
            var words = Words(s);
            var tone = Enum.Parse<MonthEventTone>(s["tone"]);
            foreach (var month in view.GetLogicalDescendants().OfType<MonthCalendar>())
            {
                month.Events = List(s).Select((e, i) => Event(e.Day, s.On("time") && e.Hour >= 0 ? new TimeSpan(e.Hour, e.Minute, 0) : null, words[e.Word], tone, s.On("link") && i == 0 ? "/events/1" : null, e.Length)).ToList();
                month.MonthChanged += (_, m) => say($"حدث `MonthChanged`، والشهر الآن `{m:yyyy-MM}`");
                month.DateChanged += (_, d) => say($"حدث `DateChanged`، و`Date` الآن `{d:yyyy-MM-dd}`");
                month.ViewChanged += (_, v) => say($"حدث `ViewChanged`، والعرض الآن `{v}`");
                month.DayInvoked += (_, d) => say(d.TimeOfDay == TimeSpan.Zero ? $"حدث `DayInvoked`، واليوم `{d:yyyy-MM-dd}`" : $"حدث `DayInvoked`، والموعد `{d:yyyy-MM-dd HH:mm}`");
                month.EventInvoked += (_, e) => say($"حدث `EventInvoked` على «{e.Title}»");
            }
        },
    };

    /// <summary>Fills the main example with the sample events.</summary>
    public static Action<Control> ExampleStage { get; } = view => view.GetLogicalDescendants().OfType<MonthCalendar>().First().Events = Sample();

    public static PlayDef[] All { get; } = [Month];
}
