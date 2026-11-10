using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// ToggleSwitch.danger: a destructive choice whose fill, halo, hover border and focus ring take the "bad" tone in both themes, plain or as a card.
public class DangerSwitchTests
{
    private static Border Bounds(ToggleSwitch t) => Part<Border>(t, "SwitchKnobBounds");
    private static Border Ring(ToggleSwitch t) => Part<Border>(t, "FocusRing");
    private static Border Outer(ToggleSwitch t) => Part<Border>(t, "OuterBorder");

    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void A_danger_switch_takes_the_bad_tone(string theme, bool card)
    {
        var danger = new ToggleSwitch { Content = "احذف بياناتي", IsChecked = true, HorizontalAlignment = HorizontalAlignment.Stretch };
        danger.Classes.Add("danger");
        if (card) danger.Classes.Add("card");
        var plain = new ToggleSwitch { Content = "تذكير", IsChecked = true };
        var window = Show(new StackPanel { Margin = new Thickness(20), Spacing = 12, Children = { danger, plain } }, FlowDirection.RightToLeft,
            theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var bad = ColorOf((IBrush)Res(danger, "InsToneBad"));
        Assert.Equal(bad, ColorOf(Bounds(danger).Background));
        Assert.Equal(Res(danger, "InsGlowBad"), Bounds(danger).BoxShadow);
        Assert.Equal(bad, ColorOf(Ring(danger).BorderBrush));
        Assert.Equal(0.45, Ring(danger).BorderBrush!.Opacity, 0.01);

        // a plain switch beside it keeps the brand's
        AssertBrush(plain, "InsSwitchFill", Bounds(plain).Background);
        Assert.Equal(Res(plain, "InsGlowRing"), Bounds(plain).BoxShadow);
        AssertBrush(plain, "InsSwitchRing", Ring(plain).BorderBrush);

        // under the pointer: off, its track's border is the bad tone; on, the track has no border, as the brand's switch
        danger.IsChecked = false;
        ((IPseudoClasses)danger.Classes).Set(":pointerover", true);
        Settle();
        AssertBrush(danger, "InsSwitchDangerHoverBorder", () => Outer(danger).BorderBrush);
        Assert.Equal(bad, ColorOf(Outer(danger).BorderBrush));
        Assert.Equal(0.40, Outer(danger).BorderBrush!.Opacity, 0.01);
        danger.IsChecked = true;
        Settle();
        Assert.Equal(Colors.Transparent, ColorOf(Outer(danger).BorderBrush));
        Capture(window, $"switch-danger-{theme.ToLowerInvariant()}{(card ? "-card" : "")}");
    }
}
