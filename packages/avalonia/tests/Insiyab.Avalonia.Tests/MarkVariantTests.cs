using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using Insiyab.Avalonia.Gallery.Playground;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The pill's sizes, outline, bare and peeking dots, the eight category tints on pills, badges and avatars, the bigger avatars with their ink-centred letters, and the big shell mark.
public class MarkVariantTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    // A control's box in window pixels, whichever way the window flows.
    private static Rect Box(Visual c, Visual to)
    {
        var a = c.TranslatePoint(default, to)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), to)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    public static IEnumerable<object[]> Cats() =>
        from n in Enumerable.Range(1, 8)
        from t in new[] { "Light", "Dark" }
        select new object[] { n, t };

    [AvaloniaFact]
    public void A_small_pill_is_no_taller_than_a_text_line()
    {
        var sm = new Pill { Classes = { "sm", "ok", "dot" }, Content = "مكتمل" };
        var plain = new Pill { Classes = { "ok", "dot" }, Content = "مكتمل" };
        var button = new Button { Classes = { "pill", "sm" }, Content = "تصفية" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { sm, plain, button } }, FlowDirection.RightToLeft);
        Assert.InRange(sm.Bounds.Height, 17.5, 18.5);
        Assert.Equal(10, sm.FontSize);
        Assert.Equal(6, Part<Ellipse>(sm, "PART_Dot").Bounds.Width);
        Assert.InRange(plain.Bounds.Height, 25.5, 26.5);
        Assert.InRange(button.Bounds.Height, 17.5, 18.5);
    }

    [AvaloniaFact]
    public void An_outline_pill_keeps_its_box_and_drops_its_fill()
    {
        var ok = new Pill { Classes = { "ok" }, Content = "مكتمل" };
        var line = new Pill { Classes = { "ok", "outline" }, Content = "مكتمل" };
        var button = new Button { Classes = { "pill", "ok", "outline" }, Content = "مكتمل" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { ok, line, button } }, FlowDirection.RightToLeft);
        Assert.Equal(ok.Bounds.Size, line.Bounds.Size);
        Assert.Equal(0, ColorOf(Part<Border>(line, "PART_Face").Background).A);
        AssertBrush(line, "InsToneOkBd", () => Part<Border>(line, "PART_Face").BorderBrush);
        AssertBrush(line, "InsToneOk", () => line.Foreground);
        Assert.Equal(0, ColorOf(Part<Border>(button, "PART_Face").Background).A);
    }

    [AvaloniaFact]
    public void A_mute_pill_is_the_neutral_one()
    {
        var p = new Pill { Classes = { "mute" }, Content = "مسودة" };
        Show(p, FlowDirection.RightToLeft);
        AssertBrush(p, "InsToneMuteBg", () => Part<Border>(p, "PART_Face").Background);
        AssertBrush(p, "InsToneMute", () => p.Foreground);
    }

    [AvaloniaTheory]
    [InlineData("ok", "InsToneOk")]
    [InlineData("warn", "InsToneWarnSignal")]
    [InlineData("bad", "InsToneBad")]
    [InlineData("brand", "InsToneBrandSoft")]
    [InlineData("", "InsToneMute")]
    public void An_empty_dot_pill_is_a_bare_dot_in_its_tone(string tone, string key)
    {
        var p = new Pill { Classes = { "dot" } };
        if (tone != "") p.Classes.Add(tone);
        Show(new StackPanel { Margin = new Thickness(20), Children = { p } }, FlowDirection.RightToLeft);
        Assert.Contains(":empty", p.Classes);
        Assert.InRange(p.Bounds.Width, 7.5, 8.5);
        Assert.InRange(p.Bounds.Height, 7.5, 8.5);
        AssertBrush(p, key, () => Part<Ellipse>(p, "PART_Dot").Fill);
        Assert.Equal(0, ColorOf(Part<Border>(p, "PART_Face").Background).A);
        p.Content = "نص";
        Settle();
        Assert.DoesNotContain(":empty", p.Classes);
        Assert.InRange(p.Bounds.Height, 25.5, 26.5);
    }

    [AvaloniaFact]
    public void A_warn_dot_reads_orange_beside_its_text()
    {
        var p = new Pill { Classes = { "warn", "dot" }, Content = "معلق" };
        Show(p, FlowDirection.RightToLeft);
        AssertBrush(p, "InsToneWarnSignal", () => Part<Ellipse>(p, "PART_Dot").Fill);
        AssertBrush(p, "InsToneWarn", () => p.Foreground);
    }

    private static (Window Window, Pill Pill, TextBlock After) PeekRig(string dir, params string[] classes)
    {
        var before = new TextBlock { Text = "الحالة" };
        var pill = new Pill { Content = "معلق" };
        foreach (var c in classes) pill.Classes.Add(c);
        var after = new TextBlock { Text = "بعدها" };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(40), Children = { before, pill, after } };
        return (Show(row, Dir(dir), width: 500), pill, after);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_peek_dot_opens_over_its_neighbours(string dir)
    {
        var (w, pill, after) = PeekRig(dir, "warn", "dot", "peek");
        var face = Part<Border>(pill, "PART_Face");
        var ground = Part<Border>(pill, "PART_Ground");
        var ring = Part<Border>(pill, "PART_Ring");
        var presenter = Part<ContentPresenter>(pill, "PART_ContentPresenter");
        Assert.InRange(pill.Bounds.Width, 15.5, 16.5);
        Assert.InRange(pill.Bounds.Height, 15.5, 16.5);
        Assert.True(ring.IsVisible);
        Assert.Equal(1, ring.Opacity);
        Assert.Equal(0, ground.Opacity);
        Assert.Equal(0, presenter.Opacity);
        var afterX = Box(after, w).X;

        w.MouseMove(Center(pill, w));
        Settle();
        Assert.Contains(":pointerover", pill.Classes);
        Assert.Equal(1, presenter.Opacity);
        Assert.Equal(1, ground.Opacity);
        Assert.Equal(0, ring.Opacity);
        AssertBrush(pill, "InsToneWarnBg", () => face.Background);
        Assert.Equal(1, pill.ZIndex);
        Assert.InRange(Box(after, w).X, afterX - 0.5, afterX + 0.5);
        var fb = Box(face, w);
        var pb = Box(pill, w);
        Assert.True(fb.Width > 40, $"face {fb}");
        Assert.True(fb.Left < pb.Left && fb.Right > pb.Right, $"the face is laid over its neighbours: face {fb}, pill {pb}");
        var dotAt = Box(Part<Ellipse>(pill, "PART_Dot"), w).Center;
        var ringDotAt = Box(Part<Ellipse>(pill, "PART_RingDot"), w).Center;
        Assert.InRange(dotAt.X, ringDotAt.X - 1, ringDotAt.X + 1);
        Assert.InRange(dotAt.Y, ringDotAt.Y - 1, ringDotAt.Y + 1);

        w.MouseMove(new Point(1, 1));
        Settle();
        Assert.Equal(0, presenter.Opacity);
        Assert.Equal(0, ground.Opacity);
        Assert.Equal(1, ring.Opacity);

        pill.Focus(NavigationMethod.Tab);
        Settle();
        Assert.Contains(":focus-visible", pill.Classes);
        Assert.Equal(1, presenter.Opacity);
        Assert.Equal((BoxShadows)Res(pill, "InsFocusRing"), face.BoxShadow);

        Assert.Equal("معلق", ControlAutomationPeer.CreatePeerForElement(pill).GetName());
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_back_peek_opens_toward_the_start(string dir)
    {
        var (w, pill, _) = PeekRig(dir, "warn", "dot", "peek", "back");
        var face = Part<Border>(pill, "PART_Face");
        Assert.InRange(pill.Bounds.Width, 15.5, 16.5);
        w.MouseMove(Center(pill, w));
        Settle();
        Assert.Contains(":pointerover", pill.Classes);
        var fb = Box(face, w);
        var pb = Box(pill, w);
        Assert.True(fb.Width > 40, $"face {fb}");
        Assert.True(fb.Left < pb.Left && fb.Right > pb.Right, $"the face is laid over its neighbours: face {fb}, pill {pb}");
        if (dir == "ltr") Assert.True(pb.Left - fb.Left > fb.Right - pb.Right, "it opens toward the start (left), its dot at its end");
        else Assert.True(fb.Right - pb.Right > pb.Left - fb.Left, "it opens toward the start (right), its dot at its end");
        var dotAt = Box(Part<Ellipse>(pill, "PART_DotEnd"), w).Center;
        var ringDotAt = Box(Part<Ellipse>(pill, "PART_RingDot"), w).Center;
        Assert.InRange(dotAt.X, ringDotAt.X - 1, ringDotAt.X + 1);
        Assert.InRange(dotAt.Y, ringDotAt.Y - 1, ringDotAt.Y + 1);
        Assert.True(Part<Ellipse>(pill, "PART_DotEnd").IsVisible);
        Assert.False(Part<Ellipse>(pill, "PART_Dot").IsVisible);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Cats))]
    public void Category_classes_tint_pills_badges_and_avatars(int n, string theme)
    {
        var pill = new Pill { Classes = { $"cat{n}", "dot" }, Content = "فئة" };
        var badge = new Badge { Classes = { $"cat{n}" }, Content = "4" };
        var avatar = new Avatar { Classes = { $"cat{n}" }, Content = "ف" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { pill, badge, avatar } }, FlowDirection.RightToLeft, Variant(theme));
        AssertBrush(pill, $"InsCat{n}Bg", () => Part<Border>(pill, "PART_Face").Background);
        AssertBrush(badge, $"InsCat{n}Bg", () => badge.Background);
        AssertBrush(avatar, $"InsCat{n}Bg", () => avatar.Background);
        AssertBrush(pill, $"InsCat{n}Ink", () => pill.Foreground);
        AssertBrush(badge, $"InsCat{n}Ink", () => badge.Foreground);
        AssertBrush(avatar, $"InsCat{n}Ink", () => avatar.Foreground);
        AssertBrush(pill, $"InsCat{n}", () => Part<Ellipse>(pill, "PART_Dot").Fill);
    }

    [AvaloniaFact]
    public void The_category_sample_shows_all_eight_in_both_themes()
    {
        var view = PlayCode.Parse(BadgesPlay.PaletteExample);
        var w = Show(view, FlowDirection.RightToLeft, ThemeVariant.Light, 900, 520);
        var all = w.GetVisualDescendants().ToList();
        Assert.Equal(2, all.OfType<ThemeVariantScope>().Count());
        Assert.Equal(16, all.OfType<Pill>().Count(p => p.Content is not null));
        Assert.Equal(16, all.OfType<Badge>().Count());
        Assert.Equal(16, all.OfType<Avatar>().Count());
        Assert.Equal(16, all.OfType<Pill>().Count(p => p.Content is null));
        Capture(w, "marks-categories");
    }

    [AvaloniaFact]
    public void Avatar_xl_and_xxl_sizes()
    {
        var xl = new Avatar { Classes = { "xl" }, Content = "ل" };
        var xxl = new Avatar { Classes = { "xxl" }, Content = "ل" };
        Show(new StackPanel { Margin = new Thickness(20), Spacing = 8, Children = { xl, xxl } }, FlowDirection.RightToLeft);
        Assert.Equal(new Size(64, 64), xl.Bounds.Size);
        Assert.Equal(24, xl.FontSize);
        Assert.Equal(new Size(100, 100), xxl.Bounds.Size);
        Assert.Equal(36, xxl.FontSize);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Avatar_initials_sit_on_the_disc_centre_by_their_ink(string dir)
    {
        var lam = new Avatar { Classes = { "xxl" }, Content = "ل" };
        var ain = new Avatar { Classes = { "xxl" }, Content = "ع" };
        var w = Show(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20, Margin = new Thickness(20), Children = { lam, ain } }, Dir(dir), ThemeVariant.Light, 400, 220);
        var frame = w.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var report = new List<string>();
        var offsets = new List<(Rect Ink, double Dx, double Dy)>();
        foreach (var (name, avatar) in new[] { ("lam", lam), ("ain", ain) })
        {
            var box = Box(avatar, w);
            var c = box.Center;
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (var y = (int)Math.Floor(box.Top); y < (int)Math.Ceiling(box.Bottom); y++)
                for (var x = (int)Math.Floor(box.Left); x < (int)Math.Ceiling(box.Right); x++)
                {
                    if (Math.Sqrt((x + 0.5 - c.X) * (x + 0.5 - c.X) + (y + 0.5 - c.Y) * (y + 0.5 - c.Y)) >= 40) continue;
                    var p = PixelAt(buffer, x, y);
                    if (p.R <= 200 || p.G <= 200 || p.B <= 200) continue;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            var ink = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            var dx = ink.Center.X - c.X;
            var dy = ink.Center.Y - c.Y;
            report.Add($"{name} {dir}: ink {ink} dx {dx:0.0} dy {dy:0.0}");
            offsets.Add((ink, dx, dy));
        }
        var all = string.Join("; ", report);
        TestContext.Current.TestOutputHelper?.WriteLine(all);
        foreach (var (ink, dx, dy) in offsets)
        {
            Assert.True(ink.Width > 4, $"no ink found: {all}");
            Assert.True(Math.Abs(dx) <= 1.5 && Math.Abs(dy) <= 1.5, all);
        }
    }

    [AvaloniaFact]
    public void A_more_avatar_draws_its_count_left_to_right_in_latin()
    {
        var more = new Avatar { Classes = { "more" }, Content = "+4" };
        Show(more, FlowDirection.RightToLeft);
        var ink = more.GetVisualDescendants().OfType<AvatarInk>().First();
        Assert.Equal(FlowDirection.LeftToRight, ink.FlowDirection);
        Assert.Equal(Res(more, "InsFontBodyEn"), ink.FontFamily);
    }

    [AvaloniaFact]
    public void The_big_shell_mark_scales_its_icon_and_corners()
    {
        var icon = new Icon { Data = Geometry.Parse("M4,4 L20,4 L20,20 L4,20 Z"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var mark = new Border { Classes = { "shell-mark", "xl" }, Child = icon };
        Show(mark, FlowDirection.RightToLeft);
        Assert.Equal(new Size(100, 100), mark.Bounds.Size);
        Assert.Equal(new CornerRadius(22), mark.CornerRadius);
        Assert.InRange(icon.Bounds.Width, 39.5, 40.5);
        Assert.Empty(LookCheck.SubjectRules(typeof(Border), "xl", "ins-still", "IsInactive"));
    }
}
