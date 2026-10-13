using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ChipsPlay
{
    public const string Example =
        """
        <WrapPanel Classes="chips">
          <ins:Chip Content="الرياض" IsRemovable="True" />
          <ins:Chip Content="عمّان" IsRemovable="True" />
          <ins:Chip Content="القاهرة" IsRemovable="True" />
        </WrapPanel>
        """;

    public static PlayDef Chip { get; } = new()
    {
        Id = "chip",
        Title = "الرقاقة",
        Controls =
        [
            Seg("kind", "النوع", "remove",
                Choice("remove", "تُزال", "IsRemovable", "على `ins:Chip`: زر الإزالة داخل الرقاقة، يرسم علامته بنفسه، واسمه لقارئ الشاشة «إزالة» ثم نص الرقاقة.", OptionKind.Property),
                Choice("filter", "فلتر", "ToggleButton.chip", "فلتر يُشغَّل ويُطفأ، فإذا شُغّل أخذ لون الهوية وظهرت عليه علامة صح.", OptionKind.Element),
                Choice("link", "رابط", "Button.chip", "رقاقة تنقلك إلى شاشة مصفّاة، والمختارة منها تحمل الكلاس `active`.", OptionKind.Element)),
            Switch("on", "الأولى مختارة", "IsChecked=\"True\"", "الفلتر مشغّل. ضعها بنفسك ليبدأ الفلتر مشغّلًا، وللرابط `active` بدلها.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Chip", "قيمة صغيرة يتصرف فيها المستخدم، ونصها في `Content`. أما الشارة فللقراءة وحدها.", OptionKind.Element, true),
            Row("الأساس", "chips", "على `WrapPanel`: يجمع الرقاقات في سطر يلتف إذا ضاق.", OptionKind.Class, true),
            Row("النوع", "Removing", "يُطلق قبل أن تزول الرقاقة، فإن جعلت `Cancel` فيه `true` بقيت. ثم يُطلق `Removed` بعد زوالها.", OptionKind.Event),
            Row("النوع", "IsCheckedChanged", "يُطلق عند كل ضغطة على الفلتر، وحالته في `IsChecked`.", OptionKind.Event),
        ],
        Disable = s => s["kind"] == "remove" ? ["on"] : [],
        Axaml = s =>
        {
            string[] items = ["الأول", "الثاني", "الثالث"];
            var chips = items.Select((t, i) =>
            {
                var on = s.On("on") && i == 0;
                return s["kind"] switch
                {
                    "filter" => $"  <ToggleButton Classes=\"chip\" Content=\"{t}\"{(on ? " IsChecked=\"True\"" : "")} />",
                    "link" => $"  <Button{Classes("chip", on ? "active" : null)} Content=\"{t}\" />",
                    _ => $"  <ins:Chip Content=\"{t}\" IsRemovable=\"True\" />",
                };
            });
            return "<WrapPanel Classes=\"chips\">\n" + string.Join("\n", chips) + "\n</WrapPanel>";
        },
        Notes = s => s["kind"] switch
        {
            "remove" => ["أزل رقاقة، فيظهر هنا الحدث، وغيّر أي خيار لتعيدها."],
            "filter" => ["اضغط رقاقة، فيظهر هنا الحدث الذي تطلقه."],
            _ => ["الرابط يدل على المختار بالكلاس `active`، وتضعه شاشتك بنفسها."],
        },
        Stage = (view, _, say) =>
        {
            view.AddHandler(Controls.Chip.RemovingEvent, (_, e) => say($"حدث `Removing` على «{(e.Source as Controls.Chip)?.Content}»"));
            foreach (var t in view.GetLogicalDescendants().OfType<ToggleButton>())
                t.IsCheckedChanged += (_, _) => say($"حدث `IsCheckedChanged` على «{t.Content}»، و`IsChecked` الآن `{t.IsChecked}`");
        },
    };

    public static PlayDef Tags { get; } = new()
    {
        Id = "tags",
        Title = "الاختيار المتعدد والوسوم",
        Wide = _ => true,
        WideMax = 520,
        Controls =
        [
            Switch("preset", "قيمة مختارة", "Values", "القيم المختارة، وكل قيمة رقاقة. تضعها في الشيفرة أو تقرؤها منها.", OptionKind.Property, on: true),
            Switch("list", "من قائمة", "ins:TagsOption", "خيارات تجعل الحقل اختيارًا متعددًا يطابق كالإكمال التلقائي، وكلما اخترت خيارًا صار رقاقة وخرج من القائمة. وبلا خيارات يصير ما تكتبه وسمًا إذا ضغطت `Enter` أو كتبت فاصلة.", OptionKind.Element, on: true),
            Switch("free", "كتابة حرة مع القائمة", "IsFree", "يقبل ما تكتبه وسمًا ولو لم يكن في القائمة.", OptionKind.Property),
            Seg("max", "حد أعلى", "",
                Choice("", "بلا"),
                Choice("3", "3", "Max", "أكثر عدد من القيم يقبله الحقل.", OptionKind.Property),
                Choice("5", "5")),
        ],
        Extra =
        [
            Row("الأساس", "ins:TagsInput", "يجمع قيمًا عدة في حقل واحد: رقاقات، وبعدها مربع الكتابة. وإذا ضغطت `Backspace` والمربع فارغ محوت آخر رقاقة.", OptionKind.Element, true),
            Row("الأساس", "TagsChanged", "يُطلق مع كل إضافة أو إزالة، ومعه القيم كلها `Values` وما أُضيف `Added` أو أُزيل `Removed`.", OptionKind.Event),
            Row("من قائمة", "EmptyText", "السطر الذي تعرضه القائمة إذا لم يطابق شيء ما كُتب، وهو «لا نتائج» إن لم تذكره.", OptionKind.Property),
        ],
        Disable = s => s.On("list") ? [] : ["free"],
        Axaml = s =>
        {
            var list = s.On("list");
            (string Value, string Label)[] pool = list
                ? [("a", "الأول"), ("b", "الثاني"), ("c", "الثالث"), ("d", "الرابع"), ("e", "الخامس")]
                : [("وسم", ""), ("وسم آخر", ""), ("وسم ثالث", ""), ("وسم رابع", ""), ("وسم خامس", "")];
            var values = new List<string>();
            if (s.On("preset")) values.Add(pool[0].Value);
            if (s.On("free") && list) values.Add("قيمة حرة");
            if (s.Has("max"))
                foreach (var p in pool)
                    if (values.Count < int.Parse(s["max"]) && !values.Contains(p.Value)) values.Add(p.Value);
            var attrs = (s.On("free") && list ? " IsFree=\"True\"" : "") + (s.Has("max") ? $" Max=\"{s["max"]}\"" : "") + $" PlaceholderText=\"{(list ? "اختر" : "اكتب واضغط Enter")}\"";
            var body = (values.Count == 0 ? "" : "    <ins:TagsInput.Values>\n" + string.Concat(values.Select(v => $"      <x:String>{v}</x:String>\n")) + "    </ins:TagsInput.Values>\n") +
                (list ? string.Concat(pool.Select(p => $"    <ins:TagsOption Value=\"{p.Value}\" Label=\"{p.Label}\" />\n")) : "");
            return "<StackPanel Classes=\"field\">\n" +
                $"  <TextBlock Classes=\"label\" Text=\"{(list ? "العناصر" : "الوسوم")}\" />\n" +
                (body.Length == 0 ? $"  <ins:TagsInput{attrs} />\n" : $"  <ins:TagsInput{attrs}>\n{body}  </ins:TagsInput>\n") +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string> { s.On("list") ? "اكتب في الحقل أو انقره، فتظهر القائمة، وما تختاره يصير رقاقة." : "اكتب كلمة واضغط `Enter`، فتصير وسمًا." };
            if (s.On("free") && s.On("list")) notes.Add("«قيمة حرة» ليست في القائمة، وقبلها الحقل لأنه يقبل الكتابة الحرة.");
            if (s.Has("max")) notes.Add("بلغ الحقل حده، فلا يقبل قيمة أخرى حتى تزيل واحدة.");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var t in view.GetLogicalDescendants().OfType<TagsInput>())
                t.TagsChanged += (_, e) => say($"حدث `TagsChanged`، والقيم الآن `{string.Join("، ", e.Values)}`");
        },
    };

    public static PlayDef[] All { get; } = [Chip, Tags];
}
