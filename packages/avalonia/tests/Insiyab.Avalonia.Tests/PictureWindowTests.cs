using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

// The picture-in-picture window: on top, never maximised, the picture and the overlay in place, the overlay on hover (faded, or at once when still), a kept place by its key, the default corner on the reading end, and the box an edge drag asks for made to keep the picture's shape.
public class PictureWindowTests
{
    private static readonly PixelRect Area = new(0, 0, 1920, 1040);

    [AvaloniaFact]
    public void It_stays_on_top_never_maximises_and_shows_the_picture()
    {
        var picture = new Border { Name = "Video" };
        var w = (PictureWindow)Track(new PictureWindow { Picture = picture, Overlay = new Button { Content = "x" } });
        w.Show();
        Settle(50);
        Assert.True(w.Topmost);
        Assert.False(w.CanMaximize);
        Assert.Same(w, TopLevel.GetTopLevel(picture));
        Assert.Equal(16d / 9, w.Width / w.Height, 2);
        Assert.False(w.IsOverlayShown);
    }

    [AvaloniaFact]
    public void The_overlay_shows_while_the_pointer_is_on_it_and_at_once_when_still()
    {
        var button = new Button { Content = "x" };
        var w = (PictureWindow)Track(new PictureWindow { Picture = new Border(), Overlay = button });
        w.Show();
        Motion.SetStill(w, true);
        Settle(50);
        Assert.Equal(0, ((Control)button.Parent!).Opacity);
        Assert.False(((Control)button.Parent!).IsHitTestVisible);

        w.MouseMove(new Point(40, 40));
        Dispatcher();
        Assert.True(w.IsOverlayShown);
        Assert.Equal(1, ((Control)button.Parent!).Opacity);              // still: no fade
        Assert.True(((Control)button.Parent!).IsHitTestVisible);

        w.ShowOverlay(false);
        Assert.False(w.IsOverlayShown);
        Assert.Equal(0, ((Control)button.Parent!).Opacity);
    }

    [AvaloniaFact]
    public void The_overlay_fades_when_motion_is_on()
    {
        var button = new Button { Content = "x" };
        var w = (PictureWindow)Track(new PictureWindow { Picture = new Border(), Overlay = button });
        w.Show();
        Motion.SetStill(w, false);
        Settle(50);
        w.ShowOverlay(true);
        Assert.NotNull(((Control)button.Parent!).Transitions);
        Settle(300);
        Assert.Equal(1, ((Control)button.Parent!).Opacity, 2);
    }

    [AvaloniaFact]
    public void A_note_shows_at_the_foot_for_its_time_over_the_picture_and_under_the_overlay_then_goes_at_once_when_still()
    {
        var button = new Button { Content = "x" };
        var w = (PictureWindow)Track(new PictureWindow { Picture = new Border(), Overlay = button, NoteLift = 40 });
        w.Show();
        Motion.SetStill(w, true);
        Settle(50);
        var line = new TextBlock { Text = "Rana: hello" };
        w.ShowNote(line, TimeSpan.FromMilliseconds(200));
        Dispatcher();
        var note = (ContentPresenter)line.Parent!;
        var layers = ((Panel)w.Content!).Children;
        Assert.True(layers.IndexOf(note) > 0 && layers.IndexOf(note) < layers.IndexOf((Control)button.Parent!));   // over the picture, under the buttons
        Assert.True(w.IsNoteShown);
        Assert.Same(line, w.Note);
        Assert.Equal(1, note.Opacity);                               // still: no fade
        Assert.Null(note.Transitions);
        Assert.False(note.IsHitTestVisible);                         // presses go through to the window (its drag)
        Assert.Equal(global::Avalonia.Layout.VerticalAlignment.Bottom, note.VerticalAlignment);
        Assert.Equal(PictureWindow.NoteGap, note.Margin.Bottom);

        w.ShowOverlay(true);                                         // it rises clear of the overlay's foot
        Assert.Equal(PictureWindow.NoteGap + 40, note.Margin.Bottom);
        w.ShowOverlay(false);
        Assert.Equal(PictureWindow.NoteGap, note.Margin.Bottom);

        Settle(350);                                                 // its time is up
        Assert.False(w.IsNoteShown);
        Assert.Equal(0, note.Opacity);
        Assert.Null(w.Note);
    }

    [AvaloniaFact]
    public void A_newer_note_takes_the_place_and_starts_the_time_again_and_notes_fade_when_motion_is_on()
    {
        var w = (PictureWindow)Track(new PictureWindow { Picture = new Border() });
        w.Show();
        Motion.SetStill(w, false);
        Settle(50);
        w.ShowNote("first", TimeSpan.FromMilliseconds(400));
        Settle(250);
        w.ShowNote("second", TimeSpan.FromMilliseconds(400));
        Settle(250);                                                 // the first one's time would be up now
        Assert.True(w.IsNoteShown);
        Assert.Equal("second", w.Note);
        var note = ((Panel)w.Content!).Children.OfType<ContentPresenter>().Single(p => Equals(p.Content, "second"));
        Assert.NotNull(note.Transitions);
        Assert.Equal(1, note.Opacity, 2);

        w.HideNote();
        Assert.False(w.IsNoteShown);
        Assert.Equal("second", w.Note);                              // kept while it fades
        Settle(500);
        Assert.Equal(0, note.Opacity, 2);
        Assert.Null(w.Note);

        w.ShowNote(null);                                            // nothing to show: nothing shows
        Assert.False(w.IsNoteShown);
    }

    [AvaloniaFact]
    public void Its_place_is_kept_by_its_key()
    {
        var kept = new Dictionary<string, PicturePlace>();
        PictureWindow.LoadPlace = key => kept.GetValueOrDefault(key);
        PictureWindow.SavePlace = (key, place) => kept[key] = place;
        try
        {
            var w = (PictureWindow)Track(new PictureWindow { PlaceKey = "player", Picture = new Border() });
            w.Show();
            Settle(50);
            w.Width = 480;
            w.Height = 270;
            w.Close();
            Assert.Equal(480, kept["player"].Width);
            Assert.Equal(270, kept["player"].Height);
        }
        finally
        {
            PictureWindow.LoadPlace = null;
            PictureWindow.SavePlace = null;
        }
    }

    [Fact]
    public void First_it_opens_at_the_bottom_corner_on_the_reading_end()
    {
        var (ltr, size) = PictureWindow.Placing(null, [Area], Area, 1, 16d / 9, 384, new Size(192, 108), rightToLeft: false);
        Assert.Equal(new Size(384, 216), size);
        Assert.Equal(new PixelPoint(1920 - 24 - 384, 1040 - 24 - 216), ltr);
        var (rtl, _) = PictureWindow.Placing(null, [Area], Area, 1, 16d / 9, 384, new Size(192, 108), rightToLeft: true);
        Assert.Equal(new PixelPoint(24, 1040 - 24 - 216), rtl);
        var (scaled, scaledSize) = PictureWindow.Placing(null, [Area], Area, 1.5, 16d / 9, 384, new Size(192, 108), rightToLeft: false);
        Assert.Equal(new Size(384, 216), scaledSize);
        Assert.Equal(new PixelPoint(1920 - 36 - 576, 1040 - 36 - 324), scaled);
    }

    [Fact]
    public void A_kept_place_on_a_screen_is_used_with_the_pictures_shape_and_one_off_every_screen_is_not()
    {
        var (at, size) = PictureWindow.Placing(new PicturePlace(300, 200, 500, 100), [Area], Area, 1, 4d / 3, 384, new Size(192, 108), false);
        Assert.Equal(new PixelPoint(300, 200), at);
        Assert.Equal(new Size(500, 375), size);                                  // the height follows the picture's shape now
        var second = new PixelRect(1920, 0, 1280, 1000);
        var (there, _) = PictureWindow.Placing(new PicturePlace(2000, 100, 400, 225), [Area, second], Area, 1, 16d / 9, 384, new Size(192, 108), false);
        Assert.Equal(new PixelPoint(2000, 100), there);                          // on the other screen
        var (gone, _) = PictureWindow.Placing(new PicturePlace(5000, 100, 400, 225), [Area], Area, 1, 16d / 9, 384, new Size(192, 108), false);
        Assert.Equal(new PixelPoint(1920 - 24 - 384, 1040 - 24 - 216), gone);  // that screen is gone: the default corner
        var (inside, _) = PictureWindow.Placing(new PicturePlace(1700, 900, 400, 225), [Area], Area, 1, 16d / 9, 384, new Size(192, 108), false);
        Assert.Equal(new PixelPoint(1920 - 400, 1040 - 225), inside);          // half out: pulled in
    }

    [Theory]
    [InlineData(PictureWindow.EdgeRight, 100, 100, 600, 300, 100, 100, 600, 360)]       // the width leads, the bottom moves
    [InlineData(PictureWindow.EdgeLeft, 100, 100, 600, 300, 100, 100, 600, 360)]
    [InlineData(PictureWindow.EdgeBottom, 100, 100, 400, 450, 100, 100, 750, 450)]      // the height leads, the right moves
    [InlineData(PictureWindow.EdgeTop, 100, 100, 400, 450, 100, 100, 750, 450)]
    [InlineData(PictureWindow.EdgeTopLeft, 50, 50, 650, 300, 50, -40, 650, 390)]        // a corner: the larger leads, its own edges move
    [InlineData(PictureWindow.EdgeBottomRight, 100, 100, 400, 450, 100, 100, 750, 450)]
    public void An_edge_drag_keeps_the_pictures_shape(int edge, int x, int y, int w, int h, int ex, int ey, int ew, int eh)
    {
        var kept = PictureWindow.Keep(edge, new PixelRect(x, y, w, h), new PixelSize(0, 0), 600d / 360, new PixelSize(192, 108));
        Assert.Equal(new PixelRect(ex, ey, ew, eh), kept);
    }

    [Fact]
    public void The_frame_is_left_out_and_the_least_size_holds()
    {
        // a 16-pixel frame on each axis: the inside, not the box, keeps 16:9
        var kept = PictureWindow.Keep(PictureWindow.EdgeRight, new PixelRect(0, 0, 656, 100), new PixelSize(16, 16), 16d / 9, new PixelSize(192, 108));
        Assert.Equal(new PixelRect(0, 0, 656, 16 + 360), kept);
        var small = PictureWindow.Keep(PictureWindow.EdgeLeft, new PixelRect(500, 0, 50, 50), new PixelSize(0, 0), 16d / 9, new PixelSize(192, 108));
        Assert.Equal(new PixelRect(550 - 192, 0, 192, 108), small);
    }

    private static void Dispatcher() => global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
}
