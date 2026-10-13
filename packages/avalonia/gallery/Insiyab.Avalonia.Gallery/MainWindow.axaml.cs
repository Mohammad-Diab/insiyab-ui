using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Gallery;

public partial class MainWindow : Window
{
    private Button? _current;

    public MainWindow()
    {
        InitializeComponent();
        DarkSwitch.IsCheckedChanged += (_, _) =>
            Application.Current!.RequestedThemeVariant = DarkSwitch.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
        LtrSwitch.IsCheckedChanged += (_, _) =>
            FlowDirection = LtrSwitch.IsChecked == true ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

        string? group = null;
        foreach (var page in GalleryPages.All)
        {
            if (page.Group != group)
            {
                group = page.Group;
                Nav.Children.Add(new TextBlock { Text = group, Classes = { "eyebrow" }, Margin = new Thickness(8, 16, 8, 4) });
            }
            var item = new Button
            {
                Content = page.Title,
                Classes = { "bare" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 6),
                Tag = page,
            };
            item.Click += (_, _) => Open(item);
            Nav.Children.Add(item);
            _current ??= item;
        }
        if (_current is not null) Open(_current);
    }

    private void Open(Button item)
    {
        _current?.Classes.Remove("current");
        _current = item;
        item.Classes.Add("current");
        Host.Content = ((GalleryPage)item.Tag!).Create();
        Scroller.Offset = default;
    }
}
