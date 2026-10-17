using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ToastsPlay
{
    public const string Example =
        """
        <Button Classes="secondary" Content="نجاح" ins:Toast.Text="تم الحفظ" ins:Toast.Tone="Ok" />
        <Button Classes="secondary" Content="خطأ" ins:Toast.Text="حدث خطأ" ins:Toast.Tone="Bad" />
        <Button Classes="secondary" Content="تحذير" ins:Toast.Text="انتبه" ins:Toast.Tone="Warn" />
        <Button Classes="secondary" Content="معلومة" ins:Toast.Text="للعلم" ins:Toast.Tone="Info" />
        """;

    public static string Code { get; } =
        """
        try
        {
            await SaveAsync();
            Toast.Show(this, "تم الحفظ", ToastTone.Ok);
        }
        catch
        {
            Toast.Show(this, "تعذر الحفظ", ToastTone.Bad);
        }
        """;

    private static readonly string[] Tones = ["Ok", "Info", "Warn", "Bad"];

    public static PlayDef Toast { get; } = new()
    {
        Id = "toast",
        Title = "الرسالة العابرة",
        Controls =
        [
            Seg("how", "تظهر", "attr",
                Choice("attr", "بخاصية على الزر", "ins:Toast.Text", "ضعها على زر فتظهر الرسالة حين يُضغط، وقيمتها نص الرسالة. ومن الشيفرة تستدعي `Toast.Show(this, text, tone)`. تطل الرسالة من الزاوية السفلى في جهة النهاية، وتغيب وحدها بعد خمس ثوانٍ.", OptionKind.Property),
                Choice("code", "من الشيفرة")),
            Select("tone", "النبرة", "Ok",
                Choice("Ok", "نجاح", "ins:Toast.Tone=\"Ok\"", "لما تم للتو بفعل المستخدم: حُفظ، أُرسل، نُسخ.", OptionKind.Property),
                Choice("Info", "معلومة", "ins:Toast.Tone=\"Info\"", "لمعلومة عابرة، وهي النبرة إن لم تذكر غيرها.", OptionKind.Property),
                Choice("Warn", "تحذير", "ins:Toast.Tone=\"Warn\"", "لما يستحق الانتباه ولا يوقف العمل.", OptionKind.Property),
                Choice("Bad", "خطأ", "ins:Toast.Tone=\"Bad\"", "لعملية تعذرت، وتبقى رسالتها حتى تُغلق.", OptionKind.Property)),
            Seg("close", "الإغلاق", "",
                Choice("", "حسب النبرة"),
                Choice("Timer", "وحدها", "ins:Toast.Close=\"Timer\"", "تغيب الرسالة وحدها بعد `Toast.Duration`، ولا زر إغلاق فيها حتى لو كانت خطأ. وبدون هذه الخاصية يبقى الخطأ حتى يُغلق وتغيب الرسائل الأخرى وحدها.", OptionKind.Property),
                Choice("Button", "بزر الإغلاق", "ins:Toast.Close=\"Button\"", "تبقى الرسالة حتى يُضغط زر إغلاقها أو تغلقها لوحة المفاتيح، حتى لو كانت معلومة.", OptionKind.Property)),
            Text("text", "النص", "تم الحفظ"),
        ],
        Extra =
        [
            Row("الأساس", "Toast.Duration", "كم تبقى الرسالة، وهي خمس ثوانٍ إن لم تغيرها.", OptionKind.Property, true),
            Row("الأساس", "Toast.FocusGesture", "المفتاح الذي ينقل التركيز إلى الرسائل ويعيده، وهو `F8`. وتتنقل بينها بالأسهم، وتغلق إحداها بـ `Esc`.", OptionKind.Property),
        ],
        Axaml = s => s["how"] == "code"
            ? "<Button x:Name=\"Show\" Classes=\"secondary\" Content=\"اعرض الرسالة\" />"
            : $"<Button Classes=\"secondary\" Content=\"اعرض الرسالة\" ins:Toast.Text=\"{s["text"]}\" ins:Toast.Tone=\"{s["tone"]}\"{(s.Has("close") ? $" ins:Toast.Close=\"{s["close"]}\"" : "")} />",
        Notes = s => s["how"] == "code"
            ? [$"اضغط الزر لترى الرسالة بخياراتك. ويعرضها في الشيفرة `Toast.Show(this, \"{s["text"]}\", ToastTone.{s["tone"]}{(s.Has("close") ? $", null, ToastClose.{s["close"]}" : "")})`."]
            : ["اضغط الزر لترى الرسالة بخياراتك."],
        Stage = (view, s, _) =>
        {
            if (s["how"] != "code") return;
            var text = s["text"].Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
            var tone = Enum.Parse<ToastTone>(Tones.Contains(s["tone"]) ? s["tone"] : "Info");
            ToastClose? close = s.Has("close") && Enum.TryParse<ToastClose>(s["close"], out var how) ? how : null;
            foreach (var b in view.GetLogicalDescendants().OfType<Button>())
                b.Click += (_, _) => Insiyab.Avalonia.Controls.Toast.Show(b, text, tone, null, close);
        },
    };

    public static PlayDef[] All { get; } = [Toast];
}
