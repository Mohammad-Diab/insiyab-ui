using Avalonia.Controls;
using Avalonia.Interactivity;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("toasts", "الرسائل العابرة")]
public partial class ToastsPage : UserControl
{
    public ToastsPage() => InitializeComponent();

    private void OnShowToast(object? sender, RoutedEventArgs e) => Toast.Show((Control)sender!, "تم الحفظ", ToastTone.Ok);
}
