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

public class MenusTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    private static ThemeVariant Theme(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static void Press(Window w, Key key, RawInputModifiers mods = RawInputModifiers.None, PhysicalKey physical = PhysicalKey.None, string? symbol = null)
    {
        w.KeyPress(key, mods, physical, symbol);
        w.KeyRelease(key, mods, physical, symbol);
        Settle(200);
    }

    private static void Type(Window w, string text)
    {
        w.KeyTextInput(text);
        Settle(200);
    }

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Physical(Control c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static void PressAt(Window w, Point p)
    {
        w.MouseMove(p);
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Settle(300);
    }

    private static (Button trigger, PopMenu menu) MenuButton()
    {
        var menu = new PopMenu
        {
            Items =
            {
                new MenuItem { Header = "العنوان", Classes = { "label" } },
                new MenuItem { Header = "تعديل", InputGesture = new KeyGesture(Key.E) },
                new MenuItem { Header = "نسخ" },
                new MenuItem { Header = "نقل", IsEnabled = false },
                new Separator(),
                new MenuItem { Header = "حذف", Classes = { "bad" } },
            },
        };
        var trigger = new Button { Content = "إجراءات", Classes = { "secondary" }, Flyout = menu, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        return (trigger, menu);
    }

    private static MenuFlyoutPresenter Presenter(PopMenu menu) => Assert.IsType<MenuFlyoutPresenter>(menu.Popup.Child);

    private static Border Box(Control presenter) => Part<Border>(presenter, "LayoutRoot");

    private static List<MenuItem> Items(PopMenu menu) => Presenter(menu).GetVisualDescendants().OfType<MenuItem>().ToList();

    // ------------------------------------------------------------ dropdown

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Menu_opens_from_the_leading_edge_six_pixels_below(string theme, string dir)
    {
        var (trigger, menu) = MenuButton();
        var w = Show(new Panel { Children = { trigger } }, Dir(dir), Theme(theme));
        Click(trigger, w);
        Assert.True(menu.IsOpen);
        Assert.True(menu.Popup.IsUsingOverlayLayer);
        var body = Presenter(menu);
        var tb = Physical(trigger, w);
        var bb = Physical(Box(body), w);
        Assert.InRange(bb.Top - tb.Bottom, 5, 7);
        if (dir == "rtl") Assert.InRange(bb.Right - tb.Right, -1, 1);
        else Assert.InRange(bb.Left - tb.Left, -1, 1);
        Assert.True(bb.Width >= 192);
        AssertBrush(body, "InsGlassStrong", () => Box(body).Background);
        AssertBrush(body, "InsRim", () => Box(body).BorderBrush);
        Assert.Equal(new CornerRadius(14), Box(body).CornerRadius);
        Capture(w, $"menus-dropdown-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void End_aligned_menu_lines_up_with_the_trailing_edge()
    {
        var (trigger, menu) = MenuButton();
        menu.IsEnd = true;
        trigger.Margin = new Thickness(300, 40, 40, 40);
        var w = Show(new Panel { Children = { trigger } });
        Assert.Equal(PlacementMode.BottomEdgeAlignedRight, menu.Placement);
        Click(trigger, w);
        var tb = Physical(trigger, w);
        var bb = Physical(Box(Presenter(menu)), w);
        Assert.InRange(bb.Right - tb.Right, -1, 1);
    }

    [AvaloniaFact]
    public void A_second_click_on_the_trigger_closes_the_menu()
    {
        var (trigger, menu) = MenuButton();
        var w = Show(new Panel { Children = { trigger } });
        Click(trigger, w);
        Assert.True(menu.IsOpen);
        Click(trigger, w);
        Assert.False(menu.IsOpen);
    }

    [AvaloniaFact]
    public void Escape_closes_the_menu()
    {
        var (trigger, menu) = MenuButton();
        var w = Show(new Panel { Children = { trigger } });
        Click(trigger, w);
        Press(w, Key.Escape);
        Assert.False(menu.IsOpen);
    }

    [AvaloniaFact]
    public void Items_carry_the_web_states()
    {
        var (trigger, menu) = MenuButton();
        var w = Show(new Panel { Children = { trigger } });
        Click(trigger, w);
        var items = Items(menu);
        var label = items[0];
        Assert.False(label.Focusable);
        Assert.False(label.IsHitTestVisible);
        AssertBrush(label, "InsLabel", () => label.Foreground);
        var edit = items[1];
        Assert.Equal(new Thickness(12, 8), edit.Padding);
        Assert.Equal(14, edit.FontSize);
        var kbd = Part<Border>(edit, "PART_Kbd");
        Assert.True(kbd.IsVisible);
        Assert.Equal("E", Part<TextBlock>(edit, "PART_InputGestureText").Text);
        Assert.False(Part<Border>(items[2], "PART_Kbd").IsVisible);

        Assert.Equal(0.45, items[3].Opacity, 2);
        AssertBrush(items[4], "InsToneBad", () => items[4].Foreground);
        w.MouseMove(Center(edit, w));
        Settle();
        Assert.True(edit.IsSelected);
        var hover = (ISolidColorBrush)Res(edit, "InsPopItemHover");
        var bg = ColorOf(edit.Background);
        Assert.Equal(hover.Color.R, bg.R);
        Assert.Equal(hover.Opacity, ((ISolidColorBrush)edit.Background!).Opacity, 2);
        AssertBrush(edit, "InsPopItemHoverFg", () => edit.Foreground);
    }

    [AvaloniaFact]
    public void Separator_is_a_hairline_in_the_line_colour()
    {
        var (trigger, menu) = MenuButton();
        var w = Show(new Panel { Children = { trigger } });
        Click(trigger, w);
        var sep = Presenter(menu).GetVisualDescendants().OfType<Separator>().Single();
        Assert.Equal(1, sep.Bounds.Height);
        Assert.Equal(new Thickness(0, 4), sep.Margin);
        AssertBrush(sep, "InsLine", () => sep.Background);
    }

    [AvaloniaFact]
    public void Checkbox_items_flip_and_keep_the_menu_open_radio_items_move_the_check_and_close()
    {
        var name = new MenuItem { Header = "الاسم", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true };
        var date = new MenuItem { Header = "التاريخ", ToggleType = MenuItemToggleType.CheckBox };
        var menu = new PopMenu { Items = { name, date } };
        var trigger = new Button { Content = "إجراءات", Flyout = menu, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        var w = Show(new Panel { Children = { trigger } });
        Click(trigger, w);
        Assert.True(Part<Panel>(date, "PART_ToggleIconPresenter").IsVisible);
        Assert.Equal(1, Part<Viewbox>(name, "PART_Mark").Opacity, 2);
        Assert.Equal(0, Part<Viewbox>(date, "PART_Mark").Opacity, 2);
        Click(date, w);
        Assert.True(date.IsChecked);
        Assert.True(menu.IsOpen);

        var a = new MenuItem { Header = "الاسم", ToggleType = MenuItemToggleType.Radio, GroupName = "g", IsChecked = true };
        var b = new MenuItem { Header = "التاريخ", ToggleType = MenuItemToggleType.Radio, GroupName = "g" };
        var radios = new PopMenu { Items = { a, b } };
        var trigger2 = new Button { Content = "ترتيب", Flyout = radios, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right };
        w.Content = new Panel { Children = { trigger2 } };
        Settle();
        Click(trigger2, w);
        Assert.False(Part<global::Avalonia.Controls.Shapes.Path>(b, "PART_Check").IsVisible);
        Assert.True(Part<global::Avalonia.Controls.Shapes.Ellipse>(b, "PART_Radio").IsVisible);
        Click(b, w);
        Assert.True(b.IsChecked);
        Assert.False(a.IsChecked);
        Assert.False(radios.IsOpen);
    }

    [AvaloniaFact]
    public void Arrow_keys_move_between_items_and_skip_the_disabled_and_the_label()
    {
        var (trigger, menu) = MenuButton();
        var w = Show(new Panel { Children = { trigger } });
        trigger.Focus();
        Press(w, Key.Enter);
        Assert.True(menu.IsOpen);
        var items = Items(menu);
        Assert.Equal("تعديل", items.First(i => i.IsSelected).Header);
        Press(w, Key.Down);
        Assert.Equal("نسخ", items.First(i => i.IsSelected).Header);
        Press(w, Key.Down);
        Assert.Equal("حذف", items.First(i => i.IsSelected).Header);
    }

    [AvaloniaFact]
    public void Card_is_a_wide_padded_flyout()
    {
        var card = new PopCard { Content = new StackPanel { Children = { new TextBlock { Text = "العنوان", Classes = { "pop-title" } }, new TextBlock { Text = "نص حر قصير.", Classes = { "pop-text" } } } } };
        var trigger = new Button { Content = "التفاصيل", Flyout = card, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        var w = Show(new Panel { Children = { trigger } }, FlowDirection.RightToLeft);
        Click(trigger, w);
        var body = Assert.IsType<FlyoutPresenter>(card.Popup.Child);
        Assert.Contains("card", body.Classes);
        Assert.Equal(320, Box(body).Bounds.Width, 1);
        Assert.Equal(new Thickness(16), body.Padding);
        var title = body.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.Equal(FontWeight.Bold, title.FontWeight);
        Capture(w, "menus-card-rtl");
    }

    [AvaloniaFact]
    public void Context_menu_and_menu_bar_share_the_look()
    {
        var context = new ContextMenu { Items = { new MenuItem { Header = "نسخ" } } };
        var target = new Border { Width = 100, Height = 40, Background = Brushes.Transparent, ContextMenu = context };
        var bar = new Menu { Items = { new MenuItem { Header = "ملف", Items = { new MenuItem { Header = "جديد" } } } } };
        var w = Show(new StackPanel { Children = { bar, target } });
        context.Open(target);
        Settle();
        AssertBrush(context, "InsGlassStrong", () => context.Background);
        Assert.Equal(new CornerRadius(14), context.CornerRadius);
        context.Close();
        var top = bar.GetVisualDescendants().OfType<MenuItem>().First();
        Assert.Equal(new Thickness(12, 6), top.Padding);
    }

    // ------------------------------------------------------------ tooltip

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Tooltip_is_the_dark_bubble_above_its_control(string theme, string dir)
    {
        var tip = new ToolTip { Content = "تعديل", Opacity = 1, RenderTransform = null };
        var host = new Button { Content = "حذف", Classes = { "secondary" } };
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { tip, host } }, Dir(dir), Theme(theme));
        AssertBrush(tip, "InsTipBg", () => tip.Background);
        AssertBrush(tip, "InsTipFg", () => tip.Foreground);
        Assert.Equal(new Thickness(10, 6), tip.Padding);
        Assert.Equal(new CornerRadius(10), tip.CornerRadius);
        Assert.Equal(FontWeight.SemiBold, tip.FontWeight);
        Assert.Equal(new Thickness(8), Part<Border>(tip, "PART_Room").Padding);
        Assert.Equal(PlacementMode.Top, ToolTip.GetPlacement(host));
        Assert.Equal(0, ToolTip.GetVerticalOffset(host));
        Assert.Equal(400, ToolTip.GetShowDelay(host));
        Assert.Equal(0, ToolTip.GetBetweenShowDelay(host));
        Capture(w, $"menus-tip-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void Tooltip_shows_above_and_centred_once_open()
    {
        var host = new Button { Content = "حذف", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(host, "تعديل");
        var w = Show(new Panel { Children = { host } }, FlowDirection.RightToLeft);
        ToolTip.SetIsOpen(host, true);
        Settle();
        var tipControl = w.GetVisualDescendants().OfType<ToolTip>().Single();
        Assert.Equal(1, tipControl.Opacity, 2);
        var tip = Part<Border>(tipControl, "PART_LayoutRoot");
        var t = Physical(tip, w);
        var h = Physical(host, w);
        Assert.InRange(h.Top - t.Bottom, 7, 9.5);
        Assert.InRange(Math.Abs(h.Center.X - t.Center.X), 0, 1);
        Capture(w, "menus-tip-open-rtl");
    }

    [AvaloniaFact]
    public void Tooltip_opens_at_once_on_keyboard_focus_and_closes_on_blur_and_escape()
    {
        var first = new Button { Content = "أ" };
        var host = new Button { Content = "ب" };
        ToolTip.SetTip(host, "تعديل");
        var w = Show(new StackPanel { Children = { first, host } });
        first.Focus();
        Press(w, Key.Tab);
        Assert.True(host.IsFocused);
        Assert.True(ToolTip.GetIsOpen(host));
        Press(w, Key.Escape);
        Assert.False(ToolTip.GetIsOpen(host));
        first.Focus();
        Press(w, Key.Tab);
        Assert.True(ToolTip.GetIsOpen(host));
        first.Focus();
        Settle(200);
        Assert.False(ToolTip.GetIsOpen(host));
    }

    // ------------------------------------------------------------ chips

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Chips_render_in_every_look(string theme, string dir)
    {
        var row = new WrapPanel { Classes = { "chips" }, Margin = new Thickness(20) };
        row.Children.Add(new Chip { Content = "الرياض", IsRemovable = true });
        row.Children.Add(new ToggleButton { Content = "الأول", IsChecked = true, Classes = { "chip" } });
        row.Children.Add(new ToggleButton { Content = "الثاني", Classes = { "chip" } });
        row.Children.Add(new Button { Content = "رابط", Classes = { "chip", "active" } });
        var w = Show(row, Dir(dir), Theme(theme));
        Assert.Equal(6, row.ItemSpacing);
        Capture(w, $"menus-chips-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void Chip_has_the_pill_look_and_a_named_remove_button()
    {
        var chip = new Chip { Content = "الرياض", IsRemovable = true };
        var plain = new Chip { Content = "عمّان" };
        var w = Show(new StackPanel { Children = { chip, plain } });
        Assert.Equal(30, chip.Bounds.Height, 0);
        AssertBrush(chip, "InsSurface", () => chip.Background);
        AssertBrush(chip, "InsCtlBd", () => chip.BorderBrush);
        Assert.Equal(new CornerRadius(999), chip.CornerRadius);
        Assert.Equal(new Thickness(12, 3, 4, 3), chip.Padding);
        Assert.Equal(new Thickness(12, 3), plain.Padding);
        var x = Part<Button>(chip, "PART_Remove");
        Assert.True(x.IsVisible);
        Assert.Equal(22, x.Bounds.Width);
        Assert.Equal("إزالة الرياض", global::Avalonia.Automation.AutomationProperties.GetName(x));
        Assert.False(Part<Button>(plain, "PART_Remove").IsVisible);
    }

    [AvaloniaFact]
    public void Removing_a_chip_can_be_cancelled_and_otherwise_takes_it_away()
    {
        var keep = new Chip { Content = "الرياض", IsRemovable = true };
        var go = new Chip { Content = "عمّان", IsRemovable = true };
        var row = new WrapPanel { Classes = { "chips" }, Children = { keep, go } };
        keep.Removing += (_, e) => e.Cancel = true;
        var removed = 0;
        go.Removed += (_, _) => removed++;
        var w = Show(row);
        Click(Part<Button>(keep, "PART_Remove"), w);
        Assert.Contains(keep, row.Children);
        Click(Part<Button>(go, "PART_Remove"), w);
        Assert.Equal(1, removed);
        Assert.DoesNotContain(go, row.Children);
    }

    [AvaloniaFact]
    public void Filter_chip_takes_the_brand_and_a_tick_when_on()
    {
        var on = new ToggleButton { Content = "الأول", IsChecked = true, Classes = { "chip" } };
        var off = new ToggleButton { Content = "الثاني", Classes = { "chip" } };
        var link = new Button { Content = "الثالث", Classes = { "chip", "active" } };
        var w = Show(new StackPanel { Children = { on, off, link } });
        AssertBrush(on, "InsToneBrandBg", () => on.Background);
        AssertBrush(on, "InsToneBrandBd", () => on.BorderBrush);
        AssertBrush(off, "InsSurface", () => off.Background);
        AssertBrush(link, "InsToneBrandBg", () => link.Background);
        Assert.Equal(12, Part<Viewbox>(on, "PART_Tick").Bounds.Width, 1);
        Assert.Equal(0, Part<Viewbox>(off, "PART_Tick").Bounds.Width, 1);
        Assert.False(Part<Viewbox>(link, "PART_Tick").IsVisible);
        Click(off, w);
        Assert.True(off.IsChecked);
        Assert.Equal(12, Part<Viewbox>(off, "PART_Tick").Bounds.Width, 1);
    }

    // ------------------------------------------------------------ tags

    private static TagsInput ListTags() => new()
    {
        PlaceholderText = "اختر",
        Width = 400,
        Options =
        {
            new TagsOption("a", "الأول"), new TagsOption("b", "الثاني"), new TagsOption("c", "الثالث"),
            new TagsOption("d", "الرابع"), new TagsOption("e", "الخامس"),
        },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Tags_field_draws_chips_then_the_box(string theme, string dir)
    {
        var tags = ListTags();
        tags.Values.Add("a");
        var w = Show(new StackPanel { Margin = new Thickness(20), Children = { tags } }, Dir(dir), Theme(theme));
        var chip = tags.GetVisualDescendants().OfType<Chip>().Single();
        Assert.Equal("الأول", chip.Content);
        Assert.Equal(46, tags.Bounds.Height, 0);
        Assert.Null(tags.InputBox!.PlaceholderText);
        var field = Part<Border>(tags, "PART_Field");
        AssertBrush(field, "InsCtlBg", () => field.Background);
        tags.InputBox.Focus();
        Settle();
        Assert.True(tags.IsDropDownOpen);
        Capture(w, $"menus-tags-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void Tags_list_hides_chosen_options_matches_Arabic_and_stays_open()
    {
        var tags = ListTags();
        tags.Values.Add("a");
        var changes = new List<TagsChangedEventArgs>();
        tags.TagsChanged += (_, e) => changes.Add(e);
        var w = Show(new StackPanel { Children = { tags } });
        tags.InputBox!.Focus();
        Settle();
        var rows = w.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("tags-option")).ToList();
        Assert.Equal(5, rows.Count);
        Assert.False(rows[0].IsVisible);
        Assert.Equal(4, rows.Count(r => r.IsVisible));

        Type(w, "ثالث");
        Assert.Equal(1, rows.Count(r => r.IsVisible));
        Press(w, Key.Down);
        Assert.Contains("active", rows[2].Classes);
        Press(w, Key.Enter);
        Assert.Equal(["a", "c"], tags.Values);
        Assert.Equal("c", changes.Last().Added);
        Assert.True(tags.IsDropDownOpen);
        Assert.Equal(string.Empty, tags.InputBox.Text);
        Assert.Equal(3, rows.Count(r => r.IsVisible));

        Type(w, "زز");
        var empty = w.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("combo-empty"));
        Assert.True(empty.IsVisible);
        Assert.Equal("لا نتائج", empty.Text);
        Press(w, Key.Escape);
        Assert.False(tags.IsDropDownOpen);
    }

    [AvaloniaFact]
    public void Free_tags_take_Enter_and_commas_refuse_duplicates_and_Backspace_takes_the_last()
    {
        var tags = new TagsInput { PlaceholderText = "اكتب واضغط Enter", Width = 400 };
        var w = Show(new StackPanel { Children = { tags } });
        Assert.Equal("اكتب واضغط Enter", tags.InputBox!.PlaceholderText);
        tags.InputBox.Focus();
        Type(w, "وسم");
        Press(w, Key.Enter);
        Type(w, "آخر");
        Type(w, "،");
        Type(w, "وسم");
        Press(w, Key.Enter);
        Assert.Equal(["وسم", "آخر"], tags.Values);
        Assert.Null(tags.InputBox.PlaceholderText);
        tags.InputBox.Text = string.Empty;
        Press(w, Key.Back);
        Settle();
        Assert.Equal(["وسم"], tags.Values);
        Assert.Single(tags.GetVisualDescendants().OfType<Chip>());
    }

    [AvaloniaFact]
    public void Tags_stop_at_their_maximum()
    {
        var tags = ListTags();
        tags.Max = 3;
        tags.Values.AddRange(["a", "b"]);
        Assert.True(tags.Add("c"));
        Assert.False(tags.Add("d"));
        Assert.Equal(3, tags.Values.Count);
    }

    // ------------------------------------------------------------ palette

    [AvaloniaFact]
    public void Search_is_Arabic_aware()
    {
        Assert.Equal("اضافه", ArabicSearch.Normalize("إضافة"));
        Assert.Equal("اسامه", ArabicSearch.Normalize("أسامة"));
        Assert.Equal("مستشفي 12", ArabicSearch.Normalize("مُسْتَشْفى ١٢"));
        Assert.True(ArabicSearch.Matches("اضافه", "إضافة"));
        Assert.Equal(100, ArabicSearch.Place("جدول", "جدول"));
        Assert.Equal(80, ArabicSearch.Place("جدول الدروس", "جدول"));
        Assert.Equal(60, ArabicSearch.Place("الجدول", "جدول"));
        Assert.Equal(40, ArabicSearch.Place("مجدول", "جدول"));
        Assert.Equal(0, ArabicSearch.Score("فاتورة", null, null, ArabicSearch.Terms("فاتورة شهر")));
        Assert.Equal(30, ArabicSearch.Score("الأزرار", "buttons", null, ArabicSearch.Terms("butt")));
    }

    private static CommandPalette Palette(List<string>? ran = null) => new()
    {
        Items =
        {
            new PaletteItem { Label = "الأزرار", Group = "الصفحات", Keywords = "buttons", Run = () => ran?.Add("الأزرار") },
            new PaletteItem { Label = "الجداول", Group = "الصفحات", Keywords = "tables", Run = () => ran?.Add("الجداول") },
            new PaletteItem { Label = "الحوار", Group = "الصفحات", Keywords = "dialog" },
            new PaletteItem { Label = "حفظ", Group = "إجراءات", Keywords = "save", Hint = "Ctrl S", Run = () => ran?.Add("حفظ") },
        },
    };

    private static List<string> Heads(CommandPalette p) =>
        p.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("palette-head")).Select(t => t.Text!).ToList();

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Palette_opens_over_the_window_with_its_groups(string theme, string dir)
    {
        var palette = Palette();
        var opener = new Button { Content = "فتح" };
        var w = Show(new Panel { Children = { opener } }, Dir(dir), Theme(theme), 800, 640);
        opener.Focus();
        palette.Open(opener);
        Settle();
        Assert.True(palette.IsOpen);
        Assert.Same(OverlayLayer.GetOverlayLayer(w), palette.GetVisualParent());
        Assert.True(palette.InputBox!.IsFocused);
        Assert.Equal(["الصفحات", "إجراءات"], Heads(palette));
        Assert.Equal("الأزرار", palette.Active!.Label);
        var dialog = Part<Panel>(palette, "PART_Dialog");
        Assert.Equal(576, dialog.Bounds.Width, 1);
        Assert.Equal(96, dialog.Bounds.Y, 1);
        Capture(w, $"menus-palette-{theme}-{dir}");
        Press(w, Key.Escape);
        Assert.False(palette.IsOpen);
        Assert.True(opener.IsFocused);
    }

    [AvaloniaFact]
    public void Palette_ranks_moves_chooses_and_remembers()
    {
        var ran = new List<string>();
        var palette = Palette(ran);
        var w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        Type(w, "جداول");
        Assert.Equal(["الجداول"], palette.Shown.Select(i => i.Label));
        Assert.Contains(palette.GetVisualDescendants().OfType<TextBlock>(), t => t.Classes.Contains("palette-where") && t.IsVisible);
        Assert.Empty(Heads(palette));
        palette.InputBox!.Text = "";
        Settle(200);
        Press(w, Key.Down);
        Assert.Equal("الجداول", palette.Active!.Label);
        Press(w, Key.Up);
        Press(w, Key.Up);
        Assert.Equal("حفظ", palette.Active!.Label);
        Press(w, Key.Enter);
        Assert.False(palette.IsOpen);
        Assert.Equal(["حفظ"], ran);
        Assert.Equal(["label:حفظ"], palette.RecentKeys);

        palette.Open(w);
        Settle();
        Assert.Equal(["الأخيرة", "الصفحات"], Heads(palette));
        Assert.Equal("حفظ", palette.Shown[0].Label);
    }

    [AvaloniaFact]
    public void Palette_choice_can_be_cancelled_and_empty_search_says_so()
    {
        var ran = new List<string>();
        var palette = Palette(ran);
        palette.Choosing += (_, e) => e.Cancel = true;
        var w = Show(new Panel(), FlowDirection.RightToLeft, null, 800, 640);
        palette.Open(w);
        Settle();
        Type(w, "غير موجود");
        Assert.True(Part<TextBlock>(palette, "PART_Empty").IsVisible);
        palette.InputBox!.Text = "اضافه";
        Settle(200);
        palette.InputBox.Text = "";
        Settle(200);
        Press(w, Key.Enter);
        Assert.Empty(ran);
        Assert.False(palette.IsOpen);
    }

    [AvaloniaFact]
    public void Palette_closes_on_a_backdrop_press_and_a_row_click_chooses()
    {
        var ran = new List<string>();
        var palette = Palette(ran);
        var w = Show(new Panel(), FlowDirection.LeftToRight, null, 800, 640);
        palette.Open(w);
        Settle();
        PressAt(w, new Point(10, 600));
        Assert.False(palette.IsOpen);
        palette.Open(w);
        Settle();
        var row = palette.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("palette-item") && ((TextBlock)((Grid)b.Child!).Children.OfType<TextBlock>().First()).Text == "الجداول");
        Click(row, w);
        Assert.Equal(["الجداول"], ran);
    }

    [AvaloniaFact]
    public void Palette_shortcuts_toggle_it_with_any_layout()
    {
        var palette = Palette();
        var box = new TextBox();
        var first = new Button { Content = "أ" };
        var w = Show(new StackPanel { Children = { first, box } }, FlowDirection.RightToLeft, null, 800, 640);
        using var keys = palette.AttachTo(w);
        Press(w, Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");
        Assert.True(palette.IsOpen);
        Press(w, Key.K, RawInputModifiers.Control, PhysicalKey.K, "k");
        Assert.False(palette.IsOpen);
        Press(w, Key.None, RawInputModifiers.Control, PhysicalKey.K, "ن");
        Assert.True(palette.IsOpen);
        palette.Close();
        box.Focus();
        Press(w, Key.Oem2, RawInputModifiers.None, PhysicalKey.Slash, "/");
        Assert.False(palette.IsOpen);
        first.Focus();
        Settle(100);
        Press(w, Key.Oem2, RawInputModifiers.None, PhysicalKey.Slash, "/");
        Assert.True(palette.IsOpen);
    }
}
