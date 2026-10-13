using Avalonia.Controls;
using Avalonia.LogicalTree;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ProgressPlay
{
    public const string Example =
        """
        <ProgressBar Value="64" AutomationProperties.Name="عادي" />
        <ProgressBar Classes="ok" Value="100" AutomationProperties.Name="نجاح" />
        <ProgressBar Classes="warn" Value="88" AutomationProperties.Name="تحذير" />
        <ProgressBar Classes="bad" Value="97" AutomationProperties.Name="خطر" />
        """;

    public static PlayDef Bar { get; } = new()
    {
        Id = "bar",
        Title = "الشريط",
        Wide = _ => true,
        Controls =
        [
            Select("tone", "النبرة", "",
                Choice("", "عادي"),
                Choice("ok", "نجاح", "ok", "لما اكتمل، أو يسير كما ينبغي."),
                Choice("warn", "تحذير", "warn", "لنسبة تقترب من حدها، كمساحة تكاد تمتلئ."),
                Choice("bad", "خطر", "bad", "لنسبة بلغت حدها أو تجاوزته.")),
            Seg("size", "المقاس", "",
                Choice("sm", "صغير", "sm", "شريط أنحف، لصف في جدول أو بطاقة."),
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "شريط أغلظ، إذا كان التقدم محور الشاشة.")),
            Switch("indet", "مدة غير معروفة", "IsIndeterminate=\"True\"", "لعمل جارٍ لا تُعرف نسبته: يسري في الشريط بريق بدل القيمة.", OptionKind.Property),
            Text("value", "القيمة", "64"),
            Switch("live", "تحديث القيمة"),
        ],
        Extra =
        [
            Row("الأساس", "ProgressBar", "شريط التقدم، يمتلئ من جهة البداية، ويعلن قارئ الشاشة قيمته.", OptionKind.Element, true),
            Row("الأساس", "Value", "القيمة الحالية، بين `Minimum` و`Maximum`. وإن غيّرتها من الشيفرة انساب الشريط إلى قيمتها الجديدة.", OptionKind.Property),
            Row("الأساس", "Maximum", "القيمة عند الامتلاء، وهي `100` إن لم تذكرها.", OptionKind.Property),
            Row("الأساس", "AutomationProperties.Name", "اسم الشريط، يقرؤه قارئ الشاشة مع قيمته.", OptionKind.Property),
        ],
        Disable = s => s.On("indet") ? ["value", "live"] : [],
        Axaml = s =>
        {
            var cls = Classes(s["tone"], s["size"]);
            if (s.On("indet")) return $"<ProgressBar{cls} IsIndeterminate=\"True\" AutomationProperties.Name=\"جارٍ التحميل\" />";
            if (!s.On("live")) return $"<ProgressBar{cls} Value=\"{s["value"]}\" AutomationProperties.Name=\"التقدم\" />";
            return "<DockPanel>\n" +
                "  <Button Classes=\"secondary sm\" DockPanel.Dock=\"Right\" Margin=\"12,0,0,0\" Content=\"تقدم\" />\n" +
                $"  <ProgressBar x:Name=\"UploadBar\"{cls} Value=\"{s["value"]}\" AutomationProperties.Name=\"التقدم\" />\n" +
                "</DockPanel>";
        },
        Notes = s =>
        {
            if (s.On("indet")) return ["لا قيمة للشريط، فهو يدل على عمل جارٍ لا تُعرف نسبته."];
            return s.On("live") ? ["اضغط الزر، فتزيد القيمة وينساب الشريط إليها. ويكفي لذلك في الشيفرة `UploadBar.Value += 10`."] : [];
        },
        Stage = (view, s, _) =>
        {
            if (!s.On("live") || s.On("indet")) return;
            var bar = view.GetLogicalDescendants().OfType<ProgressBar>().FirstOrDefault();
            var button = view.GetLogicalDescendants().OfType<Button>().FirstOrDefault();
            if (bar is not null && button is not null) button.Click += (_, _) => bar.Value = Math.Min(bar.Maximum, bar.Value + 10);
        },
    };

    public static PlayDef Ring { get; } = new()
    {
        Id = "ring",
        Title = "الحلقة",
        Controls =
        [
            Seg("size", "المقاس", "",
                Choice("", "عادي"),
                Choice("lg", "كبير", "lg", "حلقة أكبر، في بطاقة إحصائية أو بجوار عنوان.")),
            Text("value", "النسبة", "72"),
        ],
        Extra =
        [
            Row("الأساس", "ins:ProgressRing", "تعرض النسبة حين تضيق المساحة، وتدور مع عقارب الساعة في الاتجاهين كما يدور القرص.", OptionKind.Element, true),
            Row("الأساس", "Value", "القيمة، ومنها تمتلئ الحلقة بنسبتها بين `Minimum` و`Maximum`، وتكتب النسبة في وسطها.", OptionKind.Property),
            Row("الأساس", "AutomationProperties.Name", "اسم الحلقة، يقرؤه قارئ الشاشة مع قيمتها.", OptionKind.Property),
            Row("الأساس", "Width=\"64\" Height=\"64\"", "قطر الحلقة إن أردت غير المقاسين.", OptionKind.Property),
        ],
        Axaml = s => $"<ins:ProgressRing{Classes(s["size"])} Value=\"{s["value"]}\" AutomationProperties.Name=\"التقدم\" />",
    };

    public static PlayDef[] All { get; } = [Bar, Ring];
}
