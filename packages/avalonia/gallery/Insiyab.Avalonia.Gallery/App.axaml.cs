using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Insiyab.Avalonia.Gallery;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            var shell = new GalleryShell();
            shell.Bind(TemplatedControl.BackgroundProperty, shell.GetResourceObservable("InsWash1"));
            single.MainView = shell;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
