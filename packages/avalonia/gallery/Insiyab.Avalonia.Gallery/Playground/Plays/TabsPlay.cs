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

    public static PlayDef Documents { get; } = new()
    {
        Id = "documents",
        Title = "تبويبات الملفات",
        Wide = _ => true,
        Controls =
        [
            Seg("count", "عدد التبويبات", "few",
                Choice("few", "قليلة"),
                Choice("many", "كثيرة")),
            Switch("new", "زر تبويب جديد", "ShowNewButton=\"True\"", "يُظهر الزر «+» بعد آخر تبويب، ويطلق `NewTabRequested` إذا ضُغط.", OptionKind.Property),
            Switch("pinned", "تبويب مثبت", "IsClosable=\"False\"", "على `ins:DocumentTab`: تبويب لا يُغلق، فلا زر إغلاق عليه، ولا تغلقه الاختصارات ولا الزر الأوسط.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:DocumentTabs", "تبويبات لما فتحه المستخدم من ملفات أو صفحات، كتبويبات المتصفح، بهيئة علامات التبويب نفسها. ومعه `AutomationProperties.Name` يسمي المجموعة.", OptionKind.Element, true),
            Row("الأساس", "ins:DocumentTab", "تبويب واحد: عنوانه في `Header` ولوحه في `Content`، وزر إغلاقه يظهر على المختار وتحت المؤشر. ومع `ItemsSource` تُصنع التبويبات وحدها.", OptionKind.Element, true),
            Row("الأساس", "TabCloseRequested", "يُطلق إذا طُلب إغلاق تبويب، وعنصره في `Item`. فإن لم يعالجه أحد أُغلق التبويب، فاجعل `Handled` لتغلقه بنفسك أو لتبقيه مفتوحًا، كأن تسأل عن حفظ التغييرات.", OptionKind.Event),
            Row("الأساس", "TabMoveRequested", "يُطلق إذا أُفلت تبويب في مكان جديد، ومعه `From` و`To`. فإن لم يعالجه أحد نُقل التبويب، والمختار يبقى مختارًا.", OptionKind.Event),
            Row("الأساس", "CloseLabel", "اسم زر الإغلاق لقارئ الشاشة، وهو «إغلاق التبويب» إن لم تذكره. ومثله `NewLabel` للزر «+».", OptionKind.Property),
            Row("خيارات أخرى", "NewTabRequested", "يُطلق عند الضغط على «+»، فأضف أنت التبويب الجديد واختره.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            string[] names = s["count"] == "many"
                ? ["تقرير الربع الأول", "خطة المشروع", "ملاحظات الاجتماع", "الميزانية", "قائمة المهام", "العقد", "سجل التغييرات", "دليل الاستخدام", "عرض العملاء", "جدول المواعيد", "مسودة الرسالة"]
                : ["تقرير الربع الأول", "خطة المشروع", "ملاحظات الاجتماع"];
            return $"<ins:DocumentTabs{(s.On("new") ? " ShowNewButton=\"True\"" : "")} AutomationProperties.Name=\"الملفات المفتوحة\">\n" +
                (s.On("pinned") ? "  <ins:DocumentTab Header=\"الرئيسية\" IsClosable=\"False\" Content=\"محتوى «الرئيسية»، ولا يُغلق.\" />\n" : "") +
                string.Concat(names.Select(n => $"  <ins:DocumentTab Header=\"{n}\" Content=\"محتوى «{n}».\" />\n")) +
                "</ins:DocumentTabs>";
        },
        Notes = s =>
        {
            var notes = new List<string> { "أغلق تبويبًا بزره أو بـ`Ctrl+W`، واسحب تبويبًا إلى مكان آخر، فيظهر هنا الحدث. وغيّر أي خيار لتعيد التبويبات." };
            if (s["count"] == "many") notes.Add("التبويبات أكثر مما يسعه الشريط، فظهر في طرفيه سهمان يمرّرانه.");
            if (s.On("new")) notes.Add("اضغط «+» فيضيف هذا المثال تبويبًا جديدًا ويختاره، كما يفعل تطبيقك عند `NewTabRequested`.");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var tabs in view.GetLogicalDescendants().OfType<Insiyab.Avalonia.Controls.DocumentTabs>())
            {
                tabs.TabCloseRequested += (_, e) => say($"حدث `TabCloseRequested` على «{(e.Item as TabItem)?.Header}»، فأُغلق");
                tabs.TabMoveRequested += (_, e) => say($"حدث `TabMoveRequested`: «{(e.Item as TabItem)?.Header}» من `{e.From}` إلى `{e.To}`");
                var added = 0;
                tabs.NewTabRequested += (_, _) =>
                {
                    added++;
                    var header = added == 1 ? "ملف جديد" : $"ملف جديد {added}";
                    tabs.Items.Add(new Insiyab.Avalonia.Controls.DocumentTab { Header = header, Content = $"محتوى «{header}»." });
                    tabs.SelectedIndex = tabs.ItemCount - 1;
                    say("حدث `NewTabRequested`، وأضاف المثال تبويبًا");
                };
            }
        },
    };

    public static PlayDef[] All { get; } = [Tabs, Documents];
}
