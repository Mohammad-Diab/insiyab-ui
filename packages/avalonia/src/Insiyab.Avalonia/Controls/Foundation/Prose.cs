using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Insiyab.Avalonia.Controls;

/// <summary>Long-form text, the web's <c>.ins-prose</c>: stacks its children with the web's rhythm (1em between blocks, 1.75em above a heading, .5em under it, 2em round a <see cref="Separator"/>) and caps the line at <see cref="LineLength"/> characters.</summary>
public class Prose : Panel
{
    /// <summary>The longest line, in widths of the digit zero (CSS <c>ch</c>) of the inherited font; 0 for no cap.</summary>
    public static readonly StyledProperty<double> LineLengthProperty =
        AvaloniaProperty.Register<Prose, double>(nameof(LineLength), 70);

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<Prose>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<Prose>();

    static Prose()
    {
        AffectsMeasure<Prose>(LineLengthProperty, FontSizeProperty, FontFamilyProperty);
    }

    public double LineLength { get => GetValue(LineLengthProperty); set => SetValue(LineLengthProperty, value); }

    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    public FontFamily FontFamily { get => GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }

    /// <summary>The cap on the line in pixels, from the inherited font.</summary>
    internal double MaxLine
    {
        get
        {
            if (LineLength <= 0)
                return double.PositiveInfinity;
            var zero = new FormattedText("0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(FontFamily), FontSize, null);
            return zero.WidthIncludingTrailingWhitespace * LineLength;
        }
    }

    internal static bool IsHeading(Control c) =>
        c.Classes.Contains("h1") || c.Classes.Contains("h2") || c.Classes.Contains("h3") || c.Classes.Contains("h4") || c.Classes.Contains("display");

    private static double Em(Control c) => c.GetValue(TextElement.FontSizeProperty);

    // The space above a block, after the one before it: a separator keeps 2em each side, as margins collapse on the web.
    internal static double Gap(Control? before, Control child)
    {
        if (before is null)
            return 0;
        var gap = IsHeading(child) ? 1.75 * Em(child) : IsHeading(before) ? .5 * Em(child) : Em(child);
        if (child is Separator)
            gap = Math.Max(gap, 2 * Em(child));
        if (before is Separator)
            gap = Math.Max(gap, 2 * Em(before));
        return gap;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Math.Min(availableSize.Width, MaxLine);
        double height = 0, widest = 0;
        Control? before = null;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;
            child.Measure(new Size(width, double.PositiveInfinity));
            height += Gap(before, child) + child.DesiredSize.Height;
            widest = Math.Max(widest, child.DesiredSize.Width);
            before = child;
        }
        return new Size(double.IsInfinity(width) ? widest : width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Min(finalSize.Width, MaxLine);
        double y = 0;
        Control? before = null;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;
            y += Gap(before, child);
            child.Arrange(new Rect(0, y, width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
            before = child;
        }
        return finalSize;
    }
}
