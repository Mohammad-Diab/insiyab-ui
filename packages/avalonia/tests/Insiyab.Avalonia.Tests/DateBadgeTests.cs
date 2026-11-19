using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// ins:DateBadge: a day over its month, in the page's language, with the pill's tones, a solid look and three sizes.
public class DateBadgeTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_date_shows_its_day_over_its_month_in_the_page_language(string dir)
    {
        var b = new DateBadge { Date = new DateTime(2026, 3, 14) };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { b } }, Dir(dir));
        Assert.Equal("14", b.DayText);
        Assert.Equal(dir == "rtl" ? "مارس" : "Mar", b.MonthText);
        var day = Part<TextBlock>(b, "PART_Day");
        var month = Part<TextBlock>(b, "PART_Month");
        Assert.Equal("14", day.Text);
        Assert.True(Box(day, w).Bottom <= Box(month, w).Top + 1, $"day {Box(day, w)} month {Box(month, w)}");
        Assert.True(Math.Abs(Box(day, w).Center.X - Box(month, w).Center.X) <= 1);
    }

    [AvaloniaFact]
    public void An_icon_stands_in_the_day_place_while_there_is_no_day()
    {
        var glyph = Geometry.Parse("M5 2h14M5 22h14");
        var empty = new DateBadge { Day = "", Month = "لم تصدر", Icon = glyph };
        var dated = new DateBadge { Day = "82", Month = "من 100", Icon = glyph };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { empty, dated } }, FlowDirection.RightToLeft);
        Assert.True(Part<Icon>(empty, "PART_Glyph").IsVisible);
        Assert.False(Part<TextBlock>(empty, "PART_Day").IsVisible);
        Assert.False(Part<Icon>(dated, "PART_Glyph").IsVisible);
        Assert.True(Part<TextBlock>(dated, "PART_Day").IsVisible);
        empty.Day = "40";
        Settle(100);
        Assert.False(Part<Icon>(empty, "PART_Glyph").IsVisible);
    }

    [AvaloniaFact]
    public void Day_and_month_text_win_over_the_date_and_locale_picks_the_language()
    {
        var written = new DateBadge { Date = new DateTime(2026, 9, 2), Month = "رمضان" };
        var arabic = new DateBadge { Date = new DateTime(2026, 9, 2), Locale = "ar" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { written, arabic } }, FlowDirection.LeftToRight);
        Assert.Equal("رمضان", written.MonthText);
        Assert.Equal("2", written.DayText);
        Assert.Equal("سبتمبر", arabic.MonthText);
        written.Day = "٢";
        Settle(100);
        Assert.Equal("٢", written.DayText);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Tones_and_filled_take_their_colours(string theme)
    {
        var plain = new DateBadge { Day = "14", Month = "مارس" };
        var brand = new DateBadge { Classes = { "brand" }, Day = "14", Month = "مارس" };
        var ok = new DateBadge { Classes = { "ok", "filled" }, Day = "14", Month = "مارس" };
        var filledBrand = new DateBadge { Classes = { "brand", "filled" }, Day = "14", Month = "مارس" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { plain, brand, ok, filledBrand } }, FlowDirection.RightToLeft, Variant(theme));
        AssertBrush(plain, "InsToneMuteBg", () => Part<Border>(plain, "PART_Face").Background);
        AssertBrush(plain, "InsText1", () => Part<TextBlock>(plain, "PART_Day").Foreground);
        AssertBrush(brand, "InsBrandTintBg", () => Part<Border>(brand, "PART_Face").Background);
        AssertBrush(brand, "InsPillBrandInk", () => Part<TextBlock>(brand, "PART_Day").Foreground);
        AssertBrush(ok, "InsToneOk", () => Part<Border>(ok, "PART_Face").Background);
        AssertBrush(ok, "InsDateBadgeFilledInk", () => Part<TextBlock>(ok, "PART_Day").Foreground);
        AssertBrush(filledBrand, "InsPrimaryMid", () => Part<Border>(filledBrand, "PART_Face").Background);
        AssertBrush(filledBrand, "InsOnPrimaryMid", () => Part<TextBlock>(filledBrand, "PART_Day").Foreground);
    }

    [AvaloniaFact]
    public void Sizes_scale_the_badge()
    {
        var sm = new DateBadge { Classes = { "sm" }, Day = "14", Month = "مارس" };
        var plain = new DateBadge { Day = "14", Month = "مارس" };
        var lg = new DateBadge { Classes = { "lg" }, Day = "14", Month = "مارس" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { sm, plain, lg } }, FlowDirection.RightToLeft);
        Assert.True(sm.Bounds.Width < plain.Bounds.Width && plain.Bounds.Width < lg.Bounds.Width, $"{sm.Bounds.Width} {plain.Bounds.Width} {lg.Bounds.Width}");
        Assert.Equal(16, Part<TextBlock>(sm, "PART_Day").FontSize);
        Assert.Equal(20, Part<TextBlock>(plain, "PART_Day").FontSize);
        Assert.Equal(26, Part<TextBlock>(lg, "PART_Day").FontSize);
    }

    // A word with no month («يومان») stands in the middle of a tile as tall as a captioned one, in every size.
    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("sm")]
    [InlineData("lg")]
    public void A_word_without_a_month_stands_in_the_middle_of_a_tile_as_tall_as_with_one(string size)
    {
        var word = new DateBadge { Day = "يومان" };
        var captioned = new DateBadge { Day = "37", Month = "ساعة" };
        if (size.Length > 0) { word.Classes.Add(size); captioned.Classes.Add(size); }
        var w = Show(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20), Spacing = 8, Children = { word, captioned } }, FlowDirection.RightToLeft);
        Assert.Equal(captioned.Bounds.Height, word.Bounds.Height, 1);
        Assert.Equal(0, Part<TextBlock>(word, "PART_Month").Opacity);
        var text = Part<TextBlock>(word, "PART_Day");
        var day = Box(text, w); // as drawn (its shift included)
        var tile = Box(word, w);
        // an Arabic word's ink sits low in the Latin face's line box: the word is lifted by that much, so its ink (not its box) is in the middle
        var lift = DateBadge.InkLift(text);
        Assert.True(lift >= 1, $"lift {lift}");
        Assert.Equal(tile.Center.Y, day.Center.Y + lift, 1);
    }

    // A Latin word or figures alone keep the line box's middle: the Latin face is made for it (English was right as it was).
    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("sm")]
    [InlineData("lg")]
    public void A_latin_word_without_a_month_keeps_its_line_in_the_middle(string size)
    {
        var word = new DateBadge { Day = "Today" };
        if (size.Length > 0) word.Classes.Add(size);
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { word } });
        var text = Part<TextBlock>(word, "PART_Day");
        Assert.Equal(default, text.Margin);
        Assert.Equal(Box(word, w).Center.Y, Box(text, w).Center.Y, 1);
    }
}
