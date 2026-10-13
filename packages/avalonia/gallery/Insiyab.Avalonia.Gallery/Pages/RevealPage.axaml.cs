using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("reveal", "حركات الدخول")]
public partial class RevealPage : UserControl
{
    public RevealPage()
    {
        InitializeComponent();
        Replay.Click += (_, _) => Entrance.Replay(Example);
    }
}
