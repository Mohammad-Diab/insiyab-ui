using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>The SelectableTextBlocks inside a host selecting as one text, in reading order (the visual tree's): each block between the two ends whole, the end blocks from their places, each drawing its part with its own selection brush.</summary>
internal sealed class TextSelection
{
    private readonly Control _host;
    private List<SelectableTextBlock> _blocks = [];
    private IPointer? _pointer;
    private SelectableTextBlock? _anchor;
    private int _anchorAt;
    private Reach? _span;
    private ScrollViewer? _scroller;
    private Point _scrollPoint;
    private double _scrollSpeed;
    private DispatcherTimer? _scrollTimer;

    /// <summary>The selection while it runs over more than one block, else null.</summary>
    internal sealed record Reach(SelectableTextBlock From, int FromAt, SelectableTextBlock To, int ToAt);

    internal Reach? Span => _span;

    public TextSelection(Control host)
    {
        _host = host;
        host.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        host.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        host.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        host.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Bubble, handledEventsToo: true);
        host.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        host.AddHandler(SelectableTextBlock.CopyingToClipboardEvent, OnCopying);
        host.AddHandler(InputElement.LostFocusEvent, OnLostFocus);
    }

    public void Detach()
    {
        _host.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
        _host.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
        _host.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
        _host.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
        _host.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        _host.RemoveHandler(SelectableTextBlock.CopyingToClipboardEvent, OnCopying);
        _host.RemoveHandler(InputElement.LostFocusEvent, OnLostFocus);
        StopScroll();
    }

    // The host's blocks that show, in reading order.
    internal List<SelectableTextBlock> Blocks() => [.. _host.GetVisualDescendants().OfType<SelectableTextBlock>().Where(b => b.IsEffectivelyVisible)];

    private static int Length(SelectableTextBlock block) => (block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text)?.Length ?? 0;

    private static void Select(SelectableTextBlock block, int start, int end)
    {
        block.SetCurrentValue(SelectableTextBlock.SelectionStartProperty, start);
        block.SetCurrentValue(SelectableTextBlock.SelectionEndProperty, end);
    }

    /// <summary>Selects from one place to another, over the blocks between; the same block selects inside itself.</summary>
    internal void Select(SelectableTextBlock from, int fromAt, SelectableTextBlock to, int toAt)
    {
        if (from == to)
        {
            _span = null;
            foreach (var other in Blocks()) if (other != from && other.SelectionStart != other.SelectionEnd) Select(other, 0, 0);
            Select(from, fromAt, toAt);
            return;
        }
        _span = new Reach(from, fromAt, to, toAt);
        Apply(_span);
    }

    // A left press (the block has placed its caret or picked a word already): any selection over blocks ends, and a single click may start a drag.
    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_host).Properties.IsLeftButtonPressed)
        {
            if (_span is { } span) Apply(span);
            return;
        }
        _span = null;
        _pointer = null;
        var block = (e.Source as Visual)?.FindAncestorOfType<SelectableTextBlock>(includeSelf: true);
        _blocks = Blocks();
        foreach (var other in _blocks)
            if (other != block && other.SelectionStart != other.SelectionEnd) Select(other, 0, 0);
        if (block is null || !_blocks.Contains(block) || e.ClickCount != 1 || e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;
        _pointer = e.Pointer;
        _anchor = block;
        _anchorAt = block.SelectionStart;
    }

    // After the captured block has moved its own end: the block under the pointer decides what is selected, and past the edge of the scroller the page scrolls.
    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer is null || e.Pointer != _pointer || _anchor is null || !e.GetCurrentPoint(_host).Properties.IsLeftButtonPressed) return;
        Extend(e.GetPosition(_host));
        Edge(e);
    }

    private void Extend(Point inHost)
    {
        if (_anchor is null) return;
        var (block, at) = Locate(inHost);
        if (block is null) return;
        Select(_anchor, _anchorAt, block, at);
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _pointer) return;
        _pointer = null;
        StopScroll();
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (e.Pointer != _pointer) return;
        _pointer = null;
        StopScroll();
    }

    // The block and the place in its text under a point: the last block whose top is above it (its end when below it), or the first block's start when above them all.
    internal (SelectableTextBlock? Block, int At) Locate(Point inHost)
    {
        if (_blocks.Count == 0) _blocks = Blocks();
        if (_blocks.Count == 0) return (null, 0);
        SelectableTextBlock? found = null;
        foreach (var block in _blocks)
        {
            if (_host.TranslatePoint(inHost, block) is not { } p || p.Y < 0) break;
            found = block;
        }
        if (found is null) return (_blocks[0], 0);
        var point = _host.TranslatePoint(inHost, found)!.Value - new Point(found.Padding.Left, found.Padding.Top);
        var layout = found.TextLayout;
        if (point.Y >= layout.Height) return (found, Length(found));
        var hit = layout.HitTestPoint(new Point(point.X, Math.Clamp(point.Y, 0, Math.Max(layout.Height - 1, 0))));
        return (found, hit.TextPosition);
    }

    // Each block between the two ends whole, the two end blocks from their places towards each other.
    private void Apply(Reach span)
    {
        var blocks = Blocks();
        var from = blocks.IndexOf(span.From);
        var to = blocks.IndexOf(span.To);
        if (from < 0 || to < 0)
        {
            _span = null;
            return;
        }
        var forward = to > from;
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var length = Length(block);
            if (i == from) Select(block, span.FromAt, forward ? length : 0);
            else if (i == to) Select(block, forward ? 0 : length, span.ToAt);
            else if (i > Math.Min(from, to) && i < Math.Max(from, to)) Select(block, 0, length);
            else if (block.SelectionStart != block.SelectionEnd) Select(block, 0, 0);
        }
    }

    // Past the top or bottom of the scroller the page moves toward the pointer, faster the further out it is, while the button stays down.
    private void Edge(PointerEventArgs e)
    {
        _scroller ??= _host.FindAncestorOfType<ScrollViewer>();
        if (_scroller is null) return;
        var p = e.GetPosition(_scroller);
        var height = _scroller.Bounds.Height;
        var over = p.Y < 0 ? p.Y : p.Y > height ? p.Y - height : 0;
        if (over == 0)
        {
            StopScroll();
            return;
        }
        _scrollPoint = p;
        _scrollSpeed = Math.Clamp(over / 2, -40, 40);
        if (Math.Abs(_scrollSpeed) < 2) _scrollSpeed = Math.Sign(over) * 2;
        _scrollTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(30), DispatcherPriority.Input, (_, _) => ScrollStep());
        _scrollTimer.Start();
    }

    /// <summary>One step of the scroll at the edge: the page moves and the selection follows the pointer, which stays where it was over the scroller.</summary>
    internal void ScrollStep()
    {
        if (_scroller is null || _pointer is null)
        {
            StopScroll();
            return;
        }
        var y = Math.Clamp(_scroller.Offset.Y + _scrollSpeed, 0, Math.Max(0, _scroller.Extent.Height - _scroller.Viewport.Height));
        _scroller.Offset = new Vector(_scroller.Offset.X, y);
        _scroller.UpdateLayout();
        if (_scroller.TranslatePoint(_scrollPoint, _host) is { } inHost) Extend(inHost);
    }

    internal bool IsScrolling => _scrollTimer?.IsEnabled == true;

    private void StopScroll() => _scrollTimer?.Stop();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var keys = Application.Current?.PlatformSettings?.HotkeyConfiguration;
        if (keys is null) return;
        if (keys.SelectAll.Any(g => g.Matches(e)))
        {
            var blocks = Blocks();
            if (blocks.Count == 0) return;
            Select(blocks[0], 0, blocks[^1], Length(blocks[^1]));
            e.Handled = true;
        }
        else if (_span is not null && keys.Copy.Any(g => g.Matches(e)))
        {
            CopyAll();
            e.Handled = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End)
        {
            e.Handled = Move(e.Source as Visual, e.Key, e.KeyModifiers.HasFlag(KeyModifiers.Control));
        }
    }

    // Shift with an arrow, Home or End moves the selection's end: by a letter (a word with Ctrl) the way the text reads, a line up or down at the same place across, to the line's ends, or (Ctrl with Home or End) to the document's; past a block's edge it goes on into the next.
    internal bool Move(Visual? from, Key key, bool word)
    {
        var blocks = Blocks();
        if (blocks.Count == 0) return false;
        SelectableTextBlock anchor, focus;
        int anchorAt, focusAt;
        if (_span is { } span) (anchor, anchorAt, focus, focusAt) = (span.From, span.FromAt, span.To, span.ToAt);
        else
        {
            var at = from?.FindAncestorOfType<SelectableTextBlock>(includeSelf: true)
                     ?? blocks.FirstOrDefault(b => b.SelectionStart != b.SelectionEnd)
                     ?? blocks.FirstOrDefault(b => b.IsFocused);
            if (at is null || !blocks.Contains(at)) return false;
            (anchor, anchorAt, focus, focusAt) = (at, at.SelectionStart, at, at.SelectionEnd);
        }
        var index = blocks.IndexOf(focus);
        if (index < 0) return false;
        var length = Length(focus);
        var text = (focus.Inlines is { Count: > 0 } inlines ? inlines.Text : focus.Text) ?? "";
        var rtl = focus.FlowDirection == FlowDirection.RightToLeft;
        switch (key)
        {
            case Key.Left or Key.Right:
            {
                var ahead = key == Key.Right ^ rtl;
                if (ahead && focusAt >= length)
                {
                    if (index + 1 < blocks.Count) (focus, focusAt) = (blocks[index + 1], 0);
                }
                else if (!ahead && focusAt <= 0)
                {
                    if (index > 0) (focus, focusAt) = (blocks[index - 1], Length(blocks[index - 1]));
                }
                else focusAt = word ? Word(text, focusAt, ahead) : focusAt + (ahead ? 1 : -1);
                break;
            }
            case Key.Up or Key.Down:
            {
                var down = key == Key.Down;
                var layout = focus.TextLayout;
                var caret = layout.HitTestTextPosition(Math.Clamp(focusAt, 0, length));
                var y = down ? caret.Bottom + 1 : caret.Top - 1;
                if (y >= 0 && y < layout.Height) focusAt = layout.HitTestPoint(new Point(caret.X, y)).TextPosition;
                else
                {
                    var next = down ? index + 1 : index - 1;
                    if (next < 0) focusAt = 0;
                    else if (next >= blocks.Count) focusAt = length;
                    else
                    {
                        var target = blocks[next];
                        var across = focus.TranslatePoint(new Point(caret.X + focus.Padding.Left, 0), target)?.X - target.Padding.Left ?? caret.X;
                        var ty = down ? 0.5 : Math.Max(target.TextLayout.Height - 0.5, 0);
                        (focus, focusAt) = (target, target.TextLayout.HitTestPoint(new Point(across, ty)).TextPosition);
                    }
                }
                break;
            }
            case Key.Home or Key.End:
            {
                var end = key == Key.End;
                if (word) (focus, focusAt) = end ? (blocks[^1], Length(blocks[^1])) : (blocks[0], 0);
                else
                {
                    var (start, stop) = Line(focus, focusAt);
                    focusAt = end ? stop : start;
                }
                break;
            }
        }
        focusAt = Math.Clamp(focusAt, 0, Length(focus));
        Select(anchor, anchorAt, focus, focusAt);
        focus.BringIntoView(focus.TextLayout.HitTestTextPosition(focusAt).Inflate(4));
        return true;
    }

    // The next word edge ahead or behind: over the spaces, then over the letters.
    private static int Word(string text, int at, bool ahead)
    {
        if (ahead)
        {
            while (at < text.Length && char.IsWhiteSpace(text[at])) at++;
            while (at < text.Length && !char.IsWhiteSpace(text[at])) at++;
            return at;
        }
        while (at > 0 && char.IsWhiteSpace(text[at - 1])) at--;
        while (at > 0 && !char.IsWhiteSpace(text[at - 1])) at--;
        return at;
    }

    // The start and end of the line a place is on, without its line break.
    private static (int Start, int End) Line(SelectableTextBlock block, int at)
    {
        var lines = block.TextLayout.TextLines;
        foreach (var line in lines)
        {
            var start = line.FirstTextSourceIndex;
            var end = start + line.Length;
            if (at < end || line == lines[^1]) return (start, Math.Max(start, end - line.NewLineLength));
        }
        return (0, Length(block));
    }

    // A block's own Copy (its menu): while the selection runs over blocks, copy it all.
    private void OnCopying(object? sender, RoutedEventArgs e)
    {
        if (_span is null) return;
        CopyAll();
        e.Handled = true;
    }

    // Focus leaving the host clears the selection there, as in one block; a block's menu opening keeps it.
    private void OnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.NewFocusedElement is Visual next && _host.IsVisualAncestorOf(next)) return;
        var blocks = Blocks();
        if (blocks.Any(b => b.ContextFlyout?.IsOpen == true || b.ContextMenu?.IsOpen == true))
        {
            if (_span is { } span) Apply(span);
            return;
        }
        if (_span is null) return;
        _span = null;
        foreach (var block in blocks) if (block.SelectionStart != block.SelectionEnd) Select(block, 0, 0);
    }

    /// <summary>The selected text: a line per block, a list item with its marker.</summary>
    internal string SelectedText()
    {
        var text = new StringBuilder();
        foreach (var block in Blocks())
        {
            var part = block.SelectedText;
            if (part.Length == 0) continue;
            if (text.Length > 0) text.Append(Environment.NewLine);
            if (Math.Min(block.SelectionStart, block.SelectionEnd) == 0) text.Append(Marker(block));
            text.Append(part);
        }
        return text.ToString();
    }

    private void CopyAll()
    {
        var text = SelectedText();
        if (text.Length > 0) _ = Write(TopLevel.GetTopLevel(_host)?.Clipboard, text);
    }

    private static async Task Write(IClipboard? clipboard, string text)
    {
        if (clipboard is null) return;
        try { await clipboard.SetTextAsync(text); }
        catch (Exception) { }
    }

    // A list item's marker as it reads: its own written marker, else the list's number or disc.
    private string Marker(SelectableTextBlock block)
    {
        Visual? item = block;
        for (var parent = block.GetVisualParent(); parent is not null && parent != _host; item = parent, parent = parent.GetVisualParent())
        {
            if (parent is not ProseList list || item is not Control child) continue;
            var root = child is global::Avalonia.Controls.Presenters.ContentPresenter { Child: { } inner } ? inner : child;
            if ((ProseList.GetMarker(child) ?? ProseList.GetMarker(root)) is { } own) return own + " ";
            return list.IsOrdered ? $"{list.Children.IndexOf(child) + 1}. " : "• ";
        }
        return "";
    }
}
