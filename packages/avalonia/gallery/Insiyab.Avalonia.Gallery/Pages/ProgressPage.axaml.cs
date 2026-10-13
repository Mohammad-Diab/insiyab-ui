using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("progress", "التقدم")]
public partial class ProgressPage : UserControl
{
    public ProgressPage()
    {
        InitializeComponent();
        Advance.Click += (_, _) => LiveBar.Value = Math.Min(LiveBar.Maximum, LiveBar.Value + 10);
    }
}
