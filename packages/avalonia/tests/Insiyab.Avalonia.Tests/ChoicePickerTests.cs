using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System.Windows.Input;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// ChoicePicker and ChoiceDialog: a search that keeps the keyboard over a virtualized, grouped list; the keys move a highlight, Enter or a click picks; loading, failure and retry.
public class ChoicePickerTests
{
    public sealed record Product(string Name, string Code, string Kind);

    private static readonly Product[] Products =
    [
        new("كرسي مكتب دوار", "OFC601", "المفضلة"), new("طاولة الاجتماعات الكبيرة", "TBL601", "المفضلة"),
        new("خزانة ملفات", "CAB402", "كل المنتجات"), new("مصباح مكتب", "LMP301", "كل المنتجات"), new("رف كتب", "SHL501", "كل المنتجات"),
    ];

    private static ChoicePicker Picker(IEnumerable<object>? items = null, bool groups = false) => new()
    {
        ItemsSource = items ?? Products,
        TextBinding = new Binding(nameof(Product.Name)),
        KeywordsBinding = new Binding(nameof(Product.Code)),
        GroupBinding = groups ? new Binding(nameof(Product.Kind)) : null,
    };

    private static (Window Window, Button Opener) Host(FlowDirection dir = FlowDirection.RightToLeft, ThemeVariant? theme = null, double width = 900, double height = 700)
    {
        var opener = new Button { Content = "اختر", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var window = Show(opener, dir, theme, width, height);
        return (window, opener);
    }

    private static (Window Window, ChoiceDialog Dialog, Task<object?> Result) OpenDialog(ChoicePicker picker, FlowDirection dir = FlowDirection.RightToLeft, ThemeVariant? theme = null, double width = 900, double height = 700)
    {
        var (window, opener) = Host(dir, theme, width, height);
        var dialog = new ChoiceDialog { Title = "اختر منتجًا", Content = picker };
        var result = dialog.ShowAsync(opener);
        Settle(600);
        return (window, dialog, result);
    }

    private static void Press(Window w, Key key)
    {
        w.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        w.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(150);
    }

    private static string[] Names(ChoicePicker p) => p.Shown.Select(o => o switch
    {
        ComboSearchEntry e => ((Product)e.Item).Code,
        ComboSearchHeading h => "#" + h.Title,
        _ => "?",
    }).ToArray();

    private static List<ComboBoxItem> Rows(Control root) => root.GetVisualDescendants().OfType<ComboBoxItem>().Where(r => r.IsEffectivelyVisible).ToList();

    [AvaloniaFact]
    public async Task A_choice_dialog_opens_on_the_search_with_the_choice_highlighted_and_a_pick_closes_it()
    {
        var items = Enumerable.Range(1, 80).Select(i => (object)new Product($"منتج {i}", $"C{i:000}", "")).ToArray();
        var picker = Picker(items);
        picker.SelectedItem = items[60];
        var (window, dialog, result) = OpenDialog(picker);
        Assert.True(dialog.IsOpen);
        Assert.Contains("choice", dialog.Classes);
        Assert.True(picker.Search!.IsFocused);
        Assert.Same(items[60], picker.HighlightedItem);
        var hot = Assert.Single(Rows(dialog), r => r.Classes.Contains(":ins-hot"));
        Assert.Contains(":selected", hot.Classes);
        Assert.InRange(Rows(dialog).Count, 1, 40);

        Press(window, Key.Down);
        Press(window, Key.Down);
        Assert.Same(items[62], picker.HighlightedItem);
        Assert.True(picker.Search.IsFocused);
        Assert.Same(items[60], picker.SelectedItem);
        Press(window, Key.Enter);
        Assert.False(dialog.IsOpen);
        Assert.Same(items[62], await result);
        Assert.Same(items[62], picker.SelectedItem);
    }

    [AvaloniaFact]
    public async Task Escape_and_an_outside_press_cancel_without_choosing()
    {
        var picker = Picker();
        picker.SelectedItem = Products[1];
        var (window, dialog, result) = OpenDialog(picker);
        Press(window, Key.Down);
        Press(window, Key.Escape);
        Assert.False(dialog.IsOpen);
        Assert.Null(await result);
        Assert.Same(Products[1], picker.SelectedItem);

        var again = new ChoiceDialog { Content = Picker() };
        var opener = (Button)window.Content!;
        var second = again.ShowAsync(opener);
        Settle(600);
        var corner = new Point(8, 8);
        window.MouseDown(corner, MouseButton.Left);
        window.MouseUp(corner, MouseButton.Left);
        Settle(400);
        Assert.False(again.IsOpen);
        Assert.Null(await second);
    }

    [AvaloniaFact]
    public void Every_typed_word_is_found_anywhere_by_name_or_code_with_arabic_folded()
    {
        var picker = Picker();
        var (_, _, _) = OpenDialog(picker);
        picker.SearchText = "الكبيره طاوله";
        Settle(100);
        Assert.Equal(["TBL601"], Names(picker));
        picker.SearchText = "cab4";
        Settle(100);
        Assert.Equal(["CAB402"], Names(picker));
        picker.SearchText = "601";
        Settle(100);
        Assert.Equal(["OFC601", "TBL601"], Names(picker));
        picker.SearchText = "zzz";
        Settle(100);
        Assert.Empty(Names(picker));
        Assert.True(Part<TextBlock>(picker, "PART_Empty").IsVisible);
    }

    [AvaloniaFact]
    public void Headings_group_the_items_and_the_keys_and_the_pointer_pass_them_by()
    {
        var picker = Picker(groups: true);
        var (window, dialog, _) = OpenDialog(picker);
        Assert.Equal(["#المفضلة", "OFC601", "TBL601", "#كل المنتجات", "CAB402", "LMP301", "SHL501"], Names(picker));
        Assert.Same(Products[0], picker.HighlightedItem);
        Press(window, Key.Down);
        Press(window, Key.Down);
        Assert.Same(Products[2], picker.HighlightedItem);
        Press(window, Key.Up);
        Press(window, Key.Up);
        Press(window, Key.Up);
        Assert.Same(Products[0], picker.HighlightedItem);
        // headings are labels, never rows that can be highlighted or picked
        Assert.Equal(2, dialog.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Classes.Contains("pop-label")));
        Assert.Equal(5, Rows(dialog).Count);
    }

    [AvaloniaFact]
    public async Task A_click_picks_the_row_under_the_pointer()
    {
        var picker = Picker();
        var (window, dialog, result) = OpenDialog(picker);
        var row = Rows(dialog).First(r => r.DataContext is Product { Code: "LMP301" } || (r.Content as Product)?.Code == "LMP301");
        var p = Center(row, window);
        window.MouseMove(p);
        Settle(150);
        Assert.Same(Products[3], picker.HighlightedItem);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Settle(300);
        Assert.Same(Products[3], await result);
        Assert.False(dialog.IsOpen);
    }

    [AvaloniaFact]
    public void A_press_dragged_to_another_row_picks_nothing()
    {
        var picker = Picker();
        var (window, dialog, _) = OpenDialog(picker);
        var from = Center(Rows(dialog)[0], window);
        var to = Center(Rows(dialog)[3], window);
        window.MouseMove(from);
        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(to);
        window.MouseUp(to, MouseButton.Left);
        Settle(300);
        Assert.True(dialog.IsOpen);
        Assert.Null(picker.SelectedItem);
    }

    [AvaloniaFact]
    public async Task Items_changed_while_the_dialog_is_closed_show_when_it_opens_again()
    {
        var items = new ObservableCollection<object>(Products);
        var picker = Picker(items);
        var (window, dialog, result) = OpenDialog(picker);
        Press(window, Key.Escape);
        Assert.Null(await result);
        items.Add(new Product("دفتر ملاحظات", "NTB100", ""));
        var again = dialog.ShowAsync((Button)window.Content!);
        Settle(600);
        Assert.Contains("NTB100", Names(picker));
        Press(window, Key.Escape);
        Assert.Null(await again);
    }

    [AvaloniaFact]
    public void While_loading_skeletons_stand_in_and_typed_search_survives_the_items_arriving()
    {
        var items = new ObservableCollection<object>();
        var picker = Picker(items);
        picker.IsLoading = true;
        var (window, dialog, _) = OpenDialog(picker);
        Assert.True(Part<StackPanel>(picker, "PART_Loading").IsVisible);
        Assert.True(Part<StackPanel>(picker, "PART_Loading").Children.Count > 0);
        Assert.Contains(":loading", picker.Classes);
        Assert.False(Part<TextBlock>(picker, "PART_Empty").IsVisible);

        window.KeyTextInput("601");
        Settle(150);
        Assert.Equal("601", picker.SearchText);
        Press(window, Key.Enter);
        Assert.True(dialog.IsOpen);

        foreach (var c in Products) items.Add(c);
        picker.IsLoading = false;
        Settle(200);
        Assert.False(Part<StackPanel>(picker, "PART_Loading").IsVisible);
        Assert.Equal(["OFC601", "TBL601"], Names(picker));
        Assert.Equal("601", picker.Search!.Text);
    }

    [AvaloniaFact]
    public void A_failure_shows_its_reason_and_retry_runs_the_command()
    {
        var picker = Picker([]);
        var runs = 0;
        picker.RetryCommand = new Run(() => runs++);
        picker.Error = "تعذّرت قراءة المنتجات.";
        picker.IsLoading = true;
        var (window, dialog, _) = OpenDialog(picker);
        var failure = Part<StackPanel>(picker, "PART_Failure");
        Assert.False(failure.IsVisible);
        picker.IsLoading = false;
        Settle(100);
        Assert.True(failure.IsVisible);
        Assert.Contains(":failed", picker.Classes);
        Assert.False(Part<TextBlock>(picker, "PART_Empty").IsVisible);
        var retry = Part<Button>(picker, "PART_Retry");
        Assert.Equal("أعد المحاولة", retry.Content);
        Click(retry, window);
        Assert.Equal(1, runs);
        Assert.True(dialog.IsOpen);
    }

    [AvaloniaFact]
    public void The_highlight_stays_on_its_item_when_items_arrive_before_it()
    {
        var items = new ObservableCollection<object>(Products);
        var picker = Picker(items);
        var (window, _, _) = OpenDialog(picker);
        Press(window, Key.Down);
        Press(window, Key.Down);
        Assert.Same(Products[2], picker.HighlightedItem);
        items.Insert(0, new Product("منتج جديد", "NEW100", ""));
        Settle(200);
        Assert.Same(Products[2], picker.HighlightedItem);
    }

    [AvaloniaFact]
    public void A_long_list_is_virtualized_and_the_dialog_keeps_its_size_while_the_matches_change()
    {
        var items = Enumerable.Range(1, 3000).Select(i => (object)new Product($"منتج رقم {i}", $"X{i:0000}", "")).ToArray();
        var picker = Picker(items);
        var (_, dialog, _) = OpenDialog(picker, height: 800);
        Assert.InRange(Rows(dialog).Count, 1, 60);
        var height = dialog.Bounds.Height;
        var search = picker.Search!;
        var top = search.TranslatePoint(default, dialog)!.Value.Y;
        picker.SearchText = "X0007";
        Settle(200);
        Assert.Single(Names(picker));
        Assert.Equal(height, dialog.Bounds.Height, 0.5);
        Assert.Equal(top, search.TranslatePoint(default, dialog)!.Value.Y, 0.5);
    }

    [AvaloniaFact]
    public void A_narrow_short_window_keeps_the_search_and_scrolls_only_the_list()
    {
        var items = Enumerable.Range(1, 200).Select(i => (object)new Product($"منتج {i}", $"N{i:000}", "")).ToArray();
        var picker = Picker(items);
        var (window, dialog, _) = OpenDialog(picker, width: 380, height: 420);
        Assert.True(dialog.Bounds.Width <= window.Width);
        Assert.True(dialog.Bounds.Height <= window.Height);
        var search = picker.Search!;
        Assert.True(search.IsEffectivelyVisible);
        var body = Part<ScrollViewer>(dialog, "PART_Scroll");
        Assert.Equal(0, body.Extent.Height - body.Viewport.Height, 1);
        for (var i = 0; i < 40; i++) Press(window, Key.Down);
        Assert.Same(items[40], picker.HighlightedItem);
        var hot = Assert.Single(Rows(dialog), r => r.Classes.Contains(":ins-hot"));
        var y = hot.TranslatePoint(default, dialog)!.Value.Y;
        Assert.InRange(y, 0, dialog.Bounds.Height);
    }

    [AvaloniaFact]
    public void Max_rows_caps_the_list_for_a_pop()
    {
        var items = Enumerable.Range(1, 50).Select(i => (object)new Product($"منتج {i}", $"P{i:000}", "")).ToArray();
        var picker = Picker(items);
        picker.MaxRows = 5;
        var w = Show(new StackPanel { Margin = new Thickness(20), Width = 320, Children = { picker } }, FlowDirection.RightToLeft, height: 700);
        Settle(300);
        var row = Rows(picker).First();
        Assert.InRange(picker.Bounds.Height, row.Bounds.Height * 5, row.Bounds.Height * 5 + 120);
        Assert.Equal(5, Part<StackPanel>(picker, "PART_Loading").Children.Count);
        Assert.NotNull(w);
    }

    [AvaloniaFact]
    public void The_words_follow_the_direction_and_can_be_set()
    {
        var ltr = Picker();
        Show(ltr, FlowDirection.LeftToRight);
        Assert.Equal("Search", ltr.Search!.PlaceholderText);
        Assert.Equal("Try again", Part<Button>(ltr, "PART_Retry").Content);
        Assert.Equal("No results", Part<TextBlock>(ltr, "PART_Empty").Text);
        ltr.PlaceholderText = "Search by name or code";
        Assert.Equal("Search by name or code", ltr.Search.PlaceholderText);
    }

    [AvaloniaFact]
    public void Xaml_takes_the_bindings_and_a_foot()
    {
        var picker = AvaloniaRuntimeXamlLoader.Parse<ChoicePicker>(
            """
            <ins:ChoicePicker xmlns="https://github.com/avaloniaui" xmlns:ins="https://github.com/Mohammad-Diab/insiyab-ui"
                              TextBinding="{Binding Name}" KeywordsBinding="{Binding Code}" GroupBinding="{Binding Kind}" MaxRows="4">
              <ins:ChoicePicker.Foot>
                <TextBlock Text="hint" />
              </ins:ChoicePicker.Foot>
            </ins:ChoicePicker>
            """, typeof(InsiyabTheme).Assembly);
        picker.ItemsSource = Products;
        Show(picker, FlowDirection.RightToLeft);
        Assert.Equal(4, picker.MaxRows);
        Assert.True(Part<global::Avalonia.Controls.Presenters.ContentPresenter>(picker, "PART_Foot").IsVisible);
        Assert.Equal(7, picker.Shown.Count);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Shots(string theme, string dir)
    {
        var picker = Picker(groups: true);
        picker.SelectedItem = Products[3];
        var (window, _, _) = OpenDialog(picker, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 960, 640);
        Press(window, Key.Down);
        Capture(window, $"choice-dialog-{theme}-{dir}");
    }

    private sealed class Run(Action action) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => action();
    }
}

// Gutter: room for shadows and focus rings inside whatever clips, on the sides named, from the spacing tokens.
public class GutterTests
{
    [AvaloniaFact]
    public void The_sides_named_take_the_shadow_or_the_ring_room()
    {
        var all = new Border();
        var sides = new StackPanel();
        var mixed = new StackPanel();
        Gutter.SetShadow(all, GutterSides.All);
        Gutter.SetShadow(sides, GutterSides.Inline | GutterSides.Bottom);
        Gutter.SetShadow(mixed, GutterSides.Inline | GutterSides.Bottom);
        Gutter.SetRing(mixed, GutterSides.Top | GutterSides.Start);
        Show(new StackPanel { Children = { all, sides, mixed } }, FlowDirection.RightToLeft);
        Assert.Equal(new Thickness(16), all.Margin);
        Assert.Equal(new Thickness(16, 0, 16, 16), sides.Margin);
        Assert.Equal(new Thickness(16, 4, 16, 16), mixed.Margin);
        Gutter.SetShadow(all, GutterSides.None);
        Assert.Equal(default, all.Margin);
    }

    [AvaloniaFact]
    public void A_margin_set_by_the_app_wins_and_xaml_takes_a_list_of_sides()
    {
        var own = new Border { Margin = new Thickness(2) };
        Gutter.SetShadow(own, GutterSides.All);
        var panel = AvaloniaRuntimeXamlLoader.Parse<StackPanel>(
            """
            <StackPanel xmlns="https://github.com/avaloniaui" xmlns:ins="https://github.com/Mohammad-Diab/insiyab-ui" ins:Gutter.Shadow="Inline,Bottom" ins:Gutter.Ring="Top" />
            """, typeof(InsiyabTheme).Assembly);
        Show(new StackPanel { Children = { own, panel } });
        Assert.Equal(new Thickness(2), own.Margin);
        Assert.Equal(new Thickness(16, 4, 16, 16), panel.Margin);
    }

    [AvaloniaFact]
    public void A_shadow_inside_a_scroller_is_not_cut_at_its_edge()
    {
        var card = new Border { Classes = { "card" }, Height = 80, Child = new TextBlock { Text = "بطاقة" } };
        var content = new StackPanel { ClipToBounds = false, Children = { card } };
        Gutter.SetShadow(content, GutterSides.All);
        var scroller = new ScrollViewer { Content = content, Height = 200 };
        Show(scroller, FlowDirection.RightToLeft);
        var inViewport = card.TranslatePoint(default, scroller)!.Value;
        Assert.Equal(16, inViewport.Y, 0.5);
        Assert.True(inViewport.X >= 16 - 0.5);
    }
}
