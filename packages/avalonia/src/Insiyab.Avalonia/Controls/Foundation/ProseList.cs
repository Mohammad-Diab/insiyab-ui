using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A list in long-form text, the web's <c>ul</c> and <c>ol</c> inside <c>.ins-prose</c>: each child is one item, with a disc, a ring for a nested list, or its number, in <see cref="MarkerBrush"/>.</summary>
public class ProseList : Panel
{
    /// <summary>True numbers the items (<c>ol</c>); false marks them with a disc (<c>ul</c>).</summary>
    public static readonly StyledProperty<bool> IsOrderedProperty =
        AvaloniaProperty.Register<ProseList, bool>(nameof(IsOrdered));

    public static readonly StyledProperty<IBrush?> MarkerBrushProperty =
        AvaloniaProperty.Register<ProseList, IBrush?>(nameof(MarkerBrush));

    /// <summary>The start inset that holds the markers, in em (1.5 on the web).</summary>
    public static readonly StyledProperty<double> IndentProperty =
        AvaloniaProperty.Register<ProseList, double>(nameof(Indent), 1.5);

    /// <summary>The space between items, in em (.375 on the web).</summary>
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<ProseList, double>(nameof(ItemSpacing), .375);

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<ProseList>();

    private readonly List<Control> _markers = [];

    static ProseList()
    {
        AffectsMeasure<ProseList>(IsOrderedProperty, IndentProperty, ItemSpacingProperty, FontSizeProperty);
        AffectsRender<ProseList>(MarkerBrushProperty);
    }

    public bool IsOrdered { get => GetValue(IsOrderedProperty); set => SetValue(IsOrderedProperty, value); }

    public IBrush? MarkerBrush { get => GetValue(MarkerBrushProperty); set => SetValue(MarkerBrushProperty, value); }

    public double Indent { get => GetValue(IndentProperty); set => SetValue(IndentProperty, value); }

    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }

    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    /// <summary>How many lists this one sits in: 0 for a top list, which takes a disc; a nested one takes a ring.</summary>
    public int Depth => this.GetVisualAncestors().OfType<ProseList>().Count();

    /// <summary>The markers drawn beside the items, in item order.</summary>
    public IReadOnlyList<Control> Markers => _markers;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOrderedProperty || change.Property == MarkerBrushProperty)
            ResetMarkers();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ResetMarkers();
    }

    private void ResetMarkers()
    {
        foreach (var m in _markers)
            VisualChildren.Remove(m);
        _markers.Clear();
        InvalidateMeasure();
    }

    private Control Marker(int index)
    {
        if (IsOrdered)
            return new TextBlock { Text = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".", Foreground = MarkerBrush, IsHitTestVisible = false };
        var ring = Depth > 0;
        return new Ellipse
        {
            Width = 5,
            Height = 5,
            Fill = ring ? null : MarkerBrush,
            Stroke = ring ? MarkerBrush : null,
            StrokeThickness = ring ? 1 : 0,
            IsHitTestVisible = false,
        };
    }

    private void SyncMarkers(int count)
    {
        while (_markers.Count > count)
        {
            VisualChildren.Remove(_markers[^1]);
            _markers.RemoveAt(_markers.Count - 1);
        }
        while (_markers.Count < count)
        {
            var m = Marker(_markers.Count);
            _markers.Add(m);
            VisualChildren.Add(m);
        }
    }

    private List<Control> Items() => Children.Where(c => c.IsVisible).ToList();

    protected override Size MeasureOverride(Size availableSize)
    {
        var items = Items();
        SyncMarkers(items.Count);
        var indent = Indent * FontSize;
        var inner = new Size(Math.Max(0, availableSize.Width - indent), double.PositiveInfinity);
        double height = 0, widest = 0;
        for (var i = 0; i < items.Count; i++)
        {
            items[i].Measure(inner);
            _markers[i].Measure(Size.Infinity);
            height += (i > 0 ? ItemSpacing * FontSize : 0) + items[i].DesiredSize.Height;
            widest = Math.Max(widest, items[i].DesiredSize.Width);
        }
        return new Size(widest + indent, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var items = Items();
        var indent = Indent * FontSize;
        double y = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
                y += ItemSpacing * FontSize;
            var item = items[i];
            item.Arrange(new Rect(indent, y, Math.Max(0, finalSize.Width - indent), item.DesiredSize.Height));
            if (i < _markers.Count)
                ArrangeMarker(_markers[i], item, y, indent);
            y += item.DesiredSize.Height;
        }
        return finalSize;
    }

    // The marker sits in the inset, its end a quarter em before the text, on the item's first line.
    private void ArrangeMarker(Control marker, Control item, double top, double indent)
    {
        var first = FirstLine(item);
        var lineHeight = first?.LineHeight is > 0 and var lh ? lh : FontSize * 1.7;
        var size = marker.DesiredSize;
        if (marker is TextBlock text)
        {
            text.LineHeight = lineHeight;
            text.Measure(Size.Infinity);
            size = text.DesiredSize;
            var x = indent - FontSize * .25 - size.Width;
            marker.Arrange(new Rect(x, top, size.Width, lineHeight));
        }
        else
        {
            var x = indent - FontSize * .65 - size.Width;
            marker.Arrange(new Rect(x, top + (lineHeight - size.Height) / 2 + FontSize * .05, size.Width, size.Height));
        }
    }

    private static TextBlock? FirstLine(Control item) =>
        item as TextBlock ?? item.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();

    protected override void ChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        foreach (var m in _markers)
            VisualChildren.Remove(m);
        _markers.Clear();
        base.ChildrenChanged(sender, e);
    }
}
