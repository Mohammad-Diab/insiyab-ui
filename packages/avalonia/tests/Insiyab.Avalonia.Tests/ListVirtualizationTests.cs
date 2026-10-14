using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ListVirtualizationTests
{
    private const int Count = 2000;

    private static void Press(Window w, Key key)
    {
        w.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        w.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(200);
    }

    private static void Type(Window w, string text)
    {
        w.KeyTextInput(text);
        Settle(200);
    }

    private static List<Border> PaletteRows(Visual root) =>
        root.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("palette-item")).ToList();

    private static string PaletteLabel(Border row) =>
        ((Grid)row.Child!).Children.OfType<TextBlock>().First(t => t.Classes.Contains("palette-label")).Text!;

    private static List<Border> TagRows(Visual root) =>
        root.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("tags-option")).ToList();

    private static string TagLabel(Border row) => ((TextBlock)row.Child!).Text!;

    private static CommandPalette BigPalette()
    {
        var palette = new CommandPalette();
        for (var i = 0; i < Count; i++)
            palette.Items.Add(new PaletteItem { Label = $"عنصر {i}", Group = $"مجموعة {i % 20}", Keywords = $"item{i}" });
        return palette;
    }

    [AvaloniaFact]
    public void Palette_builds_only_the_rows_in_view_and_still_reaches_the_last_item()
    {
        var palette = BigPalette();
        var w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        var realized = PaletteRows(palette).Count;
        Assert.InRange(realized, 1, 99);
        Assert.Equal(Count, palette.Shown.Count);
        Assert.Equal("عنصر 0", palette.Active!.Label);

        Press(w, Key.Up);
        var last = palette.Shown[^1];
        Assert.Same(last, palette.Active);
        var row = PaletteRows(palette).Single(r => PaletteLabel(r) == last.Label);
        Assert.Contains("active", row.Classes);
        Assert.Single(PaletteRows(palette), r => r.Classes.Contains("active"));
        Assert.InRange(PaletteRows(palette).Count, 1, 99);

        Press(w, Key.Down);
        Assert.Equal("عنصر 0", palette.Active!.Label);
        var first = PaletteRows(palette).Single(r => PaletteLabel(r) == "عنصر 0");
        Assert.Contains("active", first.Classes);
        Assert.Single(PaletteRows(palette), r => r.Classes.Contains("active"));
    }

    [AvaloniaFact]
    public void Palette_search_to_one_hit_leaves_one_row()
    {
        var palette = BigPalette();
        var w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        Type(w, "item1999");
        Assert.Equal(["عنصر 1999"], palette.Shown.Select(i => i.Label));
        var rows = PaletteRows(palette);
        Assert.Single(rows);
        Assert.Equal("عنصر 1999", PaletteLabel(rows[0]));
        Assert.Contains("active", rows[0].Classes);
    }

    [AvaloniaFact]
    public void Tags_build_only_the_options_in_view_and_choose_the_last_one()
    {
        var tags = new TagsInput { Width = 400 };
        for (var i = 0; i < Count; i++) tags.Options.Add(new TagsOption($"v{i}", $"خيار {i}"));
        var w = Show(new StackPanel { Children = { tags } });
        tags.InputBox!.Focus();
        Settle();
        Assert.True(tags.IsDropDownOpen);
        Assert.InRange(TagRows(w).Count, 1, 99);

        Press(w, Key.Up);
        var row = TagRows(w).Single(r => TagLabel(r) == $"خيار {Count - 1}");
        Assert.Contains("active", row.Classes);
        Assert.Single(TagRows(w), r => r.Classes.Contains("active"));

        Press(w, Key.Enter);
        Assert.Equal([$"v{Count - 1}"], tags.Values);
        Assert.DoesNotContain(TagRows(w), r => TagLabel(r) == $"خيار {Count - 1}");
        Assert.InRange(TagRows(w).Count, 1, 99);
        Assert.True(tags.IsDropDownOpen);
    }
}
