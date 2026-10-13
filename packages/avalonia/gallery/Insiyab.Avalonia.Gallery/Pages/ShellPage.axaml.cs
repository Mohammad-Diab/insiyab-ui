using Avalonia.Controls;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("shell", "هيكل التطبيق")]
public partial class ShellPage : UserControl
{
    public ShellPage()
    {
        InitializeComponent();
        // The second narrow shell shows the drawer open, once it has become a drawer.
        EventHandler? open = null;
        open = (_, _) =>
        {
            if (!Shell.IsDrawer(OpenDrawer)) return;
            OpenDrawer.LayoutUpdated -= open;
            Shell.Sidebar(OpenDrawer, "open");
        };
        OpenDrawer.LayoutUpdated += open;
    }
}
