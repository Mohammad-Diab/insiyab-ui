using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;
using TimelineControl = Insiyab.Avalonia.Controls.Timeline;

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

    public sealed class EventRow : INotifyPropertyChanged
    {
        private string? _tone;
        private bool _isNow, _isAhead, _isNew;
        public string Title { get; init; } = "";
        public string? Body { get; init; }
        public DateTime? At { get; init; }
        public Geometry? Icon { get; init; }
        public string? Tone { get => _tone; set => Set(ref _tone, value); }
        public bool IsNow { get => _isNow; set => Set(ref _isNow, value); }
        public bool IsAhead { get => _isAhead; set => Set(ref _isAhead, value); }
        public bool IsNew { get => _isNew; set => Set(ref _isNew, value); }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public sealed class EventSample
    {
        public ObservableCollection<EventRow> Events { get; } = [];
    }

    public const string DataExample =
        """
        <ins:Timeline x:Name="Feed"
                      ItemsSource="{Binding Events}"
                      HeaderBinding="{Binding Title}"
                      ContentBinding="{Binding Body}"
                      AtBinding="{Binding At}"
                      IconBinding="{Binding Icon}"
                      ToneBinding="{Binding Tone}"
                      IsCurrentBinding="{Binding IsNow}"
                      IsPendingBinding="{Binding IsAhead}"
                      IsUnreadBinding="{Binding IsNew}" />
        <StackPanel Orientation="Horizontal" Spacing="8">
          <Button x:Name="ReadAll" Classes="secondary sm" Content="علّم الكل مقروءًا" />
          <Button x:Name="Approve" Classes="secondary sm" Content="اعتمد المراجعة" />
        </StackPanel>
        """;

    public static string DataCode { get; } =
        """
        Feed.DayOf = item => ((EventRow)item!).At;    // عنوان قبل أول حدث من كل يوم
        Feed.DayHeader = day => day.ToString("d/M");  // اختياري: نص العنوان بدل «اليوم» و«أمس»
        """;

    public static Action<Control> DataStage { get; } = view =>
    {
        Geometry? Icon(string key) => Application.Current!.TryFindResource(key, out var found) ? found as Geometry : null;
        var now = DateTime.Now;
        var day = DateTime.Today;
        var sample = new EventSample();
        sample.Events.Add(new EventRow { Title = "أُنشئ الملف", At = day.AddDays(-4).AddHours(9), Icon = Icon("IconFile"), Tone = "ok" });
        sample.Events.Add(new EventRow { Title = "أُرفقت الصور", Body = "ثلاث صور من الموقع بدقتها الكاملة.", At = day.AddDays(-4).AddHours(11.5), Icon = Icon("IconUpload"), Tone = "ok" });
        sample.Events.Add(new EventRow { Title = "عُدّلت التكلفة", Body = "تغيرت تكلفة البند الثاني بعد مراجعة العرض.", At = day.AddDays(-1).AddHours(10), Icon = Icon("IconEdit"), Tone = "info" });
        sample.Events.Add(new EventRow { Title = "أُضيفت ملاحظة", Body = "يُرجى التأكد من المقاسات قبل الطلب.", At = day.AddDays(-1).AddHours(16.25), Icon = Icon("IconMessage"), Tone = "info", IsNew = true });
        sample.Events.Add(new EventRow { Title = "أُرسل للمراجعة", At = now.AddMinutes(-90), Icon = Icon("IconShare"), Tone = "ok" });
        sample.Events.Add(new EventRow { Title = "قيد المراجعة", Body = "يراجعه فريق الجودة الآن.", At = now.AddMinutes(-20), Icon = Icon("IconClock"), Tone = "warn", IsNow = true, IsNew = true });
        sample.Events.Add(new EventRow { Title = "الاعتماد", Icon = Icon("IconCheck"), IsAhead = true });
        var feed = view.GetLogicalDescendants().OfType<TimelineControl>().First();
        feed.DayOf = item => ((EventRow)item!).At;
        feed.DataContext = sample;
        var buttons = view.GetLogicalDescendants().OfType<Button>().ToList();
        buttons.First(b => b.Name == "ReadAll").Click += (_, _) => { foreach (var e in sample.Events) e.IsNew = false; };
        buttons.First(b => b.Name == "Approve").Click += (_, _) =>
        {
            sample.Events[5].Tone = "ok";
            sample.Events[5].IsNow = false;
            sample.Events[6].IsAhead = false;
            sample.Events[6].IsNow = true;
        };
    };

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
            Row("الحدث", "IsUnread", "حدث لم يُقرأ بعد: نقطة صغيرة بلون الهوية بعد عنوانه.", OptionKind.Property),
            Row("من البيانات", "ItemsSource", "قائمة يصير كل عنصر فيها حدثًا، وتأخذ الأحداث عناوينها ونصوصها وأوقاتها وأيقوناتها من أعضائه بـ `HeaderBinding` و`ContentBinding` و`TimeBinding` أو `AtBinding` و`IconBinding`.", OptionKind.Property),
            Row("من البيانات", "ToneBinding", "نبرة كل حدث من عضو في عنصره، `ok` أو `bad` أو `warn` أو `info`، فإذا تغير العضو تغير لون الحدث في مكانه.", OptionKind.Property),
            Row("من البيانات", "IsCurrentBinding", "الحدث الجاري، ومثله `IsPendingBinding` لما لم يأت بعد و`IsUnreadBinding` لما لم يُقرأ.", OptionKind.Property),
            Row("من البيانات", "DayOf", "من الشيفرة: دالة تعطي تاريخ كل عنصر، فيوضع عنوان قبل أول حدث من كل يوم: «اليوم» أو «أمس» أو التاريخ، أو ما تكتبه `DayHeader`.", OptionKind.Property),
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
