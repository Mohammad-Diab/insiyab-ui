using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// A pill's icon, a seek row's live content and marks, a control bar's subtitles content and card, the timeline's mute tone and the panel's hairline rules.
public class ReviewedChangesTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M5 12h14");

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_pill_icon_takes_the_dots_place(string dir)
    {
        var plain = new Pill { Classes = { "ok", "dot" }, Content = "مكتمل" };
        var iconic = new Pill { Classes = { "ok", "dot" }, Content = "مكتمل", Icon = Glyph };
        var w = Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { plain, iconic } }, Dir(dir));
        Assert.DoesNotContain(":icon", plain.Classes);
        Assert.True(Part<Ellipse>(plain, "PART_Dot").IsVisible);
        Assert.False(Part<Icon>(plain, "PART_Glyph").IsVisible);
        Assert.Contains(":icon", iconic.Classes);
        Assert.False(Part<Ellipse>(iconic, "PART_Dot").IsVisible);
        var glyph = Part<Icon>(iconic, "PART_Glyph");
        Assert.True(glyph.IsVisible);
        Assert.Same(Glyph, glyph.Data);
        Assert.Equal(12, glyph.Bounds.Width, 1);
        AssertBrush(iconic, "InsToneOk", () => glyph.Foreground);
        var text = Box(Part<ContentPresenter>(iconic, "PART_ContentPresenter"), w);
        var at = Box(glyph, w);
        if (dir == "ltr") Assert.True(at.Right <= text.Left + 0.5, $"glyph {at}, text {text}");
        else Assert.True(at.Left >= text.Right - 0.5, $"glyph {at}, text {text}");
        iconic.Icon = null;
        Settle(60);
        Assert.True(Part<Ellipse>(iconic, "PART_Dot").IsVisible);
        Assert.False(glyph.IsVisible);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_peeking_pill_with_an_icon_rests_as_a_ring_around_the_glyph_and_opens_over_it(string dir)
    {
        var pill = new Pill { Classes = { "warn", "dot", "peek" }, Content = "معلق", Icon = Glyph };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(40), Children = { new TextBlock { Text = "الحالة" }, pill, new TextBlock { Text = "بعدها" } } };
        var w = Show(row, Dir(dir), width: 500);
        var face = Part<Border>(pill, "PART_Face");
        var ground = Part<Border>(pill, "PART_Ground");
        Assert.InRange(pill.Bounds.Width, 21.5, 22.5);
        Assert.InRange(pill.Bounds.Height, 21.5, 22.5);
        Assert.True(Part<Icon>(pill, "PART_RingGlyph").IsVisible);
        Assert.False(Part<Ellipse>(pill, "PART_RingDot").IsVisible);
        Assert.False(Part<Ellipse>(pill, "PART_Dot").IsVisible);
        Assert.Equal(0, ground.Opacity);

        w.MouseMove(Center(pill, w));
        Settle();
        Assert.Contains(":pointerover", pill.Classes);
        Assert.Equal(1, ground.Opacity);
        Assert.True(Box(face, w).Width > 40);
        var glyphAt = Box(Part<Icon>(pill, "PART_Glyph"), w).Center;
        var ringGlyphAt = Box(Part<Icon>(pill, "PART_RingGlyph"), w).Center;
        Assert.InRange(glyphAt.X, ringGlyphAt.X - 1, ringGlyphAt.X + 1);
        Assert.InRange(glyphAt.Y, ringGlyphAt.Y - 1, ringGlyphAt.Y + 1);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Live_content_stands_in_for_the_red_dot_and_the_word_and_belongs_to_the_row(string dir)
    {
        var state = new Border { Width = 70, Height = 20, Child = new TextBlock { Text = "متصل" } };
        var row = new MediaSeekRow { Width = 600, Duration = TimeSpan.FromMinutes(30), Value = 1, IsLive = true, LiveContent = state };
        Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, Dir(dir), width: 700);
        Settle(100);
        Assert.Same(row, state.Parent);
        Assert.Contains(state, row.GetLogicalChildren());
        Assert.True(state.IsEffectivelyVisible);
        Assert.False(Part<TextBlock>(row, "PART_LiveText").IsEffectivelyVisible);
        Assert.Equal(1, Part<Control>(row, "PART_AtLive").Opacity);

        row.LiveContent = null;
        Settle(60);
        Assert.Null(state.Parent);
        Assert.True(Part<TextBlock>(row, "PART_LiveText").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void A_seek_rows_markers_reach_its_bar()
    {
        var marks = new AvaloniaList<MediaMarker> { new() { At = 0.25, Tip = "المقدمة" }, new() { At = 0.75, Tone = "ok" } };
        var row = new MediaSeekRow { Width = 600, Duration = TimeSpan.FromMinutes(4), Markers = marks };
        Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, width: 700);
        var bar = Part<MediaBar>(row, "PART_Bar");
        Assert.Same(marks, bar.Markers);
        var later = new AvaloniaList<MediaMarker> { new() { At = 0.5 } };
        row.Markers = later;
        Settle(60);
        Assert.Same(later, bar.Markers);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Subtitles_content_sits_beside_the_icon_and_the_card_opens_from_code(string dir)
    {
        var made = new TextBlock { Text = "40%" };
        var bar = new MediaControlBar { Width = 880, SubtitlesCard = new TextBlock { Text = "أضف ملف ترجمة" }, SubtitlesContent = made };
        var w = Show(bar, Dir(dir), width: 960, height: 140);
        var subtitles = Part<ToggleSplitButton>(bar, "PART_Subtitles");
        Assert.Same(bar, made.Parent);
        Assert.True(made.IsEffectivelyVisible);
        var icon = subtitles.GetVisualDescendants().OfType<Icon>().First(i => i.Classes.Contains("media-ico"));
        var madeBox = Box(made, w);
        var iconBox = Box(icon, w);
        Assert.True(madeBox.Right <= iconBox.Left + 0.5 || madeBox.Left >= iconBox.Right - 0.5, $"content {madeBox}, icon {iconBox}");
        Assert.True(Box(subtitles, w).Contains(madeBox.Center), "inside the subtitles' button");

        var card = Assert.IsType<PopCard>(subtitles.Flyout);
        bar.ShowSubtitlesCard();
        Dispatcher.UIThread.RunJobs();
        Assert.True(card.IsOpen);
        bar.HideSubtitlesCard();
        Dispatcher.UIThread.RunJobs();
        Assert.False(card.IsOpen);

        bar.SubtitlesCard = null;
        Settle(60);
        bar.ShowSubtitlesCard();
        Dispatcher.UIThread.RunJobs();
        Assert.False(card.IsOpen);

        bar.SubtitlesContent = null;
        Assert.Null(made.Parent);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_timeline_mute_tone_colours_the_dot_chip_and_icon(string theme)
    {
        var item = new TimelineItem { Header = "تم الإرسال", Time = "الآن", Icon = Glyph, Classes = { "mute" } };
        Show(new StackPanel { Margin = new Thickness(40), Children = { new Timeline { Items = { item } } } }, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        AssertBrush(item, "InsToneMute", () => Part<Ellipse>(item, "PART_Dot").Fill);
        AssertBrush(item, "InsToneMuteBg", () => Part<Border>(item, "PART_Chip").Background);
        AssertBrush(item, "InsToneMute", () => Part<Icon>(item, "PART_Icon").Foreground);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void A_panels_head_and_foot_rules_are_the_hairline(string theme)
    {
        var head = new Border { Classes = { "panel-head" }, Child = new TextBlock { Text = "الإعدادات العامة" } };
        var foot = new Border { Classes = { "panel-foot" }, Child = new TextBlock { Text = "آخر تحديث قبل ساعة" } };
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(foot, Dock.Bottom);
        var body = new Border { Classes = { "panel-body" }, Child = new TextBlock { Text = "محتوى اللوحة" } };
        var panel = new Border { Classes = { "panel" }, Child = new DockPanel { Children = { head, foot, body } } };
        Show(new StackPanel { Margin = new Thickness(20), Children = { panel } }, FlowDirection.RightToLeft, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, height: 600);
        var line = ColorOf((IBrush)Res(head, "InsLine"));
        Assert.Equal(line, ColorOf(head.BorderBrush));
        Assert.Equal(line, ColorOf(foot.BorderBrush));
        Assert.Equal(new Thickness(0, 0, 0, 1), head.BorderThickness);
        Assert.Equal(new Thickness(0, 1, 0, 0), foot.BorderThickness);
    }
}
