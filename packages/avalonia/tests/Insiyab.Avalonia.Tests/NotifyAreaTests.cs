using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The shell is always the recorder here: no test ever adds a real icon or shows a real balloon.
public class NotifyAreaTests
{
    private sealed class Recorder : INotifyAreaShell
    {
        public bool Accepts = true;
        public int Picks;
        public readonly List<string> Calls = [];
        public IReadOnlyList<NotifyAreaMenuItem>? Shown;
        public bool ShownRtl;

        public Action<NotifyAreaEvent, int, int>? Raised { get; set; }

        public bool Add(string tip)
        {
            Calls.Add("add " + tip);
            return Accepts;
        }

        public void SetTip(string tip) => Calls.Add("tip " + tip);

        public void ShowBalloon(string title, string text, NotifyAreaLevel level) => Calls.Add($"balloon {title}|{text}|{level}");

        public int ShowMenu(IReadOnlyList<NotifyAreaMenuItem> rows, int x, int y, bool rightToLeft)
        {
            Shown = rows;
            ShownRtl = rightToLeft;
            Calls.Add($"menu {x},{y}");
            return Picks;
        }

        public void Dispose() => Calls.Add("dispose");
    }

    private static void With(Recorder? shell, Action body)
    {
        var before = NotifyAreaIcon.MakeShell;
        NotifyAreaIcon.MakeShell = _ => shell;
        try
        {
            body();
        }
        finally
        {
            NotifyAreaIcon.MakeShell = before;
        }
    }

    private static readonly byte[] NoIco = [0, 0, 1, 0, 0, 0];

    [AvaloniaFact]
    public void The_icon_is_added_with_its_tip_cut_to_what_windows_keeps()
    {
        var shell = new Recorder();
        With(shell, () =>
        {
            using var icon = new NotifyAreaIcon(NoIco, new string('ب', 200));
            Assert.True(icon.IsAdded);
            Assert.Equal("add " + new string('ب', 127), shell.Calls[0]);
            icon.Tip = "تطبيق";
            Assert.Equal("tip تطبيق", shell.Calls[^1]);
        });
        Assert.Equal("dispose", shell.Calls[^1]);
    }

    [AvaloniaFact]
    public void A_balloon_waits_for_the_icon_and_its_click_brings_back_its_tag()
    {
        var shell = new Recorder();
        With(shell, () =>
        {
            using var icon = new NotifyAreaIcon(NoIco, "tip");
            object? clicked = null;
            icon.BalloonClicked += (_, e) => clicked = e.Tag;
            icon.ShowBalloon(new string('a', 80), new string('b', 300), NotifyAreaLevel.Warning, tag: 7);
            Assert.Equal($"balloon {new string('a', 63)}|{new string('b', 255)}|Warning", shell.Calls[^1]);
            shell.Raised!(NotifyAreaEvent.BalloonClicked, 0, 0);
            Assert.Equal(7, clicked);
        });

        var refused = new Recorder { Accepts = false };
        With(refused, () =>
        {
            using var icon = new NotifyAreaIcon(NoIco, "tip");
            Assert.False(icon.IsAdded);
            icon.ShowBalloon("a", "b");
            Assert.DoesNotContain(refused.Calls, c => c.StartsWith("balloon", StringComparison.Ordinal));
        });
    }

    [AvaloniaFact]
    public void The_menu_is_written_as_it_opens_and_runs_only_the_row_picked()
    {
        var shell = new Recorder { Picks = 3 };
        With(shell, () =>
        {
            using var icon = new NotifyAreaIcon(NoIco, "tip");
            var ran = new List<string>();
            var open = new NotifyAreaMenuItem("فتح", (_, _) => ran.Add("open"));
            var mute = new NotifyAreaMenuItem("كتم", (_, _) => ran.Add("mute")) { IsChecked = true };
            var quit = new NotifyAreaMenuItem("خروج", (_, _) => ran.Add("quit"));
            icon.Menu.Add(open);
            icon.Menu.Add(NotifyAreaMenuItem.Separator());
            icon.Menu.Add(mute);
            icon.Menu.Add(quit);
            var opening = 0;
            icon.MenuOpening += (_, _) => opening++;
            shell.Raised!(NotifyAreaEvent.Menu, 120, 900);
            Assert.Equal(1, opening);
            Assert.Equal("menu 120,900", shell.Calls[^1]);
            Assert.Equal(4, shell.Shown!.Count);
            Assert.Equal(new[] { "mute" }, ran);
            mute.IsEnabled = false;
            shell.Raised!(NotifyAreaEvent.Menu, 0, 0);
            Assert.Equal(new[] { "mute" }, ran);
            shell.Picks = 2;
            shell.Raised!(NotifyAreaEvent.Menu, 0, 0);
            Assert.Equal(new[] { "mute" }, ran);
            shell.Picks = 0;
            shell.Raised!(NotifyAreaEvent.Menu, 0, 0);
            Assert.Equal(new[] { "mute" }, ran);
        });
    }

    [AvaloniaFact]
    public void The_menu_reads_right_to_left_for_arabic()
    {
        var shell = new Recorder();
        With(shell, () =>
        {
            using var icon = new NotifyAreaIcon(NoIco, "tip");
            icon.Menu.Add(new NotifyAreaMenuItem("فتح"));
            shell.Raised!(NotifyAreaEvent.Menu, 0, 0);
            Assert.False(shell.ShownRtl);
            try
            {
                InsiyabTheme.SetLanguage("ar");
                shell.Raised!(NotifyAreaEvent.Menu, 0, 0);
                Assert.True(shell.ShownRtl);
                icon.MenuDirection = FlowDirection.LeftToRight;
                shell.Raised!(NotifyAreaEvent.Menu, 0, 0);
                Assert.False(shell.ShownRtl);
            }
            finally
            {
                InsiyabTheme.SetLanguage(null);
            }
        });
    }

    [AvaloniaFact]
    public void Explorer_restarting_adds_the_icon_again_and_says_so()
    {
        var shell = new Recorder();
        With(shell, () =>
        {
            using var icon = new NotifyAreaIcon(NoIco, "tip");
            var changes = new List<bool>();
            icon.IsAddedChanged += (_, _) => changes.Add(icon.IsAdded);
            var activated = 0;
            icon.Activated += (_, _) => activated++;
            shell.Raised!(NotifyAreaEvent.ShellRestarted, 0, 0);
            Assert.Equal("add tip", shell.Calls[^1]);
            Assert.Equal(new[] { true }, changes);
            shell.Accepts = false;
            shell.Raised!(NotifyAreaEvent.ShellRestarted, 0, 0);
            Assert.Equal(new[] { true, false }, changes);
            shell.Raised!(NotifyAreaEvent.Activated, 0, 0);
            Assert.Equal(1, activated);
        });
    }

    [AvaloniaFact]
    public void With_no_shell_nothing_shows_and_nothing_fails()
    {
        With(null, () =>
        {
            var icon = new NotifyAreaIcon(NoIco, "tip");
            Assert.False(icon.IsAdded);
            icon.Tip = "x";
            icon.ShowBalloon("a", "b");
            icon.Dispose();
            icon.Dispose();
        });
    }

    [Fact]
    public void Callback_messages_decode_to_a_click_a_menu_and_a_balloon_click()
    {
        (int, int)? cursor = (40, 50);
        Assert.Equal((NotifyAreaEvent.Activated, 0, 0), NotifyArea.Decode(0, 0x0400, () => cursor));
        Assert.Equal((NotifyAreaEvent.Activated, 0, 0), NotifyArea.Decode(0, 0x0401, () => cursor));
        Assert.Equal((NotifyAreaEvent.BalloonClicked, 0, 0), NotifyArea.Decode(0, 0x0405, () => cursor));
        Assert.Equal((NotifyAreaEvent.Menu, 300, 1000), NotifyArea.Decode((1000 << 16) | 300, 0x0001007B, () => cursor));
        Assert.Equal((NotifyAreaEvent.Menu, 40, 50), NotifyArea.Decode(0, 0x007B, () => cursor));
        Assert.Equal((NotifyAreaEvent.Menu, -20, 5), NotifyArea.Decode((5 << 16) | 0xFFEC, 0x007B, () => cursor));
        Assert.Null(NotifyArea.Decode(0, 0x0200, () => cursor));
    }

    // An .ico with pictures of these widths, each entry pointing at a few bytes of its own.
    private static byte[] Ico(params int[] widths)
    {
        var data = new List<byte> { 0, 0, 1, 0, (byte)widths.Length, 0 };
        var offset = 6 + 16 * widths.Length;
        foreach (var w in widths)
        {
            data.AddRange([(byte)(w >= 256 ? 0 : w), (byte)(w >= 256 ? 0 : w), 0, 0, 1, 0, 32, 0]);
            data.AddRange(BitConverter.GetBytes(4));
            data.AddRange(BitConverter.GetBytes(offset));
            offset += 4;
        }
        foreach (var w in widths) data.AddRange([1, 2, 3, (byte)(w & 0xFF)]);
        return [.. data];
    }

    [Theory]
    [InlineData(16, 16)]
    [InlineData(20, 24)]
    [InlineData(24, 24)]
    [InlineData(40, 48)]
    [InlineData(300, 256)]
    public void The_picture_nearest_each_size_is_taken_from_the_ico(int size, int expected)
    {
        var ico = Ico(48, 16, 256, 24, 32);
        var pick = NotifyArea.Pick(ico, size);
        Assert.NotNull(pick);
        Assert.Equal(expected & 0xFF, ico[pick.Value.Offset + 3]);
        Assert.Null(NotifyArea.Pick([1, 2, 3], 16));
        Assert.Null(NotifyArea.Pick(Ico(), 16));
    }

    [Fact]
    public void Balloon_flags_pick_the_icon_and_keep_quiet_hours()
    {
        Assert.Equal(0x03u | 0x80, NotifyArea.BalloonFlags(NotifyAreaLevel.Error, true));
        Assert.Equal(0x02u | 0x80, NotifyArea.BalloonFlags(NotifyAreaLevel.Warning, true));
        Assert.Equal(0x04u | 0x20 | 0x80, NotifyArea.BalloonFlags(NotifyAreaLevel.Info, true));
        Assert.Equal(0x01u | 0x80, NotifyArea.BalloonFlags(NotifyAreaLevel.Info, false));
    }

    [Fact]
    public void Texts_are_cut_whole_with_a_zero_at_their_end()
    {
        var buffer = new ushort[8];
        NotifyArea.Write(buffer, "abcdefghij");
        Assert.Equal("abcdefg", new string(buffer.Take(7).Select(c => (char)c).ToArray()));
        Assert.Equal(0, buffer[7]);
        Assert.Equal("ab", NotifyAreaIcon.Cut("ab\U0001F600", 3));
        Assert.Equal("ab\U0001F600", NotifyAreaIcon.Cut("ab\U0001F600", 4));
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void The_native_records_have_the_sizes_windows_expects()
    {
        Assert.Equal(IntPtr.Size == 8 ? 976 : 956, Unsafe.SizeOf<NotifyAreaNative.NotifyIconData>());
        Assert.Equal(IntPtr.Size == 8 ? 80 : 48, Unsafe.SizeOf<NotifyAreaNative.WindowClass>());
    }

    [AvaloniaFact]
    public void Hiding_to_the_tray_minimises_without_a_taskbar_button_and_restoring_brings_the_state_back()
    {
        var window = Show(new Panel());
        window.WindowState = WindowState.Maximized;
        NotifyAreaIcon.HideToTray(window);
        Assert.Equal(WindowState.Minimized, window.WindowState);
        Assert.False(window.ShowInTaskbar);
        Assert.True(window.IsVisible);
        NotifyAreaIcon.HideToTray(window);
        NotifyAreaIcon.RestoreFromTray(window);
        Assert.Equal(WindowState.Maximized, window.WindowState);
        Assert.True(window.ShowInTaskbar);
    }
}
