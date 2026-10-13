using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("reveal", "حركات الدخول")]
public partial class RevealPage : UserControl
{
    private readonly DispatcherTimer _loop = new() { Interval = TimeSpan.FromSeconds(2) };

    public RevealPage()
    {
        InitializeComponent();
        Replay.Click += (_, _) => Entrance.Replay(Example);
        // An entrance runs once, so the single-element previews replay it every two seconds, as the web's do.
        _loop.Tick += (_, _) =>
        {
            Entrance.Replay(Rise);
            Entrance.Replay(Pop);
            Entrance.Replay(Slide);
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _loop.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _loop.Stop();
    }
}
