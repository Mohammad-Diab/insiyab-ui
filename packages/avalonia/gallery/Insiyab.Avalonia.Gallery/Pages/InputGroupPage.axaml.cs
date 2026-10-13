using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("input-group", "مجموعات الحقول")]
public partial class InputGroupPage : UserControl
{
    public InputGroupPage()
    {
        InitializeComponent();
        InputsPage.FlowVariants(this);
        DataValidationErrors.SetErrors(BadGroup, ["رسالة الخطأ."]);
    }
}
