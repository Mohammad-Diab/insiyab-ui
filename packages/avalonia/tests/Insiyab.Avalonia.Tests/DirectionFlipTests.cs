using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class DirectionFlipTests
{
    private static Panel? Picture(Window window) =>
        window.GetVisualDescendants().OfType<Panel>().FirstOrDefault(p => p.Name == "PART_DirectionFlip");

    private static Control Content(Window window) =>
        (Control)window.GetVisualDescendants().OfType<VisualLayerManager>().First().Child!;

    private static Border? NewPage(Window window) =>
        window.GetVisualDescendants().OfType<Border>().FirstOrDefault(p => p.Name == "PART_DirectionFlipNew");

    // The picture is drawn on the render thread, so the change runs a frame or two after the call.
    private static void WaitFor(Func<bool> condition, int milliseconds = 4000)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && clock.ElapsedMilliseconds < milliseconds) Settle(40);
        Settle(40);
    }

    private static Window Rig(FlowDirection dir)
    {
        var window = new Window
        {
            Width = 800,
            Height = 500,
            FlowDirection = dir,
            Background = Brushes.White,
            // a red band along the start edge, so a page drawn mirrored would show
            Content = new Border { Background = Brushes.Blue, Child = new Border { Background = Brushes.Red, Width = 100, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left } },
        };
        window.Show();
        Track(window);
        Settle();
        return window;
    }

    private static FlowDirection Other(FlowDirection d) => d == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // Held half way: the old picture over the window's ground is half gone, half blurred and a little darker; the page over it half shown, half turned, half blurred.
    [AvaloniaFact]
    public void The_change_runs_under_the_old_picture_and_the_new_page_turns_in_over_it()
    {
        try
        {
            var window = Rig(FlowDirection.RightToLeft);
            window.Resources["InsEase"] = new Fixed(0.5);
            var applied = false;
            DirectionFlip.Held = true;
            DirectionFlip.Run(window, () => { applied = true; window.FlowDirection = FlowDirection.LeftToRight; });
            WaitFor(() => applied);
            Assert.True(applied);
            WaitFor(() => NewPage(window) is not null);
            var picture = Picture(window);
            Assert.NotNull(picture);
            Assert.False(picture!.IsHitTestVisible);
            Assert.Equal(Matrix.Identity, picture.TransformToVisual(window));
            var old = Assert.Single(picture.Children.OfType<Panel>());
            Assert.Equal(0.5, old.Opacity, 3);
            Assert.Equal(8, Assert.IsType<BlurEffect>(old.Effect).Radius, 3);
            Assert.Equal(0.05, old.Children.OfType<Border>().Single().Opacity, 3);
            var page = NewPage(window)!;
            Assert.Same(page, picture.Children[^1]); // over the old one
            Assert.Equal(0.5, page.Opacity, 3);
            Assert.Equal(8, Assert.IsType<BlurEffect>(page.Effect).Radius, 3);
            Assert.Equal(DirectionFlip.Turned(14, rtl: false), Assert.IsType<MatrixTransform>(page.RenderTransform).Matrix);
            Assert.Single(picture.Children.OfType<Border>()); // a window of the library's keeps its own ground under the page
            var content = Content(window);
            Assert.Equal(0, content.Opacity); // the live page waits under the pictures
            DirectionFlip.Held = false;
            Settle(900);
            Assert.Null(Picture(window));
            Assert.Equal(1, content.Opacity);
            Assert.False(content.IsSet(Visual.OpacityProperty));
        }
        finally { DirectionFlip.Held = false; }
    }

    // On screen, once the old picture is gone (0.3s) and the page fully shown, still turned (its turn eased to nothing yet): the page's start edge stays where the new direction starts (the left in English, the right in Arabic) and its far edge has gone back toward the middle. Light effects keep the turn with no blur.
    [AvaloniaTheory]
    [InlineData("ltr", 1.0)]
    [InlineData("rtl", 1.0)]
    [InlineData("ltr", 2.0)]
    [InlineData("rtl", 2.0)]
    public void The_page_opens_from_the_start_edge_of_its_new_direction(string to, double scale)
    {
        var now = 1_000_000.0;
        var clock = DirectionFlip.Now;
        try
        {
            InsiyabTheme.Effects = EffectsLevel.Light;
            DirectionFlip.Now = () => now;
            var target = to == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            var window = Rig(Other(target));
            window.SetRenderScaling(scale);
            Settle(80);
            window.Resources["InsEase"] = new Fixed(0);
            var applied = false;
            DirectionFlip.Run(window, () => { applied = true; window.FlowDirection = target; });
            WaitFor(() => NewPage(window) is not null);
            Assert.True(applied);
            now += 300;
            Settle(80);
            Assert.Null(NewPage(window)!.Effect);
            Assert.Null(Picture(window)!.Children.OfType<Panel>().Single().Effect);
            using var frame = window.CaptureRenderedFrame()!;
            using var buffer = frame.Lock();
            // at the screen's density, the page drawn at its own size
            Color At(int x, int y) => PixelAt(buffer, (int)((to == "rtl" ? 799 - x : x) * scale), (int)(y * scale));
            Assert.True(IsRed(At(40, 250)), "the start edge holds, the page drawn the right way round");
            Assert.True(IsRed(At(40, 6)), "the start edge keeps its height");
            Assert.True(IsBlue(At(160, 250)), "the page past its start band");
            Assert.True(IsBlue(At(560, 250)), "the page reaches toward its far edge");
            Assert.True(IsWhite(At(760, 250)), "the far edge has gone back toward the middle");
            Assert.True(IsWhite(At(400, 6)), "the page shrinks away from its start edge");
            now += 300;
            Settle(80);
            Assert.Null(Picture(window));
        }
        finally
        {
            DirectionFlip.Now = clock;
            InsiyabTheme.ResetEffects();
        }
    }

    // A view with no ground of the library's under its content (a phone's, whose page paints its own and turns with it): the ground's colour stands behind the turning page, under the old picture, so nothing behind the window shows through the gap.
    [AvaloniaFact]
    public void A_view_with_no_ground_of_its_own_gets_the_grounds_colour_behind_the_page()
    {
        try
        {
            var window = new Window
            {
                Width = 800,
                Height = 500,
                Content = new Border { Background = Brushes.Blue },
                Template = new FuncControlTemplate<Window>((w, _) => new VisualLayerManager
                {
                    Child = new ContentPresenter { Name = "PART_ContentPresenter", [!ContentPresenter.ContentProperty] = w[!ContentControl.ContentProperty] },
                }),
            };
            window.Show();
            Track(window);
            Settle();
            DirectionFlip.Held = true;
            var applied = false;
            DirectionFlip.Run(window, () => applied = true);
            WaitFor(() => applied);
            var picture = Picture(window)!;
            var ground = Assert.IsType<Border>(picture.Children[0]);
            Assert.NotEqual("PART_DirectionFlipNew", ground.Name);
            Assert.Equal(ColorOf(Assert.IsAssignableFrom<IBrush>(Res(window, "InsWash1"))), ColorOf(ground.Background));
        }
        finally { DirectionFlip.Held = false; }
    }

    // An app that finds its device weak asks for no blur: both pages are drawn sharp, the page still turns.
    [AvaloniaFact]
    public void Asked_for_no_blur_the_page_still_turns_sharp()
    {
        try
        {
            var window = Rig(FlowDirection.RightToLeft);
            window.Resources["InsEase"] = new Fixed(0.5);
            DirectionFlip.Held = true;
            DirectionFlip.Run(window, () => window.FlowDirection = FlowDirection.LeftToRight, blur: false);
            WaitFor(() => NewPage(window) is not null);
            var page = NewPage(window)!;
            Assert.Null(page.Effect);
            Assert.Null(Picture(window)!.Children.OfType<Panel>().Single().Effect);
            Assert.Equal(DirectionFlip.Turned(14, rtl: false), Assert.IsType<MatrixTransform>(page.RenderTransform).Matrix);
        }
        finally { DirectionFlip.Held = false; }
    }

    [AvaloniaFact]
    public void A_second_change_lands_the_first()
    {
        try
        {
            var window = Rig(FlowDirection.RightToLeft);
            var applies = new List<string>();
            DirectionFlip.Held = true;
            DirectionFlip.Run(window, () => { applies.Add("en"); window.FlowDirection = FlowDirection.LeftToRight; });
            WaitFor(() => Picture(window) is not null);
            DirectionFlip.Run(window, () => { applies.Add("ar"); window.FlowDirection = FlowDirection.RightToLeft; });
            WaitFor(() => applies.Count == 2);
            WaitFor(() => Picture(window) is not null);
            Assert.Equal(["en", "ar"], applies);
            Assert.Single(window.GetVisualDescendants().OfType<Panel>(), p => p.Name == "PART_DirectionFlip");
            DirectionFlip.Held = false;
            Settle(900);
            Assert.Null(Picture(window));
            Assert.Equal(1, Content(window).Opacity);
        }
        finally { DirectionFlip.Held = false; }
    }

    [AvaloniaFact]
    public void A_second_change_while_the_picture_is_pending_applies_the_first_at_once()
    {
        var window = Rig(FlowDirection.RightToLeft);
        var applies = new List<string>();
        DirectionFlip.Run(window, () => applies.Add("en"));
        Assert.Empty(applies);
        DirectionFlip.Run(window, () => applies.Add("ar"));
        Assert.Equal(["en"], applies);
        WaitFor(() => applies.Count == 2);
        Assert.Equal(["en", "ar"], applies);
        Settle(900);
        Assert.Null(Picture(window));
    }

    [AvaloniaFact]
    public void A_still_window_changes_at_once_with_no_picture()
    {
        var window = Rig(FlowDirection.RightToLeft);
        Motion.SetStill(window, true);
        var applied = false;
        DirectionFlip.Run(window, () => applied = true);
        Assert.True(applied);
        Assert.Null(Picture(window));
        Assert.Equal(1, Content(window).Opacity);
    }

    [AvaloniaFact]
    public void A_minimised_window_changes_at_once()
    {
        var window = Rig(FlowDirection.RightToLeft);
        window.WindowState = WindowState.Minimized;
        var applied = false;
        DirectionFlip.Run(window, () => applied = true);
        Assert.True(applied);
        Assert.Null(Picture(window));
    }

    private static bool IsRed(Color c) => c.R > 200 && c.G < 60 && c.B < 60;
    private static bool IsBlue(Color c) => c.R < 60 && c.G < 60 && c.B > 200;
    private static bool IsWhite(Color c) => c.R > 230 && c.G > 230 && c.B > 230;

    // Pins the eased progress at a fixed share until the end.
    private sealed class Fixed(double share) : Easing
    {
        public override double Ease(double progress) => progress < 1 ? share : 1;
    }
}
