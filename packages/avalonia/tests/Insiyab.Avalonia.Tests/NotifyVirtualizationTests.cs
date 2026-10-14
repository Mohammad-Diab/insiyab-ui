using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The notification list builds only the rows in view, so a long list opens fast and its keys, counts and dividers go by item index.
public class NotifyVirtualizationTests
{
    private const int Total = 2000;

    private static Notify Many(int count = Total)
    {
        var notify = new Notify { FootText = "عرض الكل" };
        for (var i = 0; i < count; i++)
            notify.Items.Add(new NotifyItem { Title = "إشعار " + i, Description = "وصف", End = "الآن", IsUnread = i % 3 == 0 });
        return notify;
    }

    private static Window Host(Notify notify) => Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.RightToLeft, null, 900, 700);

    private static Visual Body(Notify notify) => Part<Popup>(notify, "PART_Popup").Child!;

    private static List<NotifyItem> Realized(Notify notify) => Body(notify).GetVisualDescendants().OfType<NotifyItem>().Where(r => notify.IndexFromContainer(r) >= 0).ToList();

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(30);
    }

    private static object? Focused(Window window) => window.FocusManager!.GetFocusedElement();

    [AvaloniaFact]
    public void A_long_list_realizes_only_the_rows_in_view_and_opens_fast()
    {
        var warm = Many(20);
        var warmWindow = Host(warm);
        warm.IsOpen = true;
        Settle(100);
        warm.IsOpen = false;
        warmWindow.Close();

        var notify = Many();
        Host(notify);
        var clock = Stopwatch.StartNew();
        notify.IsOpen = true;
        Settle(1);
        var opened = clock.ElapsedMilliseconds;
        Settle(100);
        var rows = Realized(notify);
        Assert.InRange(rows.Count, 1, 99);
        Assert.True(opened < 1000, $"opening took {opened} ms");
        Assert.Same(notify.Items[0], rows.OrderBy(r => notify.IndexFromContainer(r)).First());
    }

    [AvaloniaFact]
    public void Opening_a_long_list_focuses_the_first_row()
    {
        var notify = Many();
        var window = Host(notify);
        notify.IsOpen = true;
        Settle(100);
        Assert.Same(notify.Items[0], Focused(window));
    }

    [AvaloniaFact]
    public void End_focuses_the_last_item_and_brings_it_into_the_tree()
    {
        var notify = Many();
        var window = Host(notify);
        notify.IsOpen = true;
        Settle(100);
        Press(window, Key.End);
        var last = (NotifyItem)notify.Items[^1]!;
        Assert.Same(last, Focused(window));
        Assert.True(last.IsAttachedToVisualTree());
        Assert.InRange(Realized(notify).Count, 1, 99);
        var scroll = Body(notify).GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == "PART_Scroll");
        var bottom = last.TranslatePoint(new Point(0, last.Bounds.Height), scroll)!.Value.Y;
        Assert.True(bottom <= scroll.Viewport.Height + 1, $"last row ends at {bottom}, view is {scroll.Viewport.Height}");

        Press(window, Key.Home);
        Assert.Same(notify.Items[0], Focused(window));
        Assert.True(((NotifyItem)notify.Items[0]!).IsAttachedToVisualTree());
    }

    [AvaloniaFact]
    public void Down_keeps_moving_focus_across_the_realized_boundary()
    {
        var notify = Many();
        var window = Host(notify);
        notify.IsOpen = true;
        Settle(100);
        var firstScreen = Realized(notify).Count;
        for (var i = 1; i <= firstScreen + 12; i++)
        {
            Press(window, Key.Down);
            Assert.Same(notify.Items[i], Focused(window));
        }
        Assert.InRange(Realized(notify).Count, 1, 99);
        for (var i = firstScreen + 11; i >= firstScreen - 2; i--)
        {
            Press(window, Key.Up);
            Assert.Same(notify.Items[i], Focused(window));
        }
    }

    [AvaloniaFact]
    public void The_unread_count_comes_from_the_items_not_the_realized_rows()
    {
        var notify = Many();
        var window = Host(notify);
        var unread = Enumerable.Range(0, Total).Count(i => i % 3 == 0);
        Assert.Equal(unread, notify.UnreadCount);
        Assert.Equal("99+", notify.CountText);
        notify.IsOpen = true;
        Settle(100);
        Assert.Equal(unread, notify.UnreadCount);

        var far = (NotifyItem)notify.Items[1500]!;
        Assert.False(far.IsAttachedToVisualTree());
        far.IsUnread = false;
        Assert.Equal(unread - 1, notify.UnreadCount);
        var quiet = (NotifyItem)notify.Items[1501]!;
        quiet.IsUnread = true;
        Assert.Equal(unread, notify.UnreadCount);

        Assert.Equal(0, notify.MarkAllRead());
        Assert.Equal(0, notify.UnreadCount);
        Assert.All(notify.Items.OfType<NotifyItem>(), i => Assert.False(i.IsUnread));
        Assert.DoesNotContain(":unread", notify.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void The_head_button_marks_unrealized_items_read_too()
    {
        var notify = Many();
        Host(notify);
        notify.IsOpen = true;
        Settle(100);
        NotifyReadEventArgs? heard = null;
        notify.Read += (_, e) => heard = e;
        var readAll = Body(notify).GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_ReadAll");
        readAll.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(30);
        Assert.True(heard!.All);
        Assert.Equal(0, notify.UnreadCount);
        Assert.All(notify.Items.OfType<NotifyItem>(), i => Assert.False(i.IsUnread));
        Assert.All(Realized(notify), r => Assert.DoesNotContain(":unread", r.Classes));
    }

    [AvaloniaFact]
    public void Clicking_a_row_after_scrolling_marks_that_item_read()
    {
        var notify = Many();
        var window = Host(notify);
        notify.IsOpen = true;
        Settle(100);
        Press(window, Key.End);
        var last = (NotifyItem)notify.Items[^1]!;
        last.IsUnread = true;
        var before = notify.UnreadCount;
        NotifyReadEventArgs? heard = null;
        notify.Read += (_, e) => heard = e;
        last.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle(30);
        Assert.Same(last, heard!.Item);
        Assert.False(last.IsUnread);
        Assert.Equal(before - 1, notify.UnreadCount);
    }

    [AvaloniaFact]
    public void The_divider_sits_on_every_row_but_the_first_also_after_scrolling()
    {
        var notify = Many();
        var window = Host(notify);
        notify.IsOpen = true;
        Settle(100);
        var rows = Realized(notify).OrderBy(r => notify.IndexFromContainer(r)).ToList();
        Assert.Equal(0, notify.IndexFromContainer(rows[0]));
        Assert.Equal(0, rows[0].BorderThickness.Top);
        Assert.All(rows.Skip(1), r => Assert.Equal(1, r.BorderThickness.Top));

        Press(window, Key.End);
        rows = Realized(notify).OrderBy(r => notify.IndexFromContainer(r)).ToList();
        Assert.True(notify.IndexFromContainer(rows[0]) > 0);
        Assert.All(rows, r => Assert.Equal(1, r.BorderThickness.Top));

        Press(window, Key.Home);
        rows = Realized(notify).OrderBy(r => notify.IndexFromContainer(r)).ToList();
        Assert.Equal(0, rows[0].BorderThickness.Top);
        Assert.All(rows.Skip(1), r => Assert.Equal(1, r.BorderThickness.Top));
    }

    [AvaloniaFact]
    public void A_row_scrolled_out_of_view_keeps_the_right_divider_when_the_list_changes()
    {
        var notify = Many(60);
        var window = Host(notify);
        notify.IsOpen = true;
        Settle(100);
        var old = (NotifyItem)notify.Items[0]!;
        Press(window, Key.End);
        Assert.DoesNotContain(old, Realized(notify));
        var added = new NotifyItem { Title = "جديد" };
        notify.Items.Insert(0, added);
        notify.ScrollIntoView(0);
        Settle(60);
        var rows = Realized(notify).OrderBy(r => notify.IndexFromContainer(r)).ToList();
        Assert.Same(added, rows[0]);
        Assert.Equal(0, added.BorderThickness.Top);
        Assert.All(rows.Skip(1), r => Assert.Equal(1, r.BorderThickness.Top));
        Assert.Equal(1, old.BorderThickness.Top);
    }

    [AvaloniaFact]
    public void Scrolling_the_focused_row_out_of_view_hands_the_focus_to_a_row_in_view()
    {
        var notify = Many();
        var window = Host(notify);
        notify.IsOpen = true;
        Settle(100);
        Assert.Same(notify.Items[0], Focused(window));
        var scroll = Body(notify).GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == "PART_Scroll");
        scroll.Offset = new Vector(0, 3000);
        Settle(100);
        var held = Assert.IsType<NotifyItem>(Focused(window));
        var at = notify.IndexFromContainer(held);
        Assert.True(at > 0, "the focus went to a built row");
        Press(window, Key.Down);
        Assert.Same(notify.Items[at + 1], Focused(window));
        Press(window, Key.Escape);
        Assert.False(notify.IsOpen);
    }

    [AvaloniaFact]
    public void The_divider_follows_the_place_when_an_item_is_added_or_removed_at_the_top()
    {
        var notify = Many(5);
        Host(notify);
        notify.IsOpen = true;
        Settle(100);
        var old = (NotifyItem)notify.Items[0]!;
        var added = new NotifyItem { Title = "جديد" };
        notify.Items.Insert(0, added);
        Settle(100);
        Assert.Equal(0, added.BorderThickness.Top);
        Assert.Equal(1, old.BorderThickness.Top);
        notify.Items.RemoveAt(0);
        Settle(100);
        Assert.Equal(0, old.BorderThickness.Top);
        Assert.Equal(1, ((NotifyItem)notify.Items[1]!).BorderThickness.Top);
    }
}
