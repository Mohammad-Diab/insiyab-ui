using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class PalettePlay
{
    public const string Example =
        """
        <Button Classes="secondary" Content="فتح">
          <Button.Resources>
            <ins:CommandPalette x:Key="Palette">
              <ins:PaletteItem Label="الأزرار" Group="الصفحات" Keywords="buttons" />
              <ins:PaletteItem Label="الجداول" Group="الصفحات" Keywords="tables" />
              <ins:PaletteItem Label="الحوار" Group="الصفحات" Keywords="dialog" />
              <ins:PaletteItem Label="حفظ" Group="إجراءات" Keywords="save" Hint="Ctrl S" />
            </ins:CommandPalette>
          </Button.Resources>
        </Button>
        """;

    public static string Code { get; } =
        """
        Palette.Add(
            new PaletteItem { Label = "فاتورة جديدة", Group = "إجراءات", Hint = "N",
                Keywords = "new invoice", Run = OpenInvoiceForm },
            new PaletteItem { Label = "التقارير", Group = "الصفحات", Command = GoTo, CommandParameter = "/reports" });
                                                // يعيد العناصر، وتحذف أحدها من Palette.Items

        Palette.Open(this);                     // أو Close() أو Toggle(this)
        var keys = Palette.AttachTo(TopLevel.GetTopLevel(this)!);  // Ctrl+K و«/» في النافذة كلها

        Palette.Choosing += (_, e) =>
        {
            Debug.WriteLine($"{e.Label} {e.Key}");
            // e.Cancel = true يوقف العنصر
        };
        """;

    /// <summary>Opens the palette a button keeps in its resources when the button is pressed, and reports each choice.</summary>
    public static void Wire(Control root, Action<string>? say)
    {
        foreach (var b in root.GetLogicalDescendants().OfType<Button>().ToList())
        {
            if (!b.Resources.TryGetValue("Palette", out var found) || found is not CommandPalette palette) continue;
            b.Click += (_, _) => palette.Open(b);
            foreach (var item in palette.Items.Where(i => i.Label == "حفظ")) item.Run = () => Toast.Show(b, "حُفظ", ToastTone.Ok);
            if (say is not null) palette.Choosing += (_, e) => say($"حدث `Choosing` على «{e.Label}»");
        }
    }

    public static PlayDef Palette { get; } = new()
    {
        Id = "palette",
        Title = "لوحة الأوامر",
        Controls =
        [
            Switch("groups", "في مجموعات", "Group", "اسم مجموعة العنصر، ويظهر عنوانًا لها. وفي أثناء البحث تجتمع النتائج في قائمة واحدة، ويحمل كل سطر اسم مجموعته.", OptionKind.Property, on: true),
            Switch("hint", "اختصار في السطر", "Hint", "اختصار يظهر في آخر سطر العنصر.", OptionKind.Property, on: true),
            Seg("keys", "اختصار اللوحة", "none",
                Choice("none", "بلا"),
                Choice("alt", "Alt+K", "HotKey", "على الزر الذي يفتح اللوحة: اختصار يضغطه. أما `AttachTo` فيفتحها بـ `Ctrl+K` و«/» في النافذة كلها، وتغيرها في `Gestures`.", OptionKind.Property)),
            Seg("recent", "الأخيرة", "",
                Choice("", "خمسة"),
                Choice("3", "ثلاثة", "RecentLimit", "قبل أن تكتب شيئًا تعرض اللوحة آخر ما اخترت، الأحدث فالأقدم، وهي خمسة ما لم تحدد عددها هنا. ومفاتيحها في `RecentKeys`، فاحفظها إن أردت أن تبقى.", OptionKind.Property),
                Choice("0", "بلا", "RecentLimit=\"0\"", "يخفي «الأخيرة».", OptionKind.Property)),
            Text("place", "نص الحقل الفارغ", ""),
        ],
        Extra =
        [
            Row("الأساس", "ins:CommandPalette", "لوحة تملؤها بعناصر، فتبني حولها حقل البحث والقائمة والعناوين والتنقل بلوحة المفاتيح. وتفتحها بـ `Open(this)`، واختيار العنصر كالنقر عليه، وتُغلق اللوحة قبل ذلك.", OptionKind.Element, true),
            Row("الأساس", "PlaceholderText", "النص الظاهر في حقل البحث وهو فارغ.", OptionKind.Property),
            Row("العناصر", "ins:PaletteItem", "صفحة أو أمر، واسمه في `Label`، ويعمل حين يُختار `Command` ثم `Run`.", OptionKind.Element),
            Row("العناصر", "Keywords", "كلمات يشملها البحث ولا تظهر.", OptionKind.Property),
            Row("العناصر", "Key", "ما يُحفظ به العنصر في «الأخيرة»، وإن غاب فاسمه.", OptionKind.Property),
            Row("العناصر", "IsCurrent", "الصفحة التي أنت فيها، بخط أثقل.", OptionKind.Property),
            Row("الأحداث", "Choosing", "يُطلق عند كل اختيار قبل أن يعمل العنصر، وفيه `Label` و`Key` والعنصر `Item`. و`Cancel = true` يوقف العنصر.", OptionKind.Event),
        ],
        Axaml = s =>
        {
            var attrs = (s.Has("recent") ? $" RecentLimit=\"{s["recent"]}\"" : "") + (s.Has("place") ? $" PlaceholderText=\"{s["place"]}\"" : "");
            string Item(string label, string group, string words, string hint = "") =>
                $"      <ins:PaletteItem Label=\"{label}\"{(s.On("groups") ? $" Group=\"{group}\"" : "")} Keywords=\"{words}\"{hint} />";
            var items = new[]
            {
                Item("الملفات", "الصفحات", "files"), Item("الإعدادات", "الصفحات", "settings"),
                Item("الرسائل", "الصفحات", "messages"), Item("التقارير", "الصفحات", "reports"),
                Item("حفظ", "إجراءات", "save", s.On("hint") ? " Hint=\"Ctrl S\"" : ""),
            };
            return $"<Button Classes=\"secondary\" Content=\"فتح\"{(s["keys"] == "alt" ? " HotKey=\"Alt+K\"" : "")}>\n" +
                "  <Button.Resources>\n" +
                $"    <ins:CommandPalette x:Key=\"Palette\"{attrs}>\n" +
                string.Join("\n", items) + "\n" +
                "    </ins:CommandPalette>\n" +
                "  </Button.Resources>\n" +
                "</Button>";
        },
        Notes = s => [$"اضغط «فتح»{(s["keys"] == "alt" ? " أو Alt+K" : "")} لترى اللوحة بخياراتك، واختر منها عنصرًا ثم افتحها ثانية لترى «الأخيرة». ويفتحها الزر في الشيفرة بـ `Palette.Open(this)`."],
        Stage = (view, _, say) => Wire(view, say),
    };

    public static PlayDef[] All { get; } = [Palette];
}
