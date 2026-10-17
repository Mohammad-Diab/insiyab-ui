using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The menu, bar, dialog, file and table bits by look classes: every state pictured and dumped for the before/after comparison, runtime class changes, overrides, and which elements take a theme.
public class MenuLookTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z");

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 16, Width = 440, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(children);
        return page;
    }

    private static T First<T>(Visual root, string cls) where T : Control => root.GetVisualDescendants().OfType<T>().First(c => c.Classes.Contains(cls));

    private static Border Kbd(string key) => new() { Classes = { "kbd" }, Child = new TextBlock { Text = key } };

    private static Style App<T>(string cls, AvaloniaProperty property, object value) where T : Control =>
        new(x => x.OfType<T>().Class(cls)) { Setters = { new Setter(property, value) } };

    private static void Press(Window w, Key key)
    {
        w.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        w.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(200);
    }

    // The palette's own parts are typed, so the dumps leave the type out and read the same before and after.
    private static readonly Regex Typed = new(@"^Palette(Border|Text) \[", RegexOptions.Multiline);

    private static string Normal(string dump) => Typed.Replace(dump, m => (m.Groups[1].Value == "Border" ? "Border" : "TextBlock") + " [");

    private static void Dump(Visual root, string name) => File.WriteAllText(System.IO.Path.Combine(ShotsDir(), name + ".txt"), Normal(LookCheck.Dump(root)));

    private static void Keep(Window w, Visual root, string name)
    {
        Capture(w, name);
        Dump(root, name);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Kbd_and_pop_text_keep_their_look(string theme, string dir)
    {
        var combo = Row(Kbd("Ctrl"), new TextBlock { Text = "+", VerticalAlignment = VerticalAlignment.Center }, Kbd("K"));
        var arrows = Row(Kbd("↑"), Kbd("↓"), Kbd("Enter"), Kbd("Esc"));
        var texts = new StackPanel
        {
            Spacing = 4,
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children =
            {
                new TextBlock { Classes = { "pop-label" }, Text = "لون الهوية" },
                new TextBlock { Classes = { "pop-title" }, Text = "عنوان طويل قليلًا يلتف على سطر ثان داخل البطاقة" },
                new TextBlock { Classes = { "pop-text" }, Text = "نص حر قصير يشرح ما في البطاقة ويلتف على أكثر من سطر لنرى ارتفاع السطر." },
            },
        };
        var page = Page(combo, arrows, texts);
        var w = Show(page, Dir(dir), Variant(theme), 520, 460);
        Keep(w, page, $"menu-kbd-pop-text-{theme}-{dir}");
        var key = (Border)combo.Children[0];
        Assert.Equal(21, key.MinWidth);
        Assert.Equal(new Thickness(1, 1, 1, 2), key.BorderThickness);
        Assert.Equal(FlowDirection.LeftToRight, key.FlowDirection);
        Assert.Equal(HorizontalAlignment.Left, key.HorizontalAlignment);
        AssertBrush(key, "InsCtlBg", () => key.Background);
        var text = (TextBlock)key.Child!;
        Assert.Equal(FontWeight.Medium, text.FontWeight);
        Assert.Equal(19.2, text.LineHeight, 2);
        Assert.Equal(TextAlignment.Center, text.TextAlignment);
        AssertBrush(text, "InsText1", () => text.Foreground);
        var label = First<TextBlock>(texts, "pop-label");
        Assert.Equal(new Thickness(12, 8, 12, 4), label.Padding);
        Assert.Equal(FontWeight.Bold, label.FontWeight);
        var title = First<TextBlock>(texts, "pop-title");
        Assert.Equal(new Thickness(0, 0, 0, 4), title.Margin);
        Assert.Equal(TextWrapping.Wrap, title.TextWrapping);
        var body = First<TextBlock>(texts, "pop-text");
        Assert.Equal(22, body.LineHeight, 2);
        AssertBrush(body, "InsLabel", () => body.Foreground);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Menu_and_card_surfaces_keep_their_look(string theme, string dir)
    {
        var menu = new PopMenu
        {
            Items =
            {
                new MenuItem { Header = "العنوان", Classes = { "label" } },
                new MenuItem { Header = "تعديل", InputGesture = new KeyGesture(Key.E) },
                new MenuItem { Header = "نسخ" },
            },
        };
        var card = new PopCard
        {
            Content = new StackPanel
            {
                Width = 240,
                Children =
                {
                    new TextBlock { Classes = { "pop-label" }, Text = "لون الهوية" },
                    new TextBlock { Classes = { "pop-title" }, Text = "العنوان" },
                    new TextBlock { Classes = { "pop-text" }, Text = "نص حر قصير." },
                },
            },
        };
        var menuTrigger = new Button { Content = "إجراءات", Flyout = menu };
        var cardTrigger = new Button { Content = "بطاقة", Flyout = card };
        var w = Show(new StackPanel { Margin = new Thickness(40), Spacing = 12, HorizontalAlignment = HorizontalAlignment.Left, Children = { menuTrigger, cardTrigger } }, Dir(dir), Variant(theme), 700, 500);
        Click(menuTrigger, w);
        var presenter = Assert.IsType<MenuFlyoutPresenter>(menu.Popup.Child);
        Keep(w, presenter, $"menu-pop-menu-{theme}-{dir}");
        var kbd = Part<Border>(presenter.GetVisualDescendants().OfType<MenuItem>().ElementAt(1), "PART_Kbd");
        Assert.True(kbd.IsVisible);
        Assert.Contains("kbd", kbd.Classes);
        Click(menuTrigger, w);
        Click(cardTrigger, w);
        var body = Assert.IsType<FlyoutPresenter>(card.Popup.Child);
        Keep(w, body, $"menu-pop-card-{theme}-{dir}");
        Assert.Equal(FontWeight.Bold, First<TextBlock>(body, "pop-title").FontWeight);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Tags_list_keeps_its_look(string theme, string dir)
    {
        var tags = new TagsInput
        {
            PlaceholderText = "اختر",
            Width = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
            Options = { new TagsOption("a", "الأول"), new TagsOption("b", "الثاني"), new TagsOption("c", "الثالث"), new TagsOption("d", "الرابع") },
        };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { tags } }, Dir(dir), Variant(theme), 520, 440);
        tags.InputBox!.Focus();
        Settle();
        Visual layer = w;
        List<Border> Rows() => w.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("tags-option")).ToList();
        Keep(w, layer, $"menu-tags-open-{theme}-{dir}");
        Assert.Equal(4, Rows().Count);
        var list = First<Border>(layer, "combo-list");
        Assert.Equal(288, list.MaxHeight);
        Assert.Equal(new Thickness(1), list.BorderThickness);
        var first = Rows()[0];
        Assert.Equal(new Thickness(12, 8), first.Padding);
        AssertBrush(first, "InsPopItemRest", () => first.Background);
        var firstText = (TextBlock)first.Child!;
        Assert.Equal((double)Res(firstText, "InsFsMd"), firstText.FontSize);
        AssertBrush(firstText, "InsText1", () => firstText.Foreground);

        Press(w, Key.Down);
        Keep(w, layer, $"menu-tags-active-{theme}-{dir}");
        var active = Rows().Single(r => r.Classes.Contains("active"));
        AssertBrush(active, "InsPopItemHover", () => active.Background);
        AssertBrush(active, "InsPopItemHoverFg", () => ((TextBlock)active.Child!).Foreground);
        Assert.Equal(3, active.RenderTransform!.Value.M31, 1);

        Motion.SetStill(w, true);
        Settle(300);
        Keep(w, layer, $"menu-tags-still-{theme}-{dir}");
        Assert.True(active.RenderTransform is null || active.RenderTransform.Value.IsIdentity);
        Motion.SetStill(w, false);
        Settle(300);

        w.KeyTextInput("زز");
        Settle(300);
        Keep(w, layer, $"menu-tags-empty-{theme}-{dir}");
        var empty = First<TextBlock>(layer, "combo-empty");
        Assert.True(empty.IsVisible);
        Assert.Equal(new Thickness(12, 8), empty.Padding);
        AssertBrush(empty, "InsLabel", () => empty.Foreground);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Palette_keeps_its_look(string theme, string dir)
    {
        var palette = new CommandPalette
        {
            Items =
            {
                new PaletteItem { Label = "الأزرار", Group = "المكوّنات", Hint = "Ctrl+B", Icon = Glyph, IsCurrent = true },
                new PaletteItem { Label = "الجداول", Group = "المكوّنات" },
                new PaletteItem { Label = "الألوان", Group = "الأسس" },
            },
        };
        var w = Show(new Panel(), Dir(dir), Variant(theme), 800, 640);
        palette.Open(w);
        Settle();
        Keep(w, palette, $"menu-palette-{theme}-{dir}");
        var foot = First<Border>(palette, "palette-foot");
        Assert.Equal(new Thickness(16, 10), foot.Padding);
        Assert.Equal(new Thickness(0, 1, 0, 0), foot.BorderThickness);
        AssertBrush(foot, "InsLine", () => foot.BorderBrush);
        var arrow = First<TextBlock>(palette, "arrow");
        Assert.Equal("Segoe UI Symbol", arrow.FontFamily.FamilyNames[0]);
        AssertBrush(arrow, "InsText1", () => arrow.Foreground);
        Assert.Equal(FontWeight.Medium, arrow.FontWeight);
        Assert.Equal(VerticalAlignment.Center, arrow.VerticalAlignment);
        var footKbds = foot.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("kbd")).ToList();
        Assert.Equal(4, footKbds.Count);
        Assert.All(footKbds, k => Assert.Equal(new Thickness(1, 1, 1, 2), k.BorderThickness));
        var enter = footKbds[2].GetVisualDescendants().OfType<TextBlock>().Single();
        AssertBrush(enter, "InsText1", () => enter.Foreground);
        var move = foot.GetVisualDescendants().OfType<TextBlock>().First(t => t.Parent is StackPanel);
        AssertBrush(move, "InsText3", () => move.Foreground);
        Assert.Equal((double)Res(move, "InsFsXs"), move.FontSize);

        Motion.SetStill(w, true);
        Settle(300);
        Keep(w, palette, $"menu-palette-still-{theme}-{dir}");
        Motion.SetStill(w, false);

        palette.InputBox!.Text = "زززز";
        Settle(300);
        Keep(w, palette, $"menu-palette-empty-{theme}-{dir}");
        var empty = First<TextBlock>(palette, "palette-empty");
        Assert.True(empty.IsVisible);
        Assert.Equal(new Thickness(16, 28, 16, 32), empty.Padding);
        Assert.Equal(TextAlignment.Center, empty.TextAlignment);
        AssertBrush(empty, "InsLabel", () => empty.Foreground);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Toolbar_labels_and_tip_keys_keep_their_look(string theme, string dir)
    {
        var bar = new Toolbar { HorizontalAlignment = HorizontalAlignment.Left };
        bar.Items.Add(new ToolbarButton { Icon = Glyph, Label = "جديد", Shortcut = "Ctrl+N", ShowLabel = true });
        bar.Items.Add(new ToolbarButton { Icon = Glyph, Label = "فتح" });
        var tip = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { new TextBlock { Text = "جديد" }, new TextBlock { Text = "Ctrl+N", FlowDirection = FlowDirection.LeftToRight, Classes = { "toolbar-key" } } },
        };
        var page = Page(bar, tip);
        var w = Show(page, Dir(dir), Variant(theme), 520, 260);
        Keep(w, page, $"menu-toolbar-{theme}-{dir}");
        var label = First<TextBlock>(bar, "toolbar-label");
        Assert.True(label.IsVisible);
        Assert.Equal(VerticalAlignment.Center, label.VerticalAlignment);
        var key = First<TextBlock>(tip, "toolbar-key");
        Assert.Equal(0.7, key.Opacity, 2);
        Assert.Equal(Res(key, "InsFontBodyEn"), key.FontFamily);

        var first = (Control)bar.Items[0];
        w.MouseMove(Center(first, w));
        Settle(400);
        Keep(w, page, $"menu-toolbar-hover-{theme}-{dir}");
        Motion.SetStill(w, true);
        Settle(300);
        Keep(w, page, $"menu-toolbar-still-{theme}-{dir}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Dialog_title_and_scrim_keep_their_look(string theme, string dir)
    {
        var button = new Button { Content = "حذف", Classes = { "ghost-danger" } };
        Dialog.SetConfirm(button, "حذف الملف؟");
        var w = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, Dir(dir), Variant(theme), 900, 600);
        Click(button, w);
        Keep(w, w, $"menu-dialog-{theme}-{dir}");
        var title = First<TextBlock>(w, "dialog-title");
        Assert.Equal(17, title.FontSize);
        Assert.Equal(FontWeight.ExtraBold, title.FontWeight);
        Assert.Equal(TextWrapping.Wrap, title.TextWrapping);
        var scrim = First<Border>(w, "dialog-scrim");
        AssertBrush(scrim, "InsDialogScrim", () => scrim.Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void File_issue_table_empty_state_and_marker_keep_their_look(string theme, string dir)
    {
        var upload = new FileUpload { Multiple = true, Accept = ".pdf,image/*", MaxSize = "5MB", MaxCount = 5 };
        var table = new Table
        {
            Empty = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 4,
                Children =
                {
                    new TextBlock { Classes = { "empty-title" }, Text = "لا بيانات" },
                    new TextBlock { Classes = { "empty-hint" }, Text = "ستظهر الصفوف هنا حين تُضاف." },
                },
            },
        };
        table.Columns.Add(new TableColumn { Header = "الاسم" });
        var flight = new Canvas { Width = 120, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, Children = { new Border { Classes = { "table-marker-flight" }, Height = 24 } } };
        var page = Page(upload, table, flight);
        var w = Show(page, Dir(dir), Variant(theme), 520, 620);
        upload.AddFiles([new FileEntry("a.exe", 100)]);
        Settle(300);
        Keep(w, page, $"menu-file-table-{theme}-{dir}");
        var issue = First<TextBlock>(upload, "file-issue");
        Assert.Equal((double)Res(issue, "InsFsSm"), issue.FontSize);
        AssertBrush(issue, "InsToneBad", () => issue.Foreground);
        var emptyTitle = First<TextBlock>(table, "empty-title");
        Assert.Equal(15, emptyTitle.FontSize);
        Assert.Equal(FontWeight.Bold, emptyTitle.FontWeight);
        Assert.Equal(HorizontalAlignment.Center, emptyTitle.HorizontalAlignment);
        var emptyHint = First<TextBlock>(table, "empty-hint");
        Assert.Equal(13, emptyHint.FontSize);
        Assert.Equal(TextAlignment.Center, emptyHint.TextAlignment);
        Assert.Equal(TextWrapping.Wrap, emptyHint.TextWrapping);
        AssertBrush(emptyHint, "InsText2", () => emptyHint.Foreground);
        var bar = First<Border>(flight, "table-marker-flight");
        Assert.Equal(3, bar.Width);
        AssertBrush(bar, "InsPrimaryMid", () => bar.Background);
    }

    [AvaloniaFact]
    public void Menu_classes_added_at_runtime_take_their_look()
    {
        var kbdText = new TextBlock { Text = "K" };
        var kbd = new Border { Child = kbdText };
        var optionText = new TextBlock { Text = "خيار" };
        var option = new Border { Child = optionText };
        var liveText = new TextBlock { Text = "خيار" };
        var live = new Border { Child = liveText };
        var list = new Border();
        var empty = new TextBlock { Text = "لا نتائج" };
        var label = new TextBlock { Text = "عنوان" };
        var title = new TextBlock { Text = "عنوان" };
        var text = new TextBlock { Text = "نص" };
        var key = new TextBlock { Text = "Ctrl+N" };
        var toolLabel = new TextBlock { Text = "جديد" };
        var scrim = new Border { Height = 20 };
        var issue = new TextBlock { Text = "خطأ" };
        var flight = new Border { Height = 20 };
        var emptyTitle = new TextBlock { Text = "لا بيانات" };
        var emptyHint = new TextBlock { Text = "ستظهر الصفوف هنا." };
        var table = new Table { Empty = new StackPanel { Children = { emptyTitle, emptyHint } } };
        table.Columns.Add(new TableColumn { Header = "الاسم" });
        var page = new StackPanel { Children = { kbd, option, live, list, empty, label, title, text, key, toolLabel, scrim, issue, flight, table } };
        Show(page, FlowDirection.RightToLeft, null, 600, 900);
        Assert.Equal(0, kbd.MinWidth);
        Assert.Equal(new Thickness(0), option.Padding);
        Assert.True(double.IsInfinity(list.MaxHeight));
        Assert.Equal(new Thickness(0), empty.Padding);
        Assert.Equal(FontWeight.Normal, label.FontWeight);
        Assert.Equal(TextWrapping.NoWrap, title.TextWrapping);
        Assert.True(double.IsNaN(text.LineHeight));
        Assert.Equal(1, key.Opacity);
        Assert.Equal(VerticalAlignment.Stretch, toolLabel.VerticalAlignment);
        Assert.Null(scrim.Background);
        Assert.Equal(FontWeight.Normal, issue.FontWeight);
        Assert.True(double.IsNaN(flight.Width));
        Assert.Equal(HorizontalAlignment.Stretch, emptyTitle.HorizontalAlignment);
        Assert.Equal(TextAlignment.Start, emptyHint.TextAlignment);

        kbd.Classes.Add("kbd");
        option.Classes.Add("tags-option");
        list.Classes.Add("combo-list");
        empty.Classes.Add("combo-empty");
        label.Classes.Add("pop-label");
        title.Classes.Add("pop-title");
        text.Classes.Add("pop-text");
        key.Classes.Add("toolbar-key");
        toolLabel.Classes.Add("toolbar-label");
        scrim.Classes.Add("dialog-scrim");
        issue.Classes.Add("file-issue");
        flight.Classes.Add("table-marker-flight");
        emptyTitle.Classes.Add("empty-title");
        emptyHint.Classes.Add("empty-hint");
        Settle(300);
        Assert.Equal(21, kbd.MinWidth);
        Assert.Equal(new Thickness(1, 1, 1, 2), kbd.BorderThickness);
        Assert.Equal(HorizontalAlignment.Left, kbd.HorizontalAlignment);
        Assert.Equal(FlowDirection.LeftToRight, kbd.FlowDirection);
        Assert.Equal(FontWeight.Medium, kbdText.FontWeight);
        Assert.Equal(19.2, kbdText.LineHeight, 2);
        Assert.Equal(TextAlignment.Center, kbdText.TextAlignment);
        Assert.Equal(new Thickness(12, 8), option.Padding);
        Assert.Equal(new Cursor(StandardCursorType.Hand).ToString(), option.Cursor?.ToString());
        AssertBrush(option, "InsPopItemRest", () => option.Background);
        Assert.Equal((double)Res(optionText, "InsFsMd"), optionText.FontSize);
        AssertBrush(optionText, "InsText1", () => optionText.Foreground);
        Assert.Equal(288, list.MaxHeight);
        Assert.Equal(new Thickness(12, 8), empty.Padding);
        Assert.Equal(new Thickness(12, 8, 12, 4), label.Padding);
        Assert.Equal(FontWeight.Bold, label.FontWeight);
        Assert.Equal(TextWrapping.Wrap, title.TextWrapping);
        Assert.Equal(22, text.LineHeight, 2);
        Assert.Equal(0.7, key.Opacity, 2);
        Assert.Equal(VerticalAlignment.Center, toolLabel.VerticalAlignment);
        AssertBrush(scrim, "InsDialogScrim", () => scrim.Background);
        AssertBrush(issue, "InsToneBad", () => issue.Foreground);
        Assert.Equal(3, flight.Width);
        Assert.Equal(15, emptyTitle.FontSize);
        Assert.Equal(TextAlignment.Center, emptyHint.TextAlignment);

        live.Classes.Add("tags-option");
        live.Classes.Add("active");
        Settle(400);
        AssertBrush(live, "InsPopItemHover", () => live.Background);
        AssertBrush(liveText, "InsPopItemHoverFg", () => liveText.Foreground);
        Assert.Equal(3, live.RenderTransform!.Value.M31, 1);
        live.Classes.Remove("active");
        Settle(400);
        AssertBrush(live, "InsPopItemRest", () => live.Background);
        AssertBrush(liveText, "InsText1", () => liveText.Foreground);

        kbd.Classes.Remove("kbd");
        option.Classes.Remove("tags-option");
        list.Classes.Remove("combo-list");
        empty.Classes.Remove("combo-empty");
        label.Classes.Remove("pop-label");
        title.Classes.Remove("pop-title");
        text.Classes.Remove("pop-text");
        key.Classes.Remove("toolbar-key");
        toolLabel.Classes.Remove("toolbar-label");
        scrim.Classes.Remove("dialog-scrim");
        issue.Classes.Remove("file-issue");
        flight.Classes.Remove("table-marker-flight");
        emptyTitle.Classes.Remove("empty-title");
        emptyHint.Classes.Remove("empty-hint");
        Settle(300);
        Assert.Equal(0, kbd.MinWidth);
        Assert.Equal(FontWeight.Normal, kbdText.FontWeight);
        Assert.True(double.IsNaN(kbdText.LineHeight));
        Assert.Equal(new Thickness(0), option.Padding);
        Assert.True(double.IsInfinity(list.MaxHeight));
        Assert.Equal(new Thickness(0), empty.Padding);
        Assert.Equal(FontWeight.Normal, label.FontWeight);
        Assert.Equal(TextWrapping.NoWrap, title.TextWrapping);
        Assert.True(double.IsNaN(text.LineHeight));
        Assert.Equal(1, key.Opacity);
        Assert.Equal(VerticalAlignment.Stretch, toolLabel.VerticalAlignment);
        Assert.Null(scrim.Background);
        Assert.True(double.IsNaN(flight.Width));
        Assert.Equal(HorizontalAlignment.Stretch, emptyTitle.HorizontalAlignment);
        Assert.Equal(TextAlignment.Start, emptyHint.TextAlignment);
    }

    [AvaloniaFact]
    public void App_styles_and_local_values_still_win_over_the_menu_looks()
    {
        var kbd = Kbd("K");
        var option = new Border { Classes = { "tags-option" }, Child = new TextBlock { Text = "خيار" } };
        var title = new TextBlock { Classes = { "pop-title" }, Text = "عنوان" };
        var issue = new TextBlock { Classes = { "file-issue" }, Text = "خطأ" };
        var local = new Border { Classes = { "kbd" }, MinWidth = 40, Child = new TextBlock { Text = "K" } };
        var page = new StackPanel { Children = { kbd, option, title, issue, local } };
        page.Styles.Add(App<Border>("kbd", Border.CornerRadiusProperty, new CornerRadius(3)));
        page.Styles.Add(App<TextBlock>("pop-title", TextBlock.FontSizeProperty, 20.0));
        var appStyle = App<Border>("tags-option", Border.CornerRadiusProperty, new CornerRadius(7));
        var appText = App<TextBlock>("file-issue", TextBlock.FontSizeProperty, 21.0);
        var typeOnly = new Style(x => x.OfType<Border>()) { Setters = { new Setter(Border.BorderThicknessProperty, new Thickness(7)) } };
        Application.Current!.Styles.Add(appStyle);
        Application.Current.Styles.Add(appText);
        Application.Current.Styles.Add(typeOnly);
        try
        {
            Show(page, FlowDirection.RightToLeft, height: 400);
            Assert.Equal(new CornerRadius(3), kbd.CornerRadius);
            Assert.Equal(new CornerRadius(7), option.CornerRadius);
            Assert.Equal(20, title.FontSize);
            Assert.Equal(21, issue.FontSize);
            Assert.Equal(40, local.MinWidth);
            Assert.Equal(new Thickness(1, 1, 1, 2), kbd.BorderThickness);
            Assert.Equal(21, kbd.MinWidth);
        }
        finally
        {
            Application.Current.Styles.Remove(appStyle);
            Application.Current.Styles.Remove(appText);
            Application.Current.Styles.Remove(typeOnly);
        }
        Settle();
        Assert.Equal(Res(option, "InsRXs"), option.CornerRadius);
    }

    [AvaloniaFact]
    public void Only_the_menu_parts_take_a_theme()
    {
        var plain = new Border { Child = new TextBlock { Text = "أ" } };
        var plainText = new TextBlock { Text = "ب" };
        var kbd = Kbd("K");
        var option = new Border { Classes = { "tags-option" }, Child = new TextBlock { Text = "خيار" } };
        var list = new Border { Classes = { "combo-list" } };
        var scrim = new Border { Classes = { "dialog-scrim" } };
        var flight = new Border { Classes = { "table-marker-flight" } };
        var texts = new[] { "combo-empty", "pop-label", "pop-title", "pop-text", "toolbar-label", "toolbar-key", "file-issue", "empty-title", "empty-hint" }
            .Select(c => new TextBlock { Classes = { c }, Text = c }).ToList();
        var page = new StackPanel { Children = { plain, plainText, kbd, option, list, scrim, flight } };
        page.Children.AddRange(texts);
        Show(page, FlowDirection.RightToLeft, height: 700);
        Assert.Null(plain.Theme);
        Assert.Null(((TextBlock)plain.Child!).Theme);
        Assert.Null(plainText.Theme);
        Assert.NotNull(kbd.Theme);
        Assert.NotNull(((TextBlock)kbd.Child!).Theme);
        Assert.NotNull(option.Theme);
        Assert.Null(((TextBlock)option.Child!).Theme);
        Assert.NotNull(list.Theme);
        Assert.NotNull(scrim.Theme);
        Assert.NotNull(flight.Theme);
        Assert.All(texts, t => Assert.NotNull(t.Theme));

        var palette = new CommandPalette { Items = { new PaletteItem { Label = "الأزرار", Group = "المكوّنات", Hint = "Ctrl+B" } } };
        var w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        var foot = First<Border>(palette, "palette-foot");
        Assert.Equal("PaletteBorder", foot.GetType().Name);
        Assert.Equal("PaletteText", First<TextBlock>(palette, "arrow").GetType().Name);
        var kbds = palette.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("kbd")).ToList();
        Assert.Equal(5, kbds.Count);
        Assert.All(kbds, k => Assert.Equal(typeof(Border), k.GetType()));
        Assert.All(kbds, k => Assert.NotNull(k.Theme));
        palette.InputBox!.Text = "زززز";
        Settle(300);
        Assert.Equal("PaletteText", First<TextBlock>(palette, "palette-empty").GetType().Name);
    }

    // Rules of these classes, scoped under a control or not; the still and inactive lines stay until the last rollout step.
    private static List<string> Rules(Type type, string cls)
    {
        var subject = new Regex(@"(?:^|[\s>])" + Regex.Escape(type.Name) + @"(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\." + Regex.Escape(cls) + @"(?![\w-])(?:\.[\w-]+|:[\w-]+(?:\([^)]*\))?|\[[^\]]*\])*\s*$");
        return LookCheck.GlobalSelectors().SelectMany(s => s.Split(", ")).Select(a => a.Trim())
            .Where(a => subject.IsMatch(a) && !a.Contains("ins-still") && !a.Contains("IsInactive")).ToList();
    }

    [AvaloniaFact]
    public void No_global_rule_names_a_menu_class()
    {
        var found = new List<string>();
        foreach (var cls in new[] { "kbd", "tags-option", "combo-list", "dialog-scrim", "table-marker-flight", "palette-foot" })
            found.AddRange(Rules(typeof(Border), cls));
        foreach (var cls in new[] { "combo-empty", "pop-label", "pop-title", "pop-text", "toolbar-label", "toolbar-key", "dialog-title", "file-issue", "empty-title", "empty-hint", "palette-empty", "arrow" })
            found.AddRange(Rules(typeof(TextBlock), cls));
        var ancestor = new Regex(@"(?:^|[\s>])Border\.(?:kbd|tags-option|palette-foot)(?![\w-]).*[\s>]TextBlock(?:[.:\[][^\s>]*)?\s*$");
        found.AddRange(LookCheck.GlobalSelectors().SelectMany(s => s.Split(", ")).Select(a => a.Trim()).Where(a => ancestor.IsMatch(a) && !a.Contains("ins-still") && !a.Contains("IsInactive")));
        found = found.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        Assert.True(found.Count == 0, "found: " + string.Join(" ; ", found));
    }
}
