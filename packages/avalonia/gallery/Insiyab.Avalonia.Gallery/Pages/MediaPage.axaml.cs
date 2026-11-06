using Avalonia.Controls;
using Insiyab.Avalonia.Gallery.Playground;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("media", "الوسائط")]
public partial class MediaPage : UserControl
{
    public MediaPage()
    {
        InitializeComponent();
        Player.Stage = MediaPlay.StageExample;
        Pictures.Stage = MediaPlay.StagePicture;
    }
}
