using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>A list row's background, which also draws the row's start mark and its focus ring inside its own corners, so neither costs an element nor gets cut by a scrolling list.</summary>
internal sealed class RowBack : Decorator
{
    public const double MarkInset = 10;
    public const double MarkWidth = 3;

    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<RowBack>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<RowBack>();

    public static readonly StyledProperty<IBrush?> MarkProperty =
        AvaloniaProperty.Register<RowBack, IBrush?>(nameof(Mark));

    public static readonly StyledProperty<IBrush?> RingProperty =
        AvaloniaProperty.Register<RowBack, IBrush?>(nameof(Ring));

    static RowBack() => AffectsRender<RowBack>(BackgroundProperty, CornerRadiusProperty, MarkProperty, RingProperty, ListLook.MovingProperty);

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The 3px bar at the row's start edge; it hides while the list's mark flies.</summary>
    public IBrush? Mark { get => GetValue(MarkProperty); set => SetValue(MarkProperty, value); }

    /// <summary>The 2px focus ring drawn just inside the row's edge.</summary>
    public IBrush? Ring { get => GetValue(RingProperty); set => SetValue(RingProperty, value); }

    /// <summary>Where the mark is drawn, in this element's own space.</summary>
    public Rect MarkRect => new(0, MarkInset, MarkWidth, Math.Max(0, Bounds.Height - 2 * MarkInset));

    public override void Render(DrawingContext context)
    {
        var box = new Rect(Bounds.Size);
        var r = CornerRadius;
        if (Background is { } back) context.DrawRectangle(back, null, new RoundedRect(box, r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft));
        if (Mark is { } mark && !ListLook.GetMoving(this) && MarkRect.Height > 0)
            using (context.PushClip(new RoundedRect(box, r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft)))
                context.DrawRectangle(mark, null, new RoundedRect(MarkRect, MarkWidth / 2));
        if (Ring is { } ring && box.Width > 4 && box.Height > 4)
            context.DrawRectangle(null, new Pen(ring, 2), new RoundedRect(box.Deflate(1),
                Math.Max(0, r.TopLeft - 1), Math.Max(0, r.TopRight - 1), Math.Max(0, r.BottomRight - 1), Math.Max(0, r.BottomLeft - 1)));
    }
}
