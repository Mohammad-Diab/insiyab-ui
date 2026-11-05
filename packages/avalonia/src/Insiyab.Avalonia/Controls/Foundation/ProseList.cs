using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A list in long-form text, the web's <c>ul</c> and <c>ol</c> inside <c>.ins-prose</c>: each child is one item, with a disc, a ring for a nested list, its number, or the text of its own <see cref="MarkerProperty"/>, in <see cref="MarkerBrush"/>. As an ItemsControl's panel, the marker is read from each item's template root.</summary>
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

    /// <summary>On an item: its marker as written, such as «٣)» or «(iv)», in place of the disc or the number; the inset widens to the widest marker.</summary>
    public static readonly AttachedProperty<string?> MarkerProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Marker", typeof(ProseList));

    public static string? GetMarker(Control item) => item.GetValue(MarkerProperty);
    public static void SetMarker(Control item, string? value) => item.SetValue(MarkerProperty, value);

    private readonly List<Control> _markers = [];
    private readonly List<string?> _keys = [];
    private double _indent;

    static ProseList()
    {
        AffectsMeasure<ProseList>(IsOrderedProperty, IndentProperty, ItemSpacingProperty, FontSizeProperty);
        AffectsRender<ProseList>(MarkerBrushProperty);
        MarkerProperty.Changed.AddClassHandler<Control>((item, _) =>
        {
            var up = item.GetVisualParent();
            if ((up as ProseList ?? up?.GetVisualParent() as ProseList) is { } list) list.InvalidateMeasure();
        });
    }

    public bool IsOrdered { get => GetValue(IsOrderedProperty); set => SetValue(IsOrderedProperty, value); }

    public IBrush? MarkerBrush { get => GetValue(MarkerBrushProperty); set => SetValue(MarkerBrushProperty, value); }

    public double Indent { get => GetValue(IndentProperty); set => SetValue(IndentProperty, value); }

    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }

    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    /// <summary>How many lists this one sits in: 0 for a top list, which takes a disc; a nested one takes a ring.</summary>
    internal int Depth => this.GetVisualAncestors().OfType<ProseList>().Count();

    /// <summary>The markers drawn beside the items, in item order.</summary>
    internal IReadOnlyList<Control> Markers => _markers;

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
        _keys.Clear();
        InvalidateMeasure();
    }

    // The item's own marker text, on it or on the root of its template.
    private static string? Written(Control item)
    {
        var own = GetMarker(item);
        if (string.IsNullOrEmpty(own) && item is ContentPresenter { Child: { } root }) own = GetMarker(root);
        return string.IsNullOrEmpty(own) ? null : own;
    }

    // What the marker at a place says: the item's own text, its number, or null for a disc or a ring.
    private string? Key(Control item, int index) =>
        Written(item) ?? (IsOrdered ? (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "." : null);

    private Control Marker(string? key)
    {
        if (key is not null)
            return new TextBlock { Text = key, Foreground = MarkerBrush, IsHitTestVisible = false };
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

    private void SyncMarkers(List<Control> items)
    {
        while (_markers.Count > items.Count)
        {
            VisualChildren.Remove(_markers[^1]);
            _markers.RemoveAt(_markers.Count - 1);
            _keys.RemoveAt(_keys.Count - 1);
        }
        for (var i = 0; i < items.Count; i++)
        {
            var key = Key(items[i], i);
            if (i < _markers.Count)
            {
                if (_keys[i] == key) continue;
                VisualChildren.Remove(_markers[i]);
                _markers[i] = Marker(key);
                _keys[i] = key;
                VisualChildren.Add(_markers[i]);
                continue;
            }
            var m = Marker(key);
            _markers.Add(m);
            _keys.Add(key);
            VisualChildren.Add(m);
        }
    }

    private List<Control> Items() => Children.Where(c => c.IsVisible).ToList();

    protected override Size MeasureOverride(Size availableSize)
    {
        var items = Items();
        var indent = Indent * FontSize;
        var inner = new Size(Math.Max(0, availableSize.Width - indent), double.PositiveInfinity);
        foreach (var item in items)
            item.Measure(inner);
        // The items are measured first, so a bound item's template, and the marker on it, exists.
        SyncMarkers(items);
        var widest = 0.0;
        foreach (var m in _markers)
        {
            m.Measure(Size.Infinity);
            if (m is TextBlock) widest = Math.Max(widest, m.DesiredSize.Width);
        }
        var wide = Math.Max(indent, widest + FontSize * .25);
        if (wide > indent + 0.5)
        {
            indent = wide;
            inner = new Size(Math.Max(0, availableSize.Width - indent), double.PositiveInfinity);
            foreach (var item in items)
                item.Measure(inner);
        }
        _indent = indent;
        double height = 0, widestItem = 0;
        for (var i = 0; i < items.Count; i++)
        {
            height += (i > 0 ? ItemSpacing * FontSize : 0) + items[i].DesiredSize.Height;
            widestItem = Math.Max(widestItem, items[i].DesiredSize.Width);
        }
        return new Size(widestItem + indent, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var items = Items();
        var indent = _indent > 0 ? _indent : Indent * FontSize;
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
        _keys.Clear();
        base.ChildrenChanged(sender, e);
    }
}
