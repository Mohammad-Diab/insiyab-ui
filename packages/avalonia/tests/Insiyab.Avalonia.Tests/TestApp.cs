using Avalonia;
using Avalonia.Headless;
using Insiyab.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace Insiyab.Avalonia.Tests;

public class TestApp : Application
{
    public TestApp() => Styles.Add(new InsiyabTheme());

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
