using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class TreePlay
{
    public const string Example =
        """
        <TreeView AutomationProperties.Name="الملفات">
          <TreeViewItem IsExpanded="True">
            <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconFolder}" /><TextBlock Text="العقود" /></StackPanel></TreeViewItem.Header>
            <TreeViewItem>
              <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconPages}" /><TextBlock Text="عقد.pdf" /></StackPanel></TreeViewItem.Header>
            </TreeViewItem>
            <TreeViewItem>
              <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconFolder}" /><TextBlock Text="الملاحق" /></StackPanel></TreeViewItem.Header>
              <TreeViewItem>
                <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconPages}" /><TextBlock Text="ملحق 1.pdf" /></StackPanel></TreeViewItem.Header>
              </TreeViewItem>
              <TreeViewItem>
                <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconPages}" /><TextBlock Text="ملحق 2.pdf" /></StackPanel></TreeViewItem.Header>
              </TreeViewItem>
            </TreeViewItem>
          </TreeViewItem>
          <TreeViewItem>
            <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconFolder}" /><TextBlock Text="الفواتير" /></StackPanel></TreeViewItem.Header>
            <TreeViewItem>
              <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconPages}" /><TextBlock Text="فاتورة.pdf" /></StackPanel></TreeViewItem.Header>
            </TreeViewItem>
          </TreeViewItem>
          <TreeViewItem>
            <TreeViewItem.Header><StackPanel Classes="tree-label"><ins:Icon Data="{StaticResource IconPages}" /><TextBlock Text="ملاحظات.txt" /></StackPanel></TreeViewItem.Header>
          </TreeViewItem>
        </TreeView>
        """;

    public static string Code { get; } =
        """
        Files.SelectionChanged += (_, _) => Debug.WriteLine(Files.SelectedItem);   // اختيار بند
        Files.AddHandler(Tree.ActivatedEvent, (_, e) => Open(e.Source));         // Enter أو نقرتان على ملف

        Tree.Reveal(item);                       // يختاره ويفتح الطريق إليه
        var ticked = Tree.CheckedItems(Files);   // البنود المحددة في شجرة مربعات
        """;

    public static PlayDef Files { get; } = new()
    {
        Id = "tree",
        Title = "الشجرة",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Switch("checks", "بمربعات اختيار", "ins:Tree.Checks=\"True\"", "على `TreeView`، وتسمية كل بند `CheckBox`: إذا حددت فرعًا تحدد كل ما فيه، والفرع المحدد بعضه يظهر في حالة وسطى.", OptionKind.Property, "مربعات الاختيار"),
            Switch("icons", "بأيقونات", on: true),
            Switch("open", "فرع مفتوح", "IsExpanded=\"True\"", "على `TreeViewItem`: فرع مفتوح من البداية، وإلا بقي مغلقًا.", OptionKind.Property, "البند", on: true),
            Switch("count", "عدد في آخر الصف", "tree-end", "على `TextBlock` في آخر تسمية البند، ومثله شارة `ins:Pill` أو `ins:Badge`.", group: "البند"),
            Switch("none", "بلا اختيار", "ins:Tree.SelectNone=\"True\"", "يلغي الاختيار، حين تكون الشجرة للتنقل وحده.", OptionKind.Property),
            Switch("selected", "بند مختار", "IsSelected=\"True\"", "على `TreeViewItem`: بند مختار من البداية.", OptionKind.Property, "البند"),
            Switch("menu", "قائمة بالزر الأيمن", "ContextMenu", "قائمة الشجرة، تفتح على الصف الذي نُقر بالزر الأيمن أو بمفتاح القائمة.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "TreeView", "قوائم متداخلة تصير شجرة: تسمية كل بند `Header`، وما فيه فرعه. وتصل إليها بضغطة Tab واحدة ثم تتنقل بالأسهم.", OptionKind.Element, true),
            Row("الأساس", "tree-label", "على `StackPanel` في التسمية: أيقونة ثم نص.", OptionKind.Class),
            Row("الأساس", "AutomationProperties.Name", "اسم الشجرة يقرؤه قارئ الشاشة.", OptionKind.Property),
            Row("الأحداث", "SelectionChanged", "يُطلق حين يُختار بند.", OptionKind.Event),
            Row("الأحداث", "ins:Tree.Activated", "يُطلق على ملف حين يُضغط Enter عليه أو يُنقر نقرتين.", OptionKind.Event),
        ],
        Disable = s =>
        {
            if (s.On("checks")) return ["icons", "selected", "none"];
            return s.On("none") ? ["selected"] : [];
        },
        Axaml = s =>
        {
            string Head(string text, string icon, string? count = null, bool tick = false)
            {
                if (s.On("checks")) return $"<TreeViewItem.Header><CheckBox Content=\"{text}\"{(tick ? " IsChecked=\"True\"" : "")} /></TreeViewItem.Header>";
                var label = s.On("icons")
                    ? $"<StackPanel Classes=\"tree-label\"><ins:Icon Data=\"{{StaticResource {icon}}}\" /><TextBlock Text=\"{text}\" /></StackPanel>"
                    : $"<TextBlock Text=\"{text}\" />";
                if (count is not null && s.On("count")) label = $"<DockPanel><TextBlock Classes=\"tree-end\" DockPanel.Dock=\"Right\" Text=\"{count}\" />{label}</DockPanel>";
                return $"<TreeViewItem.Header>{label}</TreeViewItem.Header>";
            }
            var attrs = s.On("checks") ? " ins:Tree.Checks=\"True\"" : s.On("none") ? " ins:Tree.SelectNone=\"True\"" : "";
            var picked = s.On("selected") && !s.On("checks") && !s.On("none") ? " IsSelected=\"True\"" : "";
            var menu = s.On("menu")
                ? "  <TreeView.ContextMenu>\n" +
                  "    <ContextMenu>\n" +
                  "      <MenuItem Header=\"فتح\" />\n" +
                  "      <MenuItem Header=\"إعادة تسمية\" />\n" +
                  "      <Separator />\n" +
                  "      <MenuItem Classes=\"bad\" Header=\"حذف\" />\n" +
                  "    </ContextMenu>\n" +
                  "  </TreeView.ContextMenu>\n"
                : "";
            return $"<TreeView{attrs} AutomationProperties.Name=\"الملفات\">\n" + menu +
                $"  <TreeViewItem{(s.On("open") ? " IsExpanded=\"True\"" : "")}>\n" +
                $"    {Head("المستندات", "IconFolder", "2")}\n" +
                "    <TreeViewItem>\n" +
                $"      {Head("ملف.pdf", "IconFile", tick: true)}\n" +
                "    </TreeViewItem>\n" +
                "    <TreeViewItem>\n" +
                $"      {Head("الصور", "IconFolder", "2")}\n" +
                "      <TreeViewItem>\n" +
                $"        {Head("صورة 1.png", "IconFile")}\n" +
                "      </TreeViewItem>\n" +
                "      <TreeViewItem>\n" +
                $"        {Head("صورة 2.png", "IconFile")}\n" +
                "      </TreeViewItem>\n" +
                "    </TreeViewItem>\n" +
                "  </TreeViewItem>\n" +
                $"  <TreeViewItem{picked}>\n" +
                $"    {Head("ملاحظات.txt", "IconFile")}\n" +
                "  </TreeViewItem>\n" +
                "</TreeView>";
        },
        Notes = s =>
        {
            var notes = new List<string> { "انقر بندًا أو افتح فرعًا، أو انقر ملفًا نقرتين، فيظهر هنا الحدث." };
            if (s.On("checks")) notes.Add("حُدد «ملف.pdf» وحده، فظهر فرعه محددًا بعضه.");
            else if (s.On("none")) notes.Add("لا يُختار البند هنا، وتبقى الأسهم تنقلك وتفتح الفروع.");
            if (s.On("menu")) notes.Add("انقر صفًا بالزر الأيمن لترى القائمة.");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var tree in view.GetLogicalDescendants().OfType<TreeView>())
            {
                tree.SelectionChanged += (_, _) => { if (tree.SelectedItem is TreeViewItem item) say($"حدث `SelectionChanged` على «{Tree.LabelOf(item)}»"); };
                tree.AddHandler(Tree.ActivatedEvent, (_, e) => { if (e.Source is TreeViewItem item) say($"حدث `ins:Tree.Activated` على «{Tree.LabelOf(item)}»"); });
            }
        },
    };

    public static PlayDef[] All { get; } = [Files];
}
