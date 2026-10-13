using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class TooltipsPlay
{
    public const string Example =
        """
        <Button Classes="secondary icon" AutomationProperties.Name="تعديل" ToolTip.Tip="تعديل">
          <ins:Icon Data="{StaticResource IconEdit}" />
        </Button>
        <Button Classes="secondary icon" AutomationProperties.Name="مشاركة" ToolTip.Tip="مشاركة">
          <ins:Icon Data="{StaticResource IconShare}" />
        </Button>
        """;

    public static PlayDef Tip { get; } = new()
    {
        Id = "tip",
        Title = "التلميح",
        Controls =
        [
            Select("on", "على", "icon",
                Choice("icon", "زر أيقونة"),
                Choice("button", "زر بنص"),
                Choice("text", "نص عادي", "Focusable=\"True\"", "لعنصر لا يقبل التركيز، حتى يبلغ تلميحه من يستعمل لوحة المفاتيح.", OptionKind.Property)),
            Text("text", "نص التلميح", "تعديل"),
            Select("side", "الجهة", "",
                Choice("", "فوق"),
                Choice("Bottom", "تحت", "ToolTip.Placement=\"Bottom\"", "تحت العنصر.", OptionKind.Property),
                Choice("Left", "يسار", "ToolTip.Placement=\"Left\"", "على يسار العنصر.", OptionKind.Property),
                Choice("Right", "يمين", "ToolTip.Placement=\"Right\"", "على يمين العنصر.", OptionKind.Property)),
        ],
        Extra =
        [
            Row("الأساس", "ToolTip.Tip", "نص التلميح، يظهر حين يقف المؤشر على العنصر أو يبلغه التركيز، ويختفي بمفتاح `Esc`. ولزر الأيقونة اجعله اسمه نفسه في `AutomationProperties.Name`.", OptionKind.Property, true),
            Row("الجهة", "ToolTip.Placement=\"Top\"", "فوق العنصر، وهي الجهة في المكتبة إن لم تذكر غيرها. وإذا ضاق المكان بالتلميح انتقل إلى الجهة المقابلة.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var side = s.Has("side") ? $" ToolTip.Placement=\"{s["side"]}\"" : "";
            return s["on"] switch
            {
                "button" => $"<Button Classes=\"secondary\" Content=\"حذف\" ToolTip.Tip=\"{s["text"]}\"{side} />",
                "text" => $"<TextBlock Text=\"نص عادي\" Focusable=\"True\" ToolTip.Tip=\"{s["text"]}\"{side} />",
                _ => $"<Button Classes=\"secondary icon\" AutomationProperties.Name=\"{s["text"]}\" ToolTip.Tip=\"{s["text"]}\"{side}>\n  <ins:Icon Data=\"{{StaticResource IconEdit}}\" />\n</Button>",
            };
        },
        Notes = _ => ["قف بالمؤشر على العنصر أو بلغه بمفتاح `Tab` لترى التلميح بخياراتك."],
    };

    public static PlayDef[] All { get; } = [Tip];
}
