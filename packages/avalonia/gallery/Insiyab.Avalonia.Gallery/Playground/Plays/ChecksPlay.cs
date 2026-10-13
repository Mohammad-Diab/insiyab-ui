using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ChecksPlay
{
    public const string Example =
        """
        <CheckBox Content="محدد" IsChecked="True" />
        <RadioButton GroupName="demo" Content="دائرة" IsChecked="True" />
        <ToggleSwitch Content="مفتاح" IsChecked="True" />
        """;

    public static PlayDef Check { get; } = new()
    {
        Id = "check",
        Title = "مربع الاختيار وزر الاختيار",
        Controls =
        [
            Seg("type", "النوع", "checkbox",
                Choice("checkbox", "مربع"),
                Choice("radio", "دائرة")),
            Text("text", "النص", "الأول"),
            Switch("checked", "محدد", on: true),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يعطل الخيار ويبهت لونه، فلا يُنقر.", OptionKind.Property),
            Switch("deselect", "الإلغاء بنقرة ثانية", "ins:Choice.Deselect", "لأزرار الاختيار، على الزر أو أي عنصر يحويه، كالمجموعة أو النافذة كلها: إذا نقرت الزر المختار مرة ثانية أُلغي اختياره، وكذلك مفتاح المسافة.", OptionKind.Property),
            Switch("mixed", "بعضها محدد", "IsChecked=\"{x:Null}\"", "حالة «بعضها محدد» لمربع يحدد الكل. ومعها `IsThreeState=\"True\"` تصير حالة ثالثة يمر بها المربع إذا نُقر.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "CheckBox", "مربع الاختيار، ونصه في `Content`، فإذا نقرت النص تبدلت حالته.", OptionKind.Element, true),
            Row("الأساس", "RadioButton", "زر الاختيار. والأزرار التي تشترك في `GroupName` مجموعة لا يُختار منها إلا واحد.", OptionKind.Element, true),
            Row("الأساس", "IsChecked", "حالة الخيار، و`IsCheckedChanged` يُطلق عند كل تغيير.", OptionKind.Property, true),
        ],
        Disable = s => s["type"] == "radio" ? ["mixed"] : ["deselect"],
        Axaml = s =>
        {
            var off = s.On("disabled") ? " IsEnabled=\"False\"" : "";
            if (s["type"] == "checkbox")
            {
                var state = s.On("mixed") ? " IsChecked=\"{x:Null}\"" : s.On("checked") ? " IsChecked=\"True\"" : "";
                return $"<CheckBox Content=\"{s["text"]}\"{state}{off} />";
            }
            return $"<StackPanel Spacing=\"16\" AutomationProperties.Name=\"الخيارات\"{(s.On("deselect") ? " ins:Choice.Deselect=\"True\"" : "")}>\n" +
                $"  <RadioButton GroupName=\"options\" Content=\"{s["text"]}\"{(s.On("checked") ? " IsChecked=\"True\"" : "")}{off} />\n" +
                $"  <RadioButton GroupName=\"options\" Content=\"الثاني\"{off} />\n" +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["type"] == "radio") notes.Add("زر الاختيار لا يأتي وحده، فالأزرار التي تشترك في `GroupName` مجموعة واحدة لا يُختار منها إلا واحد.");
            if (s["type"] == "radio" && s.On("deselect")) notes.Add("انقر الزر المختار مرة ثانية فيُلغى، ولا يبقى في المجموعة اختيار.");
            if (s.On("mixed") && s["type"] == "checkbox") notes.Add("نقرة على المربع تنهي حالة «بعضها محدد»، وغيّر أي خيار لتعيدها.");
            return notes;
        },
    };

    public static PlayDef Toggle { get; } = new()
    {
        Id = "switch",
        Title = "المفتاح",
        Controls =
        [
            Text("label", "التسمية", "الإشعارات"),
            Switch("checked", "مفعّل", on: true),
            Switch("desc", "وصف", "ins:Choice.Description", "سطر وصف تحت التسمية، إن شئت.", OptionKind.Property),
            Switch("grid", "في شبكة", "switch-grid", "على `ins:ChoiceGrid`: يرتب المفاتيح في شبكة من البطاقات، تليق بشاشة الإعدادات."),
            Switch("card", "في بطاقة", "card", "يضع المفتاح الواحد في إطار، لإعداد هو سطر وحده."),
        ],
        Extra =
        [
            Row("الأساس", "ToggleSwitch", "لكل إعداد له حالتان، وتسميته في `Content`.", OptionKind.Element, true),
        ],
        Disable = s => s.On("grid") ? ["card"] : [],
        Wide = s => s.On("grid"),
        Axaml = s =>
        {
            string One(string label, bool on, bool card) =>
                $"<ToggleSwitch{Classes(card ? "card" : null)} Content=\"{label}\"{(on ? " IsChecked=\"True\"" : "")}{(s.On("desc") ? " ins:Choice.Description=\"وصف قصير\"" : "")} />";
            if (!s.On("grid")) return One(s["label"], s.On("checked"), s.On("card"));
            return "<ins:ChoiceGrid Classes=\"switch-grid\">\n" +
                $"  {One(s["label"], s.On("checked"), false)}\n" +
                $"  {One("التقرير اليومي", false, false)}\n" +
                "</ins:ChoiceGrid>";
        },
        Notes = s => s.On("grid") ? ["المفاتيح في الشبكة بطاقات من تلقاء نفسها، فلا حاجة فيها إلى `card`."] : [],
    };

    public static PlayDef Tile { get; } = new()
    {
        Id = "tile",
        Title = "بطاقات الاختيار",
        Wide = _ => true,
        Controls =
        [
            Seg("type", "النوع", "radio",
                Choice("radio", "واحد منها"),
                Choice("checkbox", "أكثر من واحد")),
            Switch("desc", "وصف", "ins:Choice.Description", "سطر وصف تحت العنوان.", OptionKind.Property, on: true),
            Switch("disabled", "بطاقة معطلة", "IsEnabled=\"False\"", "على البطاقة، فتبهت ولا تُختار.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "tile", "على `RadioButton` أو `CheckBox`، لخيار يحتاج إلى أكثر من سطر. وما البطاقة إلا مظهر لحالة `IsChecked`، وعنوانها في `Content`.", OptionKind.Class, true),
            Row("الأساس", "tile-grid", "على `ins:ChoiceGrid`: يرتب البطاقات في شبكة.", OptionKind.Class, true),
        ],
        Axaml = s =>
        {
            var tag = s["type"] == "radio" ? "RadioButton" : "CheckBox";
            var group = s["type"] == "radio" ? " GroupName=\"plan\"" : "";
            var desc = s.On("desc") ? " ins:Choice.Description=\"وصف قصير\"" : "";
            string Tile(string title, bool on, bool off) =>
                $"  <{tag} Classes=\"tile\"{group} Content=\"{title}\"{desc}{(on ? " IsChecked=\"True\"" : "")}{(off ? " IsEnabled=\"False\"" : "")} />\n";
            return "<ins:ChoiceGrid Classes=\"tile-grid\" AutomationProperties.Name=\"الخيارات\">\n" +
                Tile("الأول", false, false) + Tile("الثاني", true, false) + Tile("الثالث", false, s.On("disabled")) +
                "</ins:ChoiceGrid>";
        },
    };

    public static PlayDef SegBar { get; } = new()
    {
        Id = "seg",
        Title = "الشريط المقسم",
        Controls =
        [
            Switch("icons", "بأيقونات"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Segmented", "لبديل واحد من بدائل قليلة قصيرة، ومعه `AutomationProperties.Name`.", OptionKind.Element, true),
            Row("الأساس", "ins:SegmentedItem", "جزء من الشريط، ونصه أو محتواه في `Content`.", OptionKind.Element),
            Row("الأساس", "SelectedIndex", "الجزء المختار، وهو الأول إن لم تذكره.", OptionKind.Property),
            Row("الأساس", "Tag", "قيمة الجزء، تقرؤها من `SelectedItem` في الحدث.", OptionKind.Property),
            Row("الأساس", "SelectionChanged", "يُطلق عند كل اختيار.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            (string Tag, string Text, string Icon)[] parts = [("day", "اليوم", "IconCalendar"), ("week", "الأسبوع", "IconList"), ("month", "الشهر", "IconGrid")];
            return "<ins:Segmented AutomationProperties.Name=\"الفترة\">\n" +
                string.Concat(parts.Select(p => s.On("icons")
                    ? $"  <ins:SegmentedItem Tag=\"{p.Tag}\">\n" +
                      "    <StackPanel Orientation=\"Horizontal\" Spacing=\"6\">\n" +
                      $"      <ins:Icon Data=\"{{StaticResource {p.Icon}}}\" VerticalAlignment=\"Center\" />\n" +
                      $"      <TextBlock Text=\"{p.Text}\" VerticalAlignment=\"Center\" />\n" +
                      "    </StackPanel>\n" +
                      "  </ins:SegmentedItem>\n"
                    : $"  <ins:SegmentedItem Content=\"{p.Text}\" Tag=\"{p.Tag}\" />\n")) +
                "</ins:Segmented>";
        },
        Notes = _ => ["اختر جزءًا، فيظهر هنا الحدث الذي يطلقه."],
        Stage = (view, _, say) =>
        {
            foreach (var seg in view.GetLogicalDescendants().OfType<Segmented>())
                seg.SelectionChanged += (_, _) => { if (seg.SelectedItem is Control c) say($"حدث `SelectionChanged`، و`Tag` الجزء المختار `{c.Tag}`"); };
        },
    };

    public static PlayDef[] All { get; } = [Check, Toggle, Tile, SegBar];
}
