using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Insiyab.Avalonia.Tests;

public class FontTests
{
    const string Source = "avares://Insiyab.Avalonia/Assets/Fonts#";
    const string Arabic = "الخط الواضح يريح العين";
    const string Latin = "Clear type rests the eye";

    static FontFamily Resource(string key)
    {
        Assert.True(Application.Current!.TryGetResource(key, null, out var value), $"{key} is missing");
        return Assert.IsType<FontFamily>(value);
    }

    static GlyphTypeface Embedded(string family, int weight)
    {
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(new FontFamily(Source + family), FontStyle.Normal, (FontWeight)weight), out var face));
        return face!;
    }

    static List<(string Text, GlyphTypeface Face)> Runs(TextBlock block) =>
        block.TextLayout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>()
            .Where(r => !string.IsNullOrWhiteSpace(r.Text.ToString()))
            .Select(r => (r.Text.ToString(), r.ShapedBuffer.GlyphTypeface)).ToList();

    static Window Show(Control content, FlowDirection direction)
    {
        var window = new Window { FlowDirection = direction, Content = content };
        window.Show();
        return window;
    }

    [AvaloniaTheory]
    [InlineData("InsFontArabic", "Tajawal", 300)]
    [InlineData("InsFontArabic", "Tajawal", 400)]
    [InlineData("InsFontArabic", "Tajawal", 500)]
    [InlineData("InsFontArabic", "Tajawal", 700)]
    [InlineData("InsFontBodyEn", "Inter", 300)]
    [InlineData("InsFontBodyEn", "Inter", 400)]
    [InlineData("InsFontBodyEn", "Inter", 500)]
    [InlineData("InsFontBodyEn", "Inter", 600)]
    [InlineData("InsFontBodyEn", "Inter", 700)]
    [InlineData("InsFontHeadingEn", "Cause", 400)]
    [InlineData("InsFontHeadingEn", "Cause", 600)]
    [InlineData("InsFontHeadingEn", "Cause", 700)]
    public void Each_weight_renders_in_its_embedded_face(string key, string family, int weight)
    {
        var face = Embedded(family, weight);
        Assert.Equal(family, face.TypographicFamilyName is { Length: > 0 } t ? t : face.FamilyName);
        Assert.Equal(weight, (int)face.Weight);
        Assert.Equal(FontSimulations.None, face.FontSimulations);

        var block = new TextBlock { Text = family == "Tajawal" ? Arabic : Latin, FontFamily = Resource(key), FontWeight = (FontWeight)weight };
        var window = Show(block, family == "Tajawal" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);

        var runs = Runs(block);
        Assert.NotEmpty(runs);
        Assert.All(runs, r => Assert.Same(face, r.Face));
        window.Close();
    }

    [AvaloniaFact]
    public void Arabic_in_an_LTR_window_falls_back_to_Tajawal()
    {
        var body = new TextBlock { Text = $"Report {Arabic} 2026" };
        var heading = new TextBlock { Text = $"Title {Arabic}", Classes = { "h1" }, FontWeight = FontWeight.Bold };
        var window = Show(new StackPanel { Children = { body, heading } }, FlowDirection.LeftToRight);

        foreach (var (block, latinFamily) in new[] { (body, "Inter"), (heading, "Cause") })
        {
            var weight = (int)block.FontWeight;
            var runs = Runs(block);
            Assert.Contains(runs, r => r.Text.Any(c => c is >= '؀' and <= 'ۿ'));
            foreach (var (text, face) in runs)
            {
                var isArabic = text.Any(c => c is >= '؀' and <= 'ۿ');
                Assert.Same(Embedded(isArabic ? "Tajawal" : latinFamily, weight), face);
            }
        }
        window.Close();
    }

    [AvaloniaFact]
    public void Flipping_FlowDirection_switches_body_and_heading_fonts()
    {
        var body = new TextBlock { Text = Latin };
        var h1 = new TextBlock { Text = Latin, Classes = { "h1" } };
        var display = new TextBlock { Text = Latin, Classes = { "display" } };
        var h4 = new TextBlock { Text = Latin, Classes = { "h4" } };
        var button = new Button { Content = Latin };
        var window = Show(new StackPanel { Children = { body, h1, display, h4, button } }, FlowDirection.RightToLeft);

        var arabic = Resource("InsFontArabic");
        var bodyEn = Resource("InsFontBodyEn");
        var headingEn = Resource("InsFontHeadingEn");

        void Expect(FontFamily bodyFace, FontFamily headingFace)
        {
            Assert.Equal(bodyFace, body.FontFamily);
            Assert.Equal(bodyFace, h4.FontFamily);
            Assert.Equal(bodyFace, button.FontFamily);
            Assert.Equal(headingFace, h1.FontFamily);
            Assert.Equal(headingFace, display.FontFamily);
        }

        Expect(arabic, arabic);
        Assert.Same(Embedded("Tajawal", 400), Runs(body)[0].Face);

        window.FlowDirection = FlowDirection.LeftToRight;
        Expect(bodyEn, headingEn);
        Assert.Same(Embedded("Inter", 400), Runs(body)[0].Face);
        Assert.Equal("Cause", Runs(h1)[0].Face.TypographicFamilyName is { Length: > 0 } t ? t : Runs(h1)[0].Face.FamilyName);

        window.FlowDirection = FlowDirection.RightToLeft;
        Expect(arabic, arabic);
        window.Close();
    }

    [AvaloniaFact]
    public void Fluent_controls_take_the_body_stack()
    {
        Assert.Equal(Resource("InsFontArabic"), Resource("InsFontBody"));
        Assert.Equal(Resource("InsFontBody"), Resource("ContentControlThemeFontFamily"));
        Assert.Contains("Consolas", Resource("InsFontMono").FamilyNames);
    }
}
