using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class OverlaysTests
{
    private static Dialog MakeDialog(string? classes = null, bool foot = true, DialogClosedBy closedBy = DialogClosedBy.Auto)
    {
        var dialog = new Dialog { Title = "عنوان الحوار", Content = new TextBlock { Text = "نص الحوار." }, ClosedBy = closedBy };
        if (classes is not null) dialog.Classes.AddRange(classes.Split(' '));
        if (foot)
        {
            var no = new Button { Content = "إلغاء", Classes = { "bare" } };
            var yes = new Button { Content = "موافق", Classes = { "primary" } };
            Dialog.SetResult(no, "cancel");
            Dialog.SetResult(yes, "ok");
            dialog.Foot = new DialogFoot { Children = { no, yes } };
        }
        return dialog;
    }

    private static (Window Window, Button Opener) Host(Dialog dialog, FlowDirection dir = FlowDirection.LeftToRight, double width = 900, double height = 700, ThemeVariant? theme = null)
    {
        var opener = new Button { Content = "فتح الحوار", Classes = { "primary" } };
        Dialog.SetOpens(opener, dialog);
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { opener } }, dir, theme, width, height);
        return (window, opener);
    }

    private static DialogLayer Layer(Dialog dialog) => DialogStack.Of(dialog)!.Layers.Single(l => l.Dialog == dialog);

    // Settle, with an empty job each pass: the headless dispatcher only promotes due timers after running a job.
    private static void Tick(int milliseconds)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => { });
            Settle(16);
        }
    }

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle();
    }

    private static void ClickAt(Window window, Point p)
    {
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Settle();
    }

    private static void Hover(Window window, Control c)
    {
        window.MouseMove(Center(c, window));
        Settle(100);
    }

    // Physical bounds: under RTL mirroring a control's origin lands on its right edge.
    private static Rect BoundsIn(Control c, Visual root)
    {
        var a = c.TranslatePoint(default, root)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), root)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static void Pump(TopLevel root, int milliseconds = 750)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            root.InvalidateVisual();
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(8);
        }
    }

    // A single-view TopLevel, as Android and iOS host an app: an EmbeddableControlRoot on a headless surface borrowed from a window that is never shown.
    private static EmbeddableControlRoot SingleView(Control content, double width = 400, double height = 700, FlowDirection dir = FlowDirection.RightToLeft)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var donor = new Window();
        var impl = typeof(TopLevel).GetProperty("PlatformImpl", flags)!.GetValue(donor)!;
        var root = (EmbeddableControlRoot)typeof(EmbeddableControlRoot).GetConstructors(flags).First(c => c.GetParameters().Length == 1).Invoke([impl]);
        root.Width = width;
        root.Height = height;
        root.FlowDirection = dir;
        root.Content = content;
        root.Prepare();
        root.StartRendering();
        Pump(root);
        return root;
    }

    private static void PressOn(TopLevel root, Key key)
    {
        root.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        root.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Pump(root, 400);
    }

    [AvaloniaFact]
    public void Dialog_opens_on_a_scrim_with_its_parts()
    {
        var dialog = MakeDialog();
        var (window, opener) = Host(dialog);
        Click(opener, window);

        Assert.True(dialog.IsOpen);
        Assert.Contains(":open", dialog.Classes);
        var layer = Layer(dialog);
        Assert.Equal(1, layer.Scrim.Opacity, 2);
        Assert.Equal(Color.Parse("#610F172A"), ColorOf(layer.Scrim.Background));
        Assert.Equal(1, dialog.Opacity, 2);
        Assert.True(Part<Border>(dialog, "PART_Head").IsVisible);
        Assert.True(Part<Border>(dialog, "PART_Foot").IsVisible);
        var cross = Part<Button>(dialog, "PART_Close");
        Assert.True(cross.IsVisible);
        Assert.Contains("close", cross.Classes);
        Assert.Equal(30, cross.Bounds.Width, 1);
        Assert.Equal(30, cross.Bounds.Height, 1);
        Assert.Equal(Res(dialog, "InsRXl"), Part<Border>(dialog, "PART_Fill").CornerRadius);
        Assert.Equal(Res(dialog, "InsShLg"), Part<Border>(dialog, "PART_Ground").BoxShadow);
        AssertBrush(dialog, "InsLine", () => Part<Border>(dialog, "PART_Head").BorderBrush);
        Assert.Equal(new Thickness(24, 18), Part<Border>(dialog, "PART_Head").Padding);
        Assert.Equal(new Thickness(24, 16), Part<Border>(dialog, "PART_Foot").Padding);

        var box = BoundsIn(dialog, window);
        Assert.Equal(608, box.Width, 1);
        Assert.Equal(450, box.Center.X, 1);
        Assert.Equal(350, box.Center.Y, 1);
    }

    [AvaloniaFact]
    public void Dialog_sizes_follow_the_classes_and_the_window()
    {
        foreach (var (classes, width, expected) in new[] { ("sm", 900.0, 416.0), ("lg", 1200.0, 896.0), ("lg", 700.0, 668.0), ((string?)null, 400.0, 368.0) })
        {
            var dialog = MakeDialog(classes);
            var (window, opener) = Host(dialog, width: width);
            Click(opener, window);
            Assert.Equal(expected, dialog.Bounds.Width, 1);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Head_and_foot_hide_without_a_title_or_a_foot()
    {
        var dialog = MakeDialog(foot: false);
        dialog.Title = null;
        var (window, opener) = Host(dialog);
        Click(opener, window);
        Assert.False(Part<Border>(dialog, "PART_Head").IsVisible);
        Assert.False(Part<Border>(dialog, "PART_Foot").IsVisible);
    }

    [AvaloniaFact]
    public void Escape_closes_the_dialog_and_focus_returns_to_the_opener()
    {
        var dialog = MakeDialog();
        var (window, opener) = Host(dialog);
        opener.Focus();
        Click(opener, window);
        Assert.True(dialog.IsOpen);
        Assert.Equal(((DialogFoot)dialog.Foot!).Children[1], window.FocusManager!.GetFocusedElement());

        Press(window, Key.Escape);
        Assert.False(dialog.IsOpen);
        Assert.Equal(opener, window.FocusManager!.GetFocusedElement());
        Assert.Empty(DialogStack.Of(opener)!.Children);
    }

    [AvaloniaFact]
    public void Tab_stays_inside_the_dialog()
    {
        var dialog = MakeDialog();
        var (window, opener) = Host(dialog);
        Click(opener, window);
        Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(dialog));
        var seen = new List<object?>();
        for (var i = 0; i < 6; i++)
        {
            Press(window, Key.Tab);
            var focused = window.FocusManager!.GetFocusedElement() as Visual;
            Assert.True(focused is not null && dialog.IsVisualAncestorOf(focused), "focus left the dialog");
            seen.Add(focused);
        }
        Assert.Equal(3, seen.Distinct().Count());
    }

    [AvaloniaFact]
    public void An_outside_press_closes_only_what_closedby_allows()
    {
        var plain = MakeDialog();
        var (window, opener) = Host(plain);
        Click(opener, window);
        ClickAt(window, new Point(20, 20));
        Assert.True(plain.IsOpen);
        plain.Close();
        Settle();

        var any = MakeDialog(closedBy: DialogClosedBy.Any);
        any.Open(opener);
        Settle();
        ClickAt(window, new Point(20, 20));
        Assert.False(any.IsOpen);

        var none = MakeDialog(closedBy: DialogClosedBy.None);
        none.Open(opener);
        Settle();
        Press(window, Key.Escape);
        ClickAt(window, new Point(20, 20));
        Assert.True(none.IsOpen);
        Click(Part<Button>(none, "PART_Close"), window);
        Assert.False(none.IsOpen);
    }

    [AvaloniaFact]
    public async Task ShowAsync_returns_the_dismissing_buttons_value()
    {
        var dialog = MakeDialog();
        var (window, opener) = Host(dialog);
        var result = dialog.ShowAsync(opener);
        Settle();
        var ok = ((DialogFoot)dialog.Foot!).Children.OfType<Button>().Last();
        Click(ok, window);
        Assert.True(result.IsCompleted);
        Assert.Equal("ok", await result);

        var again = dialog.ShowAsync(opener);
        Settle();
        Press(window, Key.Escape);
        Assert.Null(await again);
    }

    [AvaloniaFact]
    public void A_dialog_over_a_dialog_dims_once_and_escape_closes_the_top_one()
    {
        var first = MakeDialog();
        var second = MakeDialog("sm");
        var (window, opener) = Host(first);
        Click(opener, window);
        var inner = new Button { Content = "التالي" };
        Dialog.SetOpens(inner, second);
        first.Content = inner;
        Settle();
        Click(inner, window);

        Assert.True(second.IsOpen);
        Assert.Equal(0, Layer(first).Scrim.Opacity, 2);
        Assert.Equal(1, Layer(second).Scrim.Opacity, 2);
        Press(window, Key.Escape);
        Assert.False(second.IsOpen);
        Assert.True(first.IsOpen);
        Assert.Equal(1, Layer(first).Scrim.Opacity, 2);
        Assert.Equal(inner, window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaTheory]
    [InlineData("drawer", "ltr", 0.0)]
    [InlineData("drawer", "rtl", 516.0)]
    [InlineData("drawer end", "ltr", 516.0)]
    [InlineData("drawer end", "rtl", 0.0)]
    public void Drawers_hug_their_edge(string classes, string dir, double x)
    {
        var dialog = MakeDialog(classes);
        var (window, opener) = Host(dialog, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        Click(opener, window);
        var box = BoundsIn(dialog, window);
        Assert.Equal(384, box.Width, 1);
        Assert.Equal(700, box.Height, 1);
        Assert.Equal(x, box.X, 1);
        Assert.Equal(DialogClosedBy.Any, dialog.EffectiveClosedBy);
        var fill = Part<Border>(dialog, "PART_Fill");
        Assert.Equal(classes.Contains("end") ? new Thickness(1, 0, 0, 0) : new Thickness(0, 0, 1, 0), fill.BorderThickness);

        ClickAt(window, new Point(x == 0 ? 800 : 100, 350));
        Assert.False(dialog.IsOpen);
    }

    [AvaloniaFact]
    public void The_bottom_sheet_spans_the_width_at_the_bottom()
    {
        var was = WindowCorner.Os;
        try
        {
            WindowCorner.Os = 8;
            var dialog = MakeDialog("drawer bottom", foot: false);
            var (window, opener) = Host(dialog);
            Click(opener, window);
            var box = BoundsIn(dialog, window);
            Assert.Equal(900, box.Width, 1);
            Assert.Equal(700, box.Bottom, 1);
            Assert.True(box.Height < 300);
            Assert.Equal(new CornerRadius(8, 8, 0, 0), Part<Border>(dialog, "PART_Fill").CornerRadius);
        }
        finally
        {
            WindowCorner.Os = was;
        }
    }

    [AvaloniaTheory]
    [InlineData("drawer", 8.0)]
    [InlineData("drawer end", 8.0)]
    [InlineData("drawer bottom", 8.0)]
    [InlineData("drawer", 0.0)]
    public void Drawer_corners_follow_the_window(string classes, double r)
    {
        var was = WindowCorner.Os;
        try
        {
            WindowCorner.Os = r;
            var dialog = MakeDialog(classes, foot: false);
            var (window, opener) = Host(dialog);
            Click(opener, window);
            var fill = Part<Border>(dialog, "PART_Fill");
            var want = classes.Contains("bottom") ? new CornerRadius(r, r, 0, 0) : classes.Contains("end") ? new CornerRadius(r, 0, 0, r) : new CornerRadius(0, r, r, 0);
            Assert.Equal(want, fill.CornerRadius);
            window.WindowState = WindowState.Maximized;
            Settle(100);
            Assert.Equal(new CornerRadius(0), fill.CornerRadius);
            window.WindowState = WindowState.Normal;
            Settle(100);
            Assert.Equal(want, fill.CornerRadius);
        }
        finally
        {
            WindowCorner.Os = was;
        }
    }

    [AvaloniaFact]
    public void A_drawer_in_a_single_view_keeps_its_large_corners()
    {
        var was = WindowCorner.Os;
        var dialog = MakeDialog("drawer", foot: false);
        var opener = new Button { Content = "فتح الحوار" };
        var root = SingleView(new StackPanel { Margin = new Thickness(16), Children = { opener } });
        try
        {
            WindowCorner.Os = 8;
            dialog.Open(opener);
            Pump(root);
            Assert.True(dialog.IsOpen);
            Assert.Equal(new CornerRadius(0, 32, 32, 0), Part<Border>(dialog, "PART_Fill").CornerRadius);
        }
        finally
        {
            WindowCorner.Os = was;
            root.StopRendering();
        }
    }

    [AvaloniaFact]
    public void A_narrow_drawer_takes_88_percent()
    {
        var dialog = MakeDialog("drawer");
        var (window, opener) = Host(dialog, width: 400);
        Click(opener, window);
        Assert.Equal(352, dialog.Bounds.Width, 1);
    }

    [AvaloniaFact]
    public void Foot_actions_sit_at_the_end_with_the_footer_standard()
    {
        var dialog = MakeDialog();
        var del = new Button { Content = "حذف", Classes = { "ghost-danger", "me-auto" } };
        ((DialogFoot)dialog.Foot!).Children.Insert(0, del);
        var (window, opener) = Host(dialog);
        Click(opener, window);
        var foot = (DialogFoot)dialog.Foot!;
        var buttons = foot.Children.OfType<Button>().ToList();
        var no = BoundsIn(buttons[1], foot);
        var yes = BoundsIn(buttons[2], foot);
        var start = BoundsIn(buttons[0], foot);

        Assert.Equal(foot.Bounds.Width, yes.Right, 1);
        Assert.Equal(12, yes.X - no.Right, 1);
        Assert.Equal(0, start.X, 1);
        Assert.All(buttons, b => Assert.True(b.Bounds.Width >= 80));
        Assert.Equal(new Thickness(16, 6), buttons[1].Padding);
        Assert.Equal(38, buttons[1].MinHeight);
        Assert.False(foot.IsPhone);
    }

    [AvaloniaFact]
    public void Foot_actions_share_the_width_on_a_phone()
    {
        var dialog = MakeDialog();
        var (window, opener) = Host(dialog, width: 400);
        Click(opener, window);
        var foot = (DialogFoot)dialog.Foot!;
        Assert.True(foot.IsPhone);
        Assert.Contains(":phone", foot.Classes);
        var buttons = foot.Children.OfType<Button>().ToList();
        Assert.Equal(buttons[0].Bounds.Width, buttons[1].Bounds.Width, 1);
        Assert.Equal(foot.Bounds.Width, buttons[0].Bounds.Width * 2 + 12, 1);
        Assert.All(buttons, b => Assert.Equal(46, b.Bounds.Height, 1));
    }

    [AvaloniaFact]
    public void Confirm_holds_the_click_until_the_answer_is_yes()
    {
        var clicks = 0;
        var button = new Button { Content = "أرشفة", Classes = { "secondary" } };
        Dialog.SetConfirm(button, "أرشفة الملف؟");
        Dialog.SetConfirmOk(button, "أرشفة");
        button.Click += (_, _) => clicks++;
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);

        Click(button, window);
        Assert.Equal(0, clicks);
        var dialog = DialogStack.Of(button)!.Layers.Single().Dialog;
        Assert.Contains("sm", dialog.Classes);
        Assert.Equal(416, dialog.Bounds.Width, 1);
        var foot = (DialogFoot)dialog.Foot!;
        var no = (Button)foot.Children[0];
        var yes = (Button)foot.Children[1];
        Assert.Equal("إلغاء", no.Content);
        Assert.Equal("أرشفة", yes.Content);
        Assert.Contains("primary", yes.Classes);
        Assert.Equal(no, window.FocusManager!.GetFocusedElement());
        Click(no, window);
        Assert.Equal(0, clicks);

        Click(button, window);
        yes = (Button)((DialogFoot)DialogStack.Of(button)!.Layers.Single().Dialog.Foot!).Children[1];
        Click(yes, window);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void Confirm_on_a_danger_button_answers_in_red()
    {
        var button = new Button { Content = "حذف", Classes = { "ghost-danger" } };
        Dialog.SetConfirm(button, "حذف الملف؟");
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, FlowDirection.LeftToRight, width: 900, height: 600);
        Click(button, window);
        var dialog = DialogStack.Of(button)!.Layers.Single().Dialog;
        var yes = (Button)((DialogFoot)dialog.Foot!).Children[1];
        Assert.Contains("danger", yes.Classes);
        Assert.Equal("OK", yes.Content);
        Assert.Contains("dialog-title", ((TextBlock)dialog.Content!).Classes);
    }

    [AvaloniaFact]
    public void Confirm_danger_on_a_plain_button_answers_in_red()
    {
        var button = new Button { Content = "نقل", Classes = { "secondary" } };
        Dialog.SetConfirm(button, "نقل الملف؟");
        Dialog.SetConfirmDanger(button, true);
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, FlowDirection.LeftToRight, width: 900, height: 600);
        Click(button, window);
        var yes = (Button)((DialogFoot)DialogStack.Of(button)!.Layers.Single().Dialog.Foot!).Children[1];
        Assert.Contains("danger", yes.Classes);
        Assert.DoesNotContain("primary", yes.Classes);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Toasts_stack_in_the_bottom_end_corner_in_their_tones(string dir)
    {
        var rtl = dir == "rtl";
        var button = new Button { Content = "نجاح" };
        Toast.SetText(button, "تم الحفظ");
        Toast.SetTone(button, ToastTone.Ok);
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 900, height: 600);
        Click(button, window);
        Toast.Show(button, "حدث خطأ", ToastTone.Bad);
        Settle();

        var manager = Toast.ManagerFor(button)!;
        Assert.Equal(NotificationPosition.BottomRight, manager.Position);
        var cards = manager.GetVisualDescendants().OfType<NotificationCard>().ToList();
        Assert.Equal(2, cards.Count);
        var ok = cards[0];
        Assert.Contains(":success", ok.Classes);
        Assert.Equal("✓", Part<TextBlock>(ok, "PART_Glyph").Text);
        AssertBrush(ok, "InsToneOkBg", () => ok.Background);
        AssertBrush(ok, "InsToneOk", () => ok.Foreground);
        Assert.Equal(Res(ok, "InsShLg"), Part<Border>(ok, "PART_Card").BoxShadow);
        Assert.Equal("✕", Part<TextBlock>(cards[1], "PART_Glyph").Text);
        Assert.Contains("persist", cards[1].Classes);
        Assert.True(Part<Button>(cards[1], "PART_Close").IsVisible);
        Assert.False(Part<Button>(ok, "PART_Close").IsVisible);

        var newest = BoundsIn(cards[1], window);
        Assert.Equal(600 - 16, newest.Bottom, 1);
        if (rtl) Assert.Equal(16, newest.X, 1);
        else Assert.Equal(900 - 16, newest.Right, 1);
        Assert.Equal(8, newest.Y - BoundsIn(ok, window).Bottom, 1);

        Click(ok, window);
        Tick(100);
        Assert.Equal(2, manager.GetVisualDescendants().OfType<NotificationCard>().Count(c => !c.IsClosing));
        Click(Part<Button>(cards[1], "PART_Close"), window);
        Tick(100);
        Assert.Same(cards[0], Assert.Single(manager.GetVisualDescendants().OfType<NotificationCard>(), c => !c.IsClosing));
    }

    [AvaloniaFact]
    public void A_press_on_a_toast_body_leaves_it_open()
    {
        var button = new Button { Content = "معلومة" };
        var window = Show(new StackPanel { Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);
        Toast.Show(button, "للعلم", ToastTone.Info);
        Settle(100);
        var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
        var message = card.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "للعلم");
        Click(message, window);
        Tick(100);
        Assert.False(card.IsClosing);
        Assert.False(card.IsClosed);
    }

    [AvaloniaFact]
    public void The_close_button_still_closes_an_error_toast()
    {
        var button = new Button { Content = "خطأ" };
        var window = Show(new StackPanel { Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);
        Toast.Show(button, "حدث خطأ", ToastTone.Bad);
        Settle(100);
        var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
        Click(Part<Button>(card, "PART_Close"), window);
        Tick(100);
        Assert.True(card.IsClosing || card.IsClosed, "the close button did not close the toast");
    }

    [AvaloniaFact]
    public void A_toast_set_to_close_by_button_stays_and_shows_its_close()
    {
        var button = new Button { Content = "معلومة" };
        var window = Show(new StackPanel { Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);
        var hold = Toast.Duration;
        Toast.Duration = TimeSpan.FromMilliseconds(300);
        try
        {
            Toast.Show(button, "للعلم", ToastTone.Info, null, ToastClose.Button);
            Settle(100);
            var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
            var close = Part<Button>(card, "PART_Close");
            Assert.True(close.IsVisible);
            Tick(1500);
            Assert.False(card.IsClosing);
            Click(close, window);
            Tick(100);
            Assert.True(card.IsClosing || card.IsClosed, "the close button did not close the toast");
        }
        finally
        {
            Toast.Duration = hold;
        }
    }

    [AvaloniaFact]
    public void An_error_set_to_close_by_timer_leaves_on_its_own()
    {
        var button = new Button { Content = "خطأ" };
        Show(new StackPanel { Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);
        var hold = Toast.Duration;
        Toast.Duration = TimeSpan.FromMilliseconds(300);
        try
        {
            Toast.Show(button, "حدث خطأ", ToastTone.Bad, null, ToastClose.Timer);
            Settle(100);
            var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
            Assert.False(Part<Button>(card, "PART_Close").IsVisible);
            Tick(600);
            Assert.True(card.IsClosing || card.IsClosed, "the error did not leave on its own");
        }
        finally
        {
            Toast.Duration = hold;
        }
    }

    [AvaloniaFact]
    public void The_close_attribute_on_a_button_reaches_its_toast()
    {
        var button = new Button { Content = "إظهار" };
        Toast.SetText(button, "للعلم");
        Toast.SetTone(button, ToastTone.Info);
        Toast.SetClose(button, ToastClose.Button);
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);
        Click(button, window);
        var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
        Assert.Contains("persist", card.Classes);
    }

    [AvaloniaFact]
    public void A_toast_leaves_on_its_own()
    {
        var button = new Button { Content = "معلومة" };
        var window = Show(new StackPanel { Children = { button } });
        var hold = Toast.Duration;
        Toast.Duration = TimeSpan.FromMilliseconds(300);
        try
        {
            Toast.Show(button, "للعلم");
            Settle(100);
            var manager = Toast.ManagerFor(button)!;
            var card = manager.GetVisualDescendants().OfType<NotificationCard>().Single();
            Assert.Contains(":information", card.Classes);
            Assert.Equal("i", Part<TextBlock>(card, "PART_Glyph").Text);
            Tick(600);
            Assert.True(card.IsClosing, "not closing after its time");
            Tick(600);
            Assert.True(card.IsClosed, "not closed");
            Assert.Empty(manager.GetVisualDescendants().OfType<NotificationCard>());
        }
        finally
        {
            Toast.Duration = hold;
        }
    }

    [AvaloniaFact]
    public void A_toast_sits_above_an_open_dialog()
    {
        var dialog = MakeDialog();
        var (window, opener) = Host(dialog);
        Click(opener, window);
        Toast.Show(opener, "تم الحفظ", ToastTone.Ok);
        Settle();
        var manager = Toast.ManagerFor(opener)!;
        var stack = DialogStack.Of(opener)!;
        Assert.Same(stack.Parent, manager.Parent);
        Assert.True(manager.ZIndex > stack.ZIndex);
    }

    private static Notify MakeNotify(int unread = 2)
    {
        var notify = new Notify { FootText = "عرض الكل" };
        string[] titles = ["اكتمل التصدير", "تعليق جديد على المستند", "تم حفظ التغييرات"];
        for (var i = 0; i < 3; i++)
            notify.Items.Add(new NotifyItem { Title = titles[i], Description = "وصف", End = "الآن", IsUnread = i < unread });
        return notify;
    }

    [AvaloniaFact]
    public void The_bell_counts_the_unread()
    {
        var notify = MakeNotify();
        Show(new StackPanel { Margin = new Thickness(40), Children = { notify } });
        Assert.Equal(2, notify.UnreadCount);
        Assert.Equal("2", notify.CountText);
        Assert.True(Part<Border>(notify, "PART_Count").IsVisible);
        Assert.Equal("2", Part<Badge>(notify, "PART_Badge").Content);
        var bell = Part<Button>(notify, "PART_Bell");
        Assert.Equal(48, bell.Bounds.Width, 1);
        Assert.Equal("الإشعارات, 2 unread", AutomationProperties.GetName(bell));

        notify.MarkAllRead();
        Settle();
        Assert.Equal(0, notify.UnreadCount);
        Assert.False(Part<Border>(notify, "PART_Count").IsVisible);

        for (var i = 0; i < 120; i++) notify.Items.Add(new NotifyItem { Title = "إشعار", IsUnread = true });
        Settle();
        Assert.Equal("99+", notify.CountText);
    }

    [AvaloniaFact]
    public void The_count_sits_on_the_bells_end_corner()
    {
        foreach (var dir in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            var notify = MakeNotify();
            var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, dir);
            var bell = BoundsIn(Part<Button>(notify, "PART_Bell"), window);
            var count = BoundsIn(Part<Border>(notify, "PART_Count"), window);
            if (dir == FlowDirection.LeftToRight) Assert.Equal(bell.Right + 7, count.Right, 1);
            else Assert.Equal(bell.X - 7, count.X, 1);
            Assert.Equal(bell.Y - 7, count.Y, 1);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Clicking_a_row_marks_it_read_and_says_so()
    {
        var notify = MakeNotify();
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.RightToLeft);
        NotifyReadEventArgs? heard = null;
        notify.Read += (_, e) => heard = e;
        Click(Part<Button>(notify, "PART_Bell"), window);
        Assert.True(notify.IsOpen);
        Assert.True(Part<Popup>(notify, "PART_Popup").IsOpen);
        var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(notify, "PART_Popup").Child);
        Assert.Contains("pop", presenter.Classes);
        Assert.Contains("card", presenter.Classes);
        Assert.Contains("notify", presenter.Classes);

        var first = (NotifyItem)notify.Items[0]!;
        Assert.Contains(":unread", first.Classes);
        first.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
        Assert.False(first.IsUnread);
        Assert.Equal(1, notify.UnreadCount);
        Assert.Same(first, heard!.Item);
        Assert.False(heard.All);

        heard = null;
        var readAll = presenter.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_ReadAll");
        readAll.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
        Assert.True(heard!.All);
        Assert.Equal(0, notify.UnreadCount);
    }

    [AvaloniaFact]
    public void An_empty_centre_says_so()
    {
        var notify = new Notify();
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } });
        Assert.Contains(":empty", notify.Classes);
        Click(Part<Button>(notify, "PART_Bell"), window);
        var body = Part<Popup>(notify, "PART_Popup").Child!;
        var empty = body.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Empty");
        Assert.True(empty.IsVisible);
        Assert.Equal("لا إشعارات", empty.Text);
        Assert.False(body.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_ReadAll").IsVisible);
    }

    [AvaloniaFact]
    public void A_new_notification_rings_and_counts()
    {
        var notify = MakeNotify(0);
        Show(new StackPanel { Margin = new Thickness(40), Children = { notify } });
        notify.Items.Insert(0, new NotifyItem { Title = "إشعار جديد", IsUnread = true });
        Settle(200);
        Assert.Equal(1, notify.UnreadCount);
        Assert.Contains(":unread", notify.Classes);
    }

    [AvaloniaFact]
    public async Task Enter_presses_the_dialogs_main_button_not_the_pages()
    {
        var pageClicks = 0;
        var page = new Button { Content = "افتراضي", IsDefault = true };
        page.Click += (_, _) => pageClicks++;
        var opener = new Button { Content = "فتح الحوار" };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { opener, page } });

        var field = new TextBox();
        var dialog = MakeDialog();
        dialog.Content = field;
        var result = dialog.ShowAsync(opener);
        Settle();
        Assert.Same(field, window.FocusManager!.GetFocusedElement());
        Press(window, Key.Enter);
        Assert.False(dialog.IsOpen);
        Assert.Equal("ok", await result);
        Assert.Equal(0, pageClicks);
        Settle();

        dialog.Content = new TextBox { AcceptsReturn = true };
        var again = dialog.ShowAsync(opener);
        Settle();
        Press(window, Key.Enter);
        Assert.True(dialog.IsOpen);
        Assert.Equal(0, pageClicks);
        Press(window, Key.Escape);
        Assert.Null(await again);
    }

    [AvaloniaFact]
    public void Escape_wins_over_a_page_cancel_button()
    {
        var pageClicks = 0;
        var page = new Button { Content = "إلغاء الصفحة", IsCancel = true };
        page.Click += (_, _) => pageClicks++;
        var dialog = MakeDialog();
        var opener = new Button { Content = "فتح الحوار" };
        Dialog.SetOpens(opener, dialog);
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { opener, page } });
        Click(opener, window);
        Assert.True(dialog.IsOpen);
        Press(window, Key.Escape);
        Assert.False(dialog.IsOpen);
        Assert.Equal(0, pageClicks);
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("drawer")]
    public void Dismiss_nearest_closes_the_dialog_not_the_window(string? classes)
    {
        var dialog = MakeDialog(classes, foot: false);
        var close = new Button { Content = "إغلاق الحوار" };
        Dismiss.SetNearest(close, true);
        dialog.Content = close;
        var (window, opener) = Host(dialog);
        Click(opener, window);
        Assert.True(dialog.IsOpen);

        Click(close, window);
        Assert.False(dialog.IsOpen);
        Assert.True(window.IsVisible);
        Assert.True(dialog.IsVisible);

        dialog.Open(opener);
        Settle();
        Assert.True(dialog.IsOpen);
    }

    [AvaloniaFact]
    public void Focus_starts_on_the_first_field()
    {
        var first = new TextBox();
        var dialog = MakeDialog();
        dialog.Content = new StackPanel { Spacing = 8, Children = { first, new TextBox() } };
        var (window, opener) = Host(dialog);
        Click(opener, window);
        Assert.Same(first, window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void A_dialog_opened_from_a_popup_lands_in_the_window()
    {
        var dialog = MakeDialog();
        var inner = new Button { Content = "فتح الحوار" };
        Dialog.SetOpens(inner, dialog);
        var card = new PopCard { Content = inner };
        var trigger = new Button { Content = "التفاصيل", Flyout = card, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { trigger } }, FlowDirection.LeftToRight, null, 900, 600);
        Click(trigger, window);
        Assert.True(card.IsOpen);
        Click(inner, window);
        Assert.True(dialog.IsOpen);
        Assert.Same(window, TopLevel.GetTopLevel(dialog));
        Assert.Single(DialogStack.Of(window)!.Layers);
    }

    [AvaloniaFact]
    public async Task Static_show_and_confirm_return_answers()
    {
        var opener = new Button { Content = "فتح الحوار" };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { opener } });
        var yes = new Button { Content = "موافق", Classes = { "primary" } };
        Dialog.SetResult(yes, "ok");
        var shown = Dialog.ShowAsync(opener, "عنوان الحوار", "نص الحوار.", yes);
        Settle();
        Click(yes, window);
        Assert.Equal("ok", await shown);
        Settle();

        var asked = Dialog.ConfirmAsync(opener, "تأكيد", "أرشفة العنصر؟", danger: true, confirm: "أرشفة");
        Settle();
        var dialog = DialogStack.Of(opener)!.Layers.Single().Dialog;
        Assert.Equal("تأكيد", dialog.Title);
        var action = (Button)((DialogFoot)dialog.Foot!).Children[1];
        Assert.Contains("danger", action.Classes);
        Assert.Equal("أرشفة", action.Content);
        Press(window, Key.Escape);
        Assert.False(await asked);
    }

    [AvaloniaFact]
    public async Task A_dialog_can_open_in_an_owned_window()
    {
        var dialog = MakeDialog();
        var (window, opener) = Host(dialog);
        opener.Focus();
        var shown = dialog.ShowAsWindowAsync(opener);
        Settle();

        Assert.Single(window.OwnedWindows);
        var child = window.OwnedWindows[0];
        var top = Assert.IsAssignableFrom<Window>(TopLevel.GetTopLevel(dialog));
        Assert.NotSame(window, top);
        Assert.Contains(":window", dialog.Classes);
        Assert.False(Part<Border>(dialog, "PART_Head").IsVisible);
        Assert.True(DialogStack.Of(window) is null || DialogStack.Of(window)!.Layers.All(l => l.Dialog != dialog));

        child.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
        Settle();
        Assert.Null(await shown);
        Assert.Empty(window.OwnedWindows);
        Assert.Equal(opener, window.FocusManager!.GetFocusedElement());

        var again = dialog.ShowAsync(opener);
        Settle();
        Assert.True(dialog.IsOpen);
        Assert.Same(window, TopLevel.GetTopLevel(dialog));
        Press(window, Key.Escape);
        Assert.Null(await again);
    }

    [AvaloniaFact]
    public void Single_view_hosts_dialog_toasts_and_notify()
    {
        var dialog = MakeDialog();
        var opener = new Button { Content = "فتح الحوار" };
        var notify = MakeNotify();
        var root = SingleView(new StackPanel { Margin = new Thickness(16), Spacing = 12, Children = { opener, notify } });
        try
        {
            dialog.Open(opener);
            Pump(root);
            Assert.True(dialog.IsOpen);
            Assert.Same(root, TopLevel.GetTopLevel(dialog));
            Assert.Equal(368, dialog.Bounds.Width, 1);
            var foot = (DialogFoot)dialog.Foot!;
            Assert.True(foot.IsPhone);
            var buttons = foot.Children.OfType<Button>().ToList();
            Assert.All(buttons, b => Assert.Equal(46, b.Bounds.Height, 1));
            Assert.Equal(buttons[0].Bounds.Width, buttons[1].Bounds.Width, 1);
            PressOn(root, Key.Escape);
            Assert.False(dialog.IsOpen);

            var fallback = dialog.ShowAsWindowAsync(opener);
            Pump(root);
            Assert.True(dialog.IsOpen);
            Assert.Same(root, TopLevel.GetTopLevel(dialog));
            dialog.Close();
            Pump(root);
            Assert.True(fallback.IsCompleted);

            Toast.Show(opener, "تم الحفظ", ToastTone.Ok);
            Pump(root);
            var manager = Toast.ManagerFor(opener)!;
            Assert.Same(OverlayHost.Layer(root), manager.Parent);
            Assert.Contains("phone", manager.Classes);
            var card = manager.GetVisualDescendants().OfType<NotificationCard>().Single();
            Assert.Equal(400 - 24, card.Bounds.Width, 1);

            notify.IsOpen = true;
            Pump(root);
            var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(notify, "PART_Popup").Child);
            Assert.Same(root, TopLevel.GetTopLevel(presenter));
            Assert.Equal(352, presenter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "LayoutRoot").Bounds.Width, 1);
        }
        finally
        {
            root.StopRendering();
        }

        var narrow = MakeNotify();
        var small = SingleView(new StackPanel { Margin = new Thickness(16), Children = { narrow } }, 320, 600);
        try
        {
            narrow.IsOpen = true;
            Pump(small);
            var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(narrow, "PART_Popup").Child);
            Assert.Equal(304, presenter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "LayoutRoot").Bounds.Width, 1);
        }
        finally
        {
            small.StopRendering();
        }
    }

    [AvaloniaFact]
    public void Back_closes_the_top_dialog()
    {
        var first = MakeDialog();
        var second = MakeDialog("sm");
        var (window, opener) = Host(first);
        Click(opener, window);
        second.Open(opener);
        Settle();
        window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        Settle();
        Assert.False(second.IsOpen);
        Assert.True(first.IsOpen);
        window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        Settle();
        Assert.False(first.IsOpen);
    }

    private static (DrawerHost Host, Dialog Pane) Pinned(DrawerSide side, DrawerMode mode = DrawerMode.Pinned, bool open = true)
    {
        var pane = new Dialog { Title = "عنوان الدرج", Content = new TextBlock { Text = "محتوى الدرج.", TextWrapping = TextWrapping.Wrap } };
        var host = new DrawerHost { Mode = mode, Side = side, IsPaneOpen = open, PaneLength = 320, Pane = pane, Content = new TextBlock { Text = "محتوى الصفحة." } };
        return (host, pane);
    }

    [AvaloniaFact]
    public void Pinned_drawer_takes_a_resizable_column()
    {
        var (host, pane) = Pinned(DrawerSide.Start);
        var window = Show(host, FlowDirection.LeftToRight, null, 1200, 600);

        Assert.True(host.IsPinned);
        Assert.True(pane.IsOpen);
        Assert.Contains("drawer", pane.Classes);
        Assert.Contains(":pinned", pane.Classes);
        Assert.Equal(new CornerRadius(), Part<Border>(pane, "PART_Fill").CornerRadius);
        Assert.True(DialogStack.Of(host) is null || DialogStack.Of(host)!.Layers.Count == 0);
        var box = BoundsIn(pane, window);
        Assert.Equal(0, box.X, 1);
        Assert.Equal(320, box.Width, 1);
        var splitter = Part<GridSplitter>(host, "PART_Splitter");
        var handle = BoundsIn(splitter, window);
        Assert.Equal(320, handle.X, 1);
        Assert.Equal(14, handle.Width, 1);
        var content = Part<Grid>(host, "PART_Grid").Children.OfType<ContentPresenter>().First();
        Assert.Equal(334, BoundsIn(content, window).X, 1);

        var c = Center(splitter, window);
        window.MouseMove(c);
        window.MouseDown(c, MouseButton.Left);
        window.MouseMove(new Point(c.X + 50, c.Y));
        window.MouseMove(new Point(c.X + 100, c.Y));
        window.MouseUp(new Point(c.X + 100, c.Y), MouseButton.Left);
        Settle();
        Assert.Equal(420, host.PaneLength, 2);

        c = Center(splitter, window);
        window.MouseMove(c);
        window.MouseDown(c, MouseButton.Left);
        window.MouseMove(new Point(c.X + 200, c.Y));
        window.MouseMove(new Point(c.X + 400, c.Y));
        window.MouseUp(new Point(c.X + 400, c.Y), MouseButton.Left);
        Settle();
        Assert.Equal(560, host.PaneLength, 2);

        Part<Button>(pane, "PART_Close").Focus();
        Press(window, Key.Escape);
        Assert.True(host.IsPaneOpen);
        Assert.True(pane.IsOpen);
    }

    [AvaloniaTheory]
    [InlineData(DrawerSide.Start, "rtl")]
    [InlineData(DrawerSide.End, "ltr")]
    [InlineData(DrawerSide.End, "rtl")]
    [InlineData(DrawerSide.Bottom, "ltr")]
    public void Pinned_drawer_mirrors_and_sits_at_the_end(DrawerSide side, string dir)
    {
        var (host, pane) = Pinned(side);
        var window = Show(host, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, null, 1200, 600);
        var box = BoundsIn(pane, window);
        if (side == DrawerSide.Bottom)
        {
            Assert.Equal(600 - 320, box.Y, 1);
            Assert.Equal(1200, box.Width, 1);
            return;
        }
        var expected = (side == DrawerSide.Start) == (dir == "ltr") ? 0 : 1200 - 320;
        Assert.Equal(expected, box.X, 1);
        Assert.Equal(320, box.Width, 1);
    }

    [AvaloniaFact]
    public void Pinned_drawer_falls_back_to_temporary_when_narrow()
    {
        var (host, pane) = Pinned(DrawerSide.End);
        var toggle = new Button { Content = "الدرج" };
        DrawerHost.SetToggles(toggle, host);
        host.Content = toggle;
        var window = Show(host, FlowDirection.LeftToRight, null, 800, 600);

        Assert.False(host.IsPinned);
        Assert.False(host.IsPaneOpen);
        Assert.Null(Part<Decorator>(host, "PART_Slot").Child);

        Click(toggle, window);
        Assert.True(host.IsPaneOpen);
        Assert.Same(pane, DialogStack.Of(host)!.Layers.Single().Dialog);
        Assert.Equal(1, Layer(pane).Scrim.Opacity, 2);

        window.Width = 1000;
        Settle();
        Assert.True(host.IsPinned);
        Assert.Empty(DialogStack.Of(host)!.Layers);
        Assert.Same(Part<Decorator>(host, "PART_Slot"), pane.GetVisualParent());
        Assert.True(host.IsPaneOpen);

        window.Width = 800;
        Settle();
        Assert.False(host.IsPinned);
        Assert.False(host.IsPaneOpen);
        Assert.False(pane.IsOpen);
        Assert.Empty(DialogStack.Of(host)!.Layers);
    }

    [AvaloniaFact]
    public void Hovering_a_toast_pauses_it()
    {
        var button = new Button { Content = "معلومة" };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, FlowDirection.LeftToRight, null, 900, 600);
        var hold = Toast.Duration;
        Toast.Duration = TimeSpan.FromMilliseconds(300);
        try
        {
            Toast.Show(button, "للعلم");
            Settle(100);
            var manager = Toast.ManagerFor(button)!;
            var card = manager.GetVisualDescendants().OfType<NotificationCard>().Single();
            Hover(window, card);
            Tick(900);
            Assert.False(card.IsClosing, "closed while hovered");

            window.MouseMove(new Point(5, 5));
            Tick(1200);
            Assert.Empty(manager.GetVisualDescendants().OfType<NotificationCard>());
        }
        finally
        {
            Toast.Duration = hold;
        }
    }

    [AvaloniaFact]
    public void Errors_stay_until_closed()
    {
        var button = new Button { Content = "خطأ" };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { button } }, FlowDirection.LeftToRight, null, 900, 600);
        var hold = Toast.Duration;
        Toast.Duration = TimeSpan.FromMilliseconds(300);
        try
        {
            Toast.Show(button, "حدث خطأ", ToastTone.Bad);
            Settle(100);
            var manager = Toast.ManagerFor(button)!;
            var card = manager.GetVisualDescendants().OfType<NotificationCard>().Single();
            Tick(900);
            Assert.False(card.IsClosing, "an error left on its own");

            Click(Part<Button>(card, "PART_Close"), window);
            Assert.True(card.IsClosing);
            Tick(400);
            Assert.Empty(manager.GetVisualDescendants().OfType<NotificationCard>());
        }
        finally
        {
            Toast.Duration = hold;
        }
    }

    [AvaloniaFact]
    public void F8_reaches_the_toasts_and_keys_dismiss_them()
    {
        var field = new TextBox();
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { field } }, FlowDirection.LeftToRight, null, 900, 600);
        field.Focus();
        Settle(100);
        Assert.False(Toast.FocusToasts(field));
        Toast.Show(field, "تم الحفظ", ToastTone.Ok);
        Toast.Show(field, "انتبه", ToastTone.Warn);
        Settle(200);
        var manager = Toast.ManagerFor(field)!;
        var cards = manager.GetVisualDescendants().OfType<NotificationCard>().ToList();
        Assert.Equal(2, cards.Count);

        Press(window, Key.F8);
        Assert.Same(cards[1], window.FocusManager!.GetFocusedElement());
        Press(window, Key.Up);
        Assert.Same(cards[0], window.FocusManager!.GetFocusedElement());
        Press(window, Key.Escape);
        Assert.True(cards[0].IsClosing || cards[0].IsClosed);
        Assert.Same(cards[1], window.FocusManager!.GetFocusedElement());
        Press(window, Key.F8);
        Assert.Same(field, window.FocusManager!.GetFocusedElement());

        Assert.True(Toast.FocusToasts(field));
        Press(window, Key.Enter);
        Tick(400);
        Assert.False(Toast.FocusToasts(field));
    }

    [AvaloniaFact]
    public void A_second_click_on_the_bell_closes_the_panel()
    {
        var notify = MakeNotify();
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.LeftToRight, null, 900, 600);
        var bell = Part<Button>(notify, "PART_Bell");
        Click(bell, window);
        Assert.True(notify.IsOpen);
        Click(bell, window);
        Assert.False(notify.IsOpen);
        Assert.False(Part<Popup>(notify, "PART_Popup").IsOpen);
    }

    [AvaloniaTheory]
    [InlineData("ltr", false)]
    [InlineData("rtl", false)]
    [InlineData("ltr", true)]
    public void Notify_panel_hangs_from_the_bells_leading_edge(string dir, bool end)
    {
        var rtl = dir == "rtl";
        var notify = MakeNotify();
        notify.IsEnd = end;
        if (end) notify.HorizontalAlignment = HorizontalAlignment.Right;
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, null, 900, 600);
        var bell = Part<Button>(notify, "PART_Bell");
        Click(bell, window);
        var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(notify, "PART_Popup").Child);
        var body = BoundsIn(presenter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "LayoutRoot"), window);
        var at = BoundsIn(bell, window);
        Assert.Equal(352, body.Width, 1);
        Assert.InRange(body.Y - at.Bottom, 5, 7);
        if (end || rtl) Assert.InRange(body.Right - at.Right, -1, 1);
        else Assert.InRange(body.X - at.X, -1, 1);
    }

    [AvaloniaFact]
    public void The_focus_ring_of_a_notification_stays_inside_the_scrolled_list()
    {
        var notify = new Notify();
        for (var i = 0; i < 14; i++) notify.Items.Add(new NotifyItem { Title = "إشعار " + i, Description = "وصف", End = "الآن" });
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.LeftToRight, null, 900, 600);
        Click(Part<Button>(notify, "PART_Bell"), window);
        Settle();
        var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(notify, "PART_Popup").Child);
        var scroll = presenter.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == "PART_Scroll");
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height + 100);

        void Check(string step)
        {
            var port = BoundsIn(scroll, window);
            var item = Assert.IsType<NotifyItem>(window.FocusManager!.GetFocusedElement());
            var ring = BoundsIn(Part<Border>(item, "PART_Row"), window).Inflate(3);
            Assert.True(ring.Top >= port.Top - 0.5 && ring.Bottom <= port.Bottom + 0.5, $"{step}: ring {ring.Top}-{ring.Bottom} outside list {port.Top}-{port.Bottom}");        }

        void Step(Key key, string step)
        {
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Settle(60);
            Check(step);
        }

        Check("open");
        for (var i = 1; i < 14; i++) Step(Key.Down, "down " + i);
        for (var i = 1; i < 14; i++) Step(Key.Up, "up " + i);
    }

    [AvaloniaFact]
    public void An_unread_notification_shows_it_in_its_title_icon_and_rim_not_a_dot_or_tint()
    {
        var notify = new Notify();
        var unread = new NotifyItem { Title = "جديد", Description = "وصف", End = "الآن", IsUnread = true, Icon = Geometry.Parse("M5 12h14") };
        var read = new NotifyItem { Title = "قديم", Description = "وصف", End = "أمس", Icon = Geometry.Parse("M5 12h14") };
        notify.Items.Add(unread);
        notify.Items.Add(read);
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.RightToLeft, null, 900, 600);
        Click(Part<Button>(notify, "PART_Bell"), window);
        Settle(300);
        Assert.Empty(unread.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Ellipse>());
        Assert.Equal(Brushes.Transparent, Part<Border>(unread, "PART_Row").Background);
        Assert.True(Part<Border>(unread, "PART_Mark").IsVisible);
        Assert.False(Part<Border>(read, "PART_Mark").IsVisible);
        AssertBrush(unread, "InsToneBrand", () => Part<TextBlock>(unread, "PART_Title").Foreground);
        AssertBrush(unread, "InsToneBrandSoft", () => Part<Icon>(unread, "PART_Icon").Foreground);
        AssertBrush(read, "InsText3", () => Part<Icon>(read, "PART_Icon").Foreground);
    }

    [AvaloniaFact]
    public void The_last_notification_shows_its_whole_focus_ring_with_no_foot_under_it()
    {
        var notify = new Notify { FootText = null };
        for (var i = 0; i < 3; i++) notify.Items.Add(new NotifyItem { Title = "إشعار " + i, Description = "وصف", End = "الآن" });
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.RightToLeft, null, 900, 600);
        Click(Part<Button>(notify, "PART_Bell"), window);
        Settle();
        var last = (NotifyItem)notify.Items[2]!;
        last.Focus(NavigationMethod.Directional);
        Settle(60);
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<FocusRing>(), r => r.IsEffectivelyVisible);
        var row = BoundsIn(Part<Border>(last, "PART_Row"), window);
        var frame =Assert.IsType<global::Avalonia.Media.Imaging.WriteableBitmap>(window.CaptureRenderedFrame());
        using var buffer = frame.Lock();
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        int Green(double x, double y) => bytes[(int)y * buffer.RowBytes + (int)x * 4 + 1];
        foreach (var (x, y, side) in new[] { (row.Center.X, row.Bottom - 1.5, "bottom"), (row.Center.X, row.Top + 1.5, "top"), (row.Left + 1.5, row.Center.Y, "left"), (row.Right - 1.5, row.Center.Y, "right") })
            Assert.True(Green(x, y) < 200, $"no ring ink on the row's {side} edge at {x},{y} (green {Green(x, y)})");
    }

    [AvaloniaFact]
    public void Arrows_move_through_the_notifications()
    {
        var notify = MakeNotify();
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, FlowDirection.RightToLeft);
        var bell = Part<Button>(notify, "PART_Bell");
        Click(bell, window);
        Settle();
        Assert.Same(notify.Items[0], window.FocusManager!.GetFocusedElement());
        Press(window, Key.Down);
        Assert.Same(notify.Items[1], window.FocusManager!.GetFocusedElement());
        Press(window, Key.End);
        Assert.Same(notify.Items[2], window.FocusManager!.GetFocusedElement());
        Press(window, Key.Home);
        Assert.Same(notify.Items[0], window.FocusManager!.GetFocusedElement());
        Press(window, Key.Escape);
        Assert.False(notify.IsOpen);
        Assert.Same(bell, window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void The_count_is_a_badge_on_a_ring()
    {
        var notify = MakeNotify();
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } });
        var badge = Part<Badge>(notify, "PART_Badge");
        var ring = Part<Border>(notify, "PART_Count");
        AssertBrush(badge, "InsNotifyCountBg", () => badge.Background);
        Assert.Equal(Color.Parse("#DC2626"), ColorOf(badge.Background));
        Assert.Equal(20, badge.Bounds.Height, 1);
        AssertBrush(ring, "InsWash1", () => ring.Background);
        Assert.False(notify.ClipToBounds);

        var at = BoundsIn(Part<Button>(notify, "PART_Bell"), window);
        var count = BoundsIn(ring, window);
        Assert.Equal(at.Y - 7, count.Y, 1);
        Assert.True(count.X >= 0 && count.Y >= 0 && count.Right <= window.Bounds.Width && count.Bottom <= window.Bounds.Height);
    }

    [AvaloniaFact]
    public void Overlay_popups_carry_the_body_ink()
    {
        var notify = MakeNotify();
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } });
        Click(Part<Button>(notify, "PART_Bell"), window);
        var presenter = Assert.IsType<FlyoutPresenter>(Part<Popup>(notify, "PART_Popup").Child);
        var host = presenter.FindAncestorOfType<OverlayPopupHost>()!;
        Assert.NotNull(host);
        AssertBrush(host, "InsText1", () => host.Foreground);
    }

    public static IEnumerable<object[]> Shots() => Looks();

    [AvaloniaTheory]
    [MemberData(nameof(Shots))]
    public void Overlays_render_in_every_look(string theme, string dir)
    {
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var flow = dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        var dialog = MakeDialog();
        var (window, opener) = Host(dialog, flow, 880, 560, variant);
        Click(opener, window);
        Assert.Equal(flow, dialog.FlowDirection);
        Assert.Equal(variant, dialog.ActualThemeVariant);
        Capture(window, $"ovl-dialog-{theme.ToLowerInvariant()}-{dir}");
        dialog.Close();
        Settle();

        var drawer = MakeDialog("drawer");
        drawer.Title = "درج البداية";
        drawer.Open(opener);
        Capture(window, $"ovl-drawer-{theme.ToLowerInvariant()}-{dir}");
        drawer.Close();
        Settle();

        Toast.Show(opener, "تم الحفظ", ToastTone.Ok);
        Toast.Show(opener, "حدث خطأ", ToastTone.Bad);
        Toast.Show(opener, "انتبه", ToastTone.Warn);
        Toast.Show(opener, "للعلم", ToastTone.Info);
        Capture(window, $"ovl-toasts-{theme.ToLowerInvariant()}-{dir}");
        window.Close();

        var notify = MakeNotify();
        foreach (var item in notify.Items.OfType<NotifyItem>()) item.Icon = Geometry.Parse("M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3");
        var page = Show(new StackPanel { Margin = new Thickness(40), Children = { notify } }, flow, variant, 880, 560);
        Click(Part<Button>(notify, "PART_Bell"), page);
        Capture(page, $"ovl-notify-{theme.ToLowerInvariant()}-{dir}");
        page.Close();

        var (host, _) = Pinned(DrawerSide.End);
        var pinned = Show(host, flow, variant, 1100, 560);
        Capture(pinned, $"ovl-pinned-{theme.ToLowerInvariant()}-{dir}");
        pinned.Close();
    }
}
