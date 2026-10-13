using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class FeedbackTests
{
    private static Window ShowIn(Control content, FlowDirection direction = FlowDirection.LeftToRight, ThemeVariant? theme = null) =>
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { content } }, direction, theme);

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

    // ---------------------------------------------------------------- alert

    [AvaloniaTheory]
    [InlineData("ok", "InsToneOkBg", "InsToneOk", "InsAlertOkCircle", "InsAlertOkBorder", "InsAlertIconOk")]
    [InlineData("warn", "InsToneWarnBg", "InsToneWarn", "InsAlertWarnCircle", "InsAlertWarnBorder", "InsAlertIconWarn")]
    [InlineData("bad", "InsToneBadBg", "InsToneBad", "InsAlertBadCircle", "InsAlertBadBorder", "InsAlertIconBad")]
    [InlineData("info", "InsToneBrandBg", "InsAlertInfoText", "InsPrimary", "InsAlertInfoBorder", "InsAlertIconInfo")]
    public void Alert_tones_paint_the_circle_and_the_message(string tone, string bg, string text, string circle, string border, string icon)
    {
        var alert = new Alert { Content = "تم الحفظ." };
        alert.Classes.Add(tone);
        ShowIn(alert);

        var message = Part<Border>(alert, "PART_Message");
        var disc = Part<Border>(alert, "PART_Icon");
        AssertBrush(alert, bg, () => message.Background);
        AssertBrush(alert, text, () => alert.Foreground);
        AssertBrush(alert, border, () => message.BorderBrush);
        AssertBrush(alert, border, () => disc.BorderBrush);
        AssertBrush(alert, circle, () => disc.Background);
        Assert.Same(Res(alert, icon), alert.Icon);
        Assert.Equal(44, disc.Bounds.Width);
        Assert.Equal(Res(alert, "InsRMd"), message.CornerRadius);
        Assert.Equal(14, alert.FontSize);
    }

    [AvaloniaFact]
    public void Alert_glyph_is_dark_on_the_circle_in_dark_except_info()
    {
        var ok = new Alert { Classes = { "ok" }, Content = "تم" };
        var info = new Alert { Classes = { "info" }, Content = "معلومة" };
        var window = Show(new StackPanel { Children = { ok, info } }, theme: ThemeVariant.Dark);

        Assert.Equal(Color.Parse("#0B1220"), ColorOf(ok.IconForeground));
        AssertBrush(info, "InsOnPrimary", () => info.IconForeground);
        AssertBrush(info, "InsText1", () => info.Foreground);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Alert_circle_sits_at_the_start_in_a_bite_cut_from_the_message(string dir)
    {
        var alert = new Alert { Classes = { "ok" }, Content = "تم الحفظ." };
        var window = ShowIn(alert, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);

        var disc = Part<Border>(alert, "PART_Icon");
        var message = Part<Border>(alert, "PART_Message");
        var discX = Center(disc, window).X;
        var alertX = Center(alert, window).X;
        Assert.True(dir == "rtl" ? discX > alertX : discX < alertX, $"circle at {discX}, alert centre {alertX}");

        var clip = Assert.IsAssignableFrom<Geometry>(message.Clip);
        var h = message.Bounds.Height;
        Assert.False(clip.FillContains(new Point(2, h / 2)));
        Assert.True(clip.FillContains(new Point(30, h / 2)));
        Assert.True(clip.FillContains(new Point(60, 1)));
        Assert.False(clip.FillContains(new Point(2, 1)));
    }

    [AvaloniaFact]
    public void Alert_close_dismisses_and_hides_it()
    {
        var alert = new Alert { Classes = { "info" }, Content = "رسالة قصيرة للاطلاع.", IsDismissible = true };
        var raised = 0;
        alert.Dismissed += (_, _) => raised++;
        var window = ShowIn(alert);

        var close = Part<Button>(alert, "PART_Close");
        Assert.True(close.IsVisible);
        Assert.Equal(24, close.Bounds.Width);
        Click(close, window);
        Settle(600);

        Assert.False(alert.IsVisible);
        Assert.Equal(1, raised);
        alert.IsVisible = true;
        Settle();
        Assert.True(alert.Bounds.Height >= 44);
        Assert.Equal(1, alert.Opacity);
    }

    [AvaloniaFact]
    public void Alert_close_is_hidden_unless_dismissible()
    {
        var alert = new Alert { Content = "تم" };
        ShowIn(alert);
        Assert.False(Part<Button>(alert, "PART_Close").IsVisible);
    }

    // ---------------------------------------------------------------- pill, badge

    [AvaloniaFact]
    public void Pill_is_neutral_by_default_and_the_dot_follows_its_class()
    {
        var plain = new Pill { Content = "مسودة" };
        var dotted = new Pill { Classes = { "ok", "dot" }, Content = "مكتمل" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { plain, dotted } });

        AssertBrush(plain, "InsToneMuteBg", () => Part<Border>(plain, "PART_Face").Background);
        AssertBrush(plain, "InsToneMute", () => plain.Foreground);
        Assert.False(Part<Ellipse>(plain, "PART_Dot").IsVisible);

        var dot = Part<Ellipse>(dotted, "PART_Dot");
        Assert.True(dot.IsVisible);
        Assert.Equal(7, dot.Bounds.Width);
        AssertBrush(dotted, "InsToneOk", () => dot.Fill);
        AssertBrush(dotted, "InsToneOkBd", () => Part<Border>(dotted, "PART_Face").BorderBrush);
        Assert.Equal(26, dotted.Bounds.Height, 0);
    }

    [AvaloniaFact]
    public void Brand_pill_and_badges_read_in_text1_in_dark()
    {
        var pill = new Pill { Classes = { "brand" }, Content = "مميز" };
        var badge = new Badge { Classes = { "ok" }, Content = "12" };
        Show(new StackPanel { Children = { pill, badge } }, theme: ThemeVariant.Dark);

        AssertBrush(pill, "InsText1", () => pill.Foreground);
        AssertBrush(badge, "InsText1", () => badge.Foreground);
        AssertBrush(badge, "InsToneOkBg", () => badge.Background);
    }

    [AvaloniaFact]
    public void Pressable_pill_is_a_button_that_lifts()
    {
        var button = new Button { Classes = { "pill", "ok", "dot" }, Content = "مكتمل" };
        var window = ShowIn(button);

        Assert.True(Part<Ellipse>(button, "PART_Dot").IsVisible);
        AssertBrush(button, "InsToneOkBg", () => Part<Border>(button, "PART_Face").Background);
        window.MouseMove(Center(button, window));
        Settle();
        Assert.Contains(":pointerover", button.Classes);
        Assert.Equal(-1, button.RenderTransform!.Value.M32, 1);
    }

    [AvaloniaFact]
    public void Badge_is_a_brand_count_that_pops_when_it_changes()
    {
        var badge = new Badge { Content = "12" };
        ShowIn(badge);

        AssertBrush(badge, "InsToneBrandBg", () => badge.Background);
        AssertBrush(badge, "InsToneBrand", () => badge.Foreground);
        Assert.True(badge.Bounds.Width >= 22);

        badge.Content = "13";
        Tick(30);
        var scale = badge.RenderTransform?.Value.M11 ?? 1;
        Assert.True(scale < 1, $"scale {scale}");
        Settle();
        Assert.Equal(1, badge.RenderTransform?.Value.M11 ?? 1, 2);
    }

    [AvaloniaTheory]
    [InlineData("mute", "InsToneMuteBg")]
    [InlineData("bad", "InsToneBadBg")]
    [InlineData("warn", "InsToneWarnBg")]
    public void Badge_tones(string tone, string bg)
    {
        var badge = new Badge { Classes = { tone }, Content = "3" };
        ShowIn(badge);
        AssertBrush(badge, bg, () => badge.Background);
    }

    // ---------------------------------------------------------------- avatar, money

    [AvaloniaFact]
    public void Avatar_sizes_and_the_group_overlaps_with_the_first_on_top()
    {
        var sm = new Avatar { Classes = { "sm" }, Content = "ل" };
        var lg = new Avatar { Classes = { "lg" }, Content = "ل" };
        var a = new Avatar { Content = "ل" };
        var b = new Avatar { Content = "ع" };
        var more = new Avatar { Classes = { "more" }, Content = "+4" };
        var group = new AvatarGroup { Children = { a, b, more } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { sm, lg, group } });

        Assert.Equal(30, sm.Bounds.Width);
        Assert.Equal(46, lg.Bounds.Width);
        Assert.Equal(38 * 3 - 16, group.Bounds.Width);
        Assert.Equal(30, b.Bounds.X);
        Assert.True(a.ZIndex > b.ZIndex && b.ZIndex > more.ZIndex);
        Assert.True(Part<Ellipse>(a, "PART_Ring").IsVisible);
        Assert.False(Part<Ellipse>(sm, "PART_Ring").IsVisible);
        AssertBrush(more, "InsToneMuteBg", () => more.Background);
        Assert.Equal(FlowDirection.LeftToRight, more.GetVisualDescendants().OfType<TextBlock>().First().FlowDirection);
        Assert.IsType<LinearGradientBrush>(a.Background);
        Assert.Equal(FlowDirection.LeftToRight, Part<Border>(a, "PART_Face").FlowDirection);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Money_keeps_the_figure_ltr_and_the_currency_after_it(string dir)
    {
        var money = new Money { Classes = { "neg" }, Value = "-1,500.00", Currency = "ر.س" };
        var window = ShowIn(money, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);

        var value = Part<TextBlock>(money, "PART_Value");
        var currency = Part<TextBlock>(money, "PART_Currency");
        Assert.Equal(FlowDirection.LeftToRight, value.FlowDirection);
        Assert.Equal(14 * 0.68, currency.FontSize, 2);
        AssertBrush(money, "InsToneBad", () => value.Foreground);
        AssertBrush(money, "InsText2", () => currency.Foreground);
        var vx = Center(value, window).X;
        var cx = Center(currency, window).X;
        Assert.True(dir == "rtl" ? cx < vx : cx > vx);
    }

    // ---------------------------------------------------------------- loading

    [AvaloniaFact]
    public void Skeleton_shapes_and_the_short_last_line()
    {
        var circle = new Skeleton { Classes = { "circle" } };
        var title = new Skeleton { Classes = { "title" } };
        var block = new Skeleton { Classes = { "block" } };
        var t1 = new Skeleton { Classes = { "text" } };
        var t2 = new Skeleton { Classes = { "text" } };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { circle, title, block, t1, t2 } });

        Assert.Equal(40, circle.Bounds.Width);
        Assert.Equal(20, title.Bounds.Height);
        Assert.Equal(0.4, title.FillWidth);
        Assert.Equal(128, block.Bounds.Height);
        Assert.Equal(Res(block, "InsRMd"), block.CornerRadius);
        Assert.Equal(12, t1.Bounds.Height);
        Assert.Equal(1, t1.FillWidth);
        Assert.Equal(0.6, t2.FillWidth);
        AssertBrush(t1, "InsSkelBase", () => t1.Base);
        // The headless clock can stall under an endless animation once the window settles, so this checks the loop has started, not its pace.
        Assert.InRange(t1.Phase, 1e-6, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void A_lone_text_line_is_full_width()
    {
        var line = new Skeleton { Classes = { "text" } };
        var window = ShowIn(line);
        Assert.Equal(1, line.FillWidth);
        window.Close();
    }

    [AvaloniaFact]
    public void LoadingSpinner_turns_and_takes_the_button_colour_inside_a_button()
    {
        var spinner = new LoadingSpinner();
        var small = new LoadingSpinner { Classes = { "sm" } };
        var inButton = new LoadingSpinner { Classes = { "sm" } };
        var button = new Button { Content = inButton };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { spinner, small, button } });

        Assert.Equal(32, spinner.Bounds.Width);
        Assert.Equal(3, spinner.Thickness);
        Assert.Equal(15, small.Bounds.Width);
        Assert.Equal(2, small.Thickness);
        AssertBrush(spinner, "InsPrimaryMid", () => spinner.Arc);
        Assert.Null(inButton.Arc);
        Assert.Null(inButton.Track);
        Assert.Equal(FlowDirection.LeftToRight, spinner.FlowDirection);
        // The headless clock can stall under an endless animation once the window settles, so this checks the loop has started, not its pace.
        Assert.InRange(spinner.Angle, 1e-6, 360);
        window.Close();
    }

    [AvaloniaFact]
    public void EmptyState_shows_icon_title_hint_and_action()
    {
        var action = new Button { Content = "ملف جديد" };
        var empty = new EmptyState { Icon = Geometry.Parse("M3 7h18"), Title = "لا ملفات بعد", Hint = "أضف أول ملف ليظهر هنا.", Content = action };
        var bare = new EmptyState { Title = "لا نتائج" };
        Show(new StackPanel { Children = { empty, bare } });

        Assert.True(Part<Icon>(empty, "PART_Icon").IsVisible);
        Assert.Equal(32, Part<Icon>(empty, "PART_Icon").Bounds.Width);
        Assert.Equal("لا ملفات بعد", Part<TextBlock>(empty, "PART_Title").Text);
        AssertBrush(empty, "InsText2", () => Part<TextBlock>(empty, "PART_Hint").Foreground);
        Assert.True(action.IsEffectivelyVisible);
        Assert.False(Part<Icon>(bare, "PART_Icon").IsVisible);
        Assert.False(Part<TextBlock>(bare, "PART_Hint").IsVisible);
    }

    // ---------------------------------------------------------------- progress

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void ProgressBar_fills_from_the_start_edge(string dir)
    {
        var bar = new ProgressBar { Value = 64, Width = 400 };
        var window = ShowIn(bar, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);

        var indicator = Part<Border>(bar, "PART_Indicator");
        Assert.Equal(8, bar.Bounds.Height);
        Assert.Equal(256, indicator.Bounds.Width, 0);
        AssertBrush(bar, "InsCtlBgHover", () => bar.Background);
        AssertBrush(bar, "InsPrimaryMid", () => indicator.Background);
        Assert.Equal(FlowDirection.LeftToRight, indicator.FlowDirection);
        var ix = Center(indicator, window).X;
        var bx = Center(bar, window).X;
        Assert.True(dir == "rtl" ? ix > bx : ix < bx, $"fill at {ix}, bar at {bx}");
    }

    [AvaloniaFact]
    public void ProgressBar_sizes_tones_and_indeterminate()
    {
        var sm = new ProgressBar { Classes = { "sm" }, Value = 10 };
        var lg = new ProgressBar { Classes = { "lg" }, Value = 10 };
        var warn = new ProgressBar { Classes = { "warn" }, Value = 88 };
        var busy = new ProgressBar { IsIndeterminate = true };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { sm, lg, warn, busy } });

        Assert.Equal(4, sm.Bounds.Height);
        Assert.Equal(12, lg.Bounds.Height);
        Assert.Same(Res(warn, "InsProgressWarnFill"), warn.Foreground);
        var sweep = Part<Shimmer>(busy, "IndeterminateRoot");
        Assert.True(sweep.IsVisible);
        Assert.False(Part<Panel>(busy, "DeterminateRoot").IsVisible);
        Assert.False(Part<Shimmer>(warn, "IndeterminateRoot").IsVisible);
        // The headless clock can stall under an endless animation once the window settles, so this checks the loop has started, not its pace.
        Assert.InRange(sweep.Phase, 1e-6, 1);
        Assert.Equal(0, Part<Shimmer>(warn, "IndeterminateRoot").Phase);
        window.Close();
    }

    [AvaloniaFact]
    public void ProgressBar_glides_to_a_new_value()
    {
        var bar = new ProgressBar { Value = 20, Width = 400 };
        ShowIn(bar);
        var indicator = Part<Border>(bar, "PART_Indicator");
        bar.Value = 70;
        Tick(60);
        Assert.InRange(indicator.Bounds.Width, 81, 279);
        Settle();
        Assert.Equal(280, indicator.Bounds.Width, 0);
    }

    [AvaloniaFact]
    public void ProgressRing_labels_its_value_and_sizes()
    {
        var ring = new ProgressRing { Value = 72 };
        var lg = new ProgressRing { Classes = { "lg" }, Value = 72 };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { ring, lg } });

        Assert.Equal("72%", ring.Label);
        Assert.Equal("72%", Part<TextBlock>(ring, "PART_Label").Text);
        Assert.Equal(52, ring.Bounds.Width);
        Assert.Equal(80, lg.Bounds.Width);
        Assert.Equal(16, Part<TextBlock>(lg, "PART_Label").FontSize);
        AssertBrush(ring, "InsPrimaryMid", () => ring.Fill);
        Assert.Equal(FlowDirection.LeftToRight, ring.FlowDirection);
        ring.Maximum = 200;
        Assert.Equal("36%", ring.Label);
    }

    // ---------------------------------------------------------------- save

    [AvaloniaFact]
    public void SaveStatus_holds_its_place_then_says_saving()
    {
        var status = new SaveStatus();
        var window = ShowIn(status);

        Assert.Contains(":idle", status.Classes);
        Assert.Equal(0, status.Opacity);
        Assert.Equal("", status.DisplayText);
        Assert.True(status.Bounds.Height >= 20);

        status.State = SaveState.Saving;
        Settle(300);
        Assert.Equal("جارٍ الحفظ…", Part<TextBlock>(status, "PART_Text").Text);
        Assert.Equal(1, Part<LoadingSpinner>(status, "PART_Spinner").Opacity);
        Assert.Equal(1, status.Opacity);
        window.Close();
    }

    [AvaloniaFact]
    public void SaveStatus_colours_saved_and_error()
    {
        var status = new SaveStatus();
        ShowIn(status);

        status.State = SaveState.Saved;
        Settle(400);
        Assert.Equal("حُفظ", status.DisplayText);
        Assert.Equal(0, Part<LoadingSpinner>(status, "PART_Spinner").Opacity);
        Assert.True(Part<Icon>(status, "PART_Glyph").IsVisible);
        AssertBrush(status, "InsToneOk", () => status.Foreground);
        status.SetState(SaveState.Error, "تعذر حفظ المسودة");
        Settle(400);
        Assert.Equal("تعذر حفظ المسودة", status.DisplayText);
        AssertBrush(status, "InsToneBad", () => status.Foreground);
        Assert.Equal(2.5, Part<Icon>(status, "PART_Glyph").StrokeThickness);

        status.SetState(SaveState.Saved);
        Assert.Equal("حُفظ", status.DisplayText);
    }

    [AvaloniaFact]
    public void Autosave_reports_saving_then_saved_after_the_pause()
    {
        var status = new SaveStatus();
        var box = new TextBox();
        var form = new StackPanel { Children = { status, box } };
        Autosave.SetDelay(form, 150);
        var window = ShowIn(form);

        box.Focus();
        box.Text = "ملاحظة";
        Tick(60);
        Assert.Equal(SaveState.Idle, status.State);
        Tick(250);
        Assert.Equal(SaveState.Saved, status.State);
        window.Close();
    }

    [AvaloniaFact]
    public void Autosave_waits_for_a_handler_that_saves_elsewhere()
    {
        var status = new SaveStatus();
        var box = new TextBox();
        var form = new StackPanel { Children = { status, box } };
        Autosave.SetDelay(form, 100);
        AutosaveEventArgs? pending = null;
        Autosave.AddSavingHandler(form, (_, e) => { e.Handled = true; pending = e; });
        ShowIn(form);

        box.Focus();
        box.Text = "أ";
        Tick(250);
        Assert.NotNull(pending);
        Assert.Equal(SaveState.Saving, status.State);
        pending!.Done(false);
        Assert.Equal(SaveState.Error, status.State);
    }

    // ---------------------------------------------------------------- motion

    [AvaloniaFact]
    public void Reveal_children_rise_one_after_another_and_replay()
    {
        var first = new Border { Height = 30 };
        var fourth = new Border { Height = 30 };
        var grid = new StackPanel { Children = { first, new Border { Height = 30 }, new Border { Height = 30 }, fourth } };
        Entrance.SetReveal(grid, true);
        ShowIn(grid);
        Assert.Equal(1, grid.Opacity);
        Assert.Null(fourth.RenderTransform);

        Entrance.Replay(grid);
        Tick(80);
        Assert.True(grid.Opacity < 1, $"opacity {grid.Opacity}");
        var y1 = first.RenderTransform?.Value.M32 ?? 0;
        var y4 = fourth.RenderTransform?.Value.M32 ?? 0;
        Assert.True(y4 > y1, $"first at {y1}, fourth at {y4}");
        Settle(800);
        Assert.Null(fourth.RenderTransform);
        Assert.Equal(1, grid.Opacity);
    }

    [AvaloniaTheory]
    [InlineData(EntranceEffect.Rise)]
    [InlineData(EntranceEffect.Pop)]
    [InlineData(EntranceEffect.Slide)]
    public void Single_entrances_run_again_on_replay(EntranceEffect effect)
    {
        var box = new Border { Width = 60, Height = 30 };
        Entrance.SetEffect(box, effect);
        ShowIn(box);
        Assert.Null(box.RenderTransform);
        Entrance.Replay(box);
        Tick(30);
        var m = box.RenderTransform?.Value ?? Matrix.Identity;
        Assert.False(m.IsIdentity, $"{effect} did not move");
        Settle();
        Assert.Null(box.RenderTransform);
        Assert.Equal(1, box.Opacity);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Slide_comes_from_the_leading_edge(string dir)
    {
        var box = new Border { Width = 60, Height = 30, Background = Brushes.Red, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center };
        Entrance.SetEffect(box, EntranceEffect.Slide);
        var window = ShowIn(box, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        var rest = Center(box, window).X;
        Entrance.Replay(box);
        Tick(20);
        var moving = box.TranslatePoint(new Point(30, 15), window)!.Value.X;
        Assert.True(dir == "rtl" ? moving > rest : moving < rest, $"at {moving}, rests at {rest}");
    }

    [AvaloniaFact]
    public void Transitions_of_nth_children_still_finish()
    {
        var pill = new Button { Classes = { "pill" }, Content = "الأول" };
        var status = new SaveStatus();
        var plain = new StackPanel { Spacing = 8, Children = { new Border { Height = 10 }, new Border { Height = 10 }, pill, status } };
        var revealed = new Button { Classes = { "pill" }, Content = "الثاني" };
        var reveal = new StackPanel { Children = { new Border { Height = 10 }, revealed } };
        Entrance.SetReveal(reveal, true);
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { plain, reveal } });
        Settle(600);

        window.MouseMove(Center(pill, window));
        status.State = SaveState.Saved;
        Settle(600);
        Assert.Equal(-1, pill.RenderTransform!.Value.M32, 2);
        AssertBrush(status, "InsToneOk", () => status.Foreground);
        window.MouseMove(Center(revealed, window));
        Settle(600);
        Assert.Equal(-1, revealed.RenderTransform!.Value.M32, 2);
    }

    // ---------------------------------------------------------------- looks

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Feedback_in_every_look(string theme, string dir)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 12,
            Children =
            {
                new Alert { Classes = { "ok" }, Content = "تم الحفظ." },
                new Alert { Classes = { "info" }, Content = "معلومة للاطلاع.", IsDismissible = true },
                new WrapPanel { ItemSpacing = 8, Children = { new Pill { Classes = { "ok", "dot" }, Content = "مكتمل" }, new Pill { Classes = { "brand", "dot" }, Content = "مميز" }, new Badge { Content = "12" }, new Avatar { Content = "ل ح" }, new Money { Value = "1,249.00", Currency = "ر.س" } } },
                new ProgressBar { Value = 64 },
                new ProgressBar { Classes = { "warn" }, Value = 88 },
                new WrapPanel { ItemSpacing = 12, Children = { new ProgressRing { Value = 72 }, new LoadingSpinner(), new SaveStatus { State = SaveState.Saved }, new SaveStatus { State = SaveState.Error } } },
                new Skeleton { Classes = { "text" } },
            },
        };
        var window = Show(panel, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, 640, 520);
        Capture(window, $"feedback-{theme}-{dir}");
        window.Close();
    }
}
