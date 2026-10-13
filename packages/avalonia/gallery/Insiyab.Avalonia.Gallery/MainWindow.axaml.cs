using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Gallery;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DarkSwitch.IsCheckedChanged += (_, _) =>
            Application.Current!.RequestedThemeVariant = DarkSwitch.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
        LtrSwitch.IsCheckedChanged += (_, _) =>
            FlowDirection = LtrSwitch.IsChecked == true ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
    }
}
