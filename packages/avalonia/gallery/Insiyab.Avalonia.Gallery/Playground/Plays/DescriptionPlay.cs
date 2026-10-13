using static Insiyab.Avalonia.Gallery.Playground.Play;

namespace Insiyab.Avalonia.Gallery.Playground;

public static class DescriptionPlay
{
    public const string Example =
        """
        <ins:DescriptionList>
          <ins:DescriptionItem Term="رقم الطلب" Content="1042" Classes="num" />
          <ins:DescriptionItem Term="العميل" Content="ليلى حداد" />
          <ins:DescriptionItem Term="المدينة" Content="الرياض" />
          <ins:DescriptionItem Term="الحالة" Content="قيد التجهيز" />
          <ins:DescriptionItem Term="ملاحظات" Content="التسليم بعد الساعة الرابعة، والاتصال قبل الوصول." />
        </ins:DescriptionList>
        """;

    public static PlayDef List { get; } = new()
    {
        Id = "dl",
        Title = "قائمة الوصف",
        Wide = _ => true,
        Controls =
        [
            Switch("grid", "في أعمدة", "grid", "يوزع الأزواج على أعمدة تعلو فيها التسمية قيمتها، لملخص يجمع حقائق قصيرة كثيرة."),
            Seg("count", "عدد الأزواج", "3",
                Choice("3", "3"),
                Choice("6", "6")),
            Switch("num", "محاذاة الرقم", "num", "على `ins:DescriptionItem` يحمل رقمًا، فيبقى على اتجاهه فلا تنتقل إشارة السالب إلى آخره، ويبدأ من حيث تبدأ القيم الأخرى.", on: true),
        ],
        Extra =
        [
            Row("الأساس", "ins:DescriptionList", "تفاصيل سجل واحد: عمود من التسميات والقيم بجوارها.", OptionKind.Element, true),
            Row("الأساس", "ins:DescriptionItem", "زوج واحد: التسمية في `Term` والقيمة في `Content`، نصًا كانت أو عنصرًا.", OptionKind.Element, true),
        ],
        Axaml = s =>
        {
            (string Term, string Value, bool Num)[] pairs =
            [
                ("الاسم", "سلمى", false), ("الفرق", "-12", true), ("الحالة", "مفتوح", false),
                ("التاريخ", "26 سبتمبر 2026", false), ("المدة", "14 يومًا", false), ("ملاحظات", "رسالة قصيرة", false),
            ];
            return $"<ins:DescriptionList{Classes(s.On("grid") ? "grid" : null)}>\n" +
                string.Concat(pairs.Take(int.Parse(s["count"])).Select(p =>
                    $"  <ins:DescriptionItem Term=\"{p.Term}\" Content=\"{p.Value}\"{(p.Num && s.On("num") ? " Classes=\"num\"" : "")} />\n")) +
                "</ins:DescriptionList>";
        },
        Notes = s => s.On("grid") ? [] : ["تقيس القائمة عرضها هي لا عرض النافذة، فإذا ضاق مكانها عن 416 نزلت كل تسمية فوق قيمتها."],
    };

    public static PlayDef[] All { get; } = [List];
}
