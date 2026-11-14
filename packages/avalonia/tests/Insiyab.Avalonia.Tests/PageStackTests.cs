using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;
using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// Pages one over another: the slide from the end side, Back after the overlays, a mouse's back button, the swipe from the start edge, still windows and focus.
public class PageStackTests
{
    private sealed class Held : IDisposable
    {
        private readonly Func<double> _real = FrameTween.Now;
        private readonly double _from;
        private double _at;

        public Held()
        {
            _from = _at = _real();
            FrameTween.Now = () => _at;
        }

        public void At(Window w, double ms)
        {
            _at = _from + ms;
            for (var i = 0; i < 2; i++)
            {
                w.InvalidateVisual();
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            }
        }

        public void Dispose() => FrameTween.Now = _real;
    }

    private static Border Page(string name) => new() { Name = name, Child = new StackPanel { Children = { new TextBlock { Text = name }, new Button { Content = name + " action" } } } };

    private static Control FrameOf(PageStack stack, Control page) => (Control)page.GetVisualParent()!;

    private static bool Back(TopLevel top)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        top.RaiseEvent(e);
        Settle(500);
        return e.Handled;
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_page_slides_in_from_the_end_side_and_back_out_toward_it(string dir)
    {
        var home = Page("Home");
        var stack = new PageStack { Root = home };
        var rtl = dir == "rtl";
        var w = Show(stack, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 400, height: 600);
        var detail = Page("Detail");
        var changes = new List<PageStackChange>();
        stack.Changed += (_, e) => changes.Add(e.Change);

        using (var clock = new Held())
        {
            stack.Push(detail);
            clock.At(w, 0);
            Settle(60);
            Assert.Same(detail, stack.Current);
            Assert.Equal(2, stack.Depth);
            Assert.True(stack.CanGoBack);
            Assert.True(home.IsEffectivelyVisible, "the page under stays seen while the new one slides over it");
            clock.At(w, 50);
            var x = Center(detail, w).X;
            Assert.True(rtl ? x < 200 - 20 : x > 200 + 20, $"early on the new page's middle is at {x}");
            var under = Center(home, w).X;
            Assert.True(rtl ? under > 200 : under < 200, $"the page under moves toward the start, its middle at {under}");
            clock.At(w, 400);
            Assert.Equal(200, Center(detail, w).X, 1);
            Assert.False(home.IsEffectivelyVisible);

            Assert.True(stack.Pop());
            clock.At(w, 400);
            Settle(60);
            clock.At(w, 450);
            x = Center(detail, w).X;
            Assert.True(rtl ? x < 200 - 20 : x > 200 + 20, $"early on the leaving page's middle is at {x}");
            clock.At(w, 900);
        }
        Settle(100);
        Assert.Same(home, stack.Current);
        Assert.Equal(200, Center(home, w).X, 1);
        Assert.Null(detail.GetVisualParent()?.GetVisualParent());
        Assert.False(stack.CanGoBack);
        Assert.False(stack.Pop());
        Assert.Equal(new[] { PageStackChange.Pushed, PageStackChange.Popped }, changes);
    }

    [AvaloniaFact]
    public void Back_closes_overlays_first_then_pops_one_page_and_is_left_to_the_app_at_the_root()
    {
        var stack = new PageStack { Root = Page("Home") };
        var w = Show(stack, width: 400, height: 600);
        stack.Push(Page("List"));
        stack.Push(Page("Detail"));
        Settle(500);
        var dialog = new Dialog { Title = "Details", Content = new TextBlock { Text = "Text" } };
        dialog.Open(stack);
        Settle(300);
        var reached = 0;
        w.BackRequested += (_, e) => { if (!e.Handled) reached++; };

        Assert.True(Back(w));
        Assert.False(dialog.IsOpen);
        Assert.Equal(3, stack.Depth);
        Assert.True(Back(w));
        Assert.Equal(2, stack.Depth);
        Assert.True(Back(w));
        Assert.Equal(1, stack.Depth);
        Assert.False(Back(w));
        Assert.Equal(1, reached);

        stack.Push(Page("Again"));
        Settle(500);
        stack.IsVisible = false;
        Assert.False(Back(w));
        Assert.Equal(2, stack.Depth);
    }

    [AvaloniaFact]
    public void A_mouse_back_button_goes_back_on_a_desktop_window()
    {
        var stack = new PageStack { Root = Page("Home") };
        var w = Show(stack, width: 400, height: 600);
        stack.Push(Page("Detail"));
        Settle(500);
        w.MouseMove(new Point(200, 300));
        w.MouseDown(new Point(200, 300), MouseButton.XButton1);
        w.MouseUp(new Point(200, 300), MouseButton.XButton1);
        Settle(500);
        Assert.Equal(1, stack.Depth);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_swipe_from_the_start_edge_goes_back_and_a_short_one_springs_back(string dir)
    {
        var home = Page("Home");
        var stack = new PageStack { Root = home };
        var rtl = dir == "rtl";
        var w = Show(stack, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 400, height: 600);
        var detail = Page("Detail");
        stack.Push(detail);
        Settle(500);
        var sign = rtl ? -1 : 1;
        var edge = rtl ? 400 - 8 : 8;

        void Swipe(double by, int steps, int wait)
        {
            Settle(400);
            var from = new Point(edge, 300);
            var t = w.TouchBegin(from);
            Settle(30);
            for (var k = 1; k <= steps; k++)
            {
                w.TouchMove(t, from + new Vector(sign * by * k / steps, 2));
                Settle(wait);
            }
            w.TouchEnd(t, from + new Vector(sign * by, 2));
            Settle(600);
        }

        Swipe(80, 8, 40);
        Assert.Equal(2, stack.Depth);
        Assert.Equal(200, Center(detail, w).X, 1);
        Assert.False(home.IsEffectivelyVisible);

        Swipe(240, 8, 20);
        Assert.Equal(1, stack.Depth);
        Assert.Same(home, stack.Current);
        Assert.Equal(200, Center(home, w).X, 1);

        // a swipe that starts away from the edge leaves the page alone
        stack.Push(Page("Other"));
        Settle(500);
        edge = 200;
        Swipe(180, 8, 20);
        Assert.Equal(2, stack.Depth);
    }

    [AvaloniaFact]
    public void A_still_window_swaps_the_pages_at_once()
    {
        var home = Page("Home");
        var stack = new PageStack { Root = home };
        var w = Show(stack, width: 400, height: 600);
        Motion.SetStill(w, true);
        var detail = Page("Detail");
        stack.Push(detail);
        Dispatcher.UIThread.RunJobs();
        Assert.False(FrameOf(stack, home).IsVisible);
        Assert.Null(FrameOf(stack, detail).RenderTransform);
        stack.Pop();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(detail.GetVisualParent()?.GetVisualParent());
        Assert.True(FrameOf(stack, home).IsVisible);
    }

    [AvaloniaFact]
    public void The_keyboard_moves_into_a_new_page_and_comes_back_to_where_it_was()
    {
        var home = Page("Home");
        var stack = new PageStack { Root = home };
        var w = Show(stack, width: 400, height: 600);
        var homeAction = home.GetVisualDescendants().OfType<Button>().First();
        homeAction.Focus(NavigationMethod.Tab);
        var field = new TextBox { Text = "kept" };
        var detail = new StackPanel { Children = { field } };
        stack.Push(detail);
        Settle(500);
        Assert.True(field.IsFocused);
        field.Text = "changed";
        stack.Pop();
        Settle(500);
        Assert.True(homeAction.IsFocused);

        stack.Push(detail);
        Settle(500);
        Assert.Equal("changed", field.Text);
        stack.PopToRoot();
        Settle(500);
        Assert.Equal(1, stack.Depth);
        stack.Root = Page("Fresh");
        Settle(100);
        Assert.Equal(1, stack.Depth);
        Assert.Equal("Fresh", ((Control)stack.Current!).Name);
    }
}
