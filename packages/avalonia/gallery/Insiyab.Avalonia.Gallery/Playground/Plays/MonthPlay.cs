using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class MonthPlay
{
    public const string Example =
        """
        <ins:MonthCalendar Month="2026-09-01" />
        """;

    public static string Code { get; } =
        """
        Calendar.Events =
        [
            new() { Date = new(2026, 9, 3), Title = "تسليم الطلب 1042", Tone = MonthEventTone.Warn, Href = "/orders/1042" },
            new() { Date = new(2026, 9, 8), Time = new(10, 0, 0), Title = "اجتماع الفريق" },
        ];

        Calendar.MonthChanged += async (_, month) => Calendar.Events = await LoadAsync(month);
        Calendar.DayInvoked += (_, day) => OpenNew(day);         // يوم نُقر في موضع خال من مواعيده
        Calendar.EventInvoked += (_, e) => Navigate(e.Href);     // موعد له رابط
        """;

    private static MonthEvent Event(int day, TimeSpan? time, string title, MonthEventTone tone = MonthEventTone.Brand, string? href = null) =>
        new() { Date = new DateTime(2026, 9, day), Time = time, Title = title, Tone = tone, Href = href };

    /// <summary>The events of the page's main example.</summary>
    public static List<MonthEvent> Sample() =>
    [
        Event(3, null, "تسليم الطلب 1042", MonthEventTone.Warn, "/orders/1042"),
        Event(8, new TimeSpan(10, 0, 0), "اجتماع الفريق"),
        Event(8, new TimeSpan(14, 30, 0), "مراجعة العقد", MonthEventTone.Info),
        Event(15, null, "إطلاق الحملة", MonthEventTone.Ok),
        Event(21, new TimeSpan(9, 0, 0), "زيارة المورد"),
        Event(21, new TimeSpan(11, 0, 0), "مكالمة العميل"),
        Event(21, new TimeSpan(13, 0, 0), "تدريب"),
        Event(21, new TimeSpan(16, 0, 0), "موعد نهائي", MonthEventTone.Bad),
        Event(26, null, "جرد المخزون", MonthEventTone.Info),
    ];

    private static readonly (int Day, int Hour, int Minute, int Word)[] Rows =
        [(3, -1, 0, 0), (8, 10, 0, 1), (8, 14, 30, 2), (15, -1, 0, 3), (21, 9, 0, 4)];

    private static readonly (int Day, int Hour, int Minute, int Word)[] More =
        [(21, 11, 0, 5), (21, 13, 0, 6), (21, 16, 0, 7)];

    private static IEnumerable<(int Day, int Hour, int Minute, int Word)> List(PlayState s) =>
        Rows.Concat(s.On("more") ? More : []).Append((26, -1, 0, 8));

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
            Select("tone", "النبرة", "Brand",
                Choice("Brand", "الأساسي"),
                Choice("Ok", "نجاح", "Tone", "نبرة الموعد: `Ok` و`Warn` و`Bad` و`Info`، وهو بلون الهوية إن لم تذكرها.", OptionKind.Property),
                Choice("Warn", "تحذير"),
                Choice("Bad", "خطر"),
                Choice("Info", "معلومة")),
            Switch("time", "بوقته", "Time", "وقت الموعد، مثل `14:30`، يُكتب قبل عنوانه وتُرتب به مواعيد اليوم.", OptionKind.Property, "الموعد", on: true),
            Switch("link", "رابط", "Href", "يصير الموعد رابطًا يُنقر، فيمتلئ بلونه تحت المؤشر ويُطلق `EventInvoked`.", OptionKind.Property, "الموعد"),
            Switch("more", "أكثر من ثلاثة في يوم"),
            Switch("en", "بالإنجليزية", "Locale=\"en\"", "لغة الشهر، ومنها أسماء الأيام والشهر وأول أيام الأسبوع. وإن تركتها فالعربية في الواجهة العربية والإنجليزية في غيرها.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:MonthCalendar", "شهر على شبكة أسابيع يميز اليوم الحالي، ويعرض في كل يوم ثلاثة مواعيد وعدد ما بقي، وعلى الهاتف تصير المواعيد نقاطًا.", OptionKind.Element, true),
            Row("الأساس", "Month", "الشهر الذي يُعرض، وأي يوم فيه يكفي، وبدونه الشهر الحالي.", OptionKind.Property),
            Row("الأساس", "Events", "المواعيد، لكل موعد `MonthEvent` بيومه `Date` وعنوانه `Title`.", OptionKind.Property),
            Row("الأحداث", "MonthChanged", "يُطلق إذا انتقلت إلى شهر آخر، ومعه أول يوم فيه.", OptionKind.Event),
            Row("الأحداث", "DayInvoked", "يُطلق إذا نُقر يوم في موضع خال من مواعيده، ومعه تاريخه.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var words = Words(s);
            var tone = s["tone"] == "Brand" ? "" : $", Tone = MonthEventTone.{s["tone"]}";
            var lines = List(s).Select((e, i) =>
                $"  new() {{ Date = new(2026, 9, {e.Day}){(s.On("time") && e.Hour >= 0 ? $", Time = new({e.Hour}, {e.Minute}, 0)" : "")}, Title = \"{words[e.Word]}\"{tone}{(s.On("link") && i == 0 ? ", Href = \"/events/1\"" : "")} }},");
            return $"<ins:MonthCalendar x:Name=\"Calendar\" Month=\"2026-09-01\"{(s.On("en") ? " Locale=\"en\" FlowDirection=\"LeftToRight\"" : "")} />\n" +
                "<!-- المواعيد من الشيفرة:\nCalendar.Events =\n[\n" + string.Join("\n", lines) + "\n];\n-->";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s.On("link")) notes.Add("صار «تسليم» يوم 3 رابطًا يشبه غيره من المواعيد، فمرر عليه المؤشر يمتلئ بلونه.");
            if (s.On("more")) notes.Add("لا يعرض اليوم أكثر من ثلاثة مواعيد، فإن زادت كتب عدد ما بقي، مثل «+2».");
            if (s.On("en")) notes.Add("صار الأسبوع يبدأ بالأحد كما يبدؤه أهل هذه اللغة، ومعها `FlowDirection` ليجري الشهر من اليسار.");
            notes.Add("انتقل إلى شهر آخر، أو انقر يومًا خاليًا، فيظهر هنا الحدث.");
            return notes;
        },
        Stage = (view, s, say) =>
        {
            var words = Words(s);
            var tone = Enum.Parse<MonthEventTone>(s["tone"]);
            foreach (var month in view.GetLogicalDescendants().OfType<MonthCalendar>())
            {
                month.Events = List(s).Select((e, i) => Event(e.Day, s.On("time") && e.Hour >= 0 ? new TimeSpan(e.Hour, e.Minute, 0) : null, words[e.Word], tone, s.On("link") && i == 0 ? "/events/1" : null)).ToList();
                month.MonthChanged += (_, m) => say($"حدث `MonthChanged`، والشهر الآن `{m:yyyy-MM}`");
                month.DayInvoked += (_, d) => say($"حدث `DayInvoked`، واليوم `{d:yyyy-MM-dd}`");
                month.EventInvoked += (_, e) => say($"حدث `EventInvoked` على «{e.Title}»");
            }
        },
    };

    public static PlayDef[] All { get; } = [Month];
}
