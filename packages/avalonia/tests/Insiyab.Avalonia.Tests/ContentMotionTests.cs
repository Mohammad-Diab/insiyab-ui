using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ContentMotionTests
{
    private static readonly string[] Months = ["يناير", "فبراير", "مارس", "أبريل", "مايو"];

    private static void Still(Window w, bool on) => Motion.SetStill(w, on);

    private static (Window Window, ChartPlot Plot) ShowChart(ChartType type)
    {
        var chart = new Chart { Type = type, Labels = string.Join(",", Months), Width = 500 };
        chart.Series.Add(new ChartSeries("هذا العام", 120, 150, 98, 174, 160));
        if (type != ChartType.Donut) chart.Series.Add(new ChartSeries("العام الماضي", 90, 130, 9, 140, 150));
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { chart } }, FlowDirection.RightToLeft);
        Settle(1300);
        return (w, chart.GetVisualDescendants().OfType<ChartPlot>().Single());
    }

    // Pumps frames and keeps every value read, so the steps of a 220ms fade can be checked.
    private static List<double> Watch(Window w, Func<double> read, int ms = 400)
    {
        var seen = new List<double>();
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < ms)
        {
            w.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            seen.Add(read());
            Thread.Sleep(8);
        }
        return seen;
    }

    private static bool Between(IEnumerable<double> seen, double a, double b) => seen.Any(v => v > Math.Min(a, b) + 0.02 && v < Math.Max(a, b) - 0.02);

    [AvaloniaFact]
    public void Other_bars_dim_over_220ms_and_come_back()
    {
        var (w, plot) = ShowChart(ChartType.Bar);
        Assert.Equal(1, plot.FadeOf(ChartPlot.BarKind, 1, 0));

        plot.LightMark(0, 0);
        var dimming = Watch(w, () => plot.FadeOf(ChartPlot.BarKind, 1, 0));
        Assert.True(Between(dimming, 1, 0.45), "no step between 1 and .45: " + string.Join(", ", dimming));
        Assert.Equal(0.45, plot.FadeOf(ChartPlot.BarKind, 1, 0), 3);
        Assert.Equal(1, plot.FadeOf(ChartPlot.BarKind, 0, 0), 3);

        plot.Clear();
        var back = Watch(w, () => plot.FadeOf(ChartPlot.BarKind, 1, 0));
        Assert.True(Between(back, 0.45, 1), "no step between .45 and 1: " + string.Join(", ", back));
        Assert.Equal(1, plot.FadeOf(ChartPlot.BarKind, 1, 0), 3);
    }

    [AvaloniaFact]
    public void Lit_dot_grows_to_five_and_a_half_over_220ms()
    {
        var (w, plot) = ShowChart(ChartType.Line);
        Assert.Equal(3.5, plot.FadeOf(ChartPlot.DotKind, 0, 2));
        plot.LightMark(0, 2);
        var growing = Watch(w, () => plot.FadeOf(ChartPlot.DotKind, 0, 2));
        Assert.True(Between(growing, 3.5, 5.5), "no step between 3.5 and 5.5: " + string.Join(", ", growing));
        Assert.Equal(5.5, plot.FadeOf(ChartPlot.DotKind, 0, 2), 3);
    }

    [AvaloniaFact]
    public void Other_slices_dim_over_220ms()
    {
        var (w, plot) = ShowChart(ChartType.Donut);
        plot.LightMark(0, 0);
        var dimming = Watch(w, () => plot.FadeOf(ChartPlot.SliceKind, 0, 1));
        Assert.True(Between(dimming, 1, 0.45), "no step between 1 and .45: " + string.Join(", ", dimming));
        Assert.Equal(0.45, plot.FadeOf(ChartPlot.SliceKind, 0, 1), 3);
        Assert.Equal(1, plot.FadeOf(ChartPlot.SliceKind, 0, 0), 3);
    }

    [AvaloniaFact]
    public void Hover_changes_at_once_when_motion_is_still()
    {
        var (w, plot) = ShowChart(ChartType.Bar);
        Still(w, true);
        try
        {
            plot.LightMark(0, 0);
            var seen = Watch(w, () => plot.FadeOf(ChartPlot.BarKind, 1, 0), 200);
            Assert.False(Between(seen, 1, 0.45), "a step between 1 and .45: " + string.Join(", ", seen));
            Assert.Equal(0.45, plot.FadeOf(ChartPlot.BarKind, 1, 0), 3);
        }
        finally { Still(w, false); }
    }

    private static Wizard Wiz()
    {
        var wiz = new Wizard();
        wiz.Items.Add(new WizardPanel { Header = "الأولى", Content = new TextBlock { Text = "١" } });
        wiz.Items.Add(new WizardPanel { Header = "الثانية", Content = new TextBlock { Text = "٢" } });
        return wiz;
    }

    [AvaloniaFact]
    public void Wizard_spinners_turn_at_six_tenths_of_a_second()
    {
        var wiz = Wiz();
        Show(wiz, FlowDirection.RightToLeft);
        foreach (var name in new[] { "PART_NextSpinner", "PART_FinishSpinner" })
        {
            if (name == "PART_FinishSpinner") { wiz.GoTo(1); Settle(); }
            var spinner = Assert.IsType<LoadingSpinner>(Part<Control>(wiz, name));
            var spin = spinner.Theme!.Children.OfType<Style>().SelectMany(s => s.Animations).OfType<Animation>().Single(a => a.IterationCount == IterationCount.Infinite);
            Assert.Equal(TimeSpan.FromMilliseconds(600), spin.Duration);
            Assert.Equal(15, spinner.Width);
        }
    }

    [AvaloniaFact]
    public void Wizard_spinner_spins_while_the_wizard_waits()
    {
        var wiz = Wiz();
        wiz.Leaving += (_, e) => e.Wait();
        var w = Show(wiz, FlowDirection.RightToLeft);
        wiz.Next();
        Settle(100);
        var spinner = Assert.IsType<LoadingSpinner>(Part<Control>(wiz, "PART_NextSpinner"));
        Assert.True(spinner.IsEffectivelyVisible);
        var angles = Watch(w, () => spinner.Angle, 300);
        Assert.True(angles.Distinct().Count() > 3, "the spinner did not turn");
        wiz.Proceed();
        Settle();
    }
}
