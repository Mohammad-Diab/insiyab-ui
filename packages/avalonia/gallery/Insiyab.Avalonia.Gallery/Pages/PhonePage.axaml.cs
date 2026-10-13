using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("phone", "رقم الهاتف")]
public partial class PhonePage : UserControl
{
    public PhonePage()
    {
        InitializeComponent();
        Demo.PhoneChanged += (_, e) => DemoOut.Text = "يُرسل: ‎" +(e.Value.Length > 0 ? e.Value : "—");
    }
}
