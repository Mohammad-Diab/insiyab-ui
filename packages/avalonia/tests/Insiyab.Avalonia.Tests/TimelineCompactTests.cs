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

public class TimelineCompactTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_compact_icon_item_keeps_its_text_clear_of_the_chip(string dir)
    {
        var item = new TimelineItem { Header = "تم الإرسال", Time = "الآن", Icon = Geometry.Parse("M5 12h14") };
        var timeline = new Timeline { Classes = { "compact" }, Items = { item } };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { timeline } }, Dir(dir));
        var chip = Box(Part<Border>(item, "PART_Chip"), w);
        var text = Box(Part<StackPanel>(item, "PART_Text"), w);
        if (dir == "ltr") Assert.True(text.Left >= chip.Right + 8, $"text {text.Left}, chip {chip.Right}");
        else Assert.True(text.Right <= chip.Left - 8, $"text {text.Right}, chip {chip.Left}");
        Capture(w, $"timeline-compact-icon-{dir}");
    }
}
