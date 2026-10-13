using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class LoadingPlay
{
    public const string Example =
        """
        <ins:Skeleton Classes="circle" />
        <ins:Skeleton Classes="title" />
        <ins:Skeleton Classes="block" />
        <ins:Skeleton Classes="text" />
        <ins:Skeleton Classes="text" />
        """;

    public static PlayDef Skeleton { get; } = new()
    {
        Id = "skeleton",
        Title = "هيكل التحميل",
        Wide = _ => true,
        Controls =
        [
            Select("shape", "الشكل", "text",
                Choice("text", "سطور", "text", "سطر من النص، وآخر السطور في مجموعتها أقصرها، كما في الفقرة."),
                Choice("title", "عنوان", "title", "عنوان، أغلظ من السطر وأقصر منه."),
                Choice("circle", "دائرة", "circle", "صورة رمزية أو أيقونة، وقطرها `Width`، فاجعله بقدر ما سيحل محلها."),
                Choice("block", "كتلة", "block", "صورة أو رسم بياني أو بطاقة.")),
            Seg("lines", "عدد السطور", "3",
                Choice("1", "1"),
                Choice("2", "2"),
                Choice("3", "3")),
        ],
        Extra =
        [
            Row("الأساس", "ins:Skeleton", "يرسم هيئة المحتوى قبل أن يصل، ومعه كلاس للشكل. فإذا وصل المحتوى أخذ مكانه، فلا تقفز الشاشة.", OptionKind.Element, true),
        ],
        Disable = s => s["shape"] == "text" ? [] : ["lines"],
        Axaml = s =>
        {
            var n = s["shape"] == "text" ? int.Parse(s["lines"]) : 1;
            var one = $"<ins:Skeleton Classes=\"{s["shape"]}\" />";
            return n == 1 ? one : "<StackPanel Spacing=\"12\">\n" + string.Concat(Enumerable.Repeat($"  {one}\n", n)) + "</StackPanel>";
        },
        Notes = s => s["shape"] == "text" && s["lines"] != "1" ? ["آخر السطور أقصرها، فتُقرأ السطور فقرة لم تصل بعد، لا جدولًا من الأشرطة."] : [],
    };

    public static PlayDef Spinner { get; } = new()
    {
        Id = "spinner",
        Title = "مؤشر التحميل",
        Controls =
        [
            Seg("place", "موضعه", "",
                Choice("", "وحده"),
                Choice("btn", "في زر"),
                Choice("box", "في حاوية", "loading", "على `Border`: حاوية يتوسطها المؤشر، وتحفظ للمنطقة التي تنتظر محتواها ارتفاعًا معقولًا.")),
            Seg("size", "المقاس", "",
                Choice("", "عادي"),
                Choice("sm", "صغير", "sm", "مؤشر أصغر، يسكن داخل زر أو بجوار نص.")),
        ],
        Extra =
        [
            Row("الأساس", "ins:LoadingSpinner", "لانتظار قصير لا تُعرف هيئة ما يأتي بعده، فإن عرفتها فهيكل التحميل أولى.", OptionKind.Element, true),
            Row("الأساس", "AutomationProperties.Name", "اسم المؤشر، فيعلم قارئ الشاشة أن شيئًا يُحمَّل. ودعه بلا اسم إذا كان نص الزر أو ما حوله يغني عنه.", OptionKind.Property),
        ],
        Disable = s => s["place"] == "btn" ? ["size"] : [],
        Wide = s => s["place"] == "box",
        Axaml = s =>
        {
            if (s["place"] == "btn")
                return "<Button Classes=\"primary\" IsEnabled=\"False\">\n" +
                    "  <StackPanel Orientation=\"Horizontal\" Spacing=\"8\">\n" +
                    "    <ins:LoadingSpinner Classes=\"sm\" />\n" +
                    "    <TextBlock Text=\"جارٍ الحفظ…\" VerticalAlignment=\"Center\" />\n" +
                    "  </StackPanel>\n" +
                    "</Button>";
            var spin = $"<ins:LoadingSpinner{Classes(s["size"])} AutomationProperties.Name=\"جارٍ التحميل\" />";
            return s["place"] == "box" ? $"<Border Classes=\"loading\">\n  {spin}\n</Border>" : spin;
        },
        Notes = s => s["place"] == "btn" ? ["في الزر يصغر المؤشر ويأخذ لون نص الزر، ولا اسم له لأن نص الزر يغني عنه."] : [],
    };

    public static PlayDef Empty { get; } = new()
    {
        Id = "empty",
        Title = "الحالة الفارغة",
        Wide = _ => true,
        Controls =
        [
            Seg("kind", "السبب", "new",
                Choice("new", "لا شيء بعد"),
                Choice("search", "لا نتائج")),
            Switch("ico", "أيقونة", "Icon", "أيقونة باهتة فوق العنوان، تدل على ما كان سيظهر هنا.", OptionKind.Property, "الأساس", on: true),
            Switch("action", "مع زر", on: true),
        ],
        Extra =
        [
            Row("الأساس", "ins:EmptyState", "تخبر القارئ بما سيظهر هنا، وبما يفعله بعد ذلك. وما في داخلها زر الإجراء، إن كان.", OptionKind.Element, true),
            Row("الأساس", "Title", "العنوان: ما الذي ليس هنا.", OptionKind.Property),
            Row("الأساس", "Hint", "التلميح تحت العنوان: ما الذي يفعله القارئ الآن.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var search = s["kind"] == "search";
            var (icon, title, hint, button) = search
                ? ("IconSearch", "لا نتائج", "جرب كلمة أخرى.", "<Button Classes=\"secondary\" Content=\"امسح البحث\" />")
                : ("IconFolder", "لا ملفات بعد", "أضف أول ملف ليظهر هنا.", "<Button Classes=\"primary\" Content=\"ملف جديد\" />");
            var attrs = (s.On("ico") ? $" Icon=\"{{StaticResource {icon}}}\"" : "") + $" Title=\"{title}\" Hint=\"{hint}\"";
            return s.On("action") ? $"<ins:EmptyState{attrs}>\n  {button}\n</ins:EmptyState>" : $"<ins:EmptyState{attrs} />";
        },
    };

    public static PlayDef[] All { get; } = [Skeleton, Spinner, Empty];
}
