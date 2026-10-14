using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>The column widths several <see cref="ColumnsPanel"/>s share, sized the way an HTML table with <c>inline-size: 100%</c> sizes its auto columns.</summary>
public sealed class ColumnLayout
{
    /// <summary>The narrowest a column can be dragged to.</summary>
    public const double MinWidth = 48;

    private readonly HashSet<ColumnsPanel> _panels = [];
    private readonly Dictionary<int, double> _fixed = [];
    private double[] _max = [];
    private bool _stale;
    private bool _pending;

    /// <summary>Columns that keep their natural width and take none of the spare room: the web's <c>inline-size: 1%</c> column.</summary>
    public Func<int, bool>? IsFit { get; set; }

    /// <summary>Whether a panel taken out of the tree still counts towards the widths, as a table's rows scrolled out of view do, until <see cref="Keep"/> drops it.</summary>
    public bool KeepsDetached { get; set; }

    /// <summary>Widths the user set by dragging, by column.</summary>
    public IReadOnlyDictionary<int, double> FixedWidths => _fixed;

    internal void Add(ColumnsPanel panel)
    {
        if (!_panels.Add(panel)) return;
        Grow(panel.Natural, panel);
    }

    internal void Remove(ColumnsPanel panel)
    {
        if (!_panels.Remove(panel)) return;
        _stale = true;
        Changed(panel);
    }

    internal void Detach(ColumnsPanel panel)
    {
        if (!KeepsDetached) Remove(panel);
    }

    /// <summary>Drops every panel but these from the widths: the rows a table no longer has.</summary>
    public void Keep(IEnumerable<ColumnsPanel?> panels)
    {
        var keep = panels.OfType<ColumnsPanel>().ToHashSet();
        if (_panels.RemoveWhere(p => !keep.Contains(p) && !p.IsAttachedToTree) == 0) return;
        _stale = true;
        Invalidate();
    }

    /// <summary>The widest cell of each column, over every panel.</summary>
    public double[] Natural()
    {
        if (_stale)
        {
            _stale = false;
            var count = 0;
            foreach (var p in _panels) count = Math.Max(count, p.Natural.Length);
            _max = new double[count];
            foreach (var p in _panels)
                for (var i = 0; i < p.Natural.Length; i++)
                    _max[i] = Math.Max(_max[i], p.Natural[i]);
        }
        var widths = (double[])_max.Clone();
        foreach (var (i, w) in _fixed) if (i < widths.Length) widths[i] = w;
        return widths;
    }

    /// <summary>Fixes a column's width, as dragging its heading's edge does.</summary>
    public void SetWidth(int column, double width)
    {
        _fixed[column] = Math.Max(MinWidth, Math.Round(width));
        Invalidate();
    }

    /// <summary>Fixes a column at the width of its widest cell, heading included, as double-clicking its heading's edge does.</summary>
    public void Fit(int column, double min = 0)
    {
        var width = 0.0;
        foreach (var p in _panels)
            if (column < p.Natural.Length) width = Math.Max(width, p.Natural[column]);
        if (width > 0) SetWidth(column, Math.Max(min, Math.Ceiling(width)));
    }

    /// <summary>Gives a column back its automatic width.</summary>
    public void ClearWidth(int column)
    {
        if (_fixed.Remove(column)) Invalidate();
    }

    /// <summary>Gives every column back its automatic width.</summary>
    public void ClearWidths()
    {
        if (_fixed.Count == 0) return;
        _fixed.Clear();
        Invalidate();
    }

    /// <summary>The widths for a row <paramref name="available"/> wide: every column its natural width, and the room left over shared in proportion to them.</summary>
    public double[] Resolve(double available)
    {
        var widths = Natural();
        var total = 0.0;
        var share = 0.0;
        for (var i = 0; i < widths.Length; i++)
        {
            total += widths[i];
            if (IsFit?.Invoke(i) != true && !_fixed.ContainsKey(i)) share += widths[i];
        }
        var spare = available - total;
        if (spare <= 0 || share <= 0) return widths;
        for (var i = 0; i < widths.Length; i++)
            if (IsFit?.Invoke(i) != true && !_fixed.ContainsKey(i)) widths[i] += spare * widths[i] / share;
        return widths;
    }

    /// <summary>Tells every panel the widths may have moved.</summary>
    public void Invalidate()
    {
        foreach (var p in _panels) p.InvalidateMeasure();
    }

    // A panel's cells changed width: only when that moves a column's widest cell do the other panels lay out again.
    internal void Measured(ColumnsPanel source, double[] old)
    {
        if (!_panels.Contains(source)) return;
        var now = source.Natural;
        var shrank = false;
        for (var i = 0; i < old.Length && !shrank; i++)
            shrank = i < _max.Length && old[i] >= _max[i] && (i >= now.Length || now[i] < old[i]);
        if (shrank)
        {
            _stale = true;
            Changed(source);
        }
        else Grow(now, source);
    }

    private void Grow(double[] natural, ColumnsPanel source)
    {
        if (_stale)
        {
            Changed(source);
            return;
        }
        var grew = false;
        if (natural.Length > _max.Length)
        {
            Array.Resize(ref _max, natural.Length);
            grew = true;
        }
        for (var i = 0; i < natural.Length; i++)
            if (natural[i] > _max[i])
            {
                _max[i] = natural[i];
                grew = true;
            }
        if (grew) Changed(source);
    }

    private void Changed(ColumnsPanel source)
    {
        foreach (var p in _panels)
            if (p != source && p.IsAttachedToTree) p.InvalidateArrange();
        if (_pending) return;
        _pending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _pending = false;
            foreach (var p in _panels)
                if (p.IsAttachedToTree) p.InvalidateMeasure();
        }, DispatcherPriority.Render);
    }
}

/// <summary>Lays its children out side by side, child <c>i</c> in column <c>i</c> of a shared <see cref="ColumnLayout"/>: one row of a table.</summary>
public class ColumnsPanel : Panel
{
    private ColumnLayout? _layout;
    private bool _attached;

    internal double[] Natural { get; private set; } = [];

    internal bool IsAttachedToTree => _attached;

    /// <summary>The widths this row shares with the table's other rows.</summary>
    public ColumnLayout? Layout
    {
        get => _layout;
        set
        {
            if (_layout == value) return;
            _layout?.Remove(this);
            _layout = value;
            if (_attached) _layout?.Add(this);
            InvalidateMeasure();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _layout?.Add(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _layout?.Detach(this);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var natural = new double[Children.Count];
        var height = 0.0;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            natural[i] = child.IsVisible ? child.DesiredSize.Width : 0;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        if (!natural.AsSpan().SequenceEqual(Natural))
        {
            var old = Natural;
            Natural = natural;
            _layout?.Measured(this, old);
        }
        var widths = _layout?.Natural() ?? natural;
        var total = 0.0;
        foreach (var w in widths) total += w;
        return new Size(total, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var widths = _layout?.Resolve(finalSize.Width) ?? Natural;
        var x = 0.0;
        for (var i = 0; i < Children.Count; i++)
        {
            var w = i < widths.Length ? widths[i] : 0;
            var left = Math.Round(x);
            var right = i == Children.Count - 1 ? Math.Max(left, finalSize.Width) : Math.Round(x + w);
            Children[i].Arrange(new Rect(left, 0, right - left, finalSize.Height));
            x += w;
        }
        return finalSize;
    }
}
