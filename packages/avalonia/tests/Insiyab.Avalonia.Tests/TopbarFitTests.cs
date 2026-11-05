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

public class TopbarFitTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Window Build(string dir, string account, out Grid grid, out StackPanel center, out StackPanel end, out Border accountIsland, out CaptionButtons captions)
    {
        var accountBox = new Border { Classes = { "island" }, Child = new TextBlock { Text = account } };
        var centerZone = new StackPanel { Classes = { "zone", "center" }, Children = { new Border { Classes = { "island", "title" }, Child = new TextBlock { Classes = { "shell-title" }, Text = "لوحة المتابعة" } } } };
        var endZone = new StackPanel { Classes = { "zone", "end" }, Children = { new Border { Classes = { "island" }, Child = new TextBlock { Text = "الإشعارات" } }, accountBox } };
        var caps = new CaptionButtons { Preview = CaptionPreview.Normal };
        var g = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto"),
            Children =
            {
                new StackPanel { Classes = { "zone", "start" }, Children = { new Border { Classes = { "island" }, Child = new TextBlock { Text = "القائمة" } } } },
                centerZone,
                endZone,
                caps,
            },
        };
        Grid.SetColumn(caps, 3);
        grid = g;
        center = centerZone;
        end = endZone;
        accountIsland = accountBox;
        captions = caps;
        return Show(new Border { Classes = { "topbar" }, Child = g }, Dir(dir), null, 1280, 800);
    }

    private const string Long = "مساحة العمل المشتركة لفريق التصميم والتطوير والتسويق والمبيعات";

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_long_end_zone_never_runs_under_the_caption_buttons(string dir)
    {
        var w = Build(dir, Long, out var grid, out var center, out var end, out var account, out var captions);
        var natural = end.Children.Where(c => c.IsVisible).Sum(c => c.DesiredSize.Width) + end.Spacing * (end.Children.Count - 1);
        var half = (grid.Bounds.Width - center.Bounds.Width - captions.Bounds.Width - 3 * grid.ColumnSpacing) / 2;
        Assert.True(natural > half + 20, $"the repro needs a longer account text: natural {natural}, half {half}");
        var cap = Box(captions, w);
        if (dir == "ltr")
        {
            var right = end.Children.Max(c => Box(c, w).Right);
            Assert.True(right <= cap.Left + 0.5, $"end zone right {right}, captions left {cap.Left} (natural {natural}, half {half})");
        }
        else
        {
            var left = end.Children.Min(c => Box(c, w).Left);
            Assert.True(left >= cap.Right - 0.5, $"end zone left {left}, captions right {cap.Right} (natural {natural}, half {half})");
        }
        var island = Box(account, w);
        foreach (var child in center.Children) Assert.False(island.Intersects(Box(child, w)), $"account island {island} meets the centre zone {Box(child, w)}");
        Capture(w, $"topbar-long-end-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void With_room_the_side_columns_stay_equal(string dir)
    {
        Build(dir, "حسابي", out var grid, out _, out _, out _, out _);
        Assert.Equal(grid.ColumnDefinitions[0].ActualWidth, grid.ColumnDefinitions[2].ActualWidth, 0.5);
    }
}
