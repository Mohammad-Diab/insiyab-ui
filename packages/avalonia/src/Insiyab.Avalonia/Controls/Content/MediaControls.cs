using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A player's control row of three groups, its children in order: the start group, the centre group and the end group. On one line the centre is truly centred while the sides leave room for it, else centred between them; narrower, the centre goes on its own line above the two sides; narrower still, each group takes a line. The row never turns with the page: like the time bar it runs left to right in every language, the start group on the left; only the cards and menus its buttons open read in the page's direction.</summary>
public class MediaControls : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MediaControls, double>(nameof(Spacing), 16);

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<MediaControls, double>(nameof(RowSpacing), 8);

    // The page's direction given back to a card or menu opened from the row, held while it is open.
    private static readonly ConditionalWeakTable<Popup, IDisposable> s_turned = new();

    static MediaControls()
    {
        AffectsMeasure<MediaControls>(SpacingProperty, RowSpacingProperty);
        FlowDirectionProperty.OverrideDefaultValue<MediaControls>(global::Avalonia.Media.FlowDirection.LeftToRight);
        Popup.IsOpenProperty.Changed.AddClassHandler<Popup>((popup, e) => Turn(popup, e.NewValue is true));
    }

    public MediaControls() => SetCurrentValue(FlowDirectionProperty, global::Avalonia.Media.FlowDirection.LeftToRight);

    /// <summary>The least room between the groups on one line.</summary>
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>The room between the lines when the groups wrap.</summary>
    public double RowSpacing { get => GetValue(RowSpacingProperty); set => SetValue(RowSpacingProperty, value); }

    private Control? Group(int i) => Children.Count > i ? Children[i] : null;

    /// <summary>The direction of the page around the row.</summary>
    internal FlowDirection PageDirection => this.GetVisualParent() is { } parent ? parent.FlowDirection : global::Avalonia.Media.FlowDirection.LeftToRight;

    // A card or a menu opened from a button in the row reads as the page does, though the row itself runs left to right.
    private static void Turn(Popup popup, bool open)
    {
        if (s_turned.TryGetValue(popup, out var held))
        {
            held.Dispose();
            s_turned.Remove(popup);
        }
        if (!open || popup.PlacementTarget is not Visual target || popup.Child is not Control body) return;
        var row = target as MediaControls ?? target.FindAncestorOfType<MediaControls>();
        if (row is null) return;
        s_turned.Add(popup, body.SetValue(FlowDirectionProperty, row.PageDirection, BindingPriority.Style)!);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(Size.Infinity);
        return ThreeGroups.Measure(availableSize.Width, Group(0), Group(1), Group(2), Spacing, RowSpacing);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ThreeGroups.Arrange(finalSize, Group(0), Group(1), Group(2), Spacing, RowSpacing);
        return finalSize;
    }
}

/// <summary>The three-group row's layout: one line, the centre above the sides, or three lines.</summary>
internal static class ThreeGroups
{
    internal enum Fit { OneLine, CentreAbove, ThreeLines }

    private static Size SizeOf(Control? c) => c is { IsVisible: true } ? c.DesiredSize : default;

    internal static Fit FitFor(double width, Size start, Size centre, Size end, double spacing) =>
        start.Width + centre.Width + end.Width + 2 * spacing <= width + 0.5 ? Fit.OneLine
        : start.Width + end.Width + spacing <= width + 0.5 ? Fit.CentreAbove
        : Fit.ThreeLines;

    internal static Size Measure(double available, Control? s, Control? c, Control? e, double spacing, double rowSpacing)
    {
        var (start, centre, end) = (SizeOf(s), SizeOf(c), SizeOf(e));
        var oneLine = start.Width + centre.Width + end.Width + 2 * spacing;
        var width = double.IsInfinity(available) ? oneLine : available;
        return FitFor(width, start, centre, end, spacing) switch
        {
            Fit.OneLine => new Size(Math.Min(oneLine, width), Math.Max(start.Height, Math.Max(centre.Height, end.Height))),
            Fit.CentreAbove => new Size(width, centre.Height + rowSpacing + Math.Max(start.Height, end.Height)),
            _ => new Size(width, centre.Height + start.Height + end.Height + 2 * rowSpacing),
        };
    }

    internal static void Arrange(Size size, Control? s, Control? c, Control? e, double spacing, double rowSpacing)
    {
        var (start, centre, end) = (SizeOf(s), SizeOf(c), SizeOf(e));
        var w = size.Width;
        switch (FitFor(w, start, centre, end, spacing))
        {
            case Fit.OneLine:
            {
                var h = size.Height;
                var x = (w - centre.Width) / 2;
                var left = start.Width + spacing;
                var right = w - end.Width - spacing - centre.Width;
                if (x < left || x > right) x = (left + right) / 2;
                s?.Arrange(new Rect(0, (h - start.Height) / 2, start.Width, start.Height));
                c?.Arrange(new Rect(x, (h - centre.Height) / 2, centre.Width, centre.Height));
                e?.Arrange(new Rect(w - end.Width, (h - end.Height) / 2, end.Width, end.Height));
                break;
            }
            case Fit.CentreAbove:
            {
                c?.Arrange(new Rect(Math.Max(0, (w - centre.Width) / 2), 0, centre.Width, centre.Height));
                var y = centre.Height + rowSpacing;
                var h = Math.Max(start.Height, end.Height);
                s?.Arrange(new Rect(0, y + (h - start.Height) / 2, start.Width, start.Height));
                e?.Arrange(new Rect(w - end.Width, y + (h - end.Height) / 2, end.Width, end.Height));
                break;
            }
            default:
            {
                c?.Arrange(new Rect(Math.Max(0, (w - centre.Width) / 2), 0, centre.Width, centre.Height));
                var y = centre.Height + rowSpacing;
                s?.Arrange(new Rect(0, y, start.Width, start.Height));
                y += start.Height + rowSpacing;
                e?.Arrange(new Rect(Math.Max(0, w - end.Width), y, end.Width, end.Height));
                break;
            }
        }
    }
}
