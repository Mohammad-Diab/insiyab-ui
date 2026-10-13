using Avalonia.Controls;
using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ListPlay
{
    public const string Example =
        """
        <Border Classes="panel">
          <ListBox Classes="static">
            <ListBoxItem>
              <DockPanel>
                <Border Classes="panel-ico" DockPanel.Dock="Left"><ins:Icon Data="{StaticResource IconFile}" /></Border>
                <TextBlock Classes="list-end" DockPanel.Dock="Right" Text="أمس" />
                <StackPanel Classes="list-text">
                  <TextBlock Classes="list-title" Text="العنوان" />
                  <TextBlock Classes="list-desc" Text="سطر الوصف" />
                </StackPanel>
              </DockPanel>
            </ListBoxItem>
            <ListBoxItem>
              <StackPanel Classes="list-text">
                <TextBlock Classes="list-title" Text="العنوان" />
                <TextBlock Classes="list-desc" Text="سطر الوصف" />
              </StackPanel>
            </ListBoxItem>
            <ListBoxItem>
              <StackPanel Classes="list-text">
                <TextBlock Classes="list-title" Text="عنوان فقط" />
              </StackPanel>
            </ListBoxItem>
          </ListBox>
        </Border>
        """;

    private static readonly (string Title, string Desc, string End, string Icon)[] Rows =
    [
        ("عام", "الاسم واللغة", "أمس", "IconCog"),
        ("الحساب", "البريد وكلمة المرور", "اليوم", "IconUser"),
        ("الأمان", "الجلسات والأجهزة", "قبل ساعة", "IconLock"),
    ];

    public static PlayDef List { get; } = new()
    {
        Id = "list",
        Title = "القائمة",
        Wide = _ => true,
        Controls =
        [
            Switch("panel", "في لوح", "panel", "على `Border` حول القائمة: في لوح تمتد القائمة إلى حوافه. وليس للقائمة سطح خاص بها، فتصلح خارجه أيضًا وتكفيها فواصلها.", on: true),
            Switch("start", "أيقونة في البداية", "panel-ico", "على `Border` فيه أيقونة يبدأ بها الصف، ولك أن تضع مكانها `ins:Avatar`.", on: true),
            Switch("desc", "سطر الوصف", "list-desc", "سطر ثان تحت العنوان، بخط أصغر ولون أهدأ.", on: true),
            Switch("end", "خاتمة", "list-end", "ما ينتهي به الصف، كوقت أو شارة أو زر.", on: true),
            Switch("link", "صفوف تُنقر", null, null, group: "صفوف تُنقر"),
            Switch("current", "الصف الحالي", "SelectedIndex=\"0\"", "يميز الصف الحالي بخلفية ولون الهوية على عنوانه.", OptionKind.Property, "صفوف تُنقر"),
        ],
        Extra =
        [
            Row("الأساس", "ListBox", "صفوف متتالية بينها فواصل، كل صف `ListBoxItem`، وكل ما فيه اختياري.", OptionKind.Element, true),
            Row("الأساس", "static", "على `ListBox`: صفوف تُقرأ فحسب، لا تُنقر ولا يقف عندها التنقل بلوحة المفاتيح. فإن حذفته صار كل صف قابلًا للنقر، وظهر في آخره سهم يشير إلى وجهته.", OptionKind.Class, true),
            Row("الأساس", "list-text", "على `StackPanel` يضم نصي الصف، ويأخذ ما بقي من عرضه.", OptionKind.Class),
            Row("الأساس", "list-title", "عنوان الصف، وإذا طال قُطع بنقاط.", OptionKind.Class),
            Row("صفوف تُنقر", "SelectionChanged", "يُطلق حين يُنقر صف، والصف الجديد في `SelectedItem`.", OptionKind.Event),
        ],
        Disable = s => s.On("link") ? [] : ["current"],
        Axaml = s =>
        {
            var pad = s.On("panel") ? "  " : "";
            var rows = Rows.Select(r =>
            {
                var text = $"<StackPanel Classes=\"list-text\">\n  <TextBlock Classes=\"list-title\" Text=\"{r.Title}\" />" +
                    (s.On("desc") ? $"\n  <TextBlock Classes=\"list-desc\" Text=\"{r.Desc}\" />" : "") + "\n</StackPanel>";
                var parts = new List<string>();
                if (s.On("start")) parts.Add($"<Border Classes=\"panel-ico\" DockPanel.Dock=\"Left\"><ins:Icon Data=\"{{StaticResource {r.Icon}}}\" /></Border>");
                if (s.On("end")) parts.Add($"<TextBlock Classes=\"list-end\" DockPanel.Dock=\"Right\" Text=\"{r.End}\" />");
                var inner = parts.Count == 0 ? text : "<DockPanel>\n" + Indent(string.Join("\n", parts.Append(text)), "  ") + "\n</DockPanel>";
                return "<ListBoxItem>\n" + Indent(inner, "  ") + "\n</ListBoxItem>";
            });
            var head = s.On("link")
                ? "<ListBox" + (s.On("current") ? " SelectedIndex=\"0\"" : "") + " AutomationProperties.Name=\"الإعدادات\">"
                : "<ListBox Classes=\"static\">";
            var list = head + "\n" + Indent(string.Join("\n", rows), "  ") + "\n</ListBox>";
            return s.On("panel") ? "<Border Classes=\"panel\">\n" + Indent(list, pad) + "\n</Border>" : list;
        },
        Notes = s => s.On("link") ? ["انقر صفًا فيصير هو المحدد، ويظهر هنا الحدث الذي يطلقه."] : [],
        Stage = (view, s, say) =>
        {
            if (!s.On("link")) return;
            foreach (var list in view.GetLogicalDescendants().OfType<ListBox>())
                list.SelectionChanged += (_, _) => say($"حدث `SelectionChanged`، و`SelectedIndex` الآن `{list.SelectedIndex}`");
        },
    };

    private static string Indent(string text, string pad) => string.Join("\n", text.Split('\n').Select(l => pad + l));

    public static PlayDef[] All { get; } = [List];
}
