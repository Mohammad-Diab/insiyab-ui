using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class TablesPlay
{
    public const string Example =
        """
        <ins:Table>
          <ins:Table.Columns>
            <ins:TableColumn Header="الاسم" />
            <ins:TableColumn Header="المدينة" />
            <ins:TableColumn Header="الحالة" />
          </ins:Table.Columns>
          <ins:TableRow><ins:TableCell>ليلى</ins:TableCell><ins:TableCell>الرياض</ins:TableCell><ins:TableCell>نشط</ins:TableCell></ins:TableRow>
          <ins:TableRow IsSelected="True"><ins:TableCell>عمر</ins:TableCell><ins:TableCell>عمّان</ins:TableCell><ins:TableCell>نشط</ins:TableCell></ins:TableRow>
          <ins:TableRow><ins:TableCell>سارة</ins:TableCell><ins:TableCell>القاهرة</ins:TableCell><ins:TableCell>موقوف</ins:TableCell></ins:TableRow>
          <ins:TableRow><ins:TableCell>كريم</ins:TableCell><ins:TableCell>دبي</ins:TableCell><ins:TableCell>نشط</ins:TableCell></ins:TableRow>
        </ins:Table>
        """;

    private static string Cells(params string[] cells) => string.Concat(cells.Select(c => $"<ins:TableCell>{c}</ins:TableCell>"));

    private static string Indent(string code, int by) => string.Join("\n", code.Split('\n').Select(l => new string(' ', by) + l));

    public static PlayDef Table { get; } = new()
    {
        Id = "table",
        Title = "الجدول",
        Wide = _ => true,
        Controls =
        [
            Switch("empty", "فارغ", "Table.Empty", "ما يعرضه الجدول إذا لم تكن فيه صفوف: حالة فارغة مكان الرأس والصفوف.", OptionKind.Property),
            Switch("num", "عمود أرقام", "Classes=\"num\"", "على `ins:TableColumn`: خط Inter بأرقام متساوية العرض، فتصطف القيم وتبقى إشارة السالب في مكانها.", OptionKind.Property),
            Switch("current", "الصف المحدد", "IsSelected=\"True\"", "على `ins:TableRow`: صف اخترته من القائمة، أو السجل الذي تعرضه الشاشة. يتلون، ويظهر على حافته في جهة البداية شريط صغير.", OptionKind.Property),
            Switch("panel", "في لوح", "panel-body flush", "ضع الجدول في جسم لوح بلا فراغ داخلي، فيمتد إلى حواف اللوح وتهدأ خلفية صفوفه."),
            Switch("actions", "إجراءات الصف", "RowContextMenu", "قائمة تفتح بالنقر بالزر الأيمن على الصف أو بمفتاح القائمة. ولك أن تضع معها زرًا في آخر الصف يفتح القائمة نفسها في `Flyout`.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Table", "رأس عائم فوق بطاقة من الصفوف، ويتلون الصف الذي تحت المؤشر. أعمدته في `Columns` وصفوفه `ins:TableRow` فيها `ins:TableCell`.", OptionKind.Element, true),
            Row("الأساس", "ItemsSource", "بدل كتابة الصفوف بيدك: صف لكل عنصر، ونص خلاياه من `CellText` على كل عمود أو قالبه من `CellTemplate`.", OptionKind.Property),
            Row("الأساس", "RowActivated", "يُطلق إذا نقر المستخدم صفًا مرتين أو ضغط عليه `Enter`، لفتح السجل.", OptionKind.Event),
        ],
        Disable = s => s.On("empty") ? ["num", "current", "actions"] : [],
        Axaml = s =>
        {
            var full = !s.On("empty");
            var num = full && s.On("num") ? " Classes=\"num\"" : "";
            var act = full && s.On("actions");
            var lines = new List<string>
            {
                "<ins:Table>",
                "  <ins:Table.Columns>",
                "    <ins:TableColumn Header=\"الاسم\" />",
                "    <ins:TableColumn Header=\"المدينة\" />",
                $"    <ins:TableColumn Header=\"القيمة\"{num} />",
            };
            if (act) lines.Add("    <ins:TableColumn Header=\"\" IsFit=\"True\" />");
            lines.Add("  </ins:Table.Columns>");
            if (!full)
                lines.AddRange([
                    "  <ins:Table.Empty>",
                    "    <StackPanel Spacing=\"10\">",
                    "      <TextBlock Classes=\"empty-title\" Text=\"لا بيانات\" />",
                    "      <TextBlock Classes=\"empty-hint\" Text=\"ستظهر الصفوف هنا.\" />",
                    "    </StackPanel>",
                    "  </ins:Table.Empty>",
                ]);
            else
            {
                if (act)
                    lines.AddRange([
                        "  <ins:Table.RowContextMenu>",
                        "    <ContextMenu>",
                        "      <MenuItem Header=\"تعديل\" />",
                        "      <MenuItem Header=\"حذف\" Classes=\"bad\" />",
                        "    </ContextMenu>",
                        "  </ins:Table.RowContextMenu>",
                    ]);
                string[][] rows = [["ليلى", "الرياض", "249.00"], ["عمر", "عمّان", "1,180.00"], ["سارة", "القاهرة", "-89.50"]];
                for (var i = 0; i < rows.Length; i++)
                {
                    var sel = i == 1 && s.On("current") ? " IsSelected=\"True\"" : "";
                    if (!act) lines.Add($"  <ins:TableRow{sel}>{Cells(rows[i])}</ins:TableRow>");
                    else
                        lines.AddRange([
                            $"  <ins:TableRow{sel}>",
                            $"    {Cells(rows[i])}",
                            "    <ins:TableCell>",
                            "      <Button Classes=\"secondary sm\" Content=\"إجراءات\">",
                            "        <Button.Flyout>",
                            "          <MenuFlyout>",
                            "            <MenuItem Header=\"تعديل\" />",
                            "            <MenuItem Header=\"حذف\" Classes=\"bad\" />",
                            "          </MenuFlyout>",
                            "        </Button.Flyout>",
                            "      </Button>",
                            "    </ins:TableCell>",
                            "  </ins:TableRow>",
                        ]);
                }
            }
            lines.Add("</ins:Table>");
            var table = string.Join("\n", lines);
            if (!s.On("panel")) return table;
            return "<Border Classes=\"panel\">\n  <Border Classes=\"panel-body flush\">\n" + Indent(table, 4) + "\n  </Border>\n</Border>";
        },
        Notes = s =>
        {
            if (s.On("empty")) return ["لا صفوف في الجدول، فحلت الحالة الفارغة مكان رأسه وصفوفه."];
            return s.On("actions") ? ["انقر صفًا بالزر الأيمن، أو اضغط زر «إجراءات» في آخره، فتفتح قائمته."] : [];
        },
    };

    public static PlayDef Wrap { get; } = new()
    {
        Id = "wrap",
        Title = "تمرير الجدول",
        Wide = _ => true,
        Controls =
        [
            Switch("sticky", "تثبيت الرأس", "sticky", "يضع الجدول الطويل في صندوق يتمرر ضمن `MaxHeight`، وهو 512 إن لم تذكره، ويبقي رأسه ظاهرًا."),
            Switch("pin", "تثبيت العمود الأول", "pin", "يبقي العمود الأول ظاهرًا حين يتمرر الجدول العريض أفقيًا. ولك أن تجمعه مع `sticky`."),
            Seg("frozen", "أعمدة ثابتة", "",
                Choice("", "بلا"),
                Choice("2", "عمودان", "FrozenColumns", "عدد الأعمدة التي تبقى في مكانها من البداية حين يتمرر الجدول أفقيًا، ومعها عمود المربعات إن وُجد. وعلى حافة آخرها ظل خفيف ما دامت الخلايا تمر تحته.", OptionKind.Property)),
        ],
        Extra =
        [
            Row("الأساس", "TableMinWidth", "أضيق ما يكون عليه الجدول، فإذا ضاقت حاويته عنه تمرر أفقيًا وبقيت الصفحة على عرضها.", OptionKind.Property, true),
            Row("الأساس", "CanResizeColumns", "يسحب المستخدم الحافة في آخر عنوان العمود فيغير عرضه، وينقرها مرتين فيضيق العمود على قدر أعرض ما فيه. وهو مفعّل إن لم تذكره.", OptionKind.Property, true),
            Row("الأساس", "MinWidth", "على `ins:TableColumn`: أضيق ما يُسحب إليه العمود، وهو 48 إن لم تذكره.", OptionKind.Property, true),
            Row("الأساس", "IsFit", "على `ins:TableColumn`: يضيق العمود على قدر محتواه، ويذهب ما بقي من العرض إلى غيره.", OptionKind.Property, true),
        ],
        Disable = s => s.Has("frozen") ? ["pin"] : [],
        Axaml = s =>
        {
            string[][] rows =
            [
                ["ليلى", "الرياض", "layla@example.com", "+966 50 000 0001", "نشط", "المبيعات", "2024-03-12", "18", "4,250", "تفضّل البريد"],
                ["عمر", "عمّان", "omar@example.com", "+962 79 000 0002", "نشط", "الدعم", "2023-11-05", "7", "1,120", "اتصال صباحًا"],
                ["سارة", "القاهرة", "sara@example.com", "+20 10 0000 0003", "موقوف", "التسويق", "2022-08-21", "0", "0", "موقوف حتى السداد"],
                ["كريم", "دبي", "karim@example.com", "+971 50 000 0004", "نشط", "المالية", "2024-01-30", "25", "9,800", "عميل دائم"],
                ["نور", "الدوحة", "nour@example.com", "+974 5000 0005", "نشط", "الدعم", "2023-06-14", "12", "2,640", "ينتظر ردًا"],
                ["يوسف", "جدة", "yousef@example.com", "+966 50 000 0006", "نشط", "المبيعات", "2024-05-02", "9", "3,175", "طلب فاتورة جديدة"],
                ["هدى", "بيروت", "huda@example.com", "+961 70 000 007", "نشط", "التسويق", "2023-02-18", "15", "5,430", "تفضّل الهاتف"],
                ["خالد", "الكويت", "khaled@example.com", "+965 5000 0008", "موقوف", "المالية", "2022-12-09", "3", "760", "موقوف بطلبه"],
                ["مريم", "مسقط", "maryam@example.com", "+968 9000 0009", "نشط", "المبيعات", "2024-07-23", "21", "6,915", "عميلة دائمة"],
                ["سامي", "المنامة", "sami@example.com", "+973 3600 0010", "نشط", "الدعم", "2023-09-11", "6", "1,380", "يحتاج متابعة"],
            ];
            int[] ltr = [2, 3, 6];
            var cls = Classes(s.On("sticky") ? "sticky" : null, s.On("pin") && !s.Has("frozen") ? "pin" : null);
            var frozen = s.Has("frozen") ? $" FrozenColumns=\"{s["frozen"]}\"" : "";
            return $"<ins:Table{cls}{frozen} TableMinWidth=\"1400\"{(s.On("sticky") ? " MaxHeight=\"256\"" : "")}>\n" +
                "  <ins:Table.Columns>\n" +
                "    <ins:TableColumn Header=\"الاسم\" /><ins:TableColumn Header=\"المدينة\" /><ins:TableColumn Header=\"البريد\" /><ins:TableColumn Header=\"الهاتف\" /><ins:TableColumn Header=\"الحالة\" />\n" +
                "    <ins:TableColumn Header=\"القسم\" /><ins:TableColumn Header=\"تاريخ الانضمام\" /><ins:TableColumn Header=\"الطلبات\" Classes=\"num\" /><ins:TableColumn Header=\"المبلغ\" Classes=\"num\" /><ins:TableColumn Header=\"ملاحظات\" />\n" +
                "  </ins:Table.Columns>\n" +
                string.Concat(rows.Select(r =>
                    "  <ins:TableRow>" + string.Concat(r.Select((v, i) => ltr.Contains(i) ? $"<ins:TableCell FlowDirection=\"LeftToRight\">{v}</ins:TableCell>" : $"<ins:TableCell>{v}</ins:TableCell>")) + "</ins:TableRow>\n")) +
                "</ins:Table>";
        },
        Notes = s =>
        {
            if (s.Has("frozen")) return ["مرر الجدول أفقيًا، فيبقى العمودان الأولان في مكانهما، ويظهر على حافة الثاني ظل خفيف فوق الخلايا التي تمر تحته."];
            if (s.On("sticky") && s.On("pin")) return ["مرر الجدول عموديًا وأفقيًا، فيبقى رأسه وعموده الأول ظاهرين."];
            if (s.On("sticky")) return ["مرر الجدول داخل صندوقه، وقد قصّرناه بـ`MaxHeight`، فيبقى رأسه ظاهرًا."];
            if (s.On("pin")) return ["مرر الجدول أفقيًا، فيبقى العمود الأول في مكانه."];
            return ["الجدول هنا أعرض من حاويته بـ`TableMinWidth`، فمرره أفقيًا."];
        },
    };

    public static PlayDef Sort { get; } = new()
    {
        Id = "sort",
        Title = "الترتيب",
        Wide = _ => true,
        Controls =
        [
            Switch("value", "قيمة للترتيب", "SortValue", "على `ins:TableCell` إذا لم يكن نصها هو قيمتها، كتاريخ مكتوب بالكلمات، فيرتبها بهذه القيمة.", OptionKind.Property, "قيمة الخلية"),
            Seg("start", "مرتب من البداية", "",
                Choice("", "لا"),
                Choice("Ascending", "تصاعديًا", "SortDirection=\"Ascending\"", "على العمود إذا جاءت الصفوف من الخادم مرتبة به تصاعديًا، فيظهر السهم في مكانه.", OptionKind.Property),
                Choice("Descending", "تنازليًا", "SortDirection=\"Descending\"", "مثلها، للصفوف المرتبة تنازليًا.", OptionKind.Property)),
        ],
        Extra =
        [
            Row("الأساس", "Sort=\"Auto\"", "على `ins:TableColumn`: يصير عنوانه زرًا يرتب الصفوف تصاعديًا ثم تنازليًا، ويدل السهم على الاتجاه. والخلية الفارغة تأتي في الآخر دائمًا. ويرتب العمود بالأرقام إذا كانت خلاياه كلها أرقامًا، وبالنص فيما سوى ذلك.", OptionKind.Property, true),
            Row("نوع القيم", "Sort=\"Number\"", "يرتب العمود بالأرقام في كل حال.", OptionKind.Property),
            Row("نوع القيم", "Sort=\"Text\"", "يرتب العمود نصًا ولو كانت خلاياه أرقامًا.", OptionKind.Property),
            Row("قيمة الخلية", "Value", "يرتب بها إذا لم تجد `SortValue`، ويبدأ بها التعديل في مكانه أيضًا.", OptionKind.Property),
            Row("الحدث", "Sorting", "يُطلق قبل الترتيب، ومعه العمود `Column` ورقمه `ColumnIndex` والاتجاه `Direction`. ألغه بـ`Cancel` إذا كان الترتيب على الخادم، فيتحرك السهم وحده. وترتب `Sort(column)` من الشيفرة.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            string[][] rows = [["عمر", "2026-01-15", "15 يناير", "1,180.00"], ["ليلى", "2026-03-02", "2 مارس", "249.00"], ["سارة", "2026-02-20", "20 فبراير", "89.50"]];
            if (s.Has("start"))
            {
                string[] order = ["سارة", "عمر", "ليلى"];
                rows = rows.OrderBy(r => Array.IndexOf(order, r[0])).ToArray();
                if (s["start"] == "Descending") rows = rows.Reverse().ToArray();
            }
            return "<ins:Table>\n" +
                "  <ins:Table.Columns>\n" +
                $"    <ins:TableColumn Header=\"الاسم\" Sort=\"Auto\"{(s.Has("start") ? $" SortDirection=\"{s["start"]}\"" : "")} />\n" +
                "    <ins:TableColumn Header=\"التاريخ\" Sort=\"Auto\" />\n" +
                "    <ins:TableColumn Header=\"القيمة\" Sort=\"Auto\" Classes=\"num\" />\n" +
                "  </ins:Table.Columns>\n" +
                string.Concat(rows.Select(r =>
                    $"  <ins:TableRow><ins:TableCell>{r[0]}</ins:TableCell><ins:TableCell{(s.On("value") ? $" SortValue=\"{r[1]}\"" : "")}>{r[2]}</ins:TableCell><ins:TableCell>{r[3]}</ins:TableCell></ins:TableRow>\n")) +
                "</ins:Table>";
        },
        Notes = s =>
        [
            s.On("value")
                ? "رتب عمود التاريخ، فيرتبه بالقيمة التي في `SortValue`، ويظهر هنا الحدث."
                : "رتب عمود التاريخ، فيرتبه بنصه فيأتي 2 مارس قبل 15 يناير، ثم شغّل «قيمة للترتيب» وقارن.",
        ],
        Stage = (view, _, say) =>
        {
            foreach (var t in view.GetLogicalDescendants().OfType<Table>())
                t.Sorting += (_, e) => say($"حدث `Sorting` على العمود `{e.ColumnIndex}`، والاتجاه `{e.Direction}`");
        },
    };

    public static PlayDef Select { get; } = new()
    {
        Id = "select",
        Title = "تحديد الصفوف",
        Wide = _ => true,
        Controls =
        [
            Switch("all", "مربع للكل", "HasSelectAll", "مربع في رأس الجدول يحدد الصفوف كلها أو يلغي تحديدها، ويصير محددًا كله أو جزئيًا بحسبها. وهو ظاهر إن لم تذكره، فاكتب `HasSelectAll=\"False\"` لتخفيه.", OptionKind.Property, on: true),
            Switch("checked", "صف محدد من البداية", "IsSelected=\"True\"", "على صف أرسله الخادم محددًا، فيتلون ويُعلَّم مربعه ويظهر الشريط من البداية.", OptionKind.Property, on: true),
            Switch("bulk", "شريط الإجراءات", "ins:BulkBar", "شريط يظهر ما دام في الجدول صف محدد، فيه عدد المحدد والإجراءات المتاحة له.", OptionKind.Element, "شريط الإجراءات", on: true),
            Switch("text", "صيغة العدد", "Text", "صيغة للعدد غير الافتراضية، و`{n}` مكان العدد.", OptionKind.Property, "شريط الإجراءات"),
        ],
        Extra =
        [
            Row("الأساس", "IsSelectable", "يضيف عمودًا من المربعات يحدد الصفوف، وإذا نقرت مع `Shift` حددت ما بين صفين.", OptionKind.Property, true),
            Row("الأساس", "SelectedRows", "الصفوف المحددة بترتيب ظهورها، و`SelectAll(false)` تلغي التحديد.", OptionKind.Property),
            Row("شريط الإجراءات", "Table", "يربط الشريط بجدوله: `Table=\"{Binding #People}\"`.", OptionKind.Property),
            Row("شريط الإجراءات", "ins:BulkBar.ClearsSelection", "على زر في الشريط يلغي التحديد كله.", OptionKind.Property),
            Row("الحدث", "SelectionChanged", "يُطلق مع كل تغيير في التحديد، ومعه الصفوف المحددة `Rows` وعددها `Count`.", OptionKind.Event),
        ],
        Disable = s => s.On("bulk") ? [] : ["text"],
        Axaml = s =>
        {
            var table = $"<ins:Table x:Name=\"People\" IsSelectable=\"True\"{(s.On("all") ? "" : " HasSelectAll=\"False\"")}>\n" +
                "  <ins:Table.Columns>\n" +
                "    <ins:TableColumn Header=\"الاسم\" />\n" +
                "    <ins:TableColumn Header=\"المدينة\" />\n" +
                "  </ins:Table.Columns>\n" +
                $"  <ins:TableRow{(s.On("checked") ? " IsSelected=\"True\"" : "")}>{Cells("ليلى", "الرياض")}</ins:TableRow>\n" +
                $"  <ins:TableRow>{Cells("عمر", "عمّان")}</ins:TableRow>\n" +
                $"  <ins:TableRow>{Cells("سارة", "القاهرة")}</ins:TableRow>\n" +
                "</ins:Table>";
            if (!s.On("bulk")) return table;
            return "<StackPanel>\n" + Indent(table, 2) + "\n" +
                $"  <ins:BulkBar Table=\"{{Binding #People}}\"{(s.On("text") ? " Text=\"الصفوف المحددة: {n}\"" : "")}>\n" +
                "    <StackPanel>\n" +
                "      <Button Classes=\"secondary sm\" Content=\"تصدير\" />\n" +
                "      <Button Classes=\"danger sm\" Content=\"حذف\" />\n" +
                "      <Button Classes=\"bare sm\" Content=\"إلغاء\" ins:BulkBar.ClearsSelection=\"True\" />\n" +
                "    </StackPanel>\n" +
                "  </ins:BulkBar>\n" +
                "</StackPanel>";
        },
        Notes = s => s.On("bulk") ? ["حدد الصفوف أو ألغ تحديدها، فيظهر الشريط ويختفي معها، ويكتب العدد بنفسه."] : ["حدد الصفوف أو ألغ تحديدها، فيظهر هنا الحدث."],
        Stage = (view, _, say) =>
        {
            foreach (var t in view.GetLogicalDescendants().OfType<Table>())
                t.Loaded += (_, _) => t.SelectionChanged += (_, e) => say($"حدث `SelectionChanged`، و`Count` الآن `{e.Count}`");
        },
    };

    public static PlayDef Edit { get; } = new()
    {
        Id = "edit",
        Title = "التعديل في مكانه",
        Wide = _ => true,
        Controls =
        [
            Switch("number", "عمود أرقام", "Edit=\"Number\"", "حقل للأرقام، يُكتب من اليسار.", OptionKind.Property),
            Switch("select", "اختيار من قائمة", "Edit=\"Select\"", "قائمة تختار منها بدل الحقل، ومعها خياراتها في `Options`.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "Edit=\"Text\"", "على `ins:TableColumn`: ينقر المستخدم الخلية مرتين، أو يضغط `F2`، أو يبدأ الكتابة، فتصير حقلًا. ثم يحفظ `Enter` أو مغادرة الحقل ما كتب، ويعيد `Esc` القيمة السابقة.", OptionKind.Property, true),
            Row("خيارات أخرى", "Options", "خيارات القائمة، بينها `|`: `Options=\"نشط|موقوف\"`.", OptionKind.Property),
            Row("خيارات أخرى", "Value", "على `ins:TableCell`: قيمة الخلية إذا اختلفت عن نصها، فيبدأ بها الحقل وتتحدث بعد الحفظ.", OptionKind.Property),
            Row("الحدث", "CellEditing", "يُطلق قبل حفظ التغيير، ومعه الخلية `Cell` والصف `Row` والقيمة الجديدة `Value` والسابقة `Old`، و`Revert()` تعيد السابقة. ألغه بـ`Cancel` لترفض القيمة.", OptionKind.Event),
        ],
        Axaml = s =>
            "<ins:Table>\n" +
            "  <ins:Table.Columns>\n" +
            "    <ins:TableColumn Header=\"الاسم\" Edit=\"Text\" />\n" +
            $"    <ins:TableColumn Header=\"الكمية\" Classes=\"num\"{(s.On("number") ? " Edit=\"Number\"" : "")} />\n" +
            $"    <ins:TableColumn Header=\"الحالة\"{(s.On("select") ? " Edit=\"Select\" Options=\"نشط|موقوف\"" : "")} />\n" +
            "  </ins:Table.Columns>\n" +
            $"  <ins:TableRow>{Cells("ليلى", "12", "نشط")}</ins:TableRow>\n" +
            $"  <ins:TableRow>{Cells("عمر", "7", "موقوف")}</ins:TableRow>\n" +
            "</ins:Table>",
        Notes = _ => ["انقر خلية تقبل التعديل مرتين، أو اضغط `F2`، ثم احفظ بـ`Enter` أو تراجع بـ`Esc`."],
        Stage = (view, _, say) =>
        {
            foreach (var t in view.GetLogicalDescendants().OfType<Table>())
                t.CellEditing += (_, e) => say($"حدث `CellEditing`، والقيمة `{e.Old}` صارت `{e.Value}`");
        },
    };

    public static PlayDef Sheet { get; } = new()
    {
        Id = "sheet",
        Title = "التنقل بين الخلايا",
        Wide = _ => true,
        Controls =
        [
            Switch("cells", "من خلية إلى خلية", "IsCellNavigation", "يتنقل المستخدم بين الخلايا كما في جداول البيانات: الأسهم بحسب اتجاه الواجهة، و`Home` و`End` إلى أول الصف وآخره، و`Ctrl+Home` و`Ctrl+End` إلى أول الجدول وآخره، و`PageUp` و`PageDown` صفحةً صفحة، و`Tab` و`Shift+Tab` إلى الخلية التالية والسابقة.", OptionKind.Property, on: true),
            Switch("edit", "خلايا تقبل التعديل", "Edit=\"Text\"", "على عمود: يبدأ تعديل خليته بـ`Enter` أو `F2` أو بالكتابة مباشرة، ثم يحفظ `Enter` ما كتبت وينزل إلى الخلية التي تحتها، ويتراجع `Esc`.", OptionKind.Property, on: true),
        ],
        Extra =
        [
            Row("الأساس", "CurrentRow", "الصف الذي عليه لوحة المفاتيح، ومعه `CurrentColumn` رقم عمود الخلية.", OptionKind.Property, true),
        ],
        Axaml = s =>
        {
            var edit = s.On("edit") ? " Edit=\"Text\"" : "";
            var num = s.On("edit") ? " Edit=\"Number\"" : "";
            string[][] rows = [["ليلى", "الرياض", "12", "نشط"], ["عمر", "عمّان", "7", "نشط"], ["سارة", "القاهرة", "30", "موقوف"], ["كريم", "دبي", "4", "نشط"]];
            return $"<ins:Table{(s.On("cells") ? " IsCellNavigation=\"True\"" : "")}>\n" +
                "  <ins:Table.Columns>\n" +
                $"    <ins:TableColumn Header=\"الاسم\"{edit} />\n" +
                "    <ins:TableColumn Header=\"المدينة\" />\n" +
                $"    <ins:TableColumn Header=\"الكمية\" Classes=\"num\"{num} />\n" +
                "    <ins:TableColumn Header=\"الحالة\" />\n" +
                "  </ins:Table.Columns>\n" +
                string.Concat(rows.Select(r => $"  <ins:TableRow>{Cells(r)}</ins:TableRow>\n")) +
                "</ins:Table>";
        },
        Notes = s =>
        {
            if (!s.On("cells")) return ["بلا `IsCellNavigation` تنتقل الأسهم بين الصفوف، ولا تتنقل بين الخلايا إلا إذا كان في الجدول عمود يقبل التعديل."];
            return s.On("edit")
                ? ["انقر الجدول ثم تنقل فيه بالأسهم و`Tab`، واكتب على خلية من الاسم أو الكمية فيبدأ التعديل بما كتبت."]
                : ["انقر الجدول ثم تنقل بين خلاياه بالأسهم و`Home` و`End` و`Tab`."];
        },
    };

    public static PlayDef[] All { get; } = [Table, Wrap, Sort, Select, Edit, Sheet];
}
