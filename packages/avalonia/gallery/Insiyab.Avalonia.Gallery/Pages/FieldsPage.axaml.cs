using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

public partial class FieldsPage : UserControl
{
    public FieldsPage()
    {
        InitializeComponent();
        DataValidationErrors.SetErrors(EmailBox, ["أدخل بريدًا إلكترونيًا صحيحًا."]);
    }
}
