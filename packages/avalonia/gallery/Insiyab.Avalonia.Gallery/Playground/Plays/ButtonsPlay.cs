using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ButtonsPlay
{
    public const string Example =
        """
        <Button Classes="primary">
          <StackPanel Orientation="Horizontal" Spacing="8">
            <ins:Icon Data="{StaticResource IconCheck}" />
            <TextBlock Text="حفظ" />
          </StackPanel>
        </Button>
        <Button Classes="secondary" Content="معاينة" />
        <Button Classes="ghost" Content="تصدير" />
        <Button Classes="danger" Content="حذف" />
        <Button Classes="bare" Content="إلغاء" />
        """;

    public static PlayDef Button { get; } = new()
    {
        Id = "button",
        Title = "الزر",
        WideMax = 416,
        Controls =
        [
            Select("look", "النمط", "primary",
                Choice("primary", "رئيسي", "primary", "للإجراء الرئيسي في الشاشة."),
                Choice("secondary", "ثانوي", "secondary", "لما يجاور الزر الرئيسي، وهو شكل الزر الذي لا كلاس له."),
                Choice("success", "نجاح", "success", "لإجراء يؤكد أو يقبل."),
                Choice("danger", "خطر", "danger", "لإجراء يحذف، أو لا رجعة فيه."),
                Choice("warning", "تحذير", "warning", "لإجراء يوقف أو يؤجل."),
                Choice("info", "معلومة", "info", "لإجراء يشرح أو يفصّل."),
                Choice("ghost", "هادئ", "ghost", "أهدأ من الثانوي، لإجراء لا يلفت النظر."),
                Choice("ghost-success", "هادئ بنبرة النجاح", "ghost-success", "هادئ بلون النجاح."),
                Choice("ghost-danger", "هادئ بنبرة الخطر", "ghost-danger", "هادئ بلون الخطر."),
                Choice("ghost-warning", "هادئ بنبرة التحذير", "ghost-warning", "هادئ بلون التحذير."),
                Choice("ghost-info", "هادئ بنبرة المعلومة", "ghost-info", "هادئ بلون المعلومة."),
                Choice("bare", "بلا خلفية", "bare", "لإجراء يكاد يكون رابطًا، كالإلغاء.")),
            Seg("size", "المقاس", "",
                Choice("sm", "صغير", "sm", "زر أصغر، للجداول والأشرطة."),
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "زر أكبر، للإجراء الوحيد في الشاشة.")),
            Seg("icon", "الأيقونة", "",
                Choice("", "بلا"),
                Choice("start", "قبل النص"),
                Choice("only", "وحدها", "icon", "زر مربع يسع أيقونة واحدة، ولا بد له عندئذ من `AutomationProperties.Name`.")),
            Text("text", "النص", "حفظ"),
            Switch("full", "بعرض الحاوية", "full", "يمد الزر على عرض حاويته كله."),
            Switch("lift", "يرتفع تحت المؤشر", "lift", "يرفع الزر قليلًا إذا مر عليه المؤشر."),
            Switch("link", "رابط بهيئة زر", "HyperlinkButton", "رابط في هيئة زر: ضع عليه السمة `InsLinkButton` وعنوانه في `NavigateUri`، فيفتحه إذا نُقر.", OptionKind.Element),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يعطل الزر، فلا يُنقر ولا يصل إليه التنقل بلوحة المفاتيح.", OptionKind.Property),
            Switch("toggle", "زر تبديل", "ToggleButton", "زر يبقى مضغوطًا أو غير مضغوط، ويقلب حالته عند كل ضغطة.", OptionKind.Element, "زر تبديل"),
        ],
        Extra =
        [
            Row("الأساس", "Button", "أساس كل زر، ومعه كلاس للنمط، فإن لم تذكره بدا ثانويًا.", OptionKind.Element, true),
            Row("زر تبديل", "IsChecked=\"True\"", "الزر مضغوط، فيأخذ لون نمطه، وهو رمادي ما دام غير مضغوط. ضعها بنفسك ليبدأ الزر مضغوطًا.", OptionKind.Property),
            Row("زر تبديل", "IsCheckedChanged", "يُطلق عند كل ضغطة، وحالة الزر الجديدة في `IsChecked`.", OptionKind.Event),
        ],
        Disable = s => s.On("link") ? ["toggle"] : [],
        Wide = s => s.On("full"),
        Axaml = s =>
        {
            var tag = s.On("link") ? "HyperlinkButton" : s.On("toggle") ? "ToggleButton" : "Button";
            var attrs = (s.On("link") ? " Theme=\"{StaticResource InsLinkButton}\"" : "") +
                Classes(s["look"], s["size"], s["icon"] == "only" ? "icon" : null, s.On("full") ? "full" : null, s.On("lift") ? "lift" : null) +
                (s.On("link") ? " NavigateUri=\"https://example.com/orders\"" : "") +
                (s.On("disabled") ? " IsEnabled=\"False\"" : "") +
                (s.On("toggle") && !s.On("link") ? " IsChecked=\"True\"" : "");
            const string icon = "<ins:Icon Data=\"{StaticResource IconCheck}\" />";
            return s["icon"] switch
            {
                "only" => $"<{tag}{attrs} AutomationProperties.Name=\"{s["text"]}\">\n  {icon}\n</{tag}>",
                "start" => $"<{tag}{attrs}>\n  <StackPanel Orientation=\"Horizontal\" Spacing=\"8\">\n    {icon}\n    <TextBlock Text=\"{s["text"]}\" />\n  </StackPanel>\n</{tag}>",
                _ => $"<{tag}{attrs} Content=\"{s["text"]}\" />",
            };
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["icon"] == "only") notes.Add("لا نص على الزر، فصار نصه اسمًا في `AutomationProperties.Name` يقرؤه قارئ الشاشة.");
            if (s.On("lift")) notes.Add("مرر المؤشر على الزر، فيرتفع قليلًا.");
            if (s.On("link")) notes.Add("الرابط لا يكون زر تبديل، فهذا الخيار للأزرار وحدها. ولا يفتح الرابط هنا شيئًا.");
            if (s.On("toggle") && !s.On("link"))
                notes.Add("يبدو الزر هنا مضغوطًا، فاضغطه يرجع رماديًا ويظهر هنا الحدث الذي يطلقه.");
            return notes;
        },
        Stage = (view, _, say) =>
        {
            foreach (var t in view.GetLogicalDescendants().OfType<ToggleButton>())
                t.IsCheckedChanged += (_, _) => say($"حدث `IsCheckedChanged`، و`IsChecked` الآن `{t.IsChecked}`");
        },
    };

    public static PlayDef Group { get; } = new()
    {
        Id = "group",
        Title = "مجموعة الأزرار",
        Controls =
        [
            Seg("size", "المقاس", "",
                Choice("", "عادي"),
                Choice("sm", "صغير", "sm", "لمجموعة من أزرار `sm`: يضبط انحناء طرفيها على مقاس أزرارها.")),
            Seg("look", "نمط الأزرار", "secondary",
                Choice("secondary", "ثانوي"),
                Choice("ghost", "هادئ")),
        ],
        Extra =
        [
            Row("الأساس", "btn-group", "على `StackPanel`، يضم الأزرار المتجاورة في قطعة واحدة.", OptionKind.Class, true),
            Row("الأساس", "AutomationProperties.Name", "يقول لقارئ الشاشة ما الذي تجمعه الأزرار.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var btn = Classes(s["look"], s["size"]);
            return $"<StackPanel{Classes("btn-group", s["size"])} AutomationProperties.Name=\"التنقل بين الأيام\">\n" +
                $"  <Button{btn} Content=\"السابق\" />\n" +
                $"  <Button{btn} Content=\"اليوم\" />\n" +
                $"  <Button{btn} Content=\"التالي\" />\n" +
                "</StackPanel>";
        },
        Notes = s => s.Has("size") ? ["يصغّر `sm` على المجموعة انحناء طرفيها فقط، فضعه على أزرارها أيضًا."] : [],
    };

    public static PlayDef Close { get; } = new()
    {
        Id = "close",
        Title = "زر الإغلاق",
        WideMax = 416,
        Controls =
        [
            Seg("size", "المقاس", "",
                Choice("", "عادي"),
                Choice("sm", "صغير", "sm", "زر أصغر، لتنبيه أو رقاقة.")),
            Switch("bare", "بلا خلفية", "bare", "يزيل الخلفية، لزر يسكن داخل تنبيه أو لوح."),
            Switch("dismiss", "يغلق ما يحتويه", "ins:Dismiss.Nearest=\"True\"", "يغلق أقرب تنبيه أو نافذة أو قائمة منبثقة يسكن فيها، أو ما تسميه له في `ins:Dismiss.Target`.", OptionKind.Property, "يغلق ما يحتويه"),
        ],
        Extra =
        [
            Row("الأساس", "close", "على `Button`: يرسم علامة الإغلاق بنفسه، فلا يحتاج أيقونة، ولا بد له من `AutomationProperties.Name`.", OptionKind.Class, true),
            Row("يغلق ما يحتويه", "ins:Dismiss.Dismissed", "يُطلق على ما أُغلق، ويصعد منه إلى ما فوقه.", OptionKind.Event),
        ],
        Wide = s => s.On("dismiss"),
        Axaml = s =>
        {
            var cls = Classes("close", s["size"], s.On("bare") ? "bare" : null);
            if (!s.On("dismiss")) return $"<Button{cls} AutomationProperties.Name=\"إغلاق\" />";
            return "<ins:Alert Classes=\"info\">\n" +
                "  <DockPanel>\n" +
                $"    <Button{cls} DockPanel.Dock=\"Right\" ins:Dismiss.Nearest=\"True\" AutomationProperties.Name=\"إغلاق التنبيه\" />\n" +
                "    <TextBlock Text=\"تنبيه يغلقه الزر.\" />\n" +
                "  </DockPanel>\n" +
                "</ins:Alert>";
        },
        Notes = s => s.On("dismiss") ? ["اضغط الزر فيُغلق التنبيه ويظهر هنا الحدث، وغيّر أي خيار لتعيده."] : [],
        Stage = (view, s, say) =>
        {
            if (s.On("dismiss")) view.AddHandler(Dismiss.DismissedEvent, (_, _) => say("حدث `ins:Dismiss.Dismissed` على التنبيه"));
        },
    };

    public static PlayDef Split { get; } = new()
    {
        Id = "split",
        Title = "زر بقائمة",
        WideMax = 416,
        Controls =
        [
            Seg("kind", "النوع", "split",
                Choice("split", "مقسم", "SplitButton", "زر بجزأين: الأول ينفذ الإجراء، والسهم بجانبه يفتح قائمة بإجراءات قريبة منه.", OptionKind.Element),
                Choice("toggle", "مقسم للتبديل", "ToggleSplitButton", "مثل المقسم، لكن جزأه الأول زر تبديل يبقى مضغوطًا أو غير مضغوط، وهو رمادي ما دام غير مضغوط.", OptionKind.Element),
                Choice("drop", "منسدل", "DropDownButton", "زر واحد يفتح قائمته عند الضغط، وفي طرفه سهم يدل على ذلك.", OptionKind.Element)),
            Select("look", "النمط", "primary",
                Choice("primary", "رئيسي"),
                Choice("secondary", "ثانوي"),
                Choice("success", "نجاح"),
                Choice("danger", "خطر"),
                Choice("warning", "تحذير"),
                Choice("info", "معلومة"),
                Choice("ghost", "هادئ"),
                Choice("ghost-danger", "هادئ بنبرة الخطر"),
                Choice("bare", "بلا خلفية")),
            Seg("size", "المقاس", "",
                Choice("sm", "صغير"),
                Choice("", "عادي"),
                Choice("lg", "كبير")),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يعطل الزر كله، بجزأيه إن كان مقسمًا.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "Flyout", "القائمة التي يفتحها السهم. اجعلها `ins:PopMenu` لتأخذ هيئة قوائم المكتبة وموضعها تحت الزر.", OptionKind.Property, true),
            Row("الأساس", "Click", "يُطلق عند الضغط على الجزء الأول من الزر المقسم، لا على سهمه.", OptionKind.Event),
            Row("الأساس", "IsChecked", "حالة الزر المقسم للتبديل: مضغوط أو غير مضغوط.", OptionKind.Property),
            Row("الأساس", "IsCheckedChanged", "يُطلق عند كل ضغطة على الجزء الأول من الزر المقسم للتبديل.", OptionKind.Event),
            Row("النمط", "Classes", "كلاسات الزر نفسها: النمط من `primary` إلى `bare`، والمقاس `sm` و`lg`، و`full` ليمتد على عرض حاويته. والزر المقسم للتبديل رمادي ما دام غير مضغوط، أيًا كان نمطه.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var (tag, text, items) = s["kind"] switch
            {
                "toggle" => ("ToggleSplitButton", "التدقيق", new[] { "العربية", "الإنجليزية", "اللغتان معًا" }),
                "drop" => ("DropDownButton", "ترتيب", new[] { "الأحدث", "الأقدم", "الاسم" }),
                _ => ("SplitButton", "حفظ", new[] { "حفظ باسم جديد", "حفظ نسخة", "حفظ الكل" }),
            };
            return $"<{tag}{Classes(s["look"], s["size"])} Content=\"{text}\"" + (s.On("disabled") ? " IsEnabled=\"False\"" : "") + (s["kind"] == "toggle" ? " IsChecked=\"True\"" : "") + ">\n" +
                $"  <{tag}.Flyout>\n" +
                "    <ins:PopMenu>\n" +
                string.Concat(items.Select(i => $"      <MenuItem Header=\"{i}\" />\n")) +
                "    </ins:PopMenu>\n" +
                $"  </{tag}.Flyout>\n" +
                $"</{tag}>";
        },
        Notes = s => s["kind"] switch
        {
            "toggle" => ["يبدو الزر هنا مضغوطًا، فاضغط جزأه الأول يرجع رماديًا ويظهر هنا الحدث، واضغط السهم تفتح القائمة."],
            "drop" => ["اضغط الزر فتفتح قائمته، ويقلب السهم ما دامت مفتوحة."],
            _ => ["اضغط الجزء الأول فيظهر هنا الحدث، واضغط السهم تفتح القائمة."],
        },
        Stage = (view, _, say) =>
        {
            foreach (var t in view.GetLogicalDescendants().OfType<ToggleSplitButton>())
                t.IsCheckedChanged += (_, _) => say($"حدث `IsCheckedChanged`، و`IsChecked` الآن `{t.IsChecked}`");
            foreach (var b in view.GetLogicalDescendants().OfType<SplitButton>().Where(b => b is not ToggleSplitButton))
                b.Click += (_, _) => say("حدث `Click` على الجزء الأول");
            foreach (var m in view.GetLogicalDescendants().OfType<Control>().Where(c => c is SplitButton or DropDownButton))
            {
                var flyout = m is SplitButton sb ? sb.Flyout : ((DropDownButton)m).Flyout;
                if (flyout is MenuFlyout menu)
                    foreach (var item in menu.Items.OfType<MenuItem>())
                        item.Click += (_, _) => say($"اختير «{item.Header}» من القائمة");
            }
        },
    };

    public static PlayDef[] All { get; } = [Button, Group, Close, Split];
}
