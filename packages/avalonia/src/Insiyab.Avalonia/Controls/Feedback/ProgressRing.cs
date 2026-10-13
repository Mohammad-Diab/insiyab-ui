using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Insiyab.Avalonia.Controls;

/// <summary>A percentage as a ring, for a figure in a tile or beside a heading: the web's <c>.ins-ring</c>. It fills clockwise in both directions, as a dial does. Class <c>lg</c>.</summary>
public class ProgressRing : RangeBase
{
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Thickness), 6);

    public static readonly StyledProperty<IBrush?> TrackProperty =
        AvaloniaProperty.Register<ProgressRing, IBrush?>(nameof(Track));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ProgressRing, IBrush?>(nameof(Fill));

    public static readonly DirectProperty<ProgressRing, string> LabelProperty =
        AvaloniaProperty.RegisterDirect<ProgressRing, string>(nameof(Label), o => o.Label);

    private string _label = "0%";

    static ProgressRing()
    {
        AffectsRender<ProgressRing>(ValueProperty, MinimumProperty, MaximumProperty, ThicknessProperty, TrackProperty, FillProperty);
        MaximumProperty.OverrideDefaultValue<ProgressRing>(100);
    }

    public ProgressRing() => SetCurrentValue(FlowDirectionProperty, global::Avalonia.Media.FlowDirection.LeftToRight);

    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    public IBrush? Track { get => GetValue(TrackProperty); set => SetValue(TrackProperty, value); }

    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The value as a whole percentage, the ring's label.</summary>
    public string Label { get => _label; private set => SetAndRaise(LabelProperty, ref _label, value); }

    private double Fraction => Maximum > Minimum ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1) : 0;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == MinimumProperty || change.Property == MaximumProperty)
            Label = Math.Round(Fraction * 100).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ProgressRingAutomationPeer(this);

    public override void Render(DrawingContext context)
    {
        var side = Math.Min(Bounds.Width, Bounds.Height);
        if (side <= 0) return;
        var t = Thickness;
        var r = (side - t) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var fraction = Fraction;
        if (Track is not null && fraction < 1)
            context.DrawEllipse(null, new ImmutablePen(Track.ToImmutable(), t), center, r, r);
        if (Fill is null || fraction <= 0) return;
        if (fraction >= 1)
        {
            context.DrawEllipse(null, new ImmutablePen(Fill.ToImmutable(), t), center, r, r);
            return;
        }
        var sweep = fraction * 2 * Math.PI;
        var from = -Math.PI / 2;
        var to = from + sweep;
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(center.X, center.Y - r), false);
            g.ArcTo(new Point(center.X + r * Math.Cos(to), center.Y + r * Math.Sin(to)), new Size(r, r), 0, sweep > Math.PI, SweepDirection.Clockwise);
            g.EndFigure(false);
        }
        context.DrawGeometry(null, new ImmutablePen(Fill.ToImmutable(), t, lineCap: PenLineCap.Flat), geometry);
    }
}

internal sealed class ProgressRingAutomationPeer(ProgressRing owner) : RangeBaseAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
}
