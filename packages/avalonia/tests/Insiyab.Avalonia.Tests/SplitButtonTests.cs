using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class SplitButtonTests
{
    private static PopMenu Menu() => new() { Items = { new MenuItem { Header = "حفظ باسم جديد" }, new MenuItem { Header = "حفظ نسخة" } } };

    private static SplitButton NewSplit(params string[] classes)
    {
        var split = new SplitButton { Content = "حفظ", Flyout = Menu() };
        split.Classes.AddRange(classes);
        return split;
    }

    private static Button Primary(SplitButton split) => Part<Button>(split, "PART_PrimaryButton");

    private static Button Arrow(SplitButton split) => Part<Button>(split, "PART_SecondaryButton");

    [AvaloniaFact]
    public void Halves_wear_the_split_buttons_classes_and_follow_their_changes()
    {
        var split = NewSplit("primary", "sm");
        Show(split);
        foreach (var half in new[] { Primary(split), Arrow(split) })
        {
            Assert.Contains("primary", half.Classes);
            Assert.Contains("sm", half.Classes);
            Assert.Equal(Colors.Transparent, ColorOf(half.Background));
            Assert.False(Part<Border>(half, "PART_Shadow").IsVisible);
        }
        Assert.Equal("primary", SplitFace.GetTone(split));
        Assert.IsAssignableFrom<IGradientBrush>(Part<Border>(split, "PART_Fill").Background);
        Assert.True(Part<Border>(split, "PART_Divider").IsVisible);

        split.Classes.Remove("primary");
        split.Classes.Add("ghost");
        Settle(100);
        Assert.DoesNotContain("primary", Primary(split).Classes);
        Assert.Contains("ghost", Arrow(split).Classes);
        Assert.Equal("ghost", SplitFace.GetTone(split));
        Assert.Null(Part<Border>(split, "PART_Fill").Background);
        Assert.False(Part<Border>(split, "PART_Divider").IsVisible);
    }

    [AvaloniaFact]
    public void Halves_join_with_square_inner_corners()
    {
        var split = NewSplit("secondary");
        Show(split);
        var radius = split.CornerRadius.TopLeft;
        Assert.True(radius > 0);
        Assert.Equal(new CornerRadius(radius, 0, 0, radius), Primary(split).CornerRadius);
        Assert.Equal(new CornerRadius(0, radius, radius, 0), Arrow(split).CornerRadius);
        Assert.Equal(Primary(split).Bounds.Height, Arrow(split).Bounds.Height);
    }

    [AvaloniaFact]
    public void Each_half_hovers_on_its_own()
    {
        var split = NewSplit("secondary");
        var window = Show(split);
        window.MouseMove(Center(Arrow(split), window));
        Settle();
        AssertBrush(split, "InsCtlBgHover", () => Arrow(split).Background);
        AssertBrush(split, "InsCtlBg", () => Primary(split).Background);
    }

    [AvaloniaFact]
    public void The_arrow_opens_the_menu_and_turns_the_chevron_while_the_main_half_clicks()
    {
        var split = NewSplit("primary");
        var clicks = 0;
        split.Click += (_, _) => clicks++;
        var window = Show(split);

        Click(Primary(split), window);
        Assert.Equal(1, clicks);
        Assert.False(split.Flyout!.IsOpen);

        Click(Arrow(split), window);
        Assert.True(split.Flyout.IsOpen);
        Assert.Equal(1, clicks);
        Assert.Contains(":flyout-open", split.Classes);
        Assert.Equal(-1, Part<Viewbox>(split, "PART_Chevron").RenderTransform!.Value.M11, 2);
        split.Flyout.Hide();
    }

    [AvaloniaFact]
    public void An_unchecked_toggle_split_is_neutral_until_checked()
    {
        var toggle = new ToggleSplitButton { Content = "التدقيق", Flyout = Menu(), Classes = { "primary" } };
        Show(toggle);
        Assert.Equal("", SplitFace.GetTone(toggle));
        Assert.DoesNotContain("primary", Primary(toggle).Classes);
        Assert.Null(Part<Border>(toggle, "PART_Fill").Background);

        toggle.IsChecked = true;
        Settle(100);
        Assert.Equal("primary", SplitFace.GetTone(toggle));
        Assert.Contains("primary", Primary(toggle).Classes);

        var plain = new ToggleSplitButton { Content = "التدقيق", Flyout = Menu(), IsChecked = true };
        Show(plain);
        Assert.Equal("on", SplitFace.GetTone(plain));
        AssertBrush(plain, "InsButtonToggleOn", () => Primary(plain).Background);
        AssertBrush(plain, "InsPrimaryDarker", () => Arrow(plain).Foreground);
    }

    [AvaloniaFact]
    public void A_disabled_split_fades_once_not_twice()
    {
        var split = NewSplit("primary");
        split.IsEnabled = false;
        Show(split);
        Assert.Equal(0.5, split.Opacity, 2);
        Assert.Equal(1, Primary(split).Opacity, 2);
        Assert.Equal(1, Arrow(split).Opacity, 2);
    }

    [AvaloniaFact]
    public void The_arrow_sits_at_the_trailing_end_in_either_direction()
    {
        var ltr = NewSplit("secondary");
        var w1 = Show(ltr);
        Assert.True(Center(Arrow(ltr), w1).X > Center(Primary(ltr), w1).X);
        var rtl = NewSplit("secondary");
        var w2 = Show(rtl, FlowDirection.RightToLeft);
        Assert.True(Center(Arrow(rtl), w2).X < Center(Primary(rtl), w2).X);
    }

    [AvaloniaFact]
    public void Drop_down_button_has_a_trailing_chevron_that_turns_while_open()
    {
        var drop = new DropDownButton { Content = "ترتيب", Flyout = Menu(), Classes = { "ghost" } };
        var window = Show(drop);
        var chevron = Part<Viewbox>(drop, "PART_Chevron");
        Assert.True(Center(chevron, window).X > Center(Part<ContentPresenter>(drop, "PART_ContentPresenter"), window).X);
        AssertBrush(drop, "InsPrimaryDarker", () => drop.Foreground);

        Click(drop, window);
        Assert.True(drop.Flyout!.IsOpen);
        Assert.Equal(-1, chevron.RenderTransform!.Value.M11, 2);
        drop.Flyout.Hide();
        Settle();
        Assert.Equal(1, chevron.RenderTransform!.Value.M11, 2);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks), MemberType = typeof(Ui))]
    public void Split_and_drop_down_buttons_render_in_every_look(string theme, string dir)
    {
        var content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(24),
            Children =
            {
                Row(NewSplit("primary"), NewSplit("secondary"), NewSplit("ghost"), NewSplit("danger")),
                Row(NewSplit("primary", "sm"), NewSplit("bare"), new ToggleSplitButton { Content = "التدقيق", Flyout = Menu(), IsChecked = true }, new ToggleSplitButton { Content = "التدقيق", Flyout = Menu() }),
                Row(new DropDownButton { Content = "ترتيب", Flyout = Menu(), Classes = { "secondary" } }, new DropDownButton { Content = "ترتيب", Flyout = Menu(), Classes = { "primary", "lg" } }),
            },
        };
        var window = Show(content, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, width: 880, height: 360);
        Capture(window, $"split-{theme}-{dir}");
    }
}
