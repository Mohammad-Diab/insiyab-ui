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

    public const string Radii =
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
            Switch("alert", "يحتاج انتباهًا", "alert", "مع `panel`: يلون رأس اللوح وحده بلون الخطر، لا اللوح كله."),
            Switch("flush", "جسم بلا فراغ داخلي", "flush", "مع `panel-body`: لجدول أو قائمة يمتدان إلى حواف اللوح."),
            Switch("note", "سطر شرح", "panel-note", "سطر شرح قصير تحت الرأس، أو في أعلى جسم بلا فراغ داخلي."),
        ],
        Extra =
        [
            Row("الأساس", "panel", "على `Border` حول `DockPanel`: حاوية من زجاج بثلاثة أجزاء، رأس ثم جسم ثم تذييل. ولا يرتفع اللوح تحت المؤشر، وإنما تضيء حافته.", OptionKind.Class, true),
            Row("الأساس", "panel-body", "جسم اللوح، آخر ما في `DockPanel` فيملأ ما بقي.", OptionKind.Class, true),
            Row("الأساس", "panel-title", "عنوان اللوح في رأسه.", OptionKind.Class, true),
        ],
        Disable = s => s.On("head") ? [] : ["ico", "actions", "alert"],
        Axaml = s =>
        {
            var lines = new List<string> { $"<Border{Classes("panel", s.On("alert") && s.On("head") ? "alert" : null)}>", "  <DockPanel>" };
            if (s.On("head"))
            {
                lines.Add("    <Border Classes=\"panel-head\" DockPanel.Dock=\"Top\">");
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

    public static PlayDef[] All { get; } = [Glass, Card, Panel];
}
