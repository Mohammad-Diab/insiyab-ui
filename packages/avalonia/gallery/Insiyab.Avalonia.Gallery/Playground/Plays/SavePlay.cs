using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class SavePlay
{
    public const string Example =
        """
        <ins:SaveStatus State="Saving" />
        <ins:SaveStatus State="Saved" />
        <ins:SaveStatus State="Error" />
        """;

    public static PlayDef Save { get; } = new()
    {
        Id = "save",
        Title = "حالة الحفظ",
        Wide = s => s.On("auto"),
        WideMax = 416,
        Controls =
        [
            Switch("auto", "في نموذج يحفظ نفسه", "ins:Autosave.Delay", "على الحاوية التي تضم الحقول: إذا مضت هذه المدة بالميلي ثانية على آخر تغيير قال المؤشر الذي فيها «جارٍ الحفظ» وأطلق `ins:Autosave.Saving`. والويب ينتظر `800`.", OptionKind.Property, "الحفظ التلقائي"),
            Select("state", "الحالة", "Saved",
                Choice("", "بلا حالة"),
                Choice("Saving", "جارٍ الحفظ", "State=\"Saving\"", "التغيير في طريقه إلى الحفظ.", OptionKind.Property),
                Choice("Saved", "حُفظ", "State=\"Saved\"", "حُفظ آخر تغيير.", OptionKind.Property),
                Choice("Error", "تعذر", "State=\"Error\"", "تعذر حفظ آخر تغيير.", OptionKind.Property)),
        ],
        Extra =
        [
            Row("الأساس", "ins:SaveStatus", "يخبر بمصير آخر تغيير. فإن خلا من الحالة حجز مكانه فارغًا، حتى إذا ظهر لم يقفز السطر. ومن الشيفرة `SetState(SaveState.Saved)`، ولك أن تضع كلماتك في `SavedText` وأخواتها.", OptionKind.Element, true),
            Row("الحفظ التلقائي", "ins:Autosave.Saving", "يُطلق على الحاوية حين يحين الحفظ. فإن كنت ستحفظ على الخادم فاجعل `Handled` فيه `true`، ثم استدع `Done(true)` أو `Done(false)` حين تنتهي، وإلا عُدَّ التغيير محفوظًا في الحال.", OptionKind.Event),
            Row("الحفظ التلقائي", "ins:Autosave.Indicator", "على الحاوية: مؤشر خارجها يخبر بحفظها، مثل `{Binding #Status}`.", OptionKind.Property),
        ],
        Disable = s => s.On("auto") ? ["state"] : [],
        Axaml = s =>
        {
            if (!s.On("auto")) return $"<ins:SaveStatus{(s.Has("state") ? $" State=\"{s["state"]}\"" : "")} />";
            return "<StackPanel ins:Autosave.Delay=\"800\" Spacing=\"6\">\n" +
                "  <DockPanel>\n" +
                "    <ins:SaveStatus DockPanel.Dock=\"Right\" />\n" +
                "    <TextBlock Classes=\"label\" Text=\"ملاحظة\" />\n" +
                "  </DockPanel>\n" +
                "  <TextBox AcceptsReturn=\"True\" TextWrapping=\"Wrap\" MinHeight=\"96\" />\n" +
                "</StackPanel>";
        },
        Notes = s =>
        {
            if (s.On("auto")) return ["اكتب في الحقل ثم توقف لحظة، فيظهر «جارٍ الحفظ» ثم «حُفظ»."];
            return s.Has("state") ? [] : ["لا حالة، فالمؤشر فارغ يحجز مكانه."];
        },
    };

    public static PlayDef[] All { get; } = [Save];
}
