using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class ScrollspyPlay
{
    private static string Box(string title, bool sub, string offset) =>
        "<Grid ColumnDefinitions=\"*,20,176\">\n" +
        "  <ScrollViewer Height=\"256\">\n" +
        "    <StackPanel>\n" +
        string.Concat(new[] { "القسم الأول", "القسم الثاني", "قسم فرعي", "القسم الثالث" }.Select((t, i) =>
            $"      <Border x:Name=\"Sec{i + 1}\" MinHeight=\"208\">\n" +
            $"        <TextBlock Text=\"{t}\" FontSize=\"16\" FontWeight=\"Bold\" />\n" +
            "      </Border>\n")) +
        "    </StackPanel>\n" +
        "  </ScrollViewer>\n" +
        $"  <ins:Toc Grid.Column=\"2\"{title}{offset} AutomationProperties.Name=\"المحتويات\">\n" +
        "    <ins:TocLink Content=\"القسم الأول\" Section=\"{Binding #Sec1}\" />\n" +
        "    <ins:TocLink Content=\"القسم الثاني\" Section=\"{Binding #Sec2}\" />\n" +
        $"    <ins:TocLink{(sub ? " Classes=\"sub\"" : "")} Content=\"قسم فرعي\" Section=\"{{Binding #Sec3}}\" />\n" +
        "    <ins:TocLink Content=\"القسم الثالث\" Section=\"{Binding #Sec4}\" />\n" +
        "  </ins:Toc>\n" +
        "</Grid>";

    public static string Example { get; } = Box(" Title=\"المحتويات\"", true, "");

    public static string Code { get; } =
        """
        Contents.ActiveChanged += (_, e) =>
            Debug.WriteLine($"{e.Link?.Content} {e.Section}");   // الرابط الحالي وقسمه

        var link = Contents.Refresh();   // أعد القياس بعد أن كبر محتوى فوق الأقسام، ويعيد الرابط الحالي
        """;

    public static PlayDef Toc { get; } = new()
    {
        Id = "toc",
        Title = "قائمة المحتويات",
        Wide = _ => true,
        Controls =
        [
            Switch("title", "عنوان القائمة", "Title", "عنوان صغير في أول القائمة.", OptionKind.Property, on: true),
            Switch("sub", "رابط إلى عنوان أعمق", "sub", "على `ins:TocLink` إلى عنوان أعمق، فيتأخر خطوة عن غيره.", on: true),
            Seg("offset", "خط القسم الحالي", "",
                Choice("", "ثلث المنطقة"),
                Choice("40", "40 بكسلًا", "Offset", "بُعد الخط الذي يُعرف به القسم الحالي عن أعلى منطقة التمرير، بالبكسل، بدل ثلثها. لصفحة شريطها العلوي الثابت طويل.", OptionKind.Property)),
        ],
        Extra =
        [
            Row("الأساس", "ins:Toc", "قائمة المحتويات: يتميز فيها رابط القسم الذي يُقرأ، وتنتقل إليه علامة من لون الهوية كما في الشريط الجانبي.", OptionKind.Element, true),
            Row("الأساس", "ins:TocLink", "رابط إلى قسم، والقسم في `Section`. والنقر عليه يمرر إلى قسمه ويجعله الحالي في الحال.", OptionKind.Element),
            Row("الأساس", "ScrollViewer", "منطقة التمرير، وإن تركتها فأقرب `ScrollViewer` حول القسم الأول.", OptionKind.Property),
            Row("الأساس", "ActiveChanged", "يُطلق كلما تغير الرابط الحالي، والرابط في `Link` وقسمه في `Section`.", OptionKind.Event),
        ],
        Axaml = s => Box(s.On("title") ? " Title=\"المحتويات\"" : "", s.On("sub"), s.Has("offset") ? $" Offset=\"{s["offset"]}\"" : ""),
        Notes = _ => ["الصندوق ممرر قليلًا، فغيّر الخط تنتقل العلامة."],
        Stage = (view, _, say) => Wire(view, say),
    };

    /// <summary>Scrolls the box a little once it is laid out, so the current link is not the first, and reports each change.</summary>
    /// <summary>The main example scrolls and marks its own entries.</summary>
    public static Action<Control> ExampleStage { get; } = view => Wire(view, null);

    public static void Wire(Control root, Action<string>? say)
    {
        foreach (var box in root.GetLogicalDescendants().OfType<ScrollViewer>())
            box.Loaded += (_, _) => Dispatcher.UIThread.Post(() => box.Offset = new Vector(0, 130), DispatcherPriority.Background);
        if (say is null) return;
        foreach (var toc in root.GetLogicalDescendants().OfType<Insiyab.Avalonia.Controls.Toc>())
            toc.ActiveChanged += (_, e) => say($"حدث `ActiveChanged`، والرابط الحالي «{e.Link?.Content}»");
    }

    public static PlayDef[] All { get; } = [Toc];
}
