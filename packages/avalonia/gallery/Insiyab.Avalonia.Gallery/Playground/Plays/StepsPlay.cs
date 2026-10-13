using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class StepsPlay
{
    public const string Example =
        """
        <ins:Steps>
          <ins:StepsItem Content="الأولى" IsDone="True" />
          <ins:StepsItem Content="الثانية" IsCurrent="True" />
          <ins:StepsItem Content="الثالثة" />
        </ins:Steps>
        """;

    public static string Server { get; } =
        """
        Wizard.Leaving += async (_, e) =>
        {
            if (Wizard.CurrentIndex != 0) return;
            e.Wait();                                  // المعالج ينتظر
            if (await CheckPhoneAsync(Phone.Text))
            {
                Wizard.Proceed();
                return;
            }
            DataValidationErrors.SetErrors(Phone, ["هذا الرقم غير مسجل"]);
            Wizard.Stay();
        };
        """;

    public static string Code { get; } =
        """
        var step = StoreWizard.CurrentIndex;          // رقم الخطوة الحالية
        StoreWizard.CurrentIndex = 2;                 // إلى الثالثة، بلا تحقق
        StoreWizard.Next();                           // أو Previous() أو Restart() أو Stay()

        StoreWizard.Moved += (_, e) => Debug.WriteLine($"{e.Step} {e.Panel.Header}");
        """;

    private static readonly string[] Names = ["الأولى", "الثانية", "الثالثة", "الرابعة"];

    public static PlayDef Steps { get; } = new()
    {
        Id = "steps",
        Title = "الخطوات",
        Wide = _ => true,
        Controls =
        [
            Seg("look", "الشكل", "",
                Choice("", "أرقام"),
                Choice("dots", "نقاط", "dots", "نقاط صغيرة مكان الأرقام تليق ببطاقة الدخول، وتبقى الأسماء لقارئ الشاشة."),
                Choice("grid", "شبكة", "grid", "أرقام تلتف على الأسطر بلا خطوط تصلها، لخطوات كثيرة لا تظهر أسماؤها، كأسئلة نموذج طويل. وتصير كل دائرة زرًا يُطلق `Click`، وتتنقل بينها الأسهم.")),
            Seg("at", "الخطوة الحالية", "2",
                Choice("1", "1"),
                Choice("2", "2"),
                Choice("3", "3"),
                Choice("4", "4")),
            Switch("flagged", "خطوة مؤجلة", "IsFlagged=\"True\"", "نقطة صفراء على خطوة مؤجلة يعود إليها المستخدم، وتجتمع مع المنجزة والحالية.", OptionKind.Property, "حالات أخرى"),
            Switch("error", "خطوة فيها خطأ", "IsError=\"True\"", "دائرة حمراء على خطوة فيها ما يحتاج إلى إصلاح، وتجتمع مع المنجزة والحالية.", OptionKind.Property, "حالات أخرى"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Steps", "خطوات مرقمة تصلها خطوط، تصلح وحدها لمسار موزع على عدة شاشات.", OptionKind.Element, true),
            Row("الأساس", "ins:StepsItem", "عنصر الخطوة، واسمها في `Content`، ورقمها يكتبه `ins:Steps` بنفسه. وعلى الهاتف لا يظهر إلا اسم الحالية، وتبقى البقية لقارئ الشاشة.", OptionKind.Element),
            Row("الأساس", "IsDone=\"True\"", "خطوة منجزة، يمتلئ رقمها بلون الهوية.", OptionKind.Property),
            Row("الأساس", "IsCurrent=\"True\"", "الخطوة الحالية، ويعلنها قارئ الشاشة.", OptionKind.Property),
        ],
        Disable = s =>
        {
            var off = s["look"] == "dots" ? new List<string> { "flagged", "error" } : [];
            if (s["at"] == "1" && !off.Contains("error")) off.Add("error");
            return off;
        },
        Axaml = s =>
        {
            var grid = s["look"] == "grid";
            var at = int.Parse(s["at"]);
            var rows = Enumerable.Range(1, grid ? 12 : 4).Select(i =>
                $"  <ins:StepsItem Content=\"{(grid ? $"البند {i}" : Names[i - 1])}\"" +
                (i < at ? " IsDone=\"True\"" : "") + (i == at ? " IsCurrent=\"True\"" : "") +
                (s.On("flagged") && s["look"] != "dots" && i == 1 ? " IsFlagged=\"True\"" : "") +
                (s.On("error") && s["look"] != "dots" && i == at - 1 ? " IsError=\"True\"" : "") + " />");
            return $"<ins:Steps{Classes(s["look"])}>\n{string.Join("\n", rows)}\n</ins:Steps>";
        },
        Notes = s => s["look"] switch
        {
            "grid" => ["تضع الصفحة الحالات بنفسها: منجزة لما أُجيب عنه، وحالية للمفتوح، ومؤجلة لما ينتظر المراجعة. وانقر بندًا يظهر هنا الحدث."],
            "dots" => ["لا تظهر النقطة المؤجلة ولا الخطأ في هذا الشكل، فالنقاط لا تسع إلا المنجز والحالي."],
            _ => [],
        },
        Stage = (view, s, say) =>
        {
            if (s["look"] != "grid") return;
            foreach (var item in view.GetLogicalDescendants().OfType<StepsItem>())
                item.Click += (_, _) => say($"حدث `Click` على «{item.Content}»");
        },
    };

    private static string Field(string pad, string label, string name, string extra) =>
        $"{pad}<StackPanel Spacing=\"6\">\n" +
        $"{pad}  <TextBlock Classes=\"label\" Text=\"{label}\" />\n" +
        $"{pad}  <TextBox x:Name=\"{name}\" ins:Wizard.Required=\"True\"{extra} />\n" +
        $"{pad}</StackPanel>";

    public static PlayDef Wizard { get; } = new()
    {
        Id = "wizard",
        Title = "المعالج",
        Wide = s => !s.On("dialog"),
        Controls =
        [
            Switch("body", "ارتفاع يتدرج", "AnimateHeight", "يتدرج ارتفاع المعالج بسلاسة بين خطوة وأخرى، وهو كذلك إن لم تذكره. اجعله `False` فيقفز الارتفاع.", OptionKind.Property, on: true),
            Switch("slide", "خطوات تنزلق", "Motion=\"Slide\"", "تنزلق الخطوات جنبًا إلى جنب بعرضها كاملًا، كما في بطاقة الدخول.", OptionKind.Property),
            Switch("skip", "تخطي خطوة", "IsSkipped=\"True\"", "على `ins:WizardPanel`: يتخطاه المعالج ويسقط رقمه من الخطوات، فيصلح لمسار يحدد الخادم طوله.", OptionKind.Property),
            Switch("focus", "تركيز على حقل", "ins:Wizard.AutoFocus=\"True\"", "على حقل في اللوح: يأخذ التركيز حين تبلغ خطوته، بدل أن يذهب إلى اللوح نفسه.", OptionKind.Property),
            Switch("echo", "عرض قيمة سابقة", "{Binding #NameField.Text}", "اربط نصًا بحقل من خطوة سابقة، كرقم الهاتف في خطوة الرمز.", OptionKind.Property),
            Switch("restart", "زر البدء من جديد", "ins:Wizard.Action=\"Restart\"", "على زر: يعيد المعالج إلى الخطوة الأولى. ومثله `Next` و`Previous` و`Finish` لزر تضعه أنت.", OptionKind.Property),
            Switch("dialog", "داخل مربع حوار", "CancelContent", "يعمل المعالج نفسه داخل `ins:Dialog`، فيضع زر الإلغاء بهذا النص في أول الشريط السفلي، وزري السابق والتالي في آخره، ويُطلق `Cancelled` إذا ضُغط.", OptionKind.Property),
        ],
        Extra =
        [
            Row("الأساس", "ins:Wizard", "نموذج مقسوم إلى ألواح، يُظهر منها لوحًا واحدًا في كل مرة، ويقود الخطوات فوقه وأزرار السابق والتالي والإرسال تحته.", OptionKind.Element, true),
            Row("الأساس", "ins:WizardPanel", "لوح الخطوة، واسمها في الخطوات `Header`.", OptionKind.Element),
            Row("الأساس", "ins:Wizard.Required=\"True\"", "على حقل: لا ينتقل «التالي» ما دام فارغًا، ومفتاح `Enter` داخل الحقل يعمل عمل «التالي».", OptionKind.Property),
            Row("الأساس", "CurrentIndex", "رقم الخطوة الحالية من صفر، بين الخطوات الظاهرة. وإن غيّرته من الشيفرة انتقل المعالج بلا تحقق.", OptionKind.Property),
            Row("الأحداث", "Leaving", "يُطلق قبل أن ينتقل «التالي». وإذا استدعى مستمعك `e.Wait()` انتظر المعالج مشغولًا حتى تستدعي `Proceed()` أو `Stay()`.", OptionKind.Event),
            Row("الأحداث", "Moved", "يُطلق بعد كل انتقال، ورقم الخطوة في `Step` ولوحها في `Panel`.", OptionKind.Event),
            Row("الأحداث", "Finished", "يُطلق حين يُضغط «إرسال» وكل الخطوات سليمة.", OptionKind.Event),
        ],
        Disable = s =>
        {
            var off = new List<string>();
            if (!s.On("body")) off.Add("slide");
            if (s.On("skip")) off.Add("focus");
            return off;
        },
        Axaml = s =>
        {
            var d = s.On("dialog") ? "      " : "";
            var p = d + "    ";
            var attrs = (s.On("body") ? "" : " AnimateHeight=\"False\"") + (s.On("body") && s.On("slide") ? " Motion=\"Slide\"" : "") +
                (s.On("dialog") ? " CancelContent=\"إلغاء\"" : "");
            var last = s.On("echo") || s.On("restart") || s.On("skip");
            var confirm = new List<string>();
            if (s.On("echo")) confirm.Add($"{p}  <TextBlock Text=\"{{Binding #NameField.Text, StringFormat='الاسم: {{0}}'}}\" />");
            confirm.Add($"{p}  <CheckBox ins:Wizard.Required=\"True\" Content=\"أوافق\" />");
            if (s.On("restart")) confirm.Add($"{p}  <Button Classes=\"bare\" Content=\"البدء من جديد\" ins:Wizard.Action=\"Restart\" HorizontalAlignment=\"Left\" />");
            var wizard =
                $"{d}<ins:Wizard CurrentIndex=\"{(last ? (s.On("skip") ? 1 : 2) : 1)}\"{attrs}>\n" +
                $"{d}  <ins:WizardPanel Header=\"الاسم\">\n" +
                Field(p, "الاسم", "NameField", " Text=\"سارة\"") + "\n" +
                $"{d}  </ins:WizardPanel>\n" +
                $"{d}  <ins:WizardPanel Header=\"البريد\"{(s.On("skip") ? " IsSkipped=\"True\"" : "")}>\n" +
                Field(p, "البريد", "EmailField", " FlowDirection=\"LeftToRight\"" + (s.On("focus") && !s.On("skip") ? " ins:Wizard.AutoFocus=\"True\"" : "")) + "\n" +
                $"{d}  </ins:WizardPanel>\n" +
                $"{d}  <ins:WizardPanel Header=\"التأكيد\">\n" +
                $"{p}<StackPanel Spacing=\"12\">\n" + string.Join("\n", confirm) + "\n" + $"{p}</StackPanel>\n" +
                $"{d}  </ins:WizardPanel>\n" +
                $"{d}</ins:Wizard>";
            if (!s.On("dialog")) return wizard;
            return "<Button Classes=\"primary\" Content=\"فتح المعالج\">\n" +
                "  <ins:Dialog.Opens>\n" +
                "    <ins:Dialog Title=\"إضافة\">\n" +
                wizard + "\n" +
                "    </ins:Dialog>\n" +
                "  </ins:Dialog.Opens>\n" +
                "</Button>";
        },
        Notes = s =>
        {
            if (s.On("dialog")) return ["اضغط «فتح المعالج» لترى المعالج بخياراتك في مربع الحوار."];
            var said = new List<string>();
            var last = s.On("echo") || s.On("restart") || s.On("skip");
            said.Add(last
                ? "المعالج في خطوته الأخيرة، فاضغط «السابق» أو «إرسال» يظهر هنا الحدث."
                : "المعالج في خطوته الثانية والبريد فارغ، فاكتبه واضغط «التالي» يظهر هنا الحدث.");
            if (!s.On("body")) said.Add("يقفز ارتفاع المعالج بين الخطوات ولا يتدرج، فاضغط «السابق» لترى ذلك.");
            if (s.On("slide") && s.On("body")) said.Add("اضغط «السابق» أو «التالي» فتنزلق الخطوات جنبًا إلى جنب.");
            if (s.On("focus") && !s.On("skip")) said.Add("اضغط «السابق» ثم «التالي»، فيأخذ حقل البريد التركيز حين تبلغ خطوته.");
            if (s.On("skip")) said.Add("تخطى المعالج لوح البريد وسقط رقمه من الخطوات.");
            return said;
        },
        Stage = (view, s, say) =>
        {
            var all = view.GetLogicalDescendants().ToList();
            if (s.On("dialog") && all.OfType<Button>().FirstOrDefault() is { } open && Dialog.GetOpens(open) is { } dialog)
                all = dialog.GetLogicalDescendants().ToList();
            if (all.OfType<Insiyab.Avalonia.Controls.Wizard>().FirstOrDefault() is not { } w) return;
            if (!s.On("echo") && !s.On("restart") && !s.On("skip") && all.OfType<TextBox>().FirstOrDefault(t => t.Name == "EmailField") is { } email)
                DataValidationErrors.SetErrors(email, ["هذا الحقل مطلوب."]);
            w.Moved += (_, e) => say($"حدث `Moved`، والخطوة الآن `{e.Step}`");
            w.Finished += (_, _) => { say("حدث `Finished`، فكل الخطوات سليمة"); w.Restart(); };
            w.Cancelled += (_, _) => say("حدث `Cancelled`");
        },
    };

    public static PlayDef[] All { get; } = [Steps, Wizard];
}
