using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class DialogPlay
{
    public const string Example =
        """
        <Button Classes="primary" Content="فتح الحوار">
          <ins:Dialog.Opens>
            <ins:Dialog Title="عنوان الحوار">
              <TextBlock Text="نص الحوار." TextWrapping="Wrap" />
              <ins:Dialog.Foot>
                <ins:DialogFoot>
                  <Button Classes="bare" Content="إلغاء" ins:Dialog.Result="cancel" />
                  <Button Classes="primary" Content="موافق" ins:Dialog.Result="ok" />
                </ins:DialogFoot>
              </ins:Dialog.Foot>
            </ins:Dialog>
          </ins:Dialog.Opens>
        </Button>
        """;

    public static PlayDef Dialog { get; } = new()
    {
        Id = "dialog",
        Title = "مربع الحوار",
        Controls =
        [
            Seg("size", "المقاس", "",
                Choice("sm", "صغير", "sm", "لسؤال قصير، فيقع زراه تحته مباشرة."),
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "لمحتوى عريض، كجدول أو نموذج من عمودين.")),
            Text("title", "العنوان", "عنوان الحوار"),
            Switch("foot", "الذيل", "Foot", "ذيل الحوار، وفيه `ins:DialogFoot` بأزراره مصفوفة إلى نهاية السطر.", OptionKind.Property, "الأجزاء", on: true),
            Switch("form", "فيه نموذج", "StackPanel", "لحوار يجمع بيانات: حقوله في متنه، فيتمرر المتن وحده بين رأس وذيل ثابتين. وتجد قيمة الزر الذي أغلقه فيما تعيده `ShowAsync`.", OptionKind.Element),
            Select("closedby", "ما يغلقه", "",
                Choice("", "Esc وأزراره"),
                Choice("Any", "والنقر خارجه أيضًا", "ClosedBy=\"Any\"", "يغلقه النقر خارجه مع `Esc`. ولا يُغلق الحوار بالنقر خارجه إن لم تكتبها، حتى لا تذهب نقرة شاردة بما كُتب فيه.", OptionKind.Property),
                Choice("None", "أزراره وحدها", "ClosedBy=\"None\"", "لا يغلقه `Esc` ولا النقر خارجه، بل أزراره وحدها، لحوار لا بد فيه من جواب.", OptionKind.Property)),
        ],
        Extra =
        [
            Row("الأساس", "ins:Dialog", "لوح فوق النافذة المعتمة برأس ومتن يتمرر وذيل، يحبس التركيز ما دام مفتوحًا، ويغلقه `Esc` ويعيد التركيز إلى الزر الذي فتحه. وعنوانه في `Title` ومتنه في محتواه.", OptionKind.Element, true),
            Row("الأساس", "ins:Dialog.Opens", "على الزر الذي يفتح الحوار. ومن الشيفرة `await dialog.ShowAsync(button)`، و`ShowAsWindowAsync` تفتحه في نافذة منفصلة على الحاسوب.", OptionKind.Property, true),
            Row("الأساس", "ins:Dialog.Result", "على زر في الحوار: يغلقه، وهذه القيمة نتيجته.", OptionKind.Property, true),
            Row("الأجزاء", "ShowClose", "علامة الإغلاق في الرأس، وهي ظاهرة إن لم تذكرها. واسمها لقارئ الشاشة في `CloseLabel`.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var attrs = $" Title=\"{s["title"]}\"" + Classes(s["size"]) + (s.Has("closedby") ? $" ClosedBy=\"{s["closedby"]}\"" : "");
            var body = s.On("form")
                ? new[]
                {
                    "<StackPanel Spacing=\"20\">",
                    "  <StackPanel Classes=\"field\">",
                    "    <TextBlock Classes=\"label\" Text=\"الاسم\" />",
                    "    <TextBox />",
                    "  </StackPanel>",
                    "  <StackPanel Classes=\"field\">",
                    "    <TextBlock Classes=\"label\" Text=\"النوع\" />",
                    "    <ComboBox SelectedIndex=\"0\" HorizontalAlignment=\"Stretch\">",
                    "      <ComboBoxItem Content=\"عام\" />",
                    "      <ComboBoxItem Content=\"خاص\" />",
                    "    </ComboBox>",
                    "  </StackPanel>",
                    "  <StackPanel Classes=\"field\">",
                    "    <TextBlock Classes=\"label\" Text=\"ملاحظة\" />",
                    "    <TextBox AcceptsReturn=\"True\" TextWrapping=\"Wrap\" MinHeight=\"88\" />",
                    "  </StackPanel>",
                    "  <CheckBox Content=\"أرسل لي نسخة\" />",
                    "</StackPanel>",
                }
                : ["<TextBlock Text=\"نص الحوار.\" TextWrapping=\"Wrap\" />"];
            var lines = new List<string> { "<Button Classes=\"primary\" Content=\"فتح الحوار\">", "  <ins:Dialog.Opens>", $"    <ins:Dialog{attrs}>" };
            lines.AddRange(body.Select(l => "      " + l));
            if (s.On("foot"))
                lines.AddRange([
                    "      <ins:Dialog.Foot>",
                    "        <ins:DialogFoot>",
                    "          <Button Classes=\"bare\" Content=\"إلغاء\" ins:Dialog.Result=\"cancel\" />",
                    "          <Button Classes=\"primary\" Content=\"موافق\" ins:Dialog.Result=\"ok\" />",
                    "        </ins:DialogFoot>",
                    "      </ins:Dialog.Foot>",
                ]);
            lines.AddRange(["    </ins:Dialog>", "  </ins:Dialog.Opens>", "</Button>"]);
            return string.Join("\n", lines);
        },
        Notes = s => s["closedby"] switch
        {
            "Any" => ["اضغط الزر لترى الحوار بخياراتك، ثم انقر خارجه فيُغلق."],
            "None" => ["اضغط الزر لترى الحوار بخياراتك، فلا يغلقه `Esc` ولا النقر خارجه."],
            _ => ["اضغط الزر لترى الحوار بخياراتك."],
        },
    };

    public static PlayDef Confirm { get; } = new()
    {
        Id = "confirm",
        Title = "التأكيد",
        Controls =
        [
            Seg("look", "نمط الزر", "secondary",
                Choice("secondary", "ثانوي"),
                Choice("ghost-danger", "خطر")),
            Text("text", "السؤال", "أرشفة الملف؟"),
            Text("ok", "زر التأكيد", "أرشفة"),
            Switch("tone", "تأكيد بلون الخطر", "ins:Dialog.ConfirmTone=\"danger\"", "يجعل زر التأكيد أحمر وإن لم يكن الزر نفسه من أزرار الخطر.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Dialog.Confirm", "على زر، وقيمتها السؤال، فلا تمضي النقرة ولا أمرها إلا إذا أكده المستخدم. وإذا كان الزر من أزرار الخطر جاء زر التأكيد أحمر وحده.", OptionKind.Property, true),
            Row("الأساس", "ins:Dialog.ConfirmOk", "نص زر التأكيد، وهو «تأكيد» إن لم تكتبه.", OptionKind.Property, true),
            Row("الأساس", "Dialog.ConfirmAsync", "من الشيفرة: `await Dialog.ConfirmAsync(owner, \"تأكيد\", \"أرشفة العنصر؟\", confirm: \"أرشفة\")` تعيد `true` أو `false`.", OptionKind.Property),
        ],
        Disable = s => s["look"] == "ghost-danger" ? ["tone"] : [],
        Axaml = s =>
        {
            var tone = s.On("tone") && s["look"] != "ghost-danger" ? " ins:Dialog.ConfirmTone=\"danger\"" : "";
            var ok = s["ok"].Length > 0 ? $" ins:Dialog.ConfirmOk=\"{s["ok"]}\"" : "";
            var text = s["ok"].Length > 0 ? s["ok"] : "متابعة";
            return $"<Button Classes=\"{s["look"]}\" Content=\"{text}\" ins:Dialog.Confirm=\"{s["text"]}\"{ok}{tone} />";
        },
        Notes = _ => ["اضغط الزر لترى السؤال بخياراتك."],
    };

    public static PlayDef[] All { get; } = [Dialog, Confirm];
}
