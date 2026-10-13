using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>A stroked line icon drawn from a 24×24 geometry in the current text colour, at 1em by default: the web's <c>.ins-ico</c>.</summary>
public class Icon : Control
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<Icon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(Size), double.NaN);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(StrokeThickness), 2);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<Icon>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<Icon>();

    static Icon()
    {
        AffectsRender<Icon>(DataProperty, StrokeThicknessProperty, ForegroundProperty);
        AffectsMeasure<Icon>(SizeProperty, FontSizeProperty);
    }

    // Icons are physical, as on the web; set FlowDirection="RightToLeft" on one (an arrow) to mirror it in an RTL page.
    public Icon() => SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);

    public Geometry? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    /// <summary>The icon's width and height; unset, it follows the inherited font size.</summary>
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    public double StrokeThickness { get => GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    private double Side => double.IsNaN(Size) ? FontSize : Size;

    protected override Size MeasureOverride(Size availableSize) => new(Side, Side);

    public override void Render(DrawingContext context)
    {
        if (Data is null || Foreground is null) return;
        var scale = Side / 24;
        var pen = new Pen(Foreground, StrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        using (context.PushTransform(Matrix.CreateScale(scale, scale)))
            context.DrawGeometry(null, pen, Data);
    }
}
