using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class TimelinePlay
{
    public const string Example =
        """
        <ins:Timeline>
          <ins:TimelineDay Content="24 سبتمبر" />
          <ins:TimelineItem Classes="ok" Header="استُلم الطلب" Time="8:02 ص" />
          <ins:TimelineItem Classes="ok" Header="تم الدفع" Time="8:05 ص" Content="نص الحدث." />
          <ins:TimelineItem Header="قيد التجهيز" IsCurrent="True" />
          <ins:TimelineItem Header="التسليم" IsPending="True" />
        </ins:Timeline>
        """;

    public static string Code { get; } =
        """
        Feed.Items.Add(new TimelineItem
        {
            Header = "تم الشحن",
            At = DateTime.Now,
            TimeMode = TimeMode.Relative,   // يكتب وقته النسبي من جديد كل دقيقة وحده
        });
        """;

    private static readonly (string Title, string Icon, string At, string Said, int Ago, bool Done, string? Body)[] Events =
    [
        ("أُنشئ الملف", "IconFile", "2026-09-24T08:02", "8:02 ص", 26 * 60, true, null),
        ("أُرسل للمراجعة", "IconUpload", "2026-09-24T08:05", "8:05 ص", 5, true, "نص قصير عن الحدث."),
        ("قيد المراجعة", "IconClock", "", "", 0, false, null),
        ("الاعتماد", "IconCheck", "", "", 0, false, null),
    ];

    public static PlayDef Timeline { get; } = new()
    {
        Id = "timeline",
        Title = "الخط الزمني",
        Wide = _ => true,
        Controls =
        [
            Seg("layout", "التخطيط", "",
                Choice("", "عادي"),
                Choice("compact", "مضغوط", "compact", "نقاط أصغر ومسافات أقل، لعمود ضيق."),
                Choice("split", "على جانبين", "split", "يوزع الأحداث على جانبي خط في الوسط إذا بلغ عرض القائمة 640 بكسلًا، ويجمعها في جانب واحد إذا ضاقت.")),
            Select("tone", "نبرة ما مضى", "ok",
                Choice("", "الأساسي"),
                Choice("ok", "نجاح", "ok", "على `ins:TimelineItem`: حدث بلون النجاح."),
                Choice("bad", "خطر", "bad", "حدث بلون الخطر."),
                Choice("warn", "تحذير", "warn", "حدث بلون التحذير."),
                Choice("info", "معلومة", "info", "حدث بلون المعلومة.")).In("الحدث"),
            Seg("time", "الوقت", "fixed",
                Choice("", "بلا"),
                Choice("fixed", "مكتوب", "Time", "الوقت كما تكتبه، يظهر بجانب العنوان.", OptionKind.Property),
                Choice("ago", "نسبي", "TimeMode=\"Relative\"", "يكتب الوقت من `At` كما يقوله الناس، «قبل 5 دقائق» و«أمس»، ويجدده كل دقيقة. فإذا مضى أسبوع كتب التاريخ.", OptionKind.Property),
                Choice("date", "تاريخ", "TimeMode=\"Date\"", "يكتب التاريخ من `At` دائمًا بدل الوقت النسبي.", OptionKind.Property)),
            Switch("current", "حدث جار", "IsCurrent=\"True\"", "الحدث الجاري الآن، حول نقطته هالة بلونها.", OptionKind.Property, "الحدث", on: true),
            Switch("pending", "حدث لم يأت", "IsPending=\"True\"", "حدث لم يأت بعد، نقطته فارغة والخط إليه متقطع.", OptionKind.Property, "الحدث", on: true),
            Switch("icons", "بالأيقونات", "Icon", "رقاقة فيها أيقونة تحل مكان النقطة، بلون نبرته.", OptionKind.Property, "الحدث"),
            Switch("body", "بنص", "Content", "نص الحدث تحت عنوانه، ولك أن تضع مكانه لوحًا أو بطاقة.", OptionKind.Property, "الحدث", on: true),
            Switch("day", "عنوان اليوم", "ins:TimelineDay", "عنصر في القائمة يفصل بين الأيام بعنوان.", OptionKind.Element, on: true),
        ],
        Extra =
        [
            Row("الأساس", "ins:Timeline", "قائمة الأحداث، لكل حدث فيها نقطة على الخط.", OptionKind.Element, true),
            Row("الحدث", "ins:TimelineItem", "الحدث، وعنوانه في `Header`.", OptionKind.Element),
            Row("الحدث", "At", "متى وقع الحدث، للوقت النسبي والتاريخ، ويظهر كاملًا في تلميحه.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var items = Events.Select(e =>
            {
                var attrs = Classes(e.Done ? s["tone"] : null) + $" Header=\"{e.Title}\"";
                if (e.Done && s["time"] == "fixed") attrs += $" Time=\"{e.Said}\"";
                if (e.Done && s["time"] == "ago") attrs += $" At=\"{e.At}\" TimeMode=\"Relative\"";
                if (e.Done && s["time"] == "date") attrs += $" At=\"{e.At}\" TimeMode=\"Date\"";
                if (e.Body is not null && s.On("body")) attrs += $" Content=\"{e.Body}\"";
                if (s.On("icons")) attrs += $" Icon=\"{{StaticResource {e.Icon}}}\"";
                if (e.Title == "قيد المراجعة" && s.On("current")) attrs += " IsCurrent=\"True\"";
                if (e.Title == "الاعتماد" && s.On("pending")) attrs += " IsPending=\"True\"";
                return $"  <ins:TimelineItem{attrs} />";
            });
            return $"<ins:Timeline{Classes(s["layout"])}>\n" + (s.On("day") ? "  <ins:TimelineDay Content=\"24 سبتمبر\" />\n" : "") +
                string.Join("\n", items) + "\n</ins:Timeline>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["time"] == "ago") notes.Add("يُحسب الوقت النسبي من `At`، وقد جعلته المعاينة قبل 26 ساعة و5 دقائق لترى «أمس» و«قبل 5 دقائق».");
            if (s["time"] is "ago" or "date") notes.Add("يحمل تلميح الوقت التاريخ والوقت كاملين.");
            if (s["layout"] == "split") notes.Add("تتوزع الأحداث على جانبين إذا بلغ عرض القائمة 640 بكسلًا.");
            return notes;
        },
        Stage = (view, s, _) =>
        {
            if (s["time"] != "ago") return;
            var done = view.GetLogicalDescendants().OfType<TimelineItem>().Where(i => i.At is not null).ToList();
            for (var i = 0; i < done.Count && i < 2; i++) done[i].At = DateTime.Now.AddMinutes(-Events[i].Ago);
        },
    };

    public static PlayDef[] All { get; } = [Timeline];
}
