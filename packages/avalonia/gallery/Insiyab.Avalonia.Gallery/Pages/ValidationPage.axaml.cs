using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("validation", "التحقق")]
public partial class ValidationPage : UserControl
{
    public ValidationPage()
    {
        InitializeComponent();
        InputsPage.FlowVariants(this);
        // The playgrounds show a field after a failed submit, as the web docs do.
        foreach (var form in new Control[] { ReportedForm, RuleEmail, RuleLength, RulePattern, RuleHint })
            Validate.Check(form);
        DataValidationErrors.SetErrors(TakenName, ["هذا الاسم محجوز."]);
        DataValidationErrors.SetErrors(BadChoice, ["اختر واحدًا."]);
    }
}
