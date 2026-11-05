using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Long-form text, the web's <c>.ins-prose</c>: stacks its children with the web's rhythm (1em between blocks, 1.75em above a heading, .5em under it, 2em round a <see cref="Separator"/>) and caps the line at <see cref="LineLength"/> characters. Its TextBlocks and SelectableTextBlocks take the reading look, each paragraph in its own direction's face and line; for data-bound text put <c>Classes="prose"</c> on the ItemsControl, which then stacks its items in a Prose.</summary>
public class Prose : Panel
{
    /// <summary>The longest line, in widths of the digit zero (CSS <c>ch</c>) of the inherited font; 0 for no cap.</summary>
    public static readonly StyledProperty<double> LineLengthProperty =
        AvaloniaProperty.Register<Prose, double>(nameof(LineLength), 70);

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<Prose>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<Prose>();

    // What a Prose gave a paragraph of its own, to take back when it no longer applies.
    private static readonly ConditionalWeakTable<TextBlock, Given> s_given = new();

    static Prose()
    {
        AffectsMeasure<Prose>(LineLengthProperty, FontSizeProperty, FontFamilyProperty);
        Visual.FlowDirectionProperty.Changed.AddClassHandler<TextBlock>((block, _) =>
        {
            var up = block.GetVisualParent();
            if ((up as Prose ?? up?.GetVisualParent() as Prose) is { } prose) prose.InvalidateMeasure();
        });
    }

    private sealed class Given
    {
        public bool Line, Wrap, Face;
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

    // An item of a bound list is its container's child.
    private static Control Block(Control c) => c is ContentPresenter { Child: { } inner } ? inner : c;

    internal static bool IsHeading(Control c) =>
        c.Classes.Contains("h1") || c.Classes.Contains("h2") || c.Classes.Contains("h3") || c.Classes.Contains("h4") || c.Classes.Contains("display");

    private static double Em(Control c) => c.GetValue(TextElement.FontSizeProperty);

    // The space above a block, after the one before it: a separator keeps 2em each side, as margins collapse on the web.
    internal static double Gap(Control? before, Control child)
    {
        if (before is null)
            return 0;
        before = Block(before);
        child = Block(child);
        var gap = IsHeading(child) ? 1.75 * Em(child) : IsHeading(before) ? .5 * Em(child) : Em(child);
        if (child is Separator)
            gap = Math.Max(gap, 2 * Em(child));
        if (before is Separator)
            gap = Math.Max(gap, 2 * Em(before));
        return gap;
    }

    // A paragraph the selectors do not reach takes its look here, so no rule is tried on every text block: a bound item (its parent is the item's presenter, not the Prose) and one turned against the page, which reads in its own direction's face and line. Headings keep their own look.
    private void Read(Control child)
    {
        if (Block(child) is not TextBlock block || IsHeading(block)) return;
        var bound = child is ContentPresenter;
        var turned = block.FlowDirection != FlowDirection;
        if (!s_given.TryGetValue(block, out var given))
        {
            if (!bound && !turned) return;
            given = new Given();
            s_given.Add(block, given);
        }
        var ltr = block.FlowDirection == FlowDirection.LeftToRight;
        given.Line = Give(block, TextBlock.LineHeightProperty, given.Line, bound || turned, ltr ? 26.4 : 28.8);
        given.Wrap = Give(block, TextBlock.TextWrappingProperty, given.Wrap, bound, TextWrapping.Wrap);
        var face = turned && this.TryFindResource(ltr ? "InsFontBodyEn" : "InsFontArabic", out var found) ? found as FontFamily : null;
        given.Face = Give(block, TextBlock.FontFamilyProperty, given.Face, face is not null, face!);
    }

    // Sets the value, or takes back the one given before; returns whether the paragraph holds a value of ours.
    private static bool Give<T>(TextBlock block, StyledProperty<T> property, bool ours, bool want, T value)
    {
        if (!want)
        {
            if (ours) block.ClearValue(property);
            return false;
        }
        if (!Equals(block.GetValue(property), value)) block.SetValue(property, value);
        return true;
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
            if (child is ContentPresenter { Child: null } presenter) presenter.UpdateChild();
            Read(child);
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
