using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MediaControlBarTests
{
    private static Border Face(Control button) => button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Face");

    // A rounded face leaves its corner pixel to the ground while the middle of its edge shows the face.
    private static void AssertRoundedOnScreen(Window window, Control button)
    {
        var frame = window.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var a = button.TranslatePoint(new Point(1, 1), window)!.Value;
        var b = button.TranslatePoint(new Point(1, button.Bounds.Height / 2), window)!.Value;
        var corner = PixelAt(buffer, (int)a.X, (int)a.Y);
        var edge = PixelAt(buffer, (int)b.X, (int)b.Y);
        Assert.NotEqual(edge, corner);
    }

    [AvaloniaFact]
    public void A_bare_toggle_that_is_on_or_under_the_pointer_keeps_its_rounded_face()
    {
        var on = new ToggleButton { Classes = { "bare", "icon", "media-btn" }, IsChecked = true, Content = new Icon { Classes = { "media-ico" } } };
        var hover = new Button { Classes = { "bare", "icon", "media-btn" }, Content = new Icon { Classes = { "media-ico" } } };
        var window = Show(Row(on, hover));
        window.MouseMove(Center(hover, window));
        Settle();
        Assert.Equal(new CornerRadius(14), on.CornerRadius);
        Assert.Equal(on.CornerRadius, Face(on).CornerRadius);
        Assert.Equal(hover.CornerRadius, Face(hover).CornerRadius);
        AssertBrush(on, "InsButtonToggleOn", () => Face(on).Background);
        AssertBrush(hover, "InsCtlBgHover", () => Face(hover).Background);
        AssertRoundedOnScreen(window, on);
        AssertRoundedOnScreen(window, hover);
    }

    [AvaloniaFact]
    public void The_halves_of_a_split_in_a_group_are_a_step_smaller_and_round_each_corner()
    {
        var split = new ToggleSplitButton { Classes = { "bare", "media-split" }, IsChecked = true, Content = new Icon { Classes = { "media-ico" } } };
        var group = new Border { Classes = { "media-group" }, Child = split };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { group } }, FlowDirection.RightToLeft);
        var halves = split.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(2, halves.Count);
        foreach (var half in halves)
        {
            Assert.Equal(new CornerRadius(10), half.CornerRadius);
            Assert.Equal(half.CornerRadius, Face(half).CornerRadius);
            Assert.Equal(40, half.Bounds.Height, 1);
        }
        Assert.Equal(44, split.Bounds.Height, 1);
        Assert.Equal(50, group.Bounds.Height, 1);
        AssertBrush(split, "InsButtonToggleOn", () => Face(halves[0]).Background);
        AssertRoundedOnScreen(window, halves[0]);
    }

    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Theme(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static T PartOf<T>(MediaControlBar bar, string name) where T : Control => Part<T>(bar, name);

    private static MediaControlBar Bar() => new()
    {
        Width = 880,
        SubtitlesCard = new TextBlock { Text = "أضف ملف ترجمة" },
        MoreItems = { new MenuItem { Header = "تنزيل" } },
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
    };

    private static Rect ScreenBox(Control c, Window window)
    {
        var a = c.TranslatePoint(default, window)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), window)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void The_bar_takes_the_player_look_with_toggles_on_and_off_and_under_the_pointer(string theme, string dir)
    {
        var bar = Bar();
        var window = Show(new Border { Classes = { "panel" }, Padding = new Thickness(16, 4, 16, 12), Child = bar }, Dir(dir), Theme(theme), 960, 140);
        Capture(window, $"media-control-bar-{theme}-{dir}-off");
        bar.IsSubtitlesOn = true;
        bar.IsFillingFrame = true;
        Settle();
        var volume = PartOf<SplitButton>(bar, "PART_Volume");
        var subtitles = PartOf<ToggleSplitButton>(bar, "PART_Subtitles");
        var fit = PartOf<ToggleButton>(bar, "PART_Fit");
        var full = PartOf<ToggleButton>(bar, "PART_FullScreen");
        var play = PartOf<Button>(bar, "PART_Play");
        var speed = PartOf<Button>(bar, "PART_Speed");
        foreach (var group in new[] { PartOf<Border>(bar, "PART_VolumeGroup"), PartOf<Border>(bar, "PART_SubtitlesGroup") })
        {
            Assert.Equal(50, group.Bounds.Height, 1);
            Assert.Equal(new CornerRadius(14), group.CornerRadius);
            Assert.Equal(new Thickness(1), group.BorderThickness);
            Assert.Equal(new Thickness(2), group.Padding);
            AssertBrush(group, "InsLine", group.BorderBrush);
        }
        foreach (var half in volume.GetVisualDescendants().OfType<Button>().Concat(subtitles.GetVisualDescendants().OfType<Button>()))
        {
            Assert.Equal(new CornerRadius(10), half.CornerRadius);
            Assert.Equal(40, half.Bounds.Height, 1);
        }
        Assert.Equal(40, volume.GetVisualDescendants().OfType<Button>().First().Bounds.Width, 1);
        foreach (var b in new Button[] { fit, full, PartOf<Button>(bar, "PART_Back"), PartOf<Button>(bar, "PART_More") })
        {
            Assert.Equal(new Size(44, 44), b.Bounds.Size);
            Assert.Equal(new CornerRadius(14), Face(b).CornerRadius);
        }
        AssertBrush(fit, "InsButtonToggleOn", () => Face(fit).Background);
        AssertBrush(fit, "InsPrimaryDarker", () => fit.Foreground);
        AssertBrush(subtitles, "InsButtonToggleOn", () => Face(subtitles.GetVisualDescendants().OfType<Button>().First()).Background);
        AssertRoundedOnScreen(window, fit);
        Assert.Equal(new Size(52, 52), play.Bounds.Size);
        Assert.Equal(new CornerRadius(26), play.CornerRadius);
        Assert.Equal(44, speed.Bounds.Height, 1);
        Assert.Equal("1×", speed.Content);
        AssertBrush(speed, "InsText2", () => speed.Foreground);
        Assert.Equal(22, bar.GetVisualDescendants().OfType<Icon>().First().Size);
        var skip = PartOf<TextBlock>(bar, "PART_BackSeconds");
        Assert.Equal(("10", 8.0, FontWeight.Bold), (skip.Text, skip.FontSize, skip.FontWeight));
        Capture(window, $"media-control-bar-{theme}-{dir}-on");
        window.MouseMove(Center(full, window));
        Settle();
        AssertBrush(full, "InsCtlBgHover", () => Face(full).Background);
        AssertRoundedOnScreen(window, full);
        Capture(window, $"media-control-bar-{theme}-{dir}-hover");
        window.MouseMove(Center(volume.GetVisualDescendants().OfType<Button>().First(), window));
        Settle();
        AssertRoundedOnScreen(window, volume.GetVisualDescendants().OfType<Button>().First());
        Capture(window, $"media-control-bar-{theme}-{dir}-hover-split");
    }

    [AvaloniaFact]
    public void The_sound_track_button_is_not_in_the_bar()
    {
        var bar = Bar();
        Show(bar, FlowDirection.RightToLeft, width: 960, height: 140);
        Assert.Empty(bar.GetVisualDescendants().OfType<DropDownButton>());
        var start = Assert.IsAssignableFrom<Panel>(PartOf<Border>(bar, "PART_VolumeGroup").Parent);
        Assert.Equal(["PART_VolumeGroup", "PART_StartItems"], start.Children.Select(c => c.Name));
        Assert.Single(PartOf<Border>(bar, "PART_VolumeGroup").GetLogicalDescendants().OfType<SplitButton>());
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void The_row_reads_as_the_page_its_transport_left_to_right_and_its_cards_as_the_page(FlowDirection dir)
    {
        var bar = Bar();
        var window = Show(bar, dir, width: 960, height: 400);
        double X(Control c) => ScreenBox(c, window).X;
        var volume = PartOf<SplitButton>(bar, "PART_Volume");
        var back = PartOf<Button>(bar, "PART_Back");
        var play = PartOf<Button>(bar, "PART_Play");
        var forward = PartOf<Button>(bar, "PART_Forward");
        var more = PartOf<Button>(bar, "PART_More");
        Assert.True(X(back) < X(play) && X(play) < X(forward), "the transport runs left to right in every language");
        if (dir == FlowDirection.LeftToRight)
            Assert.True(X(volume) < X(back) && X(forward) < X(more) && X(PartOf<ToggleSplitButton>(bar, "PART_Subtitles")) < X(PartOf<ToggleButton>(bar, "PART_Fit")));
        else // in Arabic the sides turn: the volume on the right, «⋯» on the left
            Assert.True(X(more) < X(back) && X(forward) < X(volume) && X(PartOf<ToggleButton>(bar, "PART_Fit")) < X(PartOf<ToggleSplitButton>(bar, "PART_Subtitles")));
        Assert.Equal(ScreenBox(bar, window).Center.X, ScreenBox(play, window).Center.X, 1);
        volume.Flyout!.ShowAt(volume);
        Settle(120);
        var card = Assert.IsAssignableFrom<Control>(((PopCard)volume.Flyout).Content);
        Assert.Equal(dir, card.FlowDirection);
        var label = card.GetLogicalDescendants().OfType<TextBlock>().First();
        Assert.Equal(dir == FlowDirection.RightToLeft ? "مستوى الصوت" : "Volume", label.Text);
        volume.Flyout.Hide();
        Settle(60);
        Assert.Equal(dir == FlowDirection.RightToLeft ? "تشغيل" : "Play", ToolTip.GetTip(play));
        Assert.Equal(dir == FlowDirection.RightToLeft ? "رجوع 10 ثوانٍ" : "Back 10 seconds", ToolTip.GetTip(back));
    }

    [AvaloniaFact]
    public void Items_join_each_group_and_any_default_can_be_taken_out()
    {
        var mine = new Button { Classes = { "bare", "icon", "media-btn" }, Content = new Icon { Classes = { "media-ico" } } };
        var chapters = new ToggleButton { Classes = { "bare", "icon", "media-btn" }, Content = new Icon { Classes = { "media-ico" } } };
        var live = new Button { Classes = { "bare", "media-speed" }, Content = "مباشر" };
        var bar = Bar();
        bar.StartItems.Add(mine);
        bar.CenterItems.Add(live);
        bar.EndItems.Add(chapters);
        bar.ShowFit = false;
        var window = Show(bar, FlowDirection.RightToLeft, width: 960, height: 140);
        double X(Control c) => ScreenBox(c, window).X;
        Assert.Same(bar, mine.Parent);
        Assert.True(X(mine) < X(PartOf<Border>(bar, "PART_VolumeGroup")));   // an Arabic page: the start group runs right to left
        Assert.True(X(PartOf<Button>(bar, "PART_Forward")) < X(live));
        Assert.Equal(ScreenBox(bar, window).Center.X, ScreenBox(PartOf<Button>(bar, "PART_Play"), window).Center.X, 1);
        Assert.True(X(PartOf<Button>(bar, "PART_More")) < X(chapters) && X(chapters) < X(PartOf<ToggleButton>(bar, "PART_FullScreen")));
        Assert.False(PartOf<ToggleButton>(bar, "PART_Fit").IsVisible);
        Assert.Equal(new Size(44, 44), mine.Bounds.Size);

        bar.ShowSpeed = false;
        bar.ShowVolume = false;
        bar.ShowSubtitles = false;
        bar.EndItems.Remove(chapters);
        Settle(60);
        Assert.False(PartOf<Border>(bar, "PART_VolumeGroup").IsVisible);
        Assert.False(PartOf<Border>(bar, "PART_SubtitlesGroup").IsVisible);
        Assert.False(PartOf<ToggleButton>(bar, "PART_SubtitlesToggle").IsVisible);
        Assert.Null(chapters.Parent);
        Assert.Equal(ScreenBox(bar, window).Center.X, ScreenBox(PartOf<Button>(bar, "PART_Play"), window).Center.X, 1);

        Assert.True(PartOf<Button>(bar, "PART_More").IsVisible);
        bar.MoreItems.Clear();
        Assert.False(PartOf<Button>(bar, "PART_More").IsVisible);
        bar.MoreItems.Add(new MenuItem { Header = "تنزيل" });
        bar.ShowMore = false;
        Assert.False(PartOf<Button>(bar, "PART_More").IsVisible);

        bar.ShowSubtitles = true;
        bar.SubtitlesCard = null;
        Assert.True(PartOf<ToggleButton>(bar, "PART_SubtitlesToggle").IsVisible);
        Assert.False(PartOf<Border>(bar, "PART_SubtitlesGroup").IsVisible);
    }

    [AvaloniaFact]
    public void The_defaults_turn_their_state_and_ask_to_skip()
    {
        var bar = Bar();
        var window = Show(bar, FlowDirection.RightToLeft, width: 960, height: 400);
        var skips = new List<TimeSpan>();
        bar.SkipRequested += (_, e) => skips.Add(e.Offset);
        var play = PartOf<Button>(bar, "PART_Play");
        Click(play, window);
        Assert.True(bar.IsPlaying);
        Assert.Equal("إيقاف مؤقت", ToolTip.GetTip(play));
        Click(PartOf<Button>(bar, "PART_Back"), window);
        Click(PartOf<Button>(bar, "PART_Forward"), window);
        Assert.Equal([TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(10)], skips);
        var volume = PartOf<SplitButton>(bar, "PART_Volume");
        Click(volume.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PrimaryButton"), window);
        Assert.True(bar.IsMuted);
        Assert.Equal("إعادة الصوت", ToolTip.GetTip(volume));
        Click(PartOf<ToggleSplitButton>(bar, "PART_Subtitles").GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PrimaryButton"), window);
        Assert.True(bar.IsSubtitlesOn);
        Click(PartOf<ToggleButton>(bar, "PART_Fit"), window);
        Assert.True(bar.IsFillingFrame);
        var full = PartOf<ToggleButton>(bar, "PART_FullScreen");
        Click(full, window);
        Assert.True(bar.IsFullScreen);
        Assert.Equal("الخروج من ملء الشاشة", ToolTip.GetTip(full));

        bar.IsFillingFrame = false;
        Assert.False(PartOf<ToggleButton>(bar, "PART_Fit").IsChecked);

        var speed = PartOf<Button>(bar, "PART_Speed");
        speed.Flyout!.ShowAt(speed);
        Settle(120);
        var items = ((PopMenu)speed.Flyout).Items.Count > 0 ? ((PopMenu)speed.Flyout).Items.OfType<MenuItem>().ToList() : ((IEnumerable<MenuItem>)((PopMenu)speed.Flyout).ItemsSource!).ToList();
        Assert.Equal(["0.75×", "1×", "1.25×", "1.5×", "1.75×", "2×"], items.Select(i => (string)i.Header!));
        items[3].RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        speed.Flyout.Hide();
        Settle(60);
        Assert.Equal(1.5, bar.Speed);
        Assert.Equal("1.5×", speed.Content);

        var slider = ((PopCard)volume.Flyout!).Content is Control card ? card.GetLogicalDescendants().OfType<Slider>().Single() : null;
        Assert.NotNull(slider);
        slider!.Value = 50;
        Assert.Equal(0.5, bar.Volume, 3);
        Assert.False(bar.IsMuted);
        bar.Volume = 0.2;
        Assert.Equal(20, slider.Value, 3);

        var bound = new MediaControlBar();
        var source = new TestState();
        bound.DataContext = source;
        bound.Bind(MediaControlBar.IsPlayingProperty, new global::Avalonia.Data.Binding(nameof(TestState.Playing)));
        var w2 = Show(bound, width: 960, height: 140);
        Click(PartOf<Button>(bound, "PART_Play"), w2);
        Assert.True(source.Playing);
    }

    [AvaloniaFact]
    public void Subtitles_being_made_ring_their_icon_and_the_button_keeps_its_size()
    {
        var bar = Bar();
        Assert.True(double.IsNaN(bar.SubtitlesProgress));
        Assert.False(bar.IsSubtitlesWaiting);
        var window = Show(bar, FlowDirection.RightToLeft, width: 960, height: 200);
        var subtitles = PartOf<ToggleSplitButton>(bar, "PART_Subtitles");
        var icon = subtitles.GetVisualDescendants().OfType<ProgressIcon>().Single();
        var toggleIcon = Assert.IsType<ProgressIcon>(PartOf<ToggleButton>(bar, "PART_SubtitlesToggle").Content); // hidden beside the split, so read through its content
        var size = subtitles.Bounds.Size;
        var at = ScreenBox(PartOf<ToggleButton>(bar, "PART_Fit"), window);
        Assert.DoesNotContain(":busy", icon.Classes);

        bar.IsSubtitlesWaiting = true;
        Settle(60);
        Assert.Contains(":waiting", icon.Classes);
        Assert.Contains(":waiting", toggleIcon.Classes);
        Assert.Equal("الترجمة", ToolTip.GetTip(subtitles));

        bar.IsSubtitlesWaiting = false;
        bar.SubtitlesProgress = 24;
        Settle(60);
        Assert.Contains(":progress", icon.Classes);
        Assert.Equal(24, Part<ProgressRing>(icon, "PART_Ring").Value);
        Assert.Equal("الترجمة · 24%", ToolTip.GetTip(subtitles));
        Assert.Equal(size, subtitles.Bounds.Size);
        Assert.Equal(at, ScreenBox(PartOf<ToggleButton>(bar, "PART_Fit"), window));

        bar.SubtitlesProgress = double.NaN;
        Settle(60);
        Assert.DoesNotContain(":busy", icon.Classes);
        Assert.Equal("الترجمة", ToolTip.GetTip(subtitles));
        Assert.Equal(size, subtitles.Bounds.Size);
    }

    private sealed class TestState
    {
        public bool Playing { get; set; }
    }
}
