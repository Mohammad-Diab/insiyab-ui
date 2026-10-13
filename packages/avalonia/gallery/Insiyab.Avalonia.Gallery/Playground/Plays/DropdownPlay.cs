using Avalonia.Controls;
using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class DropdownPlay
{
    public const string Example =
        """
        <Button Classes="secondary" Content="إجراءات">
          <Button.Flyout>
            <ins:PopMenu>
              <MenuItem Classes="label" Header="العنوان" />
              <MenuItem Header="تعديل" InputGesture="E">
                <MenuItem.Icon><ins:Icon Data="{StaticResource IconEdit}" /></MenuItem.Icon>
              </MenuItem>
              <MenuItem Header="نسخ" />
              <MenuItem Header="نقل" IsEnabled="False" />
              <Separator />
              <MenuItem Classes="bad" Header="حذف" />
            </ins:PopMenu>
          </Button.Flyout>
        </Button>
        """;

    public static PlayDef Pop { get; } = new()
    {
        Id = "pop",
        Title = "القائمة المنسدلة",
        Controls =
        [
            Seg("align", "المحاذاة", "",
                Choice("", "من البداية"),
                Choice("end", "من النهاية", "IsEnd=\"True\"", "تحاذي القائمة زرها من نهايته، لزر في آخر الشريط. وتنقلب وحدها إذا كادت تخرج من النافذة.", OptionKind.Property)),
            Seg("body", "المحتوى", "",
                Choice("", "عناصر"),
                Choice("card", "بطاقة", "ins:PopCard", "تضع فيها ما شئت من محتوى بدل العناصر، ويبقى لها سلوك القائمة كله.", OptionKind.Element)),
            Switch("label", "عنوان مجموعة", "label", "على `MenuItem`: عنوان صغير فوق مجموعة من العناصر، لا يُختار.", group: "العناصر"),
            Seg("check", "قابلة للتحديد", "",
                Choice("", "لا"),
                Choice("CheckBox", "متعدد", "ToggleType=\"CheckBox\"", "عنصر يُحدد ويُلغى تحديده، وتبقى القائمة مفتوحة ما دمت تحدد فيها.", OptionKind.Property),
                Choice("Radio", "واحد", "ToggleType=\"Radio\"", "لا يُحدد إلا عنصر واحد من العناصر التي تشترك في `GroupName`.", OptionKind.Property)).In("عناصر قابلة للتحديد"),
            Switch("kbd", "اختصار", "InputGesture", "اختصار العنصر، يظهر في آخره.", OptionKind.Property, "العناصر"),
            Switch("disabled", "عنصر معطل", "IsEnabled=\"False\"", "يعطل العنصر فتتخطاه الأسهم.", OptionKind.Property, "العناصر"),
            Switch("sep", "فاصل", "Separator", "خط يفصل بين مجموعتين.", OptionKind.Element, "العناصر"),
            Switch("bad", "إجراء خطير", "bad", "على `MenuItem`: لعنصر يحذف أو لا رجعة فيه.", group: "العناصر"),
            Switch("split", "زر مقسوم", "btn-group", "ضع زر القائمة في `StackPanel` بالكلاس `btn-group` بجانب زر، فيكون الإجراء الأساسي بضغطة واحدة، وتبقى بدائله في القائمة.", group: "الزر المقسوم"),
        ],
        Extra =
        [
            Row("الأساس", "ins:PopMenu", "في `Button.Flyout`: قائمة تنفتح تحت زرها، يغلقها `Esc` والنقر خارجها، وتتنقل الأسهم بين عناصرها.", OptionKind.Element, true),
            Row("الأساس", "MenuItem", "عنصر القائمة، يغلقها إذا اخترته ويعيد التركيز إلى زرها. ونصه في `Header`، وأيقونته في `Icon`.", OptionKind.Element),
            Row("الأساس", "Click", "يُطلق على العنصر حين يُختار، ومنه العنصر القابل للتحديد، وحالته الجديدة في `IsChecked`.", OptionKind.Event),
            Row("المحتوى", "pop-title", "على `TextBlock`: عنوان محتوى البطاقة."),
            Row("المحتوى", "pop-text", "على `TextBlock`: نص البطاقة."),
            Row("عناصر قابلة للتحديد", "IsChecked=\"True\"", "العنصر محدد.", OptionKind.Property),
        ],
        Disable = s =>
        {
            if (s["body"] == "card") return ["label", "kbd", "disabled", "sep", "bad", "check"];
            return s.Has("check") ? ["kbd"] : [];
        },
        Axaml = s =>
        {
            var p = s.On("split") ? "      " : "    ";
            var end = s.Has("align") ? " IsEnd=\"True\"" : "";
            string flyout;
            if (s["body"] == "card")
                flyout = $"<ins:PopCard{end}>\n" +
                    $"{p}  <StackPanel>\n" +
                    $"{p}    <TextBlock Classes=\"pop-title\" Text=\"العنوان\" />\n" +
                    $"{p}    <TextBlock Classes=\"pop-text\" Text=\"نص حر قصير.\" />\n" +
                    $"{p}  </StackPanel>\n" +
                    $"{p}</ins:PopCard>";
            else
            {
                var lines = new List<string>();
                if (s.On("label")) lines.Add("<MenuItem Classes=\"label\" Header=\"العنوان\" />");
                var words = s.Has("check") ? new[] { "الاسم", "التاريخ", "الحجم" } : ["تعديل", "نسخ", "نقل"];
                for (var i = 0; i < 3; i++)
                {
                    var off = s.On("disabled") && i == 2 ? " IsEnabled=\"False\"" : "";
                    if (s.Has("check"))
                        lines.Add($"<MenuItem Header=\"{words[i]}\" ToggleType=\"{s["check"]}\"{(s["check"] == "Radio" ? " GroupName=\"sort\"" : "")}{(i == 0 ? " IsChecked=\"True\"" : "")}{off} />");
                    else
                        lines.Add($"<MenuItem Header=\"{words[i]}\"{(s.On("kbd") && i == 0 ? " InputGesture=\"E\"" : "")}{off} />");
                }
                if (s.On("sep")) lines.Add("<Separator />");
                if (s.On("bad")) lines.Add("<MenuItem Classes=\"bad\" Header=\"حذف\" />");
                flyout = $"<ins:PopMenu{end}>\n" + string.Join("\n", lines.Select(l => p + "  " + l)) + $"\n{p}</ins:PopMenu>";
            }
            if (!s.On("split"))
                return $"<Button Classes=\"secondary\" Content=\"{(s["body"] == "card" ? "التفاصيل" : "إجراءات")}\">\n" +
                    "  <Button.Flyout>\n" +
                    $"    {flyout}\n" +
                    "  </Button.Flyout>\n" +
                    "</Button>";
            return "<StackPanel Classes=\"btn-group\" AutomationProperties.Name=\"الحفظ\">\n" +
                "  <Button Classes=\"primary\" Content=\"حفظ\" />\n" +
                "  <Button Classes=\"primary icon\" AutomationProperties.Name=\"خيارات الحفظ\">\n" +
                "    <ins:Icon Data=\"{StaticResource IconChevDown}\" />\n" +
                "    <Button.Flyout>\n" +
                $"      {flyout}\n" +
                "    </Button.Flyout>\n" +
                "  </Button>\n" +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var said = new List<string> { "اضغط الزر لترى القائمة بخياراتك." };
            if (s["body"] == "card") said.Add("البطاقة لا عناصر فيها، فخيارات العناصر للقائمة وحدها.");
            else if (s["check"] == "CheckBox") said.Add("حدد عنصرًا فتبقى القائمة مفتوحة للتالي.");
            else if (s["check"] == "Radio") said.Add("اختر عنصرًا فينتقل التحديد إليه وتُغلق القائمة.");
            if (s.On("split")) said.Add("زر القائمة أيقونة وحدها، فلا بد له من `AutomationProperties.Name`.");
            return said;
        },
        Stage = (view, _, say) =>
        {
            foreach (var b in view.GetLogicalDescendants().OfType<Button>())
                if (b.Flyout is MenuFlyout menu)
                    foreach (var item in menu.Items.OfType<MenuItem>())
                    {
                        if (item.ToggleType != MenuItemToggleType.None)
                            item.PropertyChanged += (_, e) => { if (e.Property == MenuItem.IsCheckedProperty) say($"تغير `IsChecked` على «{item.Header}»، و`IsChecked` الآن `{item.IsChecked}`"); };
                        else
                            item.Click += (_, _) => say($"حدث `Click` على «{item.Header}»");
                    }
        },
    };

    public static PlayDef[] All { get; } = [Pop];
}
