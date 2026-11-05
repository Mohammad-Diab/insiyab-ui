using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class AccordionPlay
{
    public const string Example =
        """
        <Border Classes="panel">
          <StackPanel Classes="accordion">
            <Expander Header="القسم الأول" Content="محتوى القسم الأول." IsExpanded="True" />
            <Expander Header="القسم الثاني" Content="محتوى القسم الثاني." />
            <Expander Header="القسم الثالث" Content="محتوى القسم الثالث." />
          </StackPanel>
        </Border>
        """;

    public static PlayDef Accordion { get; } = new()
    {
        Id = "accordion",
        Title = "الأكورديون",
        Wide = _ => true,
        WideMax = 560,
        Controls =
        [
            Switch("one", "قسم واحد مفتوح", "ins:Collapse.Group", "أعط الأقسام كلها اسم مجموعة واحدًا، فإذا فتحت أحدها انغلق الباقي.", OptionKind.Property),
            Switch("open", "الأول مفتوح", on: true),
            Switch("panel", "في لوح", on: true),
        ],
        Extra =
        [
            Row("الأساس", "accordion", "على `StackPanel` يضم أقسامًا من `Expander`، ويفصل بينها بخط رفيع، وتنقل الأسهم و`Home` و`End` التركيز بين عناوينها. ولا سطح له، فضعه في `Border.panel` فتستقيم زوايا أقسامه على زواياه.", OptionKind.Class, true),
        ],
        Axaml = s =>
        {
            var group = s.On("one") ? " ins:Collapse.Group=\"group\"" : "";
            var pad = s.On("panel") ? "    " : "  ";
            string[] words = ["الأول", "الثاني", "الثالث"];
            var items = string.Join("\n", words.Select((w, i) =>
                $"{pad}<Expander{group} Header=\"القسم {w}\" Content=\"محتوى القسم {w}.\"{(i == 0 && s.On("open") ? " IsExpanded=\"True\"" : "")} />"));
            var acc = (s.On("panel") ? "  " : "") + "<StackPanel Classes=\"accordion\">\n" + items + "\n" + (s.On("panel") ? "  " : "") + "</StackPanel>";
            return s.On("panel") ? "<Border Classes=\"panel\">\n" + acc + "\n</Border>" : acc;
        },
        Notes = s => [s.On("one") ? "افتح قسمًا آخر، فينغلق المفتوح قبله." : "افتح قسمًا آخر، فيبقى المفتوح قبله مفتوحًا. وفعّل «قسم واحد مفتوح» لينغلق."],
    };

    public static PlayDef Collapse { get; } = new()
    {
        Id = "collapse",
        Title = "القسم المنفرد",
        Wide = _ => true,
        WideMax = 560,
        Controls =
        [
            Text("title", "العنوان", "خيارات متقدمة"),
            Switch("open", "مفتوح", "IsExpanded=\"True\"", "يبدأ القسم مفتوحًا.", OptionKind.Property),
            Switch("flush", "رأس بلا حشو", "flush", "على `Expander`: رأس بلا حشو ولا خلفية تحت المؤشر، وسهمه عند أول سطر منه، ومحتوى بلا حشو، فيصلح صف قائمة رأسًا له."),
        ],
        Extra =
        [
            Row("الأساس", "Expander", "قسم ينفتح بالنقر على عنوانه وبلوحة المفاتيح، وينساب وهو ينفتح. ويصلح وحده أو داخل `StackPanel.accordion`.", OptionKind.Element, true),
            Row("الأساس", "Header", "عنوان القسم، وبالنقر عليه ينفتح وينغلق.", OptionKind.Property, true),
            Row("الأساس", "Content", "محتوى القسم، نصًا كان أو عنصرًا.", OptionKind.Property, true),
        ],
        Axaml = s => $"<Expander{(s.On("flush") ? " Classes=\"flush\"" : "")} Header=\"{s["title"]}\" Content=\"محتوى إضافي.\"{(s.On("open") ? " IsExpanded=\"True\"" : "")} />",
    };

    public static PlayDef Fold { get; } = new()
    {
        Id = "fold",
        Title = "الطي",
        Wide = _ => true,
        WideMax = 560,
        Controls =
        [
            Switch("chevron", "سهم يدور", "fold", "على `ins:Icon`: سهم يشير إلى الأسفل ويدور إلى الأعلى إذا حمل الكلاس `open`، فاربط `Classes.open` بحالة الطي. ولا يدور بحركة إذا خُففت المؤثرات.", on: true),
            Switch("still", "بلا تلاشٍ", "Fade=\"False\"", "يطول ويقصر دون أن يتلاشى ما فيه.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Fold", "يحمل عنصرًا واحدًا، يطول من لا شيء إذا صار `IsOpen` صحيحًا ويقصر حتى يختفي، فيتجاوزه تباعد اللوح الذي هو فيه. ولا يقص ما فيه إلا وهو يتحرك، ولا يتحرك قبل أول عرض ولا عند تبدل `DataContext` ولا إذا خُففت المؤثرات.", OptionKind.Element, true),
            Row("الأساس", "IsOpen", "مفتوح أو مطوي.", OptionKind.Property, true),
            Row("خيارات أخرى", "Bleed", "مسافة تبقى ظاهرة على الجانبين وفوق المحتوى وهو يتحرك، فلا تُقص ظلال ما فيه ولا حلقات التركيز، ويُقص أسفله وحده.", OptionKind.Property),
            Row("خيارات أخرى", "EasesHeight", "إذا تغير طول ما فيه وهو مفتوح انساب إلى طوله الجديد.", OptionKind.Property),
            Row("خيارات أخرى", "EaseNext()", "ينساب التغير التالي وحده في طول ما فيه.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var button = s.On("chevron")
                ? "  <ToggleButton x:Name=\"more\">\n    <StackPanel Orientation=\"Horizontal\" Spacing=\"6\">\n      <TextBlock Text=\"التفاصيل\" />\n      <ins:Icon Classes=\"fold\" Classes.open=\"{Binding #more.IsChecked}\" />\n    </StackPanel>\n  </ToggleButton>"
                : "  <ToggleButton x:Name=\"more\" Content=\"التفاصيل\" />";
            return "<StackPanel Spacing=\"8\">\n" + button + "\n" +
                "  <ins:Fold IsOpen=\"{Binding #more.IsChecked}\"" + (s.On("still") ? " Fade=\"False\"" : "") + ">\n" +
                "    <TextBlock TextWrapping=\"Wrap\" Text=\"يظهر هذا النص وهو ينساب من تحت الزر، ويختفي حين تنقره ثانية.\" />\n" +
                "  </ins:Fold>\n</StackPanel>";
        },
        Notes = _ => ["انقر الزر فينفتح ما تحته وينطوي."],
    };

    public static PlayDef[] All { get; } = [Accordion, Collapse, Fold];
}
