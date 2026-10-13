using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("timeline", "الخط الزمني")]
public partial class TimelinePage : UserControl
{
    public TimelinePage()
    {
        InitializeComponent();
        RelFirst.At = DateTime.Now.AddMinutes(-26 * 60);
        RelSecond.At = DateTime.Now.AddMinutes(-5);
        DateFirst.At = new DateTime(2026, 9, 24, 8, 2, 0);
        DateSecond.At = new DateTime(2026, 9, 24, 8, 5, 0);
    }
}
