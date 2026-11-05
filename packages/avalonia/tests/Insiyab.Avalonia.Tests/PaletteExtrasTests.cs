using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class PaletteExtrasTests
{
    private static (CommandPalette Palette, Window Window) Open(params PaletteItem[] items)
    {
        var palette = new CommandPalette();
        palette.Items.AddRange(items);
        var w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        return (palette, w);
    }

    private static Border RowOf(CommandPalette palette, string label) =>
        palette.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("palette-item") && b.Tag is PaletteItem i && i.Label == label);

    [AvaloniaFact]
    public void A_description_is_a_second_muted_line_under_the_label()
    {
        var (palette, _) = Open(
            new PaletteItem { Label = "الإعدادات", Group = "الصفحات" },
            new PaletteItem { Label = "دليل الاستخدام", Description = "ملف PDF - 0.9 MB", Group = "المساعدة" });
        var row = RowOf(palette, "دليل الاستخدام");
        var texts = row.GetVisualDescendants().OfType<TextBlock>().ToList();
        var label = texts.Single(t => t.Classes.Contains("palette-label"));
        var desc = texts.Single(t => t.Classes.Contains("palette-desc"));

        Assert.Equal("ملف PDF - 0.9 MB", desc.Text);
        Assert.True(desc.TranslatePoint(default, row)!.Value.Y >= label.TranslatePoint(new Point(0, label.Bounds.Height), row)!.Value.Y - 0.5);
        AssertBrush(row, "InsText3", () => desc.Foreground);
        Assert.Equal(TextTrimming.CharacterEllipsis, desc.TextTrimming);
        Assert.DoesNotContain(RowOf(palette, "الإعدادات").GetVisualDescendants().OfType<TextBlock>(), t => t.Classes.Contains("palette-desc"));
    }

    [AvaloniaFact]
    public void Search_only_items_wait_for_something_typed()
    {
        var help = new PaletteItem { Label = "كيف أغير كلمة المرور", Group = "الأسئلة", IsSearchOnly = true };
        var page = new PaletteItem { Label = "الإعدادات", Group = "الصفحات", Keywords = "كلمة المرور" };
        var (palette, _) = Open(page, help);
        Assert.DoesNotContain(help, palette.Shown);
        Assert.Contains(page, palette.Shown);

        palette.Text = "كلمه المرور";
        Settle(100);
        Assert.Contains(help, palette.Shown);
        Assert.Same(help, palette.Shown[0]);

        palette.Text = "";
        Settle(100);
        Assert.DoesNotContain(help, palette.Shown);
    }

    [AvaloniaFact]
    public void Text_reads_and_writes_the_search_field()
    {
        var (palette, _) = Open(new PaletteItem { Label = "الأزرار" }, new PaletteItem { Label = "الجداول" });
        var seen = new List<string>();
        palette.TextChanged += (_, _) => seen.Add(palette.Text);

        palette.InputBox!.Text = "جد";
        Assert.Equal("جد", palette.Text);
        Assert.Equal(["جد"], seen);
        Assert.Single(palette.Shown);

        palette.Text = "زر";
        Assert.Equal("زر", palette.InputBox.Text);
        Assert.Equal(["جد", "زر"], seen);
    }

    [AvaloniaFact]
    public void Items_added_while_the_text_changes_show_in_that_search()
    {
        var (palette, _) = Open(new PaletteItem { Label = "الأزرار" });
        PaletteItem? asked = null;
        palette.TextChanged += (_, _) =>
        {
            if (asked is not null) palette.Items.Remove(asked);
            asked = string.IsNullOrEmpty(palette.Text) ? null : new PaletteItem { Label = "ابحث في المستندات عن «" + palette.Text + "»", Keywords = palette.Text, Key = "docs" };
            if (asked is not null) palette.Items.Add(asked);
        };
        palette.Text = "تقرير";
        Assert.NotNull(asked);
        Assert.Contains(asked, palette.Shown);
        Assert.Single(palette.Shown);
        Assert.Same(asked, palette.Active);
    }

    [AvaloniaFact]
    public void Opening_clears_the_text_and_says_so()
    {
        var (palette, w) = Open(new PaletteItem { Label = "الأزرار" });
        palette.Text = "زر";
        palette.Close();
        var seen = 0;
        palette.TextChanged += (_, _) => seen++;
        palette.Open(w);
        Assert.Equal("", palette.Text);
        Assert.Equal(1, seen);
    }
}
