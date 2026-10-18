using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class MatrixPlay
{
    public const string Example =
        """
        <ins:ChoiceMatrix>
          <ins:ChoiceMatrix.Columns>
            <ins:MatrixColumn Header="غير راضٍ" Value="1" />
            <ins:MatrixColumn Header="محايد" Value="2" />
            <ins:MatrixColumn Header="راضٍ" Value="3" />
            <ins:MatrixColumn Header="راضٍ جدًا" Value="4" />
          </ins:ChoiceMatrix.Columns>
          <ins:MatrixRow Header="السعر" SelectedIndex="2" />
          <ins:MatrixRow Header="الجودة" />
          <ins:MatrixRow Header="التوصيل" />
        </ins:ChoiceMatrix>
        """;

    public static PlayDef Matrix { get; } = new()
    {
        Id = "matrix",
        Title = "مصفوفة الاختيار",
        Wide = _ => true,
        Controls =
        [
            Switch("cards", "بطاقات بوصف", "cards", "إذا حملت الخلايا كلامًا: كل خلية بطاقة تحيط بزر الاختيار ووصفه من `Description`، فالبطاقة كلها هي الاختيار.", group: "بطاقات بوصف"),
            Switch("points", "درجات", "Points", "على `ins:MatrixColumn`: درجة المستوى في أول البطاقة.", OptionKind.Property, "بطاقات بوصف"),
            Switch("sum", "المجموع", "Sum", "مجموع القيم المختارة، تربطه بنص يعرضه: `{Binding #Matrix.Sum}`.", OptionKind.Property),
            Switch("deselect", "الإلغاء بنقرة ثانية", "deselect", "إذا نقرت الخيار المختار مرة ثانية أُلغي اختياره، وكذلك مفتاح المسافة."),
            Switch("required", "الإجابة لازمة", "IsComplete", "صحيحة إذا أُجيب عن كل صف، فلا ترسل النموذج حتى تصير صحيحة. وقل ذلك للمستخدم بسطر فوق المصفوفة.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:ChoiceMatrix", "صفوف من الأسئلة تشترك في إجابات واحدة، لكل صف مجموعة أزرار اختيار تتنقل بينها بالأسهم، ولكل زر اسم لقارئ الشاشة من اسم صفه وعموده، مثل «الأول: جيد».", OptionKind.Element, true),
            Row("الأساس", "ins:MatrixColumn", "عمود إجابة في `Columns`: عنوانه `Header` وقيمته `Value`، وتُجمع القيم العددية في `Sum`.", OptionKind.Element, true),
            Row("الأساس", "ins:MatrixRow", "صف سؤال: عنوانه `Header`، والعمود المختار فيه `SelectedIndex`، و`-1` إذا لم يُختر شيء.", OptionKind.Element, true),
            Row("الأساس", "SelectionChanged", "يُطلق إذا تغير اختيار في أي صف.", OptionKind.Event),
        ],
        Disable = s => s.On("cards") ? [] : ["points"],
        Axaml = s =>
        {
            (string Value, string Header)[] cols = s.On("cards") ? [("1", "غير راضٍ"), ("3", "راضٍ"), ("5", "راضٍ جدًا")] : [("1", "غير راضٍ"), ("2", "محايد"), ("3", "راضٍ"), ("4", "راضٍ جدًا")];
            var cls = Classes(s.On("cards") ? "cards" : null, s.On("deselect") ? "deselect" : null);
            var matrix = $"<ins:ChoiceMatrix{(s.On("sum") ? " x:Name=\"Matrix\"" : "")}{cls}>\n" +
                "  <ins:ChoiceMatrix.Columns>\n" +
                string.Concat(cols.Select(c =>
                    $"    <ins:MatrixColumn Header=\"{c.Header}\" Value=\"{c.Value}\"" +
                    (s.On("cards") ? " Description=\"وصف قصير للمستوى.\"" + (s.On("points") ? $" Points=\"{c.Value}\"" : "") : "") + " />\n")) +
                "  </ins:ChoiceMatrix.Columns>\n" +
                "  <ins:MatrixRow Header=\"الأول\" SelectedIndex=\"1\" />\n" +
                "  <ins:MatrixRow Header=\"الثاني\" />\n" +
                "</ins:ChoiceMatrix>";
            if (!s.On("sum") && !s.On("required")) return matrix;
            return "<StackPanel Spacing=\"8\">\n" +
                (s.On("required") ? "  <TextBlock Classes=\"hint\" Text=\"أجب عن كل صف.\" />\n" : "") +
                string.Concat(matrix.Split('\n').Select(l => $"  {l}\n")) +
                (s.On("sum") ? "  <TextBlock>\n    <Run Text=\"المجموع: \" /><Run FontWeight=\"Bold\" Text=\"{Binding #Matrix.Sum}\" />\n  </TextBlock>\n" : "") +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s.On("sum")) notes.Add("اختر في الصفوف، فيتغير المجموع تحت المصفوفة.");
            if (s.On("deselect")) notes.Add("انقر الخيار المختار في الصف الأول مرة ثانية فيُلغى.");
            if (s.On("required")) notes.Add("أجب عن الصف الثاني، فتصير `IsComplete` صحيحة ويظهر هنا الحدث.");
            return notes;
        },
        Stage = (view, s, say) =>
        {
            foreach (var m in view.GetLogicalDescendants().OfType<ChoiceMatrix>())
                m.SelectionChanged += (_, _) => say(s.On("required") ? $"حدث `SelectionChanged`، و`IsComplete` الآن `{m.IsComplete}`" : "حدث `SelectionChanged`");
        },
    };

    public static PlayDef[] All { get; } = [Matrix];
}
