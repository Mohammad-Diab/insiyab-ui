using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace Insiyab.Avalonia.Controls;

/// <summary>Part of a text highlighted in a tone, the web's <c>.ins-mark</c>: an inline (a <see cref="Span"/>) in a <c>TextBlock</c>, so it wraps across lines as one highlight. Warn unless a class <c>ok bad info brand</c> says otherwise. Set <see cref="Note"/> to tie it to a <see cref="Controls.Note"/>: pointing at either lights both (<c>:lit</c>), and a click on the mark brings its note into view.</summary>
public class Mark : Span, INoteLit
{
    public static readonly StyledProperty<Note?> NoteProperty =
        AvaloniaProperty.Register<Mark, Note?>(nameof(Note));

    private TextBlock? _host;
    private bool _inside;
    private Cursor? _hand;

    /// <summary>The note that says why this part is marked; several marks may share one.</summary>
    [ResolveByName]
    public Note? Note { get => GetValue(NoteProperty); set => SetValue(NoteProperty, value); }

    /// <summary>A plain-text mark: its words as one run.</summary>
    public string? Text
    {
        get => Inlines.Count == 1 && Inlines[0] is Run run ? run.Text : null;
        set
        {
            Inlines.Clear();
            if (value is not null) Inlines.Add(new Run(value));
        }
    }

    void INoteLit.SetLit(bool on) => PseudoClasses.Set(":lit", on);

    /// <summary>Whether the pointer is over this mark.</summary>
    internal bool IsPointerInside => _inside;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NoteProperty)
        {
            var (old, now) = change.GetOldAndNewValue<Note?>();
            old?.Detach(this);
            if (now is not null && _host is not null) now.Attach(this);
            if (old is not null && NoteLinks.Lit == old) NoteLinks.Light(null);
            PseudoClasses.Set(":noted", now is not null);
        }
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _host = this.FindLogicalAncestorOfType<TextBlock>();
        if (_host is null) return;
        _host.PointerMoved += OnHostMoved;
        _host.PointerExited += OnHostExited;
        _host.PointerReleased += OnHostReleased;
        Note?.Attach(this);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        if (_host is not null)
        {
            NoteLinks.Peek(_host, Note, false);
            _host.PointerMoved -= OnHostMoved;
            _host.PointerExited -= OnHostExited;
            _host.PointerReleased -= OnHostReleased;
            _host = null;
        }
        NoteLinks.Leave(Note);
        Note?.Detach(this);
    }

    // The span's place in its TextBlock's text, counted the way the inlines build their runs.
    internal (int Start, int End)? Range()
    {
        if (_host is null) return null;
        var at = 0;
        return Walk(_host.Inlines, ref at);
    }

    private (int, int)? Walk(InlineCollection? inlines, ref int at)
    {
        if (inlines is null) return null;
        foreach (var inline in inlines)
        {
            var start = at;
            if (inline is Span span)
            {
                var found = Walk(span.Inlines, ref at);
                if (inline == this) return (start, at);
                if (found is not null) return found;
            }
            else if (inline is Run run) at += run.Text?.Length ?? 0;
            else at += 1;
        }
        return null;
    }

    internal bool Contains(Point point)
    {
        if (_host is null || Range() is not { } r || r.End <= r.Start) return false;
        var p = point - new Point(_host.Padding.Left, _host.Padding.Top);
        foreach (var rect in _host.TextLayout.HitTestTextRange(r.Start, r.End - r.Start))
            if (rect.Inflate(1).Contains(p)) return true;
        return false;
    }

    private void OnHostMoved(object? sender, PointerEventArgs e)
    {
        if (_host is null) return;
        var inside = Contains(e.GetPosition(_host));
        if (inside == _inside) return;
        _inside = inside;
        if (Note is null) return;
        _host.Cursor = inside ? _hand ??= new Cursor(StandardCursorType.Hand) : null;
        if (inside) NoteLinks.Enter(Note);
        else NoteLinks.Leave(Note);
        if (inside && e.Pointer.Type == PointerType.Mouse) NoteLinks.Peek(_host, Note, true);
        else if (!inside) NoteLinks.Peek(_host, Note, false);
    }

    private void OnHostExited(object? sender, PointerEventArgs e)
    {
        if (_host is not null) NoteLinks.Peek(_host, Note, false);
        if (!_inside) return;
        _inside = false;
        if (_host is not null && Note is not null) _host.Cursor = null;
        NoteLinks.Leave(Note);
    }

    private void OnHostReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_host is null || Note is not { } note || e.InitialPressMouseButton is not (MouseButton.Left or MouseButton.None)) return;
        if (!Contains(e.GetPosition(_host))) return;
        note.BringIntoView();
        if (e.Pointer.Type != PointerType.Mouse)
        {
            NoteLinks.Enter(note);
            DispatcherTimer.RunOnce(() => NoteLinks.Leave(note), TimeSpan.FromMilliseconds(1500));
        }
        e.Handled = true;
    }
}
