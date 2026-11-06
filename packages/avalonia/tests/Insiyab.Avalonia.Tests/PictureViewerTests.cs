using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class PictureViewerTests
{
    private static WriteableBitmap Picture(int width, int height) =>
        new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);

    private static (Window Window, Button Owner) Host(double width = 1000, double height = 800, FlowDirection dir = FlowDirection.LeftToRight)
    {
        var owner = new Button { Content = "افتح", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Children = { owner } }, dir, width: width, height: height);
        return (window, owner);
    }

    private static PictureZoom Zoom(Visual root) => root.GetVisualDescendants().OfType<PictureZoom>().Single();

    private static Dialog Viewer(Visual root) => root.GetVisualDescendants().OfType<Dialog>().Single(d => d.IsOpen);

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Settle(60);
    }

    private static Button Tool(Dialog dialog, string name) =>
        dialog.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == name);

    [AvaloniaFact]
    public void The_frame_is_half_the_window_and_the_picture_fits_inside_never_past_its_own_size()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000), "الواجهة");
        Settle();
        var dialog = Viewer(window);
        var zoom = Zoom(dialog);
        Assert.Equal("الواجهة", dialog.Title);
        Assert.Equal(new Size(500, 400), zoom.Bounds.Size);
        Assert.Equal(0.25, zoom.Zoom, 4);
        Assert.Equal(550, dialog.MaxWidth, 1);
        Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "25%");
        Assert.False(Tool(dialog, "Zoom out").IsEnabled);
        dialog.Close();
        Settle(60);

        _ = PictureViewer.ShowAsync(owner, Picture(100, 50));
        Settle();
        dialog = Viewer(window);
        Assert.Null(dialog.Title);
        Assert.Equal(1, Zoom(dialog).Zoom, 4);
        Assert.Equal("View picture", AutomationProperties.GetName(dialog));
        var reset = dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Actual size");
        Assert.False(reset.IsEnabled);
    }

    [AvaloniaFact]
    public void The_wheel_zooms_about_the_pointer_and_nothing_else_moves()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000));
        Settle();
        var dialog = Viewer(window);
        var zoom = Zoom(dialog);
        var dialogBox = dialog.Bounds;
        var at = new Point(150, 120);
        var before = (at - zoom.Origin()) / zoom.Zoom;
        window.MouseMove(zoom.TranslatePoint(at, window)!.Value);
        window.MouseWheel(zoom.TranslatePoint(at, window)!.Value, new Vector(0, 1));
        window.MouseWheel(zoom.TranslatePoint(at, window)!.Value, new Vector(0, 1));
        Settle(60);
        Assert.Equal(0.25 * 1.25 * 1.25, zoom.Zoom, 4);
        var after = (at - zoom.Origin()) / zoom.Zoom;
        Assert.Equal(before.X, after.X, 1);
        window.MouseWheel(zoom.TranslatePoint(at, window)!.Value, new Vector(0, 1));
        Settle(60);
        var third = (at - zoom.Origin()) / zoom.Zoom;
        Assert.Equal(after.X, third.X, 1);
        Assert.Equal(after.Y, third.Y, 1);
        Assert.Equal(dialogBox, dialog.Bounds);
        Assert.Equal(new Size(500, 400), zoom.Bounds.Size);
    }

    [AvaloniaFact]
    public void Keys_zoom_fit_and_move_the_picture_while_open_and_rest_after_it_closes()
    {
        var (window, owner) = Host();
        var shown = PictureViewer.ShowAsync(owner, Picture(2000, 1000));
        Settle();
        var dialog = Viewer(window);
        var zoom = Zoom(dialog);
        Assert.True(zoom.IsFocused);
        Press(window, Key.OemPlus);
        Assert.Equal(0.3125, zoom.Zoom, 4);
        Press(window, Key.D1);
        Assert.Equal(1, zoom.Zoom, 4);
        var x = zoom.Offset.X;
        Press(window, Key.Right);
        Assert.Equal(x + 48, zoom.Offset.X, 1);
        Press(window, Key.Subtract);
        Assert.Equal(0.8, zoom.Zoom, 4);
        Press(window, Key.D0);
        Assert.True(zoom.IsFitted);
        Press(window, Key.Escape);
        Settle(60);
        Assert.False(dialog.IsOpen);
        Assert.True(shown.IsCompleted);
        Press(window, Key.OemPlus);
        Assert.True(zoom.IsFitted);
    }

    [AvaloniaFact]
    public void A_drag_moves_a_zoomed_picture_and_stops_at_its_edges()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000));
        Settle();
        var zoom = Zoom(Viewer(window));
        zoom.ShowActual();
        Settle(60);
        var middle = zoom.TranslatePoint(new Point(250, 200), window)!.Value;
        window.MouseMove(middle);
        window.MouseDown(middle, MouseButton.Left);
        window.MouseMove(middle + new Point(-3000, -3000));
        window.MouseUp(middle + new Point(-3000, -3000), MouseButton.Left);
        Settle(60);
        Assert.Equal(new Vector(1500, 600), zoom.Offset);
        window.MouseMove(middle);
        window.MouseDown(middle, MouseButton.Left);
        window.MouseMove(middle + new Point(100, 0));
        window.MouseUp(middle + new Point(100, 0), MouseButton.Left);
        Settle(60);
        Assert.Equal(new Vector(1400, 600), zoom.Offset);
    }

    [AvaloniaFact]
    public void It_opens_in_a_single_view_with_half_of_it_for_the_frame()
    {
        var owner = new Button { Content = "افتح" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { owner } }, 400, 700);
        try
        {
            _ = PictureViewer.ShowAsync(owner, Picture(800, 800), "صورة");
            OverlaysTests.Pump(root);
            var dialog = Viewer(root);
            Assert.True(dialog.IsOpen);
            var zoom = Zoom(dialog);
            Assert.Equal(new Size(200, 350), zoom.Bounds.Size);
            Assert.Equal(0.25, zoom.Zoom, 4);
            dialog.Close();
            OverlaysTests.Pump(root, 100);
        }
        finally
        {
            root.StopRendering();
        }
    }

    [AvaloniaFact]
    public void A_button_holding_a_picture_opens_it_and_one_without_lets_the_press_through()
    {
        var inner = new Button { Content = "س", Width = 40, Height = 40 };
        var card = new Button { Content = inner, Padding = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var cardClicks = 0;
        card.Click += (_, _) => cardClicks++;
        PictureViewer.SetTitle(inner, "سارة");
        PictureViewer.SetSource(inner, null);
        var window = Show(new Panel { Children = { card } });
        Assert.False(inner.IsHitTestVisible);
        Assert.False(inner.Focusable);
        Click(inner, window);
        Assert.Equal(1, cardClicks);
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Dialog>(), d => d.IsOpen);

        PictureViewer.SetSource(inner, Picture(300, 200));
        Assert.True(inner.IsHitTestVisible);
        Assert.True(inner.Focusable);
        Assert.Equal("View picture", ToolTip.GetTip(inner));
        Click(inner, window);
        var dialog = Viewer(window);
        Assert.Equal("سارة", dialog.Title);
        Assert.Equal(1, cardClicks);
        dialog.Close();
    }

    [AvaloniaFact]
    public void An_arabic_page_names_the_buttons_in_arabic()
    {
        var (window, owner) = Host(dir: FlowDirection.RightToLeft);
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000));
        Settle();
        var dialog = Viewer(window);
        Assert.True(Tool(dialog, "تكبير").IsEnabled);
        Assert.False(Tool(dialog, "تصغير").IsEnabled);
        Assert.Contains(dialog.GetVisualDescendants().OfType<Button>(), b => b.Content as string == "الحجم الأصلي");
        Assert.Equal(FlowDirection.LeftToRight, Zoom(dialog).FlowDirection);
    }
}
