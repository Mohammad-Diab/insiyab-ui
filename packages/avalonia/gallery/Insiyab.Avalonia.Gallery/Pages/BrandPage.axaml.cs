using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Insiyab.Avalonia.Gallery.Pages;

public partial class BrandPage : UserControl
{
    public BrandPage()
    {
        InitializeComponent();
        ShowCurrent();
    }

    private void OnSwatch(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string hex })
            return;
        InsiyabTheme.SetPrimary(Color.Parse(hex));
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var c = InsiyabTheme.Primary;
        Current.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
