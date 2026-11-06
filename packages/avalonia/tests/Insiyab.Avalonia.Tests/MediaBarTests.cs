using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MediaBarTests
{
    private static (Window Window, MediaBar Bar, List<MediaSeekEventArgs> Seeks) Host(FlowDirection dir = FlowDirection.LeftToRight, Action<MediaBar>? setup = null)
    {
        var bar = new MediaBar { Width = 400, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        setup?.Invoke(bar);
        var seeks = new List<MediaSeekEventArgs>();
        bar.SeekRequested += (_, e) => seeks.Add(e);
        var window = Show(new Panel { Children = { bar } }, dir);
        return (window, bar, seeks);
    }

    // A point on the bar's track, a share of the way from its left end.
    private static Point On(MediaBar bar, Window window, double share) =>
        bar.TranslatePoint(new Point(bar.Bounds.Width * share, bar.Bounds.Height - MediaBar.TrackFromBottom), window)!.Value;

    [AvaloniaFact]
    public void A_press_seeks_there_and_a_drag_follows_whatever_the_player_reports()
    {
        var (window, bar, seeks) = Host();
        window.MouseMove(On(bar, window, 0.25));
        window.MouseDown(On(bar, window, 0.25), MouseButton.Left);
        Settle(60);
        Assert.True(bar.IsDragging);
        Assert.Equal(0.25, seeks[^1].Value, 2);
        Assert.True(seeks[^1].IsDragging);

        bar.Value = 0.9;
        window.MouseMove(On(bar, window, 0.5));
        Settle(60);
        Assert.Equal(0.5, seeks[^1].Value, 2);
        Assert.Equal(0.5, bar.Shown, 2);

        window.MouseUp(On(bar, window, 0.5), MouseButton.Left);
        Settle(60);
        Assert.False(bar.IsDragging);
        Assert.False(seeks[^1].IsDragging);
        Assert.Equal(0.5, seeks[^1].Value, 2);
        Assert.Equal(0.5, bar.Value, 2);
    }

    [AvaloniaFact]
    public void The_bar_runs_left_to_right_in_an_arabic_page()
    {
        var (window, bar, seeks) = Host(FlowDirection.RightToLeft);
        Assert.Equal(FlowDirection.LeftToRight, bar.FlowDirection);
        var left = bar.TranslatePoint(new Point(40, bar.Bounds.Height - 9), window)!.Value;
        var right = bar.TranslatePoint(new Point(360, bar.Bounds.Height - 9), window)!.Value;
        Assert.True(left.X < right.X, "the bar's start must be on the screen's left");
        window.MouseDown(left, MouseButton.Left);
        window.MouseUp(left, MouseButton.Left);
        Settle(60);
        Assert.Equal(0.1, seeks[^1].Value, 2);
    }

    [AvaloniaFact]
    public void Nothing_seeks_into_the_lead_and_its_bubble_says_why()
    {
        var (window, bar, seeks) = Host(setup: b =>
        {
            b.Lead = 0.2;
            b.LeadTip = "لم تكن هنا بعد";
            b.Duration = TimeSpan.FromMinutes(10);
        });
        window.MouseMove(On(bar, window, 0.1));
        Settle(60);
        Assert.Equal("لم تكن هنا بعد", bar.Bubble?.Text);
        window.MouseDown(On(bar, window, 0.1), MouseButton.Left);
        window.MouseUp(On(bar, window, 0.1), MouseButton.Left);
        Settle(60);
        Assert.Equal(0.2, seeks[^1].Value, 3);
        Assert.Equal(TimeSpan.FromMinutes(2), seeks[^1].Time);
        window.MouseMove(On(bar, window, 0.5));
        Settle(60);
        Assert.Equal("5:00", bar.Bubble?.Text);
    }

    [AvaloniaFact]
    public void The_bubble_follows_the_pointer_names_the_mark_under_it_and_goes_on_leaving()
    {
        var (window, bar, _) = Host(setup: b =>
        {
            b.Duration = TimeSpan.FromSeconds(200);
            b.Markers!.Add(new MediaMarker { At = 0.75, Tip = "الخاتمة", Tone = "ok" });
        });
        window.MouseMove(On(bar, window, 0.25));
        Settle(80);
        Assert.Equal("0:50", bar.Bubble?.Text);
        var face = bar.BubbleFace!;
        var first = face.PointToScreen(new Point(face.Bounds.Width / 2, 0));
        window.MouseMove(On(bar, window, 0.75));
        Settle(80);
        Assert.Equal("2:30 · الخاتمة", bar.Bubble?.Text);
        face = bar.BubbleFace!;
        var second = face.PointToScreen(new Point(face.Bounds.Width / 2, 0));
        Assert.InRange(second.X - first.X, 190, 210);
        Assert.True(second.Y + face.Bounds.Height <= bar.PointToScreen(new Point(0, bar.Bounds.Height - MediaBar.TrackFromBottom - 14)).Y + 1, "the bubble must sit above the marks");
        window.MouseMove(new Point(5, 5));
        Settle(80);
        Assert.Null(bar.Bubble);
    }

    [AvaloniaFact]
    public void Keys_step_by_time_and_home_and_end_go_to_the_ends()
    {
        var (window, bar, seeks) = Host(setup: b =>
        {
            b.Duration = TimeSpan.FromSeconds(100);
            b.Value = 0.5;
            b.Lead = 0.1;
        });
        bar.Focus();
        void Press(Key key)
        {
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Settle(30);
        }
        Press(Key.Right);
        Assert.Equal(0.55, bar.Value, 3);
        Press(Key.Left);
        Press(Key.Left);
        Assert.Equal(0.45, bar.Value, 3);
        Press(Key.PageUp);
        Assert.Equal(0.75, bar.Value, 3);
        Press(Key.End);
        Assert.Equal(1, bar.Value, 3);
        Press(Key.Home);
        Assert.Equal(0.1, bar.Value, 3);
        Assert.False(seeks[^1].IsDragging);
        bar.Duration = TimeSpan.Zero;
        Press(Key.Right);
        Assert.Equal(0.11, bar.Value, 3);
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(65, "1:05")]
    [InlineData(599, "9:59")]
    [InlineData(3729, "1:02:09")]
    [InlineData(-3, "0:00")]
    public void The_clock_writes_minutes_and_from_an_hour_hours(int seconds, string expected) =>
        Assert.Equal(expected, MediaBar.Clock(TimeSpan.FromSeconds(seconds)));

    [AvaloniaFact]
    public void The_played_part_buffered_part_and_marks_paint_in_their_tones()
    {
        var (window, bar, _) = Host(setup: b =>
        {
            b.Value = 0.3;
            b.Buffered = 0.6;
            b.Markers!.Add(new MediaMarker { At = 0.9, Tone = "bad" });
            b.Markers!.Add(new MediaMarker { At = 0.8 });
            b.CurrentMarker = 0;
        });
        Settle();
        var frame = window.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        Color At(double share, double up)
        {
            var p = bar.TranslatePoint(new Point(bar.Bounds.Width * share, bar.Bounds.Height - MediaBar.TrackFromBottom - up), window)!.Value;
            return PixelAt(buffer, (int)p.X, (int)p.Y);
        }
        var brand = ColorOf((IBrush)Res(bar, "InsPrimaryMid"));
        Assert.Equal(brand, At(0.15, 0));
        Assert.Equal(brand, At(0.9, 11));
        Assert.NotEqual(At(0.5, 0), At(0.7, 0));
        Assert.NotEqual(brand, At(0.5, 0));
        Assert.NotEqual(brand, At(0.8, 9));
        Assert.NotEqual(At(0.85, 9), At(0.8, 9));
    }

    [AvaloniaFact]
    public void A_screen_reader_reads_and_moves_the_bar_as_a_slider_in_seconds()
    {
        var (_, bar, seeks) = Host(setup: b =>
        {
            b.Duration = TimeSpan.FromSeconds(120);
            b.Value = 0.25;
        });
        var peer = ControlAutomationPeer.CreatePeerForElement(bar);
        Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());
        Assert.Equal("Playback position", peer.GetName());
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(peer);
        Assert.Equal(30, range.Value, 3);
        Assert.Equal(120, range.Maximum, 3);
        Assert.Equal(5, range.SmallChange, 3);
        range.SetValue(60);
        Assert.Equal(0.5, bar.Value, 3);
        Assert.Equal(TimeSpan.FromSeconds(60), seeks[^1].Time);
    }

    [AvaloniaFact]
    public void An_arabic_window_names_the_bar_in_arabic()
    {
        var (_, bar, _) = Host(FlowDirection.RightToLeft);
        Assert.Equal("موضع التشغيل", ControlAutomationPeer.CreatePeerForElement(bar).GetName());
    }

    [AvaloniaFact]
    public void Marks_written_in_markup_are_drawn()
    {
        var bar = global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Parse<MediaBar>(
            """
            <ins:MediaBar xmlns="https://github.com/avaloniaui" xmlns:ins="https://github.com/Mohammad-Diab/insiyab-ui" Value="0.4" Duration="0:4:00" CurrentMarker="1">
              <ins:MediaBar.Markers>
                <ins:MediaMarker At="0.25" Tip="المقدمة" />
                <ins:MediaMarker At="0.6" Tip="الشرح" Tone="brand" />
              </ins:MediaBar.Markers>
            </ins:MediaBar>
            """, typeof(MediaBar).Assembly);
        Assert.Equal(2, bar.Markers!.Count);
        Assert.Equal("brand", bar.Markers[1].Tone);
        Assert.Equal(TimeSpan.FromMinutes(4), bar.Duration);
    }
}
