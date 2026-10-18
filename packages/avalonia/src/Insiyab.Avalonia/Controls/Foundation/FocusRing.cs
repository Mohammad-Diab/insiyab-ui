using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Insiyab.Avalonia.Controls;

/// <summary>The keyboard focus outline, the web's <c>:focus-visible</c> ring: a 2px line 2px outside the control, its corners following the control's radius as a CSS outline does.</summary>
internal class FocusRing : Border
{
    /// <summary>The gap between the control's edge and the ring.</summary>
    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<FocusRing, double>(nameof(Offset), 2);

    private Visual? _target;

    static FocusRing()
    {
        AdornerLayer.AdornedElementProperty.Changed.AddClassHandler<FocusRing>((ring, _) => ring.Track());
    }

    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OffsetProperty || change.Property == BorderThicknessProperty)
            Sync();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Track();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_target is not null)
            _target.PropertyChanged -= OnTargetChanged;
        _target = null;
    }

    private void Track()
    {
        var target = AdornerLayer.GetAdornedElement(this);
        if (ReferenceEquals(target, _target))
            return;
        if (_target is not null)
            _target.PropertyChanged -= OnTargetChanged;
        _target = target;
        if (_target is not null)
            _target.PropertyChanged += OnTargetChanged;
        Sync();
    }

    private void OnTargetChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TemplatedControl.CornerRadiusProperty || e.Property == CornerRadiusProperty)
            Sync();
    }

    private void Sync()
    {
        var width = BorderThickness.Left;
        var grow = Offset + width;
        Margin = new Thickness(-grow);
        var r = _target switch
        {
            TemplatedControl t => t.CornerRadius,
            Border b => b.CornerRadius,
            _ => default,
        };
        static double Out(double corner, double by) => corner > 0 ? corner + by : 0;
        CornerRadius = new CornerRadius(Out(r.TopLeft, grow), Out(r.TopRight, grow), Out(r.BottomRight, grow), Out(r.BottomLeft, grow));
    }
}
