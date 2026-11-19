using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Sizes for a finger (Themes/Foundation/Touch.axaml): a host marked touch grows small controls and rows to the platforms' targets, and the choices of a segmented row, a switch and a check box reach a finger's box past their edges as buttons do; outside such a host nothing changes.
public class TouchSizeTests
{
    private static bool Hits(Window w, Control c, Point p) => w.InputHitTest(p) is Visual v && (v == c || c.IsVisualAncestorOf(v));

    [AvaloniaFact]
    public void A_touch_host_gives_small_controls_and_rows_a_fingers_size_and_a_desktop_page_keeps_its_own()
    {
        Control[] Make() =>
        [
            new Button { Classes = { "sm" }, Content = "Open" },
            new Button { Classes = { "icon", "sm" }, Content = "x" },
            new Button { Classes = { "pop-item" }, Content = "Row" },
            new CheckBox { Content = "Meetings" },
            new TextBox { Classes = { "sm" }, Width = 160 },
            new Button { Classes = { "close" } },
        ];
        var plain = Make();
        var touch = Make();
        var w = Show(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 20,
            Children =
            {
                new StackPanel { Spacing = 8, Children = { plain[0], plain[1], plain[2], plain[3], plain[4], plain[5] } },
                new StackPanel { Classes = { "touch" }, Spacing = 8, Children = { touch[0], touch[1], touch[2], touch[3], touch[4], touch[5] } },
            },
        }, width: 900, height: 600);
        Settle(100);
        Assert.Equal(30, plain[0].Bounds.Height);
        Assert.Equal(38, touch[0].Bounds.Height);
        Assert.Equal(new Size(30, 30), plain[1].Bounds.Size);
        Assert.Equal(new Size(38, 38), touch[1].Bounds.Size);
        Assert.True(plain[2].Bounds.Height < 44);
        Assert.Equal(48, touch[2].Bounds.Height);
        Assert.True(plain[3].Bounds.Height < 30);
        Assert.Equal(48, touch[3].Bounds.Height);
        Assert.Equal(38, plain[4].Bounds.Height);
        Assert.Equal(44, touch[4].Bounds.Height);
        // the close button keeps one size everywhere; a finger finds it by its reach
        Assert.Equal(plain[5].Bounds.Size, touch[5].Bounds.Size);

        // a check box's mark and words sit in the middle of its taller row
        var mark = Part<Control>(touch[3], "BoxHost");
        var at = mark.TranslatePoint(new Point(0, mark.Bounds.Height / 2), touch[3])!.Value;
        Assert.InRange(at.Y, 23, 25);
    }

    [AvaloniaFact]
    public void A_segmented_choice_a_switch_and_a_check_box_reach_a_fingers_box_past_their_edges_under_a_finger_only()
    {
        var seg = new Segmented { Classes = { "chips" }, ItemsSource = new[] { "All", "Unread" }, SelectedIndex = 0 };
        var toggle = new ToggleSwitch();
        var check = new CheckBox { Content = "Meetings" };
        var w = Show(new StackPanel { Margin = new Thickness(40), Spacing = 40, Children = { seg, toggle, check } }, width: 500, height: 500);
        Settle(100);
        var item = seg.GetVisualDescendants().OfType<SegmentedItem>().First();
        Point Below(Control c) => c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height + 3), w)!.Value;
        Assert.False(Hits(w, item, Below(item)));
        Assert.False(Hits(w, toggle, Below(toggle)));
        Assert.False(Hits(w, check, Below(check)));
        Touch.SetTouch(w, true);
        Assert.True(Hits(w, item, Below(item)));
        Assert.True(Hits(w, toggle, Below(toggle)));
        Assert.True(Hits(w, check, Below(check)));
    }

    // TouchTarget.Size: the controls inside reach a larger box than the library's 44 under a finger (the calendar head's 48); a mouse finds them as drawn.
    [AvaloniaFact]
    public void A_touch_target_size_lets_presses_reach_its_box_under_a_finger_only()
    {
        var plain = new Button { Content = "Today", Height = 44 };
        var wide = new Button { Content = "Today", Height = 44 };
        var host = new StackPanel { Margin = new Thickness(40), Spacing = 60, Children = { plain, new Border { Child = wide, [TouchTarget.SizeProperty] = 48 } } };
        var w = Show(host, width: 400, height: 400);
        Settle(100);
        Point Off(Control c, double dy) => c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height + dy), w)!.Value;
        Assert.False(Hits(w, wide, Off(wide, 1)));
        Touch.SetTouch(w, true);
        Assert.False(Hits(w, plain, Off(plain, 1))); // already 44: nothing past its edge
        Assert.True(Hits(w, wide, Off(wide, 1)));
        Assert.False(Hits(w, wide, Off(wide, 3))); // 2 past each edge: 48
    }

    // The seek row under a finger is a 44 band with its track in the middle and the times on the track's line; a desktop row keeps its 28 bar.
    [AvaloniaFact]
    public void A_touch_seek_row_keeps_its_track_and_times_on_one_line_in_the_middle_of_its_band()
    {
        var plain = new MediaSeekRow { Value = 0.4, Duration = TimeSpan.FromMinutes(17.5), Width = 300 };
        var touch = new MediaSeekRow { Value = 0.4, Duration = TimeSpan.FromMinutes(17.5), Width = 300 };
        var w = Show(new StackPanel { Spacing = 20, Children = { plain, new Border { Classes = { "touch" }, Child = touch } } });
        foreach (var (row, height) in new[] { (plain, 28.0), (touch, 44.0) })
        {
            var bar = Part<MediaBar>(row, "PART_Bar");
            Assert.Equal(height, bar.Bounds.Height, 1);
            var track = bar.TranslatePoint(new Point(0, MediaBar.TrackY(bar.Bounds.Height)), w)!.Value.Y;
            var now = Part<TextBlock>(row, "PART_Now");
            var nowMiddle = now.TranslatePoint(new Point(0, now.Bounds.Height / 2), w)!.Value.Y;
            Assert.InRange(nowMiddle - track, -1, 1);
            if (height == 44)
                Assert.Equal(row.TranslatePoint(new Point(0, row.Bounds.Height / 2), w)!.Value.Y, track, 1);
        }
    }

    // The red ✕ that stops a download: on a desktop page it takes a small icon's line (30 by 13), under a finger its whole 30 face.
    [AvaloniaFact]
    public void The_red_close_takes_a_lines_room_on_a_desktop_page_and_its_face_under_a_finger()
    {
        Button Make() => new() { Classes = { "close", "bad" } };
        var plain = Make();
        var touch = Make();
        Show(new StackPanel { Spacing = 20, Children = { new StackPanel { Children = { plain } }, new StackPanel { Classes = { "touch" }, Children = { touch } } } });
        Assert.Equal(30, plain.Bounds.Width, 1);
        Assert.Equal(30, plain.Bounds.Height, 1);
        Assert.Equal(13, plain.DesiredSize.Height, 1);
        Assert.Equal(30, touch.DesiredSize.Height, 1);
    }

    // The media button's sizes: regular 44, md 56 and lg 72, md and lg round.
    [AvaloniaFact]
    public void The_media_button_comes_in_three_sizes()
    {
        Button Make(string? size)
        {
            var b = new Button { Classes = { "bare", "icon", "media-btn" }, Content = new Icon() };
            if (size is not null) b.Classes.Add(size);
            return b;
        }
        var (regular, md, lg) = (Make(null), Make("md"), Make("lg"));
        Show(new StackPanel { Children = { regular, md, lg } });
        Assert.Equal(44, regular.Bounds.Height, 1);
        Assert.Equal(56, md.Bounds.Height, 1);
        Assert.Equal(72, lg.Bounds.Height, 1);
        Assert.Equal(28, md.CornerRadius.TopLeft, 1);
        Assert.Equal(36, lg.CornerRadius.TopLeft, 1);
        Assert.Equal(34, ((Icon)lg.Content!).Size, 1);
    }
}
