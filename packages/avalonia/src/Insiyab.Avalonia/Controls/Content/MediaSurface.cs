using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Insiyab.Avalonia.Controls;

/// <summary>A picture or a video's frames on the dark media stage, fitted (centred), filled or stretched; never mirrored by the page's direction, only by <see cref="IsMirrored"/>.</summary>
public class MediaSurface : Control
{
    /// <summary>A still picture, shown while no frame has been written.</summary>
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<MediaSurface, IImage?>(nameof(Source));

    /// <summary><c>Uniform</c> fits the picture inside, centred (the default); <c>UniformToFill</c> fills and cuts the overflow; <c>Fill</c> stretches; <c>None</c> keeps its own size, centred.</summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<MediaSurface, Stretch>(nameof(Stretch), Stretch.Uniform);

    /// <summary>Flips the picture left to right, as a camera showing its own person.</summary>
    public static readonly StyledProperty<bool> IsMirroredProperty =
        AvaloniaProperty.Register<MediaSurface, bool>(nameof(IsMirrored));

    /// <summary>The stage around the picture; the theme gives <c>InsMediaStage</c>.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<MediaSurface>();

    private WriteableBitmap? _frame;

    static MediaSurface()
    {
        AffectsRender<MediaSurface>(SourceProperty, StretchProperty, IsMirroredProperty, BackgroundProperty);
        ClipToBoundsProperty.OverrideDefaultValue<MediaSurface>(true);
        FlowDirectionProperty.OverrideDefaultValue<MediaSurface>(FlowDirection.LeftToRight);
    }

    public MediaSurface()
    {
        SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    public IImage? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    public Stretch Stretch { get => GetValue(StretchProperty); set => SetValue(StretchProperty, value); }

    public bool IsMirrored { get => GetValue(IsMirroredProperty); set => SetValue(IsMirroredProperty, value); }

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    internal WriteableBitmap? Frame => _frame;

    /// <summary>The size of the last frame written, in pixels; empty while there is none.</summary>
    public PixelSize FrameSize => _frame?.PixelSize ?? default;

    /// <summary>Draws a video frame: <paramref name="width"/> by <paramref name="height"/> pixels of opaque BGRA, <paramref name="stride"/> bytes a row (0 for <c>width * 4</c>). The frame's bitmap is made again only when the size changes; call it on the UI thread.</summary>
    public unsafe void WriteFrame(ReadOnlySpan<byte> bgra, int width, int height, int stride = 0)
    {
        if (width <= 0 || height <= 0) return;
        var row = width * 4;
        if (stride <= 0) stride = row;
        if (stride < row || bgra.Length < (long)stride * (height - 1) + row) throw new ArgumentException("The pixels are fewer than the size says.", nameof(bgra));
        if (_frame is null || _frame.PixelSize.Width != width || _frame.PixelSize.Height != height)
        {
            _frame?.Dispose();
            _frame = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        }
        using (var target = _frame.Lock())
            for (var y = 0; y < height; y++)
                bgra.Slice(y * stride, row).CopyTo(new Span<byte>((byte*)target.Address + (long)y * target.RowBytes, row));
        InvalidateVisual();
    }

    /// <summary>Drops the last frame and frees its bitmap; <see cref="Source"/> shows again.</summary>
    public void ClearFrame()
    {
        _frame?.Dispose();
        _frame = null;
        InvalidateVisual();
    }

    /// <summary>Where a picture of <paramref name="size"/> lands inside <paramref name="room"/> for a stretch.</summary>
    internal static Rect Place(Size size, Size room, Stretch stretch)
    {
        if (size.Width <= 0 || size.Height <= 0) return default;
        var bounds = new Rect(room);
        if (stretch == Stretch.Fill) return bounds;
        var scale = stretch switch
        {
            Stretch.Uniform => Math.Min(room.Width / size.Width, room.Height / size.Height),
            Stretch.UniformToFill => Math.Max(room.Width / size.Width, room.Height / size.Height),
            _ => 1,
        };
        var shown = size * scale;
        return new Rect((room.Width - shown.Width) / 2, (room.Height - shown.Height) / 2, shown.Width, shown.Height);
    }

    public override void Render(DrawingContext context)
    {
        var room = Bounds.Size;
        if (Background is { } stage) context.FillRectangle(stage, new Rect(room));
        IImage? picture = _frame ?? Source;
        if (picture is null || room.Width <= 0 || room.Height <= 0) return;
        var size = picture is Bitmap bitmap ? new Size(bitmap.PixelSize.Width, bitmap.PixelSize.Height) : picture.Size;
        var dest = Place(size, room, Stretch);
        if (dest.Width <= 0 || dest.Height <= 0) return;
        using (IsMirrored ? context.PushTransform(new Matrix(-1, 0, 0, 1, room.Width, 0)) : default(DrawingContext.PushedState?))
            context.DrawImage(picture, new Rect(size), dest);
    }
}
