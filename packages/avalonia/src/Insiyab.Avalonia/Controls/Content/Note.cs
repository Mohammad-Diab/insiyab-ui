using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>A remark about the text, the web's <c>.ins-note</c>: a tone bar at its start edge (warn unless a class <c>ok bad info brand</c> says otherwise). A <see cref="Mark"/> or <see cref="Ref"/> points at it through its <c>Note</c> property, and pointing at either one lights them all (<c>:lit</c>).</summary>
public class Note : ContentControl, INoteLit
{
    void INoteLit.SetLit(bool on) => PseudoClasses.Set(":lit", on);

    private readonly List<StyledElement> _pointers = [];

    /// <summary>The marks and references that point at this note.</summary>
    public IReadOnlyList<StyledElement> Pointers => _pointers;

    internal void Attach(StyledElement pointer)
    {
        if (!_pointers.Contains(pointer)) _pointers.Add(pointer);
    }

    internal void Detach(StyledElement pointer)
    {
        _pointers.Remove(pointer);
        if (NoteLinks.Lit == this && _pointers.Count == 0) NoteLinks.Light(null);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (_pointers.Count > 0) NoteLinks.Light(this);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (NoteLinks.Lit == this) NoteLinks.Light(null);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (NoteLinks.Lit == this) NoteLinks.Light(null);
    }
}

internal interface INoteLit
{
    void SetLit(bool on);
}

/// <summary>One lit group at a time across the app, as on the web: a note and every pointer that shares it.</summary>
internal static class NoteLinks
{
    public static Note? Lit { get; private set; }

    public static void Light(Note? note)
    {
        if (Lit == note) return;
        if (Lit is { } old) Set(old, false);
        Lit = note;
        if (note is not null) Set(note, true);
    }

    private static void Set(Note note, bool on)
    {
        ((INoteLit)note).SetLit(on);
        foreach (var pointer in note.Pointers)
            if (pointer is INoteLit lit) lit.SetLit(on);
    }

    // The shared wiring for Mark and Ref: keep the note's pointer list and the help text current.
    public static void Repoint(Control pointer, Note? oldNote, Note? newNote)
    {
        oldNote?.Detach(pointer);
        if (newNote is not null && TopLevel.GetTopLevel(pointer) is not null) newNote.Attach(pointer);
        if (oldNote is not null && Lit == oldNote) Light(null);
    }

    public static void Enter(Note? note)
    {
        if (note is not null) Light(note);
    }

    public static void Leave(Note? note)
    {
        if (note is not null && Lit == note) Light(null);
    }

    public static void FocusLeft(Note? note, FocusChangedEventArgs e)
    {
        if (note is null || Lit != note) return;
        if (e.NewFocusedElement is Control next && (next == note || note.Pointers.Contains(next))) return;
        Light(null);
    }

    public static string? HelpOf(Note? note) => note?.Content as string;

    private static Control? _peekHost;

    // Whether any part of the note is inside every scroll viewport above it and inside its window.
    public static bool IsOnScreen(Note note)
    {
        if (!note.IsEffectivelyVisible || TopLevel.GetTopLevel(note) is not { } top) return false;
        var box = new Rect(note.Bounds.Size);
        for (var v = note.GetVisualParent(); v is not null; v = v.GetVisualParent())
        {
            if (v is not ScrollViewer sv) continue;
            if (note.TransformToVisual(sv) is not { } m || !box.TransformToAABB(m).Intersects(new Rect(sv.Bounds.Size))) return false;
        }
        return note.TransformToVisual(top) is { } t && box.TransformToAABB(t).Intersects(new Rect(top.ClientSize));
    }

    // The note's words as a tooltip at the pointer while its note is out of sight; one host at a time, and never over a tip the host has of its own.
    public static void Peek(Control host, Note? note, bool on)
    {
        if (on)
        {
            if (note is null || HelpOf(note) is not { Length: > 0 } text || IsOnScreen(note) || _peekHost == host) return;
            if (_peekHost is not null) Peek(_peekHost, null, false);
            if (ToolTip.GetTip(host) is not null) return;
            ToolTip.SetTip(host, text);
            ToolTip.SetPlacement(host, PlacementMode.Pointer);
            ToolTip.SetIsOpen(host, true);
            _peekHost = host;
        }
        else if (_peekHost == host)
        {
            ToolTip.SetIsOpen(host, false);
            host.ClearValue(ToolTip.TipProperty);
            host.ClearValue(ToolTip.PlacementProperty);
            _peekHost = null;
        }
    }
}
