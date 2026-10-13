using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>One person's words in a thread, the web's <c>.ins-comment</c>: an <see cref="Avatar"/> disc, then <see cref="Author"/> and <see cref="Time"/>, the text as <c>Content</c>, optional <see cref="Actions"/> (bare buttons quieten until hovered) and nested <see cref="Replies"/> hung under a guide line. Class <c>own</c> moves the reader's own message to the other side. Stack several in a <c>StackPanel Classes="thread"</c>.</summary>
[PseudoClasses(":time", ":actions", ":replies")]
public class Comment : ContentControl
{
    public static readonly StyledProperty<object?> AvatarProperty =
        AvaloniaProperty.Register<Comment, object?>(nameof(Avatar));

    public static readonly StyledProperty<string?> AuthorProperty =
        AvaloniaProperty.Register<Comment, string?>(nameof(Author));

    public static readonly StyledProperty<string?> TimeProperty =
        AvaloniaProperty.Register<Comment, string?>(nameof(Time));

    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<Comment, object?>(nameof(Actions));

    public static readonly DirectProperty<Comment, global::Avalonia.Controls.Controls> RepliesProperty =
        AvaloniaProperty.RegisterDirect<Comment, global::Avalonia.Controls.Controls>(nameof(Replies), c => c.Replies);

    /// <summary>On a TextBox: Ctrl+Enter (Cmd+Enter on a Mac) runs this command with the text, and plain Enter still makes a new line.</summary>
    public static readonly AttachedProperty<ICommand?> SubmitCommandProperty =
        AvaloniaProperty.RegisterAttached<Comment, TextBox, ICommand?>("SubmitCommand");

    private ContentPresenter? _presenter;

    static Comment()
    {
        SubmitCommandProperty.Changed.AddClassHandler<TextBox>((box, e) =>
        {
            box.RemoveHandler(KeyDownEvent, OnSubmitKey);
            if (e.NewValue is not null) box.AddHandler(KeyDownEvent, OnSubmitKey, RoutingStrategies.Tunnel);
        });
    }

    public Comment()
    {
        Replies = [];
        Replies.CollectionChanged += (_, _) => PseudoClasses.Set(":replies", Replies.Count > 0);
        CopyCommand = new ContentCommand(() => _ = CopyAsync());
        AddHandler(ContextRequestedEvent, OnContextTunnel, RoutingStrategies.Tunnel);
    }

    public static ICommand? GetSubmitCommand(TextBox box) => box.GetValue(SubmitCommandProperty);

    public static void SetSubmitCommand(TextBox box, ICommand? value) => box.SetValue(SubmitCommandProperty, value);

    /// <summary>What the disc shows: an initial as a string, or a control such as an image, clipped to the circle.</summary>
    public object? Avatar { get => GetValue(AvatarProperty); set => SetValue(AvatarProperty, value); }

    /// <summary>The writer's name.</summary>
    public string? Author { get => GetValue(AuthorProperty); set => SetValue(AuthorProperty, value); }

    /// <summary>When it was written, as it should read ("قبل ساعتين", "10:05").</summary>
    public string? Time { get => GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    /// <summary>A row under the text, usually <c>Button Classes="bare sm"</c> items in a horizontal StackPanel.</summary>
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>The answers, indented under this comment.</summary>
    public global::Avalonia.Controls.Controls Replies { get; }

    /// <summary>Copies the selected part of the text, or all of it when nothing is selected: for a Copy item in the comment's <c>ContextMenu</c>.</summary>
    public ICommand CopyCommand { get; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
    }

    internal async Task CopyAsync()
    {
        var text = _presenter?.Child is SelectableTextBlock s
            ? (string.IsNullOrEmpty(s.SelectedText) ? s.Text : s.SelectedText)
            : Content as string;
        if (string.IsNullOrEmpty(text) || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
        await clipboard.SetTextAsync(text);
    }

    // The comment's own menu wins over its text's Copy flyout; a reply's right-click stays with the reply.
    private void OnContextTunnel(object? sender, ContextRequestedEventArgs e)
    {
        if (e.Handled || ContextMenu is not { } menu) return;
        if ((e.Source as Visual)?.FindAncestorOfType<Comment>(true) != this) return;
        e.Handled = true;
        var pointer = e.TryGetPosition(this, out _);
        menu.Placement = pointer ? PlacementMode.Pointer : PlacementMode.Bottom;
        menu.PlacementTarget = this;
        menu.Open(this);
    }

    private static void OnSubmitKey(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || e.Key != Key.Enter || (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) == 0) return;
        if (GetSubmitCommand(box) is not { } command) return;
        e.Handled = true;
        var text = box.Text ?? "";
        if (text.Trim().Length > 0 && command.CanExecute(text)) command.Execute(text);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActionsProperty) PseudoClasses.Set(":actions", change.NewValue is not null);
        else if (change.Property == TimeProperty) PseudoClasses.Set(":time", !string.IsNullOrEmpty(change.GetNewValue<string?>()));
    }
}
