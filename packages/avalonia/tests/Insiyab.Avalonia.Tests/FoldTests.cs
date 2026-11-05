using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Insiyab.Avalonia.Tests;

// ins:Fold: a child that grows from nothing and fades in, and back; clipped only while it moves; and the flush Expander head and the fold chevron that go with it.
public class FoldTests
{
    // Folds run on a clock this holds and steps, so a slow run sees the same frames as a fast one.
    private sealed class HeldClock : IDisposable
    {
        private readonly Func<double> _real = Fold.Now;
        private double _at;
        public HeldClock() { _at = _real(); Fold.Now = () => _at; }
        public void Step(Window w, double ms)
        {
            _at += ms;
            for (var i = 0; i < 3; i++) { w.InvalidateVisual(); Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); }
            Dispatcher.UIThread.RunJobs();
        }
        public void Dispose() => Fold.Now = _real;
    }

    private static Border Block(double h) => new() { Width = 200, Height = h, Background = Brushes.Teal };

    [AvaloniaFact]
    public void Opens_from_nothing_to_its_child_and_fades_with_it()
    {
        var fold = new Fold { Child = Block(100) };
        var after = new Border { Height = 10 };
        var w = Show(new StackPanel { Children = { fold, after } });
        Assert.False(fold.IsVisible);
        using var clock = new HeldClock();
        fold.IsOpen = true;
        clock.Step(w, 0);
        Assert.True(fold.IsVisible);
        Assert.InRange(fold.Bounds.Height, 0, 1);
        clock.Step(w, 140);
        Assert.InRange(fold.Bounds.Height, 5, 99);
        Assert.InRange(fold.Opacity, 0.05, 0.99);
        Assert.NotNull(fold.Clip);
        clock.Step(w, 200);
        Assert.InRange(fold.Bounds.Height, 99.5, 100.5);
        Assert.Equal(1, fold.Opacity);
        Assert.Null(fold.Clip);
        Assert.InRange(after.Bounds.Y, 99.5, 100.5);
    }

    [AvaloniaFact]
    public void Closes_out_of_its_panel_so_the_spacing_goes_too()
    {
        var a = new Border { Height = 20 };
        var fold = new Fold { IsOpen = true, Child = Block(60) };
        var b = new Border { Height = 20 };
        var w = Show(new StackPanel { Spacing = 10, Children = { a, fold, b } });
        Assert.InRange(b.Bounds.Y, 20 + 10 + 60 + 10 - 0.5, 20 + 10 + 60 + 10 + 0.5);
        using var clock = new HeldClock();
        fold.IsOpen = false;
        clock.Step(w, 0);
        Assert.True(fold.IsVisible);
        Assert.InRange(fold.Bounds.Height, 59, 60.5);
        clock.Step(w, 90);
        Assert.True(fold.Bounds.Height > 0 && fold.Bounds.Height < 60, $"height {fold.Bounds.Height}");
        clock.Step(w, 200);
        Assert.False(fold.IsVisible);
        Assert.InRange(b.Bounds.Y, 29.5, 30.5);
    }

    [AvaloniaFact]
    public void Clips_only_while_moving_and_bleed_frees_the_sides()
    {
        var fold = new Fold { Bleed = 12, Child = Block(100) };
        var w = Show(new StackPanel { Children = { fold } });
        Assert.Null(fold.Clip);
        using var clock = new HeldClock();
        fold.IsOpen = true;
        clock.Step(w, 100);
        var r = Assert.IsType<RectangleGeometry>(fold.Clip).Rect;
        Assert.InRange(r.X, -12.5, -11.5);
        Assert.InRange(r.Y, -12.5, -11.5);
        Assert.InRange(r.Width, fold.Bounds.Width + 23.5, fold.Bounds.Width + 24.5);
        Assert.InRange(r.Height, fold.Bounds.Height + 11.5, fold.Bounds.Height + 12.5);
        clock.Step(w, 300);
        Assert.Null(fold.Clip);
        fold.Bleed = 0;
        fold.IsOpen = false;
        clock.Step(w, 60);
        var closing = Assert.IsType<RectangleGeometry>(fold.Clip).Rect;
        Assert.InRange(closing.X, -0.5, 0.5);
        Assert.InRange(closing.Y, -0.5, 0.5);
        Assert.InRange(closing.Width, fold.Bounds.Width - 0.5, fold.Bounds.Width + 0.5);
    }

    [AvaloniaFact]
    public void Snaps_before_its_first_layout_on_new_data_and_in_a_still_window()
    {
        var host = new StackPanel();
        var w = Show(host);
        using var clock = new HeldClock();
        var fold = new Fold { Child = Block(80) };
        host.Children.Add(fold);
        fold.IsOpen = true;
        clock.Step(w, 0);
        Assert.InRange(fold.Bounds.Height, 79.5, 80.5);
        Settle(100);
        fold.DataContext = new object();
        fold.IsOpen = false;
        clock.Step(w, 0);
        Assert.False(fold.IsVisible);
        Settle(100);
        Motion.SetStill(w, true);
        fold.IsOpen = true;
        clock.Step(w, 0);
        Assert.InRange(fold.Bounds.Height, 79.5, 80.5);
        Assert.Null(fold.Clip);
    }

    [AvaloniaFact]
    public void Eases_a_height_change_when_asked()
    {
        var child = Block(100);
        var fold = new Fold { IsOpen = true, EasesHeight = true, Child = child };
        var w = Show(new StackPanel { Children = { fold } });
        using var clock = new HeldClock();
        child.Height = 160;
        clock.Step(w, 0);
        Assert.InRange(fold.Bounds.Height, 99, 101);
        clock.Step(w, 140);
        Assert.True(fold.Bounds.Height > 101 && fold.Bounds.Height < 159, $"height {fold.Bounds.Height}");
        Assert.Equal(1, fold.Opacity);
        clock.Step(w, 200);
        Assert.InRange(fold.Bounds.Height, 159.5, 160.5);
        fold.EasesHeight = false;
        child.Height = 60;
        clock.Step(w, 0);
        Assert.InRange(fold.Bounds.Height, 59.5, 60.5);
        fold.EaseNext();
        child.Height = 120;
        clock.Step(w, 0);
        Assert.InRange(fold.Bounds.Height, 59, 61);
        clock.Step(w, 400);
        Assert.InRange(fold.Bounds.Height, 119.5, 120.5);
        child.Height = 90;
        clock.Step(w, 0);
        Assert.InRange(fold.Bounds.Height, 89.5, 90.5);
    }

    [AvaloniaFact]
    public void The_fold_chevron_turns_with_open_and_holds_still_in_a_still_window()
    {
        var icon = new Icon { Classes = { "fold" }, Size = 16 };
        var w = Show(icon);
        Assert.NotNull(icon.Data);
        Assert.True(icon.RenderTransform is null || icon.RenderTransform.Value.IsIdentity);
        Assert.Single(icon.Transitions!);
        icon.Classes.Add("open");
        Settle();
        Assert.InRange(icon.RenderTransform!.Value.M11, -1.01, -0.99);
        Motion.SetStill(w, true);
        Settle(100);
        Assert.True(icon.Transitions is null || icon.Transitions.Count == 0);
    }

    [AvaloniaFact]
    public void A_flush_expander_head_has_no_padding_and_its_chevron_on_the_first_line()
    {
        var e = new Expander { Classes = { "flush" }, Header = new TextBlock { Text = "بند في القائمة" }, Content = "التفاصيل.", IsExpanded = true };
        var w = Show(e, FlowDirection.RightToLeft);
        var head = Part<ToggleButton>(e, "ExpanderHeader");
        Assert.Equal(new Thickness(0), head.Padding);
        Assert.Equal(new Thickness(0), Part<Border>(e, "ExpanderContent").Padding);
        var chev = Part<ShapePath>(head, "PART_Chevron");
        Assert.Equal(VerticalAlignment.Top, chev.VerticalAlignment);
        Assert.InRange(chev.Bounds.Y, 6, 10);
        AssertBrush(head, "InsText1", () => head.Foreground);
        w.MouseMove(Center(head, w));
        Settle();
        Assert.Equal(0, ColorOf(Part<Border>(head, "PART_Face").Background).A);
    }
}
