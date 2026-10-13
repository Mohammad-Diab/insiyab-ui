using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MaskedTextBoxTests
{
    private static MaskedTextBox NewBox(string mask = "000 000 0000") => new() { Mask = mask, Width = 280, FlowDirection = FlowDirection.LeftToRight };

    private static string Features(FontFeatureCollection? features) => features is null ? "" : string.Join(",", features.Select(f => f.ToString()));

    private static string Ink(TextBlock ink) => string.Concat(ink.Inlines!.OfType<Run>().Select(r => r.Text));

    [AvaloniaFact]
    public void Pieces_split_the_text_at_the_empty_places()
    {
        var pieces = MaskInk.Pieces("05_ __", '_').ToList();
        Assert.Equal(new (string, bool)[] { ("05", false), ("_", true), (" ", false), ("__", true) }, pieces);
        Assert.Empty(MaskInk.Pieces("", '_'));
    }

    [AvaloniaFact]
    public void Masked_box_has_the_field_look()
    {
        var box = NewBox();
        Show(box);
        Assert.Equal((double)Res(box, "InsFieldH"), box.MinHeight);
        Assert.True(box.Bounds.Height >= 46);
        AssertBrush(box, "InsCtlBg", () => Part<Border>(box, "PART_BorderElement").Background);
        AssertBrush(box, "InsCtlBd", () => Part<Border>(box, "PART_BorderElement").BorderBrush);
    }

    [AvaloniaFact]
    public void Masked_box_draws_its_empty_places_muted_over_a_clear_presenter()
    {
        var box = NewBox();
        Show(box);
        var ink = Part<TextBlock>(box, "PART_MaskInk");
        var presenter = Part<TextPresenter>(box, "PART_TextPresenter");
        Assert.True(ink.IsVisible);
        Assert.Contains("masked", Part<Panel>(box, "PART_TextLayer").Classes);
        Assert.Equal(Colors.Transparent, ColorOf(presenter.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(box.Text, Ink(ink));

        var prompt = ink.Inlines!.OfType<Run>().First(r => r.Classes.Contains("prompt"));
        Assert.Contains('_', prompt.Text!);
        AssertBrush(box, "InsText3", () => prompt.Foreground);

        box.Text = "0501234";
        Settle(100);
        Assert.Equal(box.Text, Ink(ink));
        var typed = ink.Inlines!.OfType<Run>().First();
        Assert.DoesNotContain("prompt", typed.Classes);
        Assert.StartsWith("050", typed.Text);
        AssertBrush(box, "InsText1", () => ink.Foreground);
        Assert.Equal(presenter.Bounds.X, ink.Bounds.X, 1);
    }

    [AvaloniaFact]
    public void Masked_box_writes_in_tabular_latin_figures()
    {
        var box = NewBox();
        Show(box);
        var presenter = Part<TextPresenter>(box, "PART_TextPresenter");
        var ink = Part<TextBlock>(box, "PART_MaskInk");
        var latin = (FontFamily)Res(box, "InsFontBodyEn");
        Assert.Equal(latin, presenter.GetValue(TextElement.FontFamilyProperty));
        Assert.Equal(latin, ink.FontFamily);
        Assert.Contains("tnum", Features(presenter.GetValue(TextElement.FontFeaturesProperty)));
        Assert.Contains("tnum", Features(ink.FontFeatures));
    }

    [AvaloniaFact]
    public void A_plain_text_box_has_no_ink()
    {
        var box = new TextBox { Text = "نص", Width = 280 };
        Show(box);
        Assert.False(Part<TextBlock>(box, "PART_MaskInk").IsVisible);
        Assert.DoesNotContain("masked", Part<Panel>(box, "PART_TextLayer").Classes);
        AssertBrush(box, "InsText1", () => Part<TextPresenter>(box, "PART_TextPresenter").GetValue(TextElement.ForegroundProperty));
    }

    [AvaloniaFact]
    public void Error_and_ok_tint_only_the_border()
    {
        var bad = NewBox();
        bad.Classes.Add("bad");
        var ok = NewBox();
        ok.Classes.Add("ok");
        Show(new StackPanel { Spacing = 12, Children = { bad, ok } });
        AssertBrush(bad, "InsTextBoxBadBorder", () => Part<Border>(bad, "PART_BorderElement").BorderBrush);
        AssertBrush(bad, "InsCtlBg", () => Part<Border>(bad, "PART_BorderElement").Background);
        Assert.Equal(0.55, Part<Border>(bad, "PART_BorderElement").BorderBrush!.Opacity, 2);
        AssertBrush(ok, "InsTextBoxOkBorder", () => Part<Border>(ok, "PART_BorderElement").BorderBrush);
        AssertBrush(ok, "InsCtlBg", () => Part<Border>(ok, "PART_BorderElement").Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Masked_boxes_render_in_every_look(string theme, string dir)
    {
        var filled = NewBox("0000 0000 0000 0000");
        var date = NewBox("00/00/0000");
        var bad = NewBox();
        bad.Classes.Add("bad");
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(24), Children = { NewBox(), filled, date, bad } };
        var window = Show(content, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, width: 480, height: 360);
        filled.Text = "41111111";
        date.Text = "1504";
        Capture(window, $"masked-{theme}-{dir}");
    }
}
