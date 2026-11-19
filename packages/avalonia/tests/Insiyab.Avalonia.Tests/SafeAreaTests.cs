using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Notifications;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A phone's covered edges, set by the test.
internal sealed class FakeInsets : IInsetsSource
{
    public Thickness SafeArea { get; private set; }
    public Rect Keyboard { get; private set; }
    public event EventHandler? Changed;

    public void Set(Thickness safe, Rect keyboard = default)
    {
        SafeArea = safe;
        Keyboard = keyboard;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // Every TopLevel made while the action runs reads this source.
    public static void With(FakeInsets fake, Action action)
    {
        Insets.SourceFor = _ => fake;
        try { action(); }
        finally { Insets.SourceFor = null; }
    }
}

public class SafeAreaTests
{
    private static readonly Thickness Phone = new(10, 24, 12, 30);

    [AvaloniaFact]
    public void Edges_add_the_safe_area_to_the_padding_and_follow_a_rotation()
    {
        var fake = new FakeInsets();
        fake.Set(Phone);
        FakeInsets.With(fake, () =>
        {
            var bar = new Border { Padding = new Thickness(8), Child = new TextBlock { Text = "شريط" } };
            SafeArea.SetEdges(bar, SafeAreaEdges.Top | SafeAreaEdges.Bottom);
            Show(new Panel { Children = { bar } }, width: 390, height: 700);
            Assert.Equal(new Thickness(8, 32, 8, 38), bar.Padding);
            Assert.Equal(Phone, SafeArea.Of(bar));

            fake.Set(new Thickness(44, 0, 44, 21));
            Settle(100);
            Assert.Equal(new Thickness(8, 8, 8, 29), bar.Padding);

            SafeArea.SetEdges(bar, SafeAreaEdges.All);
            Assert.Equal(new Thickness(52, 8, 52, 29), bar.Padding);

            SafeArea.SetEdges(bar, SafeAreaEdges.None);
            Assert.Equal(new Thickness(8), bar.Padding);
        });
    }

    [AvaloniaFact]
    public void Left_and_right_stay_on_the_screen_sides_in_right_to_left()
    {
        var fake = new FakeInsets();
        fake.Set(new Thickness(40, 0, 0, 0));
        FakeInsets.With(fake, () =>
        {
            var bar = new Border { Child = new TextBlock { Text = "شريط" } };
            SafeArea.SetEdges(bar, SafeAreaEdges.Left);
            var window = Show(new Panel { Children = { bar } }, FlowDirection.RightToLeft, width: 700, height: 390);
            Assert.Equal(new Thickness(0, 0, 40, 0), bar.Padding);
            var text = (TextBlock)bar.Child!;
            var a = text.TranslatePoint(default, window)!.Value;
            var b = text.TranslatePoint(new Point(text.Bounds.Width, 0), window)!.Value;
            Assert.True(Math.Min(a.X, b.X) >= 40 - 0.5, $"the text starts at {Math.Min(a.X, b.X)}");
        });
    }

    [AvaloniaFact]
    public void An_element_without_padding_takes_the_safe_area_as_margin_and_an_app_padding_is_kept()
    {
        var fake = new FakeInsets();
        fake.Set(Phone);
        FakeInsets.With(fake, () =>
        {
            var row = new StackPanel { Margin = new Thickness(4) };
            SafeArea.SetEdges(row, SafeAreaEdges.Bottom);
            var box = new ContentControl { Padding = new Thickness(16), Content = "محتوى" };
            SafeArea.SetEdges(box, SafeAreaEdges.Top);
            Show(new StackPanel { Children = { row, box } });
            Assert.Equal(new Thickness(4, 4, 4, 34), row.Margin);
            Assert.Equal(new Thickness(16, 40, 16, 16), box.Padding);
        });
    }

    [AvaloniaFact]
    public void A_desktop_window_is_never_padded()
    {
        var bar = new Border { Padding = new Thickness(8) };
        SafeArea.SetEdges(bar, SafeAreaEdges.All | SafeAreaEdges.Keyboard);
        Show(new Panel { Children = { bar } });
        Assert.Equal(new Thickness(8), bar.Padding);
        Assert.Equal(default, SafeArea.Of(bar));
    }

    [AvaloniaFact]
    public void A_bar_at_the_foot_rises_above_the_keyboard()
    {
        var fake = new FakeInsets();
        fake.Set(Phone);
        FakeInsets.With(fake, () =>
        {
            var bar = new Border { VerticalAlignment = VerticalAlignment.Bottom, Child = new TextBox { Text = "رسالة" } };
            SafeArea.SetEdges(bar, SafeAreaEdges.Bottom | SafeAreaEdges.Keyboard);
            Show(new Panel { Children = { bar } }, width: 390, height: 700);
            Assert.Equal(30, bar.Padding.Bottom);

            fake.Set(Phone, new Rect(0, 400, 390, 300));
            Settle(100);
            Assert.Equal(300, bar.Padding.Bottom);
            var box = (TextBox)bar.Child!;
            Assert.True(box.TranslatePoint(new Point(0, box.Bounds.Height), bar)!.Value.Y <= bar.Bounds.Height - 300 + 0.5);

            fake.Set(Phone);
            Settle(100);
            Assert.Equal(30, bar.Padding.Bottom);
        });
    }

    [AvaloniaFact]
    public void A_dialog_and_a_bottom_drawer_keep_clear_of_the_bars_and_the_keyboard()
    {
        var fake = new FakeInsets();
        fake.Set(Phone, new Rect(0, 420, 390, 280));
        FakeInsets.With(fake, () =>
        {
            var opener = new Button { Content = "فتح" };
            var window = Show(new StackPanel { Children = { opener } }, width: 390, height: 700);
            var dialog = new Dialog { Title = "عنوان", Content = new TextBox { Text = "حقل" } };
            dialog.Open(opener);
            Settle();
            var box = Box(dialog, window);
            Assert.True(box.Top >= 24 - 0.5 && box.Bottom <= 420 + 0.5, $"dialog at {box}");
            dialog.Close();
            Settle();

            var drawer = new Dialog { Title = "درج", Classes = { "drawer", "bottom" }, Content = new TextBlock { Text = "محتوى" } };
            drawer.Open(opener);
            Settle();
            box = Box(drawer, window);
            Assert.Equal(420, box.Bottom, 1);

            fake.Set(Phone);
            Settle();
            Assert.Equal(700 - 30, Box(drawer, window).Bottom, 1);
            drawer.Close();
            Settle();
        });
    }

    // A side drawer on a phone or a tablet runs the screen's whole height, as Material's side sheet: under the status bar and the navigation bar, its head and its content kept between them (it ended above the navigation bar's band, cut).
    [AvaloniaFact]
    public void A_side_drawer_reaches_the_top_and_the_foot_with_its_content_between_the_bars()
    {
        var fake = new FakeInsets();
        fake.Set(new Thickness(0, 24, 0, 32));
        FakeInsets.With(fake, () =>
        {
            var opener = new Button { Content = "فتح" };
            var window = Show(new StackPanel { Children = { opener } }, FlowDirection.RightToLeft, width: 800, height: 700);
            var body = new TextBlock { Text = "محتوى" };
            var drawer = new Dialog { Title = "درج", Classes = { "drawer", "end" }, Content = body };
            drawer.Open(opener);
            Settle(400);
            var box = Box(drawer, window);
            Assert.Equal(0, box.Top, 1);
            Assert.Equal(700, box.Bottom, 1);
            var head = drawer.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Head");
            Assert.True(Box(head, window).Top >= 24 - 0.5, $"head at {Box(head, window)}");
            drawer.Close();
            Settle();
        });
    }

    [AvaloniaFact]
    public void A_toast_keeps_clear_of_the_gesture_bar()
    {
        var fake = new FakeInsets();
        fake.Set(Phone);
        FakeInsets.With(fake, () =>
        {
            var anchor = new Button { Content = "زر" };
            Show(new StackPanel { Children = { anchor } }, FlowDirection.RightToLeft, width: 390, height: 700);
            Toast.Show(anchor, "تم الحفظ", ToastTone.Ok);
            Settle();
            var manager = TopLevel.GetTopLevel(anchor)!.GetVisualDescendants().OfType<WindowNotificationManager>().Single();
            Assert.Equal(new Thickness(12, 24, 10, 30), manager.Margin);
        });
    }

    [AvaloniaFact]
    public void A_focused_field_in_a_scroller_scrolls_above_the_keyboard_and_the_room_goes_with_it()
    {
        var fake = new FakeInsets();
        fake.Set(default);
        FakeInsets.With(fake, () =>
        {
            var fields = new StackPanel { Spacing = 8 };
            for (var i = 0; i < 10; i++) fields.Children.Add(new TextBox { Text = $"حقل {i + 1}", Height = 40 });
            var scroller = new ScrollViewer { Content = fields };
            var window = Show(scroller, width: 390, height: 700);
            var last = (TextBox)fields.Children[^1];
            last.Focus();
            Settle(100);

            fake.Set(default, new Rect(0, 350, 390, 350));
            Settle();
            var bottom = last.TranslatePoint(new Point(0, last.Bounds.Height), window)!.Value.Y;
            Assert.True(bottom <= 350 - KeyboardRoom.Gap + 0.5, $"the field ends at {bottom}");
            Assert.Equal(350 + KeyboardRoom.Gap, scroller.Padding.Bottom, 1);

            fake.Set(default);
            Settle();
            Assert.Equal(default, scroller.Padding);
        });
    }

    [AvaloniaFact]
    public void A_pop_menu_turns_above_its_trigger_rather_than_open_under_the_keyboard()
    {
        var fake = new FakeInsets();
        fake.Set(Phone);
        FakeInsets.With(fake, () =>
        {
            var menu = new PopMenu { Items = { new MenuItem { Header = "تعديل" }, new MenuItem { Header = "نسخ" }, new MenuItem { Header = "نقل" }, new MenuItem { Header = "مشاركة" }, new MenuItem { Header = "حذف" } } };
            var trigger = new Button { Content = "إجراءات", Flyout = menu, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(24, 300, 0, 0) };
            var window = Show(new Panel { Children = { trigger } }, width: 390, height: 700);
            Click(trigger, window);
            var card = Part<Border>((Control)menu.Popup.Child!, "LayoutRoot");
            Assert.True(Box(card, window).Top > Box(trigger, window).Bottom, "with no keyboard it opens below");
            menu.Hide();
            Settle();

            fake.Set(Phone, new Rect(0, 420, 390, 280));
            Click(trigger, window);
            card = Part<Border>((Control)menu.Popup.Child!, "LayoutRoot");
            var box = Box(card, window);
            Assert.True(box.Bottom < Box(trigger, window).Top, $"the menu at {box} stays over its trigger");
            Assert.True(box.Top >= 24, $"the menu at {box} clears the status bar");
            menu.Hide();
            Settle();
        });
    }

    private static Rect Box(Control c, Visual root)
    {
        var a = c.TranslatePoint(default, root)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), root)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaFact]
    public void The_status_band_fills_the_top_inset_a_step_darker_than_the_ground_over_everything()
    {
        var fake = new FakeInsets();
        fake.Set(new Thickness(0, 24, 0, 20));
        FakeInsets.With(fake, () =>
        {
            var view = new Panel();
            SafeArea.SetStatusBand(view, true);
            var window = Show(view, width: 390, height: 700);
            var band = StatusBand.Of(window);
            Assert.NotNull(band);
            Assert.False(band!.IsHitTestVisible);
            Assert.True(band.ZIndex > 1, "over the dialogs and the toasts");
            Assert.True(band.TryFindResource("InsWash1Color", band.ActualThemeVariant, out var lightFound) && lightFound is Color);
            var wash = (Color)lightFound!;
            var light = band.Shade();
            Assert.True(light.R + light.G + light.B < wash.R + wash.G + wash.B, "darker than the ground in light");
            Assert.True(light.R + light.G + light.B > wash.R + wash.G + wash.B - 60, "a step darker, not a dark bar");

            window.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
            Settle(50);
            Assert.Equal(global::Avalonia.Styling.ThemeVariant.Dark, band.ActualThemeVariant);
            Assert.True(band.TryFindResource("InsWash1Color", band.ActualThemeVariant, out var found) && found is Color);
            var darkWash = (Color)found!;
            var dark = band.Shade();
            Assert.True(dark.R + dark.G + dark.B < darkWash.R + darkWash.G + darkWash.B, "darker than the dark ground");

            SafeArea.SetStatusBand(view, false);
            Assert.False(band.IsVisible);
        });
    }
}
