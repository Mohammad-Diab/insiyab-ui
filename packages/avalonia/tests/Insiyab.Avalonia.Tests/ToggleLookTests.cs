using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A toggle of any style is the neutral grey button when off and takes its own style's colour when on, like the web.
public class ToggleLookTests
{
    private static readonly string[] Coloured = ["primary", "success", "danger", "warning", "info", "ghost", "ghost-success", "ghost-danger", "ghost-warning", "ghost-info"];
    private static readonly string[] Styles = [.. Coloured, "secondary", "bare", ""];

    private static Color[] Colours(IBrush? brush) => brush switch
    {
        ISolidColorBrush s => [Color.FromArgb((byte)Math.Round(s.Color.A * s.Opacity), s.Color.R, s.Color.G, s.Color.B)],
        IGradientBrush g => [.. g.GradientStops.Select(x => x.Color)],
        _ => [],
    };

    private static Color[][] Look(Button b) => [Colours(b.Background), Colours(b.BorderBrush), Colours(b.Foreground)];

    private static bool Same(Color[][] a, Color[][] b) =>
        a.Length == b.Length && a.Zip(b).All(p => p.First.Length == p.Second.Length && p.First.Zip(p.Second).All(c =>
            Math.Abs(c.First.A - c.Second.A) <= 6 && Math.Abs(c.First.R - c.Second.R) <= 6 && Math.Abs(c.First.G - c.Second.G) <= 6 && Math.Abs(c.First.B - c.Second.B) <= 6));

    private static string Say(Color[][] look) => string.Join(" | ", look.Select(p => string.Join(",", p)));

    // Brush transitions run on the real clock, so a match is awaited for up to 2s before it is asserted.
    private static void AssertLook(bool same, Button a, Button b, string what)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 2000 && Same(Look(a), Look(b)) != same) Settle(16);
        Assert.True(Same(Look(a), Look(b)) == same, $"{what}: {Say(Look(a))} vs {Say(Look(b))}");
    }

    private static T Make<T>(string style) where T : Button, new()
    {
        var b = new T { Content = "حفظ" };
        if (style != "") b.Classes.Add(style);
        return b;
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Toggle_is_grey_off_and_its_own_colour_on(string theme)
    {
        var grey = Make<Button>("secondary");
        var rows = Styles.Select(s => (Style: s, Plain: Make<Button>(s), Off: Make<ToggleButton>(s), On: Make<ToggleButton>(s))).ToList();
        foreach (var r in rows) r.On.IsChecked = true;
        var panel = new WrapPanel { Margin = new Thickness(12), Children = { grey } };
        foreach (var r in rows) panel.Children.AddRange([r.Plain, r.Off, r.On]);
        Show(panel, theme: theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, width: 1200, height: 600);

        foreach (var r in rows)
        {
            var name = r.Style == "" ? "plain" : r.Style;
            AssertLook(false, r.Off, r.On, $"{theme} {name}: on and off differ");
            if (r.Style != "bare") AssertLook(true, r.Off, grey, $"{theme} {name}: off is the grey button");
            if (Coloured.Contains(r.Style)) AssertLook(true, r.On, r.Plain, $"{theme} {name}: on is the style's own look");
        }

        foreach (var r in rows.Where(r => r.Style is "secondary" or ""))
        {
            var tint = (ISolidColorBrush)Res(r.On, "InsButtonToggleOn");
            Assert.Equal(tint.Color, ColorOf(r.On.Background));
            AssertBrush(r.On, "InsPrimaryDarker", () => r.On.Foreground);
        }
        var bare = rows.Single(r => r.Style == "bare");
        AssertBrush(bare.On, "InsPrimaryDarker", () => bare.On.Foreground);
        AssertBrush(bare.Off, "InsText2", () => bare.Off.Foreground);
    }

    [AvaloniaFact]
    public void Clicking_a_primary_toggle_turns_it_from_grey_to_the_brand()
    {
        var grey = Make<Button>("secondary");
        var primary = Make<Button>("primary");
        var toggle = Make<ToggleButton>("primary");
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { grey, primary, toggle } });

        AssertLook(true, toggle, grey, "off");
        Click(toggle, window);
        window.MouseMove(new Point(600, 400));
        Assert.True(toggle.IsChecked);
        AssertLook(true, toggle, primary, "on");
        Assert.IsAssignableFrom<ILinearGradientBrush>(toggle.Background);
    }
}
