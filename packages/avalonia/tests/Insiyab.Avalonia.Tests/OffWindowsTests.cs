using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
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

            TitleBar.SetExtends(window, false);
            Assert.False(window.ExtendClientAreaToDecorationsHint);
            Assert.Null(window.WindowDecorationsTheme);
        });
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
