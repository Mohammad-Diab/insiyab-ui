using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Insiyab.Avalonia.Controls;
using Tmds.DBus.Protocol;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Linux's reduced motion, dock badge and picture window (insiyab-notes 165 L3, L4, L6): their pure parts, on any system. No test talks to a bus or a display.
public class LinuxGapsTests
{
    // ---- L3: reduced motion ----

    [Theory]
    [InlineData(null, null, null, false)]
    [InlineData(1u, null, null, true)]
    [InlineData(0u, null, null, false)]
    [InlineData(null, false, null, true)]
    [InlineData(null, true, null, false)]
    [InlineData(null, null, 0.0, true)]
    [InlineData(null, null, 0.5, false)]
    [InlineData(null, null, 1.0, false)]
    [InlineData(0u, true, 0.0, true)] // any one asking for less wins
    public void Any_desktop_setting_asking_for_less_motion_wins(uint? reduced, bool? gnome, double? kde, bool expected) =>
        Assert.Equal(expected, LinuxMotionSettings.Reduces(new LinuxMotionState(reduced, gnome, kde)));

    [Fact]
    public void Portal_values_are_read_through_their_variant_wrappings()
    {
        var state = default(LinuxMotionState);
        // the older Read wraps a value twice
        state = LinuxMotionSettings.With(state, LinuxMotionSettings.Gnome, LinuxMotionSettings.GnomeAnimations, VariantValue.Variant(VariantValue.Variant(VariantValue.Bool(false))));
        Assert.False(state.GnomeAnimations);
        state = LinuxMotionSettings.With(state, LinuxMotionSettings.Appearance, LinuxMotionSettings.ReducedMotion, VariantValue.UInt32(1));
        Assert.Equal(1u, state.ReducedMotion);
        // KDE's portal gives the file's text
        state = LinuxMotionSettings.With(state, LinuxMotionSettings.Kde, LinuxMotionSettings.KdeFactor, VariantValue.String("0"));
        Assert.Equal(0d, state.KdeFactor);
        state = LinuxMotionSettings.With(state, LinuxMotionSettings.Kde, LinuxMotionSettings.KdeFactor, VariantValue.Double(0.25));
        Assert.Equal(0.25, state.KdeFactor);
        // a setting removed (back to its default) comes as an empty text
        state = LinuxMotionSettings.With(state, LinuxMotionSettings.Kde, LinuxMotionSettings.KdeFactor, VariantValue.String(""));
        Assert.Null(state.KdeFactor);
        // other settings leave it as it was
        var before = state;
        Assert.Equal(before, LinuxMotionSettings.With(state, LinuxMotionSettings.Appearance, "color-scheme", VariantValue.UInt32(1)));
        Assert.Equal(before, LinuxMotionSettings.With(state, "org.gnome.desktop.a11y", LinuxMotionSettings.GnomeAnimations, VariantValue.Bool(true)));
    }

    [Fact]
    public void Kde_animation_speed_is_read_from_its_group_in_kdeglobals()
    {
        Assert.Equal(0d, LinuxMotionSettings.KdeFactorFromFile("[General]\nAnimationDurationFactor=2\n\n[KDE]\n# speed\nAnimationDurationFactor=0\nLookAndFeelPackage=org.kde.breeze.desktop\n"));
        Assert.Equal(0.5, LinuxMotionSettings.KdeFactorFromFile("[KDE]\r\nAnimationDurationFactor[$i]=0.5\r\n"));
        Assert.Null(LinuxMotionSettings.KdeFactorFromFile("[General]\nAnimationDurationFactor=0\n"));
        Assert.Null(LinuxMotionSettings.KdeFactorFromFile("[KDE]\nSingleClick=false\n"));
        Assert.Null(LinuxMotionSettings.KdeFactorFromFile("[KDE]\nAnimationDurationFactor=fast\n"));
        Assert.Null(LinuxMotionSettings.KdeFactorFromFile(""));
    }

    [Fact]
    public void Kdeglobals_is_in_the_xdg_config_folder_and_counts_only_in_a_kde_session()
    {
        Assert.Equal(Path.Combine("/cfg", "kdeglobals"), LinuxMotionSettings.KdeFile("/cfg", "/home/a"));
        Assert.Equal(Path.Combine("/home/a", ".config", "kdeglobals"), LinuxMotionSettings.KdeFile(null, "/home/a"));
        Assert.Equal(Path.Combine("/home/a", ".config", "kdeglobals"), LinuxMotionSettings.KdeFile("relative", "/home/a"));
        Assert.Null(LinuxMotionSettings.KdeFile(null, null));
        Assert.True(LinuxMotionSettings.IsKde("KDE"));
        Assert.True(LinuxMotionSettings.IsKde("ubuntu:KDE"));
        Assert.False(LinuxMotionSettings.IsKde("ubuntu:GNOME"));
        Assert.False(LinuxMotionSettings.IsKde(null));
    }

    [AvaloniaFact]
    public void A_linux_motion_change_the_library_sees_updates_the_windows_and_says_so()
    {
        var window = Show(new Panel());
        var told = 0;
        EventHandler count = (_, _) => told++;
        Motion.SystemChanged += count;
        try
        {
            Assert.DoesNotContain(Motion.StillPseudoClass, window.Classes);
            Motion.SystemOverride = true; // the desktop now asks for less motion
            Motion.SystemSettingChanged();
            Assert.Equal(1, told);
            Assert.Contains(Motion.StillPseudoClass, window.Classes);
        }
        finally
        {
            Motion.SystemChanged -= count;
            Motion.SystemOverride = null;
            Motion.Refresh(window);
            window.Close();
        }
    }

    // ---- L4: the dock badge ----

    [Fact]
    public void The_launcher_entry_names_the_app_by_its_menu_entry()
    {
        Assert.Equal("application://my-app.desktop", LauncherEntry.AppUri("my-app.desktop"));
        Assert.Equal("my-app.desktop", LauncherEntry.DesktopFile("my-app.desktop", "/usr/share/applications/other.desktop", "/opt/app/App"));
        Assert.Equal("my-app.desktop", LauncherEntry.DesktopFile("my-app", null, null));
        Assert.Equal("other.desktop", LauncherEntry.DesktopFile(null, "/usr/share/applications/other.desktop", "/opt/app/App"));
        Assert.Equal("App.desktop", LauncherEntry.DesktopFile(null, null, "/opt/app/App"));
        Assert.Null(LauncherEntry.DesktopFile(null, null, null));
    }

    [Fact]
    public void The_launcher_entry_path_is_a_valid_object_path_one_per_app()
    {
        var path = LauncherEntry.ObjectPath("my-app.desktop");
        Assert.StartsWith("/com/canonical/unity/launcherentry/", path);
        Assert.Matches("^(/[A-Za-z0-9_]+)+$", path);
        Assert.Equal(path, LauncherEntry.ObjectPath("my-app.desktop"));
        Assert.NotEqual(path, LauncherEntry.ObjectPath("other.desktop"));
    }

    [Theory]
    [InlineData(null, 0L)]
    [InlineData("3", 3L)]
    [InlineData("250", 250L)]
    [InlineData("0", 0L)]
    [InlineData("99+", 0L)]
    public void The_dock_gets_the_window_count_itself(string? shown, long expected) =>
        Assert.Equal(expected, LauncherEntry.CountOf(shown));

    private sealed class CountingSink : IBadgeSink
    {
        public readonly List<int> Counts = [];

        bool IBadgeSink.Counts => true;

        public void Apply(TopLevel top, string? text, string? label) => Counts.Add(Taskbar.GetBadge(top));

        public void Forget(TopLevel top)
        {
        }
    }

    [AvaloniaFact]
    public void A_sink_that_draws_the_count_itself_gets_every_count_above_99()
    {
        var before = Taskbar.Sink;
        var sink = new CountingSink();
        Taskbar.Sink = sink;
        try
        {
            var window = Show(new Panel());
            Taskbar.SetBadge(window, 100);
            Settle(60);
            Taskbar.SetBadge(window, 101);
            Settle(60);
            Assert.Equal([100, 101], sink.Counts);
            window.Close();
        }
        finally
        {
            Taskbar.Sink = before;
        }
    }

    [AvaloniaFact]
    public void Linux_picks_the_dock_badge_only_on_linux_itself()
    {
        Os.Fake = OsKind.Linux;
        try
        {
            var sink = Taskbar.Default();
            if (OperatingSystem.IsLinux()) Assert.IsType<LauncherBadge>(sink);
            else Assert.IsType<NoBadge>(sink);
        }
        finally
        {
            Os.Fake = null;
        }
    }

    // ---- L6: the picture window ----

    [Fact]
    public void The_aspect_hints_keep_the_other_hints_and_take_the_frame_as_the_base()
    {
        var avalonia = new X11SizeHints.SizeHints { Flags = (1 << 4) | (1 << 5) };
        var hints = X11SizeHints.WithAspect(avalonia, 16d / 9, new PixelSize(26, 26));
        Assert.Equal((nint)((1 << 4) | (1 << 5) | X11SizeHints.PAspect | X11SizeHints.PBaseSize), hints.Flags);
        Assert.Equal(17778, hints.MinAspectX);
        Assert.Equal(17778, hints.MaxAspectX);
        Assert.Equal(10000, hints.MinAspectY);
        Assert.Equal(10000, hints.MaxAspectY);
        Assert.Equal(26, hints.BaseWidth);
        Assert.Equal(26, hints.BaseHeight);
        var free = X11SizeHints.WithAspect(hints, 0, new PixelSize(26, 26));
        Assert.Equal((nint)((1 << 4) | (1 << 5)), free.Flags);
    }

    [Fact]
    public void A_resize_that_kept_the_shape_is_left_alone()
    {
        Assert.Null(PictureWindow.Settled(new Size(384, 216), new Size(480, 270), 16d / 9, PictureWindow.Least));
        Assert.Null(PictureWindow.Settled(new Size(384, 216), new Size(481, 271), 16d / 9, PictureWindow.Least)); // within 1%
        Assert.Null(PictureWindow.Settled(new Size(384, 216), new Size(500, 200), 0, PictureWindow.Least)); // any shape
    }

    [Fact]
    public void A_resize_off_the_shape_is_set_back_led_by_the_side_that_changed_more()
    {
        // the width was dragged: the height follows
        Assert.Equal(new Size(480, 270), PictureWindow.Settled(new Size(384, 216), new Size(480, 216), 16d / 9, PictureWindow.Least));
        // the height was dragged: the width follows
        Assert.Equal(new Size(533, 300), PictureWindow.Settled(new Size(384, 216), new Size(384, 300), 16d / 9, PictureWindow.Least));
        // never under the least
        Assert.Equal(new Size(192, 108), PictureWindow.Settled(new Size(384, 216), new Size(100, 216), 16d / 9, PictureWindow.Least));
    }

    [AvaloniaFact]
    public void Off_linux_the_picture_window_has_no_frame_room_and_keeps_its_least_size()
    {
        var window = new PictureWindow();
        Assert.Equal(default, WindowFrame.InsetOf(window));
        Assert.Equal(PictureWindow.Least.Width, window.MinWidth);
        Assert.Equal(PictureWindow.Least.Height, window.MinHeight);
    }
}
