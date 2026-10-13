using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class QuotePlay
{
    public const string Example =
        """
        <Border Classes="quote">
          <StackPanel>
            <TextBlock Text="يُحفظ الملف كل خمس دقائق، ويمكنك استعادة أي نسخة سابقة منه." />
            <ContentControl Classes="quote-source">
              <StackPanel Orientation="Horizontal">
                <HyperlinkButton Content="دليل الاستخدام" />
                <TextBlock Text=" · ص 12" />
              </StackPanel>
            </ContentControl>
          </StackPanel>
        </Border>
        """;

    public static PlayDef Quote { get; } = new()
    {
        Id = "quote",
        Title = "الاقتباس",
        Controls =
        [
            Seg("source", "المصدر", "link",
                Choice("", "بلا"),
                Choice("text", "نص", "quote-source", "على `ContentControl`: مصدر الاقتباس بخط أصغر، تسبقه أيقونة مستند."),
                Choice("link", "رابط", "HyperlinkButton", "رابط في المصدر يأخذ لون الهوية، ليصل القارئ إلى النص الأصلي من `NavigateUri`.", OptionKind.Element)),
            Text("text", "النص", "يُحفظ الملف كل خمس دقائق، ويمكنك استعادة أي نسخة سابقة منه."),
        ],
        Extra =
        [
            Row("الأساس", "quote", "على `Border` يضم النص المنقول ومصدره: نص منقول من مكان آخر، على حافته شريط بلون الهوية.", OptionKind.Class, true),
        ],
        Axaml = s =>
        {
            var text = $"<TextBlock Text=\"{s["text"]}\" />";
            if (!s.Has("source")) return $"<Border Classes=\"quote\">\n  {text}\n</Border>";
            var source = s["source"] == "text"
                ? "    <ContentControl Classes=\"quote-source\" Content=\"دليل الاستخدام · ص 12\" />\n"
                : "    <ContentControl Classes=\"quote-source\">\n" +
                  "      <StackPanel Orientation=\"Horizontal\">\n" +
                  "        <HyperlinkButton Content=\"دليل الاستخدام\" NavigateUri=\"https://example.com/guide\" />\n" +
                  "        <TextBlock Text=\" · ص 12\" />\n" +
                  "      </StackPanel>\n" +
                  "    </ContentControl>\n";
            return "<Border Classes=\"quote\">\n  <StackPanel>\n" + $"    {text}\n" + source + "  </StackPanel>\n</Border>";
        },
    };

    public static PlayDef Ref { get; } = new()
    {
        Id = "ref",
        Title = "علامة المرجع",
        Controls =
        [
            Text("num", "الرقم", "1"),
            Switch("note", "تضيء مع ملاحظتها", "Note", "تربط العلامة بملاحظة `ins:Note` باسمها، فإذا مررت المؤشر على إحداهما أضاءتا معًا، وإذا نقرت العلامة ظهرت الملاحظة.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Ref", "علامة صغيرة مرقمة في متن النص، تحيل إلى مصدر أو ملاحظة. وهي زر، فتأخذ `Click` أو `Command`، وتوضع في السطر داخل `InlineUIContainer` بالمحاذاة `Superscript`.", OptionKind.Element, true),
        ],
        Axaml = s =>
            "<StackPanel Spacing=\"16\">\n" +
            "  <TextBlock TextWrapping=\"Wrap\">\n" +
            $"    <Run Text=\"نص يحيل إلى مرجعه\" /><InlineUIContainer BaselineAlignment=\"Superscript\"><ins:Ref Content=\"{s["num"]}\"{(s.On("note") ? " Note=\"RefNote\"" : "")} /></InlineUIContainer><Run Text=\".\" />\n" +
            "  </TextBlock>\n" +
            "  <ins:Note Name=\"RefNote\" Classes=\"info\" Content=\"المرجع الذي تحيل إليه العلامة.\" />\n" +
            "</StackPanel>",
        Notes = s => s.On("note") ? ["مرر المؤشر على العلامة أو على الملاحظة، فتضيئان معًا."] : [],
    };

    public static PlayDef Mark { get; } = new()
    {
        Id = "mark",
        Title = "التظليل",
        Controls =
        [
            Select("tone", "النبرة", "",
                Choice("", "تحذير"),
                Choice("ok", "نجاح", "ok", "لجزء صحيح أو مقبول."),
                Choice("bad", "خطأ", "bad", "لجزء خاطئ أو مرفوض."),
                Choice("info", "معلومة", "info", "لجزء يحتاج شرحًا."),
                Choice("brand", "الهوية", "brand", "بلون الهوية، لتظليل لا يحكم على النص.")),
            Switch("note", "مع ملاحظة", "Note", "تربط التظليل بملاحظة `ins:Note` باسمها. فإذا مررت المؤشر على أحدهما أضاءا معًا، وإذا نقرت التظليل ظهرت ملاحظته. وقد تشترك عدة تظليلات في ملاحظة واحدة.", OptionKind.Property, "الملاحظة"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Mark", "داخل `TextBlock`: يظلل جزءًا من النص بنبرة التحذير ما لم تختر غيرها، ويمتد على الأسطر كأنه قطعة واحدة. ونصه في `Text`.", OptionKind.Element, true),
            Row("الملاحظة", ":lit", "حالة يأخذها التظليل وملاحظته ما داما مضاءين، فتصل إليها الأنماط.", OptionKind.Class),
        ],
        Axaml = s =>
        {
            var mark = $"<ins:Mark{Classes(s["tone"])} Text=\"جملة تحتاج مراجعة\"{(s.On("note") ? " Note=\"MarkNote\"" : "")} />";
            var p = $"<TextBlock TextWrapping=\"Wrap\">\n  <Run Text=\"في هذه الفقرة \" />{mark}<Run Text=\"، وبعدها بقية النص.\" />\n</TextBlock>";
            if (!s.On("note")) return p;
            return "<StackPanel Spacing=\"16\">\n" + string.Join("\n", p.Split('\n').Select(l => "  " + l)) + "\n" +
                $"  <ins:Note Name=\"MarkNote\"{Classes(s["tone"])} Content=\"سبب التظليل، يشرحه لمن يقرأ.\" />\n" +
                "</StackPanel>";
        },
        Notes = s => s.On("note") ? ["مرر المؤشر على التظليل أو على الملاحظة، فيضيئان معًا."] : [],
    };

    public static PlayDef Note { get; } = new()
    {
        Id = "note",
        Title = "الملاحظة",
        Controls =
        [
            Seg("tone", "النبرة", "",
                Choice("", "تحذير"),
                Choice("ok", "نجاح", "ok", "ملاحظة على جزء صحيح."),
                Choice("bad", "خطأ", "bad", "ملاحظة على جزء خاطئ."),
                Choice("info", "معلومة", "info", "ملاحظة تشرح."),
                Choice("brand", "الهوية", "brand", "ملاحظة بلون الهوية.")),
            Text("text", "النص", "ملاحظة قصيرة على النص."),
        ],
        Extra =
        [
            Row("الأساس", "ins:Note", "تعليق على النص بنبرة التحذير ما لم تختر غيرها، يشير إليه التظليل أو علامة المرجع باسمها في `Name`.", OptionKind.Element, true),
        ],
        Axaml = s => $"<ins:Note{Classes(s["tone"])} Content=\"{s["text"]}\" />",
        Notes = _ => ["اجعل نبرة الملاحظة كنبرة التظليل الذي يشير إليها."],
    };

    public static PlayDef[] All { get; } = [Quote, Ref, Mark, Note];
}
