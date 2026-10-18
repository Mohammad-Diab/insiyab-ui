using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Insiyab.Avalonia.Controls;

/// <summary>The badge's picture for the taskbar: a full-bleed disc with the count in white, drawn at the exact size the shell shows.</summary>
internal static class BadgeIcon
{
    /// <summary>The small-icon size in device pixels for a window's scale: 16 at 100%, 20 at 125%, 24 at 150%, 32 at 200%.</summary>
    internal static int Pixels(double scaling) => (int)Math.Round(16 * scaling, MidpointRounding.AwayFromZero);

    internal static RenderTargetBitmap Draw(string text, int pixels, Color fill, FontFamily font)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(pixels, pixels), new Vector(96, 96));
        using var context = bitmap.CreateDrawingContext();
        var radius = pixels / 2.0;
        context.DrawEllipse(new SolidColorBrush(fill), null, new Point(radius, radius), radius, radius);
        if (Fit(text, pixels, font) is { } digits)
        {
            var ink = digits.Bounds;
            using (context.PushTransform(Matrix.CreateTranslation(radius - ink.Center.X, radius - ink.Center.Y)))
                context.DrawGeometry(Brushes.White, null, digits);
        }
        return bitmap;
    }

    internal static byte[] Png(string text, int pixels, Color fill, FontFamily font)
    {
        using var bitmap = Draw(text, pixels, fill, font);
        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    // The digits' outline at the largest size whose ink box sits inside the disc with a pixel to spare.
    private static Geometry? Fit(string text, int pixels, FontFamily font)
    {
        var radius = pixels / 2.0;
        var face = new Typeface(font, FontStyle.Normal, FontWeight.Bold);
        var size = pixels * (text.Length == 1 ? 0.70 : text.Length == 2 ? 0.60 : 0.50);
        for (; size >= 3; size -= 0.25)
        {
            var shape = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, size, Brushes.White).BuildGeometry(default);
            if (shape is null) return null;
            var half = shape.Bounds.Height / 2;
            if (half < radius && shape.Bounds.Width <= 2 * Math.Sqrt(radius * radius - half * half) - 1) return shape;
        }
        return null;
    }
}
