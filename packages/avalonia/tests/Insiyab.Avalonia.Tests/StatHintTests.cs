using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class StatHintTests
{
    [AvaloniaFact]
    public void A_stat_hint_stops_at_two_lines()
    {
        var hint = new TextBlock { Classes = { "stat-hint" }, Text = string.Concat(Enumerable.Repeat("آخر خبر في القائمة بعنوان طويل جدًا ", 6)) };
        Show(new StackPanel { Width = 200, Children = { hint } }, FlowDirection.RightToLeft);
        Assert.Equal(2, hint.MaxLines);
        Assert.Equal(TextTrimming.CharacterEllipsis, hint.TextTrimming);
        Assert.Equal(TextWrapping.Wrap, hint.TextWrapping);
        Assert.Equal(2 * 18.7, hint.Bounds.Height, 1.0);
    }
}
