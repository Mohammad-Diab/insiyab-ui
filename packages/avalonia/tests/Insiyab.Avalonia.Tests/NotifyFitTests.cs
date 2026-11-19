using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

/// <summary>The bell's panel in a window no wider than a phone (a 360 window): the panel narrows to the window less the bar's sides and stays inside it.</summary>
public class NotifyFitTests
{
    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight, 360)]
    [InlineData(FlowDirection.RightToLeft, 360)]
    [InlineData(FlowDirection.LeftToRight, 1000)]
    [InlineData(FlowDirection.RightToLeft, 1000)]
    public void The_panel_stays_inside_a_narrow_window(FlowDirection dir, double width)
    {
        var bell = new Notify { IsEnd = true };
        bell.Items.Add(new NotifyItem { Title = "رسالتان جديدتان في بريدك", Description = "موعد الجلسة القادمة", End = "أمس", IsUnread = true });
        var bar = new Border
        {
            Classes = { "topbar" },
            Padding = new Thickness(12, 10),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new StackPanel { Classes = { "zone", "end" }, HorizontalAlignment = HorizontalAlignment.Right, Children = { bell } },
        };
        var window = Show(bar, dir, width: width, height: 700);
        bell.IsOpen = true;
        Settle();

        var popup = Part<Popup>(bell, "PART_Popup");
        var root = popup.Child!.GetVisualDescendants().OfType<Border>().First(b => b.Name == "LayoutRoot");
        var top = TopLevel.GetTopLevel(root)!;
        var a = top.PointToScreen(default);
        var b = window.PointToScreen(default);
        var box = new Rect(root.Bounds.Size).TransformToAABB(root.TransformToVisual(top)!.Value);
        var left = box.Left + a.X - b.X;
        var right = box.Right + a.X - b.X;
        Assert.True(left >= 11.5 && right <= width - 11.5, $"panel {left:0}..{right:0} in a {width} window; bell {new Rect(bell.Bounds.Size).TransformToAABB(bell.TransformToVisual(window)!.Value)}; offset {popup.HorizontalOffset}; popup child {popup.Child!.Bounds}");
    }
}
