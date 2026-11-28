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

// The picture viewer filling the window (fillWindow, FillWindow on a button): the picture takes the dialog's whole room, a tap zooms in about the point and back, a pinch zooms about the fingers, a drag reaches every edge.
public class PictureViewerFillTests
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

    private static Point At(PictureZoom zoom, Window window, double x, double y) => zoom.TranslatePoint(new Point(x, y), window)!.Value;

    [AvaloniaFact]
    public void The_picture_takes_the_dialogs_whole_room_with_a_small_gutter_and_no_buttons_but_the_cross()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000), "الواجهة", fillWindow: true);
        Settle();
        var dialog = Viewer(window);
        var zoom = Zoom(dialog);
        Assert.Equal("الواجهة", dialog.Title);
        Assert.Equal(1000 - 96, dialog.Bounds.Width, 1);
        Assert.Equal(dialog.Bounds.Width - 2 - 2 * PictureViewer.Gutter, zoom.Bounds.Width, 1);
        Assert.True(zoom.Bounds.Height > dialog.Bounds.Height - 100, "the picture's view runs down to the dialog's foot, under its head");
        Assert.Equal(zoom.Room, zoom.Bounds.Size);
        Assert.Equal(Math.Min(zoom.Room.Width / 2000, zoom.Room.Height / 1000), zoom.Zoom, 4);
        Assert.True(zoom.IsFitted);
        // the cross is the dialog's one button: no zoom buttons, no percent
        Assert.Single(dialog.GetVisualDescendants().OfType<Button>(), b => b.IsEffectivelyVisible);
        Assert.DoesNotContain(dialog.GetVisualDescendants().OfType<TextBlock>(), t => t.Text?.EndsWith('%') == true);
        dialog.Close();
        Settle(60);

        // no title: the head still holds the cross; the dialog is named for a screen reader
        _ = PictureViewer.ShowAsync(owner, Picture(100, 50), fillWindow: true);
        Settle();
        dialog = Viewer(window);
        Assert.Equal("View picture", AutomationProperties.GetName(dialog));
        Assert.True(Part<Button>(dialog, "PART_Close").IsEffectivelyVisible);
        dialog.Close();
    }

    [AvaloniaFact]
    public void On_a_phone_the_picture_fills_the_width_and_a_small_one_grows_past_its_own_size()
    {
        var owner = new Button { Content = "افتح" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { owner } }, 360, 800);
        try
        {
            _ = PictureViewer.ShowAsync(owner, Picture(600, 200), "f(x)", fillWindow: true);
            OverlaysTests.Pump(root);
            var dialog = Viewer(root);
            var zoom = Zoom(dialog);
            Assert.Equal(360 - 32 - 2 - 2 * PictureViewer.Gutter, zoom.Bounds.Width, 1);
            Assert.True(zoom.Bounds.Height > 600, "the view runs the phone's height");
            Assert.Equal(zoom.Room.Width / 600, zoom.Zoom, 4);
            Assert.Equal(zoom.Room.Width, 600 * zoom.Zoom, 1); // the picture spans the view's width
            dialog.Close();
            OverlaysTests.Pump(root, 100);

            _ = PictureViewer.ShowAsync(owner, Picture(100, 40), fillWindow: true);
            OverlaysTests.Pump(root);
            zoom = Zoom(Viewer(root));
            Assert.Equal(Math.Min(PictureZoom.FitMost, zoom.Room.Width / 100), zoom.Zoom, 4);
            Assert.True(zoom.Zoom > 1, "a small picture is never shown below its own size");
            Viewer(root).Close();
            OverlaysTests.Pump(root, 100);
        }
        finally
        {
            root.StopRendering();
        }
    }

    [AvaloniaFact]
    public void A_tap_zooms_in_about_the_point_tapped_and_a_second_tap_fits_it_again()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000), fillWindow: true);
        Settle();
        var zoom = Zoom(Viewer(window));
        var at = new Point(zoom.Room.Width * 0.3, zoom.Room.Height * 0.4);
        var under = (at - zoom.Origin()) / zoom.Zoom;
        var p = At(zoom, window, at.X, at.Y);
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Settle(60);
        Assert.False(zoom.IsFitted);
        Assert.Equal(zoom.Zoomed, zoom.Zoom, 4);
        var after = (at - zoom.Origin()) / zoom.Zoom;
        Assert.Equal(under.X, after.X, 1);
        Assert.Equal(under.Y, after.Y, 1);

        // a second tap, after the double click's time: the picture fits again
        Settle(600);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Settle(60);
        Assert.True(zoom.IsFitted);
    }

    [AvaloniaFact]
    public void A_drag_moves_a_zoomed_picture_to_every_edge_and_is_no_tap()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000), fillWindow: true);
        Settle();
        var zoom = Zoom(Viewer(window));
        zoom.ShowActual();
        Settle(60);
        var middle = At(zoom, window, zoom.Room.Width / 2, zoom.Room.Height / 2);
        window.MouseMove(middle);
        window.MouseDown(middle, MouseButton.Left);
        window.MouseMove(middle + new Point(-3000, -3000));
        window.MouseUp(middle + new Point(-3000, -3000), MouseButton.Left);
        Settle(60);
        Assert.Equal(1, zoom.Zoom, 4);
        Assert.Equal(new Vector(2000 - zoom.Room.Width, 1000 - zoom.Room.Height), zoom.Offset);
        Settle(600);
        window.MouseMove(middle);
        window.MouseDown(middle, MouseButton.Left);
        window.MouseMove(middle + new Point(3000, 3000));
        window.MouseUp(middle + new Point(3000, 3000), MouseButton.Left);
        Settle(60);
        Assert.Equal(default, zoom.Offset);
        Assert.Equal(1, zoom.Zoom, 4);
    }

    [AvaloniaFact]
    public void Two_fingers_pinch_the_picture_about_their_middle_and_one_finger_moves_it()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000), fillWindow: true);
        Settle();
        var zoom = Zoom(Viewer(window));
        var fit = zoom.Zoom;
        var a = new Point(zoom.Room.Width / 2 - 50, zoom.Room.Height / 2);
        var b = new Point(zoom.Room.Width / 2 + 50, zoom.Room.Height / 2);
        var middle = new Point(zoom.Room.Width / 2, zoom.Room.Height / 2);
        var under = (middle - zoom.Origin()) / zoom.Zoom;
        using var one = window.TouchBegin(At(zoom, window, a.X, a.Y));
        using var two = window.TouchBegin(At(zoom, window, b.X, b.Y));
        window.TouchMove(one, At(zoom, window, a.X - 50, a.Y));
        window.TouchMove(two, At(zoom, window, b.X + 50, b.Y));
        Settle(60);
        Assert.Equal(fit * 2, zoom.Zoom, 3);
        var after = (middle - zoom.Origin()) / zoom.Zoom;
        Assert.Equal(under.X, after.X, 1);
        Assert.Equal(under.Y, after.Y, 1);

        // one finger lifts: the other moves the picture; neither lift is a tap
        window.TouchEnd(two, At(zoom, window, b.X + 50, b.Y));
        var offset = zoom.Offset;
        window.TouchMove(one, At(zoom, window, a.X - 50 - 40, a.Y));
        Settle(60);
        Assert.Equal(offset.X + 40, zoom.Offset.X, 1);
        window.TouchEnd(one, At(zoom, window, a.X - 50 - 40, a.Y));
        Settle(60);
        Assert.Equal(fit * 2, zoom.Zoom, 3);

        // pinched in below the fit, it stays fitted
        using var three = window.TouchBegin(At(zoom, window, a.X - 50, a.Y));
        using var four = window.TouchBegin(At(zoom, window, b.X + 50, b.Y));
        window.TouchMove(three, At(zoom, window, middle.X - 5, a.Y));
        window.TouchMove(four, At(zoom, window, middle.X + 5, b.Y));
        window.TouchEnd(three, At(zoom, window, middle.X - 5, a.Y));
        window.TouchEnd(four, At(zoom, window, middle.X + 5, b.Y));
        Settle(60);
        Assert.True(zoom.IsFitted);
    }

    [AvaloniaFact]
    public void A_finger_tap_zooms_and_fits_as_a_click_does()
    {
        var owner = new Button { Content = "افتح" };
        var root = OverlaysTests.SingleView(new StackPanel { Children = { owner } }, 360, 800);
        try
        {
            _ = PictureViewer.ShowAsync(owner, Picture(600, 200), fillWindow: true);
            OverlaysTests.Pump(root);
            var zoom = Zoom(Viewer(root));
            var p = zoom.TranslatePoint(new Point(100, zoom.Room.Height / 2), root)!.Value;
            using (var finger = root.TouchBegin(p))
                root.TouchEnd(finger, p);
            OverlaysTests.Pump(root, 100);
            Assert.Equal(zoom.Zoomed, zoom.Zoom, 4);
            Assert.True(zoom.CanPan);
            OverlaysTests.Pump(root, 500);
            using (var finger = root.TouchBegin(p))
                root.TouchEnd(finger, p);
            OverlaysTests.Pump(root, 100);
            Assert.True(zoom.IsFitted);
            Viewer(root).Close();
            OverlaysTests.Pump(root, 100);
        }
        finally
        {
            root.StopRendering();
        }
    }

    [AvaloniaFact]
    public void The_wheel_zooms_about_the_pointer_and_nothing_else_moves()
    {
        var (window, owner) = Host();
        _ = PictureViewer.ShowAsync(owner, Picture(4000, 2000), fillWindow: true);
        Settle();
        var dialog = Viewer(window);
        var zoom = Zoom(dialog);
        var dialogBox = dialog.Bounds;
        var room = zoom.Bounds.Size;
        var fit = zoom.Zoom;
        var at = new Point(room.Width * 0.4, room.Height / 2);
        var before = (at - zoom.Origin()) / zoom.Zoom;
        window.MouseMove(At(zoom, window, at.X, at.Y));
        window.MouseWheel(At(zoom, window, at.X, at.Y), new Vector(0, 1));
        window.MouseWheel(At(zoom, window, at.X, at.Y), new Vector(0, 1));
        Settle(60);
        Assert.Equal(fit * 1.25 * 1.25, zoom.Zoom, 4);
        var after = (at - zoom.Origin()) / zoom.Zoom;
        Assert.Equal(before.X, after.X, 1);
        Assert.Equal(before.Y, after.Y, 1);
        Assert.Equal(dialogBox, dialog.Bounds);
        Assert.Equal(room, zoom.Bounds.Size);
    }

    [AvaloniaFact]
    public void Keys_zoom_fit_and_move_the_picture_while_open_and_rest_after_it_closes()
    {
        var (window, owner) = Host();
        var shown = PictureViewer.ShowAsync(owner, Picture(4000, 2000), fillWindow: true);
        Settle();
        var dialog = Viewer(window);
        var zoom = Zoom(dialog);
        var fit = zoom.Zoom;
        Assert.True(zoom.IsFocused);
        Press(window, Key.OemPlus);
        Assert.Equal(fit * 1.25, zoom.Zoom, 4);
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
    public void A_button_holding_a_picture_opens_it_and_one_without_lets_the_press_through()
    {
        var inner = new Button { Content = "س", Width = 40, Height = 40 };
        var card = new Button { Content = inner, Padding = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var cardClicks = 0;
        card.Click += (_, _) => cardClicks++;
        PictureViewer.SetTitle(inner, "سارة");
        PictureViewer.SetFillWindow(inner, true);
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
        Assert.Contains("picture", dialog.Classes);
        Assert.Equal(1, cardClicks);
        dialog.Close();
    }

    [AvaloniaFact]
    public void An_arabic_page_names_the_viewer_in_arabic_and_keeps_the_picture_left_to_right()
    {
        var (window, owner) = Host(dir: FlowDirection.RightToLeft);
        _ = PictureViewer.ShowAsync(owner, Picture(2000, 1000), fillWindow: true);
        Settle();
        var dialog = Viewer(window);
        Assert.Equal("عرض الصورة", AutomationProperties.GetName(dialog));
        Assert.Equal("عرض الصورة", AutomationProperties.GetName(Zoom(dialog)));
        Assert.Equal(FlowDirection.LeftToRight, Zoom(dialog).FlowDirection);
    }
}
