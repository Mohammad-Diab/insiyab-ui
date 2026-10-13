using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ChartsPlay
{
    public const string Example =
        """
        <ins:Chart Type="Bar" Labels="يناير,فبراير,مارس,أبريل,مايو,يونيو">
          <ins:ChartSeries Name="الطلبات" Values="120,150,98,174,160,210" />
          <ins:ChartSeries Name="المرتجعات" Values="14,9,12,18,11,15" />
        </ins:Chart>
        """;

    public static string Code { get; } =
        """
        Sales.Type = ChartType.Line;
        Sales.Labels = "يناير,فبراير,مارس";
        Sales.Series.Add(new ChartSeries { Name = "المبيعات", Values = "120,150,98" });
        // يُعاد الرسم كلما تغيرت السلاسل أو قيمها
        """;

    public static PlayDef Chart { get; } = new()
    {
        Id = "chart",
        Title = "الرسم البياني",
        Wide = s => s["type"] != "Spark",
        Controls =
        [
            Select("type", "النوع", "Bar",
                Choice("Bar", "أعمدة", "Type=\"Bar\"", "أعمدة لكل فئة، تتجاور فيها السلاسل. وهو النوع إذا غابت القيمة.", OptionKind.Property),
                Choice("HBar", "أعمدة أفقية", "Type=\"HBar\"", "يأتي الاسم في أول السطر وعموده بعده، فاختره إذا طالت الأسماء.", OptionKind.Property),
                Choice("Histogram", "توزيع", "Type=\"Histogram\"", "أعمدة متلاصقة تبين كيف تتوزع القيم على الفئات، وإذا تعددت السلاسل تراكبت شفافة لتقارن بينها.", OptionKind.Property),
                Choice("Line", "خط", "Type=\"Line\"", "لما يتغير مع الزمن.", OptionKind.Property),
                Choice("Area", "مساحة", "Type=\"Area\"", "خط يملأ ما تحته.", OptionKind.Property),
                Choice("Donut", "حلقة", "Type=\"Donut\"", "لأجزاء من كل، يرسم السلسلة الأولى وحدها، وتحته مفتاحه بقيمه، وفي وسطه المجموع.", OptionKind.Property),
                Choice("Spark", "خط مصغر", "Type=\"Spark\"", "خط بلا محاور في حجم كلمة، تضعه في بطاقة إحصائية مثلًا.", OptionKind.Property)),
            Seg("source", "البيانات", "series",
                Choice("series", "سلاسل"),
                Choice("attr", "خاصية", "Values", "سلسلة واحدة بلا `ins:ChartSeries`، أرقامها مفصولة بفواصل: `3,5,4`.", OptionKind.Property)).In("البيانات"),
            Seg("series", "السلاسل", "2",
                Choice("1", "1"),
                Choice("2", "2"),
                Choice("3", "3")),
            Switch("stack", "مكدسة", "Stacked=\"True\"", "مع `Bar` أو `HBar`: تجمع السلاسل بعضها فوق بعض في عمود واحد.", OptionKind.Property, "النوع"),
            Text("center", "تحت المجموع", ""),
            Switch("caption", "بعنوان", "Caption", "عنوان قصير يُكتب تحت الرسم.", OptionKind.Property),
            Switch("ltr", "من اليسار", "FlowDirection=\"LeftToRight\"", "تجري الفئات من اليسار وإن كانت الواجهة عربية.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Chart", "يرسم من سلاسله `ins:ChartSeries`، وأسماء فئاته في `Labels` مفصولة بفواصل. وتسير الفئات مع اتجاه القراءة، وتُنسق الأرقام بأرقام لاتينية، ويقرأ قارئ الشاشة الأرقام كلها.", OptionKind.Element, true),
            Row("الأساس", "ins:ChartSeries", "سلسلة: اسمها في `Name`، وأرقامها في `Values` مفصولة بفواصل.", OptionKind.Element),
            Row("الأساس", "PlotHeight", "ارتفاع الرسم.", OptionKind.Property),
            Row("الأساس", "Brush1", "ألوان السلاسل، من `Brush1` إلى `Brush6`.", OptionKind.Property),
            Row("البيانات", "SeriesName", "مع `Values`: اسم السلسلة، ويظهر مع القيمة تحت المؤشر.", OptionKind.Property),
            Row("الحلقة", "CenterLabel", "نص صغير يُكتب تحت المجموع في وسط الحلقة.", OptionKind.Property),
            Row("الحلقة", "Center", "نص يُكتب في وسط الحلقة مكان المجموع.", OptionKind.Property),
        ],
        Disable = s =>
        {
            var off = new List<string>();
            var spark = s["type"] == "Spark";
            var single = spark || s["source"] == "attr" || s["type"] == "Donut";
            if (spark) off.AddRange(["source", "caption"]);
            if (single) off.Add("series");
            if ((s["type"] != "Bar" && s["type"] != "HBar") || single || s["series"] == "1") off.Add("stack");
            if (s["type"] != "Donut") off.Add("center");
            return off;
        },
        Axaml = s =>
        {
            var type = s["type"];
            var hist = type == "Histogram";
            var spark = type == "Spark";
            var attr = spark || s["source"] == "attr";
            var labels = hist ? "0–2,2–4,4–6,6–8,8–10" : "يناير,فبراير,مارس,أبريل,مايو";
            string[] names = hist ? ["المجموعة الأولى", "المجموعة الثانية", "المجموعة الثالثة"] : ["هذا العام", "العام الماضي", "قبل عامين"];
            string[] data = hist ? ["3,8,19,27,12", "2,6,15,31,16", "1,4,10,22,20"] : ["120,150,98,174,160", "90,130,110,140,150", "60,80,95,100,120"];
            var n = attr || type == "Donut" ? 1 : int.Parse(s["series"]);
            var stack = s.On("stack") && n > 1 && type is "Bar" or "HBar";
            var at = $" Type=\"{type}\"" + (stack ? " Stacked=\"True\"" : "") +
                (type == "Donut" && s.Has("center") ? $" CenterLabel=\"{s["center"]}\"" : "") +
                (s.On("caption") && !spark ? $" Caption=\"{(hist ? "توزيع القيم" : "الأشهر الخمسة الأولى")}\"" : "") +
                (s.On("ltr") ? " FlowDirection=\"LeftToRight\"" : "");
            if (spark)
                return "<Border Classes=\"glass\" Padding=\"16,12\">\n" +
                    "  <DockPanel>\n" +
                    $"    <ins:Chart DockPanel.Dock=\"Right\"{at} Values=\"12,18,14,22,19,27,31\" Margin=\"12,0,0,0\" />\n" +
                    "    <StackPanel Spacing=\"1\" VerticalAlignment=\"Center\">\n" +
                    "      <TextBlock Text=\"الزيارات\" FontSize=\"12\" FontWeight=\"SemiBold\" Foreground=\"{DynamicResource InsText2}\" />\n" +
                    "      <TextBlock Classes=\"num\" Text=\"31\" FontSize=\"24\" FontWeight=\"ExtraBold\" />\n" +
                    "    </StackPanel>\n" +
                    "  </DockPanel>\n" +
                    "</Border>";
            if (attr) return $"<ins:Chart{at} Labels=\"{labels}\" Values=\"{data[0]}\" SeriesName=\"{names[0]}\" />";
            return $"<ins:Chart{at} Labels=\"{labels}\">\n" +
                string.Join("\n", Enumerable.Range(0, n).Select(i => $"  <ins:ChartSeries Name=\"{names[i]}\" Values=\"{data[i]}\" />")) +
                "\n</ins:Chart>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s["type"] == "Spark") notes.Add("الخط المصغر سلسلة واحدة في `Values`، ومكانه بطاقة إحصائية كهذه.");
            if (s["type"] == "Donut") notes.Add("ترسم الحلقة السلسلة الأولى وحدها.");
            if (s["type"] != "Spark") notes.Add("مرر المؤشر على الرسم فتظهر القيمة التي تحته.");
            return notes;
        },
    };

    public static PlayDef[] All { get; } = [Chart];
}
