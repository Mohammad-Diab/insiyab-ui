using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Styling;

[assembly: XmlnsDefinition("https://github.com/Mohammad-Diab/insiyab-ui", "Insiyab.Avalonia")]
[assembly: XmlnsDefinition("https://github.com/Mohammad-Diab/insiyab-ui", "Insiyab.Avalonia.Controls")]
[assembly: XmlnsPrefix("https://github.com/Mohammad-Diab/insiyab-ui", "ins")]

namespace Insiyab.Avalonia;

/// <summary>The whole Insiyab look. Put it in <c>Application.Styles</c> in place of <c>FluentTheme</c>.</summary>
public partial class InsiyabTheme : Styles
{
    public InsiyabTheme(IServiceProvider? serviceProvider = null)
    {
        OnLoadingFonts();
        AvaloniaXamlLoader.Load(serviceProvider, this);
        OnLoadedBrand();
        OnLoadedFrost();
        Controls.Motion.Register();
        Controls.WindowActivity.Register();
        Controls.TipKeyboard.Register();
        Controls.TextDirection.Register();
        Controls.ClassTheme.Register(this);
    }

    partial void OnLoadingFonts();

    partial void OnLoadedBrand();

    partial void OnLoadedFrost();
}
