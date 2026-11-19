using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A whole in parts (a storage bar): the pieces from the start edge as long as their shares, the legend under the bar with each part's dot, name and figure.
public class MeterTests
{
    private static Meter Make() => new()
    {
        Classes = { "lg" },
        Width = 400,
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
        Items =
        {
            new MeterPart { Classes = { "ok" }, Label = "منجزة", Value = 100 },
            new MeterPart { Classes = { "brand" }, Label = "هذا الفصل", Value = 27 },
            new MeterPart { Classes = { "mute" }, Label = "متبقية", Value = 173, Text = "173 h" },
        },
    };

    private static Rect Box(Visual v, Visual to)
    {
        var a = v.TranslatePoint(default, to)!.Value;
        var b = v.TranslatePoint(new Point(v.Bounds.Width, v.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static double Lum(Color c)
    {
        static double L(byte v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        return 0.2126 * L(c.R) + 0.7152 * L(c.G) + 0.0722 * L(c.B);
    }

    private static double Ratio(Color a, Color b) => (Math.Max(Lum(a), Lum(b)) + 0.05) / (Math.Min(Lum(a), Lum(b)) + 0.05);

    [AvaloniaFact]
    public void The_pieces_take_their_shares_a_gap_apart_and_a_small_one_still_shows()
    {
        var pieces = new (double, IBrush?)[] { (100, Brushes.Green), (27, Brushes.Purple), (173, Brushes.Gray) };
        var laid = MeterBar.Layout(pieces, 0, 304);
        Assert.Equal(3, laid.Count);
        Assert.Equal(0, laid[0].X);
        Assert.Equal(100, laid[0].Width, 1);   // 300 of room for 300 of value
        Assert.Equal(27, laid[1].Width, 1);
        Assert.Equal(laid[0].Width + MeterBar.Gap, laid[1].X, 1);
        Assert.Equal(304, laid[2].X + laid[2].Width, 1);

        // a maximum above the parts leaves the rest as the track (no fill) at the end
        var rest = MeterBar.Layout([(50, Brushes.Green)], 200, 202);
        Assert.Equal(2, rest.Count);
        Assert.Equal(50, rest[0].Width, 1);
        Assert.Null(rest[1].Fill);

        // a tiny share is drawn MinPiece long, taken from the longest
        var tiny = MeterBar.Layout([(1, Brushes.Green), (999, Brushes.Gray)], 0, 302);
        Assert.Equal(MeterBar.MinPiece, tiny[0].Width, 1);
        Assert.Equal(302, tiny[1].X + tiny[1].Width, 1);

        Assert.Empty(MeterBar.Layout([], 0, 300));
    }

    [AvaloniaTheory]
    [InlineData("ltr")]
    [InlineData("rtl")]
    public void The_bar_starts_at_the_start_edge_and_the_legend_dots_take_the_pieces_colours(string dir)
    {
        var meter = Make();
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { meter } }, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        var bar = meter.GetVisualDescendants().OfType<MeterBar>().Single();
        Assert.Equal(12, bar.Bounds.Height);
        var parts = meter.GetVisualDescendants().OfType<MeterPart>().ToList();
        Assert.Equal(["منجزة", "هذا الفصل", "متبقية"], parts.Select(p => p.Label));
        Assert.Equal(["100", "27", "173 h"], parts.Select(p => p.Figure));
        // the legend under the bar, its first entry at the start edge
        var first = Box(parts[0], w);
        var box = Box(bar, w);
        Assert.True(first.Top >= box.Bottom, "the legend sits under the bar");

        var frame = w.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var y = (int)box.Center.Y;
        // the first part (a third of the bar) at the start edge, the last at the end
        var startX = dir == "rtl" ? (int)box.Right - 20 : (int)box.Left + 20;
        var endX = dir == "rtl" ? (int)box.Left + 20 : (int)box.Right - 20;
        var start = PixelAt(buffer, startX, y);
        var end = PixelAt(buffer, endX, y);
        var okColour = ColorOf(parts[0].Fill);
        var muteColour = ColorOf(parts[2].Fill);
        Assert.True(Math.Abs(start.R - okColour.R) + Math.Abs(start.G - okColour.G) + Math.Abs(start.B - okColour.B) < 12, $"start {start} is the first part's {okColour}");
        Assert.True(Math.Abs(end.R - muteColour.R) + Math.Abs(end.G - muteColour.G) + Math.Abs(end.B - muteColour.B) < 12, $"end {end} is the last part's {muteColour}");
        if (dir == "rtl")
            Assert.True(Box(parts[0], w).Left > Box(parts[1], w).Left, "the first entry stands at the right in a right-to-left page");
        foreach (var part in parts)
        {
            var dot = part.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Dot");
            Assert.Same(part.Fill, dot.Background);
        }
    }

    [AvaloniaFact]
    public void A_hidden_part_leaves_the_bar_and_the_legend_and_a_part_worth_nothing_keeps_its_entry()
    {
        var meter = Make();
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { meter } });
        var parts = meter.GetVisualDescendants().OfType<MeterPart>().ToList();
        parts[2].IsVisible = false;
        parts[1].Value = 0;
        Settle(100);
        Assert.Single(meter.Pieces());
        Assert.Contains(":empty", parts[1].Classes);
        Assert.True(parts[1].IsEffectivelyVisible);
        Assert.Equal("0", parts[1].Figure);
        Assert.False(parts[2].IsEffectivelyVisible);
        // the one piece fills the bar
        var bar = meter.GetVisualDescendants().OfType<MeterBar>().Single();
        var frame = w.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var box = Box(bar, w);
        var far = PixelAt(buffer, (int)box.Right - 8, (int)box.Center.Y);
        var ok = ColorOf(parts[0].Fill);
        Assert.True(Math.Abs(far.G - ok.G) < 12 && Math.Abs(far.R - ok.R) < 12, $"the bar's end {far} is the one part's {ok}");
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_tones_stay_apart_from_their_neighbours_and_the_card(string theme)
    {
        var meter = Make();
        var card = new Border { Classes = { "glass" }, Padding = new Thickness(16), Child = meter };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { card } }, theme: theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var bar = meter.GetVisualDescendants().OfType<MeterBar>().Single();
        var parts = meter.GetVisualDescendants().OfType<MeterPart>().ToList();
        var frame = w.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var box = Box(bar, w);
        var laid = MeterBar.Layout(meter.Pieces(), 0, box.Width);
        var seen = laid.Select(p => PixelAt(buffer, (int)(box.Left + p.X + p.Width / 2), (int)box.Center.Y)).ToList();
        var (ok, brand, mute) = (seen[0], seen[1], seen[2]);
        Assert.True(Ratio(ok, brand) >= 3, $"ok {ok} beside brand {brand}: {Ratio(ok, brand):0.0}");
        Assert.True(Ratio(brand, mute) >= 3, $"brand {brand} beside mute {mute}: {Ratio(brand, mute):0.0}");
        // every piece, so every legend dot, stands 3:1 from the card around it
        var ground = PixelAt(buffer, (int)box.Left + 4, (int)box.Top - 4);
        foreach (var (name, c) in new[] { ("ok", ok), ("brand", brand), ("mute", mute) })
            Assert.True(Ratio(c, ground) >= 3, $"{name} {c} on the card {ground}: {Ratio(c, ground):0.0}");
        foreach (var part in parts)
        {
            var dot = Box(part.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Dot"), w);
            var ink = PixelAt(buffer, (int)dot.Center.X, (int)dot.Center.Y);
            var beside = PixelAt(buffer, (int)dot.Left - 3, (int)dot.Center.Y);
            Assert.True(Ratio(ink, beside) >= 3, $"{part.Label}'s dot {ink} on the card {beside}: {Ratio(ink, beside):0.0}");
        }
        Assert.Equal(3, parts.Count);
    }
}
