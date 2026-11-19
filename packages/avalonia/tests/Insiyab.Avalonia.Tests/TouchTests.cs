using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A finger beside a mouse: no hover left behind, no tap while scrolling, small controls a finger can hit, long presses for menus, tips and words, and drags that keep their finger.
public class TouchTests
{
    private static IHeadlessTouchPointer Down(Window w, Point p)
    {
        var t = w.TouchBegin(p);
        Settle(40);
        return t;
    }

    private static void Up(Window w, IHeadlessTouchPointer t, Point p)
    {
        w.TouchEnd(t, p);
        Settle(80);
    }

    // Taps are kept apart, so two in a row are never one double tap.
    private static void Tap(Window w, Point p)
    {
        Settle(550);
        Up(w, Down(w, p), p);
    }

    private static void Swipe(Window w, Point from, Vector by, int steps = 8)
    {
        Settle(550);
        var t = Down(w, from);
        for (var k = 1; k <= steps; k++)
        {
            w.TouchMove(t, from + by * k / steps);
            Settle(12);
        }
        Up(w, t, from + by);
    }

    private static void Hold(Window w, Point p, Action? whileHeld = null)
    {
        Settle(550);
        var t = w.TouchBegin(p);
        Settle(900);
        whileHeld?.Invoke();
        Up(w, t, p);
    }

    private static Border Filler(double height = 900) => new() { Height = height };

    [AvaloniaFact]
    public void A_finger_leaves_no_hover_and_shows_a_press_while_it_is_down()
    {
        var icon = new Button { Classes = { "icon" }, Content = new Icon { Data = global::Avalonia.Media.Geometry.Parse("M4 12h16") } };
        var list = new ListBox { ItemsSource = new[] { "Lamp", "Desk" }, ItemTemplate = new FuncDataTemplate<string>((n, _) => new TextBlock { Classes = { "list-title" }, Text = n }) };
        var card = new Border { Classes = { "card", "hoverable" }, Height = 60, Child = new TextBlock { Text = "Card" } };
        var w = Show(new StackPanel { Spacing = 16, Margin = new Thickness(24), Children = { icon, list, card } });

        var t = Down(w, Center(icon, w));
        Assert.Contains(":pressed", icon.Classes);
        Assert.DoesNotContain(":pointerover", icon.Classes);
        Assert.Contains(InsiyabTheme.TouchPseudoClass, w.Classes);
        Up(w, t, Center(icon, w));
        Assert.DoesNotContain(":pressed", icon.Classes);
        Assert.DoesNotContain(":pointerover", icon.Classes);

        var row = Assert.IsType<ListBoxItem>(list.ContainerFromIndex(1));
        t = Down(w, Center(row, w));
        Assert.Contains(":pressed", row.Classes);
        AssertBrush(row, "InsPressLayer", () => row.Background); // the press's state layer (Press.axaml), clearer than the hover's tint
        Up(w, t, Center(row, w));

        Tap(w, Center(card, w));
        Assert.False(HoverLift.GetIsLifted(card));
        Assert.DoesNotContain(":pointerover", card.Classes);

        // a mouse still hovers as it always did, and the window leaves touch
        w.MouseMove(Center(icon, w));
        Settle(100);
        Assert.Contains(":pointerover", icon.Classes);
        Assert.DoesNotContain(InsiyabTheme.TouchPseudoClass, w.Classes);
    }

    [AvaloniaFact]
    public void A_finger_that_scrolls_fires_nothing_and_a_tap_still_does()
    {
        var invoked = new List<object?>();
        var list = new ListBox { ItemsSource = new[] { "Lamp", "Desk", "Chair" }, ItemTemplate = new FuncDataTemplate<string>((n, _) => new TextBlock { Classes = { "list-title" }, Text = n }) };
        ListRow.AddInvokedHandler(list, (_, e) => invoked.Add(e.Item));
        var clicks = new Dictionary<string, int>();
        Button Count(Button b, string name)
        {
            clicks[name] = 0;
            b.Click += (_, _) => clicks[name]++;
            return b;
        }
        var cardButton = Count(new Button { Height = 80, Content = new TextBlock { Text = "Open the file" } }, "card");
        cardButton.Theme = (global::Avalonia.Styling.ControlTheme)Application.Current!.FindResource("InsCardButton")!;
        var stat = Count(new Button { Classes = { "stat-link" }, Content = new Border { Classes = { "stat", "glass" }, Child = new TextBlock { Text = "Orders" } } }, "stat");
        var chip = Count(new ToggleButton { Classes = { "chip" }, Content = "Tea" }, "chip");
        var button = Count(new Button { Content = "Save" }, "button");
        var stack = new StackPanel { Spacing = 12, Margin = new Thickness(24), Children = { list, cardButton, stat, chip, button, Filler() } };
        var scroller = new ScrollViewer { Content = stack, IsScrollInertiaEnabled = false };
        var w = Show(scroller, height: 420);

        var row = (Control)list.ContainerFromIndex(1)!;
        foreach (var (target, name) in new (Control, string)[] { (row, "row"), (cardButton, "card"), (stat, "stat"), (chip, "chip"), (button, "button") })
        {
            scroller.Offset = default;
            Settle(80);
            Swipe(w, Center(target, w), new Vector(0, -48));
            Assert.True(scroller.Offset.Y > 20, $"{name}: the page did not scroll");
            if (name == "row") Assert.Empty(invoked);
            else Assert.True(clicks[name] == 0, $"{name}: {clicks[name]} clicks after a scroll");

            scroller.Offset = default;
            Settle(80);
            Tap(w, Center(target, w));
            if (name == "row") Assert.Equal(["Desk"], invoked);
            else Assert.True(clicks[name] == 1, $"{name}: {clicks[name]} clicks after a tap");
        }
    }

    [AvaloniaFact]
    public void A_month_chip_opens_on_a_tap_but_not_under_a_scrolling_finger()
    {
        var day = new DateTime(2026, 9, 21);
        var ev = new MonthEvent { Date = day, Title = "Review", Time = new TimeSpan(10, 0, 0), Href = "https://example.com" };
        var month = new MonthCalendar { View = MonthCalendarView.Month, Date = day, Events = [ev] };
        var opened = 0;
        var days = 0;
        month.EventInvoked += (_, _) => opened++;
        month.DayInvoked += (_, _) => days++;
        var scroller = new ScrollViewer { Content = new StackPanel { Children = { month, Filler() } }, IsScrollInertiaEnabled = false };
        var w = Show(scroller, width: 900, height: 600);
        var chip = month.ChipFor(ev)!;

        Swipe(w, Center(chip, w), new Vector(0, -48));
        Assert.True(scroller.Offset.Y > 20);
        Assert.Equal(0, opened);
        Assert.Equal(0, days);

        scroller.Offset = default;
        Settle(80);
        Tap(w, Center(chip, w));
        Assert.Equal(1, opened);
        Assert.Equal(0, days);

        // a long press shows the event's name and opens nothing
        Hold(w, Center(chip, w), () => Assert.True(ToolTip.GetIsOpen(chip)));
        Assert.Equal(1, opened);
    }

    [AvaloniaFact]
    public void A_small_button_reaches_a_finger_44_wide_and_a_mouse_finds_it_as_drawn()
    {
        var close = new Button { Classes = { "close", "sm" } };
        var clicks = 0;
        close.Click += (_, _) => clicks++;
        var w = Show(new Panel { Children = { new Border { Width = 200, Height = 200, Child = close, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top } } });
        Settle();
        close.HorizontalAlignment = HorizontalAlignment.Center;
        close.VerticalAlignment = VerticalAlignment.Center;
        Settle(200);
        var size = close.Bounds.Size;
        Assert.True(size.Width < 30 && size.Height < 30);
        var off = Center(close, w) + new Vector(size.Width / 2 + 8, 0);

        Click(close, w);
        Assert.Equal(1, clicks);
        Assert.DoesNotContain(InsiyabTheme.TouchPseudoClass, w.Classes);
        w.MouseMove(off);
        w.MouseDown(off, MouseButton.Left);
        w.MouseUp(off, MouseButton.Left);
        Settle(100);
        Assert.Equal(1, clicks);
        Assert.DoesNotContain(":pointerover", close.Classes);

        // on a phone the view is in touch from the start
        Touch.SetTouch(w, true);
        Tap(w, off);
        Assert.Equal(2, clicks);
        Tap(w, Center(close, w) + new Vector(0, size.Height / 2 + 8));
        Assert.Equal(3, clicks);
        Tap(w, Center(close, w) + new Vector(size.Width / 2 + 30, 0));
        Assert.Equal(3, clicks);
        Assert.Equal(size, close.Bounds.Size);
    }

    [AvaloniaFact]
    public void Neighbours_share_a_finger_by_who_is_nearer_and_a_field_beside_keeps_its_own()
    {
        var a = new Button { Classes = { "icon", "sm" }, Content = "A" };
        var b = new Button { Classes = { "icon", "sm" }, Content = "B" };
        var field = new TextBox { Width = 160 };
        var small = new Button { Classes = { "icon", "sm" }, Content = "C" };
        int ca = 0, cb = 0, cs = 0;
        a.Click += (_, _) => ca++;
        b.Click += (_, _) => cb++;
        small.Click += (_, _) => cs++;
        var w = Show(new StackPanel
        {
            Margin = new Thickness(40),
            Spacing = 24,
            Children =
            {
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { a, b } },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Children = { field, small } },
            },
        });
        Touch.SetTouch(w, true);
        var gapStart = a.TranslatePoint(new Point(a.Bounds.Width, a.Bounds.Height / 2), w)!.Value;
        Tap(w, gapStart + new Vector(1, 0));
        Assert.Equal((1, 0), (ca, cb));
        Tap(w, gapStart + new Vector(5, 0));
        Assert.Equal((1, 1), (ca, cb));

        var fieldEdge = field.TranslatePoint(new Point(field.Bounds.Width - 3, field.Bounds.Height / 2), w)!.Value;
        Tap(w, fieldEdge);
        Assert.Equal(0, cs);
        Assert.True(field.IsFocused);
    }

    [AvaloniaFact]
    public void A_peeking_pill_opens_on_a_tap_near_its_dot_and_closes_on_a_tap_elsewhere()
    {
        var pill = new Pill { Classes = { "ok", "dot", "peek" }, Content = "Shipped" };
        var other = new Button { Content = "Next" };
        var w = Show(new StackPanel { Margin = new Thickness(60), Spacing = 40, Children = { pill, other } });
        Touch.SetTouch(w, true);
        var near = Center(pill, w) + new Vector(pill.Bounds.Width / 2 + 9, 0);
        Tap(w, near);
        Assert.Contains(":open", pill.Classes);
        Assert.True(Part<Control>(pill, "PART_Peek").GetValue(PillPeek.IsOpenProperty));
        Tap(w, Center(other, w));
        Assert.DoesNotContain(":open", pill.Classes);
        Tap(w, Center(pill, w));
        Assert.Contains(":open", pill.Classes);
        Tap(w, Center(pill, w));
        Assert.DoesNotContain(":open", pill.Classes);
    }

    [AvaloniaFact]
    public void A_long_press_opens_the_menu_or_the_tip_and_is_not_a_tap()
    {
        var menu = new ContextMenu { ItemsSource = new[] { "Rename", "Delete" } };
        var withMenu = new Button { Content = "File", ContextMenu = menu };
        var tipped = new Button { Classes = { "icon" }, Content = "⋯" };
        ToolTip.SetTip(tipped, "More");
        var plain = new Button { Content = "Go" };
        int menuClicks = 0, tipClicks = 0, plainClicks = 0;
        withMenu.Click += (_, _) => menuClicks++;
        tipped.Click += (_, _) => tipClicks++;
        plain.Click += (_, _) => plainClicks++;
        var w = Show(new StackPanel { Margin = new Thickness(40), Spacing = 40, Children = { withMenu, tipped, plain } });

        Hold(w, Center(withMenu, w), () => Assert.True(menu.IsOpen));
        Assert.True(menu.IsOpen);
        Assert.Equal(0, menuClicks);
        menu.Close();
        Settle(100);

        Hold(w, Center(tipped, w), () => Assert.True(ToolTip.GetIsOpen(tipped)));
        Assert.Equal(0, tipClicks);
        Assert.True(ToolTip.GetIsOpen(tipped));
        Settle((int)Touch.TipFor.TotalMilliseconds + 300);
        Assert.False(ToolTip.GetIsOpen(tipped));

        // a long press with nothing to show is still a press
        Hold(w, Center(plain, w));
        Assert.Equal(1, plainClicks);
    }

    [AvaloniaFact]
    public void A_long_press_on_a_row_with_a_menu_opens_it_and_does_not_open_the_row()
    {
        var invoked = 0;
        var menu = new ContextMenu { ItemsSource = new[] { "Share" } };
        var list = new ListBox { ItemsSource = new[] { "Lamp", "Desk" }, ContextMenu = menu, ItemTemplate = new FuncDataTemplate<string>((n, _) => new TextBlock { Classes = { "list-title" }, Text = n }) };
        ListRow.AddInvokedHandler(list, (_, _) => invoked++);
        var w = Show(list);
        var row = (Control)list.ContainerFromIndex(0)!;
        Hold(w, Center(row, w));
        Assert.True(menu.IsOpen);
        Assert.Equal(0, invoked);
        menu.Close();
        Tap(w, Center(row, w));
        Assert.Equal(1, invoked);
    }

    [AvaloniaFact]
    public void Text_takes_a_word_on_a_long_press_and_a_finger_drag_does_not_select()
    {
        var field = new TextBox { Text = "fresh green tea", Width = 260 };
        var text = new SelectableTextBlock { Text = "alpha beta gamma", FontSize = 20 };
        var rich = new RichText { Text = "one two three\n\nfour five six" };
        var w = Show(new StackPanel { Margin = new Thickness(24), Spacing = 24, Children = { field, text, rich } }, width: 520);

        Hold(w, Center(field, w));
        Assert.NotEqual(field.SelectionStart, field.SelectionEnd);
        Assert.True(field.ContextFlyout?.IsOpen);
        field.ContextFlyout!.Hide();
        Settle(100);

        var layout = text.TextLayout;
        var beta = layout.HitTestTextPosition(8);
        var onBeta = text.TranslatePoint(new Point(beta.X + 2, beta.Y + beta.Height / 2), w)!.Value;
        Swipe(w, text.TranslatePoint(new Point(2, text.Bounds.Height / 2), w)!.Value, new Vector(120, 0));
        Assert.Equal(text.SelectionStart, text.SelectionEnd);
        Hold(w, onBeta);
        Assert.Equal("beta", text.SelectedText);
        Assert.True(text.ContextFlyout?.IsOpen);
        Assert.True(text.CanCopy);
        text.ContextFlyout!.Hide();
        Settle(100);

        var para = rich.GetVisualDescendants().OfType<SelectableTextBlock>().Last(b => (b.Text ?? b.Inlines?.Text ?? "").Contains("five"));
        var five = para.TextLayout.HitTestTextPosition(6);
        Hold(w, para.TranslatePoint(new Point(five.X + 2, five.Y + five.Height / 2), w)!.Value);
        Assert.Equal("five", para.SelectedText);
    }

    [AvaloniaFact]
    public void A_swipe_that_starts_on_text_scrolls_the_page_and_selects_nothing()
    {
        var text = new SelectableTextBlock { Text = "alpha beta gamma delta", FontSize = 20 };
        var rich = new RichText { Text = "one two three\n\nfour five six" };
        var scroller = new ScrollViewer { Content = new StackPanel { Margin = new Thickness(24), Spacing = 24, Children = { text, rich, Filler() } }, IsScrollInertiaEnabled = false };
        var w = Show(scroller, width: 520, height: 400);

        Swipe(w, text.TranslatePoint(new Point(10, text.Bounds.Height / 2), w)!.Value, new Vector(30, -60));
        Assert.True(scroller.Offset.Y > 20);
        Assert.Equal(text.SelectionStart, text.SelectionEnd);

        scroller.Offset = default;
        Settle(80);
        Swipe(w, Center(rich, w), new Vector(0, -60));
        Assert.True(scroller.Offset.Y > 20);
        Assert.All(rich.GetVisualDescendants().OfType<SelectableTextBlock>(), b => Assert.Equal(b.SelectionStart, b.SelectionEnd));
    }

    [AvaloniaFact]
    public void The_media_bar_follows_a_finger_along_it_and_lets_a_vertical_swipe_scroll()
    {
        var bar = new MediaBar { Width = 300, Duration = TimeSpan.FromMinutes(10) };
        var seeks = new List<(double Value, bool Dragging)>();
        bar.SeekRequested += (_, e) => seeks.Add((e.Value, e.IsDragging));
        var scroller = new ScrollViewer { Content = new StackPanel { Margin = new Thickness(40), Children = { bar, Filler() } }, IsScrollInertiaEnabled = false };
        var w = Show(scroller, width: 420, height: 400);
        var left = bar.TranslatePoint(new Point(30, bar.Bounds.Height / 2), w)!.Value;

        Swipe(w, left, new Vector(150, 9));
        Assert.Equal(0, scroller.Offset.Y);
        Assert.Contains(seeks, s => s.Dragging);
        Assert.False(seeks[^1].Dragging);
        Assert.Equal(0.6, seeks[^1].Value, 1);

        seeks.Clear();
        Swipe(w, left, new Vector(0, -60));
        Assert.True(scroller.Offset.Y > 20);
        Assert.Empty(seeks);

        scroller.Offset = default;
        Settle(80);
        var above = left + new Vector(120, -bar.Bounds.Height / 2 - 6);
        Tap(w, above);
        Assert.Single(seeks);
        Assert.Equal(0.5, seeks[0].Value, 1);
    }

    [AvaloniaFact]
    public void A_slider_keeps_a_finger_that_drags_along_it_and_gives_back_a_swipe()
    {
        var slider = new Slider { Width = 240, Minimum = 0, Maximum = 100, Value = 20 };
        var scroller = new ScrollViewer { Content = new StackPanel { Margin = new Thickness(40), Children = { slider, Filler() } }, IsScrollInertiaEnabled = false };
        var w = Show(scroller, width: 420, height: 400);
        var start = slider.TranslatePoint(new Point(slider.Bounds.Width * 0.2, slider.Bounds.Height / 2), w)!.Value;

        Swipe(w, start, new Vector(96, 10), steps: 12);
        Assert.Equal(0, scroller.Offset.Y);
        Assert.True(slider.Value > 50, $"value {slider.Value}");

        slider.Value = 20;
        Settle(80);
        var mid = slider.TranslatePoint(new Point(slider.Bounds.Width * 0.7, slider.Bounds.Height / 2), w)!.Value;
        Swipe(w, mid, new Vector(0, -60));
        Assert.True(scroller.Offset.Y > 20);
        Assert.Equal(20, slider.Value, 1);
    }

    [AvaloniaFact]
    public void A_finger_drags_the_split_grip_and_the_column_edge_from_beside_them()
    {
        var split = new Split { Width = 800, Height = 200, First = new TextBlock { Text = "Inbox" }, Second = new TextBlock { Text = "Message" } };
        var w = Show(split, width: 840, height: 240);
        Touch.SetTouch(w, true);
        var handle = Part<Control>(split, "PART_Handle");
        var share = split.Share;
        var beside = Center(handle, w) + new Vector(handle.Bounds.Width / 2 + 10, 0);
        Swipe(w, beside, new Vector(-90, 0));
        Assert.True(split.Share < share - 5, $"share {split.Share}");

        var table = new Table();
        table.Columns.AddRange([new TableColumn { Header = "Item" }, new TableColumn { Header = "Place" }, new TableColumn { Header = "Count" }]);
        foreach (var (item, place, count) in new[] { ("Lamp", "Hall", "3"), ("Desk", "Office", "1") })
        {
            var row = new TableRow();
            foreach (var c in new[] { item, place, count }) row.Cells.Add(new TableCell { Content = c });
            table.Rows.Add(row);
        }
        var tw = Show(table, width: 760);
        Touch.SetTouch(tw, true);
        var head = (Control)table.GetVisualDescendants().OfType<TableHeader>().First(h => h.Column is not null);
        var grip = Part<Border>(head, "PART_Grip");
        var width = head.Bounds.Width;
        var inside = grip.TranslatePoint(new Point(-8, grip.Bounds.Height / 2), tw)!.Value;
        Swipe(tw, inside, new Vector(50, 0));
        Assert.Equal(width + 50, head.Bounds.Width, 1);
    }
}
