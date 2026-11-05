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

    public const string SettingsExample =
        """
        <Border Classes="panel">
          <ListBox Classes="static">
            <ListBoxItem>
              <DockPanel>
                <StackPanel Classes="list-end" DockPanel.Dock="Right">
                  <ToggleSwitch IsChecked="True" AutomationProperties.Name="الإشعارات" />
                </StackPanel>
                <StackPanel Classes="list-text">
                  <TextBlock Classes="list-title" Text="الإشعارات" />
                  <TextBlock Classes="list-desc" Text="تنبيه حين تصل رسالة جديدة" />
                </StackPanel>
              </DockPanel>
            </ListBoxItem>
            <ListBoxItem>
              <DockPanel>
                <StackPanel Classes="list-end" DockPanel.Dock="Right">
                  <ComboBox SelectedIndex="0" Width="140" AutomationProperties.Name="اللغة">
                    <ComboBoxItem Content="العربية" />
                    <ComboBoxItem Content="English" />
                  </ComboBox>
                </StackPanel>
                <StackPanel Classes="list-text">
                  <TextBlock Classes="list-title" Text="اللغة" />
                  <TextBlock Classes="list-desc" Text="لغة الواجهة والتواريخ" />
                </StackPanel>
              </DockPanel>
            </ListBoxItem>
            <ListBoxItem>
              <DockPanel>
                <StackPanel Classes="list-end" DockPanel.Dock="Right">
                  <ins:Pill Classes="ok dot sm" Content="مفعّل" />
                  <Button Classes="secondary sm" Content="إدارة" />
                </StackPanel>
                <StackPanel Classes="list-text">
                  <TextBlock Classes="list-title" Text="التحقق بخطوتين" />
                  <TextBlock Classes="list-desc" Text="رمز يصل إلى هاتفك عند كل دخول" />
                </StackPanel>
              </DockPanel>
            </ListBoxItem>
          </ListBox>
        </Border>
        """;

    private static readonly (string Head, (string Title, string From)[] Rows)[] Mail =
    [
        ("اليوم", [("تأكيد الحجز", "خدمة العملاء"), ("فاتورة الشهر", "قسم المالية"), ("تذكير بالاجتماع", "إدارة المشاريع")]),
        ("أمس", [("رد على الاستفسار", "فريق الدعم"), ("تحديث الطلب", "خدمة العملاء"), ("موعد التسليم", "إدارة المشاريع")]),
        ("الأسبوع الماضي", [("تقرير الأداء", "قسم المالية"), ("دعوة إلى ورشة", "الموارد البشرية"), ("تغيير كلمة المرور", "فريق الدعم")]),
    ];

    public static string StickyExample { get; } =
        "<Border Classes=\"panel\">\n  <ListBox Classes=\"master ruled sticky-headings\" Height=\"280\" SelectedIndex=\"1\" AutomationProperties.Name=\"الرسائل\">\n" +
        string.Concat(Mail.Select(g =>
            $"    <ListBoxItem ins:ListRow.IsHeading=\"True\" Content=\"{g.Head}\" />\n" +
            string.Concat(g.Rows.Select(r =>
                "    <ListBoxItem>\n      <StackPanel Classes=\"list-text\">\n" +
                $"        <TextBlock Classes=\"list-title\" Text=\"{r.Title}\" />\n        <TextBlock Classes=\"list-desc\" Text=\"{r.From}\" />\n" +
                "      </StackPanel>\n    </ListBoxItem>\n")))) +
        "  </ListBox>\n</Border>";

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
            Switch("heads", "عناوين المجموعات", "ins:ListRow.IsHeading", "على `ListBoxItem`: عنوان صغير بلون العلامة يجمع ما بعده من صفوف، لا يُختار ولا يقف عنده التنقل بالأسهم، ولا فاصل بينه وبين أول صف بعده. ومع البيانات اربطه في `ItemContainerTheme`.", OptionKind.Property),
            Switch("state", "حالة الصفوف", "ins:ListRow.State", "على `ListBoxItem`: `Past` يخفت صفًا مضى، و`Current` يضع شريطًا بلون العلامة في بداية الصف الحالي.", OptionKind.Property),
            Switch("ruled", "فواصل ثابتة", "ruled", "على `ListBox`: لا تختفي الفواصل تحت المؤشر ولا حول الصف المختار، ويبقى بين الخلفية والفاصل فراغ صغير."),
            Switch("compact", "صفوف مضغوطة", "compact", "على `ListBox`: صفوف أقصر بلا فواصل، لقائمة طويلة في بطاقة منبثقة أو حوار."),
            Switch("link", "صفوف تُنقر", null, null, group: "صفوف تُنقر"),
            Switch("current", "الصف الحالي", "SelectedIndex", "يميز الصف المختار بخلفية ولون العلامة على عنوانه.", OptionKind.Property, "صفوف تُنقر"),
            Switch("master", "بجانب التفاصيل", "master", "على `ListBox` بجانب لوح التفاصيل: لا سهم في الصفوف، ويبقى المختار ظاهرًا بلون العلامة وشريط في بدايته بخلاف مرور المؤشر، ويطير الشريط إلى الصف الذي تختاره.", group: "صفوف تُنقر"),
        ],
        Extra =
        [
            Row("الأساس", "ListBox", "صفوف متتالية بينها فواصل، كل صف `ListBoxItem`، وكل ما فيه اختياري.", OptionKind.Element, true),
            Row("الأساس", "static", "على `ListBox`: صفوف تُقرأ فحسب، لا تُنقر ولا يقف عندها التنقل بلوحة المفاتيح. فإن حذفته صار كل صف قابلًا للنقر، وظهر في آخره سهم يشير إلى وجهته.", OptionKind.Class, true),
            Row("الأساس", "list-text", "على `StackPanel` يضم نصي الصف، ويأخذ ما بقي من عرضه.", OptionKind.Class),
            Row("الأساس", "list-title", "عنوان الصف، وإذا طال قُطع بنقاط.", OptionKind.Class),
            Row("صفوف تُنقر", "SelectionChanged", "يُطلق حين يُنقر صف، والصف الجديد في `SelectedItem`.", OptionKind.Event),
        ],
        Disable = s => s.On("link") ? [] : ["current", "master"],
        Axaml = s =>
        {
            var pad = s.On("panel") ? "  " : "";
            var rows = new List<string>();
            for (var i = 0; i < Rows.Length; i++)
            {
                if (s.On("heads") && i == 0) rows.Add("<ListBoxItem ins:ListRow.IsHeading=\"True\" Content=\"الأساسيات\" />");
                if (s.On("heads") && i == 2) rows.Add("<ListBoxItem ins:ListRow.IsHeading=\"True\" Content=\"الحماية\" />");
                var r = Rows[i];
                var text = $"<StackPanel Classes=\"list-text\">\n  <TextBlock Classes=\"list-title\" Text=\"{r.Title}\" />" +
                    (s.On("desc") ? $"\n  <TextBlock Classes=\"list-desc\" Text=\"{r.Desc}\" />" : "") + "\n</StackPanel>";
                var parts = new List<string>();
                if (s.On("start")) parts.Add($"<Border Classes=\"panel-ico\" DockPanel.Dock=\"Left\"><ins:Icon Data=\"{{StaticResource {r.Icon}}}\" /></Border>");
                if (s.On("end")) parts.Add($"<TextBlock Classes=\"list-end\" DockPanel.Dock=\"Right\" Text=\"{r.End}\" />");
                var inner = parts.Count == 0 ? text : "<DockPanel>\n" + Indent(string.Join("\n", parts.Append(text)), "  ") + "\n</DockPanel>";
                var state = s.On("state") && i < 2 ? $" ins:ListRow.State=\"{(i == 0 ? "Past" : "Current")}\"" : "";
                rows.Add($"<ListBoxItem{state}>\n" + Indent(inner, "  ") + "\n</ListBoxItem>");
            }
            var first = s.On("heads") ? 1 : 0;
            var ruled = s.On("ruled") ? "ruled" : null;
            var compact = s.On("compact") ? "compact" : null;
            var head = s.On("link")
                ? "<ListBox" + Classes(s.On("master") ? "master" : null, ruled, compact) + (s.On("current") ? $" SelectedIndex=\"{first}\"" : "") + " AutomationProperties.Name=\"الإعدادات\">"
                : "<ListBox" + Classes("static", ruled, compact) + ">";
            var list = head + "\n" + Indent(string.Join("\n", rows), "  ") + "\n</ListBox>";
            return s.On("panel") ? "<Border Classes=\"panel\">\n" + Indent(list, pad) + "\n</Border>" : list;
        },
        Notes = s => s.On("link") ? [s.On("master") ? "اختر صفًا آخر فيطير الشريط إليه." : "انقر صفًا فيصير هو المحدد، ويظهر هنا الحدث الذي يطلقه."] : [],
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
