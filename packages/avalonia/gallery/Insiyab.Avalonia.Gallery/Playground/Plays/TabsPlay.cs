using Avalonia.Controls;
using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class TabsPlay
{
    public const string Example =
        """
        <TabControl AutomationProperties.Name="الإعدادات">
          <TabItem Header="عام" Content="محتوى «عام»." />
          <TabItem Header="الدفع" Content="محتوى «الدفع»." />
          <TabItem Header="الشحن" Content="محتوى «الشحن»." />
          <ins:TabLink Header="رابط" />
        </TabControl>
        """;

    public static PlayDef Tabs { get; } = new()
    {
        Id = "tabs",
        Title = "علامات التبويب",
        Wide = _ => true,
        Controls =
        [
            Seg("look", "الهيئة", "",
                Choice("", "عادية"),
                Choice("line", "بخط", "line", "هيئة أخف: التبويبات صف فوق خط رفيع، وتحت المختار منها خط ينتقل إلى التبويب الجديد."),
                Choice("seg", "مقسمة", "seg", "التبويبات شريط مقسم، يبدل الألواح بالآلية نفسها.")),
            Switch("link", "تبويب رابط", "ins:TabLink", "تبويب يذهب إلى مكان آخر بدل أن يعرض لوحًا: لا يُختار أبدًا، ويطلق `Click`، ويأخذ هيئة المختار ما دام `IsCurrent`.", OptionKind.Element),
        ],
        Extra =
        [
            Row("الأساس", "TabControl", "يضم التبويبات، ومعه `AutomationProperties.Name` يسمي المجموعة. وتتنقل الأسهم بين التبويبات، والسهم الأيسر في العربية هو «التالي».", OptionKind.Element, true),
            Row("الأساس", "TabItem", "تبويب واحد: عنوانه في `Header` ولوحه في `Content`.", OptionKind.Element, true),
            Row("الأساس", "SelectedIndex", "التبويب المختار، وهو الأول إن لم تذكره. وغيّره من الشيفرة لتختار تبويبًا.", OptionKind.Property),
            Row("الأساس", "SelectionChanged", "يُطلق عند اختيار تبويب، والمختار في `SelectedItem`.", OptionKind.Event),
        ],
        Disable = s => s["look"] == "seg" ? ["link"] : [],
        Axaml = s =>
        {
            string[] tabs = ["عام", "الحساب", "الإشعارات"];
            return $"<TabControl{Classes(s["look"])} AutomationProperties.Name=\"الإعدادات\">\n" +
                string.Concat(tabs.Select(t => $"  <TabItem Header=\"{t}\" Content=\"محتوى «{t}».\" />\n")) +
                (s.On("link") && s["look"] != "seg" ? "  <ins:TabLink Header=\"المساعدة\" />\n" : "") +
                "</TabControl>";
        },
        Notes = s => s["look"] == "seg"
            ? ["الشريط المقسم لا يحمل رابطًا، فخيار التبويب الرابط للهيئتين الأخريين."]
            : ["اختر تبويبًا، فيظهر هنا الحدث الذي يطلقه."],
        Stage = (view, _, say) =>
        {
            foreach (var tabs in view.GetLogicalDescendants().OfType<TabControl>())
                tabs.Loaded += (_, _) => tabs.SelectionChanged += (_, _) => { if (tabs.SelectedItem is TabItem t) say($"حدث `SelectionChanged`، والمختار «{t.Header}»"); };
        },
    };

    public static PlayDef[] All { get; } = [Tabs];
}
