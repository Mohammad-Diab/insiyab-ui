using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MotionTests
{
    private static bool IsIdentity(ITransform? t) => t is null || t.Value.IsIdentity;

    [AvaloniaFact]
    public void Still_marks_the_window_and_everything_in_it()
    {
        var inner = new Button { Content = "زر" };
        var window = Show(new StackPanel { Children = { inner } });
        Assert.False(Motion.IsStill(inner));
        Assert.DoesNotContain(Motion.StillPseudoClass, window.Classes);

        Motion.SetStill(window, true);
        Assert.True(Motion.IsStill(inner));
        Assert.Contains(Motion.StillPseudoClass, window.Classes);

        Motion.SetStill(window, false);
        Assert.False(Motion.IsStill(inner));
        Assert.DoesNotContain(Motion.StillPseudoClass, window.Classes);
    }

    [AvaloniaFact]
    public void The_system_setting_makes_every_window_still()
    {
        try
        {
            Motion.SystemOverride = true;
            var inner = new Button { Content = "زر" };
            var window = Show(new StackPanel { Children = { inner } });
            Assert.True(Motion.IsStill(inner));
            Assert.Contains(Motion.StillPseudoClass, window.Classes);
        }
        finally { Motion.SystemOverride = null; }
    }

    [AvaloniaFact]
    public void A_still_island_keeps_its_hover_colour_and_loses_its_lift()
    {
        var island = new Button { Classes = { "island" }, Content = "جزيرة", HorizontalAlignment = HorizontalAlignment.Left };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { island } });
        window.MouseMove(Center(island, window));
        Settle();
        Assert.False(IsIdentity(island.RenderTransform));

        window.MouseMove(new Point(600, 400));
        Settle();
        Motion.SetStill(window, true);
        window.MouseMove(Center(island, window));
        Settle();
        Assert.True(IsIdentity(island.RenderTransform));
        AssertBrush(island, "InsGlassHover", () => island.Background);
    }

    [AvaloniaFact]
    public void A_still_button_does_not_dip_when_pressed()
    {
        var button = new Button { Classes = { "primary" }, Content = "حفظ", HorizontalAlignment = HorizontalAlignment.Left };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { button } });
        Motion.SetStill(window, true);
        Settle();
        window.MouseDown(Center(button, window), MouseButton.Left);
        Settle();
        Assert.True(IsIdentity(Part<Panel>(button, "PART_Root").RenderTransform));
        window.MouseUp(Center(button, window), MouseButton.Left);
    }

    [AvaloniaFact]
    public void A_still_sidebar_link_does_not_nudge()
    {
        var nav = new ListBox { Classes = { "shell-nav" }, Width = 240, Items = { new ListBoxItem { Content = "الرئيسية" }, new ListBoxItem { Content = "الطلبات" } } };
        var window = Show(nav, width: 400, height: 300);
        Motion.SetStill(window, true);
        var link = (ListBoxItem)nav.ContainerFromIndex(1)!;
        window.MouseMove(Center(link, window));
        Settle();
        Assert.True(IsIdentity(link.RenderTransform));
        AssertBrush(link, "InsGlassInner", () => link.Background);
    }
}
