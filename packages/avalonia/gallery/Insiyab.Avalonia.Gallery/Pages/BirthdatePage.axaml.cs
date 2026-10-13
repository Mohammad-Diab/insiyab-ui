using Avalonia.Controls;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("birthdate", "تاريخ الميلاد")]
public partial class BirthdatePage : UserControl
{
    private static readonly DateTime Born = new(1993, 1, 22);

    public BirthdatePage()
    {
        InitializeComponent();

        PlayFilled.SetDate(Born);
        PlayDisabled.SetDate(Born);
        ServerField.SetDate(Born);

        ServerReset.Click += (_, _) => ServerField.SetDate(Born);
        ServerSend.Click += (_, _) => ServerHint.Text = "أُرسل " + ServerField.Value;
    }
}
