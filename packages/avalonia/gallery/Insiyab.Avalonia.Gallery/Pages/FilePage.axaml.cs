using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("file", "رفع الملفات")]
public partial class FilePage : UserControl
{
    // The one-pixel picture the docs use, here as a file already added to the "uploads at once" field.
    private const string Pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    public FilePage()
    {
        InitializeComponent();
        PlayUpload.FileAdded += OnAdded;
        var bytes = Convert.FromBase64String(Pixel);
        var bitmap = new Bitmap(new MemoryStream(bytes));
        PlayUpload.AddFiles([new FileEntry("صورة.png", bytes.Length, () => Task.FromResult<Stream>(new MemoryStream(bytes)), bitmap)]);
    }

    // The page's own upload, pretended: it climbs by a fifth every 300 ms, then finishes or fails by the file name.
    private void OnAdded(object? sender, FileAddedEventArgs e)
    {
        if (!e.Upload) return;
        var fraction = 0.0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (_, _) =>
        {
            fraction += 0.2;
            if (fraction >= 1)
            {
                timer.Stop();
                if (e.Entry.Name.Contains("خطأ")) PlayUpload.Fail(e.Entry, "تعذر الرفع، أزِل الملف أو أعد إضافته.");
                else PlayUpload.Done(e.Entry, "photo-" + Random.Shared.Next(1_000_000));
                return;
            }
            if (!PlayUpload.Progress(e.Entry, fraction)) timer.Stop();
        };
        timer.Start();
    }
}
