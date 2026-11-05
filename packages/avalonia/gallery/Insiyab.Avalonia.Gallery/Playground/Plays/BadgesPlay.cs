using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class BadgesPlay
{
    private static readonly (string Word, string Letter)[] Cats = [("تصميم", "ت"), ("تسويق", "س"), ("مبيعات", "م"), ("دعم", "د"), ("مالية", "ل"), ("تطوير", "ط"), ("شحن", "ش"), ("إدارة", "إ")];

    public static string PaletteExample { get; } = "<Grid ColumnDefinitions=\"*,*\" ColumnSpacing=\"12\">\n" + Scope("Light", "") + Scope("Dark", " Grid.Column=\"1\"") + "</Grid>";

    private static string Scope(string variant, string column) =>
        $"  <ThemeVariantScope{column} RequestedThemeVariant=\"{variant}\">\n" +
        "    <Border Background=\"{DynamicResource InsWash1}\" CornerRadius=\"{DynamicResource InsRMd}\" Padding=\"16\">\n" +
        "      <StackPanel Spacing=\"12\">\n" +
        Line(i => $"<ins:Pill Classes=\"cat{i + 1} dot\" Content=\"{Cats[i].Word}\" />") +
        Line(i => $"<ins:Badge Classes=\"cat{i + 1}\" Content=\"{i + 1}\" />") +
        Line(i => $"<ins:Avatar Classes=\"cat{i + 1}\" Content=\"{Cats[i].Letter}\" />") +
        Line(i => $"<ins:Pill Classes=\"cat{i + 1} dot\" AutomationProperties.Name=\"{Cats[i].Word}\" />") +
        "      </StackPanel>\n    </Border>\n  </ThemeVariantScope>\n";

    private static string Line(Func<int, string> item) =>
        "        <WrapPanel ItemSpacing=\"8\" LineSpacing=\"8\">\n" + string.Concat(Enumerable.Range(0, 8).Select(i => "          " + item(i) + "\n")) + "        </WrapPanel>\n";

    public const string Example =
        """
        <ins:Pill Classes="ok dot" Content="مكتمل" />
        <ins:Pill Classes="warn dot" Content="معلق" />
        <ins:Pill Classes="bad dot" Content="ملغى" />
        <ins:Pill Classes="info dot" Content="جارٍ" />
        <ins:Pill Classes="brand dot" Content="مميز" />
        <ins:Pill Classes="dot" Content="مسودة" />
        """;

    public static PlayDef Pill { get; } = new()
    {
        Id = "pill",
        Title = "شارة الحالة",
        Controls =
        [
            Select("tone", "النبرة", "ok",
                Choice("", "محايدة"),
                Choice("ok", "نجاح", "ok", "لحالة تمت أو تسير كما ينبغي."),
                Choice("warn", "تحذير", "warn", "لحالة معلقة أو تنتظر شيئًا، ونقطتها برتقالية لتُقرأ وهي صغيرة."),
                Choice("bad", "خطر", "bad", "لحالة ملغاة أو متعثرة."),
                Choice("info", "معلومة", "info", "لحالة جارية."),
                Choice("brand", "العلامة", "brand", "بلون العلامة، لما يستحق أن يتميز."),
                Choice("mute", "هادئة", "mute", "كالمحايدة، لتبدّل النبرة من الكود دون أن تحذف كلاسًا.")),
            Text("text", "النص", "مكتمل"),
            Seg("dot", "النقطة", "dot", Choice("", "بلا"), Choice("dot", "قبل النص", "dot", "نقطة بلون الشارة قبل النص، لحالة تكفيها علامة بلا أيقونة."), Choice("bare", "وحدها", "AutomationProperties.Name", "`ins:Pill` بالكلاس `dot` ونبرته بلا محتوى: نقطة وحدها بقطر 8. أعطها هذا الاسم ليقرأه قارئ الشاشة.", OptionKind.Property), Choice("peek", "تنفتح", "peek", "مع `dot`: لا تأخذ إلا مكان النقطة، وتنفتح الشارة كاملة فوق ما حولها إذا مر عليها المؤشر أو بلغها التركيز، ونصها اسمها لقارئ الشاشة. ومعها `back` تنفتح نحو البداية.")),
            Seg("size", "المقاس", "", Choice("", "عادي"), Choice("sm", "صغير", "sm", "شارة لا تزيد على سطر النص، لتجلس داخل جملة أو صف كثيف.")),
            Switch("outline", "محددة", "outline", "بلا تعبئة: حد النبرة ولونها فقط، بالمقاس نفسه، لشارة لا تنافس شارات الحالة."),
            Switch("press", "قابلة للنقر", "Button.pill", "زر بالكلاس `pill` ومعه النبرة و`dot`، فترتفع الشارة قليلًا تحت المؤشر. أما `ins:Pill` التي تُقرأ فحسب فلا تتحرك.", OptionKind.Element),
        ],
        Disable = s => s["dot"] is "bare" or "peek" ? ["press"] : [],
        Extra =
        [
            Row("الأساس", "ins:Pill", "شارة للحالة، ومعها كلاس للنبرة. وهي محايدة إن لم تذكر نبرة.", OptionKind.Element, true),
        ],
        Axaml = s =>
        {
            var kind = s["dot"];
            var size = s.Has("size") ? s["size"] : null;
            var outline = s.On("outline") ? "outline" : null;
            if (kind == "bare") return $"<ins:Pill{Classes(s["tone"], "dot", size, outline)} AutomationProperties.Name=\"{s["text"]}\" />";
            if (kind == "peek")
                return "<StackPanel Orientation=\"Horizontal\" Spacing=\"8\">\n  <TextBlock Text=\"الحالة\" />\n" +
                    $"  <ins:Pill{Classes(s["tone"], "dot", "peek", size, outline)} Content=\"{s["text"]}\" />\n" +
                    "  <TextBlock Text=\"ما بعدها لا يتحرك\" />\n</StackPanel>";
            var dot = kind == "dot" ? "dot" : null;
            if (!s.On("press")) return $"<ins:Pill{Classes(s["tone"], dot, size, outline)} Content=\"{s["text"]}\" />";
            return "<StackPanel Orientation=\"Horizontal\" Spacing=\"8\">\n" +
                $"  <Button{Classes("pill", s["tone"], dot, size, outline)} Content=\"{s["text"]}\" />\n" +
                $"  <Button{Classes("pill", dot, size)} Content=\"الأول\" />\n" +
                $"  <Button{Classes("pill", dot, size)} Content=\"الثاني\" />\n" +
                "</StackPanel>";
        },
        Notes = s => s["dot"] == "peek" ? ["مرّر المؤشر على النقطة أو بلغها بـ Tab، فتنفتح الشارة فوق ما بعدها."] : s.On("press") ? ["شارات تُنقر، كأزرار تصفية. مر عليها بالمؤشر فترتفع قليلًا."] : [],
    };

    public static PlayDef Count { get; } = new()
    {
        Id = "count",
        Title = "شارة العدد",
        Controls =
        [
            Select("tone", "النبرة", "",
                Choice("", "العلامة"),
                Choice("ok", "نجاح", "ok", "لعدد ما تم."),
                Choice("warn", "تحذير", "warn", "لعدد ما ينتظر."),
                Choice("bad", "خطر", "bad", "لعدد ما تعثر، أو ما يستدعي الانتباه."),
                Choice("mute", "هادئة", "mute", "لعدد لا يطلب شيئًا، كالصفر.")),
            Text("text", "العدد", "12"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Badge", "تحمل عددًا صغيرًا، بلون العلامة إن لم تذكر نبرة. وإذا ظهرت أو تغير عددها قفزت قفزة خفيفة.", OptionKind.Element, true),
        ],
        Axaml = s => $"<ins:Badge{Classes(s["tone"])} Content=\"{s["text"]}\" />",
    };

    public static PlayDef Avatar { get; } = new()
    {
        Id = "avatar",
        Title = "الصورة الرمزية",
        Controls =
        [
            Seg("size", "المقاس", "",
                Choice("sm", "صغير", "sm", "صورة أصغر، لقائمة أو سطر."),
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "صورة أكبر، لرأس صفحة أو بطاقة."),
                Choice("xl", "أكبر", "xl", "بقطر 64، لرأس صفحة شخص."),
                Choice("xxl", "الأكبر", "xxl", "بقطر 100، لرأس صفحة الملف الشخصي أو حوار «حول».")),
            Select("cat", "اللون", "",
                Choice("", "العلامة"),
                Choice("cat1", "الأول"),
                Choice("cat2", "الثاني"),
                Choice("cat3", "الثالث"),
                Choice("cat4", "الرابع"),
                Choice("cat5", "الخامس"),
                Choice("cat6", "السادس"),
                Choice("cat7", "السابع"),
                Choice("cat8", "الثامن")),
            Switch("group", "مجموعة صور", "ins:AvatarGroup", "يرص عدة صور يتداخل بعضها في بعض، وتحيط بكل صورة حلقة بلون السطح فيبقى بين كل صورتين فاصل. ومعه `AutomationProperties.Name` يقول كم هم.", OptionKind.Element, "مجموعة صور"),
            Text("text", "الحروف", "ل ح"),
        ],
        Extra =
        [
            Row("الألوان", "cat1 … cat8", "ثمانية ألوان للفئات لا للحالات، لكل لون تعبئة هادئة وحبر يُقرأ عليها في الوضعين. وتصلح على `ins:Pill` و`ins:Badge` و`ins:Avatar`."),
            Row("الأساس", "ins:Avatar", "قرص يحمل حرفًا أو حرفين في وسطه تمامًا بحسب شكل الحرف لا سطره، أو صورة تملؤه.", OptionKind.Element, true),
            Row("مجموعة صور", "more", "آخر المجموعة، يعد من بقي، مثل `+4`."),
        ],
        Disable = s => s.On("group") ? ["text"] : [],
        Axaml = s =>
        {
            if (!s.On("group")) return $"<ins:Avatar{Classes(s["size"], s["cat"])} Content=\"{s["text"]}\" />";
            return "<ins:AvatarGroup AutomationProperties.Name=\"المشاركون: 7\">\n" +
                string.Concat(new[] { "ل", "ع", "س" }.Select(t => $"  <ins:Avatar{Classes(s["size"], s["cat"])} Content=\"{t}\" />\n")) +
                $"  <ins:Avatar{Classes(s["size"], s["cat"], "more")} Content=\"+4\" />\n" +
                "</ins:AvatarGroup>";
        },
    };

    public static PlayDef Date { get; } = new()
    {
        Id = "date",
        Title = "شارة التاريخ",
        Controls =
        [
            Select("tone", "النبرة", "brand",
                Choice("", "محايدة"),
                Choice("ok", "نجاح", "ok", "لموعد تم."),
                Choice("warn", "تحذير", "warn", "لموعد يقترب."),
                Choice("bad", "خطر", "bad", "لموعد فات."),
                Choice("info", "معلومة", "info", "لموعد للعلم."),
                Choice("brand", "العلامة", "brand", "للموعد القادم، بلون العلامة.")),
            Switch("filled", "ممتلئة", "filled", "تعبئة مصمتة بلون النبرة وحبر يُقرأ عليها، لموعد واحد يستحق أن يبرز."),
            Seg("size", "المقاس", "", Choice("sm", "صغير", "sm", "لصف كثيف."), Choice("", "عادي"), Choice("lg", "كبير", "lg", "لرأس بطاقة.")),
            Text("day", "اليوم", "14", "Day", "اليوم كما يُكتب، ويغلب ما في `Date`."),
            Text("month", "الشهر", "مارس", "Month", "الشهر كما يُكتب، ويغلب ما في `Date`."),
        ],
        Extra =
        [
            Row("الأساس", "ins:DateBadge", "يوم فوق شهره في مربع صغير، لصف أو بطاقة لها تاريخ.", OptionKind.Element, true),
            Row("الأساس", "Date", "التاريخ، فيُكتب منه اليوم والشهر بلغة الصفحة: بالعربية من اليمين إلى اليسار، وبالإنجليزية في الاتجاه الآخر، أو كما تقول `Locale`.", OptionKind.Property, true),
        ],
        Axaml = s => $"<ins:DateBadge{Classes(s["tone"], s.On("filled") ? "filled" : null, s["size"])} Day=\"{s["day"]}\" Month=\"{s["month"]}\" />",
    };

    public static PlayDef Money { get; } = new()
    {
        Id = "money",
        Title = "المبلغ",
        Controls =
        [
            Seg("sign", "الاتجاه", "",
                Choice("", "بلا"),
                Choice("pos", "زيادة", "pos", "بلون النجاح، لمبلغ زاد."),
                Choice("neg", "نقص", "neg", "بلون الخطر، لمبلغ نقص.")),
            Text("val", "القيمة", "1,249.00"),
            Text("cur", "العملة", "ر.س"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Money", "مبلغ بأرقام متساوية العرض، يصلح لكل عملة.", OptionKind.Element, true),
            Row("الأساس", "Value", "القيمة كما تُكتب، بإشارتها وفواصلها، معزولة الاتجاه، فتبقى إشارة السالب في مكانها في السطر العربي.", OptionKind.Property),
            Row("الأساس", "Currency", "العملة بعد القيمة، أصغر منها وأهدأ لونًا.", OptionKind.Property),
        ],
        Axaml = s => $"<ins:Money{Classes(s["sign"])} Value=\"{s["val"]}\" Currency=\"{s["cur"]}\" />",
        Notes = s => s["sign"] == "neg" && !s["val"].StartsWith('-') ? ["اكتب الإشارة في القيمة، مثل `-1,500.00`، فالكلاس يلوّن ولا يضيفها."] : [],
    };

    public static PlayDef Num { get; } = new()
    {
        Id = "num",
        Title = "الرقم",
        Controls =
        [
            Text("text", "الرقم", "#40218"),
        ],
        Extra =
        [
            Row("الأساس", "num", "على `TextBlock`، لرقم ليس مبلغًا، كرقم المرجع والهاتف والتاريخ: يعزل اتجاهه كما تُعزل قيمة المبلغ، فلا يضطرب في السطر العربي.", OptionKind.Class, true),
        ],
        Axaml = s =>
            "<StackPanel Orientation=\"Horizontal\" Spacing=\"4\">\n" +
            "  <TextBlock Text=\"رقم المرجع\" />\n" +
            $"  <TextBlock Classes=\"num\" Text=\"{s["text"]}\" />\n" +
            "</StackPanel>",
    };

    public static PlayDef[] All { get; } = [Pill, Count, Avatar, Date, Money, Num];
}
