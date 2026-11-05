using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class SurfacesPlay
{
    public const string Example =
        """
        <Border Classes="glass" Padding="20"><TextBlock Text="glass" /></Border>
        <Border Classes="card hoverable" Padding="20"><TextBlock Text="card" /></Border>
        <Border Classes="glass-inner" Padding="20"><TextBlock Text="glass-inner" /></Border>
        """;

    public const string RadiiExample =
        """
        <Border Classes="radius" CornerRadius="{DynamicResource InsRXs}"><TextBlock Text="xs · 7" /></Border>
        <Border Classes="radius" CornerRadius="{DynamicResource InsRSm}"><TextBlock Text="sm · 10" /></Border>
        <Border Classes="radius" CornerRadius="{DynamicResource InsRMd}"><TextBlock Text="md · 14" /></Border>
        <Border Classes="radius" CornerRadius="{DynamicResource InsRLg}"><TextBlock Text="lg · 22" /></Border>
        <Border Classes="radius" CornerRadius="{DynamicResource InsRXl}"><TextBlock Text="xl · 32" /></Border>
        <Border Classes="radius" CornerRadius="{DynamicResource InsRPill}"><TextBlock Text="pill" /></Border>
        """;

    public static PlayDef Glass { get; } = new()
    {
        Id = "glass",
        Title = "الزجاج",
        Controls =
        [
            Seg("kind", "السطح", "",
                Choice("", "زجاج"),
                Choice("inner", "مسطح", "glass-inner", "مظهر مسطح لسطح يوضع داخل سطح آخر: لون خفيف وحد رفيع، بلا ضبابية.")),
            Switch("nest", "زجاج داخل زجاج"),
        ],
        Extra =
        [
            Row("الأساس", "glass", "على `Border`: السطح الأساسي بطبقاته الأربع، لا فراغ داخليًا فيه ولا سلوك. وإذا وضعته داخل سطح آخر تخلى وحده عن حده وظله ولمعته.", OptionKind.Class, true),
        ],
        Disable = s => s.Has("kind") ? ["nest"] : [],
        Axaml = s =>
        {
            if (s.Has("kind")) return "<Border Classes=\"glass-inner\" Padding=\"20\"><TextBlock Text=\"سطح مسطح\" /></Border>";
            if (!s.On("nest")) return "<Border Classes=\"glass\" Padding=\"20\"><TextBlock Text=\"سطح من زجاج\" /></Border>";
            return "<Border Classes=\"glass\" Padding=\"20\">\n" +
                "  <StackPanel Spacing=\"14\">\n" +
                "    <TextBlock Text=\"السطح الخارجي\" />\n" +
                "    <Border Classes=\"glass\" Padding=\"16\"><TextBlock Text=\"السطح الداخلي\" /></Border>\n" +
                "  </StackPanel>\n" +
                "</Border>";
        },
        Notes = s => s.On("nest") && !s.Has("kind") ? ["السطحان يحملان الكلاس نفسه، والداخلي منهما صار مسطحًا وحده."] : [],
    };

    public static PlayDef Card { get; } = new()
    {
        Id = "card",
        Title = "البطاقة",
        Controls =
        [
            Switch("link", "زر بهيئة بطاقة", "InsCardButton", "ضع هذه السمة على `Button` في `Theme`، فترتفع البطاقة تحت المؤشر دون `hoverable`.", OptionKind.Property),
            Switch("hover", "ترتفع تحت المؤشر", "hoverable", "ترفع البطاقة قليلًا إذا مر عليها المؤشر، لبطاقة تُنقر."),
        ],
        Extra =
        [
            Row("الأساس", "card", "على `Border`: بطاقة من زجاج بطبقاته الأربع، باسم يقول ما هي.", OptionKind.Class, true),
        ],
        Disable = s => s.On("link") ? ["hover"] : [],
        Axaml = s =>
        {
            if (s.On("link")) return "<Button Theme=\"{StaticResource InsCardButton}\" Padding=\"20\" Content=\"بطاقة تُنقر\" />";
            return $"<Border{Classes("card", s.On("hover") ? "hoverable" : null)} Padding=\"20\"><TextBlock Text=\"بطاقة\" /></Border>";
        },
        Notes = s => s.On("hover") || s.On("link") ? ["مرّر المؤشر على البطاقة."] : [],
    };

    public static PlayDef Tile { get; } = new()
    {
        Id = "tile",
        Title = "البطاقة على اللوح",
        Wide = _ => true,
        WideMax = 520,
        Controls =
        [
            Switch("hover", "ترتفع تحت المؤشر", "hoverable", "مع `tile`: ترتفع بكسلًا واحدًا تحت المؤشر، ويأخذ حدها لون العلامة ويكبر ظلها قليلًا، ولا ترتفع إذا خُففت المؤثرات."),
        ],
        Extra =
        [
            Row("الأساس", "tile", "على `Border` داخل لوح: بطاقة مصمتة بحد رفيع وظل صغير، فلا يقع زجاج فوق زجاج.", OptionKind.Class, true),
        ],
        Axaml = s => "<Border Classes=\"panel\" Padding=\"20\">\n" + $"  <Border{Classes("tile", s.On("hover") ? "hoverable" : null)} Padding=\"16\"><TextBlock Text=\"بطاقة على لوح\" /></Border>\n" + "</Border>",
        Notes = s => s.On("hover") ? ["مرّر المؤشر على البطاقة."] : [],
    };

    public static PlayDef Panel { get; } = new()
    {
        Id = "panel",
        Title = "اللوح",
        Wide = _ => true,
        WideMax = 520,
        Controls =
        [
            Switch("head", "رأس", "panel-head", "رأس اللوح، وفيه عنوانه `TextBlock.panel-title`. ضعه في `DockPanel.Dock=\"Top\"`.", on: true),
            Switch("ico", "أيقونة في الرأس", "panel-ico", "أيقونة القسم على رقعة ملونة قبل العنوان.", on: true),
            Switch("actions", "إجراءات في الرأس", "panel-actions", "على `StackPanel` في آخر الرأس، فيه أزرار اللوح.", on: true),
            Switch("foot", "تذييل", "panel-foot", "سطر في أسفل اللوح لأزراره، في `DockPanel.Dock=\"Bottom\"`.", on: true),
            Select("tone", "النبرة", "", Choice("", "بلا"), Choice("alert", "يحتاج انتباهًا", "alert", "مع `panel`: يلون رأس اللوح وحده بلون الخطر، لا اللوح كله."), Choice("ok", "نجاح", "ok", "مع `panel`: رأس بلون النجاح، لما تم."), Choice("warn", "تحذير", "warn", "مع `panel`: رأس بلون التحذير، لما ينتظر."), Choice("info", "معلومة", "info", "مع `panel`: رأس بلون المعلومة، لملخص يُقرأ أولًا."), Choice("brand", "مميز", "brand", "مع `panel`: رأس بلون العلامة، لبند واحد يستحق أن يبرز.")),
            Switch("flush", "جسم بلا فراغ داخلي", "flush", "مع `panel-body`: لجدول أو قائمة يمتدان إلى حواف اللوح."),
            Switch("note", "سطر شرح", "panel-note", "سطر شرح قصير تحت الرأس، أو في أعلى جسم بلا فراغ داخلي."),
            Switch("band", "رأس بشريط", "band", "مع `panel-head`: خلفية خفيفة للرأس تستدير مع زاويتي اللوح العلويتين."),
            Switch("low", "ظل أخف", "low", "مع `panel`: ظل أقصر وأخف للوح كبير يملأ الصفحة، من `InsShPanel`."),
            Switch("dense", "مضغوط", "dense", "مع `panel`: حشو أصغر في الرأس والجسم والتذييل، وأيقونة أصغر."),
        ],
        Extra =
        [
            Row("الأساس", "panel", "على `Border` حول `DockPanel`: حاوية من زجاج بثلاثة أجزاء، رأس ثم جسم ثم تذييل. ولا يرتفع اللوح تحت المؤشر، وإنما تضيء حافته.", OptionKind.Class, true),
            Row("الأساس", "panel-body", "جسم اللوح، آخر ما في `DockPanel` فيملأ ما بقي.", OptionKind.Class, true),
            Row("الأساس", "panel-title", "عنوان اللوح في رأسه.", OptionKind.Class, true),
        ],
        Disable = s => s.On("head") ? [] : ["ico", "actions", "tone", "band"],
        Axaml = s =>
        {
            var lines = new List<string> { $"<Border{Classes("panel", s.On("head") && s.Has("tone") ? s["tone"] : null, s.On("low") ? "low" : null, s.On("dense") ? "dense" : null)}>", "  <DockPanel>" };
            if (s.On("head"))
            {
                lines.Add($"    <Border{Classes("panel-head", s.On("band") ? "band" : null)} DockPanel.Dock=\"Top\">");
                lines.Add("      <Grid ColumnDefinitions=\"Auto,*,Auto\" ColumnSpacing=\"12\">");
                if (s.On("ico")) lines.Add("        <Border Classes=\"panel-ico\"><ins:Icon Data=\"{StaticResource IconUsers}\" /></Border>");
                lines.Add("        <TextBlock Grid.Column=\"1\" Classes=\"panel-title\" Text=\"العنوان\" />");
                if (s.On("actions")) lines.Add("        <StackPanel Grid.Column=\"2\" Classes=\"panel-actions\"><Button Classes=\"bare sm\" Content=\"تعديل\" /></StackPanel>");
                lines.Add("      </Grid>");
                lines.Add("    </Border>");
            }
            if (s.On("note") && !s.On("flush")) lines.Add("    <Border Classes=\"panel-note\" DockPanel.Dock=\"Top\"><TextBlock Text=\"سطر شرح.\" /></Border>");
            if (s.On("foot")) lines.AddRange(["    <Border Classes=\"panel-foot\" DockPanel.Dock=\"Bottom\">", "      <Button Classes=\"primary sm\" Content=\"حفظ\" />", "    </Border>"]);
            if (s.On("flush"))
            {
                var list = new[] { "<ListBox Classes=\"static\">", "  <ListBoxItem Content=\"البند الأول\" />", "  <ListBoxItem Content=\"البند الثاني\" />", "</ListBox>" };
                lines.Add("    <Border Classes=\"panel-body flush\">");
                if (s.On("note"))
                {
                    lines.Add("      <StackPanel>");
                    lines.Add("        <Border Classes=\"panel-note\"><TextBlock Text=\"سطر شرح.\" /></Border>");
                    lines.AddRange(list.Select(l => "        " + l));
                    lines.Add("      </StackPanel>");
                }
                else lines.AddRange(list.Select(l => "      " + l));
                lines.Add("    </Border>");
            }
            else lines.Add("    <Border Classes=\"panel-body\"><TextBlock Text=\"المحتوى.\" /></Border>");
            lines.AddRange(["  </DockPanel>", "</Border>"]);
            return string.Join("\n", lines);
        },
    };

    public static PlayDef Sheet { get; } = new()
    {
        Id = "sheet",
        Title = "الورقة",
        Wide = _ => true,
        WideMax = 560,
        Controls =
        [
            Switch("desk", "على مكتب", "desk", "على `Border` حول الورقة: سطح خافت تحتها يفصلها عن الصفحة.", on: true),
        ],
        Extra =
        [
            Row("الأساس", "sheet", "على `Border`: ورقة لنص طويل يُقرأ، مصمتة بحد رفيع وظل صغير وحشو واسع.", OptionKind.Class, true),
        ],
        Axaml = s =>
        {
            var sheet = "<Border Classes=\"sheet\">\n  <StackPanel Spacing=\"8\">\n    <TextBlock Classes=\"h3\" Text=\"ملاحظات الاجتماع\" />\n" +
                "    <TextBlock Classes=\"prose\" TextWrapping=\"Wrap\" Text=\"اتفقنا على موعد التسليم الجديد، وعلى أن يراجع كل فريق قائمته قبل نهاية الأسبوع.\" />\n  </StackPanel>\n</Border>";
            return s.On("desk") ? "<Border Classes=\"desk\">\n" + Indent(sheet, "  ") + "\n</Border>" : sheet;
        },
    };

    private static string Indent(string text, string pad) => string.Join("\n", text.Split('\n').Select(l => pad + l));

    public static PlayDef[] All { get; } = [Glass, Card, Tile, Panel, Sheet];
}
