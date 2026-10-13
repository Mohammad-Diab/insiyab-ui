using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Gallery.Pages;

[GalleryPage("tokens", "التوكنز")]
public partial class TokensPage : UserControl
{
    public TokensPage()
    {
        InitializeComponent();
        var tokens = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://Insiyab.Avalonia/Generated/Tokens.axaml"));
        var entries = new List<KeyValuePair<object, object?>>();
        if (tokens.ThemeDictionaries.TryGetValue(ThemeVariant.Light, out var light) && light is IResourceDictionary lightTokens)
            entries.AddRange(lightTokens);
        entries.AddRange(tokens);

        foreach (var (key, value) in entries)
        {
            if (key is not string name) continue;
            switch (value)
            {
                case Color:
                    ColorList.Children.Add(Swatch(name[..^"Color".Length]));
                    break;
                case global::Avalonia.CornerRadius:
                    RadiusList.Children.Add(Radius(name));
                    break;
                case BoxShadows:
                    ShadowList.Children.Add(Shadow(name));
                    break;
            }
        }
    }

    private static Border Bound(string key, AvaloniaProperty property, Border border)
    {
        border.Bind(property, border.GetResourceObservable(key));
        return border;
    }

    private static TextBlock Key(string key) => new() { Text = key, Classes = { "key" } };

    private static StackPanel Swatch(string key) => new()
    {
        Width = 120,
        Spacing = 6,
        Children =
        {
            Bound(key, Border.BackgroundProperty, Bound("InsLine", Border.BorderBrushProperty, new Border
            {
                Height = 48,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
            })),
            Key(key),
        },
    };

    private static StackPanel Radius(string key) => new()
    {
        Spacing = 6,
        Children =
        {
            Bound(key, Border.CornerRadiusProperty, Bound("InsToneInfoBg", Border.BackgroundProperty, Bound("InsToneInfoBd", Border.BorderBrushProperty, new Border
            {
                Width = 96,
                Height = 72,
                BorderThickness = new Thickness(1),
            }))),
            Key(key),
        },
    };

    private static StackPanel Shadow(string key) => new()
    {
        Spacing = 10,
        Children =
        {
            Bound(key, Border.BoxShadowProperty, Bound("InsSurface", Border.BackgroundProperty, Bound("InsRMd", Border.CornerRadiusProperty, new Border
            {
                Width = 120,
                Height = 72,
            }))),
            Key(key),
        },
    };
}
