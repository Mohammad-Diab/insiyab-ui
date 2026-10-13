using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class ContentTests
{
    private const string NoteText = "هذه الجملة تحتاج مصدرًا.";
    private const string CommentText = "راجعت الملف، والعنوان يحتاج تعديلًا بسيطًا.";

    private static readonly string[] Months = ["يناير", "فبراير", "مارس", "أبريل", "مايو"];

    private static FlowDirection Dir(string name) => name == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static Window ShowIn(Control content, FlowDirection direction = FlowDirection.LeftToRight, ThemeVariant? theme = null) =>
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { content } }, direction, theme);

    private static Point Place(Visual visual, Window window) => visual.TranslatePoint(new Point(0, 0), window)!.Value;

    private static Rect BoxIn(Control control, Window window)
    {
        var a = control.TranslatePoint(new Point(0, 0), window)!.Value;
        var b = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), window)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static void Hover(Window window, Point point, int milliseconds = 250)
    {
        window.MouseMove(point);
        Settle(milliseconds);
    }

    private static void ClickAt(Window window, Point point)
    {
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Settle();
    }

    private static void RightClick(Window window, Point point)
    {
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Right);
        window.MouseUp(point, MouseButton.Right);
        Settle();
    }

    private static void Press(Window window, Key key, PhysicalKey physical, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, physical, null);
        window.KeyRelease(key, modifiers, physical, null);
        Settle(120);
    }

    private static string? ClipText(Control control)
    {
        var read = TopLevel.GetTopLevel(control)!.Clipboard!.TryGetTextAsync();
        Settle(120);
        return read.Result;
    }

    // ---------------------------------------------------------------- quote, mark, note, ref

    private sealed record MarkRig(Window Window, TextBlock Host, Mark Mark, Note Note, ScrollViewer? Scroll);

    private static MarkRig RigMark(FlowDirection direction, string? tone = null, bool far = false, double height = 420)
    {
        var note = new Note { Content = NoteText };
        var mark = new Mark { Text = "جملة تحتاج مراجعة", Note = note };
        if (tone is not null && tone != "warn") mark.Classes.Add(tone);
        var host = new TextBlock { TextWrapping = TextWrapping.Wrap };
        host.Inlines!.Add(new Run("في هذه الفقرة "));
        host.Inlines.Add(mark);
        host.Inlines.Add(new Run("، وبعدها بقية النص."));
        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        stack.Children.Add(host);
        if (far) stack.Children.Add(new Border { Height = 900 });
        stack.Children.Add(note);
        if (!far) return new MarkRig(Show(stack, direction, null, 640, height), host, mark, note, null);
        var scroll = new ScrollViewer { Content = stack };
        return new MarkRig(Show(scroll, direction, null, 640, height), host, mark, note, scroll);
    }

    private static Point MarkPoint(MarkRig rig)
    {
        var range = rig.Mark.Range()!.Value;
        var rect = rig.Host.TextLayout.HitTestTextRange(range.Start, range.End - range.Start).First();
        return rig.Host.TranslatePoint(rect.Center, rig.Window)!.Value;
    }

    private static Point FirstWordPoint(MarkRig rig)
    {
        var rect = rig.Host.TextLayout.HitTestTextRange(0, 3).First();
        return rig.Host.TranslatePoint(rect.Center, rig.Window)!.Value;
    }

    [AvaloniaFact]
    public void Quote_has_the_web_frame()
    {
        var quote = new Border { Child = new TextBlock { Text = "نص مقتبس." } };
        quote.Classes.Add("quote");
        ShowIn(quote);

        Assert.Equal(new Thickness(3, 1, 1, 1), quote.BorderThickness);
        Assert.Equal(Res(quote, "InsRMd"), quote.CornerRadius);
        Assert.Equal(new Thickness(18, 14, 16, 14), quote.Padding);
        AssertBrush(quote, "InsGlassInner", () => quote.Background);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Mark_hover_lights_its_note(string dir)
    {
        var rig = RigMark(Dir(dir));
        Hover(rig.Window, MarkPoint(rig));
        Assert.Contains(":lit", rig.Mark.Classes);
        Assert.Contains(":lit", rig.Note.Classes);

        Hover(rig.Window, FirstWordPoint(rig));
        Assert.DoesNotContain(":lit", rig.Mark.Classes);
        Assert.DoesNotContain(":lit", rig.Note.Classes);
    }

    [AvaloniaTheory]
    [InlineData("warn", "Warn")]
    [InlineData("ok", "Ok")]
    [InlineData("bad", "Bad")]
    [InlineData("info", "Info")]
    [InlineData("brand", "Brand")]
    public void Mark_tones_fill_and_light(string tone, string key)
    {
        var rig = RigMark(FlowDirection.RightToLeft, tone);
        AssertBrush(rig.Host, "InsTone" + key + "Bg", () => rig.Mark.Background);

        Hover(rig.Window, MarkPoint(rig));
        AssertBrush(rig.Host, "InsTone" + key + "Bd", () => rig.Mark.Background);
    }

    [AvaloniaFact]
    public void Note_hover_lights_its_marks()
    {
        var rig = RigMark(FlowDirection.RightToLeft);
        Hover(rig.Window, Center(rig.Note, rig.Window), 400);
        Assert.Contains(":lit", rig.Mark.Classes);
        var ring = Part<Border>(rig.Note, "PART_Ring");
        for (var i = 0; i < 10 && ring.Opacity < 1; i++) Settle(150);
        Assert.Equal(1, ring.Opacity, 0.02);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Mark_click_brings_its_note_into_view(string dir)
    {
        var rig = RigMark(Dir(dir), far: true, height: 300);
        Assert.Equal(0, rig.Scroll!.Offset.Y);
        ClickAt(rig.Window, MarkPoint(rig));
        Assert.True(rig.Scroll.Offset.Y > 0, "the note should have been scrolled to");
    }

    [AvaloniaFact]
    public void Ref_click_brings_its_note_into_view()
    {
        var note = new Note { Content = NoteText };
        var number = new Ref { Content = "1", Note = note };
        var host = new TextBlock { TextWrapping = TextWrapping.Wrap };
        host.Inlines!.Add(new Run("الجملة تحتاج مصدرًا"));
        host.Inlines.Add(new InlineUIContainer { Child = number, BaselineAlignment = BaselineAlignment.Superscript });
        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { host, new Border { Height = 900 }, note } };
        var scroll = new ScrollViewer { Content = stack };
        var window = Show(scroll, FlowDirection.LeftToRight, null, 640, 300);

        Click(number, window);
        Assert.True(scroll.Offset.Y > 0, "the note should have been scrolled to");
    }

    [AvaloniaFact]
    public void Ref_lights_on_hover_and_focus()
    {
        var note = new Note { Content = NoteText };
        var number = new Ref { Content = "1", Note = note, HorizontalAlignment = HorizontalAlignment.Left };
        var next = new Button { Content = "التالي" };
        var window = ShowIn(new StackPanel { Spacing = 12, Children = { number, note, next } });

        Hover(window, Center(number, window));
        Assert.Contains(":lit", number.Classes);
        Assert.Contains(":lit", note.Classes);

        Hover(window, new Point(630, 410));
        Assert.DoesNotContain(":lit", number.Classes);
        Assert.DoesNotContain(":lit", note.Classes);

        number.Focus(NavigationMethod.Tab);
        Settle(150);
        Assert.Contains(":lit", number.Classes);

        next.Focus();
        Settle(150);
        Assert.DoesNotContain(":lit", number.Classes);
    }

    [AvaloniaFact]
    public void Ref_number_is_centred()
    {
        var number = new Ref { Content = "1", HorizontalAlignment = HorizontalAlignment.Left };
        ShowIn(number);

        var text = number.GetVisualDescendants().OfType<TextBlock>().First();
        var centre = text.TranslatePoint(new Point(0, text.Bounds.Height / 2), number)!.Value.Y;
        Assert.InRange(Math.Abs(centre - number.Bounds.Height / 2), 0, 1.5);
    }

    [AvaloniaFact]
    public void Mark_peeks_its_note_when_out_of_sight()
    {
        var rig = RigMark(FlowDirection.LeftToRight, far: true, height: 300);
        Hover(rig.Window, MarkPoint(rig));
        Assert.True(ToolTip.GetIsOpen(rig.Host));
        Assert.Equal(NoteText, ToolTip.GetTip(rig.Host));

        Hover(rig.Window, FirstWordPoint(rig));
        Assert.False(ToolTip.GetIsOpen(rig.Host));
        Assert.Null(ToolTip.GetTip(rig.Host));
    }

    [AvaloniaFact]
    public void Mark_does_not_peek_a_visible_note()
    {
        var rig = RigMark(FlowDirection.LeftToRight);
        Hover(rig.Window, MarkPoint(rig));
        Assert.Contains(":lit", rig.Mark.Classes);
        Assert.False(ToolTip.GetIsOpen(rig.Host));
    }

    // ---------------------------------------------------------------- comments

    private static Comment Cmt(string text = CommentText, string? time = "قبل ساعتين", bool own = false, string avatar = "ل", string author = "ليلى")
    {
        var comment = new Comment { Avatar = avatar, Author = author, Time = time, Content = text };
        if (own) comment.Classes.Add("own");
        return comment;
    }

    private static Button Reply()
    {
        var button = new Button { Content = "رد" };
        button.Classes.Add("bare");
        button.Classes.Add("sm");
        return button;
    }

    [AvaloniaFact]
    public void Comment_reuses_the_small_avatar()
    {
        var comment = Cmt();
        ShowIn(comment);

        var avatar = Part<Avatar>(comment, "PART_Avatar");
        Assert.Contains("sm", avatar.Classes);
        Assert.Equal(30, avatar.Bounds.Width);
        Assert.Equal("ل", avatar.Content);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Own_comment_moves_to_the_other_side(string dir)
    {
        var plain = Cmt();
        var own = Cmt(own: true, avatar: "أ", author: "أنت");
        var window = ShowIn(new StackPanel { Spacing = 12, Children = { plain, own } }, Dir(dir));

        double Side(Comment c)
        {
            var avatar = BoxIn(Part<Avatar>(c, "PART_Avatar"), window);
            var body = BoxIn(Part<StackPanel>(c, "PART_Body"), window);
            return avatar.Center.X - body.Center.X;
        }

        if (dir == "rtl")
        {
            Assert.True(Side(plain) > 0, "a plain comment's avatar is on the right in RTL");
            Assert.True(Side(own) < 0, "an own comment's avatar is on the left in RTL");
        }
        else
        {
            Assert.True(Side(plain) < 0, "a plain comment's avatar is on the left in LTR");
            Assert.True(Side(own) > 0, "an own comment's avatar is on the right in LTR");
        }
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Own_actions_are_not_clipped(string dir)
    {
        var comment = Cmt(own: true, avatar: "أ", author: "أنت");
        var reply = Reply();
        comment.Actions = new StackPanel { Orientation = Orientation.Horizontal, Children = { reply } };
        var window = ShowIn(comment, Dir(dir));

        var button = BoxIn(reply, window);
        var whole = BoxIn(comment, window);
        var body = BoxIn(Part<StackPanel>(comment, "PART_Body"), window);
        Assert.True(button.Width > 20 && button.Height > 10, "the action keeps its size");
        Assert.True(button.Left >= whole.Left - 0.5 && button.Right <= whole.Right + 0.5, "the action lies inside the comment");
        if (dir == "rtl") Assert.InRange(Math.Abs(button.Left - body.Left), 0, 12);
        else Assert.InRange(Math.Abs(button.Right - body.Right), 0, 12);
    }

    [AvaloniaFact]
    public void Bare_actions_are_quiet_and_padded()
    {
        var comment = Cmt();
        var reply = Reply();
        comment.Actions = new StackPanel { Orientation = Orientation.Horizontal, Children = { reply } };
        var window = ShowIn(comment);

        AssertBrush(comment, "InsLabel", () => reply.Foreground);
        Assert.Equal(new Thickness(8, 4), reply.Padding);
        window.MouseMove(Center(reply, window));
        AssertBrush(comment, "InsToneBrand", () => reply.Foreground);
    }

    [AvaloniaFact]
    public void Time_dot_follows_the_time()
    {
        var timed = Cmt();
        var bare = Cmt(time: null);
        ShowIn(new StackPanel { Spacing = 12, Children = { timed, bare } });

        Assert.True(Part<TextBlock>(timed, "PART_Dot").IsVisible);
        Assert.False(Part<TextBlock>(bare, "PART_Dot").IsVisible);
    }

    [AvaloniaFact]
    public void Replies_hang_under_the_avatar()
    {
        var comment = Cmt();
        comment.Replies.Add(Cmt("عدلته، وهو جاهز الآن.", "قبل ساعة", false, "ع", "عمر"));
        var window = ShowIn(comment);

        var guide = BoxIn(Part<Border>(comment, "PART_Replies"), window);
        var avatar = BoxIn(Part<Avatar>(comment, "PART_Avatar"), window);
        Assert.InRange(Math.Abs(guide.Left + 1 - avatar.Center.X), 0, 1.5);
    }

    [AvaloniaFact]
    public void Comment_text_is_selectable()
    {
        var comment = Cmt();
        ShowIn(comment);

        var text = comment.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
        text.SelectAll();
        Assert.Equal(CommentText, text.SelectedText);
    }

    [AvaloniaFact]
    public void Comment_copy_takes_the_selection_or_all()
    {
        var comment = Cmt();
        ShowIn(comment);

        comment.CopyCommand.Execute(null);
        Assert.Equal(CommentText, ClipText(comment));

        var text = comment.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
        text.SelectionStart = 0;
        text.SelectionEnd = 6;
        comment.CopyCommand.Execute(null);
        Assert.Equal(CommentText[..6], ClipText(comment));
    }

    [AvaloniaFact]
    public void Comment_menu_wins_over_the_text_flyout()
    {
        var comment = Cmt();
        var menu = new ContextMenu { Items = { new MenuItem { Header = "رد" } } };
        comment.ContextMenu = menu;
        var window = ShowIn(comment);

        var text = comment.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
        var at = text.TranslatePoint(new Point(6, text.Bounds.Height / 2), window)!.Value;
        RightClick(window, at);

        Assert.True(menu.IsOpen);
        Assert.True(text.ContextFlyout?.IsOpen != true);
    }

    [AvaloniaFact]
    public void Reply_right_click_does_not_open_the_parent_menu()
    {
        var parent = Cmt();
        var menu = new ContextMenu { Items = { new MenuItem { Header = "رد" } } };
        parent.ContextMenu = menu;
        var reply = Cmt("عدلته، وهو جاهز الآن.", "قبل ساعة", false, "ع", "عمر");
        parent.Replies.Add(reply);
        var window = ShowIn(parent);

        var text = reply.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
        var at = text.TranslatePoint(new Point(6, text.Bounds.Height / 2), window)!.Value;
        RightClick(window, at);

        Assert.False(menu.IsOpen);
    }

    private sealed class Recorder : ICommand
    {
        public List<object?> Calls { get; } = [];

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Calls.Add(parameter);
    }

    [AvaloniaFact]
    public void SubmitCommand_runs_on_ctrl_enter_only()
    {
        var recorder = new Recorder();
        var box = new TextBox { AcceptsReturn = true };
        Comment.SetSubmitCommand(box, recorder);
        var window = ShowIn(box);

        box.Text = "سطر";
        box.Focus();
        Settle(120);
        Press(window, Key.Enter, PhysicalKey.Enter);
        Assert.Empty(recorder.Calls);

        var before = box.Text;
        Press(window, Key.Enter, PhysicalKey.Enter, RawInputModifiers.Control);
        Assert.Single(recorder.Calls);
        Assert.Equal(before, recorder.Calls[0]);
        Assert.Equal(before, box.Text);
    }

    // ---------------------------------------------------------------- countdown

    [AvaloniaFact]
    public void Clock_is_ltr_with_tabular_figures()
    {
        var countdown = new Countdown { Seconds = 600, StartPaused = true };
        ShowIn(countdown, FlowDirection.RightToLeft);

        Assert.Equal("10:00", countdown.ClockText);
        var clock = Part<TextBlock>(countdown, "PART_Clock");
        Assert.Equal(FlowDirection.LeftToRight, clock.FlowDirection);
        Assert.Contains(clock.FontFeatures!, f => f.Tag == "tnum");
    }

    [AvaloniaFact]
    public void Clock_width_does_not_change_with_the_digits()
    {
        var countdown = new Countdown { Seconds = 671, StartPaused = true };
        ShowIn(countdown);
        var clock = Part<TextBlock>(countdown, "PART_Clock");
        var wide = clock.Bounds.Width;

        countdown.Seconds = 600;
        Settle(300);
        Assert.Equal("10:00", countdown.ClockText);
        Assert.InRange(Math.Abs(wide - clock.Bounds.Width), 0, 0.5);
    }

    [AvaloniaTheory]
    [InlineData("rtl", "يومان")]
    [InlineData("ltr", "2d")]
    public void Days_are_said_in_the_reading_language(string dir, string days)
    {
        var countdown = new Countdown { Seconds = 200000, StartPaused = true };
        ShowIn(countdown, Dir(dir));

        Assert.Equal(days, countdown.DaysText);
        Assert.Equal("07:33:20", countdown.ClockText);
        Assert.True(Part<TextBlock>(countdown, "PART_Days").IsVisible);
    }

    [AvaloniaFact]
    public void Warns_then_ends()
    {
        var countdown = new Countdown { Seconds = 2 };
        var events = new List<CountdownState>();
        countdown.StateChanged += (_, e) => events.Add(e.State);
        ShowIn(countdown);

        Assert.Contains(":warn", countdown.Classes);
        Assert.Equal([CountdownState.Warn], events);

        Settle(2600);
        Assert.Contains(":done", countdown.Classes);
        Assert.Equal([CountdownState.Warn, CountdownState.End], events);
        Assert.Equal(0, countdown.Left);
        AssertBrush(countdown, "InsToneBadBg", () => Part<Border>(countdown, "PART_Face").Background);
    }

    [AvaloniaFact]
    public void Start_stop_restart()
    {
        var countdown = new Countdown { Seconds = 600, StartPaused = true };
        ShowIn(countdown);
        Assert.False(countdown.IsRunning);
        Assert.Null(countdown.Left);

        Assert.Equal(600, countdown.Start());
        Assert.True(countdown.IsRunning);

        Settle(1100);
        Assert.True(countdown.Stop() <= 599);
        Assert.False(countdown.IsRunning);
        Assert.Contains(":paused", countdown.Classes);

        Assert.Equal(600, countdown.Restart());
    }

    [AvaloniaFact]
    public void Large_size()
    {
        var countdown = new Countdown { Seconds = 600, StartPaused = true };
        countdown.Classes.Add("lg");
        ShowIn(countdown);

        Assert.Equal(24, countdown.FontSize);
    }

    // ---------------------------------------------------------------- charts

    private static Chart Bars(ChartType type = ChartType.Bar)
    {
        var chart = new Chart { Type = type, Labels = string.Join(",", Months), Width = 500 };
        chart.Series.Add(new ChartSeries("هذا العام", 120, 150, 98, 174, 160));
        chart.Series.Add(new ChartSeries("العام الماضي", 90, 130, 9, 140, 150));
        return chart;
    }

    private static (Window Window, ChartPlot Plot) ShowChart(Chart chart, FlowDirection direction = FlowDirection.LeftToRight)
    {
        var window = ShowIn(chart, direction);
        Settle(700);
        return (window, chart.GetVisualDescendants().OfType<ChartPlot>().Single());
    }

    private static Point InWindow(ChartPlot plot, Window window, Point point) => plot.TranslatePoint(point, window)!.Value;

    private static ChartPlot.Hit HitOf(ChartPlot plot, int series, int index) => plot.Hits.First(h => h.Series == series && h.Index == index);

    [AvaloniaFact]
    public void Chart_brushes_follow_the_tokens()
    {
        var chart = Bars();
        ShowIn(chart);

        AssertBrush(chart, "InsPrimary", () => chart.Brush1);
        AssertBrush(chart, "InsChart2", () => chart.Brush2);
        AssertBrush(chart, "InsChart3", () => chart.Brush3);
        AssertBrush(chart, "InsChart4", () => chart.Brush4);
        AssertBrush(chart, "InsChart5", () => chart.Brush5);
        AssertBrush(chart, "InsChart6", () => chart.Brush6);
        AssertBrush(chart, "InsLine", () => chart.GridBrush);
        AssertBrush(chart, "InsToneMuteBg", () => chart.TrackBrush);
    }

    [AvaloniaFact]
    public void Chart_data_sources()
    {
        var chart = Bars();
        Assert.Equal(5, chart.DataLabels.Count);
        Assert.Equal(2, chart.DataSeries.Count);

        var single = new Chart { Values = "3,5,4", SeriesName = "س" };
        Assert.Single(single.DataSeries);
        Assert.Equal("س", single.DataSeries[0].Name);

        chart.SetData(["a", "b"], new ChartSeries("x", 1, 2));
        Assert.Equal(["a", "b"], chart.DataLabels);
        Assert.Equal("x", chart.DataSeries.Single().Name);

        chart.ClearData();
        Assert.Equal(5, chart.DataLabels.Count);
        Assert.Equal(2, chart.DataSeries.Count);
    }

    [AvaloniaTheory]
    [InlineData("١٢٠", 120.0)]
    [InlineData("−5", -5.0)]
    [InlineData("1,234.5", 1234.5)]
    [InlineData("abc", double.NaN)]
    public void ParseNumber_reads_both_digit_sets(string text, double expected)
    {
        Assert.Equal(expected, ChartSeries.ParseNumber(text));
    }

    [AvaloniaFact]
    public void Format_compacts_in_the_reading_language()
    {
        var rtl = new Chart { FlowDirection = FlowDirection.RightToLeft };
        var ltr = new Chart { FlowDirection = FlowDirection.LeftToRight };

        Assert.Equal("12 ألف", rtl.Format(12000, true));
        Assert.Equal("12K", ltr.Format(12000, true));
        Assert.Equal("1,234.5", ltr.Format(1234.5, false));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Categories_run_in_reading_order(string dir)
    {
        var (_, plot) = ShowChart(Bars(), Dir(dir));

        var first = HitOf(plot, 0, 0).Box.X;
        var last = HitOf(plot, 0, 4).Box.X;
        if (dir == "rtl") Assert.True(first > last, "the first category is on the right");
        else Assert.True(first < last, "the first category is on the left");
    }

    [AvaloniaFact]
    public void Hovering_a_bar_shows_its_tip()
    {
        var chart = Bars();
        var (window, plot) = ShowChart(chart, FlowDirection.RightToLeft);

        Hover(window, InWindow(plot, window, HitOf(plot, 0, 2).Box.Center), 200);

        Assert.True(Part<Border>(chart, "PART_Tip").IsVisible);
        Assert.Equal("مارس · هذا العام", Part<TextBlock>(chart, "PART_TipLabel").Text);
        Assert.Equal("98", Part<TextBlock>(chart, "PART_TipValue").Text);
        Assert.Equal(2, plot.LitHit!.Index);
    }

    [AvaloniaFact]
    public void Bar_column_hit_reaches_a_short_bar()
    {
        var chart = Bars();
        var (window, plot) = ShowChart(chart, FlowDirection.RightToLeft);

        var bar = HitOf(plot, 1, 2).Box;
        Hover(window, InWindow(plot, window, new Point(bar.Center.X, Math.Max(12, bar.Bottom - 120))), 200);

        Assert.NotNull(plot.LitHit);
        Assert.Equal(1, plot.LitHit!.Series);
        Assert.Equal(2, plot.LitHit.Index);
        Assert.Equal("9", Part<TextBlock>(chart, "PART_TipValue").Text);
    }

    [AvaloniaFact]
    public void Line_crosshair_snaps_and_lists_every_series()
    {
        var chart = Bars(ChartType.Line);
        var (window, plot) = ShowChart(chart);

        var x = HitOf(plot, 0, 3).Box.Center.X + 6;
        Hover(window, InWindow(plot, window, new Point(x, 20)), 200);

        Assert.Equal(3, plot.CrossIndex);
        Assert.True(Part<StackPanel>(chart, "PART_TipRows").IsVisible);
        Assert.Equal(2, Part<StackPanel>(chart, "PART_TipRows").Children.Count);
        Assert.False(Part<TextBlock>(chart, "PART_TipValue").IsVisible);

        Hover(window, new Point(630, 410), 200);
        Assert.Equal(-1, plot.CrossIndex);
        Assert.False(Part<Border>(chart, "PART_Tip").IsVisible);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Chart_arrows_move_in_screen_order(string dir)
    {
        var chart = Bars();
        var (window, plot) = ShowChart(chart, Dir(dir));
        var (next, nextKey) = dir == "rtl" ? (Key.Left, PhysicalKey.ArrowLeft) : (Key.Right, PhysicalKey.ArrowRight);

        chart.Focus();
        Settle(120);
        Press(window, next, nextKey);
        Press(window, next, nextKey);
        Assert.Equal(1, plot.LitHit!.Index);

        Press(window, Key.Down, PhysicalKey.ArrowDown);
        Assert.Equal(1, plot.LitHit!.Series);

        Press(window, Key.End, PhysicalKey.End);
        Assert.Equal(4, plot.LitHit!.Index);
    }

    [AvaloniaFact]
    public void Chart_escape_clears()
    {
        var chart = Bars();
        var (window, plot) = ShowChart(chart);

        chart.Focus();
        Settle(120);
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Assert.NotNull(plot.LitHit);
        Assert.True(Part<Border>(chart, "PART_Tip").IsVisible);

        Press(window, Key.Escape, PhysicalKey.Escape);
        Assert.Null(plot.LitHit);
        Assert.False(Part<Border>(chart, "PART_Tip").IsVisible);
    }

    [AvaloniaFact]
    public void Line_keys_move_the_crosshair()
    {
        var chart = Bars(ChartType.Line);
        var (window, plot) = ShowChart(chart);

        chart.Focus();
        Settle(120);
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Assert.Equal(0, plot.CrossIndex);
        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Assert.Equal(1, plot.CrossIndex);
    }

    [AvaloniaFact]
    public void Chart_menu_copies_all_data()
    {
        var chart = Bars();
        var (window, plot) = ShowChart(chart, FlowDirection.RightToLeft);

        RightClick(window, InWindow(plot, window, new Point(8, 3)));

        var menu = chart.CopyMenu!;
        Assert.True(menu.IsOpen);
        var items = menu.Items.OfType<MenuItem>().ToArray();
        Assert.Equal(2, items.Length);
        Assert.False(items[0].IsEnabled);
        items[1].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal(chart.ToTabSeparated(), ClipText(chart));
    }

    [AvaloniaFact]
    public void Chart_menu_copies_the_value_under_the_pointer()
    {
        var chart = Bars();
        var (window, plot) = ShowChart(chart, FlowDirection.RightToLeft);

        var at = InWindow(plot, window, HitOf(plot, 0, 2).Box.Center);
        Hover(window, at, 200);
        RightClick(window, at);

        var items = chart.CopyMenu!.Items.OfType<MenuItem>().ToArray();
        Assert.True(items[0].IsEnabled);
        Assert.Equal("نسخ القيمة", items[0].Header);
        items[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.Equal("98", ClipText(chart));
    }

    [AvaloniaFact]
    public void Chart_ctrl_c_copies_the_lit_value_or_all()
    {
        var chart = Bars();
        var (window, plot) = ShowChart(chart);

        chart.Focus();
        Settle(120);
        Press(window, Key.C, PhysicalKey.C, RawInputModifiers.Control);
        Assert.Equal(chart.ToTabSeparated(), ClipText(chart));

        Press(window, Key.Right, PhysicalKey.ArrowRight);
        Press(window, Key.C, PhysicalKey.C, RawInputModifiers.Control);
        Assert.Equal("120", ClipText(chart));
    }

    [AvaloniaFact]
    public void ToTabSeparated_format()
    {
        var nl = Environment.NewLine;
        var expected = "\tهذا العام\tالعام الماضي" + nl + "يناير\t120\t90" + nl + "فبراير\t150\t130" + nl + "مارس\t98\t9" + nl + "أبريل\t174\t140" + nl + "مايو\t160\t150";

        Assert.Equal(expected, Bars().ToTabSeparated());
    }

    [AvaloniaFact]
    public void Chart_pens_are_reused()
    {
        var (_, plot) = ShowChart(Bars(ChartType.Line));

        var count = plot.PenCount;
        plot.InvalidateVisual();
        Settle(300);
        Assert.Equal(count, plot.PenCount);
        Assert.True(count > 0);
    }

    [AvaloniaFact]
    public void Spark_is_small_and_quiet()
    {
        var chart = new Chart { Type = ChartType.Spark, Values = "12,18,14" };
        ShowIn(chart);

        Assert.Equal(96, chart.Width);
        Assert.False(Part<WrapPanel>(chart, "PART_Legend").IsVisible);
        Assert.False(chart.Focusable);
    }

    [AvaloniaFact]
    public void Donut_legend_lists_categories_with_values()
    {
        var chart = new Chart { Type = ChartType.Donut, Labels = "أ,ب,ج,د,هـ", Values = "120,80,60,40,20", SeriesName = "العدد" };
        ShowIn(chart);

        var legend = Part<WrapPanel>(chart, "PART_Legend");
        Assert.Equal(5, legend.Children.Count);
        var value = ((StackPanel)legend.Children[0]).Children.OfType<TextBlock>().First(t => t.Classes.Contains("chart-key-value"));
        Assert.Equal("120", value.Text);
    }
}
