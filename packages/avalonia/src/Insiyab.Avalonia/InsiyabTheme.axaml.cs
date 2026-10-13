using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Insiyab.Avalonia;

/// <summary>The whole Insiyab look. Put it in <c>Application.Styles</c> in place of <c>FluentTheme</c>.</summary>
public partial class InsiyabTheme : Styles
{
    public InsiyabTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
        OnLoadedBrand();
        OnLoadedFrost();
    }

    partial void OnLoadedBrand();

    partial void OnLoadedFrost();
}
