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
}
