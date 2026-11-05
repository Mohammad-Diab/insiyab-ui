using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class StatusBarScopeTests
{
    [AvaloniaFact]
    public void Only_the_bars_own_items_take_its_short_sizes()
    {
        var direct = new ProgressBar { Value = 40 };
        var inner = new ProgressBar { Value = 40 };
        var button = new StatusButton { Content = "المهام", Flyout = new PopCard { Content = new StackPanel { Width = 280, Children = { inner } } } };
        var bar = new StatusBar();
        bar.Start.Add(direct);
        bar.End.Add(button);
        var dock = new DockPanel { Children = { bar } };
        DockPanel.SetDock(bar, Dock.Bottom);
        Show(dock, FlowDirection.RightToLeft);
        button.Flyout!.ShowAt(button);
        Settle();
        Assert.Equal(96, direct.Bounds.Width, 0.5);
        Assert.Equal(280, inner.Bounds.Width, 0.5);
    }
}
