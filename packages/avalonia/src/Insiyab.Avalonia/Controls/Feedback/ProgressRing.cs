using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Insiyab.Avalonia.Controls;

/// <summary>A percentage as a ring, for a figure in a tile or beside a heading: the web's <c>.ins-ring</c>. It fills clockwise in both directions, as a dial does. Classes <c>sm</c> (24px, no label) and <c>lg</c>. The hole shows the percentage, or <see cref="Text"/> in its place (the tasks done of a plan, say) with an optional <see cref="Caption"/> under it.</summary>
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

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ProgressRing, string?>(nameof(Text));

    public static readonly StyledProperty<string?> CaptionProperty =
        AvaloniaProperty.Register<ProgressRing, string?>(nameof(Caption));

    // Where the fill starts, in degrees clockwise from twelve: a ProgressIcon turns its ring while work runs (TurningFill).
    internal static readonly StyledProperty<double> TurnProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Turn));

    private string _label = "0%";
    private TextBlock? _labelPart;
    private TextBlock? _captionPart;

    static ProgressRing()
    {
        AffectsRender<ProgressRing>(ValueProperty, MinimumProperty, MaximumProperty, ThicknessProperty, TrackProperty, FillProperty, TurnProperty);
        MaximumProperty.OverrideDefaultValue<ProgressRing>(100);
    }

    public ProgressRing() => SetCurrentValue(FlowDirectionProperty, global::Avalonia.Media.FlowDirection.LeftToRight);

    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    public IBrush? Track { get => GetValue(TrackProperty); set => SetValue(TrackProperty, value); }

    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>What the hole shows: <see cref="Text"/> when set, else the value as a whole percentage.</summary>
    public string Label { get => _label; private set => SetAndRaise(LabelProperty, ref _label, value); }

    /// <summary>A figure the hole shows in place of the percentage (the value still fills the ring and is what a screen reader hears); null or empty: the percentage.</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>A short line under the hole's figure, in a smaller muted face ("hours done"); null: none. It fades with the figure when the hole is too small for them.</summary>
    public string? Caption { get => GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>Where the fill starts, in degrees clockwise from twelve (0, twelve, unless a <see cref="ProgressIcon"/> turns it).</summary>
    internal double Turn { get => GetValue(TurnProperty); set => SetValue(TurnProperty, value); }

    private double Fraction => Maximum > Minimum ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1) : 0;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == MinimumProperty || change.Property == MaximumProperty || change.Property == TextProperty)
            Label = Text is { Length: > 0 } text ? text : Math.Round(Fraction * 100).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
        else if (change.Property == CaptionProperty)
            PseudoClasses.Set(":caption", !string.IsNullOrEmpty(change.GetNewValue<string?>()));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ProgressRingAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _labelPart = e.NameScope.Find<TextBlock>("PART_Label");
        _captionPart = e.NameScope.Find<TextBlock>("PART_Caption");
    }

    // A label wider than the ring's hole would sit on the stroke, so it fades out (a 22px ring has no room for "45%").
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (_labelPart is not null)
        {
            var widest = Math.Max(_labelPart.DesiredSize.Width, _captionPart is { IsVisible: true } caption ? caption.DesiredSize.Width : 0);
            PseudoClasses.Set(":crowded", widest > Math.Min(finalSize.Width, finalSize.Height) - 2 * Thickness - 4);
        }
        return size;
    }

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
        var from = (Turn - 90) * Math.PI / 180;
        var to = from + sweep;
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(center.X + r * Math.Cos(from), center.Y + r * Math.Sin(from)), false);
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
