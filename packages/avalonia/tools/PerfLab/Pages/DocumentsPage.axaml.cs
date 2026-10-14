using Avalonia.Controls;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

namespace PerfLab.Pages;

public partial class DocumentsPage : UserControl
{
    private static readonly string[] Names = ["عقد التوريد", "فاتورة 10431", "محضر الاجتماع", "سياسة الإرجاع", "دليل الشحن", "ملاحظات الفريق", "عرض الأسعار", "تقرير الربع"];

    private const string Paragraph =
        "يتضمن هذا المستند شروط التوريد ومواعيد التسليم وطريقة الدفع، ويُراجع كل ثلاثة أشهر. وتلتزم الجهة الموردة بتسليم الطلبات في مواعيدها، وإذا تأخر طلب أكثر من يومين أُبلغ العميل وعُرض عليه بديل أو استرداد المبلغ.";

    public DocumentsPage()
    {
        InitializeComponent();
        for (var i = 0; i < Names.Length; i++)
        {
            var body = new Prose();
            body.Children.Add(new TextBlock { Text = Names[i], Classes = { "h2" } });
            for (var p = 0; p < 10; p++) body.Children.Add(new TextBlock { Text = Paragraph, TextWrapping = TextWrapping.Wrap });
            Tabs.Items.Add(new DocumentTab { Header = Names[i], IsClosable = i > 0, Content = body });
        }
        Tabs.SelectedIndex = 0;
    }
}
