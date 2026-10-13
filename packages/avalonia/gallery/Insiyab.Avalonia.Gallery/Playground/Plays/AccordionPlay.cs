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
        ],
        Extra =
        [
            Row("الأساس", "Expander", "قسم ينفتح بالنقر على عنوانه وبلوحة المفاتيح، وينساب وهو ينفتح. ويصلح وحده أو داخل `StackPanel.accordion`.", OptionKind.Element, true),
            Row("الأساس", "Header", "عنوان القسم، وبالنقر عليه ينفتح وينغلق.", OptionKind.Property, true),
            Row("الأساس", "Content", "محتوى القسم، نصًا كان أو عنصرًا.", OptionKind.Property, true),
        ],
        Axaml = s => $"<Expander Header=\"{s["title"]}\" Content=\"محتوى إضافي.\"{(s.On("open") ? " IsExpanded=\"True\"" : "")} />",
    };

    public static PlayDef[] All { get; } = [Accordion, Collapse];
}
