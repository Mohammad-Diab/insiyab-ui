using Avalonia.Controls;
using Avalonia.LogicalTree;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery.Playground;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("phone", "رقم الهاتف")]
public partial class PhonePage : UserControl
{
    public PhonePage()
    {
        InitializeComponent();
        var parts = Example.GetLogicalDescendants().ToList();
        var hint = parts.OfType<TextBlock>().First(t => t.Classes.Contains("hint"));
        var box = parts.OfType<PhoneBox>().First();
        void Show(string value) => hint.Text = "يُرسل: ‎" + (value.Length > 0 ? value : "—");
        Show(box.Value ?? "");
        box.PhoneChanged += (_, e) => Show(e.Value);
    }
}
