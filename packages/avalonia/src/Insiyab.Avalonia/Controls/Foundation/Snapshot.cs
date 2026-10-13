using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Insiyab.Avalonia.Controls;

// A picture of a visual at the screen's pixel density. Render at a non-96 dpi misplaces every RenderTransform, and an Image takes a scaled bitmap's DIP size as pixels, so this draws at 96 dpi through a VisualBrush.
internal static class Snapshot
{
    public static RenderTargetBitmap Take(Visual visual, Size size, double scale)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(Math.Max(1, (int)Math.Ceiling(size.Width * scale)), Math.Max(1, (int)Math.Ceiling(size.Height * scale))));
        using (var context = bitmap.CreateDrawingContext())
        using (context.PushTransform(Matrix.CreateScale(scale, scale)))
            context.DrawRectangle(new VisualBrush(visual) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top }, null, new Rect(size));
        return bitmap;
    }
}
