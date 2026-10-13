using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Insiyab.Avalonia;

namespace AotCheck;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
        => AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}

internal sealed class App : Application
{
    public override void Initialize() => Styles.Add(new InsiyabTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var form = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
            form.Children.Add(new TextBlock { Text = "تسجيل الدخول", Classes = { "h1" } });
            form.Children.Add(new TextBox { PlaceholderText = "اسم المستخدم", FlowDirection = FlowDirection.LeftToRight });
            form.Children.Add(new TextBox { PasswordChar = '•', Classes = { "revealPasswordButton" }, FlowDirection = FlowDirection.LeftToRight });
            form.Children.Add(new CheckBox { Content = "تذكرني" });
            form.Children.Add(new ToggleSwitch { Content = "الوضع الداكن" });
            form.Children.Add(new Button { Content = "دخول", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Stretch });
            var window = new Window
            {
                Width = 480,
                Height = 560,
                FlowDirection = FlowDirection.RightToLeft,
                Content = new Border { Classes = { "panel" }, Margin = new Thickness(24), Child = form },
            };
            desktop.MainWindow = window;
            if (desktop.Args is { Length: > 0 } a && a[0] == "--smoke")
            {
                window.Opened += (_, _) => DispatcherTimer.RunOnce(() =>
                {
                    InsiyabTheme.SetPrimary(Color.Parse("#0F766E"));
                    RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                    DispatcherTimer.RunOnce(() => desktop.Shutdown(0), TimeSpan.FromSeconds(1));
                }, TimeSpan.FromSeconds(1));
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
