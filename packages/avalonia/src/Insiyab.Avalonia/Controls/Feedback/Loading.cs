using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Insiyab.Avalonia.Controls;

/// <summary>A highlight sweeping across a fill in the reading direction, the skeleton's and the indeterminate bar's <c>ins-skel</c> keyframes. <see cref="Phase"/> runs 0 to 1, animated by the theme every 1.8s: one of the two loops the library allows, as both mean waiting.</summary>
public class Shimmer : Control
{
    public static readonly StyledProperty<IBrush?> BaseProperty =
        AvaloniaProperty.Register<Shimmer, IBrush?>(nameof(Base));

    public static readonly StyledProperty<IBrush?> HighlightProperty =
        AvaloniaProperty.Register<Shimmer, IBrush?>(nameof(Highlight));

    public static readonly StyledProperty<double> InsetProperty =
        AvaloniaProperty.Register<Shimmer, double>(nameof(Inset));

    public static readonly StyledProperty<double> PhaseProperty =
        AvaloniaProperty.Register<Shimmer, double>(nameof(Phase));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<Shimmer>();

    public static readonly StyledProperty<double> FillWidthProperty =
        AvaloniaProperty.Register<Shimmer, double>(nameof(FillWidth), 1);

    static Shimmer() => AffectsRender<Shimmer>(BaseProperty, HighlightProperty, InsetProperty, PhaseProperty, CornerRadiusProperty, FillWidthProperty);

    public IBrush? Base { get => GetValue(BaseProperty); set => SetValue(BaseProperty, value); }

    public IBrush? Highlight { get => GetValue(HighlightProperty); set => SetValue(HighlightProperty, value); }

    /// <summary>Where the base stops start and end on the 240% gradient: 0 for the skeleton, .3 for the indeterminate bar.</summary>
    public double Inset { get => GetValue(InsetProperty); set => SetValue(InsetProperty, value); }

    public double Phase { get => GetValue(PhaseProperty); set => SetValue(PhaseProperty, value); }

    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The share of the width that is drawn, from the start edge: a title is .4, a paragraph's last line .6.</summary>
    public double FillWidth { get => GetValue(FillWidthProperty); set => SetValue(FillWidthProperty, value); }

    // On a 240% image the highlight travels from -20% to 120% of the box: the one sweep that starts and ends out of sight.
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;
        var fill = new Rect(0, 0, w * Math.Clamp(FillWidth, 0, 1), h);
        var shape = new RoundedRect(fill, CornerRadius);
        var baseColor = (Base as ISolidColorBrush)?.Color ?? Colors.Transparent;
        var hi = (Highlight as ISolidColorBrush)?.Color ?? Colors.Transparent;
        var baseAlpha = Base?.Opacity ?? 1;
        var hiAlpha = Highlight?.Opacity ?? 1;
        var b = Color.FromArgb((byte)(baseColor.A * baseAlpha), baseColor.R, baseColor.G, baseColor.B);
        var c = Color.FromArgb((byte)(hi.A * hiAlpha), hi.R, hi.G, hi.B);
        var image = w * 2.4;
        var start = -0.2 * w + 1.4 * w * Math.Clamp(Phase, 0, 1) - image / 2;
        // Immutable, as a brush made fresh each frame must be: a mutable one has to be registered with the compositor.
        var brush = new ImmutableLinearGradientBrush(
            [new ImmutableGradientStop(Inset, b), new ImmutableGradientStop(0.5, c), new ImmutableGradientStop(1 - Inset, b)],
            1, null, null, GradientSpreadMethod.Pad,
            new RelativePoint(start, 0, RelativeUnit.Absolute), new RelativePoint(start + image, 0, RelativeUnit.Absolute));
        context.DrawRectangle(brush, null, shape);
    }
}

/// <summary>The shape of content that has not arrived, the web's <c>.ins-skel</c>. Shape classes <c>text title circle block</c>; a circle's diameter is its <c>Width</c>.</summary>
[PseudoClasses(":last-line")]
public class Skeleton : Shimmer
{
    private IChildIndexProvider? _siblings;

    // The last of two or more lines is a paragraph's short last line (:last-line), watched here: an :nth-child rule in the theme held a removed skeleton through its parent.
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _siblings = ((ILogical)this).LogicalParent as IChildIndexProvider;
        if (_siblings is not null) _siblings.ChildIndexChanged += OnSiblings;
        Place();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        if (_siblings is not null) _siblings.ChildIndexChanged -= OnSiblings;
        _siblings = null;
    }

    private void OnSiblings(object? sender, ChildIndexChangedEventArgs e) => Place();

    private void Place()
    {
        var i = _siblings?.GetChildIndex(this) ?? -1;
        PseudoClasses.Set(":last-line", i > 0 && _siblings!.TryGetTotalCount(out var count) && i == count - 1);
    }
}

/// <summary>A ring with one quadrant in the brand colour, turning: the web's <c>.ins-spinner</c> (named apart from Avalonia's own <c>Spinner</c>). Class <c>sm</c>. Inside a button it takes the button's text colour. Clocks do not mirror, so neither does it.</summary>
public class LoadingSpinner : Control
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<LoadingSpinner, double>(nameof(Angle));

    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<LoadingSpinner, double>(nameof(Thickness), 3);

    public static readonly StyledProperty<double> ArcLengthProperty =
        AvaloniaProperty.Register<LoadingSpinner, double>(nameof(ArcLength), 90);

    public static readonly StyledProperty<IBrush?> ArcProperty =
        AvaloniaProperty.Register<LoadingSpinner, IBrush?>(nameof(Arc));

    public static readonly StyledProperty<IBrush?> TrackProperty =
        AvaloniaProperty.Register<LoadingSpinner, IBrush?>(nameof(Track));

    public static readonly StyledProperty<double> TrackOpacityProperty =
        AvaloniaProperty.Register<LoadingSpinner, double>(nameof(TrackOpacity), 0.28);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<LoadingSpinner>();

    static LoadingSpinner() => AffectsRender<LoadingSpinner>(AngleProperty, ThicknessProperty, ArcLengthProperty, ArcProperty, TrackProperty, TrackOpacityProperty, ForegroundProperty);

    public LoadingSpinner() => SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);

    /// <summary>The turn, in degrees; the theme spins it.</summary>
    public double Angle { get => GetValue(AngleProperty); set => SetValue(AngleProperty, value); }

    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    /// <summary>How much of the ring the arc covers, in degrees: a quadrant for the spinner, three for the save status.</summary>
    public double ArcLength { get => GetValue(ArcLengthProperty); set => SetValue(ArcLengthProperty, value); }

    /// <summary>The arc; null takes the inherited text colour.</summary>
    public IBrush? Arc { get => GetValue(ArcProperty); set => SetValue(ArcProperty, value); }

    /// <summary>The rest of the ring; null is the arc's colour at <see cref="TrackOpacity"/>.</summary>
    public IBrush? Track { get => GetValue(TrackProperty); set => SetValue(TrackProperty, value); }

    public double TrackOpacity { get => GetValue(TrackOpacityProperty); set => SetValue(TrackOpacityProperty, value); }

    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public override void Render(DrawingContext context)
    {
        var side = Math.Min(Bounds.Width, Bounds.Height);
        if (side <= 0) return;
        var arc = Arc ?? Foreground;
        if (arc is null) return;
        var t = Thickness;
        var r = (side - t) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var track = Track?.ToImmutable();
        if (track is null && arc is ISolidColorBrush solid)
            track = new ImmutableSolidColorBrush(solid.Color, solid.Opacity * TrackOpacity);
        if (track is not null)
            context.DrawEllipse(null, new ImmutablePen(track, t), center, r, r);
        var from = (Angle - 90 - ArcLength / 2) * Math.PI / 180;
        var to = from + ArcLength * Math.PI / 180;
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(center.X + r * Math.Cos(from), center.Y + r * Math.Sin(from)), false);
            g.ArcTo(new Point(center.X + r * Math.Cos(to), center.Y + r * Math.Sin(to)), new Size(r, r), 0, ArcLength > 180, SweepDirection.Clockwise);
            g.EndFigure(false);
        }
        context.DrawGeometry(null, new ImmutablePen(arc.ToImmutable(), t, lineCap: PenLineCap.Flat), geometry);
    }
}
