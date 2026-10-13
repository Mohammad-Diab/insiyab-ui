using Avalonia.Controls;
using Avalonia.Interactivity;
using Insiyab.Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("dialog", "مربع الحوار")]
public partial class DialogPage : UserControl
{
    public DialogPage() => InitializeComponent();

    private async void OnWindow(object? sender, RoutedEventArgs e) => await ((Dialog)Resources["DlgWin"]!).ShowAsWindowAsync((Control)sender!);

    private async void OnConfirm(object? sender, RoutedEventArgs e) => await Dialog.ConfirmAsync((Control)sender!, "تأكيد", "أرشفة العنصر؟", confirm: "أرشفة");
}
