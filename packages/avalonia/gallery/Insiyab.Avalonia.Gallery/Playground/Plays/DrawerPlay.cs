using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class DrawerPlay
{
    public const string Example =
        """
        <Button Classes="secondary" Content="البداية">
          <ins:Dialog.Opens>
            <ins:Dialog Classes="drawer" Title="درج البداية">
              <TextBlock Text="محتوى الدرج." TextWrapping="Wrap" />
              <ins:Dialog.Foot>
                <ins:DialogFoot>
                  <Button Classes="bare" Content="إلغاء" ins:Dialog.Result="cancel" />
                  <Button Classes="primary" Content="تطبيق" ins:Dialog.Result="ok" />
                </ins:DialogFoot>
              </ins:Dialog.Foot>
            </ins:Dialog>
          </ins:Dialog.Opens>
        </Button>
        <Button Classes="secondary" Content="النهاية">
          <ins:Dialog.Opens>
            <ins:Dialog Classes="drawer end" Title="درج النهاية">
              <TextBlock Text="محتوى الدرج." TextWrapping="Wrap" />
            </ins:Dialog>
          </ins:Dialog.Opens>
        </Button>
        <Button Classes="secondary" Content="الأسفل">
          <ins:Dialog.Opens>
            <ins:Dialog Classes="drawer bottom" Title="لوح سفلي">
              <TextBlock Text="محتوى اللوح." TextWrapping="Wrap" />
            </ins:Dialog>
          </ins:Dialog.Opens>
        </Button>
        """;

    public static PlayDef Drawer { get; } = new()
    {
        Id = "drawer",
        Title = "الدرج",
        Controls =
        [
            Seg("edge", "الحافة", "",
                Choice("", "البداية"),
                Choice("end", "النهاية", "end", "يخرج الدرج من حافة النهاية، وهي اليسار في العربية."),
                Choice("bottom", "الأسفل", "bottom", "لوح يصعد من أسفل النافذة ويمتد على عرضها، ويناسب الهاتف.")),
            Text("title", "العنوان", "عنوان الدرج"),
            Switch("foot", "الذيل", "ins:DialogFoot", "ذيل الدرج في `ins:Dialog.Foot`، يثبت في أسفله وفيه أزراره.", OptionKind.Element, "الأجزاء", on: true),
            Select("closedby", "ما يغلقه", "",
                Choice("", "Esc والنقر خارجه"),
                Choice("CloseRequest", "Esc وأزراره", "ClosedBy=\"CloseRequest\"", "لا يغلقه النقر خارجه، بل `Esc` وأزراره، لدرج فيه نموذج لا تريد أن تضيعه نقرة شاردة.", OptionKind.Property),
                Choice("None", "أزراره وحدها", "ClosedBy=\"None\"", "لا يغلقه `Esc` ولا النقر خارجه، بل أزراره وحدها.", OptionKind.Property)).In("الإغلاق"),
        ],
        Extra =
        [
            Row("الأساس", "drawer", "على `ins:Dialog`، فيخرج من حافة البداية، وهي اليمين في العربية. ويغلقه `Esc` والنقر خارجه.", OptionKind.Class, true),
            Row("الأساس", "ins:Dialog.Opens", "على الزر الذي يفتح الدرج، كما يفتح مربع الحوار.", OptionKind.Property),
            Row("الأساس", "ins:Dialog.Result", "على زر داخل الدرج يغلقه، وقيمته نتيجة الإغلاق.", OptionKind.Property),
            Row("الأجزاء", "Title", "عنوان الدرج في رأسه، ومعه زر الإغلاق.", OptionKind.Property),
            Row("الأجزاء", "Content", "متن الدرج: ما تضعه داخله يملأ ما بين الرأس والذيل، ويتمرر وحده.", OptionKind.Property),
            Row("الإغلاق", "Closed", "يُطلق حين يُغلق الدرج، ونتيجته في `Result`، وهي `null` إن أغلقه `Esc` أو النقر خارجه.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var foot = s.On("foot")
                ? "\n      <ins:Dialog.Foot>\n" +
                  "        <ins:DialogFoot>\n" +
                  "          <Button Classes=\"bare\" Content=\"إلغاء\" ins:Dialog.Result=\"cancel\" />\n" +
                  "          <Button Classes=\"primary\" Content=\"تطبيق\" ins:Dialog.Result=\"ok\" />\n" +
                  "        </ins:DialogFoot>\n" +
                  "      </ins:Dialog.Foot>"
                : "";
            var closed = s.Has("closedby") ? $" ClosedBy=\"{s["closedby"]}\"" : "";
            return "<Button Classes=\"secondary\" Content=\"فتح الدرج\">\n" +
                "  <ins:Dialog.Opens>\n" +
                $"    <ins:Dialog{Classes("drawer", s["edge"])} Title=\"{s["title"]}\"{closed}>\n" +
                "      <TextBlock Text=\"محتوى الدرج.\" TextWrapping=\"Wrap\" />" + foot + "\n" +
                "    </ins:Dialog>\n" +
                "  </ins:Dialog.Opens>\n" +
                "</Button>";
        },
        Notes = s => [s.Has("closedby")
            ? "اضغط الزر لترى الدرج بخياراتك، ثم انقر خارجه فلا يُغلق."
            : "اضغط الزر لترى الدرج بخياراتك، ثم انقر خارجه فيُغلق."],
        Stage = (view, _, say) =>
        {
            foreach (var button in view.GetLogicalDescendants().OfType<Button>())
                if (Dialog.GetOpens(button) is { } dialog)
                    dialog.Closed += (_, e) => say($"حدث `Closed`، و`Result` هو `{e.Result ?? "null"}`");
        },
    };

    public static PlayDef Host { get; } = new()
    {
        Id = "host",
        Title = "الدرج المثبّت",
        Wide = _ => true,
        Controls =
        [
            Seg("mode", "مكانه", "Pinned",
                Choice("Pinned", "بجوار المحتوى", "Mode=\"Pinned\"", "يأخذ الدرج عمودًا بجوار المحتوى ويدفعه، وتسحب حافته فيتغير عرضه. وإذا ضاقت النافذة عن `PinnedMinWidth` طفا فوق الصفحة.", OptionKind.Property),
                Choice("Temporary", "فوق النافذة", "Mode=\"Temporary\"", "يطفو الدرج فوق النافذة كأي درج، ويغلقه النقر خارجه.", OptionKind.Property)),
            Seg("side", "الحافة", "End",
                Choice("Start", "البداية", "Side=\"Start\"", "الدرج في حافة البداية، وهي اليمين في العربية.", OptionKind.Property),
                Choice("End", "النهاية", "Side=\"End\"", "الدرج في حافة النهاية، وهي اليسار في العربية.", OptionKind.Property)),
            Switch("open", "مفتوح", "IsPaneOpen=\"True\"", "الدرج مفتوح، ويقلبه الزر الذي عليه `ins:DrawerHost.Toggles`.", OptionKind.Property, on: true),
        ],
        Extra =
        [
            Row("الأساس", "ins:DrawerHost", "يضم المحتوى والدرج بجواره.", OptionKind.Element, true),
            Row("الأساس", "Pane", "الدرج نفسه: `ins:Dialog` يأخذ الكلاس `drawer` وحده.", OptionKind.Property, true),
            Row("الأساس", "ins:DrawerHost.Toggles", "على الزر الذي يفتح الدرج ويغلقه، وإليه يعود التركيز إذا أُغلق الدرج الطافي.", OptionKind.Property),
            Row("الأساس", "PaneLength", "عرض الدرج المثبّت، ويتغير إذا سُحبت حافته بين `MinPaneLength` و`MaxPaneLength`.", OptionKind.Property),
            Row("الأساس", "PinnedMinWidth", "عرض النافذة الذي يحتاجه الدرج المثبّت، وهو `900` إن لم تذكره. فإن ضاقت عنه طفا الدرج فوق الصفحة.", OptionKind.Property),
        ],
        Axaml = s =>
            "<Border Classes=\"card\" Padding=\"0\" Height=\"280\">\n" +
            $"  <ins:DrawerHost x:Name=\"Host\" Mode=\"{s["mode"]}\" Side=\"{s["side"]}\"{(s.On("open") ? " IsPaneOpen=\"True\"" : "")}>\n" +
            "    <ins:DrawerHost.Pane>\n" +
            "      <ins:Dialog Title=\"عنوان الدرج\">\n" +
            "        <TextBlock Text=\"محتوى الدرج.\" TextWrapping=\"Wrap\" />\n" +
            "      </ins:Dialog>\n" +
            "    </ins:DrawerHost.Pane>\n" +
            "    <StackPanel Margin=\"24\" Spacing=\"12\">\n" +
            "      <Button Classes=\"secondary\" Content=\"الدرج\" HorizontalAlignment=\"Left\" ins:DrawerHost.Toggles=\"{Binding #Host}\" />\n" +
            "      <TextBlock Text=\"محتوى الصفحة.\" />\n" +
            "    </StackPanel>\n" +
            "  </ins:DrawerHost>\n" +
            "</Border>",
        Notes = s => s["mode"] == "Temporary"
            ? [s.On("open") ? "الدرج الطافي مفتوح فوق النافذة، فانقر خارجه يُغلق، واضغط زر «الدرج» يُفتح من جديد." : "اضغط زر «الدرج» فيُفتح فوق النافذة."]
            : ["اسحب حافة الدرج ليتغير عرضه، واضغط زر «الدرج» يُغلق أو يُفتح. وإذا ضاقت النافذة طفا فوق الصفحة."],
    };

    public static PlayDef[] All { get; } = [Drawer, Host];
}
