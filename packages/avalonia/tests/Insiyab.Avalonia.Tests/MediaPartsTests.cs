using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MediaPartsTests
{
    // Opaque BGRA pixels: the left half red, the right half blue.
    private static byte[] Halves(int width, int height)
    {
        var bytes = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                var red = x < width / 2;
                bytes[i] = red ? (byte)0 : (byte)255;
                bytes[i + 1] = 0;
                bytes[i + 2] = red ? (byte)255 : (byte)0;
                bytes[i + 3] = 255;
            }
        return bytes;
    }

    private static Color Pixel(Window window, Visual at, Point point)
    {
        var frame = window.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var p = at.TranslatePoint(point, window)!.Value;
        return PixelAt(buffer, (int)p.X, (int)p.Y);
    }

    [Theory]
    [InlineData(Stretch.Uniform, 0, 25, 200, 100)]
    [InlineData(Stretch.UniformToFill, -50, 0, 300, 150)]
    [InlineData(Stretch.Fill, 0, 0, 200, 150)]
    [InlineData(Stretch.None, 60, 55, 80, 40)]
    public void A_picture_is_fitted_filled_stretched_or_kept_centred(Stretch stretch, double x, double y, double w, double h) =>
        Assert.Equal(new Rect(x, y, w, h), MediaSurface.Place(new Size(80, 40), new Size(200, 150), stretch));

    [AvaloniaFact]
    public void A_frame_is_drawn_on_the_stage_and_its_bitmap_made_again_only_for_a_new_size()
    {
        var surface = new MediaSurface { Width = 200, Height = 150, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var window = Show(new Panel { Children = { surface } });
        surface.WriteFrame(Halves(80, 40), 80, 40);
        Settle(60);
        Assert.Equal(new PixelSize(80, 40), surface.FrameSize);
        Assert.Equal(Colors.Black, Pixel(window, surface, new Point(100, 10)));
        Assert.Equal(Colors.Red, Pixel(window, surface, new Point(40, 75)));
        Assert.Equal(Colors.Blue, Pixel(window, surface, new Point(160, 75)));
        var bitmap = surface.Frame;
        surface.WriteFrame(Halves(80, 40), 80, 40);
        Assert.Same(bitmap, surface.Frame);
        surface.WriteFrame(Halves(40, 40), 40, 40, 160);
        Assert.NotSame(bitmap, surface.Frame);
        surface.ClearFrame();
        Assert.Equal(default, surface.FrameSize);
        Assert.Throws<ArgumentException>(() => surface.WriteFrame(new byte[10], 80, 40));
    }

    [AvaloniaFact]
    public void Only_is_mirrored_flips_the_picture_never_the_page_direction()
    {
        var surface = new MediaSurface { Width = 200, Height = 100, Stretch = Stretch.Fill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var window = Show(new Panel { Children = { surface } }, FlowDirection.RightToLeft);
        surface.WriteFrame(Halves(20, 10), 20, 10);
        Settle(60);
        var left = new Point(20, 50);
        var screenLeft = surface.TranslatePoint(left, window)!.Value.X < surface.TranslatePoint(new Point(180, 50), window)!.Value.X;
        Assert.True(screenLeft);
        Assert.Equal(Colors.Red, Pixel(window, surface, left));
        surface.IsMirrored = true;
        Settle(60);
        Assert.Equal(Colors.Blue, Pixel(window, surface, left));
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void A_tile_puts_its_caption_at_the_foot_start_and_flips_only_its_picture(FlowDirection dir)
    {
        var picture = new Border { Background = Brushes.Gray };
        var tile = new MediaTile { Width = 208, Height = 170, Content = picture, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var window = Show(new Panel { Children = { tile } }, dir);
        var caption = Part<Border>(tile, "PART_Caption");
        Assert.False(caption.IsVisible);
        tile.Caption = "سارة";
        tile.IsMirrored = true;
        Settle(60);
        Assert.True(caption.IsVisible);
        var tileBox = ScreenBox(tile, window);
        var box = ScreenBox(caption, window);
        Assert.Equal(tileBox.Bottom - 7, box.Bottom, 1);
        if (dir == FlowDirection.LeftToRight) Assert.Equal(tileBox.Left + 7, box.Left, 1);
        else Assert.Equal(tileBox.Right - 7, box.Right, 1);
        var presenter = Part<ContentPresenter>(tile, "PART_ContentPresenter");
        Assert.Equal(-1, presenter.RenderTransform!.Value.M11, 3);
        Assert.Null(caption.RenderTransform);
        AssertBrush(tile, "InsMediaStage", Part<Border>(tile, "PART_Stage").Background);
        AssertBrush(tile, "InsMediaEdge", Part<Border>(tile, "PART_Stage").BorderBrush);
        AssertBrush(tile, "InsMediaNote", caption.Background);
    }

    private static Rect ScreenBox(Control c, Window window)
    {
        var a = c.TranslatePoint(default, window)!.Value;
        var b = c.TranslatePoint(new Point(c.Bounds.Width, c.Bounds.Height), window)!.Value;
        return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
    }

    private static Border Block(double width, double height) => new() { Width = width, Height = height };

    [AvaloniaTheory]
    [InlineData(600, 252, 1)]
    [InlineData(300, 122, 1)]
    [InlineData(200, 52, 2)]
    [InlineData(150, 27, 3)]
    public void The_control_row_centres_its_middle_and_wraps_in_two_steps(double width, double centreX, int lines)
    {
        var start = Block(100, 40);
        var centre = Block(96, 40);
        var end = Block(60, 40);
        var row = new MediaControls { Width = width, Children = { start, centre, end }, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
        Show(new Panel { Children = { row } }, width: 700);
        Assert.Equal(centreX, centre.Bounds.X, 1);
        Assert.Equal(0, centre.Bounds.Y, 1);
        Assert.Equal(lines == 1 ? 40 : lines == 2 ? 88 : 136, row.Bounds.Height, 1);
        Assert.Equal(0, start.Bounds.X, 1);
        Assert.Equal(lines == 1 ? 0 : 48, start.Bounds.Y, 1);
        Assert.Equal(width - 60, end.Bounds.X, 1);
        Assert.Equal(lines == 1 ? 0 : lines == 2 ? 48 : 96, end.Bounds.Y, 1);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void The_control_row_never_turns_with_the_page_but_its_cards_read_as_the_page_does(FlowDirection dir)
    {
        var volume = new Button { Width = 44, Height = 44 };
        var sound = new Button { Width = 44, Height = 44 };
        var back = Block(44, 44);
        var play = Block(52, 52);
        var forward = Block(44, 44);
        var full = Block(44, 44);
        var words = new TextBlock { Text = "مستوى الصوت" };
        volume.Flyout = new PopCard { Content = words };
        var start = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { volume, sound } };
        var centre = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Children = { back, play, forward } };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { full } };
        var row = new MediaControls { Width = 600, Children = { start, centre, end }, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Children = { row } }, dir, width: 700);
        double X(Control c) => ScreenBox(c, window).X;
        Assert.Equal(FlowDirection.LeftToRight, row.FlowDirection);
        Assert.True(X(volume) < X(sound), "the start group's own order never turns");
        Assert.True(X(back) < X(play) && X(play) < X(forward), "back, play, forward run left to right");
        Assert.True(X(start) < X(centre) && X(centre) < X(end), "the start group sits on the left in every language");
        Assert.Equal(ScreenBox(row, window).Center.X, ScreenBox(centre, window).Center.X, 1);
        volume.Flyout.ShowAt(volume);
        Settle(120);
        Assert.Equal(dir, words.FlowDirection);
        volume.Flyout.Hide();
        Settle(60);
    }

    [AvaloniaFact]
    public void The_control_bar_icons_share_one_size()
    {
        var anchor = Show(new Border());
        var keys = new[] { "InsMediaBackIcon", "InsMediaForwardIcon", "InsMediaSubtitlesIcon", "InsMediaFillIcon", "InsMediaMoreIcon" };
        foreach (var glyph in MediaControlBar.CodeGlyphs.Concat(keys.Select(k => (Geometry)Res(anchor, k))))
        {
            var ink = glyph.Bounds;
            Assert.InRange(Math.Max(ink.Width, ink.Height), 16, 21);
            Assert.InRange(ink.Center.X, 10.5, 13.5);
            Assert.InRange(ink.Center.Y, 10.5, 13.5);
        }
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_media_looks_keep_their_dark_ground_and_light_ink_in_both_themes(string theme)
    {
        var noteText = new TextBlock { Text = "سارة" };
        var captionText = new TextBlock { Text = "ترجمة" };
        var note = new Border { Classes = { "media-note" }, Child = new StackPanel { Children = { noteText } } };
        var caption = new Border { Classes = { "media-caption" }, Child = captionText };
        Show(new StackPanel { Children = { note, caption } }, theme: theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        AssertBrush(note, "InsMediaNote", note.Background);
        AssertBrush(note, "InsMediaNoteInk", noteText.Foreground);
        AssertBrush(caption, "InsMediaCaption", caption.Background);
        AssertBrush(caption, "InsMediaCaptionInk", captionText.Foreground);
        Assert.Equal(FontWeight.SemiBold, captionText.FontWeight);
        Assert.Equal(Color.Parse("#B3101318"), ColorOf((IBrush)Res(note, "InsMediaNote")));
        Assert.Equal(Colors.Black, ColorOf((IBrush)Res(note, "InsMediaStage")));
    }
}
