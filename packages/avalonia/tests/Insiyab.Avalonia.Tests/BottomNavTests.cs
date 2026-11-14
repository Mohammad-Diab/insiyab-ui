using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The phone's bottom bar: equal items, the brand pill that flies, a second press, keys, screen readers, the safe area and light effects.
public class BottomNavTests
{
    private static readonly Geometry Glyph = Geometry.Parse("M4 12h16 M12 4v16");

    private static BottomNav Make() => new()
    {
        Items =
        {
            new BottomNavItem { Icon = Glyph, Label = "Home" },
            new BottomNavItem { Icon = Glyph, Label = "Orders", Badge = 5 },
            new BottomNavItem { Icon = Glyph, Label = "Files", ShowDot = true },
            new BottomNavItem { Icon = Glyph, Label = "Account", Badge = 120 },
        },
        SelectedIndex = 0,
    };

    private static Window Host(BottomNav nav, FlowDirection dir = FlowDirection.LeftToRight) =>
        Show(new DockPanel { Children = { nav, new Border() } }.Also(d => DockPanel.SetDock(nav, Dock.Bottom)), dir, width: 400, height: 700);

    private static BottomNavItem Item(BottomNav nav, int i) => (BottomNavItem)nav.ContainerFromIndex(i)!;

    private sealed class Held : IDisposable
    {
        private readonly Func<double> _real = MarkerFlight.Now;
        private readonly double _from;
        private double _at;

        public Held()
        {
            _from = _at = _real();
            MarkerFlight.Now = () => _at;
        }

        public void At(Window w, double ms)
        {
            _at = _from + ms;
            w.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => MarkerFlight.Now = _real;
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void Items_share_the_width_with_the_chosen_pill_behind_its_icon_and_their_badges(string dir)
    {
        var nav = Make();
        var w = Host(nav, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        Assert.Equal(400, nav.Bounds.Width, 1);
        for (var i = 0; i < 4; i++) Assert.Equal(100 - 2, Item(nav, i).Bounds.Width, 1);
        var first = Center(Item(nav, 0), w).X;
        var last = Center(Item(nav, 3), w).X;
        Assert.True(dir == "rtl" ? first > last : first < last, $"the first item is at {first}, the last at {last}");

        Assert.Equal(1, Part<Border>(Item(nav, 0), "PART_Pill").Opacity);
        Assert.Equal(0, Part<Border>(Item(nav, 1), "PART_Pill").Opacity);
        var icon = Part<Icon>(Item(nav, 0), "PART_Icon");
        var label = Part<TextBlock>(Item(nav, 0), "PART_Label");
        Assert.True(Center(icon, w).Y < Center(label, w).Y, "the icon sits over its label");
        AssertBrush(nav, "InsToneBrand", () => icon.Foreground);

        Assert.True(Part<Border>(Item(nav, 1), "PART_Badge").IsVisible);
        Assert.Equal("5", Item(nav, 1).BadgeText);
        Assert.Equal("99+", Item(nav, 3).BadgeText);
        Assert.False(Part<Border>(Item(nav, 2), "PART_Badge").IsVisible);
        Assert.True(Part<Border>(Item(nav, 2), "PART_Dot").IsVisible);
        Assert.False(Part<Border>(Item(nav, 0), "PART_Dot").IsVisible);

        Item(nav, 1).Badge = 0;
        Assert.False(Part<Border>(Item(nav, 1), "PART_Badge").IsVisible);
    }

    [AvaloniaFact]
    public void Choosing_flies_the_pill_to_the_new_item_and_a_second_press_reselects()
    {
        var nav = Make();
        var w = Host(nav);
        var again = 0;
        nav.Reselected += (_, _) => again++;
        var flight = Part<Border>(nav, "PART_Flight");
        var from = Part<Border>(Item(nav, 0), "PART_Pill").TranslatePoint(default, flight.Parent as Visual ?? nav)!.Value.X;
        var to = Part<Border>(Item(nav, 2), "PART_Pill").TranslatePoint(default, flight.Parent as Visual ?? nav)!.Value.X;

        using (var clock = new Held())
        {
            var p = Center(Item(nav, 2), w);
            w.MouseMove(p);
            w.MouseDown(p, MouseButton.Left);
            w.MouseUp(p, MouseButton.Left);
            clock.At(w, 0);
            Assert.Equal(2, nav.SelectedIndex);
            Assert.Contains("moving", nav.Classes);
            Assert.True(flight.IsVisible);
            Assert.Equal(0, Part<Border>(Item(nav, 2), "PART_Pill").Opacity);
            Assert.Equal(from, Canvas.GetLeft(flight), 1);
            clock.At(w, 300);
            var mid = Canvas.GetLeft(flight);
            Assert.True(mid > from && mid <= to, $"half way the pill is at {mid}");
            clock.At(w, 700);
            Assert.DoesNotContain("moving", nav.Classes);
            Assert.False(flight.IsVisible);
        }
        Settle(300);
        Assert.Equal(1, Part<Border>(Item(nav, 2), "PART_Pill").Opacity);
        Assert.Equal(0, again);

        Click(Item(nav, 2), w);
        Assert.Equal(1, again);
        Assert.Equal(2, nav.SelectedIndex);
        Click(Item(nav, 0), w);
        Assert.Equal(1, again);
    }

    [AvaloniaFact]
    public void A_finger_tap_on_the_chosen_item_reselects_and_a_still_window_moves_the_pill_at_once()
    {
        var nav = Make();
        var w = Host(nav);
        Touch.SetTouch(w, true);
        var again = 0;
        nav.Reselected += (_, _) => again++;
        var p = Center(Item(nav, 0), w);
        var t = w.TouchBegin(p);
        Settle(40);
        w.TouchEnd(t, p);
        Settle(100);
        Assert.Equal(1, again);

        Motion.SetStill(w, true);
        nav.SelectedIndex = 3;
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain("moving", nav.Classes);
        Assert.Equal(1, Part<Border>(Item(nav, 3), "PART_Pill").Opacity);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_arrows_move_along_the_bar_as_it_reads_and_home_and_end_jump(string dir)
    {
        var nav = Make();
        var w = Host(nav, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
        var before = new Button { Content = "Before" };
        ((DockPanel)w.Content!).Children.Insert(0, before);
        DockPanel.SetDock(before, Dock.Top);
        Settle(100);
        before.Focus();
        w.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Settle(100);
        Assert.True(Item(nav, 0).IsFocused, "Tab enters the bar on the chosen item");

        var forward = dir == "rtl" ? Key.Left : Key.Right;
        var back = dir == "rtl" ? Key.Right : Key.Left;
        w.KeyPress(forward, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(100);
        Assert.Equal(1, nav.SelectedIndex);
        w.KeyPress(back, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(100);
        Assert.Equal(0, nav.SelectedIndex);
        w.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(100);
        Assert.Equal(3, nav.SelectedIndex);
        w.KeyPress(Key.Home, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(100);
        Assert.Equal(0, nav.SelectedIndex);

        var again = 0;
        nav.Reselected += (_, _) => again++;
        w.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Settle(100);
        Assert.Equal(1, again);
    }

    [AvaloniaFact]
    public void Screen_readers_hear_tabs_named_by_label_and_count()
    {
        var nav = Make();
        Host(nav);
        var bar = ControlAutomationPeer.CreatePeerForElement(nav);
        Assert.Equal(AutomationControlType.Tab, bar.GetAutomationControlType());
        var orders = ControlAutomationPeer.CreatePeerForElement(Item(nav, 1));
        Assert.Equal(AutomationControlType.TabItem, orders.GetAutomationControlType());
        Assert.Equal("Orders, 5", orders.GetName());
        Assert.Equal("Files, New", ControlAutomationPeer.CreatePeerForElement(Item(nav, 2)).GetName());
        Assert.Equal("Home", ControlAutomationPeer.CreatePeerForElement(Item(nav, 0)).GetName());
    }

    [AvaloniaFact]
    public void The_bar_pads_itself_by_the_safe_area_and_turns_solid_with_light_effects()
    {
        var fake = new FakeInsets();
        fake.Set(new Thickness(0, 24, 0, 30));
        FakeInsets.With(fake, () =>
        {
            var nav = Make();
            var root = OverlaysTests.SingleView(new DockPanel { Children = { nav, new Border() } }.Also(d => DockPanel.SetDock(nav, Dock.Bottom)), 390, 760);
            try
            {
                Assert.Equal(new Thickness(4, 0, 4, 30), nav.Padding);
                var item = Item(nav, 0);
                var foot = item.TranslatePoint(new Point(0, item.Bounds.Height), root)!.Value.Y;
                Assert.Equal(760 - 30, foot, 1);
                var ground = Part<Border>(nav, "PART_Ground");
                Assert.Equal(760, ground.TranslatePoint(new Point(0, ground.Bounds.Height), root)!.Value.Y, 1);
                Assert.Equal(0.92, ground.Opacity, 2);
                Assert.Equal(new Thickness(0, 1, 0, 0), ground.BorderThickness);
                InsiyabTheme.Effects = EffectsLevel.Light;
                OverlaysTests.Pump(root, 100);
                Assert.Equal(1, ground.Opacity);
            }
            finally
            {
                InsiyabTheme.Effects = EffectsLevel.Full;
                root.StopRendering();
            }
        });
    }
}

internal static class AlsoExtension
{
    public static T Also<T>(this T value, Action<T> act)
    {
        act(value);
        return value;
    }
}
