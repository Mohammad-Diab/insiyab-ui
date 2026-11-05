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

public class NotifyFootTests
{
    private static Notify MakeNotify(bool closeOnItemClick)
    {
        var notify = new Notify { FootText = "عرض الكل", CloseOnItemClick = closeOnItemClick };
        string[] titles = ["اكتمل التصدير", "تعليق جديد على المستند", "تم حفظ التغييرات"];
        for (var i = 0; i < 3; i++)
            notify.Items.Add(new NotifyItem { Title = titles[i], Description = "وصف", End = "الآن", IsUnread = i < 2 });
        return notify;
    }

    private static (int clicks, bool open) PressFoot(bool closeOnItemClick)
    {
        var notify = MakeNotify(closeOnItemClick);
        var clicks = 0;
        notify.FootClick += (_, _) => clicks++;
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.RightToLeft);
        notify.IsOpen = true;
        Settle();
        var foot = Part<Button>(Part<Popup>(notify, "PART_Popup").Child!, "PART_Foot");
        Click(foot, w);
        return (clicks, notify.IsOpen);
    }

    [AvaloniaFact]
    public void The_foot_closes_the_bell_when_rows_do()
    {
        var (clicks, open) = PressFoot(true);
        Assert.Equal(1, clicks);
        Assert.False(open);
    }

    [AvaloniaFact]
    public void The_foot_leaves_the_bell_open_otherwise()
    {
        var (clicks, open) = PressFoot(false);
        Assert.Equal(1, clicks);
        Assert.True(open);
    }
}
