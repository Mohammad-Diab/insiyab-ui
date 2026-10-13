using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class OtpPlay
{
    public const string Example =
        """
        <StackPanel Classes="field">
          <TextBlock Classes="label" Text="رمز التحقق" />
          <ins:OtpBox Length="6" AutomationProperties.Name="رمز التحقق" />
          <TextBlock Classes="hint" Text="رمز من ستة أرقام." />
        </StackPanel>
        """;

    public static string Code { get; } =
        """
        CodeBox.Completed += async (_, e) =>         // اكتمل الرمز
        {
            if (await VerifyAsync(e.Value)) return;
            CodeBox.IsInvalid = true;                // يحمر الحقل، ويعود مع الكتابة
            CodeBox.SetCode("");                     // وافرغ الحقل لمحاولة جديدة
        };

        var code = CodeBox.Text;                     // الرمز المكتوب
        CodeBox.SetCode("123456");                   // ضبطه بهدوء، دون حدث
        """;

    public static PlayDef Otp { get; } = new()
    {
        Id = "otp",
        Title = "حقل الرمز",
        Controls =
        [
            Seg("size", "عدد الخانات", "6",
                Choice("4", "4"),
                Choice("6", "6"),
                Choice("8", "8")),
            Switch("alnum", "حروف وأرقام", "Alphanumeric=\"True\"", "لرمز فيه حروف لاتينية وأرقام، فتظهر حروفه كبيرة. والأرقام وحدها هي الأصل.", OptionKind.Property),
            Switch("disabled", "معطل", "IsEnabled=\"False\"", "يبهت الحقل ولا يُكتب فيه.", OptionKind.Property),
            Switch("submit", "يُرسل وحده", "SubmitCommand", "أمر يُستدعى بالرمز حين تمتلئ آخر خانة.", OptionKind.Property),
            Switch("invalid", "رمز مرفوض", "IsInvalid=\"True\"", "رمز رفضه الخادم، فيحمر الحقل حتى يعود صاحبه إلى الكتابة.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:OtpBox", "حقل نصي واحد مرسوم خانات، تحت كل خانة نقطة تمتلئ بلون الهوية كلما كُتب رقم. فيعمل معه اللصق واقتراح الهاتف للرمز وقارئ الشاشة كما تعمل مع أي حقل، وتسميته عليك بـ `AutomationProperties.Name`.", OptionKind.Element, true),
            Row("الأساس", "Length", "عدد الخانات، من 4 إلى 10، وهو ستة إن لم تذكره.", OptionKind.Property),
            Row("الأساس", "Completed", "يُطلق حين يكتمل الرمز، والرمز في `Value`.", OptionKind.Event),
        ],
        Disable = s => s.On("disabled") ? ["invalid", "submit"] : [],
        Axaml = s =>
            "<StackPanel Classes=\"field\">\n" +
            "  <TextBlock Classes=\"label\" Text=\"رمز التحقق\" />\n" +
            $"  <ins:OtpBox Length=\"{s["size"]}\"{(s.On("alnum") ? " Alphanumeric=\"True\"" : "")}" +
            (s.On("submit") && !s.On("disabled") ? " SubmitCommand=\"{Binding SendCode}\"" : "") +
            (s.On("invalid") && !s.On("disabled") ? " IsInvalid=\"True\"" : "") +
            (s.On("disabled") ? " IsEnabled=\"False\"" : "") + " AutomationProperties.Name=\"رمز التحقق\" />\n" +
            "</StackPanel>",
        Notes = s =>
        {
            var notes = new List<string>();
            if (s.On("submit") && !s.On("disabled")) notes.Add("املأ الخانات، فيُستدعى الأمر وحده. والإرسال هنا رسالة عابرة لا أكثر.");
            if (s.On("invalid") && !s.On("disabled")) notes.Add("اكتب رقمًا، فيعود الحقل إلى لونه.");
            return notes;
        },
        Stage = (view, s, say) =>
        {
            foreach (var box in view.GetLogicalDescendants().OfType<OtpBox>())
            {
                box.SetCode(s.On("alnum") ? "A7K" : "482");
                box.Completed += (_, e) => say($"حدث `Completed`، و`Value` هو `{e.Value}`");
                if (s.On("submit") && !s.On("disabled"))
                    box.SubmitCommand = new Send(code =>
                    {
                        Toast.Show(box, "أُرسل " + code, ToastTone.Ok);
                        box.SetCode("");
                    });
            }
        },
    };

    private sealed class Send(Action<string> run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run(parameter as string ?? "");
    }

    /// <summary>The right code in the main example is 123456; any other is turned down.</summary>
    public static Action<Control> ExampleStage { get; } = view =>
    {
        var parts = view.GetLogicalDescendants().ToList();
        var box = parts.OfType<OtpBox>().First();
        var hint = parts.OfType<TextBlock>().First(t => t.Classes.Contains("hint"));
        box.Completed += (_, e) =>
        {
            if (e.Value == "123456") hint.Text = "الرمز صحيح.";
            else
            {
                box.IsInvalid = true;
                hint.Text = "الرمز غير صحيح.";
            }
        };
    };

    public static PlayDef[] All { get; } = [Otp];
}
