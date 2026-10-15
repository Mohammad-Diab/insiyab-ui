using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The rows of a <see cref="Table"/>, one under another. Only the rows in or near view are in the tree, so a table of thousands of rows costs what a few screenfuls do; the rest keep their place and height. Rows just scrolled past wait hidden until the scroll rests, so scrolling back finds them styled; the rows sit in small blocks, so a row coming or going costs a frame only its block's rows.</summary>
public class TableRowsPanel : Panel, IChildIndexProvider
{
    // Rows past the view on each side, as a share of the view's height, that stay in the tree so a scroll finds them ready.
    private const double Ahead = 0.25;

    // Hidden rows kept for a scroll back: at least this many, or two windows' worth, and let go once the viewport has rested this long.
    private const int PoolFloor = 32;
    private const int RestMs = 500;
    private const int TrimBatch = 8;

    // A row added to or taken from a visual's children makes Avalonia relink all its siblings on the next frame, so the rows sit in blocks of this many and only one block is relinked.
    private const int BlockSize = 8;

    private List<TableRow> _rows = [];
    private readonly Dictionary<TableRow, int> _index = [];
    private readonly Dictionary<TableRow, double> _heights = [];
    private readonly HashSet<TableRow> _realized = [];
    private readonly LinkedList<TableRow> _pool = [];
    private readonly Dictionary<TableRow, LinkedListNode<TableRow>> _pooled = [];
    private DispatcherTimer? _rest;
    private int _trim;
    private double[] _tops = [0];
    private bool _dirty = true;
    private double _estimate = 40;
    private Rect _viewport;
    private bool _hasViewport;
    private int _from;
    private int _to;
    private TableRow? _kept;
    private readonly Dictionary<int, Block> _blocks = [];
    private readonly Dictionary<Control, Block> _home = [];
    private readonly Stack<Block> _spare = [];
    private bool _sizing;
    private EventHandler<ChildIndexChangedEventArgs>? _indexChanged;

    public TableRowsPanel()
    {
        EffectiveViewportChanged += (_, e) =>
        {
            _viewport = e.EffectiveViewport;
            _hasViewport = true;
            Rest();
            var (from, to) = Range();
            if (from < _from || to > _to || from > _from + 1 || to < _to - 1) InvalidateMeasure();
        };
    }

    /// <summary>Raised when a row comes into the tree, or back from waiting hidden.</summary>
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
        foreach (var row in _pool.Where(r => !_index.ContainsKey(r)).ToList()) DropPooled(row);
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

    /// <summary>Whether the row is in the tree and shown: not out of the table, and not waiting hidden for a scroll back.</summary>
    internal bool InView(TableRow row) => _realized.Contains(row) && row.IsAttachedToVisualTree();

    /// <summary>Whether the row is in the tree but hidden, kept for a scroll back.</summary>
    internal bool Waiting(TableRow row) => _pooled.ContainsKey(row);

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
            foreach (var row in _pool.ToList()) DropPooled(row);
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
        var pooled = false;
        foreach (var row in _realized.ToList())
        {
            var i = _index[row];
            if (i >= _from && i < _to || row == _kept || Busy(row)) width = Math.Max(width, row.DesiredSize.Width);
            else pooled |= Pool(row);
        }
        if (pooled) Rest();
        _sizing = true;
        foreach (var block in _blocks.Values) block.Measure(availableSize);
        _sizing = false;
        foreach (var row in fresh) Realized?.Invoke(row);
        return new Size(width, _tops[_rows.Count]);
    }

    // Puts the row in the tree and lays it out; true when its height was new.
    private bool Bring(TableRow row, Size available, List<TableRow> fresh, ref double width)
    {
        if (_realized.Add(row))
        {
            if (_pooled.Remove(row, out var node))
            {
                _pool.Remove(node);
                row.IsVisible = true;
            }
            else Children.Add(row);
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

    // A row that left the range stays in the tree, hidden, so it comes back without being styled again, and the oldest past the cap go the old way; true when it was kept. A row its owner hid goes the old way too.
    private bool Pool(TableRow row)
    {
        if (!row.IsVisible)
        {
            Drop(row);
            return false;
        }
        _realized.Remove(row);
        row.IsVisible = false;
        _pooled[row] = _pool.AddLast(row);
        var cap = Math.Max(PoolFloor, 2 * (_to - _from));
        while (_pool.Count > cap && _pool.First is { } oldest) DropPooled(oldest.Value);
        return true;
    }

    private void DropPooled(TableRow row)
    {
        if (!_pooled.Remove(row, out var node)) return;
        _pool.Remove(node);
        Children.Remove(row);
        row.IsVisible = true;
    }

    // Starts the wait for the viewport to rest; every move of it starts the wait again and stops a trim under way.
    private void Rest()
    {
        _trim++;
        if (_pool.Count == 0)
        {
            _rest?.Stop();
            return;
        }
        if (_rest is null)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(RestMs) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Trim(_trim);
            };
            _rest = timer;
        }
        _rest.Stop();
        _rest.Start();
    }

    // Lets the waiting rows go a few at a time, in the background, until the pool is empty or the viewport moves.
    private void Trim(int token)
    {
        if (token != _trim) return;
        for (var i = 0; i < TrimBatch && _pool.First is { } oldest; i++) DropPooled(oldest.Value);
        if (_pool.Count > 0) Dispatcher.UIThread.Post(() => Trim(token), DispatcherPriority.Background);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_pool.Count > 0) Rest();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _trim++;
        _rest?.Stop();
    }

    // The rows are the panel's logical children, so they keep their styles, but their visual parents are the blocks.
    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Move) LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
        else
        {
            if (e.OldItems?.OfType<Control>().ToList() is { } gone)
            {
                LogicalChildren.RemoveAll(gone);
                foreach (var child in gone) Unplace(child);
            }
            if (e.NewItems?.OfType<Control>().ToList() is { } added)
            {
                LogicalChildren.InsertRange(e.NewStartingIndex, added);
                foreach (var child in added) Place(child);
            }
        }
        InvalidateMeasure();
    }

    // A row goes to the block of its place in the table, or to a spare one; blocks are never taken out, so the panel's own children stay put while rows come and go.
    private void Place(Control child)
    {
        var key = child is TableRow row && _index.TryGetValue(row, out var i) ? i / BlockSize : -1;
        if (!_blocks.TryGetValue(key, out var block))
        {
            if (!_spare.TryPop(out block))
            {
                block = new Block(this);
                ((ISetInheritanceParent)block).SetParent(this);
                VisualChildren.Add(block);
            }
            block.Key = key;
            _blocks[key] = block;
        }
        block.Hold(child);
        _home[child] = block;
    }

    private void Unplace(Control child)
    {
        if (!_home.Remove(child, out var block)) return;
        block.Let(child);
        if (block.Count > 0) return;
        _blocks.Remove(block.Key);
        _spare.Push(block);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var block in _blocks.Values) block.Arrange(new Rect(finalSize));
        foreach (var row in _realized)
        {
            var i = _index[row];
            row.Arrange(new Rect(0, _tops[i], finalSize.Width, HeightOf(row)));
        }
        return finalSize;
    }

    // A few rows' visual parent, over the whole panel; the panel measures and places the rows itself, and hears from a block only when a row changed size outside its layout.
    private sealed class Block(TableRowsPanel owner) : Control
    {
        public int Key;

        public int Count => VisualChildren.Count;

        public void Hold(Control child) => VisualChildren.Add(child);

        public void Let(Control child) => VisualChildren.Remove(child);

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!owner._sizing) owner.InvalidateMeasure();
            return default;
        }

        protected override Size ArrangeOverride(Size finalSize) => finalSize;

        protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
    }
}
