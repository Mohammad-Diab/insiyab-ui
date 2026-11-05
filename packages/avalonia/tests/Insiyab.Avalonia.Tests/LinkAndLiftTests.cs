using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class LinkAndLiftTests
{
    private static FlowDirection Dir(string dir) => dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    private static ThemeVariant Theme(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void A_link_button_is_brand_soft_words_underlined_under_the_pointer(string theme, string dir)
    {
        var b = new Button { Classes = { "link" }, Content = "عرض المزيد" };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { b } }, Dir(dir), Theme(theme));
        AssertBrush(b, "InsToneBrandSoft", () => b.Foreground);
        Assert.Equal(default, b.Padding);
        Assert.Equal(0, b.MinHeight);
        Assert.False(Part<Border>(b, "PART_Face").ClipToBounds);
        Assert.False(Part<Border>(b, "PART_Sweep").IsVisible);
        Assert.Equal(0, PressDip.GetDepth(Part<Panel>(b, "PART_Press")));

        w.MouseMove(Center(b, w));
        Settle();
        AssertBrush(b, "InsLinkHover", () => b.Foreground);
        var text = b.GetVisualDescendants().OfType<TextBlock>().First();
        Assert.NotNull(text.TextDecorations);
        Assert.Single(text.TextDecorations!);
        Assert.Equal(0.14, text.TextDecorations![0].StrokeOffset, 3);
        Capture(w, $"link-button-{theme}-{dir}");
    }

    [AvaloniaFact]
    public void A_drop_down_link_takes_the_link_look()
    {
        var b = new DropDownButton { Classes = { "link" }, Content = "المزيد" };
        Show(new StackPanel { Margin = new Thickness(40), Children = { b } }, FlowDirection.RightToLeft);
        AssertBrush(b, "InsToneBrandSoft", () => b.Foreground);
        Assert.False(Part<Border>(b, "PART_Face").ClipToBounds);
    }

    [AvaloniaFact]
    public void A_bare_or_link_face_never_clips_its_ink()
    {
        var bare = new Button { Classes = { "bare" }, Content = "الأغبر" };
        var link = new Button { Classes = { "link" }, Content = "الأغبر" };
        var primary = new Button { Classes = { "primary" }, Content = "حفظ" };
        Show(new StackPanel { Margin = new Thickness(40), Spacing = 12, Children = { bare, link, primary } }, FlowDirection.RightToLeft);
        Assert.False(Part<Border>(bare, "PART_Face").ClipToBounds);
        Assert.False(Part<Border>(link, "PART_Face").ClipToBounds);
        Assert.True(Part<Border>(primary, "PART_Face").ClipToBounds);
    }

    [AvaloniaFact]
    public void Only_the_leading_icon_nudges_and_a_card_button_nudges_none()
    {
        Icon lead, trail;
        var plain = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    (lead = new Icon { Data = Geometry.Parse("M5 12h14") }),
                    new TextBlock { Text = "فتح" },
                    (trail = new Icon { Data = Geometry.Parse("M5 12h14"), Classes = { "trail" } }),
                },
            },
        };
        var cardIcon = new Icon { Data = Geometry.Parse("M5 12h14") };
        var card = new Button { Content = cardIcon, Padding = new Thickness(20) };
        var statIcon = new Icon { Data = Geometry.Parse("M5 12h14") };
        var stat = new Button { Classes = { "stat-link" }, Content = statIcon };
        var w = Show(new StackPanel { Margin = new Thickness(40), Spacing = 12, Children = { plain, card, stat } }, FlowDirection.RightToLeft);
        card.Theme = (ControlTheme)Res(card, "InsCardButton");
        Settle();

        w.MouseMove(Center(plain, w));
        Settle();
        Assert.Equal(new Thickness(2, 0, -2, 0), lead.Margin);
        Assert.Equal(default, trail.Margin);

        w.MouseMove(Center(card, w));
        Settle();
        Assert.Equal(default, cardIcon.Margin);

        w.MouseMove(Center(stat, w));
        Settle();
        Assert.Equal(default, statIcon.Margin);
    }

    [AvaloniaFact]
    public void A_lifted_button_keeps_its_hit_area_under_a_resting_pointer()
    {
        var button = new Button { Classes = { "primary", "lift" }, Content = "ابدأ" };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { button } }, FlowDirection.RightToLeft);
        var p = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height - 1), w)!.Value;
        w.MouseMove(p);
        Settle();
        w.MouseMove(p);
        Settle(300);
        Assert.True(button.IsPointerOver);
        Assert.Equal(-2, Part<Panel>(button, "PART_Root").RenderTransform!.Value.M32, 0.1);
    }

    [AvaloniaFact]
    public void A_card_button_keeps_its_hit_area_under_a_resting_pointer()
    {
        var card = new Button { Content = "بطاقة", Padding = new Thickness(20) };
        var w = Show(new StackPanel { Margin = new Thickness(40), Children = { card } }, FlowDirection.RightToLeft);
        card.Theme = (ControlTheme)Res(card, "InsCardButton");
        Settle();
        var p = card.TranslatePoint(new Point(card.Bounds.Width / 2, card.Bounds.Height - 1), w)!.Value;
        w.MouseMove(p);
        Settle();
        w.MouseMove(p);
        Settle(300);
        Assert.True(card.IsPointerOver);
        Assert.Equal(-2, Part<Border>(card, "PART_Face").RenderTransform!.Value.M32, 0.1);
    }

    [AvaloniaFact]
    public void A_hoverable_surface_stays_up_while_the_pointer_rests_on_its_bottom_edge()
    {
        var card = new Border { Classes = { "card", "hoverable" }, Width = 200, Height = 100 };
        var parent = new StackPanel { Margin = new Thickness(40), Children = { card } };
        var w = Show(parent, FlowDirection.RightToLeft);
        var p = parent.TranslatePoint(new Point(card.Bounds.Center.X, card.Bounds.Bottom - 1), w)!.Value;
        w.MouseMove(p);
        Settle();
        w.MouseMove(p);
        Settle(300);
        Assert.True(HoverLift.GetIsLifted(card));
        Assert.Equal(-2, card.RenderTransform!.Value.M32, 0.1);
        Capture(w, "hoverable-lifted");
        var away = parent.TranslatePoint(new Point(card.Bounds.Center.X, card.Bounds.Bottom + 8), w)!.Value;
        w.MouseMove(away);
        Settle();
        Assert.False(HoverLift.GetIsLifted(card));
        Assert.Equal(0, card.RenderTransform?.Value.M32 ?? 0, 0.1);
    }

    [AvaloniaFact]
    public void An_icon_with_dir_follows_the_page_direction()
    {
        var plain = new Icon();
        var arrow = new Icon { Classes = { "dir" } };
        var w = Show(new StackPanel { Children = { plain, arrow } }, FlowDirection.RightToLeft);
        Assert.Equal(FlowDirection.LeftToRight, plain.FlowDirection);
        Assert.Equal(FlowDirection.RightToLeft, arrow.FlowDirection);
        w.FlowDirection = FlowDirection.LeftToRight;
        Settle();
        Assert.Equal(FlowDirection.LeftToRight, arrow.FlowDirection);
    }
}
