using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class MonthPlay
{
    public const string Example =
        """
        <ins:MonthCalendar x:Name="Calendar" Month="2026-09-01">
          <ins:MonthCalendar.EventDetails>
            <DataTemplate>
              <StackPanel Spacing="6">
                <TextBlock Classes="pop-title" Text="{Binding Title}" />
                <TextBlock Classes="pop-text" Text="{Binding Tag}" />
              </StackPanel>
            </DataTemplate>
          </ins:MonthCalendar.EventDetails>
        </ins:MonthCalendar>
        <!-- والمواعيد في Events من الشيفرة، كما في «الأحداث» -->
        """;

    // The card each event opens, as the playground writes it.
    private const string DetailsTemplate =
        """
          <ins:MonthCalendar.EventDetails>
            <DataTemplate>
              <StackPanel Spacing="6">
                <TextBlock Classes="pop-title" Text="{Binding Title}" />
                <TextBlock Classes="pop-text" Text="{Binding Tag}" />
              </StackPanel>
            </DataTemplate>
          </ins:MonthCalendar.EventDetails>
        """;

    public static string Code { get; } =
        """
        Calendar.Events =
        [
            new() { Date = new(2026, 9, 3), Title = "تسليم الطلب 1042", Tone = MonthEventTone.Warn, Href = "/orders/1042" },
            new() { Date = new(2026, 9, 8), Time = new(10, 0, 0), Duration = TimeSpan.FromMinutes(90), Title = "اجتماع الفريق", Tag = "في قاعة الاجتماعات" },
        ];

        Calendar.MonthChanged += async (_, month) => Calendar.Events = await LoadAsync(month);
        Calendar.DateChanged += async (_, day) => Calendar.Events = await LoadWeekAsync(day);   // في الأسبوع واليوم
        Calendar.DayInvoked += (_, day) => OpenNew(day);         // يوم نُقر في موضع خال من مواعيده، ومعه الساعة في الأسبوع واليوم
        Calendar.EventInvoked += (_, e) => Navigate(e.Href);     // موعد له رابط
        Calendar.ShowDetails(Calendar.Events[1]);                // يفتح بطاقة موعد من الشيفرة، وتغلقها HideDetails
        """;

    private static MonthEvent Event(int day, TimeSpan? time, string title, MonthEventTone tone = MonthEventTone.Brand, string? href = null, int minutes = 0, string? tag = null) =>
        new() { Date = new DateTime(2026, 9, day), Time = time, Title = title, Tone = tone, Href = href, Duration = minutes > 0 ? TimeSpan.FromMinutes(minutes) : null, Tag = tag };

    /// <summary>The events of the page's main example.</summary>
    public static List<MonthEvent> Sample() =>
    [
        Event(3, null, "تسليم الطلب 1042", MonthEventTone.Warn, "/orders/1042", tag: "يُسلَّم إلى المستودع قبل نهاية اليوم"),
        Event(8, new TimeSpan(10, 0, 0), "اجتماع الفريق", minutes: 90, tag: "في قاعة الاجتماعات، ساعة ونصف"),
        Event(8, new TimeSpan(14, 30, 0), "مراجعة العقد", MonthEventTone.Info, tag: "مع المستشار القانوني"),
        Event(15, null, "إطلاق الحملة", MonthEventTone.Ok, tag: "طوال اليوم، على كل القنوات"),
        Event(21, new TimeSpan(9, 0, 0), "زيارة المورد", minutes: 90, tag: "في مقر المورد، ساعة ونصف"),
        Event(21, new TimeSpan(10, 0, 0), "مكالمة العميل", tag: "مكالمة فيديو، ساعة"),
        Event(21, new TimeSpan(13, 0, 0), "تدريب", minutes: 120, tag: "للفريق الجديد، ساعتان"),
        Event(21, new TimeSpan(16, 0, 0), "موعد نهائي", MonthEventTone.Bad, minutes: 30, tag: "آخر موعد لتسليم العروض"),
        Event(26, null, "جرد المخزون", MonthEventTone.Info, tag: "طوال اليوم، في المستودع"),
        Event(22, new TimeSpan(5, 30, 0), "رحلة مبكرة", minutes: 60, tag: "من المطار، ساعة"),
        Event(22, new TimeSpan(19, 0, 0), "عشاء العمل", minutes: 90, tag: "مع الشركاء، ساعة ونصف"),
    ];

    private static readonly (int Day, int Hour, int Minute, int Word, int Length)[] Rows =
        [(3, -1, 0, 0, 0), (8, 10, 0, 1, 90), (8, 14, 30, 2, 0), (15, -1, 0, 3, 0), (21, 9, 0, 4, 90), (22, 5, 30, 9, 60), (22, 19, 0, 10, 90)];

    private static readonly (int Day, int Hour, int Minute, int Word, int Length)[] More =
        [(21, 10, 0, 5, 0), (21, 13, 0, 6, 120), (21, 16, 0, 7, 30)];

    private static IEnumerable<(int Day, int Hour, int Minute, int Word, int Length)> List(PlayState s) =>
        Rows.Concat(s.On("more") ? More : []).Append((26, -1, 0, 8, 0));

    // The line under an event's title in its card: its day and its hours.
    private static string Detail(PlayState s, (int Day, int Hour, int Minute, int Word, int Length) e)
    {
        var en = s.On("en");
        if (!s.On("time") || e.Hour < 0) return en ? $"September {e.Day}, all day" : $"{e.Day} سبتمبر، طوال اليوم";
        var from = e.Hour * 60 + e.Minute;
        var to = from + (e.Length > 0 ? e.Length : 60);
        string At(int minutes)
        {
            var (h, m) = (minutes / 60 % 24, minutes % 60);
            var twelve = h % 12 == 0 ? 12 : h % 12;
            return en ? $"{twelve}:{m:00} {(h < 12 ? "AM" : "PM")}" : $"{twelve}:{m:00} {(h < 12 ? "ص" : "م")}";
        }
        return en ? $"September {e.Day}, {At(from)} – {At(to)}" : $"{e.Day} سبتمبر، من {At(from)} إلى {At(to)}";
    }

    private static string[] Words(PlayState s) => s.On("en")
        ? ["Delivery", "Meeting", "Review", "Workshop", "Call", "Training", "Visit", "Deadline", "Holiday", "Early flight", "Team dinner"]
        : ["تسليم", "اجتماع", "مراجعة", "ورشة عمل", "مكالمة", "تدريب", "زيارة", "موعد نهائي", "إجازة", "رحلة مبكرة", "عشاء العمل"];

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
            Switch("details", "بطاقة التفاصيل", "EventDetails", "قالب بطاقة الموعد، يُبنى والموعد بياناته، فتربط فيه `Title` و`Tag` وغيرهما. وما دام موجودًا صار كل موعد يُنقر، فتفتح بطاقته بجانبه من جهة نهايته، أو من جهة بدايته إن لم يتسع لها، وإلا فتحته أو فوقه، داخل النافذة دائمًا. وتخرج البطاقة من الموعد نفسه وتعود إليه إذا أُغلقت، والنقرة الثانية على الموعد تغلقها، وكذلك `Esc`. ومن الشيفرة يفتحها `ShowDetails` ويغلقها `HideDetails`.", OptionKind.Property, "الموعد"),
            Switch("titlestart", "العنوان أولًا", "HeaderLayout", "ترتيب الرأس. والأصل `Centered`: «اليوم» ومفتاح ساعات الدوام في أوله، والتاريخ بين سهميه في وسطه، ومبدّل العرض في آخره، فإن ضاق السطر نزل التاريخ إلى سطر وحده. واكتب `HeaderLayout=\"TitleStart\"` ليكون التاريخ في أوله والأزرار كلها معًا في آخره.", OptionKind.Property),
            Switch("allhours", "اليوم كله", "WorkHoursOnly", "يعرض الأسبوع واليوم ساعات الدوام وحدها، وهذا هو الأصل. والموعد الذي يقع كله خارجها لا يختفي، بل يُعد في شريط فوق الساعات أو تحتها، مثل «+2»، فإن نقرته ظهر اليوم كله عند أول تلك المواعيد. والموعد الذي يتجاوز طرف الدوام يُقص عنده بزاوية مستقيمة. واكتب `WorkHoursOnly=\"False\"` ليبدأ باليوم كله في صندوق بارتفاع الدوام، تمر بساعاته بالتمرير ويبدأ عند أول الدوام.", OptionKind.Property, "الأسبوع واليوم"),
            Switch("hours", "دوام من 8 إلى 17", "DayStartHour", "أول ساعة في الدوام، وهي 6 إن لم تذكرها، ومعها `DayEndHour` الساعة التي ينتهي عندها، وهي 18. وتكون الساعة بين 0 و24، والنهاية بعد البداية.", OptionKind.Property, "الأسبوع واليوم"),
            Switch("nohours", "بلا مفتاح الساعات", "ShowHoursSwitch", "اكتب `ShowHoursSwitch=\"False\"` ليختفي مفتاح «ساعات الدوام» من الرأس، ويبقى ما في `WorkHoursOnly`.", OptionKind.Property, "الأسبوع واليوم"),
            Switch("noswitch", "بلا مبدّل العرض", "ShowViewSwitch", "اكتب `ShowViewSwitch=\"False\"` ليختفي مبدّل الشهر والأسبوع واليوم من الرأس، ويبقى العرض الذي في `View`.", OptionKind.Property),
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
            Row("الموعد", "Tag", "على `MonthEvent`: ما تحفظه الصفحة مع الموعد، كسجله أو سطر عنه، لتعرضه بطاقته.", OptionKind.Property),
            Row("الأحداث", "DayInvoked", "يُطلق إذا نُقر يوم في موضع خال من مواعيده، ومعه تاريخه، وفي الأسبوع واليوم ساعته أيضًا.", OptionKind.Event),
        ],
        Disable = s => s["view"] == "Month" ? ["allhours", "hours", "nohours"] : [],
        Axaml = s =>
        {
            var words = Words(s);
            var tone = s["tone"] == "Brand" ? "" : $", Tone = MonthEventTone.{s["tone"]}";
            var lines = List(s).Select((e, i) =>
                $"  new() {{ Date = new(2026, 9, {e.Day}){(s.On("time") && e.Hour >= 0 ? $", Time = new({e.Hour}, {e.Minute}, 0)" : "")}{(s.On("time") && e.Hour >= 0 && e.Length > 0 ? $", Duration = TimeSpan.FromMinutes({e.Length})" : "")}, Title = \"{words[e.Word]}\"{tone}{(s.On("link") && i == 0 ? ", Href = \"/events/1\"" : "")}{(s.On("details") ? $", Tag = \"{Detail(s, e)}\"" : "")} }},");
            var view = s["view"] == "Month" ? "" : $" View=\"{s["view"]}\" Date=\"2026-09-21\"";
            var week = s["view"] != "Month";
            var hours = (week && s.On("allhours") ? " WorkHoursOnly=\"False\"" : "") + (week && s.On("hours") ? " DayStartHour=\"8\" DayEndHour=\"17\"" : "") + (week && s.On("nohours") ? " ShowHoursSwitch=\"False\"" : "");
            var sw = s.On("noswitch") ? " ShowViewSwitch=\"False\"" : "";
            var head = s.On("titlestart") ? " HeaderLayout=\"TitleStart\"" : "";
            var open = $"<ins:MonthCalendar x:Name=\"Calendar\" Month=\"2026-09-01\"{view}{hours}{sw}{head}{(s.On("en") ? " Locale=\"en\" FlowDirection=\"LeftToRight\"" : "")}";
            var calendar = s.On("details") ? open + ">\n" + DetailsTemplate + "\n</ins:MonthCalendar>\n" : open + " />\n";
            return calendar + "<!-- المواعيد من الشيفرة:\nCalendar.Events =\n[\n" + string.Join("\n", lines) + "\n];\n-->";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["view"] != "Month")
            {
                notes.Add("يقع كل موعد في ساعته بطول مدته، وإذا تداخل موعدان صارا جنبًا إلى جنب، والمواعيد بلا وقت في صف «طوال اليوم». ويمر خط أحمر على الساعة الحالية في اليوم الحالي.");
                notes.Add(s.On("allhours") ? "يعرض اليوم كله في صندوق بارتفاع الدوام، ويبدأ عند أول الدوام، فمرر لترى ما قبله وما بعده." : $"يعرض ساعات الدوام وحدها، {(s.On("hours") ? "من 8:00 ص إلى 5:00 م" : "من 6:00 ص إلى 6:00 م")}. {(s.On("hours") ? "وفي يوم 22 موعد قبلها وآخر بعدها، فتجد عدد كل منهما في شريط فوق الساعات أو تحتها" : "وفي يوم 22 موعد يبدأ قبلها فيُقص عند أولها، وآخر بعدها فتجد عدده في شريط تحت الساعات")}، انقره ليظهر اليوم كله عند موعده. ومفتاح «ساعات الدوام» في الرأس يبدّل بين الحالين.");
                if (s["view"] == "Week") notes.Add("انقر اسم يوم في رأس الأسبوع فيفتح يومه.");
                if (!s.On("more")) notes.Add("شغّل «أكثر من ثلاثة في يوم» لترى موعدين متداخلين يوم 21.");
            }
            if (s.On("details")) notes.Add("انقر أي موعد فتخرج بطاقته منه إلى جانبه، أو إلى ما تحته أو فوقه إذا ضاق الجانبان. وانقره ثانية أو اضغط `Esc` فتعود إليه.");
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
                month.Events = List(s).Select((e, i) => Event(e.Day, s.On("time") && e.Hour >= 0 ? new TimeSpan(e.Hour, e.Minute, 0) : null, words[e.Word], tone, s.On("link") && i == 0 ? "/events/1" : null, e.Length, s.On("details") ? Detail(s, e) : null)).ToList();
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
