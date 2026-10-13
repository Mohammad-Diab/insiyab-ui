using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class BadgesPlay
{
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
                Choice("warn", "تحذير", "warn", "لحالة معلقة أو تنتظر شيئًا."),
                Choice("bad", "خطر", "bad", "لحالة ملغاة أو متعثرة."),
                Choice("info", "معلومة", "info", "لحالة جارية."),
                Choice("brand", "العلامة", "brand", "بلون العلامة، لما يستحق أن يتميز.")),
            Text("text", "النص", "مكتمل"),
            Switch("dot", "بنقطة", "dot", "نقطة بلون الشارة قبل النص، لحالة تكفيها علامة بلا أيقونة.", on: true),
            Switch("press", "قابلة للنقر", "Button.pill", "زر بالكلاس `pill` ومعه النبرة و`dot`، فترتفع الشارة قليلًا تحت المؤشر. أما `ins:Pill` التي تُقرأ فحسب فلا تتحرك.", OptionKind.Element),
        ],
        Extra =
        [
            Row("الأساس", "ins:Pill", "شارة للحالة، ومعها كلاس للنبرة. وهي محايدة إن لم تذكر نبرة.", OptionKind.Element, true),
        ],
        Axaml = s =>
        {
            var dot = s.On("dot") ? "dot" : null;
            if (!s.On("press")) return $"<ins:Pill{Classes(s["tone"], dot)} Content=\"{s["text"]}\" />";
            return "<StackPanel Orientation=\"Horizontal\" Spacing=\"8\">\n" +
                $"  <Button{Classes("pill", s["tone"], dot)} Content=\"{s["text"]}\" />\n" +
                $"  <Button{Classes("pill", dot)} Content=\"الأول\" />\n" +
                $"  <Button{Classes("pill", dot)} Content=\"الثاني\" />\n" +
                "</StackPanel>";
        },
        Notes = s => s.On("press") ? ["شارات تُنقر، كأزرار تصفية. مر عليها بالمؤشر فترتفع قليلًا."] : [],
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
                Choice("lg", "كبير", "lg", "صورة أكبر، لرأس صفحة أو بطاقة.")),
            Switch("group", "مجموعة صور", "ins:AvatarGroup", "يرص عدة صور يتداخل بعضها في بعض، وتحيط بكل صورة حلقة بلون السطح فيبقى بين كل صورتين فاصل. ومعه `AutomationProperties.Name` يقول كم هم.", OptionKind.Element, "مجموعة صور"),
            Text("text", "الحروف", "ل ح"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Avatar", "قرص بلون العلامة يحمل حرفًا أو حرفين، أو صورة تملؤه.", OptionKind.Element, true),
            Row("مجموعة صور", "more", "آخر المجموعة، يعد من بقي، مثل `+4`."),
        ],
        Disable = s => s.On("group") ? ["text"] : [],
        Axaml = s =>
        {
            if (!s.On("group")) return $"<ins:Avatar{Classes(s["size"])} Content=\"{s["text"]}\" />";
            return "<ins:AvatarGroup AutomationProperties.Name=\"المشاركون: 7\">\n" +
                string.Concat(new[] { "ل", "ع", "س" }.Select(t => $"  <ins:Avatar{Classes(s["size"])} Content=\"{t}\" />\n")) +
                $"  <ins:Avatar{Classes(s["size"], "more")} Content=\"+4\" />\n" +
                "</ins:AvatarGroup>";
        },
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

    public static PlayDef[] All { get; } = [Pill, Count, Avatar, Money, Num];
}
