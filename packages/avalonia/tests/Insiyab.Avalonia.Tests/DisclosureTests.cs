using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;
using Ellipse = Avalonia.Controls.Shapes.Ellipse;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Insiyab.Avalonia.Tests;

public class DisclosureTests
{
    private sealed record Node(string Name, Node[] Kids);

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Theme(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static void Press(Window w, Key key, RawInputModifiers mods = RawInputModifiers.None, string? symbol = null)
    {
        w.KeyPress(key, mods, PhysicalKey.None, symbol);
        w.KeyRelease(key, mods, PhysicalKey.None, symbol);
        Settle(200);
    }

    // The headless frame pump only runs while something is dirty, so a transition's last frames can go missing; invalidating the window keeps them coming.
    private static void Pump(Window w, int milliseconds = 500)
    {
        var clock = Stopwatch.StartNew();
        do
        {
            w.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(8);
        }
        while (clock.ElapsedMilliseconds < milliseconds);
        Dispatcher.UIThread.RunJobs();
    }

    private static Control? Focused(Window w) => w.FocusManager?.GetFocusedElement() as Control;

    private static void DoubleClick(Control c, Window w)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle(300);
    }

    private static void RightClick(Control c, Window w)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Right);
        w.MouseUp(p, MouseButton.Right);
        Settle(300);
    }

    private static void MouseButtonAt(Control c, Window w, MouseButton b)
    {
        var p = Center(c, w);
        w.MouseMove(p);
        w.MouseDown(p, b);
        w.MouseUp(p, b);
        Settle(300);
    }

    // ------------------------------------------------------------ tabs

    private static TabControl ThreeTabs(params Control[] extra)
    {
        var tc = new TabControl
        {
            Items =
            {
                new TabItem { Header = "عام", Content = "محتوى «عام»." },
                new TabItem { Header = "الحساب", Content = "محتوى «الحساب»." },
                new TabItem { Header = "الإشعارات", Content = "محتوى «الإشعارات»." },
            },
        };
        foreach (var e in extra) tc.Items.Add(e);
        return tc;
    }

    private static TabItem Tab(TabControl tc, int i) => Assert.IsAssignableFrom<TabItem>(tc.Items[i]);

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Tabs_strip_is_the_recessed_track_with_the_selected_tab_raised(string theme, string dir)
    {
        var tc = ThreeTabs();
        var w = Show(tc, Dir(dir), Theme(theme));
        Pump(w);
        var strip = Part<Border>(tc, "PART_Strip");
        Assert.Equal(new Thickness(4), strip.Padding);
        Assert.Equal(new Thickness(1), strip.BorderThickness);
        Assert.Equal(new CornerRadius(14), strip.CornerRadius);
        AssertBrush(strip, "InsCtlBgHover", () => strip.Background);
        AssertBrush(strip, "InsLineGlass", () => strip.BorderBrush);
        Assert.InRange(strip.Bounds.Height, 47, 49);
        var t0 = Tab(tc, 0);
        var t1 = Tab(tc, 1);
        Assert.Equal(38, t0.MinHeight);
        Assert.Equal(new Thickness(14, 8), t0.Padding);
        Assert.Equal(13, t0.FontSize);
        Assert.Equal(FontWeight.Bold, t0.FontWeight);
        Assert.Equal(new CornerRadius(10), t0.CornerRadius);
        var face = Part<Border>(t0, "PART_LayoutRoot");
        AssertBrush(face, "InsSegOnBg", () => face.Background);
        AssertBrush(t0, "InsSegOnFg", () => t0.Foreground);
        AssertBrush(t1, "InsText2", () => t1.Foreground);
        Assert.False(tc.ClipToBounds);
        Capture(w, $"disclosure-tabs-{theme}-{dir}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Tabs_line_look_underlines_the_selected_tab(string theme, string dir)
    {
        var tc = ThreeTabs();
        tc.Classes.Add("line");
        var w = Show(tc, Dir(dir), Theme(theme));
        Pump(w);
        var strip = Part<Border>(tc, "PART_Strip");
        Assert.Equal(new Thickness(0, 0, 0, 1), strip.BorderThickness);
        AssertBrush(strip, "InsLine", () => strip.BorderBrush);
        Assert.Equal(0, ColorOf(strip.Background).A);
        var t0 = Tab(tc, 0);
        var t1 = Tab(tc, 1);
        AssertBrush(t0, "InsToneBrand", () => t0.Foreground);
        var pipe0 = Part<Panel>(t0, "PART_SelectedPipe");
        var pipe1 = Part<Panel>(t1, "PART_SelectedPipe");
        Assert.True(pipe0.IsVisible);
        Assert.InRange(pipe0.RenderTransform!.Value.M11, 0.99, 1.01);
        Assert.True(pipe1.RenderTransform!.Value.M11 < 0.01);
        Assert.Equal(13, t0.FontSize);
        Capture(w, $"disclosure-tabs-line-{theme}-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Tabs_panel_text_takes_the_arabic_line_height(string dir)
    {
        var tc = ThreeTabs();
        Show(tc, Dir(dir));
        var text = tc.GetVisualDescendants().OfType<TextBlock>().First(t => t.Parent == tc);
        Assert.Equal(TextWrapping.Wrap, text.TextWrapping);
        if (dir == "rtl") Assert.Equal(23.8, text.LineHeight);
        else Assert.True(double.IsNaN(text.LineHeight));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Tabs_arrows_follow_the_direction_and_skip_links(string dir)
    {
        var tc = ThreeTabs(new TabLink { Header = "المساعدة" });
        var w = Show(tc, Dir(dir));
        Tab(tc, 0).Focus(NavigationMethod.Tab);
        Press(w, Key.Left);
        Assert.Equal(dir == "rtl" ? 1 : 2, tc.SelectedIndex);
        Assert.Same(tc.ContainerFromIndex(tc.SelectedIndex), Focused(w));
        Press(w, Key.End);
        Assert.Equal(2, tc.SelectedIndex);
        Assert.Same(tc.ContainerFromIndex(2), Focused(w));
        Press(w, Key.Home);
        Assert.Equal(0, tc.SelectedIndex);
        Assert.Same(tc.ContainerFromIndex(0), Focused(w));
    }

    [AvaloniaFact]
    public void Tabs_ctrl_tab_and_ctrl_page_keys_switch_from_inside_the_panel()
    {
        var field = new TextBox();
        var tc = ThreeTabs(new TabLink { Header = "المساعدة" });
        Tab(tc, 0).Content = field;
        var w = Show(tc);
        field.Focus();
        Assert.Same(field, Focused(w));
        Press(w, Key.Tab, RawInputModifiers.Control);
        Assert.Equal(1, tc.SelectedIndex);
        Assert.Same(tc.ContainerFromIndex(1), Focused(w));
        Press(w, Key.Tab, RawInputModifiers.Control);
        Assert.Equal(2, tc.SelectedIndex);
        Press(w, Key.Tab, RawInputModifiers.Control);
        Assert.Equal(0, tc.SelectedIndex);
        Press(w, Key.Tab, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.Equal(2, tc.SelectedIndex);
        Press(w, Key.PageUp, RawInputModifiers.Control);
        Assert.Equal(1, tc.SelectedIndex);
        Press(w, Key.PageDown, RawInputModifiers.Control);
        Assert.Equal(2, tc.SelectedIndex);
        Assert.Same(tc.ContainerFromIndex(2), Focused(w));

        var skipping = ThreeTabs();
        Tab(skipping, 1).IsEnabled = false;
        var w2 = Show(skipping);
        Tab(skipping, 0).Focus(NavigationMethod.Tab);
        Press(w2, Key.Tab, RawInputModifiers.Control);
        Assert.Equal(2, skipping.SelectedIndex);
    }

    [AvaloniaFact]
    public void Tabs_nested_tab_control_keeps_its_own_ctrl_tab()
    {
        var inner = new TabControl
        {
            Items =
            {
                new TabItem { Header = "أول", Content = "محتوى «أول»." },
                new TabItem { Header = "ثان", Content = "محتوى «ثان»." },
            },
        };
        var outer = ThreeTabs();
        Tab(outer, 0).Content = inner;
        var w = Show(outer);
        Tab(inner, 0).Focus(NavigationMethod.Tab);
        Press(w, Key.Tab, RawInputModifiers.Control);
        Assert.Equal(1, inner.SelectedIndex);
        Assert.Equal(0, outer.SelectedIndex);
    }

    [AvaloniaFact]
    public void Tabs_tab_enters_at_the_selected_tab_then_goes_into_the_panel()
    {
        var before = new Button { Content = "قبل" };
        var after = new Button { Content = "بعد" };
        var field = new TextBox();
        var tc = ThreeTabs();
        Tab(tc, 1).Content = field;
        tc.SelectedIndex = 1;
        var w = Show(new StackPanel { Children = { before, tc, after } });
        before.Focus(NavigationMethod.Tab);
        Press(w, Key.Tab);
        Assert.Same(tc.ContainerFromIndex(1), Focused(w));
        Press(w, Key.Tab);
        Assert.Same(field, Focused(w));
    }

    [AvaloniaFact]
    public void Tabs_link_tab_clicks_and_never_selects()
    {
        var link = new TabLink { Header = "المساعدة" };
        var tc = ThreeTabs(link);
        var clicks = 0;
        link.Click += (_, _) => clicks++;
        var w = Show(tc);
        Click(link, w);
        Assert.Equal(1, clicks);
        Assert.Equal(0, tc.SelectedIndex);
        link.Focus(NavigationMethod.Tab);
        Press(w, Key.Enter);
        Assert.Equal(2, clicks);
        Assert.Equal(0, tc.SelectedIndex);
        link.IsCurrent = true;
        Pump(w);
        AssertBrush(link, "InsSegOnFg", () => link.Foreground);
    }

    [AvaloniaFact]
    public void Tabs_panel_rises_without_leaving_a_transform()
    {
        var tc = ThreeTabs();
        var w = Show(tc);
        tc.SelectedIndex = 1;
        Pump(w, 800);
        var a = Part<ContentPresenter>(tc, "PART_SelectedContentHost");
        var b = Part<ContentPresenter>(tc, "PART_SelectedContentHost2");
        Assert.Null(a.RenderTransform);
        Assert.Null(b.RenderTransform);
        var shown = a.IsVisible ? a : b;
        Assert.Equal(1, shown.Opacity);
    }

    [AvaloniaFact]
    public void Tab_strip_alone_has_the_same_keys_and_no_clip()
    {
        var strip = new TabStrip
        {
            Items =
            {
                new TabStripItem { Content = "عام" },
                new TabStripItem { Content = "الحساب" },
                new TabStripItem { Content = "الإشعارات" },
            },
            SelectedIndex = 0,
        };
        var w = Show(strip);
        Assert.False(strip.ClipToBounds);
        Assert.IsAssignableFrom<Control>(strip.Items[0]).Focus(NavigationMethod.Tab);
        Press(w, Key.Tab, RawInputModifiers.Control);
        Assert.Equal(1, strip.SelectedIndex);
    }

    // ------------------------------------------------------------ accordion

    private static (Border Panel, StackPanel Stack, Expander[] Sections) Accordion()
    {
        var sections = new[]
        {
            new Expander { Header = "القسم الأول", Content = "محتوى القسم الأول.", IsExpanded = true },
            new Expander { Header = "القسم الثاني", Content = "محتوى القسم الثاني." },
            new Expander { Header = "القسم الثالث", Content = "محتوى القسم الثالث." },
        };
        var stack = new StackPanel { Classes = { "accordion" } };
        foreach (var s in sections) stack.Children.Add(s);
        return (new Border { Classes = { "panel" }, Child = stack }, stack, sections);
    }

    private static ToggleButton HeaderOf(Expander e) => Part<ToggleButton>(e, "ExpanderHeader");

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Accordion_summary_and_body_metrics(string theme, string dir)
    {
        var (panel, _, sections) = Accordion();
        var w = Show(panel, Dir(dir), Theme(theme));
        Pump(w);
        var rtl = dir == "rtl";
        var head = HeaderOf(sections[0]);
        Assert.Equal(new Thickness(16, 14), head.Padding);
        Assert.Equal(14, head.FontSize);
        Assert.Equal(FontWeight.Bold, head.FontWeight);
        if (rtl) Assert.InRange(head.Bounds.Height, 50.8, 52.8);
        else Assert.InRange(head.Bounds.Height, 45, 47);
        var title = head.GetVisualDescendants().OfType<TextBlock>().First(t => t.Parent == head);
        if (rtl) Assert.Equal(23.8, title.LineHeight);
        else Assert.True(double.IsNaN(title.LineHeight));
        var body = sections[0].GetVisualDescendants().OfType<TextBlock>().First(t => t.Parent == sections[0]);
        Assert.Equal(25.2, body.LineHeight);
        Assert.Equal(TextWrapping.Wrap, body.TextWrapping);
        Assert.Equal(new Thickness(16, 0, 16, 16), Part<Border>(sections[0], "ExpanderContent").Padding);
        AssertBrush(head, "InsToneBrand", () => head.Foreground);
        var open = Part<ShapePath>(head, "PART_Chevron");
        AssertBrush(open, "InsToneBrandSoft", () => open.Stroke);
        Assert.InRange(open.RenderTransform!.Value.M11, -1.01, -0.99);
        Assert.Equal(13, open.Width);
        Assert.Equal(8, open.Height);
        var closed = Part<ShapePath>(HeaderOf(sections[1]), "PART_Chevron");
        AssertBrush(closed, "InsText3", () => closed.Stroke);
        Capture(w, $"disclosure-accordion-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void Accordion_in_a_panel_squares_inner_corners()
    {
        var (panel, _, sections) = Accordion();
        Show(panel);
        Assert.Equal(new CornerRadius(21, 21, 0, 0), HeaderOf(sections[0]).CornerRadius);
        Assert.Equal(new CornerRadius(0, 0, 21, 21), HeaderOf(sections[2]).CornerRadius);
        Assert.Equal(new Thickness(0), sections[0].BorderThickness);
        Assert.Equal(new Thickness(0, 1, 0, 0), sections[1].BorderThickness);
        Assert.Equal(new Thickness(0, 1, 0, 0), sections[2].BorderThickness);
    }

    [AvaloniaFact]
    public void Collapse_group_keeps_one_open()
    {
        var (panel, _, sections) = Accordion();
        foreach (var s in sections) Collapse.SetGroup(s, "g");
        Show(panel);
        Assert.True(sections[0].IsExpanded);
        sections[2].IsExpanded = true;
        Settle();
        Assert.False(sections[0].IsExpanded);
        Assert.True(sections[2].IsExpanded);
    }

    [AvaloniaFact]
    public void Accordion_headers_are_a_keyboard_list()
    {
        var before = new Button { Content = "قبل" };
        var (panel, _, sections) = Accordion();
        var w = Show(new StackPanel { Children = { before, panel } });
        before.Focus(NavigationMethod.Tab);
        Press(w, Key.Tab);
        Assert.Same(HeaderOf(sections[0]), Focused(w));
        Press(w, Key.Tab);
        Assert.Same(HeaderOf(sections[1]), Focused(w));
        Press(w, Key.Space);
        Assert.True(sections[1].IsExpanded);
        Press(w, Key.Enter);
        Assert.False(sections[1].IsExpanded);
        Press(w, Key.Down);
        Assert.Same(HeaderOf(sections[2]), Focused(w));
        Press(w, Key.Down);
        Assert.Same(HeaderOf(sections[2]), Focused(w));
        Press(w, Key.Up);
        Assert.Same(HeaderOf(sections[1]), Focused(w));
        Press(w, Key.Home);
        Assert.Same(HeaderOf(sections[0]), Focused(w));
        Press(w, Key.End);
        Assert.Same(HeaderOf(sections[2]), Focused(w));
    }

    [AvaloniaFact]
    public void Accordion_opens_with_a_height_animation()
    {
        var (panel, _, sections) = Accordion();
        Show(panel);
        var section = sections[1];
        var expand = Part<ExpandPresenter>(section, "PART_Expand");
        var content = Part<Border>(section, "ExpanderContent");
        section.IsExpanded = true;
        Settle(60);
        Assert.True(expand.Bounds.Height > 0);
        Assert.True(expand.Bounds.Height < content.DesiredSize.Height);
        Settle();
        Assert.InRange(expand.Bounds.Height, content.DesiredSize.Height - 1, content.DesiredSize.Height + 1);
        // Once the open transition ended, a progress set with SetCurrentValue fell back to 0 and the section closed again.
        Settle(600);
        Assert.InRange(expand.Bounds.Height, content.DesiredSize.Height - 1, content.DesiredSize.Height + 1);
    }

    // ------------------------------------------------------------ steps

    private static Steps StepsOf(params StepsItem[] items)
    {
        var steps = new Steps();
        foreach (var i in items) steps.Items.Add(i);
        return steps;
    }

    private static StepsItem Step(string name, bool done = false, bool current = false, bool flagged = false, bool error = false) =>
        new() { Content = name, IsDone = done, IsCurrent = current, IsFlagged = flagged, IsError = error };

    private static StepsItem StepAt(Steps s, int i) => Assert.IsType<StepsItem>(s.Items[i]);

    [AvaloniaFact]
    public void Steps_number_every_step_including_the_last()
    {
        var four = StepsOf(Step("الأولى"), Step("الثانية"), Step("الثالثة"), Step("الرابعة"));
        var single = StepsOf(Step("الأولى"));
        var w = Show(new StackPanel { Children = { four, single } });
        Assert.Equal([1, 2, 3, 4], Enumerable.Range(0, 4).Select(i => StepAt(four, i).Number).ToArray());
        Assert.Equal(1, StepAt(single, 0).Number);
        StepAt(four, 1).IsVisible = false;
        Settle(100);
        Assert.Equal(1, StepAt(four, 0).Number);
        Assert.Equal(2, StepAt(four, 2).Number);
        Assert.Equal(3, StepAt(four, 3).Number);
        Assert.Contains(":first", StepAt(four, 0).Classes);
        Assert.Contains(":last", StepAt(four, 3).Classes);
        Assert.DoesNotContain(":last", StepAt(four, 2).Classes);
        Assert.NotNull(w);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Steps_states_take_the_web_colours(string theme, string dir)
    {
        var first = StepsOf(Step("الأولى", done: true, flagged: true), Step("الثانية", current: true), Step("الثالثة"), Step("الرابعة"));
        var second = StepsOf(Step("الأولى", done: true), Step("الثانية", done: true, error: true), Step("الثالثة", current: true), Step("الرابعة"));
        var w = Show(new StackPanel { Children = { first, second } }, Dir(dir), Theme(theme));
        Pump(w);
        Assert.False(first.ClipToBounds);
        Assert.False(StepAt(first, 0).ClipToBounds);
        var plain = StepAt(first, 2);
        var circle = Part<Border>(plain, "PART_Circle");
        Assert.Equal(32, circle.Bounds.Width);
        Assert.Equal(32, circle.Bounds.Height);
        AssertBrush(circle, "InsCtlBd", () => circle.BorderBrush);
        AssertBrush(circle, "InsSurface", () => circle.Background);
        var done = StepAt(first, 0);
        Assert.Equal(1, Part<Border>(done, "PART_Fill").Opacity);
        var number = Part<TextBlock>(done, "PART_Number");
        AssertBrush(number, "InsOnPrimary", () => number.Foreground);
        var current = StepAt(first, 1);
        var currentCircle = Part<Border>(current, "PART_Circle");
        AssertBrush(currentCircle, "InsToneBrandSoft", () => currentCircle.BorderBrush);
        AssertBrush(current, "InsToneBrand", () => current.Foreground);
        var bad = StepAt(second, 1);
        var badCircle = Part<Border>(bad, "PART_Circle");
        AssertBrush(badCircle, "InsToneBad", () => badCircle.BorderBrush);
        AssertBrush(badCircle, "InsToneBadBg", () => badCircle.Background);
        Assert.Equal(0, Part<Border>(bad, "PART_Fill").Opacity);
        Assert.True(Part<Border>(done, "PART_Flag").IsVisible);
        var joinIn = Part<Border>(current, "PART_LineStart");
        AssertBrush(joinIn, "InsToneBrandSoft", () => joinIn.Background);
        Assert.False(Part<Border>(done, "PART_LineStart").IsVisible);
        Assert.False(Part<Border>(StepAt(first, 3), "PART_LineEnd").IsVisible);
        var label = plain.GetVisualDescendants().OfType<TextBlock>().First(t => t.Parent == plain);
        Assert.Equal(TextAlignment.Center, label.TextAlignment);
        Capture(w, $"disclosure-steps-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void Steps_dots_current_is_one_and_a_half_times_and_not_clipped()
    {
        var steps = StepsOf(Step("الأولى", done: true), Step("الثانية", current: true), Step("الثالثة"), Step("الرابعة"));
        steps.Classes.Add("dots");
        var w = Show(steps);
        Pump(w);
        Assert.False(steps.ClipToBounds);
        Assert.False(StepAt(steps, 1).ClipToBounds);
        var dot = Part<Panel>(StepAt(steps, 1), "PART_Dot");
        Assert.Equal(8, dot.Bounds.Width);
        Assert.Equal(8, dot.Bounds.Height);
        Assert.InRange(dot.RenderTransform!.Value.M11, 1.49, 1.51);
        Assert.False(Part<TextBlock>(StepAt(steps, 1), "PART_Number").IsVisible);
        Assert.False(Part<ContentPresenter>(StepAt(steps, 1), "PART_ContentPresenter").IsVisible);
        var done = Part<Border>(StepAt(steps, 0), "PART_Circle");
        AssertBrush(done, "InsToneBrandSoft", () => done.Background);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Steps_grid_is_one_tab_stop_with_arrow_keys(string dir)
    {
        var before = new Button { Content = "قبل" };
        var grid = new Steps { Width = 200 };
        grid.Classes.Add("grid");
        for (var i = 1; i <= 12; i++) grid.Items.Add(Step($"البند {i}", done: i < 2, current: i == 2));
        var w = Show(new StackPanel { Children = { before, grid } }, Dir(dir));
        var items = Enumerable.Range(0, 12).Select(i => StepAt(grid, i)).ToList();
        Assert.Equal([1], items.Select((s, i) => (s, i)).Where(t => t.s.IsTabStop).Select(t => t.i).ToArray());
        before.Focus(NavigationMethod.Tab);
        Press(w, Key.Tab);
        Assert.Same(items[1], Focused(w));
        Press(w, Key.Right);
        Assert.Same(items[dir == "rtl" ? 0 : 2], Focused(w));
        Press(w, Key.Home);
        Assert.Same(items[0], Focused(w));
        Press(w, Key.End);
        Assert.Same(items[11], Focused(w));
        var lastRow = Center(items[11], w).Y;
        Press(w, Key.Up);
        Assert.True(Center(Assert.IsType<StepsItem>(Focused(w)), w).Y < lastRow - 1);
        Press(w, Key.Down);
        Assert.InRange(Center(Assert.IsType<StepsItem>(Focused(w)), w).Y, lastRow - 1, lastRow + 1);
        var pressed = 0;
        var target = Assert.IsType<StepsItem>(Focused(w));
        target.Click += (_, _) => pressed++;
        Press(w, Key.Enter);
        Assert.Equal(1, pressed);
    }

    // ------------------------------------------------------------ wizard

    private static Wizard Form(out TextBox name, out TextBox email, out CheckBox agree)
    {
        name = new TextBox();
        email = new TextBox { FlowDirection = FlowDirection.LeftToRight };
        agree = new CheckBox { Content = "أوافق" };
        Wizard.SetRequired(name, true);
        Wizard.SetRequired(email, true);
        Wizard.SetRequired(agree, true);
        return new Wizard
        {
            Items =
            {
                new WizardPanel { Header = "الاسم", Content = new StackPanel { Children = { new TextBlock { Text = "الاسم" }, name } } },
                new WizardPanel { Header = "البريد", Content = new StackPanel { Children = { new TextBlock { Text = "البريد" }, email } } },
                new WizardPanel { Header = "التأكيد", Content = agree },
            },
        };
    }

    private static WizardPanel PanelAt(Wizard wiz, int i) => Assert.IsType<WizardPanel>(wiz.Items[i]);

    [AvaloniaFact]
    public void Wizard_next_checks_required_fields_first()
    {
        var wiz = Form(out var name, out _, out _);
        var moved = new List<int>();
        wiz.Moved += (_, e) => moved.Add(e.Step);
        var w = Show(wiz);
        wiz.Next();
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        Assert.True(DataValidationErrors.GetHasErrors(name));
        name.Text = "سارة";
        Settle();
        wiz.Next();
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        Assert.Contains(1, moved);
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_enter_is_next_and_finish_on_the_last_step()
    {
        var wiz = Form(out var name, out var email, out var agree);
        var finished = 0;
        wiz.Finished += (_, _) => finished++;
        var w = Show(wiz);
        name.Text = "سارة";
        name.Focus();
        Press(w, Key.Enter);
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        email.Text = "a@b.c";
        email.Focus();
        Press(w, Key.Enter);
        Settle();
        Assert.Equal(2, wiz.CurrentIndex);
        agree.IsChecked = true;
        PanelAt(wiz, 2).Focus();
        Press(w, Key.Enter);
        Assert.Equal(1, finished);
        w.Close();

        var multi = Form(out var note, out _, out _);
        note.AcceptsReturn = true;
        note.Text = "سارة";
        var w2 = Show(multi);
        note.Focus();
        Press(w2, Key.Enter);
        Settle();
        Assert.Equal(0, multi.CurrentIndex);
        w2.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Wizard_alt_arrows_follow_the_direction(string dir)
    {
        var wiz = Form(out var name, out var email, out _);
        var w = Show(wiz, Dir(dir));
        name.Text = "سارة";
        email.Text = "a@b.c";
        wiz.Next();
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        var back = dir == "rtl" ? Key.Right : Key.Left;
        var forward = dir == "rtl" ? Key.Left : Key.Right;
        Press(w, back, RawInputModifiers.Alt);
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        Press(w, forward, RawInputModifiers.Alt);
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_mouse_back_and_forward_buttons()
    {
        var wiz = Form(out var name, out var email, out _);
        var w = Show(wiz);
        name.Text = "سارة";
        email.Text = "a@b.c";
        wiz.Next();
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        MouseButtonAt(email, w, MouseButton.XButton1);
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        MouseButtonAt(name, w, MouseButton.XButton2);
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_busy_waits_with_a_spinner()
    {
        var wiz = Form(out var name, out var email, out _);
        wiz.Leaving += (_, e) => e.Wait();
        var w = Show(wiz);
        name.Text = "سارة";
        email.Text = "a@b.c";
        wiz.Next();
        Settle(100);
        Assert.True(wiz.IsBusy);
        Assert.Equal(0, wiz.CurrentIndex);
        var next = Part<Button>(wiz, "PART_Next");
        var spinner = Assert.IsType<LoadingSpinner>(Part<Control>(wiz, "PART_NextSpinner"));
        Assert.True(spinner.IsVisible);
        Assert.Equal(0.75, next.Opacity);
        Assert.False(next.IsHitTestVisible);
        wiz.Proceed();
        Settle();
        Assert.Equal(1, wiz.CurrentIndex);
        Assert.False(wiz.IsBusy);
        Assert.False(spinner.IsVisible);
        wiz.Next();
        Settle(100);
        Assert.True(wiz.IsBusy);
        wiz.Stay();
        Settle(100);
        Assert.False(wiz.IsBusy);
        Assert.Equal(1, wiz.CurrentIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_skipped_step_drops_its_number()
    {
        var wiz = Form(out _, out _, out _);
        var w = Show(wiz);
        PanelAt(wiz, 1).IsSkipped = true;
        Settle();
        Assert.Equal(2, wiz.Panels.Count);
        var steps = Part<Steps>(wiz, "PART_Steps");
        Assert.True(StepAt(steps, 0).IsVisible);
        Assert.False(StepAt(steps, 1).IsVisible);
        Assert.Equal(1, StepAt(steps, 0).Number);
        Assert.Equal(2, StepAt(steps, 2).Number);
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_first_and_last_step_buttons()
    {
        var wiz = Form(out _, out _, out _);
        var w = Show(wiz);
        var previous = Part<Button>(wiz, "PART_Previous");
        Assert.Equal(0, previous.Opacity);
        Assert.False(previous.IsHitTestVisible);
        wiz.GoTo(2);
        Settle();
        Assert.False(Part<Button>(wiz, "PART_Next").IsVisible);
        Assert.True(Part<Button>(wiz, "PART_Finish").IsVisible);
        Assert.Equal(1, previous.Opacity);
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_cancel_sits_at_the_start()
    {
        var wiz = Form(out _, out _, out _);
        wiz.CancelContent = "إلغاء";
        var cancelled = 0;
        wiz.Cancelled += (_, _) => cancelled++;
        var w = Show(wiz);
        var cancel = Part<Button>(wiz, "PART_Cancel");
        Assert.True(cancel.IsVisible);
        Assert.Equal(0, Grid.GetColumn(cancel));
        Assert.Equal(2, Grid.GetColumn(Part<Button>(wiz, "PART_Previous")));
        Assert.Equal(new Thickness(16, 6), cancel.Padding);
        Assert.Equal(38, cancel.MinHeight);
        Click(cancel, w);
        Assert.Equal(1, cancelled);
        w.Close();

        var plain = Form(out _, out _, out _);
        var w2 = Show(plain);
        Assert.False(Part<Button>(plain, "PART_Cancel").IsVisible);
        Assert.Equal(0, Grid.GetColumn(Part<Button>(plain, "PART_Previous")));
        w2.Close();
    }

    [AvaloniaFact]
    public void Wizard_moves_by_transform_operations_and_leaves_none()
    {
        var wiz = Form(out var name, out var email, out _);
        var w = Show(wiz, width: 640, height: 420);
        name.Text = "سارة";
        email.Text = "a@b.c";
        wiz.Next();
        Settle(60);
        var arriving = PanelAt(wiz, 1);
        if (arriving.RenderTransform is not null) Assert.IsNotType<TranslateTransform>(arriving.RenderTransform);
        Pump(w, 800);
        foreach (var p in wiz.Items.OfType<WizardPanel>())
        {
            Assert.False(p.IsSet(Visual.RenderTransformProperty));
            Assert.False(p.IsSet(Visual.OpacityProperty));
        }
        wiz.Motion = WizardMotion.Slide;
        wiz.Previous();
        Settle(60);
        foreach (var p in wiz.Items.OfType<WizardPanel>())
            if (p.RenderTransform is not null) Assert.IsNotType<TranslateTransform>(p.RenderTransform);
        Pump(w, 1500);
        foreach (var p in wiz.Items.OfType<WizardPanel>())
        {
            Assert.False(p.IsSet(Visual.RenderTransformProperty));
            Assert.False(p.IsSet(Visual.OpacityProperty));
        }
        Assert.Equal(0, wiz.CurrentIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Wizard_finish_goes_to_the_first_bad_step()
    {
        var wiz = Form(out _, out var email, out var agree);
        var finished = 0;
        wiz.Finished += (_, _) => finished++;
        var w = Show(wiz);
        Assert.False(wiz.ClipToBounds);
        email.Text = "a@b.c";
        agree.IsChecked = true;
        wiz.GoTo(2);
        Settle();
        Assert.Equal(2, wiz.CurrentIndex);
        wiz.Finish();
        Settle();
        Assert.Equal(0, wiz.CurrentIndex);
        Assert.Equal(0, finished);
        w.Close();
    }

    // ------------------------------------------------------------ tree

    private sealed class Files
    {
        public TreeView Tree = new();
        public TreeViewItem Contracts = Row("العقود");
        public TreeViewItem Contract = Row("عقد.pdf");
        public TreeViewItem Annexes = Row("الملاحق");
        public TreeViewItem Annex1 = Row("ملحق 1.pdf");
        public TreeViewItem Annex2 = Row("ملحق 2.pdf");
        public TreeViewItem Invoices = Row("الفواتير");
        public TreeViewItem Invoice = Row("فاتورة.pdf");
        public TreeViewItem Notes = Row("ملاحظات.txt");

        public Files()
        {
            Annexes.Items.Add(Annex1);
            Annexes.Items.Add(Annex2);
            Contracts.Items.Add(Contract);
            Contracts.Items.Add(Annexes);
            Invoices.Items.Add(Invoice);
            Tree.Items.Add(Contracts);
            Tree.Items.Add(Invoices);
            Tree.Items.Add(Notes);
        }

        public void Open(bool contracts, bool annexes = false)
        {
            Contracts.IsExpanded = contracts;
            Annexes.IsExpanded = annexes;
        }

        private static TreeViewItem Row(string text) => new() { Header = text };
    }

    private static ContentPresenter HeaderOf(TreeViewItem item) => Part<ContentPresenter>(item, "PART_HeaderPresenter");

    private static Border RowOf(TreeViewItem item) => Part<Border>(item, "PART_LayoutRoot");

    private static ShapePath ChevronOf(TreeViewItem item) => Part<ShapePath>(item, "PART_Chevron");

    private static ToggleButton ToggleOf(TreeViewItem item) => Part<ToggleButton>(item, "PART_ExpandCollapseChevron");

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Tree_look_matches_the_web_rows(string theme, string dir)
    {
        var f = new Files();
        f.Open(true);
        var w = Show(f.Tree, Dir(dir), Theme(theme));
        f.Notes.IsSelected = true;
        Pump(w);
        var row = RowOf(f.Notes);
        Assert.Equal(34, row.MinHeight);
        Assert.Equal(new Thickness(6, 3, 8, 3), row.Padding);
        Assert.Equal(new CornerRadius(10), row.CornerRadius);
        AssertBrush(row, "InsTreeSelected", () => row.Background);
        Assert.Equal(0.1, Assert.IsAssignableFrom<ISolidColorBrush>(row.Background).Opacity, 3);
        var header = HeaderOf(f.Notes);
        AssertBrush(header, "InsPrimaryDarker", () => header.Foreground);
        Assert.Equal(FontWeight.Bold, header.FontWeight);
        var branch = Part<Border>(f.Contracts, "PART_Branch");
        Assert.Equal(new Thickness(14, 0, 0, 0), branch.Margin);
        Assert.Equal(new Thickness(8, 0, 0, 0), branch.Padding);
        Assert.Equal(new Thickness(1, 0, 0, 0), branch.BorderThickness);
        AssertBrush(branch, "InsLine", () => branch.BorderBrush);
        Assert.False(ToggleOf(f.Notes).IsVisible);
        var chevron = ChevronOf(f.Contracts);
        Assert.Equal(5, chevron.Width);
        Assert.Equal(10, chevron.Height);
        Capture(w, $"disclosure-tree-{theme}-{dir}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Tree_chevron_points_into_a_closed_branch_and_down_an_open_one(string dir)
    {
        var f = new Files();
        f.Open(true);
        var w = Show(f.Tree, Dir(dir));
        var c = ChevronOf(f.Annexes);
        var tip = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height / 2), w)!.Value;
        var tail = c.TranslatePoint(new Point(0, c.Bounds.Height / 2), w)!.Value;
        if (dir == "ltr") Assert.True(tip.X > tail.X);
        else Assert.True(tip.X < tail.X);
        f.Annexes.IsExpanded = true;
        Pump(w);
        tip = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height / 2), w)!.Value;
        tail = c.TranslatePoint(new Point(0, c.Bounds.Height / 2), w)!.Value;
        Assert.True(tip.Y > tail.Y + 2);
    }

    [AvaloniaFact]
    public void Tree_selection_follows_the_arrows_and_ctrl_only_moves()
    {
        var f = new Files();
        f.Open(true);
        var w = Show(f.Tree);
        f.Contracts.Focus(NavigationMethod.Tab);
        Press(w, Key.Down);
        Assert.Same(f.Contract, Focused(w));
        Assert.True(f.Contract.IsSelected);
        Press(w, Key.Down, RawInputModifiers.Control);
        Assert.Same(f.Annexes, Focused(w));
        Assert.True(f.Contract.IsSelected);
        Assert.False(f.Annexes.IsSelected);
        Press(w, Key.Space);
        Assert.True(f.Annexes.IsSelected);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Tree_inward_and_outward_arrows_follow_the_direction(string dir)
    {
        var f = new Files();
        f.Open(true);
        var w = Show(f.Tree, Dir(dir));
        var inward = dir == "rtl" ? Key.Left : Key.Right;
        var outward = dir == "rtl" ? Key.Right : Key.Left;
        f.Annexes.Focus(NavigationMethod.Tab);
        Press(w, inward);
        Assert.True(f.Annexes.IsExpanded);
        Press(w, inward);
        Assert.Same(f.Annex1, Focused(w));
        Press(w, outward);
        Assert.Same(f.Annexes, Focused(w));
        Assert.True(f.Annexes.IsExpanded);
        Press(w, outward);
        Assert.False(f.Annexes.IsExpanded);
    }

    [AvaloniaFact]
    public void Tree_numpad_star_plus_minus()
    {
        var f = new Files();
        var w = Show(f.Tree);
        f.Contracts.Focus(NavigationMethod.Tab);
        Press(w, Key.Multiply);
        Assert.True(f.Contracts.IsExpanded);
        Assert.True(f.Annexes.IsExpanded);
        Assert.False(f.Invoices.IsExpanded);
        f.Open(false);
        Settle();
        f.Contracts.Focus(NavigationMethod.Tab);
        Press(w, Key.D8, RawInputModifiers.Shift, "*");
        Assert.True(f.Contracts.IsExpanded);
        Assert.True(f.Annexes.IsExpanded);
        Assert.False(f.Invoices.IsExpanded);
        Press(w, Key.Subtract);
        Assert.False(f.Contracts.IsExpanded);
        Press(w, Key.Add);
        Assert.True(f.Contracts.IsExpanded);
    }

    [AvaloniaFact]
    public void Tree_type_ahead_builds_a_prefix()
    {
        var f = new Files();
        f.Open(true, true);
        var w = Show(f.Tree);
        f.Contracts.Focus(NavigationMethod.Tab);
        Press(w, Key.M, RawInputModifiers.None, "م");
        Assert.Same(f.Annex1, Focused(w));
        Assert.True(f.Annex1.IsSelected);
        Press(w, Key.L, RawInputModifiers.None, "ل");
        Press(w, Key.A, RawInputModifiers.None, "ا");
        Assert.Same(f.Notes, Focused(w));
        Assert.True(f.Notes.IsSelected);
        Settle(1100);
        Press(w, Key.E, RawInputModifiers.None, "ع");
        Assert.Same(f.Contract, Focused(w));
        Assert.True(f.Contract.IsSelected);
    }

    [AvaloniaFact]
    public void Tree_click_selects_double_click_opens()
    {
        var f = new Files();
        var w = Show(f.Tree);
        var activated = new List<object?>();
        Tree.AddActivatedHandler(f.Tree, (_, e) => activated.Add(e.Source));
        Click(HeaderOf(f.Invoices), w);
        Assert.True(f.Invoices.IsSelected);
        Assert.False(f.Invoices.IsExpanded);
        DoubleClick(HeaderOf(f.Invoices), w);
        Assert.True(f.Invoices.IsExpanded);
        Settle(800);
        DoubleClick(HeaderOf(f.Invoices), w);
        Assert.False(f.Invoices.IsExpanded);
        f.Invoices.IsExpanded = true;
        Settle();
        Click(ToggleOf(f.Invoices), w);
        Assert.False(f.Invoices.IsExpanded);
        Assert.Same(f.Invoices, Focused(w));
        Settle(800);
        DoubleClick(HeaderOf(f.Notes), w);
        Assert.Single(activated);
        Assert.Same(f.Notes, activated[0]);
        Press(w, Key.Enter);
        Assert.Equal(2, activated.Count);
        Assert.Same(f.Notes, activated[1]);
        f.Invoices.Focus(NavigationMethod.Tab);
        Press(w, Key.Enter);
        Assert.True(f.Invoices.IsExpanded);
    }

    [AvaloniaFact]
    public void Tree_right_click_selects_the_row_and_opens_its_menu()
    {
        var f = new Files();
        var menu = new ContextMenu { Items = { new MenuItem { Header = "فتح" } } };
        f.Tree.ContextMenu = menu;
        var w = Show(f.Tree);
        RightClick(HeaderOf(f.Invoices), w);
        Assert.True(f.Invoices.IsSelected);
        Assert.True(menu.IsOpen);
        Assert.Equal(PlacementMode.Pointer, menu.Placement);
        menu.Close();
        Settle();
        Assert.Same(f.Invoices, Focused(w));
        f.Notes.Focus(NavigationMethod.Tab);
        Press(w, Key.Apps);
        Assert.True(menu.IsOpen);
        Assert.Equal(PlacementMode.Bottom, menu.Placement);
        Assert.Same(RowOf(f.Notes), menu.PlacementTarget);
        menu.Close();
        Settle();

        f.Tree.ContextMenu = null;
        var flyout = new PopMenu { Items = { new MenuItem { Header = "فتح" } } };
        f.Tree.ContextFlyout = flyout;
        RightClick(HeaderOf(f.Invoices), w);
        Assert.True(flyout.IsOpen);
    }

    [AvaloniaFact]
    public void Tree_chevron_click_moves_focus_out_of_a_closing_branch()
    {
        var f = new Files();
        f.Open(true, true);
        var w = Show(f.Tree);
        f.Annex1.Focus(NavigationMethod.Tab);
        Click(ToggleOf(f.Annexes), w);
        Assert.False(f.Annexes.IsExpanded);
        Assert.Same(f.Annexes, Focused(w));
    }

    [AvaloniaFact]
    public void Tree_checks_cascade_and_show_mixed()
    {
        var tree = new TreeView();
        Tree.SetChecks(tree, true);
        var file = Boxed("ملف.pdf");
        var one = Boxed("صورة 1.png");
        var two = Boxed("صورة 2.png");
        var pictures = Boxed("الصور", one, two);
        var docs = Boxed("المستندات", file, pictures);
        var notes = Boxed("ملاحظات.txt");
        tree.Items.Add(docs);
        tree.Items.Add(notes);
        docs.IsExpanded = true;
        pictures.IsExpanded = true;
        var w = Show(tree);
        Click(BoxIn(docs), w);
        Assert.True(BoxIn(file).IsChecked);
        Assert.True(BoxIn(pictures).IsChecked);
        Assert.True(BoxIn(one).IsChecked);
        Assert.True(BoxIn(two).IsChecked);
        Click(BoxIn(file), w);
        Assert.False(BoxIn(file).IsChecked);
        Assert.Null(BoxIn(docs).IsChecked);
        var ticked = Tree.CheckedItems(tree);
        Assert.Equal(3, ticked.Count);
        Assert.Contains(pictures, ticked);
        Assert.Contains(one, ticked);
        Assert.Contains(two, ticked);
        Click(RowOf(notes), w);
        Assert.Null(tree.SelectedItem);
    }

    private static TreeViewItem Boxed(string text, params TreeViewItem[] kids)
    {
        var item = new TreeViewItem { Header = new CheckBox { Content = text } };
        foreach (var k in kids) item.Items.Add(k);
        return item;
    }

    private static CheckBox BoxIn(TreeViewItem item) => Assert.IsType<CheckBox>(item.Header);

    [AvaloniaFact]
    public void Tree_branch_entrance_leaves_no_local_transform()
    {
        var f = new Files();
        var w = Show(f.Tree);
        f.Contracts.IsExpanded = true;
        Settle(60);
        var branch = Part<Border>(f.Contracts, "PART_Branch");
        if (branch.RenderTransform is not null) Assert.IsNotType<TransformGroup>(branch.RenderTransform);
        Pump(w);
        Assert.False(branch.IsSet(Visual.RenderTransformProperty));
        Assert.Equal(1, branch.Opacity);
        Assert.NotNull(w);
    }

    [AvaloniaFact]
    public void Tree_bound_through_items_source_has_the_same_keys()
    {
        var tree = new TreeView
        {
            ItemsSource = new[]
            {
                new Node("العقود", [new("عقد.pdf", []), new("الملاحق", [new("ملحق 1.pdf", [])])]),
                new Node("ملاحظات.txt", []),
            },
            ItemTemplate = new FuncTreeDataTemplate<Node>((n, _) => new TextBlock { Text = n.Name }, n => n.Kids),
        };
        var w = Show(tree);
        var activated = new List<object?>();
        Tree.AddActivatedHandler(tree, (_, e) => activated.Add(e.Source));
        var first = Assert.IsType<TreeViewItem>(tree.ContainerFromIndex(0));
        first.Focus(NavigationMethod.Tab);
        Press(w, Key.Right);
        Assert.True(first.IsExpanded);
        Press(w, Key.Right);
        Assert.Equal("عقد.pdf", Tree.LabelOf(Assert.IsType<TreeViewItem>(Focused(w))));
        Press(w, Key.M, RawInputModifiers.None, "م");
        var last = Assert.IsType<TreeViewItem>(Focused(w));
        Assert.Equal("ملاحظات.txt", Tree.LabelOf(last));
        Press(w, Key.Enter);
        Assert.Single(activated);
        Assert.Same(last, activated[0]);
    }

    [AvaloniaFact]
    public void Tree_reveal_opens_the_path_and_selects()
    {
        var f = new Files();
        Show(f.Tree);
        Tree.Reveal(f.Annex1);
        Settle();
        Assert.True(f.Contracts.IsExpanded);
        Assert.True(f.Annexes.IsExpanded);
        Assert.True(f.Annex1.IsSelected);
        Assert.NotNull(Part<ScrollViewer>(f.Tree, "PART_ScrollViewer"));
    }

    // ------------------------------------------------------------ timeline

    private static Timeline Hero()
    {
        var tl = new Timeline();
        tl.Items.Add(new TimelineDay { Content = "24 سبتمبر" });
        tl.Items.Add(new TimelineItem { Classes = { "ok" }, Header = "استُلم الطلب", Time = "8:02 ص" });
        tl.Items.Add(new TimelineItem { Classes = { "ok" }, Header = "تم الدفع", Time = "8:05 ص", Content = "نص الحدث." });
        tl.Items.Add(new TimelineItem { Header = "قيد التجهيز", IsCurrent = true });
        tl.Items.Add(new TimelineItem { Header = "التسليم", IsPending = true });
        return tl;
    }

    private static TimelineItem EventAt(Timeline tl, int i) => Assert.IsType<TimelineItem>(tl.Items[i]);

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Timeline_matches_the_web_marks(string theme, string dir)
    {
        var tl = Hero();
        var w = Show(tl, Dir(dir), Theme(theme));
        Assert.False(tl.ClipToBounds);
        var ok = EventAt(tl, 1);
        var paid = EventAt(tl, 2);
        var current = EventAt(tl, 3);
        var pending = EventAt(tl, 4);
        var okDot = Part<Ellipse>(ok, "PART_Dot");
        AssertBrush(okDot, "InsToneOk", () => okDot.Fill);
        var currentDot = Part<Ellipse>(current, "PART_Dot");
        AssertBrush(currentDot, "InsToneBrandSoft", () => currentDot.Fill);
        Assert.True(Part<Ellipse>(current, "PART_Halo").IsVisible);
        Assert.False(Part<Ellipse>(ok, "PART_Halo").IsVisible);
        var ring = Part<Ellipse>(ok, "PART_Ring");
        AssertBrush(ring, "InsTimelineRing", () => ring.Fill);
        var pendingDot = Part<Ellipse>(pending, "PART_Dot");
        AssertBrush(pendingDot, "InsCtlBg", () => pendingDot.Fill);
        AssertBrush(pendingDot, "InsCtlBd", () => pendingDot.Stroke);
        Assert.Equal(2, pendingDot.StrokeThickness);
        Assert.IsType<LinearGradientBrush>(Part<Border>(current, "PART_Rail").Background);
        Assert.IsType<LinearGradientBrush>(Part<Border>(pending, "PART_Rail").Background);
        var marker = Part<Panel>(ok, "PART_Marker");
        Assert.Equal(12, marker.Bounds.Width);
        Assert.Equal(12, marker.Bounds.Height);
        Assert.Equal(new Thickness(5, 20, 0, 0), Part<Border>(ok, "PART_Rail").Margin);
        Assert.Equal(new Thickness(28, 0, 0, 22), Part<StackPanel>(ok, "PART_Text").Margin);
        Assert.Equal(new Thickness(28, 0, 0, 0), Part<StackPanel>(pending, "PART_Text").Margin);
        Assert.False(Part<Border>(pending, "PART_Rail").IsVisible);
        var time = Part<TextBlock>(ok, "PART_Time");
        Assert.Equal(12, time.FontSize);
        AssertBrush(time, "InsText3", () => time.Foreground);
        var body = Part<ContentPresenter>(paid, "PART_ContentPresenter");
        Assert.Equal(13, body.FontSize);
        AssertBrush(body, "InsText2", () => body.Foreground);
        Assert.Equal(22.1, body.LineHeight);
        var title = Part<ContentPresenter>(ok, "PART_HeaderPresenter");
        if (dir == "rtl") Assert.Equal(23.8, title.LineHeight);
        else Assert.True(double.IsNaN(title.LineHeight));
        Capture(w, $"disclosure-timeline-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void Timeline_compact_and_split()
    {
        var compact = Hero();
        compact.Classes.Add("compact");
        Show(compact);
        var item = EventAt(compact, 1);
        var marker = Part<Panel>(item, "PART_Marker");
        Assert.Equal(9, marker.Bounds.Width);
        Assert.Equal(9, marker.Bounds.Height);
        Assert.Equal(new Thickness(3.5, 17, 0, 0), Part<Border>(item, "PART_Rail").Margin);
        Assert.Equal(new Thickness(20, 0, 0, 14), Part<StackPanel>(item, "PART_Text").Margin);

        var wide = Hero();
        wide.Classes.Add("split");
        Show(wide, width: 800);
        var panel = Assert.IsAssignableFrom<Panel>(wide.ItemsPanelRoot);
        var half = panel.Bounds.Width / 2;
        var events = Enumerable.Range(1, 4).Select(i => EventAt(wide, i)).ToList();
        for (var i = 0; i < events.Count; i++) Assert.Equal(i % 2 == 0 ? half : 0, events[i].Bounds.X);
        var start = Part<ContentPresenter>(events[1], "PART_HeaderPresenter");
        Assert.Equal(TextAlignment.Right, start.TextAlignment);

        var narrow = Hero();
        narrow.Classes.Add("split");
        Show(narrow, width: 500);
        foreach (var e in Enumerable.Range(1, 4).Select(i => EventAt(narrow, i))) Assert.Equal(0, e.Bounds.X);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Timeline_relative_and_date_times(string dir)
    {
        var rtl = dir == "rtl";
        var before = TimelineItem.Now;
        TimelineItem.Now = () => new DateTime(2026, 9, 25, 9, 0, 0);
        try
        {
            var five = new TimelineItem { Header = "أ", TimeMode = TimeMode.Relative };
            var yesterday = new TimelineItem { Header = "ب", TimeMode = TimeMode.Relative };
            var date = new TimelineItem { Header = "ج", TimeMode = TimeMode.Date };
            var tl = new Timeline();
            tl.Items.Add(five);
            tl.Items.Add(yesterday);
            tl.Items.Add(date);
            Show(tl, Dir(dir));
            var now = TimelineItem.Now();
            five.At = now.AddMinutes(-5);
            yesterday.At = now.AddHours(-26);
            var day = new DateTime(2026, 9, 24, 8, 2, 0);
            date.At = day;
            Settle(100);
            Assert.Equal(rtl ? "قبل 5 دقائق" : "5 minutes ago", five.TimeText);
            Assert.Equal(rtl ? "أمس" : "yesterday", yesterday.TimeText);
            Assert.Equal(rtl ? "24 سبتمبر" : "September 24", date.TimeText);
            Assert.Equal(RelativeTime.FullDate(day, rtl, true), ToolTip.GetTip(date) as string);
        }
        finally
        {
            TimelineItem.Now = before;
        }
    }
}
