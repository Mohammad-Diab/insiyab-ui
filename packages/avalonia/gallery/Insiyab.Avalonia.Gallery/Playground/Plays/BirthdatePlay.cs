using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class BirthdatePlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="تاريخ الميلاد" />
          <ins:BirthDateField />
        </StackPanel>
        """;

    public const string FormExample =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="تاريخ الميلاد" />
          <ins:BirthDateField SelectedDate="1993-01-22" IsRequired="True" />
        </StackPanel>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="12">
          <Button Classes="secondary" Content="إعادة الضبط" />
          <Button Classes="primary" Content="إرسال" />
        </StackPanel>
        """;

    public static string Code { get; } =
        """
        Birth.DateChanged += (_, e) =>
        {
            var iso = e.Value;      // فارغ ما دام التاريخ ناقصًا
        };

        var value = Birth.Value;                   // التاريخ بصيغة ISO
        Birth.SetDate(new DateTime(1993, 1, 22));  // ضبطه بهدوء، دون حدث
        Birth.Clear();                             // مسحه
        """;

    public static PlayDef Birth { get; } = new()
    {
        Id = "birth",
        Title = "حقل الميلاد",
        Wide = s => s["look"] == "",
        WideMax = 320,
        Controls =
        [
            Seg("look", "الشكل", "",
                Choice("", "حقل"),
                Choice("Inline", "كبير", "Layout=\"Inline\"", "حين يكون تاريخ الميلاد هو السؤال الوحيد في الشاشة، كخطوة في معالج تسجيل: كتل كبيرة ينمو فيها الرقم، وتحت كل كتلة عمودها، دون نافذة منبثقة.", OptionKind.Property)),
            Seg("min", "أقل عمر", "",
                Choice("", "بلا"),
                Choice("18", "18", "MinAge=\"18\"", "أقل عمر مقبول، فينتهي عمود السنة عنده، ويُرفض كل تاريخ أقرب منه برسالة تقول السبب. الافتراض 0.", OptionKind.Property)).In("العمر"),
            Seg("max", "أكبر عمر", "",
                Choice("", "120"),
                Choice("70", "70", "MaxAge=\"70\"", "أكبر عمر، فيقصر عمود السنة. الافتراض 120.", OptionKind.Property)).In("العمر"),
            Seg("start", "عمر البداية", "",
                Choice("", "31"),
                Choice("22", "22", "StartAge=\"22\"", "العمر الذي يبدأ منه عمود السنة، لتطبيق يعرف أعمار جمهوره. الافتراض 31، وهو الوسيط العالمي للعمر.", OptionKind.Property)).In("العمر"),
            Switch("value", "فيه تاريخ", "SelectedDate=\"1993-01-22\"", "التاريخ الذي يبدأ منه الحقل. ويُضبط من الشيفرة بهدوء، دون حدث.", OptionKind.Property),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يبهت الحقل كله، فلا يُكتب فيه ولا تُفتح أعمدته.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:BirthDateField", "حقل لليوم والشهر والسنة تكتب فيه أو تختار من الأعمدة، ويحمل التاريخ ميلاديًا دائمًا.", OptionKind.Element, true),
            Row("الأساس", "Value", "التاريخ كما يُرسل، بصيغة ISO مثل `1993-01-22`، أو فراغ ما دام ناقصًا.", OptionKind.Property),
            Row("الأساس", "IsRequired=\"True\"", "يقول الحقل الفارغ إنه مطلوب، ورسالته في `Message`.", OptionKind.Property),
            Row("الأساس", "DateChanged", "يُطلق حين يتغير التاريخ، وفيه `Value`، أو فراغ ما دام ناقصًا.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var attrs = (s.Has("look") ? " Layout=\"Inline\"" : "") +
                (s.Has("min") ? $" MinAge=\"{s["min"]}\"" : "") + (s.Has("max") ? $" MaxAge=\"{s["max"]}\"" : "") + (s.Has("start") ? $" StartAge=\"{s["start"]}\"" : "") +
                (s.On("value") ? " SelectedDate=\"1993-01-22\"" : "") + (s.On("disabled") ? " IsEnabled=\"False\"" : "");
            return "<StackPanel Classes=\"field\">\n" +
                "  <TextBlock Classes=\"label\" Text=\"تاريخ الميلاد\" />\n" +
                $"  <ins:BirthDateField{attrs} />\n" +
                "</StackPanel>";
        },
        Notes = s =>
        {
            if (s.Has("min") || s.Has("max") || s.Has("start"))
                return [s.Has("look") ? "انظر عمود السنة، فقد تغيرت سنواته." : "افتح الأعمدة بزر التقويم، فتجد سنوات عمود السنة قد تغيرت."];
            return s.Has("look") || s.On("disabled") ? [] : ["اكتب التاريخ في الحقل، أو افتح الأعمدة بزر التقويم في طرفه."];
        },
        Stage = (view, _, say) =>
        {
            foreach (var f in view.GetLogicalDescendants().OfType<BirthDateField>())
                f.DateChanged += (_, e) => say($"حدث `DateChanged`، و`Value` الآن `{(e.Value.Length > 0 ? e.Value : "—")}`");
        },
    };

    public static PlayDef[] All { get; } = [Birth];
}
