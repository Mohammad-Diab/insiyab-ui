using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>What closes a dialog besides its own buttons: HTML's <c>closedby</c>. <see cref="Auto"/> is <see cref="CloseRequest"/> for a dialog and <see cref="Any"/> for a drawer.</summary>
public enum DialogClosedBy
{
    Auto,
    Any,
    CloseRequest,
    None,
}

/// <summary>Carries the value a dialog closed with: the <see cref="Dialog.ResultProperty"/> of the button that closed it, or null for Escape, the cross and an outside press.</summary>
public sealed class DialogClosedEventArgs(object? result) : EventArgs
{
    public object? Result { get; } = result;
}

/// <summary>The web's <c>dialog.ins-dialog</c> and <c>dialog.ins-drawer</c>: a modal panel over a dimmed window, with a head, a scrolling body and a foot. Classes: <c>sm</c>, <c>lg</c>; <c>drawer</c>, <c>drawer end</c>, <c>drawer bottom</c>. Declare it in <c>Resources</c> (or build it in code) and open it with <see cref="OpensProperty"/> on a button, <see cref="Open"/>, <see cref="ShowAsync(Control)"/> or <see cref="ShowAsWindowAsync"/>.</summary>
[TemplatePart("PART_Close", typeof(Button))]
[PseudoClasses(":open", ":titled", ":footed", ":pinned", ":window")]
public class Dialog : ContentControl
{
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<Dialog, object?>(nameof(Title));

    public static readonly StyledProperty<object?> FootProperty =
        AvaloniaProperty.Register<Dialog, object?>(nameof(Foot));

    public static readonly StyledProperty<DialogClosedBy> ClosedByProperty =
        AvaloniaProperty.Register<Dialog, DialogClosedBy>(nameof(ClosedBy));

    public static readonly StyledProperty<bool> ShowCloseProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(ShowClose), true);

    public static readonly StyledProperty<string> CloseLabelProperty =
        AvaloniaProperty.Register<Dialog, string>(nameof(CloseLabel), "إغلاق");

    public static readonly DirectProperty<Dialog, bool> IsOpenProperty =
        AvaloniaProperty.RegisterDirect<Dialog, bool>(nameof(IsOpen), d => d.IsOpen);

    /// <summary>On a button: the dialog it opens, as <c>data-ins-dialog="#id"</c>.</summary>
    public static readonly AttachedProperty<Dialog?> OpensProperty =
        AvaloniaProperty.RegisterAttached<Dialog, Button, Dialog?>("Opens");

    /// <summary>On a button inside a dialog: closes it with this value as the result.</summary>
    public static readonly AttachedProperty<object?> ResultProperty =
        AvaloniaProperty.RegisterAttached<Dialog, Button, object?>("Result");

    /// <summary>On a button: asks this question first, and lets the click through only on a yes, as <c>data-ins-confirm</c>.</summary>
    public static readonly AttachedProperty<string?> ConfirmProperty =
        AvaloniaProperty.RegisterAttached<Dialog, Button, string?>("Confirm");

    /// <summary>The confirm button's text, as <c>data-ins-confirm-ok</c>.</summary>
    public static readonly AttachedProperty<string?> ConfirmOkProperty =
        AvaloniaProperty.RegisterAttached<Dialog, Button, string?>("ConfirmOk");

    /// <summary><c>danger</c> makes the confirm button red, as <c>data-ins-confirm-tone</c>; a danger button gets it on its own.</summary>
    public static readonly AttachedProperty<string?> ConfirmToneProperty =
        AvaloniaProperty.RegisterAttached<Dialog, Button, string?>("ConfirmTone");

    private static readonly AttachedProperty<bool> ConfirmedProperty =
        AvaloniaProperty.RegisterAttached<Dialog, Button, bool>("Confirmed");

    private bool _isOpen;
    private TaskCompletionSource<object?>? _waiting;
    private Button? _close;
    private DrawerHost? _host;
    private DialogWindow? _window;

    static Dialog()
    {
        Button.ClickEvent.AddClassHandler(typeof(Button), OnAnyClick, RoutingStrategies.Bubble);
    }

    public Dialog()
    {
        AddHandler(Dismiss.DismissedEvent, OnDismissed);
    }

    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The foot, usually a <see cref="DialogFoot"/> of buttons; none when null.</summary>
    public object? Foot { get => GetValue(FootProperty); set => SetValue(FootProperty, value); }

    public DialogClosedBy ClosedBy { get => GetValue(ClosedByProperty); set => SetValue(ClosedByProperty, value); }

    /// <summary>Whether the head carries the close cross.</summary>
    public bool ShowClose { get => GetValue(ShowCloseProperty); set => SetValue(ShowCloseProperty, value); }

    /// <summary>The cross's accessible name.</summary>
    public string CloseLabel { get => GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    public bool IsOpen { get => _isOpen; private set => SetAndRaise(IsOpenProperty, ref _isOpen, value); }

    /// <summary>A drawer: the <c>drawer</c> class.</summary>
    public bool IsDrawer => Classes.Contains("drawer");

    /// <summary>What actually closes it, with <see cref="DialogClosedBy.Auto"/> resolved.</summary>
    public DialogClosedBy EffectiveClosedBy => ClosedBy != DialogClosedBy.Auto ? ClosedBy : IsDrawer ? DialogClosedBy.Any : DialogClosedBy.CloseRequest;

    public event EventHandler? Opened;
    public event EventHandler<DialogClosedEventArgs>? Closed;

    public static Dialog? GetOpens(Button button) => button.GetValue(OpensProperty);
    public static void SetOpens(Button button, Dialog? value) => button.SetValue(OpensProperty, value);
    public static object? GetResult(Button button) => button.GetValue(ResultProperty);
    public static void SetResult(Button button, object? value) => button.SetValue(ResultProperty, value);
    public static string? GetConfirm(Button button) => button.GetValue(ConfirmProperty);
    public static void SetConfirm(Button button, string? value) => button.SetValue(ConfirmProperty, value);
    public static string? GetConfirmOk(Button button) => button.GetValue(ConfirmOkProperty);
    public static void SetConfirmOk(Button button, string? value) => button.SetValue(ConfirmOkProperty, value);
    public static string? GetConfirmTone(Button button) => button.GetValue(ConfirmToneProperty);
    public static void SetConfirmTone(Button button, string? value) => button.SetValue(ConfirmToneProperty, value);

    internal bool IsPinned => _host is not null;

    internal void Pin(DrawerHost? host)
    {
        _host = host;
        PseudoClasses.Set(":pinned", host is not null);
    }

    internal void SetPinnedOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        PseudoClasses.Set(":open", open);
        if (open)
        {
            Opened?.Invoke(this, EventArgs.Empty);
            return;
        }
        Closed?.Invoke(this, new DialogClosedEventArgs(null));
        var waiting = _waiting;
        _waiting = null;
        waiting?.TrySetResult(null);
    }

    /// <summary>Opens it over the window <paramref name="anchor"/> is in; focus goes back to <paramref name="anchor"/> when it closes.</summary>
    public void Open(Control anchor)
    {
        if (_host is not null)
        {
            _host.OpenPane(anchor);
            return;
        }
        if (IsOpen) return;
        if (Parent is not null && Parent is not Panel { Parent: DialogLayer }) throw new InvalidOperationException("Declare a Dialog in Resources or as a DrawerHost pane, not inside the page.");
        if (!DialogStack.Open(this, anchor)) return;
        IsOpen = true;
        PseudoClasses.Set(":open", true);
        Opened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Opens it and waits for it to close; the result is the closing button's <see cref="ResultProperty"/>, or null.</summary>
    public Task<object?> ShowAsync(Control anchor)
    {
        _waiting?.TrySetResult(null);
        var waiting = _waiting = new TaskCompletionSource<object?>();
        Open(anchor);
        if (!IsOpen)
        {
            _waiting = null;
            waiting.TrySetResult(null);
        }
        return waiting.Task;
    }

    /// <summary>Opens it in its own owned window when <paramref name="owner"/> is in a desktop window (a drawer never is), else over the page as <see cref="ShowAsync(Control)"/> does; <paramref name="modal"/> blocks the owner.</summary>
    public Task<object?> ShowAsWindowAsync(Control owner, bool modal = false)
    {
        if (IsOpen) return Task.FromResult<object?>(null);
        if (IsDrawer || OverlayHost.Top(owner) is not Window ownerWindow) return ShowAsync(owner);
        _waiting?.TrySetResult(null);
        var waiting = _waiting = new TaskCompletionSource<object?>();
        _window = new DialogWindow(this, owner, ownerWindow);
        PseudoClasses.Set(":window", true);
        IsOpen = true;
        PseudoClasses.Set(":open", true);
        Opened?.Invoke(this, EventArgs.Empty);
        _window.Present(modal);
        return waiting.Task;
    }

    /// <summary>Opens a dialog over <paramref name="owner"/>'s window and waits: a string <paramref name="content"/> becomes a wrapping text, and an action button closes it only if it carries <see cref="ResultProperty"/>.</summary>
    public static Task<object?> ShowAsync(Control owner, object? title, object? content, params Button[] actions)
    {
        var dialog = new Dialog { Title = title, Content = content is string s ? new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap } : content };
        if (actions.Length > 0)
        {
            var foot = new DialogFoot();
            foreach (var a in actions) foot.Children.Add(a);
            dialog.Foot = foot;
        }
        return dialog.ShowAsync(owner);
    }

    internal void OnWindowGone()
    {
        _window = null;
        PseudoClasses.Set(":window", false);
        if (IsOpen) Close();
    }

    public void Close(object? result = null)
    {
        if (!IsOpen) return;
        IsOpen = false;
        PseudoClasses.Set(":open", false);
        if (_window is not null) _window.CloseFromDialog();
        else if (_host is not null) _host.OnPaneClosed();
        else DialogStack.Close(this);
        Closed?.Invoke(this, new DialogClosedEventArgs(result));
        var waiting = _waiting;
        _waiting = null;
        waiting?.TrySetResult(result);
    }

    /// <summary>Escape or an outside press, honouring <see cref="ClosedBy"/>.</summary>
    internal bool RequestClose(bool outside)
    {
        var by = EffectiveClosedBy;
        if (by == DialogClosedBy.None || (outside && by != DialogClosedBy.Any)) return false;
        Close();
        return true;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_close is not null) _close.Click -= OnCloseClick;
        _close = e.NameScope.Find<Button>("PART_Close");
        if (_close is not null) _close.Click += OnCloseClick;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty) PseudoClasses.Set(":titled", change.NewValue is not null);
        else if (change.Property == FootProperty) PseudoClasses.Set(":footed", change.NewValue is not null);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!Classes.Contains("drawer") && !Classes.Contains("dialog")) Classes.Add("dialog");
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || _host is not null || !IsOpen) return;
        if (_window is null && DialogStack.Of(this) is { } stack && !stack.IsTop(this)) return;
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            RequestClose(outside: false);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            if (e.Source is TextBox { AcceptsReturn: true }) return;
            if (DefaultButton() is { } button) Press(button);
            e.Handled = true;
        }
    }

    // The control focus starts on when the caller knows better than the order below, as the cancel button of a question.
    internal Control? InitialFocus { get; set; }

    // Where focus starts: the first field of the body, else the default button, else the first focusable thing.
    internal Control? FocusTarget()
    {
        if (InitialFocus is { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } initial && this.IsVisualAncestorOf(initial)) return initial;
        var body = this.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(s => s.Name == "PART_Scroll");
        return (body is null ? null : FocusManager.FindFirstFocusableElement(body)) as Control
            ?? DefaultButton()
            ?? FocusManager.FindFirstFocusableElement(this) as Control;
    }

    // The foot button Enter presses: the one marked IsDefault, else the last main action.
    internal Button? DefaultButton()
    {
        if (Foot is not Panel foot) return null;
        var buttons = foot.Children.OfType<Button>().Where(b => b.IsVisible && b.IsEffectivelyEnabled).ToList();
        return buttons.FirstOrDefault(b => b.IsDefault)
            ?? buttons.LastOrDefault(b => b.Classes.Contains("primary") || b.Classes.Contains("danger") || b.Classes.Contains("success") || b.Classes.Contains("warning"));
    }

    internal static void Press(Button button)
    {
        var click = new RoutedEventArgs(Button.ClickEvent);
        button.RaiseEvent(click);
        if (!click.Handled && button.Command is { } command && command.CanExecute(button.CommandParameter))
            command.Execute(button.CommandParameter);
    }

    // ins:Dismiss raises this on the dialog and then hides it; the hiding is undone so the dialog can open again.
    private void OnDismissed(object? sender, RoutedEventArgs e)
    {
        if (e.Source != this) return;
        Close();
        Dispatcher.UIThread.Post(() => ClearValue(IsVisibleProperty), DispatcherPriority.Send);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Close();
    }

    /// <summary>A small dialog with a question and two buttons, as <c>Insiyab.confirm()</c>: true on the confirm button, false on cancel, Escape or the cross. Focus starts on cancel.</summary>
    public static async Task<bool> ConfirmAsync(Control owner, string? title, string message, bool danger = false, string? confirm = null, string? cancel = null)
    {
        var arabic = owner.FlowDirection == FlowDirection.RightToLeft;
        var no = new Button { Content = cancel ?? (arabic ? "إلغاء" : "Cancel"), Classes = { "bare" } };
        var yes = new Button { Content = confirm ?? (arabic ? "تأكيد" : "OK"), Classes = { danger ? "danger" : "primary" } };
        SetResult(no, "cancel");
        SetResult(yes, "ok");
        var question = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
        if (title is null) question.Classes.Add("dialog-title");
        var dialog = new Dialog
        {
            Title = title,
            Content = question,
            Foot = new DialogFoot { Children = { no, yes } },
            Classes = { "sm", "confirm" },
        };
        dialog.InitialFocus = no;
        return Equals(await dialog.ShowAsync(owner), "ok");
    }

    private static void OnAnyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || e.Source != button) return;

        if (GetConfirm(button) is { } question)
        {
            if (button.GetValue(ConfirmedProperty))
            {
                button.SetValue(ConfirmedProperty, false);
            }
            else
            {
                e.Handled = true;
                AskThenReplay(button, question);
                return;
            }
        }

        if (GetOpens(button) is { } target)
        {
            target.Open(button);
            return;
        }

        if (button.IsSet(ResultProperty))
        {
            var owner = button.FindLogicalAncestorOfType<Dialog>();
            if (owner is null) return;
            e.Handled = true;
            owner.Close(GetResult(button));
        }
    }

    // The click is held back, the question asked, and on a yes the same click is replayed, so Click handlers and the Command still run.
    private static async void AskThenReplay(Button button, string question)
    {
        var danger = button.Classes.Contains("danger") || button.Classes.Contains("ghost-danger") || GetConfirmTone(button) == "danger";
        var yes = await ConfirmAsync(button, null, question, danger, GetConfirmOk(button));
        if (!yes) return;
        button.SetValue(ConfirmedProperty, true);
        Press(button);
        button.SetValue(ConfirmedProperty, false);
    }
}
