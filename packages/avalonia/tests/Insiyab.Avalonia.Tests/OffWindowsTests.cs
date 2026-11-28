using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The library off Windows: on a phone the Windows parts stay quiet and "Remove animations" makes it still; on Linux the title bar is drawn as on Windows.
public class OffWindowsTests
{
    private static void As(OsKind os, Action action)
    {
        Os.Fake = os;
        try { action(); }
        finally
        {
            Os.Fake = null;
            AndroidDevice.AnimatorScaleSource = null;
            Motion.Refresh();
        }
    }

    [AvaloniaFact]
    public void On_linux_the_title_bar_is_drawn_and_the_library_moves_the_window()
    {
        As(OsKind.Linux, () =>
        {
            var button = new Button { Content = "بحث" };
            var bar = new Border { Background = Brushes.Transparent, Child = new StackPanel { Children = { button, new TextBlock { Text = "العنوان" } } } };
            TitleBar.SetIsDragArea(bar, true);
            var window = new Window { Width = 640, Height = 400, Content = bar };
            TitleBar.SetExtends(window, true);
            window.Show();
            Track(window);
            Settle();
            Assert.True(window.ExtendClientAreaToDecorationsHint);
            Assert.Equal(40, window.ExtendClientAreaTitleBarHeightHint);
            Assert.NotNull(window.WindowDecorationsTheme);
            // no caption role on Linux: that role takes the press before the app sees it, so the library moves the window and maximises on a double-click
            Assert.Equal(WindowDecorationsElementRole.None, WindowDecorationProperties.GetElementRole(bar));
            Assert.Equal(WindowDecorationsElementRole.User, WindowDecorationProperties.GetElementRole(button));

            // Avalonia's X11 frame, shadow and resize grips get room: a shadow only where a compositor shows see-through pixels
            Assert.Same(TitleBar.LinuxDecorations(window), window.WindowDecorationsTheme);

            TitleBar.SetExtends(window, false);
            Assert.False(window.ExtendClientAreaToDecorationsHint);
            Assert.Null(window.WindowDecorationsTheme);
        });
    }

    private static Thickness Room(ControlTheme theme, AvaloniaProperty property) =>
        (Thickness)theme.Setters.OfType<Setter>().Single(s => s.Property == property).Value!;

    [AvaloniaFact]
    public void On_linux_the_decorations_leave_room_for_a_shadow_and_the_resize_grips()
    {
        Assert.Equal(new Thickness(WindowFrame.ShadowRoom), Room(TitleBar.LinuxSeeThrough, WindowDrawnDecorations.DefaultShadowThicknessProperty));
        Assert.Equal(new Thickness(1), Room(TitleBar.LinuxSeeThrough, WindowDrawnDecorations.DefaultFrameThicknessProperty));
        // no compositor: no shadow, a grip band along the edge
        Assert.Equal(new Thickness(0), Room(TitleBar.LinuxSolid, WindowDrawnDecorations.DefaultShadowThicknessProperty));
        Assert.Equal(new Thickness(WindowFrame.SolidGrip), Room(TitleBar.LinuxSolid, WindowDrawnDecorations.DefaultFrameThicknessProperty));
    }

    [AvaloniaFact]
    public void The_linux_frame_draws_a_shadow_a_hairline_and_round_corners_only_while_restored()
    {
        // restored with a compositor: frame 1 + shadow 12 on each side, the top also holds the 40 of the title bar
        var restored = new Thickness(13, 53, 13, 13);
        var look = WindowFrame.For(true, true, restored, 1, 8);
        Assert.Equal(new Thickness(13), look.Padding);
        Assert.Equal(8, look.Radius);
        Assert.Equal(1, look.Hairline);
        Assert.True(look.Shadow);
        Assert.Equal(12, look.Extents);

        // at 150 % the window manager hears device pixels: 20 px of margin, the outer one the hairline
        var scaled = WindowFrame.For(true, true, new Thickness(40d / 3, 160d / 3, 40d / 3, 40d / 3), 1.5, 8);
        Assert.Equal(19, scaled.Extents);
        Assert.Equal(2d / 3, scaled.Hairline, 6);

        // maximised or full screen: no margin, so nothing at all
        Assert.Equal(default, WindowFrame.For(true, true, new Thickness(0, 40, 0, 0), 1, 8));

        // no compositor: a 1 px line round the whole window, square corners, no shadow
        var solid = WindowFrame.For(true, false, new Thickness(4, 44, 4, 4), 1, 8);
        Assert.Equal(new Thickness(1), solid.Padding);
        Assert.Equal(0, solid.Radius);
        Assert.False(solid.Shadow);
        Assert.Equal(0, solid.Extents);

        // not a Linux window drawing its own title bar
        Assert.Equal(default, WindowFrame.For(false, true, restored, 1, 8));

        Assert.Equal(12, WindowFrame.DesktopRadius("ubuntu:GNOME"));
        Assert.Equal(8, WindowFrame.DesktopRadius("KDE"));
        Assert.Equal(8, WindowFrame.DesktopRadius(null));
    }

    [AvaloniaFact]
    public void Off_linux_the_window_frame_leaves_the_window_as_it_was()
    {
        var window = Show(new Border());
        var frame = window.GetVisualDescendants().OfType<WindowFrame>().Single();
        Assert.Equal(default, frame.Padding);
        Assert.Null(((Visual)frame.Child!).Clip);
    }

    [AvaloniaFact]
    public void The_windows_parts_do_nothing_on_android()
    {
        As(OsKind.Android, () =>
        {
            var caps = new CaptionButtons();
            var window = new Window { Width = 640, Height = 400, Content = new Border { Classes = { "topbar" }, Child = caps } };
            TitleBar.SetExtends(window, true);
            window.Show();
            Track(window);
            Settle();
            Assert.False(window.ExtendClientAreaToDecorationsHint);
            Assert.DoesNotContain(":shown", caps.Classes);

            Assert.False(NotifyAreaIcon.IsSupported);
            using var stream = AssetLoader.Open(new Uri("avares://Insiyab.Avalonia.Gallery/Assets/gallery.ico"));
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            using var icon = new NotifyAreaIcon(bytes.ToArray(), "معرض");
            Assert.False(icon.IsAdded);
            icon.ShowBalloon("عنوان", "نص", NotifyAreaLevel.Info, null);

            Assert.IsType<NoBadge>(Taskbar.Default());
            Taskbar.SetBadge(window, 3);
            Settle(100);

            Assert.False(Motion.SystemReducesMotion);
            Assert.Null(AndroidDevice.AnimatorScale);
            Assert.False(Frost.IsMaterialOn(window));
        });
    }

    [AvaloniaFact]
    public void Remove_animations_on_android_makes_every_view_still()
    {
        As(OsKind.Android, () =>
        {
            AndroidDevice.AnimatorScaleSource = () => 0f;
            var inner = new Button { Content = "زر" };
            var window = Show(new StackPanel { Children = { inner } });
            Motion.Refresh(window);
            Assert.True(Motion.SystemReducesMotion);
            Assert.True(Motion.IsStill(inner));
            Assert.Contains(Motion.StillPseudoClass, window.Classes);

            AndroidDevice.AnimatorScaleSource = () => 0.5f;
            Motion.Refresh(window);
            Assert.False(Motion.IsStill(inner));
            Assert.DoesNotContain(Motion.StillPseudoClass, window.Classes);
        });
    }

    [AvaloniaFact]
    public void The_android_readers_are_quiet_elsewhere()
    {
        Assert.Null(AndroidDevice.AnimatorScale);
        Assert.False(AndroidDevice.IsLowMemory);
    }
}
