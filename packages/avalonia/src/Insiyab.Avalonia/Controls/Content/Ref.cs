using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;

namespace Insiyab.Avalonia.Controls;

/// <summary>A small numbered reference marker in running text, the web's <c>.ins-ref</c>: a button, so it takes a <c>Click</c> or a <c>Command</c>. With <see cref="Note"/> set it lights with that note, and a click with no handler of its own brings the note into view. Inline it goes in an <c>InlineUIContainer BaselineAlignment="Superscript"</c>.</summary>
public class Ref : Button, INoteLit
{
    public static readonly StyledProperty<Note?> NoteProperty =
        AvaloniaProperty.Register<Ref, Note?>(nameof(Note));

    /// <summary>The note this marker points at.</summary>
    [ResolveByName]
    public Note? Note { get => GetValue(NoteProperty); set => SetValue(NoteProperty, value); }

    protected override Type StyleKeyOverride => typeof(Ref);

    void INoteLit.SetLit(bool on) => PseudoClasses.Set(":lit", on);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NoteProperty)
        {
            var (old, now) = change.GetOldAndNewValue<Note?>();
            NoteLinks.Repoint(this, old, now);
            AutomationProperties.SetHelpText(this, NoteLinks.HelpOf(now));
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Note?.Attach(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        NoteLinks.Peek(this, Note, false);
        NoteLinks.Leave(Note);
        Note?.Detach(this);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        NoteLinks.Enter(Note);
        if (e.Pointer.Type == PointerType.Mouse) NoteLinks.Peek(this, Note, true);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        NoteLinks.Peek(this, Note, false);
        if (!IsFocused) NoteLinks.Leave(Note);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        NoteLinks.Enter(Note);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        NoteLinks.FocusLeft(Note, e);
    }

    protected override void OnClick()
    {
        base.OnClick();
        if (Command is null) Note?.BringIntoView();
    }
}
