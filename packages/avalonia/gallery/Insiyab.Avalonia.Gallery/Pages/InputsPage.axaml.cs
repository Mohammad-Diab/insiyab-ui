using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("inputs", "الحقول")]
public partial class InputsPage : UserControl
{
    public InputsPage()
    {
        InitializeComponent();
        DataValidationErrors.SetErrors(EmailBox, ["أدخل بريدًا إلكترونيًا صحيحًا."]);
    }
}
