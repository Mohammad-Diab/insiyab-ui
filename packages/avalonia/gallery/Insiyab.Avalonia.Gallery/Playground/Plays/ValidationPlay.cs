using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ValidationPlay
{
    public const string Example =
        """
        <StackPanel ins:Validate.Form="True">
          <StackPanel Spacing="16">
            <StackPanel Classes="field">
              <TextBlock Classes="label" Text="البريد" />
              <TextBox FlowDirection="LeftToRight" ins:Validate.Required="True" ins:Validate.Email="True" />
            </StackPanel>
            <StackPanel Classes="field">
              <TextBlock Classes="label" Text="كلمة المرور" />
              <TextBox PasswordChar="•" FlowDirection="LeftToRight" ins:Validate.Required="True" ins:Validate.MinLength="8" ins:Validate.Message="كلمة المرور قصيرة." />
              <TextBlock Classes="hint" Text="ثمانية أحرف على الأقل." />
            </StackPanel>
          </StackPanel>
          <StackPanel Classes="form-commit">
            <Button Classes="primary" Content="إرسال" ins:Validate.Submit="True" />
          </StackPanel>
        </StackPanel>
        """;

    public static PlayDef Rules { get; } = new()
    {
        Id = "validate",
        Title = "قيود الحقول",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Select("rule", "القيد", "required",
                Choice("required", "مطلوب", "ins:Validate.Required", "لا يُترك الحقل فارغًا.", OptionKind.Property),
                Choice("email", "بريد صحيح", "ins:Validate.Email", "بريد على صورته المعروفة، `name@example.com`.", OptionKind.Property),
                Choice("minlength", "ثمانية أحرف على الأقل", "ins:Validate.MinLength", "أقل عدد من الأحرف.", OptionKind.Property),
                Choice("pattern", "أرقام فقط", "ins:Validate.Pattern", "تعبير نمطي تطابقه القيمة كلها، مثل `[0-9]+`.", OptionKind.Property)),
            Seg("msg", "الرسالة", "",
                Choice("", "رسالة المكتبة"),
                Choice("own", "رسالتك", "ins:Validate.Message", "رسالتك بدل رسالة المكتبة، لأي قيد يخالفه الحقل.", OptionKind.Property)),
            Switch("hint", "تلميح"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Validate.Form", "على الحاوية التي تضم الحقول: هي النموذج الذي يفحصه زر الإرسال. ولا يظهر الخطأ إلا إذا غادر المستخدم حقلًا غيّره أو حاول الإرسال.", OptionKind.Property, true),
            Row("الأساس", "ins:Validate.Submit", "على زر الإرسال: يفحص النموذج أولًا، ويوقف النقرة وأمرها ما دام فيه حقل غير صالح، وينقل التركيز إلى أوله.", OptionKind.Property, true),
            Row("الأساس", "ins:Validate.Submitted", "يُطلق على النموذج إذا وجد زر الإرسال حقوله كلها صالحة.", OptionKind.Event),
            Row("الأساس", "Validate.Check(form)", "يفحص النموذج من الشيفرة كما يفحصه زر الإرسال، ويقول هل حقوله كلها صالحة. و`Validate.Reset(form)` تمحو رسائله.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var (label, attrs, own, hint) = s["rule"] switch
            {
                "email" => ("البريد", " FlowDirection=\"LeftToRight\" ins:Validate.Required=\"True\" ins:Validate.Email=\"True\"", "اكتب بريدًا صحيحًا.", "تلميح قصير."),
                "minlength" => ("كلمة المرور", " PasswordChar=\"•\" FlowDirection=\"LeftToRight\" ins:Validate.Required=\"True\" ins:Validate.MinLength=\"8\"", "كلمة المرور قصيرة.", "ثمانية أحرف على الأقل."),
                "pattern" => ("الرمز", " ins:Validate.Required=\"True\" ins:Validate.Pattern=\"[0-9]+\"", "اكتب أرقامًا فقط.", "تلميح قصير."),
                _ => ("الاسم", " ins:Validate.Required=\"True\"", "اكتب الاسم.", "تلميح قصير."),
            };
            if (s.Has("msg")) attrs += $" ins:Validate.Message=\"{own}\"";
            return "<StackPanel ins:Validate.Form=\"True\">\n" +
                "  <StackPanel Classes=\"field\">\n" +
                $"    <TextBlock Classes=\"label\" Text=\"{label}\" />\n" +
                $"    <TextBox{attrs} />\n" +
                (s.On("hint") ? $"    <TextBlock Classes=\"hint\" Text=\"{hint}\" />\n" : "") +
                "  </StackPanel>\n" +
                "  <StackPanel Classes=\"form-commit\">\n" +
                "    <Button Classes=\"primary\" Content=\"إرسال\" ins:Validate.Submit=\"True\" />\n" +
                "  </StackPanel>\n" +
                "</StackPanel>";
        },
        Notes = _ => ["هكذا يبدو الحقل بعد محاولة إرسال فاشلة. اكتب فيه، فتتغير الرسالة أو تزول."],
        Stage = (view, _, _) =>
        {
            var form = view.GetLogicalDescendants().OfType<Control>().FirstOrDefault(Validate.GetForm);
            if (form is null) return;
            // Check focuses the first bad field, so the control that was just changed gets its focus back.
            var focused = TopLevel.GetTopLevel(view)?.FocusManager?.GetFocusedElement() as InputElement;
            Validate.Check(form);
            focused?.Focus();
        },
    };

    public static PlayDef Server { get; } = new()
    {
        Id = "server",
        Title = "أخطاء من الخادم",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Seg("state", "الحالة", "bad",
                Choice("bad", "خطأ", "bad", "على الحقل أو القائمة، ومعه رسالة في `ContentControl.error`: خطأ لا تعرفه القيود، كاسم محجوز."),
                Choice("ok", "مقبول", "ok", "على الحقل أو القائمة، ومعه رسالة في `ContentControl.success`: قيمة تستحق التأكيد، كاسم متاح.")),
            Seg("el", "العنصر", "input",
                Choice("input", "حقل"),
                Choice("select", "قائمة")),
        ],
        Extra =
        [
            Row("الأساس", "ContentControl.error", "رسالة الخطأ تحت الحقل، تسبقها علامته.", OptionKind.Element, true),
            Row("الأساس", "ContentControl.success", "رسالة القبول تحت الحقل.", OptionKind.Element, true),
            Row("خيارات أخرى", "DataValidationErrors", "خطأ يأتي من ربط البيانات، أو تضعه بـ`DataValidationErrors.SetErrors(box, [\"هذا الاسم محجوز.\"])`: يلون الحقل ويكتب رسالته تحته وحده، فيغنيك عن `bad` والرسالة.", OptionKind.Property),
        ],
        Axaml = s =>
        {
            var bad = s["state"] == "bad";
            var cls = Classes(s["state"]);
            var control = s["el"] == "select"
                ? $"  <ComboBox{cls} PlaceholderText=\"اختر…\"{(bad ? "" : " SelectedIndex=\"0\"")}>\n    <ComboBoxItem Content=\"الأول\" />\n    <ComboBoxItem Content=\"الثاني\" />\n  </ComboBox>\n"
                : $"  <TextBox{cls} Text=\"name\" FlowDirection=\"LeftToRight\" />\n";
            var message = bad
                ? $"  <ContentControl Classes=\"error\" Content=\"{(s["el"] == "select" ? "اختر واحدًا." : "هذا الاسم محجوز.")}\" />\n"
                : $"  <ContentControl Classes=\"success\" Content=\"{(s["el"] == "select" ? "اختيار صحيح." : "الاسم متاح.")}\" />\n";
            return "<StackPanel Classes=\"field\">\n" +
                $"  <TextBlock Classes=\"label\" Text=\"{(s["el"] == "select" ? "الخيار" : "اسم المستخدم")}\" />\n" +
                control + message +
                "</StackPanel>";
        },
    };

    public static PlayDef[] All { get; } = [Rules, Server];
}
