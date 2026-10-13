using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class DayNightTests
{
    // The theme here is the app's, so the window follows it instead of pinning one as Ui.Show does.
    private static Window Following(Control content, FlowDirection dir = FlowDirection.LeftToRight)
    {
        var window = new Window { Width = 400, Height = 200, FlowDirection = dir, Content = new StackPanel { Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Left, Children = { content } } };
        window.Show();
        Track(window);
        Settle();
        return window;
    }

    private static void Reset() => Application.Current!.RequestedThemeVariant = ThemeVariant.Light;

    private static double X(Visual v) => v.RenderTransform?.Value.M31 ?? 0;

    [AvaloniaFact]
    public void Theme_toggle_flips_sets_and_follows_the_system()
    {
        Reset();
        try
        {
            var toggle = new Button { Content = "تبديل الوضع" };
            ThemeToggle.SetMode(toggle, "toggle");
            var system = new Button { Content = "اتبع النظام" };
            ThemeToggle.SetMode(system, "system");
            var dark = new Button { Content = "داكن" };
            ThemeToggle.SetMode(dark, "dark");
            var window = Following(new StackPanel { Children = { toggle, system, dark } });

            Click(toggle, window);
            Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
            Assert.True(ThemeToggle.IsDark);
            Click(toggle, window);
            Assert.Equal(ThemeVariant.Light, Application.Current.RequestedThemeVariant);
            Click(dark, window);
            Click(dark, window);
            Assert.Equal(ThemeVariant.Dark, Application.Current.RequestedThemeVariant);
            Click(system, window);
            Assert.Equal(ThemeVariant.Default, Application.Current.RequestedThemeVariant);
            Assert.Equal("light", ThemeToggle.Apply("dusk"));
            Assert.Equal("dark", ThemeToggle.Apply("dark"));
        }
        finally { Reset(); }
    }

    [AvaloniaFact]
    public void Day_night_is_an_island_holding_the_web_sky()
    {
        Reset();
        try
        {
            var dn = new DayNight();
            Following(dn);
            Assert.Equal(new Size(86, 48), dn.Bounds.Size);
            var sky = Part<Border>(dn, "PART_Sky");
            Assert.Equal(new Size(72, 36), sky.Bounds.Size);
            Assert.Equal(new Point(7, 6), sky.TranslatePoint(default, dn));
            Assert.Equal(Res(dn, "InsIslandShadow"), Part<Border>(dn, "PART_Face").BoxShadow);
            Assert.False(dn.IsChecked);
            Assert.Equal(0, Part<Border>(dn, "PART_Night").Opacity, 2);
            Assert.Equal(1, Part<Canvas>(dn, "PART_Cloud").Opacity, 2);
            Assert.Equal(1, Part<Canvas>(dn, "PART_Rays").Opacity, 2);
            Assert.Equal(0, X(Part<Panel>(dn, "PART_Orb")), 1);
            Assert.Equal(0, Part<Ellipse>(dn, "PART_Star1").Opacity, 2);
        }
        finally { Reset(); }
    }

    [AvaloniaFact]
    public void A_click_brings_the_night_and_another_the_day()
    {
        Reset();
        try
        {
            var dn = new DayNight();
            var window = Following(dn);
            Click(dn, window);
            Assert.True(ThemeToggle.IsDark);
            Assert.True(dn.IsChecked);
            Settle(900);
            Assert.Equal(1, Part<Border>(dn, "PART_Night").Opacity, 2);
            Assert.Equal(36, X(Part<Panel>(dn, "PART_Orb")), 1);
            Assert.Equal(0, Part<Canvas>(dn, "PART_Cloud").Opacity, 2);
            Assert.Equal(15, Part<Canvas>(dn, "PART_Cloud").RenderTransform!.Value.M32, 1);
            Assert.Equal(0, Part<Canvas>(dn, "PART_Rays").Opacity, 2);
            Assert.Equal(1, Part<Ellipse>(dn, "PART_Shade").RenderTransform!.Value.M11, 2);
            foreach (var star in new[] { "PART_Star1", "PART_Star2", "PART_Star3" })
                Assert.Equal(0.92, Part<Ellipse>(dn, star).Opacity, 2);
            Assert.Equal(Res(dn, "InsDnMoon"), Part<Border>(dn, "PART_OrbFace").Background);
            Capture(window, "daynight-night");

            Click(dn, window);
            Assert.False(ThemeToggle.IsDark);
            Settle(900);
            Assert.False(dn.IsChecked);
            Assert.Equal(0, X(Part<Panel>(dn, "PART_Orb")), 1);
            Assert.Equal(1, Part<Canvas>(dn, "PART_Cloud").Opacity, 2);
            Assert.Equal(0, Part<Ellipse>(dn, "PART_Shade").RenderTransform!.Value.M11, 2);
        }
        finally { Reset(); }
    }

    [AvaloniaFact]
    public void The_sun_moves_over_time_and_the_stars_come_out_in_turn()
    {
        Reset();
        try
        {
            var dn = new DayNight();
            var window = Following(dn);
            ThemeToggle.Apply("dark");
            Settle(40);
            Assert.InRange(X(Part<Panel>(dn, "PART_Orb")), 0.5, 35);
            Assert.True(Part<Ellipse>(dn, "PART_Star1").Opacity > 0);
            Assert.Equal(0, Part<Ellipse>(dn, "PART_Star3").Opacity, 2);
            Settle(900);
            Assert.Equal(36, X(Part<Panel>(dn, "PART_Orb")), 1);
        }
        finally { Reset(); }
    }

    [AvaloniaFact]
    public void Day_night_follows_a_theme_set_elsewhere_and_starts_in_it()
    {
        Reset();
        try
        {
            ThemeToggle.Apply("dark");
            var dn = new DayNight();
            Following(dn);
            Assert.True(dn.IsChecked);
            Assert.Equal(36, X(Part<Panel>(dn, "PART_Orb")), 1);
            ThemeToggle.Apply("light");
            Settle();
            Assert.False(dn.IsChecked);
        }
        finally { Reset(); }
    }

    [AvaloniaFact]
    public void In_rtl_the_sun_starts_at_the_start_edge_and_crosses_toward_the_end_lit_from_the_top_left()
    {
        Reset();
        try
        {
            var dn = new DayNight();
            var window = Following(dn, FlowDirection.RightToLeft);
            var orb = Part<Panel>(dn, "PART_Orb");
            var sky = Part<Border>(dn, "PART_Sky");
            double FromMiddle() => orb.TranslatePoint(new Point(12, 12), window)!.Value.X - sky.TranslatePoint(new Point(36, 18), window)!.Value.X;
            Assert.Equal(18, FromMiddle(), 1);
            Assert.Equal(Res(dn, "InsDnDayRtl"), sky.Background);
            Assert.Equal(Res(dn, "InsDnSunRtl"), Part<Border>(dn, "PART_OrbFace").Background);
            Click(dn, window);
            Settle(900);
            Assert.Equal(-18, FromMiddle(), 1);
            Assert.Equal(Res(dn, "InsDnMoonRtl"), Part<Border>(dn, "PART_OrbFace").Background);
            Capture(window, "daynight-night-rtl");
        }
        finally { Reset(); }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Day_night_renders_in_every_look(string theme, string dir)
    {
        Reset();
        try
        {
            ThemeToggle.Apply(theme == "Dark" ? "dark" : "light");
            var dn = new DayNight();
            var big = new LayoutTransformControl { LayoutTransform = new ScaleTransform(4, 4), Child = dn };
            var window = Following(big, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
            window.Width = 500;
            window.Height = 320;
            Settle();
            Capture(window, $"daynight-{theme}-{dir}");
            Assert.Equal(theme == "Dark", dn.IsChecked);
        }
        finally { Reset(); }
    }
}
