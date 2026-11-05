using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class NotifyHeadTests
{
    private static (Notify Bell, Button Mute, Window Window) Open(FlowDirection dir, bool stacked)
    {
        var mute = new Button { Classes = { "ghost", "icon", "sm" }, Content = new Icon { Data = Geometry.Parse("M4 4l16 16") } };
        var bell = new Notify { HeadActions = mute };
        if (stacked) bell.Classes.Add("stacked");
        bell.Items.Add(new NotifyItem { Title = "تعليق جديد", Description = "على المستند", End = "الآن", IsUnread = true });
        bell.Items.Add(new NotifyItem { Title = "اكتمل التصدير", End = "أمس" });
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { bell } }, dir, width: 700, height: 600);
        bell.IsOpen = true;
        Settle();
        return (bell, mute, window);
    }

    private static Control Body(Notify bell) => Part<Popup>(bell, "PART_Popup").Child!;

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Head_actions_sit_before_mark_all_in_the_head(FlowDirection dir)
    {
        var (bell, mute, window) = Open(dir, false);
        var body = Body(bell);
        var readAll = Part<Button>(body, "PART_ReadAll");
        var title = Part<TextBlock>(body, "PART_Title");

        Assert.True(mute.IsEffectivelyVisible);
        Assert.True(body.IsVisualAncestorOf(mute));
        var ltr = dir == FlowDirection.LeftToRight;
        double X(Control c) => Center(c, window).X;
        Assert.True(ltr ? X(title) < X(mute) && X(mute) < X(readAll) : X(title) > X(mute) && X(mute) > X(readAll), $"title {X(title)}, actions {X(mute)}, mark all {X(readAll)}");
        Assert.Equal(Center(readAll, window).Y, Center(mute, window).Y, 1.0);

        mute.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(50);
        Assert.True(readAll.IsFocused, "mark all comes after the head actions");
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void A_stacked_head_puts_the_title_over_its_actions(FlowDirection dir)
    {
        var (bell, mute, window) = Open(dir, true);
        var body = Body(bell);
        var readAll = Part<Button>(body, "PART_ReadAll");
        var title = Part<TextBlock>(body, "PART_Title");

        var titleBottom = title.TranslatePoint(new Point(0, title.Bounds.Height), window)!.Value.Y;
        Assert.True(titleBottom <= mute.TranslatePoint(default, window)!.Value.Y + 0.5);
        Assert.True(titleBottom <= readAll.TranslatePoint(default, window)!.Value.Y + 0.5);
        var ltr = dir == FlowDirection.LeftToRight;
        Assert.True(ltr ? Center(mute, window).X < Center(readAll, window).X : Center(mute, window).X > Center(readAll, window).X);
        var head = Part<Border>(body, "PART_Head");
        var start = head.TranslatePoint(default, window)!.Value.X;
        var muteStart = mute.TranslatePoint(default, window)!.Value.X;
        Assert.Equal(head.Padding.Left, Math.Abs(muteStart - start), 1.0);
    }

    [AvaloniaFact]
    public void A_head_action_click_is_not_a_row_click()
    {
        var (bell, mute, window) = Open(FlowDirection.RightToLeft, false);
        bell.CloseOnItemClick = true;
        var clicks = 0;
        mute.Click += (_, _) => clicks++;
        Click(mute, window);
        Assert.Equal(1, clicks);
        Assert.True(bell.IsOpen);
        Assert.Equal(1, bell.UnreadCount);
    }

    [AvaloniaFact]
    public void With_no_head_actions_the_head_is_as_before()
    {
        var bell = new Notify();
        bell.Items.Add(new NotifyItem { Title = "تعليق", IsUnread = true });
        Show(new StackPanel { Margin = new Thickness(40), Children = { bell } }, FlowDirection.RightToLeft);
        bell.IsOpen = true;
        Settle();
        var head = Part<Border>(Body(bell), "PART_Head");
        Assert.Equal(new Thickness(16, 10, 12, 10), head.Padding);
        Assert.Equal(51, head.Bounds.Height, 0.5);
    }
}
