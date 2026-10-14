using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace PerfLab.Pages;

public partial class OverlaysPage : UserControl
{
    public OverlaysPage()
    {
        InitializeComponent();
        ConfirmButton.Click += async (_, _) => await Dialog.ConfirmAsync(this, "حذف الطلب", "سيُحذف الطلب 10431 نهائيًا.", danger: true);
    }
}
