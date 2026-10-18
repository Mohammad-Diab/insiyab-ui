using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("taskbar", "شارة شريط المهام")]
public partial class TaskbarPage : UserControl
{
    public TaskbarPage() => InitializeComponent();

    private void OnNew(object? sender, RoutedEventArgs e) =>
        Bell.Items.Insert(0, new NotifyItem { Icon = Icon("IconBell"), Title = "إشعار جديد", Description = "وصل للتو.", End = "الآن", IsUnread = true });

    private void OnHundred(object? sender, RoutedEventArgs e)
    {
        var icon = Icon("IconBell");
        for (var i = 0; i < 100; i++)
            Bell.Items.Add(new NotifyItem { Icon = icon, Title = "إشعار", Description = "وصل للتو.", End = "الآن", IsUnread = true });
    }

    private Geometry? Icon(string key) => this.TryFindResource(key, out var found) ? found as Geometry : null;
}
