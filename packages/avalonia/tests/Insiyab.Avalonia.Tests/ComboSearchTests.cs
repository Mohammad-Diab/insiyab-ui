using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// ComboBox.searchable: a search field over a virtualized list when open; every typed word anywhere, Arabic folded; headings; the keys move a highlight and only a pick chooses.
public class ComboSearchTests
{
    public sealed record Food(string Name, string English, string Kind);

    private static readonly Food[] Foods =
    [
        new("تفاح أحمر", "red apple", "فواكه"), new("موز", "banana", "فواكه"), new("برتقال", "orange", "فواكه"),
        new("خيار", "cucumber", "خضار"), new("جزر", "carrot", "خضار"), new("بطاطا حلوة", "sweet potato", "خضار"),
        new("عدس", "lentils", "بقول"), new("حمّص", "chickpeas", "بقول"),
    ];

    private static ComboBox Combo(object[]? items = null, double width = 300)
    {
        var combo = new ComboBox { Classes = { "searchable" }, Width = width, ItemsSource = items ?? Foods, DisplayMemberBinding = new Binding(nameof(Food.Name)) };
        return combo;
    }

    private static ComboSearcher Searcher(ComboBox combo) => Assert.IsType<ComboSearcher>(ComboSearchLook.Of(combo));

    private static Border Card(ComboBox c) => (Border)((Border)Part<Popup>(c, "PART_Popup").Child!).Child!;

    private static Window Open(ComboBox combo, FlowDirection dir = FlowDirection.RightToLeft)
    {
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { combo } }, dir, height: 600);
        combo.Focus();
        combo.IsDropDownOpen = true;
        Settle(300);
        return w;
    }

    private static void Key(Window w, Key key, RawInputModifiers mods = RawInputModifiers.None)
    {
        var top = TopLevel.GetTopLevel(w.FocusManager?.GetFocusedElement() as Visual) ?? w;
        top.KeyPress(key, mods, PhysicalKey.None, null);
        top.KeyRelease(key, mods, PhysicalKey.None, null);
        Settle(150);
    }

    private static void Type(Window w, string text)
    {
        var top = TopLevel.GetTopLevel(w.FocusManager?.GetFocusedElement() as Visual) ?? w;
        top.KeyTextInput(text);
        Settle(150);
    }

    private static string[] Names(ComboSearcher s) => s.Shown.Select(o => o switch
    {
        ComboSearchEntry e => ((Food)e.Item).Name,
        ComboSearchHeading h => "#" + h.Title,
        _ => "?",
    }).ToArray();

    private static Food? Hot(ComboSearcher s) => s.Hot as Food;

    [AvaloniaFact]
    public void Opening_puts_the_keyboard_in_the_search_over_the_whole_list_on_the_choice()
    {
        var items = Enumerable.Range(1, 60).Select(i => (object)new Food($"عنصر {i}", $"item {i}", "")).ToArray();
        var combo = Combo(items);
        combo.SelectedIndex = 40;
        Open(combo);
        var s = Searcher(combo);
        Assert.True(s.Search.IsFocused);
        Assert.Equal(60, s.Shown.Count);
        Assert.Same(items[40], s.Hot);
        var rows = Card(combo).GetVisualDescendants().OfType<ComboBoxItem>().Where(r => r.IsVisible).ToList();
        Assert.InRange(rows.Count, 1, 30);
        Assert.Contains(rows, r => r.Classes.Contains(":ins-hot") && r.Classes.Contains(":selected"));
        Assert.Equal(40, combo.SelectedIndex);
    }

    [AvaloniaFact]
    public void Every_typed_word_is_found_anywhere_with_arabic_folded()
    {
        var combo = Combo();
        Open(combo);
        var s = Searcher(combo);
        s.Search.Text = "حلوه بطاطا";
        Settle(100);
        Assert.Equal(["بطاطا حلوة"], Names(s));
        s.Search.Text = "حمص";
        Settle(100);
        Assert.Equal(["حمّص"], Names(s));
    }

    [AvaloniaFact]
    public void Keywords_find_an_item_by_its_other_words()
    {
        var combo = Combo();
        ComboSearch.SetKeywords(combo, new Binding(nameof(Food.English)));
        Open(combo);
        var s = Searcher(combo);
        s.Search.Text = "APPLE";
        Settle(100);
        Assert.Equal(["تفاح أحمر"], Names(s));
    }

    [AvaloniaFact]
    public void Headings_show_only_when_the_items_fall_in_more_than_one_group()
    {
        var combo = Combo();
        ComboSearch.SetGroup(combo, new Binding(nameof(Food.Kind)));
        Open(combo);
        var s = Searcher(combo);
        Assert.Equal(["#فواكه", "تفاح أحمر", "موز", "برتقال", "#خضار", "خيار", "جزر", "بطاطا حلوة", "#بقول", "عدس", "حمّص"], Names(s));
        Assert.Contains(Card(combo).GetVisualDescendants().OfType<TextBlock>(), t => t.Classes.Contains("pop-label") && t.Text == "فواكه");
        combo.IsDropDownOpen = false;
        Settle(100);
        var one = Combo(Foods.Take(3).Cast<object>().ToArray());
        ComboSearch.SetGroup(one, new Binding(nameof(Food.Kind)));
        Open(one);
        Assert.Equal(["تفاح أحمر", "موز", "برتقال"], Names(Searcher(one)));
    }

    [AvaloniaFact]
    public void Arrows_move_the_highlight_without_choosing_and_enter_picks()
    {
        var combo = Combo();
        combo.SelectedIndex = 0;
        var w = Open(combo);
        var s = Searcher(combo);
        Key(w, global::Avalonia.Input.Key.Down);
        Key(w, global::Avalonia.Input.Key.Down);
        Assert.Equal("برتقال", Hot(s)?.Name);
        Assert.Equal(0, combo.SelectedIndex);
        Assert.True(combo.IsDropDownOpen);
        Key(w, global::Avalonia.Input.Key.Up);
        Assert.Equal("موز", Hot(s)?.Name);
        Key(w, global::Avalonia.Input.Key.Enter);
        Assert.False(combo.IsDropDownOpen);
        Assert.Equal(1, combo.SelectedIndex);
        Assert.True(combo.IsFocused);
    }

    [AvaloniaFact]
    public void Escape_closes_and_keeps_the_choice()
    {
        var combo = Combo();
        combo.SelectedIndex = 2;
        var w = Open(combo);
        Key(w, global::Avalonia.Input.Key.Down);
        Key(w, global::Avalonia.Input.Key.Escape);
        Assert.False(combo.IsDropDownOpen);
        Assert.Equal(2, combo.SelectedIndex);
        Assert.True(combo.IsFocused);
    }

    [AvaloniaFact]
    public void A_letter_on_the_closed_field_opens_the_list_with_the_search_started()
    {
        var combo = Combo();
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { combo } }, FlowDirection.RightToLeft, height: 600);
        combo.Focus();
        Settle(100);
        Type(w, "ج");
        Settle(200);
        var s = Searcher(combo);
        Assert.True(combo.IsDropDownOpen);
        Assert.Equal("ج", s.Search.Text);
        Assert.Equal(["جزر"], Names(s));
    }

    [AvaloniaFact]
    public void The_closed_field_opens_on_its_keys_and_never_steps_the_choice()
    {
        var combo = Combo();
        combo.SelectedIndex = 3;
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { combo } }, FlowDirection.RightToLeft, height: 600);
        combo.Focus();
        Settle(100);
        Key(w, global::Avalonia.Input.Key.Down);
        Assert.True(combo.IsDropDownOpen);
        Assert.Equal(3, combo.SelectedIndex);
        Key(w, global::Avalonia.Input.Key.Escape);
        Assert.False(combo.IsDropDownOpen);
        Key(w, global::Avalonia.Input.Key.Space);
        Type(w, " ");
        Assert.True(combo.IsDropDownOpen);
        Assert.Equal("", Searcher(combo).Search.Text ?? "");
    }

    [AvaloniaFact]
    public void A_space_typed_in_the_search_stays_there()
    {
        var combo = Combo();
        var w = Open(combo);
        var s = Searcher(combo);
        Type(w, "بطاطا");
        Key(w, global::Avalonia.Input.Key.Space);
        Type(w, " ");
        Type(w, "حلوة");
        Assert.True(combo.IsDropDownOpen);
        Assert.Equal("بطاطا حلوة", s.Search.Text);
        Assert.Null(combo.SelectedItem);
    }

    [AvaloniaFact]
    public void Nothing_found_shows_the_empty_line_in_the_page_language()
    {
        var combo = Combo();
        Open(combo);
        var s = Searcher(combo);
        s.Search.Text = "لا شيء هنا";
        Settle(100);
        var empty = Card(combo).GetVisualDescendants().OfType<TextBlock>().Single(t => t.Classes.Contains("combo-empty"));
        Assert.True(empty.IsVisible);
        Assert.Equal("لا نتائج", empty.Text);
        Assert.Equal("ابحث", s.Search.PlaceholderText);
        Assert.Null(s.Hot);
    }

    [AvaloniaFact]
    public void The_list_width_comes_from_the_field_and_holds_while_scrolling()
    {
        var items = Enumerable.Range(1, 80).Select(i => (object)new Food(i % 10 == 0 ? $"عنصر طويل جدًا جدًا جدًا جدًا جدًا جدًا جدًا جدًا {i}" : $"عنصر {i}", "", "")).ToArray();
        var combo = Combo(items, width: 300);
        Open(combo);
        var card = Card(combo);
        Assert.Equal(300, card.Bounds.Width, 0.5);
        var scroller = card.GetVisualDescendants().OfType<ScrollViewer>().First();
        foreach (var y in new[] { 400.0, 1200, 2400 })
        {
            scroller.Offset = new Vector(0, y);
            Settle(100);
            Assert.Equal(300, card.Bounds.Width, 0.5);
        }
        var narrow = Combo(width: 120);
        Open(narrow);
        Assert.Equal(220, Card(narrow).Bounds.Width, 0.5);
    }

    [AvaloniaFact]
    public void A_click_on_a_row_picks_it_and_the_pointer_moves_the_highlight()
    {
        var combo = Combo();
        var w = Open(combo);
        var s = Searcher(combo);
        var row = Card(combo).GetVisualDescendants().OfType<ComboBoxItem>().First(r => (r.Content as Food)?.Name == "جزر");
        var top = TopLevel.GetTopLevel(row)!;
        var p = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), top)!.Value;
        top.MouseMove(p);
        Settle(100);
        Assert.Equal("جزر", Hot(s)?.Name);
        top.MouseDown(p, MouseButton.Left);
        top.MouseUp(p, MouseButton.Left);
        Settle(200);
        Assert.False(combo.IsDropDownOpen);
        Assert.Equal("جزر", (combo.SelectedItem as Food)?.Name);
    }

    [AvaloniaFact]
    public void The_searchable_list_keeps_the_room_for_its_shadow()
    {
        var combo = Combo();
        Open(combo);
        var room = (Border)Part<Popup>(combo, "PART_Popup").Child!;
        Assert.Equal(new Thickness(40, 16, 40, 64), room.Padding);
        Assert.Equal("PopupBorder", Card(combo).Name);
        Assert.IsType<DockPanel>(Card(combo).Child);
    }

    [AvaloniaFact]
    public void A_plain_select_keeps_its_own_list_and_taking_the_class_away_gives_it_back()
    {
        var plain = new ComboBox { Items = { "أ", "ب" } };
        var combo = Combo();
        Show(new StackPanel { Children = { plain, combo } });
        Assert.IsType<ScrollViewer>(Card(plain).Child);
        Assert.Null(ComboSearchLook.Of(plain));
        Assert.IsType<DockPanel>(Card(combo).Child);
        combo.Classes.Remove("searchable");
        Settle(100);
        var scroller = Assert.IsType<ScrollViewer>(Card(combo).Child);
        Assert.IsType<ItemsPresenter>(scroller.Content);
        Assert.Null(ComboSearchLook.Of(combo));
    }

    [AvaloniaFact]
    public void Written_items_are_searched_by_their_text()
    {
        var combo = new ComboBox { Classes = { "searchable" }, Width = 260, Items = { new ComboBoxItem { Content = "دمشق" }, new ComboBoxItem { Content = "حلب" }, new ComboBoxItem { Content = "حمص", IsEnabled = false } } };
        var w = Open(combo);
        var s = Searcher(combo);
        s.Search.Text = "ح";
        Settle(100);
        Assert.Equal(2, s.Shown.Count);
        Key(w, global::Avalonia.Input.Key.Down);
        Assert.Same(combo.Items[1], s.Hot);
        Key(w, global::Avalonia.Input.Key.Enter);
        Assert.Equal(1, combo.SelectedIndex);
    }

    [AvaloniaFact]
    public void Keywords_and_group_bind_from_markup()
    {
        var combo = global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Parse<ComboBox>(
            """
            <ComboBox xmlns="https://github.com/avaloniaui" xmlns:ins="https://github.com/Mohammad-Diab/insiyab-ui"
                      Classes="searchable" ins:ComboSearch.Keywords="{Binding English}" ins:ComboSearch.Group="{Binding Kind}" ins:ComboSearch.Placeholder="ابحث عن صنف" />
            """, typeof(InsiyabTheme).Assembly);
        Assert.IsAssignableFrom<BindingBase>(ComboSearch.GetKeywords(combo));
        Assert.IsAssignableFrom<BindingBase>(ComboSearch.GetGroup(combo));
        combo.ItemsSource = Foods;
        combo.DisplayMemberBinding = new Binding(nameof(Food.Name));
        Open(combo);
        var s = Searcher(combo);
        Assert.Equal("ابحث عن صنف", s.Search.PlaceholderText);
        s.Search.Text = "carrot";
        Settle(100);
        Assert.Equal(["#خضار", "جزر"], Names(s));
    }
}
