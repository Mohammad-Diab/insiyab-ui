using Avalonia.Controls;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("alerts", "التنبيهات")]
public partial class AlertsPage : UserControl
{
    public AlertsPage()
    {
        InitializeComponent();
        // The web brings a dismissed alert back when an option changes; with no options here it comes back by itself.
        Dismissible.Dismissed += (_, _) => DispatcherTimer.RunOnce(() => Dismissible.IsVisible = true, TimeSpan.FromSeconds(1.5));
    }
}
