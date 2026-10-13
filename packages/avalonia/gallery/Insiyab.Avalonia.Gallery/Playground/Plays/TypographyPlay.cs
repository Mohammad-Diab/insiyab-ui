using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class TypographyPlay
{
    public const string Example =
        """
        <TextBlock Classes="display" Text="انسياب" />
        <TextBlock Classes="h1" Text="عنوان أول" />
        <TextBlock Classes="h2" Text="عنوان ثان" />
        <TextBlock Classes="h3" Text="عنوان ثالث" />
        <TextBlock Classes="h4" Text="عنوان رابع" />
        """;

    public const string FacesExample =
        """
        <TextBlock Classes="h3" Text="تجوال · Tajawal" />
        <TextBlock Classes="h3" FlowDirection="LeftToRight" FontFamily="{DynamicResource InsFontBodyEn}" Text="Inter · 1,240,905" />
        <TextBlock Classes="h3" FlowDirection="LeftToRight" FontFamily="{DynamicResource InsFontHeadingEn}" Text="Cause" />
        """;

    public static PlayDef Heading { get; } = new()
    {
        Id = "heading",
        Title = "العنوان",
        Controls =
        [
            Select("size", "المقاس", "h2",
                Choice("display", "عنوان الواجهة", "display", "أكبر العناوين، لعنوان واحد يتصدر صفحة أو واجهة."),
                Choice("h1", "أول", "h1", "بمقاس العنوان الأول."),
                Choice("h2", "ثان", "h2", "بمقاس العنوان الثاني."),
                Choice("h3", "ثالث", "h3", "بمقاس العنوان الثالث."),
                Choice("h4", "رابع", "h4", "بمقاس العنوان الرابع.")),
            Seg("level", "المستوى", "2",
                Choice("1", "1"),
                Choice("2", "2"),
                Choice("3", "3"),
                Choice("", "بلا")),
            Text("text", "النص", "الإعدادات"),
            Switch("eyebrow", "عنوان صغير فوقه", "eyebrow", "عنوان صغير بلون الهوية يعلو العنوان، يسمي القسم الذي هو فيه."),
            Switch("lead", "تمهيد تحته", "lead", "فقرة التمهيد التي تلي العنوان، بخط أكبر من المتن ولون أهدأ."),
        ],
        Extra =
        [
            Row("المقاس", "AutomationProperties.HeadingLevel", "مستوى العنوان في بنية الصفحة، يقرؤه قارئ الشاشة. وهو مستقل عن المقاس.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var level = s.Has("level") ? $" AutomationProperties.HeadingLevel=\"{s["level"]}\"" : "";
            var heading = $"<TextBlock Classes=\"{s["size"]}\"{level} Text=\"{s["text"]}\" />";
            if (!s.On("eyebrow") && !s.On("lead")) return heading;
            var lines = new List<string>();
            if (s.On("eyebrow")) lines.Add("<TextBlock Classes=\"eyebrow\" Text=\"الحساب\" />");
            lines.Add(heading);
            if (s.On("lead")) lines.Add("<TextBlock Classes=\"lead\" Text=\"الاسم والبريد وكلمة المرور.\" />");
            return "<StackPanel Spacing=\"8\">\n" + string.Concat(lines.Select(l => $"  {l}\n")) + "</StackPanel>";
        },
        Notes = s =>
        {
            if (!s.Has("level")) return ["نص بهيئة عنوان، لا يعده قارئ الشاشة عنوانًا."];
            return s["size"][1..] != s["level"] ? [$"المستوى `{s["level"]}` لبنية الصفحة، والمقاس من الكلاس، ولا يلزم أن يتفقا."] : [];
        },
    };

    public static PlayDef Prose { get; } = new()
    {
        Id = "prose",
        Title = "النص الطويل",
        Wide = _ => true,
        Controls =
        [
            Switch("list", "قائمة", "ins:ProseList", "قائمة بعلامات بلون الهوية، يتداخل بعضها في بعض. ومعها `IsOrdered=\"True\"` تصير مرقمة.", OptionKind.Element),
            Switch("quote", "اقتباس", "Border.blockquote", "اقتباس بشريط على حافته في جهة البداية وخلفية هادئة، ولا يميل.", OptionKind.Element),
            Switch("code", "شيفرة", "Border.pre", "كتلة شيفرة تبدأ من اليسار في الاتجاهين.", OptionKind.Element),
            Switch("table", "جدول", "Grid.table", "جدول بسيط بخطوط وحسب، للجدول الذي يأتي في وسط المقال، خلاياه `Border.th` و`Border.td`. أما جدول البيانات فهو `DataGrid`.", OptionKind.Element),
            Switch("dl", "قائمة وصف", "TextBlock.dt, TextBlock.dd", "تسمية عريضة وقيمتها تحتها بلون أهدأ."),
        ],
        Extra =
        [
            Row("الأساس", "ins:Prose", "ينسق ما في داخله من نص طويل، كمقال أو صفحة مساعدة، ويباعد بين كتله، ويحد عرض السطر ليسهل على العين.", OptionKind.Element, true),
            Row("الأساس", "LineLength", "أقصى طول للسطر بعدد الحروف، وهو `70` إن لم تذكره.", OptionKind.Property),
            Row("في النص", "TextBlock.h1 … h4", "بمقاسات العناوين، وأقرب إلى ما بعدها منها إلى ما قبلها."),
            Row("في النص", "Run.a", "رابط تحته خط، بعيد عن نقاط الحروف العربية."),
            Row("في النص", "Run.strong", "نص عريض."),
            Row("في النص", "Run.em", "توكيد باللون لا بالميل، فلا يميل الحرف العربي."),
            Row("في النص", "Run.mark", "تظليل بلون الهوية الخفيف."),
            Row("في النص", "Run.code", "شيفرة قصيرة في السطر. اعزلها بين `U+2066` و`U+2069` فلا تنقلب في الجملة العربية."),
            Row("في النص", "Separator", "خط فاصل بين قسمين، وحوله مسافة أوسع.", OptionKind.Element),
        ],
        Axaml = s =>
        {
            var lines = new List<string>
            {
                "<ins:Prose>",
                "  <TextBlock Classes=\"h2\" Text=\"عنوان\" />",
                "  <TextBlock>",
                "    <Run Text=\"فقرة فيها \" /><Run Classes=\"strong\" Text=\"نص عريض\" /><Run Text=\" و\" /><Run Classes=\"em\" Text=\"توكيد\" /><Run Text=\" و\" /><Run Classes=\"mark\" Text=\"تظليل\" /><Run Text=\" و\" /><Run Classes=\"code\" Text=\"&#x2066;&#x202F;code&#x202F;&#x2069;\" /><Run Text=\" و\" /><Run Classes=\"a\" Text=\"رابط\" /><Run Text=\".\" />",
                "  </TextBlock>",
            };
            if (s.On("list"))
                lines.AddRange([
                    "  <ins:ProseList IsOrdered=\"True\">",
                    "    <TextBlock Text=\"البند الأول\" />",
                    "    <StackPanel>",
                    "      <TextBlock Text=\"البند الثاني\" />",
                    "      <ins:ProseList>",
                    "        <TextBlock Text=\"بند متداخل\" />",
                    "      </ins:ProseList>",
                    "    </StackPanel>",
                    "  </ins:ProseList>",
                ]);
            if (s.On("quote")) lines.AddRange(["  <Border Classes=\"blockquote\">", "    <TextBlock Text=\"اقتباس قصير.\" />", "  </Border>"]);
            if (s.On("code")) lines.AddRange(["  <Border Classes=\"pre\">", "    <TextBlock Text=\"GET /api/items\" />", "  </Border>"]);
            if (s.On("table"))
                lines.AddRange([
                    "  <Grid Classes=\"table\" ColumnDefinitions=\"*,*\" RowDefinitions=\"Auto,Auto,Auto\">",
                    "    <Border Classes=\"th\" Grid.Row=\"0\" Grid.Column=\"0\"><TextBlock Text=\"الاسم\" /></Border>",
                    "    <Border Classes=\"th\" Grid.Row=\"0\" Grid.Column=\"1\"><TextBlock Text=\"القيمة\" /></Border>",
                    "    <Border Classes=\"td\" Grid.Row=\"1\" Grid.Column=\"0\"><TextBlock Text=\"الأول\" /></Border>",
                    "    <Border Classes=\"td\" Grid.Row=\"1\" Grid.Column=\"1\"><TextBlock Text=\"12\" /></Border>",
                    "    <Border Classes=\"td\" Grid.Row=\"2\" Grid.Column=\"0\"><TextBlock Text=\"الثاني\" /></Border>",
                    "    <Border Classes=\"td\" Grid.Row=\"2\" Grid.Column=\"1\"><TextBlock Text=\"7\" /></Border>",
                    "  </Grid>",
                ]);
            if (s.On("dl")) lines.AddRange(["  <StackPanel>", "    <TextBlock Classes=\"dt\" Text=\"المدة\" />", "    <TextBlock Classes=\"dd\" Text=\"14 يومًا\" />", "  </StackPanel>"]);
            lines.Add("</ins:Prose>");
            return string.Join("\n", lines);
        },
    };

    public static PlayDef Divider { get; } = new()
    {
        Id = "divider",
        Title = "الفاصل",
        Controls =
        [
            Switch("v", "عمودي", "v", "على `Separator`: فاصل عمودي بين عناصر صف، بطول الصف نفسه.", group: "فاصل عمودي"),
            Text("text", "النص", "أو"),
            Switch("start", "النص في البداية", "start", "يضع النص في البداية، فيصير عنوان قسم يمتد بعده الخط."),
        ],
        Extra =
        [
            Row("الأساس", "ins:Divider", "النص في `Content` بين خطين.", OptionKind.Element, true),
            Row("الأساس", "Separator", "خط وحسب، إذا لم يكن بين القسمين كلمة.", OptionKind.Element, true),
        ],
        Disable = s => s.On("v") ? ["text", "start"] : s.Has("text") ? [] : ["start"],
        Wide = s => !s.On("v"),
        Axaml = s =>
        {
            if (s.On("v"))
                return "<StackPanel Orientation=\"Horizontal\" Spacing=\"10\">\n" +
                    "  <TextBlock Text=\"ملف\" />\n" +
                    "  <Separator Classes=\"v\" />\n" +
                    "  <TextBlock Text=\"تعديل\" />\n" +
                    "</StackPanel>";
            return s.Has("text") ? $"<ins:Divider{Classes(s.On("start") ? "start" : null)} Content=\"{s["text"]}\" />" : "<Separator />";
        },
    };

    public static PlayDef Kbd { get; } = new()
    {
        Id = "kbd",
        Title = "المفتاح",
        Controls =
        [
            Text("key", "المفتاح", "K"),
            Switch("ctrl", "مع Ctrl", on: true),
        ],
        Extra =
        [
            Row("الأساس", "Border.kbd", "حول `TextBlock` فيه اسم المفتاح: مفتاح بحافة سفلية أثقل، يُكتب اسمه من اليسار ولو كان في جملة عربية.", OptionKind.Class, true),
        ],
        Axaml = s =>
            "<StackPanel Orientation=\"Horizontal\" Spacing=\"6\">\n" +
            "  <TextBlock Text=\"اضغط\" VerticalAlignment=\"Center\" />\n" +
            (s.On("ctrl") ? "  <Border Classes=\"kbd\"><TextBlock Text=\"Ctrl\" /></Border>\n  <TextBlock Text=\"+\" VerticalAlignment=\"Center\" />\n" : "") +
            $"  <Border Classes=\"kbd\"><TextBlock Text=\"{s["key"]}\" /></Border>\n" +
            "  <TextBlock Text=\"للبحث.\" VerticalAlignment=\"Center\" />\n" +
            "</StackPanel>",
    };

    public static PlayDef Search { get; } = new()
    {
        Id = "find",
        Title = "البحث في الصفحة",
        Wide = _ => true,
        Controls =
        [
            Text("text", "النص", "تقرير"),
            Switch("open", "مفتوح من البداية", "IsOpen", "يظهر الشريط من البداية، وإن تركته فتحه `Ctrl+F`.", OptionKind.Property, on: true),
            Switch("target", "في منطقة واحدة", "Target", "يحصر البحث في عنصر واحد بدل كل المناطق المعلَّمة في النافذة: `Target=\"{Binding #Help}\"`.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:FindBar", "شريط بحث صغير يفتحه `Ctrl+F`: فيه الحقل وعدد النتائج وزرا السابقة والتالية وزر الإغلاق. يظلل النتائج كلها ويميز الحالية ويمرر الصفحة إليها، وينتقل `Enter` و`F3` إلى التالية، و`Shift+Enter` و`Shift+F3` إلى السابقة، ويغلقه `Esc`.", OptionKind.Element, true),
            Row("الأساس", "ins:Find.IsSearchable", "على حاوية: يبحث الشريط في كل `TextBlock` داخلها. ولا يفرق البحث بين حرف بحركاته وبغيرها، ولا بين الهمزات، ولا بين ة وه، ولا بين ى وي.", OptionKind.Property, true),
            Row("الأساس", "Text", "ما يُبحث عنه، وهو نص الحقل.", OptionKind.Property),
            Row("النتائج", "MatchCount", "عدد النتائج، ومعه `CurrentMatch` رقم الحالية منها، و`Step(1)` و`Step(-1)` تنتقلان بينها.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var bar = "<ins:FindBar" + (s.On("open") ? " IsOpen=\"True\"" : "") + (s.Has("text") ? $" Text=\"{s["text"]}\"" : "") + (s.On("target") ? " Target=\"{Binding #Help}\"" : "") + " />";
            return "<StackPanel Spacing=\"12\">\n" +
                $"  {bar}\n" +
                $"  <StackPanel x:Name=\"Help\"{(s.On("target") ? "" : " ins:Find.IsSearchable=\"True\"")} Spacing=\"8\">\n" +
                "    <TextBlock Classes=\"h4\" Text=\"تقارير الشهر\" />\n" +
                "    <TextBlock TextWrapping=\"Wrap\" Text=\"يصل التَّقرير الشهري إلى بريدك في أول يوم من كل شهر، وفيه ملخص المبيعات والطلبات.\" />\n" +
                "    <TextBlock TextWrapping=\"Wrap\" Text=\"وإن أردت تقريرًا لفترة أخرى فاختر بدايتها ونهايتها من صفحة التقارير، ثم اضغط «إنشاء».\" />\n" +
                "    <TextBlock TextWrapping=\"Wrap\" Text=\"ولا يُحذف أي تقرير قبل مرور سنة على إنشائه.\" />\n" +
                "  </StackPanel>\n" +
                "</StackPanel>";
        },
        Notes = s =>
        [
            s.On("open")
                ? "اكتب في الحقل، ثم تنقل بين النتائج بـ`Enter` و`Shift+Enter`، وأغلقه بـ`Esc`. ولاحظ أن «تقرير» وجدت «التَّقرير» بحركاته و«تقريرًا» بتنوينها."
                : "اضغط `Ctrl+F` في أي مكان من الصفحة فيظهر الشريط وفي حقله المؤشر.",
        ],
    };

    public static PlayDef[] All { get; } = [Heading, Prose, Divider, Kbd, Search];
}
