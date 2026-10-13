using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class PageHeadPlay
{
    public const string Example =
        """
        <ins:PageHead Title="عنوان الصفحة" Subtitle="سطر وصف قصير">
          <ins:PageHead.Breadcrumb>
            <ins:Breadcrumb>
              <x:String>الرئيسية</x:String>
              <x:String>القسم</x:String>
              <x:String>الصفحة</x:String>
            </ins:Breadcrumb>
          </ins:PageHead.Breadcrumb>
          <ins:PageHead.Actions>
            <Button Classes="primary" Content="حفظ" />
          </ins:PageHead.Actions>
        </ins:PageHead>
        """;

    public static PlayDef Head { get; } = new()
    {
        Id = "head",
        Title = "رأس الصفحة",
        Wide = _ => true,
        Controls =
        [
            Text("title", "العنوان", "عنوان الصفحة"),
            Switch("sub", "سطر الوصف", "Subtitle", "سطر قصير تحت العنوان.", OptionKind.Property, on: true),
            Switch("actions", "الإجراءات", "Actions", "أزرار الصفحة في آخر الرأس، وتنزل تحت العنوان إذا ضاق السطر.", OptionKind.Property, on: true),
            Switch("crumbs", "مسار التنقل", "Breadcrumb", "مسار التنقل `ins:Breadcrumb` في أول الرأس، على سطر وحده.", OptionKind.Property, on: true),
        ],
        Extra =
        [
            Row("الأساس", "ins:PageHead", "رأس الصفحة: العنوان وما تحته، والإجراءات في آخره.", OptionKind.Element, true),
            Row("الأساس", "Title", "عنوان الصفحة، ويعلنه قارئ الشاشة عنوانًا.", OptionKind.Property),
            Row("الأساس", "TitleMinWidth", "أضيق ما يصير إليه عمود العنوان قبل أن تنزل الإجراءات تحته.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var parts = new List<string>();
            if (s.On("crumbs"))
                parts.Add("  <ins:PageHead.Breadcrumb>\n" +
                    "    <ins:Breadcrumb>\n" +
                    "      <x:String>الرئيسية</x:String>\n" +
                    "      <x:String>القسم</x:String>\n" +
                    "      <x:String>الصفحة</x:String>\n" +
                    "    </ins:Breadcrumb>\n" +
                    "  </ins:PageHead.Breadcrumb>");
            if (s.On("actions"))
                parts.Add("  <ins:PageHead.Actions>\n" +
                    "    <Button Classes=\"primary\" Content=\"حفظ\" />\n" +
                    "  </ins:PageHead.Actions>");
            var head = $"<ins:PageHead Title=\"{s["title"]}\"{(s.On("sub") ? " Subtitle=\"سطر وصف قصير\"" : "")}";
            return parts.Count == 0 ? head + " />" : head + ">\n" + string.Join("\n", parts) + "\n</ins:PageHead>";
        },
    };

    public static PlayDef Crumbs { get; } = new()
    {
        Id = "breadcrumb",
        Title = "مسار التنقل",
        Wide = _ => true,
        Controls =
        [
            Seg("depth", "عدد المستويات", "3",
                Choice("2", "2"),
                Choice("3", "3"),
                Choice("4", "4")),
            Text("here", "الصفحة الحالية", "الفواتير"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Breadcrumb", "مسار من عناصر نصية. والفاصل بينها مرسوم لا حرف مكتوب، فلا يقرؤه قارئ الشاشة، وينقلب مع اتجاه الواجهة.", OptionKind.Element, true),
            Row("الأساس", "IsCurrent", "العنصر الأخير هو الصفحة الحالية، لا يُنقر، ويُختصر نصه الطويل بثلاث نقاط.", OptionKind.Property),
            Row("الأساس", "ItemClick", "يُطلق حين يُنقر عنصر قبل الأخير، والعنصر في `Item`.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var trail = new[] { "الرئيسية", "الإعدادات", "الحساب" }.Take(int.Parse(s["depth"]) - 1).Append(s["here"]);
            return "<ins:Breadcrumb>\n" + string.Join("\n", trail.Select(t => $"  <x:String>{t}</x:String>")) + "\n</ins:Breadcrumb>";
        },
        Notes = s => s["here"].Length > 24 ? ["النص أطول مما يسعه العنصر، فاختُصر بثلاث نقاط."] : ["انقر عنصرًا قبل الأخير يظهر هنا الحدث."],
        Stage = (view, _, say) =>
        {
            foreach (var b in view.GetLogicalDescendants().OfType<Breadcrumb>())
                b.ItemClick += (_, e) => say($"حدث `ItemClick` على «{e.Item}»");
        },
    };

    public static PlayDef[] All { get; } = [Head, Crumbs];
}
