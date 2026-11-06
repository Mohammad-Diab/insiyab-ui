using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Insiyab.Avalonia.Controls;

using static Insiyab.Avalonia.Tests.Ui;

namespace Insiyab.Avalonia.Tests;

public class MediaSeekRowTests
{
    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_bar_keeps_its_place_whatever_digits_the_times_show(string dir)
    {
        var row = new MediaSeekRow { Width = 600, Duration = TimeSpan.FromMinutes(4) };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 700);
        var bar = Part<MediaBar>(row, "PART_Bar");
        Settle(100);
        var places = new List<Rect>();
        foreach (var at in new[] { 0.0, 0.0042, 0.4458, 0.8333, 1.0 })
        {
            row.Value = at;
            Settle(30);
            places.Add(new Rect(bar.TranslatePoint(default, window)!.Value, bar.Bounds.Size));
        }
        Assert.All(places, p => Assert.Equal(places[0], p));
        Assert.Equal("4:00", Part<TextBlock>(row, "PART_Now").Text);
        Assert.Equal("4:00", Part<TextBlock>(row, "PART_Length").Text);
        Assert.True(bar.TranslatePoint(default, window)!.Value.X < Part<TextBlock>(row, "PART_Length").TranslatePoint(default, window)!.Value.X);
    }

    [AvaloniaFact]
    public void A_drag_writes_its_time_beside_the_bar_and_the_value_binds_both_ways()
    {
        var row = new MediaSeekRow { Width = 600, Duration = TimeSpan.FromMinutes(10) };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, width: 700);
        var bar = Part<MediaBar>(row, "PART_Bar");
        Settle(100);
        bar.Value = 0.5;
        Assert.Equal(0.5, row.Value);
        Assert.Equal("5:00", Part<TextBlock>(row, "PART_Now").Text);
        row.Value = 0.1;
        Assert.Equal(0.1, bar.Value);
        Assert.Equal("1:00", Part<TextBlock>(row, "PART_Now").Text);
    }

    [AvaloniaTheory]
    [InlineData("rtl", "مباشر")]
    [InlineData("ltr", "Live")]
    public void A_live_stream_says_live_and_a_person_behind_gets_a_way_back(string dir, string word)
    {
        var row = new MediaSeekRow { Width = 600, Duration = TimeSpan.FromMinutes(30), Value = 1, IsLive = true };
        var window = Show(new StackPanel { Margin = new Thickness(20), Children = { row } }, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 700);
        var bar = Part<MediaBar>(row, "PART_Bar");
        Settle(100);
        var back = Part<Button>(row, "PART_BackToLive");
        Assert.False(Part<Panel>(row, "PART_LengthSlot").IsEffectivelyVisible);
        Assert.Equal(word, Part<TextBlock>(row, "PART_LiveText").Text);
        Assert.Equal(1, Part<Control>(row, "PART_AtLive").Opacity);
        Assert.Equal(0, back.Opacity);
        Assert.False(back.IsHitTestVisible);
        var barAt = new Rect(bar.TranslatePoint(default, window)!.Value, bar.Bounds.Size);

        row.Behind = TimeSpan.FromSeconds(2);
        Settle(30);
        Assert.False(row.IsBehind);
        Assert.Equal(0, back.Opacity);

        row.Behind = TimeSpan.FromSeconds(42);
        Settle(30);
        Assert.True(row.IsBehind);
        Assert.Equal(1, back.Opacity);
        Assert.Equal(0, Part<Control>(row, "PART_AtLive").Opacity);
        Assert.Equal("−0:42", Part<TextBlock>(row, "PART_Behind").Text);
        Assert.Equal(barAt, new Rect(bar.TranslatePoint(default, window)!.Value, bar.Bounds.Size));

        MediaLiveEventArgs? asked = null;
        row.LiveRequested += (_, e) => asked = e;
        row.LiveTarget = TimeSpan.FromSeconds(10);
        window.MouseDown(Center(back, window), MouseButton.Left);
        window.MouseUp(Center(back, window), MouseButton.Left);
        Settle(30);
        Assert.NotNull(asked);
        Assert.Equal(TimeSpan.FromSeconds(10), asked!.Behind);
    }

    [AvaloniaFact]
    public void The_play_glyph_slides_into_pause_and_lands_on_each_shape()
    {
        var at0 = PlayMorph.Shape(0).Bounds;
        var at1 = PlayMorph.Shape(1).Bounds;
        var mid = PlayMorph.Shape(0.5).Bounds;
        Assert.Equal(new Rect(6, 3, 14, 18), at0);
        Assert.Equal(new Rect(6, 4, 12, 16), at1);
        Assert.InRange(mid.Width, at1.Width, at0.Width);
        Assert.InRange(mid.Height, at1.Height, at0.Height);

        var bar = new MediaControlBar { Width = 700 };
        var window = Show(bar, width: 760);
        Settle(100);
        var icon = bar.GetVisualDescendants().OfType<Icon>().First(i => i.Name == "PART_PlayIcon");
        Assert.Equal(at0, icon.Data!.Bounds);
        Assert.True(icon.IsFilled);
        bar.IsPlaying = true;
        Settle(600);
        Assert.Equal(at1, icon.Data!.Bounds);
        bar.IsPlaying = false;
        Settle(600);
        Assert.Equal(at0, icon.Data!.Bounds);
    }

    [AvaloniaFact]
    public void The_speed_button_lights_up_under_the_pointer()
    {
        var bar = new MediaControlBar { Width = 700 };
        var window = Show(bar, width: 760);
        Settle(100);
        var speed = bar.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_Speed");
        window.MouseMove(Center(speed, window));
        Settle(300);
        AssertBrush(speed, "InsCtlBgHover", () => speed.Background);
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void The_arrow_of_a_split_sits_in_the_middle_of_its_half(string dir)
    {
        var bar = new MediaControlBar { Width = 700, SubtitlesCard = new TextBlock { Text = "ب" } };
        Show(bar, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 760);
        Settle(100);
        var split = bar.GetVisualDescendants().OfType<ToggleSplitButton>().First(s => s.Name == "PART_Subtitles");
        var half = split.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_SecondaryButton");
        var glyph = half.GetVisualDescendants().OfType<Viewbox>().First();
        var face = half.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Face");
        var c = glyph.TranslatePoint(new Point(glyph.Bounds.Width / 2, glyph.Bounds.Height / 2), face)!.Value;
        Assert.True(Math.Abs(c.X - face.Bounds.Width / 2) <= 1, $"arrow centre {c.X} in a face {face.Bounds.Width} wide; half {half.Bounds.Width}, padding {half.Padding}, glyph {glyph.Bounds}, margin {glyph.Margin}");
    }

    [AvaloniaTheory]
    [InlineData("rtl")]
    [InlineData("ltr")]
    public void A_picture_grows_out_of_the_button_that_opened_it(string dir)
    {
        var owner = new Button { Content = "صورة", Width = 80, Height = 60, Margin = new Thickness(40, 300, 40, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(new Panel { Children = { owner } }, dir == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, width: 1000, height: 800);
        _ = PictureViewer.ShowAsync(owner, new WriteableBitmap(new PixelSize(800, 600), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque));
        Settle();
        var dialog = window.GetVisualDescendants().OfType<Dialog>().Single(d => d.IsOpen);
        Assert.True(dialog.GrowsFromAnchor);
        var layer = window.GetVisualDescendants().OfType<DialogLayer>().Single(l => l.Dialog == dialog);
        var ops = layer.GrowFrom(dialog)!;
        var size = dialog.Bounds.Size;
        var centre = new Point(size.Width / 2, size.Height / 2);
        Point Map(Point p) => centre + (Vector)(ops.Value.Transform(p - centre));
        var a = owner.TranslatePoint(default, dialog)!.Value;
        var b = owner.TranslatePoint(new Point(owner.Bounds.Width, owner.Bounds.Height), dialog)!.Value;
        var want = new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
        var got = new Rect(Map(default), Map(new Point(size.Width, size.Height))).Normalize();
        Assert.Equal(want.X, got.X, 1);
        Assert.Equal(want.Y, got.Y, 1);
        Assert.Equal(want.Width, got.Width, 1);
        Assert.Equal(want.Height, got.Height, 1);
        Assert.Equal(TransformOperations.Identity.Value, (dialog.RenderTransform as TransformOperations)?.Value ?? Matrix.Identity);
        dialog.Close();
        Settle(100);
    }
}
