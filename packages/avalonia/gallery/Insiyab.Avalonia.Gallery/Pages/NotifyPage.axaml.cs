using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("notify", "مركز الإشعارات")]
public partial class NotifyPage : UserControl
{
    public NotifyPage()
    {
        InitializeComponent();
        var rows = new[] { ("اكتمل التصدير", "الملف جاهز للتنزيل.", "الآن", "IconDownload"), ("تعليق جديد على المستند", "على الفقرة الثانية.", "قبل ساعة", "IconMessage"), ("تم حفظ التغييرات", "حُفظت نسخة جديدة من الملف.", "أمس", "IconCheck") };
        for (var i = 0; i < 120; i++)
        {
            var (title, desc, end, icon) = rows[Math.Min(i, 2)];
            Many.Items.Add(new NotifyItem { Icon = Geometry(icon), Title = title, Description = desc, End = end, IsUnread = true });
        }
    }

    private Geometry? Geometry(string key) => this.TryFindResource(key, out var value) ? value as Geometry : null;

    private void OnAdd(object? sender, RoutedEventArgs e) =>
        Play.Items.Insert(0, new NotifyItem { Icon = Geometry("IconBell"), Title = "إشعار جديد", Description = "وصل للتو.", End = "الآن", IsUnread = true });
}
