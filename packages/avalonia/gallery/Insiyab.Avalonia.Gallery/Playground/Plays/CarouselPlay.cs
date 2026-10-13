using Avalonia.Controls;
using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class CarouselPlay
{
    public const string Example =
        """
        <Carousel AutomationProperties.Name="شرائح">
          <Border Classes="car car1"><TextBlock Text="الأولى" /></Border>
          <Border Classes="car car2"><TextBlock Text="الثانية" /></Border>
          <Border Classes="car car3"><TextBlock Text="الثالثة" /></Border>
          <Border Classes="car car4"><TextBlock Text="الرابعة" /></Border>
        </Carousel>
        """;

    public static string Code { get; } =
        """
        Projects.SelectionChanged += (_, _) =>
            Debug.WriteLine(Projects.SelectedIndex);   // الشريحة في أول العرض

        var at = Projects.SelectedIndex;               // الموضع الحالي، من الصفر
        Projects.SelectedIndex = 2;                    // انتقل إلى الموضع الثالث
        """;

    private static readonly string[] Words = ["الأولى", "الثانية", "الثالثة", "الرابعة", "الخامسة", "السادسة"];

    public static PlayDef Carousel { get; } = new()
    {
        Id = "carousel",
        Title = "عرض الشرائح",
        Wide = _ => true,
        Controls =
        [
            Seg("per", "شرائح معًا", "",
                Choice("", "واحدة"),
                Choice("two", "اثنتان", "two", "شريحتان معًا في عرض لا يقل عن 576 بكسلًا، وإلا فواحدة. فالعرض نفسه هو الذي يقرر لا النافذة."),
                Choice("three", "ثلاث", "three", "ثلاث شرائح معًا في عرض لا يقل عن 896 بكسلًا، واثنتان تحت ذلك، وواحدة تحت 576.")),
            Seg("count", "عدد الشرائح", "4",
                Choice("1", "1"),
                Choice("4", "4"),
                Choice("6", "6")),
            Switch("peek", "طرف التالية", "peek", "يكشف طرف الشريحة التالية، فيعرف المستخدم أن بعدها المزيد."),
            Switch("dots", "بلا نقاط", "no-dots", "يخفي النقاط تحت العرض، ويبقى الزران."),
        ],
        Extra =
        [
            Row("الأساس", "Carousel", "عرض شرائح كل عنصر فيه شريحة: صف تسحبه بإصبعك، وزرا السابق والتالي، ونقطة لكل موضع، وأسهم لوحة المفاتيح تسير مع اتجاه القراءة. ويخبر قارئ الشاشة عند كل شريحة أنها «2 من 4».", OptionKind.Element, true),
            Row("الأساس", "AutomationProperties.Name", "اسم العرض يقرؤه قارئ الشاشة.", OptionKind.Property),
            Row("الأساس", "SelectionChanged", "يُطلق كلما تغيرت الشريحة في أول العرض، وموضعها في `SelectedIndex`.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var slides = Words.Take(int.Parse(s["count"])).Select((w, i) => $"  <Border Classes=\"car car{i % 4 + 1}\"><TextBlock Text=\"{w}\" /></Border>");
            return $"<Carousel{Classes(s["per"], s.On("peek") ? "peek" : null, s.On("dots") ? "no-dots" : null)} AutomationProperties.Name=\"شرائح\">\n" +
                string.Join("\n", slides) + "\n</Carousel>";
        },
        Notes = s =>
        {
            if (s["count"] == "1") return ["شريحة واحدة لا تحتاج زرين ولا نقاطًا، فأخفاها العرض."];
            if (s["per"] == "two") return ["لا تجتمع شريحتان إلا في عرض لا يقل عن 576 بكسلًا، وهذه المعاينة أضيق من ذلك، فتبقى فيها شريحة واحدة."];
            if (s["per"] == "three") return ["لا تجتمع ثلاث شرائح إلا في عرض لا يقل عن 896 بكسلًا، واثنتان من 576. وهذه المعاينة أضيق من ذلك، فتبقى فيها شريحة واحدة."];
            return ["انتقل إلى شريحة أخرى، فيظهر هنا الحدث. والصفائح الملونة `car` للمعرض وحده."];
        },
        Stage = (view, _, say) =>
        {
            foreach (var c in view.GetLogicalDescendants().OfType<global::Avalonia.Controls.Carousel>())
                c.SelectionChanged += (_, _) => say($"حدث `SelectionChanged`، و`SelectedIndex` الآن `{c.SelectedIndex}`");
        },
    };

    public static PlayDef[] All { get; } = [Carousel];
}
