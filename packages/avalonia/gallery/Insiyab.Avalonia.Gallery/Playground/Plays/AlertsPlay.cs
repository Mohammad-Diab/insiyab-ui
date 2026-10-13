using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class AlertsPlay
{
    public const string Example =
        """
        <ins:Alert Classes="ok" Content="تم الحفظ." />
        <ins:Alert Classes="info" Content="معلومة للاطلاع." />
        <ins:Alert Classes="warn" Content="انتبه قبل المتابعة." />
        <ins:Alert Classes="bad" Content="حدث خطأ." />
        """;

    public static PlayDef Alert { get; } = new()
    {
        Id = "alert",
        Title = "التنبيه",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Select("tone", "النبرة", "info",
                Choice("ok", "نجاح", "ok", "لما تم بنجاح."),
                Choice("info", "معلومة", "info", "لمعلومة يحسن أن يعرفها القارئ، بلون العلامة."),
                Choice("warn", "تحذير", "warn", "لما يستحق الانتباه قبل المتابعة."),
                Choice("bad", "خطأ", "bad", "لخطأ وقع، أو عملية تعذرت.")),
            Text("text", "النص", "رسالة قصيرة للاطلاع."),
            Switch("action", "مع رابط"),
            Switch("dismiss", "يمكن إغلاقه", "IsDismissible", "يظهر زر إغلاق في الرسالة، فيُغلق التنبيه بضغطة. ويُخفى التنبيه ولا يُحذف، فتعيده بـ`IsVisible=\"True\"`.", OptionKind.Property, "الإغلاق"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Alert", "دائرة تحمل رمز التنبيه في تجويف الرسالة، ومعه كلاس للنبرة. ونصه أو ما فيه في `Content`.", OptionKind.Element, true),
            Row("الأساس", "Icon", "رمز آخر غير رمز النبرة، إن شئت.", OptionKind.Property),
            Row("الإغلاق", "CloseLabel", "اسم زر الإغلاق لقارئ الشاشة.", OptionKind.Property),
            Row("الإغلاق", "Dismissed", "يُطلق بعد أن يُغلق التنبيه ويختفي.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var attrs = $" Classes=\"{s["tone"]}\"" + (s.On("dismiss") ? " IsDismissible=\"True\"" : "");
            if (!s.On("action")) return $"<ins:Alert{attrs} Content=\"{s["text"]}\" />";
            return $"<ins:Alert{attrs}>\n" +
                "  <WrapPanel ItemSpacing=\"6\">\n" +
                $"    <TextBlock Text=\"{s["text"]}\" VerticalAlignment=\"Center\" />\n" +
                "    <HyperlinkButton Content=\"التفاصيل\" NavigateUri=\"https://example.com/details\" />\n" +
                "  </WrapPanel>\n" +
                "</ins:Alert>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s.On("action")) notes.Add("إن كان للمشكلة حل فدل عليه برابط أو زر صغير داخل النص.");
            if (s.On("dismiss")) notes.Add("اضغط الزر فيُغلق التنبيه ويظهر هنا الحدث، وغيّر أي خيار لتعيده.");
            return notes;
        },
        Stage = (view, s, say) =>
        {
            if (s.On("dismiss")) view.AddHandler(Controls.Alert.DismissedEvent, (_, _) => say("حدث `Dismissed` على التنبيه"));
        },
    };

    public static PlayDef[] All { get; } = [Alert];
}
