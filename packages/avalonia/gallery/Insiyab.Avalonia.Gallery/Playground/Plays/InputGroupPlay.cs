using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class InputGroupPlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="السعر" />
          <TextBox Classes="input-group" Text="249">
            <TextBox.InnerRightContent>
              <ContentControl Classes="addon" Content="ر.س" />
            </TextBox.InnerRightContent>
          </TextBox>
        </StackPanel>
        """;

    public static PlayDef Group { get; } = new()
    {
        Id = "group",
        Title = "مجموعة الحقل",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Seg("addon", "الملحق", "text",
                Choice("", "بلا"),
                Choice("text", "نص"),
                Choice("icon", "أيقونة")),
            Seg("side", "موضع الملحق", "end",
                Choice("start", "قبل الحقل", "InnerLeftContent", "ما يسبق الحقل في أوله، على اتجاه الواجهة: يمين الحقل في العربية.", OptionKind.Property),
                Choice("end", "بعده", "InnerRightContent", "ما يلي الحقل في آخره، على اتجاه الواجهة.", OptionKind.Property)),
            Text("text", "نص الملحق", "ر.س"),
            Seg("btn", "زر ملحق", "",
                Choice("", "بلا"),
                Choice("text", "بنص", "Button", "زر في آخر المجموعة، أصغر من الحقل بدرجة لتبقى زاويتاهما متوازيتين.", OptionKind.Element),
                Choice("icon", "بأيقونة", "icon", "زر مربع بأيقونة وحدها، ولا بد له من `AutomationProperties.Name`.")),
            Switch("bad", "خطأ", "bad", "على الحقل، فتتلون المجموعة كلها، ومعه رسالة `ContentControl.error` تحتها. ويأخذ الهيئة نفسها إذا كان في `DataValidationErrors` خطأ."),
            Switch("ltr", "باتجاه LTR", "FlowDirection=\"LeftToRight\"", "على الحقل، لعنوان موقع أو اسم مستخدم، فتبقى البادئة قبل النص على يساره.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "input-group", "على `TextBox`: يضم الحقل وملحقاته في قطعة واحدة، لها إطار واحد وحلقة تركيز واحدة.", OptionKind.Class, true),
            Row("الملحق", "addon", "على `ContentControl` في أحد طرفي الحقل: نص أو أيقونة ثابتة.", OptionKind.Class),
        ],
        Disable = s => s.Has("addon") ? (s["addon"] == "icon" ? ["text"] : []) : ["side", "text"],
        Axaml = s =>
        {
            var addon = s["addon"] == "icon"
                ? "<ContentControl Classes=\"addon\"><ins:Icon Data=\"{StaticResource IconWallet}\" /></ContentControl>"
                : $"<ContentControl Classes=\"addon\" Content=\"{s["text"]}\" />";
            var button = s["btn"] switch
            {
                "text" => "<Button Classes=\"secondary\" Content=\"تطبيق\" />",
                "icon" => "<Button Classes=\"secondary icon\" AutomationProperties.Name=\"تطبيق\"><ins:Icon Data=\"{StaticResource IconCheck}\" /></Button>",
                _ => null,
            };
            var start = s.Has("addon") && s["side"] == "start" ? addon : null;
            var end = new List<string>();
            if (s.Has("addon") && s["side"] == "end") end.Add(addon);
            if (button is not null) end.Add(button);
            string Slot(string name, List<string> parts) =>
                $"    <TextBox.{name}>\n" +
                (parts.Count == 1
                    ? $"      {parts[0]}\n"
                    : "      <StackPanel Orientation=\"Horizontal\" Spacing=\"4\">\n" + string.Concat(parts.Select(p => $"        {p}\n")) + "      </StackPanel>\n") +
                $"    </TextBox.{name}>\n";
            var attrs = Classes("input-group", s.On("bad") ? "bad" : null) +
                (s.On("ltr") ? " FlowDirection=\"LeftToRight\" PlaceholderText=\"my-site\"" : " Text=\"249\"");
            var slots = (start is null ? "" : Slot("InnerLeftContent", [start])) + (end.Count == 0 ? "" : Slot("InnerRightContent", end));
            var box = slots.Length == 0 ? $"  <TextBox{attrs} />\n" : $"  <TextBox{attrs}>\n{slots}  </TextBox>\n";
            return "<StackPanel Classes=\"field\">\n" +
                $"  <TextBlock Classes=\"label\" Text=\"{(s.On("ltr") ? "الرابط" : "المبلغ")}\" />\n" +
                box +
                (s.On("bad") ? "  <ContentControl Classes=\"error\" Content=\"رسالة الخطأ.\" />\n" : "") +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string>();
            if (s.Has("addon") && s["side"] == "start" && !s.On("ltr")) notes.Add("الملحق في `InnerLeftContent`، فهو في أول الحقل على يمينه في الواجهة العربية.");
            if (s.On("ltr")) notes.Add("صار الحقل من اليسار إلى اليمين، فالملحق قبله يبقى على يساره.");
            return notes;
        },
    };

    public static PlayDef Password { get; } = new()
    {
        Id = "password",
        Title = "إظهار كلمة المرور",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Seg("target", "زر العين", "",
                Choice("", "في الحقل"),
                Choice("own", "خارجه", "InsTextBoxRevealButton", "سمة `ToggleButton` يظهر الحقل خارجه، وتربط `IsChecked` منه بالخاصية `RevealPassword` من الحقل.", OptionKind.Property)),
        ],
        Extra =
        [
            Row("الأساس", "revealPasswordButton", "على `TextBox` معه `PasswordChar`: زر العين في آخر الحقل، يظهر كلمة المرور أو يخفيها.", OptionKind.Class, true),
            Row("الأساس", "RevealPassword", "حالة الحقل: هل كلمة المرور ظاهرة. تقرؤها أو تغيّرها من الشيفرة.", OptionKind.Property, true),
        ],
        Axaml = s =>
        {
            if (!s.Has("target"))
                return "<StackPanel Classes=\"field\">\n" +
                    "  <TextBlock Classes=\"label\" Text=\"كلمة المرور\" />\n" +
                    "  <TextBox Classes=\"revealPasswordButton\" PasswordChar=\"•\" Text=\"secret\" />\n" +
                    "</StackPanel>";
            return "<StackPanel Spacing=\"6\">\n" +
                "  <StackPanel Classes=\"field\">\n" +
                "    <TextBlock Classes=\"label\" Text=\"كلمة المرور\" />\n" +
                "    <TextBox x:Name=\"Password\" PasswordChar=\"•\" Text=\"secret\" />\n" +
                "  </StackPanel>\n" +
                "  <ToggleButton Theme=\"{DynamicResource InsTextBoxRevealButton}\" HorizontalAlignment=\"Left\"\n" +
                "                AutomationProperties.Name=\"إظهار كلمة المرور\"\n" +
                "                IsChecked=\"{Binding #Password.RevealPassword, Mode=TwoWay}\" />\n" +
                "</StackPanel>";
        },
        Notes = s =>
        {
            var notes = new List<string> { "اضغط العين، فتظهر كلمة المرور، ثم اضغطها ثانية لتخفيها." };
            if (s.Has("target")) notes.Add("الزر هنا خارج الحقل، فيربطه باسمه.");
            return notes;
        },
    };

    public static PlayDef Stepper { get; } = new()
    {
        Id = "stepper",
        Title = "العداد",
        Controls =
        [
            Seg("step", "الخطوة", "",
                Choice("", "1"),
                Choice("5", "5"),
                Choice("0.5", "0.5")),
            Switch("limits", "حد أدنى وأعلى", "Minimum", "ومعها `Maximum`: لا يتجاوزهما الزران، ويتعطل كل منهما عند حده.", OptionKind.Property, on: true),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يعطل الحقل وزريه معًا.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "NumericUpDown", "حقل رقمي بين زرين، ينقصه الأول ويزيده الثاني، وتزيده الأسهم وعجلة الفأرة أيضًا. وقيمته في `Value`.", OptionKind.Element, true),
            Row("الخطوة", "Increment", "مقدار الزيادة والإنقاص في كل ضغطة، وهو 1 إن لم تذكره.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var value = s["step"] switch { "5" => "5", "0.5" => "2.5", _ => "2" };
            var attrs = $" Value=\"{value}\"" + (s.On("limits") ? " Minimum=\"0\" Maximum=\"10\"" : "") + (s.Has("step") ? $" Increment=\"{s["step"]}\"" : "") + (s.On("disabled") ? " IsEnabled=\"False\"" : "");
            return "<StackPanel Classes=\"field\">\n" +
                "  <TextBlock Classes=\"label\" Text=\"الكمية\" />\n" +
                $"  <NumericUpDown Width=\"137\"{attrs} />\n" +
                "</StackPanel>";
        },
    };

    public static PlayDef[] All { get; } = [Group, Password, Stepper];
}
