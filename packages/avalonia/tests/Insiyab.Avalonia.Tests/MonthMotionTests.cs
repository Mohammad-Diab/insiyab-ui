using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MonthMotionTests
{
    // The entrance clock held still, so a motion keeps its first frame and where it starts from can be read.
    private static T Held<T>(Func<T> run)
    {
        var real = Entrance.Now;
        Entrance.Now = () => 1000;
        try { return run(); }
        finally { Entrance.Now = real; }
    }

    private static (double X, double Y, double Opacity) Start(MonthCalendar month) =>
        month.Body!.RenderTransform is TranslateTransform t ? (t.X, t.Y, month.Body.Opacity) : (0, 0, month.Body.Opacity);

    [AvaloniaTheory]
    [InlineData(FlowDirection.RightToLeft)]
    [InlineData(FlowDirection.LeftToRight)]
    public void A_step_slides_the_new_days_in_from_the_side_they_come_from(FlowDirection dir) => Held(() =>
    {
        var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Width = 760 };
        Show(month, dir, width: 820, height: 800);
        month.Go(1);
        Settle(100);
        Assert.Equal((MonthCalendar.TurnSlide, 0d, 0d), Start(month));
        month.Go(-1);
        Settle(100);
        Assert.Equal((-MonthCalendar.TurnSlide, 0d, 0d), Start(month));

        month.View = MonthCalendarView.Week;
        month.Date = new DateTime(2026, 9, 21);
        Settle(100);
        Assert.Equal((0d, 0d, 1d), Start(month));
        month.Go(1);
        Settle(100);
        Assert.Equal(MonthCalendar.TurnSlide, Start(month).X);
        return 0;
    });

    [AvaloniaFact]
    public void A_switch_of_view_rises_in_and_today_already_shown_stays_put() => Held(() =>
    {
        var month = new MonthCalendar { Width = 760 };
        var w = Show(month, FlowDirection.RightToLeft, width: 820, height: 800);
        var views = month.HeadRow!.Children[2] is Panel end ? (Segmented)end.Children[0] : null;
        views!.SelectedIndex = 1;
        Settle(100);
        Assert.Equal(MonthCalendarView.Week, month.View);
        Assert.Equal((0d, 6d, 0d), Start(month));

        month.Go(0);
        Settle(100);
        Assert.Equal((0d, 0d, 1d), Start(month));
        return 0;
    });

    [AvaloniaFact]
    public void Still_motion_turns_with_no_slide()
    {
        Motion.SystemOverride = true;
        try
        {
            var month = new MonthCalendar { Month = new DateTime(2026, 9, 1), Width = 760 };
            Show(month, FlowDirection.RightToLeft, width: 820, height: 800);
            month.Go(1);
            Settle(100);
            Assert.Null(month.Body!.RenderTransform);
            Assert.Equal(1, month.Body.Opacity);
        }
        finally { Motion.SystemOverride = null; }
    }
}
