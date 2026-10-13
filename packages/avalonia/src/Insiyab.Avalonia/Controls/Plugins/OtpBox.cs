using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Insiyab.Avalonia.Controls;

/// <summary>Carries the code an <see cref="OtpBox"/> just completed.</summary>
public class OtpCompletedEventArgs(RoutedEvent routedEvent, string value) : RoutedEventArgs(routedEvent)
{
    public string Value { get; } = value;
}

/// <summary>The one-time code field, the web's <c>data-ins-otp</c>: one real text box drawn as a block with a dot per character. Paste, an OTP suggestion and a screen reader all meet one ordinary field; typing is always at the end.</summary>
[TemplatePart("PART_CellsHost", typeof(Panel))]
[PseudoClasses(":invalid", ":complete")]
public class OtpBox : TextBox
{
    public static readonly StyledProperty<int> LengthProperty =
        AvaloniaProperty.Register<OtpBox, int>(nameof(Length), 6, coerce: (_, v) => Math.Clamp(v, 4, 10));

    public static readonly StyledProperty<bool> AlphanumericProperty =
        AvaloniaProperty.Register<OtpBox, bool>(nameof(Alphanumeric));

    public static readonly StyledProperty<bool> IsInvalidProperty =
        AvaloniaProperty.Register<OtpBox, bool>(nameof(IsInvalid));

    public static readonly StyledProperty<ICommand?> SubmitCommandProperty =
        AvaloniaProperty.Register<OtpBox, ICommand?>(nameof(SubmitCommand));

    public static readonly StyledProperty<string?> LocaleProperty =
        AvaloniaProperty.Register<OtpBox, string?>(nameof(Locale));

    public static readonly DirectProperty<OtpBox, bool> IsCompleteProperty =
        AvaloniaProperty.RegisterDirect<OtpBox, bool>(nameof(IsComplete), o => o.IsComplete);

    public static readonly DirectProperty<OtpBox, string?> ValidationMessageProperty =
        AvaloniaProperty.RegisterDirect<OtpBox, string?>(nameof(ValidationMessage), o => o.ValidationMessage);

    public static readonly RoutedEvent<OtpCompletedEventArgs> CompletedEvent =
        RoutedEvent.Register<OtpBox, OtpCompletedEventArgs>(nameof(Completed), RoutingStrategies.Bubble);

    private readonly List<OtpCell> _cells = [];
    private Panel? _host;
    private bool _syncing, _quiet, _edited, _left, _complete;
    private string _last = string.Empty;
    private string? _message;

    static OtpBox()
    {
        AcceptsReturnProperty.OverrideDefaultValue<OtpBox>(false);
        FocusAdornerProperty.OverrideDefaultValue<OtpBox>(null);
    }

    public OtpBox()
    {
        SetCurrentValue(FlowDirectionProperty, FlowDirection.LeftToRight);
        TextInputOptions.SetContentType(this, TextInputContentType.Digits);
        AddHandler(PastingFromClipboardEvent, OnPasting);
    }

    /// <summary>How many characters, 4 to 10.</summary>
    public int Length { get => GetValue(LengthProperty); set => SetValue(LengthProperty, value); }

    /// <summary>Latin letters too, upper-cased; digits only otherwise (<c>data-ins-otp-chars="alnum"</c>).</summary>
    public bool Alphanumeric { get => GetValue(AlphanumericProperty); set => SetValue(AlphanumericProperty, value); }

    /// <summary>A code the server turned down (<c>aria-invalid</c>): the block turns red until the code is typed again.</summary>
    public bool IsInvalid { get => GetValue(IsInvalidProperty); set => SetValue(IsInvalidProperty, value); }

    /// <summary>Run with the code when the last character is entered (<c>data-ins-otp-submit</c>); unset, nothing is submitted.</summary>
    public ICommand? SubmitCommand { get => GetValue(SubmitCommandProperty); set => SetValue(SubmitCommandProperty, value); }

    /// <summary>"ar" or "en" for the message; unset, the direction decides.</summary>
    public string? Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public bool IsComplete { get => _complete; private set => SetAndRaise(IsCompleteProperty, ref _complete, value); }

    /// <summary>"Enter the whole code" while the code is part-typed; null when empty or complete.</summary>
    public string? ValidationMessage { get => _message; private set => SetAndRaise(ValidationMessageProperty, ref _message, value); }

    /// <summary>The code was completed: the web's <c>ins:otp</c>.</summary>
    public event EventHandler<OtpCompletedEventArgs>? Completed
    {
        add => AddHandler(CompletedEvent, value);
        remove => RemoveHandler(CompletedEvent, value);
    }

    /// <summary>Sets the code quietly, with no <see cref="Completed"/>: <c>Insiyab.otp(field, value)</c>. An empty string clears it.</summary>
    public void SetCode(string? value)
    {
        _quiet = true;
        try
        {
            SetCurrentValue(TextProperty, value ?? string.Empty);
            Sync();
        }
        finally { _quiet = false; }
    }

    private void OnPasting(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _ = PasteAsync();
    }

    private async Task PasteAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
        PasteText(await clipboard.TryGetTextAsync());
    }

    // A paste that is a whole code replaces the field; a shorter one is added at the end, as typing is.
    internal void PasteText(string? pasted)
    {
        if (!IsEffectivelyEnabled || IsReadOnly) return;
        var code = Clean(pasted);
        if (code.Length == 0) return;
        SetCurrentValue(TextProperty, code.Length == Length ? code : (Text ?? string.Empty) + (pasted ?? string.Empty));
    }

    internal string Clean(string? value)
    {
        var s = PluginText.Latin(value);
        var sb = new StringBuilder(s.Length);
        foreach (var raw in s)
        {
            var c = Alphanumeric ? char.ToUpperInvariant(raw) : raw;
            if (c >= '0' && c <= '9') sb.Append(c);
            else if (Alphanumeric && c >= 'A' && c <= 'Z') sb.Append(c);
            if (sb.Length == Length) break;
        }
        return sb.ToString();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host = e.NameScope.Find<Panel>("PART_CellsHost");
        BuildCells();
        Sync(quiet: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            if (_syncing) return;
            if (!_quiet && _attached)
            {
                _edited = true;
                if (IsInvalid) SetCurrentValue(IsInvalidProperty, false);
            }
            Sync(_quiet || !_attached);
        }
        else if (change.Property == LengthProperty)
        {
            BuildCells();
            Sync(quiet: true);
        }
        else if (change.Property == AlphanumericProperty)
        {
            TextInputOptions.SetContentType(this, Alphanumeric ? TextInputContentType.Normal : TextInputContentType.Digits);
            TextInputOptions.SetUppercase(this, Alphanumeric);
            Sync(quiet: true);
        }
        else if (change.Property == CaretIndexProperty || change.Property == SelectionStartProperty || change.Property == SelectionEndProperty)
        {
            var end = (Text ?? string.Empty).Length;
            if (CaretIndex != end || SelectionStart != end || SelectionEnd != end)
                Dispatcher.UIThread.Post(ToEnd);
        }
        else if (change.Property == IsInvalidProperty || change.Property == IsEnabledProperty)
        {
            Render();
        }
        else if (change.Property == LocaleProperty || change.Property == FlowDirectionProperty)
        {
            Check();
        }
    }

    private bool _attached;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
    }

    // One place to type: the end. Arrows, Home and End have nowhere to go.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown)
        {
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        Dispatcher.UIThread.Post(ToEnd);
        Render();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (_edited) _left = true;
        Render();
    }

    private void ToEnd()
    {
        var end = (Text ?? string.Empty).Length;
        if (CaretIndex != end) SetCurrentValue(CaretIndexProperty, end);
        if (SelectionStart != end) SetCurrentValue(SelectionStartProperty, end);
        if (SelectionEnd != end) SetCurrentValue(SelectionEndProperty, end);
    }

    // After every change: cleaned, redrawn, checked, and announced the moment the last box fills.
    private void Sync(bool quiet = true)
    {
        var was = _last;
        var value = Clean(Text);
        if (value != (Text ?? string.Empty))
        {
            _syncing = true;
            try { SetCurrentValue(TextProperty, value); }
            finally { _syncing = false; }
        }
        ToEnd();
        _last = value;
        IsComplete = value.Length == Length;
        Check();
        Render();
        if (!quiet && IsComplete && value != was)
        {
            RaiseEvent(new OtpCompletedEventArgs(CompletedEvent, value));
            if (SubmitCommand is { } command && command.CanExecute(value)) command.Execute(value);
        }
    }

    // The box is always left to right, so the page's language is the one around it.
    private void Check()
    {
        var len = (Text ?? string.Empty).Length;
        if (len == 0 || len >= Length) { ValidationMessage = null; return; }
        ValidationMessage = PluginText.IsArabic(this.GetVisualParent() as Control ?? this, Locale)
            ? $"اكتب الرمز كاملًا: {Length}{(Alphanumeric ? " خانات." : " أرقام.")}"
            : $"Enter all {Length}{(Alphanumeric ? " characters." : " digits.")}";
    }

    private void BuildCells()
    {
        if (_host is null) return;
        _host.Children.Clear();
        _cells.Clear();
        for (var i = 0; i < Length; i++)
        {
            var cell = new OtpCell();
            _cells.Add(cell);
            _host.Children.Add(cell);
        }
        Render();
    }

    private void Render()
    {
        var value = Text ?? string.Empty;
        var invalid = IsInvalid || (_left && ValidationMessage is not null);
        PseudoClasses.Set(":invalid", invalid);
        PseudoClasses.Set(":complete", value.Length == Length);
        var focused = IsFocused && IsEffectivelyEnabled;
        var at = Math.Min(value.Length, _cells.Count - 1);
        for (var i = 0; i < _cells.Count; i++)
        {
            var ch = i < value.Length ? value[i].ToString() : string.Empty;
            _cells[i].Character = ch;
            _cells[i].SetState(ch.Length > 0, focused && i == at, invalid);
        }
    }
}

/// <summary>One drawn box of an <see cref="OtpBox"/>: the character, the dot under it, and the caret on the next one to type.</summary>
[PseudoClasses(":filled", ":active", ":invalid")]
public class OtpCell : TemplatedControl
{
    public static readonly StyledProperty<string?> CharacterProperty =
        AvaloniaProperty.Register<OtpCell, string?>(nameof(Character));

    static OtpCell() => TransformOperationsAnimator.Ensure();

    public OtpCell()
    {
        IsHitTestVisible = false;
        Focusable = false;
    }

    public string? Character { get => GetValue(CharacterProperty); set => SetValue(CharacterProperty, value); }

    internal void SetState(bool filled, bool active, bool invalid)
    {
        PseudoClasses.Set(":filled", filled);
        PseudoClasses.Set(":active", active);
        PseudoClasses.Set(":invalid", invalid);
    }
}
