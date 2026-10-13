using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ToolbarPlay
{
    public const string Example =
        """
        <ins:Toolbar>
          <ins:ToolbarButton Icon="{StaticResource IconPlus}" Label="طلب جديد" ShowLabel="True" Shortcut="Ctrl+N" />
          <ins:ToolbarButton Icon="{StaticResource IconFolder}" Label="فتح" Shortcut="Ctrl+O" />
          <ins:ToolbarButton Icon="{StaticResource IconDownload}" Label="تنزيل" />
          <Separator />
          <ins:ToolbarToggleButton Icon="{StaticResource IconStar}" Label="مميّز" />
          <ins:ToolbarButton Icon="{StaticResource IconShare}" Label="مشاركة">
            <ins:ToolbarButton.Flyout>
              <ins:PopMenu>
                <MenuItem Header="بالبريد" />
                <MenuItem Header="نسخ الرابط" />
              </ins:PopMenu>
            </ins:ToolbarButton.Flyout>
          </ins:ToolbarButton>
          <ins:ToolbarSplitButton Icon="{StaticResource IconUpload}" Label="تصدير" ShowLabel="True">
            <ins:ToolbarSplitButton.Flyout>
              <ins:PopMenu>
                <MenuItem Header="ملف PDF" />
                <MenuItem Header="جدول Excel" />
              </ins:PopMenu>
            </ins:ToolbarSplitButton.Flyout>
          </ins:ToolbarSplitButton>
          <Separator />
          <ins:ToolbarButton Icon="{StaticResource IconTrash}" Label="حذف" Shortcut="Delete" />
        </ins:Toolbar>
        """;

    public static PlayDef Toolbar { get; } = new()
    {
        Id = "toolbar",
        Title = "شريط الأدوات",
        Wide = _ => true,
        Controls =
        [
            Seg("look", "المظهر", "",
                Choice("", "زجاج"),
                Choice("plain", "بلا إطار", "plain", "بلا زجاج ولا حد، يلتصق بالنافذة وتحته خط رفيع، كشريط أوامر تحت شريط العنوان.")),
            Seg("width", "العرض", "",
                Choice("", "كامل"),
                Choice("360", "ضيق"),
                Choice("220", "أضيق")),
            Switch("labels", "الأسماء بجانب الرموز", "ShowLabel=\"True\"", "على الأمر: يظهر اسمه بجانب رمزه. وبدونه يكون الاسم في التلميح وحده. والأمر الذي لا رمز له يظهر اسمه دائمًا.", OptionKind.Property, "الأمر"),
            Switch("search", "حقل بحث", null, null),
            Switch("always", "الحذف في القائمة وحدها", "ins:Toolbar.Overflow=\"Always\"", "على الأمر: لا يظهر في الصف أبدًا، بل في قائمة «المزيد» وحدها.", OptionKind.Property, "الانتقال إلى «المزيد»"),
            Switch("disabled", "تعطيل الحذف", "IsEnabled=\"False\"", "الأمر معطل: يبهت في الصف وفي القائمة، وتتخطاه الأسهم.", OptionKind.Property, "الأمر"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Toolbar", "صف من الأوامر بارتفاع مضغوط. فإذا ضاق عنها نقل ما لا يتسع له، من آخره، إلى زر «المزيد» الذي يفتح قائمة بالأوامر نفسها وأسمائها ظاهرة. وهو محطة واحدة لمفتاح Tab، والأسهم تتنقل بين أوامره.", OptionKind.Element, true),
            Row("الأساس", "ins:ToolbarButton", "أمر برمز. ومع `Flyout` يصير قائمة منسدلة بسهم صغير، وفي «المزيد» قائمة فرعية.", OptionKind.Element),
            Row("الأساس", "ins:ToolbarToggleButton", "أمر يبقى مفعّلًا أو لا، مثل «مميّز»، وفي «المزيد» عنصر بعلامة اختيار.", OptionKind.Element),
            Row("الأساس", "ins:ToolbarSplitButton", "أمر رئيسي ومعه سهم يفتح خيارات أخرى في `Flyout`. وفي «المزيد» قائمة فرعية أولها الأمر الرئيسي.", OptionKind.Element),
            Row("الأساس", "Separator", "خط عمودي يفصل بين مجموعات الأوامر، ولا يبدأ به الصف ولا ينتهي.", OptionKind.Element),
            Row("الأمر", "Icon", "رمز الأمر، مثل `{StaticResource IconPlus}`.", OptionKind.Property),
            Row("الأمر", "Label", "اسم الأمر: في التلميح، وبجانب الرمز مع `ShowLabel`، وفي قائمة «المزيد»، ولقارئ الشاشة.", OptionKind.Property),
            Row("الأمر", "Shortcut", "اختصار يظهر في التلميح وفي قائمة «المزيد»، مثل `Ctrl+S`. وبدونه يظهر `HotKey`، وهو الذي يربط المفتاح فعلًا.", OptionKind.Property),
            Row("الأمر", "Command", "يعمل كما في أي زر، من الصف ومن قائمة «المزيد».", OptionKind.Property),
            Row("الأمر", "Click", "يُطلق حين يُنقر الأمر في الصف أو يُختار من قائمة «المزيد».", OptionKind.Event),
            Row("الانتقال إلى «المزيد»", "ins:Toolbar.Overflow=\"Never\"", "على الأمر: يبقى في الصف مهما ضاق. والحقول وما ليس زرًا تبقى في الصف دائمًا.", OptionKind.Property),
            Row("الانتقال إلى «المزيد»", "MoreText", "اسم زر «المزيد» وتلميحه، «المزيد» إن لم تضعه.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var labels = s.On("labels") ? " ShowLabel=\"True\"" : "";
            var width = s.Has("width") ? $" Width=\"{s["width"]}\"" : "";
            var del = (s.On("always") ? " ins:Toolbar.Overflow=\"Always\"" : "") + (s.On("disabled") ? " IsEnabled=\"False\"" : "");
            var lines = new List<string>
            {
                $"<ins:ToolbarButton Icon=\"{{StaticResource IconPlus}}\" Label=\"طلب جديد\"{labels} Shortcut=\"Ctrl+N\" />",
                $"<ins:ToolbarButton Icon=\"{{StaticResource IconFolder}}\" Label=\"فتح\"{labels} Shortcut=\"Ctrl+O\" />",
                $"<ins:ToolbarButton Icon=\"{{StaticResource IconCopy}}\" Label=\"نسخ\"{labels} />",
                "<Separator />",
                $"<ins:ToolbarToggleButton Icon=\"{{StaticResource IconStar}}\" Label=\"مميّز\"{labels} />",
                $"<ins:ToolbarButton Icon=\"{{StaticResource IconShare}}\" Label=\"مشاركة\"{labels}>",
                "  <ins:ToolbarButton.Flyout>",
                "    <ins:PopMenu>",
                "      <MenuItem Header=\"بالبريد\" />",
                "      <MenuItem Header=\"نسخ الرابط\" />",
                "    </ins:PopMenu>",
                "  </ins:ToolbarButton.Flyout>",
                "</ins:ToolbarButton>",
                "<Separator />",
                $"<ins:ToolbarButton Icon=\"{{StaticResource IconTrash}}\" Label=\"حذف\"{labels} Shortcut=\"Delete\"{del} />",
            };
            if (s.On("search")) lines.Add("<TextBox Classes=\"sm\" Width=\"150\" PlaceholderText=\"ابحث في الطلبات\" />");
            return $"<ins:Toolbar{Classes(s["look"])}{width}>\n" + string.Join("\n", lines.Select(l => "  " + l)) + "\n</ins:Toolbar>";
        },
        Notes = s =>
        {
            if (s.Has("width")) return ["ضاق الشريط، فانتقل ما لا يتسع له إلى زر «المزيد» في آخره. افتحه لترى الأوامر بأسمائها."];
            return s.On("always") ? ["الحذف في قائمة «المزيد» وحدها."] : [];
        },
        Stage = (view, _, say) =>
        {
            foreach (var b in view.GetLogicalDescendants().OfType<Button>())
            {
                if (b is ToggleButton t) t.IsCheckedChanged += (_, _) => say($"حدث `IsCheckedChanged`، و`IsChecked` الآن `{t.IsChecked}`");
                else if (b is ToolbarButton { Flyout: null } tb) tb.Click += (_, _) => say($"حدث `Click` على «{tb.Label}»");
            }
        },
    };

    public static PlayDef[] All { get; } = [Toolbar];
}
