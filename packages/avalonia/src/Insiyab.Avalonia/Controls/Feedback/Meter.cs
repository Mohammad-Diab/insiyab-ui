using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Insiyab.Avalonia.Controls;

/// <summary>A whole in parts, as a phone's storage shows its disk (iOS's and Google One's storage bar, Android's battery use): one bar split into its parts, each as long as its share and in its tone, from the start edge, a hairline gap between them; under it a legend naming each part with its dot and its figure. The parts are <see cref="MeterPart"/>s (as children or made from <c>ItemsSource</c>). A part worth 0 or hidden takes no room in the bar; a hidden part leaves the legend too. <see cref="Maximum"/> above the parts' sum leaves the rest as the empty track at the end (0, the default: the parts fill the bar). Class <c>lg</c> draws a 12px bar (8 by default).</summary>
public class Meter : ItemsControl
{
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<Meter, double>(nameof(Maximum));

    public static readonly StyledProperty<double> BarHeightProperty =
        AvaloniaProperty.Register<Meter, double>(nameof(BarHeight), 8);

    public static readonly StyledProperty<IBrush?> TrackProperty =
        AvaloniaProperty.Register<Meter, IBrush?>(nameof(Track));

    private MeterBar? _bar;

    /// <summary>The whole the parts are shares of; 0 (or less than their sum): their sum, and the parts fill the bar.</summary>
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>The bar's thickness (8; class <c>lg</c> 12).</summary>
    public double BarHeight { get => GetValue(BarHeightProperty); set => SetValue(BarHeightProperty, value); }

    /// <summary>The brush of the bar's empty rest, past the parts, when <see cref="Maximum"/> is above their sum.</summary>
    public IBrush? Track { get => GetValue(TrackProperty); set => SetValue(TrackProperty, value); }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<MeterPart>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new MeterPart();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _bar = e.NameScope.Find<MeterBar>("PART_Bar");
        if (_bar is not null)
            _bar.Owner = this;
        Redraw();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MaximumProperty || change.Property == TrackProperty || change.Property == ItemCountProperty)
            Redraw();
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        Redraw();
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        Redraw();
    }

    /// <summary>A part's value, fill or visibility changed: the bar is drawn again.</summary>
    internal void Redraw() => _bar?.InvalidateVisual();

    /// <summary>The parts as the bar draws them, in order: each shown part worth more than 0, with its fill.</summary>
    internal IReadOnlyList<(double Value, IBrush? Fill)> Pieces()
    {
        var pieces = new List<(double, IBrush?)>();
        foreach (var container in GetRealizedContainers())
            if (container is MeterPart { IsVisible: true } part && part.Value > 0 && double.IsFinite(part.Value))
                pieces.Add((part.Value, part.Fill));
        return pieces;
    }
}

/// <summary>A part of a <see cref="Meter"/>: its <see cref="Value"/> (its share of the bar), its <see cref="Label"/> and its figure in the legend (<see cref="Text"/>, else the value as it is), with a dot in its fill. Tone classes paint its piece of the bar and its dot: <c>ok warn bad info brand mute</c> (none: the brand's mid tone).</summary>
[PseudoClasses(":empty")]
public class MeterPart : TemplatedControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MeterPart, double>(nameof(Value));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MeterPart, string?>(nameof(Label));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MeterPart, string?>(nameof(Text));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<MeterPart, IBrush?>(nameof(Fill));

    public static readonly DirectProperty<MeterPart, string> FigureProperty =
        AvaloniaProperty.RegisterDirect<MeterPart, string>(nameof(Figure), o => o.Figure);

    private string _figure = "0";

    public MeterPart() => PseudoClasses.Set(":empty", true);

    /// <summary>The part's share of the whole; 0 or less: no piece of the bar (its legend entry stays, its figure 0).</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The part's name in the legend.</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The figure the legend shows after the name; null or empty: the value.</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>The piece's and the dot's brush (set by the tone class).</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>What the legend shows after the name: <see cref="Text"/>, else the value.</summary>
    public string Figure { get => _figure; private set => SetAndRaise(FigureProperty, ref _figure, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == TextProperty)
        {
            Figure = Text is { Length: > 0 } text ? text : Value.ToString("0.##", CultureInfo.InvariantCulture);
            PseudoClasses.Set(":empty", !(Value > 0));
        }
        if (change.Property == ValueProperty || change.Property == FillProperty || change.Property == IsVisibleProperty)
            this.FindLogicalAncestorOfType<Meter>()?.Redraw();
    }
}

/// <summary>A <see cref="Meter"/>'s bar: its parts' pieces from the start edge (a right-to-left page mirrors it), 2px apart, inside one rounded track.</summary>
internal sealed class MeterBar : Control
{
    /// <summary>The space between two pieces, where the surface shows.</summary>
    internal const double Gap = 2;

    /// <summary>The shortest piece drawn for a part worth more than 0, so a small share still shows.</summary>
    internal const double MinPiece = 4;

    internal Meter? Owner { get; set; }

    protected override Size MeasureOverride(Size availableSize) => new(0, Owner?.BarHeight ?? 8);

    /// <summary>Each piece's start and width in a bar <paramref name="width"/> wide, from the start edge; the track's rest last (Fill null) when the maximum leaves one.</summary>
    internal static IReadOnlyList<(double X, double Width, IBrush? Fill)> Layout(IReadOnlyList<(double Value, IBrush? Fill)> pieces, double maximum, double width)
    {
        var sum = pieces.Sum(p => p.Value);
        var rest = maximum > sum ? maximum - sum : 0;
        var whole = sum + rest;
        var count = pieces.Count + (rest > 0 ? 1 : 0);
        var result = new List<(double, double, IBrush?)>();
        if (whole <= 0 || count == 0 || width <= 0)
            return result;
        var room = Math.Max(0, width - Gap * (count - 1));
        var widths = pieces.Select(p => room * p.Value / whole).Append(rest > 0 ? room * rest / whole : -1).Where(w => w >= 0).ToArray();
        // a small share still shows: the shortest pieces take MinPiece from the longest one
        var longest = Array.IndexOf(widths, widths.Max());
        for (var i = 0; i < widths.Length; i++)
            if (i != longest && widths[i] < MinPiece)
            {
                widths[longest] -= MinPiece - widths[i];
                widths[i] = MinPiece;
            }
        var x = 0d;
        for (var i = 0; i < widths.Length; i++)
        {
            result.Add((x, widths[i], i < pieces.Count ? pieces[i].Fill : null));
            x += widths[i] + Gap;
        }
        return result;
    }

    public override void Render(DrawingContext context)
    {
        if (Owner is null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;
        var h = Bounds.Height;
        var shape = new RoundedRect(new Rect(Bounds.Size), h / 2);
        var pieces = Layout(Owner.Pieces(), Owner.Maximum, Bounds.Width);
        if (pieces.Count == 0)
        {
            // nothing to show yet: the empty track
            if (Owner.Track is { } empty)
                context.DrawRectangle(empty.ToImmutable(), null, shape);
            return;
        }
        using (context.PushClip(shape))
            foreach (var (x, w, fill) in pieces)
                if ((fill ?? Owner.Track) is { } brush)
                    context.DrawRectangle(brush.ToImmutable(), null, new Rect(x, 0, w, h));
    }
}
