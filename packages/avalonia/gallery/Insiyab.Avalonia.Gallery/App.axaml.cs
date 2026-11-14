using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Insiyab.Avalonia.Controls;

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
            // Edge to edge: the ground runs under the system bars and the shell keeps its content clear of them.
            TopLevel.SetAutoSafeAreaPadding(shell, false);
            SafeArea.SetEdges(shell, SafeAreaEdges.All);
            shell.AttachedToVisualTree += (_, _) =>
            {
                if (TopLevel.GetTopLevel(shell)?.InsetsManager is { } insets) insets.DisplayEdgeToEdgePreference = true;
            };
            single.MainView = shell;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
