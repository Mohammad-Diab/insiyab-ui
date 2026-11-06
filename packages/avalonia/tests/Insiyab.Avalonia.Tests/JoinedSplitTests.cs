using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class JoinedSplitTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Split Joined(double width) => new()
    {
        Classes = { "joined" },
        Width = width,
        Height = 260,
        StackWidth = 640,
        CornerRadius = new CornerRadius(14),
        Background = Brushes.White,
        First = new Border { Background = Brushes.WhiteSmoke, Height = 600, Child = new TextBlock { Text = "القائمة" } },
        Second = new Border { Height = 200, Child = new TextBlock { Text = "التفاصيل" } },
    };

    private static bool Hits(Window w, Point p, Control target) =>
        w.InputHitTest(p) is Visual v && (v == target || target.IsVisualAncestorOf(v));

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Joined_panes_meet_at_a_hairline_whose_grab_strip_runs_into_the_second_pane(string dir)
    {
        var split = Joined(700);
        var w = Show(new Panel { Margin = new Thickness(20), Children = { split } }, Dir(dir), width: 760, height: 320);
        var first = Part<ScrollViewer>(split, "PART_First");
        var second = Part<ScrollViewer>(split, "PART_Second");
        var handle = Part<Control>(split, "PART_Handle");
        var grip = Assert.IsAssignableFrom<Border>(Part<Control>(split, "PART_Grip"));
        Assert.Equal(new Thickness(0), first.Padding);
        Assert.Equal(new Thickness(0), second.Padding);
        Assert.False(grip.IsHitTestVisible);
        Assert.Equal(3, grip.Bounds.Width, 0.5);
        Assert.Equal(handle.Bounds.Height, grip.Bounds.Height, 0.5);

        var f = Box(first, w);
        var s = Box(second, w);
        var h = Box(handle, w);
        Assert.Equal(8, h.Width, 0.5);
        if (dir == "rtl")
        {
            Assert.Equal(1, f.Left - s.Right, 0.5);
            Assert.Equal(f.Left, h.Right, 0.5);
        }
        else
        {
            Assert.Equal(1, s.Left - f.Right, 0.5);
            Assert.Equal(f.Right, h.Left, 0.5);
        }
        var y = f.Center.Y;
        var intoFirst = dir == "rtl" ? f.Left + 1 : f.Right - 1;
        var intoSecond = dir == "rtl" ? s.Right - 6 : s.Left + 6;
        Assert.False(Hits(w, new Point(intoFirst, y), handle));
        Assert.True(Hits(w, new Point(intoSecond, y), handle));
        AssertBrush(grip, "InsLine", () => grip.Background);

        w.MouseMove(new Point(intoSecond, y));
        AssertBrush(grip, "InsPrimaryMid", () => grip.Background);
        Capture(w, $"split-joined-{dir}");
    }

    [AvaloniaFact]
    public void A_joined_split_rounds_its_panes_with_its_corners()
    {
        var split = Joined(700);
        Show(new Panel { Margin = new Thickness(20), Children = { split } }, FlowDirection.RightToLeft, width: 760, height: 320);
        var frame = Assert.IsAssignableFrom<Border>(Part<Control>(split, "PART_Frame"));
        Assert.True(frame.ClipToBounds);
        Assert.Equal(new CornerRadius(14), frame.CornerRadius);

        var plain = new Split { Width = 600, Height = 200 };
        Show(plain, FlowDirection.RightToLeft, width: 640, height: 240);
        Assert.False(Part<Control>(plain, "PART_Frame").ClipToBounds);
        Assert.Equal(new Thickness(5, 0), Part<ScrollViewer>(plain, "PART_First").Padding);
        Assert.False(Part<Control>(plain, "PART_Seam").IsVisible);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Stacked_joined_panes_lie_one_under_the_other_with_a_hairline_between(string dir)
    {
        var split = Joined(500);
        var w = Show(new Panel { Margin = new Thickness(20), Children = { split } }, Dir(dir), width: 560, height: 1000);
        var seam = Part<Control>(split, "PART_Seam");
        Assert.True(seam.IsVisible);
        Assert.False(Part<Control>(split, "PART_Handle").IsVisible);
        var s = Box(Part<ScrollViewer>(split, "PART_Second"), w);
        var line = Box(seam, w);
        Assert.Equal(s.Top, line.Top, 0.5);
        Assert.Equal(1, line.Height, 0.5);
        Assert.Equal(s.Width, line.Width, 0.5);
        AssertBrush(seam, "InsLine", () => ((Border)seam).Background);
    }

    [AvaloniaFact]
    public void A_vertical_joined_split_grabs_from_the_hairline_down_into_the_second_pane()
    {
        var split = Joined(500);
        split.Orientation = Orientation.Vertical;
        split.Height = 400;
        var w = Show(new Panel { Margin = new Thickness(20), Children = { split } }, FlowDirection.RightToLeft, width: 560, height: 460);
        var first = Box(Part<ScrollViewer>(split, "PART_First"), w);
        var second = Box(Part<ScrollViewer>(split, "PART_Second"), w);
        var handle = Part<Control>(split, "PART_Handle");
        var grip = Part<Control>(split, "PART_Grip");
        Assert.Equal(1, second.Top - first.Bottom, 0.5);
        Assert.Equal(8, handle.Bounds.Height, 0.5);
        Assert.Equal(3, grip.Bounds.Height, 0.5);
        Assert.Equal(handle.Bounds.Width, grip.Bounds.Width, 0.5);
        Assert.False(Hits(w, new Point(first.Center.X, first.Bottom - 1), handle));
        Assert.True(Hits(w, new Point(first.Center.X, second.Top + 6), handle));
    }
}
