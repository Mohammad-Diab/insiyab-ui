using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class SplitPlay
{
    public const string Example =
        """
        <ins:Split Height="288" Padding="20">
          <ins:Split.First>
            <StackPanel Spacing="12">
              <TextBlock Classes="h4" Text="المستند" />
              <TextBlock TextWrapping="Wrap" Text="تُرد المنتجات خلال أربعة عشر يومًا من الاستلام، بشرط أن تكون في عبوتها الأصلية ومعها الفاتورة." />
              <TextBlock TextWrapping="Wrap" Text="ويستبدل المنتج التالف دون رسوم خلال ثلاثين يومًا." />
            </StackPanel>
          </ins:Split.First>
          <ins:Split.Second>
            <StackPanel Spacing="12">
              <TextBlock Classes="h4" Text="الملاحظات" />
              <TextBlock TextWrapping="Wrap" Text="المدة واضحة، لكن شرط العبوة يحتاج تفصيلًا." />
            </StackPanel>
          </ins:Split.Second>
        </ins:Split>
        """;

    public static PlayDef Split { get; } = new()
    {
        Id = "split",
        Title = "العرض المقسوم",
        Wide = _ => true,
        Controls =
        [
            Seg("start", "حصة اللوح الأول", "",
                Choice("30", "30%", "Share=\"30\"", "حصة اللوح الأول بالنسبة المئوية، والأصل 50. ومعها `DefaultShare` التي يعيده إليها النقر المزدوج و`Enter`.", OptionKind.Property),
                Choice("", "50%"),
                Choice("70", "70%")),
            Switch("limits", "حدود أضيق للسحب", "Min=\"60\"", "أصغر حصة للوح الأول، ولا تنزل عنها الحصة التي يبدأ بها. والأصل 20، ومعها `Max` لأكبر حصة، والأصل 80.", OptionKind.Property),
            Switch("v", "لوح فوق لوح", "Orientation=\"Vertical\"", "يضع اللوحين أحدهما فوق الآخر، فيصير المقبض أفقيًا.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Split", "يضع لوحين جنبًا إلى جنب يتقاسمان العرض، ولا بد له من ارتفاع تعطيه إياه، فيتمرر كل لوح وحده. والمقبض بينهما تحركه الأسهم أيضًا، ويذهب به `Home` و`End` إلى الطرفين.", OptionKind.Element, true),
            Row("الأساس", "First", "اللوح الأول، و`Second` اللوح الثاني.", OptionKind.Property, true),
            Row("الأساس", "StackWidth", "إذا ضاق العرض المقسوم عن هذا العرض صار اللوحان أحدهما فوق الآخر بلا مقبض، وهو 640 إن لم تذكره، و`0` يلغيه.", OptionKind.Property),
            Row("الأساس", "ShareCommitted", "يُطلق إذا انتهى السحب أو حركت الأسهم المقبض، ومعه الحصة الجديدة `Share`.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var attrs = (s.On("v") ? " Height=\"320\" Orientation=\"Vertical\"" : " Height=\"192\" StackWidth=\"0\"") +
                (s.Has("start") ? $" Share=\"{s["start"]}\" DefaultShare=\"{s["start"]}\"" : "") +
                (s.On("limits") ? " Min=\"60\" Max=\"75\"" : "");
            return $"<ins:Split{attrs}>\n" +
                "  <ins:Split.First>\n" +
                "    <Panel Classes=\"demo-box\"><Rectangle /><TextBlock Text=\"اللوح الأول\" /></Panel>\n" +
                "  </ins:Split.First>\n" +
                "  <ins:Split.Second>\n" +
                "    <Panel Classes=\"demo-box\"><Rectangle /><TextBlock Text=\"اللوح الثاني\" /></Panel>\n" +
                "  </ins:Split.Second>\n" +
                "</ins:Split>";
        },
        Notes = s =>
        {
            var notes = new List<string> { "اسحب الخط بين اللوحين، فيظهر هنا الحدث الذي يطلقه." };
            if (!s.On("v")) notes.Add("يبقي `StackWidth=\"0\"` اللوحين جنبًا إلى جنب في هذه المساحة الضيقة.");
            if (s.On("limits")) notes.Add("لا تخرج القسمة عن 60% و75%، حتى الحصة التي يبدأ بها اللوح الأول.");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var split in view.GetLogicalDescendants().OfType<Controls.Split>())
                split.ShareCommitted += (_, e) => say($"حدث `ShareCommitted`، و`Share` الآن `{e.Share:0}`");
        },
    };

    public static PlayDef[] All { get; } = [Split];
}
