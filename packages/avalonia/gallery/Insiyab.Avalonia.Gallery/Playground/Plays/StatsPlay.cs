using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class StatsPlay
{
    private static string Card(string classes, string icon, string label, string value) =>
        $"  <Border Classes=\"{classes}\">\n" +
        "    <DockPanel>\n" +
        $"      <Border Classes=\"stat-ico\"><ins:Icon Data=\"{{StaticResource {icon}}}\" /></Border>\n" +
        "      <StackPanel Classes=\"stat-body\">\n" +
        $"        <TextBlock Classes=\"stat-label\" Text=\"{label}\" />\n" +
        $"        <TextBlock Classes=\"stat-value\" Text=\"{value}\" />\n" +
        "      </StackPanel>\n" +
        "    </DockPanel>\n" +
        "  </Border>\n";

    public static string Example { get; } =
        "<UniformGrid Columns=\"3\" ColumnSpacing=\"12\" RowSpacing=\"12\">\n" +
        Card("stat glass", "IconCart", "الطلبات", "31") +
        Card("stat glass ok", "IconCheck", "مكتملة", "24") +
        Card("stat glass warn", "IconClock", "معلقة", "5") +
        Card("stat glass bad", "IconWarn", "فاشلة", "2") +
        Card("stat glass info", "IconUsers", "عملاء جدد", "9") +
        Card("stat glass zero", "IconInbox", "شكاوى", "0") +
        "</UniformGrid>";

    public static PlayDef Stat { get; } = new()
    {
        Id = "stat",
        Title = "البطاقة",
        Controls =
        [
            Select("tone", "النبرة", "",
                Choice("", "عادية"),
                Choice("ok", "نجاح", "ok", "لرقم محمود، كالمكتمل."),
                Choice("warn", "تحذير", "warn", "لرقم ينتظر أو يحتاج انتباهًا، كالمعلق."),
                Choice("bad", "خطر", "bad", "لرقم يدل على خلل، كالفاشل."),
                Choice("info", "معلومة", "info", "لرقم يُعرض للعلم وحسب."),
                Choice("zero", "صفر", "zero", "للقيمة الصفرية، فتطمئن القارئ بدل أن تنبهه."),
                Choice("mute", "خافتة", "mute", "للقيمة الثانوية، فتخفت بين أخواتها.")),
            Seg("size", "المقاس", "",
                Choice("sm", "صغير", "sm", "لشريط تتزاحم فيه البطاقات."),
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "لأهم رقم في الشاشة.")),
            Text("label", "التسمية", "الملفات"),
            Text("value", "القيمة", "128"),
            Switch("glass", "سطح قائم بذاته", "glass", "يجعل البطاقة سطحًا زجاجيًا قائمًا بذاته. ومن دونه ضعها داخل لوح.", on: true),
            Switch("hint", "تلميح", "stat-hint", "على `TextBlock` تحت القيمة يبين سياقها."),
            Switch("trend", "التغير", "stat-trend", "على `StackPanel` تحت القيمة يضم `ins:Delta` و`TextBlock.delta-ref`."),
            Switch("link", "رابط", "stat-link", "على `Button` يحيط بالبطاقة: بطاقة تنقل إلى مكان، فتتحرك أيقونتها تحت المؤشر.", OptionKind.Class),
        ],
        Extra =
        [
            Row("الأساس", "stat", "على `Border` حول `DockPanel`: أيقونة في رقاقة، ثم تسمية وقيمة.", OptionKind.Class, true),
            Row("الأساس", "stat-ico", "على `Border`: رقاقة الأيقونة، بلون النبرة.", OptionKind.Class, true),
            Row("الأساس", "stat-body", "على `StackPanel`: يضم التسمية والقيمة وما تحتهما.", OptionKind.Class, true),
            Row("الأساس", "stat-label", "على `TextBlock`: ما يعنيه الرقم.", OptionKind.Class, true),
            Row("الأساس", "stat-value", "على `TextBlock`: الرقم نفسه، بخط Inter وأرقام متساوية العرض، وبلون النبرة.", OptionKind.Class, true),
        ],
        Axaml = s =>
        {
            var cls = string.Join(' ', new[] { "stat", s.On("glass") ? "glass" : null, s["tone"], s["size"] }.Where(c => !string.IsNullOrEmpty(c)));
            var pad = s.On("link") ? "  " : "";
            var lines = new List<string>
            {
                $"<Border Classes=\"{cls}\">",
                "  <DockPanel>",
                "    <Border Classes=\"stat-ico\"><ins:Icon Data=\"{StaticResource IconFile}\" /></Border>",
                "    <StackPanel Classes=\"stat-body\">",
                $"      <TextBlock Classes=\"stat-label\" Text=\"{s["label"]}\" />",
                $"      <TextBlock Classes=\"stat-value\" Text=\"{s["value"]}\" />",
            };
            if (s.On("hint")) lines.Add("      <TextBlock Classes=\"stat-hint\" Text=\"هذا الشهر\" />");
            if (s.On("trend")) lines.Add("      <StackPanel Classes=\"stat-trend\"><ins:Delta Classes=\"up\" Content=\"12%\" /><TextBlock Classes=\"delta-ref\" Text=\"عن الشهر الماضي\" /></StackPanel>");
            lines.AddRange(["    </StackPanel>", "  </DockPanel>", "</Border>"]);
            var card = string.Join("\n", lines.Select(l => pad + l));
            return s.On("link") ? "<Button Classes=\"stat-link\">\n" + card + "\n</Button>" : card;
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["tone"] == "zero") notes.Add("اجعل القيمة صفرًا مع `zero`، فهو للقيمة الصفرية.");
            if (s.On("link")) notes.Add("مرر المؤشر على البطاقة، فتتحرك أيقونتها.");
            return notes;
        },
    };

    public static PlayDef Delta { get; } = new()
    {
        Id = "delta",
        Title = "التغير",
        Controls =
        [
            Seg("dir", "الاتجاه", "up",
                Choice("up", "صعود", "up", "سهم إلى أعلى باللون الأخضر."),
                Choice("down", "هبوط", "down", "سهم إلى أسفل باللون الأحمر."),
                Choice("flat", "ثبات", "flat", "خط أفقي بلون خافت، لما بقي على حاله.")),
            Switch("invert", "النقص خير", "invert", "يعكس اللونين حين يكون النقص خيرًا من الزيادة، كالتكاليف ووقت الانتظار."),
            Switch("ref", "ما يقارن به", "delta-ref", "على `TextBlock` بعد `ins:Delta`، يذكر ما يقارن به الرقم.", on: true),
            Text("text", "المقدار", "12%"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Delta", "مقدار ما تغير الرقم في `Content`: يدل سهمه على الاتجاه، ولونه على أن التغير محمود أو مذموم. ويبقى الرقم من اليسار فتبقى الإشارة أمامه.", OptionKind.Element, true),
        ],
        Disable = s => s["dir"] == "flat" ? ["invert"] : [],
        Axaml = s =>
        {
            var d = $"<ins:Delta{Classes(s["dir"], s.On("invert") && s["dir"] != "flat" ? "invert" : null)} Content=\"{s["text"]}\" />";
            return s.On("ref") ? $"<StackPanel Orientation=\"Horizontal\">\n  {d}\n  <TextBlock Classes=\"delta-ref\" Text=\"عن الشهر الماضي\" />\n</StackPanel>" : d;
        },
        Notes = s => s.On("invert") && s["dir"] != "flat" ? ["الاتجاه على حاله، وانعكس اللون فقط."] : [],
    };

    public static PlayDef[] All { get; } = [Stat, Delta];
}
