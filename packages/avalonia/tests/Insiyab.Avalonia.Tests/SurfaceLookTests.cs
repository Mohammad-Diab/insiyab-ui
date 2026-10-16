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

// The surfaces and the panel's parts by look classes: every state pictured and dumped for the before/after comparison, runtime class changes, overrides, and which elements take a theme.
public class SurfaceLookTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");

    private static Border Surface(string classes, string text, Control? child = null)
    {
        var box = new Border { Padding = new Thickness(12), MinHeight = 44, Child = child ?? new TextBlock { Text = text } };
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) box.Classes.Add(c);
        return box;
    }

    private static Border Stat(string classes)
    {
        var body = new StackPanel { Classes = { "stat-body" }, Children = { new TextBlock { Classes = { "stat-label" }, Text = "الملفات" }, new TextBlock { Classes = { "stat-value" }, Text = "128" } } };
        var tile = new Border { Child = new DockPanel { Children = { new Border { Classes = { "stat-ico" }, Child = new Icon { Data = Glyph } }, body } } };
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) tile.Classes.Add(c);
        return tile;
    }

    private static Border Panel(string mods = "", bool ico = true, bool note = false, bool flush = false)
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

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static bool Moved(Visual v) => v.RenderTransform is { } t && !t.Value.IsIdentity;

    private static bool Near(Color a, Color b) =>
        Math.Abs(a.A - b.A) <= 6 && Math.Abs(a.R - b.R) <= 6 && Math.Abs(a.G - b.G) <= 6 && Math.Abs(a.B - b.B) <= 6;

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Surfaces_keep_their_look(string theme, string dir)
    {
        var plain = new UniformGrid { Columns = 4, ColumnSpacing = 12, Children = { Surface("card", "بطاقة"), Surface("glass", "زجاج"), Surface("glass-inner", "سطح داخلي"), Surface("panel", "لوح") } };
        var cardHover = Surface("card hoverable", "بطاقة ترتفع");
        var glassHover = Surface("glass hoverable", "زجاج يرتفع");
        var lifts = new UniformGrid { Columns = 2, ColumnSpacing = 12, Children = { cardHover, glassHover } };
        var outer = Surface("glass", "", new StackPanel
        {
            Spacing = 8,
            Children = { new TextBlock { Text = "زجاج يضم أسطحًا" }, Surface("glass", "زجاج داخل زجاج"), Surface("glass-inner", "سطح داخلي"), Surface("card", "بطاقة"), Surface("panel", "لوح"), Stat("stat glass sm") },
        });
        var panels = new UniformGrid { Columns = 2, ColumnSpacing = 12, Children = { Panel(note: true), Panel("alert", note: true) } };
        var flush = Panel(flush: true, ico: false);
        var stats = new UniformGrid { Columns = 3, ColumnSpacing = 12, Children = { Stat("stat glass"), Stat("stat glass ok hoverable"), Stat("stat glass lg") } };
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 14, Width = 880, HorizontalAlignment = HorizontalAlignment.Left, Children = { plain, lifts, outer, panels, flush, stats } };
        var w = Show(page, Dir(dir), Variant(theme), 920, 1500);
        LookCheck.Keep(w, page, $"surface-{theme}-{dir}");
        var cards = plain.Children.Cast<Border>().ToList();
        Assert.Equal(new CornerRadius(14), cards[0].CornerRadius);
        Assert.Equal(new CornerRadius(14), cards[1].CornerRadius);
        Assert.Equal(new CornerRadius(14), cards[2].CornerRadius);
        Assert.Equal(new CornerRadius(22), cards[3].CornerRadius);
        Assert.Equal(2, cards[0].BoxShadow.Count);
        Assert.Equal(new Thickness(1), cards[2].BorderThickness);
        AssertBrush(cards[2], "InsGlassInner", () => cards[2].Background);
        var inner = outer.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("glass") && b != outer);
        Assert.Equal(0, inner.BoxShadow.Count);
        AssertBrush(inner, "InsGlassInner", () => inner.Background);

        w.MouseMove(Center(cardHover, w));
        Settle();
        LookCheck.Keep(w, page, $"surface-hover-card-{theme}-{dir}");
        Assert.True(Moved(cardHover));
        Assert.False(Moved(glassHover));

        w.MouseMove(Center(glassHover, w));
        Settle();
        LookCheck.Keep(w, page, $"surface-hover-glass-{theme}-{dir}");
        Assert.True(Moved(glassHover));
        Assert.False(Moved(cardHover));

        Motion.SetStill(w, true);
        Settle();
        LookCheck.Keep(w, page, $"surface-still-{theme}-{dir}");
        Assert.False(Moved(glassHover));
        Assert.Equal(2, glassHover.BoxShadow.Count);
    }

    [AvaloniaFact]
    public void Surface_classes_added_at_runtime_take_their_look()
    {
        var box = new Border { Padding = new Thickness(12), Child = new TextBlock { Text = "نص" } };
        var inner = new Border { Padding = new Thickness(12), Child = new TextBlock { Text = "داخل" } };
        var outer = new Border { Classes = { "glass" }, Padding = new Thickness(20), Child = new StackPanel { Children = { box, inner } } };
        Show(outer, FlowDirection.RightToLeft);
        Assert.Equal(new CornerRadius(0), box.CornerRadius);
        Assert.Equal(0, box.BoxShadow.Count);
        box.Classes.Add("card");
        Settle();
        Assert.Equal(new CornerRadius(14), box.CornerRadius);
        Assert.Equal(2, box.BoxShadow.Count);
        Assert.Equal(new Thickness(1), box.BorderThickness);
        box.Classes.Remove("card");
        Settle();
        Assert.Equal(new CornerRadius(0), box.CornerRadius);
        Assert.Equal(0, box.BoxShadow.Count);
        Assert.Equal(new Thickness(0), box.BorderThickness);
        box.Classes.Add("panel");
        Settle();
        Assert.Equal(new CornerRadius(22), box.CornerRadius);
        box.Classes.Add("hoverable");
        Settle();
        Assert.Equal(3, box.Transitions!.Count);
        box.Classes.Remove("hoverable");
        Settle();
        Assert.True(box.Transitions is null || box.Transitions.Count == 0);
        inner.Classes.Add("glass");
        Settle();
        Assert.Equal(0, inner.BoxShadow.Count);
        AssertBrush(inner, "InsGlassInner", () => inner.Background);
        inner.Classes.Remove("glass");
        Settle();
        Assert.Equal(0, inner.BoxShadow.Count);
        Assert.Null(inner.Background);
        inner.Classes.Add("glass-inner");
        Settle();
        AssertBrush(inner, "InsGlassInner", () => inner.Background);
        Assert.Equal(new Thickness(1), inner.BorderThickness);
    }

    private static void AssertLike(Border reference, Border box)
    {
        Assert.Same(reference.Background, box.Background);
        Assert.Same(reference.BorderBrush, box.BorderBrush);
        Assert.Equal(reference.BoxShadow, box.BoxShadow);
    }

    private static void AssertFlat(Border box)
    {
        Assert.Equal(0, box.BoxShadow.Count);
        AssertBrush(box, "InsGlassInner", () => box.Background);
        Assert.Equal(Colors.Transparent, ColorOf(box.BorderBrush));
    }

    [AvaloniaFact]
    public void Only_glass_inside_glass_strips_to_a_flat_fill()
    {
        var glassInCard = Surface("glass hoverable", "زجاج داخل بطاقة");
        var statInCard = Stat("stat glass hoverable");
        var cardInCard = Surface("card hoverable", "بطاقة داخل بطاقة");
        var card = Surface("card hoverable", "", new StackPanel { Spacing = 8, Children = { glassInCard, statInCard, cardInCard } });
        var glassInGlass = Surface("glass hoverable", "زجاج داخل زجاج");
        var statInGlass = Stat("stat glass hoverable");
        var glass = Surface("glass hoverable", "", new StackPanel { Spacing = 8, Children = { glassInGlass, statInGlass } });
        var refGlass = Surface("glass hoverable", "زجاج");
        var refStat = Stat("stat glass hoverable");
        var refCard = Surface("card hoverable", "بطاقة");
        Show(new StackPanel { Spacing = 12, Children = { card, glass, refGlass, refStat, refCard } }, FlowDirection.RightToLeft, height: 900);
        AssertLike(refGlass, glassInCard);
        AssertLike(refStat, statInCard);
        AssertLike(refCard, cardInCard);
        AssertLike(refCard, card);
        AssertLike(refGlass, glass);
        AssertFlat(glassInGlass);
        AssertFlat(statInGlass);
    }

    [AvaloniaFact]
    public void Glass_inside_follows_the_outer_surface_gaining_and_losing_glass()
    {
        var inCard = Surface("glass", "داخل بطاقة");
        var statInCard = Stat("stat glass");
        var card = Surface("card", "", new StackPanel { Spacing = 8, Children = { inCard, statInCard } });
        var inPlain = Surface("glass hoverable", "داخل إطار");
        var plain = new Border { Padding = new Thickness(12), Child = new StackPanel { Children = { new Border { Child = inPlain } } } };
        var refGlass = Surface("glass", "زجاج");
        var refStat = Stat("stat glass");
        var refHover = Surface("glass hoverable", "زجاج يرتفع");
        Show(new StackPanel { Spacing = 12, Children = { card, plain, refGlass, refStat, refHover } }, FlowDirection.RightToLeft, height: 900);
        AssertLike(refGlass, inCard);
        AssertLike(refStat, statInCard);
        AssertLike(refHover, inPlain);
        card.Classes.Add("glass");
        plain.Classes.Add("glass");
        Settle();
        AssertFlat(inCard);
        AssertFlat(statInCard);
        AssertFlat(inPlain);
        card.Classes.Remove("glass");
        plain.Classes.Remove("glass");
        Settle();
        AssertLike(refGlass, inCard);
        AssertLike(refStat, statInCard);
        AssertLike(refHover, inPlain);
    }

    [AvaloniaFact]
    public void Panel_alert_and_flush_follow_classes_changed_at_runtime()
    {
        var panel = Panel(note: false);
        var flushPanel = Panel(flush: true);
        var w = Show(new StackPanel { Children = { panel, flushPanel } }, FlowDirection.RightToLeft, height: 900);
        var head = First<Border>(panel, "panel-head");
        var title = First<TextBlock>(panel, "panel-title");
        var chip = First<Border>(panel, "panel-ico");
        var body = First<Border>(flushPanel, "panel-body");
        var note = First<Border>(flushPanel, "panel-note");
        Assert.Null(head.Background);
        Assert.Equal(new Thickness(10), body.Padding);
        Assert.Equal(new Thickness(10, 6, 10, 12), note.Padding);
        Assert.Equal(new Thickness(0), note.BorderThickness);
        panel.Classes.Add("alert");
        Settle();
        AssertBrush(head, "InsToneBadBg", () => head.Background);
        AssertBrush(title, "InsToneBad", () => title.Foreground);
        AssertBrush(chip, "InsToneBadBg", () => chip.Background);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), head.CornerRadius);
        panel.Classes.Remove("alert");
        Settle();
        Assert.Null(head.Background);
        Assert.Equal(new CornerRadius(0), head.CornerRadius);
        AssertBrush(chip, "InsPanelIcoBrush", () => chip.Background);
        body.Classes.Remove("flush");
        Settle();
        Assert.Equal(new Thickness(20), body.Padding);
        Assert.Equal(new Thickness(18, 10, 18, 10), note.Padding);
        Assert.Equal(new Thickness(0, 0, 0, 1), note.BorderThickness);
        body.Classes.Add("flush");
        Settle();
        Assert.Equal(new Thickness(10, 6, 10, 12), note.Padding);
        Assert.NotNull(w);
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_surface_looks()
    {
        var card = Surface("card", "بطاقة");
        var glass = Surface("glass", "زجاج");
        var panel = Panel();
        var page = new StackPanel { Spacing = 12, Children = { card, glass, panel } };
        page.Styles.Add(App<Border>("card", Border.CornerRadiusProperty, new CornerRadius(3)));
        page.Styles.Add(App<Border>("panel-head", Border.PaddingProperty, new Thickness(4)));
        page.Styles.Add(App<TextBlock>("panel-title", TextBlock.ForegroundProperty, Brushes.Red));
        var appStyle = App<Border>("panel-foot", Border.PaddingProperty, new Thickness(7));
        var typeOnly = new Style(x => x.OfType<Border>()) { Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(9)) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(typeOnly);
        try
        {
            Show(page, FlowDirection.RightToLeft, height: 700);
            glass.Background = Brushes.Green;
            Settle();
            Assert.Equal(new CornerRadius(3), card.CornerRadius);
            Assert.Equal(new Thickness(4), First<Border>(panel, "panel-head").Padding);
            Assert.True(Near(Colors.Red, ColorOf(First<TextBlock>(panel, "panel-title").Foreground)));
            Assert.Equal(new Thickness(7), First<Border>(panel, "panel-foot").Padding);
            Assert.True(Near(Colors.Green, ColorOf(glass.Background)));
            Assert.Equal(new CornerRadius(14), glass.CornerRadius);
            Assert.Equal(new CornerRadius(22), panel.CornerRadius);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(typeOnly);
        }
        Settle();
        Assert.Equal(new Thickness(18, 12, 18, 12), First<Border>(panel, "panel-foot").Padding);
    }

    [AvaloniaFact]
    public void Only_the_surface_parts_take_a_theme()
    {
        Assert.False(Application.Current!.TryFindResource(typeof(Border), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(TextBlock), out _));
        Assert.False(Application.Current!.TryFindResource(typeof(StackPanel), out _));
        var plain = new Border { Child = new TextBlock { Text = "أ" } };
        var plainStack = new StackPanel();
        var modifiers = new Border { Classes = { "alert", "flush" } };
        var surfaces = new[] { "card", "glass", "panel", "glass-inner", "hoverable", "panel-head", "panel-ico", "panel-note", "panel-body", "panel-foot" }.Select(c => Surface(c, c)).ToList();
        var title = new TextBlock { Classes = { "panel-title" }, Text = "عنوان" };
        var actions = new StackPanel { Classes = { "panel-actions" } };
        var page = new StackPanel { Children = { plain, plainStack, modifiers, title, actions } };
        foreach (var s in surfaces) page.Children.Add(s);
        Show(page, FlowDirection.RightToLeft, height: 900);
        Assert.Null(plain.Theme);
        Assert.Null(((TextBlock)plain.Child!).Theme);
        Assert.Null(plainStack.Theme);
        Assert.Null(modifiers.Theme);
        Assert.All(surfaces, s => Assert.NotNull(s.Theme));
        Assert.NotNull(title.Theme);
        Assert.NotNull(actions.Theme);
        var parts = Panel(note: true);
        Show(parts, FlowDirection.RightToLeft);
        var classed = parts.GetVisualDescendants().OfType<Border>().Where(b => b.GetType() == typeof(Border) && b.Classes.Any(c => !c.StartsWith(':'))).ToList();
        Assert.True(classed.Count >= 5);
        Assert.All(classed, b => Assert.NotNull(b.Theme));
    }

    [AvaloniaFact]
    public void No_global_rule_names_a_surface_class()
    {
        var found = new List<string>();
        foreach (var cls in new[] { "card", "glass", "panel", "glass-inner", "hoverable", "panel-head", "panel-ico", "panel-note", "panel-body", "panel-foot", "alert", "flush" })
            found.AddRange(LookCheck.SubjectRules(typeof(Border), cls, "ins-still", "IsInactive"));
        found.AddRange(LookCheck.SubjectRules(typeof(TextBlock), "panel-title", "ins-still", "IsInactive"));
        found.AddRange(LookCheck.SubjectRules(typeof(StackPanel), "panel-actions", "ins-still", "IsInactive"));
        found = found.Distinct().ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
