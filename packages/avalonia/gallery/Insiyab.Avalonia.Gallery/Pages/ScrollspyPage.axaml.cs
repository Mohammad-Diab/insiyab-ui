using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("scrollspy", "تتبع القراءة")]
public partial class ScrollspyPage : UserControl
{
    public ScrollspyPage()
    {
        InitializeComponent();
        foreach (var box in new[] { BoxA, BoxB, BoxC, BoxD }) Scroll(box);
    }

    // The playground stages start scrolled to 130, so the current link is not the first.
    private static void Scroll(ScrollViewer box) =>
        box.Loaded += (_, _) => Dispatcher.UIThread.Post(() => box.Offset = new Vector(0, 130), DispatcherPriority.Background);
}
