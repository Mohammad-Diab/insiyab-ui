using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("taskbar", "شارة شريط المهام")]
public partial class TaskbarPage : UserControl
{
    public const string TrayCode =
        """
        var tray = new NotifyAreaIcon(icoBytes, "التطبيق");
        tray.Menu.Add(new NotifyAreaMenuItem("فتح", (_, _) => NotifyAreaIcon.RestoreFromTray(window)));
        tray.Menu.Add(NotifyAreaMenuItem.Separator());
        tray.Menu.Add(new NotifyAreaMenuItem("خروج", (_, _) => lifetime.Shutdown()));
        tray.Activated += (_, _) => NotifyAreaIcon.RestoreFromTray(window);
        tray.BalloonClicked += (_, e) => Open(e.Tag);
        tray.ShowBalloon("اكتمل التصدير", "الملف جاهز للتنزيل.", NotifyAreaLevel.Info, tag: file);
        """;

    private NotifyAreaIcon? _tray;
    private int _sent;

    public TaskbarPage() => InitializeComponent();

    private void OnTray(object? sender, RoutedEventArgs e)
    {
        if (TrayOn.IsChecked == true && _tray is null)
        {
            using var stream = AssetLoader.Open(new Uri("avares://Insiyab.Avalonia.Gallery/Assets/gallery.ico"));
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            _tray = new NotifyAreaIcon(copy.ToArray(), "انسياب");
            var window = TopLevel.GetTopLevel(this) as Window;
            void Restore()
            {
                if (window is not null) NotifyAreaIcon.RestoreFromTray(window);
            }
            _tray.Menu.Add(new NotifyAreaMenuItem("إظهار النافذة", (_, _) => Restore()));
            _tray.Menu.Add(NotifyAreaMenuItem.Separator());
            _tray.Menu.Add(new NotifyAreaMenuItem("إخفاء الأيقونة", (_, _) => TrayOn.IsChecked = false));
            _tray.Activated += (_, _) => Restore();
            _tray.BalloonClicked += (_, a) => Toast.Show(this, $"نُقر الإشعار رقم {a.Tag}.", ToastTone.Info);
        }
        else if (TrayOn.IsChecked != true) DropTray();
        Balloon.IsEnabled = _tray?.IsAdded == true;
    }

    private void OnBalloon(object? sender, RoutedEventArgs e) =>
        _tray?.ShowBalloon("إشعار من المعرض", "انقر هنا لترى رقمه في المعرض.", NotifyAreaLevel.Info, tag: ++_sent);

    private void DropTray()
    {
        _tray?.Dispose();
        _tray = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (TrayOn.IsChecked == true) TrayOn.IsChecked = false;
        DropTray();
    }

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
