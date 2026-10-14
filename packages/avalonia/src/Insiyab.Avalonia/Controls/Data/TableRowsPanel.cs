using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The rows of a <see cref="Table"/>, one under another. Only the rows in or near view are in the tree, so a table of thousands of rows costs what a few screenfuls do; the rest keep their place and height.</summary>
public class TableRowsPanel : Panel, IChildIndexProvider
{
    // Rows past the view on each side, as a share of the view's height, that stay in the tree so a scroll finds them ready.
    private const double Ahead = 0.25;

    private List<TableRow> _rows = [];
    private readonly Dictionary<TableRow, int> _index = [];
    private readonly Dictionary<TableRow, double> _heights = [];
    private readonly HashSet<TableRow> _realized = [];
    private double[] _tops = [0];
    private bool _dirty = true;
    private double _estimate = 40;
    private Rect _viewport;
    private bool _hasViewport;
    private int _from;
    private int _to;
    private TableRow? _kept;
    private EventHandler<ChildIndexChangedEventArgs>? _indexChanged;

    public TableRowsPanel()
    {
        EffectiveViewportChanged += (_, e) =>
        {
            _viewport = e.EffectiveViewport;
            _hasViewport = true;
            var (from, to) = Range();
            if (from < _from || to > _to || from > _from + 1 || to < _to - 1) InvalidateMeasure();
        };
    }

    /// <summary>Raised when a row comes into the tree.</summary>
    internal event Action<TableRow>? Realized;

    /// <summary>The rows in the order they are shown.</summary>
    internal IReadOnlyList<TableRow> Rows => _rows;

    /// <summary>The height of a row not laid out yet: the mean of those that were.</summary>
    internal double RowHeight => _estimate;

    internal void SetRows(IEnumerable<TableRow> rows)
    {
        _rows = rows.ToList();
        _index.Clear();
        for (var i = 0; i < _rows.Count; i++) _index[_rows[i]] = i;
        foreach (var gone in _heights.Keys.Where(r => !_index.ContainsKey(r)).ToList()) _heights.Remove(gone);
        if (_kept is not null && !_index.ContainsKey(_kept)) _kept = null;
        foreach (var row in _realized.Where(r => !_index.ContainsKey(r)).ToList()) Drop(row);
        _dirty = true;
        _indexChanged?.Invoke(this, ChildIndexChangedEventArgs.ChildIndexesReset);
        InvalidateMeasure();
    }

    /// <summary>Keeps this row in the tree wherever it is, as the row the keyboard is on.</summary>
    internal void Keep(TableRow? row)
    {
        if (_kept == row) return;
        _kept = row;
        if (row is not null && !_realized.Contains(row)) InvalidateMeasure();
    }

    /// <summary>Where the row starts, from the panel's top.</summary>
    internal double OffsetOf(TableRow row)
    {
        if (_dirty) Offsets();
        return _index.TryGetValue(row, out var i) ? _tops[i] : 0;
    }

    internal double HeightOf(TableRow row) => _heights.TryGetValue(row, out var h) ? h : _estimate;

    int IChildIndexProvider.GetChildIndex(ILogical child) => child is TableRow row && _index.TryGetValue(row, out var i) ? i : -1;

    bool IChildIndexProvider.TryGetTotalCount(out int count)
    {
        count = _rows.Count;
        return true;
    }

    event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
    {
        add => _indexChanged += value;
        remove => _indexChanged -= value;
    }

    private void Offsets()
    {
        _dirty = false;
        if (_tops.Length != _rows.Count + 1) _tops = new double[_rows.Count + 1];
        var y = 0.0;
        for (var i = 0; i < _rows.Count; i++)
        {
            _tops[i] = y;
            y += _heights.TryGetValue(_rows[i], out var h) ? h : _estimate;
        }
        _tops[_rows.Count] = y;
    }

    // The rows to have in the tree: those over the view and a margin either side; before the view is known, a window's height from the top.
    private (int From, int To) Range()
    {
        if (_rows.Count == 0) return (0, 0);
        double top, bottom;
        if (_hasViewport && _viewport.Height > 0)
        {
            var margin = _viewport.Height * Ahead;
            top = _viewport.Top - margin;
            bottom = _viewport.Bottom + margin;
        }
        else
        {
            top = 0;
            var cap = this.FindAncestorOfType<Table>()?.MaxHeight ?? double.PositiveInfinity;
            bottom = Math.Min(TopLevel.GetTopLevel(this)?.ClientSize.Height ?? 1000, cap) * (1 + Ahead);
        }
        var from = Find(top);
        var to = Math.Min(_rows.Count, Find(bottom) + 1);
        return (from, Math.Max(from, to));
    }

    // The index of the row at this height.
    private int Find(double y)
    {
        int lo = 0, hi = _rows.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (_tops[mid] <= y) lo = mid;
            else hi = mid - 1;
        }
        return Math.Clamp(lo, 0, Math.Max(0, _rows.Count - 1));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_rows.Count == 0)
        {
            foreach (var row in _realized.ToList()) Drop(row);
            return default;
        }
        var width = 0.0;
        var fresh = new List<TableRow>();
        for (var pass = 0; pass < 2; pass++)
        {
            Offsets();
            (_from, _to) = Range();
            var measured = false;
            for (var i = _from; i < _to; i++) measured |= Bring(_rows[i], availableSize, fresh, ref width);
            if (_kept is not null) measured |= Bring(_kept, availableSize, fresh, ref width);
            if (!measured) break;
            _estimate = _heights.Count > 0 ? _heights.Values.Average() : _estimate;
        }
        Offsets();
        foreach (var row in _realized.ToList())
        {
            var i = _index[row];
            if (i >= _from && i < _to || row == _kept || Busy(row)) width = Math.Max(width, row.DesiredSize.Width);
            else Drop(row);
        }
        foreach (var row in fresh) Realized?.Invoke(row);
        return new Size(width, _tops[_rows.Count]);
    }

    // Puts the row in the tree and lays it out; true when its height was new.
    private bool Bring(TableRow row, Size available, List<TableRow> fresh, ref double width)
    {
        if (_realized.Add(row))
        {
            Children.Add(row);
            fresh.Add(row);
        }
        row.Measure(new Size(available.Width, double.PositiveInfinity));
        width = Math.Max(width, row.DesiredSize.Width);
        var h = row.DesiredSize.Height;
        if (_heights.TryGetValue(row, out var old) && Math.Abs(old - h) < 0.01) return false;
        _heights[row] = h;
        _dirty = true;
        return true;
    }

    // A row with a field open in it, or the keyboard inside, stays put until that ends.
    private static bool Busy(TableRow row) => row.IsKeyboardFocusWithin || row.Cells.Any(c => c.IsEditing);

    private void Drop(TableRow row)
    {
        _realized.Remove(row);
        Children.Remove(row);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var row in _realized)
        {
            var i = _index[row];
            row.Arrange(new Rect(0, _tops[i], finalSize.Width, HeightOf(row)));
        }
        return finalSize;
    }
}
