using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A hover fade between a tint given by colour alpha and one given by brush Opacity must not darken on the way.
public class HoverFadeTests
{
    private static double Alpha(IBrush? b) => b is ISolidColorBrush s ? s.Color.A / 255.0 * s.Opacity : 0;

    private sealed class Probe : BrushFade
    {
        public IBrush? At(double p, IBrush? from, IBrush? to) => Interpolate(p, from, to);
    }

    [Fact]
    public void Fading_between_an_opacity_tint_and_an_alpha_tint_never_overshoots()
    {
        IBrush unread = new SolidColorBrush(Color.Parse("#4C1D95"), 0.05);
        IBrush grey = new SolidColorBrush(Color.Parse("#090F172A"));
        var probe = new Probe();
        for (var p = 0.0; p <= 1.0001; p += 0.05)
        {
            Assert.InRange(Alpha(probe.At(p, unread, grey)), 0.034, 0.051);
            Assert.InRange(Alpha(probe.At(p, grey, unread)), 0.034, 0.051);
        }
    }

    // CL-2: a clear rest (Transparent is a clear WHITE) fades in as the target colour made clear: a dark ground never passes through grey or white
    [Fact]
    public void Fading_from_transparent_keeps_the_target_colour_all_the_way()
    {
        var dark = Color.Parse("#16181F");
        var probe = new Probe();
        for (var p = 0.05; p <= 1.0001; p += 0.05)
        {
            var c = ((ISolidColorBrush)probe.At(p, Brushes.Transparent, new SolidColorBrush(dark))!).Color;
            Assert.Equal((dark.R, dark.G, dark.B), (c.R, c.G, c.B));
            var back = ((ISolidColorBrush)probe.At(p, new SolidColorBrush(dark), Brushes.Transparent)!).Color;
            if (back.A > 0)
                Assert.Equal((dark.R, dark.G, dark.B), (back.R, back.G, back.B));
        }
    }

    [Fact]
    public void A_gradient_fades_stop_by_stop_from_a_clear_rest()
    {
        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#4C1D95"), 0), new GradientStop(Color.Parse("#6D28D9"), 1) },
        };
        var half = Assert.IsAssignableFrom<ILinearGradientBrush>(new Probe().At(0.5, Brushes.Transparent, gradient));
        Assert.Equal(2, half.GradientStops.Count);
        Assert.Equal(Color.FromArgb(128, 0x4C, 0x1D, 0x95), half.GradientStops[0].Color);
        Assert.Equal(Color.FromArgb(128, 0x6D, 0x28, 0xD9), half.GradientStops[1].Color);
    }

    [AvaloniaFact]
    public void An_unread_notification_does_not_flash_darker_on_hover()
    {
        var notify = new Notify();
        notify.Items.Add(new NotifyItem { Title = "اكتمل التصدير", Description = "الملف جاهز", End = "الآن", IsUnread = true, Icon = Geometry.Parse("M5 12h14") });
        notify.Items.Add(new NotifyItem { Title = "تعليق", Description = "على الفقرة", End = "قبل ساعة" });
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.RightToLeft, null, 900, 600);
        Click(Part<Button>(notify, "PART_Bell"), window);
        Settle(400);
        var row = (NotifyItem)notify.Items[0]!;
        var face = Part<Border>(row, "PART_Row");
        var start = Alpha(face.Background);
        window.MouseMove(Center(row, window));
        var peak = start;
        for (var i = 0; i < 20; i++)
        {
            Settle(16);
            peak = Math.Max(peak, Alpha(face.Background));
        }
        var end = Alpha(face.Background);
        Assert.True(peak <= Math.Max(start, end) + 0.005, $"hover fade peaked at {peak:0.000} between {start:0.000} and {end:0.000}");
        Assert.All(row.GetVisualDescendants().OfType<Icon>(), i => Assert.Equal(default, i.Margin));
    }
}
