using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class PickersPlay
{
    private const string Cities =
        "  <AutoCompleteBox.ItemsSource>\n" +
        "    <col:AvaloniaList x:TypeArguments=\"x:String\">\n" +
        "      <x:String>الرياض</x:String>\n" +
        "      <x:String>جدّة</x:String>\n" +
        "      <x:String>القاهرة</x:String>\n" +
        "      <x:String>الإسكندريّة</x:String>\n" +
        "    </col:AvaloniaList>\n" +
        "  </AutoCompleteBox.ItemsSource>\n";

    public const string Example =
        """
        <UniformGrid Rows="1" ColumnSpacing="16">
          <StackPanel Classes="field">
            <TextBlock Classes="label" Text="المدينة" />
            <AutoCompleteBox TextFilter="{x:Static ins:ArabicMatch.Contains}">
              <AutoCompleteBox.ItemsSource>
                <col:AvaloniaList x:TypeArguments="x:String">
                  <x:String>الرياض</x:String>
                  <x:String>جدّة</x:String>
                  <x:String>القاهرة</x:String>
                </col:AvaloniaList>
              </AutoCompleteBox.ItemsSource>
            </AutoCompleteBox>
          </StackPanel>
          <StackPanel Classes="field">
            <TextBlock Classes="label" Text="التاريخ" />
            <ins:DateField SelectedDate="2026-09-30" />
          </StackPanel>
          <StackPanel Classes="field">
            <TextBlock Classes="label" Text="الوقت" />
            <ins:TimeField SelectedTime="09:30" />
          </StackPanel>
        </UniformGrid>
        """;

    public const string Native =
        """
        <CalendarDatePicker SelectedDate="2026-09-30" Width="300" />
        <DatePicker />
        <TimePicker />
        <Calendar DisplayDate="2026-09-30" SelectedDate="2026-09-30" />
        """;

    public static PlayDef Combo { get; } = new()
    {
        Id = "combo",
        Title = "الإكمال التلقائي",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Switch("arabic", "مطابقة تفهم العربية", "TextFilter", "مع `{x:Static ins:ArabicMatch.Contains}`: مطابقة لا تلتفت إلى التشكيل، وتعد صور الهمزة على الألف حرفًا واحدًا، وتساوي بين التاء المربوطة والهاء، وبين الألف المقصورة والياء، وتجد الكلمة في وسط الخيار أيضًا.", OptionKind.Property, on: true),
            Switch("complete", "يكمل النص في الحقل", "IsTextCompletionEnabled", "يكتب في الحقل بقية أول خيار يطابق ما كُتب، محددةً فيستبدلها ما يُكتب بعدها.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "AutoCompleteBox", "حقل وقائمة خيارات تنفتح تحته وهو يكتب، تتنقل فيها الأسهم، و`Enter` للاختيار و`Esc` للإغلاق. وخياراته في `ItemsSource`، و`col` فيه هو `xmlns:col=\"using:Avalonia.Collections\"`.", OptionKind.Element, true),
            Row("الأساس", "MinimumPrefixLength", "كم حرفًا يُكتب قبل أن تنفتح القائمة، وهو حرف واحد إن لم تذكره.", OptionKind.Property),
            Row("الأساس", "SelectionChanged", "يُطلق عند الاختيار، والخيار المختار في `SelectedItem`.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var attrs = (s.On("arabic") ? " TextFilter=\"{x:Static ins:ArabicMatch.Contains}\"" : "") + (s.On("complete") ? " IsTextCompletionEnabled=\"True\"" : "");
            return $"<AutoCompleteBox{attrs} AutomationProperties.Name=\"المدينة\">\n" + Cities + "</AutoCompleteBox>";
        },
        Notes = s =>
        {
            var notes = new List<string>
            {
                s.On("arabic")
                    ? "اكتب «جده» أو «اسكندرية» فتجد المدينة كما كُتبت في القائمة، وإذا اخترت مدينة ظهر هنا الحدث."
                    : "اكتب «جده» فلا تجد شيئًا، فالمطابقة الافتراضية حرفًا بحرف ومن أول الخيار. ثم شغّل المطابقة العربية وجرب ثانية.",
            };
            if (s.On("complete")) notes.Add("اكتب «الر» فيكمل الحقل «الرياض».");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var box in view.GetLogicalDescendants().OfType<AutoCompleteBox>())
                box.SelectionChanged += (_, _) => { if (box.SelectedItem is string city) say($"حدث `SelectionChanged`، و`SelectedItem` الآن «{city}»"); };
        },
    };

    public static PlayDef Date { get; } = new()
    {
        Id = "date",
        Title = "التاريخ",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Select("mode", "ما يُختار", "day",
                Choice("day", "يوم واحد"),
                Choice("range", "فترة بين تاريخين", "Mode=\"Range\"", "فترة في حقل واحد: النقرة الأولى تحدد البداية والثانية النهاية، والنهاية في `EndDate`.", OptionKind.Property),
                Choice("datetime", "تاريخ ووقت", "Mode=\"DateTime\"", "تقويم وإلى جانبه أعمدة للوقت، والوقت في `SelectedTime`، والخطوة بالدقائق في `MinuteStep`.", OptionKind.Property),
                Choice("pair", "تاريخ ووقت في حقلين", "ins:DateTimeGroup", "حقل تاريخ وحقل وقت في إطار واحد، و`Value` يحملهما معًا، `2026-09-26T14:30`.", OptionKind.Element)),
            Switch("bounds", "حدود للتاريخ", "MinDate", "أول يوم يُختار، فتبهت الأيام قبله في التقويم، ويُرفض ما يُكتب قبله برسالة.", OptionKind.Property, "الحدود"),
        ],
        Extra =
        [
            Row("الأساس", "ins:DateField", "يكتب التاريخ بلغة الواجهة، ويقبل كتابته، ويفتح تقويمًا يبدأ أسبوعه يوم السبت في الواجهة العربية، وتتنقل فيه الأسهم بين الأيام مع اتجاهها.", OptionKind.Element, true),
            Row("الأساس", "SelectedDate", "التاريخ المختار، ويُكتب في الشيفرة `2026-09-30`.", OptionKind.Property),
            Row("الأساس", "Value", "التاريخ كما يُرسل إلى الخادم، `2026-09-30`، أو `2026-09-26T14:30` مع الوقت.", OptionKind.Property),
            Row("الأساس", "CalendarKind=\"Hijri\"", "تقويم أم القرى، ويبقى `SelectedDate` ميلاديًا. ومعه `ShowGregorian` يظهر في التقويم مفتاح يبدّل إلى الميلادي.", OptionKind.Property),
            Row("الأساس", "DateChanged", "يُطلق حين يغيّر المستخدم التاريخ، ومعه `Date` و`End` و`Time` و`Value`.", OptionKind.Event),
            Row("الحدود", "MaxDate", "آخر يوم يُختار.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var b = s.On("bounds") ? " MinDate=\"2026-09-01\" MaxDate=\"2026-10-31\"" : "";
            var (label, field) = s["mode"] switch
            {
                "range" => ("الفترة", $"<ins:DateField Mode=\"Range\" SelectedDate=\"2026-09-16\" EndDate=\"2026-09-23\"{b} />"),
                "datetime" => ("الموعد", $"<ins:DateField Mode=\"DateTime\" SelectedDate=\"2026-09-26\" SelectedTime=\"14:30\" MinuteStep=\"15\"{b} />"),
                "pair" => ("الموعد", $"<ins:DateTimeGroup SelectedDate=\"2026-09-26\" SelectedTime=\"14:30\"{b} />"),
                _ => ("التاريخ", $"<ins:DateField SelectedDate=\"2026-09-30\"{b} />"),
            };
            return "<StackPanel Classes=\"field\">\n" + $"  <TextBlock Classes=\"label\" Text=\"{label}\" />\n" + $"  {field}\n" + "</StackPanel>";
        },
        Notes = _ => ["اضغط زر التقويم لتراه بالخيارات التي اخترتها، وإذا اخترت تاريخًا ظهر هنا الحدث."],
        Stage = (view, _, say) =>
        {
            foreach (var field in view.GetLogicalDescendants().OfType<DateField>())
                field.DateChanged += (_, e) => say($"حدث `DateChanged`، و`Value` الآن `{e.Value}`");
        },
    };

    public static PlayDef Time { get; } = new()
    {
        Id = "time",
        Title = "الوقت",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Seg("step", "الخطوة بالدقائق", "",
                Choice("15", "15", "MinuteStep", "كم دقيقة تقفز الدقائق في عمودها، ونصف ساعة إن لم تذكرها.", OptionKind.Property),
                Choice("", "30"),
                Choice("60", "60")),
            Switch("bounds", "حدود للوقت", "MinTime", "أول وقت يُختار، ويُرفض ما يُكتب قبله برسالة.", OptionKind.Property, "الحدود"),
        ],
        Extra =
        [
            Row("الأساس", "ins:TimeField", "يعرض الوقت كما تكتبه لغة الواجهة، ويقبل كتابته مثل «14:30» و«2:30 م»، وتظهر تحته أعمدة للساعة والدقيقة والفترة. والوقت في `SelectedTime`، وما يُرسل في `Value` بالصيغة `HH:MM`.", OptionKind.Element, true),
            Row("الأساس", "TimeChanged", "يُطلق حين يغيّر المستخدم الوقت.", OptionKind.Event),
            Row("الحدود", "MaxTime", "آخر وقت يُختار.", OptionKind.Property),
        ],
        Axaml = s =>
            "<StackPanel Classes=\"field\">\n" +
            "  <TextBlock Classes=\"label\" Text=\"الموعد\" />\n" +
            $"  <ins:TimeField SelectedTime=\"09:00\"{(s.Has("step") ? $" MinuteStep=\"{s["step"]}\"" : "")}{(s.On("bounds") ? " MinTime=\"08:00\" MaxTime=\"18:00\"" : "")} />\n" +
            "</StackPanel>",
        Notes = _ => ["افتح الحقل لترى الأعمدة بالخطوة والحدود التي اخترتها، وإذا اخترت وقتًا ظهر هنا الحدث."],
        Stage = (view, _, say) =>
        {
            foreach (var field in view.GetLogicalDescendants().OfType<TimeField>())
                field.TimeChanged += (_, _) => say($"حدث `TimeChanged`، و`Value` الآن `{field.Value}`");
        },
    };

    public static PlayDef Range { get; } = new()
    {
        Id = "range",
        Title = "المنزلق",
        Wide = _ => true,
        WideMax = 480,
        Controls =
        [
            Seg("step", "الخطوة", "5",
                Choice("1", "1"),
                Choice("5", "5"),
                Choice("10", "10", "TickFrequency", "مقدار كل خطوة، بالسحب وبالأسهم، ومعه `IsSnapToTickEnabled=\"True\"`.", OptionKind.Property)),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يبهت المنزلق ولا يتحرك.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "Slider", "يملأ المسار بلون الهوية حتى المقبض، ولا بد له من تسمية. وفي الواجهة العربية يقع الحد الأدنى على اليمين.", OptionKind.Element, true),
            Row("الأساس", "Minimum", "ومعها `Maximum` و`Value`، كما في أي منزلق.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var value = 20 + int.Parse(s["step"]);
            return $"<Slider Minimum=\"0\" Maximum=\"50\" TickFrequency=\"{s["step"]}\" IsSnapToTickEnabled=\"True\" Value=\"{value}\" AutomationProperties.Name=\"النسبة\"{(s.On("disabled") ? " IsEnabled=\"False\"" : "")} />";
        },
        Notes = s => [$"هكذا يبدو بعد خطوة واحدة: من 20 إلى `{20 + int.Parse(s["step"])}`."],
    };

    public static PlayDef Stepper { get; } = new()
    {
        Id = "stepper",
        Title = "العداد",
        Controls =
        [
            Seg("step", "الخطوة", "",
                Choice("", "1"),
                Choice("5", "5", "Increment", "مقدار كل ضغطة، وهو 1 إن لم تذكره.", OptionKind.Property)),
            Switch("bounds", "حدود للعدد", "Minimum", "أقل عدد، فيتعطل زر الإنقاص عنده.", OptionKind.Property, "الحدود", on: true),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يتعطل الحقل وزراه معًا.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "NumericUpDown", "حقل رقمي بين زرين، يزيده أحدهما وينقصه الآخر، وتزيده الأسهم وعجلة الفأرة أيضًا. وقيمته في `Value`، و`ValueChanged` يُطلق عند كل تغيير.", OptionKind.Element, true),
            Row("الحدود", "Maximum", "أكبر عدد، فيتعطل زر الزيادة عنده.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var value = 5 + (s.Has("step") ? 5 : 1);
            return $"<NumericUpDown Width=\"137\" Value=\"{value}\"{(s.On("bounds") ? " Minimum=\"0\" Maximum=\"20\"" : "")}{(s.Has("step") ? $" Increment=\"{s["step"]}\"" : "")} AutomationProperties.Name=\"العدد\"{(s.On("disabled") ? " IsEnabled=\"False\"" : "")} />";
        },
        Notes = s =>
        {
            if (s.On("disabled")) return [];
            var notes = new List<string> { $"هكذا يبدو بعد ضغطة واحدة على +: من 5 إلى `{5 + (s.Has("step") ? 5 : 1)}`." };
            if (s.On("bounds")) notes.Add("اضغط − حتى الصفر، فيتعطل الزر عنده.");
            return notes;
        },
    };

    public static PlayDef[] All { get; } = [Combo, Date, Time, Range, Stepper];
}
