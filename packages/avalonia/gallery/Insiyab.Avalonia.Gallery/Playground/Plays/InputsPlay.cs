using Avalonia.Controls;
using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class InputsPlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="الاسم" />
          <TextBox />
          <TextBlock Classes="hint" Text="تلميح قصير." />
        </StackPanel>
        """;

    public static PlayDef Field { get; } = new()
    {
        Id = "field",
        Title = "الحقل",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Seg("el", "العنصر", "input",
                Choice("input", "حقل"),
                Choice("select", "قائمة", "ComboBox", "قائمة تفتح بطاقة زجاجية كقوائم المكتبة.", OptionKind.Element),
                Choice("textarea", "نص طويل", "AcceptsReturn=\"True\"", "على `TextBox`: مربع للنص الطويل، يطول مع ما يُكتب فيه حتى `MaxHeight`.", OptionKind.Property)),
            Select("type", "النوع", "text",
                Choice("text", "نص"),
                Choice("email", "بريد"),
                Choice("tel", "هاتف"),
                Choice("number", "رقم"),
                Choice("password", "كلمة مرور", "PasswordChar=\"•\"", "يخفي ما يُكتب، ومعه الكلاس `revealPasswordButton` يظهر زر العين الذي يكشفه.", OptionKind.Property)),
            Seg("size", "المقاس", "",
                Choice("sm", "صغير", "sm", "حقل أصغر، للجداول والأشرطة. وللقائمة مثله."),
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "حقل أكبر، لشاشة دخول أو بحث هو كل الشاشة.")),
            Select("state", "الحالة", "",
                Choice("", "عادي"),
                Choice("disabled", "معطل", "IsEnabled=\"False\"", "يعطل الحقل، فلا يُكتب فيه ولا يصل إليه التنقل بلوحة المفاتيح.", OptionKind.Property),
                Choice("readonly", "للقراءة فقط", "IsReadOnly=\"True\"", "تُقرأ قيمة الحقل وتُنسخ ولا تُغيَّر.", OptionKind.Property),
                Choice("bad", "خطأ", "bad", "يدل على الخطأ، ومعه رسالة `ContentControl.error` تحت الحقل. ويأخذ الحقل الهيئة نفسها وحده إذا كان في `DataValidationErrors` خطأ، وتجد التحقق في صفحة التحقق."),
                Choice("ok", "مقبول", "ok", "يدل على قبول القيمة، ومعه رسالة `ContentControl.success`.")),
            Seg("count", "العداد", "",
                Choice("", "بلا"),
                Choice("chars", "أحرف", "ins:InputCount.Mode=\"Chars\"", "يعد تحت الحقل ما كُتب من أصل `MaxLength`، ويصفرّ إذا دنا من الحد ويحمرّ إذا تجاوزه.", OptionKind.Property),
                Choice("words", "كلمات", "ins:InputCount.Mode=\"Words\"", "يعد الكلمات من أصل `ins:InputCount.Max`، فإذا تجاوزها صار في الحقل خطأ.", OptionKind.Property)),
            Text("label", "التسمية", "الاسم"),
            Switch("hint", "تلميح", "hint", "على `TextBlock` تحت الحقل: سطر قصير يشرح ما يُكتب فيه."),
            Switch("req", "مطلوب", "Run.req", "علامة الحقل المطلوب بعد التسمية. أما التحقق من أنه مطلوب فبـ`ins:Validate.Required`."),
        ],
        Extra =
        [
            Row("الأساس", "field", "على `StackPanel`: يجمع التسمية والحقل والتلميح في عمود واحد.", OptionKind.Class, true),
            Row("الأساس", "TextBox", "الحقل نفسه.", OptionKind.Element, true),
            Row("الأساس", "label", "على `TextBlock` فوق الحقل: التسمية.", OptionKind.Class, true),
            Row("النوع", "FlowDirection=\"LeftToRight\"", "على حقل البريد أو الهاتف وحده، فيُكتب من اليسار ويبقى سائر النموذج عربيًا.", OptionKind.Property),
            Row("العداد", "ins:InputCount.Max", "حد الكلمات للعداد، ويحد الأحرف أيضًا إذا غاب `MaxLength`.", OptionKind.Property),
            Row("العداد", "ins:InputCount.Format", "صيغة العداد، مثل `{n} من {max}`.", OptionKind.Property),
        ],
        Disable = s =>
        {
            var off = new List<string>();
            if (s["el"] != "input") off.Add("type");
            if (s["el"] == "textarea") off.Add("size");
            if (s["el"] == "select") off.Add("count");
            return off;
        },
        Axaml = s =>
        {
            var el = s["el"];
            var type = el == "input" ? s["type"] : "text";
            var ltr = type is "email" or "tel";
            var classes = Classes(el == "textarea" ? null : s["size"], s["state"] is "bad" or "ok" ? s["state"] : null, type == "password" ? "revealPasswordButton" : null);
            var attrs = classes;
            if (s["state"] == "disabled") attrs += " IsEnabled=\"False\"";
            if (s["state"] == "readonly" && el != "select") attrs += " IsReadOnly=\"True\"";
            if (el != "select" && s["count"] == "chars") attrs += " MaxLength=\"60\" ins:InputCount.Mode=\"Chars\"";
            if (el != "select" && s["count"] == "words") attrs += " ins:InputCount.Mode=\"Words\" ins:InputCount.Max=\"50\"";
            var filled = s["state"] is "disabled" or "readonly" or "bad" or "ok";
            var value = type switch
            {
                "email" => filled ? "name@example.com" : "",
                "tel" => filled ? "+966 50 000 0000" : "",
                "number" => "12",
                "password" => "secret",
                _ => filled ? "نص قصير" : "",
            };
            string control;
            if (el == "select")
                control = $"  <ComboBox{attrs} SelectedIndex=\"0\">\n" +
                    "    <ComboBoxItem Content=\"الأول\" />\n" +
                    "    <ComboBoxItem Content=\"الثاني\" />\n" +
                    "    <ComboBoxItem Content=\"الثالث\" />\n" +
                    "  </ComboBox>\n";
            else
            {
                if (el == "textarea") attrs += " AcceptsReturn=\"True\" TextWrapping=\"Wrap\" MinHeight=\"96\"";
                if (ltr) attrs += " FlowDirection=\"LeftToRight\"";
                if (type == "password") attrs += " FlowDirection=\"LeftToRight\" PasswordChar=\"•\"";
                if (ltr && value.Length == 0) attrs += $" PlaceholderText=\"{(type == "email" ? "name@example.com" : "+966 50 000 0000")}\"";
                if (value.Length > 0) attrs += $" Text=\"{value}\"";
                control = $"  <TextBox{attrs} />\n";
            }
            var label = s.On("req")
                ? $"  <TextBlock Classes=\"label\"><Run Text=\"{s["label"]}\" /><Run Classes=\"req\" Text=\" *\" /></TextBlock>\n"
                : $"  <TextBlock Classes=\"label\" Text=\"{s["label"]}\" />\n";
            return "<StackPanel Classes=\"field\">\n" + label + control +
                (s.On("hint") ? "  <TextBlock Classes=\"hint\" Text=\"تلميح قصير.\" />\n" : "") +
                (s["state"] == "bad" ? "  <ContentControl Classes=\"error\" Content=\"رسالة الخطأ.\" />\n" : "") +
                (s["state"] == "ok" ? "  <ContentControl Classes=\"success\" Content=\"رسالة القبول.\" />\n" : "") +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["el"] == "input" && s["type"] is "email" or "tel") notes.Add("يُكتب البريد والهاتف من اليسار، فأخذ الحقل وحده `FlowDirection=\"LeftToRight\"`.");
            if (s["el"] == "input" && s["type"] == "number") notes.Add("للأرقام التي تزيد وتنقص حقل العداد `NumericUpDown`، وتجده في صفحة أدوات الاختيار.");
            if (s["el"] == "select" && s["state"] == "readonly") notes.Add("القائمة لا تعرف القراءة فقط، فإما أن تبقى كما هي أو تُعطَّل.");
            if (s.Has("count") && s["el"] != "select") notes.Add("اكتب في الحقل، فيتغير العداد تحته.");
            return notes;
        },
    };

    public static PlayDef Search { get; } = new()
    {
        Id = "search",
        Title = "حقل البحث",
        Wide = _ => true,
        WideMax = 512,
        Controls =
        [
            Seg("size", "المقاس", "",
                Choice("sm", "صغير"),
                Choice("", "عادي"),
                Choice("lg", "كبير")),
            Switch("bar", "مع زر", "Grid", "يضم حقل البحث وزره في سطر واحد: الحقل في عمود `*` والزر في عمود `Auto`.", OptionKind.Element),
        ],
        Extra =
        [
            Row("الأساس", "search", "على `TextBox`: يضع أيقونة البحث داخل الحقل في أوله، وتأخذ لون الهوية إذا تركّز فيه. ومعه `sm` أو `lg` للمقاس.", OptionKind.Class, true),
        ],
        Axaml = s =>
        {
            var box = $"<TextBox{Classes("search", s["size"])} PlaceholderText=\"بحث\" AutomationProperties.Name=\"بحث\" />";
            if (!s.On("bar")) return box;
            return "<Grid ColumnDefinitions=\"*,Auto\" ColumnSpacing=\"8\">\n" +
                $"  {box}\n" +
                $"  <Button Grid.Column=\"1\"{Classes("primary", s["size"])} Content=\"بحث\" />\n" +
                "</Grid>";
        },
    };

    public static PlayDef Form { get; } = new()
    {
        Id = "form",
        Title = "النموذج",
        Wide = _ => true,
        Controls =
        [
            Seg("layout", "ترتيب الحقول", "",
                Choice("", "عمود"),
                Choice("row", "سطر", "UniformGrid Rows=\"1\"", "يجعل الحقول متجاورة في سطر واحد، متساوية العرض.", OptionKind.Element),
                Choice("grid", "شبكة", "UniformGrid Columns=\"2\"", "لحقول قصيرة كثيرة لا يسعها سطر: شبكة تتوازى حقولها.", OptionKind.Element)),
            Switch("commit", "سطر الحفظ", "form-commit", "على `StackPanel`: يجمع أزرار الحفظ في آخر النموذج، ويبدأ بالزر الرئيسي.", on: true),
        ],
        Extra =
        [
            Row("الأساس", "field", "يجمع التسمية والحقل والتلميح في عمود واحد، وتباعد بين الحقول `StackPanel Spacing=\"16\"`.", OptionKind.Class, true),
        ],
        Axaml = s =>
        {
            var wrap = s["layout"] switch
            {
                "row" => "<UniformGrid Rows=\"1\" ColumnSpacing=\"12\">",
                "grid" => "<UniformGrid Columns=\"2\" ColumnSpacing=\"12\" RowSpacing=\"16\">",
                _ => "<StackPanel Spacing=\"16\">",
            };
            var close = s.Has("layout") ? "</UniformGrid>" : "</StackPanel>";
            var fields = string.Concat(new[] { "الاسم", "المدينة", "الهاتف" }.Select(l =>
                "    <StackPanel Classes=\"field\">\n" +
                $"      <TextBlock Classes=\"label\" Text=\"{l}\" />\n" +
                "      <TextBox />\n" +
                "    </StackPanel>\n"));
            return "<StackPanel>\n" + $"  {wrap}\n" + fields + $"  {close}\n" +
                (s.On("commit") ? "  <StackPanel Classes=\"form-commit\">\n    <Button Classes=\"primary\" Content=\"حفظ\" />\n  </StackPanel>\n" : "") +
                "</StackPanel>";
        },
    };

    public static PlayDef Masked { get; } = new()
    {
        Id = "masked",
        Title = "حقل بقناع",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Select("mask", "القناع", "phone",
                Choice("phone", "رقم الجوال"),
                Choice("card", "رقم البطاقة"),
                Choice("date", "تاريخ"),
                Choice("code", "رمز الحجز")),
            Seg("size", "المقاس", "",
                Choice("sm", "صغير"),
                Choice("", "عادي"),
                Choice("lg", "كبير")),
            Seg("state", "الحالة", "",
                Choice("", "عادي"),
                Choice("bad", "خطأ"),
                Choice("ok", "مقبول")),
            Seg("prompt", "علامة الفراغ", "",
                Choice("", "_"),
                Choice("dot", "·", "PromptChar", "العلامة التي تملأ أماكن القناع الفارغة، وتظهر باهتة حتى يُكتب مكانها.", OptionKind.Property)),
            Switch("filled", "فيه قيمة"),
            Switch("hide", "يخفي الفراغ عند الخروج", "HidePromptOnLeave=\"True\"", "يخفي أماكن القناع الفارغة إذا خرج التركيز من الحقل.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "MaskedTextBox", "حقل بقناع ثابت، كرقم الجوال أو البطاقة أو التاريخ: لا يقبل كل مكان فيه إلا ما يسمح به القناع، وتبقى الأماكن الفارغة باهتة حتى تمتلئ. وأرقامه متساوية العرض، فلا يتحرك ما بعدها إذا امتلأت.", OptionKind.Element, true),
            Row("الأساس", "Mask", "القناع: `0` رقم لا بد منه، و`9` رقم يجوز تركه، و`L` حرف لا بد منه، و`>` يكبّر ما بعده من الحروف، وما سوى ذلك يبقى كما هو.", OptionKind.Property, true),
            Row("الأساس", "FlowDirection=\"LeftToRight\"", "الأقنعة تُكتب من اليسار، فضعها على الحقل وحده ويبقى سائر النموذج عربيًا.", OptionKind.Property),
            Row("الأساس", "MaskCompleted", "صحيحة إذا امتلأت كل الأماكن التي لا بد منها في القناع.", OptionKind.Property),
            Row("الحالة", "bad", "يدل على الخطأ كما في الحقل، وكذلك `ok` للقبول. ولا يتلون فيهما إلا الإطار.", OptionKind.Class),
        ],
        Axaml = s =>
        {
            var (label, mask, value) = s["mask"] switch
            {
                "card" => ("رقم البطاقة", "0000 0000 0000 0000", "41111111"),
                "date" => ("تاريخ الميلاد", "00/00/0000", "1504"),
                "code" => ("رمز الحجز", ">LL-0000", "QA12"),
                _ => ("رقم الجوال", "000 000 0000", "0501234"),
            };
            var attrs = Classes(s["size"], s["state"]) + $" Mask=\"{mask}\" FlowDirection=\"LeftToRight\"" +
                (s["prompt"] == "dot" ? " PromptChar=\"·\"" : "") +
                (s.On("hide") ? " HidePromptOnLeave=\"True\"" : "") +
                (s.On("filled") ? $" Text=\"{value}\"" : "");
            return "<StackPanel Classes=\"field\">\n" +
                $"  <TextBlock Classes=\"label\" Text=\"{label}\" />\n" +
                $"  <MaskedTextBox{attrs} />\n" +
                (s["state"] == "bad" ? "  <ContentControl Classes=\"error\" Content=\"القيمة ناقصة.\" />\n" : "") +
                (s["state"] == "ok" ? "  <ContentControl Classes=\"success\" Content=\"القيمة كاملة.\" />\n" : "") +
                "</StackPanel>";
        },
        Notes = _ => ["اكتب في الحقل: يمتلئ القناع بالترتيب، ولا يقبل كل مكان إلا ما يسمح به. وإذا امتلأ ظهر هنا."],
        Stage = (view, _, say) =>
        {
            foreach (var box in view.GetLogicalDescendants().OfType<MaskedTextBox>())
                box.TextChanged += (_, _) => { if (box.MaskCompleted is true) say("امتلأ القناع، و`MaskCompleted` الآن `True`"); };
        },
    };

    public static PlayDef[] All { get; } = [Field, Search, Form, Masked];
}
