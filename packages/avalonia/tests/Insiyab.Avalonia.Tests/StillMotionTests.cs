using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A still window (Motion.Still) shows each state at once: pops, toasts, the sidebar, tooltips, switches and check marks land without moving.
public class StillMotionTests
{
    private static bool IsIdentity(ITransform? t) => t is null || t.Value.IsIdentity;

    // A few frames, far shorter than any of the motions under test.
    private static void Tick(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        do
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(5);
        }
        while (clock.ElapsedMilliseconds < milliseconds);
    }

    private static Border PopBody(out Panel items)
    {
        var list = new ItemsControl { ItemsSource = new[] { "نسخ", "لصق", "حذف", "تحديد الكل", "بحث" } };
        var body = new Border { Child = list };
        PopMotion.SetStagger(body, true);
        Show(new StackPanel { Children = { body } });
        items = list.GetVisualDescendants().OfType<ItemsPresenter>().First().Panel!;
        Assert.Equal(5, items.Children.Count);
        return body;
    }

    [AvaloniaFact]
    public void A_pop_and_its_items_rise_in_one_after_another()
    {
        var body = PopBody(out var items);
        PopMotion.Play(body);
        Tick(40);
        Assert.True(items.Children[^1].Opacity < 1, "the last item should still be waiting its turn");
    }

    [AvaloniaFact]
    public void A_still_pop_and_its_items_are_there_at_once()
    {
        var body = PopBody(out var items);
        Motion.SetStill((Window)TopLevel.GetTopLevel(body)!, true);
        PopMotion.Play(body);
        Tick(40);
        Assert.Equal(1, body.Opacity);
        Assert.True(IsIdentity(body.RenderTransform));
        foreach (var item in items.Children)
        {
            Assert.Equal(1, item.Opacity);
            Assert.True(IsIdentity(item.RenderTransform));
        }
    }

    [AvaloniaFact]
    public void A_still_sidebar_collapses_and_returns_at_once()
    {
        var shell = new SplitView { IsPaneOpen = true, Pane = new Border(), Content = new Panel() };
        var window = Show(shell, width: 1200, height: 600);
        Motion.SetStill(window, true);
        Tick(20);
        var pane = Part<Panel>(shell, "PART_PaneRoot");
        var chrome = Part<Border>(shell, "PART_PaneChrome");

        shell.IsPaneOpen = false;
        Tick(40);
        Assert.InRange(pane.Bounds.Width, 0, 1);
        Assert.Equal(0, chrome.Opacity, 2);

        shell.IsPaneOpen = true;
        Tick(40);
        Assert.Equal(252, pane.Bounds.Width, 1);
        Assert.Equal(1, chrome.Opacity, 2);
    }

    [AvaloniaFact]
    public void A_still_drawer_and_its_scrim_arrive_at_once()
    {
        var shell = new SplitView { Pane = new Border(), Content = new Panel() };
        var window = Show(shell, width: 800, height: 500);
        Assert.Equal(SplitViewDisplayMode.Overlay, shell.DisplayMode);
        Motion.SetStill(window, true);
        Tick(20);
        var pane = Part<Panel>(shell, "PART_PaneRoot");
        var scrim = Part<Rectangle>(shell, "LightDismissLayer");

        shell.IsPaneOpen = true;
        Tick(40);
        Assert.Equal(0, pane.TranslatePoint(default, window)!.Value.X, 1);
        Assert.Equal(1, scrim.Opacity, 2);
    }

    [AvaloniaFact]
    public void A_still_toast_comes_and_goes_at_once()
    {
        var button = new Button { Content = "معلومة" };
        var window = Show(new StackPanel { Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);
        Motion.SetStill(window, true);
        Toast.Show(button, "للعلم", ToastTone.Info, null, ToastClose.Button);
        Tick(40);
        var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
        Assert.Equal(1, card.Opacity, 2);
        Assert.True(IsIdentity(card.RenderTransform));

        card.Close();
        Tick(40);
        Assert.True(card.IsClosed, "a still toast should leave at once");
    }

    [AvaloniaFact]
    public void A_toast_rises_in_and_leaves_on_its_curve()
    {
        var button = new Button { Content = "معلومة" };
        Show(new StackPanel { Children = { button } }, FlowDirection.RightToLeft, width: 900, height: 600);
        Toast.Show(button, "للعلم", ToastTone.Info, null, ToastClose.Button);
        Tick(40);
        var card = Toast.ManagerFor(button)!.GetVisualDescendants().OfType<NotificationCard>().Single();
        Assert.True(card.Opacity < 1, "the toast should still be rising in");

        Settle(400);
        card.Close();
        Tick(40);
        Assert.False(card.IsClosed);
        Settle(400);
        Assert.True(card.IsClosed);
    }

    [AvaloniaFact]
    public void A_still_dialog_and_its_scrim_arrive_at_once()
    {
        var dialog = new Dialog { Title = "عنوان الحوار", Content = new TextBlock { Text = "نص الحوار." } };
        var opener = new Button { Content = "فتح الحوار" };
        var window = Show(new StackPanel { Margin = new Thickness(24), Children = { opener } }, width: 900, height: 700);
        Motion.SetStill(window, true);
        dialog.Open(opener);
        Tick(40);
        Assert.Equal(1, dialog.Opacity, 2);
        Assert.True(IsIdentity(dialog.RenderTransform));
        Assert.Equal(1, DialogStack.Of(dialog)!.Layers.Single(l => l.Dialog == dialog).Scrim.Opacity, 2);
    }

    [AvaloniaFact]
    public void A_still_tooltip_shows_without_rising()
    {
        var tip = new ToolTip { Content = "تعديل" };
        var host = new Button { Content = "زر", [ToolTip.TipProperty] = tip };
        var window = Show(new StackPanel { Margin = new Thickness(40), Children = { host } });
        Motion.SetStill(window, true);
        ToolTip.SetIsOpen(host, true);
        Tick(40);
        Assert.Null(tip.Transitions);
        Assert.Equal(1, tip.Opacity, 2);
        Assert.True(IsIdentity(tip.RenderTransform));
        ToolTip.SetIsOpen(host, false);
    }

    [AvaloniaFact]
    public void A_still_switch_thumb_and_tick_land_at_once()
    {
        var toggle = new ToggleSwitch();
        var check = new CheckBox { Content = "موافق" };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { toggle, check } });
        Motion.SetStill(window, true);
        Tick(20);
        Assert.Null(toggle.KnobTransitions);
        var knobs = Part<Panel>(toggle, "PART_MovingKnobs");

        toggle.IsChecked = true;
        check.IsChecked = true;
        Tick(40);
        var left = Canvas.GetLeft(knobs);
        var glyph = Part<global::Avalonia.Controls.Shapes.Path>(check, "CheckGlyph");
        Assert.Equal(1, glyph.RenderTransform!.Value.M11, 2);
        Settle(400);
        Assert.Equal(Canvas.GetLeft(knobs), left, 1);
    }

    [AvaloniaFact]
    public void A_still_skeleton_is_a_plain_fill()
    {
        var skeleton = new Skeleton { Classes = { "text" }, Width = 200 };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { skeleton } });
        Motion.SetStill(window, true);
        Settle(300);
        Assert.Equal(0, skeleton.Phase);
        Assert.Equal(ColorOf((IBrush)Res(skeleton, "InsSkelBase")), ColorOf(skeleton.Highlight));
    }

    private static bool HasSoft(BoxShadows shadows)
    {
        foreach (var s in shadows) if (!s.IsInset && s.Blur > 0) return true;
        return false;
    }

    private static bool HasInset(BoxShadows shadows)
    {
        foreach (var s in shadows) if (s.IsInset) return true;
        return false;
    }

    [AvaloniaFact]
    public void A_still_window_drops_the_soft_shadows_and_keeps_the_highlight()
    {
        var island = new Border { Classes = { "island" }, Child = new TextBlock { Text = "جزيرة" } };
        var panel = new Border { Classes = { "panel" }, Width = 200, Height = 80 };
        var button = new Button { Classes = { "success" }, Content = "حفظ" };
        var slider = new Slider { Width = 200 };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { island, panel, button, slider } });
        var drop = Part<Border>(button, "PART_Shadow");
        var knob = Part<Border>(slider.GetVisualDescendants().OfType<Thumb>().First(), "PART_Face");
        Assert.True(HasSoft(island.BoxShadow), "a moving window's island keeps its drop");
        Assert.True(HasSoft(panel.BoxShadow), "a moving window's panel keeps its drop");
        Assert.True(HasSoft(drop.BoxShadow) && HasSoft(knob.BoxShadow));

        Motion.SetStill(window, true);
        Settle(300);
        Assert.False(HasSoft(island.BoxShadow));
        Assert.False(HasSoft(panel.BoxShadow));
        Assert.True(HasInset(island.BoxShadow), "the island's bevel stays");
        Assert.False(HasSoft(drop.BoxShadow) || HasSoft(knob.BoxShadow), "a toned button's drop and the slider's knob go flat too");

        Motion.SetStill(window, false);
        Settle(300);
        Assert.True(HasSoft(island.BoxShadow));
        Assert.True(HasSoft(panel.BoxShadow));
    }
}
