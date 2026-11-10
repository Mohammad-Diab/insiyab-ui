using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Styling;

namespace Insiyab.Avalonia.Controls;

/// <summary>A stroked line icon drawn from a 24×24 geometry in the current text colour, at 1em by default: the web's <c>.ins-ico</c>. A busy button's leading icon (<see cref="Busy"/>) shows the small <see cref="LoadingSpinner"/> in its own box instead.</summary>
public class Icon : Control
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<Icon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(Size), double.NaN);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(StrokeThickness), 2);

    /// <summary>Fills the glyph's closed shapes with its colour as well as drawing its lines, as a solid play triangle.</summary>
    public static readonly StyledProperty<bool> IsFilledProperty =
        AvaloniaProperty.Register<Icon, bool>(nameof(IsFilled));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<Icon>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<Icon>();

    // How much of the glyph shows: 1, and 0 while the spinner turns in its place.
    internal static readonly StyledProperty<double> GlyphOpacityProperty =
        AvaloniaProperty.Register<Icon, double>("GlyphOpacity", 1);

    private LoadingSpinner? _spinner;
    private CancellationTokenSource? _fade;

    static Icon()
    {
        AffectsRender<Icon>(DataProperty, StrokeThicknessProperty, ForegroundProperty, IsFilledProperty, GlyphOpacityProperty);
        AffectsMeasure<Icon>(SizeProperty, FontSizeProperty);
    }

    // Icons are physical, as on the web; class dir makes one follow the page, and FlowDirection="RightToLeft" mirrors one everywhere.
    public Icon() => SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);

    public Geometry? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    /// <summary>The icon's width and height; unset, it follows the inherited font size.</summary>
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    public double StrokeThickness { get => GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    public bool IsFilled { get => GetValue(IsFilledProperty); set => SetValue(IsFilledProperty, value); }

    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    private double Side => double.IsNaN(Size) ? FontSize : Size;

    // Whether the spinner turns in the glyph's place.
    internal bool IsWaiting { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = new Size(Side, Side);
        _spinner?.Measure(side);
        return side;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _spinner?.Arrange(new Rect(finalSize));
        return finalSize;
    }

    /// <summary>Shows the small spinner in the icon's box, in its colour, as the glyph fades out; false fades the glyph back and takes the spinner away.</summary>
    internal void Wait(bool on)
    {
        if (IsWaiting == on) return;
        IsWaiting = on;
        if (on)
        {
            if (_spinner is null)
            {
                // The arc in the icon's own colour (an unset arc takes the inherited text colour), the ring the icon's box, the stroke of the small spinner.
                _spinner = new LoadingSpinner
                {
                    Classes = { "sm" },
                    Width = double.NaN,
                    Height = double.NaN,
                    HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
                    Arc = null,
                    Track = null,
                    IsHitTestVisible = false,
                };
                LogicalChildren.Add(_spinner);
                VisualChildren.Add(_spinner);
            }
            _spinner.IsVisible = true;
        }
        else if (_spinner is not null) _spinner.IsVisible = false;
        Fade(on ? 0 : 1);
        InvalidateMeasure();
    }

    // The glyph eases to its new opacity: out on the exit time, back on the enter time; at once in a still window or where no one sees it.
    private void Fade(double to)
    {
        _fade?.Cancel();
        _fade = null;
        var from = GlyphOpacity;
        SetValue(GlyphOpacityProperty, to);
        if (from == to || Motion.IsStill(this) || !Seen.Now(this)) return;
        _fade = new CancellationTokenSource();
        var animation = new Animation
        {
            Duration = to == 0 ? Motion.Exit(this) : Motion.Enter(this),
            Easing = Motion.Ease(this),
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(GlyphOpacityProperty, from) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(GlyphOpacityProperty, to) } },
            },
        };
        _ = animation.RunAsync(this, _fade.Token);
    }

    private double GlyphOpacity => GetValue(GlyphOpacityProperty);

    public override void Render(DrawingContext context)
    {
        if (Data is null || Foreground is null) return;
        var glyph = GlyphOpacity;
        if (glyph <= 0) return;
        if (glyph >= 1) Draw(context, Data, Foreground);
        else
            using (context.PushOpacity(glyph))
                Draw(context, Data, Foreground);
    }

    private void Draw(DrawingContext context, Geometry data, IBrush foreground)
    {
        var scale = Side / 24;
        var pen = new Pen(foreground, StrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        using (context.PushTransform(Matrix.CreateScale(scale, scale)))
            context.DrawGeometry(IsFilled ? foreground : null, pen, data);
    }
}
