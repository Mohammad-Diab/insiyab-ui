using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace Insiyab.Avalonia.Tests;

public class SmokeTests
{
    [AvaloniaFact]
    public void Theme_loads_and_a_window_shows()
    {
        var window = new Window { Content = new Button { Content = "OK" } };
        window.Show();
        Assert.True(window.IsVisible);
    }
}
