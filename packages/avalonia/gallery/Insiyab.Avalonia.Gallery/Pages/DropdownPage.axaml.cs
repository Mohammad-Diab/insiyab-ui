using Avalonia.Controls;
using Avalonia.Layout;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("dropdown", "القوائم المنسدلة")]
public partial class DropdownPage : UserControl
{
    public DropdownPage()
    {
        InitializeComponent();
        MenusGallery.DrawOpen(this);
    }
}
