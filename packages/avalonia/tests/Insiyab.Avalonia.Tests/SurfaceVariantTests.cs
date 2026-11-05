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

// The solid tile, sheet and desk, and the panel's tones, band head, low shadow and dense parts.
public class SurfaceVariantTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");

    private static Border Surface(string classes, string text, Control? child = null)
    {
        var box = new Border { Padding = new Thickness(12), MinHeight = 44, Child = child ?? new TextBlock { Text = text } };
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) box.Classes.Add(c);
        return box;
    }

    private static Border Panel(string mods = "", bool ico = true, bool note = false, bool flush = false, string headClasses = "")
    {
        var title = new TextBlock { Classes = { "panel-title" }, Text = "الإعدادات العامة" };
        var actions = new StackPanel { Classes = { "panel-actions" }, Children = { new TextBlock { Text = "تعديل" } } };
        DockPanel.SetDock(actions, Dock.Right);
        var row = new DockPanel();
        if (ico)
        {
            var chip = new Border { Classes = { "panel-ico" }, Child = new Icon { Data = Glyph } };
            DockPanel.SetDock(chip, Dock.Left);
            row.Children.Add(chip);
        }
        row.Children.Add(actions);
        row.Children.Add(title);
        var head = new Border { Classes = { "panel-head" }, Child = row };
        foreach (var c in headClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries)) head.Classes.Add(c);
        DockPanel.SetDock(head, Dock.Top);
        var foot = new Border { Classes = { "panel-foot" }, Child = new TextBlock { Text = "آخر تحديث قبل ساعة" } };
        DockPanel.SetDock(foot, Dock.Bottom);
        var dock = new DockPanel { Children = { head, foot } };
        Control content = new TextBlock { Text = "محتوى اللوحة" };
        if (note && !flush)
        {
            var line = new Border { Classes = { "panel-note" }, Child = new TextBlock { Text = "سطر شرح." } };
            DockPanel.SetDock(line, Dock.Top);
            dock.Children.Add(line);
        }
        if (flush) content = new StackPanel { Children = { new Border { Classes = { "panel-note" }, Child = new TextBlock { Text = "سطر شرح." } }, new TextBlock { Text = "البند الأول", Margin = new Thickness(10) } } };
        var body = new Border { Classes = { "panel-body" }, Child = content };
        if (flush) body.Classes.Add("flush");
        dock.Children.Add(body);
        var panel = new Border { Classes = { "panel" }, Child = dock };
        foreach (var c in mods.Split(' ', StringSplitOptions.RemoveEmptyEntries)) panel.Classes.Add(c);
        return panel;
    }

    private static T First<T>(Visual root, string cls) where T : Control => root.GetVisualDescendants().OfType<T>().First(c => c.Classes.Contains(cls));

    [AvaloniaFact]
    public void A_tile_is_a_solid_card_that_lifts_a_pixel()
    {
        var tile = Surface("tile hoverable", "بطاقة");
        var w = Show(Surface("panel", "", new StackPanel { Margin = new Thickness(20), Children = { tile } }), FlowDirection.RightToLeft);
        Assert.Same(Res(tile, "InsLookSurface"), tile.Theme);
        AssertBrush(tile, "InsSurface", () => tile.Background);
        AssertBrush(tile, "InsLine", () => tile.BorderBrush);
        Assert.Equal(new Thickness(1), tile.BorderThickness);
        Assert.Equal(new CornerRadius(14), tile.CornerRadius);
        Assert.Equal((BoxShadows)Res(tile, "InsShSm"), tile.BoxShadow);
        w.MouseMove(Center(tile, w));
        Settle();
        Assert.InRange(tile.RenderTransform!.Value.M32, -1.1, -0.9);
        AssertBrush(tile, "InsTileHoverBorder", () => tile.BorderBrush);
        Assert.Equal((BoxShadows)Res(tile, "InsShMd"), tile.BoxShadow);
        Motion.SetStill(w, true);
        Settle();
        Assert.True(tile.RenderTransform is null || tile.RenderTransform.Value.IsIdentity);
    }

    [AvaloniaFact]
    public void Sheet_and_desk_are_solid_surfaces()
    {
        var sheet = new Border { Classes = { "sheet" }, Child = new TextBlock { Text = "نص" } };
        var desk = new Border { Classes = { "desk" }, Child = sheet };
        Show(desk, FlowDirection.RightToLeft);
        Assert.Equal(new Thickness(24, 20), sheet.Padding);
        AssertBrush(sheet, "InsSurface", () => sheet.Background);
        Assert.Equal(new CornerRadius(14), sheet.CornerRadius);
        AssertBrush(desk, "InsCtlBg", () => desk.Background);
        Assert.Equal(new CornerRadius(22), desk.CornerRadius);
        Assert.Equal(new Thickness(16), desk.Padding);
    }

    [AvaloniaTheory]
    [InlineData("alert", "InsToneBadBg", "InsToneBadBd", "InsToneBad", "InsToneBadBg")]
    [InlineData("ok", "InsToneOkBg", "InsToneOkBd", "InsToneOk", "InsToneOkBg")]
    [InlineData("warn", "InsToneWarnBg", "InsToneWarnBd", "InsToneWarn", "InsToneWarnBg")]
    [InlineData("info", "InsToneInfoBg", "InsToneInfoBd", "InsToneInfo", "InsToneInfoBg")]
    [InlineData("brand", "InsBrandTintBg", "InsBrandTintBd", "InsToneBrand", "InsPanelIcoBrush")]
    public void Panel_tones_tint_the_head_title_and_icon(string tone, string bg, string bd, string ink, string ico)
    {
        var panel = Panel(tone);
        Show(new StackPanel { Children = { panel } }, FlowDirection.RightToLeft, height: 600);
        var head = First<Border>(panel, "panel-head");
        var title = First<TextBlock>(panel, "panel-title");
        var chip = First<Border>(panel, "panel-ico");
        AssertBrush(head, bg, () => head.Background);
        AssertBrush(head, bd, () => head.BorderBrush);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), head.CornerRadius);
        AssertBrush(title, ink, () => title.Foreground);
        AssertBrush(chip, ico, () => chip.Background);
        panel.Classes.Remove(tone);
        Settle();
        Assert.Null(head.Background);
    }

    [AvaloniaFact]
    public void A_band_head_takes_the_inner_glass_and_rounds_with_the_panel()
    {
        var panel = Panel(headClasses: "band");
        Show(new StackPanel { Children = { panel } }, FlowDirection.RightToLeft, height: 600);
        var head = First<Border>(panel, "panel-head");
        AssertBrush(head, "InsGlassInner", () => head.Background);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), head.CornerRadius);
    }

    [AvaloniaFact]
    public void A_low_panel_takes_the_panel_shadow()
    {
        var low = Panel("low");
        var plain = Panel();
        Show(new StackPanel { Children = { low, plain } }, FlowDirection.RightToLeft, height: 900);
        var sh = (BoxShadows)Res(low, "InsShPanel");
        Assert.Equal(sh.Count + 1, low.BoxShadow.Count);
        Assert.Equal(sh[0], low.BoxShadow[0]);
        Assert.Equal(((BoxShadows)Res(plain, "InsShGlass"))[0], plain.BoxShadow[0]);
    }

    [AvaloniaFact]
    public void A_dense_panel_tightens_its_parts()
    {
        var panel = Panel("dense", note: true);
        var flushPanel = Panel("dense", flush: true);
        Show(new StackPanel { Children = { panel, flushPanel } }, FlowDirection.RightToLeft, height: 900);
        Assert.Equal(new Thickness(14, 10), First<Border>(panel, "panel-head").Padding);
        Assert.Equal(new Thickness(14, 8), First<Border>(panel, "panel-body").Padding);
        Assert.Equal(new Thickness(14, 8), First<Border>(panel, "panel-foot").Padding);
        Assert.Equal(new Thickness(14, 8), First<Border>(panel, "panel-note").Padding);
        var chip = First<Border>(panel, "panel-ico");
        Assert.Equal(new Size(30, 30), chip.Bounds.Size);
        Assert.Equal(15, First<TextBlock>(panel, "panel-title").FontSize);
        Assert.Equal(new Thickness(10), First<Border>(flushPanel, "panel-body").Padding);
    }

    [AvaloniaFact]
    public void The_new_surfaces_take_the_surface_look()
    {
        var tile = Surface("tile", "أ");
        var sheet = Surface("sheet", "ب");
        var desk = Surface("desk", "ج");
        var card = Surface("card", "د");
        var lifted = Surface("tile hoverable", "هـ");
        ClassTheme.Conflicts.Clear();
        Show(new StackPanel { Children = { tile, sheet, desk, card, lifted } }, FlowDirection.RightToLeft, height: 900);
        foreach (var b in new[] { tile, sheet, desk, card, lifted }) Assert.Same(Res(b, "InsLookSurface"), b.Theme);
        Assert.True(ClassTheme.Conflicts.Count == 0, string.Join("\n", ClassTheme.Conflicts));
    }

    [AvaloniaFact]
    public void No_global_rule_names_a_new_surface_class()
    {
        var found = new List<string>();
        foreach (var cls in new[] { "tile", "sheet", "desk", "low", "dense", "band" })
            found.AddRange(LookCheck.SubjectRules(typeof(Border), cls, "ins-still", "IsInactive"));
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
