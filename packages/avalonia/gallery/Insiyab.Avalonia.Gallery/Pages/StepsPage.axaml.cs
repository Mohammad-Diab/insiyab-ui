using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("steps", "الخطوات والمعالج")]
public partial class StepsPage : UserControl
{
    public StepsPage()
    {
        InitializeComponent();
        for (var i = 1; i <= 12; i++) GridSteps.Items.Add(new StepsItem { Content = $"البند {i}", IsDone = i < 2, IsCurrent = i == 2 });
        FormWizard.CurrentIndex = 1;
        DataValidationErrors.SetErrors(EmailField, ["هذا الحقل مطلوب."]);
        CancelWizard.CurrentIndex = 2;
    }
}
