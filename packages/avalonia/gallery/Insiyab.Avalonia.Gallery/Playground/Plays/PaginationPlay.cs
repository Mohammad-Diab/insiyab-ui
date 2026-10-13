using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class PaginationPlay
{
    public const string Example =
        """
        <ins:Pagination PageCount="12" CurrentPage="3" />
        """;

    public static PlayDef Pages { get; } = new()
    {
        Id = "pagination",
        Title = "الترقيم",
        Controls =
        [
            Seg("at", "الصفحة الحالية", "3",
                Choice("1", "الأولى"),
                Choice("3", "في الوسط"),
                Choice("12", "الأخيرة")),
            Switch("compact", "مختصر", "IsCompact=\"True\"", "يكتفي بالسهمين وبينهما موضعك، مثل «3 من 12»، فيناسب الهاتف والقائمة الجانبية.", OptionKind.Property, "الترقيم المختصر"),
        ],
        Extra =
        [
            Row("الأساس", "ins:Pagination", "يبني أزرار الصفحات وحده: «السابق» و«التالي» بسهميهما، والصفحة الأولى والأخيرة، وما حول الحالية، وفجوات بينها.", OptionKind.Element, true),
            Row("الأساس", "PageCount", "عدد الصفحات.", OptionKind.Property),
            Row("الأساس", "CurrentPage", "الصفحة الحالية، من 1. وفي الأولى يتعطل «السابق»، وفي الأخيرة «التالي».", OptionKind.Property),
            Row("الأساس", "PreviousText", "نص «السابق»، ويشير سهمه إلى اليمين في الواجهة العربية. ومعه `NextText` لنص «التالي».", OptionKind.Property),
            Row("الأساس", "PageChanged", "يُطلق حين تتغير الصفحة الحالية بزر أو من الشيفرة، والصفحة الجديدة في `CurrentPage`.", OptionKind.Event),
            Row("الترقيم المختصر", "CompactFormat", "صيغة الموضع في الترقيم المختصر: `{0}` للصفحة الحالية و`{1}` للعدد.", OptionKind.Property),
        ],
        Axaml = s => $"<ins:Pagination PageCount=\"12\" CurrentPage=\"{s["at"]}\"{(s.On("compact") ? " IsCompact=\"True\"" : "")} />",
        Notes = s => s["at"] switch
        {
            "1" => ["لا صفحة قبل الأولى، فجاء «السابق» معطلًا. وانقر صفحة يظهر هنا الحدث."],
            "12" => ["لا صفحة بعد الأخيرة، فجاء «التالي» معطلًا. وانقر صفحة يظهر هنا الحدث."],
            _ => ["انقر صفحة أو سهمًا فتنتقل إليها، ويظهر هنا الحدث."],
        },
        Stage = (view, _, say) =>
        {
            foreach (var p in view.GetLogicalDescendants().OfType<Pagination>())
                p.PageChanged += (_, _) => say($"حدث `PageChanged`، و`CurrentPage` الآن `{p.CurrentPage}`");
        },
    };

    public static PlayDef[] All { get; } = [Pages];
}
