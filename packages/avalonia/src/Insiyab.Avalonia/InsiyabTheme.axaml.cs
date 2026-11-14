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
    /// <summary>The pseudo-class a TopLevel carries while the last pointer on it was a finger (from the start on a phone), for an app's own touch styles.</summary>
    public const string TouchPseudoClass = ":ins-touch";

    public InsiyabTheme(IServiceProvider? serviceProvider = null)
    {
        OnLoadingFonts();
        AvaloniaXamlLoader.Load(serviceProvider, this);
        OnLoadedBrand();
        OnLoadedFrost();
        Reword();
        Controls.Motion.Register();
        Controls.BackStack.Register();
        Controls.KeyboardRoom.Register();
        Controls.Touch.Register();
        Controls.WindowActivity.Register();
        Controls.TipKeyboard.Register();
        Controls.TextDirection.Register();
        Controls.Words.Register();
        Controls.ClassTheme.Register(this);
    }

    partial void OnLoadingFonts();

    partial void OnLoadedBrand();

    partial void OnLoadedFrost();
}
