using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class PhonePlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="الهاتف" />
          <ins:PhoneBox Value="+966501234567" />
          <TextBlock Classes="hint" />
        </StackPanel>
        """;

    public static string Code { get; } =
        """
        var value = Mobile.Value;                   // "+966501234567"
        var country = Mobile.Country;               // "SA"
        var valid = Mobile.IsValid;                 // true
        Mobile.Value = "+97142345678";              // ضبطه، والدولة معه، بهدوء

        Mobile.PhoneChanged += (_, e) =>
            Debug.WriteLine($"{e.Value} {e.Country} {e.IsValid}");   // حين تتغير القيمة
        """;

    public static PlayDef Phone { get; } = new()
    {
        Id = "phone",
        Title = "حقل الهاتف",
        Wide = _ => true,
        WideMax = 416,
        Controls =
        [
            Switch("value", "فيه رقم", "Value=\"+966501234567\"", "الرقم بصيغة E.164، يبدأ به الحقل ودولته معه، وتغلب دولته `DefaultCountry`. ويحمله الحقل كذلك أيًّا كانت صورة ما كُتب.", OptionKind.Property),
            Select("country", "الدولة الأولى", "",
                Choice("", "السعودية"),
                Choice("AE", "الإمارات", "DefaultCountry=\"AE\"", "الدولة التي يبدأ بها الحقل، فإن غابت فدولة الرقم نفسه، وإلا فالسعودية.", OptionKind.Property),
                Choice("KW", "الكويت"),
                Choice("QA", "قطر")),
            Switch("only", "دول محددة وحدها", "OnlyCountries=\"SA,AE,KW,QA,BH,OM\"", "لا يعرض الحقل إلا الدول التي تكتب رموزها، أيًّا كانت، ويرفض رقمًا من غيرها برسالة. والمثال هنا دول الخليج.", OptionKind.Property),
            Switch("preferred", "دول تتصدر القائمة", "PreferredCountries=\"EG,JO,SY,LB\"", "دول في أول القائمة، فوق الباقي.", OptionKind.Property),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يتعطل معه زر الدولة.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:PhoneBox", "حقل في أوله زر يعرض رمز الدولة ومفتاحها ويفتح قائمة الدول، ويكتب الرقم كما يكتبه أهل بلده، مثل `050 123 4567`.", OptionKind.Element, true),
            Row("الأساس", "PlaceholderText", "إن تركته وضع الحقل فيه مثالًا من الدولة المختارة.", OptionKind.Property),
            Row("الأساس", "IsValid", "صلاح الرقم لطول أرقام دولته، تقرؤه ولا تكتبه، ورسالته في `ValidationMessage`.", OptionKind.Property),
            Row("الأساس", "PhoneChanged", "يُطلق حين تتغير القيمة، وفيه الرقم `Value` والدولة `Country` وصلاحه `IsValid`.", OptionKind.Event),
        ],
        Disable = s => (s.On("value") ? new[] { "country" } : []).Concat(s.On("only") ? ["preferred"] : []),
        Axaml = s =>
        {
            var attrs = (s.On("value") ? " Value=\"+966501234567\"" : s.Has("country") ? $" DefaultCountry=\"{s["country"]}\"" : "") +
                (s.On("only") ? " OnlyCountries=\"SA,AE,KW,QA,BH,OM\"" : "") + (s.On("preferred") && !s.On("only") ? " PreferredCountries=\"EG,JO,SY,LB\"" : "") +
                (s.On("disabled") ? " IsEnabled=\"False\"" : "");
            return "<StackPanel Classes=\"field\">\n" +
                "  <TextBlock Classes=\"label\" Text=\"الهاتف\" />\n" +
                $"  <ins:PhoneBox{attrs} />\n" +
                "</StackPanel>";
        },
        Notes = s => s.On("disabled") ? [] :
            [s.On("value") ? "الرقم يختار دولته بنفسه، فلا حاجة إلى الدولة الأولى. واضغط زر الدولة لترى القائمة بخياراتك." : "اضغط زر الدولة لترى القائمة بخياراتك."],
        Stage = (view, _, say) =>
        {
            foreach (var p in view.GetLogicalDescendants().OfType<PhoneBox>())
                p.PhoneChanged += (_, e) => say($"حدث `PhoneChanged`: `{(e.Value.Length > 0 ? e.Value : "—")}` و`{e.Country}`، وصالح: `{e.IsValid}`");
        },
    };

    public static PlayDef[] All { get; } = [Phone];
}
