using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class CalendarFootTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static (Window w, DateCalendar cal, CalendarFoot grid) Rig(string dir, DatePickMode mode = DatePickMode.Single)
    {
        var cal = new DateCalendar { CalendarKind = CalendarKind.Hijri, Mode = mode };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { cal } }, Dir(dir), null, 640, 700);
        cal.Show(new DateTime(2026, 9, 10));
        Settle(300);
        var foot = cal.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("cal-foot"));
        return (w, cal, (CalendarFoot)foot.Child!);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Clear_fits_the_footer_of_a_hijri_calendar(string dir)
    {
        var (w, cal, grid) = Rig(dir);
        var clear = cal.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-clear"));
        var today = cal.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-today"));
        var kind = cal.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("cal-switch"));
        foreach (var part in new Control[] { clear, today, kind })
        {
            var r = Box(part, grid);
            Assert.True(r.Left >= -0.5 && r.Right <= grid.Bounds.Width + 0.5, $"{part.Classes.FirstOrDefault()} {r.Left}..{r.Right} of {grid.Bounds.Width}; today {today.Bounds.Width}, switch {kind.Bounds.Width}, clear {clear.Bounds.Width}");
        }
        Assert.Equal(dir == "ltr", grid.IsStacked);
        if (grid.IsStacked) Assert.True(Box(kind, grid).Bottom <= Box(clear, grid).Top + 0.5);
        Capture(w, $"cal-foot-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_range_footer_puts_the_switch_on_its_own_line_and_fits(string dir)
    {
        var (w, cal, grid) = Rig(dir, DatePickMode.Range);
        Assert.True(grid.IsStacked);
        var kind = cal.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("cal-switch"));
        var parts = new Control[] { kind, cal.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-today")), cal.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("cal-clear")), cal.GetVisualDescendants().OfType<TextBlock>().First(b => b.Classes.Contains("cal-hint")) };
        foreach (var part in parts)
        {
            var r = Box(part, grid);
            Assert.True(r.Left >= -0.5 && r.Right <= grid.Bounds.Width + 0.5 && r.Bottom <= grid.Bounds.Height + 0.5, $"{part.Classes.FirstOrDefault()} {r} in {grid.Bounds.Size}");
            if (part != kind) Assert.True(r.Top >= Box(kind, grid).Bottom - 0.5);
        }
        Capture(w, $"cal-foot-range-{dir}");
    }
}
