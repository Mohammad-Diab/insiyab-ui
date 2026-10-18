using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class TaskbarTests
{
    private sealed class Recorder : IBadgeSink
    {
        public readonly List<(string? Text, string? Label)> Applied = [];
        public readonly List<TopLevel> Forgotten = [];

        public void Apply(TopLevel top, string? text, string? label) => Applied.Add((text, label));
        public void Forget(TopLevel top) => Forgotten.Add(top);
    }

    private static void With(Action<Recorder> body)
    {
        var before = Taskbar.Sink;
        var recorder = new Recorder();
        Taskbar.Sink = recorder;
        try
        {
            body(recorder);
        }
        finally
        {
            Taskbar.Sink = before;
        }
    }

    private static void Flush() => Settle(60);

    private static ResourceDictionary CountColours(Color light, Color dark) => new()
    {
        ThemeDictionaries =
        {
            [ThemeVariant.Light] = new ResourceDictionary { ["InsNotifyCountBg"] = new SolidColorBrush(light) },
            [ThemeVariant.Dark] = new ResourceDictionary { ["InsNotifyCountBg"] = new SolidColorBrush(dark) },
        },
    };

    [Theory]
    [InlineData(0, null)]
    [InlineData(-5, null)]
    [InlineData(1, "1")]
    [InlineData(10, "10")]
    [InlineData(99, "99")]
    [InlineData(100, "99+")]
    [InlineData(5000, "99+")]
    public void Badge_text_is_the_count_with_99_plus_above_99(int count, string? expected) =>
        Assert.Equal(expected, Taskbar.TextFor(count));

    [AvaloniaFact]
    public void Setting_the_badge_sends_the_text_and_the_label_once()
    {
        With(sink =>
        {
            var window = Show(new Panel());
            Taskbar.SetBadge(window, 3);
            Taskbar.SetBadge(window, 3);
            Flush();
            Taskbar.SetBadge(window, 3);
            Taskbar.SetBadgeLabel(window, null);
            Flush();
            var sent = Assert.Single(sink.Applied);
            Assert.Equal(("3", "3"), sent);
        });
    }

    [AvaloniaFact]
    public void Quick_changes_send_only_the_last_badge()
    {
        With(sink =>
        {
            var window = Show(new Panel());
            Taskbar.SetBadge(window, 1);
            Taskbar.SetBadge(window, 2);
            Taskbar.SetBadge(window, 3);
            Flush();
            Assert.Equal(("3", "3"), Assert.Single(sink.Applied));

            Taskbar.SetBadge(window, 4);
            Taskbar.SetBadge(window, 3);
            Flush();
            Assert.Single(sink.Applied);
        });
    }

    [AvaloniaFact]
    public void Zero_clears_the_badge()
    {
        With(sink =>
        {
            var window = Show(new Panel());
            Taskbar.SetBadge(window, 0);
            Taskbar.SetBadge(window, -5);
            Flush();
            Assert.Empty(sink.Applied);

            Taskbar.SetBadge(window, 3);
            Flush();
            Taskbar.SetBadge(window, 0);
            Flush();
            Assert.Equal(2, sink.Applied.Count);
            Assert.Equal(("3", "3"), sink.Applied[0]);
            Assert.Equal((null, null), sink.Applied[1]);
        });
    }

    [AvaloniaFact]
    public void BadgeLabel_replaces_the_spoken_text()
    {
        With(sink =>
        {
            var window = Show(new Panel());
            Taskbar.SetBadge(window, 3);
            Taskbar.SetBadgeLabel(window, "3 رسائل جديدة");
            Flush();
            Assert.Equal(("3", "3 رسائل جديدة"), Assert.Single(sink.Applied));

            Taskbar.SetBadgeLabel(window, null);
            Flush();
            Assert.Equal(2, sink.Applied.Count);
            Assert.Equal(("3", "3"), sink.Applied[1]);
        });
    }

    [AvaloniaFact]
    public void A_theme_change_sends_the_badge_again()
    {
        With(sink =>
        {
            var window = Show(new Panel());
            window.Resources = CountColours(Colors.Red, Colors.Teal);
            Taskbar.SetBadge(window, 3);
            Flush();
            Assert.Single(sink.Applied);

            window.RequestedThemeVariant = ThemeVariant.Dark;
            Flush();
            Assert.Equal(2, sink.Applied.Count);
            Assert.Equal(("3", "3"), sink.Applied[1]);
        });
    }

    [AvaloniaFact]
    public void A_closed_window_forgets_its_badge()
    {
        With(sink =>
        {
            var window = Show(new Panel());
            Taskbar.SetBadge(window, 3);
            Flush();
            Assert.DoesNotContain(window, sink.Forgotten);

            window.Close();
            Flush();
            Assert.Single(sink.Forgotten, w => ReferenceEquals(w, window));

            Taskbar.SetBadge(window, 4);
            Flush();
            Assert.Single(sink.Applied);
            Assert.Single(sink.Forgotten, w => ReferenceEquals(w, window));
        });
    }

    [AvaloniaFact]
    public void A_window_without_a_native_handle_never_reaches_windows()
    {
        var before = Taskbar.Sink;
        var windows = new WindowsBadge();
        Taskbar.Sink = windows;
        try
        {
            var window = Show(new Panel());
            Assert.NotEqual("HWND", window.TryGetPlatformHandle()?.HandleDescriptor);
            Assert.Null(Record.Exception(() => windows.Apply(window, "3", "3")));
            Assert.Null(Record.Exception(() => windows.Apply(window, null, null)));
            Assert.Null(Record.Exception(() => windows.Forget(window)));

            Taskbar.SetBadge(window, 3);
            Flush();
            Taskbar.SetBadge(window, 0);
            Flush();
            Assert.Equal(0, TaskbarNative.List);
        }
        finally
        {
            Taskbar.Sink = before;
        }
    }

    [AvaloniaFact]
    public void A_single_view_takes_a_badge_without_failing()
    {
        var notify = new Notify();
        for (var i = 0; i < 2; i++) notify.Items.Add(new NotifyItem { Title = "إشعار", IsUnread = true });
        var root = OverlaysTests.SingleView(new StackPanel { Children = { notify } });
        try
        {
            Taskbar.SetBadge(root, 4);
            Taskbar.SetBadgeLabel(root, "4 جديدة");
            OverlaysTests.Pump(root, 100);
            Assert.Equal(4, Taskbar.GetBadge(root));

            root.ClearValue(Taskbar.BadgeProperty);
            notify.ShowTaskbarBadge = true;
            OverlaysTests.Pump(root, 100);
            Assert.Equal(2, Taskbar.GetBadge(root));
            notify.MarkAllRead();
            OverlaysTests.Pump(root, 100);
            Assert.Equal(0, Taskbar.GetBadge(root));
        }
        finally
        {
            root.StopRendering();
        }
    }

    [AvaloniaFact]
    public void Dock_shows_the_last_window_that_still_has_a_badge()
    {
        var a = Show(new Panel());
        var b = Show(new Panel());
        var dock = new DockOrder();
        Assert.Equal("3", dock.Set(a, "3"));
        Assert.Equal("5", dock.Set(b, "5"));
        Assert.Equal("3", dock.Set(b, null));
        Assert.Null(dock.Forget(a));

        var later = new DockOrder();
        later.Set(a, "3");
        later.Set(b, "5");
        Assert.Equal("4", later.Set(a, "4"));
        Assert.Equal("5", later.Forget(a));
        Assert.Null(later.Forget(b));
    }

    private sealed class Shot(RenderTargetBitmap bitmap) : IDisposable
    {
        private readonly WriteableBitmap _copy = new(bitmap.PixelSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

        public int Size => bitmap.PixelSize.Width;

        public Color At(int x, int y)
        {
            using var buffer = _copy.Lock();
            bitmap.CopyPixels(buffer);
            var i = y * buffer.RowBytes + x * 4;
            byte Byte(int k) => Marshal.ReadByte(buffer.Address, i + k);
            return Color.FromArgb(Byte(3), Byte(2), Byte(1), Byte(0));
        }

        public IEnumerable<(int X, int Y, Color Pixel)> All()
        {
            using var buffer = _copy.Lock();
            bitmap.CopyPixels(buffer);
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                {
                    var i = y * buffer.RowBytes + x * 4;
                    byte Byte(int k) => Marshal.ReadByte(buffer.Address, i + k);
                    yield return (x, y, Color.FromArgb(Byte(3), Byte(2), Byte(1), Byte(0)));
                }
        }

        public void Dispose()
        {
            _copy.Dispose();
            bitmap.Dispose();
        }
    }

    private static bool Near(Color a, Color b, int tolerance = 4) =>
        Math.Abs(a.A - b.A) <= tolerance && Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;

    private static bool NearWhite(Color c) => c.A == 255 && c.R >= 200 && c.G >= 200 && c.B >= 200;

    [AvaloniaTheory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(32)]
    public void Icon_is_a_red_disc_with_white_digits(int size)
    {
        var window = Show(new Panel());
        using var shot = new Shot(BadgeIcon.Draw("3", size, Taskbar.FillFor(window), Taskbar.FontFor(window)));
        Assert.Equal(size, shot.Size);
        var top = shot.At(size / 2, (int)Math.Round(0.12 * size));
        Assert.True(Near(top, Color.Parse("#DC2626")), $"top of the disc is {top}");
        Assert.Equal(0, shot.At(0, 0).A);
        Assert.Equal(0, shot.At(size - 1, size - 1).A);
        Assert.True(shot.All().Count(p => NearWhite(p.Pixel)) >= 3, "the digit shows in white");
    }

    [AvaloniaTheory]
    [InlineData(16, "99+")]
    [InlineData(24, "99+")]
    [InlineData(32, "99+")]
    [InlineData(16, "88")]
    [InlineData(24, "88")]
    [InlineData(32, "88")]
    public void Ninety_nine_plus_stays_inside_the_disc(int size, string text)
    {
        var window = Show(new Panel());
        using var shot = new Shot(BadgeIcon.Draw(text, size, Taskbar.FillFor(window), Taskbar.FontFor(window)));
        var white = shot.All().Where(p => NearWhite(p.Pixel)).ToList();
        Assert.True(white.Count >= 3, "the digits show in white");
        var radius = size / 2.0;
        foreach (var (x, y, _) in white)
        {
            var reach = Math.Sqrt(Math.Pow(x + 0.5 - radius, 2) + Math.Pow(y + 0.5 - radius, 2));
            Assert.True(reach <= radius - 0.5, $"white at {x},{y} is {reach:0.00} from the centre of a disc of radius {radius}");
        }
    }

    [AvaloniaFact]
    public void Icon_follows_an_app_colour_for_the_count()
    {
        var window = Show(new Panel());
        Assert.Equal(Color.Parse("#DC2626"), Taskbar.FillFor(window));
        var teal = Color.Parse("#0D9488");
        window.Resources["InsNotifyCountBg"] = new SolidColorBrush(teal);
        Assert.Equal(teal, Taskbar.FillFor(window));
        using var shot = new Shot(BadgeIcon.Draw("3", 24, Taskbar.FillFor(window), Taskbar.FontFor(window)));
        Assert.True(Near(shot.At(12, 3), teal), $"the disc is {shot.At(12, 3)}");
    }

    private static Notify Bell(int unread = 3, bool badge = true)
    {
        var notify = new Notify { ShowTaskbarBadge = badge };
        for (var i = 0; i < 5; i++) notify.Items.Add(new NotifyItem { Title = "إشعار " + i, IsUnread = i < unread });
        return notify;
    }

    [AvaloniaFact]
    public void ShowTaskbarBadge_puts_the_unread_count_on_the_window()
    {
        var notify = Bell();
        var window = Show(new StackPanel { Children = { notify } });
        Assert.Equal(3, Taskbar.GetBadge(window));

        var late = Bell(2, badge: false);
        var other = Show(new StackPanel { Children = { late } });
        Assert.Equal(0, Taskbar.GetBadge(other));
        late.ShowTaskbarBadge = true;
        Assert.Equal(2, Taskbar.GetBadge(other));
    }

    [AvaloniaFact]
    public void The_window_badge_follows_reading_and_new_notifications()
    {
        var notify = Bell();
        var window = Show(new StackPanel { Children = { notify } });
        Assert.Equal(3, Taskbar.GetBadge(window));

        notify.MarkRead((NotifyItem)notify.Items[0]!);
        Assert.Equal(2, Taskbar.GetBadge(window));

        notify.Items.Insert(0, new NotifyItem { Title = "جديد", IsUnread = true });
        Assert.Equal(3, Taskbar.GetBadge(window));

        notify.MarkAllRead();
        Assert.Equal(0, Taskbar.GetBadge(window));

        for (var i = 0; i < 120; i++) notify.Items.Add(new NotifyItem { Title = "إشعار", IsUnread = true });
        Assert.Equal(120, Taskbar.GetBadge(window));
        Assert.Equal("99+", Taskbar.TextFor(Taskbar.GetBadge(window)));
    }

    [AvaloniaFact]
    public void Without_ShowTaskbarBadge_the_window_has_no_badge()
    {
        var notify = Bell(badge: false);
        var window = Show(new StackPanel { Children = { notify } });
        Assert.Equal(0, Taskbar.GetBadge(window));
        Assert.Null(Taskbar.GetBadgeLabel(window));
        notify.Items.Insert(0, new NotifyItem { Title = "جديد", IsUnread = true });
        notify.MarkAllRead();
        Assert.Equal(0, Taskbar.GetBadge(window));
        Assert.Null(Taskbar.GetBadgeLabel(window));
    }

    [AvaloniaFact]
    public void A_badge_the_app_sets_wins_over_the_bell()
    {
        var notify = Bell();
        var window = Show(new StackPanel { Children = { notify } });
        Taskbar.SetBadge(window, 7);
        Assert.Equal(7, Taskbar.GetBadge(window));
        notify.MarkRead((NotifyItem)notify.Items[0]!);
        Assert.Equal(7, Taskbar.GetBadge(window));

        window.ClearValue(Taskbar.BadgeProperty);
        Assert.Equal(2, Taskbar.GetBadge(window));
    }

    [AvaloniaFact]
    public void Turning_ShowTaskbarBadge_off_or_removing_the_bell_clears_the_badge()
    {
        var notify = Bell();
        var panel = new StackPanel { Children = { notify } };
        var window = Show(panel);
        Assert.Equal(3, Taskbar.GetBadge(window));

        notify.ShowTaskbarBadge = false;
        Assert.Equal(0, Taskbar.GetBadge(window));
        Assert.Null(Taskbar.GetBadgeLabel(window));

        notify.ShowTaskbarBadge = true;
        Assert.Equal(3, Taskbar.GetBadge(window));
        panel.Children.Remove(notify);
        Settle(60);
        Assert.Equal(0, Taskbar.GetBadge(window));
        Assert.Null(Taskbar.GetBadgeLabel(window));
        notify.MarkAllRead();
        notify.Items.Insert(0, new NotifyItem { Title = "جديد", IsUnread = true });
        Assert.Equal(0, Taskbar.GetBadge(window));
    }

    [AvaloniaFact]
    public void Two_bells_in_one_window_each_keep_their_own_badge_entry()
    {
        var first = Bell(3);
        var second = Bell(2);
        var panel = new StackPanel { Children = { first, second } };
        var window = Show(panel);
        Assert.Equal(2, Taskbar.GetBadge(window));

        panel.Children.Remove(second);
        Settle(60);
        Assert.Equal(3, Taskbar.GetBadge(window));
        panel.Children.Remove(first);
        Settle(60);
        Assert.Equal(0, Taskbar.GetBadge(window));
    }

    [AvaloniaFact]
    public void The_bell_label_is_the_badge_label()
    {
        With(sink =>
        {
            var notify = Bell();
            var window = Show(new StackPanel { Children = { notify } }, FlowDirection.RightToLeft);
            Assert.Equal("الإشعارات، 3 غير مقروءة", Taskbar.GetBadgeLabel(window));
            Flush();
            Assert.Equal(("3", "الإشعارات، 3 غير مقروءة"), sink.Applied[^1]);

            notify.MarkRead((NotifyItem)notify.Items[0]!);
            notify.MarkRead((NotifyItem)notify.Items[1]!);
            Assert.Equal("الإشعارات، إشعار واحد غير مقروء", Taskbar.GetBadgeLabel(window));
            Flush();
            Assert.Equal(("1", "الإشعارات، إشعار واحد غير مقروء"), sink.Applied[^1]);
        });
    }

    [Theory]
    [InlineData(1.0, 16)]
    [InlineData(1.25, 20)]
    [InlineData(1.5, 24)]
    [InlineData(1.75, 28)]
    [InlineData(2.0, 32)]
    public void Icon_size_follows_the_scaling(double scaling, int expected) =>
        Assert.Equal(expected, BadgeIcon.Pixels(scaling));
}
