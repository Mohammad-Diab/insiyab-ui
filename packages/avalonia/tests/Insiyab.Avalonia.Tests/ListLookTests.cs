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

// The list's parts and the accordion by look classes: every state pictured and dumped for the before/after comparison, runtime class changes, overrides, and which elements take a theme.
public class ListLookTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");

    private static ListBoxItem Row(string title, string? desc = null, string? end = null, bool ico = false, bool endStack = false)
    {
        var text = new StackPanel { Classes = { "list-text" }, Children = { new TextBlock { Classes = { "list-title" }, Text = title } } };
        if (desc is not null) text.Children.Add(new TextBlock { Classes = { "list-desc" }, Text = desc });
        var dock = new DockPanel();
        if (ico)
        {
            var chip = new Border { Classes = { "panel-ico" }, Child = new Icon { Data = Glyph } };
            DockPanel.SetDock(chip, Dock.Left);
            dock.Children.Add(chip);
        }
        if (end is not null)
        {
            Control tail = endStack
                ? new StackPanel { Classes = { "list-end" }, Children = { new TextBlock { Text = end }, new TextBlock { Text = "٣" } } }
                : new TextBlock { Classes = { "list-end" }, Text = end };
            DockPanel.SetDock(tail, Dock.Right);
            dock.Children.Add(tail);
        }
        dock.Children.Add(text);
        return new ListBoxItem { Content = dock };
    }

    private static ListBox Rows(string? cls, int selected)
    {
        var list = new ListBox();
        if (cls is not null) list.Classes.Add(cls);
        list.Items.Add(Row("عام", "الاسم واللغة", "أمس", ico: true));
        list.Items.Add(Row("الحساب", "البريد وكلمة المرور", "اليوم", endStack: true));
        list.Items.Add(Row("الأمان"));
        list.SelectedIndex = selected;
        return list;
    }

    private static (Border Panel, StackPanel Stack, Expander[] Sections) Accordion(bool inPanel)
    {
        var sections = new[]
        {
            new Expander { Header = "القسم الأول", Content = "محتوى القسم الأول.", IsExpanded = true },
            new Expander { Header = "القسم الثاني", Content = "محتوى القسم الثاني." },
            new Expander { Header = "القسم الثالث", Content = "محتوى القسم الثالث." },
        };
        var stack = new StackPanel { Classes = { "accordion" } };
        foreach (var s in sections) stack.Children.Add(s);
        var panel = new Border { Child = stack };
        if (inPanel) panel.Classes.Add("panel");
        return (panel, stack, sections);
    }

    private static TextBlock Title(ListBox list, int index) =>
        list.GetVisualDescendants().OfType<ListBoxItem>().ElementAt(index).GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("list-title"));

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static bool Near(Color a, Color b) =>
        Math.Abs(a.A - b.A) <= 6 && Math.Abs(a.R - b.R) <= 6 && Math.Abs(a.G - b.G) <= 6 && Math.Abs(a.B - b.B) <= 6;

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void List_parts_keep_their_look(string theme, string dir)
    {
        var inPanel = Rows(null, 1);
        var still = Rows("static", 1);
        var flush = Rows(null, 0);
        var bare = Rows(null, 2);
        var page = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 14,
            Width = 460,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children =
            {
                new Border { Classes = { "panel" }, Child = inPanel },
                new Border { Classes = { "panel" }, Child = still },
                new Border { Classes = { "panel" }, Child = new Border { Classes = { "panel-body", "flush" }, Child = flush } },
                bare,
            },
        };
        var w = Show(page, Dir(dir), Variant(theme), 500, 1100);
        LookCheck.Keep(w, page, $"list-{theme}-{dir}");
        AssertBrush(inPanel, "InsToneBrand", () => Title(inPanel, 1).Foreground);
        AssertBrush(inPanel, "InsText1", () => Title(inPanel, 0).Foreground);
        AssertBrush(still, "InsText1", () => Title(still, 1).Foreground);
        AssertBrush(flush, "InsToneBrand", () => Title(flush, 0).Foreground);
        var desc = inPanel.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("list-desc"));
        Assert.Equal(12, desc.FontSize);
        if (dir == "rtl") Assert.Equal(20.4, desc.LineHeight, 1);
        else Assert.True(double.IsNaN(desc.LineHeight));
        Assert.Equal(TextTrimming.CharacterEllipsis, Title(inPanel, 0).TextTrimming);
        Assert.Equal(FontWeight.Bold, Title(inPanel, 0).FontWeight);

        var hovered = bare.GetVisualDescendants().OfType<ListBoxItem>().First();
        w.MouseMove(Center(hovered, w));
        Settle();
        LookCheck.Keep(w, page, $"list-hover-{theme}-{dir}");
        AssertBrush(bare, "InsCtlBg", () => hovered.Background);

        w.SetValue(WindowActivity.IsInactiveProperty, true);
        Settle();
        LookCheck.Keep(w, page, $"list-inactive-{theme}-{dir}");
        AssertBrush(inPanel, "InsText1", () => Title(inPanel, 1).Foreground);
        AssertBrush(flush, "InsText1", () => Title(flush, 0).Foreground);
        w.ClearValue(WindowActivity.IsInactiveProperty);
        Settle();
        AssertBrush(inPanel, "InsToneBrand", () => Title(inPanel, 1).Foreground);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Accordions_keep_their_look(string theme, string dir)
    {
        var (panel, _, inside) = Accordion(true);
        var (bare, _, outside) = Accordion(false);
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 520, HorizontalAlignment = HorizontalAlignment.Left, Children = { panel, bare } };
        var w = Show(page, Dir(dir), Variant(theme), 560, 760);
        LookCheck.Keep(w, page, $"accordion-{theme}-{dir}");
        Assert.Equal(new CornerRadius(21, 21, 0, 0), Part<ToggleButton>(inside[0], "ExpanderHeader").CornerRadius);
        Assert.Equal(new CornerRadius(0, 0, 21, 21), Part<ToggleButton>(inside[2], "ExpanderHeader").CornerRadius);
        Assert.Equal(new Thickness(0, 1, 0, 0), outside[1].BorderThickness);

        inside[1].IsExpanded = true;
        inside[2].IsExpanded = true;
        outside[2].IsExpanded = true;
        Settle();
        LookCheck.Keep(w, page, $"accordion-open-{theme}-{dir}");
        Assert.Equal(new CornerRadius(0), Part<ToggleButton>(inside[2], "ExpanderHeader").CornerRadius);

        var header = Part<ToggleButton>(outside[0], "ExpanderHeader");
        w.MouseMove(Center(header, w));
        Settle();
        LookCheck.Keep(w, page, $"accordion-hover-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void List_classes_added_at_runtime_take_their_look()
    {
        var title = new TextBlock { Text = "العنوان" };
        var desc = new TextBlock { Text = "الوصف" };
        var text = new StackPanel { Children = { title, desc } };
        var end = new StackPanel { Children = { new TextBlock { Text = "أمس" } } };
        var item = new ListBoxItem { Content = new StackPanel { Children = { text, end } } };
        var list = new ListBox { Items = { item, new ListBoxItem { Content = "ثان" } } };
        Show(list, FlowDirection.RightToLeft);
        var plainSize = title.FontSize;
        Assert.Equal(0, text.Spacing);
        title.Classes.Add("list-title");
        desc.Classes.Add("list-desc");
        text.Classes.Add("list-text");
        end.Classes.Add("list-end");
        Settle();
        Assert.Equal(14, title.FontSize);
        Assert.Equal(FontWeight.Bold, title.FontWeight);
        Assert.Equal(23.8, title.LineHeight, 1);
        Assert.Equal(12, desc.FontSize);
        Assert.Equal(2, text.Spacing);
        Assert.Equal(Orientation.Horizontal, end.Orientation);
        Assert.Equal(8, end.Spacing);
        AssertBrush(list, "InsText1", () => title.Foreground);
        list.SelectedIndex = 0;
        Settle();
        AssertBrush(list, "InsToneBrand", () => title.Foreground);
        list.Classes.Add("static");
        Settle();
        AssertBrush(list, "InsText1", () => title.Foreground);
        list.Classes.Remove("static");
        Settle();
        AssertBrush(list, "InsToneBrand", () => title.Foreground);
        title.Classes.Remove("list-title");
        text.Classes.Remove("list-text");
        end.Classes.Remove("list-end");
        Settle();
        Assert.Equal(plainSize, title.FontSize);
        Assert.Equal(0, text.Spacing);
        Assert.Equal(Orientation.Vertical, end.Orientation);
        Assert.True(double.IsNaN(title.LineHeight));
    }

    [AvaloniaFact]
    public void The_accordion_class_turns_the_keys_on_and_off()
    {
        var stack = new StackPanel { Children = { new Expander { Header = "أ", Content = "ب" } } };
        Show(stack, FlowDirection.RightToLeft);
        Assert.False(CollapseLook.GetKeys(stack));
        stack.Classes.Add("accordion");
        Settle();
        Assert.True(CollapseLook.GetKeys(stack));
        stack.Classes.Remove("accordion");
        Settle();
        Assert.False(CollapseLook.GetKeys(stack));
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_list_looks()
    {
        var list = Rows(null, 1);
        var other = Rows(null, 0);
        var page = new StackPanel { Spacing = 12, Width = 460, Children = { list, other } };
        page.Styles.Add(App<TextBlock>("list-desc", TextBlock.ForegroundProperty, Brushes.Red));
        page.Styles.Add(App<StackPanel>("list-text", StackPanel.SpacingProperty, 9d));
        var appStyle = App<TextBlock>("list-title", TextBlock.FontSizeProperty, 30d);
        var typeOnly = new Style(x => x.OfType<TextBlock>()) { Setters = { new Setter(TextBlock.LineHeightProperty, 99d) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(typeOnly);
        try
        {
            Show(page, FlowDirection.RightToLeft, height: 700);
            Title(other, 2).Foreground = Brushes.Green;
            Settle();
            var desc = list.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains("list-desc"));
            Assert.True(Near(Colors.Red, ColorOf(desc.Foreground)));
            Assert.Equal(9, list.GetVisualDescendants().OfType<StackPanel>().First(s => s.Classes.Contains("list-text")).Spacing);
            Assert.Equal(30, Title(list, 0).FontSize);
            Assert.True(Near(Colors.Green, ColorOf(Title(other, 2).Foreground)));
            Assert.Equal(23.8, Title(list, 0).LineHeight, 1);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(typeOnly);
        }
        Settle();
        Assert.Equal(14, Title(list, 0).FontSize);
    }

    [AvaloniaFact]
    public void Only_the_list_parts_take_a_theme()
    {
        var plainText = new TextBlock { Text = "أ" };
        var plainStack = new StackPanel { Children = { plainText } };
        var list = Rows(null, 0);
        var (panel, stack, _) = Accordion(true);
        Show(new StackPanel { Children = { plainStack, list, panel } }, FlowDirection.RightToLeft, height: 900);
        Assert.Null(plainText.Theme);
        Assert.Null(plainStack.Theme);
        Assert.NotNull(stack.Theme);
        foreach (var cls in new[] { "list-title", "list-desc", "list-end" })
            Assert.All(list.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains(cls)), t => Assert.NotNull(t.Theme));
        Assert.All(list.GetVisualDescendants().OfType<StackPanel>().Where(s => s.Classes.Contains("list-text") || s.Classes.Contains("list-end")), s => Assert.NotNull(s.Theme));
        Assert.All(list.GetVisualDescendants().OfType<Border>().Where(b => b.GetType() == typeof(Border) && b.Classes.Contains("panel-ico")), b => Assert.NotNull(b.Theme));
    }

    [AvaloniaFact]
    public void No_global_rule_names_a_list_or_accordion_class()
    {
        var found = new List<string>();
        foreach (var cls in new[] { "list-title", "list-desc", "list-end" })
            found.AddRange(LookCheck.SubjectRules(typeof(TextBlock), cls, "ins-still", "IsInactive"));
        foreach (var cls in new[] { "list-text", "list-end", "accordion" })
            found.AddRange(LookCheck.SubjectRules(typeof(StackPanel), cls, "ins-still", "IsInactive"));
        found = found.Distinct().ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
